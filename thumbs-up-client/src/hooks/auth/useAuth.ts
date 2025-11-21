/**
 * useAuth Hook
 * Provides convenient access to auth store and operations
 */

import { useCallback } from 'react';
import { useAuthStore } from '../../stores/authStore';
import { authService } from '../../services/authService';
import type { User, LoginRequest, RegisterRequest } from '../../shared/types';

interface UseAuthReturn {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, firstName?: string, lastName?: string, companyName?: string) => Promise<void>;
  logout: () => void;
  hasRole: (role: string) => boolean;
}

/**
 * Hook to access authentication state and operations
 * Wraps zustand store for cleaner component usage
 */
export const useAuth = (): UseAuthReturn => {
  const {
    user,
    isAuthenticated,
    isLoading,
    setAuth,
    clearAuth,
  } = useAuthStore();

  /**
   * Login user
   */
  const login = useCallback(async (email: string, password: string): Promise<void> => {
    const loginData: LoginRequest = { email, password };
    const response = await authService.login(loginData);
    setAuth(
      { email: response.email, firstName: response.firstName, lastName: response.lastName, companyName: response.companyName },
      response.token,
      response.expiresAt
    );
  }, [setAuth]);

  /**
   * Register new user
   */
  const register = useCallback(async (
    email: string,
    password: string,
    firstName?: string,
    lastName?: string,
    companyName?: string
  ): Promise<void> => {
    const registerData: RegisterRequest = { email, password, firstName, lastName, companyName };
    const response = await authService.register(registerData);
    setAuth(
      { email: response.email, firstName: response.firstName, lastName: response.lastName, companyName: response.companyName },
      response.token,
      response.expiresAt
    );
  }, [setAuth]);

  /**
   * Check if user has a specific role
   * Note: Role checking is not yet implemented in the backend
   */
  const hasRole = (_role: string): boolean => {
    if (!user) return false;
    // TODO: Implement role checking when backend supports it
    return true;
  };

  /**
   * Logout wrapper
   */
  const logout = (): void => {
    clearAuth();
  };

  return {
    user,
    isAuthenticated,
    isLoading,
    login,
    register,
    logout,
    hasRole,
  };
};
