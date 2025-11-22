/**
 * ClientCard Component
 * Displays a clickable card with client information
 */

import { useNavigate } from 'react-router-dom';
import { Card } from '../common';
import type { Client } from '../../shared/types';

interface ClientCardProps {
  client: Client;
  onSelectForAi?: () => void;
}

export function ClientCard({ client, onSelectForAi }: ClientCardProps) {
  const navigate = useNavigate();

  const handleClick = () => {
    navigate(`/clients/${client.id}`);
  };

  // Determine what to display as the primary name
  const displayName = client.name || client.email;
  const showEmail = !client.name;
  const showCompany = !!client.companyName;

  return (
    <Card 
      className="hover:shadow-md transition-shadow cursor-pointer" 
      onClick={handleClick}
    >
      <div className="flex items-center gap-3">
        {/* Avatar */}
        <div className="flex-shrink-0">
          <div className="w-12 h-12 rounded-full bg-primary-100 flex items-center justify-center overflow-hidden">
            {client.profilePictureUrl ? (
              <img 
                src={client.profilePictureUrl} 
                alt={displayName} 
                className="w-full h-full object-cover"
              />
            ) : (
              <span className="text-primary-600 font-semibold text-lg">
                {displayName.charAt(0).toUpperCase()}
              </span>
            )}
          </div>
        </div>

        {/* Client Info */}
        <div className="flex-1 min-w-0">
          <h3 className="text-lg font-semibold text-gray-900 truncate dark:text-gray-100">
            {displayName}
          </h3>
          {showCompany && (
            <p className="text-sm text-gray-600 truncate dark:text-gray-300">
              {client.companyName}
            </p>
          )}
          {showEmail && showCompany && (
            <p className="text-sm text-gray-500 truncate">
              {client.email}
            </p>
          )}
          <p className="text-sm text-gray-500 mt-1">
            <span className="font-medium">{client.submissionCount}</span> submission{client.submissionCount !== 1 ? 's' : ''}
          </p>
        </div>

        {/* Actions */}
        <div className="flex-shrink-0 flex flex-col items-end gap-2">
          {onSelectForAi && (
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                onSelectForAi();
              }}
              className="inline-flex items-center rounded-full px-2.5 py-1 text-xs font-medium bg-purple-50 text-purple-700 hover:bg-purple-100"
            >
              <span className="mr-1">★</span>
              AI summary
            </button>
          )}
          <svg
            className="w-5 h-5 text-gray-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M9 5l7 7-7 7"
            />
          </svg>
        </div>
      </div>
    </Card>
  );
}
