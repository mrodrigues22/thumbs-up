/**
 * App Component
 * Main application router with protected routes
 */

import { useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';
import { useAuthStore } from './stores/authStore';
import { ProtectedRoute } from './components/layout';
import { LoadingSpinner } from './components/common';

// Pages
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import SubmissionsPage from './pages/SubmissionsPage';
import SubmissionDetailPage from './pages/SubmissionDetailPage';
import CreateSubmissionPage from './pages/CreateSubmissionPage';
import ClientReviewPage from './pages/ClientReviewPage';

function App() {
  const { loadAuth, isLoading } = useAuthStore((state) => ({
    loadAuth: state.loadAuth,
    isLoading: state.isLoading,
  }));

  useEffect(() => {
    loadAuth();
  }, [loadAuth]);

  if (isLoading) {
    return <LoadingSpinner fullScreen size="large" />;
  }

  return (
    <>
      <BrowserRouter>
        <Routes>
          {/* Public Routes */}
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/review/:token" element={<ClientReviewPage />} />

          {/* Protected Routes */}
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <SubmissionsPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/submissions"
            element={
              <ProtectedRoute>
                <SubmissionsPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/submissions/create"
            element={
              <ProtectedRoute>
                <CreateSubmissionPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/submissions/:id"
            element={
              <ProtectedRoute>
                <SubmissionDetailPage />
              </ProtectedRoute>
            }
          />

          {/* Default Route */}
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          
          {/* 404 Route */}
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </BrowserRouter>

      {/* Toast Notifications */}
      <ToastContainer
        position="top-right"
        autoClose={3000}
        hideProgressBar={false}
        newestOnTop
        closeOnClick
        rtl={false}
        pauseOnFocusLoss
        draggable
        pauseOnHover
        theme="light"
      />
    </>
  );
}

export default App;
