# React Feature-Based Architecture & Coding Rules

## 1. Mục tiêu

Tài liệu này là guideline bắt buộc để AI Agent sử dụng khi xây dựng, refactor hoặc mở rộng React application.

Mục tiêu:

- Tổ chức code theo **feature/business domain**, không tổ chức chủ yếu theo loại file.
- UI component chỉ chịu trách nhiệm render và xử lý interaction gần UI.
- Business logic được tách khỏi component.
- Server state sử dụng **TanStack Query**.
- Global/client state chỉ dùng **Zustand** khi thực sự cần.
- State cục bộ hoặc state chỉ phục vụ một subtree dùng **React useState/useReducer/Context**.
- Component được chia nhỏ theo trách nhiệm, nhưng không chia nhỏ một cách máy móc.
- File dễ tìm, dễ đọc, dễ test và dễ thay đổi.
- Tránh tạo "god component", "god hook", "god store".
- Giữ dependency flow rõ ràng giữa UI → feature logic → data/API.

---

# 2. Nguyên tắc kiến trúc tổng thể

Ưu tiên kiến trúc:

```text
src/
├── app/
├── components/
├── features/
├── hooks/
├── lib/
├── services/
├── stores/
├── types/
└── utils/
```

Trong đó:

- `app/`: application bootstrap, router, providers, layout cấp ứng dụng.
- `features/`: business features chính. Đây là nơi chứa phần lớn code nghiệp vụ.
- `components/`: component dùng chung giữa nhiều feature.
- `hooks/`: custom hooks thực sự generic, không phụ thuộc một feature cụ thể.
- `services/`: infrastructure/API client dùng chung hoặc API layer có tính cross-feature.
- `stores/`: Zustand stores có phạm vi global/cross-feature.
- `lib/`: thư viện/configuration/adapters.
- `types/`: type dùng chung ở cấp application.
- `utils/`: utility thuần, không chứa business logic.

## Quy tắc quan trọng

> Nếu code chỉ phục vụ một feature, ưu tiên đặt code đó bên trong `features/<feature-name>/`.

Không đưa code feature-specific lên `components/`, `hooks/`, `services/`, `stores/` chỉ vì file đó là component/hook/service/store.

Ví dụ:

```text
features/
└── contracts/
    ├── api/
    ├── components/
    ├── hooks/
    ├── pages/
    ├── schemas/
    ├── types/
    └── utils/
```

Thay vì:

```text
components/
  ContractTable.tsx

hooks/
  useContracts.ts

services/
  contractService.ts

stores/
  contractStore.ts
```

nếu tất cả chỉ phục vụ Contract feature.

---

# 3. Cấu trúc feature chuẩn

Một feature có thể sử dụng cấu trúc:

```text
features/
└── contracts/
    ├── api/
    │   ├── contract.api.ts
    │   └── contract.query.ts
    │
    ├── components/
    │   ├── ContractTable/
    │   │   ├── ContractTable.tsx
    │   │   ├── ContractTableRow.tsx
    │   │   └── index.ts
    │   │
    │   ├── ContractFilters/
    │   │   ├── ContractFilters.tsx
    │   │   └── index.ts
    │   │
    │   ├── ContractForm/
    │   │   ├── ContractForm.tsx
    │   │   ├── ContractFormFields.tsx
    │   │   ├── contract-form.schema.ts
    │   │   └── index.ts
    │   │
    │   └── ContractStatusBadge.tsx
    │
    ├── hooks/
    │   ├── useContractFilters.ts
    │   ├── useContractForm.ts
    │   └── useContractActions.ts
    │
    ├── pages/
    │   ├── ContractsPage.tsx
    │   └── ContractDetailPage.tsx
    │
    ├── schemas/
    │   └── contract.schema.ts
    │
    ├── stores/
    │   └── contract-ui.store.ts
    │
    ├── types/
    │   └── contract.types.ts
    │
    └── utils/
        └── contract.utils.ts
```

Không bắt buộc feature nào cũng phải có toàn bộ thư mục trên.

> Chỉ tạo folder khi thực sự có code thuộc trách nhiệm đó.

Ví dụ feature nhỏ:

