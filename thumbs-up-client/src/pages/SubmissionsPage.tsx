/**
 * SubmissionsPage (formerly DashboardPage)
 * Main dashboard showing all submissions with filters
 */

import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Layout } from '../components/layout';
import { Button } from '../components/common';
import { SubmissionList, SubmissionFilters } from '../components/submissions';
import { useSubmissions, useDeleteSubmission } from '../hooks/submissions';
import type { SubmissionFilters as Filters } from '../shared/types';


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

  const { deleteSubmission, isLoading: isDeleting } = useDeleteSubmission();

  const handleDelete = async (id: string) => {
    if (!window.confirm('Are you sure you want to delete this submission?')) {
      return;
    }

    const success = await deleteSubmission(id);
    if (success) {
      refetch();
    }
  };

  const handleFiltersChange = (newFilters: Filters) => {
    setFilters(newFilters);
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

        {/* Submissions List */}
        <SubmissionList
          submissions={filteredSubmissions}
          isLoading={isLoading || isDeleting}
          isError={isError}
          error={error ? new Error(error.message) : null}
          onDelete={handleDelete}
          onRetry={refetch}
          emptyMessage="No submissions found. Create your first one to get started!"
        />
      </div>
    </Layout>
  );
}
