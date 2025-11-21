/**
 * PricingCard Component
 * Individual pricing tier card with subscribe button
 */

import { Button } from '../common';
import type { SubscriptionPlan } from '../../services/subscriptionService';

interface PricingCardProps {
  plan: SubscriptionPlan;
  currentTier?: string;
  onSubscribe: (priceId: string, tier: string) => void;
  isPopular?: boolean;
  isLoading?: boolean;
}

export default function PricingCard({
  plan,
  currentTier,
  onSubscribe,
  isPopular = false,
  isLoading = false,
}: PricingCardProps) {
  const isCurrentPlan = currentTier === plan.tier;
  
  const formatStorageLimit = (bytes: number): string => {
    const gb = bytes / (1024 * 1024 * 1024);
    return `${gb}GB`;
  };

  return (
    <div
      className={`bg-white dark:bg-gray-800 rounded-xl p-8 shadow-lg border-2 transition-transform hover:scale-105 ${
        isPopular
          ? 'border-blue-600 relative transform scale-105'
          : 'border-gray-200 dark:border-gray-700'
      }`}
    >
      {isPopular && (
        <div className="absolute top-0 right-0 bg-blue-600 text-white px-3 py-1 text-sm font-semibold rounded-bl-lg rounded-tr-lg">
          Popular
        </div>
      )}

      <h3 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
        {plan.tier}
      </h3>

      <div className="text-4xl font-bold text-gray-900 dark:text-white mb-6">
        ${plan.price}
        <span className="text-lg text-gray-600 dark:text-gray-400">/{plan.interval}</span>
      </div>

      <ul className="space-y-3 mb-8">
        <li className="flex items-start">
          <span className="text-green-500 mr-2">✓</span>
          <span className="text-gray-600 dark:text-gray-300">
            {plan.submissionsLimit === -1 ? 'Unlimited' : `Up to ${plan.submissionsLimit}`} submissions
          </span>
        </li>
        <li className="flex items-start">
          <span className="text-green-500 mr-2">✓</span>
          <span className="text-gray-600 dark:text-gray-300">
            {plan.clientsLimit === -1 ? 'Unlimited' : `${plan.clientsLimit}`} clients
          </span>
        </li>
        <li className="flex items-start">
          <span className="text-green-500 mr-2">✓</span>
          <span className="text-gray-600 dark:text-gray-300">
            {formatStorageLimit(plan.storageLimitBytes)} storage
          </span>
        </li>
        {plan.hasAiFeatures && (
          <li className="flex items-start">
            <span className="text-green-500 mr-2">✓</span>
            <span className="text-gray-600 dark:text-gray-300">
              AI insights & predictions
            </span>
          </li>
        )}
        {plan.hasCustomBranding && (
          <li className="flex items-start">
            <span className="text-green-500 mr-2">✓</span>
            <span className="text-gray-600 dark:text-gray-300">
              Custom branding
            </span>
          </li>
        )}
        <li className="flex items-start">
          <span className="text-green-500 mr-2">✓</span>
          <span className="text-gray-600 dark:text-gray-300">
            {plan.tier === 'Enterprise' ? 'Dedicated' : plan.tier === 'Pro' ? 'Priority' : 'Email'} support
          </span>
        </li>
      </ul>

      {isCurrentPlan ? (
        <Button variant="secondary" fullWidth disabled>
          Current Plan
        </Button>
      ) : (
        <Button
          variant={isPopular ? 'primary' : 'secondary'}
          fullWidth
          onClick={() => onSubscribe(plan.priceId, plan.tier)}
          disabled={isLoading}
        >
          {isLoading ? 'Processing...' : plan.tier === 'Enterprise' ? 'Contact Sales' : 'Get Started'}
        </Button>
      )}
    </div>
  );
}
