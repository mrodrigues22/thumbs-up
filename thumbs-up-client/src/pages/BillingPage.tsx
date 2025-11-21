/**
 * BillingPage
 * Full billing management page with subscription details and pricing options
 */

import { useEffect, useState } from 'react';
import { initializePaddle, type Paddle } from '@paddle/paddle-js';
import { Layout } from '../components/layout';
import { useSubscriptionStore } from '../stores/subscriptionStore';
import { subscriptionService } from '../services/subscriptionService';
import UsageIndicator from '../components/billing/UsageIndicator';
import PricingCard from '../components/billing/PricingCard';
import SubscriptionManager from '../components/billing/SubscriptionManager';
import { Button } from '../components/common';

export default function BillingPage() {
  const { subscription, plans, isLoading, loadSubscription, loadPlans } = useSubscriptionStore();
  const [paddle, setPaddle] = useState<Paddle | null>(null);
  const [checkoutLoading, setCheckoutLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Initialize Paddle
    initializePaddle({
      environment: import.meta.env.VITE_PADDLE_ENVIRONMENT || 'sandbox',
      token: import.meta.env.VITE_PADDLE_CLIENT_TOKEN || '',
    }).then((paddleInstance) => {
      if (paddleInstance) {
        setPaddle(paddleInstance);
      }
    });

    // Load subscription and plans
    loadSubscription();
    loadPlans();
  }, [loadSubscription, loadPlans]);

  const handleSubscribe = async (priceId: string, tier: string) => {
    if (tier === 'Enterprise') {
      // For Enterprise, redirect to contact sales
      window.location.href = 'mailto:sales@thumbsup.com?subject=Enterprise%20Plan%20Inquiry';
      return;
    }

    setCheckoutLoading(true);
    setError(null);

    try {
      const { checkoutUrl } = await subscriptionService.createCheckout(
        priceId,
        `${window.location.origin}/subscription-success`
      );

      // Open Paddle checkout
      if (paddle) {
        paddle.Checkout.open({
          transactionId: checkoutUrl.split('/').pop() || '',
        });
      } else {
        // Fallback: redirect to checkout URL
        window.location.href = checkoutUrl;
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create checkout session');
    } finally {
      setCheckoutLoading(false);
    }
  };

  const handleRefresh = async () => {
    await loadSubscription();
  };

  if (isLoading && !subscription) {
    return (
      <Layout>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="flex items-center justify-center min-h-[400px]">
            <div className="text-center">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
              <p className="text-gray-600 dark:text-gray-400">Loading billing information...</p>
            </div>
          </div>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
            Billing & Subscription
          </h1>
          <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">
            Manage your subscription and view usage statistics
          </p>
        </div>

        {error && (
          <div className="mb-6 p-4 bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-200 rounded-lg flex items-center justify-between">
            <span>{error}</span>
            <button
              onClick={() => setError(null)}
              className="ml-4 underline hover:no-underline"
            >
              Dismiss
            </button>
          </div>
        )}

        {/* Usage & Management */}
        <div className="grid lg:grid-cols-3 gap-6 mb-12">
          {/* Usage Stats */}
          <div className="lg:col-span-2">
            {subscription?.usage && (
              <UsageIndicator
                submissionsUsed={subscription.usage.submissionsUsed}
                submissionsLimit={subscription.usage.submissionsLimit}
                storageUsed={subscription.usage.storageUsedFormatted}
                storageLimit={subscription.usage.storageLimitFormatted}
                submissionsPercentage={subscription.usage.submissionsPercentage}
                storagePercentage={subscription.usage.storagePercentage}
              />
            )}
          </div>

          {/* Subscription Management */}
          <div>
            {subscription && (
              <SubscriptionManager
                subscription={subscription}
                onUpdate={handleRefresh}
              />
            )}
          </div>
        </div>

        {/* Pricing Plans */}
        <div className="mb-8">
          <div className="mb-6">
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
              Available Plans
            </h2>
            <p className="text-gray-600 dark:text-gray-400">
              {subscription?.tier && subscription.tier !== 'None' 
                ? 'Upgrade or change your plan anytime' 
                : 'Choose a plan to get started'}
            </p>
          </div>

          {isLoading ? (
            <div className="flex justify-center py-12">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            </div>
          ) : plans.length === 0 ? (
            <div className="text-center py-12 bg-gray-50 dark:bg-gray-800 rounded-lg">
              <p className="text-gray-600 dark:text-gray-400">
                No plans available. Please contact support.
              </p>
            </div>
          ) : (
            <div className="grid md:grid-cols-3 gap-6 max-w-6xl">
              {plans.map((plan) => (
                <PricingCard
                  key={plan.tier}
                  plan={plan}
                  currentTier={subscription?.tier}
                  onSubscribe={handleSubscribe}
                  isPopular={plan.tier === 'Pro'}
                  isLoading={checkoutLoading}
                />
              ))}
            </div>
          )}
        </div>

        {/* Help Section */}
        <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-6 text-center max-w-2xl mx-auto">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
            Need Help?
          </h3>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Have questions about billing or need assistance with your subscription?
          </p>
          <Button
            variant="secondary"
            onClick={() => (window.location.href = 'mailto:support@thumbsup.com')}
          >
            Contact Support
          </Button>
        </div>
      </div>
    </Layout>
  );
}
