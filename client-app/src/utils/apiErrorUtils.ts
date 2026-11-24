import axios from "axios";

export interface ApiProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export interface ParsedApiError {
  generalError: string | null;
  fieldErrors: Record<string, string[]>;
}

export function parseApiError(error: unknown): ParsedApiError {
  // Check if the error came from Axios with a server response
  if (axios.isAxiosError<ApiProblemDetails>(error) && error.response?.data) {
    const data = error.response.data;

    // Handles ValidationProblemDetails and ProblemDetails errors (e.g. InvalidCredentials, EmailNotConfirmed)
    if (data.errors) {
      const normalizedErrors: Record<string, string[]> = {};

      for (const [key, messages] of Object.entries(data.errors)) {
        normalizedErrors[key.toLowerCase()] = messages;
      }

      return {
        generalError: data.detail ?? null,
        fieldErrors: normalizedErrors,
      };
    }
  }

  // Deals with unexpected errors
  return {
    generalError: "Something went wrong. Please try again later.",
    fieldErrors: {},
  };
}