```text
features/
└── dashboard/
    ├── components/
    │   └── ProjectSummaryCard.tsx
    └── pages/
        └── DashboardPage.tsx
```

Không tạo 10 folder rỗng chỉ để "đúng architecture".

---

# 4. Quy tắc phân chia trách nhiệm

Luôn đặt câu hỏi:

> "Logic này thuộc UI, feature, server state, client state hay infrastructure?"

## UI Component

Component nên tập trung vào:

- render JSX
- props
- event handler trực tiếp
- accessibility
- layout
- visual state
- composition

Ví dụ tốt:

```tsx
function ContractStatusBadge({
  status,
}: {
  status: ContractStatus;
}) {
  return (
    <Badge variant={getStatusVariant(status)}>
      {getStatusLabel(status)}
    </Badge>
  );
}
```

Không nên để component chứa:

- API call phức tạp
- business rule dài
- transformation dữ liệu lớn
- permission logic phức tạp
- nhiều mutation orchestration
- xử lý cache phức tạp
- logic filter/sort/pagination quá dài

---

# 5. Page component

Page nên đóng vai trò **composition layer**.

Ví dụ:

```tsx
function ContractsPage() {
  return (
    <PageLayout>
      <ContractHeader />
      <ContractFilters />
      <ContractTable />
      <ContractPagination />
      <CreateContractDialog />
    </PageLayout>
  );
}
```

Page không nên trở thành nơi:

```tsx
// BAD

function ContractsPage() {
  const [filters, setFilters] = useState(...)
  const [selectedIds, setSelectedIds] = useState(...)
  const [isOpen, setIsOpen] = useState(...)
  const [form, setForm] = useState(...)

  const query = useQuery(...)

  const createMutation = useMutation(...)
  const updateMutation = useMutation(...)
  const deleteMutation = useMutation(...)

  // 300 dòng business logic...
}
```

Nếu Page bắt đầu quá dài, tách logic.

---

# 6. Quy tắc "Thin Component"

Mục tiêu:

```text
Page
  ↓
Feature components
  ↓
Hooks / Query / Mutation
  ↓
API
```

Không nên:

```text
Page
  ↓
500 lines of logic
  ↓
API
```

Một component UI lớn có thể được xem xét refactor khi:

- > 200–300 dòng và có nhiều trách nhiệm.
- Có nhiều state không liên quan nhau.
- Có nhiều `useEffect`.
- Có nhiều API calls.
- Có nhiều business conditions.
- JSX bị lồng quá sâu.
- Một phần UI có lifecycle/state riêng.
- Có block code có thể đặt tên thành một component/hook có ý nghĩa.

Không dùng số dòng như luật tuyệt đối. **Responsibility quan trọng hơn line count.**

---

# 7. Khi nào tạo component con?

Tách component khi:

### 7.1 Có trách nhiệm riêng

```text
ContractPage
├── ContractHeader
├── ContractFilters
├── ContractTable
├── ContractSummary
└── ContractFormDialog
```

### 7.2 Có state/interaction riêng

Ví dụ:

```text
ContractForm
└── PartnerSelector
```

`PartnerSelector` có search, loading, pagination riêng → nên tách.

### 7.3 Có khả năng tái sử dụng

Nếu component được sử dụng bởi nhiều nơi → cân nhắc đưa vào:

```text
components/
```

### 7.4 JSX quá phức tạp

Nếu một section có quá nhiều conditional rendering:

```tsx
{isAdmin && ...}
{isOwner && ...}
{canEdit && ...}
{status === ... && ...}
```

Hãy xem xét tách component hoặc đưa logic điều kiện vào hook/selector/helper.

---

# 8. Không over-componentize

Không tách những thứ quá nhỏ một cách vô nghĩa.

Không nên:

```text
ContractPage
├── ContractTitle
├── ContractTitleText
├── ContractTitleIcon
├── ContractTitleWrapper
```

nếu chúng không có trách nhiệm độc lập.

Nguyên tắc:

> Component nên đại diện cho một meaningful UI responsibility.

---

# 9. TanStack Query = Server State

TanStack Query được sử dụng cho dữ liệu đến từ backend.

