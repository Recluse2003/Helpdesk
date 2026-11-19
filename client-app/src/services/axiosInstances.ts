import axios from "axios";

// Authentication requests (login, logout, refresh).
export const authApi = axios.create({
  baseURL: "/api",
  withCredentials: true,
});

// Normal application API requests.
export const api = axios.create({
  baseURL: "/api",
  withCredentials: true,
});
