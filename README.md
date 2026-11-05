# QTKD — Phase 2, Tuần 06

Sinh viên: `Phạm Nguyễn Thế Khôi` — MSSV: `23120136`

API quản trị kinh doanh (Customer, Contract, PriceList/PriceItem) viết bằng C# · ASP.NET Core Web API · Entity Framework Core · PostgreSQL.

**Phạm vi Tuần 06:** API tạo/xem/sửa Customer, tạo/xem/sửa/gửi duyệt Contract, tạo/xem PriceList và PriceItem; quy tắc CTR-01/02/03 và user story US03 (gửi duyệt hợp đồng).
Chưa có Approval thật và chưa có test tự động.

## 1. Yêu cầu cài đặt

| Công cụ | Phiên bản | Kiểm tra |
|---|---|---|
| .NET SDK | 10 | `dotnet --version` (kết quả `10.x.x`) |
| PostgreSQL | 14 trở lên | `psql --version`, hoặc mở pgAdmin |
| Git | bản bất kỳ | `git --version` |
| dotnet-ef | 10 | `dotnet ef --version` |

Cài `dotnet-ef` nếu chưa có:

```bash
dotnet tool install --global dotnet-ef --version "10.*"
```

## 2. Lấy mã nguồn

```bash
git clone https://github.com/khoispeedcuber/qtkd.git
cd qtkd
```

Các lệnh bên dưới chạy tại thư mục gốc (nơi có file `QTKD.sln`).

## 3. Tạo database

Mở công cụ truy vấn của PostgreSQL bằng tài khoản quản trị `postgres`:

- **pgAdmin:** bấm chọn database `postgres`, rồi menu **Tools → Query Tool**.
- **psql:** mở **SQL Shell (psql)** (Windows) hoặc chạy `psql postgres` (macOS/Linux).

Chạy 2 câu lệnh sau (thay `<mật khẩu>` bằng mật khẩu tự chọn cho user `qtkd_user`):

```sql
CREATE ROLE qtkd_user LOGIN PASSWORD '<mật khẩu>';
CREATE DATABASE qtkd OWNER qtkd_user ENCODING 'UTF8' TEMPLATE template0;
```

Kiểm tra database dùng bảng mã UTF-8 (cần để lưu tiếng Việt):

```sql
SELECT datname, pg_encoding_to_char(encoding) FROM pg_database WHERE datname = 'qtkd';
```

Kết quả đúng: `qtkd | UTF8`.

## 4. Cấu hình chuỗi kết nối

File `src/QTKD.Api/appsettings.json` chỉ chứa chuỗi kết nối **không có mật khẩu**. Mật khẩu được đặt riêng bằng User Secrets (lưu ngoài thư mục dự án, không đưa lên Git):

```bash
dotnet user-secrets set "ConnectionStrings:QtkdDb" "Host=localhost;Port=5432;Database=qtkd;Username=qtkd_user;Password=<mật khẩu>" --project src/QTKD.Api
```

Nếu PostgreSQL của bạn không dùng cổng 5432, đổi `Port=` cho đúng.

Có thể dùng biến môi trường thay cho User Secrets:

```bash
# macOS / Linux
export ConnectionStrings__QtkdDb="Host=localhost;Port=5432;Database=qtkd;Username=qtkd_user;Password=<mật khẩu>"
```
```powershell
# Windows PowerShell (chỉ có hiệu lực trong cửa sổ đang mở)
$env:ConnectionStrings__QtkdDb = "Host=localhost;Port=5432;Database=qtkd;Username=qtkd_user;Password=<mật khẩu>"
```

## 5. Tạo bảng bằng migration

Thư mục `src/QTKD.Api/Migrations/` đã có sẵn trong repository (gồm migration tạo bảng ban đầu và migration bổ sung cho luồng gửi duyệt hợp đồng), chỉ cần áp dụng vào database:

```bash
dotnet ef database update --project src/QTKD.Api
```

Kết quả đúng: các dòng `Applying migration '...'.` rồi `Done.`

Trong lần chạy đầu có thể thấy một khối `fail: ... SELECT "MigrationId" ... FROM "__EFMigrationsHistory"`. Đây **không phải lỗi**: EF thử đọc bảng lịch sử migration trước khi bảng này được tạo.

Database `qtkd` sau đó có 5 bảng `Customers`, `Contracts`, `PriceLists`, `PriceItems`, `ApprovalRequests` và bảng `__EFMigrationsHistory`.

## 6. Chạy API

```bash
dotnet run --project src/QTKD.Api
```

Kết quả đúng trong console: `Now listening on: http://localhost:5080` và `Hosting environment: Development`.

- API: `http://localhost:5080`
- Swagger UI: `http://localhost:5080/swagger`
- Lần chạy đầu, API tự thêm dữ liệu mẫu (chỉ ở môi trường Development): 2 Customer, 1 Contract `Draft` (`HD-2026-018`), 1 PriceList có 2 PriceItem. Các lần chạy sau không thêm lại.