Ví dụ:

- contracts
- projects
- users
- departments
- notifications
- payments
- reports

Không dùng Zustand để lưu server data nếu không có lý do đặc biệt.

## Recommended

```text
features/contracts/api/
├── contract.api.ts
└── contract.query.ts
```

### API

```ts
export async function getContracts(params: ContractListParams) {
  const response = await api.get('/contracts', {
    params,
  });

  return response.data;
}
```

### Query

```ts
export function useContracts(params: ContractListParams) {
  return useQuery({
    queryKey: contractKeys.list(params),
    queryFn: () => getContracts(params),
  });
}
```

### Query keys

Tập trung query key:

```ts
export const contractKeys = {
  all: ['contracts'] as const,

  lists: () => [...contractKeys.all, 'list'] as const,

  list: (params: ContractListParams) =>
    [...contractKeys.lists(), params] as const,

  details: () => [...contractKeys.all, 'detail'] as const,

  detail: (id: string) =>
    [...contractKeys.details(), id] as const,
};
```

Không hard-code query key rải rác trong component.

---

# 10. Mutation

Mutation nên được đặt gần feature API/query layer.

Ví dụ:

```text
features/contracts/api/
├── contract.api.ts
├── contract.query.ts
└── contract.mutation.ts
```

Component:

```tsx
const createContract = useCreateContract();

const handleSubmit = async (data: CreateContractInput) => {
  await createContract.mutateAsync(data);
};
```

Không viết toàn bộ API + invalidate cache + toast + transformation trực tiếp trong UI.

Mutation hook có thể chịu trách nhiệm:

```ts
export function useCreateContract() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createContract,
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: contractKeys.lists(),
      });
    },
  });
}
```

---

# 11. Khi nào dùng Zustand?

Zustand dùng cho **client state**, đặc biệt khi state:

- được dùng ở nhiều component không cùng subtree
- cần tồn tại xuyên nhiều màn hình
- là UI/application state
- không phải dữ liệu server

Ví dụ phù hợp:

```text
sidebar collapsed
theme
current workspace
selected project
global modal
notification preferences
multi-step UI state
```

Ví dụ:

```ts
type ContractUIState = {
  selectedIds: string[];
  setSelectedIds: (ids: string[]) => void;

  isCreateDialogOpen: boolean;
  setCreateDialogOpen: (open: boolean) => void;
};
```

Không nên:

```ts
// BAD

contractStore.contracts = [...]
```

nếu `contracts` là dữ liệu lấy từ API.

Hãy để TanStack Query quản lý.

---

# 12. Khi nào dùng React Context?

Context phù hợp khi state/config cần truyền xuống một subtree.

Ví dụ:

- Form context
- Permission context trong một module
- Theme context
- Feature-specific provider
- Configuration của một component tree

Không dùng Context để biến mọi thứ thành global state.

Nếu state thay đổi thường xuyên và được dùng rộng → cân nhắc Zustand.

Nếu state là server data → TanStack Query.

---

# 13. Decision tree cho State Management

AI Agent phải dùng quy tắc sau:

```text
Dữ liệu đến từ backend?
        │
        ├── YES → TanStack Query
        │
        └── NO
             │
             ├── Chỉ dùng trong component?
             │       └── useState / useReducer
             │
             ├── Chỉ dùng trong một subtree?
             │       └── Context
             │
             └── Dùng ở nhiều nơi / global?
                     └── Zustand
```

Không chọn Zustand chỉ vì "dễ dùng".

---

# 14. Form state

Form state nên được quản lý bởi form library hiện tại của project, ví dụ React Hook Form.

Ví dụ:

```text
features/contracts/components/ContractForm/
├── ContractForm.tsx
├── ContractFormFields.tsx
├── contract-form.schema.ts
└── index.ts
```

Form component:

```tsx
function ContractForm() {
  const form = useForm({
    resolver: zodResolver(contractFormSchema),
  });

  const mutation = useCreateContract();

  const onSubmit = form.handleSubmit(async (values) => {
    await mutation.mutateAsync(values);
  });

  return (
    <form onSubmit={onSubmit}>
      <ContractFormFields />
    </form>
  );
}
```

