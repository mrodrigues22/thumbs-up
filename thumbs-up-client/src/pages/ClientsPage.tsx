/**
 * Clients Page
 * Manage client contacts with full CRUD operations
 */

import { useEffect, useState } from 'react';
import { Layout } from '../components/layout';
import { clientService } from '../services/clientService';
import { Button, Card, LoadingSpinner, ErrorMessage, Modal, Input } from '../components/common';
import type { Client, UpdateClientRequest } from '../shared/types';

export default function ClientsPage() {
  const [clients, setClients] = useState<Client[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingClient, setEditingClient] = useState<Client | null>(null);
  const [formData, setFormData] = useState({
    email: '',
    name: '',
    companyName: '',
  });
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    loadClients();
  }, []);

  const loadClients = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await clientService.getClients();
      setClients(data);
    } catch (err) {
      setError('Failed to load clients. Please try again.');
      console.error('Error loading clients:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleOpenModal = (client?: Client) => {
    if (client) {
      setEditingClient(client);
      setFormData({
        email: client.email,
        name: client.name || '',
        companyName: client.companyName || '',
      });
    } else {
      setEditingClient(null);
      setFormData({
        email: '',
        name: '',
        companyName: '',
      });
    }
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
    setEditingClient(null);
    setFormData({
      email: '',
      name: '',
      companyName: '',
    });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      if (editingClient) {
        const updateData: UpdateClientRequest = {
          email: formData.email,
          name: formData.name || undefined,
          companyName: formData.companyName || undefined,
        };
        await clientService.updateClient(editingClient.id, updateData);
      } else {
        await clientService.createClient(formData);
      }
      await loadClients();
      handleCloseModal();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to save client. Please try again.');
      console.error('Error saving client:', err);
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (client: Client) => {
    if (!confirm(`Are you sure you want to delete ${client.email}?`)) {
      return;
    }

    try {
      setError(null);
      await clientService.deleteClient(client.id);
      await loadClients();
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to delete client. Please try again.');
      console.error('Error deleting client:', err);
    }
  };

  const filteredClients = clients.filter(client => {
    const search = searchTerm.toLowerCase();
    return (
      client.email.toLowerCase().includes(search) ||
      client.name?.toLowerCase().includes(search) ||
      client.companyName?.toLowerCase().includes(search)
    );
  });

  const sortedClients = [...filteredClients].sort((a, b) => {
    return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
  });

  return (
    <Layout>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between mb-8">
          <div>
            <h1 className="text-3xl font-bold text-gray-900">Clients</h1>
            <p className="mt-2 text-sm text-gray-600">Manage your client contacts</p>
          </div>
          <div className="mt-4 sm:mt-0">
            <Button onClick={() => handleOpenModal()} variant="primary" size="medium">
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
                  d="M12 4v16m8-8H4"
                />
              </svg>
              Add Client
            </Button>
          </div>
        </div>

        {/* Error Message */}
        {error && (
          <div className="mb-6">
            <ErrorMessage error={error} />
          </div>
        )}

        {/* Search Bar */}
        <Card className="mb-6">
          <div className="flex items-center gap-3">
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
                d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
              />
            </svg>
            <input
              type="text"
              placeholder="Search clients by email, name, or company..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="flex-1 border-0 focus:ring-0 text-sm"
            />
          </div>
        </Card>

        {/* Clients List */}
        {loading ? (
          <div className="flex justify-center items-center py-12">
            <LoadingSpinner size="large" />
          </div>
        ) : sortedClients.length === 0 ? (
          <Card>
            <div className="text-center py-12">
              <svg
                className="mx-auto h-12 w-12 text-gray-400 mb-4"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"
                />
              </svg>
              <h3 className="text-lg font-medium text-gray-900 mb-1">No clients found</h3>
              <p className="text-gray-500 mb-4">
                {searchTerm ? 'Try adjusting your search criteria.' : 'Get started by adding your first client.'}
              </p>
              {!searchTerm && (
                <Button onClick={() => handleOpenModal()} variant="primary">
                  Add Your First Client
                </Button>
              )}
            </div>
          </Card>
        ) : (
          <div className="grid gap-4">
            {sortedClients.map((client) => (
              <Card key={client.id} className="hover:shadow-md transition-shadow">
                <div className="flex flex-col sm:flex-row justify-between items-start gap-4">
                  <div className="flex-1">
                    <div className="flex items-start gap-3">
                      <div className="flex-shrink-0">
                        <div className="w-12 h-12 rounded-full bg-primary-100 flex items-center justify-center">
                          <span className="text-primary-600 font-semibold text-lg">
                            {(client.name || client.email).charAt(0).toUpperCase()}
                          </span>
                        </div>
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 mb-1">
                          {client.name && (
                            <h3 className="text-lg font-semibold text-gray-900 truncate">
                              {client.name}
                            </h3>
                          )}
                        </div>
                        <p className="text-gray-600 mb-1">{client.email}</p>
                        {client.companyName && (
                          <p className="text-sm text-gray-500 mb-2">
                            <span className="font-medium">Company:</span> {client.companyName}
                          </p>
                        )}
                        <div className="flex flex-wrap gap-4 text-sm text-gray-500">
                          <span>
                            <span className="font-medium">Submissions:</span> {client.submissionCount}
                          </span>
                          <span>
                            <span className="font-medium">Created:</span>{' '}
                            {new Date(client.createdAt).toLocaleDateString()}
                          </span>
                          {client.lastUsedAt && (
                            <span>
                              <span className="font-medium">Last Used:</span>{' '}
                              {new Date(client.lastUsedAt).toLocaleDateString()}
                            </span>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                  <div className="flex gap-2 flex-shrink-0">
                    <Button
                      onClick={() => handleOpenModal(client)}
                      variant="ghost"
                      size="small"
                    >
                      Edit
                    </Button>
                    <Button
                      onClick={() => handleDelete(client)}
                      variant="ghost"
                      size="small"
                      className="text-red-600 hover:text-red-700 hover:bg-red-50"
                    >
                      Delete
                    </Button>
                  </div>
                </div>
              </Card>
            ))}
          </div>
        )}

        {/* Create/Edit Modal */}
        <Modal
          isOpen={isModalOpen}
          onClose={handleCloseModal}
          title={editingClient ? 'Edit Client' : 'Add New Client'}
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
                onClick={handleCloseModal}
                variant="ghost"
                disabled={submitting}
              >
                Cancel
              </Button>
              <Button type="submit" variant="primary" disabled={submitting}>
                {submitting ? 'Saving...' : editingClient ? 'Update Client' : 'Add Client'}
              </Button>
            </div>
          </form>
        </Modal>
      </div>
    </Layout>
  );
}
