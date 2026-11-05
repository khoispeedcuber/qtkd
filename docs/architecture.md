# Kiến trúc QTKD

API quản trị kinh doanh theo kiến trúc phân lớp: Controller → Service → Repository → DbContext → PostgreSQL.

## 1. Sơ đồ thành phần

```mermaid
flowchart TB
    C["Client<br/>(Swagger / Postman / curl)"] --> M[GlobalExceptionMiddleware]
    M --> A[Controllers]
    A --> S[Services]
    S --> R[Repositories]
    R --> D[QtkdDbContext]
    D --> P[(PostgreSQL)]
    A -. dùng .-> T[DTOs]
    S -. dùng .-> T
    R -. dùng .-> E[Entities]
    D -. ánh xạ .-> E
```

| Lớp | Trách nhiệm | Không làm |
|---|---|---|
| Controller | Nhận HTTP request, gọi Service, trả response và status code | Nghiệp vụ, truy cập DbContext |
| Service | Validation đầu vào, ném exception nghiệp vụ, chuyển Entity thành DTO | Xử lý HTTP, viết truy vấn EF |
| Repository | Truy vấn EF Core (LINQ), trả Entity | Validation, biết về DTO |
| DbContext | Ánh xạ bảng, quan hệ, ràng buộc | |
| Middleware | Chuyển exception thành response lỗi thống nhất | Nghiệp vụ |

Các lớp được nối bằng Dependency Injection, đăng ký trong `Program.cs`. Entity không lộ ra ngoài API: Controller chỉ trả DTO (`CustomerDto`, `ContractDto`, `PriceListDto`, `PriceItemDto`, `SubmitContractResponse`).

## 2. Luồng xử lý `GET /api/contracts/1`

```mermaid
sequenceDiagram
    participant Client
    participant MW as Middleware
    participant Ctl as Controller
    participant Svc as Service
    participant Repo as Repository
    participant DB as PostgreSQL
    Client->>MW: GET /api/contracts/1
    MW->>Ctl: _next(context)
    Ctl->>Svc: GetByIdAsync(1)
    Svc->>Svc: kiểm tra id > 0
    Svc->>Repo: GetByIdWithCustomerAsync(1)
    Repo->>DB: SELECT Contract JOIN Customer
    DB-->>Repo: 1 dòng hoặc không có
    Repo-->>Svc: Contract hoặc null
    Svc-->>Ctl: ContractDto
    Ctl-->>MW: 200 OK
    MW-->>Client: 200 OK
    Note over Svc,MW: Sai id → RequestValidationException (400).<br/>Không có hợp đồng → NotFoundException (404).<br/>Middleware đổi thành ProblemDetails.
```

Truy vấn dùng `async/await` vì đây là thao tác I/O: trong lúc chờ PostgreSQL, thread được trả lại để phục vụ request khác.

## 3. Xử lý lỗi

| Tình huống | Nơi phát hiện | Kết quả |
|---|---|---|
| `GET /api/contracts/abc` (sai kiểu) | `[ApiController]` model binding | `400` + `errors.id` |
| `GET /api/contracts/0`, `/-1` | `ContractService` ném `RequestValidationException` | `400` ProblemDetails |
| `GET /api/contracts/9999` | `ContractService` ném `NotFoundException` | `404` ProblemDetails |
| `POST/PUT` thiếu trường bắt buộc, sai định dạng | `[ApiController]` + DataAnnotations trên request | `400` + `errors.<Trường>` |
| Vi phạm CTR-02 (thiếu điều kiện gửi duyệt), CTR-03 (Draft → Approved) | `ContractService` ném `RuleViolationException` | `400` ProblemDetails có `rule`; CTR-02 kèm `errors` theo trường |
| Vi phạm CTR-01, gửi duyệt khi không còn Draft, trùng số hợp đồng / mã dịch vụ, gửi trùng đồng thời | `ContractService`, `PriceListService` ném `ConflictException`; `RepositoryBase` đổi lỗi trùng khóa của database thành `ConflictException` | `409` ProblemDetails (có `rule` với CTR-01 và US03) |
| Lỗi bất ngờ (mất kết nối DB, bảng chưa tồn tại...) | `GlobalExceptionMiddleware` | `500`, thông báo chung cho client |

Lỗi 500 không trả chi tiết exception cho client để tránh lộ cấu trúc hệ thống; chi tiết nằm trong log, đối chiếu bằng `traceId` trong response.

## 4. Quyết định thiết kế

**Repository.** Tách Repository để Service không phụ thuộc EF Core (có thể kiểm thử bằng repository giả mà không cần database) và để truy vấn nằm một chỗ. Đánh đổi: thêm file và một lớp trung gian. Repository trả `Task<Entity>` / `IReadOnlyList<Entity>`, không trả `IQueryable`, để không làm lộ chi tiết truy vấn ra ngoài. Mỗi repository chỉ có các phương thức đang được dùng.

**Dữ liệu.**
- `PriceItem` dùng khóa chính kép `(PriceListId, ServiceCode)`: một dịch vụ không xuất hiện hai lần trong cùng bảng giá.
- Customer–Contract đặt `OnDelete(Restrict)`: không xóa được khách hàng đang có hợp đồng. PriceList–PriceItem đặt `Cascade`: dòng giá không có ý nghĩa nếu thiếu bảng giá.
- Trạng thái lưu dạng chuỗi (`Draft`, `Active`...) để dễ đọc trong PostgreSQL và khi đối chiếu với response của API.
- Ngày hiệu lực dùng `DateOnly` (kiểu `date` trong PostgreSQL), tránh lỗi múi giờ.
- Check constraint ở database (`StartDate <= EndDate`, `UnitPrice >= 0`) là lớp bảo vệ cuối cùng, độc lập với code nghiệp vụ.
- `ApprovalRequests` có chỉ mục duy nhất trên `ContractId`: mỗi hợp đồng chỉ có một yêu cầu duyệt, chặn gửi trùng ngay ở database; quan hệ với Contract đặt `Restrict`.
- `StartDate`, `EndDate`, `Content`, `AttachmentFileName` của Contract có thể rỗng khi còn `Draft`; CTR-02 chỉ kiểm khi gửi duyệt.
- Các trường lấy theo gợi ý tối thiểu của đặc tả Phase 2; tên trạng thái hợp đồng theo đặc tả đó. Các cột `Content`, `AttachmentFileName` và bảng `ApprovalRequests` của tuần 6 là giả định, ghi rõ trong [doi-chieu-quy-tac.md](doi-chieu-quy-tac.md).

**Cấu hình.** Chuỗi kết nối trong `appsettings.json` không chứa mật khẩu; mật khẩu đặt bằng User Secrets hoặc biến môi trường. Dữ liệu mẫu (`DbSeeder`) chỉ chạy ở môi trường Development, chỉ khi database đã migrate, và không thêm trùng.