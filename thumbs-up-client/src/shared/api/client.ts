/**
 * API Client
 * Axios instance with interceptors for authentication and error handling
 */

import axios, { AxiosError } from 'axios';
import type { AxiosRequestConfig, AxiosResponse } from 'axios';
import { API_CONFIG, STORAGE_KEYS } from './config';
import type { ApiError } from '../types';

/**
 * Create axios instance with default configuration
 */
export const apiClient = axios.create({
  baseURL: API_CONFIG.BASE_URL,
  timeout: API_CONFIG.TIMEOUT,
  headers: API_CONFIG.HEADERS,
});

/**
 * Request interceptor - Add auth token to requests
 */
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem(STORAGE_KEYS.AUTH_TOKEN);
    
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  }
);

/**
 * Response interceptor - Handle common errors
 */
apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    return response;
  },
  (error: AxiosError<ApiError>) => {
    // Handle 401 Unauthorized
    if (error.response?.status === 401) {
      const publicRoutes = ['/login', '/register', '/review'];
      const isPublicRoute = publicRoutes.some(route => 
        window.location.pathname.startsWith(route)
      );
      
      if (!isPublicRoute) {
        // Clear auth data
        localStorage.removeItem(STORAGE_KEYS.AUTH_TOKEN);
        localStorage.removeItem(STORAGE_KEYS.USER);
        
        // Redirect to login
        window.location.href = '/login';
      }
    }
    
    // Format error for consistent handling
    const apiError: ApiError = {
      message: error.response?.data?.message || error.message || 'An unexpected error occurred',
      errors: error.response?.data?.errors,
      statusCode: error.response?.status,
    };
    
    return Promise.reject(apiError);
  }
);

/**
 * Generic request wrapper with AbortController support
 */
export interface RequestOptions<T = unknown> extends AxiosRequestConfig {
  signal?: AbortSignal;
  data?: T;
}

/**
 * GET request helper
 */
export const get = <TResponse = unknown>(
  url: string,
  options?: RequestOptions
): Promise<TResponse> => {
  return apiClient.get<TResponse>(url, options).then(res => res.data);
};

/**
 * POST request helper
 */
export const post = <TResponse = unknown, TData = unknown>(
  url: string,
  data?: TData,
  options?: RequestOptions
): Promise<TResponse> => {
  return apiClient.post<TResponse>(url, data, options).then(res => res.data);
};

/**
 * PUT request helper
 */
export const put = <TResponse = unknown, TData = unknown>(
  url: string,
  data?: TData,
  options?: RequestOptions
): Promise<TResponse> => {
  return apiClient.put<TResponse>(url, data, options).then(res => res.data);
};

/**
 * PATCH request helper
 */
export const patch = <TResponse = unknown, TData = unknown>(
  url: string,
  data?: TData,
  options?: RequestOptions
): Promise<TResponse> => {
  return apiClient.patch<TResponse>(url, data, options).then(res => res.data);
};

/**
 * DELETE request helper
 */
export const del = <TResponse = unknown>(
  url: string,
  options?: RequestOptions
): Promise<TResponse> => {
  return apiClient.delete<TResponse>(url, options).then(res => res.data);
};

/**
 * Create an AbortController for cancellable requests
 */
export const createAbortController = (): AbortController => {
  return new AbortController();
};

/**
 * Export the default axios instance for backward compatibility
 */
export const api = apiClient;
