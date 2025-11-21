/**
 * API Configuration and Endpoints
 * Centralized API configuration following best practices
 */

export const API_CONFIG = {
  BASE_URL: import.meta.env.VITE_API_URL || 'http://localhost:5039/api',
  TIMEOUT: 30000, // 30 seconds
  HEADERS: {
    'Content-Type': 'application/json',
  },
} as const;

/**
 * API Endpoints
 * Centralized endpoint definitions for type safety and maintainability
 */
export const API_ENDPOINTS = {
  // Auth endpoints
  AUTH: {
    LOGIN: '/auth/login',
    REGISTER: '/auth/register',
    LOGOUT: '/auth/logout',
    REFRESH: '/auth/refresh',
    ME: '/auth/me',
  },
  
  // Submission endpoints
  SUBMISSIONS: {
    BASE: '/submission',
    BY_ID: (id: string) => `/submission/${id}`,
    LIST: '/submission',
    CREATE: '/submission',
    UPDATE: (id: string) => `/submission/${id}`,
    DELETE: (id: string) => `/submission/${id}`,
    EXPORT: (id: string) => `/submission/${id}/export`,
  },
  
  // Review endpoints
  REVIEWS: {
    BASE: '/review',
    VALIDATE: '/review/validate',
    BY_TOKEN: (token: string) => `/review/${token}`,
    SUBMIT: '/review/submit',
  },
  
  // Media endpoints
  MEDIA: {
    UPLOAD: '/media/upload',
    BY_ID: (id: string) => `/media/${id}`,
    DELETE: (id: string) => `/media/${id}`,
  },
} as const;

/**
 * Storage keys for localStorage
 */
export const STORAGE_KEYS = {
  AUTH_TOKEN: 'authToken',
  USER: 'user',
  THEME: 'theme',
  LANGUAGE: 'language',
} as const;

/**
 * Query keys for React Query
 * Organized by feature for better cache management
 */
export const QUERY_KEYS = {
  AUTH: {
    ME: ['auth', 'me'] as const,
  },
  SUBMISSIONS: {
    ALL: ['submissions'] as const,
    LIST: (filters?: unknown) => ['submissions', 'list', filters] as const,
    DETAIL: (id: string) => ['submissions', 'detail', id] as const,
    BY_STATUS: (status: number) => ['submissions', 'status', status] as const,
  },
  REVIEWS: {
    ALL: ['reviews'] as const,
    BY_TOKEN: (token: string) => ['reviews', 'token', token] as const,
  },
} as const;

/**
 * Mutation keys for React Query
 */
export const MUTATION_KEYS = {
  AUTH: {
    LOGIN: 'auth:login',
    REGISTER: 'auth:register',
    LOGOUT: 'auth:logout',
  },
  SUBMISSIONS: {
    CREATE: 'submissions:create',
    UPDATE: 'submissions:update',
    DELETE: 'submissions:delete',
  },
  REVIEWS: {
    SUBMIT: 'reviews:submit',
    VALIDATE: 'reviews:validate',
  },
} as const;
