import { get, post } from '../shared/api/client';

export interface SubscriptionStatus {
  status: string;
  tier: string;
  paddleSubscriptionId?: string;
  startDate?: string;
  endDate?: string;
  cancelledAt?: string;
  usage: UsageStats;
}

export interface UsageStats {
  submissionsUsed: number;
  submissionsLimit: number;
  storageUsedBytes: number;
  storageLimitBytes: number;
  storageUsedFormatted: string;
  storageLimitFormatted: string;
  submissionsPercentage: number;
  storagePercentage: number;
}

export interface CheckoutResponse {
  checkoutUrl: string;
  transactionId: string;
}

export interface SubscriptionPlan {
  tier: string;
  priceId: string;
  price: number;
  currency: string;
  interval: string;
  submissionsLimit: number;
  clientsLimit: number;
  storageLimitBytes: number;
  hasAiFeatures: boolean;
  hasCustomBranding: boolean;
}

export const subscriptionService = {
  /**
   * Get current user's subscription status and usage
   */
  getStatus: async (): Promise<SubscriptionStatus> => {
    return get<SubscriptionStatus>('/api/subscription/status');
  },

  /**
   * Create a checkout session for a specific price/plan
   */
  createCheckout: async (priceId: string, successUrl?: string): Promise<CheckoutResponse> => {
    return post<CheckoutResponse>('/api/subscription/checkout', { 
      priceId, 
      successUrl 
    });
  },

  /**
   * Cancel the current subscription
   */
  cancel: async (immediately: boolean = false): Promise<void> => {
    return post('/api/subscription/cancel', { immediately });
  },

  /**
   * Reactivate a cancelled subscription
   */
  reactivate: async (): Promise<void> => {
    return post('/api/subscription/reactivate');
  },

  /**
   * Upgrade/downgrade subscription to a new plan
   */
  upgrade: async (newPriceId: string): Promise<void> => {
    return post('/api/subscription/upgrade', { newPriceId });
  },

  /**
   * Get URL to update payment method
   */
  getPaymentMethodUrl: async (): Promise<{ url: string }> => {
    return get('/api/subscription/payment-method-url');
  },

  /**
   * Get available subscription plans
   */
  getPlans: async (): Promise<SubscriptionPlan[]> => {
    return get<SubscriptionPlan[]>('/api/subscription/plans');
  },
};