Schema không nên viết inline trong component lớn.

---

# 15. Business logic

Business logic nên được đặt trong:

- custom hook
- domain utility
- service/domain function
- mutation/query layer
- feature-specific helper

Ví dụ:

```ts
export function canEditContract(
  contract: Contract,
  user: User
) {
  return (
    user.isAdmin ||
    contract.createdBy === user.id
  );
}
```

Đặt:

```text
features/contracts/utils/contract-permission.ts
```

hoặc:

```text
features/contracts/domain/contract-permission.ts
```

nếu project sử dụng domain layer.

Không viết:

```tsx
{user.isAdmin ||
 contract.createdBy === user.id ? (
  <EditButton />
) : null}
```

rải rác khắp application.

---

# 16. Permission / Authorization

Permission logic nên được tập trung.

Ví dụ:

```text
features/contracts/
├── permissions/
│   └── contract.permissions.ts
```

```ts
export function canEditContract(context: ContractPermissionContext) {
  // business rules
}
```

UI chỉ gọi:

```tsx
const canEdit = canEditContract({
  user,
  contract,
});
```

Không duplicate permission rules ở nhiều component.

---

# 17. Custom Hooks

Custom hook nên có **một responsibility chính**.

Tốt:

```text
useContractFilters()
useContractActions()
useContractPermissions()
useContractForm()
```

Không tốt:

```text
useContractEverything()
```

Một hook không nên trở thành "component không có JSX".

Nếu hook dài hàng trăm dòng và xử lý quá nhiều thứ → chia nhỏ.

---

# 18. Folder con của Component

Với component phức tạp:

```text
ContractForm/
├── ContractForm.tsx
├── ContractFormFields.tsx
├── ContractPartnerSection.tsx
├── ContractPaymentSection.tsx
├── contract-form.schema.ts
├── contract-form.types.ts
└── index.ts
```

Khi component đơn giản:

```text
ContractStatusBadge.tsx
```

Không cần tạo folder.

Quy tắc:

> File đơn giản → file trực tiếp.  
> Component phức tạp → folder riêng.

---

# 19. Colocation

Code nên được đặt gần nơi sử dụng.

Ví dụ:

```text
features/contracts/components/ContractForm/
├── ContractForm.tsx
├── ContractFormFields.tsx
├── contract-form.schema.ts
└── contract-form.types.ts
```

Không nên:

```text
schemas/
  contractFormSchema.ts

types/
  contractFormTypes.ts

utils/
  contractFormUtils.ts
```

nếu chúng chỉ phục vụ ContractForm.

---

# 20. Shared Components

Chỉ đưa component vào:

```text
src/components/
```

khi nó thực sự dùng được ở nhiều feature.

Ví dụ:

```text
components/
├── DataTable/
├── ConfirmDialog/
├── EmptyState/
├── PageHeader/
├── SearchInput/
└── PermissionGate/
```

Không đưa:

```text
ContractTable
ProjectTable
PaymentTable
```

vào shared chỉ vì chúng đều là Table.

Nếu business behavior khác nhau, giữ trong feature.

---

# 21. Shared hooks

`src/hooks/` chỉ chứa hook generic.

Ví dụ:

```text
hooks/
├── useDebounce.ts
├── useMediaQuery.ts
├── usePrevious.ts
└── useClickOutside.ts
```

Không:

```text
hooks/
└── useContracts.ts
```

nếu hook chỉ phục vụ Contract feature.

Hãy đặt:

```text
features/contracts/api/contract.query.ts
```

hoặc:

```text
features/contracts/hooks/useContracts.ts
```

tùy trách nhiệm.

---

# 22. API layer

Không gọi axios/fetch trực tiếp trong UI component.

Không:

```tsx
useEffect(() => {
  axios.get('/api/contracts');
}, []);
```

Nên:

```text
features/contracts/api/
├── contract.api.ts
├── contract.query.ts
└── contract.mutation.ts
```

UI:

```tsx
const { data, isLoading } = useContracts(params);
```

---

# 23. Service layer

Không tạo service chỉ để bọc một dòng axios một cách máy móc.

