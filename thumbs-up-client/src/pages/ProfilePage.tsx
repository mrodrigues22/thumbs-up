/**
 * ProfilePage
 * User profile page where users can view and edit their personal information
 */

import { useState } from 'react';
import { Layout } from '../components/layout';
import { Card, Button, Input, ErrorMessage } from '../components/common';
import { useAuth } from '../hooks/auth';
import { toast } from 'react-toastify';
import { authService } from '../services/authService';

export default function ProfilePage() {
  const { user, updateUser } = useAuth();
  const [formData, setFormData] = useState({
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    companyName: user?.companyName || '',
    email: user?.email || '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const updatedUser = await authService.updateProfile({
        firstName: formData.firstName,
        lastName: formData.lastName,
        companyName: formData.companyName,
      });
      
      updateUser(updatedUser);
      toast.success('Profile updated successfully!');
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to update profile';
      setError(errorMessage);
      toast.error(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Layout>
      <div className="max-w-2xl mx-auto py-8 px-4 sm:px-6 lg:px-8">
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900">Profile Settings</h1>
          <p className="mt-2 text-sm text-gray-600">
            Manage your personal information
          </p>
        </div>

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
              disabled
              placeholder="you@example.com"
            />

            <Input
              label="First Name"
              name="firstName"
              type="text"
              value={formData.firstName}
              onChange={(value) => setFormData({ ...formData, firstName: value })}
              placeholder="John"
            />

            <Input
              label="Last Name"
              name="lastName"
              type="text"
              value={formData.lastName}
              onChange={(value) => setFormData({ ...formData, lastName: value })}
              placeholder="Doe"
            />

            <Input
              label="Company Name"
              name="companyName"
              type="text"
              value={formData.companyName}
              onChange={(value) => setFormData({ ...formData, companyName: value })}
              placeholder="Acme Inc."
            />

            <div className="flex justify-end space-x-3 pt-4">
              <Button
                type="submit"
                variant="primary"
                disabled={loading}
              >
                {loading ? 'Saving...' : 'Save Changes'}
              </Button>
            </div>
          </form>
        </Card>
      </div>
    </Layout>
  );
}
