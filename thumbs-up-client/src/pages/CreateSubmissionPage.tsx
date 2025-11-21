import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Layout } from '../components/layout';
import { Card, Button, Input, Textarea } from '../components/common';
import { useCreateSubmission } from '../hooks/submissions';
import type { CreateSubmissionRequest } from '../shared/types';
import { submissionService } from '../services/submissionService';

export default function CreateSubmissionPage() {
  const navigate = useNavigate();
  useCreateSubmission();
  const [formData, setFormData] = useState({
    clientEmail: '',
    accessPassword: '',
    message: '',
  });
  const [files, setFiles] = useState<File[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [reviewLink, setReviewLink] = useState('');

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      const selectedFiles = Array.from(e.target.files);
      setFiles(selectedFiles);
      setError('');
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (files.length === 0) {
      setError('Please select at least one file');
      return;
    }

    setError('');
    setLoading(true);

    try {
      const response = await submissionService.createSubmission({
        ...formData,
        files,
      });
      
      setSuccess(true);
      setReviewLink(`${window.location.origin}/review/${response.accessToken}`);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to create submission');
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <div style={{ maxWidth: '600px', margin: '50px auto', padding: '20px' }}>
        <h1>Submission Created!</h1>
        <p>Your submission has been created successfully.</p>
        <div style={{ backgroundColor: '#f0f0f0', padding: '15px', marginTop: '20px', borderRadius: '5px' }}>
          <p><strong>Review Link:</strong></p>
          <input
            type="text"
            value={reviewLink}
            readOnly
            style={{ width: '100%', padding: '8px', marginTop: '5px' }}
          />
          <p style={{ marginTop: '15px' }}><strong>Access Password:</strong> {formData.accessPassword}</p>
        </div>
        <p style={{ marginTop: '20px', color: '#666' }}>
          Share this link and password with your client to review the media.
        </p>
        <button onClick={() => navigate('/dashboard')} style={{ padding: '10px 20px', marginTop: '20px' }}>
          Back to Dashboard
        </button>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '600px', margin: '50px auto', padding: '20px' }}>
      <h1>Create Submission</h1>
      <form onSubmit={handleSubmit}>
        {error && <div style={{ color: 'red', marginBottom: '10px' }}>{error}</div>}
        
        <div style={{ marginBottom: '15px' }}>
          <label>Client Email:</label>
          <input
            type="email"
            value={formData.clientEmail}
            onChange={(e) => setFormData({ ...formData, clientEmail: e.target.value })}
            required
            style={{ width: '100%', padding: '8px', marginTop: '5px' }}
          />
        </div>

        <div style={{ marginBottom: '15px' }}>
          <label>Access Password (min 4 characters):</label>
          <input
            type="text"
            value={formData.accessPassword}
            onChange={(e) => setFormData({ ...formData, accessPassword: e.target.value })}
            required
            minLength={4}
            style={{ width: '100%', padding: '8px', marginTop: '5px' }}
          />
          <small>This password will be shared with the client to access the review.</small>
        </div>

        <div style={{ marginBottom: '15px' }}>
          <label>Message (optional):</label>
          <textarea
            value={formData.message}
            onChange={(e) => setFormData({ ...formData, message: e.target.value })}
            rows={4}
            style={{ width: '100%', padding: '8px', marginTop: '5px' }}
          />
        </div>

        <div style={{ marginBottom: '15px' }}>
          <label>Upload Files (images or videos, same type only):</label>
          <input
            type="file"
            onChange={handleFileChange}
            multiple
            accept="image/*,video/*"
            style={{ width: '100%', padding: '8px', marginTop: '5px' }}
          />
          {files.length > 0 && (
            <p style={{ marginTop: '5px', color: '#666' }}>
              {files.length} file(s) selected
            </p>
          )}
        </div>

        <div style={{ display: 'flex', gap: '10px' }}>
          <button type="submit" disabled={loading} style={{ padding: '10px 20px' }}>
            {loading ? 'Creating...' : 'Create Submission'}
          </button>
          <button type="button" onClick={() => navigate('/dashboard')} style={{ padding: '10px 20px' }}>
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}
