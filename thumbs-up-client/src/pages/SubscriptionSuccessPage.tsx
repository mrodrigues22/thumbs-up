/**
 * SubscriptionSuccessPage
 * Success page after completing Paddle checkout
 */

import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSubscriptionStore } from '../stores/subscriptionStore';
import { Button } from '../components/common';

export default function SubscriptionSuccessPage() {
  const navigate = useNavigate();
  const { loadSubscription } = useSubscriptionStore();
  const [isChecking, setIsChecking] = useState(true);
  const [retryCount, setRetryCount] = useState(0);
  const maxRetries = 10;

  useEffect(() => {
    // Poll for subscription status
    const checkSubscription = async () => {
      try {
        await loadSubscription();
        setIsChecking(false);
      } catch (error) {
        if (retryCount < maxRetries) {
          setTimeout(() => {
            setRetryCount(retryCount + 1);
          }, 2000); // Retry every 2 seconds
        } else {
          setIsChecking(false);
        }
      }
    };

    if (isChecking) {
      checkSubscription();
    }
  }, [retryCount, isChecking, loadSubscription]);

  const handleContinue = () => {
    navigate('/dashboard');
  };

  return (
    <div className="min-h-screen bg-gradient-to-b from-blue-50 to-white dark:from-gray-900 dark:to-gray-800 flex items-center justify-center px-4">
      <div className="max-w-md w-full bg-white dark:bg-gray-800 rounded-lg shadow-xl p-8 text-center">
        {isChecking ? (
          <>
            <div className="animate-spin rounded-full h-16 w-16 border-b-4 border-blue-600 mx-auto mb-6"></div>
            <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
              Processing Your Subscription...
            </h1>
            <p className="text-gray-600 dark:text-gray-400 mb-4">
              Please wait while we confirm your payment and activate your subscription.
            </p>
            <p className="text-sm text-gray-500 dark:text-gray-500">
              This usually takes just a few seconds.
            </p>
          </>
        ) : (
          <>
            <div className="text-6xl mb-6">🎉</div>
            <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-4">
              Welcome Aboard!
            </h1>
            <p className="text-gray-600 dark:text-gray-400 mb-6">
              Your subscription has been activated successfully. You now have access to all the premium features.
            </p>
            <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-4 mb-6">
              <p className="text-sm text-gray-700 dark:text-gray-300">
                Start creating submissions, invite clients, and use AI-powered insights to understand your clients better.
              </p>
            </div>
            <Button variant="primary" onClick={handleContinue} fullWidth>
              Go to Dashboard
            </Button>
            <button
              onClick={() => navigate('/billing')}
              className="mt-4 text-sm text-blue-600 dark:text-blue-400 hover:underline"
            >
              View subscription details
            </button>
          </>
        )}
      </div>
    </div>
  );
}
