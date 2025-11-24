/**
 * SubmissionsPage (formerly DashboardPage)
 * Main dashboard showing all submissions with filters
 * Displays submissions grouped by client with image galleries
 */

import { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { Layout } from '../components/layout';
import { Button, Card, LoadingSpinner, ErrorMessage } from '../components/common';
import { SubmissionFilters, SubmissionStatusBadge } from '../components/submissions';
import { useSubmissions } from '../hooks/submissions';
import type { SubmissionFilters as Filters, SubmissionResponse } from '../shared/types';
import { MediaFileType as MediaFileTypeEnum } from '../shared/types';

export default function SubmissionsPage() {
  const navigate = useNavigate();
  const [filters, setFilters] = useState<Filters>({
    sortBy: 'createdAt',
    sortOrder: 'desc',
  });

  const {
    filteredSubmissions,
    isLoading,
    isError,
    error,
    refetch,
  } = useSubmissions({ filters });

  // Group submissions by client and sort by most recent
  const groupedSubmissions = useMemo(() => {
    // Sort submissions by date (most recent first)
    const sorted = [...filteredSubmissions].sort((a, b) => 
      new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );

    // Group by client
    const grouped = new Map<string, SubmissionResponse[]>();
    sorted.forEach(submission => {
      const clientKey = submission.clientId || submission.clientEmail;
      if (!grouped.has(clientKey)) {
        grouped.set(clientKey, []);
      }
      grouped.get(clientKey)!.push(submission);
    });

    return Array.from(grouped.entries()).map(([clientKey, submissions]) => ({
      clientKey,
      clientName: submissions[0].clientName || submissions[0].clientEmail,
      clientEmail: submissions[0].clientEmail,
      clientCompanyName: submissions[0].clientCompanyName,
      submissions: submissions.slice(0, 3), // Only show the 3 most recent submissions per client
      totalCount: submissions.length,
    }));
  }, [filteredSubmissions]);

  const handleFiltersChange = (newFilters: Filters) => {
    setFilters(newFilters);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <Layout>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">Submissions</h1>
            <p className="mt-2 text-sm text-gray-600">
              Manage and review your client submissions
            </p>
          </div>
          <div className="mt-4 sm:mt-0">
            <Button
              onClick={() => navigate('/submissions/create')}
              variant="primary"
              size="medium"
            >
              <svg
                className="w-5 h-5 mr-2"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 4v16m8-8H4"
                />
              </svg>
              New Submission
            </Button>
          </div>
        </div>

        {/* Filters */}
        <SubmissionFilters
          onFiltersChange={handleFiltersChange}
          initialFilters={filters}
        />

        {/* Loading State */}
        {isLoading && <LoadingSpinner size="large" />}

        {/* Error State */}
        {isError && error && (
          <ErrorMessage error={new Error(error.message)} onRetry={refetch} />
        )}

        {/* Empty State */}
        {!isLoading && !isError && groupedSubmissions.length === 0 && (
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
            <p className="mt-1 text-sm text-gray-500">
              No submissions found. Create your first one to get started!
            </p>
          </div>
        )}

        {/* Grouped Submissions by Client */}
        {!isLoading && !isError && groupedSubmissions.length > 0 && (
          <div className="space-y-8">
            {groupedSubmissions.map((group) => (
              <ClientSubmissionGroup
                key={group.clientKey}
                group={group}
                formatDate={formatDate}
                navigate={navigate}
              />
            ))}
          </div>
        )}
      </div>
    </Layout>
  );
}

// Component for each client group
interface ClientSubmissionGroupProps {
  group: {
    clientKey: string;
    clientName: string;
    clientEmail: string;
    clientCompanyName?: string;
    submissions: SubmissionResponse[];
    totalCount: number;
  };
  formatDate: (date: string) => string;
  navigate: (path: string) => void;
}

function ClientSubmissionGroup({ group, formatDate, navigate }: ClientSubmissionGroupProps) {
  return (
    <Card className="overflow-hidden">
      {/* Client Header */}
      <div className="bg-gray-50 px-6 py-4 border-b border-gray-200">
        <h2 className="text-xl font-semibold text-gray-900">{group.clientName}</h2>
        <div className="flex items-center gap-4 mt-1 text-sm text-gray-600">
          <span>{group.clientEmail}</span>
          {group.clientCompanyName && (
            <>
              <span>•</span>
              <span>{group.clientCompanyName}</span>
            </>
          )}
          <span>•</span>
          <span>{group.totalCount} submission{group.totalCount !== 1 ? 's' : ''}</span>
          {group.totalCount > group.submissions.length && (
            <>
              <span>•</span>
              <span className="text-blue-600">Showing {group.submissions.length} most recent</span>
            </>
          )}
        </div>
      </div>

      {/* Submissions Grid */}
      <div className="p-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {group.submissions.map((submission) => (
            <SubmissionCard
              key={submission.id}
              submission={submission}
              formatDate={formatDate}
              navigate={navigate}
            />
          ))}
        </div>
      </div>
    </Card>
  );
}

