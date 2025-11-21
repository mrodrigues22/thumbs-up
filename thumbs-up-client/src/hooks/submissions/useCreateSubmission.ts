/**
 * useCreateSubmission Hook
 * Handles creating new submissions with file uploads
 */

import { useState, useCallback } from 'react';
import { submissionService } from '../../services/submissionService';
import type { CreateSubmissionRequest, SubmissionResponse, ApiError } from '../../shared/types';
import { formatApiError } from '../../shared/api';
import { toast } from 'react-toastify';

interface UseCreateSubmissionReturn {
  createSubmission: (data: CreateSubmissionRequest) => Promise<SubmissionResponse | null>;
  isLoading: boolean;
  isError: boolean;
  isSuccess: boolean;
  error: ApiError | null;
  data: SubmissionResponse | null;
  reset: () => void;
}

/**
 * Hook to create new submissions
 * Includes loading states, error handling, and success notifications
 */
export const useCreateSubmission = (): UseCreateSubmissionReturn => {
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isError, setIsError] = useState<boolean>(false);
  const [isSuccess, setIsSuccess] = useState<boolean>(false);
  const [error, setError] = useState<ApiError | null>(null);
  const [data, setData] = useState<SubmissionResponse | null>(null);

  /**
   * Create submission
   */
  const createSubmission = useCallback(async (
    submissionData: CreateSubmissionRequest
  ): Promise<SubmissionResponse | null> => {
    setIsLoading(true);
    setIsError(false);
    setIsSuccess(false);
    setError(null);

    try {
      const result = await submissionService.createSubmission(submissionData);
      setData(result);
      setIsSuccess(true);
      
      toast.success('Submission created successfully!', {
        position: 'top-right',
        autoClose: 3000,
      });
      
      return result;
    } catch (err) {
      const apiError: ApiError = {
        message: formatApiError(err),
        statusCode: (err as ApiError).statusCode,
        errors: (err as ApiError).errors,
      };
      
      setIsError(true);
      setError(apiError);
      
      toast.error(apiError.message, {
        position: 'top-right',
        autoClose: 5000,
      });
      
      return null;
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
    setData(null);
  }, []);

  return {
    createSubmission,
    isLoading,
    isError,
    isSuccess,
    error,
    data,
    reset,
  };
};
