# Vovinam Tournament Management System — Backend

Backend cho hệ thống quản lý giải đấu Vovinam: quản lý đoàn/VĐV, bốc thăm, điều hành trận đấu (đối kháng + quyền) theo thời gian thực, trọng tài tự nhận diện, màn hình công khai, và dữ liệu in thẻ (VĐV, trọng tài, trưởng đoàn/HLV).

## Công nghệ sử dụng

- **ASP.NET Core 8** (Web API)
- **Entity Framework Core** + **SQLite**
- **SignalR** — đồng bộ điểm số/thời gian/trạng thái trận theo thời gian thực
- **JWT** — xác thực Admin/Bàn thư ký
- P/Invoke Win32 (`EnumDisplayMonitors`/`GetMonitorInfo`) — mở màn hình công khai đúng màn hình phụ, không phụ thuộc thư viện WinForms

## Kiến trúc tổng quan

Hệ thống gồm 2 mảng tách biệt:

1. **Hệ thống vận hành nội bộ (LAN)** — backend này + 2 frontend con: trang Bàn thư ký (điều hành trận, quản lý dữ liệu) và trang Trọng tài/Màn hình công khai (chạy trên máy tại từng sân, không cần đăng nhập).
2. **Cổng đăng ký công khai** — theme WordPress riêng (đội tự đăng ký VĐV/trưởng đoàn/HLV), đồng bộ dữ liệu sang backend qua import Excel thủ công (không có API kết nối trực tiếp 2 hệ thống).

Phân quyền theo vai trò: `Admin` (toàn quyền, gồm cả các thao tác nhạy cảm như xoá dữ liệu) và `BanThuKy` (vận hành ngày thi đấu). Một số API cố tình để **mở, không yêu cầu đăng nhập** — xem mục Bảo mật bên dưới.

## Cấu trúc thư mục

```
Controllers/    Các API endpoint (REST)
Models/         Entity của Entity Framework Core
DTOs/           Đối tượng truyền dữ liệu qua lại API
Services/       Nghiệp vụ dùng chung (tải/lưu ảnh, mở màn hình công khai kiosk...)
Hubs/           SignalR hub (MatchHub) — lõi realtime của toàn hệ thống
Data/           ApplicationDbContext
Migrations/     Lịch sử migration của EF Core
```

## Các module chính

- **Thiết lập giải** — tên giải, số sân thi đấu, logo/tiêu đề in trên thẻ.
- **Đoàn & VĐV** — quản lý đơn vị và vận động viên, import từ Excel.
- **Nội dung & bốc thăm** — tạo nội dung thi đấu, bốc thăm nhánh đối kháng và danh sách quyền.
- **Điều hành trận đấu (realtime)** — bắt đầu/tạm dừng/kết thúc hiệp, cộng trừ điểm, đồng bộ tức thời tới mọi màn hình đang xem qua SignalR.
- **Trọng tài** — mỗi trọng tài tự chọn đúng tên mình trên thiết bị riêng; có cơ chế chống 2 thiết bị cùng nhận 1 danh tính (khoá tại tầng SQL, không có khoảng hở khi 2 người bấm cùng lúc), tự phát hiện thiết lập cũ hết hạn, và cho phép Bàn thư ký chủ động reset khi thiết bị gặp sự cố.
- **Cán bộ đoàn** (Trưởng đoàn/HLV) — nhập từ file Excel do WordPress xuất ra, ảnh đại diện được tự động tải về lưu cục bộ (không phụ thuộc link ảnh ngoài, tránh lỗi CORS khi in thẻ).
- **Màn hình công khai** — endpoint hỗ trợ mở/đóng trình duyệt ở chế độ kiosk trên màn hình phụ, tự phát hiện và dọn dẹp tiến trình cũ (kể cả khi backend đã khởi động lại).
- **Kết quả & báo cáo**.

## Realtime — MatchHub (SignalR)

Lõi đồng bộ thời gian thực của toàn hệ thống:

- `PressLight` — nhận điểm trọng tài bấm, kiểm tra người bấm có đang thật sự là giám định active tại đúng sân đó không (chặn dự bị bấm nhầm được tính điểm), gộp các lượt bấm trong 1 khung thời gian để tính đồng thuận.
- Phát sự kiện thay đổi trạng thái trận, danh sách trọng tài (`TrongTaiChanged`)... để mọi client (Bàn thư ký, Trọng tài, Màn hình công khai) luôn đồng bộ mà không cần tải lại trang.

## Bảo mật

- Toàn bộ API **sửa/xoá dữ liệu** đều yêu cầu đăng nhập đúng vai trò.
- Một số API **GET cố tình để mở**, vì được gọi từ những nơi không đăng nhập theo đúng thiết kế (màn hình công khai, thiết bị trọng tài tự chọn danh tính) — ví dụ danh sách trọng tài, danh sách trận, thông tin giải đấu. Đây là quyết định có chủ đích, không phải thiếu sót.
- Tải ảnh từ URL ngoài (import cán bộ đoàn) đi qua lớp chống SSRF: chặn dải IP nội bộ/riêng tư, giới hạn số lần chuyển hướng, kiểm tra content-type và dung lượng trước khi lưu.

## Cài đặt & chạy

Yêu cầu: .NET 8 SDK.

```bash
dotnet restore
dotnet ef database update
dotnet run
```

Cấu hình trong `appsettings.Development.json`:

```json
{
  "AthleteImages": {
    "TrustedLocalHosts": ["vectorsp.test", "localhost"]
  }
}
```

`TrustedLocalHosts` là danh sách domain nội bộ được phép tải ảnh về (VD domain WordPress chạy local) — vượt qua lớp chống SSRF vốn mặc định chặn các địa chỉ riêng tư/nội bộ.

## Migration

Sau khi đổi Model, luôn tạo migration mới trước khi chạy:

```bash
dotnet ef migrations add <TenMigration>
dotnet ef database update
```

## Build & Publish (triển khai)

Dự án build ra file `.exe` chạy trực tiếp trên máy tại điểm thi đấu (không cần cài .NET runtime riêng nếu publish self-contained). Bản build frontend (`dist/`) được copy vào `wwwroot/` của backend trước khi publish, để backend phục vụ luôn cả giao diện lẫn API trên cùng 1 cổng.
