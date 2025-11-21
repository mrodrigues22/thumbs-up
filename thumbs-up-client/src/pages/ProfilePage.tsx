/**
 * ProfilePage
 * User profile page where users can view and edit their personal information
 */

import { useState, useRef } from 'react';
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
  const [uploadingPicture, setUploadingPicture] = useState(false);
  const [previewUrl, setPreviewUrl] = useState<string | null>(user?.profilePictureUrl || null);
  const fileInputRef = useRef<HTMLInputElement>(null);

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

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate file type
    if (!file.type.startsWith('image/')) {
      toast.error('Please select an image file');
      return;
    }

    // Validate file size (5MB)
    if (file.size > 5 * 1024 * 1024) {
      toast.error('File size must be less than 5MB');
      return;
    }

    setUploadingPicture(true);
    setError('');

    try {
      const response = await authService.uploadProfilePicture(file);
      
      // Update preview and user state
      setPreviewUrl(response.profilePictureUrl);
      updateUser({ profilePictureUrl: response.profilePictureUrl });
      
      toast.success('Profile picture updated!');
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to upload profile picture';
      toast.error(errorMessage);
    } finally {
      setUploadingPicture(false);
    }
  };

  const handlePictureClick = () => {
    fileInputRef.current?.click();
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

          {/* Profile Picture Section */}
          <div className="mb-6 flex flex-col items-center">
            <div className="mb-4">
              <div 
                onClick={handlePictureClick}
                className="relative w-32 h-32 rounded-full overflow-hidden bg-gray-200 cursor-pointer hover:opacity-80 transition-opacity"
              >
                {uploadingPicture && (
                  <div className="absolute inset-0 flex items-center justify-center bg-black bg-opacity-50 z-10">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-white"></div>
                  </div>
                )}
                {previewUrl ? (
                  <img 
                    src={previewUrl} 
                    alt="Profile" 
                    className="w-full h-full object-cover"
                  />
                ) : (
                  <div className="w-full h-full flex items-center justify-center text-gray-400 text-4xl">
                    {user?.firstName?.[0]?.toUpperCase() || user?.email?.[0]?.toUpperCase() || '?'}
                  </div>
                )}
                <div className="absolute inset-0 flex items-center justify-center bg-black bg-opacity-0 hover:bg-opacity-30 transition-all">
                  <span className="text-white text-sm opacity-0 hover:opacity-100">Change</span>
                </div>
              </div>
            </div>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/*"
              onChange={handleFileChange}
              className="hidden"
            />
            <button
              type="button"
              onClick={handlePictureClick}
              className="text-sm text-primary hover:text-primary-dark font-medium"
            >
              Upload Profile Picture
            </button>
            <p className="text-xs text-gray-500 mt-1">JPG, PNG or GIF (max 5MB)</p>
          </div>

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
