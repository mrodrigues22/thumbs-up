import { create } from 'zustand';
import { subscriptionService } from '../services/subscriptionService';
import type { SubscriptionStatus, SubscriptionPlan } from '../services/subscriptionService';

interface SubscriptionStore {
  subscription: SubscriptionStatus | null;
  plans: SubscriptionPlan[];
  isLoading: boolean;
  error: string | null;
  
  // Actions
  loadSubscription: () => Promise<void>;
  loadPlans: () => Promise<void>;
  refresh: () => Promise<void>;
  clearError: () => void;
}

export const useSubscriptionStore = create<SubscriptionStore>((set, get) => ({
  subscription: null,
  plans: [],
  isLoading: false,
  error: null,

  loadSubscription: async () => {
    set({ isLoading: true, error: null });
    try {
      const subscription = await subscriptionService.getStatus();
      set({ subscription, isLoading: false });
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Failed to load subscription',
        isLoading: false 
      });
    }
  },

  loadPlans: async () => {
    set({ isLoading: true, error: null });
    try {
      const plans = await subscriptionService.getPlans();
      set({ plans, isLoading: false });
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Failed to load plans',
        isLoading: false 
      });
    }
  },

  refresh: async () => {
    await Promise.all([
      get().loadSubscription(),
      get().loadPlans()
    ]);
  },

  clearError: () => set({ error: null }),
}));
