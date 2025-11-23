/**
 * Clients Page
 * Manage client contacts with full CRUD operations
 */

import { useEffect, useState, useRef } from 'react';
import { Layout } from '../components/layout';
import { clientService } from '../services/clientService';
import { aiService } from '../services/aiService';
import { Button, Card, ErrorMessage, Modal, Input, LoadingSpinner } from '../components/common';
import { ClientsList } from '../components/clients';
import type { Client, UpdateClientRequest, ClientSummaryResponse } from '../shared/types';

export default function ClientsPage() {
  const [clients, setClients] = useState<Client[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedClient, setSelectedClient] = useState<Client | null>(null);
  const [aiSummary, setAiSummary] = useState<ClientSummaryResponse | null>(null);
  const [aiLoading, setAiLoading] = useState(false);
  const [aiError, setAiError] = useState<string | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingClient, setEditingClient] = useState<Client | null>(null);
  const [formData, setFormData] = useState({
    email: '',
    name: '',
    companyName: '',
  });
  const [submitting, setSubmitting] = useState(false);
  const summaryAbortRef = useRef<AbortController | null>(null);

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

  useEffect(() => {
    return () => {
      summaryAbortRef.current?.abort();
    };
  }, []);

  const loadClientSummary = async (client: Client) => {
    summaryAbortRef.current?.abort();
    const controller = new AbortController();
    summaryAbortRef.current = controller;

    try {
      setAiLoading(true);
      setAiError(null);
      const summary = await aiService.getClientSummary(client.id, controller.signal);
      if (controller.signal.aborted) {
        return;
      }
      setAiSummary(summary);
    } catch (err) {
      if (controller.signal.aborted) {
        return;
      }
      console.error('Error loading AI summary:', err);
      setAiError('Failed to load AI summary.');
      setAiSummary(null);
    } finally {
      if (!controller.signal.aborted) {
        setAiLoading(false);
      }
      if (summaryAbortRef.current === controller) {
        summaryAbortRef.current = null;
      }
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

  const handleSelectClient = (client: Client | null) => {
    setSelectedClient(client);
    setAiSummary(null);
    setAiError(null);
    if (client) {
      void loadClientSummary(client);
    }
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

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 items-start">
          <div className="lg:col-span-2">
            <ClientsList
              clients={sortedClients}
              loading={loading}
              searchTerm={searchTerm}
              onAddClient={() => handleOpenModal()}
              onSelectClient={handleSelectClient}
            />
          </div>

          <div className="lg:col-span-1">
            <Card className="h-full">
              <h2 className="text-lg font-semibold text-gray-900 mb-2">AI Client Summary</h2>
              <p className="text-xs text-gray-500 mb-3">
                Uses your past approvals/rejections and content to describe this client&apos;s preferences.
              </p>

              {!selectedClient && (
                <p className="text-sm text-gray-500">
                  Select a client from the list to see their AI summary.
                </p>
              )}

              {selectedClient && (
                <div className="space-y-3">
                  <p className="text-sm font-medium text-gray-900">
                    {selectedClient.name || selectedClient.email}
                  </p>

                  {aiLoading && (
                    <div className="flex items-center gap-2 text-sm text-gray-500">
                      <LoadingSpinner size="small" />
                      <span>Analyzing client history...</span>
                    </div>
                  )}

                  {aiError && !aiLoading && (
                    <ErrorMessage error={aiError} />
                  )}

                  {aiSummary && !aiLoading && !aiError && (
                    <div className="border-t pt-3 mt-2 space-y-4">
                      {/* Stats */}
                      <div className="flex gap-4 text-xs">
                        <div className="flex items-center gap-1">
                          <span className="font-semibold text-green-600">{aiSummary.approvedCount}</span>
                          <span className="text-gray-500">approved</span>
                        </div>
                        <div className="flex items-center gap-1">
                          <span className="font-semibold text-red-600">{aiSummary.rejectedCount}</span>
                          <span className="text-gray-500">rejected</span>
                        </div>
                      </div>

                      {/* Style Preferences */}
                      {aiSummary.stylePreferences.length > 0 && (
                        <div>
                          <h3 className="text-xs font-semibold text-purple-700 uppercase tracking-wide mb-1.5">
                            Style Preferences
                          </h3>
                          <ul className="space-y-1">
                            {aiSummary.stylePreferences.map((item, idx) => (
                              <li key={idx} className="text-sm text-gray-700 flex items-start gap-2">
                                <span className="text-purple-500 mt-0.5">•</span>
                                <span>{item}</span>
                              </li>
                            ))}
                          </ul>
                        </div>
                      )}

                      {/* Recurring Positives */}
                      {aiSummary.recurringPositives.length > 0 && (
                        <div>
                          <h3 className="text-xs font-semibold text-green-700 uppercase tracking-wide mb-1.5">
                            Recurring Positives
                          </h3>
                          <ul className="space-y-1">
                            {aiSummary.recurringPositives.map((item, idx) => (
                              <li key={idx} className="text-sm text-gray-700 flex items-start gap-2">
                                <span className="text-green-500 mt-0.5">✓</span>
                                <span>{item}</span>
                              </li>
                            ))}
                          </ul>
                        </div>
                      )}

                      {/* Rejection Reasons */}
                      {aiSummary.rejectionReasons.length > 0 && (
                        <div>
                          <h3 className="text-xs font-semibold text-red-700 uppercase tracking-wide mb-1.5">
                            Common Rejections
                          </h3>
                          <ul className="space-y-1">
                            {aiSummary.rejectionReasons.map((item, idx) => (
                              <li key={idx} className="text-sm text-gray-700 flex items-start gap-2">
                                <span className="text-red-500 mt-0.5">✗</span>
                                <span>{item}</span>
                              </li>
                            ))}
                          </ul>
                        </div>
                      )}

                      <p className="text-xs text-gray-400 pt-2 border-t">
                        Updated {new Date(aiSummary.generatedAt).toLocaleString()}
                      </p>
                    </div>
                  )}
                </div>
              )}
            </Card>
          </div>
        </div>

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
