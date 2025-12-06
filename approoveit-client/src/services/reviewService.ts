import { api } from './api';
import type { SubmissionResponse, SubmitReviewRequest, ValidateAccessRequest } from '../shared/types';

export const reviewService = {
  async validateAccess(data: ValidateAccessRequest): Promise<{ valid: boolean; message: string }> {
    const response = await api.post<{ valid: boolean; message: string }>('/review/validate', data);
    return response.data;
  },

  async getSubmissionByToken(token: string, password: string): Promise<SubmissionResponse> {
    const response = await api.get<SubmissionResponse>(`/review/${token}`, {
      params: { password },
    });
    return response.data;
  },

  async submitReview(data: SubmitReviewRequest): Promise<{ message: string }> {
    const response = await api.post<{ message: string }>('/review/submit', data);
    return response.data;
  },
};
