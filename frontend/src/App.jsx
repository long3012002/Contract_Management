import { Route, Routes, Navigate } from "react-router-dom";
import { lazy, Suspense } from "react";
import { AppLayout } from "./components/layout/AppLayout";
import { useAuthInit } from "./features/auth/hooks/useAuthInit";

// Lazy load route components for bundle size optimization (bundle-dynamic-imports)
const Login = lazy(() => import("./pages/Login"));
const Dashboard = lazy(() => import("./pages/Dashboard"));
const ImplProjectsPage = lazy(() => import("./features/implementation-projects/components/ImplProjectsPage"));
const ImplProjectDetail = lazy(() => import("./features/implementation-projects/components/ImplProjectDetail"));
const BidPackagesPage = lazy(() => import("./features/bid-packages/components/BidPackagesPage"));
const ContractorsPage = lazy(() => import("./features/contractors/components/ContractorsPage"));
const ContractsPage = lazy(() => import("./features/contracts/components/ContractsPage"));
const ContractDetail = lazy(() => import("./features/contracts/components/ContractDetail"));
const PermissionsPage = lazy(() => import("./features/permissions/components/PermissionsPage"));
const UsersPage = lazy(() => import("./features/users/components/UsersPage"));
const AuditLogsPage = lazy(() => import("./features/audit-logs"));
const SourceProjectsPage = lazy(() => import("./features/source-projects/components/SourceProjectsPage"));


function App() {
  const { isInitializing } = useAuthInit();

  if (isInitializing) {
    return null;
  }

  return (
    <Suspense fallback={null}>
      <Routes>
        <Route path="/login" element={<Login />} />
        
        {/* App Routes wrapped in Layout */}
        <Route element={<AppLayout />}>
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/source-projects" element={<SourceProjectsPage />} />
          <Route path="/projects" element={<ImplProjectsPage />} />
          <Route path="/projects/:id" element={<ImplProjectDetail />} />
          <Route path="/bid-packages" element={<BidPackagesPage />} />
          <Route path="/contractors" element={<ContractorsPage />} />
          <Route path="/contracts" element={<ContractsPage />} />
          <Route path="/contracts/:id" element={<ContractDetail />} />
          {/* Fallback for other mock routes */}
          <Route path="/tasks" element={<div className="p-6">Đang phát triển: Công việc</div>} />
          <Route path="/team" element={<UsersPage />} />
          <Route path="/permissions" element={<PermissionsPage />} />
          <Route path="/audit-logs" element={<AuditLogsPage />} />
          <Route path="/settings" element={<div className="p-6">Đang phát triển: Cài đặt</div>} />
        </Route>

        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </Suspense>
  );
}

export default App;