Service phù hợp khi:

- có nhiều API operations
- có transformation
- có business/infrastructure orchestration
- tích hợp third-party
- cần tái sử dụng

Ví dụ:

```text
features/onlyoffice/
├── api/
├── services/
└── components/
```

---

# 24. Utils

Utility phải càng thuần càng tốt.

Tốt:

```ts
formatCurrency()
formatDate()
calculateDuration()
getContractStatusLabel()
```

Không nên để utility:

```ts
async function createContractAndInvalidateCache() {}
```

Đó không còn là utility thuần.

---

# 25. Barrel exports

Có thể dùng:

```text
components/ContractTable/index.ts
```

```ts
export { ContractTable } from './ContractTable';
```

Import:

```ts
import { ContractTable } from '../components/ContractTable';
```

Không tạo barrel file toàn application nếu khiến dependency khó trace.

Ưu tiên barrel ở phạm vi feature/component.

---

# 26. Dependency Direction

Ưu tiên dependency flow:

```text
Page
 ↓
Feature Components
 ↓
Feature Hooks
 ↓
Feature API
 ↓
Shared API Client
 ↓
Backend
```

Shared layer không được import ngược feature.

Ví dụ:

```text
components/
```

không được import:

```text
features/contracts/
```

nếu component đó được coi là shared.

Tương tự:

```text
hooks/useDebounce.ts
```

không nên import:

```text
features/contracts/types
```

---

# 27. Không tạo circular dependency

Tránh:

```text
contracts → projects → contracts
```

Nếu xảy ra, xem xét:

- đưa type dùng chung lên shared type
- đưa logic chung xuống lib/domain
- thay đổi dependency direction

---

# 28. Naming Convention

## Components

```text
ContractTable.tsx
ContractForm.tsx
ContractStatusBadge.tsx
```

PascalCase.

## Hooks

```text
useContracts.ts
useContractFilters.ts
```

camelCase với prefix `use`.

## Zustand

```text
contract-ui.store.ts
auth.store.ts
```

hoặc convention thống nhất của project.

## API

```text
contract.api.ts
contract.query.ts
contract.mutation.ts
```

## Types

```text
contract.types.ts
```

## Schemas

```text
contract.schema.ts
```

---

# 29. Naming phải phản ánh trách nhiệm

Không:

```text
helpers.ts
common.ts
misc.ts
utils2.ts
data.ts
manager.ts
```

Các tên này làm file khó tìm.

Nên:

```text
contract-permission.ts
contract-transformer.ts
contract-validation.ts
contract.mapper.ts
```

---

# 30. Component Props

Props nên rõ ràng.

Tốt:

```ts
type ContractTableProps = {
  contracts: Contract[];
  isLoading?: boolean;
  onSelect: (id: string) => void;
};
```

Không truyền quá nhiều dependency:

```tsx
<ContractTable
  api={api}
  queryClient={queryClient}
  user={user}
  store={store}
  router={router}
  config={config}
/>
```

Nếu component cần quá nhiều dependency → xem xét hook/composition.

---

# 31. Event handling

Không viết business logic dài trong JSX:

```tsx
onClick={() => {
  // 30 lines...
}}
```

Nên:

```tsx
const handleDelete = async () => {
  await deleteContract.mutateAsync(contract.id);
};

return (
  <Button onClick={handleDelete}>
    Xóa
  </Button>
);
```

Nếu handler vẫn dài → đưa vào hook/action.

---

# 32. useEffect

AI Agent phải hạn chế `useEffect`.

Không dùng `useEffect` để:

- đồng bộ server state thủ công
- tính derived state
- xử lý logic có thể làm trực tiếp trong render
- thay thế event handler
- fetch data khi TanStack Query đã phù hợp

Ví dụ không tốt:

```tsx
useEffect(() => {
  fetchContracts();
}, [filters]);
```

Nếu là server state:

```tsx
const query = useContracts(filters);
```

---

# 33. Derived State

Không tạo state cho dữ liệu có thể tính toán.

Không:

```tsx
const [total, setTotal] = useState(0);

useEffect(() => {
  setTotal(items.length);
}, [items]);
```

