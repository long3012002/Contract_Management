# Design System — Co-opBank Investment Project Management

Tài liệu thiết kế hệ thống giao diện cho website quản lý dự án đầu tư Co-opBank, xây dựng trên **React 19 + Vite + Tailwind CSS v4 + shadcn/UI**.

---

## 1. Typography — Hiển thị tiếng Việt rõ nét

### Lựa chọn Font

| Vai trò | Font | Lý do |
|---------|------|-------|
| **Display / Heading** | **Be Vietnam Pro** | Font được thiết kế riêng cho tiếng Việt, hỗ trợ đầy đủ dấu thanh, dấu mũ. Nét chữ hiện đại, chuyên nghiệp, phù hợp ngành tài chính |
| **Body / UI** | **Inter** | Font UI hàng đầu, có Vietnamese subset tốt, dễ đọc ở mọi kích thước, hỗ trợ tabular numbers cho dữ liệu tài chính |
| **Monospace / Data** | **JetBrains Mono** | Cho mã số, số liệu tài chính, bảng biểu cần canh cột |

### Type Scale

```
--font-size-xs:    0.75rem   / 12px   — Caption, helper text
--font-size-sm:    0.875rem  / 14px   — Body small, table cell
--font-size-base:  1rem      / 16px   — Body default
--font-size-lg:    1.125rem  / 18px   — Body large, subtitle
--font-size-xl:    1.25rem   / 20px   — Card title
--font-size-2xl:   1.5rem    / 24px   — Section heading
--font-size-3xl:   1.875rem  / 30px   — Page heading
--font-size-4xl:   2.25rem   / 36px   — Dashboard number
```

### Font Weight Mapping

| Weight | Giá trị | Sử dụng |
|--------|---------|---------|
| Regular | 400 | Body text, description |
| Medium | 500 | Labels, table headers, sidebar items |
| Semibold | 600 | Card titles, section headings |
| Bold | 700 | Page headings, KPI numbers |

### Line Height & Letter Spacing

```
Heading:  line-height: 1.2 — 1.3,  letter-spacing: -0.02em
Body:     line-height: 1.5 — 1.6,  letter-spacing: 0
Caption:  line-height: 1.4,         letter-spacing: 0.01em
```

> [!TIP]
> **Be Vietnam Pro** là font Google Fonts miễn phí, được thiết kế bởi nhà thiết kế người Việt. Các ký tự có dấu như `ă, â, ê, ô, ơ, ư, đ` và tổ hợp dấu thanh `ắ, ầ, ể, ồ, ớ, ừ, ặ, ẫ, ệ, ỗ, ợ, ữ` đều hiển thị sắc nét, không bị lệch baseline.

---

## 2. Color System

### 2.1 Brand Colors

```
Navy Primary:     #213B72   — hsl(219, 55%, 29%)
Red Accent:       #E42128   — hsl(358, 79%, 51%)
```

### 2.2 shadcn/UI CSS Variable Mapping (Light Mode)

```css
:root {
  /* === Base === */
  --background:           0 0% 100%;          /* #FFFFFF */
  --foreground:           219 55% 15%;         /* Gần navy, đậm hơn cho text */

  /* === Card === */
  --card:                 0 0% 100%;           /* #FFFFFF */
  --card-foreground:      219 55% 15%;

  /* === Popover === */
  --popover:              0 0% 100%;
  --popover-foreground:   219 55% 15%;

  /* === Primary (Navy) === */
  --primary:              219 55% 29%;         /* #213B72 */
  --primary-foreground:   0 0% 100%;           /* White text */

  /* === Secondary (Navy nhạt) === */
  --secondary:            219 30% 95%;         /* Navy tint rất nhạt */
  --secondary-foreground: 219 55% 29%;

  /* === Muted === */
  --muted:                215 20% 96%;         /* #F1F4F8 */
  --muted-foreground:     215 15% 47%;

  /* === Accent (Red) === */
  --accent:               358 79% 51%;         /* #E42128 */
  --accent-foreground:    0 0% 100%;

  /* === Destructive === */
  --destructive:          0 84% 60%;           /* Đỏ error, khác đỏ brand */
  --destructive-foreground: 0 0% 98%;

  /* === Border / Input / Ring === */
  --border:               215 20% 90%;         /* #E2E7ED */
  --input:                215 20% 90%;
  --ring:                 219 55% 29%;         /* Navy focus ring */

  /* === Radius === */
  --radius:               0.5rem;              /* 8px — bo tròn nhẹ, chuyên nghiệp */
}
```

