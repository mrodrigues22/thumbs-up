import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { reviewService } from '../services/reviewService';
import type { SubmissionResponse } from '../types';
import { ReviewStatus, MediaFileType } from '../types';

export default function ClientReviewPage() {
  const { token } = useParams<{ token: string }>();
  const [password, setPassword] = useState('');
  const [authenticated, setAuthenticated] = useState(false);
  const [submission, setSubmission] = useState<SubmissionResponse | null>(null);
  const [selectedStatus, setSelectedStatus] = useState<ReviewStatus | null>(null);
  const [comment, setComment] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [submitted, setSubmitted] = useState(false);

  const handlePasswordSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;
    
    setError('');
    setLoading(true);

    try {
      const data = await reviewService.getSubmissionByToken(token, password);
      setSubmission(data);
      setAuthenticated(true);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Invalid access credentials');
    } finally {
      setLoading(false);
    }
  };

  const handleReviewSubmit = async () => {
    if (!token || !selectedStatus) return;

    setError('');
    setLoading(true);

    try {
      await reviewService.submitReview({
        accessToken: token,
        accessPassword: password,
        status: selectedStatus,
        comment: comment || undefined,
      });
      setSubmitted(true);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to submit review');
    } finally {
      setLoading(false);
    }
  };

  if (submitted) {
    return (
      <div style={{ maxWidth: '600px', margin: '50px auto', padding: '20px', textAlign: 'center' }}>
        <h1>Thank You!</h1>
        <p>Your review has been submitted successfully.</p>
        <p>The sender will be notified of your decision.</p>
      </div>
    );
  }

  if (!authenticated) {
    return (
      <div style={{ maxWidth: '400px', margin: '50px auto', padding: '20px' }}>
        <h1>Enter Access Password</h1>
        <form onSubmit={handlePasswordSubmit}>
          {error && <div style={{ color: 'red', marginBottom: '10px' }}>{error}</div>}
          <div style={{ marginBottom: '15px' }}>
            <label>Password:</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              style={{ width: '100%', padding: '8px', marginTop: '5px' }}
            />
          </div>
          <button type="submit" disabled={loading} style={{ padding: '10px 20px' }}>
            {loading ? 'Validating...' : 'Access Review'}
          </button>
        </form>
      </div>
    );
  }

  if (!submission) return <div>Loading...</div>;

  return (
    <div style={{ maxWidth: '800px', margin: '50px auto', padding: '20px' }}>
      <h1>Review Media</h1>
      {submission.message && (
        <div style={{ backgroundColor: '#f0f0f0', padding: '15px', marginBottom: '20px', borderRadius: '5px' }}>
          <p><strong>Message from sender:</strong></p>
          <p>{submission.message}</p>
        </div>
      )}

      <div style={{ marginBottom: '30px' }}>
        <h2>Media Files ({submission.mediaFiles.length})</h2>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: '15px' }}>
          {submission.mediaFiles.map((file) => (
            <div key={file.id} style={{ border: '1px solid #ddd', borderRadius: '5px', padding: '10px' }}>
              {file.fileType === MediaFileType.Image ? (
                <img src={file.fileUrl} alt={file.fileName} style={{ width: '100%', height: 'auto' }} />
              ) : (
                <video src={file.fileUrl} controls style={{ width: '100%', height: 'auto' }} />
              )}
              <p style={{ fontSize: '12px', marginTop: '5px', wordBreak: 'break-word' }}>{file.fileName}</p>
            </div>
          ))}
        </div>
      </div>

      {error && <div style={{ color: 'red', marginBottom: '10px' }}>{error}</div>}

      <div style={{ backgroundColor: '#f9f9f9', padding: '20px', borderRadius: '5px' }}>
        <h2>Your Decision</h2>
        <div style={{ marginBottom: '15px' }}>
          <label>
            <input
              type="radio"
              name="status"
              checked={selectedStatus === ReviewStatus.Approved}
              onChange={() => setSelectedStatus(ReviewStatus.Approved)}
            />
            {' '}Approve
          </label>
          <br />
          <label>
            <input
              type="radio"
              name="status"
              checked={selectedStatus === ReviewStatus.Rejected}
              onChange={() => setSelectedStatus(ReviewStatus.Rejected)}
            />
            {' '}Reject
          </label>
        </div>

        <div style={{ marginBottom: '15px' }}>
          <label>Comment (optional):</label>
          <textarea
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            rows={4}
            style={{ width: '100%', padding: '8px', marginTop: '5px' }}
          />
        </div>

        <button
          onClick={handleReviewSubmit}
          disabled={loading || !selectedStatus}
          style={{ padding: '10px 20px' }}
        >
          {loading ? 'Submitting...' : 'Submit Review'}
        </button>
      </div>
    </div>
  );
}
