import { Link } from 'react-router-dom';

interface UpgradePromptProps {
  isOpen: boolean;
  onClose: () => void;
  message?: string;
  feature?: string;
}

export default function UpgradePrompt({ isOpen, onClose, message, feature }: UpgradePromptProps) {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex min-h-screen items-center justify-center p-4">
        {/* Backdrop */}
        <div 
          className="fixed inset-0 bg-black bg-opacity-50 transition-opacity"
          onClick={onClose}
        />
        
        {/* Modal */}
        <div className="relative bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-md w-full p-6">
          <div className="text-center">
            <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-yellow-100 dark:bg-yellow-900/20 mb-4">
              <svg className="h-6 w-6 text-yellow-600 dark:text-yellow-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
            </div>
            
            <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
              Upgrade Required
            </h3>
            
            <p className="text-sm text-gray-600 dark:text-gray-400 mb-6">
              {message || 'This feature requires a Pro or Enterprise subscription.'}
              {feature && (
                <span className="block mt-2 text-xs text-gray-500 dark:text-gray-500">
                  Feature: {feature}
                </span>
              )}
            </p>
            
            <div className="flex gap-3">
              <button
                onClick={onClose}
                className="flex-1 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
              >
                Cancel
              </button>
              <Link
                to="/pricing"
                className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors font-medium"
              >
                View Plans
              </Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