### 2.3 Dark Mode

```css
.dark {
  --background:           220 25% 10%;         /* #161C27 */
  --foreground:           210 20% 92%;

  --card:                 220 25% 13%;         /* #1C2435 */
  --card-foreground:      210 20% 92%;

  --primary:              219 55% 45%;         /* Navy sáng hơn cho dark */
  --primary-foreground:   0 0% 100%;

  --secondary:            219 30% 18%;
  --secondary-foreground: 219 30% 85%;

  --muted:                220 20% 18%;
  --muted-foreground:     215 15% 60%;

  --accent:               358 79% 58%;         /* Red sáng hơn cho dark */
  --accent-foreground:    0 0% 100%;

  --destructive:          0 72% 51%;
  --destructive-foreground: 0 0% 98%;

  --border:               220 20% 20%;
  --input:                220 20% 20%;
  --ring:                 219 55% 50%;
}
```

### 2.4 Extended Palette — Semantic Colors

Ngoài biến shadcn, định nghĩa thêm các màu ngữ nghĩa cho ứng dụng quản lý dự án:

```css
:root {
  /* === Status Colors === */
  --status-success:       160 84% 39%;        /* #10B981 — Emerald */
  --status-warning:       38 92% 50%;         /* #F59E0B — Amber */
  --status-error:         0 84% 60%;          /* #EF4444 — Red */
  --status-info:          219 55% 29%;        /* #213B72 — Navy (brand) */

  /* === Status Background (nhạt, dùng cho badge/alert) === */
  --status-success-bg:    152 76% 95%;        /* #ECFDF5 */
  --status-warning-bg:    48 96% 95%;         /* #FFFBEB */
  --status-error-bg:      0 86% 97%;          /* #FEF2F2 */
  --status-info-bg:       219 40% 96%;        /* #EFF2F8 */

  /* === Project Phase Colors === */
  --phase-proposal:       262 83% 58%;        /* #8B5CF6 — Violet — Đề xuất */
  --phase-approval:       219 55% 29%;        /* #213B72 — Navy — Phê duyệt */
  --phase-execution:      199 89% 48%;        /* #0EA5E9 — Sky — Thực hiện */
  --phase-completed:      160 84% 39%;        /* #10B981 — Emerald — Hoàn thành */
  --phase-suspended:      38 92% 50%;         /* #F59E0B — Amber — Tạm dừng */
  --phase-cancelled:      0 84% 60%;          /* #EF4444 — Red — Hủy bỏ */

  /* === Chart / Data Viz (6-stop palette) === */
  --chart-1:              219 55% 29%;        /* Navy */
  --chart-2:              358 79% 51%;        /* Red brand */
  --chart-3:              199 89% 48%;        /* Sky */
  --chart-4:              160 84% 39%;        /* Emerald */
  --chart-5:              38 92% 50%;         /* Amber */
  --chart-6:              262 83% 58%;        /* Violet */
}
```

---

## 3. Component Design Tokens

### 3.1 Buttons

| Variant | Background | Text | Border | Sử dụng |
|---------|-----------|------|--------|---------|
| **Primary** | `--primary` (Navy) | White | — | Hành động chính: "Lưu", "Tạo dự án", "Phê duyệt" |
| **Secondary** | `--secondary` | `--secondary-foreground` | — | Hành động phụ: "Hủy", "Quay lại" |
| **Accent** | `--accent` (Red) | White | — | CTA nổi bật: "Gửi đề xuất", "Ký hợp đồng" — **dùng rất tiết kiệm** |
| **Outline** | Transparent | `--primary` | `--border` | Hành động bổ trợ: "Xuất file", "Xem chi tiết" |
| **Ghost** | Transparent | `--foreground` | — | Hành động nhẹ: icon buttons, menu items |
| **Destructive** | `--destructive` | White | — | Hành động nguy hiểm: "Xóa dự án", "Hủy hợp đồng" |
| **Link** | Transparent | `--primary` | — | Navigation inline |

> [!IMPORTANT]
> **Quy tắc sử dụng accent (đỏ)**: Mỗi màn hình chỉ nên có **tối đa 1 button accent**. Nếu có nhiều hành động, dùng Primary (navy) cho chính, Outline cho phụ.

