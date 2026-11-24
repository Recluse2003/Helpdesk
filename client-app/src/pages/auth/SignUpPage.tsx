import React, { useState } from "react";
import { useApiFormErrors } from "../../hooks/useApiFormErrors";
import { AuthFormInput } from "./AuthFormInput/AuthFormInput";
import { registerUser } from "../../services/authService";
import { Link } from "react-router";
import styles from "./Auth.module.css";

export default function SignUpPage() {
  const [email, setEmail] = useState<string>("");
  const [displayName, setDisplayName] = useState<string>("");
  const [password, setPassword] = useState<string>("");
  const [confirmPassword, setConfirmPassword] = useState<string>("");
  const [confirmPasswordError, setConfirmPasswordError] = useState<string[]>(
    [],
  );

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
    setConfirmPasswordError([]);

    if (password != confirmPassword) {
      setConfirmPasswordError(["The passwords do not match."]);
      return;
    }

    setIsLoading(true);

    try {
      await registerUser({
        displayName: displayName,
        email: email,
        password: password,
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
            We've sent a verification link to <strong>{email}</strong>. Please
            click the link in that email to activate your account before logging
            in.
          </p>
          <Link to="/auth/login">Go to Log In</Link>
        </div>
      </>
    );
  }

  return (
    <>
      <h2>Sign Up</h2>
      <hr />
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
          autoComplete="username"
        />

        <AuthFormInput
          id="displayName"
          label="Display Name"
          type="text"
          value={displayName}
          onChange={(e) => {
            setDisplayName(e.target.value);
            clearFieldError("displayName");
          }}
          disabled={isLoading}
          errors={fieldErrors.email}
        />

        <AuthFormInput
          id="password"
          label="Password"
          type="password"
          value={password}
          onChange={(e) => {
            setPassword(e.target.value);
            clearFieldError("password");
          }}
          disabled={isLoading}
          errors={fieldErrors.password}
          autoComplete="new-password"
        />

        <AuthFormInput
          id="confirmPassword"
          label="Confirm password"
          type="password"
          value={confirmPassword}
          onChange={(e) => {
            setConfirmPassword(e.target.value);
            setConfirmPasswordError([]);
          }}
          disabled={isLoading}
          errors={confirmPasswordError}
          autoComplete="new-password"
        />

        <button type="submit" disabled={isLoading}>
          {isLoading ? "Signing up..." : "Sign Up"}
        </button>

        <p>
          Already have an account?
          <br />
          <Link to="/auth/login">Log in!</Link>
        </p>
      </form>
    </>
  );
}
