# Hook Design Patterns

Tài liệu này định nghĩa các quy tắc thiết kế custom hook cho dự án, rút ra từ thực tiễn refactor tính năng *Dự án nguồn* (`source-projects`).

---

## 1. Nguyên tắc Single Responsibility

Mỗi custom hook chỉ đảm nhận **một trách nhiệm duy nhất**. Nếu một hook phải làm nhiều việc (fetch data, filter, paginate, mutate), hãy tách nó ra.

Phân loại theo trách nhiệm:

| Loại | Hậu tố gợi ý | Trách nhiệm |
|---|---|---|
| **Query Hook** | `useXxxQuery` | Fetch data, map dữ liệu từ backend, định nghĩa mutations (CRUD) |
| **Filter Hook** | `useXxxFilter` | Quản lý state filter/search/pagination, nhận data làm input |
| **Orchestrator Hook** | `useXxx` | Ghép các hook chuyên biệt, re-export qua namespace |

### Ví dụ thực tế (`source-projects`):
```
hooks/
├── useSourceProjectsQuery.js    ← fetch + mutations
├── useSourceProjectsFilter.js   ← filter + pagination
└── useSourceProjects.js         ← orchestrator (7 dòng)
```

---

## 2. Quy tắc về Return Object

### ❌ Không nên: Return object phẳng quá nhiều giá trị

Khi một hook return quá **7–8 fields**, đây là dấu hiệu hook đang làm quá nhiều việc.

```js
// ❌ Khó đọc, khó maintain
return {
  data, allItems, totalItems, totalPages,
  currentPage, setCurrentPage,
  searchTerm, setSearchTerm,
  statusFilter, setStatusFilter,
  yearFilter, setYearFilter,
  addItem, updateItem, deleteItem,
  activateItem, mergeItems,
  isLoading, isError,
};
```

### ✅ Nên: Nhóm theo namespace hoặc tách thành nhiều hooks

```js
// ✅ Orchestrator return rõ ràng
export function useSourceProjects() {
  const query = useSourceProjectsQuery();   // data & mutations
  const filter = useSourceProjectsFilter(query.allItems); // filter & pagination

  return { query, filter };
}
```

Consumer chỉ destructure phần cần thiết:
```js
const { query, filter } = useSourceProjects();

// Hoặc qua Context (phổ biến hơn):
const { data, searchTerm, addItem } = useSourceProjectsContext();
```

---

## 3. Query Hook – Quy tắc cụ thể

- **Query Keys**: Khai báo tập trung trong một constant object `QUERY_KEYS` trong cùng file.
  ```js
  const QUERY_KEYS = {
    allProjects: ['all-projects'],
    implProjects: ['impl-projects'],
  };
  ```
- **Mapper tách riêng**: Logic chuyển đổi dữ liệu backend → frontend đặt trong `utils/xxxMapper.js`, không viết inline trong hook.
- **onSuccess invalidate**: Các mutation cùng invalidate một nhóm query nên dùng chung hàm `invalidateQueries()`.
  ```js
  const invalidateQueries = () => {
    queryClient.invalidateQueries({ queryKey: QUERY_KEYS.allProjects });
  };
  const createMutation = useMutation({ mutationFn: api.create, onSuccess: invalidateQueries });
  const deleteMutation = useMutation({ mutationFn: api.delete, onSuccess: invalidateQueries });
  ```

---

## 4. Filter Hook – Quy tắc cụ thể

- **Nhận data làm input**: Filter hook không tự fetch, nhận `allItems` (array) làm tham số.
  ```js
  export function useXxxFilter(allItems = []) { ... }
  ```
- **Constants ra ngoài**: Các hằng số cấu hình (ví dụ `ITEMS_PER_PAGE`) khai báo bên ngoài hàm hook để tránh re-create mỗi lần render.
  ```js
  const ITEMS_PER_PAGE = 8; // ← ngoài hàm hook

  export function useXxxFilter(allItems = []) { ... }
  ```
- **Thứ tự xử lý**: Luôn apply filter trước, pagination sau.
  ```js
  const filteredData = useMemo(() => applyFilters(allItems), [allItems, ...filters]);
  const data = useMemo(() => paginate(filteredData, currentPage), [filteredData, currentPage]);
  ```

---

## 5. Orchestrator Hook – Quy tắc cụ thể

- Không chứa logic, không có `useState`/`useMemo`/`useCallback` trực tiếp.
- Chỉ gọi các hook chuyên biệt và ghép kết quả.
- Đây là **public API** của feature, tên hook phản ánh tên feature (không có hậu tố).

```js
// ✅ Orchestrator thuần túy
export function useSourceProjects() {
  const query = useSourceProjectsQuery();
  const filter = useSourceProjectsFilter(query.allItems);
  return { query, filter };
}
```

---

## 6. Tích hợp với React Context

Khi một feature dùng Context để chia sẻ state cho cây component, Context Provider nên:
- Gọi orchestrator hook để lấy `{ query, filter }`.
- Spread cả hai vào `value` để các component con vẫn dùng được flat API qua `useXxxContext()`.
- Bổ sung UI state cục bộ (modal open/close, selection) trực tiếp trong Context, không đưa vào hook.

```js
export function SourceProjectsProvider({ children }) {
  const { query, filter } = useSourceProjects();
  const [isModalOpen, setIsModalOpen] = useState(false);

  const value = {
    ...query,   // addItem, updateItem, deleteItem, ...
    ...filter,  // data, searchTerm, setSearchTerm, ...
    isModalOpen,
    setIsModalOpen,
    // ...các handlers toast.promise
  };

  return <Context.Provider value={value}>{children}</Context.Provider>;
}
```

---

## 7. Khi nào cần tách hook?

Dùng checklist sau để quyết định:

- [ ] Hook return **≥ 8 fields** → Xem xét tách.
- [ ] Hook vừa fetch data vừa quản lý filter/pagination → Tách thành Query + Filter.
- [ ] Logic mapping backend→frontend vượt **~15 dòng** → Chuyển ra `utils/xxxMapper.js`.
- [ ] Nhiều mutation dùng chung `onSuccess` → Gom vào một hàm `invalidateQueries`.
- [ ] Hook được dùng lại ở nhiều feature → Đưa vào `src/hooks/` dùng chung.
