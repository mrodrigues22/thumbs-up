/**
 * Navbar Component
 * Main navigation bar with user menu
 */

import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../hooks/auth';
import { Button } from '../common';

export const Navbar: React.FC = () => {
  const { user, isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav className="bg-white shadow-md">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          {/* Logo/Brand */}
          <div className="flex items-center">
            <Link to="/dashboard" className="flex items-center space-x-2">
                        <img 
                            src="/logo.svg" 
                            className="h-8 cursor-pointer" 
                            alt="Logo" 
                        />
            </Link>
          </div>

          {/* Navigation Links */}
          {isAuthenticated && (
            <div className="hidden md:flex items-center space-x-6">
              <Link
                to="/dashboard"
                className={`transition-colors font-medium ${
                  location.pathname === '/dashboard'
                    ? 'text-primary'
                    : 'text-gray-700 hover:text-primary'
                }`}
              >
                Dashboard
              </Link>
              <Link
                to="/submissions/create"
                className={`transition-colors font-medium ${
                  location.pathname === '/submissions/create'
                    ? 'text-primary'
                    : 'text-gray-700 hover:text-primary'
                }`}
              >
                New Submission
              </Link>
            </div>
          )}

          {/* User Menu */}
          <div className="flex items-center space-x-4">
            {isAuthenticated ? (
              <>
                <span className="text-sm text-gray-600">
                  {user?.firstName || user?.email}
                </span>
                <Button onClick={handleLogout} variant="ghost" size="small">
                  Logout
                </Button>
              </>
            ) : (
              <div className="space-x-2">
                <Button onClick={() => navigate('/login')} variant="ghost" size="small">
                  Login
                </Button>
                <Button onClick={() => navigate('/register')} variant="primary" size="small">
                  Register
                </Button>
              </div>
            )}
          </div>
        </div>
      </div>
    </nav>
  );
};
