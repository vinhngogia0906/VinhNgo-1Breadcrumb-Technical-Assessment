import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { authApi } from '../api/auth';
import { getStoredToken, setStoredToken, setUnauthorizedHandler } from '../api/client';
import type { AuthUser } from '../api/types';

const USER_KEY = 'library.user';

type AuthContextValue = {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isInitialized: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, displayName: string, password: string) => Promise<void>;
  logout: () => void;
};

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStoredUser(): AuthUser | null {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(readStoredUser);
  const [isInitialized, setIsInitialized] = useState(false);

  const logout = useCallback(() => {
    setStoredToken(null);
    localStorage.removeItem(USER_KEY);
    setUser(null);
  }, []);

  useEffect(() => {
    setUnauthorizedHandler(logout);
    setIsInitialized(true);
    return () => setUnauthorizedHandler(null);
  }, [logout]);

  const persist = useCallback((token: string, nextUser: AuthUser) => {
    setStoredToken(token);
    localStorage.setItem(USER_KEY, JSON.stringify(nextUser));
    setUser(nextUser);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const res = await authApi.login(email, password);
    persist(res.token, res.user);
  }, [persist]);

  const register = useCallback(async (email: string, displayName: string, password: string) => {
    const res = await authApi.register(email, displayName, password);
    persist(res.token, res.user);
  }, [persist]);

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isAuthenticated: user !== null && getStoredToken() !== null,
    isInitialized,
    login,
    register,
    logout,
  }), [user, isInitialized, login, register, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
