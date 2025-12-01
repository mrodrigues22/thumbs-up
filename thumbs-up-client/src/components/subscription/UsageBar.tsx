interface UsageBarProps {
  used: number;
  total: number;
  label: string;
  unit?: string;
}

export default function UsageBar({ used, total, label, unit = '' }: UsageBarProps) {
  const isUnlimited = total === -1 || total === Number.MAX_VALUE;
  const percentage = isUnlimited ? 0 : Math.min((used / total) * 100, 100);
  
  const getColorClass = () => {
    if (isUnlimited) return 'bg-green-600';
    if (percentage >= 90) return 'bg-red-600';
    if (percentage >= 75) return 'bg-yellow-600';
    return 'bg-blue-600';
  };

  return (
    <div className="space-y-2">
      <div className="flex justify-between text-sm">
        <span className="text-gray-700 dark:text-gray-300">{label}</span>
        <span className="text-gray-900 dark:text-white font-medium">
          {isUnlimited ? (
            'Unlimited'
          ) : (
            <>
              {used} / {total}{unit}
            </>
          )}
        </span>
      </div>
      
      {!isUnlimited && (
        <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2">
          <div
            className={`h-2 rounded-full transition-all duration-300 ${getColorClass()}`}
            style={{ width: `${percentage}%` }}
          />
        </div>
      )}
      
      {isUnlimited && (
        <div className="w-full bg-green-100 dark:bg-green-900/20 rounded-full h-2">
          <div className="h-2 rounded-full bg-green-600 w-full" />
        </div>
      )}
    </div>
  );
}
