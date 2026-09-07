# Tài Liệu Yêu Cầu Sửa Đổi API — Dành Cho Backend Team

**Phiên bản:** 1.0  
**Ngày tạo:** 2026-08-26  
**Người yêu cầu:** Frontend Team  
**Mức độ ưu tiên:** High

---

## Bối Cảnh

Hiện tại có **5 vấn đề API** khiến Frontend phải tự thực hiện các tác vụ JOIN, filter, và lookup data — công việc đúng ra phải được Database/Backend xử lý. Điều này gây ra:

- Ứng dụng phải gọi `pageSize: 1000` để tải hàng nghìn record thừa về client
- Trình duyệt phải xử lý nhiều vòng lặp lồng nhau làm chậm UI (O(N²) complexity)
- Tốn băng thông mạng và RAM client không cần thiết

---

## 🔴 VẤN ĐỀ 1 — CRITICAL

### `GET /api/NghiepVu/du-an` (Dự án triển khai) không embed thông tin Dự án nguồn

#### Mô tả vấn đề

API trả về danh sách Dự án triển khai chỉ kèm theo các **chuỗi ID thô** của dự án nguồn (vốn) liên kết. Frontend không thể hiển thị tên/mã/giá trị của dự án nguồn chỉ từ ID.

**Workaround Frontend đang phải làm:**
1. Gọi thêm `GET /api/impl-projects?pageSize=1000` để lấy toàn bộ danh sách dự án (bao gồm cả loại 1 — Dự án nguồn)
2. Parse chuỗi `"guid-1;guid-2;guid-3"` thành mảng ID
3. Tự JOIN: lọc trong mảng 1000 dự án đó để tìm dự án nguồn phù hợp

#### Response hiện tại ❌

```json
{
  "items": [
    {
      "id": "abc-123",
      "name": "Dự án CNTT 2024",
      "loaiDuAn": 2,
      "nguonDuAnIds": "guid-1;guid-2",
      "listNguonDuAnIds": ["guid-1", "guid-2"],
      "sourceProjects": null
    }
  ]
}
```

#### Response yêu cầu ✅

```json
{
  "items": [
    {
      "id": "abc-123",
      "name": "Dự án CNTT 2024",
      "loaiDuAn": 2,
      "nguonDuAnIds": "guid-1;guid-2",
      "sourceProjects": [
        {
          "id": "guid-1",
          "code": "SP-2024-001",
          "name": "Nguồn vốn Ngân sách Nhà nước 2024",
          "tongDuToanHienTai": 5000000000
        },
        {
          "id": "guid-2",
          "code": "SP-2024-002",
          "name": "Nguồn vốn ODA",
          "tongDuToanHienTai": 3000000000
        }
      ]
    }
  ]
}
```

#### Thay đổi cần thực hiện

Khi query danh sách Dự án triển khai (`loaiDuAn = 2`), thực hiện JOIN với bảng Dự án nguồn (`loaiDuAn = 1`) dựa trên quan hệ ID đang lưu trong `nguonDuAnIds` / `listNguonDuAnIds`. Populate object `sourceProjects[]` với 4 trường: `id`, `code`, `name`, `tongDuToanHienTai`.

> **Lưu ý:** Field `nguonDuAnIds` và `listNguonDuAnIds` có thể giữ nguyên để không breaking change, chỉ cần bổ sung thêm field `sourceProjects`.

#### Tác động sau khi fix

Frontend sẽ xóa bỏ toàn bộ đoạn workaround ~40 dòng code và không cần gọi `pageSize: 1000` nữa trong `useImplProjectsQuery`.

---

## 🟠 VẤN ĐỀ 2 — HIGH

### `GET /api/NghiepVu/licenses` không hỗ trợ filter theo `hopDongId`

#### Mô tả vấn đề

API hàng hóa/dịch vụ (licenses) không nhận query parameter `hopDongId`. Khi cần lấy danh sách mặt hàng của một hợp đồng cụ thể, Frontend phải tải **tất cả** licenses trong hệ thống rồi tự filter trên client.

**Workaround Frontend đang phải làm:**
```javascript
// contractItemsService.js
const allPaged = await licensesApi.getAll({ page: 1, pageSize: 1000 });
const existingDbItems = (allPaged?.items || []).filter(
  (item) => item.hopDongId?.toLowerCase() === hopDongId.toLowerCase()
);
```

#### Endpoint hiện tại ❌

