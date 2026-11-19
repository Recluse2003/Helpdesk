import styles from "./Auth.module.css";
import { Outlet } from "react-router";
import { useAuth } from "../../context/AuthContext";
import { Role } from "../../types/auth";
import { Navigate } from "react-router";

export default function AuthLayout() {
  const { isAuthenticated, isLoading, user } = useAuth();

  if (isLoading) {
    return <div>Checking your session...</div>;
  }

  if (isAuthenticated && user) {
    if (user.roles.includes(Role.ADMINISTRATOR)) {
      return <Navigate to="/" replace />;
    }

    if (user.roles.includes(Role.SUPPORTAGENT)) {
      return <Navigate to="/" replace />;
    }

    if (user.roles.includes(Role.CUSTOMER)) {
      return <Navigate to="/my-tickets" replace />;
    }

    return <Navigate to="/unauthorized" replace />;
  }

  return (
    <div className={styles.wrapper}>
      <div className={styles.card}>
        <Outlet />
      </div>
    </div>
  );
}
