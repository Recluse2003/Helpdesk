import { useAuth } from "../context/AuthContext";
import { Outlet, Navigate } from "react-router";
import { Role } from "../types/auth";

interface PrivateRoutesProps {
  allowedRoles: Role[];
}

export default function PrivateRoutes({ allowedRoles }: PrivateRoutesProps) {
  const { isAuthenticated, isLoading, user } = useAuth();

  if (isLoading) {
    return <div>Loading...</div>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  const hasRequiredRole = user?.roles?.some((role) =>
    allowedRoles.includes(role),
  );

  return hasRequiredRole ? <Outlet /> : <Navigate to="/unauthorized" replace />;
}
