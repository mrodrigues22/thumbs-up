import { create } from 'zustand';
import type { User } from '../shared/types';

interface AuthState {
  user: User | null;
  token: string | null;
  tokenExpiresAt: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  setAuth: (user: User, token: string, expiresAt: string) => void;
  clearAuth: () => void;
  loadAuth: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  token: null,
  tokenExpiresAt: null,
  isAuthenticated: false,
  isLoading: true,

  setAuth: (user, token, expiresAt) => {
    localStorage.setItem('authToken', token);
    localStorage.setItem('tokenExpiresAt', expiresAt);
    localStorage.setItem('user', JSON.stringify(user));
    set({ user, token, tokenExpiresAt: expiresAt, isAuthenticated: true, isLoading: false });
  },

  clearAuth: () => {
    localStorage.removeItem('authToken');
    localStorage.removeItem('tokenExpiresAt');
    localStorage.removeItem('user');
    set({ user: null, token: null, tokenExpiresAt: null, isAuthenticated: false, isLoading: false });
  },

  loadAuth: () => {
    const token = localStorage.getItem('authToken');
    const expiresAt = localStorage.getItem('tokenExpiresAt');
    const userStr = localStorage.getItem('user');
    
    if (token && userStr && expiresAt) {
      // Check if token is expired
      const expirationTime = new Date(expiresAt).getTime();
      const now = Date.now();
      
      if (now >= expirationTime) {
        // Token expired - clear everything
        localStorage.removeItem('authToken');
        localStorage.removeItem('tokenExpiresAt');
        localStorage.removeItem('user');
        set({ user: null, token: null, tokenExpiresAt: null, isAuthenticated: false, isLoading: false });
      } else {
        // Token still valid - restore auth
        const user = JSON.parse(userStr);
        set({ user, token, tokenExpiresAt: expiresAt, isAuthenticated: true, isLoading: false });
      }
    } else {
      // No stored auth
      set({ isLoading: false });
    }
  },
}));
