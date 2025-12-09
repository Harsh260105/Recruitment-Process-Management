import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "@/store";

export const PrivateRoute = () => {
  const isAuthenticated = useAuth((state) => state.auth.isAuthenticated);

  if (!isAuthenticated) {
    return <Navigate to="/auth/login" replace />;
  }

  return <Outlet />;
};