### 3.2 Badges / Tags

```
┌─────────────────────────────────────────────────────────────┐
│  Badge System — Kết hợp màu nền nhạt + text đậm + bo tròn │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Trạng thái dự án:                                         │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌─────────┐       │
│  │ Đề xuất  │ │ Phê duyệt│ │ Thực hiện│ │Hoàn thành│      │
│  │ Violet   │ │  Navy    │ │  Sky     │ │ Emerald  │       │
│  └──────────┘ └──────────┘ └──────────┘ └─────────┘       │
│  ┌──────────┐ ┌──────────┐                                 │
│  │ Tạm dừng │ │ Hủy bỏ  │                                 │
│  │ Amber    │ │  Red     │                                 │
│  └──────────┘ └──────────┘                                 │
│                                                             │
│  Mức độ ưu tiên:                                           │
│  ┌──────┐ ┌───────┐ ┌────┐ ┌──────────┐                   │
│  │ Thấp │ │Trung bình│ │ Cao│ │ Khẩn cấp │                │
│  │ Gray │ │ Navy     │ │Amber│ │  Red     │                │
│  └──────┘ └────────┘ └────┘ └──────────┘                  │
│                                                             │
│  Loại hợp đồng:                                           │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐                   │
│  │ Xây lắp  │ │ Tư vấn   │ │ Mua sắm  │                  │
│  │ Sky      │ │ Violet   │ │ Emerald  │                   │
│  └──────────┘ └──────────┘ └──────────┘                   │
└─────────────────────────────────────────────────────────────┘
```

**Quy tắc badge:**
- Background: dùng màu nhạt (`-bg` variant, opacity 10-15%)
- Text: dùng màu đậm tương ứng
- Border-radius: `rounded-full` (pill shape)
- Font-size: `text-xs` (12px), font-weight: `medium` (500)
- Padding: `px-2.5 py-0.5`

### 3.3 Progress Bar

```
Tiến độ dự án — Gradient từ Navy → thay đổi theo mức:

0-25%:   Navy (#213B72)                     ████░░░░░░░░░░░░
25-50%:  Navy → Sky gradient                ████████░░░░░░░░
50-75%:  Sky → Emerald gradient             ████████████░░░░
75-100%: Emerald (#10B981)                  ████████████████

Track background: var(--muted) — #F1F4F8
Height: 8px (default), 4px (compact), 12px (large)
Border-radius: rounded-full

Tiến độ giải ngân:
- Hiển thị dạng segmented bar
- Mỗi segment = 1 đợt giải ngân
- Segment completed: Navy
- Segment current: Red (accent)
- Segment upcoming: Muted
```

### 3.4 Text Colors

| Token | Màu | Sử dụng |
|-------|-----|---------|
| `foreground` | Navy rất đậm `hsl(219, 55%, 15%)` | Body text chính |
| `muted-foreground` | `hsl(215, 15%, 47%)` | Text phụ, description, placeholder |
| `primary` | `#213B72` | Links, emphasis text, active states |
| `accent` | `#E42128` | Số liệu cần chú ý, deadline cận, vượt ngân sách |
| `status-success` | `#10B981` | Số dương, hoàn thành, đạt KPI |
| `status-warning` | `#F59E0B` | Cảnh báo, sắp đến hạn |
| `status-error` | `#EF4444` | Lỗi, quá hạn, vượt chi |

### 3.5 Card Design

```
┌──────────────────────────────────────────┐
│  Card — Dự án đầu tư                    │
│                                          │
│  Background: var(--card) — White         │
│  Border: 1px solid var(--border)         │
│  Border-radius: var(--radius) — 8px     │
│  Shadow: 0 1px 3px rgba(33,59,114,0.06) │
│          — navy-tinted shadow            │
│                                          │
│  Hover: shadow tăng lên                  │
│         0 4px 12px rgba(33,59,114,0.1)   │
│         border-color nhạt hơn            │
│         transition: 150ms ease           │
│                                          │
│  Padding: 24px (desktop), 16px (mobile)  │
│                                          │
│  Card Header:                            │
│    - Title: text-lg, semibold            │
│    - Subtitle: text-sm, muted-foreground │
│    - Có thể kèm badge trạng thái        │
│                                          │
│  Card Footer:                            │
│    - Border-top: 1px solid var(--border) │
│    - Chứa actions hoặc metadata          │
└──────────────────────────────────────────┘
```

