/**
 * useValidateAccess Hook
 * Validates access token and password for client review
 */

import { useState, useCallback } from 'react';
import { reviewService } from '../../services/reviewService';
import type { ValidateAccessRequest, ValidateAccessResponse, ApiError } from '../../shared/types';
import { formatApiError } from '../../shared/api';

interface UseValidateAccessReturn {
  validateAccess: (data: ValidateAccessRequest) => Promise<ValidateAccessResponse | null>;
  isLoading: boolean;
  isError: boolean;
  isSuccess: boolean;
  error: ApiError | null;
  result: ValidateAccessResponse | null;
  reset: () => void;
}

/**
 * Hook to validate access credentials for reviewing submissions
 */
export const useValidateAccess = (): UseValidateAccessReturn => {
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isError, setIsError] = useState<boolean>(false);
  const [isSuccess, setIsSuccess] = useState<boolean>(false);
  const [error, setError] = useState<ApiError | null>(null);
  const [result, setResult] = useState<ValidateAccessResponse | null>(null);

  const validateAccess = useCallback(async (
    data: ValidateAccessRequest
  ): Promise<ValidateAccessResponse | null> => {
    setIsLoading(true);
    setIsError(false);
    setIsSuccess(false);
    setError(null);

    try {
      const response = await reviewService.validateAccess(data);
      setResult(response);
      setIsSuccess(response.valid);
      
      if (!response.valid) {
        setIsError(true);
        setError({
          message: response.message,
        });
      }
      
      return response;
    } catch (err) {
      const apiError: ApiError = {
        message: formatApiError(err),
        statusCode: (err as ApiError).statusCode,
      };
      
      setIsError(true);
      setError(apiError);
      
      return null;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const reset = useCallback(() => {
    setIsLoading(false);
    setIsError(false);
    setIsSuccess(false);
    setError(null);
    setResult(null);
  }, []);

  return {
    validateAccess,
    isLoading,
    isError,
    isSuccess,
    error,
    result,
    reset,
  };
};