```
GET /api/NghiepVu/licenses?page=1&pageSize=1000
// Trả về toàn bộ, Frontend tự filter
```

#### Endpoint yêu cầu ✅

```
GET /api/NghiepVu/licenses?hopDongId={guid}&page=1&pageSize=50
```

#### Thay đổi cần thực hiện

Thêm query parameter `hopDongId` (optional) vào endpoint `GET /api/licenses`. Khi được truyền vào, filter kết quả theo `WHERE hopDongId = @hopDongId` trước khi phân trang.

Tương tự cho `GET /api/NghiepVu/licenses?duAnId={guid}` (đã có thể hoạt động nhưng cần xác nhận lại).

#### Tác động sau khi fix

Xóa được 3 lần gọi `pageSize: 1000` trong `contractItemsService.js` và logic filter client-side.

---

## 🟠 VẤN ĐỀ 3 — HIGH

### `GET /api/NghiepVu/hop-dong` (danh sách) không embed `projectName` và `contractorName`

#### Mô tả vấn đề

API danh sách hợp đồng chỉ trả về `duAnId` và `nhaThauId` dưới dạng ID thuần. Frontend cần hiển thị tên dự án và tên nhà thầu, nên phải gọi thêm 2 API phụ rồi tự JOIN.

> ⚡ **Lưu ý quan trọng:** API chi tiết `GET /api/contracts/{id}` đã làm đúng — có embed đủ `duAnName`, `nhaThau.name`, v.v. Chỉ cần áp dụng logic tương tự cho API danh sách.

**Workaround Frontend đang phải làm:**
```javascript
// useContractQueries.js
const { allData: projects } = useImplProjects();      // Gọi thêm API 1
const { allData: contractors } = useContractors();     // Gọi thêm API 2

// contractMappers.js
projectName: projects.find(p => p.id === item.duAnId)?.name || 'Dự án không xác định'
contractorName: contractors.find(c => c.id === item.nhaThauId)?.name || 'Nhà thầu không xác định'
```

#### Response hiện tại ❌

```json
{
  "items": [
    {
      "id": "hop-dong-guid",
      "soHopDong": "HD-2024-001",
      "duAnId": "project-guid",
      "nhaThauId": "contractor-guid",
      "giaTriHopDong": 500000000
    }
  ]
}
```

#### Response yêu cầu ✅

```json
{
  "items": [
    {
      "id": "hop-dong-guid",
      "soHopDong": "HD-2024-001",
      "duAnId": "project-guid",
      "duAnName": "Dự án CNTT 2024",
      "nhaThauId": "contractor-guid",
      "nhaThauName": "Công ty Cổ phần Công nghệ ABC",
      "nhaThau": {
        "id": "contractor-guid",
        "name": "Công ty Cổ phần Công nghệ ABC",
        "taxCode": "0123456789"
      },
      "giaTriHopDong": 500000000
    }
  ]
}
```

#### Thay đổi cần thực hiện

Trong query lấy danh sách hợp đồng, thực hiện JOIN:
- `DuAn` table → lấy `tenDuAn` → trả về field `duAnName`
- `NhaThau` table → lấy `tenNhaThau` → trả về field `nhaThauName` và object `nhaThau`

**Tham khảo:** Đây là cách API `GET /api/NghiepVu/hop-dong/{id}` đang làm — chỉ cần áp dụng tương tự cho query list.

#### Tác động sau khi fix

Xóa bỏ 2 API calls phụ (`useImplProjects`, `useContractors`) trong `useContractQueries.js`, giảm số lượng network requests mỗi khi mở trang danh sách hợp đồng từ 3 xuống còn 1.

---

## 🟡 VẤN ĐỀ 4 — MEDIUM

### `GET /api/HeThong/user` không có endpoint lấy thông tin user theo ID (hoặc batch by IDs)

#### Mô tả vấn đề

Để hiển thị tên + avatar của người quản lý dự án (projectManagerId), Frontend phải tải toàn bộ 1.000 user về rồi `.find()` trong đó.

**Workaround Frontend đang phải làm:**
```javascript
// ProjectManagerDisplay.jsx
const { data: usersData } = useUsers({ pageSize: 1000 }); // tải 1000 users
const matchedUsers = allUsers.filter(u => managerIds.has(u.id)); // tìm 1-2 người
```

#### Yêu cầu — Chọn 1 trong 2 cách