Không chạy với `--no-launch-profile` (sẽ mất môi trường Development, không nạp User Secrets và không thêm dữ liệu mẫu).

## 7. Các endpoint

Mô tả chi tiết, kèm request/response mẫu: [docs/api.md](docs/api.md). Bảng đối chiếu US03, CTR-01/02/03 với mã nguồn, các giả định và giới hạn: [docs/doi-chieu-quy-tac.md](docs/doi-chieu-quy-tac.md).

| Method | Endpoint | Chức năng |
|---|---|---|
| GET | `/api/customers`, `/api/customers/{id}` | Xem danh sách / chi tiết khách hàng |
| POST | `/api/customers` | Tạo khách hàng |
| PUT | `/api/customers/{id}` | Sửa khách hàng |
| GET | `/api/contracts`, `/api/contracts/{id}` | Xem danh sách / chi tiết hợp đồng |
| POST | `/api/contracts` | Tạo hợp đồng (trạng thái `Draft`) |
| PUT | `/api/contracts/{id}` | Sửa hợp đồng (CTR-01, CTR-03) |
| POST | `/api/contracts/{id}/submit` | Gửi duyệt: `Draft` → `PendingApproval` (US03, CTR-02) |
| GET | `/api/price-lists`, `/api/price-lists/{id}` | Xem bảng giá kèm dòng giá |
| POST | `/api/price-lists` | Tạo bảng giá |
| GET | `/api/price-lists/{id}/items` | Xem các dòng giá của bảng giá |
| POST | `/api/price-lists/{id}/items` | Thêm dòng giá vào bảng giá |

Thử bằng Swagger UI (**Try it out → Execute**) hoặc `curl` (Windows PowerShell dùng `curl.exe`). Đối chiếu dữ liệu trong database:

```sql
SELECT * FROM "Customers" ORDER BY "Id";
SELECT * FROM "Contracts" ORDER BY "Id";
SELECT * FROM "ApprovalRequests";
```

(Tên bảng và cột có chữ hoa nên phải đặt trong dấu nháy kép.)

## 8. Cấu trúc thư mục

```
├── QTKD.sln
├── src/QTKD.Api/
│   ├── Controllers/    nhận request, trả response
│   ├── DTOs/           dữ liệu vào/ra của API
│   ├── Entities/       mô hình dữ liệu ánh xạ bảng
│   ├── Services/       nghiệp vụ và validation
│   ├── Repositories/   truy vấn database (EF Core)
│   ├── Data/           QtkdDbContext, dữ liệu mẫu
│   ├── Exceptions/     exception nghiệp vụ
│   ├── Middleware/     xử lý lỗi tập trung
│   ├── Migrations/     migration của EF Core
│   └── Program.cs
└── docs/               tài liệu API, đối chiếu quy tắc nghiệp vụ, thiết kế
```

Luồng xử lý: `Controller → Service → Repository → DbContext → PostgreSQL`.
Chi tiết kiến trúc và các quyết định thiết kế: [docs/architecture.md](docs/architecture.md).

## 9. Lỗi thường gặp

| Lỗi | Cách xử lý |
|---|---|
| `Couldn't find a project to run` | Chạy từ thư mục có `QTKD.sln` và giữ nguyên tham số `--project src/QTKD.Api` |
| `28P01: password authentication failed` | Sai mật khẩu trong chuỗi kết nối: chạy `dotnet user-secrets list --project src/QTKD.Api` để xem lại |
| `3D000: database "qtkd" does not exist` | Chưa tạo database ở mục 3 |
| `Failed to connect to 127.0.0.1:5432` | PostgreSQL chưa chạy, hoặc dùng cổng khác (sửa `Port=`) |
| `relation "Customers" does not exist` | Chưa chạy `dotnet ef database update` (mục 5) |
| `column "Content" does not exist` | Chưa áp dụng migration mới: chạy lại `dotnet ef database update` (mục 5) |
| Cổng 5080 đang bị chiếm | Tắt tiến trình đang chạy API cũ (`Ctrl+C`) |

## 10. Giới hạn hiện tại

- Chưa có module Approval: gửi duyệt thành công chỉ chuyển hợp đồng sang `PendingApproval` và ghi một dòng mô phỏng vào `ApprovalRequests`; không thể phê duyệt/từ chối.
- Điều kiện nội dung/tệp bắt buộc của CTR-02 là giả định, chưa phải quy định chính thức (xem [docs/doi-chieu-quy-tac.md](docs/doi-chieu-quy-tac.md)).
- Tệp đính kèm chỉ lưu tên tệp; chưa có xác thực, ký điện tử, giao diện web.
- Chưa có test tự động.
