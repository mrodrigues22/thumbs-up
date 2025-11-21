import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { Layout } from '../components/layout';
import { Card, Button, Input, Textarea, ErrorMessage } from '../components/common';
import { reviewService } from '../services/reviewService';
import type { SubmissionResponse } from '../types';
import { ReviewStatus, MediaFileType } from '../types';
import { toast } from 'react-toastify';

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
      toast.success('Access granted!');
    } catch (err: any) {
      const errorMessage = err.response?.data?.message || 'Invalid access credentials';
      setError(errorMessage);
      toast.error(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleReviewSubmit = async () => {
    if (!token || selectedStatus === null) return;

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
      toast.success('Review submitted successfully!');
    } catch (err: any) {
      const errorMessage = err.response?.data?.message || 'Failed to submit review';
      setError(errorMessage);
      toast.error(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  if (submitted) {
    return (
      <Layout showNavbar={false}>
        <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
          <div className="max-w-md w-full">
            <Card>
              <div className="text-center">
                <div className="inline-flex items-center justify-center w-16 h-16 bg-green-100 rounded-full mb-4">
                  <svg className="w-8 h-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                  </svg>
                </div>
                <h1 className="text-3xl font-bold text-gray-900 mb-4">Thank You!</h1>
                <p className="text-gray-600 mb-2">Your review has been submitted successfully.</p>
                <p className="text-gray-600">The sender will be notified of your decision.</p>
              </div>
            </Card>
          </div>
        </div>
      </Layout>
    );
  }

  if (!authenticated) {
    return (
      <Layout showNavbar={false}>
        <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
          <div className="max-w-md w-full">
            {/* Header */}
            <div className="text-center mb-8">
              <span className="text-6xl">👍</span>
              <h2 className="mt-4 text-3xl font-extrabold text-gray-900">
                Review Access
              </h2>
              <p className="mt-2 text-sm text-gray-600">
                Enter the password to view and review the submission
              </p>
            </div>

            {/* Password Form Card */}
            <Card>
              {error && (
                <ErrorMessage error={error} className="mb-4" />
              )}

              <form onSubmit={handlePasswordSubmit} className="space-y-6">
                <Input
                  label="Access Password"
                  name="password"
                  type="password"
                  value={password}
                  onChange={setPassword}
                  required
                  placeholder="Enter the password"
                  autoComplete="off"
                />

                <Button
                  type="submit"
                  variant="primary"
                  fullWidth
                  loading={loading}
                  disabled={loading}
                >
                  Access Review
                </Button>
              </form>

              <div className="mt-6 text-center">
                <p className="text-xs text-gray-500">
                  This password was provided by the sender
                </p>
              </div>
            </Card>
          </div>
        </div>
      </Layout>
    );
  }

  if (!submission) {
    return (
      <Layout showNavbar={false}>
        <div className="min-h-screen bg-gray-50 flex items-center justify-center">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
            <p className="mt-4 text-gray-600">Loading submission...</p>
          </div>
        </div>
      </Layout>
    );
  }

  return (
    <Layout showNavbar={false}>
      <div className="min-h-screen bg-gray-50 py-8 px-4 sm:px-6 lg:px-8">
        <div className="max-w-5xl mx-auto">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-gray-900">Review Media</h1>
            <p className="mt-2 text-sm text-gray-600">
              Review the submitted files and provide your feedback
            </p>
          </div>

          {/* Message from sender */}
          {submission.message && (
            <Card className="mb-6">
              <div className="flex">
                <svg className="w-5 h-5 text-blue-600 mt-0.5 mr-3 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 8h10M7 12h4m1 8l-4-4H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-3l-4 4z" />
                </svg>
                <div>
                  <h3 className="text-sm font-medium text-gray-900 mb-1">Message from sender</h3>
                  <p className="text-sm text-gray-700">{submission.message}</p>
                </div>
              </div>
            </Card>
          )}

          {/* Media Files */}
          <Card className="mb-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">
              Media Files ({submission.mediaFiles.length})
            </h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              {submission.mediaFiles.map((file) => (
                <div key={file.id} className="group relative bg-white border border-gray-200 rounded-lg overflow-hidden hover:shadow-md transition-shadow">
                  <div className="aspect-w-16 aspect-h-12 bg-gray-100">
                    {file.fileType === MediaFileType.Image ? (
                      <img 
                        src={file.fileUrl} 
                        alt={file.fileName} 
                        className="w-full h-full object-cover"
                      />
                    ) : (
                      <video 
                        src={file.fileUrl} 
                        controls 
                        className="w-full h-full object-cover"
                      />
                    )}
                  </div>
                  <div className="p-3">
                    <p className="text-xs text-gray-600 truncate" title={file.fileName}>
                      {file.fileName}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </Card>

          {/* Review Form */}
          <Card>
            <h2 className="text-xl font-semibold text-gray-900 mb-6">Your Decision</h2>
            
            {error && (
              <ErrorMessage error={error} className="mb-4" />
            )}

            <div className="space-y-6">
              {/* Radio buttons for status */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-3">
                  Select your review decision
                  <span className="text-red-500 ml-1">*</span>
                </label>
                <div className="space-y-3">
                  <label className={`flex items-center p-4 border-2 rounded-lg cursor-pointer transition-all ${
                    selectedStatus === ReviewStatus.Approved 
                      ? 'border-green-500 bg-green-50' 
                      : 'border-gray-300 hover:border-gray-400'
                  }`}>
                    <input
                      type="radio"
                      name="status"
                      checked={selectedStatus === ReviewStatus.Approved}
                      onChange={() => setSelectedStatus(ReviewStatus.Approved)}
                      className="h-4 w-4 text-green-600 focus:ring-green-500"
                    />
                    <div className="ml-3 flex items-center">
                      <svg className="w-5 h-5 text-green-600 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                      <span className="text-sm font-medium text-gray-900">
                        Approve
                      </span>
                    </div>
                  </label>
                  
                  <label className={`flex items-center p-4 border-2 rounded-lg cursor-pointer transition-all ${
                    selectedStatus === ReviewStatus.Rejected 
                      ? 'border-red-500 bg-red-50' 
                      : 'border-gray-300 hover:border-gray-400'
                  }`}>
                    <input
                      type="radio"
                      name="status"
                      checked={selectedStatus === ReviewStatus.Rejected}
                      onChange={() => setSelectedStatus(ReviewStatus.Rejected)}
                      className="h-4 w-4 text-red-600 focus:ring-red-500"
                    />
                    <div className="ml-3 flex items-center">
                      <svg className="w-5 h-5 text-red-600 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                      <span className="text-sm font-medium text-gray-900">
                        Reject
                      </span>
                    </div>
                  </label>
                </div>
              </div>

              {/* Comment textarea */}
              <Textarea
                label="Comment (Optional)"
                name="comment"
                value={comment}
                onChange={setComment}
                rows={4}
                placeholder="Add any feedback or comments..."
                helperText="Your comment will be shared with the sender"
              />

              {/* Submit button */}
              <Button
                onClick={handleReviewSubmit}
                variant="primary"
                fullWidth
                loading={loading}
                disabled={loading || selectedStatus === null}
              >
                Submit Review
              </Button>
            </div>
          </Card>
        </div>
      </div>
    </Layout>
  );
}
