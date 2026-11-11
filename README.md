# QTKD — Phase 2 (Tuần 05–07)

Sinh viên: `Phạm Nguyễn Thế Khôi` — MSSV: `23120136`
Repository: https://github.com/khoispeedcuber/qtkd

API quản trị kinh doanh (Customer, Contract, PriceList/PriceItem) viết bằng C# · ASP.NET Core Web API · Entity Framework Core · PostgreSQL · xUnit.

| Tuần | Nội dung | Kết quả trong repository |
|---|---|---|
| 05 | Nền tảng: solution, entity, `QtkdDbContext`, migration, dữ liệu mẫu, API đọc dữ liệu | `src/QTKD.Api`, migration `InitialCreate`, `GET /api/customers`, `GET /api/contracts/{id}` |
| 06 | Chức năng và quy tắc nghiệp vụ: tạo/xem/sửa Customer, Contract, PriceList/PriceItem; gửi hợp đồng trình duyệt (US03); CTR-01/02/03 | Migration `AddContractSubmission`, [docs/api.md](docs/api.md), [docs/doi-chieu-quy-tac.md](docs/doi-chieu-quy-tac.md) |
| 07 | Kiểm thử tự động: unit test cho CTR-01/02/03, integration test TC01–TC08 trên database kiểm thử riêng; sửa lỗi phát hiện được | `tests/`, [docs/kiem-thu.md](docs/kiem-thu.md) |

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

- **pgAdmin:** chọn database `postgres`, rồi menu **Tools → Query Tool**.
- **psql:** mở **SQL Shell (psql)** (Windows) hoặc chạy `psql postgres` (macOS/Linux).

Chạy các câu lệnh sau (thay `<mật khẩu>` bằng mật khẩu tự chọn cho user `qtkd_user`):

```sql
CREATE ROLE qtkd_user LOGIN PASSWORD '<mật khẩu>';
CREATE DATABASE qtkd      OWNER qtkd_user ENCODING 'UTF8' TEMPLATE template0;
CREATE DATABASE qtkd_test OWNER qtkd_user ENCODING 'UTF8' TEMPLATE template0;
```

- `qtkd`: database của ứng dụng, dùng khi chạy API.
- `qtkd_test`: database riêng cho integration test. Bộ test xóa toàn bộ dữ liệu của database này trước mỗi test, vì vậy không dùng chung với `qtkd`.

Kiểm tra bảng mã UTF-8 (cần để lưu tiếng Việt):

```sql
SELECT datname, pg_encoding_to_char(encoding) FROM pg_database WHERE datname IN ('qtkd', 'qtkd_test');
```

Kết quả đúng: cả hai dòng là `UTF8`.

## 4. Cấu hình chuỗi kết nối

File `src/QTKD.Api/appsettings.json` chỉ chứa chuỗi kết nối **không có mật khẩu**. Mật khẩu được đặt riêng bằng User Secrets (lưu ngoài thư mục dự án, không đưa lên Git):

```bash
dotnet user-secrets set "ConnectionStrings:QtkdDb" "Host=localhost;Port=5432;Database=qtkd;Username=qtkd_user;Password=<mật khẩu>" --project src/QTKD.Api
```

Nếu PostgreSQL không dùng cổng 5432, đổi `Port=` cho đúng.

Có thể dùng biến môi trường thay cho User Secrets:

```bash
# macOS / Linux
export ConnectionStrings__QtkdDb="Host=localhost;Port=5432;Database=qtkd;Username=qtkd_user;Password=<mật khẩu>"
```
```powershell
# Windows PowerShell (chỉ có hiệu lực trong cửa sổ đang mở)
$env:ConnectionStrings__QtkdDb = "Host=localhost;Port=5432;Database=qtkd;Username=qtkd_user;Password=<mật khẩu>"
```

Integration test dùng lại chuỗi kết nối này và tự đổi tên database thành `qtkd_test`, nên không cần cấu hình thêm. Muốn dùng database kiểm thử khác, đặt `ConnectionStrings:QtkdTestDb` (User Secrets của `src/QTKD.Api` hoặc biến môi trường `ConnectionStrings__QtkdTestDb`); tên database phải kết thúc bằng `_test`, nếu không bộ test dừng ngay để tránh xóa nhầm dữ liệu.

## 5. Tạo bảng bằng migration

Thư mục `src/QTKD.Api/Migrations/` có sẵn hai migration: `InitialCreate` (tuần 05) và `AddContractSubmission` (tuần 06). Áp dụng vào database `qtkd`:

```bash
dotnet ef database update --project src/QTKD.Api
```

Kết quả đúng: các dòng `Applying migration '...'.` rồi `Done.`

Trong lần chạy đầu có thể thấy một khối `fail: ... SELECT "MigrationId" ... FROM "__EFMigrationsHistory"`. Đây **không phải lỗi**: EF thử đọc bảng lịch sử migration trước khi bảng này được tạo.

