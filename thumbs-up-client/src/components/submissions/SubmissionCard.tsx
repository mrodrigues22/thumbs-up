/**
 * SubmissionCard Component
 * Displays submission summary in a card format
 */

import { Link } from 'react-router-dom';
import type { SubmissionResponse } from '../../shared/types';
import { Card, Button } from '../common';
import { SubmissionStatusBadge } from './SubmissionStatusBadge';

interface SubmissionCardProps {
  submission: SubmissionResponse;
  onDelete?: (id: string) => void;
}

export const SubmissionCard: React.FC<SubmissionCardProps> = ({
  submission,
  onDelete,
}) => {
  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  return (
    <Card className="hover:shadow-lg transition-shadow">
      <Link to={`/submissions/${submission.id}`} className="block">
        <div className="space-y-4">
          {/* Header */}
          <div className="flex items-start justify-between">
            <div className="flex-1">
              <h3 className="text-lg font-semibold text-gray-900">
                {submission.clientName || submission.clientEmail}
              </h3>
              <p className="text-sm text-gray-500 mt-1">
                Created: {formatDate(submission.createdAt)}
              </p>
            </div>
            <SubmissionStatusBadge status={submission.status} />
          </div>

          {/* Message Preview */}
          {submission.message && (
            <p className="text-sm text-gray-600 line-clamp-2">{submission.message}</p>
          )}

          {/* Stats */}
          <div className="flex items-center space-x-4 text-sm text-gray-500">
            <div className="flex items-center">
              <svg className="w-4 h-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
                />
              </svg>
              {submission.mediaFiles.length} file{submission.mediaFiles.length !== 1 ? 's' : ''}
            </div>
            <div className="flex items-center">
              <svg className="w-4 h-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"
                />
              </svg>
              Expires: {formatDate(submission.expiresAt)}
            </div>
          </div>
        </div>
      </Link>

      {/* Actions */}
      {onDelete && (
        <div 
          className="flex items-center justify-end pt-4 border-t border-gray-200"
          onClick={(e: React.MouseEvent) => {
            e.preventDefault();
            e.stopPropagation();
          }}
        >
          <Button
            variant="ghost"
            size="small"
            onClick={() => onDelete(submission.id)}
          >
            Delete
          </Button>
        </div>
      )}
    </Card>
  );
};
