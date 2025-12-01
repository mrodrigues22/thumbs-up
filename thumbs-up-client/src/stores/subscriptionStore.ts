import { create } from 'zustand';
import type { Subscription, UsageStats, SubscriptionTier } from '../types/subscription';
import { subscriptionService } from '../services/subscriptionService';

interface SubscriptionStore {
  subscription: Subscription | null;
  usage: UsageStats | null;
  isLoading: boolean;
  error: string | null;

  // Actions
  fetchSubscription: () => Promise<void>;
  fetchUsage: () => Promise<void>;
  checkFeatureAccess: (feature: string) => boolean;
  hasFeature: (feature: string) => Promise<boolean>;
  canCreateSubmission: () => boolean;
  getTierName: () => string;
  reset: () => void;
}

export const useSubscriptionStore = create<SubscriptionStore>((set, get) => ({
  subscription: null,
  usage: null,
  isLoading: false,
  error: null,

  fetchSubscription: async () => {
    set({ isLoading: true, error: null });
    try {
      const subscription = await subscriptionService.getCurrentSubscription();
      set({ subscription, isLoading: false });
    } catch (error: any) {
      set({ 
        error: error.response?.data?.message || 'Failed to fetch subscription', 
        isLoading: false 
      });
    }
  },

  fetchUsage: async () => {
    try {
      const usage = await subscriptionService.getUsage();
      set({ usage });
    } catch (error: any) {
      set({ error: error.response?.data?.message || 'Failed to fetch usage' });
    }
  },

  checkFeatureAccess: (feature: string): boolean => {
    const { subscription } = get();
    if (!subscription) return false;

    switch (feature.toLowerCase()) {
      case 'ai':
      case 'ai_features':
        return subscription.limits.aiFeatures;
      case 'priority_support':
        return subscription.limits.prioritySupport;
      case 'unlimited_submissions':
        return subscription.limits.submissionsPerMonth === -1;
      case 'unlimited_clients':
        return subscription.limits.clientsMax === -1;
      default:
        return true;
    }
  },

  hasFeature: async (feature: string): Promise<boolean> => {
    try {
      return await subscriptionService.checkFeatureAccess(feature);
    } catch {
      return false;
    }
  },

  canCreateSubmission: (): boolean => {
    const { subscription } = get();
    if (!subscription) return false;
    return subscription.usage.submissionsRemaining > 0;
  },

  getTierName: (): string => {
    const { subscription } = get();
    if (!subscription) return 'Free';
    
    switch (subscription.tier) {
      case 0: return 'Free';
      case 1: return 'Pro';
      case 2: return 'Enterprise';
      default: return 'Free';
    }
  },

  reset: () => {
    set({
      subscription: null,
      usage: null,
      isLoading: false,
      error: null
    });
  }
}));
