import { useState, useRef, useEffect } from 'react';
import type { Client } from '../../shared/types';

interface ClientSelectorProps {
  clients: Client[];
  selectedClientId?: string;
  onSelect: (client: Client | null) => void;
  placeholder?: string;
  disabled?: boolean;
}

export function ClientSelector({
  clients,
  selectedClientId,
  onSelect,
  placeholder = 'Select a client...',
  disabled = false,
}: ClientSelectorProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const dropdownRef = useRef<HTMLDivElement>(null);
  
  const selectedClient = clients.find(c => c.id === selectedClientId);
  
  // Filter clients based on search term
  const filteredClients = clients.filter(client => {
    const searchLower = searchTerm.toLowerCase();
    return (
      client.email.toLowerCase().includes(searchLower) ||
      client.name.toLowerCase().includes(searchLower) ||
      client.companyName?.toLowerCase().includes(searchLower)
    );
  });

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => document.removeEventListener('mousedown', handleClickOutside);
    }
  }, [isOpen]);

  const handleSelect = (client: Client) => {
    onSelect(client);
    setIsOpen(false);
    setSearchTerm('');
  };

  const handleClear = () => {
    onSelect(null);
    setSearchTerm('');
  };

  return (
    <div className="relative" ref={dropdownRef}>
      <label className="block text-sm font-medium text-gray-700 mb-2 dark:text-gray-300">
        Select Client
      </label>
      
      {/* Selected client display / trigger */}
      <div
        onClick={() => !disabled && setIsOpen(!isOpen)}
        className={`w-full px-3 py-2 border rounded-md bg-white cursor-pointer flex items-center justify-between dark:bg-gray-700 ${
          disabled ? 'bg-gray-100 cursor-not-allowed dark:bg-gray-600' : 'hover:border-gray-400 dark:hover:border-gray-500'
        } ${isOpen ? 'border-blue-500 ring-1 ring-blue-500 dark:ring-blue-500' : 'border-gray-300: dark:border-gray-600'}`}
      >
        <div className="flex-1 min-w-0">
          {selectedClient ? (
            <div>
              <div className="font-medium text-gray-900 truncate dark:text-gray-100">
                {selectedClient.name}
              </div>
              <div className="text-sm text-gray-500 truncate dark:text-gray-300">
                {selectedClient.email}
              </div>
            </div>
          ) : (
            <span className="text-gray-500 dark:text-gray-400">{placeholder}</span>
          )}
        </div>
        
        <div className="flex items-center gap-2 ml-2">
          {selectedClient && !disabled && (
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                handleClear();
              }}
              className="text-gray-400 hover:text-gray-600"
            >
              <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          )}
          
          <svg
            className={`w-5 h-5 text-gray-400 transition-transform ${isOpen ? 'transform rotate-180' : ''}`}
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
          </svg>
        </div>
      </div>

      {/* Dropdown menu */}
      {isOpen && !disabled && (
        <div className="absolute z-10 w-full mt-1 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md shadow-lg max-h-80 overflow-hidden">
          {/* Search input */}
          <div className="p-2 border-b border-gray-200">
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Search clients..."
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-100"
              autoFocus
            />
          </div>

          {/* Client list */}
          <div className="overflow-y-auto max-h-64">
            {filteredClients.length === 0 ? (
              <div className="px-3 py-4 text-center text-gray-500">
                No clients found
              </div>
            ) : (
              filteredClients.map((client) => (
                <button
                  key={client.id}
                  type="button"
                  onClick={() => handleSelect(client)}
                  className={`w-full px-3 py-2 text-left hover:bg-gray-100 flex items-start gap-2 dark:hover:bg-gray-600 ${
                    client.id === selectedClientId ? 'bg-blue-50 dark:bg-blue-900/30' : ''
                  }`}
                >
                  <div className="flex-1 min-w-0">
                    <div className="font-medium text-gray-900 truncate dark:text-gray-100">
                      {client.name}
                    </div>
                    <div className="text-sm text-gray-600 truncate dark:text-gray-400">
                      {client.email}
                    </div>
                    {client.companyName && (
                      <div className="text-xs text-gray-500 truncate">
                        {client.companyName}
                      </div>
                    )}
                  </div>
                  
                  {client.id === selectedClientId && (
                    <svg className="w-5 h-5 text-blue-600 flex-shrink-0 mt-1" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                    </svg>
                  )}
                </button>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}
