import React from "react";
import styles from "./AuthFormInput.module.css";

interface FormInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label: string;
  id: string;
  errors?: string[];
}

export function AuthFormInput({
  label,
  id,
  errors,
  className,
  ...props
}: FormInputProps) {
  const hasErrors = Boolean(errors && errors.length > 0);

  return (
    <div className={styles.field}>
      <label htmlFor={id} className={styles.label}>
        {label}
      </label>
      <input
        id={id}
        className={`${styles.input} ${hasErrors ? styles.inputError : ""} ${className ?? ""}`}
        aria-invalid={hasErrors}
        aria-describedby={hasErrors ? `${id}-error` : undefined}
        {...props}
      />

      {hasErrors && (
        <div id={`${id}-error`} className={styles.errorList} role="alert">
          {errors!.map((msg, index) => (
            <p key={index} className={styles.errorText}>
              {msg}
            </p>
          ))}
        </div>
      )}
    </div>
  );
}
