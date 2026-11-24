import { useState, useCallback } from "react";
import { parseApiError } from "../utils/apiErrorUtils";

export function useApiFormErrors() {
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  // Parses an unknown caught error and populates general + field errors
  const handleApiError = useCallback((error: unknown) => {
    const parsed = parseApiError(error);
    setGeneralError(parsed.generalError);
    setFieldErrors(parsed.fieldErrors);
  }, []);

  // Clears a single field's error when the user starts typing again
  const clearFieldError = useCallback((fieldName: string) => {
    const key = fieldName.toLowerCase();
    setFieldErrors((prev) => {
      if (!prev[key]) return prev;
      const updated = { ...prev };
      delete updated[key];
      return updated;
    });
  }, []);

  // Resets everything
  const clearAllErrors = useCallback(() => {
    setGeneralError(null);
    setFieldErrors({});
  }, []);

  return {
    generalError,
    fieldErrors,
    handleApiError,
    clearFieldError,
    clearAllErrors,
  };
}
