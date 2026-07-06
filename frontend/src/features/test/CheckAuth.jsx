import { Navigate, Outlet } from "react-router-dom";

function CheckAuth() {
  const isAuth = false;
  return isAuth ? <Outlet /> : <Navigate to="/login" replace />;
}

export default CheckAuth