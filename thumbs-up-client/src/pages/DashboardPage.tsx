import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { submissionService } from '../services/submissionService';
import { useAuthStore } from '../stores/authStore';
import type { SubmissionResponse } from '../types';
import { SubmissionStatus } from '../types';

export default function DashboardPage() {
  const [submissions, setSubmissions] = useState<SubmissionResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();
  const { user, clearAuth } = useAuthStore();

  useEffect(() => {
    loadSubmissions();
  }, []);

  const loadSubmissions = async () => {
    try {
      const data = await submissionService.getSubmissions();
      setSubmissions(data);
    } catch (error) {
      console.error('Failed to load submissions:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleLogout = () => {
    clearAuth();
    navigate('/login');
  };

  const getStatusBadge = (status: SubmissionStatus) => {
    const colors = {
      [SubmissionStatus.Pending]: '#ffc107',
      [SubmissionStatus.Approved]: '#28a745',
      [SubmissionStatus.Rejected]: '#dc3545',
      [SubmissionStatus.Expired]: '#6c757d',
    };
    const statusLabels = {
      [SubmissionStatus.Pending]: 'Pending',
      [SubmissionStatus.Approved]: 'Approved',
      [SubmissionStatus.Rejected]: 'Rejected',
      [SubmissionStatus.Expired]: 'Expired',
    };
    return (
      <span style={{ 
        backgroundColor: colors[status], 
        color: 'white', 
        padding: '4px 8px', 
        borderRadius: '4px',
        fontSize: '12px'
      }}>
        {statusLabels[status]}
      </span>
    );
  };

  return (
    <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '20px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '30px' }}>
        <div>
          <h1>Dashboard</h1>
          <p>Welcome, {user?.firstName || user?.email}!</p>
        </div>
        <div>
          <button onClick={() => navigate('/create-submission')} style={{ padding: '10px 20px', marginRight: '10px' }}>
            New Submission
          </button>
          <button onClick={handleLogout} style={{ padding: '10px 20px' }}>
            Logout
          </button>
        </div>
      </div>

      {loading ? (
        <p>Loading submissions...</p>
      ) : submissions.length === 0 ? (
        <p>No submissions yet. Create your first one!</p>
      ) : (
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ borderBottom: '2px solid #ddd' }}>
              <th style={{ padding: '10px', textAlign: 'left' }}>Client Email</th>
              <th style={{ padding: '10px', textAlign: 'left' }}>Created</th>
              <th style={{ padding: '10px', textAlign: 'left' }}>Files</th>
              <th style={{ padding: '10px', textAlign: 'left' }}>Status</th>
              <th style={{ padding: '10px', textAlign: 'left' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {submissions.map((sub) => (
              <tr key={sub.id} style={{ borderBottom: '1px solid #ddd' }}>
                <td style={{ padding: '10px' }}>{sub.clientEmail}</td>
                <td style={{ padding: '10px' }}>{new Date(sub.createdAt).toLocaleDateString()}</td>
                <td style={{ padding: '10px' }}>{sub.mediaFiles.length}</td>
                <td style={{ padding: '10px' }}>{getStatusBadge(sub.status)}</td>
                <td style={{ padding: '10px' }}>
                  <button onClick={() => navigate(`/submission/${sub.id}`)} style={{ padding: '5px 10px' }}>
                    View
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
