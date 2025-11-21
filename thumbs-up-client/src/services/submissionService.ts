import { api } from './api';
import type { CreateSubmissionRequest, SubmissionResponse } from '../shared/types';

export const submissionService = {
  async createSubmission(data: CreateSubmissionRequest): Promise<SubmissionResponse> {
    const formData = new FormData();
    formData.append('clientEmail', data.clientEmail);
    if (data.message) {
      formData.append('message', data.message);
    }
    data.files.forEach((file) => {
      formData.append('files', file);
    });

    const response = await api.post<SubmissionResponse>('/submission', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  async getSubmissions(): Promise<SubmissionResponse[]> {
    const response = await api.get<SubmissionResponse[]>('/submission');
    return response.data;
  },

  async getSubmission(id: string): Promise<SubmissionResponse> {
    const response = await api.get<SubmissionResponse>(`/submission/${id}`);
    return response.data;
  },

  async deleteSubmission(id: string): Promise<void> {
    await api.delete(`/submission/${id}`);
  },
};
