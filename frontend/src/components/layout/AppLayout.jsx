import { Outlet, Navigate } from "react-router-dom";
import { AppSidebar } from "./AppSidebar";
import { PageHeader } from "./PageHeader";
import { useAuthStore } from "../../features/auth/store/authStore";
import { Breadcrumb } from "../common/Breadcrumb";

export function AppLayout() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return (
    <div className="flex h-dvh bg-zinc-50 w-full overflow-hidden text-foreground">
      <AppSidebar />
      <div className="flex flex-col flex-1 overflow-hidden">
        <PageHeader />
        <main className="flex-1 overflow-y-scroll p-6 [scrollbar-gutter:stable]">
          <div className="max-w-[1400px] mx-auto">
            <Breadcrumb />
            {/* The routed content will appear here */}
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
