using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using VovinamApi.Data;
using VovinamApi.Hubs;
using VovinamApi.Models;
using VovinamApi.Services;

// Console mặc định trên Windows dùng codepage cũ, hiển thị tiếng Việt có
// dấu thành dấu "?" (không phải lỗi logic, chỉ là hiển thị) — ép UTF-8
// để các thông báo lỗi tiếng Việt (như check Jwt:Key bên dưới) đọc được.
Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// Ghi thêm log ra file cạnh .exe (logs/app-YYYY-MM-DD.log) — mặc định
// ASP.NET Core chỉ in ra console, mà console dễ bị đóng/không hiện lúc
// chạy ngày thi đấu, mất hết dấu vết nếu có lỗi. Dùng luôn hạ tầng
// logging có sẵn (không thêm gói ngoài như Serilog để khỏi phụ thuộc
// thêm) — FileLoggerProvider định nghĩa ở cuối file này.
var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logDir);
var logFilePath = Path.Combine(logDir, $"app-{DateTime.Now:yyyy-MM-dd}.log");
builder.Logging.AddProvider(new FileLoggerProvider(logFilePath));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddSingleton<LiveCourtStateStore>();
builder.Services.AddSingleton<ManHinhCongKhaiLauncher>();

// Tải ảnh VĐV từ URL WordPress về local. Tắt auto-redirect để service
// tự kiểm tra lại từng URL redirect, tránh redirect vào localhost/private IP.
builder.Services
    .AddHttpClient<AthleteImageService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("VovinamTournament/1.0");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
    });

// SQLite — 1 file duy nhất (vovinam.db) nằm CẠNH .exe, tính đường dẫn
// tuyệt đối theo AppContext.BaseDirectory (không dùng thư mục làm việc
// hiện tại, vì cái đó đổi tuỳ theo cách mở .exe — double-click, tạo
// shortcut riêng, chạy từ Task Scheduler... mỗi kiểu có thể cho ra 1 thư
// mục làm việc khác nhau). Máy nào cũng chỉ cần chạy .exe, không cần cài
// SQL Server/LocalDB riêng như trước nữa.
var dbPath = Path.Combine(AppContext.BaseDirectory, "vovinam.db");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Thiếu cấu hình Jwt:Key (đang rỗng). Chạy: dotnet user-secrets set \"Jwt:Key\" \"<key>\" " +
        "trong thư mục vovinam-backend (hoặc set environment variable Jwt__Key nếu chạy bản publish), " +
        "dùng ĐÚNG 1 giá trị y hệt cho mọi nơi chạy, rồi chạy lại app.");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!)),
        };
    });

// Cổng đăng ký (React/Vite, chạy ở port riêng lúc dev) gọi API qua trình
// duyệt — cần CORS. Khi đã build+gộp vào wwwroot (cùng gốc, đúng cách
// đang chạy thử ở đây) thì CORS này không còn tác dụng gì (không hại) vì
// không còn cross-origin nữa.
builder.Services.AddCors(options =>
{
    options.AddPolicy("PortalFrontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Tự tạo/cập nhật đúng cấu trúc DB lúc khởi động — máy mới chỉ cần chạy
// .exe là có ngay file vovinam.db đúng schema mới nhất, không cần cài
// .NET SDK hay tự chạy "dotnet ef database update" tay nữa. Bật WAL để
// nhiều sân/nhiều người cùng đọc-ghi lúc thi đấu không bị chặn nhau (mặc
// định SQLite khoá cả file mỗi lần ghi, WAL cho phép đọc trong lúc đang
// ghi dở).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

// Lưới an toàn cuối cùng — lỗi chưa lường trước trước đây rơi thẳng vào
// response lỗi mà không có gì ghi lại, không biết đường nào lần lúc sập
// giữa giải. Giờ ghi vào file log (kèm log ở trên) rồi mới trả lỗi gọn
// cho client, không lộ chi tiết ra ngoài.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalExceptionHandler");
        logger.LogError(ex, "Lỗi chưa lường trước tại {Path}", context.Request.Path);
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = "Có lỗi xảy ra ở server — đã ghi vào logs/, xem lại file mới nhất để biết chi tiết.",
            }));
        }
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("PortalFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<MatchHub>("/hubs/match");
app.MapFallbackToFile("index.html");

// Tự mở trình duyệt vào đúng trang đăng nhập BTC ngay khi server sẵn
// sàng nhận request. Mở đúng theo IP LAN thật của máy (không phải
// "localhost") — vì mục đích chính là để BTC nhìn thẳng vào thanh địa
// chỉ là biết ngay URL cần đưa cho trọng tài/màn hình công khai/BTK sân
// khác, khỏi phải tự chạy ipconfig tìm tay mỗi lần mở giải.
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var urlDauTien = app.Urls.FirstOrDefault();
        var port = urlDauTien != null && Uri.TryCreate(urlDauTien, UriKind.Absolute, out var uri)
            ? uri.Port
            : 2004; 
        var host = LayIpLan() ?? "localhost";
        var duongDanMo = $"http://{host}:{port}/";
        Process.Start(new ProcessStartInfo(duongDanMo) { UseShellExecute = true });
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex,
            "Không tự mở được trình duyệt — vào tay địa chỉ http://localhost:2004/ nếu cần.");
    }
});

// Lấy đúng IP LAN thật của máy — mẹo: "kết nối" UDP tới 1 địa chỉ ngoài
// (8.8.8.8) không thật sự gửi gì đi cả (UDP không bắt tay), chỉ để hệ
// điều hành tự chọn đúng card mạng/IP sẽ dùng để ra ngoài theo bảng định
// tuyến sẵn có — vẫn hoạt động dù máy không có Internet thật, miễn có
// mạng LAN/WiFi đang kết nối. Trả về null (rồi rơi về "localhost") nếu
// máy không có mạng nào cả.
static string? LayIpLan()
{
    try
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect("8.8.8.8", 65530);
        return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString();
    }
    catch
    {
        return null;
    }
}

app.Run();

// Logger tối giản, tự ghi text ra file — không dùng thư viện ngoài
// (Serilog/NLog...) để khỏi phải thêm gói mới chỉ vì mỗi việc ghi log.
// Chỉ ghi từ Warning trở lên, tránh log Information dồn dập làm đầy ổ
// đĩa qua nhiều ngày thi đấu.
sealed class FileLoggerProvider(string filePath) : ILoggerProvider
{
    private readonly object _writeLock = new();

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, filePath, _writeLock);

    public void Dispose() { }

    private sealed class FileLogger(string category, string filePath, object writeLock) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {category}: {formatter(state, exception)}";
            if (exception != null) line += Environment.NewLine + exception;
            lock (writeLock)
            {
                try
                {
                    File.AppendAllText(filePath, line + Environment.NewLine);
                }
                catch
                {
                    // Đừng để lỗi ghi log (ổ đĩa đầy, mất quyền ghi...) làm sập
                    // cả app — mất log còn hơn mất luôn phần mềm đang chạy dở.
                }
            }
        }
    }
}