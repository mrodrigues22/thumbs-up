/**
 * ProtectedRoute Component
 * Wraps routes that require authentication
 */

import { Navigate } from 'react-router-dom';
import { useAuth } from '../../hooks/auth';
import { LoadingSpinner } from '../common';

interface ProtectedRouteProps {
  children: React.ReactElement;
  requireRole?: string;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requireRole,
}) => {
  const { isAuthenticated, isLoading, hasRole } = useAuth();

  // Show loading while checking authentication
  if (isLoading) {
    return <LoadingSpinner fullScreen size="large" />;
  }

  // Redirect to login if not authenticated
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // Check role if required (future feature)
  if (requireRole && !hasRole(requireRole)) {
    return <Navigate to="/dashboard" replace />;
  }

  return children;
};
