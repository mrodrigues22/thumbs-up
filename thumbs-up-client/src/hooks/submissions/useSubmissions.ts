/**
 * useSubmissions Hook
 * Fetches and manages list of submissions with filtering and pagination
 */

import { useState, useEffect, useCallback } from 'react';
import { submissionService } from '../../services/submissionService';
import type { SubmissionResponse, SubmissionFilters, ApiError } from '../../shared/types';
import { formatApiError } from '../../shared/api';

interface UseSubmissionsOptions {
  filters?: SubmissionFilters;
  autoFetch?: boolean;
}

interface UseSubmissionsReturn {
  submissions: SubmissionResponse[];
  isLoading: boolean;
  isError: boolean;
  error: ApiError | null;
  refetch: () => Promise<void>;
  filteredSubmissions: SubmissionResponse[];
}

/**
 * Hook to fetch and manage submissions list
 * Supports filtering, sorting, and manual refetch
 */
export const useSubmissions = (options: UseSubmissionsOptions = {}): UseSubmissionsReturn => {
  const { filters, autoFetch = true } = options;
  
  const [submissions, setSubmissions] = useState<SubmissionResponse[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isError, setIsError] = useState<boolean>(false);
  const [error, setError] = useState<ApiError | null>(null);

  /**
   * Fetch submissions from API with filters
   */
  const fetchSubmissions = useCallback(async () => {
    setIsLoading(true);
    setIsError(false);
    setError(null);

    try {
      const data = await submissionService.getSubmissions(filters);
      setSubmissions(data);
    } catch (err) {
      setIsError(true);
      setError({
        message: formatApiError(err),
        statusCode: (err as ApiError).statusCode,
      });
    } finally {
      setIsLoading(false);
    }
  }, [filters]);

  // Filtering now happens on the backend, so filteredSubmissions is the same as submissions
  const filteredSubmissions = submissions;

  // Auto-fetch on mount or when filters change
  useEffect(() => {
    if (autoFetch) {
      fetchSubmissions();
    }
  }, [autoFetch, fetchSubmissions]);

  return {
    submissions,
    isLoading,
    isError,
    error,
    refetch: fetchSubmissions,
    filteredSubmissions,
  };
};
