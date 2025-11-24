/**
 * useSubmissions Hook
 * Fetches and manages list of submissions with filtering and pagination
 */

import { useState, useEffect, useCallback } from 'react';
import { submissionService } from '../../services/submissionService';
import type { SubmissionResponse, SubmissionFilters, ApiError } from '../../shared/types';
import { formatApiError } from '../../shared/api';

interface UseSubmissionsOptions {
  filters?: SubmissionFilters;
  autoFetch?: boolean;
}

interface UseSubmissionsReturn {
  submissions: SubmissionResponse[];
  isLoading: boolean;
  isError: boolean;
  error: ApiError | null;
  refetch: () => Promise<void>;
  filteredSubmissions: SubmissionResponse[];
}

/**
 * Hook to fetch and manage submissions list
 * Supports filtering, sorting, and manual refetch
 */
export const useSubmissions = (options: UseSubmissionsOptions = {}): UseSubmissionsReturn => {
  const { filters, autoFetch = true } = options;
  
  const [submissions, setSubmissions] = useState<SubmissionResponse[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isError, setIsError] = useState<boolean>(false);
  const [error, setError] = useState<ApiError | null>(null);

  /**
   * Fetch submissions from API
   */
  const fetchSubmissions = useCallback(async () => {
    setIsLoading(true);
    setIsError(false);
    setError(null);

    try {
      const data = await submissionService.getSubmissions();
      setSubmissions(data);
    } catch (err) {
      setIsError(true);
      setError({
        message: formatApiError(err),
        statusCode: (err as ApiError).statusCode,
      });
    } finally {
      setIsLoading(false);
    }
  }, []);

  /**
   * Apply client-side filters to submissions
   */
  const applyFilters = useCallback((submissions: SubmissionResponse[]): SubmissionResponse[] => {
    if (!filters) return submissions;

    let filtered = [...submissions];

    // Filter by status
    if (filters.status !== undefined) {
      filtered = filtered.filter(sub => sub.status === filters.status);
    }

    // Filter by search term (searches in client name, email, caption, and message)
    if (filters.searchTerm) {
      const searchLower = filters.searchTerm.toLowerCase();
      filtered = filtered.filter(sub =>
        sub.clientName?.toLowerCase().includes(searchLower) ||
        sub.clientEmail.toLowerCase().includes(searchLower) ||
        sub.captions?.toLowerCase().includes(searchLower) ||
        sub.message?.toLowerCase().includes(searchLower)
      );
    }

    // Filter by date range
    if (filters.dateFrom) {
      filtered = filtered.filter(sub =>
        new Date(sub.createdAt) >= new Date(filters.dateFrom!)
      );
    }
    if (filters.dateTo) {
      filtered = filtered.filter(sub =>
        new Date(sub.createdAt) <= new Date(filters.dateTo!)
      );
    }

    // Sort submissions
    if (filters.sortBy) {
      filtered.sort((a, b) => {
        let comparison = 0;
        
        switch (filters.sortBy) {
          case 'createdAt':
            comparison = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
            break;
          case 'client':
            // Sort by clientName first, fall back to clientEmail if name is not available
            const aClient = a.clientName || a.clientEmail;
            const bClient = b.clientName || b.clientEmail;
            comparison = aClient.localeCompare(bClient);
            break;
          case 'status':
            comparison = a.status - b.status;
            break;
        }
        
        return filters.sortOrder === 'desc' ? -comparison : comparison;
      });
    }

    return filtered;
  }, [filters]);

  const filteredSubmissions = applyFilters(submissions);

  // Auto-fetch on mount if enabled
  useEffect(() => {
    if (autoFetch) {
      fetchSubmissions();
    }
  }, [autoFetch, fetchSubmissions]);

  return {
    submissions,
    isLoading,
    isError,
    error,
    refetch: fetchSubmissions,
    filteredSubmissions,
  };
};
