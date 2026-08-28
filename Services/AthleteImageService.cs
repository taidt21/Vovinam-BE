using System.Net;
using System.Net.Sockets;

namespace VovinamApi.Services;

public sealed class AthleteImageService
{
    private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB
    private const int MaxRedirects = 3;

    private readonly HttpClient _httpClient;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AthleteImageService> _logger;
    private readonly HashSet<string> _trustedLocalHosts;

    public AthleteImageService(
        HttpClient httpClient,
        IWebHostEnvironment env,
        ILogger<AthleteImageService> logger,
        IConfiguration config)
    {
        _httpClient = httpClient;
        _env = env;
        _logger = logger;

        // Danh sách host được TIN CẬY, bỏ qua kiểm tra IP nội bộ/loopback
        // bên dưới — dùng cho site WordPress chạy local lúc dev (VD domain
        // .test của Laragon luôn trỏ về 127.0.0.1, bị chặn oan nếu không có
        // ngoại lệ này). CHỈ áp dụng cho ĐÚNG host được liệt kê ra trong
        // cấu hình (appsettings.Development.json) — appsettings.json (bản
        // production) không khai báo gì thì vẫn chặn private/loopback như
        // cũ, không mở thêm lỗ hổng nào cho host lạ.
        var trusted = config.GetSection("AthleteImages:TrustedLocalHosts").Get<string[]>() ?? [];
        _trustedLocalHosts = new HashSet<string>(trusted, StringComparer.OrdinalIgnoreCase);
    }

    public bool IsManagedLocalPath(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && value.StartsWith("/uploads/athletes/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Nhận URL ảnh từ WordPress, tải về wwwroot/uploads/athletes và trả
    /// đường dẫn local dạng /uploads/athletes/xxx.jpg.
    /// Nếu tải thất bại trả null để việc import VĐV vẫn tiếp tục.
    /// </summary>
    public async Task<string?> TryDownloadAsync(string? source, CancellationToken cancellationToken = default)
    {
        var raw = source?.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // Khi PUT VĐV, frontend gửi lại đường dẫn local hiện có thì giữ nguyên,
        // tuyệt đối không cố tải lại nó qua HTTP.
        if (IsManagedLocalPath(raw)) return raw;

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            _logger.LogWarning("Bỏ qua ảnh VĐV vì URL không hợp lệ: {Url}", raw);
            return null;
        }

        string? createdFile = null;

        try
        {
            using var response = await GetSafeResponseAsync(uri, cancellationToken);
            if (response is null) return null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Không tải được ảnh VĐV {Url}. HTTP {Status}", raw, (int)response.StatusCode);
                return null;
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
            var extension = mediaType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/avif" => ".avif",
                _ => null,
            };

            if (extension is null)
            {
                _logger.LogWarning("Bỏ qua ảnh VĐV {Url}: Content-Type {ContentType} không được hỗ trợ", raw, mediaType);
                return null;
            }

            if (response.Content.Headers.ContentLength is long declaredLength && declaredLength > MaxImageBytes)
            {
                _logger.LogWarning("Bỏ qua ảnh VĐV {Url}: ảnh lớn hơn 5 MB", raw);
                return null;
            }

            var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : _env.WebRootPath;

            var folder = Path.Combine(webRoot, "uploads", "athletes");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folder, fileName);
            createdFile = fullPath;

            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            var buffer = new byte[81920];
            long total = 0;
            while (true)
            {
                var read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0) break;

                total += read;
                if (total > MaxImageBytes)
                {
                    await output.DisposeAsync();
                    TryDeletePhysicalFile(fullPath);
                    createdFile = null;
                    _logger.LogWarning("Bỏ qua ảnh VĐV {Url}: dữ liệu tải về vượt quá 5 MB", raw);
                    return null;
                }

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            createdFile = null; // file hoàn chỉnh, không dọn ở catch/finally
            return $"/uploads/athletes/{fileName}";
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (createdFile is not null) TryDeletePhysicalFile(createdFile);
            _logger.LogWarning("Timeout khi tải ảnh VĐV: {Url}", raw);
            return null;
        }
        catch (Exception ex)
        {
            if (createdFile is not null) TryDeletePhysicalFile(createdFile);
            _logger.LogWarning(ex, "Không tải được ảnh VĐV: {Url}", raw);
            return null;
        }
    }

    public void DeleteLocalFile(string? localPath)
    {
        if (!IsManagedLocalPath(localPath)) return;

        try
        {
            var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : _env.WebRootPath;

            // Chỉ lấy filename để không cho path traversal từ dữ liệu DB.
            var fileName = Path.GetFileName(localPath!.Replace('\\', '/'));
            var fullPath = Path.Combine(webRoot, "uploads", "athletes", fileName);
            TryDeletePhysicalFile(fullPath);
        }
        catch (Exception ex)
        {
            // Xóa file thất bại không được làm hỏng thao tác DB.
            _logger.LogWarning(ex, "Không xóa được ảnh local của VĐV: {Path}", localPath);
        }
    }

    private static void TryDeletePhysicalFile(string fullPath)
    {
        try
        {
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        catch
        {
            // Best effort cleanup. Caller sẽ log ở ngữ cảnh phù hợp nếu cần.
        }
    }

    private async Task<HttpResponseMessage?> GetSafeResponseAsync(Uri initialUri, CancellationToken cancellationToken)
    {
        var current = initialUri;

        for (var redirectCount = 0; redirectCount <= MaxRedirects; redirectCount++)
        {
            if (!await IsSafePublicHttpUriAsync(current))
            {
                _logger.LogWarning("Chặn URL ảnh không an toàn/private: {Url}", current);
                return null;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if ((int)response.StatusCode is >= 300 and <= 399 && response.Headers.Location is not null)
            {
                if (redirectCount == MaxRedirects)
                {
                    response.Dispose();
                    _logger.LogWarning("Ảnh redirect quá nhiều lần: {Url}", initialUri);
                    return null;
                }

                var next = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(current, response.Headers.Location);
                response.Dispose();
                current = next;
                continue;
            }

            return response;
        }

        return null;
    }

    private async Task<bool> IsSafePublicHttpUriAsync(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        if (string.IsNullOrWhiteSpace(uri.Host)) return false;

        if (_trustedLocalHosts.Contains(uri.Host)) return true;

        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost);
        }
        catch
        {
            return false;
        }

        return addresses.Length > 0 && addresses.All(IsPublicAddress);
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return false;

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();

            // 0.0.0.0/8, 10/8, 100.64/10, 127/8, 169.254/16,
            // 172.16/12, 192.168/16, multicast/reserved 224/4+.
            if (b[0] == 0 || b[0] == 10 || b[0] == 127) return false;
            if (b[0] == 100 && b[1] is >= 64 and <= 127) return false;
            if (b[0] == 169 && b[1] == 254) return false;
            if (b[0] == 172 && b[1] is >= 16 and <= 31) return false;
            if (b[0] == 192 && b[1] == 168) return false;
            if (b[0] >= 224) return false;
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.Equals(IPAddress.IPv6Any)
                || address.Equals(IPAddress.IPv6None)
                || address.IsIPv6LinkLocal
                || address.IsIPv6Multicast
                || address.IsIPv6SiteLocal)
                return false;

            var b = address.GetAddressBytes();
            // fc00::/7 (IPv6 Unique Local Address)
            if ((b[0] & 0xFE) == 0xFC) return false;

            return true;
        }

        return false;
    }
}
