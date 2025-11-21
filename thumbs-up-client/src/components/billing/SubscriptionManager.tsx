/**
 * SubscriptionManager Component
 * Manage current subscription (cancel, reactivate, update payment)
 */

import { useState } from 'react';
import { Button } from '../common';
import { subscriptionService } from '../../services/subscriptionService';
import type { SubscriptionStatus } from '../../services/subscriptionService';

interface SubscriptionManagerProps {
  subscription: SubscriptionStatus;
  onUpdate: () => void;
}

export default function SubscriptionManager({ subscription, onUpdate }: SubscriptionManagerProps) {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleCancel = async () => {
    if (!confirm('Are you sure you want to cancel your subscription? You will retain access until the end of your billing period.')) {
      return;
    }

    setIsLoading(true);
    setError(null);
    try {
      await subscriptionService.cancel(false);
      await onUpdate();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to cancel subscription');
    } finally {
      setIsLoading(false);
    }
  };

  const handleReactivate = async () => {
    setIsLoading(true);
    setError(null);
    try {
      await subscriptionService.reactivate();
      await onUpdate();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reactivate subscription');
    } finally {
      setIsLoading(false);
    }
  };

  const handleUpdatePayment = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const { url } = await subscriptionService.getPaymentMethodUrl();
      window.open(url, '_blank');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to get payment update URL');
    } finally {
      setIsLoading(false);
    }
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  const isCancelled = subscription.status === 'Cancelled';
  const hasActiveSubscription = subscription.paddleSubscriptionId;

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg p-6 shadow-md">
      <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
        Subscription Management
      </h3>

      {error && (
        <div className="mb-4 p-3 bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-200 rounded">
          {error}
        </div>
      )}

      <div className="space-y-3 mb-6">
        <div className="flex justify-between">
          <span className="text-gray-600 dark:text-gray-400">Current Plan:</span>
          <span className="font-semibold text-gray-900 dark:text-white">{subscription.tier}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-gray-600 dark:text-gray-400">Status:</span>
          <span className={`font-semibold ${
            isCancelled ? 'text-red-600' : 'text-green-600'
          }`}>
            {subscription.status}
          </span>
        </div>
        {subscription.startDate && (
          <div className="flex justify-between">
            <span className="text-gray-600 dark:text-gray-400">Started:</span>
            <span className="text-gray-900 dark:text-white">{formatDate(subscription.startDate)}</span>
          </div>
        )}
        {subscription.endDate && (
          <div className="flex justify-between">
            <span className="text-gray-600 dark:text-gray-400">
              {isCancelled ? 'Ends:' : 'Renews:'}
            </span>
            <span className="text-gray-900 dark:text-white">{formatDate(subscription.endDate)}</span>
          </div>
        )}
        {subscription.cancelledAt && (
          <div className="flex justify-between">
            <span className="text-gray-600 dark:text-gray-400">Cancelled:</span>
            <span className="text-gray-900 dark:text-white">{formatDate(subscription.cancelledAt)}</span>
          </div>
        )}
      </div>

      {hasActiveSubscription && (
        <div className="space-y-3">
          {isCancelled ? (
            <Button
              variant="primary"
              fullWidth
              onClick={handleReactivate}
              disabled={isLoading}
            >
              {isLoading ? 'Processing...' : 'Reactivate Subscription'}
            </Button>
          ) : (
            <>
              <Button
                variant="secondary"
                fullWidth
                onClick={handleUpdatePayment}
                disabled={isLoading}
              >
                Update Payment Method
              </Button>
              <Button
                variant="secondary"
                fullWidth
                onClick={handleCancel}
                disabled={isLoading}
              >
                {isLoading ? 'Processing...' : 'Cancel Subscription'}
              </Button>
            </>
          )}
        </div>
      )}
    </div>
  );
}
