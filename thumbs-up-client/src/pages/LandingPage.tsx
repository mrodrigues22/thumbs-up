/**
 * LandingPage
 * Marketing landing page for new users to learn about the app
 */

import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { initializePaddle, type Paddle } from '@paddle/paddle-js';
import { useDarkMode } from '../hooks/useDarkMode';
import { useAuthStore } from '../stores/authStore';
import { Button } from '../components/common';
import { subscriptionService, type SubscriptionPlan } from '../services/subscriptionService';

export default function LandingPage() {
  const { isDarkMode, toggleDarkMode } = useDarkMode();
  const { isAuthenticated } = useAuthStore();
  const navigate = useNavigate();
  const [paddle, setPaddle] = useState<Paddle | null>(null);
  const [checkoutLoading, setCheckoutLoading] = useState<string | null>(null);
  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [plansLoading, setPlansLoading] = useState(true);

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

    // Load plans
    subscriptionService.getPlans()
      .then(setPlans)
      .catch(console.error)
      .finally(() => setPlansLoading(false));
  }, []);

  const handleGetStarted = async (tier: string, priceId: string) => {
    // If user is not authenticated, redirect to register
    if (!isAuthenticated) {
      navigate('/register', { state: { selectedPlan: tier, priceId } });
      return;
    }

    // If authenticated, open checkout
    if (tier === 'Enterprise') {
      window.location.href = 'mailto:sales@thumbsup.com?subject=Enterprise%20Plan%20Inquiry';
      return;
    }

    setCheckoutLoading(tier);
    try {
      const { checkoutUrl } = await subscriptionService.createCheckout(
        priceId,
        `${window.location.origin}/subscription-success`
      );

      if (paddle) {
        paddle.Checkout.open({
          transactionId: checkoutUrl.split('/').pop() || '',
        });
      } else {
        window.location.href = checkoutUrl;
      }
    } catch (error) {
      console.error('Failed to create checkout:', error);
      alert('Failed to start checkout. Please try again.');
    } finally {
      setCheckoutLoading(null);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-b from-blue-50 to-white dark:from-gray-900 dark:to-gray-800">
      {/* Navigation */}
      <nav className="bg-white dark:bg-gray-800 shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center">
              <img 
                src={isDarkMode ? "/logo-light.svg" : "/logo.svg"}
                className="h-10" 
                alt="Thumbs Up Logo" 
              />
            </div>
            <div className="flex items-center gap-4">
              <button
                onClick={toggleDarkMode}
                className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
                aria-label="Toggle dark mode"
              >
                {isDarkMode ? '🌞' : '🌙'}
              </button>
              <Link to="/login">
                <Button variant="secondary" size="small">
                  Log In
                </Button>
              </Link>
              <Link to="/register">
                <Button variant="primary" size="small">
                  Get Started
                </Button>
              </Link>
            </div>
          </div>
        </div>
      </nav>

      {/* Hero Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 text-center">
        <h1 className="text-5xl md:text-6xl font-bold text-gray-900 dark:text-white mb-6">
          Get Client Feedback
          <span className="block text-blue-600 dark:text-blue-400">The Easy Way</span>
        </h1>
        <p className="text-xl text-gray-600 dark:text-gray-300 mb-8 max-w-3xl mx-auto">
          Share your creative work, collect client reviews, and get AI-powered insights. 
          Streamline your approval process and predict client preferences with Thumbs Up.
        </p>
        <div className="flex gap-4 justify-center">
          <Link to="/register">
            <Button variant="primary" size="large">
              Get Started
            </Button>
          </Link>
          <a href="#pricing">
            <Button variant="secondary" size="large">
              View Pricing
            </Button>
          </a>
        </div>
      </section>

      {/* AI Features Section */}
      <section className="bg-gradient-to-r from-purple-600 to-blue-600 dark:from-purple-900 dark:to-blue-900 py-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-12">
            <div className="text-5xl mb-4">🤖</div>
            <h2 className="text-3xl md:text-4xl font-bold text-white mb-4">
              Powered by AI
            </h2>
            <p className="text-xl text-purple-100 max-w-3xl mx-auto">
              Leverage artificial intelligence to understand your clients better and predict approval likelihood
            </p>
          </div>
          <div className="grid md:grid-cols-3 gap-8">
            {/* AI Feature 1 */}
            <div className="bg-white/10 backdrop-blur-lg rounded-xl p-8 text-white">
              <div className="text-4xl mb-4">📝</div>
              <h3 className="text-xl font-semibold mb-3">
                AI Review Summaries
              </h3>
              <p className="text-purple-100">
                Get instant AI-generated summaries of all your client's reviews and feedback, saving you time and highlighting key insights.
              </p>
            </div>

            {/* AI Feature 2 */}
            <div className="bg-white/10 backdrop-blur-lg rounded-xl p-8 text-white">
              <div className="text-4xl mb-4">🎨</div>
              <h3 className="text-xl font-semibold mb-3">
                Style & Preference Analysis
              </h3>
              <p className="text-purple-100">
                Our AI learns your client's preferences and style from their past reviews, giving you deep insights into what they love.
              </p>
            </div>

            {/* AI Feature 3 */}
            <div className="bg-white/10 backdrop-blur-lg rounded-xl p-8 text-white">
              <div className="text-4xl mb-4">🎯</div>
              <h3 className="text-xl font-semibold mb-3">
                Approval Predictions
              </h3>
              <p className="text-purple-100">
                Before submitting, get AI predictions on the likelihood of client approval based on their historical preferences and feedback patterns.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
        <h2 className="text-3xl md:text-4xl font-bold text-center text-gray-900 dark:text-white mb-12">
          Everything You Need to Get Feedback
        </h2>
        <div className="grid md:grid-cols-3 gap-8">
          {/* Feature 1 */}
          <div className="bg-white dark:bg-gray-800 rounded-xl p-8 shadow-lg hover:shadow-xl transition-shadow">
            <div className="text-4xl mb-4">📸</div>
            <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">
              Upload & Share
            </h3>
            <p className="text-gray-600 dark:text-gray-300">
              Upload images, videos, and documents. Share them with clients via secure, password-protected links.
            </p>
          </div>

          {/* Feature 2 */}
          <div className="bg-white dark:bg-gray-800 rounded-xl p-8 shadow-lg hover:shadow-xl transition-shadow">
            <div className="text-4xl mb-4">💬</div>
            <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">
              Collect Reviews
            </h3>
            <p className="text-gray-600 dark:text-gray-300">
              Clients can leave comments, ratings, and approvals directly on your submissions—no account required.
            </p>
          </div>

          {/* Feature 3 */}
          <div className="bg-white dark:bg-gray-800 rounded-xl p-8 shadow-lg hover:shadow-xl transition-shadow">
            <div className="text-4xl mb-4">📊</div>
            <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">
              Manage Projects
            </h3>
            <p className="text-gray-600 dark:text-gray-300">
              Keep track of all your submissions, client feedback, and project status in one organized dashboard.
            </p>
          </div>

          {/* Feature 4 */}
          <div className="bg-white dark:bg-gray-800 rounded-xl p-8 shadow-lg hover:shadow-xl transition-shadow">
            <div className="text-4xl mb-4">👥</div>
            <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">
              Client Management
            </h3>
            <p className="text-gray-600 dark:text-gray-300">
              Organize your clients, track their projects, and maintain a complete history of all interactions.
            </p>
          </div>

          {/* Feature 5 */}
          <div className="bg-white dark:bg-gray-800 rounded-xl p-8 shadow-lg hover:shadow-xl transition-shadow">
            <div className="text-4xl mb-4">🔒</div>
            <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">
              Secure & Private
            </h3>
            <p className="text-gray-600 dark:text-gray-300">
              Your work is protected with password-protected access and secure storage for peace of mind.
            </p>
          </div>

          {/* Feature 6 */}
          <div className="bg-white dark:bg-gray-800 rounded-xl p-8 shadow-lg hover:shadow-xl transition-shadow">
            <div className="text-4xl mb-4">⚡</div>
            <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">
              Fast & Simple
            </h3>
            <p className="text-gray-600 dark:text-gray-300">
              Intuitive interface that gets out of your way. Upload, share, and get feedback in minutes.
            </p>
          </div>
        </div>
      </section>

      {/* How It Works Section */}
      <section className="bg-gray-100 dark:bg-gray-800 py-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <h2 className="text-3xl md:text-4xl font-bold text-center text-gray-900 dark:text-white mb-12">
            How It Works
          </h2>
          <div className="grid md:grid-cols-4 gap-8">
            <div className="text-center">
              <div className="bg-blue-600 text-white w-12 h-12 rounded-full flex items-center justify-center text-xl font-bold mx-auto mb-4">
                1
              </div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                Sign Up
              </h3>
              <p className="text-gray-600 dark:text-gray-300">
                Create your account and choose a plan
              </p>
            </div>
            <div className="text-center">
              <div className="bg-blue-600 text-white w-12 h-12 rounded-full flex items-center justify-center text-xl font-bold mx-auto mb-4">
                2
              </div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                Upload Work
              </h3>
              <p className="text-gray-600 dark:text-gray-300">
                Add your images, videos, or documents
              </p>
            </div>
            <div className="text-center">
              <div className="bg-blue-600 text-white w-12 h-12 rounded-full flex items-center justify-center text-xl font-bold mx-auto mb-4">
                3
              </div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                Share Link
              </h3>
              <p className="text-gray-600 dark:text-gray-300">
                Send secure review link to your client
              </p>
            </div>
            <div className="text-center">
              <div className="bg-blue-600 text-white w-12 h-12 rounded-full flex items-center justify-center text-xl font-bold mx-auto mb-4">
                4
              </div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                Get Feedback
              </h3>
              <p className="text-gray-600 dark:text-gray-300">
                Receive comments and approvals instantly
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* Pricing Section */}
      <section id="pricing" className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
        <h2 className="text-3xl md:text-4xl font-bold text-center text-gray-900 dark:text-white mb-4">
          Simple, Transparent Pricing
        </h2>
        <p className="text-center text-gray-600 dark:text-gray-300 mb-12">
          Choose the plan that works for you
        </p>
        
        {plansLoading ? (
          <div className="flex justify-center py-12">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          </div>
        ) : (
          <div className="grid md:grid-cols-3 gap-8 max-w-5xl mx-auto">
            {plans.map((plan) => {
              const isPro = plan.tier === 'Pro';
              const isEnterprise = plan.tier === 'Enterprise';
              const storageGB = plan.storageLimitBytes / (1024 * 1024 * 1024);
              
              return (
                <div 
                  key={plan.tier}
                  className={`bg-white dark:bg-gray-800 rounded-xl p-8 shadow-lg border-2 ${
                    isPro 
                      ? 'border-blue-600 shadow-xl transform scale-105' 
                      : 'border-gray-200 dark:border-gray-700'
                  } relative`}
                >
                  {isPro && (
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
                        {plan.clientsLimit === -1 ? 'Unlimited' : plan.clientsLimit} clients
                      </span>
                    </li>
                    <li className="flex items-start">
                      <span className="text-green-500 mr-2">✓</span>
                      <span className="text-gray-600 dark:text-gray-300">
                        {storageGB}GB storage
                      </span>
                    </li>
                    {plan.hasAiFeatures && (
                      <li className="flex items-start">
                        <span className="text-green-500 mr-2">✓</span>
                        <span className="text-gray-600 dark:text-gray-300">AI insights & predictions</span>
                      </li>
                    )}
                    {plan.hasCustomBranding && (
                      <li className="flex items-start">
                        <span className="text-green-500 mr-2">✓</span>
                        <span className="text-gray-600 dark:text-gray-300">Custom branding</span>
                      </li>
                    )}
                    <li className="flex items-start">
                      <span className="text-green-500 mr-2">✓</span>
                      <span className="text-gray-600 dark:text-gray-300">
                        {isPro ? 'Priority' : isEnterprise ? 'Dedicated' : 'Email'} support
                      </span>
                    </li>
                    {isEnterprise && (
                      <>
                        <li className="flex items-start">
                          <span className="text-green-500 mr-2">✓</span>
                          <span className="text-gray-600 dark:text-gray-300">Team collaboration</span>
                        </li>
                        <li className="flex items-start">
                          <span className="text-green-500 mr-2">✓</span>
                          <span className="text-gray-600 dark:text-gray-300">Advanced AI analytics</span>
                        </li>
                      </>
                    )}
                  </ul>
                  
                  <Button 
                    variant={isPro ? 'primary' : 'secondary'}
                    fullWidth
                    onClick={() => handleGetStarted(plan.tier, plan.priceId)}
                    disabled={checkoutLoading === plan.tier}
                  >
                    {checkoutLoading === plan.tier 
                      ? 'Loading...' 
                      : isEnterprise 
                        ? 'Contact Sales' 
                        : 'Get Started'}
                  </Button>
                </div>
              );
            })}
          </div>
        )}
      </section>

      {/* CTA Section */}
      <section className="bg-blue-600 dark:bg-blue-700 py-16">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl md:text-4xl font-bold text-white mb-4">
            Ready to Streamline Your Feedback Process?
          </h2>
          <p className="text-xl text-blue-100 mb-8">
            Join hundreds of creatives using AI-powered insights to understand their clients better
          </p>
          <Link to="/register">
            <Button variant="secondary" size="large">
              Get Started Today
            </Button>
          </Link>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-gray-900 dark:bg-black text-gray-300 py-12">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-4 gap-8">
            <div>
              <img 
                src="/logo-light.svg"
                className="h-8 mb-4" 
                alt="Thumbs Up Logo" 
              />
              <p className="text-sm text-gray-400">
                The easiest way to collect client feedback on your creative work.
              </p>
            </div>
            <div>
              <h4 className="font-semibold text-white mb-4">Product</h4>
              <ul className="space-y-2 text-sm">
                <li><a href="#" className="hover:text-white transition-colors">Features</a></li>
                <li><a href="#pricing" className="hover:text-white transition-colors">Pricing</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Security</a></li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold text-white mb-4">Company</h4>
              <ul className="space-y-2 text-sm">
                <li><a href="#" className="hover:text-white transition-colors">About</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Blog</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Contact</a></li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold text-white mb-4">Legal</h4>
              <ul className="space-y-2 text-sm">
                <li><a href="#" className="hover:text-white transition-colors">Privacy</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Terms</a></li>
                <li><a href="#" className="hover:text-white transition-colors">Cookie Policy</a></li>
              </ul>
            </div>
          </div>
          <div className="border-t border-gray-800 mt-8 pt-8 text-center text-sm text-gray-400">
            <p>&copy; {new Date().getFullYear()} Thumbs Up. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
