import React, { useState, useEffect } from 'react';
import { toast } from 'sonner';
import { projectApi } from '../api/projectApi';
import Button from '../components/Button/Button';

export default function ProjectManagement() {
  const [projects, setProjects] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [loading, setLoading] = useState(false);

  // Form Modal States
  const [showFormModal, setShowFormModal] = useState(false);
  const [editingProject, setEditingProject] = useState(null);

  // Form Inputs
  const [projectCode, setProjectCode] = useState('');
  const [projectName, setProjectName] = useState('');
  const [description, setDescription] = useState('');
  const [totalBudget, setTotalBudget] = useState(0);
  const [status, setStatus] = useState('Planning');
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    fetchProjects();
  }, [page, search]);

  const fetchProjects = async () => {
    try {
      setLoading(true);
      const data = await projectApi.getAll(search, page, pageSize);
      setProjects(data.items || []);
      setTotalCount(data.totalCount || 0);
    } catch (error) {
      console.error(error);
      toast.error('Không thể kết nối đến máy chủ hoặc tải danh sách dự án.');
    } finally {
      setLoading(false);
    }
  };

  const openCreateModal = () => {
    setEditingProject(null);
    setProjectCode(`DA-${new Date().getFullYear()}-${Math.floor(100 + Math.random() * 900)}`);
    setProjectName('');
    setDescription('');
    setTotalBudget(0);
    setStatus('Planning');
    setIsActive(true);
    setShowFormModal(true);
  };

  const openEditModal = (project) => {
    setEditingProject(project);
    setProjectCode(project.code || '');
    setProjectName(project.name || '');
    setDescription(project.description || '');
    setTotalBudget(project.totalBudget || 0);
    setStatus(project.status || 'Planning');
    setIsActive(project.isActive !== false);
    setShowFormModal(true);
  };

  const handleSaveProject = async (e) => {
    e.preventDefault();
    if (!projectCode.trim()) {
      toast.warning('Vui lòng điền mã dự án.');
      return;
    }
    if (!projectName.trim()) {
      toast.warning('Vui lòng điền tên dự án.');
      return;
    }

    const payload = {
      code: projectCode.trim(),
      name: projectName.trim(),
      description: description.trim(),
      totalBudget: Number(totalBudget) || 0,
      status,
      isActive
    };

    try {
      setLoading(true);
      if (editingProject) {
        await projectApi.update(editingProject.id, payload);
        toast.success('Cập nhật dự án thành công!');
      } else {
        await projectApi.create(payload);
        toast.success('Tạo mới dự án thành công!');
      }
      setShowFormModal(false);
      fetchProjects();
    } catch (error) {
      console.error(error);
      const errMsg = error.response?.data?.message || 'Có lỗi xảy ra khi lưu dự án.';
      toast.error(errMsg);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteProject = async (id) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa dự án này không?')) {
      return;
    }
    try {
      setLoading(true);
      await projectApi.delete(id);
      toast.success('Xóa dự án thành công!');
      fetchProjects();
    } catch (error) {
      console.error(error);
      toast.error('Lỗi khi xóa dự án.');
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (val) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div className="min-h-screen bg-slate-50 text-slate-800 p-6 font-sans">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex flex-col md:flex-row md:items-center md:justify-between mb-8 pb-5 border-b border-slate-200">
          <div>
            <h1 className="text-3xl font-bold tracking-tight text-slate-900 font-display">
              Quản Lý Dự Án CNTT
            </h1>
            <p className="text-slate-500 mt-1 text-sm md:text-base">
              Quản lý các chương trình, dự án đầu tư công nghệ thông tin và ngân sách phát triển liên quan.
            </p>
          </div>
          <div className="mt-4 md:mt-0 flex gap-2.5">
            <button
              onClick={fetchProjects}
              className="px-4 py-2 bg-white border border-slate-200 hover:bg-slate-50 text-slate-700 font-medium rounded-custom text-sm shadow-sm transition-colors flex items-center gap-1.5 cursor-pointer"
            >
              Tải lại
            </button>
            <Button onClick={openCreateModal} className="text-sm px-5 py-2 shadow-sm">
              <svg className="w-4 h-4 mr-1.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
              Thêm dự án mới
            </Button>
          </div>
        </div>

        {/* Filters/Search */}
        <div className="bg-white p-4 rounded-xl border border-slate-200/80 shadow-sm mb-6 flex flex-col md:flex-row items-center justify-between gap-4">
          <div className="w-full md:w-96 relative">
            <input
              type="text"
              placeholder="Tìm kiếm mã hoặc tên dự án..."
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
            Tổng cộng: {totalCount} dự án
          </div>
        </div>

        {/* Loading Spinner */}
        {loading && (
          <div className="w-full bg-blue-50 border border-blue-100 rounded-lg p-3 text-blue-700 mb-6 flex items-center gap-2.5 text-sm animate-pulse">
            <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24" fill="none">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
            </svg>
            <span>Đang lấy thông tin dự án từ máy chủ...</span>
          </div>
        )}

        {/* Table List */}
        <div className="bg-white rounded-xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-100 bg-slate-100/40 text-slate-500 font-semibold text-xs tracking-wider uppercase">
                  <th className="p-4 pl-6">Mã Dự Án</th>
                  <th className="p-4">Tên Dự Án</th>
                  <th className="p-4">Mô Tả</th>
                  <th className="p-4 text-right">Tổng Ngân Sách</th>
                  <th className="p-4 text-center">Trạng Thái</th>
                  <th className="p-4 text-center">Kích Hoạt</th>
                  <th className="p-4 text-center">Thao Tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-sm">
                {projects.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="p-12 text-center text-slate-400">
                      Không tìm thấy dữ liệu dự án nào.
                    </td>
                  </tr>
                ) : (
                  projects.map((p) => (
                    <tr key={p.id} className="hover:bg-slate-50/40 transition-colors">
                      <td className="p-4 pl-6 font-mono font-bold text-xs text-primary">
                        {p.code}
                      </td>
                      <td className="p-4 font-semibold text-slate-900 max-w-xs truncate" title={p.name}>
                        {p.name}
                      </td>
                      <td className="p-4 text-slate-500 max-w-xs truncate" title={p.description}>
                        {p.description || '-'}
                      </td>
                      <td className="p-4 text-right font-mono font-semibold text-slate-900">
                        {formatCurrency(p.totalBudget)}
                      </td>
                      <td className="p-4 text-center">
                        <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-3xs font-bold uppercase tracking-wider border ${
                          p.status === 'Executing'
                            ? 'bg-blue-50 text-blue-700 border-blue-100'
                            : p.status === 'Completed'
                            ? 'bg-emerald-50 text-emerald-700 border-emerald-100'
                            : p.status === 'Suspended'
                            ? 'bg-red-50 text-red-700 border-red-100'
                            : 'bg-slate-100 text-slate-700 border-slate-200'
                        }`}>
                          {p.status}
                        </span>
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
                            title="Sửa dự án"
                          >
                            <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                            </svg>
                          </button>
                          <button
                            onClick={() => handleDeleteProject(p.id)}
                            className="p-1.5 border border-red-100 hover:border-red-200 rounded-lg hover:bg-red-50 text-red-500 hover:text-red-700 transition-colors cursor-pointer"
                            title="Xóa dự án"
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
                {editingProject ? 'Cập Nhật Thông Tin Dự Án' : 'Thêm Dự Án Mới'}
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

            <form onSubmit={handleSaveProject} className="p-6 space-y-4 max-h-[70vh] overflow-y-auto">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                    Mã Dự Án
                  </label>
                  <input
                    type="text"
                    value={projectCode}
                    onChange={(e) => setProjectCode(e.target.value)}
                    disabled={!!editingProject}
                    placeholder="Ví dụ: DA-2026-001"
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
                    <option value="Planning">Planning (Lên kế hoạch)</option>
                    <option value="Executing">Executing (Đang thực hiện)</option>
                    <option value="Completed">Completed (Đã hoàn thành)</option>
                    <option value="Suspended">Suspended (Tạm ngưng)</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                  Tên Dự Án
                </label>
                <input
                  type="text"
                  value={projectName}
                  onChange={(e) => setProjectName(e.target.value)}
                  placeholder="Ví dụ: Dự án nâng cấp hệ thống Core Banking Co-opBank"
                  className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                  Tổng Ngân Sách (VNĐ)
                </label>
                <input
                  type="number"
                  value={totalBudget}
                  onChange={(e) => setTotalBudget(Number(e.target.value))}
                  placeholder="0"
                  className="w-full px-3.5 py-2 border border-slate-200 rounded-custom text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary font-mono"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                  Mô Tả Dự Án
                </label>
                <textarea
                  rows={3}
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Nhập thông tin mô tả chi tiết dự án, mục tiêu và phạm vi..."
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
                  Kích hoạt dự án (Active)
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
                  {editingProject ? 'Lưu cập nhật' : 'Tạo mới'}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
