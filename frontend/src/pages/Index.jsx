import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../features/auth/store/authStore';
import { adminApi } from '../api/adminApi';
import { toast } from 'sonner';

export default function Index() {
  const navigate = useNavigate();
  const { user, logout } = useAuthStore();
  const [features, setFeatures] = useState([]);
  const [selectedFeature, setSelectedFeature] = useState(null);
  const [loading, setLoading] = useState(false);

  // Load features
  useEffect(() => {
    fetchFeatures();
  }, []);

  const fetchFeatures = async () => {
    try {
      setLoading(true);
      const data = await adminApi.features();
      setFeatures(data || []);
      if (data && data.length > 0) {
        setSelectedFeature(data[0]);
      }
    } catch (error) {
      console.error('Không thể tải các tính năng:', error);
      // Fallback fallback menu nếu API trả về lỗi phân quyền
      const fallbackFeatures = [
        {
          id: 'fb-perm',
          code: 'SYSTEM_PERMISSIONS',
          name: 'Quản lý phân quyền',
          description: 'Cấu hình nhóm vai trò, phân quyền chức năng và gán vai trò người dùng.',
          isActive: true,
          path: '/permissions'
        },
        {
          id: 'fb-contract',
          code: 'CONTRACT_MANAGEMENT',
          name: 'Quản lý hợp đồng',
          description: 'Xem danh sách, soạn thảo, ký duyệt và theo dõi phụ lục hợp đồng CNTT.',
          isActive: true,
          path: '/contracts'
        },
        {
          id: 'fb-report',
          code: 'REPORT_STATISTICS',
          name: 'Báo cáo thống kê',
          description: 'Biểu đồ trực quan hóa dữ liệu hợp đồng, doanh thu và tiến độ thực hiện.',
          isActive: true
        },
        {
          id: 'fb-test',
          code: 'SYSTEM_TEST',
          name: 'Kiểm thử hệ thống',
          description: 'Giao diện chạy thử các API và xác thực token JWT.',
          isActive: true,
          path: '/test'
        }
      ];
      setFeatures(fallbackFeatures);
      setSelectedFeature(fallbackFeatures[0]);
    } finally {
      setLoading(false);
    }
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
    toast.success('Đăng xuất thành công.');
  };

  const handleMenuClick = (feat) => {
    setSelectedFeature(feat);
    // Nếu tính năng có path được định nghĩa sẵn, có thể chuyển hướng nhanh hoặc hiển thị ở iframe/tab
  };

  return (
    <div className="flex h-screen bg-slate-100 text-slate-800 font-sans overflow-hidden">
      {/* SIDEBAR */}
      <aside className="w-80 bg-slate-900 text-slate-200 flex flex-col shadow-xl z-20 border-r border-slate-800">
        {/* Sidebar Header */}
        <div className="p-6 border-b border-slate-800 bg-slate-950 flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-primary flex items-center justify-center text-white font-bold text-lg shadow-md border border-white/10">
            CP
          </div>
          <div>
            <h1 className="font-bold text-md text-white font-display uppercase tracking-wider">
              Co-opBank Portal
            </h1>
            <p className="text-2xs text-slate-500 uppercase tracking-widest font-semibold mt-0.5">
              Hệ thống quản lý
            </p>
          </div>
        </div>

        {/* User Info Card */}
        <div className="p-4 mx-4 my-4 rounded-xl bg-slate-800/40 border border-slate-800/80 flex items-center gap-3">
          <div className="w-10 h-10 rounded-full bg-slate-800 border border-slate-700 flex items-center justify-center font-bold text-primary-foreground text-sm uppercase">
            {user?.username?.substring(0, 2) || 'US'}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-semibold text-white truncate">{user?.username || 'Người dùng'}</p>
            <p className="text-3xs text-slate-500 font-mono truncate">Token: {user?.accessToken ? 'Đã kích hoạt' : 'N/A'}</p>
          </div>
        </div>

        {/* Menu Navigation */}
        <div className="flex-1 overflow-y-auto px-3 space-y-1.5">
          <p className="px-3 text-3xs font-bold text-slate-500 uppercase tracking-widest mb-3">
            Danh mục tính năng
          </p>

          {loading ? (
            <div className="px-3 py-4 text-xs text-slate-400 animate-pulse">
              Đang tải danh mục...
            </div>
          ) : features.length === 0 ? (
            <div className="px-3 py-4 text-xs text-slate-500">
              Không có tính năng nào khả dụng.
            </div>
          ) : (
            features.map((feat) => {
              const isSelected = selectedFeature?.id === feat.id;
              return (
                <button
                  key={feat.id}
                  onClick={() => handleMenuClick(feat)}
                  className={`w-full flex items-center gap-3 px-4 py-3 text-sm font-medium rounded-xl transition-all duration-200 cursor-pointer ${
                    isSelected
                      ? 'bg-primary text-white shadow-md shadow-primary/20 scale-[1.02]'
                      : 'text-slate-400 hover:bg-slate-800/50 hover:text-slate-100'
                  }`}
                >
                  {/* Icon placeholder based on code */}
                  <svg
                    className={`w-5 h-5 transition-transform ${isSelected ? 'scale-110' : ''}`}
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d={
                        feat.code?.includes('PERM') || feat.code?.includes('ADMIN')
                          ? 'M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z'
                          : feat.code?.includes('CONTRACT')
                          ? 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z'
                          : feat.code?.includes('REPORT')
                          ? 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 002 2h2a2 2 0 002-2z'
                          : 'M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z'
                      }
                    />
                  </svg>
                  <span className="truncate">{feat.name}</span>
                </button>
              );
            })
          )}
        </div>

        {/* Sidebar Footer */}
        <div className="p-4 border-t border-slate-800 bg-slate-950/50">
          <button
            onClick={handleLogout}
            className="w-full flex items-center justify-center gap-2 py-2.5 bg-slate-800 hover:bg-red-950/40 border border-slate-700 hover:border-red-900/60 text-slate-300 hover:text-red-400 rounded-xl font-semibold text-xs shadow-inner transition-all duration-200 cursor-pointer"
          >
            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
            </svg>
            Đăng xuất hệ thống
          </button>
        </div>
      </aside>

      {/* WORKSPACE AREA */}
      <main className="flex-1 flex flex-col overflow-hidden bg-slate-50">
        {/* Header bar */}
        <header className="h-16 bg-white border-b border-slate-200 px-8 flex items-center justify-between shadow-sm z-10">
          <div>
            <span className="text-2xs font-semibold px-2 py-0.5 bg-slate-100 text-slate-500 rounded border border-slate-200/80 font-mono">
              FEATURE CODE: {selectedFeature?.code || 'N/A'}
            </span>
          </div>
          <div className="flex items-center gap-4">
            <span className="text-xs font-semibold text-slate-500">
              Trạng thái: 
            </span>
            <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-100 shadow-sm animate-pulse">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
              Đang hoạt động
            </span>
          </div>
        </header>

        {/* Content body */}
        <div className="flex-1 overflow-y-auto p-8 max-w-7xl w-full mx-auto">
          {selectedFeature ? (
            <div className="space-y-6">
              {/* Feature Title Card */}
              <div className="bg-white p-8 rounded-2xl border border-slate-200/80 shadow-sm relative overflow-hidden">
                <div className="absolute right-0 top-0 w-32 h-32 bg-primary/2 rounded-full -mr-8 -mt-8 pointer-events-none"></div>
                <h2 className="text-2xl font-bold text-slate-900 mb-2 font-display">
                  {selectedFeature.name}
                </h2>
                <p className="text-slate-500 text-sm max-w-2xl leading-relaxed">
                  {selectedFeature.description || 'Không có mô tả bổ sung cho phân hệ này.'}
                </p>

                {/* Quick Link/Navigate details */}
                {(selectedFeature.path || selectedFeature.code?.includes('PERM') || selectedFeature.code?.includes('TEST') || selectedFeature.code?.includes('CONTRACT')) && (
                  <div className="mt-6 pt-5 border-t border-slate-100 flex items-center gap-3">
                    <button
                      onClick={() => {
                        let targetPath = selectedFeature.path;
                        if (!targetPath) {
                          if (selectedFeature.code?.includes('PERM')) targetPath = '/permissions';
                          else if (selectedFeature.code?.includes('TEST')) targetPath = '/test';
                          else if (selectedFeature.code?.includes('CONTRACT')) targetPath = '/contracts';
                        }
                        if (targetPath) navigate(targetPath);
                      }}
                      className="px-5 py-2.5 bg-primary text-white font-semibold text-sm rounded-xl hover:bg-primary/95 shadow-md shadow-primary/10 transition-all duration-200 cursor-pointer flex items-center gap-1.5"
                    >
                      Mở giao diện phân hệ
                      <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M14 5l7 7m0 0l-7 7m7-7H3" />
                      </svg>
                    </button>
                  </div>
                )}
              </div>

              {/* Grid with statistics */}
              <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-sm flex items-center gap-4">
                  <div className="w-12 h-12 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center">
                    <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                  </div>
                  <div>
                    <p className="text-2xs font-semibold text-slate-400 uppercase tracking-wider">Tổng số lượt truy cập</p>
                    <p className="text-xl font-bold text-slate-900 mt-0.5">1,248 lượt</p>
                  </div>
                </div>

                <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-sm flex items-center gap-4">
                  <div className="w-12 h-12 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center">
                    <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                  </div>
                  <div>
                    <p className="text-2xs font-semibold text-slate-400 uppercase tracking-wider">Thời gian cập nhật</p>
                    <p className="text-xl font-bold text-slate-900 mt-0.5">Vừa xong</p>
                  </div>
                </div>

                <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-sm flex items-center gap-4">
                  <div className="w-12 h-12 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
                    <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                    </svg>
                  </div>
                  <div>
                    <p className="text-2xs font-semibold text-slate-400 uppercase tracking-wider">Mức độ an toàn</p>
                    <p className="text-xl font-bold text-slate-900 mt-0.5">SSL / AES-256</p>
                  </div>
                </div>
              </div>

              {/* Placeholder table/content panel */}
              <div className="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
                <div className="p-6 border-b border-slate-100 flex items-center justify-between">
                  <h3 className="font-bold text-slate-950 font-display">Nhật ký hoạt động gần đây</h3>
                  <button className="text-xs font-semibold text-primary hover:underline">Xem tất cả</button>
                </div>
                <div className="p-6">
                  <div className="space-y-4">
                    <div className="flex items-center justify-between text-sm py-2.5 border-b border-slate-100 last:border-0">
                      <div className="flex items-center gap-3">
                        <span className="w-2.5 h-2.5 rounded-full bg-blue-500"></span>
                        <span className="font-medium text-slate-800">Kiểm tra thông tin tài khoản đăng nhập</span>
                      </div>
                      <span className="text-slate-400 font-mono text-xs">Hôm nay - 14:15</span>
                    </div>
                    <div className="flex items-center justify-between text-sm py-2.5 border-b border-slate-100 last:border-0">
                      <div className="flex items-center gap-3">
                        <span className="w-2.5 h-2.5 rounded-full bg-emerald-500"></span>
                        <span className="font-medium text-slate-800">Cập nhật ma trận phân quyền hệ thống</span>
                      </div>
                      <span className="text-slate-400 font-mono text-xs">Hôm nay - 11:32</span>
                    </div>
                    <div className="flex items-center justify-between text-sm py-2.5 border-b border-slate-100 last:border-0">
                      <div className="flex items-center gap-3">
                        <span className="w-2.5 h-2.5 rounded-full bg-slate-300"></span>
                        <span className="font-medium text-slate-800">Đồng bộ dữ liệu tính năng hệ thống</span>
                      </div>
                      <span className="text-slate-400 font-mono text-xs">Hôm qua - 17:40</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          ) : (
            <div className="text-center py-20 text-slate-400">
              Vui lòng chọn một tính năng bên trái để xem nội dung công việc.
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
