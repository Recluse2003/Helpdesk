import React, { useState } from "react";
import { useApiFormErrors } from "../../hooks/useApiFormErrors";
import { FormInput } from "../../components/FormInput/FormInput";
import { Link } from "react-router";
import styles from "./Auth.module.css";
import { forgotPassword } from "../../services/authService";

export default function ForgotPasswordPage() {
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

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    clearAllErrors();

    setIsLoading(true);

    try {
      await forgotPassword({
        email: email,
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
        <h2>Check your email</h2>
        <hr />
        <div role="status">
          <p>
            We've sent a reset password link to <strong>{email}</strong>. Please
            click the link in that email to reset the password to you account.
          </p>
          <Link to="/login">Go to Log In</Link>
        </div>
      </>
    );
  }

  return (
    <>
      <h2>Forgot your password?</h2>
      <hr />
      <p>
        Enter the email you used to sign up and we will send you a link to set a
        new password
      </p>
      <form onSubmit={handleSubmit} noValidate>
        {generalError && (
          <div role="alert" className={styles.error}>
            {generalError}
          </div>
        )}

        <FormInput
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
          Know the password to your account?
          <br />
          <Link to="/auth/login">Log in!</Link>
        </p>
      </form>
    </>
  );
}