Nên:

```tsx
const total = items.length;
```

Nếu phép tính nặng:

```tsx
const total = useMemo(
  () => calculateTotal(items),
  [items]
);
```

Nhưng không lạm dụng `useMemo`.

---

# 34. Memoization

Không tự động dùng:

```text
useMemo
useCallback
React.memo
```

ở mọi nơi.

Chỉ dùng khi:

- phép tính thực sự tốn chi phí
- reference stability thực sự cần thiết
- component re-render thực sự gây vấn đề
- profiling cho thấy cần tối ưu

Code dễ đọc quan trọng hơn premature optimization.

---

# 35. Loading / Error / Empty State

Mỗi feature nên có chiến lược rõ ràng cho:

```text
Loading
Error
Empty
Success
```

Ví dụ:

```tsx
if (isLoading) {
  return <ContractTableSkeleton />;
}

if (isError) {
  return <ContractErrorState />;
}

if (!data.length) {
  return <ContractEmptyState />;
}

return <ContractTable data={data} />;
```

Có thể tách:

```text
components/
├── ContractTableSkeleton.tsx
├── ContractEmptyState.tsx
└── ContractErrorState.tsx
```

nếu UI đủ phức tạp.

---

# 36. Permission UI

Permission không chỉ dùng để ẩn button.

Ví dụ:

```tsx
{canEdit && <EditButton />}
```

nhưng backend vẫn phải enforce authorization.

Frontend permission chỉ là:

- UX
- hide/show action
- disable action
- prevent unnecessary requests

Không coi frontend permission là security boundary.

---

# 37. Modal / Dialog

Không để một Page quản lý hàng chục modal:

```tsx
const [isCreateOpen, ...]
const [isEditOpen, ...]
const [isDeleteOpen, ...]
const [isAssignOpen, ...]
const [isHistoryOpen, ...]
```

Nếu feature phức tạp:

```text
features/contracts/
├── components/
│   ├── CreateContractDialog/
│   ├── EditContractDialog/
│   ├── DeleteContractDialog/
│   └── ContractHistoryDialog/
└── stores/
    └── contract-ui.store.ts
```

Hoặc dialog tự quản lý state nếu state không cần chia sẻ.

---

# 38. List Page Pattern

Đối với các màn hình quản lý nghiệp vụ như:

- Nghị quyết
- Dự án
- Gói thầu
- Hợp đồng
- Đối tác
- Thanh toán

ưu tiên pattern:

```text
Page
├── PageHeader
├── Summary (optional)
├── Filters
├── Table
├── Pagination
└── Dialogs
```

Logic:

```text
Page
  ↓
useFilters()
  ↓
useQuery()
  ↓
Table
```

Mutation:

```text
Dialog
  ↓
useMutation()
  ↓
invalidateQueries()
```

---

# 39. Ví dụ hoàn chỉnh

```text
features/contracts/
│
├── api/
│   ├── contract.api.ts
│   ├── contract.query.ts
│   ├── contract.mutation.ts
│   └── contract.keys.ts
│
├── components/
│   ├── ContractTable/
│   │   ├── ContractTable.tsx
│   │   ├── ContractTableRow.tsx
│   │   └── index.ts
│   │
│   ├── ContractFilters/
│   │   ├── ContractFilters.tsx
│   │   └── index.ts
│   │
│   ├── ContractForm/
│   │   ├── ContractForm.tsx
│   │   ├── ContractFormFields.tsx
│   │   ├── ContractPartnerSection.tsx
│   │   ├── contract-form.schema.ts
│   │   └── index.ts
│   │
│   ├── CreateContractDialog.tsx
│   ├── EditContractDialog.tsx
│   └── ContractStatusBadge.tsx
│
├── hooks/
│   ├── useContractFilters.ts
│   └── useContractPermissions.ts
│
├── pages/
│   ├── ContractsPage.tsx
│   └── ContractDetailPage.tsx
│
├── stores/
│   └── contract-ui.store.ts
│
├── types/
│   └── contract.types.ts
│
├── schemas/
│   └── contract.schema.ts
│
└── utils/
    ├── contract.utils.ts
    └── contract-permission.ts
```

