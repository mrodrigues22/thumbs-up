/**
 * useSubmissionDetail Hook
 * Fetches and manages a single submission by ID
 */

import { useState, useEffect, useCallback } from 'react';
import { submissionService } from '../../services/submissionService';
import type { SubmissionResponse, ApiError } from '../../shared/types';
import { formatApiError } from '../../shared/api';

interface UseSubmissionDetailOptions {
  id: string;
  autoFetch?: boolean;
}

interface UseSubmissionDetailReturn {
  submission: SubmissionResponse | null;
  isLoading: boolean;
  isError: boolean;
  error: ApiError | null;
  refetch: () => Promise<void>;
}

/**
 * Hook to fetch a single submission by ID
 * Supports auto-fetch and manual refetch
 */
export const useSubmissionDetail = (options: UseSubmissionDetailOptions): UseSubmissionDetailReturn => {
  const { id, autoFetch = true } = options;
  
  const [submission, setSubmission] = useState<SubmissionResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isError, setIsError] = useState<boolean>(false);
  const [error, setError] = useState<ApiError | null>(null);

  /**
   * Fetch submission from API
   */
  const fetchSubmission = useCallback(async () => {
    if (!id) return;

    setIsLoading(true);
    setIsError(false);
    setError(null);

    try {
      const data = await submissionService.getSubmission(id);
      setSubmission(data);
    } catch (err) {
      setIsError(true);
      setError({
        message: formatApiError(err),
        statusCode: (err as ApiError).statusCode,
      });
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  // Auto-fetch on mount or when ID changes
  useEffect(() => {
    if (autoFetch && id) {
      fetchSubmission();
    }
  }, [autoFetch, id, fetchSubmission]);

  return {
    submission,
    isLoading,
    isError,
    error,
    refetch: fetchSubmission,
  };
};
