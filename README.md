# vovinam-backend

Phần mềm điều hành giải Vovinam — chạy dạng .exe tại chỗ (LAN), không public
ra Internet. Backend (.NET 8 + SQLite) phục vụ luôn cả frontend đã build
(React) từ `wwwroot/`.

## Chạy thử trên máy đang code (lần đầu / sau khi đổi model)

1. Cài .NET 8 SDK (không chỉ Runtime — SDK mới có lệnh `dotnet ef`).
2. Tạo/cập nhật migration đúng theo model hiện tại:
   ```
   dotnet ef migrations add <TenMoTa>
   ```
   (chỉ cần làm khi Model/ đổi — ví dụ thêm cột mới. Không cần làm nếu chỉ
   sửa Controller/logic thường.)
3. `dotnet run` — lúc khởi động app tự tạo/cập nhật file `vovinam.db` đúng
   schema mới nhất, không cần tự chạy `dotnet ef database update` tay.

## Đóng gói để mang sang máy khác (chỉ chạy .exe, không cần cài .NET SDK)

Chạy `build-publish.bat` (nằm cạnh file này) — script tự làm 3 bước:
build frontend (`vovinam-frontend`) → gộp vào `wwwroot` → `dotnet publish`.
Kết quả nằm trong `bin\publish\`.

Máy đích **chỉ cần cài .NET 8 Runtime** (không cần SDK, không cần SQL
Server/LocalDB — SQLite là 1 file, tự tạo lúc chạy lần đầu).

## Copy sang máy khác — nhớ mang theo

- Cả thư mục `bin\publish\` (chứa `vovinam-backend.exe` + toàn bộ DLL).
- Nếu ĐÃ có dữ liệu giải (đội, VĐV, lịch...) muốn giữ lại: copy kèm file
  `vovinam.db` từ máy cũ, đè vào đúng chỗ trên máy mới — không thì máy mới
  sẽ tự tạo 1 file `vovinam.db` mới, trống trơn.
- Thư mục `wwwroot\uploads\` (ảnh đại diện VĐV tải từ trang đăng ký) —
  không đi kèm code, phải copy tay nếu muốn giữ ảnh cũ.

## Vài chỗ cần biết khi vận hành

- **Log lỗi**: nằm ở `logs\app-<ngày>.log`, cạnh file .exe. Có lỗi gì lúc
  thi đấu (kể cả không thấy hiện trên màn hình) thì xem file này trước.
- **Cổng chạy**: mặc định `5267` (sửa trong `appsettings.json` mục `Urls`
  nếu trùng cổng với phần mềm khác trên máy đó).
- **Đăng nhập BTC / khoá ký JWT**: trong `appsettings.json` (`AdminAuth`,
  `Jwt:Key`) — đổi trực tiếp file này nếu muốn đổi mật khẩu/khoá. Vì phần
  mềm không public nên để thẳng trong file cấu hình là đủ, không cần cơ
  chế bí mật riêng.
- Các thiết bị khác (trọng tài, màn hình công khai, bàn thư ký sân khác)
  truy cập qua `http://<IP-máy-chạy-.exe>:5267` trong cùng mạng LAN/WiFi.
