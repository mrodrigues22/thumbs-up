import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSubscriptionStore } from '../stores/subscriptionStore';

export default function SubscriptionSuccessPage() {
  const navigate = useNavigate();
  const { fetchSubscription } = useSubscriptionStore();

  useEffect(() => {
    // Refresh subscription data after successful payment
    const refreshData = async () => {
      await fetchSubscription();
      
      // Redirect to subscription management after 3 seconds
      setTimeout(() => {
        navigate('/subscription');
      }, 3000);
    };

    refreshData();
  }, [fetchSubscription, navigate]);

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center px-4">
      <div className="max-w-md w-full bg-white dark:bg-gray-800 rounded-lg shadow-lg p-8 text-center">
        <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-green-100 dark:bg-green-900/20 mb-6">
          <svg
            className="h-10 w-10 text-green-600 dark:text-green-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M5 13l4 4L19 7"
            />
          </svg>
        </div>

        <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
          Payment Successful!
        </h1>

        <p className="text-gray-600 dark:text-gray-400 mb-6">
          Your subscription has been upgraded successfully. You now have access to all premium features.
        </p>

        <div className="space-y-3">
          <button
            onClick={() => navigate('/subscription')}
            className="w-full px-6 py-3 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors font-medium"
          >
            View Subscription
          </button>
          <button
            onClick={() => navigate('/dashboard')}
            className="w-full px-6 py-3 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
          >
            Go to Dashboard
          </button>
        </div>

        <p className="text-sm text-gray-500 dark:text-gray-500 mt-6">
          Redirecting to subscription management in 3 seconds...
        </p>
      </div>
    </div>
  );
}
