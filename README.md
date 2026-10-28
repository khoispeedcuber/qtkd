# QTKD — Phase 2, Tuần 05

Sinh viên: `Phạm Nguyễn Thế Khôi` — MSSV: `23120136`

API quản trị kinh doanh (Customer, Contract, PriceList/PriceItem) viết bằng C# · ASP.NET Core Web API · Entity Framework Core · PostgreSQL.

**Phạm vi Tuần 05:** tạo dự án, tạo database bằng EF Core migration, dữ liệu mẫu và 2 API đọc dữ liệu.
Chưa có tạo/sửa/gửi trình duyệt hợp đồng (Tuần 06) và chưa có test tự động (Tuần 07).

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
git clone <địa chỉ repository>
cd <tên thư mục vừa clone>
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

Thư mục `src/QTKD.Api/Migrations/` đã có sẵn trong repository, chỉ cần áp dụng vào database:

```bash
dotnet ef database update --project src/QTKD.Api
```

Kết quả đúng: dòng `Applying migration '..._InitialCreate'.` rồi `Done.`

Trong lần chạy đầu có thể thấy một khối `fail: ... SELECT "MigrationId" ... FROM "__EFMigrationsHistory"`. Đây **không phải lỗi**: EF thử đọc bảng lịch sử migration trước khi bảng này được tạo.

Database `qtkd` sau đó có 4 bảng `Customers`, `Contracts`, `PriceLists`, `PriceItems` và bảng `__EFMigrationsHistory`.

## 6. Chạy API

```bash
dotnet run --project src/QTKD.Api
```

Kết quả đúng trong console: `Now listening on: http://localhost:5080` và `Hosting environment: Development`.

- API: `http://localhost:5080`
- Swagger UI: `http://localhost:5080/swagger`
- Lần chạy đầu, API tự thêm dữ liệu mẫu (chỉ ở môi trường Development): 2 Customer, 1 Contract `Draft` (`HD-2026-018`), 1 PriceList có 2 PriceItem. Các lần chạy sau không thêm lại.

Không chạy với `--no-launch-profile` (sẽ mất môi trường Development, không nạp User Secrets và không thêm dữ liệu mẫu).

## 7. Thử các endpoint

Mở Swagger UI, chọn endpoint, bấm **Try it out → Execute**, hoặc dùng `curl`
(trên Windows PowerShell dùng `curl.exe`):

```bash
curl -i http://localhost:5080/api/customers
curl -i http://localhost:5080/api/contracts/1
```

| Method | Endpoint | Kết quả |
|---|---|---|
| GET | `/api/customers` | `200` — danh sách khách hàng |
| GET | `/api/contracts/{id}` | `200` — hợp đồng kèm tên khách hàng |
| GET | `/api/contracts/9999` (không tồn tại) | `404` — `"title": "Không tìm thấy dữ liệu"` |
| GET | `/api/contracts/0` hoặc `/api/contracts/-1` | `400` — `"detail": "Id hợp đồng phải là số nguyên dương."` |
| GET | `/api/contracts/abc` | `400` — lỗi định dạng id |

Kết quả mong đợi của `GET /api/customers`:

```json
[
  { "id": 1, "name": "Công ty Minh Phát", "status": "Active" },
  { "id": 2, "name": "Công ty Hải Đăng", "status": "Active" }
]
```

Kết quả mong đợi của `GET /api/contracts/1`:

```json
{
  "id": 1,
  "contractNo": "HD-2026-018",
  "customerId": 1,
  "customerName": "Công ty Minh Phát",
  "startDate": "2026-11-01",
  "endDate": "2027-10-31",
  "status": "Draft"
}
```

Đối chiếu với database (pgAdmin hoặc psql, đang kết nối vào `qtkd`):

```sql
SELECT * FROM "Customers" ORDER BY "Id";
SELECT * FROM "Contracts";
```

(Tên bảng và cột có chữ hoa nên phải đặt trong dấu nháy kép.)

## 8. Cấu trúc thư mục

```
├── QTKD.sln
├── src/QTKD.Api/
│   ├── Controllers/    nhận request, trả response
│   ├── DTOs/           dữ liệu trả ra qua API
│   ├── Entities/       mô hình dữ liệu ánh xạ bảng
│   ├── Services/       nghiệp vụ và validation
│   ├── Repositories/   truy vấn database (EF Core)
│   ├── Data/           QtkdDbContext, dữ liệu mẫu
│   ├── Exceptions/     exception nghiệp vụ
│   ├── Middleware/     xử lý lỗi tập trung
│   ├── Migrations/     migration của EF Core
│   └── Program.cs
└── docs/               tài liệu thiết kế
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
| Cổng 5080 đang bị chiếm | Tắt tiến trình đang chạy API cũ (`Ctrl+C`) |

## 10. Giới hạn hiện tại

- Chỉ có 2 endpoint đọc dữ liệu; chưa có tạo/sửa/gửi trình duyệt hợp đồng.
- Chưa có Approval, Payment, ký điện tử, giao diện web.
- Chưa có test tự động.