---

# 40. Ví dụ Page sau khi refactor

```tsx
export function ContractsPage() {
  const filters = useContractFilters();

  const contractsQuery = useContracts(filters.value);

  return (
    <PageLayout>
      <PageHeader
        title="Hợp đồng"
        action={<CreateContractButton />}
      />

      <ContractFilters
        value={filters.value}
        onChange={filters.setFilters}
      />

      <ContractTable
        data={contractsQuery.data?.items ?? []}
        isLoading={contractsQuery.isLoading}
      />

      <ContractPagination
        page={filters.value.page}
        total={contractsQuery.data?.total ?? 0}
        onChange={filters.setPage}
      />
    </PageLayout>
  );
}
```

Đây là mục tiêu:

> Page đọc vào phải hiểu được cấu trúc màn hình chỉ trong vài giây.

---

# 41. Nguyên tắc "Open File And Know Why"

Khi AI Agent cần sửa một nghiệp vụ, developer phải có khả năng đoán được file cần mở.

Ví dụ yêu cầu:

> "Sửa validation khi tạo hợp đồng"

AI Agent nên tìm:

```text
features/contracts/components/ContractForm/contract-form.schema.ts
```

Không phải tìm trong:

```text
src/utils/helpers.ts
src/common/common.ts
src/components/Form.tsx
src/services/service.ts
```

---

# 42. Nguyên tắc Feature Boundary

Một feature nên có boundary rõ ràng.

Ví dụ:

```text
features/projects/
features/bids/
features/contracts/
features/payments/
```

Nếu Contract cần Project:

```text
contracts → projects
```

chỉ import thứ thực sự cần.

Không import toàn bộ module.

---

# 43. Khi nào tách thành feature mới?

Tách feature khi nghiệp vụ có:

- route riêng
- business rules riêng
- API riêng
- permissions riêng
- nhiều component riêng
- lifecycle riêng

Ví dụ:

```text
features/contracts/
```

không nên chứa toàn bộ:

```text
contract
payment
partner
audit
notification
```

nếu chúng đã là các domain lớn.

---

# 44. Shared Domain

Nếu hai feature thực sự dùng chung một domain:

```text
features/projects/
features/contracts/
```

và cả hai đều cần `Partner`, có thể cân nhắc:

```text
features/partners/
```

hoặc shared domain tùy kiến trúc.

Không copy:

```text
ProjectPartner
ContractPartner
```

nếu thực chất là cùng một domain concept.

---

# 45. AI Agent Refactoring Rules

Khi được yêu cầu refactor một component, AI Agent phải thực hiện theo thứ tự:

### Step 1 — Đọc toàn bộ component

Xác định:

- UI
- server state
- client state
- form state
- business logic
- permission logic
- API calls
- derived state
- side effects

### Step 2 — Phân loại logic

```text
UI → Component
Server state → TanStack Query
Global client state → Zustand
Subtree state → Context
Local state → useState/useReducer
Form → React Hook Form / existing form solution
Business rule → hook/domain/utils
API → feature/api
```

### Step 3 — Thiết kế folder

Chỉ tạo folder cần thiết.

### Step 4 — Tách logic

Không thay đổi behavior nếu task chỉ là refactor.

### Step 5 — Tách component

Chỉ tách component có trách nhiệm rõ ràng.

### Step 6 — Kiểm tra dependency

Đảm bảo không tạo circular dependency.

### Step 7 — Kiểm tra API/query

Đảm bảo component không gọi API trực tiếp.

### Step 8 — Kiểm tra state

Đảm bảo không dùng Zustand cho server state.

### Step 9 — Kiểm tra UI

UI sau refactor phải giữ nguyên behavior và visual behavior nếu task không yêu cầu thay đổi UI.

### Step 10 — Cleanup

Xóa:

- dead code
- unused imports
- duplicate logic
- obsolete hooks
- state không còn sử dụng

---

# 46. AI Agent Không Được Làm

Không được:

