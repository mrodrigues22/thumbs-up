import { useState, useEffect } from 'react';
import { insightsService } from '../../services/insightsService';
import type { ApprovalPredictionResponse, ApiError } from '../../shared/types';
import { formatApiError } from '../../shared/api';

const isAbortError = (err: unknown) => {
  if (typeof err !== 'object' || err === null) {
    return false;
  }
  const name = (err as { name?: string }).name;
  return name === 'CanceledError' || name === 'AbortError';
};

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

    let isSubscribed = true;
    const controller = new AbortController();

    const fetchPrediction = async () => {
      setIsLoading(true);
      setIsError(false);
      setError(null);

      try {
        const data = await insightsService.predictApproval({
          clientId,
          submissionId,
        } as { clientId: string; submissionId: string }, controller.signal);
        if (isSubscribed) {
          setPrediction(data);
        }
      } catch (err) {
        if (!isSubscribed || controller.signal.aborted || isAbortError(err)) {
          return;
        }
        if (isSubscribed) {
          setIsError(true);
          setError({
            message: formatApiError(err),
            statusCode: (err as ApiError).statusCode,
          });
        }
      } finally {
        if (isSubscribed && !controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    };

    fetchPrediction();

    return () => {
      isSubscribed = false;
      controller.abort();
    };
  }, [clientId, submissionId, enabled]);

  return { prediction, isLoading, isError, error };
};
