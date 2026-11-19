import { Routes, Route, Navigate } from "react-router";
import "./App.css";
import LoginPage from "./pages/auth/LoginPage";
import SignUpPage from "./pages/auth/SignUpPage";
import ForgotPasswordPage from "./pages/auth/ForgotPasswordPage";
import ConfirmEmailPage from "./pages/auth/ConfirmEmailPage";
import ResetPasswordPage from "./pages/auth/ResetPasswordPage";
import MyTicketsPage from "./pages/MyTickets";
import PrivateRoutes from "./components/PrivateRoutes";
import AuthLayout from "./pages/auth/AuthLayout";
import MainLayout from "./pages/MainLayout";
import { Role } from "./types/auth";

function App() {
  return (
    <Routes>
      <Route path="/unauthorized" element="" />

      <Route element={<AuthLayout />}>
        <Route path="/auth/login" element={<LoginPage />} />
        <Route path="/auth/sign-up" element={<SignUpPage />} />
        <Route path="/auth/confirm-email" element={<ConfirmEmailPage />} />
        <Route path="/auth/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/auth/reset-password" element={<ResetPasswordPage />} />
      </Route>

      <Route element={<MainLayout />}>
        <Route element={<PrivateRoutes allowedRoles={[Role.CUSTOMER]} />}>
          <Route path="/my-tickets" element={<MyTicketsPage />} />
        </Route>

        <Route
          element={
            <PrivateRoutes
              allowedRoles={[Role.ADMINISTRATOR, Role.SUPPORTAGENT]}
            />
          }
        ></Route>

        <Route
          element={<PrivateRoutes allowedRoles={[Role.ADMINISTRATOR]} />}
        ></Route>
      </Route>

      <Route path="/" element={<Navigate to="/auth/login" replace />} />
      <Route path="*" element="" />
    </Routes>
  );
}

export default App;
