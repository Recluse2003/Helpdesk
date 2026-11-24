import React, { useEffect, useState, useRef } from "react";
import { useApiFormErrors } from "../../hooks/useApiFormErrors";
import { AuthFormInput } from "./AuthFormInput/AuthFormInput";
import { api } from "../../services/api";
import styles from "./Auth.module.css";
import { Link, useSearchParams, useNavigate } from "react-router";
import { confirmEmail } from "../../services/authService";

export default function ConfirmEmailPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const userId = searchParams.get("userId");
  const code = searchParams.get("code");

  const hasCalledApi = useRef(false);

  const [isVerifying, setIsVerifying] = useState<boolean>(
    Boolean(userId && code),
  );
  const [email, setEmail] = useState<string>("");
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isSuccess, setIsSuccess] = useState<boolean>(false);

  const {
    generalError,
    fieldErrors,
    handleApiError,
    clearFieldError,
    clearAllErrors,
  } = useApiFormErrors();

  useEffect(() => {
    if (!userId || !code || hasCalledApi.current) return;

    hasCalledApi.current = true;

    const confirmEmailAddress = async () => {
      try {
        await confirmEmail({ userId, code });

        navigate("/auth/login?verified=true", { replace: true });
      } catch (err: unknown) {
        handleApiError(err);

        setIsVerifying(false);
      }
    };

    confirmEmailAddress();
  }, [userId, code]);

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    clearAllErrors();

    setIsLoading(true);

    try {
      await api.post("/auth/resend-confirmation-email", {
        email: email,
      });

      setIsSuccess(true);
    } catch (err: unknown) {
      handleApiError(err);
    } finally {
      setIsLoading(false);
    }
  }

  if (isVerifying) {
    return (
      <>
        <h2>Confirming your email...</h2>
        <hr />
        <p>Please wait while we verify your email address.</p>
      </>
    );
  }

  return (
    <>
      <h2>Invalid or expired email confirmation link</h2>
      <hr />
      <p>
        Input the email address of the account you are attempting to confirm to
        have a new confirmation email sent to you!
      </p>

      {isSuccess ? (
        <div role="status" className={styles.success}>
          <p>
            If an account exists for <strong>{email}</strong>, a new
            confirmation link has been sent. Please check your inbox.
          </p>
          <p>
            <Link to="/login">Return to Log in</Link>
          </p>
        </div>
      ) : (
        <form onSubmit={handleSubmit} noValidate>
          {generalError && (
            <div role="alert" className={styles.error}>
              {generalError}
            </div>
          )}

          <AuthFormInput
            id="email"
            label="Email"
            type="email"
            value={email}
            onChange={(e) => {
              setEmail(e.target.value);
              clearFieldError("email");
            }}
            disabled={isLoading}
            errors={fieldErrors.email}
            autoComplete="email"
          />

          <button type="submit" disabled={isLoading}>
            {isLoading ? "Submitting email..." : "Submit Email"}
          </button>

          <p>
            Account has already been confirmed?
            <br />
            <Link to="/auth/login">Log in!</Link>
          </p>
        </form>
      )}
    </>
  );
}
