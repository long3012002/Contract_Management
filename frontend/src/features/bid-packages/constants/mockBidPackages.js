import { MOCK_IMPL_PROJECTS } from '../../implementation-projects/constants/mockImplProjects';

export const BID_PACKAGE_STATUSES = [
  { value: 'Draft', label: 'Bản nháp' },
  { value: 'Active', label: 'Đang hoạt động' },
  { value: 'Completed', label: 'Đã hoàn thành' },
];

export const MOCK_BID_PACKAGES = [
  {
    id: 'bp-001',
    code: 'GT-2026-001',
    name: 'Mua sắm thiết bị HSM phần cứng',
    projectId: 'ip-001', // Core Banking
    estimatedValue: 3000000000,
    totalContractValue: 2850000000, // 95%
    warningThresholdPercent: 90,
    status: 'Active',
    description: 'Cung cấp và lắp đặt thiết bị HSM chuyên dụng cho hệ thống Core Banking.',
    createdAt: '2026-02-10T08:00:00',
    updatedAt: '2026-03-01T10:00:00',
  },
  {
    id: 'bp-002',
    code: 'GT-2026-002',
    name: 'Triển khai phần mềm Core Banking',
    projectId: 'ip-001',
    estimatedValue: 6000000000,
    totalContractValue: 4500000000, // 75%
    warningThresholdPercent: 90,
    status: 'Active',
    description: 'Mua bản quyền và dịch vụ triển khai.',
    createdAt: '2026-02-15T09:00:00',
    updatedAt: '2026-04-10T14:00:00',
  },
  {
    id: 'bp-003',
    code: 'GT-2025-001',
    name: 'Bản quyền hệ thống ERP',
    projectId: 'ip-002', // ERP
    estimatedValue: 12000000000,
    totalContractValue: 11800000000, // 98.3%
    warningThresholdPercent: 95,
    status: 'Completed',
    description: 'Mua bản quyền phần mềm ERP.',
    createdAt: '2025-02-01T08:00:00',
    updatedAt: '2025-10-15T09:00:00',
  },
  {
    id: 'bp-004',
    code: 'GT-2026-003',
    name: 'Cung cấp máy chủ Cloud',
    projectId: 'ip-004', // Data Center
    estimatedValue: 8000000000,
    totalContractValue: 8100000000, // 101.25% - Vượt dự toán
    warningThresholdPercent: 100,
    status: 'Active',
    description: 'Thuê hạ tầng máy chủ ảo hoá.',
    createdAt: '2026-05-10T08:00:00',
    updatedAt: '2026-06-05T10:00:00',
  },
  {
    id: 'bp-005',
    code: 'GT-2026-004',
    name: 'Tư vấn giám sát triển khai Payment Gateway',
    projectId: 'ip-006', // Payment Gateway
    estimatedValue: 500000000,
    totalContractValue: 0,
    warningThresholdPercent: 90,
    status: 'Draft',
    description: 'Thuê đơn vị tư vấn giám sát độc lập.',
    createdAt: '2026-05-01T14:00:00',
    updatedAt: '2026-05-01T14:00:00',
  }
];

export const PROJECT_OPTIONS = MOCK_IMPL_PROJECTS.map(p => ({
  value: p.id,
  label: p.name,
}));
