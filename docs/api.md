# Danh sách API

Địa chỉ gốc khi chạy cục bộ: `http://localhost:5080`. Dữ liệu trao đổi dạng JSON (tên trường camelCase). Ngày dùng định dạng `yyyy-MM-dd`.

## 1. Quy ước response

**Thành công:** trả thẳng DTO (hoặc mảng DTO), không bọc thêm lớp ngoài.

| Thao tác | Mã trả về |
|---|---|
| Đọc, sửa, gửi duyệt | `200 OK` |
| Tạo mới | `201 Created` + header `Location` |

**Lỗi:** mọi lỗi đều là `application/problem+json` (RFC 7807) với cùng các trường chính.

| Trường | Ý nghĩa |
|---|---|
| `status` | Mã HTTP |
| `title` | Tiêu đề ngắn của loại lỗi |
| `detail` | Mô tả lỗi cho người dùng (có ở lỗi nghiệp vụ) |
| `traceId` | Mã để đối chiếu với log |
| `errors` | Lỗi theo từng trường: `{ "Tên trường": ["thông báo", ...] }` (có ở lỗi dữ liệu và lỗi CTR-02) |
| `rule` | Mã quy tắc bị vi phạm: `CTR-01`, `CTR-02`, `CTR-03`, `US03` (chỉ có ở lỗi quy tắc nghiệp vụ) |

| Mã | Khi nào | Nơi tạo |
|---|---|---|
| `400` | Thiếu/sai dữ liệu đầu vào; khách hàng tham chiếu không hợp lệ; vi phạm CTR-02, CTR-03 | `[ApiController]` (model validation), `RequestValidationException`, `RuleViolationException` |
| `404` | Không tìm thấy Customer / Contract / PriceList | `NotFoundException` |
| `409` | Sai trạng thái (CTR-01, gửi duyệt lại), trùng số hợp đồng, trùng mã dịch vụ | `ConflictException` |
| `500` | Lỗi không dự đoán được; thông báo chung, chi tiết nằm trong log | `GlobalExceptionMiddleware` |

Ví dụ lỗi dữ liệu (`400`, do model validation):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "Name": ["Tên khách hàng là bắt buộc."] },
  "traceId": "00-..."
}
```

Ví dụ lỗi quy tắc nghiệp vụ (`400`, CTR-02):

```json
{
  "title": "Vi phạm quy tắc nghiệp vụ",
  "status": 400,
  "detail": "Hợp đồng chưa đủ điều kiện gửi duyệt. Hợp đồng vẫn ở trạng thái Draft.",
  "instance": "/api/contracts/2/submit",
  "traceId": "0HN...",
  "rule": "CTR-02",
  "errors": {
    "Content": ["Thiếu nội dung hợp đồng."],
    "AttachmentFileName": ["Thiếu tệp đính kèm."]
  }
}
```

## 2. Customer

| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/api/customers` | Danh sách khách hàng |
| GET | `/api/customers/{id}` | Chi tiết khách hàng |
| POST | `/api/customers` | Tạo khách hàng (mặc định `Active`) |
| PUT | `/api/customers/{id}` | Sửa tên và trạng thái (`Active` / `Suspended`) |

`POST /api/customers`

```json
{ "name": "Công ty Đại Dương" }
```

`201 Created`

```json
{ "id": 3, "name": "Công ty Đại Dương", "status": "Active" }
```

Thiếu tên (`{}` hoặc `{ "name": "  " }`) → `400`, `errors.Name`.

`PUT /api/customers/3`

```json
{ "name": "Công ty Đại Dương", "status": "Suspended" }
```

`200 OK`

```json
{ "id": 3, "name": "Công ty Đại Dương", "status": "Suspended" }
```

Trạng thái ngoài `Active`/`Suspended` → `400`. Id không tồn tại → `404`.

## 3. Contract

| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/api/contracts` | Danh sách hợp đồng |
| GET | `/api/contracts/{id}` | Chi tiết hợp đồng |
| POST | `/api/contracts` | Tạo hợp đồng (luôn ở `Draft`) |
| PUT | `/api/contracts/{id}` | Sửa hợp đồng (CTR-01, CTR-03) |
| POST | `/api/contracts/{id}/submit` | Gửi duyệt (US03, CTR-02, CTR-03) |

### POST /api/contracts

```json
{
  "contractNo": "HD-2026-020",
  "customerId": 1,
  "startDate": "2026-11-01",
  "endDate": "2027-10-31"
}
```

`201 Created`

```json
{
  "id": 2,
  "contractNo": "HD-2026-020",
  "customerId": 1,
  "customerName": "Công ty Minh Phát",
  "startDate": "2026-11-01",
  "endDate": "2027-10-31",
  "content": null,
  "attachmentFileName": null,
  "status": "Draft"
}
```

| Tình huống | Kết quả |
|---|---|
| Thiếu `contractNo` hoặc `customerId` | `400`, `errors` theo trường |
| `startDate` > `endDate` | `400` |
| Khách hàng không tồn tại hoặc không `Active` | `400` |
| Trùng `contractNo` | `409` |

Hợp đồng `Draft` được phép thiếu ngày, nội dung, tệp đính kèm; việc đủ hay chưa được kiểm tra khi gửi duyệt.

### PUT /api/contracts/{id}

Thay thế toàn bộ các trường sửa được (số hợp đồng không đổi). Trường `status` là tùy chọn và không dùng để đổi trạng thái.

```json
{
  "customerId": 1,
  "startDate": "2026-11-01",
  "endDate": "2027-10-31",
  "content": "Hợp đồng dịch vụ xếp dỡ container năm 2026.",
  "attachmentFileName": "HD-2026-020.pdf"
}
```

`200 OK`: trả `ContractDto` đã cập nhật (cùng dạng như trên).

| Tình huống | Kết quả |
|---|---|
| Hợp đồng không ở `Draft`/`RevisionRequested` | `409`, `rule: CTR-01`, dữ liệu không đổi |
| Body có `"status": "Approved"` | `400`, `rule: CTR-03` (không chuyển trực tiếp sang `Approved`) |
| Body có `status` là giá trị khác trạng thái hiện tại (không phải `Approved`) | `400`; trạng thái chỉ đổi qua `POST /api/contracts/{id}/submit` |
| Body không có `status`, hoặc `status` trùng trạng thái hiện tại | Bỏ qua trường này, sửa bình thường |
| Dữ liệu/khách hàng không hợp lệ | `400` |
| Id không tồn tại | `404` |

Ví dụ `409` (CTR-01):

```json
{
  "title": "Xung đột trạng thái dữ liệu",
  "status": 409,
  "detail": "Chỉ được sửa hợp đồng ở trạng thái Draft hoặc RevisionRequested; hợp đồng hiện ở PendingApproval.",
  "instance": "/api/contracts/2",
  "traceId": "0HN...",
  "rule": "CTR-01"
}
```

Ví dụ `400` (CTR-03), request có `"status": "Approved"`:

```json
{
  "title": "Vi phạm quy tắc nghiệp vụ",
  "status": 400,
  "detail": "Không được chuyển trực tiếp từ Draft sang Approved. Hợp đồng phải được gửi duyệt (PendingApproval) trước.",
  "instance": "/api/contracts/2",
  "traceId": "0HN...",
  "rule": "CTR-03"
}
```

### POST /api/contracts/{id}/submit (US03)

Không có body. Gửi duyệt hợp đồng `Draft` khi thỏa các điều kiện CTR-02.

`200 OK`

```json
{ "contractId": 2, "status": "PendingApproval", "approvalId": 1 }
```

| Tình huống | Kết quả |
|---|---|
| Id không tồn tại | `404` |
| Hợp đồng không ở `Draft` (gồm cả gửi lặp lại) | `409`, `rule: US03`; không tạo thêm `ApprovalRequest` |
| Thiếu điều kiện CTR-02 | `400`, `rule: CTR-02`, `errors` liệt kê đủ các điều kiện thiếu; hợp đồng vẫn `Draft`, dữ liệu không đổi |

Ví dụ `409` khi gửi lại:

```json
{
  "title": "Xung đột trạng thái dữ liệu",
  "status": 409,
  "detail": "Chỉ hợp đồng ở trạng thái Draft mới được gửi duyệt; hợp đồng hiện ở PendingApproval.",
  "instance": "/api/contracts/2/submit",
  "traceId": "0HN...",
  "rule": "US03"
}
```

## 4. Pricing

| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/api/price-lists` | Danh sách bảng giá kèm các dòng giá |
| GET | `/api/price-lists/{id}` | Chi tiết bảng giá kèm các dòng giá |
| POST | `/api/price-lists` | Tạo bảng giá |
| GET | `/api/price-lists/{id}/items` | Các dòng giá của một bảng giá |
| POST | `/api/price-lists/{id}/items` | Thêm dòng giá vào bảng giá |

`POST /api/price-lists`

```json
{ "name": "Bảng giá dịch vụ kho 2027", "effectiveDate": "2027-01-01" }
```

`201 Created`

```json
{ "id": 2, "name": "Bảng giá dịch vụ kho 2027", "effectiveDate": "2027-01-01", "items": [] }
```

`POST /api/price-lists/2/items`

```json
{ "serviceCode": "WAREHOUSE-M2", "unitPrice": 95000 }
```

`201 Created`

```json
{ "priceListId": 2, "serviceCode": "WAREHOUSE-M2", "unitPrice": 95000 }
```

| Tình huống | Kết quả |
|---|---|
| Bảng giá `{id}` không tồn tại | `404` (dòng giá luôn gắn vào đúng bảng giá có thật) |
| Thiếu `serviceCode` hoặc `unitPrice` | `400`, `errors` theo trường |
| `unitPrice` < 0 | `400` |
| `serviceCode` đã có trong bảng giá này | `409` |
