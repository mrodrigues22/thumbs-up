import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { subscriptionService } from '../services/subscriptionService';
import { useSubscriptionStore } from '../stores/subscriptionStore';
import SubscriptionBadge from '../components/subscription/SubscriptionBadge';
import UsageBar from '../components/subscription/UsageBar';
import type { Transaction } from '../types/subscription';

export default function SubscriptionManagementPage() {
  const navigate = useNavigate();
  const { subscription, usage, fetchSubscription, fetchUsage } = useSubscriptionStore();
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isCancelling, setIsCancelling] = useState(false);
  const [showCancelModal, setShowCancelModal] = useState(false);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      await Promise.all([
        fetchSubscription(),
        fetchUsage(),
        loadTransactions()
      ]);
    } finally {
      setIsLoading(false);
    }
  };

  const loadTransactions = async () => {
    try {
      const data = await subscriptionService.getTransactions(1, 10);
      setTransactions(data);
    } catch (error) {
      console.error('Failed to load transactions:', error);
    }
  };

  const handleCancelSubscription = async () => {
    setIsCancelling(true);
    try {
      await subscriptionService.cancelSubscription({ immediately: false });
      await fetchSubscription();
      setShowCancelModal(false);
      alert('Your subscription has been scheduled for cancellation at the end of the billing period.');
    } catch (error: any) {
      console.error('Failed to cancel subscription:', error);
      alert(error.response?.data?.message || 'Failed to cancel subscription');
    } finally {
      setIsCancelling(false);
    }
  };

  const handleManagePayment = async () => {
    try {
      const portalUrl = await subscriptionService.getCustomerPortalUrl();
      window.location.href = portalUrl;
    } catch (error: any) {
      console.error('Failed to open customer portal:', error);
      alert(error.response?.data?.message || 'Failed to open customer portal');
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  };

  const formatCurrency = (amount: number, currency: string) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency
    }).format(amount);
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (!subscription) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center">
        <div className="text-center">
          <p className="text-gray-600 dark:text-gray-400">No subscription found</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 py-8 px-4">
      <div className="max-w-6xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <button
            onClick={() => navigate(-1)}
            className="text-blue-600 dark:text-blue-400 hover:underline mb-4 flex items-center"
          >
            ← Back
          </button>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
            Subscription Management
          </h1>
        </div>

        <div className="grid lg:grid-cols-3 gap-6">
          {/* Main Content */}
          <div className="lg:col-span-2 space-y-6">
            {/* Current Plan */}
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
              <div className="flex items-center justify-between mb-6">
                <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
                  Current Plan
                </h2>
                <SubscriptionBadge />
              </div>

              <div className="space-y-4">
                <div>
                  <p className="text-sm text-gray-600 dark:text-gray-400">Plan Name</p>
                  <p className="text-lg font-medium text-gray-900 dark:text-white">
                    {subscription.planName || 'Free'}
                  </p>
                </div>

                {subscription.currentPeriodStart && (
                  <div>
                    <p className="text-sm text-gray-600 dark:text-gray-400">Billing Period</p>
                    <p className="text-lg font-medium text-gray-900 dark:text-white">
                      {formatDate(subscription.currentPeriodStart)} - {formatDate(subscription.currentPeriodEnd!)}
                    </p>
                  </div>
                )}

                {subscription.cancelledAt && (
                  <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4">
                    <p className="text-sm text-yellow-800 dark:text-yellow-300">
                      Your subscription is scheduled to cancel on {formatDate(subscription.currentPeriodEnd!)}
                    </p>
                  </div>
                )}

                <div className="flex gap-3 pt-4">
                  {subscription.tier === 0 ? (
                    <button
                      onClick={() => navigate('/pricing')}
                      className="px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
                    >
                      Upgrade Plan
                    </button>
                  ) : (
                    <>
                      <button
                        onClick={handleManagePayment}
                        className="px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
                      >
                        Manage Payment Method
                      </button>
                      {!subscription.cancelledAt && (
                        <button
                          onClick={() => setShowCancelModal(true)}
                          className="px-6 py-2 border border-red-300 dark:border-red-700 text-red-600 dark:text-red-400 rounded-md hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
                        >
                          Cancel Subscription
                        </button>
                      )}
                    </>
                  )}
                </div>
              </div>
            </div>

            {/* Transaction History */}
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
              <h2 className="text-xl font-semibold text-gray-900 dark:text-white mb-6">
                Transaction History
              </h2>

              {transactions.length === 0 ? (
                <p className="text-gray-600 dark:text-gray-400 text-center py-8">
                  No transactions yet
                </p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full">
                    <thead>
                      <tr className="border-b border-gray-200 dark:border-gray-700">
                        <th className="text-left py-3 px-4 text-sm font-medium text-gray-700 dark:text-gray-300">
                          Date
                        </th>
                        <th className="text-left py-3 px-4 text-sm font-medium text-gray-700 dark:text-gray-300">
                          Description
                        </th>
                        <th className="text-right py-3 px-4 text-sm font-medium text-gray-700 dark:text-gray-300">
                          Amount
                        </th>
                        <th className="text-center py-3 px-4 text-sm font-medium text-gray-700 dark:text-gray-300">
                          Status
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {transactions.map((transaction) => (
                        <tr key={transaction.id} className="border-b border-gray-100 dark:border-gray-700">
                          <td className="py-3 px-4 text-sm text-gray-900 dark:text-white">
                            {formatDate(transaction.createdAt)}
                          </td>
                          <td className="py-3 px-4 text-sm text-gray-700 dark:text-gray-300">
                            {transaction.details || 'Subscription payment'}
                          </td>
                          <td className="py-3 px-4 text-sm text-gray-900 dark:text-white text-right font-medium">
                            {formatCurrency(transaction.amount, transaction.currency)}
                          </td>
                          <td className="py-3 px-4 text-center">
                            <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                              transaction.status === 0 ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300' :
                              transaction.status === 1 ? 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300' :
                              'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
                            }`}>
                              {transaction.status === 0 ? 'Completed' :
                               transaction.status === 1 ? 'Failed' :
                               transaction.status === 2 ? 'Refunded' : 'Pending'}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Usage Stats */}
            {usage && (
              <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                  Current Usage
                </h3>
                <div className="space-y-4">
                  <UsageBar
                    used={usage.submissionsUsed}
                    total={subscription.limits.submissionsPerMonth}
                    label="Submissions"
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
                </div>
              </div>
            )}

            {/* Features */}
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                Plan Features
              </h3>
              <ul className="space-y-2">
                <li className="flex items-center text-sm text-gray-700 dark:text-gray-300">
                  <svg className="h-5 w-5 text-green-500 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                  </svg>
                  {subscription.limits.submissionsPerMonth === -1 ? 'Unlimited' : subscription.limits.submissionsPerMonth} submissions/month
                </li>
                <li className="flex items-center text-sm text-gray-700 dark:text-gray-300">
                  <svg className="h-5 w-5 text-green-500 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                  </svg>
                  {subscription.limits.storageGB === -1 ? 'Unlimited' : `${subscription.limits.storageGB}GB`} storage
                </li>
                <li className="flex items-center text-sm text-gray-700 dark:text-gray-300">
                  <svg className="h-5 w-5 text-green-500 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                  </svg>
                  {subscription.limits.clientsMax === -1 ? 'Unlimited' : subscription.limits.clientsMax} clients
                </li>
                <li className={`flex items-center text-sm ${subscription.limits.aiFeatures ? 'text-gray-700 dark:text-gray-300' : 'text-gray-400 dark:text-gray-600'}`}>
                  {subscription.limits.aiFeatures ? (
                    <svg className="h-5 w-5 text-green-500 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                    </svg>
                  ) : (
                    <svg className="h-5 w-5 text-gray-400 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  )}
                  AI-powered insights
                </li>
              </ul>

              {subscription.tier !== 2 && (
                <button
                  onClick={() => navigate('/pricing')}
                  className="mt-4 w-full px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors text-sm"
                >
                  Upgrade for More Features
                </button>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Cancel Modal */}
      {showCancelModal && (
        <div className="fixed inset-0 z-50 overflow-y-auto">
          <div className="flex min-h-screen items-center justify-center p-4">
            <div className="fixed inset-0 bg-black bg-opacity-50 transition-opacity" onClick={() => setShowCancelModal(false)} />
            
            <div className="relative bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-md w-full p-6">
              <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-4">
                Cancel Subscription?
              </h3>
              <p className="text-sm text-gray-600 dark:text-gray-400 mb-6">
                Your subscription will remain active until the end of your current billing period ({subscription.currentPeriodEnd && formatDate(subscription.currentPeriodEnd)}). After that, you'll be moved to the Free plan.
              </p>
              
              <div className="flex gap-3">
                <button
                  onClick={() => setShowCancelModal(false)}
                  className="flex-1 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700"
                  disabled={isCancelling}
                >
                  Keep Subscription
                </button>
                <button
                  onClick={handleCancelSubscription}
                  className="flex-1 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 disabled:opacity-50"
                  disabled={isCancelling}
                >
                  {isCancelling ? 'Cancelling...' : 'Yes, Cancel'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
