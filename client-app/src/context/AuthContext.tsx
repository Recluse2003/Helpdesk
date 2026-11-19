import {
  createContext,
  useContext,
  useState,
  useEffect,
  useMemo,
  useCallback,
  type ReactNode,
} from "react";

import { setAccessToken, subscribeToToken } from "../services/tokenStore";
import { api } from "../services/api";
import { refreshAccessToken } from "../services/authService";
import { Role } from "../types/auth";
import { loginUser, logoutUser } from "../services/authService";

export interface User {
  id: string;
  email: string;
  displayName: string;
  roles: Role[];
}

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<User>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<User>;
}

const AuthContext = createContext<AuthContextType | null>(null);

export default function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const fetchCurrentUser = useCallback(async (): Promise<User> => {
    const { data } = await api.get<User>("/auth/me");

    setUser(data);

    return data;
  }, []);

  useEffect(() => {
    return subscribeToToken((token) => {
      if (!token) {
        setUser(null);
      }
    });
  }, []);

  useEffect(() => {
    async function restoreSession() {
      try {
        // Calls /api/auth/refresh endpoint
        await refreshAccessToken();

        // Fetch the user profile using the new access token
        const meResponse = await api.get<User>("/auth/me");
        setUser(meResponse.data);
      } catch {
        setAccessToken(null);
        setUser(null);
      } finally {
        setIsLoading(false);
      }
    }

    restoreSession();
  }, []);

  const login = useCallback(
    async (email: string, password: string) => {
      await loginUser({ email, password });

      return fetchCurrentUser();
    },
    [fetchCurrentUser],
  );

  const logout = useCallback(async () => {
    try {
      await logoutUser();
    } finally {
      setAccessToken(null);
      setUser(null);
    }
  }, []);

  const value = useMemo(
    () => ({
      user,
      isAuthenticated: Boolean(user),
      isLoading,
      login,
      logout,
      refreshUser: fetchCurrentUser,
    }),
    [user, isLoading, login, logout, fetchCurrentUser],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }

  return context;
}
