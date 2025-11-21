/**
 * Client Detail Page
 * View and manage a single client's information
 */

import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Layout } from '../components/layout';
import { clientService } from '../services/clientService';
import { Button, Card, LoadingSpinner, ErrorMessage, Modal, Input } from '../components/common';
import type { Client, UpdateClientRequest } from '../shared/types';

export default function ClientDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [client, setClient] = useState<Client | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [formData, setFormData] = useState({
    email: '',
    name: '',
    companyName: '',
  });
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (id) {
      loadClient();
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
      console.error('Error loading client:', err);
    } finally {
      setLoading(false);
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
      console.error('Error updating client:', err);
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
      console.error('Error deleting client:', err);
    }
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
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <Card>
            <div className="text-center py-12">
              <h3 className="text-lg font-medium text-gray-900 mb-1">Client not found</h3>
              <p className="text-gray-500 mb-4">The requested client could not be found.</p>
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
              <div className="w-16 h-16 rounded-full bg-primary-100 flex items-center justify-center">
                <span className="text-primary-600 font-semibold text-2xl">
                  {(client.name || client.email).charAt(0).toUpperCase()}
                </span>
              </div>
              <div>
                <h1 className="text-3xl font-bold text-gray-900">
                  {client.name || client.email}
                </h1>
                {client.name && (
                  <p className="text-gray-600 mt-1">{client.email}</p>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Error Message */}
        {error && (
          <div className="mb-6">
            <ErrorMessage error={error} />
          </div>
        )}

        {/* Client Details */}
        <Card className="mb-6">
          <div className="px-4 py-5 sm:p-6">
            <div className="flex justify-between items-start mb-6">
              <h2 className="text-lg font-medium text-gray-900">Client Information</h2>
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
                  <dt className="text-sm font-medium text-gray-500">Name</dt>
                  <dd className="mt-1 text-sm text-gray-900">{client.name}</dd>
                </div>
              )}
              <div>
                <dt className="text-sm font-medium text-gray-500">Email</dt>
                <dd className="mt-1 text-sm text-gray-900">{client.email}</dd>
              </div>
              {client.companyName && (
                <div>
                  <dt className="text-sm font-medium text-gray-500">Company</dt>
                  <dd className="mt-1 text-sm text-gray-900">{client.companyName}</dd>
                </div>
              )}
              <div>
                <dt className="text-sm font-medium text-gray-500">Submissions</dt>
                <dd className="mt-1 text-sm text-gray-900">{client.submissionCount}</dd>
              </div>
              <div>
                <dt className="text-sm font-medium text-gray-500">Created</dt>
                <dd className="mt-1 text-sm text-gray-900">
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
      </div>
    </Layout>
  );
}
