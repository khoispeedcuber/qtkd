# Kiểm thử — Tuần 07

## 1. Môi trường và cách chạy

| Thành phần | Phiên bản |
|---|---|
| .NET SDK / runtime | 10.0.401 / 10.0.12 |
| xUnit | 2.9, VSTest adapter 3.1.5 |
| Integration test | `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`), môi trường `Testing` |
| Database kiểm thử | PostgreSQL, database `qtkd_test` (tách khỏi `qtkd`) |

```bash
dotnet test
```

Cách tổ chức:

- **Unit test** (`tests/QTKD.UnitTests`): gọi trực tiếp `ContractService`, `CustomerService` với repository giả trong bộ nhớ (`Fakes/`). Kiểm tra quy tắc nghiệp vụ, trạng thái hợp đồng sau khi gọi và số lần lưu (`SaveChangesCount`), không cần database.
- **Integration test** (`tests/QTKD.IntegrationTests`): chạy API trong bộ nhớ, gửi request HTTP thật, đọc lại dữ liệu bằng `QtkdDbContext` trên `qtkd_test`. Trước khi chạy, bộ test áp dụng migration; trước mỗi test, toàn bộ bảng được làm rỗng (`TRUNCATE ... RESTART IDENTITY`) để các test không phụ thuộc nhau. Bộ test từ chối chạy nếu tên database không kết thúc bằng `_test`.

Mỗi integration test kiểm tra ba thứ: HTTP status, nội dung response (DTO hoặc `rule`/`errors` trong ProblemDetails) và dữ liệu trong database sau khi gọi.

## 2. Kết quả

Lần chạy ngày 12/11/2026, sau commit `88dfced`:

```
Test summary: total: 39, failed: 0, succeeded: 39, skipped: 0
```

| Project | Số test | Pass | Fail |
|---|---|---|---|
| `QTKD.UnitTests` | 30 | 30 | 0 |
| `QTKD.IntegrationTests` | 9 | 9 | 0 |

## 3. Bảng TC01–TC08

Tất cả TC01–TC08 đều được tự động hóa; không có test case nào phải chạy thủ công.

| TC | Dữ liệu/thao tác | Kết quả mong đợi | Integration test | Unit test liên quan | Kết quả |
|---|---|---|---|---|---|
| TC01 | Tạo Customer hợp lệ | `201`; đọc lại qua `GET /api/customers/{id}` và trong DB đúng tên, `Active` | `TC01_CreateCustomer_Valid_IsSavedAndReadBack` | `CustomerServiceTests.Create_ValidName_IsActive` | Pass |
| TC02 | Tạo Customer thiếu tên | `400`, `errors.Name`; DB không có dòng mới | `TC02_CreateCustomer_MissingName_IsRejectedWithMessage` | `CustomerServiceTests.Create_MissingName_IsRejected` | Pass |
| TC03 | Sửa Contract ở `Draft` | `200`; DB có nội dung và tệp mới, vẫn `Draft` | `TC03_UpdateDraftContract_ValidData_IsSaved` | `Ctr01UpdateContractTests.Update_EditableStatus_SavesNewData` | Pass |
| TC04 | Sửa Contract ở `PendingApproval` | `409`, `rule: CTR-01`; nội dung và trạng thái trong DB không đổi | `TC04_UpdatePendingApprovalContract_IsRejectedAndDataUnchanged` | `Ctr01UpdateContractTests.Update_LockedStatus_IsRejectedAndDataUnchanged` | Pass |
| TC05 | Gửi duyệt khi thiếu nội dung và tệp | `400`, `rule: CTR-02`, `errors` đúng `AttachmentFileName`, `Content`; vẫn `Draft`; không có `ApprovalRequest` | `TC05_SubmitContract_MissingCtr02Conditions_IsRejectedAndStaysDraft` | `Ctr02SubmitContractTests.Submit_OneConditionMissing_IsRejectedAndContractStaysDraft`, `Submit_SeveralConditionsMissing_ReportsAllOfThem` | Pass |
| TC06 | Gửi duyệt hợp đồng hợp lệ | `200`, `PendingApproval`; `GET` và DB đều `PendingApproval`; đúng 1 `ApprovalRequest` khớp `approvalId` | `TC06_SubmitContract_Valid_MovesToPendingApproval` | `Ctr02SubmitContractTests.Submit_AllConditionsMet_MovesToPendingApproval` | Pass |
| TC07 | Đổi `Draft` thẳng sang `Approved` bằng `PUT` | `400`, `rule: CTR-03`; DB vẫn `Draft`, nội dung không đổi | `TC07_UpdateDraftContract_ToApproved_IsRejected` | `Ctr03StatusTransitionTests.Update_DraftToApproved_IsRejectedAndStaysDraft` | Pass |
| TC08 | Gửi duyệt cùng một hợp đồng hai lần (lần lượt và đồng thời) | Lần 2 nhận `409`; DB chỉ có 1 `ApprovalRequest` | `TC08_SubmitSameContractTwice_CreatesOnlyOneApproval`, `TC08_SubmitSameContractConcurrently_CreatesOnlyOneApproval` | `Ctr02SubmitContractTests.Submit_ContractAlreadySubmitted_IsRejectedWithoutNewApproval` | Pass |

