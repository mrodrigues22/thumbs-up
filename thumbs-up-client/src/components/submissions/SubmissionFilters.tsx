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
    <div className="bg-white rounded-lg shadow-md p-4 mb-6">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-gray-900">Filters</h3>
        <button
          onClick={() => setIsExpanded(!isExpanded)}
          className="text-sm text-blue-600 hover:text-blue-700"
        >
          {isExpanded ? 'Hide' : 'Show'} filters
        </button>
      </div>

      {/* Search */}
      <div className="mb-4">
        <Input
          label=""
          name="search"
          type="text"
          value={filters.searchTerm || ''}
          onChange={(value) => handleFilterChange('searchTerm', value)}
          placeholder="Search by client email or message..."
        />
      </div>

      {isExpanded && (
        <div className="space-y-4">
          {/* Status Filter */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Status
            </label>
            <div className="flex flex-wrap gap-2">
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
                  className={`px-3 py-1 rounded-full text-sm font-medium transition-colors ${
                    filters.status === value
                      ? 'bg-blue-600 text-white'
                      : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>

          {/* Sort */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Sort By
              </label>
              <select
                value={filters.sortBy || 'createdAt'}
                onChange={(e) => handleFilterChange('sortBy', e.target.value)}
                className="input-field"
              >
                <option value="createdAt">Created Date</option>
                <option value="clientEmail">Client Email</option>
                <option value="status">Status</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Order
              </label>
              <select
                value={filters.sortOrder || 'desc'}
                onChange={(e) => handleFilterChange('sortOrder', e.target.value)}
                className="input-field"
              >
                <option value="desc">Newest First</option>
                <option value="asc">Oldest First</option>
              </select>
            </div>
          </div>

          {/* Date Range */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="dateFrom" className="block text-sm font-medium text-gray-700 mb-1">
                From Date
              </label>
              <input
                id="dateFrom"
                name="dateFrom"
                type="date"
                value={filters.dateFrom || ''}
                onChange={(e) => handleFilterChange('dateFrom', e.target.value)}
                className="input-field"
              />
            </div>
            <div>
              <label htmlFor="dateTo" className="block text-sm font-medium text-gray-700 mb-1">
                To Date
              </label>
              <input
                id="dateTo"
                name="dateTo"
                type="date"
                value={filters.dateTo || ''}
                onChange={(e) => handleFilterChange('dateTo', e.target.value)}
                className="input-field"
              />
            </div>
          </div>

          {/* Reset Button */}
          <div className="flex justify-end">
            <Button variant="secondary" size="small" onClick={handleReset}>
              Reset Filters
            </Button>
          </div>
        </div>
      )}
    </div>
  );
};
