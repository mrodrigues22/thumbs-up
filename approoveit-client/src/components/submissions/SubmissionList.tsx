/**
 * SubmissionList Component
 * Displays a grid of submission cards
 */

import type { SubmissionResponse } from '../../shared/types';
import { LoadingSpinner, ErrorMessage } from '../common';
import { SubmissionCard } from './SubmissionCard';

interface SubmissionListProps {
  submissions: SubmissionResponse[];
  isLoading?: boolean;
  isError?: boolean;
  error?: Error | null;
  onDelete?: (id: string) => void;
  onRetry?: () => void;
  emptyMessage?: string;
}

export const SubmissionList: React.FC<SubmissionListProps> = ({
  submissions,
  isLoading = false,
  isError = false,
  error = null,
  onDelete,
  onRetry,
  emptyMessage = 'No submissions found.',
}) => {
  if (isLoading) {
    return <LoadingSpinner size="large" />;
  }

  if (isError && error) {
    return <ErrorMessage error={error} onRetry={onRetry} />;
  }

  if (submissions.length === 0) {
    return (
      <div className="text-center py-12">
        <svg
          className="mx-auto h-12 w-12 text-gray-400"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
          />
        </svg>
        <h3 className="mt-2 text-sm font-medium text-gray-900">No submissions</h3>
        <p className="mt-1 text-sm text-gray-500">{emptyMessage}</p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      {submissions.map((submission) => (
        <SubmissionCard
          key={submission.id}
          submission={submission}
          onDelete={onDelete}
        />
      ))}
    </div>
  );
};
