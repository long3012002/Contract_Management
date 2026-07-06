import React, { useState, useEffect } from 'react';
import { toast } from 'sonner';
import { partnerApi } from '../api/partnerApi';
import Button from '../components/Button/Button';

export default function PartnerManagement() {
  const [partners, setPartners] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [loading, setLoading] = useState(false);

  // Form Modal States
  const [showFormModal, setShowFormModal] = useState(false);
  const [editingPartner, setEditingPartner] = useState(null);

  // Form Inputs
  const [partnerCode, setPartnerCode] = useState('');
  const [partnerName, setPartnerName] = useState('');
  const [taxCode, setTaxCode] = useState('');
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [address, setAddress] = useState('');
  const [description, setDescription] = useState('');
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    fetchPartners();
  }, [page, search]);

  const fetchPartners = async () => {
    try {
      setLoading(true);
      const data = await partnerApi.getAll(search, page, pageSize);
      setPartners(data.items || []);
      setTotalCount(data.totalCount || 0);
    } catch (error) {
      console.error(error);
      toast.error('Không thể kết nối đến máy chủ hoặc tải danh sách đối tác.');
    } finally {
      setLoading(false);
    }
  };

  const openCreateModal = () => {
    setEditingPartner(null);
    setPartnerCode(`DT-${new Date().getFullYear()}-${Math.floor(100 + Math.random() * 900)}`);
    setPartnerName('');
    setTaxCode('');
    setPhone('');
    setEmail('');
    setAddress('');
    setDescription('');
    setIsActive(true);
    setShowFormModal(true);
  };

  const openEditModal = (partner) => {
    setEditingPartner(partner);
    setPartnerCode(partner.code || '');
    setPartnerName(partner.name || '');
    setTaxCode(partner.taxCode || '');
    setPhone(partner.phone || '');
    setEmail(partner.email || '');
    setAddress(partner.address || '');
    setDescription(partner.description || '');
    setIsActive(partner.isActive !== false);
    setShowFormModal(true);
  };

  const handleSavePartner = async (e) => {
    e.preventDefault();
    if (!partnerCode.trim()) {
      toast.warning('Vui lòng điền mã đối tác.');
      return;
    }
    if (!partnerName.trim()) {
      toast.warning('Vui lòng điền tên đối tác.');
      return;
    }

    const payload = {
      code: partnerCode.trim(),
      name: partnerName.trim(),
      taxCode: taxCode.trim(),
      phone: phone.trim(),
      email: email.trim(),
      address: address.trim(),
      description: description.trim(),
      isActive
    };

    try {
      setLoading(true);
      if (editingPartner) {
        await partnerApi.update(editingPartner.id, payload);
        toast.success('Cập nhật đối tác thành công!');
      } else {
        await partnerApi.create(payload);
        toast.success('Tạo mới đối tác thành công!');
      }
      setShowFormModal(false);
      fetchPartners();
    } catch (error) {
      console.error(error);
      const errMsg = error.response?.data?.message || 'Có lỗi xảy ra khi lưu đối tác.';
      toast.error(errMsg);
    } finally {
      setLoading(false);
    }
  };

  const handleDeletePartner = async (id) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa đối tác này không?')) {
      return;
    }
    try {
      setLoading(true);
      await partnerApi.delete(id);
      toast.success('Xóa đối tác thành công!');
      fetchPartners();
    } catch (error) {
      console.error(error);
      toast.error('Lỗi khi xóa đối tác.');
    } finally {
      setLoading(false);
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="min-h-screen bg-slate-50 text-slate-800 p-6 font-sans">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex flex-col md:flex-row md:items-center md:justify-between mb-8 pb-5 border-b border-slate-200">
          <div>
            <h1 className="text-3xl font-bold tracking-tight text-slate-900 font-display">
              Quản Lý Đối Tác & Nhà Cung Cấp
            </h1>
            <p className="text-slate-500 mt-1 text-sm md:text-base">
              Quản lý danh sách đối tác cung cấp dịch vụ, thiết bị công nghệ thông tin và thông tin liên hệ.
            </p>
          </div>
          <div className="mt-4 md:mt-0 flex gap-2.5">
            <button
              onClick={fetchPartners}
              className="px-4 py-2 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 font-medium rounded-custom text-sm shadow-sm transition-colors flex items-center gap-1.5 cursor-pointer"
            >
              Tải lại
            </button>
            <Button onClick={openCreateModal} className="text-sm px-5 py-2 shadow-sm">
              <svg className="w-4 h-4 mr-1.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              Thêm đối tác mới
            </Button>
          </div>
        </div>

        {/* Filters/Search */}
        <div className="bg-white p-4 rounded-xl border border-slate-200/80 shadow-sm mb-6 flex flex-col md:flex-row items-center justify-between gap-4">
          <div className="w-full md:w-96 relative">
            <input
              type="text"
              placeholder="Tìm theo tên, mã hoặc mã số thuế đối tác..."
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
            Tổng cộng: {totalCount} đối tác
          </div>
        </div>

        {/* Loading Spinner */}
        {loading && (
          <div className="w-full bg-blue-50 border border-blue-100 rounded-lg p-3 text-blue-700 mb-6 flex items-center gap-2.5 text-sm animate-pulse">
            <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
            </svg>
            <span>Đang tải thông tin đối tác...</span>
          </div>
        )}

        {/* Table List */}
        <div className="bg-white rounded-xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-100 bg-slate-100/40 text-slate-500 font-semibold text-xs tracking-wider uppercase">
                  <th className="p-4 pl-6">Mã Đối Tác</th>
                  <th className="p-4">Tên Đối Tác</th>
                  <th className="p-4">Mã Số Thuế</th>
                  <th className="p-4">Điện thoại</th>
                  <th className="p-4">Email</th>
                  <th className="p-4">Địa chỉ</th>
                  <th className="p-4 text-center">Trạng Thái</th>
                  <th className="p-4 text-center">Thao Tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-sm">
                {partners.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="p-12 text-center text-slate-400">
                      Không tìm thấy dữ liệu đối tác nào.
                    </td>
                  </tr>
                ) : (
                  partners.map((p) => (
                    <tr key={p.id} className="hover:bg-slate-50/40 transition-colors">
                      <td className="p-4 pl-6 font-mono font-bold text-xs text-primary">
                        {p.code}
                      </td>
                      <td className="p-4 font-semibold text-slate-900 max-w-xs truncate" title={p.name}>
                        {p.name}
                      </td>
                      <td className="p-4 text-slate-600 font-mono text-xs">
                        {p.taxCode || '-'}
                      </td>
                      <td className="p-4 text-slate-600">
                        {p.phone || '-'}
                      </td>
                      <td className="p-4 text-slate-600 max-w-xs truncate" title={p.email}>
                        {p.email || '-'}
                      </td>
                      <td className="p-4 text-slate-500 max-w-xs truncate" title={p.address}>
                        {p.address || '-'}
                      </td>
                      <td className="p-4 text-center">
                        <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-3xs font-bold ${
                          p.isActive !== false
                            ? 'bg-emerald-50 text-emerald-700'
                            : 'bg-slate-100 text-slate-500'
                        }`}>
                          {p.isActive !== false ? 'Active' : 'Inactive'}
                        </span>
                      </td>
                      <td className="p-4 text-center">
                        <div className="flex items-center justify-center gap-1.5">
                          <button
                            onClick={() => openEditModal(p)}
                            className="p-1.5 border border-slate-200 rounded-lg hover:bg-slate-100 text-slate-600 hover:text-slate-900 transition-colors cursor-pointer"
                            title="Sửa đối tác"
                          >
                            <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                            </svg>
                          </button>
                          <button
                            onClick={() => handleDeletePartner(p.id)}
                            className="p-1.5 border border-red-100 hover:border-red-200 rounded-lg hover:bg-red-50 text-red-500 hover:text-red-700 transition-colors cursor-pointer"
                            title="Xóa đối tác"
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
            <div className="p-5 border-b border-slate-100 bg-slate-50 flex items-center justify-between">
              <h2 className="text-lg font-bold font-display text-slate-950">
                {editingPartner ? 'Cập Nhật Thông Tin Đối Tác' : 'Thêm Đối Tác Mới'}
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

            <form onSubmit={handleSavePartner} className="p-6 space-y-4 max-h-[70vh] overflow-y-auto">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Mã Đối Tác
                  </label>
                  <input
                    type="text"
                    value={partnerCode}
                    onChange={(e) => setPartnerCode(e.target.value)}
                    disabled={!!editingPartner}
                    placeholder="Ví dụ: DT-2026-001"
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary disabled:bg-slate-50 disabled:text-slate-400"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Tên Đối Tác / Nhà Cung Cấp
                  </label>
                  <input
                    type="text"
                    value={partnerName}
                    onChange={(e) => setPartnerName(e.target.value)}
                    placeholder="Ví dụ: Công ty Cổ phần Công nghệ FPT"
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Mã Số Thuế
                  </label>
                  <input
                    type="text"
                    value={taxCode}
                    onChange={(e) => setTaxCode(e.target.value)}
                    placeholder="Nhập mã số thuế doanh nghiệp..."
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary font-mono"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Số Điện Thoại
                  </label>
                  <input
                    type="text"
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
                    placeholder="Nhập số điện thoại liên hệ..."
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Địa Chỉ Email
                  </label>
                  <input
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    placeholder="partner@company.com"
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Địa Chỉ Văn Phòng
                  </label>
                  <input
                    type="text"
                    value={address}
                    onChange={(e) => setAddress(e.target.value)}
                    placeholder="Ví dụ: Tòa nhà FPT, Cầu Giấy, Hà Nội"
                    className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                  Mô Tả / Ghi Chú
                </label>
                <textarea
                  rows={3}
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Nhập thêm ghi chú hoặc thế mạnh sản phẩm của đối tác này..."
                  className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary resize-none"
                />
              </div>

              <div className="flex items-center gap-2 pt-2">
                <input
                  type="checkbox"
                  id="isActive"
                  checked={isActive}
                  onChange={(e) => setIsActive(e.target.checked)}
                  className="w-4 h-4 rounded text-primary border-slate-300 focus:ring-primary/20 accent-primary"
                />
                <label htmlFor="isActive" className="text-xs font-semibold text-slate-500 uppercase tracking-wider select-none">
                  Kích hoạt đối tác (Active)
                </label>
              </div>

              <div className="flex justify-end gap-2.5 pt-4 border-t border-slate-100 mt-6">
                <button
                  type="button"
                  onClick={() => setShowFormModal(false)}
                  className="px-4 py-2 border border-slate-200 text-slate-700 font-semibold rounded-xl text-sm hover:bg-slate-50 transition-colors cursor-pointer"
                >
                  Hủy bỏ
                </button>
                <Button type="submit" className="text-sm px-6">
                  {editingPartner ? 'Lưu cập nhật' : 'Tạo mới'}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
