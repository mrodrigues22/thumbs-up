/**
 * UsageIndicator Component
 * Displays subscription usage stats with progress bars
 */

interface UsageIndicatorProps {
  submissionsUsed: number;
  submissionsLimit: number;
  storageUsed: string;
  storageLimit: string;
  submissionsPercentage: number;
  storagePercentage: number;
}

export default function UsageIndicator({
  submissionsUsed,
  submissionsLimit,
  storageUsed,
  storageLimit,
  submissionsPercentage,
  storagePercentage,
}: UsageIndicatorProps) {
  const getProgressColor = (percentage: number) => {
    if (percentage >= 90) return 'bg-red-600';
    if (percentage >= 75) return 'bg-yellow-600';
    return 'bg-blue-600';
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg p-6 shadow-md space-y-6">
      <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
        Usage This Month
      </h3>

      {/* Submissions Usage */}
      <div>
        <div className="flex justify-between items-center mb-2">
          <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
            Submissions
          </span>
          <span className="text-sm text-gray-600 dark:text-gray-400">
            {submissionsUsed} / {submissionsLimit === -1 ? '∞' : submissionsLimit}
          </span>
        </div>
        {submissionsLimit !== -1 && (
          <>
            <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2">
              <div
                className={`h-2 rounded-full transition-all ${getProgressColor(submissionsPercentage)}`}
                style={{ width: `${Math.min(submissionsPercentage, 100)}%` }}
              />
            </div>
            {submissionsPercentage >= 90 && (
              <p className="mt-1 text-xs text-red-600 dark:text-red-400">
                You're close to your limit. Consider upgrading.
              </p>
            )}
          </>
        )}
      </div>

      {/* Storage Usage */}
      <div>
        <div className="flex justify-between items-center mb-2">
          <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
            Storage
          </span>
          <span className="text-sm text-gray-600 dark:text-gray-400">
            {storageUsed} / {storageLimit}
          </span>
        </div>
        <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2">
          <div
            className={`h-2 rounded-full transition-all ${getProgressColor(storagePercentage)}`}
            style={{ width: `${Math.min(storagePercentage, 100)}%` }}
          />
        </div>
        {storagePercentage >= 90 && (
          <p className="mt-1 text-xs text-red-600 dark:text-red-400">
            Storage almost full. Upgrade for more space.
          </p>
        )}
      </div>
    </div>
  );
}
