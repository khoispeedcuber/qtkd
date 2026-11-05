# Đối chiếu US03 và CTR-01/02/03 với mã nguồn

Đường dẫn tính từ `src/QTKD.Api/`.

## 1. US03 - Gửi duyệt hợp đồng

Endpoint: `POST /api/contracts/{id}/submit`.

| Bước theo đề | Xử lý | Vị trí |
|---|---|---|
| Endpoint nhận yêu cầu | `ContractsController.Submit` gọi `IContractService.SubmitAsync` | `Controllers/ContractsController.cs:49-55` |
| Tìm Contract theo id | `GetForUpdateAsync`; không có → `NotFoundException` (404) | `Services/ContractService.cs:104-105` |
| Kiểm tra trạng thái Draft | Khác `Draft` → `ConflictException` (409) | `Services/ContractService.cs:109-112` |
| Kiểm tra Customer, ngày hiệu lực, nội dung/tệp | `CollectSubmitErrors` (CTR-02) | `Services/ContractService.cs:115-120`, `133-153` |
| Chỉ khi hợp lệ mới chuyển `PendingApproval` và lưu | Đổi trạng thái + thêm `ApprovalRequest`, một lần `SaveChangesAsync` (một transaction) | `Services/ContractService.cs:124-127` |
| Thiếu thông tin / sai trạng thái → lỗi rõ ràng, Contract không đổi | Ném exception trước khi sửa bất kỳ trường nào; middleware đổi thành `400`/`409` | `Services/ContractService.cs:109-120`, `Middleware/GlobalExceptionMiddleware.cs:48-61` |
| Không đi thẳng Draft → Approved | Submit chỉ gán `PendingApproval`; không có đường nào gán `Approved` | `Services/ContractService.cs:124` |
| Gửi lại cùng hợp đồng không tạo xử lý tương đương | Lần 2 gặp trạng thái `PendingApproval` → 409 trước khi tạo `ApprovalRequest`. Lớp bảo vệ thứ hai: chỉ mục duy nhất `ApprovalRequests.ContractId`; hai request đồng thời thì một request bị `ConflictException` | `Services/ContractService.cs:109-112`, `Data/QtkdDbContext.cs:44`, `Repositories/RepositoryBase.cs:25-28` |
| Định dạng response thành công/lỗi thống nhất, ghi trong tài liệu API | Thành công: DTO; lỗi: ProblemDetails | `docs/api.md` mục 1 |

## 2. Quy tắc nghiệp vụ

| Quy tắc | Nội dung | Xử lý | Vị trí |
|---|---|---|---|
| CTR-01 | Chỉ sửa hợp đồng ở `Draft` hoặc `RevisionRequested` | `UpdateAsync` kiểm tra trạng thái trước khi gán bất kỳ trường nào; sai → `ConflictException` (409), `rule: CTR-01` | `Services/ContractService.cs:74-78` |
| CTR-02 | Chỉ gửi duyệt khi Customer, ngày hiệu lực, nội dung/tệp bắt buộc hợp lệ | `CollectSubmitErrors` gom mọi điều kiện thiếu; có lỗi → `RuleViolationException` (400), `rule: CTR-02` | `Services/ContractService.cs:114-120`, `133-153` |
| CTR-03 | Không chuyển trực tiếp Draft → Approved | (a) `RejectStatusChange` từ chối `status = Approved` trong PUT, `rule: CTR-03`; (b) `SubmitAsync` chỉ gán `PendingApproval` | `Services/ContractService.cs:80-81`, `156-170`, `124` |

Các ràng buộc ở database (lớp bảo vệ cuối): `StartDate <= EndDate` (`Data/QtkdDbContext.cs:37-38`), `UnitPrice >= 0`, khóa duy nhất `ContractNo` và `ApprovalRequests.ContractId`.

## 3. Giả định về điều kiện bắt buộc của CTR-02

Tài liệu Phase 2 nêu CTR-02 là "Customer, ngày hiệu lực, nội dung/tệp bắt buộc hợp lệ" nhưng không liệt kê cụ thể nội dung/tệp nào là bắt buộc. Bản cài đặt này dùng giả định sau; đây **chưa phải quy định chính thức**. Phần cần đối chiếu lại với Phase 1 là danh sách nội dung/tệp bắt buộc cụ thể.

| Điều kiện | Giả định dùng trong demo |
|---|---|
| Customer | Tồn tại và ở trạng thái `Active` |
| Ngày hiệu lực | Có cả `StartDate` và `EndDate`, và `StartDate <= EndDate` |
| Nội dung | `Content` không rỗng (không chỉ là khoảng trắng) |
| Tệp | `AttachmentFileName` không rỗng. Chỉ lưu tên tệp; hệ thống chưa tải lên/lưu tệp thật |

## 4. Cách mô phỏng Approval và giới hạn

Chưa xây dựng module Approval. Khi gửi duyệt thành công, hệ thống ghi một dòng vào bảng `ApprovalRequests` (id, `ContractId`, thời điểm gửi) và trả `approvalId` trong response. Bảng này chỉ mô phỏng "yêu cầu phê duyệt đã được tạo" và là căn cứ để chặn gửi duyệt trùng.

Giới hạn:
- Không có người duyệt, không có thao tác phê duyệt/từ chối; do đó trạng thái `Approved`, `RevisionRequested`, `Rejected`... không thể đạt được qua API.
- Gửi duyệt chỉ áp dụng cho hợp đồng `Draft`. Việc gửi lại hợp đồng `RevisionRequested` thuộc luồng Approval, chưa làm.
- `ApprovalRequests.ContractId` duy nhất nên mỗi hợp đồng chỉ có một yêu cầu phê duyệt; luồng nhiều vòng duyệt sẽ phải thay ràng buộc này.
- Chưa có xác thực/phân quyền (mã `403` trong tài liệu gốc chưa áp dụng).
- Tệp đính kèm chỉ là tên tệp.
- Khóa duy nhất bảo vệ chống gửi trùng nhưng chưa có cơ chế `Idempotency-Key`; gửi lại nhận `409`, không nhận lại kết quả cũ.
- Chưa có test tự động.
