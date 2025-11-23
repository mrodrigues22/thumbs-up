import { api } from './api';
import type { ApprovalPredictionRequest, ApprovalPredictionResponse } from '../shared/types';

export const insightsService = {
  async predictApproval(
    request: ApprovalPredictionRequest,
    signal?: AbortSignal,
  ): Promise<ApprovalPredictionResponse> {
    const response = await api.post<ApprovalPredictionResponse>('/insights/predict', request, { signal });
    return response.data;
  },
};
