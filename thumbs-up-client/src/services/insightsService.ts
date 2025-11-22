import { api } from './api';
import type { ApprovalPredictionRequest, ApprovalPredictionResponse } from '../shared/types';

export const insightsService = {
  async predictApproval(request: ApprovalPredictionRequest): Promise<ApprovalPredictionResponse> {
    const response = await api.post<ApprovalPredictionResponse>('/insights/predict', request);
    return response.data;
  },
};
