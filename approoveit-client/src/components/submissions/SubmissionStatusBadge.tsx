/**
 * SubmissionStatusBadge Component
 * Displays submission status with appropriate color
 */

import { SubmissionStatus } from '../../shared/types';

interface SubmissionStatusBadgeProps {
  status: SubmissionStatus;
}

export const SubmissionStatusBadge: React.FC<SubmissionStatusBadgeProps> = ({ status }) => {
  const getStatusConfig = () => {
    switch (status) {
      case SubmissionStatus.Pending:
        return { label: 'Pending', color: 'bg-yellow-100 text-yellow-800 border-yellow-200' };
      case SubmissionStatus.Approved:
        return { label: 'Approved', color: 'bg-green-100 text-green-800 border-green-200' };
      case SubmissionStatus.Rejected:
        return { label: 'Rejected', color: 'bg-red-100 text-red-800 border-red-200' };
      case SubmissionStatus.Expired:
        return { label: 'Expired', color: 'bg-gray-100 text-gray-800 border-gray-200' };
      default:
        return { label: 'Unknown', color: 'bg-gray-100 text-gray-800 border-gray-200' };
    }
  };

  const { label, color } = getStatusConfig();

  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border ${color}`}>
      {label}
    </span>
  );
};
