import React, { useState } from "react";
import { useApiFormErrors } from "../../hooks/useApiFormErrors";
import { FormInput } from "../../components/FormInput/FormInput";
import { Link } from "react-router";
import styles from "./Auth.module.css";
import { useSearchParams } from "react-router";
import { resetPassword } from "../../services/authService";

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams();

  const userId = searchParams.get("userId");
  const code = searchParams.get("code");

  const [newPassword, setNewPassword] = useState<string>("");
  const [confirmNewPassword, setConfirmNewPassword] = useState<string>("");
  const [confirmNewPasswordError, setConfirmNewPasswordError] = useState<
    string[]
  >([]);

  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isSuccess, setIsSuccess] = useState<boolean>(false);

  const {
    generalError,
    fieldErrors,
    handleApiError,
    clearFieldError,
    clearAllErrors,
  } = useApiFormErrors();

  if (!userId || !code) {
    return (
      <>
        <h2>Invalid or expired password reset link</h2>
        <hr />
        <p>Please request a new password reset link.</p>
        <Link to="/auth/forgot-password">Request new link</Link>
      </>
    );
  }

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    clearAllErrors();
    setConfirmNewPasswordError([]);

    if (!userId || !code) {
      handleApiError(new Error("Invalid password reset link."));
      return;
    }

    if (newPassword != confirmNewPassword) {
      setConfirmNewPasswordError(["The passwords do not match."]);
      return;
    }

    setIsLoading(true);

    try {
      await resetPassword({
        userId: userId,
        code: code,
        newPassword: newPassword,
      });

      setIsSuccess(true);
    } catch (err: unknown) {
      handleApiError(err);
    } finally {
      setIsLoading(false);
    }
  }

  if (isSuccess) {
    return (
      <>
        <h2>You have successfully reset your password</h2>
        <hr />
        <div role="status">
          <Link to="/auth/login">Log In</Link>
        </div>
      </>
    );
  }

  return (
    <>
      <h2>Reset Password</h2>
      <hr />
      <p>Enter the new password</p>
      <form onSubmit={handleSubmit} noValidate>
        {generalError && (
          <div role="alert" className={styles.error}>
            {generalError}
          </div>
        )}

        <FormInput
          id="password"
          label="New password"
          type="password"
          value={newPassword}
          onChange={(e) => {
            setNewPassword(e.target.value);
            clearFieldError("newPassword");
          }}
          disabled={isLoading}
          errors={fieldErrors.newPassword}
          autoComplete="new-password"
        />

        <FormInput
          id="confirmPassword"
          label="Confirm new password"
          type="password"
          value={confirmNewPassword}
          onChange={(e) => {
            setConfirmNewPassword(e.target.value);
            setConfirmNewPasswordError([]);
          }}
          disabled={isLoading}
          errors={confirmNewPasswordError}
          autoComplete="new-password"
        />

        <button type="submit" disabled={isLoading}>
          {isLoading ? "Submitting new password..." : "Submit New Password"}
        </button>

        <p>
          Already know the password to your account? <br />
          <Link to="/auth/login">Log In!</Link>
        </p>
      </form>
    </>
  );
}
