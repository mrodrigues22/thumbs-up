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
                to="/clients"
                className={`transition-colors font-medium ${
                  location.pathname === '/clients'
                    ? 'text-primary'
                    : 'text-gray-700 hover:text-primary'
                }`}
              >
                Clients
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
                <Link 
                  to="/profile" 
                  className="flex items-center space-x-2 text-sm text-gray-600 hover:text-primary transition-colors"
                >
                  {user?.profilePictureUrl ? (
                    <img 
                      src={user.profilePictureUrl} 
                      alt="Profile" 
                      className="w-8 h-8 rounded-full object-cover border-2 border-gray-200"
                    />
                  ) : (
                    <div className="w-8 h-8 rounded-full bg-gray-200 flex items-center justify-center text-gray-600 text-sm font-medium border-2 border-gray-200">
                      {user?.firstName?.[0]?.toUpperCase() || user?.email?.[0]?.toUpperCase() || '?'}
                    </div>
                  )}
                  <span>{user?.firstName || user?.email}</span>
                </Link>
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
