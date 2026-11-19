import { authApi } from "./axiosInstances";
import { setAccessToken } from "./tokenStore";

interface RefreshResponse {
  accessToken: string;
}

interface LoginRequest {
  email: string;
  password: string;
}

interface LoginResponse {
  accessToken: string;
}

interface ConfirmEmailRequest {
  userId: string;
  code: string;
}

interface RegisterRequest {
  displayName: string;
  email: string;
  password: string;
}

interface ForgotPasswordRequest {
  email: string;
}

interface ResetPasswordRequest {
  userId: string;
  code: string;
  newPassword: string;
}

let refreshPromise: Promise<string> | null = null;

export async function loginUser(request: LoginRequest): Promise<LoginResponse> {
  const { data } = await authApi.post<LoginResponse>("/auth/login", request);

  setAccessToken(data.accessToken);

  return data;
}

export async function confirmEmail(
  request: ConfirmEmailRequest,
): Promise<void> {
  await authApi.post("/auth/confirm-email", null, {
    params: {
      userId: request.userId,
      code: request.code,
    },
  });
}

export async function logoutUser(): Promise<void> {
  await authApi.post("/auth/logout");
}

export async function registerUser(request: RegisterRequest): Promise<void> {
  await authApi.post("/auth/register", request);
}

export async function forgotPassword(
  request: ForgotPasswordRequest,
): Promise<void> {
  await authApi.post("/auth/forgot-password", request);
}

export async function resetPassword(
  request: ResetPasswordRequest,
): Promise<void> {
  await authApi.post("/auth/reset-password", request);
}

export async function refreshAccessToken(): Promise<string> {
  if (!refreshPromise) {
    refreshPromise = authApi
      .post<RefreshResponse>("/auth/refresh")
      .then((response) => {
        const newAccessToken = response.data.accessToken;
        setAccessToken(newAccessToken);
        return newAccessToken;
      })
      .finally(() => {
        refreshPromise = null;
      });
  }

  return refreshPromise;
}