### 3.6 Table Design

```
Table — Danh sách dự án / hợp đồng

Header:
  - Background: var(--muted) — #F1F4F8
  - Text: var(--muted-foreground), uppercase, text-xs, tracking-wide
  - Font-weight: 600
  - Padding: 12px 16px

Row:
  - Border-bottom: 1px solid var(--border)
  - Padding: 16px
  - Hover: background var(--muted) với opacity 50%

Selected Row:
  - Background: var(--secondary) — navy tint nhạt
  - Left border: 3px solid var(--primary)

Zebra striping: KHÔNG dùng — dùng border-bottom thay thế
```

### 3.7 Sidebar Navigation

```
Sidebar:
  - Background: var(--primary) — #213B72 (Navy)
  - Width: 260px (expanded), 68px (collapsed)
  - Text: white, opacity 70% (inactive), 100% (active)
  
  Active item:
    - Background: rgba(255, 255, 255, 0.12)
    - Border-left: 3px solid var(--accent) — #E42128 (Red)
    - Text: white, opacity 100%
    - Font-weight: 500
    
  Hover item:
    - Background: rgba(255, 255, 255, 0.06)
    
  Logo area:
    - Height: 64px
    - Logo Co-opBank trên nền navy
    - Separator: 1px solid rgba(255, 255, 255, 0.1)
```

### 3.8 Form Elements

```
Input / Select / Textarea:
  - Border: 1px solid var(--input)
  - Border-radius: var(--radius) — 8px
  - Height: 40px (default), 36px (compact)
  - Padding: 0 12px
  - Font-size: 14px (sm)
  
  Focus:
    - Border: 1px solid var(--ring) — Navy
    - Ring: 2px solid navy, opacity 20%
    - Outline: none
    
  Error:
    - Border: 1px solid var(--destructive)
    - Ring: 2px solid red, opacity 20%
    - Helper text: text-destructive, text-xs

  Disabled:
    - Opacity: 0.5
    - Cursor: not-allowed
    - Background: var(--muted)

Label:
  - Font-size: 14px
  - Font-weight: 500 (medium)
  - Color: var(--foreground)
  - Margin-bottom: 6px
```

### 3.9 Toast / Notification

```
Sử dụng Sonner (đã cài sẵn):

Success: icon ✓ emerald + text
Error:   icon ✕ red + text  
Warning: icon ⚠ amber + text
Info:    icon ℹ navy + text

Position: top-right
Duration: 4000ms (default), 6000ms (error)
```

---

## 4. Spacing & Layout System

### Grid

```
Sidebar (fixed) + Main content (fluid)

Desktop:  sidebar 260px + content (padding 32px)
Tablet:   sidebar collapsed 68px + content (padding 24px)
Mobile:   sidebar hidden (hamburger) + content (padding 16px)
```

### Spacing Scale (Tailwind default)

```
4px  → gap nhỏ nhất (icon-to-text)
8px  → gap giữa elements cùng group
12px → padding nội bộ compact
16px → padding card mobile, gap sections
24px → padding card desktop
32px → page padding, gap giữa sections lớn
48px → gap giữa major sections
```

---

## 5. Kế hoạch triển khai

### Bước 1: Setup shadcn/UI + Tailwind CSS v4

1. Cài đặt shadcn/UI CLI và khởi tạo
2. Cấu hình `components.json` với path aliases
3. Tạo file `src/index.css` với toàn bộ CSS variables ở trên
4. Import Google Fonts (Be Vietnam Pro + Inter)

### Bước 2: Cài đặt shadcn components cốt lõi

```bash
# UI primitives
npx shadcn@latest add button badge card input label select textarea
npx shadcn@latest add table dialog sheet dropdown-menu
npx shadcn@latest add tabs progress separator avatar
npx shadcn@latest add tooltip popover command
npx shadcn@latest add sidebar # shadcn sidebar component
```

### Bước 3: Tạo custom components

- `ProjectBadge` — badge trạng thái dự án với màu semantic
- `PriorityBadge` — badge mức ưu tiên  
- `ProjectProgress` — progress bar gradient theo mức
- `DisbursementBar` — segmented bar cho giải ngân
- `StatCard` — KPI card cho dashboard
- `DataTable` — table wrapper với sorting, filtering, pagination

