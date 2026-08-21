using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VovinamApi.Data;
using VovinamApi.Hubs;
using VovinamApi.Models;
using VovinamApi.Services;

// Console mặc định trên Windows dùng codepage cũ, hiển thị tiếng Việt có
// dấu thành dấu "?" (không phải lỗi logic, chỉ là hiển thị) — ép UTF-8
// để các thông báo lỗi tiếng Việt (như check Jwt:Key bên dưới) đọc được.
Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddSingleton<LiveCourtStateStore>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

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

app.Run();