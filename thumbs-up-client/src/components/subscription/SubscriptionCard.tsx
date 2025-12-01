import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useSubscriptionStore } from '../../stores/subscriptionStore';
import SubscriptionBadge from './SubscriptionBadge';
import UsageBar from './UsageBar';

export default function SubscriptionCard() {
  const { subscription, usage, fetchSubscription, fetchUsage } = useSubscriptionStore();

  useEffect(() => {
    fetchSubscription();
    fetchUsage();
  }, [fetchSubscription, fetchUsage]);

  if (!subscription) {
    return (
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <div className="animate-pulse">
          <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-1/2 mb-4"></div>
          <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded w-3/4"></div>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow">
      <div className="p-6 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
            Your Subscription
          </h3>
          <SubscriptionBadge />
        </div>
        
        {subscription.tier === 0 && (
          <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4 mb-4">
            <p className="text-sm text-blue-800 dark:text-blue-300 mb-2">
              Upgrade to Pro for more submissions, storage, and AI-powered insights!
            </p>
            <Link
              to="/pricing"
              className="inline-block px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors text-sm font-medium"
            >
              View Plans
            </Link>
          </div>
        )}
      </div>

      {usage && (
        <div className="p-6 space-y-4">
          <UsageBar
            used={usage.submissionsUsed}
            total={subscription.limits.submissionsPerMonth}
            label="Submissions this month"
          />
          
          <UsageBar
            used={Math.round(usage.storageUsedGB * 10) / 10}
            total={subscription.limits.storageGB}
            label="Storage"
            unit=" GB"
          />
          
          {subscription.limits.clientsMax !== -1 && (
            <UsageBar
              used={usage.clientsCount}
              total={subscription.limits.clientsMax}
              label="Clients"
            />
          )}
          
          <div className="pt-4 border-t border-gray-200 dark:border-gray-700">
            <Link
              to="/subscription"
              className="text-sm text-blue-600 dark:text-blue-400 hover:underline"
            >
              Manage subscription →
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}
