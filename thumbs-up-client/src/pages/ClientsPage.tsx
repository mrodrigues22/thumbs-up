/**
 * Clients Page
 * Manage client contacts with full CRUD operations
 */

import { useEffect, useState } from 'react';
import { Layout } from '../components/layout';
import { clientService } from '../services/clientService';
import { Button, Card, ErrorMessage, Modal, Input } from '../components/common';
import { ClientsList } from '../components/clients';
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
              className="flex-1 border-0 focus:ring-0 text-sm bg-transparent placeholder-gray-400"
            />
          </div>
        </Card>

        {/* Clients List */}
        <ClientsList
          clients={sortedClients}
          loading={loading}
          searchTerm={searchTerm}
          onAddClient={() => handleOpenModal()}
        />

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
