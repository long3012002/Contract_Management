import { Navigate, Outlet } from "react-router-dom";
import { useAuthStore } from "../auth/store/authStore";

function CheckAuth() {
  const { isAuthenticated } = useAuthStore();
  return isAuthenticated ? <Outlet /> : <Navigate to="/login" replace />;
}

export default CheckAuth