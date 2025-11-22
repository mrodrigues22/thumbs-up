import { api } from './api';
import type {
  ClientSummaryResponse,
  ApprovalPredictionRequest,
  ApprovalPredictionResponse,
} from '../shared/types';

export const aiService = {
  async getClientSummary(clientId: string): Promise<ClientSummaryResponse> {
    const response = await api.get<ClientSummaryResponse>(`/insights/clients/${clientId}/summary`);
    return response.data;
  },

  async predictApproval(data: ApprovalPredictionRequest): Promise<ApprovalPredictionResponse> {
    const response = await api.post<ApprovalPredictionResponse>('/insights/predict', data);
    return response.data;
  },
};
