import React, { useState, useEffect } from 'react';
import { toast } from 'sonner';
import { contractApi } from '../api/contractApi';
import Button from '../components/Button/Button';

export default function ContractManagement() {
  const [contracts, setContracts] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [loading, setLoading] = useState(false);

  // Form states
  const [showFormModal, setShowFormModal] = useState(false);
  const [editingContract, setEditingContract] = useState(null);

  // Form inputs
  const [contractNumber, setContractNumber] = useState('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [contractValue, setContractValue] = useState(0);
  const [signedDate, setSignedDate] = useState('');
  const [effectiveDate, setEffectiveDate] = useState('');
  const [expiredDate, setExpiredDate] = useState('');
  const [renewalReminderDate, setRenewalReminderDate] = useState('');
  const [isRenewalRequired, setIsRenewalRequired] = useState(true);
  const [status, setStatus] = useState('Draft');
  const [isActive, setIsActive] = useState(true);

  // Load contracts
  useEffect(() => {
    fetchContracts();
  }, [page, search]);

  const fetchContracts = async () => {
    try {
      setLoading(true);
      const data = await contractApi.getAll(search, page, pageSize);
      // Backend returns a PagedResult structure e.g., { items: [], totalCount: 12 }
      setContracts(data.items || []);
      setTotalCount(data.totalCount || 0);
    } catch (error) {
      console.error(error);
      toast.error('Không thể kết nối đến máy chủ hoặc tải danh sách hợp đồng.');
    } finally {
      setLoading(false);
    }
  };

  const openCreateModal = () => {
    setEditingContract(null);
    setContractNumber(`HD-${new Date().getFullYear()}-${Math.floor(1000 + Math.random() * 9000)}`);
    setTitle('');
    setDescription('');
    setContractValue(0);
    setSignedDate(new Date().toISOString().split('T')[0]);
    setEffectiveDate(new Date().toISOString().split('T')[0]);
    setExpiredDate('');
    setRenewalReminderDate('');
    setIsRenewalRequired(true);
    setStatus('Draft');
    setIsActive(true);
    setShowFormModal(true);
  };

  const openEditModal = (contract) => {
    setEditingContract(contract);
    setContractNumber(contract.contractNumber || '');
    setTitle(contract.title || '');
    setDescription(contract.description || '');
    setContractValue(contract.contractValue || 0);
    setSignedDate(contract.signedDate ? contract.signedDate.split('T')[0] : '');
    setEffectiveDate(contract.effectiveDate ? contract.effectiveDate.split('T')[0] : '');
    setExpiredDate(contract.expiredDate ? contract.expiredDate.split('T')[0] : '');
    setRenewalReminderDate(contract.renewalReminderDate ? contract.renewalReminderDate.split('T')[0] : '');
    setIsRenewalRequired(contract.isRenewalRequired !== false);
    setStatus(contract.status || 'Draft');
    setIsActive(contract.isActive !== false);
    setShowFormModal(true);
  };

  const handleSaveContract = async (e) => {
    e.preventDefault();
    if (!title.trim()) {
      toast.warning('Vui lòng điền tên hợp đồng.');
      return;
    }
    if (!contractNumber.trim()) {
      toast.warning('Vui lòng điền số hợp đồng.');
      return;
    }

    const payload = {
      title: title.trim(),
      description: description.trim(),
      contractValue: Number(contractValue) || 0,
      signedDate: signedDate ? new Date(signedDate).toISOString() : null,
      effectiveDate: effectiveDate ? new Date(effectiveDate).toISOString() : null,
      expiredDate: expiredDate ? new Date(expiredDate).toISOString() : null,
      renewalReminderDate: renewalReminderDate ? new Date(renewalReminderDate).toISOString() : null,
      isRenewalRequired,
      status,
      isActive,
      projectId: null,
      bidPackageId: null
    };

    try {
      setLoading(true);
      if (editingContract) {
        // Update (uses UpdateContractDto)
        await contractApi.update(editingContract.id, payload);
        toast.success('Cập nhật hợp đồng thành công!');
      } else {
        // Create (uses CreateContractDto wrapped in array)
        const createPayload = {
          ...payload,
          contractNumber: contractNumber.trim()
        };
        await contractApi.create(createPayload);
        toast.success('Tạo mới hợp đồng thành công!');
      }
      setShowFormModal(false);
      fetchContracts();
    } catch (error) {
      console.error(error);
      const errMsg = error.response?.data?.message || 'Có lỗi xảy ra khi lưu hợp đồng.';
      toast.error(errMsg);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteContract = async (id) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa hợp đồng này không?')) {
      return;
    }
    try {
      setLoading(true);
      await contractApi.delete(id);
      toast.success('Xóa hợp đồng thành công!');
      fetchContracts();
    } catch (error) {
      console.error(error);
      toast.error('Lỗi khi xóa hợp đồng.');
    } finally {
      setLoading(false);
    }
  };

  // Helper format currency
  const formatCurrency = (val) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
  };

  const formatShortDate = (dateStr) => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('vi-VN');
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="min-h-screen bg-slate-50 text-slate-800 p-6 font-sans">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex flex-col md:flex-row md:items-center md:justify-between mb-8 pb-5 border-b border-slate-200">
          <div>
            <h1 className="text-3xl font-bold tracking-tight text-slate-900 font-display">
              Quản Lý Hợp Đồng CNTT
            </h1>
            <p className="text-slate-500 mt-1 text-sm md:text-base">
              Theo dõi và cấu hình các hợp đồng, phụ lục, thời gian gia hạn và các cảnh báo liên quan.
            </p>
          </div>
          <div className="mt-4 md:mt-0 flex gap-2.5">
            <button
              onClick={fetchContracts}
              className="px-4 py-2 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 font-medium rounded-custom text-sm shadow-sm transition-colors flex items-center gap-1.5 cursor-pointer"
            >
              Tải lại
            </button>
            <Button onClick={openCreateModal} className="text-sm px-5 py-2 shadow-sm">
              <svg className="w-4 h-4 mr-1.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              Thêm hợp đồng mới
            </Button>
          </div>
        </div>

        {/* Filters/Search Area */}
        <div className="bg-white p-4 rounded-xl border border-slate-200/80 shadow-sm mb-6 flex flex-col md:flex-row items-center justify-between gap-4">
          <div className="w-full md:w-96 relative">
            <input
              type="text"
              placeholder="Tìm kiếm số hiệu hoặc tên hợp đồng..."
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
              className="w-full pl-9 pr-4 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
            />
            <svg className="w-4 h-4 text-slate-400 absolute left-3 top-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
          </div>

          <div className="text-xs text-slate-400 font-semibold uppercase tracking-wider">
            Tổng cộng: {totalCount} hợp đồng
          </div>
        </div>

        {/* Loading Pulsing State */}
        {loading && (
          <div className="w-full bg-blue-50 border border-blue-100 rounded-lg p-3 text-blue-700 mb-6 flex items-center gap-2.5 text-sm animate-pulse">
            <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
            </svg>
            <span>Đang đồng bộ dữ liệu hợp đồng...</span>
          </div>
        )}

        {/* Main List Table */}
        <div className="bg-white rounded-xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-100 bg-slate-100/40 text-slate-500 font-semibold text-xs tracking-wider uppercase">
                  <th className="p-4 pl-6">Số Hợp Đồng</th>
                  <th className="p-4">Tên Hợp Đồng</th>
                  <th className="p-4 text-right">Giá Trị</th>
                  <th className="p-4">Ngày Hiệu Lực</th>
                  <th className="p-4">Ngày Hết Hạn</th>
                  <th className="p-4 text-center">Trạng Thái</th>
                  <th className="p-4 text-center">Thao Tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-sm">
                {contracts.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="p-12 text-center text-slate-400">
                      Không tìm thấy dữ liệu hợp đồng nào thỏa mãn điều kiện.
                    </td>
                  </tr>
                ) : (
                  contracts.map((c) => (
                    <tr key={c.id} className="hover:bg-slate-50/40 transition-colors">
                      <td className="p-4 pl-6 font-mono font-bold text-xs text-primary">
                        {c.contractNumber}
                      </td>
                      <td className="p-4 font-semibold text-slate-900 max-w-xs truncate" title={c.title}>
                        {c.title}
                      </td>
                      <td className="p-4 text-right font-mono font-semibold text-slate-900">
                        {formatCurrency(c.contractValue)}
                      </td>
                      <td className="p-4 text-slate-500">
                        {formatShortDate(c.effectiveDate)}
                      </td>
                      <td className="p-4 text-slate-500">
                        {formatShortDate(c.expiredDate)}
                      </td>
                      <td className="p-4 text-center">
                        <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-3xs font-bold uppercase tracking-wider border ${
                          c.status === 'Active'
                            ? 'bg-emerald-50 text-emerald-700 border-emerald-100'
                            : c.status === 'Draft'
                            ? 'bg-slate-100 text-slate-700 border-slate-200'
                            : c.status === 'Expired'
                            ? 'bg-red-50 text-red-700 border-red-100'
                            : 'bg-amber-50 text-amber-700 border-amber-100'
                        }`}>
                          {c.status}
                        </span>
                      </td>
                      <td className="p-4 text-center">
                        <div className="flex items-center justify-center gap-1.5">
                          <button
                            onClick={() => openEditModal(c)}
                            className="p-1.5 border border-slate-200 rounded-lg hover:bg-slate-100 text-slate-600 hover:text-slate-900 transition-colors cursor-pointer"
                            title="Sửa hợp đồng"
                          >
                            <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                            </svg>
                          </button>
                          <button
                            onClick={() => handleDeleteContract(c.id)}
                            className="p-1.5 border border-red-100 hover:border-red-200 rounded-lg hover:bg-red-50 text-red-500 hover:text-red-700 transition-colors cursor-pointer"
                            title="Xóa hợp đồng"
                          >
                            <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                            </svg>
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="p-4 border-t border-slate-100 flex items-center justify-between bg-slate-50/50">
              <span className="text-xs text-slate-500">
                Trang {page} / {totalPages}
              </span>
              <div className="flex gap-2">
                <button
                  onClick={() => setPage((prev) => Math.max(prev - 1, 1))}
                  disabled={page === 1}
                  className="px-3 py-1.5 bg-white border border-slate-200 text-slate-700 font-semibold rounded-lg text-xs hover:bg-slate-50 disabled:opacity-50 transition-colors cursor-pointer"
                >
                  Trước
                </button>
                <button
                  onClick={() => setPage((prev) => Math.min(prev + 1, totalPages))}
                  disabled={page === totalPages}
                  className="px-3 py-1.5 bg-white border border-slate-200 text-slate-700 font-semibold rounded-lg text-xs hover:bg-slate-50 disabled:opacity-50 transition-colors cursor-pointer"
                >
                  Sau
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* FORM MODAL */}
      {showFormModal && (
        <div className="fixed inset-0 bg-slate-900/60 flex items-center justify-center p-4 z-50 overflow-y-auto">
          <div className="bg-white rounded-2xl border border-slate-200 shadow-2xl w-full max-w-2xl overflow-hidden my-8">
            {/* Modal Header */}
            <div className="p-5 border-b border-slate-100 bg-slate-50 flex items-center justify-between">
              <h2 className="text-lg font-bold font-display text-slate-950">
                {editingContract ? 'Cập Nhật Thông Tin Hợp Đồng' : 'Thêm Hợp Đồng Mới'}
              </h2>
              <button
                onClick={() => setShowFormModal(false)}
                className="text-slate-400 hover:text-slate-600 p-1"
              >
                <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            {/* Modal Form */}
            <form onSubmit={handleSaveContract} className="p-6 space-y-4 max-h-[70vh] overflow-y-auto">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Số Hợp Đồng
                  </label>
                  <input
                    type="text"
                    value={contractNumber}
                    onChange={(e) => setContractNumber(e.target.value)}
                    disabled={!!editingContract}
                    placeholder="Ví dụ: HD-2026-9912"
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary disabled:bg-slate-50 disabled:text-slate-400"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Trạng Thái
                  </label>
                  <select
                    value={status}
                    onChange={(e) => setStatus(e.target.value)}
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary bg-white"
                  >
                    <option value="Draft">Draft (Bản nháp)</option>
                    <option value="Active">Active (Đang hiệu lực)</option>
                    <option value="Expired">Expired (Hết hiệu lực)</option>
                    <option value="Pending">Pending (Chờ duyệt)</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                  Tên Hợp Đồng
                </label>
                <input
                  type="text"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="Ví dụ: Mua sắm bản quyền phần mềm Microsoft Office 365"
                  className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Giá Trị Hợp Đồng (VNĐ)
                  </label>
                  <input
                    type="number"
                    value={contractValue}
                    onChange={(e) => setContractValue(Number(e.target.value))}
                    placeholder="0"
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary font-mono"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Ngày Ký Kết
                  </label>
                  <input
                    type="date"
                    value={signedDate}
                    onChange={(e) => setSignedDate(e.target.value)}
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Ngày Hiệu Lực
                  </label>
                  <input
                    type="date"
                    value={effectiveDate}
                    onChange={(e) => setEffectiveDate(e.target.value)}
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Ngày Hết Hạn
                  </label>
                  <input
                    type="date"
                    value={expiredDate}
                    onChange={(e) => setExpiredDate(e.target.value)}
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Ngày Nhắc Gia Hạn
                  </label>
                  <input
                    type="date"
                    value={renewalReminderDate}
                    onChange={(e) => setRenewalReminderDate(e.target.value)}
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                  />
                </div>

                <div className="flex items-center gap-2 pt-5">
                  <input
                    type="checkbox"
                    id="isRenewalRequired"
                    checked={isRenewalRequired}
                    onChange={(e) => setIsRenewalRequired(e.target.checked)}
                    className="w-4 h-4 rounded text-primary border-slate-300 focus:ring-primary/20 accent-primary"
                  />
                  <label htmlFor="isRenewalRequired" className="text-xs font-semibold text-slate-500 uppercase tracking-wider select-none">
                    Yêu cầu gia hạn (Renewal Required)
                  </label>
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                  Mô Tả Hợp Đồng
                </label>
                <textarea
                  rows={3}
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Ghi chú chi tiết thông tin đối tác, thiết bị mua sắm hoặc điều khoản gia hạn..."
                  className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary resize-none"
                />
              </div>

              {editingContract && (
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="isActive"
                    checked={isActive}
                    onChange={(e) => setIsActive(e.target.checked)}
                    className="w-4 h-4 rounded text-primary border-slate-300 focus:ring-primary/20 accent-primary"
                  />
                  <label htmlFor="isActive" className="text-xs font-semibold text-slate-500 uppercase tracking-wider select-none">
                    Hợp đồng đang Active
                  </label>
                </div>
              )}

              {/* Modal Actions */}
              <div className="flex justify-end gap-2.5 pt-4 border-t border-slate-100 mt-6">
                <button
                  type="button"
                  onClick={() => setShowFormModal(false)}
                  className="px-4 py-2 border border-slate-200 text-slate-700 font-semibold rounded-xl text-sm hover:bg-slate-50 transition-colors cursor-pointer"
                >
                  Hủy bỏ
                </button>
                <Button type="submit" className="text-sm px-6">
                  {editingContract ? 'Lưu cập nhật' : 'Tạo mới'}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
