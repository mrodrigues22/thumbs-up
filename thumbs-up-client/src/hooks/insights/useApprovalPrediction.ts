import { useState, useEffect } from 'react';
import { insightsService } from '../../services/insightsService';
import type { ApprovalPredictionResponse, ApiError } from '../../shared/types';
import { formatApiError } from '../../shared/api';

interface UseApprovalPredictionOptions {
  clientId?: string;
  submissionId?: string;
  enabled?: boolean;
}

interface UseApprovalPredictionReturn {
  prediction: ApprovalPredictionResponse | null;
  isLoading: boolean;
  isError: boolean;
  error: ApiError | null;
}

export const useApprovalPrediction = (
  options: UseApprovalPredictionOptions,
): UseApprovalPredictionReturn => {
  const { clientId, submissionId, enabled = true } = options;

  const [prediction, setPrediction] = useState<ApprovalPredictionResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isError, setIsError] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  useEffect(() => {
    const shouldFetch =
      enabled &&
      !!clientId &&
      !!submissionId;

    if (!shouldFetch) {
      return;
    }

    let cancelled = false;

    const fetchPrediction = async () => {
      setIsLoading(true);
      setIsError(false);
      setError(null);

      try {
        const data = await insightsService.predictApproval({
          clientId,
          submissionId,
        } as { clientId: string; submissionId: string });
        if (!cancelled) {
          setPrediction(data);
        }
      } catch (err) {
        if (!cancelled) {
          setIsError(true);
          setError({
            message: formatApiError(err),
            statusCode: (err as ApiError).statusCode,
          });
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    };

    fetchPrediction();

    return () => {
      cancelled = true;
    };
  }, [clientId, submissionId, enabled]);

  return { prediction, isLoading, isError, error };
};
