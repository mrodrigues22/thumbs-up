import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { Layout } from '../components/layout';
import { Card, Button, Input, Textarea, ErrorMessage, ClientSelector } from '../components/common';
import { useCreateSubmission } from '../hooks/submissions';
import { useClients, clientKeys } from '../hooks/clients/useClients';
import { submissionService } from '../services/submissionService';
import { toast } from 'react-toastify';

type ClientMode = 'existing' | 'new' | 'quick';

export default function CreateSubmissionPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  useCreateSubmission();
  const { data: clients = [], isLoading: clientsLoading } = useClients();
  
  const [clientMode, setClientMode] = useState<ClientMode>('existing');
  const [selectedClientId, setSelectedClientId] = useState<string>();
  const [formData, setFormData] = useState({
    clientEmail: '',
    clientName: '',
    message: '',
    captions: '',
  });
  const [files, setFiles] = useState<File[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [reviewLink, setReviewLink] = useState('');
  const [accessPassword, setAccessPassword] = useState('');
  const [isDragging, setIsDragging] = useState(false);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      const selectedFiles = Array.from(e.target.files);
      setFiles(selectedFiles);
      setError('');
    }
  };

  const removeFile = (indexToRemove: number) => {
    setFiles(files.filter((_, index) => index !== indexToRemove));
  };

  const clearAllFiles = () => {
    setFiles([]);
  };

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(true);
  };

  const handleDragLeave = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);

    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      const droppedFiles = Array.from(e.dataTransfer.files);
      setFiles(droppedFiles);
      setError('');
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (files.length === 0) {
      setError('Please select at least one file');
      return;
    }

    // Validate based on client mode
    if (clientMode === 'existing' && !selectedClientId) {
      setError('Please select a client');
      return;
    }
    
    if (clientMode === 'new' && (!formData.clientEmail || !formData.clientName)) {
      setError('Please enter both client name and email');
      return;
    }
    
    if (clientMode === 'quick' && !formData.clientEmail) {
      setError('Please enter client email');
      return;
    }

    setError('');
    setLoading(true);

    try {
      const submissionData: any = {
        files,
        message: formData.message,
        captions: formData.captions,
      };

      // Add client information based on mode
      if (clientMode === 'existing') {
        submissionData.clientId = selectedClientId;
      } else if (clientMode === 'new') {
        submissionData.clientEmail = formData.clientEmail;
        submissionData.clientName = formData.clientName;
      } else {
        // quick mode
        submissionData.clientEmail = formData.clientEmail;
      }

      const response = await submissionService.createSubmission(submissionData);
      
      // If we created a new client, invalidate the clients cache
      if (clientMode === 'new' && formData.clientName) {
        queryClient.invalidateQueries({ queryKey: clientKeys.list() });
      }
      
      setSuccess(true);
      setReviewLink(`${window.location.origin}/review/${response.accessToken}`);
      setAccessPassword(response.accessPassword || '');
      toast.success('Submission created successfully!');
    } catch (err: any) {
      const errorMessage = err.response?.data?.message || 'Failed to create submission';
      setError(errorMessage);
      toast.error(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    toast.success('Copied to clipboard!');
  };

  if (success) {
    return (
      <Layout>
        <div className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
          <div className="max-w-3xl mx-auto">
            {/* Success Header */}
            <div className="text-center mb-8">
              <div className="inline-flex items-center justify-center w-16 h-16 bg-green-100 rounded-full mb-4">
                <svg className="w-8 h-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                </svg>
              </div>
              <h1 className="text-3xl font-bold text-gray-900">Submission Created!</h1>
              <p className="mt-2 text-gray-600">Your submission has been created successfully.</p>
            </div>

            {/* Review Details Card */}
            <Card>
              <div className="space-y-6">
                {/* Review Link Section */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Review Link
                  </label>
                  <div className="flex gap-2">
                    <input
                      type="text"
                      value={reviewLink}
                      readOnly
                      className="flex-1 px-3 py-2 border border-gray-300 rounded-md bg-gray-50 text-sm font-mono"
                    />
                    <Button
                      variant="secondary"
                      size="small"
                      onClick={() => copyToClipboard(reviewLink)}
                    >
                      <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" />
                      </svg>
                    </Button>
                  </div>
                </div>

                {/* Password Section */}
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Access Password
                  </label>
                  <div className="flex gap-2">
                    <input
                      type="text"
                      value={accessPassword}
                      readOnly
                      className="flex-1 px-3 py-2 border border-gray-300 rounded-md bg-gray-50 text-sm font-mono"
                    />
                    <Button
                      variant="secondary"
                      size="small"
                      onClick={() => copyToClipboard(accessPassword)}
                    >
                      <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" />
                      </svg>
                    </Button>
                  </div>
                </div>

                {/* Instructions */}
                <div className="bg-blue-50 border border-blue-200 rounded-md p-4">
                  <div className="flex">
                    <svg className="w-5 h-5 text-blue-600 mt-0.5 mr-3 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                    <div>
                      <h3 className="text-sm font-medium text-blue-900 mb-1">Next Steps</h3>
                      <p className="text-sm text-blue-700">
                        Share both the review link and password with your client. They will need both to access and review the media files.
                      </p>
                    </div>
                  </div>
                </div>

                {/* Actions */}
                <div className="flex gap-3 pt-4">
                  <Button
                    variant="primary"
                    fullWidth
                    onClick={() => navigate('/dashboard')}
                  >
                    Back to Dashboard
                  </Button>
                  <Button
                    variant="secondary"
                    fullWidth
                    onClick={() => {
                      setSuccess(false);
                      setFormData({ clientEmail: '', clientName: '', message: '', captions: '' });
                      setSelectedClientId(undefined);
                      setClientMode('existing');
                      setFiles([]);
                      setReviewLink('');
                      setAccessPassword('');
                    }}
                  >
                    Create Another
                  </Button>
                </div>
              </div>
            </Card>
          </div>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-3xl mx-auto">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-gray-900">Create Submission</h1>
            <p className="mt-2 text-sm text-gray-600">
              Upload files for your client to review and approve
            </p>
          </div>

          {/* Form Card */}
          <Card>
            {error && (
              <ErrorMessage error={error} className="mb-6" />
            )}

            <form onSubmit={handleSubmit} className="space-y-6">
              {/* Client Mode Selection */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-3">
                  Client Information
                </label>
                <div className="flex gap-4 mb-4">
                  <button
                    type="button"
                    onClick={() => setClientMode('existing')}
                    className={`flex-1 py-2 px-4 rounded-md text-sm font-medium transition-colors ${
                      clientMode === 'existing'
                        ? 'bg-blue-600 text-white'
                        : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                    }`}
                  >
                    Select Existing
                  </button>
                  <button
                    type="button"
                    onClick={() => setClientMode('new')}
                    className={`flex-1 py-2 px-4 rounded-md text-sm font-medium transition-colors ${
                      clientMode === 'new'
                        ? 'bg-blue-600 text-white'
                        : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                    }`}
                  >
                    Create New
                  </button>
                  <button
                    type="button"
                    onClick={() => setClientMode('quick')}
                    className={`flex-1 py-2 px-4 rounded-md text-sm font-medium transition-colors ${
                      clientMode === 'quick'
                        ? 'bg-blue-600 text-white'
                        : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                    }`}
                  >
                    Quick Email
                  </button>
                </div>

                {/* Existing Client Selector */}
                {clientMode === 'existing' && (
                  clientsLoading ? (
                    <div className="text-center py-4 text-gray-500">Loading clients...</div>
                  ) : clients.length === 0 ? (
                    <div className="text-center py-4 text-gray-500">
                      No saved clients yet. Create a new client or use quick email entry.
                    </div>
                  ) : (
                    <ClientSelector
                      clients={clients}
                      selectedClientId={selectedClientId}
                      onSelect={(client) => setSelectedClientId(client?.id)}
                      placeholder="Select a client..."
                    />
                  )
                )}

                {/* New Client Form */}
                {clientMode === 'new' && (
                  <div className="space-y-4">
                    <Input
                      label="Client Name"
                      name="clientName"
                      type="text"
                      value={formData.clientName}
                      onChange={(value) => setFormData({ ...formData, clientName: value })}
                      required
                      placeholder="John Doe"
                      helperText="The name of your client"
                    />
                    <Input
                      label="Client Email"
                      name="clientEmail"
                      type="email"
                      value={formData.clientEmail}
                      onChange={(value) => setFormData({ ...formData, clientEmail: value })}
                      required
                      placeholder="client@example.com"
                      helperText="The email address of your client"
                    />
                  </div>
                )}

                {/* Quick Email Entry */}
                {clientMode === 'quick' && (
                  <Input
                    label="Client Email"
                    name="clientEmail"
                    type="email"
                    value={formData.clientEmail}
                    onChange={(value) => setFormData({ ...formData, clientEmail: value })}
                    required
                    placeholder="client@example.com"
                    helperText="Quick submission without saving client"
                  />
                )}
              </div>

              {/* Message */}
              <Textarea
                label="Message for the client (Optional)"
                name="message"
                value={formData.message}
                onChange={(value) => setFormData({ ...formData, message: value })}
                rows={4}
                placeholder="Add a message for your client..."
                helperText="Include any context or instructions for your client"
              />

              {/* Captions */}
              <Textarea
                label="Captions (Optional)"
                name="captions"
                value={formData.captions}
                onChange={(value) => setFormData({ ...formData, captions: value })}
                rows={4}
                placeholder="Add captions for your files..."
                helperText="Optional captions for the media files"
              />

              {/* File Upload */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Upload Files
                  <span className="text-red-500 ml-1">*</span>
                </label>
                <div 
                  className={`mt-1 flex justify-center px-6 pt-5 pb-6 border-2 border-dashed rounded-md transition-colors ${
                    isDragging 
                      ? 'border-blue-500 bg-blue-50' 
                      : 'border-gray-300 hover:border-gray-400'
                  }`}
                  onDragOver={handleDragOver}
                  onDragLeave={handleDragLeave}
                  onDrop={handleDrop}
                >
                  <div className="space-y-1 text-center">
                    <svg
                      className="mx-auto h-12 w-12 text-gray-400"
                      stroke="currentColor"
                      fill="none"
                      viewBox="0 0 48 48"
                    >
                      <path
                        d="M28 8H12a4 4 0 00-4 4v20m32-12v8m0 0v8a4 4 0 01-4 4H12a4 4 0 01-4-4v-4m32-4l-3.172-3.172a4 4 0 00-5.656 0L28 28M8 32l9.172-9.172a4 4 0 015.656 0L28 28m0 0l4 4m4-24h8m-4-4v8m-12 4h.02"
                        strokeWidth={2}
                        strokeLinecap="round"
                        strokeLinejoin="round"
                      />
                    </svg>
                    <div className="flex text-sm text-gray-600">
                      <label
                        htmlFor="file-upload"
                        className="relative cursor-pointer bg-white rounded-md font-medium text-blue-600 hover:text-blue-500 focus-within:outline-none focus-within:ring-2 focus-within:ring-offset-2 focus-within:ring-blue-500"
                      >
                        <span>Upload files</span>
                        <input
                          id="file-upload"
                          name="file-upload"
                          type="file"
                          onChange={handleFileChange}
                          multiple
                          accept="image/*,video/*"
                          className="sr-only"
                        />
                      </label>
                      <p className="pl-1">or drag and drop</p>
                    </div>
                    <p className="text-xs text-gray-500">
                      Images or videos (same type only)
                    </p>
                  </div>
                </div>
                {files.length > 0 && (
                  <div className="mt-3 p-3 bg-gray-50 rounded-md">
                    <div className="flex items-center justify-between mb-2">
                      <p className="text-sm font-medium text-gray-700">
                        {files.length} file(s) selected:
                      </p>
                      <button
                        type="button"
                        onClick={clearAllFiles}
                        className="text-xs text-red-600 hover:text-red-800 font-medium"
                      >
                        Clear all
                      </button>
                    </div>
                    <ul className="text-sm text-gray-600 space-y-2">
                      {files.map((file, index) => (
                        <li key={index} className="flex items-center justify-between bg-white p-2 rounded border border-gray-200">
                          <div className="flex items-center min-w-0 flex-1">
                            <svg className="w-4 h-4 text-gray-400 mr-2 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                            </svg>
                            <span className="truncate">{file.name}</span>
                          </div>
                          <button
                            type="button"
                            onClick={() => removeFile(index)}
                            className="ml-2 text-red-500 hover:text-red-700 flex-shrink-0"
                            title="Remove file"
                          >
                            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                            </svg>
                          </button>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>

              {/* Form Actions */}
              <div className="flex gap-3 pt-4">
                <Button
                  type="submit"
                  variant="primary"
                  fullWidth
                  loading={loading}
                  disabled={loading || files.length === 0}
                >
                  Create Submission
                </Button>
                <Button
                  type="button"
                  variant="secondary"
                  fullWidth
                  onClick={() => navigate('/dashboard')}
                  disabled={loading}
                >
                  Cancel
                </Button>
              </div>
            </form>
          </Card>
        </div>
      </div>
    </Layout>
  );
}
