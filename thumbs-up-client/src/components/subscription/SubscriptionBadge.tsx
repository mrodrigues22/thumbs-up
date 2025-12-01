import { useSubscriptionStore } from '../../stores/subscriptionStore';

export default function SubscriptionBadge() {
  const { subscription } = useSubscriptionStore();
  
  if (!subscription) return null;

  const getTierColor = () => {
    switch (subscription.tier) {
      case 0: return 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300';
      case 1: return 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300';
      case 2: return 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-300';
      default: return 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300';
    }
  };

  const getTierName = () => {
    switch (subscription.tier) {
      case 0: return 'Free';
      case 1: return 'Pro';
      case 2: return 'Enterprise';
      default: return 'Free';
    }
  };

  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getTierColor()}`}>
      {getTierName()}
    </span>
  );
}
