import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { clientService } from '../../services/clientService';
import type { CreateClientRequest, UpdateClientRequest } from '../../shared/types';

// Query key factory
export const clientKeys = {
  all: ['clients'] as const,
  lists: () => [...clientKeys.all, 'list'] as const,
  list: () => [...clientKeys.lists()] as const,
  details: () => [...clientKeys.all, 'detail'] as const,
  detail: (id: string) => [...clientKeys.details(), id] as const,
};

// Fetch all clients
export function useClients() {
  return useQuery({
    queryKey: clientKeys.list(),
    queryFn: () => clientService.getClients(),
  });
}

// Fetch a single client
export function useClient(id: string) {
  return useQuery({
    queryKey: clientKeys.detail(id),
    queryFn: () => clientService.getClient(id),
    enabled: !!id,
  });
}

// Create a new client
export function useCreateClient() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateClientRequest) => clientService.createClient(data),
    onSuccess: () => {
      // Invalidate and refetch clients list
      queryClient.invalidateQueries({ queryKey: clientKeys.list() });
    },
  });
}

// Update a client
export function useUpdateClient() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateClientRequest }) =>
      clientService.updateClient(id, data),
    onSuccess: (_, variables) => {
      // Invalidate and refetch clients list and the specific client
      queryClient.invalidateQueries({ queryKey: clientKeys.list() });
      queryClient.invalidateQueries({ queryKey: clientKeys.detail(variables.id) });
    },
  });
}

// Delete a client
export function useDeleteClient() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => clientService.deleteClient(id),
    onSuccess: () => {
      // Invalidate and refetch clients list
      queryClient.invalidateQueries({ queryKey: clientKeys.list() });
    },
  });
}
