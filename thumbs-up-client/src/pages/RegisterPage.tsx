/**
 * RegisterPage
 * User registration page with improved UI
 */

import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Layout } from '../components/layout';
import { Card, Button, Input, ErrorMessage } from '../components/common';
import { useAuth } from '../hooks/auth';
import { toast } from 'react-toastify';

export default function RegisterPage() {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    companyName: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const { register } = useAuth();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      await register(
        formData.email,
        formData.password,
        formData.firstName,
        formData.lastName,
        formData.companyName
      );
      toast.success('Account created successfully!');
      navigate('/dashboard');
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Registration failed';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Layout showNavbar={false}>
      <div className="min-h-screen flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8 bg-gray-50">
        <div className="max-w-md w-full">
          {/* Header */}
          <div className="text-center mb-8">
            <span className="text-6xl">👍</span>
            <h2 className="mt-4 text-3xl font-extrabold text-gray-900">
              Thumbs Up
            </h2>
            <p className="mt-2 text-sm text-gray-600">
              Create your account
            </p>
          </div>

          {/* Register Card */}
          <Card>
            {error && (
              <ErrorMessage error={error} className="mb-4" />
            )}

            <form onSubmit={handleSubmit} className="space-y-6">
              <Input
                label="Email Address"
                name="email"
                type="email"
                value={formData.email}
                onChange={(value) => setFormData({ ...formData, email: value })}
                required
                placeholder="you@example.com"
                autoComplete="email"
              />

              <Input
                label="Password"
                name="password"
                type="password"
                value={formData.password}
                onChange={(value) => setFormData({ ...formData, password: value })}
                required
                placeholder="Minimum 6 characters"
                helperText="Must be at least 6 characters long"
                autoComplete="new-password"
              />

              <div className="grid grid-cols-2 gap-4">
                <Input
                  label="First Name"
                  name="firstName"
                  type="text"
                  value={formData.firstName}
                  onChange={(value) => setFormData({ ...formData, firstName: value })}
                  placeholder="John"
                  autoComplete="given-name"
                />

                <Input
                  label="Last Name"
                  name="lastName"
                  type="text"
                  value={formData.lastName}
                  onChange={(value) => setFormData({ ...formData, lastName: value })}
                  placeholder="Doe"
                  autoComplete="family-name"
                />
              </div>

              <Input
                label="Company Name (Optional)"
                name="companyName"
                type="text"
                value={formData.companyName}
                onChange={(value) => setFormData({ ...formData, companyName: value })}
                placeholder="Acme Corporation"
                autoComplete="organization"
              />

              <Button
                type="submit"
                variant="primary"
                fullWidth
                loading={loading}
                disabled={loading}
              >
                Create Account
              </Button>
            </form>

            <div className="mt-6 text-center">
              <p className="text-sm text-gray-600">
                Already have an account?{' '}
                <Link to="/login" className="font-medium text-blue-600 hover:text-blue-500">
                  Sign in here
                </Link>
              </p>
            </div>
          </Card>
        </div>
      </div>
    </Layout>
  );
}