Database `qtkd` sau đó có 5 bảng `Customers`, `Contracts`, `PriceLists`, `PriceItems`, `ApprovalRequests` và bảng `__EFMigrationsHistory`.

Database `qtkd_test` không cần chạy lệnh này: bộ integration test tự áp dụng migration khi bắt đầu.

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

## 8. Chạy kiểm thử

Cần hoàn thành mục 3 và 4 trước (database `qtkd_test` và chuỗi kết nối). Không cần chạy API.

```bash
dotnet test
```

Kết quả đúng: `Test summary: total: 39, failed: 0, succeeded: 39`.

| Project | Loại | Phạm vi | Database |
|---|---|---|---|
| `tests/QTKD.UnitTests` | Unit test (30 test) | `ContractService` (CTR-01, CTR-02, CTR-03, gửi duyệt lặp) và `CustomerService` | Không dùng; repository được thay bằng bản giả trong bộ nhớ |
| `tests/QTKD.IntegrationTests` | Integration test (9 test) | TC01–TC08 qua HTTP: tạo Customer, tạo/sửa Contract, gửi duyệt, đọc lại trạng thái | `qtkd_test` |

Chạy riêng từng loại:

```bash
dotnet test tests/QTKD.UnitTests
dotnet test tests/QTKD.IntegrationTests
```

Mỗi integration test kiểm tra cả HTTP status, nội dung response và dữ liệu đọc lại từ database. Bảng TC01–TC08, đối chiếu từng test với quy tắc và lỗi đã tìm thấy: [docs/kiem-thu.md](docs/kiem-thu.md).

Lưu kết quả dạng TRX (mỗi project một file trong thư mục `TestResults/`):

```bash
dotnet test tests/QTKD.UnitTests --logger "trx;LogFileName=unit-tests.trx" --results-directory TestResults
dotnet test tests/QTKD.IntegrationTests --logger "trx;LogFileName=integration-tests.trx" --results-directory TestResults
```

## 9. Cấu trúc thư mục

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
├── tests/
│   ├── QTKD.UnitTests/         unit test nghiệp vụ (repository giả)
│   └── QTKD.IntegrationTests/  integration test qua HTTP và PostgreSQL
└── docs/               kiến trúc, API, đối chiếu quy tắc nghiệp vụ, kiểm thử
```

Luồng xử lý: `Controller → Service → Repository → DbContext → PostgreSQL`.
Chi tiết kiến trúc và các quyết định thiết kế: [docs/architecture.md](docs/architecture.md).

## 10. Lỗi thường gặp

| Lỗi | Cách xử lý |
|---|---|
| `Couldn't find a project to run` | Chạy từ thư mục có `QTKD.sln` và giữ nguyên tham số `--project src/QTKD.Api` |
| `28P01: password authentication failed` | Sai mật khẩu trong chuỗi kết nối: chạy `dotnet user-secrets list --project src/QTKD.Api` để xem lại |
| `3D000: database "qtkd" does not exist` | Chưa tạo database ở mục 3 |
| `3D000: database "qtkd_test" does not exist` (khi chạy test) | Chưa tạo database `qtkd_test` ở mục 3 |
| `Failed to connect to 127.0.0.1:5432` | PostgreSQL chưa chạy, hoặc dùng cổng khác (sửa `Port=`) |
| `relation "Customers" does not exist` | Chưa chạy `dotnet ef database update` (mục 5) |
| `column "Content" does not exist` | Chưa áp dụng migration `AddContractSubmission`: chạy lại `dotnet ef database update` (mục 5) |
| `Integration test chỉ chạy trên database có tên kết thúc bằng _test` | `ConnectionStrings:QtkdTestDb` đang trỏ tới database khác; sửa lại tên database |
| Cổng 5080 đang bị chiếm | Tắt tiến trình đang chạy API cũ (`Ctrl+C`) |
| Log tiếng Việt hiện dấu `?` trong PowerShell | Chỉ là bảng mã của cửa sổ console; chạy `[Console]::OutputEncoding = [Text.Encoding]::UTF8` trước lệnh `dotnet` |

## 11. Giới hạn hiện tại

- Chưa có module Approval: gửi duyệt thành công chỉ chuyển hợp đồng sang `PendingApproval` và ghi một dòng mô phỏng vào `ApprovalRequests`; không thể phê duyệt/từ chối.
- Điều kiện nội dung/tệp bắt buộc của CTR-02 là giả định, chưa phải quy định chính thức (xem [docs/doi-chieu-quy-tac.md](docs/doi-chieu-quy-tac.md)).
- Tệp đính kèm chỉ lưu tên tệp; chưa có xác thực, phân quyền, ký điện tử, giao diện web.
- Phần Pricing chưa có test tự động; đã kiểm tra thủ công bằng Swagger ở tuần 06.
- Không còn lỗi đã biết chưa sửa sau đợt kiểm thử tuần 07 (chi tiết trong [docs/kiem-thu.md](docs/kiem-thu.md)).
