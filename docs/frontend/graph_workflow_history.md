Hãy cải thiện màn hình **Lịch sử công việc** hiện tại thành dạng **workflow graph đơn giản, trực quan và dễ thao tác**.

### Mục tiêu

Người dùng chỉ cần nhìn vào màn hình là hiểu được:

- Ai tạo công việc.
- Người nào đã nhận.
- Ai đã xác nhận.
- Ai đang chờ xác nhận.
- Ai đã chuyển tiếp cho ai.
- Khi một người chuyển tiếp cho nhiều người thì thể hiện được các nhánh.

Không cần xây dựng graph quá phức tạp.

---

### 1. Layout chính

Sử dụng graph dạng **trên xuống dưới**:

```text
        [Người tạo]
             │
             ▼
       [Người xử lý]
             │
       ┌─────┼─────┐
       ▼     ▼     ▼
      [B]   [C]   [D]
       ✓     ⏳    ✓
```

Ưu tiên:

- Dễ đọc.
- Ít màu.
- Khoảng cách thoáng.
- Không có quá nhiều control.
- Không cần minimap.
- Không cần animation phức tạp.
- Không cần zoom/pan nếu graph vẫn nằm vừa trong drawer.

---

### 2. Node

Mỗi node chỉ cần hiển thị:

```text
┌─────────────────────┐
│ 👤 Nguyễn Văn B     │
│ Phòng CNTT          │
│ ✓ Đã xác nhận       │
└─────────────────────┘
```

Thông tin chính:

- Avatar
- Tên
- Phòng ban/chức vụ nếu cần
- Trạng thái

Không đưa thời gian, nội dung chuyển tiếp và các thông tin dài vào node.

---

### 3. Trạng thái

Chỉ cần 3 trạng thái chính:

**Đã xác nhận**

```text
✓ Đã xác nhận
```

**Đang chờ**

```text
⏳ Chờ xác nhận
```

**Quá hạn**

```text
⚠ Quá hạn
```

Người tạo có thể thêm badge nhỏ:

```text
Người tạo
```

Không sử dụng quá nhiều màu.

---

### 4. Chuyển tiếp nhiều người

Đây là trường hợp quan trọng nhất.

Nếu B chuyển tiếp cho C, D, E:

```text
             [B]
              │
       ┌──────┼──────┐
       ▼      ▼      ▼
      [C]    [D]    [E]
       ✓      ⏳      ✓
```

Mũi tên chỉ đơn giản là đường nối giữa các node.

Không cần label trên từng mũi tên nếu không cần thiết.

Nếu cần thông tin chi tiết, click vào node để xem.

---

### 5. Click node

Khi click vào một người, mở một detail panel nhỏ hoặc drawer:

```text
Nguyễn Văn B

Phòng CNTT
Chuyên viên CNTT

✓ Đã xác nhận

Nhận lúc:
12/08/2026 09:00

Xác nhận lúc:
12/08/2026 09:15

Chuyển tiếp cho:
C, D, E
```

Chỉ hiển thị thông tin chi tiết khi người dùng cần.

---

### 6. Summary nhỏ phía trên

Ở đầu phần lịch sử hiển thị:

```text
Lịch sử xử lý

8 người tham gia · 5 đã xác nhận · 2 đang chờ · 1 quá hạn
```

Không cần tạo dashboard hoặc nhiều card thống kê.

---

### 7. UI/UX

Ưu tiên phong cách hiện tại của hệ thống:

- Background trung tính.
- Card trắng.
- Border nhẹ.
- Border radius vừa phải.
- Font weight vừa phải.
- Không lạm dụng bold.
- Màu chỉ dùng để biểu thị trạng thái.
- Node nhỏ gọn.
- Khoảng cách giữa các node đủ rộng để dễ nhìn.

Graph phải giống một **sơ đồ luồng xử lý nghiệp vụ**, không giống một flowchart kỹ thuật.

---

### 8. Không làm quá phức tạp

Không cần:

- Minimap.
- Toolbar nhiều chức năng.
- Zoom in/out nếu không cần.
- Animation.
- Drag node.
- Context menu.
- Filter phức tạp.
- Collapse/expand phức tạp.
- Các thao tác chỉnh sửa trực tiếp trên graph.

Mục tiêu chỉ là:

**Xem luồng → hiểu trạng thái → click người cần xem chi tiết.**

---

### 9. Implementation

Nếu project đã có thư viện graph phù hợp thì có thể tận dụng.

Nếu chưa có, ưu tiên implementation đơn giản bằng React + CSS/layout hiện tại thay vì thêm một thư viện lớn chỉ để vẽ vài node.

Tách component ở mức vừa phải:

```text
WorkHistory
├── WorkflowGraph
├── WorkflowNode
└── WorkflowNodeDetail
```

Không over-engineering.

---

### 10. Acceptance criteria

Đảm bảo tối thiểu các trường hợp:

**Một luồng:**

```text
A
↓
B
↓
C
```

**Một người chuyển tiếp nhiều người:**

```text
      B
   ↙  ↓  ↘
  C   D   E
```

**Có trạng thái khác nhau:**

```text
      B ✓
   ↙  ↓  ↘
  C ✓ D ⏳ E ⚠
```

**Có thể click vào từng node để xem chi tiết.**

Hãy ưu tiên **đơn giản, dễ đọc, dễ thao tác và phù hợp với hệ thống quản lý công việc doanh nghiệp**, không biến màn hình này thành một công cụ workflow designer.