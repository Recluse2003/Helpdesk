import type { InternalAxiosRequestConfig } from "axios";

import { api } from "./axiosInstances";
import { getAccessToken, setAccessToken } from "./tokenStore";
import { refreshAccessToken } from "./authService";

type RetryRequestConfig = InternalAxiosRequestConfig & {
  _retry?: boolean;
};

// Attach the current access token to normal API requests.
api.interceptors.request.use((config) => {
  const token = getAccessToken();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

// Handle expired access tokens.
api.interceptors.response.use(
  (response) => response,

  async (error) => {
    const originalRequest = error.config as RetryRequestConfig | undefined;

    if (
      error.response?.status !== 401 ||
      !originalRequest ||
      originalRequest._retry
    ) {
      return Promise.reject(error);
    }

    // Prevent this request from refreshing more than once.
    originalRequest._retry = true;

    try {
      // Refresh the access token.
      const newAccessToken = await refreshAccessToken();

      // Retry the original request with the new token.
      originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;

      return api(originalRequest);
    } catch (refreshError) {
      setAccessToken(null);

      return Promise.reject(refreshError);
    }
  },
);

export { api };
