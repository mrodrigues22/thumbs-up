import { api } from './api';
import type {
  ClientSummaryResponse,
  ApprovalPredictionRequest,
  ApprovalPredictionResponse,
} from '../shared/types';

export const aiService = {
  async getClientSummary(clientId: string, signal?: AbortSignal): Promise<ClientSummaryResponse> {
    const response = await api.get<ClientSummaryResponse>(`/insights/clients/${clientId}/summary`, { signal });
    return response.data;
  },

  async predictApproval(
    data: ApprovalPredictionRequest,
    signal?: AbortSignal,
  ): Promise<ApprovalPredictionResponse> {
    const response = await api.post<ApprovalPredictionResponse>('/insights/predict', data, { signal });
    return response.data;
  },
};
