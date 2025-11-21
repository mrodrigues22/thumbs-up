/**
 * useSubmitReview Hook
 * Submits client review (approve/reject) for a submission
 */

import { useState, useCallback } from 'react';
import { reviewService } from '../../services/reviewService';
import type { SubmitReviewRequest, ApiError } from '../../shared/types';
import { formatApiError } from '../../shared/api';
import { toast } from 'react-toastify';

interface UseSubmitReviewReturn {
  submitReview: (data: SubmitReviewRequest) => Promise<boolean>;
  isLoading: boolean;
  isError: boolean;
  isSuccess: boolean;
  error: ApiError | null;
  reset: () => void;
}

/**
 * Hook to submit client review
 * Includes loading states, error handling, and success notifications
 */
export const useSubmitReview = (): UseSubmitReviewReturn => {
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isError, setIsError] = useState<boolean>(false);
  const [isSuccess, setIsSuccess] = useState<boolean>(false);
  const [error, setError] = useState<ApiError | null>(null);

  const submitReview = useCallback(async (data: SubmitReviewRequest): Promise<boolean> => {
    setIsLoading(true);
    setIsError(false);
    setIsSuccess(false);
    setError(null);

    try {
      await reviewService.submitReview(data);
      setIsSuccess(true);
      
      toast.success('Review submitted successfully! Thank you for your feedback.', {
        position: 'top-right',
        autoClose: 3000,
      });
      
      return true;
    } catch (err) {
      const apiError: ApiError = {
        message: formatApiError(err),
        statusCode: (err as ApiError).statusCode,
      };
      
      setIsError(true);
      setError(apiError);
      
      toast.error(apiError.message, {
        position: 'top-right',
        autoClose: 5000,
      });
      
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const reset = useCallback(() => {
    setIsLoading(false);
    setIsError(false);
    setIsSuccess(false);
    setError(null);
  }, []);

  return {
    submitReview,
    isLoading,
    isError,
    isSuccess,
    error,
    reset,
  };
};
