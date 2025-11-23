import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { Layout } from '../components/layout';
import { Card, Button, Textarea, ErrorMessage } from '../components/common';
import { reviewService } from '../services/reviewService';
import type { SubmissionResponse } from '../shared/types';
import { ReviewStatus, MediaFileType } from '../shared/types';
import { toast } from 'react-toastify';
import { useDarkMode } from '../hooks/useDarkMode';

export default function ClientReviewPage() {
  const { token } = useParams<{ token: string }>();
  const [passwordDigits, setPasswordDigits] = useState<string[]>(['', '', '', '', '', '']);
  const [authenticated, setAuthenticated] = useState(false);
  const [submission, setSubmission] = useState<SubmissionResponse | null>(null);
  const [selectedStatus, setSelectedStatus] = useState<ReviewStatus | null>(null);
  const [comment, setComment] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [currentMediaIndex, setCurrentMediaIndex] = useState(0);
  const { isDarkMode } = useDarkMode();

  const handleDigitChange = (index: number, value: string) => {
    // Only allow single alphanumeric characters
    if (value.length > 1) {
      value = value.slice(-1);
    }
    
    const newDigits = [...passwordDigits];
    newDigits[index] = value.toUpperCase();
    setPasswordDigits(newDigits);
    
    // Auto-focus next input
    if (value && index < 5) {
      const nextInput = document.getElementById(`digit-${index + 1}`);
      nextInput?.focus();
    }
  };

  const handleDigitPaste = (e: React.ClipboardEvent<HTMLInputElement>) => {
    e.preventDefault();
    const pastedData = e.clipboardData.getData('text').trim().toUpperCase();
    
    if (pastedData.length === 6) {
      const newDigits = pastedData.split('').slice(0, 6);
      setPasswordDigits(newDigits);
      
      // Focus the last input
      const lastInput = document.getElementById('digit-5');
      lastInput?.focus();
    }
  };

  const handleDigitKeyDown = (index: number, e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Backspace' && !passwordDigits[index] && index > 0) {
      const prevInput = document.getElementById(`digit-${index - 1}`);
      prevInput?.focus();
    }
  };

  const handlePasswordSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;
    
    const fullPassword = passwordDigits.join('');
    if (fullPassword.length < 6) {
      setError('Please enter all 6 characters');
      toast.error('Please enter all 6 characters');
      return;
    }
    
    setError('');
    setLoading(true);

    try {
      const data = await reviewService.getSubmissionByToken(token, fullPassword);
      
      // Check if already reviewed
      if (data.review) {
        setSubmission(data);
        setAuthenticated(true);
        // Show already reviewed state instead of the form
        return;
      }
      
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

    // Validate that comment is provided when rejecting
    if (selectedStatus === ReviewStatus.Rejected && !comment.trim()) {
      setError('A comment is required when rejecting a submission');
      toast.error('A comment is required when rejecting a submission');
      return;
    }

    setError('');
    setLoading(true);

    const fullPassword = passwordDigits.join('');
    try {
      await reviewService.submitReview({
        accessToken: token,
        accessPassword: fullPassword,
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
              <img 
                              src={isDarkMode ? "/logo-light.svg" : "/logo.svg"}
                              className="h-12 cursor-pointer mx-auto" 
                              alt="Logo" 
                          />
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
                <div>
                  <label className="block text-sm text-center font-medium text-gray-700 mb-3">
                    Access password
                  </label>
                  <div className="flex justify-center gap-2">
                    {passwordDigits.map((digit, index) => (
                      <input
                        key={index}
                        id={`digit-${index}`}
                        type="text"
                        maxLength={1}
                        value={digit}
                        onChange={(e) => handleDigitChange(index, e.target.value)}
                        onKeyDown={(e) => handleDigitKeyDown(index, e)}
                        onPaste={handleDigitPaste}
                        className="w-12 h-14 text-center text-2xl font-bold border-2 border-gray-300 rounded-lg focus:border-blue-500 focus:ring-2 focus:ring-blue-200 outline-none transition-all uppercase"
                        autoComplete="off"
                        autoFocus={index === 0}
                      />
                    ))}
                  </div>
                  <p className="mt-2 text-xs text-gray-500 text-center">
                    Enter the 6-character access code
                  </p>
                </div>

                <Button
                  type="submit"
                  variant="primary"
                  fullWidth
                  loading={loading}
                  disabled={loading}
                >
                  Access review
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

  // If authenticated and submission has already been reviewed, show info message
  if (authenticated && submission?.review) {
    const isApproved = submission.review.status === ReviewStatus.Approved;
    const reviewDate = new Date(submission.review.reviewedAt).toLocaleString();
    
    return (
      <Layout showNavbar={false}>
        <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
          <div className="max-w-md w-full">
            <Card>
              <div className="text-center">
                <div className={`inline-flex items-center justify-center w-16 h-16 rounded-full mb-4 ${
                  isApproved ? 'bg-green-100' : 'bg-red-100'
                }`}>
                  {isApproved ? (
                    <svg className="w-8 h-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                    </svg>
                  ) : (
                    <svg className="w-8 h-8 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  )}
                </div>
                <h1 className="text-3xl font-bold text-gray-900 mb-4">Already Reviewed</h1>
                <p className="text-gray-600 mb-4">
                  This submission was already reviewed on {reviewDate}.
                </p>
                <div className="text-left bg-gray-50 rounded-lg p-4 mb-4">
                  <p className="text-sm font-medium text-gray-700 mb-1">Review Status:</p>
                  <p className={`text-lg font-semibold ${isApproved ? 'text-green-600' : 'text-red-600'}`}>
                    {isApproved ? 'Approved' : 'Rejected'}
                  </p>
                  {submission.review.comment && (
                    <>
                      <p className="text-sm font-medium text-gray-700 mt-3 mb-1">Comment:</p>
                      <p className="text-sm text-gray-600">{submission.review.comment}</p>
                    </>
                  )}
                </div>
                <p className="text-sm text-gray-500">
                  The sender has been notified of this review.
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
            <h1 className="text-3xl font-bold text-gray-900">Review content</h1>
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

          {/* Media Files Carousel */}
          <Card className="mb-6">
            <h2 className="text-xl font-semibold text-gray-900 mb-4">
              Content
            </h2>
            
            {submission.mediaFiles.length === 1 ? (
              // Single image - no carousel needed
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
              // Multiple images - Instagram-style carousel
              <div className="relative">
                {/* Main carousel container */}
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
                        aria-label="Previous image"
                      >
                        <svg className="w-6 h-6 text-gray-800" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                        </svg>
                      </button>
                    )}
                    
                    {currentMediaIndex < submission.mediaFiles.length - 1 && (
                      <button
                        onClick={() => setCurrentMediaIndex(currentMediaIndex + 1)}
                        className="absolute right-2 top-1/2 -translate-y-1/2 bg-white/90 hover:bg-white rounded-full p-2 shadow-lg transition-all z-10"
                        aria-label="Next image"
                      >
                        <svg className="w-6 h-6 text-gray-800" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                        </svg>
                      </button>
                    )}

                    {/* Counter indicator (top right) */}
                    <div className="absolute top-4 right-4 bg-black/60 text-white px-3 py-1 rounded-full text-sm font-medium">
                      {currentMediaIndex + 1} / {submission.mediaFiles.length}
                    </div>
                  </div>
                  
                </div>

                {/* Dots indicator */}
                <div className="flex justify-center gap-2 mt-4">
                  {submission.mediaFiles.map((_, index) => (
                    <button
                      key={index}
                      onClick={() => setCurrentMediaIndex(index)}
                      className={`w-2 h-2 rounded-full transition-all ${
                        index === currentMediaIndex 
                          ? 'bg-blue-600 w-8' 
                          : 'bg-gray-300 hover:bg-gray-400'
                      }`}
                      aria-label={`Go to image ${index + 1}`}
                    />
                  ))}
                </div>
              </div>
            )}
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
                label={selectedStatus === ReviewStatus.Rejected ? "Comment (Required)" : "Comment (Optional)"}
                name="comment"
                value={comment}
                onChange={setComment}
                rows={4}
                placeholder="Add any feedback or comments..."
                helperText={selectedStatus === ReviewStatus.Rejected ? "A comment is required when rejecting" : "Your comment will be shared with the sender"}
                required={selectedStatus === ReviewStatus.Rejected}
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
