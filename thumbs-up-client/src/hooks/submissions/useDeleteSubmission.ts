/**
 * useDeleteSubmission Hook
 * Handles deleting submissions with confirmation
 */

import { useState, useCallback } from 'react';
import { submissionService } from '../../services/submissionService';
import type { ApiError } from '../../shared/types';
import { formatApiError } from '../../shared/api';
import { toast } from 'react-toastify';

interface UseDeleteSubmissionReturn {
  deleteSubmission: (id: string) => Promise<boolean>;
  isLoading: boolean;
  isError: boolean;
  isSuccess: boolean;
  error: ApiError | null;
  reset: () => void;
}

/**
 * Hook to delete submissions
 * Includes loading states, error handling, and success notifications
 */
export const useDeleteSubmission = (): UseDeleteSubmissionReturn => {
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isError, setIsError] = useState<boolean>(false);
  const [isSuccess, setIsSuccess] = useState<boolean>(false);
  const [error, setError] = useState<ApiError | null>(null);

  /**
   * Delete submission
   */
  const deleteSubmission = useCallback(async (id: string): Promise<boolean> => {
    setIsLoading(true);
    setIsError(false);
    setIsSuccess(false);
    setError(null);

    try {
      await submissionService.deleteSubmission(id);
      setIsSuccess(true);
      
      toast.success('Submission deleted successfully!', {
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

  /**
   * Reset hook state
   */
  const reset = useCallback(() => {
    setIsLoading(false);
    setIsError(false);
    setIsSuccess(false);
    setError(null);
  }, []);

  return {
    deleteSubmission,
    isLoading,
    isError,
    isSuccess,
    error,
    reset,
  };
};
