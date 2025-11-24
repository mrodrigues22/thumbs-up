/**
 * SubmissionFilters Component
 * Filter and sort submissions
 */

import { useState } from 'react';
import type { SubmissionFilters as Filters } from '../../shared/types';
import { Button, Input } from '../common';
import { SubmissionStatus as Status } from '../../shared/types';

interface SubmissionFiltersProps {
  onFiltersChange: (filters: Filters) => void;
  initialFilters?: Filters;
}

export const SubmissionFilters: React.FC<SubmissionFiltersProps> = ({
  onFiltersChange,
  initialFilters,
}) => {
  const [filters, setFilters] = useState<Filters>(initialFilters || {});
  const [isExpanded, setIsExpanded] = useState(false);

  const handleFilterChange = (key: keyof Filters, value: unknown) => {
    const newFilters = { ...filters, [key]: value };
    setFilters(newFilters);
    onFiltersChange(newFilters);
  };

  const handleReset = () => {
    setFilters({});
    onFiltersChange({});
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-sm border border-gray-200 dark:border-gray-700 p-3 mb-4">
      {/* Compact Search Bar */}
      <div className="flex items-center gap-2">
        <div className="flex-1">
          <Input
            label=""
            name="search"
            type="text"
            value={filters.searchTerm || ''}
            onChange={(value) => handleFilterChange('searchTerm', value)}
            placeholder="Search submissions..."
          />
        </div>
        <button
          onClick={() => setIsExpanded(!isExpanded)}
          className="px-3 py-2 text-sm text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-gray-700 rounded-md transition-colors whitespace-nowrap flex items-center gap-1"
        >
          <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4" />
          </svg>
          {isExpanded ? 'Less' : 'More'}
        </button>
      </div>

      {/* Status Pills - Always Visible */}
      <div className="flex flex-wrap gap-1.5 mt-2">
        {[
          { value: undefined, label: 'All' },
          { value: Status.Pending, label: 'Pending' },
          { value: Status.Approved, label: 'Approved' },
          { value: Status.Rejected, label: 'Rejected' },
          { value: Status.Expired, label: 'Expired' },
        ].map(({ value, label }) => (
          <button
            key={label}
            onClick={() => handleFilterChange('status', value)}
            className={`px-2.5 py-1 rounded-full text-xs font-medium transition-colors ${
              filters.status === value
                ? 'bg-blue-600 text-white'
                : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      {/* Expanded Filters */}
      {isExpanded && (
        <div className="mt-3 pt-3 border-t border-gray-200 dark:border-gray-700 space-y-3">
          {/* Date Range */}
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="dateFrom" className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                From
              </label>
              <input
                id="dateFrom"
                name="dateFrom"
                type="date"
                value={filters.dateFrom || ''}
                onChange={(e) => handleFilterChange('dateFrom', e.target.value)}
                className="input-field text-sm"
              />
            </div>
            <div>
              <label htmlFor="dateTo" className="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
                To
              </label>
              <input
                id="dateTo"
                name="dateTo"
                type="date"
                value={filters.dateTo || ''}
                onChange={(e) => handleFilterChange('dateTo', e.target.value)}
                className="input-field text-sm"
              />
            </div>
          </div>

          {/* Reset Button */}
          <div className="flex justify-end">
            <Button variant="secondary" size="small" onClick={handleReset}>
              Reset
            </Button>
          </div>
        </div>
      )}
    </div>
  );
};