- tạo `utils.ts` khổng lồ
- tạo `helpers.ts` khổng lồ
- tạo `common.ts` khổng lồ
- tạo `useEverything.ts`
- tạo Zustand store chứa toàn bộ application state
- dùng Zustand làm cache cho API data
- gọi API trực tiếp từ UI component
- dùng `useEffect` thay TanStack Query
- nhét business rule vào JSX
- tạo component con chỉ để giảm số dòng
- tạo folder rỗng không cần thiết
- duplicate business logic
- copy/paste permission logic
- tạo abstraction chỉ vì "clean code" nhưng không có nhu cầu thực tế
- refactor behavior khi task chỉ yêu cầu tổ chức code

---

# 47. Rule cho import

Ưu tiên:

```text
same component
    ↓
same feature
    ↓
shared
    ↓
external
```

Ví dụ:

```tsx
import { ContractForm } from '../components/ContractForm';
import { useContracts } from '../api/contract.query';

import { Button } from '@/components/ui/button';
import { api } from '@/lib/api';
```

Tránh import ngược từ shared vào feature.

---

# 48. Absolute Import

Nếu project đã cấu hình alias:

```ts
@/features
@/components
@/lib
```

hãy sử dụng nhất quán.

Ví dụ:

```tsx
import { Button } from '@/components/ui/button';
import { useContracts } from '@/features/contracts/api/contract.query';
```

Không trộn quá nhiều kiểu import:

```tsx
../../../components
@/components
../../../../features
```

---

# 49. Barrel Index

Có thể dùng:

```text
components/ContractForm/index.ts
```

nhưng không nên tạo:

```text
features/contracts/index.ts
```

chứa hàng trăm export nếu không thực sự cần.

Mục tiêu là giảm import path mà không che giấu dependency.

---

# 50. Checklist trước khi hoàn thành

AI Agent phải tự kiểm tra:

## Architecture

- [ ] Code feature-specific nằm trong feature.
- [ ] Shared code thực sự reusable.
- [ ] Không có circular dependency.
- [ ] Folder structure dễ tìm.

## UI

- [ ] Component chủ yếu render UI.
- [ ] Không có business logic dài trong JSX.
- [ ] Không có API call trực tiếp.
- [ ] Component có responsibility rõ ràng.

## State

- [ ] Server state → TanStack Query.
- [ ] Global client state → Zustand khi cần.
- [ ] Local state → useState/useReducer.
- [ ] Subtree state → Context khi phù hợp.
- [ ] Form state → form library.

## API

- [ ] API nằm trong feature/api.
- [ ] Query key được chuẩn hóa.
- [ ] Mutation xử lý cache invalidation hợp lý.
- [ ] Không duplicate API logic.

## Maintainability

- [ ] Không có `helpers.ts` khổng lồ.
- [ ] Không có `useEverything`.
- [ ] Không over-componentize.
- [ ] Không duplicate business rules.
- [ ] Naming rõ ràng.

## Quality

- [ ] Không unused code.
- [ ] Không unused imports.
- [ ] Không unnecessary useEffect.
- [ ] Không unnecessary useMemo/useCallback.
- [ ] Behavior không bị thay đổi ngoài phạm vi task.

---

# 51. Golden Rule

Khi không biết đặt code ở đâu, hãy hỏi 4 câu:

```text
1. Code này phục vụ feature nào?
        ↓
2. Đây là UI, state, API hay business logic?
        ↓
3. Nó được dùng ở đâu?
        ↓
4. Nó có thực sự cần shared/global không?
```

Sau đó áp dụng:

```text
Feature-specific
    → features/<feature>

Reusable UI
    → components/

Reusable hook
    → hooks/

Server state
    → TanStack Query

Global client state
    → Zustand

Subtree state
    → Context

Local state
    → useState / useReducer

API
    → feature/api

Business rule
    → feature/hooks | domain | utils

Pure utility
    → utils/
```

## Mục tiêu cuối cùng

Codebase phải đạt được 4 tiêu chí:

```text
EASY TO FIND
    ↓
EASY TO READ
    ↓
EASY TO CHANGE
    ↓
HARD TO BREAK
```

> Ưu tiên **đơn giản + rõ trách nhiệm + colocate code** hơn là tạo một architecture quá phức tạp.
