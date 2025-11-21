/**
 * SubmissionDetailPage
 * View details of a single submission
 */

import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Layout } from '../components/layout';
import { Card, Button, LoadingSpinner, ErrorMessage } from '../components/common';
import { SubmissionStatusBadge } from '../components/submissions';
import { useSubmissionDetail, useDeleteSubmission } from '../hooks/submissions';
import { MediaFileType } from '../shared/types';
import { toast } from 'react-toastify';
import { useAuthStore } from '../stores/authStore';

export default function SubmissionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [currentMediaIndex, setCurrentMediaIndex] = useState(0);
  const { user } = useAuthStore();

  const { submission, isLoading, isError, error, refetch } = useSubmissionDetail({
    id: id!,
  });

  const { deleteSubmission, isLoading: isDeleting } = useDeleteSubmission();

  const handleDelete = async () => {
    if (!window.confirm('Are you sure you want to delete this submission?')) {
      return;
    }

    const success = await deleteSubmission(id!);
    if (success) {
      navigate('/dashboard');
    }
  };

  const handleCopyLink = () => {
    if (submission) {
      const reviewLink = `${window.location.origin}/review/${submission.accessToken}`;
      navigator.clipboard.writeText(reviewLink);
      toast.success('Link copied to clipboard!');
    }
  };

  const handleCopyPassword = () => {
    if (submission?.accessPassword) {
      navigator.clipboard.writeText(submission.accessPassword);
      toast.success('Password copied to clipboard!');
    }
  };

  if (isLoading) {
    return (
      <Layout>
        <LoadingSpinner fullScreen size="large" />
      </Layout>
    );
  }

  if (isError || !submission) {
    return (
      <Layout>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <ErrorMessage error={error || 'Submission not found'} onRetry={refetch} />
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
        {/* Header */}
        <div className="mb-6">
          <button
            onClick={() => navigate('/dashboard')}
            className="text-blue-600 hover:text-blue-700 mb-3 inline-flex items-center"
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
            Back to Dashboard
          </button>
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">Submission Details</h1>
            </div>
            <SubmissionStatusBadge status={submission.status} />
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main Content */}
          <div className="lg:col-span-2 space-y-6">
            {/* Review - Show first if it exists */}
            {submission.review && (
              <Card title="Client Review">
                <dl className="grid grid-cols-1 gap-4">
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Status</dt>
                    <dd className="mt-1 text-sm text-gray-900">
                      {submission.review.status === 0 ? 'Approved' : 'Rejected'}
                    </dd>
                  </div>
                  {submission.review.comment && (
                    <div>
                      <dt className="text-sm font-medium text-gray-500">Comment</dt>
                      <dd className="mt-1 text-sm text-gray-900">
                        {submission.review.comment}
                      </dd>
                    </div>
                  )}
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Reviewed At</dt>
                    <dd className="mt-1 text-sm text-gray-900">
                      {new Date(submission.review.reviewedAt).toLocaleString()}
                    </dd>
                  </div>
                </dl>
              </Card>
            )}

            {/* Submission Info */}
            <Card title="Submission Information">
              <dl className="grid grid-cols-1 gap-4">
                <div>
                  <dt className="text-sm font-medium text-gray-500">Client Email</dt>
                  <dd className="mt-1 text-sm text-gray-900">{submission.clientEmail}</dd>
                </div>
                {submission.message && (
                  <div>
                    <dt className="text-sm font-medium text-gray-500">Message</dt>
                    <dd className="mt-1 text-sm text-gray-900">{submission.message}</dd>
                  </div>
                )}
                <div>
                  <dt className="text-sm font-medium text-gray-500">Created</dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {new Date(submission.createdAt).toLocaleString()}
                  </dd>
                </div>
                <div>
                  <dt className="text-sm font-medium text-gray-500">Expires</dt>
                  <dd className="mt-1 text-sm text-gray-900">
                    {new Date(submission.expiresAt).toLocaleString()}
                  </dd>
                </div>
              </dl>
            </Card>

            {/* Actions - Show at bottom if reviewed */}
            {submission.review && (
              <Card title="Actions">
                <div className="space-y-3">
                  <Button
                    fullWidth
                    variant="danger"
                    onClick={handleDelete}
                    disabled={isDeleting}
                    loading={isDeleting}
                  >
                    Delete Submission
                  </Button>
                </div>
              </Card>
            )}
          </div>

          {/* Share Info - Only show if not reviewed */}
            {!submission.review && (
              <Card title="Share with Client">
                <div className="space-y-3">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Review Link
                    </label>
                    <div className="relative">
                      <input
                        type="text"
                        readOnly
                        value={`${window.location.origin}/review/${submission.accessToken}`}
                        className="input-field text-xs pr-10"
                      />
                      <button
                        onClick={handleCopyLink}
                        className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
                        title="Copy to clipboard"
                      >
                        <svg
                          className="w-5 h-5"
                          fill="none"
                          viewBox="0 0 24 24"
                          stroke="currentColor"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
                          />
                        </svg>
                      </button>
                    </div>
                  </div>
                  {submission.accessPassword && (
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Access Password
                      </label>
                      <div className="relative">
                        <input
                          type="text"
                          readOnly
                          value={submission.accessPassword}
                          className="input-field font-mono pr-10"
                        />
                        <button
                          onClick={handleCopyPassword}
                          className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
                          title="Copy to clipboard"
                        >
                          <svg
                            className="w-5 h-5"
                            fill="none"
                            viewBox="0 0 24 24"
                            stroke="currentColor"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
                            />
                          </svg>
                        </button>
                      </div>
                    </div>
                  )}
                  <p className="text-xs text-gray-500">
                    Share this link and password with your client to review the media files.
                  </p>
                  <Button
                    fullWidth
                    variant="primary"
                    onClick={() => {
                      const reviewLink = `${window.location.origin}/review/${submission.accessToken}`;
                      const userName = user?.firstName && user?.lastName 
                        ? `${user.firstName} ${user.lastName}`
                        : user?.firstName || user?.lastName || user?.email || 'Your content provider';
                      const senderName = user?.companyName || userName;
                      
                      const message = submission.accessPassword 
                        ? `Hello! \n\nThis is *${senderName}*\n\nPlease review your media files:\n\n *Link:*\n${reviewLink}\n\n *Access Password:*\n\`\`\`${submission.accessPassword}\`\`\`\n\nLooking forward to your feedback!`
                        : `Hello! \n\nThis is *${senderName}*\n\nPlease review your media files:\n\n *Link:*\n${reviewLink}\n\nLooking forward to your feedback!`;
                      
                      const whatsappUrl = `https://wa.me/?text=${encodeURIComponent(message)}`;
                      window.open(whatsappUrl, '_blank');
                    }}
                  >
                    Share via WhatsApp
                  </Button>
                </div>
              </Card>
            )}

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Media Files Carousel */}
            <Card title={`Content (${submission.mediaFiles.length})`}>
              {submission.mediaFiles.length === 0 ? (
                <p className="text-sm text-gray-500">No media files uploaded.</p>
              ) : submission.mediaFiles.length === 1 ? (
                // Single media - no carousel needed
                <div className="bg-gray-100 rounded-lg overflow-hidden">
                  <div className="relative" style={{ paddingBottom: '75%' }}>
                    {submission.mediaFiles[0].fileType === MediaFileType.Image ? (
                      <img 
                        src={submission.mediaFiles[0].fileUrl} 
                        alt={submission.mediaFiles[0].fileName} 
                        className="absolute inset-0 w-full h-full object-contain"
                      />
                    ) : (
                      <video 
                        src={submission.mediaFiles[0].fileUrl} 
                        controls 
                        className="absolute inset-0 w-full h-full object-contain"
                      />
                    )}
                  </div>
                </div>
              ) : (
                // Multiple media - Instagram-style carousel
                <div className="relative">
                  <div className="bg-gray-100 rounded-lg overflow-hidden">
                    <div className="relative" style={{ paddingBottom: '75%' }}>
                      {submission.mediaFiles[currentMediaIndex].fileType === MediaFileType.Image ? (
                        <img 
                          src={submission.mediaFiles[currentMediaIndex].fileUrl} 
                          alt={submission.mediaFiles[currentMediaIndex].fileName} 
                          className="absolute inset-0 w-full h-full object-contain"
                        />
                      ) : (
                        <video 
                          src={submission.mediaFiles[currentMediaIndex].fileUrl} 
                          controls 
                          className="absolute inset-0 w-full h-full object-contain"
                        />
                      )}
                      
                      {/* Navigation arrows */}
                      {currentMediaIndex > 0 && (
                        <button
                          onClick={() => setCurrentMediaIndex(currentMediaIndex - 1)}
                          className="absolute left-2 top-1/2 -translate-y-1/2 bg-white/90 hover:bg-white rounded-full p-2 shadow-lg transition-all z-10"
                          aria-label="Previous media"
                        >
                          <svg className="w-5 h-5 text-gray-800" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                          </svg>
                        </button>
                      )}
                      
                      {currentMediaIndex < submission.mediaFiles.length - 1 && (
                        <button
                          onClick={() => setCurrentMediaIndex(currentMediaIndex + 1)}
                          className="absolute right-2 top-1/2 -translate-y-1/2 bg-white/90 hover:bg-white rounded-full p-2 shadow-lg transition-all z-10"
                          aria-label="Next media"
                        >
                          <svg className="w-5 h-5 text-gray-800" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                          </svg>
                        </button>
                      )}

                      {/* Counter indicator */}
                      <div className="absolute top-3 right-3 bg-black/60 text-white px-2.5 py-1 rounded-full text-xs font-medium">
                        {currentMediaIndex + 1} / {submission.mediaFiles.length}
                      </div>
                    </div>
                  </div>

                  {/* Dots indicator */}
                  <div className="flex justify-center gap-1.5 mt-3">
                    {submission.mediaFiles.map((_, index) => (
                      <button
                        key={index}
                        onClick={() => setCurrentMediaIndex(index)}
                        className={`w-1.5 h-1.5 rounded-full transition-all ${
                          index === currentMediaIndex 
                            ? 'bg-blue-600 w-6' 
                            : 'bg-gray-300 hover:bg-gray-400'
                        }`}
                        aria-label={`Go to media ${index + 1}`}
                      />
                    ))}
                  </div>
                </div>
              )}
            </Card>

            

            {/* Actions - Show in sidebar if not reviewed */}
            {!submission.review && (
              <Card title="Actions">
                <div className="space-y-3">
                  <Button
                    fullWidth
                    variant="danger"
                    onClick={handleDelete}
                    disabled={isDeleting}
                    loading={isDeleting}
                  >
                    Delete Submission
                  </Button>
                </div>
              </Card>
            )}
          </div>
        </div>
      </div>
    </Layout>
  );
}
