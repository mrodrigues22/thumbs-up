import { api } from './api';
import type { CreateSubmissionRequest, SubmissionResponse, SubmissionFilters } from '../shared/types';

export const submissionService = {
  async createSubmission(data: CreateSubmissionRequest): Promise<SubmissionResponse> {
    const formData = new FormData();
    
    // Add client information based on what's provided
    if (data.clientId) {
      formData.append('clientId', data.clientId);
    }
    if (data.clientEmail) {
      formData.append('clientEmail', data.clientEmail);
    }
    if (data.clientName) {
      formData.append('clientName', data.clientName);
    }
    
    if (data.message) {
      formData.append('message', data.message);
    }
    if (data.captions) {
      formData.append('captions', data.captions);
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

  async getSubmissions(filters?: SubmissionFilters): Promise<SubmissionResponse[]> {
    const params = new URLSearchParams();
    
    if (filters) {
      if (filters.status) params.append('status', filters.status.toString());
      if (filters.searchTerm) params.append('searchTerm', filters.searchTerm);
      if (filters.dateFrom) params.append('dateFrom', filters.dateFrom);
      if (filters.dateTo) params.append('dateTo', filters.dateTo);
      if (filters.sortBy) params.append('sortBy', filters.sortBy);
      if (filters.sortOrder) params.append('sortOrder', filters.sortOrder);
    }
    
    const url = params.toString() ? `/submission?${params.toString()}` : '/submission';
    const response = await api.get<SubmissionResponse[]>(url);
    return response.data;
  },

  async getSubmission(id: string): Promise<SubmissionResponse> {
    const response = await api.get<SubmissionResponse>(`/submission/${id}`);
    return response.data;
  },

  async deleteSubmission(id: string): Promise<void> {
    await api.delete(`/submission/${id}`);
  },

  async getSubmissionsByClient(clientId: string): Promise<SubmissionResponse[]> {
    const response = await api.get<SubmissionResponse[]>(`/client/${clientId}/submissions`);
    return response.data;
  },

  async requestReanalysis(id: string): Promise<void> {
    await api.post(`/submission/${id}/reanalyze`);
  },
};