// Component for each submission card
interface SubmissionCardProps {
  submission: SubmissionResponse;
  formatDate: (date: string) => string;
  navigate: (path: string) => void;
}

function SubmissionCard({ submission, formatDate, navigate }: SubmissionCardProps) {
  const [currentMediaIndex, setCurrentMediaIndex] = useState(0);

  return (
    <div 
      className="bg-white border border-gray-200 rounded-lg overflow-hidden cursor-pointer hover:shadow-lg transition-shadow"
      onClick={() => navigate(`/submissions/${submission.id}`)}
    >
      {/* Image Gallery */}
      <div>
        {submission.mediaFiles.length === 0 ? (
          <div className="bg-gray-100 flex items-center justify-center h-64">
            <svg className="w-12 h-12 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
              />
            </svg>
          </div>
        ) : submission.mediaFiles.length === 1 ? (
          // Single image
          <div className="relative bg-gray-100" style={{ paddingBottom: '100%' }}>
            {submission.mediaFiles[0].fileType === MediaFileTypeEnum.Image ? (
              <img
                src={submission.mediaFiles[0].fileUrl}
                alt={submission.mediaFiles[0].fileName}
                className="absolute inset-0 w-full h-full object-cover"
              />
            ) : (
              <video
                src={submission.mediaFiles[0].fileUrl}
                className="absolute inset-0 w-full h-full object-cover"
              />
            )}
          </div>
        ) : (
          // Multiple images - carousel
          <div className="relative">
            <div className="bg-gray-100">
              <div className="relative" style={{ paddingBottom: '100%' }}>
                {submission.mediaFiles[currentMediaIndex].fileType === MediaFileTypeEnum.Image ? (
                  <img
                    src={submission.mediaFiles[currentMediaIndex].fileUrl}
                    alt={submission.mediaFiles[currentMediaIndex].fileName}
                    className="absolute inset-0 w-full h-full object-cover"
                  />
                ) : (
                  <video
                    src={submission.mediaFiles[currentMediaIndex].fileUrl}
                    className="absolute inset-0 w-full h-full object-cover"
                  />
                )}

                {/* Navigation arrows */}
                {currentMediaIndex > 0 && (
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      setCurrentMediaIndex(currentMediaIndex - 1);
                    }}
                    className="absolute left-2 top-1/2 -translate-y-1/2 bg-white/90 hover:bg-white rounded-full p-2 shadow-lg transition-all z-10"
                    aria-label="Previous image"
                  >
                    <svg className="w-5 h-5 text-gray-800" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                    </svg>
                  </button>
                )}

                {currentMediaIndex < submission.mediaFiles.length - 1 && (
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      setCurrentMediaIndex(currentMediaIndex + 1);
                    }}
                    className="absolute right-2 top-1/2 -translate-y-1/2 bg-white/90 hover:bg-white rounded-full p-2 shadow-lg transition-all z-10"
                    aria-label="Next image"
                  >
                    <svg className="w-5 h-5 text-gray-800" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                    </svg>
                  </button>
                )}

                {/* Counter indicator */}
                <div className="absolute top-3 right-3 bg-black/60 text-white px-2.5 py-1 rounded-full text-xs font-medium">
                  {currentMediaIndex + 1} / {submission.mediaFiles.length}
                </div>
              </div>
            </div>

            {/* Dots indicator */}
            <div className="absolute bottom-3 left-0 right-0 flex justify-center gap-1.5">
              {submission.mediaFiles.map((_, index) => (
                <button
                  key={index}
                  onClick={(e) => {
                    e.stopPropagation();
                    setCurrentMediaIndex(index);
                  }}
                  className={`w-1.5 h-1.5 rounded-full transition-all ${
                    index === currentMediaIndex
                      ? 'bg-white w-6'
                      : 'bg-white/60 hover:bg-white/80'
                  }`}
                  aria-label={`Go to image ${index + 1}`}
                />
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Submission Info */}
      <div className="p-4 space-y-2">
        <p className="text-sm text-gray-700 font-medium">
          {formatDate(submission.createdAt)}
        </p>
        <SubmissionStatusBadge status={submission.status} />
      </div>
    </div>
  );
}
