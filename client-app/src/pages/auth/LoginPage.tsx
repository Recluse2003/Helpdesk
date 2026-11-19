import React, { useState } from "react";
import { useAuth } from "../../context/AuthContext";
import { useApiFormErrors } from "../../hooks/useApiFormErrors";
import { FormInput } from "../../components/FormInput/FormInput";
import { useNavigate, useSearchParams } from "react-router";
import { Link } from "react-router";
import { getAuthenticatedUserPath } from "../../utils/authUtils";
import styles from "./Auth.module.css";

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const [email, setEmail] = useState<string>("");
  const [password, setPassword] = useState<string>("");
  const [isLoading, setIsLoading] = useState<boolean>(false);

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

    if (searchParams.has("verified")) {
      setSearchParams({}, { replace: true });
    }

    setIsLoading(true);

    try {
      const loggedInUser = await login(email, password);

      navigate(getAuthenticatedUserPath(loggedInUser.roles));
    } catch (err: unknown) {
      handleApiError(err);
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <>
      <h2>Log In</h2>
      <hr />
      <form onSubmit={handleSubmit} noValidate>
        {searchParams.get("verified") === "true" && (
          <div role="status" className={styles.success}>
            Account email has been confirmed. You may now log in!
          </div>
        )}

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
          autoComplete="username"
        />

        <FormInput
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
          autoComplete="current-password"
        />

        <Link to="/auth/forgot-password">Forgot your password?</Link>

        <button type="submit" disabled={isLoading}>
          {isLoading ? "Logging in..." : "Log In"}
        </button>

        <p>
          Do not have an account? <br />
          <Link to="/auth/sign-up">Sign up!</Link>
        </p>
      </form>
    </>
  );
}
