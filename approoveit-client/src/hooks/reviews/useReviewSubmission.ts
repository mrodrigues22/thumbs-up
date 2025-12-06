/**
 * useReviewSubmission Hook
 * Fetches submission details for client review using token
 */

import { useState, useEffect, useCallback } from 'react';
import { reviewService } from '../../services/reviewService';
import type { SubmissionResponse, ApiError } from '../../shared/types';
import { formatApiError } from '../../shared/api';

interface UseReviewSubmissionOptions {
  token: string;
  password: string;
  autoFetch?: boolean;
}

interface UseReviewSubmissionReturn {
  submission: SubmissionResponse | null;
  isLoading: boolean;
  isError: boolean;
  error: ApiError | null;
  refetch: () => Promise<void>;
}

/**
 * Hook to fetch submission details for client review
 */
export const useReviewSubmission = (options: UseReviewSubmissionOptions): UseReviewSubmissionReturn => {
  const { token, password, autoFetch = true } = options;
  
  const [submission, setSubmission] = useState<SubmissionResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isError, setIsError] = useState<boolean>(false);
  const [error, setError] = useState<ApiError | null>(null);

  const fetchSubmission = useCallback(async () => {
    if (!token || !password) return;

    setIsLoading(true);
    setIsError(false);
    setError(null);

    try {
      const data = await reviewService.getSubmissionByToken(token, password);
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
  }, [token, password]);

  useEffect(() => {
    if (autoFetch && token && password) {
      fetchSubmission();
    }
  }, [autoFetch, token, password, fetchSubmission]);

  return {
    submission,
    isLoading,
    isError,
    error,
    refetch: fetchSubmission,
  };
};
