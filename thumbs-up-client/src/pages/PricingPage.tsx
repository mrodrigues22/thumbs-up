import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { subscriptionService } from '../services/subscriptionService';
import { useSubscriptionStore } from '../stores/subscriptionStore';
import type { Plan } from '../types/subscription';

export default function PricingPage() {
  const navigate = useNavigate();
  const { subscription, fetchSubscription } = useSubscriptionStore();
  const [plans, setPlans] = useState<Plan[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [billingPeriod, setBillingPeriod] = useState<'monthly' | 'yearly'>('monthly');
  const [processingPlanId, setProcessingPlanId] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      // Load plans (always works)
      const plansData = await subscriptionService.getPlans();
      setPlans(plansData);
      
      // Try to load subscription if user is logged in
      const token = localStorage.getItem('authToken');
      if (token) {
        try {
          await fetchSubscription();
        } catch (error: any) {
          // Silently fail if not authenticated - pricing page should work for everyone
          // Only log if it's not a 401/404 (which are expected for non-subscribers)
          if (error.response?.status !== 401 && error.response?.status !== 404) {
            console.error('Error fetching subscription:', error);
          }
        }
      }
    } catch (error) {
      console.error('Failed to load pricing data:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleUpgrade = async (plan: Plan) => {
    if (!plan.id) return;
    
    // Check if user is logged in
    const token = localStorage.getItem('authToken');
    if (!token) {
      alert('Please log in or sign up to subscribe to a plan.');
      navigate('/login', { state: { returnUrl: '/pricing' } });
      return;
    }
    
    setProcessingPlanId(plan.id);
    
    try {
      const priceId = billingPeriod === 'monthly' 
        ? plan.id  // Assuming this is the monthly price ID
        : plan.id.replace('Monthly', 'Yearly'); // Simple transformation for demo
      
      const checkoutSession = await subscriptionService.createCheckoutSession({
        priceId,
        successUrl: `${window.location.origin}/subscription/success`,
        cancelUrl: `${window.location.origin}/pricing`
      });
      
      // Redirect to Paddle checkout
      window.location.href = checkoutSession.checkoutUrl;
    } catch (error: any) {
      console.error('Failed to create checkout session:', error);
      
      let errorMessage = 'Failed to start checkout. Please try again.';
      
      if (error.response?.status === 401) {
        errorMessage = 'Your session has expired. Please log in again.';
        navigate('/login', { state: { returnUrl: '/pricing' } });
      } else if (error.response?.data?.message) {
        errorMessage = error.response.data.message;
      } else if (!navigator.onLine) {
        errorMessage = 'No internet connection. Please check your connection and try again.';
      }
      
      alert(errorMessage);
      setProcessingPlanId(null);
    }
  };

  const getPrice = (plan: Plan) => {
    return billingPeriod === 'monthly' ? plan.monthlyPrice : plan.yearlyPrice / 12;
  };

  const getSavings = (plan: Plan) => {
    const monthlyCost = plan.monthlyPrice * 12;
    const yearlyCost = plan.yearlyPrice;
    return monthlyCost - yearlyCost;
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600 dark:text-gray-400">Loading plans...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 py-12 px-4">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            Choose Your Plan
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400 mb-8">
            Simple, transparent pricing for creators and agencies
          </p>
          
          {/* Billing Toggle */}
          <div className="inline-flex items-center bg-white dark:bg-gray-800 rounded-lg p-1 shadow">
            <button
              onClick={() => setBillingPeriod('monthly')}
              className={`px-6 py-2 rounded-md text-sm font-medium transition-colors ${
                billingPeriod === 'monthly'
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white'
              }`}
            >
              Monthly
            </button>
            <button
              onClick={() => setBillingPeriod('yearly')}
              className={`px-6 py-2 rounded-md text-sm font-medium transition-colors ${
                billingPeriod === 'yearly'
                  ? 'bg-blue-600 text-white'
                  : 'text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white'
              }`}
            >
              Yearly
              <span className="ml-2 text-xs text-green-600 dark:text-green-400">
                Save 17%
              </span>
            </button>
          </div>
        </div>

        {/* Plans Grid */}
        <div className="grid md:grid-cols-3 gap-8 max-w-6xl mx-auto">
          {plans.map((plan) => (
            <div
              key={plan.id}
              className={`bg-white dark:bg-gray-800 rounded-lg shadow-lg overflow-hidden ${
                plan.isPopular ? 'ring-2 ring-blue-600 relative' : ''
              }`}
            >
              {plan.isPopular && (
                <div className="absolute top-0 right-0 bg-blue-600 text-white px-4 py-1 text-xs font-semibold rounded-bl-lg">
                  POPULAR
                </div>
              )}
              
              <div className="p-8">
                <h3 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
                  {plan.name}
                </h3>
                <p className="text-gray-600 dark:text-gray-400 mb-6">
                  {plan.description}
                </p>
                
                <div className="mb-6">
                  <div className="flex items-baseline">
                    <span className="text-5xl font-bold text-gray-900 dark:text-white">
                      ${getPrice(plan).toFixed(0)}
                    </span>
                    <span className="text-gray-600 dark:text-gray-400 ml-2">
                      /month
                    </span>
                  </div>
                  {billingPeriod === 'yearly' && plan.tier !== 0 && (
                    <p className="text-sm text-green-600 dark:text-green-400 mt-2">
                      Save ${getSavings(plan)}/year
                    </p>
                  )}
                </div>
                
                {/* CTA Button */}
                {subscription && subscription.tier === plan.tier ? (
                  <div className="mb-6">
                    <button
                      className="w-full px-6 py-3 border-2 border-gray-300 dark:border-gray-600 rounded-lg text-gray-700 dark:text-gray-300 font-medium"
                      disabled
                    >
                      Current Plan
                    </button>
                  </div>
                ) : subscription && subscription.tier > plan.tier ? (
                  <div className="mb-6">
                    <button
                      className="w-full px-6 py-3 border-2 border-gray-300 dark:border-gray-600 rounded-lg text-gray-700 dark:text-gray-300 font-medium"
                      disabled
                    >
                      Downgrade Not Available
                    </button>
                  </div>
                ) : (
                  <>
                    <button
                      onClick={() => handleUpgrade(plan)}
                      disabled={processingPlanId === plan.id}
                      className={`w-full px-6 py-3 rounded-lg font-medium transition-colors mb-6 ${
                        plan.isPopular
                          ? 'bg-blue-600 text-white hover:bg-blue-700'
                          : 'bg-gray-900 text-white hover:bg-gray-800 dark:bg-white dark:text-gray-900 dark:hover:bg-gray-100'
                      } disabled:opacity-50 disabled:cursor-not-allowed`}
                    >
                      {processingPlanId === plan.id ? (
                        <span className="flex items-center justify-center">
                          <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                          </svg>
                          Processing...
                        </span>
                      ) : subscription ? (
                        'Upgrade Now'
                      ) : (
                        'Get Started'
                      )}
                    </button>
                    {!localStorage.getItem('authToken') && (
                      <p className="text-xs text-gray-500 dark:text-gray-400 text-center -mt-3 mb-3">
                        You'll be asked to log in or sign up
                      </p>
                    )}
                  </>
                )}
                
                {/* Features */}
                <ul className="space-y-3">
                  {plan.features.map((feature, index) => (
                    <li key={index} className="flex items-start">
                      <svg
                        className="h-5 w-5 text-green-500 mr-3 mt-0.5 flex-shrink-0"
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
                      <span className="text-gray-700 dark:text-gray-300 text-sm">
                        {feature}
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          ))}
        </div>

        {/* FAQ or Additional Info */}
        <div className="mt-16 text-center">
          <p className="text-gray-600 dark:text-gray-400">
            Need a custom plan?{' '}
            <a href="mailto:sales@thumbsup.com" className="text-blue-600 dark:text-blue-400 hover:underline">
              Contact us
            </a>
          </p>
        </div>
      </div>
    </div>
  );
}