**Option A** — Embed thông tin user trực tiếp vào response dự án *(khuyên dùng)*:

```json
{
  "id": "project-guid",
  "projectManagerId": "user-guid",
  "projectManager": {
    "id": "user-guid",
    "fullName": "Nguyễn Văn A",
    "username": "nguyenvana",
    "avatarUrl": null
  }
}
```

**Option B** — Thêm endpoint batch lookup:

```
POST /api/HeThong/user/batch-by-ids
Content-Type: application/json

{ "ids": ["user-guid-1", "user-guid-2"] }
```

```json
// Response
[
  { "id": "user-guid-1", "fullName": "Nguyễn Văn A", "username": "nguyenvana" },
  { "id": "user-guid-2", "fullName": "Trần Thị B", "username": "tranthib" }
]
```

#### Tác động sau khi fix

Xóa được `pageSize: 1000` trong `ProjectManagerDisplay.jsx` và `ProjectMembersContext.jsx`.

---

## 🟡 VẤN ĐỀ 5 — MEDIUM

### `GET /api/NghiepVu/du-an` (Dự án nguồn) không hỗ trợ filter theo `status`

#### Mô tả vấn đề

Khi mở form tạo/sửa Dự án triển khai, người dùng cần chọn "Dự án nguồn" từ danh sách. Chỉ những dự án nguồn có trạng thái `Available` (chưa được phân bổ) mới được hiển thị. Hiện tại Frontend phải tải hết 1.000 dự án nguồn rồi tự filter.

**Workaround Frontend đang phải làm:**
```javascript
// useSourceProjectPicker.js
const result = await sourceProjectsApi.getAll({ page: 1, pageSize: 1000 });
const available = allProjects.filter(
  sp => sp.status === 'Available' || sp.allocatedProjectId === editingProjectId
);
```

#### Endpoint hiện tại ❌

```
GET /api/NghiepVu/du-an?loaiDuAn=1&page=1&pageSize=1000
```

#### Endpoint yêu cầu ✅

```
GET /api/NghiepVu/du-an?loaiDuAn=1&status=Available&page=1&pageSize=50
GET /api/NghiepVu/du-an?loaiDuAn=1&status=Available&allocatedProjectId={guid}&page=1&pageSize=50
```

#### Thay đổi cần thực hiện

Thêm query parameters (optional):
- `status` — filter theo trạng thái (`Available`, `Allocated`, v.v.)
- `allocatedProjectId` — nếu truyền vào, bao gồm thêm các dự án đang được phân bổ cho project đó (dùng khi edit)

#### Tác động sau khi fix

Giảm payload response từ 1.000 records xuống còn ~20-50 records mỗi lần mở form chọn dự án nguồn.

---

## 📊 Tóm Tắt Tổng Hợp

| # | Endpoint | Vấn đề | Mức độ | Thay đổi BE |
|---|----------|---------|--------|-------------|
| 1 | `GET /api/NghiepVu/du-an` | Thiếu embed `sourceProjects[]` | 🔴 Critical | JOIN thêm bảng dự án nguồn |
| 2 | `GET /api/NghiepVu/licenses` | Không filter `?hopDongId=` | 🟠 High | Thêm query param + WHERE clause |
| 3 | `GET /api/NghiepVu/hop-dong` | Thiếu `duAnName`, `nhaThauName` | 🟠 High | JOIN thêm 2 bảng khi query list |
| 4 | `GET /api/HeThong/user` | Không có batch lookup | 🟡 Medium | Thêm endpoint hoặc embed vào dự án |
| 5 | `GET /api/NghiepVu/du-an` | Không filter `?status=` | 🟡 Medium | Thêm query param + WHERE clause |

## 📋 Thứ Tự Ưu Tiên Đề Xuất

```
1. Vấn đề 3 (Contracts - embed tên)   ← Dễ nhất, tham chiếu API detail đã làm đúng
2. Vấn đề 2 (Licenses - filter)       ← Chỉ thêm WHERE clause đơn giản
3. Vấn đề 5 (Source projects - filter) ← Tương tự vấn đề 2
4. Vấn đề 1 (Impl projects - embed)   ← Phức tạp hơn do JOIN nhiều bảng
5. Vấn đề 4 (Users - batch lookup)    ← Tùy chọn cách implement
```

---

*Tài liệu này được tạo bởi Frontend Team dựa trên phân tích mã nguồn thực tế.*