### Bước 4: Tạo Layout components

- `AppLayout` — sidebar + main content
- `AppSidebar` — navigation sidebar navy
- `PageHeader` — breadcrumb + title + actions

---

## 6. Cấu trúc thư mục đề xuất

```
src/
├── components/
│   ├── ui/              ← shadcn/UI components (auto-generated)
│   │   ├── button.jsx
│   │   ├── badge.jsx
│   │   ├── card.jsx
│   │   └── ...
│   ├── layout/          ← Layout components
│   │   ├── AppLayout.jsx
│   │   ├── AppSidebar.jsx
│   │   └── PageHeader.jsx
│   └── shared/          ← Custom business components
│       ├── ProjectBadge.jsx
│       ├── PriorityBadge.jsx
│       ├── ProjectProgress.jsx
│       └── StatCard.jsx
├── pages/               ← Page components
│   ├── Dashboard.jsx
│   ├── Projects.jsx
│   ├── ProjectDetail.jsx
│   ├── Contracts.jsx
│   └── ...
├── lib/
│   └── utils.js         ← cn() helper từ shadcn
├── hooks/               ← Custom hooks
├── services/            ← API services (axios)
├── index.css            ← Design tokens + Tailwind
├── App.jsx
└── main.jsx
```

---

## 7. Critical UI/UX Patterns for Internal Management

### 7.1 Data-Heavy Tables
- Supports sorting, filtering, pagination, column visibility.
- Sticky headers, resizable columns.
- Row selection with bulk actions.
- Virtualized rendering for large datasets.

### 7.2 Loading / Empty / Error States
- Skeleton loaders for tables, cards, dashboards.
- Empty state illustrations with clear call‑to‑action.
- Error toast with retry button and error code.

### 7.3 Role‑Based UI
- Permission‑driven component rendering.
- Admin view shows management controls, audit logs.
- Analyst view hides destructive actions, shows read‑only data.

### 7.4 Navigation Patterns
- Persistent sidebar (nav) with collapsible sections.
- Breadcrumbs on top of pages for context.
- Top‑level global search with instant results.

### 7.5 Form Patterns
- Inline validation, debounced async checks.
- Multi‑step wizard for complex data entry (e.g., contract creation).
- Save‑draft and auto‑save features.

### 7.6 Dashboard Overview
- KPI cards, trend charts, recent activity feed.
- Configurable widgets, drag‑and‑drop layout for power users.
- Drill‑down links to detailed tables.

### 7.7 Dialogs & Modals
- Confirmation dialogs for destructive actions.
- Full‑screen modal for editing large entities.
- Accessible focus trapping and ESC close.

### 7.8 Responsive Strategy
- Mobile‑first breakpoints: sm (640px), md (768px), lg (1024px).
- Sidebar collapses to hamburger on <lg.
- Tables switch to card list on <md.

### 7.9 Accessibility
- WCAG AA contrast for all text (≥4.5:1).
- Keyboard navigation for all interactive elements.
- ARIA labels for icons, tables, modals.
- Focus-visible outlines using `--ring`.


## Open Questions

> [!IMPORTANT]
> **1. Dark mode**: Có cần hỗ trợ dark mode ngay từ đầu không? Hay chỉ light mode trước?

> [!IMPORTANT]
> **2. Responsive**: Ứng dụng này có cần hỗ trợ mobile/tablet không? Hay chỉ dùng trên desktop?

> [!IMPORTANT]
> **3. shadcn/UI version**: Bạn muốn dùng shadcn/UI bản mới nhất (hỗ trợ Tailwind v4) hay bản stable cũ hơn?

> [!IMPORTANT]
> **4. Sidebar style**: Sidebar navy đậm (như đã đề xuất) hay sidebar trắng minimalist? Sidebar navy tạo brand identity mạnh hơn nhưng sidebar trắng nhẹ nhàng hơn.

---

## Verification Plan

### Manual Verification
- Kiểm tra font tiếng Việt hiển thị đúng trên Chrome, Safari, Firefox
- Kiểm tra contrast ratio tất cả các màu text đạt WCAG AA (≥ 4.5:1)
- Kiểm tra responsive trên các breakpoints
- Review visual trên trình duyệt thực tế
