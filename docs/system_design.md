# Dashboard System Design: Monochrome & #00377B Accent

Thiết kế dựa trên nền tảng **Monochrome (Đen/Trắng)** kết hợp với màu thương hiệu **#00377B** mang lại phong cách rất hiện đại, tối giản và chuyên nghiệp (Enterprise-grade). Việc sử dụng nền tảng trắng/đen làm chủ đạo giúp nội dung nổi bật, đồng thời làm cho các màu sắc ngữ nghĩa (semantic colors) như Success, Warning, Error không bị xung đột, giúp người dùng dễ dàng nhận diện trạng thái.

Dưới đây là **System Design** chi tiết cho Dashboard của dự án:

## 1. Color Palette (Bảng màu)

### Nền tảng Đen - Trắng (Grayscale / Zinc)
Sử dụng dải màu `zinc` của Tailwind CSS làm chủ đạo (tương tự mặc định của shadcn/ui) vì nó mang sắc thái xám lạnh, sang trọng:
- **Background**: `#FFFFFF` (Trắng tinh) cho nền ứng dụng và các Card/Panel.
- **Background Muted**: `#F4F4F5` (Zinc-100) cho sidebar, nền bảng (table header), hoặc vùng cần phân cách.
- **Border**: `#E4E4E7` (Zinc-200) cho viền mỏng, divider.
- **Text Primary**: `#09090B` (Zinc-950) cho tiêu đề, nội dung chính.
- **Text Secondary**: `#71717A` (Zinc-500) cho mô tả, placeholder, nội dung phụ.

### Màu Thương hiệu (Brand Accent - #00377B)
Màu xanh `#00377B` sẽ đóng vai trò tạo điểm nhấn (Primary action, Active state, Branding).
Dải màu từ 20% đến 80% được tạo ra để sử dụng linh hoạt:
- **Primary (100%)**: `#00377B` - Nút bấm chính (Solid Button), Logo, Link, Icon quan trọng.
- **Primary-80**: `#335F95` - Hover state cho các nút bấm chính.
- **Primary-60**: `#6687B0` - Viền (Border) khi focus vào các ô input.
- **Primary-40**: `#99AFCA` - Trạng thái disabled hoặc Placeholder nhấn mạnh.
- **Primary-20**: `#CCD7E4` - Border của các thành phần tương tác nhẹ.
- **Primary-10** (rất nhạt): `#E6EBF2` - Background cho nút bấm dạng Secondary/Ghost, hoặc nền của mục đang được chọn (Active Menu Item) trên Sidebar.

### Màu Ngữ nghĩa (Semantic / Status Colors)
Nhờ có nền trắng/đen, các màu badge này sẽ cực kỳ nổi bật:
- **Success**: `#10B981` (Emerald) - Trạng thái thành công, Đã duyệt.
- **Warning**: `#F59E0B` (Amber) - Cảnh báo, Đang chờ xử lý.
- **Error/Destructive**: `#EF4444` (Red) - Lỗi, Từ chối, Xóa.
- **Info**: `#0EA5E9` (Sky) - Thông tin bổ sung (nếu cần phân biệt với Primary).

---

## 2. Typography (Kiểu chữ)

- **Font Family**: Khuyến nghị sử dụng **Inter** (mặc định của Tailwind) hoặc **Geist** (mang hơi hướng dev/tech), **Plus Jakarta Sans** (rất hiện đại cho dashboard tài chính).
- **Heading**: Dùng font-weight 600 (Semibold) hoặc 700 (Bold), màu `Zinc-950`.
- **Body**: Dùng font-weight 400 (Regular) hoặc 500 (Medium), màu `Zinc-950` hoặc `Zinc-500`.

---

## 3. UI Components (Tùy biến shadcn/ui)

- **Buttons**:
  - `variant="default"`: Nền `#00377B`, chữ Trắng, bo góc vừa phải (radius `0.5rem` hoặc `0.375rem`).
  - `variant="outline"`: Viền Đen/Xám, nền Trắng, khi hover chuyển sang nền Xám nhạt.
  - `variant="ghost"`: Không viền, khi hover có nền `#E6EBF2` (Primary 10%) và chữ `#00377B`.
- **Cards (Thẻ nội dung)**:
  - Nền Trắng, viền Xám nhạt (`Zinc-200`), đổ bóng nhẹ (`shadow-sm`).
  - Phù hợp để chứa biểu đồ (Charts), Bảng số liệu (Tables).
- **Badges (Nhãn trạng thái)**:
  - Dạng Outline (Viền màu + Chữ màu) hoặc Nền nhạt (Nền 10% màu + Chữ 100% màu) để giữ sự thanh thoát.
  - Ví dụ Badge Success: Nền xanh lá cực nhạt + Chữ màu Emerald.
- **Inputs & Forms**:
  - Trạng thái bình thường: Viền xám (`Zinc-200`).
  - Trạng thái Focus: Viền chuyển sang `#00377B` (Ring Primary) + shadow xanh nhạt.

---

## 4. Layout & Structure (Cấu trúc giao diện)

- **Sidebar (Menu bên trái)**:
  - Nền Đen (`Zinc-950`) chữ Trắng để tách biệt không gian, HOẶC nền Trắng tinh viền phải `Zinc-200`. Khuyến nghị **Sidebar Trắng/Xám nhạt (Zinc-50)** kết hợp Logo nhấn màu `#00377B` sẽ mang lại cảm giác sạch sẽ nhất.
  - Các mục (Menu items) đang active: Background `#E6EBF2` (10% Primary) với chữ `#00377B` đậm, có thể thêm viền trái (border-left) dày 3px màu `#00377B`.
- **Header (Thanh điều hướng trên cùng)**:
  - Nền Trắng, có viền dưới phân cách `Zinc-200`.
  - Chứa thanh tìm kiếm (Search), Thông báo (Notification), và User Profile.
- **Main Content Area (Khu vực chính)**:
  - Nền hơi xám cực nhạt (`#FAFAFA` hoặc `#F4F4F5`) để các khối Cards nền Trắng nổi lên rõ ràng.

---

## 5. Áp dụng vào shadcn/ui (CSS Variables)

Cấu hình mẫu cho CSS (`frontend/src/index.css` hoặc `globals.css`):

```css
@layer base {
  :root {
    --background: 0 0% 100%;       /* White */
    --foreground: 240 10% 3.9%;    /* Zinc 950 */

    --card: 0 0% 100%;
    --card-foreground: 240 10% 3.9%;

    --popover: 0 0% 100%;
    --popover-foreground: 240 10% 3.9%;

    /* Biến đổi từ màu #00377B (HSL: 213, 100%, 24%) */
    --primary: 213 100% 24%;
    --primary-foreground: 0 0% 100%;

    /* Dùng Primary-10 cho muted/secondary */
    --secondary: 213 40% 94%;      /* Tương đương #E6EBF2 */
    --secondary-foreground: 213 100% 24%;

    --muted: 240 4.8% 95.9%;
    --muted-foreground: 240 3.8% 46.1%;

    --accent: 213 40% 94%;
    --accent-foreground: 213 100% 24%;

    --destructive: 0 84.2% 60.2%;
    --destructive-foreground: 0 0% 98%;

    --border: 240 5.9% 90%;
    --input: 240 5.9% 90%;
    --ring: 213 100% 24%;

    --radius: 0.5rem;
  }
}
```
