import axios from 'axios';
import type {
  Subscription,
  UsageStats,
  Plan,
  Transaction,
  CheckoutSession,
  CreateCheckoutRequest,
  CancelSubscriptionRequest
} from '../types/subscription';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5039/api';

// Create axios instance with auth token
const api = axios.create({
  baseURL: `${API_URL}/subscription`,
  headers: {
    'Content-Type': 'application/json'
  }
});

// Add auth token to requests
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('authToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export const subscriptionService = {
  /**
   * Get current user's subscription
   */
  async getCurrentSubscription(): Promise<Subscription> {
    const response = await api.get('/current');
    return response.data;
  },

  /**
   * Get usage statistics
   */
  async getUsage(): Promise<UsageStats> {
    const response = await api.get('/usage');
    return response.data;
  },

  /**
   * Get available subscription plans
   */
  async getPlans(): Promise<Plan[]> {
    const response = await api.get('/plans');
    return response.data;
  },

  /**
   * Create checkout session for subscription upgrade
   */
  async createCheckoutSession(request: CreateCheckoutRequest): Promise<CheckoutSession> {
    const response = await api.post('/checkout', request);
    return response.data;
  },

  /**
   * Cancel subscription
   */
  async cancelSubscription(request: CancelSubscriptionRequest = {}): Promise<void> {
    await api.post('/cancel', request);
  },

  /**
   * Get customer portal URL
   */
  async getCustomerPortalUrl(): Promise<string> {
    const response = await api.post('/customer-portal');
    return response.data.url;
  },

  /**
   * Get transaction history
   */
  async getTransactions(page: number = 1, pageSize: number = 20): Promise<Transaction[]> {
    const response = await api.get('/transactions', {
      params: { page, pageSize }
    });
    return response.data;
  },

  /**
   * Check feature access
   */
  async checkFeatureAccess(featureName: string): Promise<boolean> {
    const response = await api.get(`/feature/${featureName}`);
    return response.data.hasAccess;
  }
};
