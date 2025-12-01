export enum SubscriptionTier {
  Free = 0,
  Pro = 1,
  Enterprise = 2
}

export enum SubscriptionStatus {
  Active = 0,
  Cancelled = 1,
  PastDue = 2,
  Paused = 3,
  Expired = 4,
  Trialing = 5
}

export enum TransactionStatus {
  Completed = 0,
  Failed = 1,
  Refunded = 2,
  Pending = 3
}

export enum TransactionType {
  Subscription = 0,
  OneTime = 1,
  Refund = 2,
  Credit = 3
}

export default interface UsageLimits {
  submissionsPerMonth: number;
  storageGB: number;
  clientsMax: number;
  aiFeatures: boolean;
  prioritySupport: boolean;
}

export interface UsageStats {
  submissionsUsed: number;
  submissionsRemaining: number;
  storageUsedGB: number;
  storageRemainingGB: number;
  clientsCount: number;
  clientsRemaining: number;
  periodStart: string;
  periodEnd: string;
}

export interface Subscription {
  id: string;
  userId: string;
  status: SubscriptionStatus;
  tier: SubscriptionTier;
  planName?: string;
  currentPeriodStart?: string;
  currentPeriodEnd?: string;
  cancelledAt?: string;
  trialEndsAt?: string;
  isActive: boolean;
  canUpgrade: boolean;
  limits: UsageLimits;
  usage: UsageStats;
}

export interface Plan {
  id: string;
  name: string;
  tier: SubscriptionTier;
  description: string;
  monthlyPrice: number;
  yearlyPrice: number;
  currency: string;
  features: string[];
  limits: UsageLimits;
  isPopular: boolean;
}

export interface Transaction {
  id: string;
  paddleTransactionId: string;
  amount: number;
  currency: string;
  status: TransactionStatus;
  type: TransactionType;
  details?: string;
  createdAt: string;
}

export interface CheckoutSession {
  checkoutUrl: string;
  transactionId: string;
}

export interface CreateCheckoutRequest {
  priceId: string;
  successUrl?: string;
  cancelUrl?: string;
}

export interface CancelSubscriptionRequest {
  immediately?: boolean;
  reason?: string;
}
