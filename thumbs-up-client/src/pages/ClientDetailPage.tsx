/**
 * Client Detail Page
 * View and manage a single client's information
 */

import { useEffect, useState, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Layout } from '../components/layout';
import { clientService } from '../services/clientService';
import { submissionService } from '../services/submissionService';
import { Button, Card, LoadingSpinner, ErrorMessage, Modal, Input, ImageCropper } from '../components/common';
import { toast } from 'react-toastify';
import type { Client, UpdateClientRequest, SubmissionResponse, SubmissionStatus } from '../shared/types';

export default function ClientDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [client, setClient] = useState<Client | null>(null);
  const [submissions, setSubmissions] = useState<SubmissionResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingSubmissions, setLoadingSubmissions] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [formData, setFormData] = useState({
    email: '',
    name: '',
    companyName: '',
  });
  const [submitting, setSubmitting] = useState(false);
  const [uploadingPicture, setUploadingPicture] = useState(false);
  const [imageToCrop, setImageToCrop] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (id) {
      loadClient();
      loadSubmissions();
    }
  }, [id]);

  const loadClient = async () => {
    if (!id) return;
    
    try {
      setLoading(true);
      setError(null);
      const data = await clientService.getClient(id);
      setClient(data);
      setFormData({
        email: data.email,
        name: data.name || '',
        companyName: data.companyName || '',
      });
    } catch (err) {
      setError('Failed to load client details. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const loadSubmissions = async () => {
    if (!id) return;
    
    try {
      setLoadingSubmissions(true);
      const data = await submissionService.getSubmissionsByClient(id);
      setSubmissions(data);
    } catch (err: any) {
    } finally {
      setLoadingSubmissions(false);
    }
  };

  const handleOpenEditModal = () => {
    if (client) {
      setFormData({
        email: client.email,
        name: client.name || '',
        companyName: client.companyName || '',
      });
      setIsEditModalOpen(true);
    }
  };

  const handleCloseEditModal = () => {
    setIsEditModalOpen(false);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!client) return;

    setSubmitting(true);
    setError(null);

    try {
      const updateData: UpdateClientRequest = {
        email: formData.email,
        name: formData.name || undefined,
        companyName: formData.companyName || undefined,
      };
      await clientService.updateClient(client.id, updateData);
      await loadClient();
      handleCloseEditModal();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to update client. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async () => {
    if (!client) return;
    
    if (!confirm(`Are you sure you want to delete ${client.name || client.email}?`)) {
      return;
    }

    try {
      setError(null);
      await clientService.deleteClient(client.id);
      navigate('/clients');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to delete client. Please try again.');
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate file type
    if (!file.type.startsWith('image/')) {
      toast.error('Please select an image file');
      return;
    }

    // Validate file size (10MB before cropping)
    if (file.size > 10 * 1024 * 1024) {
      toast.error('File size must be less than 10MB');
      return;
    }

    // Read file and show cropper
    const reader = new FileReader();
    reader.onload = () => {
      setImageToCrop(reader.result as string);
    };
    reader.readAsDataURL(file);
    
    // Reset file input
    e.target.value = '';
  };

  const handleCropComplete = async (croppedImageBlob: Blob) => {
    if (!client) return;
    
    setImageToCrop(null);
    setUploadingPicture(true);
    setError(null);

    try {
      // Convert blob to file
      const file = new File([croppedImageBlob], 'client-picture.jpg', { type: 'image/jpeg' });
      
      const response = await clientService.uploadClientProfilePicture(client.id, file);
      
      // Update client data
      setClient({
        ...client,
        profilePictureUrl: response.profilePictureUrl,
      });
      
      toast.success('Profile picture updated!');
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to upload profile picture';
      toast.error(errorMessage);
    } finally {
      setUploadingPicture(false);
    }
  };

  const handleCropCancel = () => {
    setImageToCrop(null);
  };

  const handlePictureClick = () => {
    fileInputRef.current?.click();
  };

  const handleCreateSubmission = () => {
    navigate(`/submissions/new?clientId=${id}`);
  };

  const getStatusBadge = (status: SubmissionStatus) => {
    const badges = {
      0: { label: 'Pending', class: 'bg-yellow-100 text-yellow-800' },
      1: { label: 'Approved', class: 'bg-green-100 text-green-800' },
      2: { label: 'Rejected', class: 'bg-red-100 text-red-800' },
      3: { label: 'Expired', class: 'bg-gray-100 text-gray-800' },
    };
    const badge = badges[status as keyof typeof badges];
    return (
      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${badge.class}`}>
        {badge.label}
      </span>
    );
  };

  if (loading) {
    return (
      <Layout>
        <div className="flex justify-center items-center min-h-[60vh]">
          <LoadingSpinner size="large" />
        </div>
      </Layout>
    );
  }

  if (!client) {
    return (
      <Layout>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 dark:text-gray-100">
          <Card>
            <div className="text-center py-12">
              <h3 className="text-lg font-medium text-gray-900 mb-1 dark:text-gray-100">Client not found</h3>
              <p className="text-gray-500 mb-4 dark:text-gray-300">The requested client could not be found.</p>
              <Button onClick={() => navigate('/clients')} variant="primary">
                Back to Clients
              </Button>
            </div>
          </Card>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="mb-6">
          <button
            onClick={() => navigate('/clients')}
            className="flex items-center text-gray-600 hover:text-gray-900 mb-4"
          >
            <svg
              className="w-5 h-5 mr-2"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M15 19l-7-7 7-7"
              />
            </svg>
            Back to Clients
          </button>

          <div className="flex items-start justify-between">
            <div className="flex items-center gap-4">
              <div 
                onClick={handlePictureClick}
                className="relative w-16 h-16 rounded-full overflow-hidden bg-primary-100 flex items-center justify-center cursor-pointer hover:opacity-80 transition-opacity"
              >
                {uploadingPicture && (
                  <div className="absolute inset-0 flex items-center justify-center bg-black bg-opacity-50 z-10">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-white"></div>
                  </div>
                )}
                {client.profilePictureUrl ? (
                  <img 
                    src={client.profilePictureUrl} 
                    alt={client.name || client.email} 
                    className="w-full h-full object-cover"
                  />
                ) : (
                  <span className="text-primary-600 font-semibold text-2xl dark:text-gray-100">
                    {(client.name || client.email).charAt(0).toUpperCase()}
                  </span>
                )}
                <div className="absolute inset-0 flex items-center justify-center bg-black bg-opacity-0 hover:bg-opacity-30 transition-all">
                  <span className="text-white text-xs opacity-0 hover:opacity-100">Change</span>
                </div>
              </div>
              <div>
                <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100">
                  {client.name || client.email}
                </h1>
                {client.name && (
                  <p className="text-gray-600 mt-1">{client.email}</p>
                )}
              </div>
            </div>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/*"
              onChange={handleFileChange}
              className="hidden"
            />
          </div>
        </div>

        {/* Error Message */}
        {error && (
          <div className="mb-6">
            <ErrorMessage error={error} />
          </div>
        )}

        {/* Submissions Section */}
        <Card className="mb-6">
          <div className="px-4 py-5 sm:p-6">
            <div className="flex justify-between items-center mb-6">
              <div>
                <h2 className="text-lg font-medium text-gray-900 dark:text-gray-100">Submissions</h2>
                <p className="mt-1 text-sm text-gray-500">
                  All submissions created for this client
                </p>
              </div>
              <Button onClick={handleCreateSubmission} variant="primary">
                <svg
                  className="w-4 h-4 mr-2"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 4v16m8-8H4"
                  />
                </svg>
                New Submission
              </Button>
            </div>

            {loadingSubmissions ? (
              <div className="flex justify-center py-8">
                <LoadingSpinner size="medium" />
              </div>
            ) : submissions.length === 0 ? (
              <div className="text-center py-12 bg-gray-50 rounded-lg dark:bg-gray-700">
                <svg
                  className="mx-auto h-12 w-12 text-gray-400"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                  />
                </svg>
                <h3 className="mt-2 text-sm font-medium text-gray-900 dark:text-gray-100">No submissions</h3>
                <p className="mt-1 text-sm text-gray-500 dark:text-gray-300">
                  Get started by creating a new submission for this client.
                </p>
                <div className="mt-6">
                  <Button onClick={handleCreateSubmission} variant="primary">
                    <svg
                      className="w-4 h-4 mr-2"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M12 4v16m8-8H4"
                      />
                    </svg>
                    New Submission
                  </Button>
                </div>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th
                        scope="col"
                        className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                      >
                        Status
                      </th>
                      <th
                        scope="col"
                        className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                      >
                        Message
                      </th>
                      <th
                        scope="col"
                        className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                      >
                        Files
                      </th>
                      <th
                        scope="col"
                        className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                      >
                        Created
                      </th>
                      <th
                        scope="col"
                        className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                      >
                        Expires
                      </th>
                      <th scope="col" className="relative px-6 py-3">
                        <span className="sr-only">Actions</span>
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {submissions.map((submission) => (
                      <tr
                        key={submission.id}
                        className="hover:bg-gray-50"
                      >
                        <td className="px-6 py-4 whitespace-nowrap">
                          {getStatusBadge(submission.status)}
                        </td>
                        <td className="px-6 py-4">
                          <div className="text-sm text-gray-900 max-w-xs truncate">
                            {submission.message || 'No message'}
                          </div>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {submission.mediaFiles.length} file{submission.mediaFiles.length !== 1 ? 's' : ''}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {new Date(submission.createdAt).toLocaleDateString()}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {new Date(submission.expiresAt).toLocaleDateString()}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                          <Button
                            onClick={() => navigate(`/submissions/${submission.id}`)}
                            variant="ghost"
                            size="small"
                          >
                            View
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </Card>

        {/* Client Details */}
        <Card>
          <div className="px-4 py-5 sm:p-6">
            <div className="flex justify-between items-start mb-6">
              <h2 className="text-lg font-medium text-gray-900 dark:text-gray-100">Client Information</h2>
              <div className="flex gap-2">
                <Button onClick={handleOpenEditModal} variant="ghost" size="small">
                  <svg
                    className="w-4 h-4 mr-1"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                    />
                  </svg>
                  Edit
                </Button>
                <Button
                  onClick={handleDelete}
                  variant="ghost"
                  size="small"
                  className="text-red-600 hover:text-red-700 hover:bg-red-50"
                >
                  <svg
                    className="w-4 h-4 mr-1"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                    />
                  </svg>
                  Delete
                </Button>
              </div>
            </div>

            <dl className="grid grid-cols-1 gap-x-4 gap-y-6 sm:grid-cols-2">
              {client.name && (
                <div>
                  <dt className="text-sm font-medium text-gray-500 dark:text-gray-100">Name</dt>
                  <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">{client.name}</dd>
                </div>
              )}
              <div>
                <dt className="text-sm font-medium text-gray-500 dark:text-gray-100">Email</dt>
                <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">{client.email}</dd>
              </div>
              {client.companyName && (
                <div>
                  <dt className="text-sm font-medium text-gray-500 dark:text-gray-100">Company</dt>
                  <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">{client.companyName}</dd>
                </div>
              )}
              <div>
                <dt className="text-sm font-medium text-gray-500 dark:text-gray-100">Submissions</dt>
                <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">{client.submissionCount}</dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500 dark:text-gray-100">Created</dt>
                <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">
                  {new Date(client.createdAt).toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                  })}
                </dd>
              </div>
              {client.lastUsedAt && (
                <div>
                  <dt className="text-sm font-medium text-gray-500">Last Used</dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {new Date(client.lastUsedAt).toLocaleDateString('en-US', {
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric',
                    })}
                  </dd>
                </div>
              )}
            </dl>
          </div>
        </Card>

        {/* Edit Modal */}
        <Modal
          isOpen={isEditModalOpen}
          onClose={handleCloseEditModal}
          title="Edit Client"
        >
          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Profile Picture Section */}
            <div className="mb-6 flex flex-col items-center">
              <div className="mb-4">
                <div 
                  onClick={handlePictureClick}
                  className="relative w-24 h-24 rounded-full overflow-hidden bg-gray-200 cursor-pointer hover:opacity-80 transition-opacity"
                >
                  {uploadingPicture && (
                    <div className="absolute inset-0 flex items-center justify-center bg-black bg-opacity-50 z-10">
                      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-white"></div>
                    </div>
                  )}
                  {client?.profilePictureUrl ? (
                    <img 
                      src={client.profilePictureUrl} 
                      alt="Profile" 
                      className="w-full h-full object-cover"
                    />
                  ) : (
                    <div className="w-full h-full flex items-center justify-center text-gray-400 text-4xl">
                      {client?.name?.[0]?.toUpperCase() || client?.email?.[0]?.toUpperCase() || '?'}
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
              <p className="text-xs text-gray-500 mt-1">JPG, PNG or GIF (max 10MB)</p>
            </div>

            <Input
              label="Email"
              name="email"
              type="email"
              required
              value={formData.email}
              onChange={(value) => setFormData({ ...formData, email: value })}
              placeholder="client@example.com"
            />

            <Input
              label="Name"
              name="name"
              type="text"
              value={formData.name}
              onChange={(value) => setFormData({ ...formData, name: value })}
              placeholder="John Doe"
            />

            <Input
              label="Company Name"
              name="companyName"
              type="text"
              value={formData.companyName}
              onChange={(value) => setFormData({ ...formData, companyName: value })}
              placeholder="Acme Corporation"
            />

            <div className="flex justify-end gap-3 pt-4">
              <Button
                type="button"
                onClick={handleCloseEditModal}
                variant="ghost"
                disabled={submitting}
              >
                Cancel
              </Button>
              <Button type="submit" variant="primary" disabled={submitting}>
                {submitting ? 'Saving...' : 'Update Client'}
              </Button>
            </div>
          </form>
        </Modal>

        {/* Image Cropper Modal */}
        {imageToCrop && (
          <ImageCropper
            image={imageToCrop}
            onCropComplete={handleCropComplete}
            onCancel={handleCropCancel}
            aspectRatio={1}
            cropShape="round"
          />
        )}
      </div>
    </Layout>
  );
}