## 4. Unit test theo quy tắc

| Quy tắc | Trường hợp đúng | Trường hợp sai |
|---|---|---|
| CTR-01 | Sửa được khi `Draft`, `RevisionRequested` | Bị từ chối (`ConflictException`, `CTR-01`) ở `PendingApproval`, `Approved`, `Active`, `Rejected`, `Cancelled`, `Expired`; dữ liệu không đổi, không lưu |
| CTR-02 | Đủ điều kiện → `PendingApproval`, tạo 1 `ApprovalRequest` | Thiếu từng điều kiện (khách hàng không `Active`, thiếu `StartDate`, thiếu `EndDate`, thiếu `Content`, `AttachmentFileName` chỉ có khoảng trắng) → `RuleViolationException`, `CTR-02`, đúng một lỗi theo trường, vẫn `Draft`, không lưu; thiếu nhiều điều kiện → báo đủ các trường |
| CTR-03 | Gửi duyệt hợp lệ chỉ sang `PendingApproval`; `PUT` gửi `status` trùng trạng thái hiện tại được chấp nhận | `PUT` với `status` = `Approved`/`approved` → `RuleViolationException`, `CTR-03`, vẫn `Draft` |

## 5. Lỗi tìm thấy và đã sửa

**Cập nhật khách hàng chấp nhận trạng thái không hợp lệ.**

- **Tái hiện:** `PUT /api/customers/2` với body `{ "name": "Công ty Hải Đăng", "status": "1" }`. Trước khi sửa, API trả `200` và khách hàng bị đổi sang `Suspended`. Tương tự với `"0"` (thành `Active`) và `"Active, Suspended"` (thành `Suspended`). Tài liệu API chỉ cho phép `Active` hoặc `Suspended`.
- **Phát hiện bằng:** `CustomerServiceTests.Update_UnknownStatusName_IsRejectedAndUnchanged`. Ở commit `264824b` (thêm bộ test), 3 trường hợp `"1"`, `"0"`, `"Active, Suspended"` fail với `Assert.Throws() Failure: No exception was thrown`; `"Closed"` pass.
- **Nguyên nhân:** `CustomerService.UpdateAsync` dùng `Enum.TryParse`. Hàm này nhận cả chuỗi số (`"1"` → giá trị 1 = `Suspended`) và danh sách tên ngăn cách bởi dấu phẩy; kiểm tra `Enum.IsDefined` sau đó vẫn đúng vì kết quả là một giá trị có trong enum.
- **Sửa:** so khớp chuỗi nhận được với đúng tên các giá trị của `CustomerStatus` (không phân biệt hoa thường); không khớp → `400`. Commit `88dfced`.
- **Sau khi sửa:** `dotnet test` 39/39 pass.

## 6. Các tình huống đã thử, không phát hiện lỗi

- Gửi duyệt khi thiếu từng điều kiện CTR-02 riêng lẻ và khi thiếu nhiều điều kiện cùng lúc.
- Sửa hợp đồng ở cả 8 trạng thái của `ContractStatus`.
- Đổi trạng thái sang `Approved` qua `PUT`, kể cả viết thường.
- Gửi duyệt cùng một hợp đồng hai lần lần lượt và hai request đồng thời: luôn chỉ một request thành công, một request nhận `409`, chỉ một `ApprovalRequest`.
- Tạo khách hàng với tên `null`, rỗng, chỉ có khoảng trắng; tên có khoảng trắng đầu/cuối được cắt bỏ.

## 7. Phần chưa tự động hóa

- Pricing (PriceList, PriceItem): chưa nằm trong danh sách TC01–TC08; đã kiểm tra thủ công bằng Swagger ở tuần 06 (tạo bảng giá, thêm dòng giá hợp lệ, đơn giá âm → `400`, bảng giá không tồn tại → `404`).
