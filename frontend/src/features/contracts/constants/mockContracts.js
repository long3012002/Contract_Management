import { MOCK_IMPL_PROJECTS } from '../../implementation-projects/constants/mockImplProjects';
import { MOCK_CONTRACTORS } from '../../contractors/constants/mockContractors';

export const CONTRACT_STATUSES = [
  { value: 'Draft', label: 'Dự thảo' },
  { value: 'Active', label: 'Đang thực hiện' },
  { value: 'Expired', label: 'Hết hạn' },
  { value: 'Terminated', label: 'Đã thanh lý' },
];

const today = new Date();
const in15Days = new Date(today);
in15Days.setDate(today.getDate() + 15);
const pastMonth = new Date(today);
pastMonth.setMonth(today.getMonth() - 1);
const nextYear = new Date(today);
nextYear.setFullYear(today.getFullYear() + 1);

const formatDateStr = (d) => d.toISOString().split('T')[0];

export const MOCK_CONTRACTS = [
  {
    id: 'c-001',
    code: 'HD-2026-001',
    name: 'Hợp đồng triển khai Core Banking phase 1',
    projectId: 'ip-001',
    contractorId: 'ct-001', // Công ty ABC
    signedDate: '2026-02-20',
    startDate: '2026-02-25',
    expiredDate: formatDateStr(nextYear),
    warrantyPeriod: '12 tháng',
    paymentMethod: 'Chuyển khoản',
    value: 4500000000,
    status: 'Active',
    description: 'Bản quyền và dịch vụ triển khai giai đoạn 1.',
    paymentPlans: [
      { id: 'pp-1', name: 'Triển khai phần mềm', ratio: 30, value: 1350000000, startDate: '2026-02-25', endDate: '2026-05-20', condition: 'Ký biên bản nghiệm thu tổng thể' },
      { id: 'pp-2', name: 'License & Cloud', ratio: 70, value: 3150000000, startDate: '2026-05-21', endDate: '2026-08-20', condition: 'Kích hoạt bản quyền thành công' },
    ],
    files: [
      { id: 'f-1', name: 'HopDong_CoreBanking.pdf', url: '#' },
    ],
    createdAt: '2026-02-20T08:00:00',
    updatedAt: '2026-02-20T08:00:00',
  },
  {
    id: 'c-002',
    code: 'HD-2026-002',
    name: 'Hợp đồng mua sắm thiết bị HSM',
    projectId: 'ip-001',
    contractorId: 'ct-002', // Tổng công ty XYZ
    signedDate: '2026-03-01',
    startDate: '2026-03-05',
    expiredDate: formatDateStr(in15Days), // Sắp hết hạn (≤ 30 ngày)
    warrantyPeriod: '24 tháng',
    paymentMethod: 'Chuyển khoản',
    value: 2850000000,
    status: 'Active',
    description: 'Cung cấp 2 thiết bị HSM.',
    paymentPlans: [
      { id: 'pp-3', name: 'Thanh toán 100% sau bàn giao', ratio: 100, value: 2850000000, startDate: '2026-03-05', endDate: '2026-04-01', condition: 'Biên bản bàn giao thiết bị' }
    ],
    files: [],
    createdAt: '2026-03-01T09:00:00',
    updatedAt: '2026-03-01T09:00:00',
  },
  {
    id: 'c-003',
    code: 'HD-2025-001',
    name: 'Hợp đồng bản quyền ERP',
    projectId: 'ip-002',
    contractorId: 'ct-003', // DEF
    signedDate: '2025-02-15',
    startDate: '2025-02-15',
    expiredDate: formatDateStr(pastMonth), // Đã hết hạn
    warrantyPeriod: 'Không áp dụng',
    paymentMethod: 'Chuyển khoản',
    value: 11800000000,
    status: 'Terminated',
    description: 'Cung cấp bản quyền phần mềm ERP trọn đời.',
    paymentPlans: [],
    files: [],
    createdAt: '2025-02-15T10:00:00',
    updatedAt: '2025-12-01T15:00:00',
  }
];

export const PROJECT_OPTIONS = MOCK_IMPL_PROJECTS.map(p => ({
  value: p.id,
  label: p.name,
}));

export const CONTRACTOR_OPTIONS = MOCK_CONTRACTORS.map(c => ({
  value: c.id,
  label: `${c.name} (${c.taxCode})`,
}));
