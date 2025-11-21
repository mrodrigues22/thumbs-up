import { api } from './api';
import type { Client, CreateClientRequest, UpdateClientRequest } from '../shared/types';

export const clientService = {
  // Get all clients for the current user
  async getClients(): Promise<Client[]> {
    const response = await api.get<Client[]>('/client');
    return response.data;
  },

  // Get a specific client by ID
  async getClient(id: string): Promise<Client> {
    const response = await api.get<Client>(`/client/${id}`);
    return response.data;
  },

  // Create a new client
  async createClient(data: CreateClientRequest): Promise<Client> {
    const response = await api.post<Client>('/client', data);
    return response.data;
  },

  // Update an existing client
  async updateClient(id: string, data: UpdateClientRequest): Promise<Client> {
    const response = await api.put<Client>(`/client/${id}`, data);
    return response.data;
  },

  // Delete a client
  async deleteClient(id: string): Promise<void> {
    await api.delete(`/client/${id}`);
  },
};
