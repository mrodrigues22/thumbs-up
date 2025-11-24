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
import { useApprovalPrediction } from '../hooks/insights';
import { MediaFileType, ApprovalPredictionStatus, ContentFeatureStatus, SubmissionStatus } from '../shared/types';
import { toast } from 'react-toastify';
import { useAuthStore } from '../stores/authStore';
import { submissionService } from '../services/submissionService';

export default function SubmissionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [currentMediaIndex, setCurrentMediaIndex] = useState(0);
  const [showFullOcr, setShowFullOcr] = useState(false);
  const [isReanalyzing, setIsReanalyzing] = useState(false);
  const { user } = useAuthStore();

  const { submission, isLoading, isError, error, refetch } = useSubmissionDetail({
    id: id!,
  });

  const { deleteSubmission, isLoading: isDeleting } = useDeleteSubmission();

  const { prediction, isLoading: isLoadingPrediction, isError: isPredictionError } =
    useApprovalPrediction({
      clientId: submission?.clientId,
      submissionId: submission?.id,
      enabled: !!submission?.clientId,
    });

  // Format rationale with **bold** markers and line breaks.
  const renderFormattedRationale = (text: string) => {
    if (!text) return null;
    const segments = text.split('**');
    return segments.map((seg, i) => {
      const withLineBreaks = seg.split(/\n+/).map((line, li, arr) => (
        <span key={`${i}-${li}`}>
          {line}
          {li < arr.length - 1 && <br />}
        </span>
      ));
      if (i % 2 === 1) {
        return (
          <strong key={i} className="font-semibold">
            {withLineBreaks}
          </strong>
        );
      }
      return <span key={i}>{withLineBreaks}</span>;
    });
  };

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

  const handleCopyOcrText = () => {
    if (submission?.contentFeature?.ocrText) {
      navigator.clipboard.writeText(submission.contentFeature.ocrText);
      toast.success('Detected text copied');
    }
  };

  const handleReanalyze = async () => {
    if (!submission) return;
    setIsReanalyzing(true);
    try {
      await submissionService.requestReanalysis(submission.id);
      toast.success('Reanalysis queued. Refresh in ~30 seconds.');
    } catch (err) {
      console.error('Failed to queue reanalysis', err);
      toast.error('Unable to start reanalysis. Please try again.');
    } finally {
      setIsReanalyzing(false);
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

  const isPredictionReady = prediction?.status === ApprovalPredictionStatus.Ready && prediction?.probability != null;
  const probabilityPercent = isPredictionReady && prediction?.probability != null ? prediction.probability * 100 : null;
  const probabilityColorClasses = (() => {
    if (probabilityPercent == null) return 'bg-gray-200 text-gray-700 dark:bg-gray-700/40 dark:text-gray-300';
    if (probabilityPercent >= 80) return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300';
    if (probabilityPercent >= 50) return 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-300';
    return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300';
  })();
  const probabilityBarStyle = probabilityPercent != null
    ? { width: `${probabilityPercent}%` }
    : { width: '0%' };

  const predictionStatusStyles: Record<ApprovalPredictionStatus, { border: string; bg: string; text: string }> = {
    [ApprovalPredictionStatus.PendingSignals]: {
      border: 'border-amber-300',
      bg: 'bg-amber-50 dark:bg-amber-900/20',
      text: 'text-amber-800',
    },
    [ApprovalPredictionStatus.MissingHistory]: {
      border: 'border-slate-300',
      bg: 'bg-slate-50 dark:bg-slate-800/40',
      text: 'text-slate-800',
    },
    [ApprovalPredictionStatus.Error]: {
      border: 'border-red-300',
      bg: 'bg-red-50 dark:bg-red-900/20',
      text: 'text-red-800',
    },
    [ApprovalPredictionStatus.Ready]: {
      border: 'border-emerald-300',
      bg: 'bg-emerald-50 dark:bg-emerald-900/20',
      text: 'text-emerald-800',
    },
  };
  const predictionStatusStyle = prediction ? predictionStatusStyles[prediction.status] : null;
  const predictionStatusMessage = prediction?.statusMessage ?? 'Waiting for image analysis before scoring this submission.';

  const contentFeatureStatusMeta: Record<ContentFeatureStatus, { label: string; classes: string }> = {
    [ContentFeatureStatus.Pending]: {
      label: 'Analysis pending',
      classes: 'border border-amber-200 bg-amber-50 text-amber-800',
    },
    [ContentFeatureStatus.Completed]: {
      label: 'Analysis complete',
      classes: 'border border-emerald-200 bg-emerald-50 text-emerald-700',
    },
    [ContentFeatureStatus.NoSignals]: {
      label: 'Limited signals',
      classes: 'border border-indigo-200 bg-indigo-50 text-indigo-700',
    },
    [ContentFeatureStatus.NoImages]: {
      label: 'No images to analyze',
      classes: 'border border-slate-200 bg-slate-50 text-slate-700',
    },
    [ContentFeatureStatus.Failed]: {
      label: 'Analysis failed',
      classes: 'border border-red-200 bg-red-50 text-red-700',
    },
  };
  const normalizeFeatureStatus = (status: ContentFeatureStatus | number | null | undefined): ContentFeatureStatus | null => {
    if (status == null) return null;
    if (typeof status === 'string') return status as ContentFeatureStatus;
    const numericMap: Record<number, ContentFeatureStatus> = {
      0: ContentFeatureStatus.Pending,
      1: ContentFeatureStatus.Completed,
      2: ContentFeatureStatus.NoSignals,
      3: ContentFeatureStatus.NoImages,
      4: ContentFeatureStatus.Failed,
    };
    return numericMap[status] ?? null;
  };
  const feature = submission.contentFeature;
  const featureStatus = normalizeFeatureStatus(feature?.analysisStatus);
  const featureStatusDisplay = featureStatus ? contentFeatureStatusMeta[featureStatus] : null;
  const analyzedTimestamp = feature?.lastAnalyzedAt ?? feature?.extractedAt;
  const canRenderInsights = !!feature && (featureStatus === ContentFeatureStatus.Completed || featureStatus === ContentFeatureStatus.NoSignals);
  const featureStatusMessage = (() => {
    if (!feature) {
      return 'Insights will populate after the AI pipeline finishes. Trigger another run if the card stays empty.';
    }
    if (!featureStatus) {
      return 'Analysis has not started yet.';
    }
    switch (featureStatus) {
      case ContentFeatureStatus.Pending:
        return 'Image analysis is running. Refresh once the queue catches up.';
      case ContentFeatureStatus.NoImages:
        return 'This submission has no image files to analyze.';
      case ContentFeatureStatus.Failed:
        return feature.failureReason || 'Analysis failed. Try re-running after checking the files.';
      default:
        return null;
    }
  })();

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
              <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100">Submission Details</h1>
            </div>
            <SubmissionStatusBadge status={submission.status} />
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main Content */}
          <div className="lg:col-span-2 space-y-6">

            {/* AI Approval Insight - Only show for pending/expired submissions */}
            {submission.status !== SubmissionStatus.Approved && submission.status !== SubmissionStatus.Rejected && (
              <>
                {isLoadingPrediction && !prediction && (
                  <Card title="AI Approval Insight">
                    <div className="animate-pulse space-y-3">
                      <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-48" />
                      <div className="h-10 bg-gray-200 dark:bg-gray-700 rounded w-32" />
                      <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-full" />
                    </div>
                  </Card>
                )}
                {prediction && (
              <Card title="AI Approval Insight">
                {isPredictionReady ? (
                  <>
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-2">
                          <p className="text-sm text-gray-500 dark:text-gray-300">
                            Estimated likelihood of client approval
                          </p>
                          <div className="relative group cursor-help">
                            <svg className="w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                            </svg>
                            <div className="absolute left-1/2 top-full mt-2 -translate-x-1/2 w-64 bg-gray-800 text-white text-xs p-2 rounded shadow-lg opacity-0 group-hover:opacity-100 transition-opacity z-20">
                              This AI estimate analyzes historical approval patterns, media attributes, and textual cues. Use as guidance—not a final decision.
                            </div>
                          </div>
                        </div>
                        <div className={`inline-flex items-center gap-2 rounded-full px-4 py-2 ${probabilityColorClasses}`}>
                          <span className="text-2xl font-bold">
                            {probabilityPercent?.toFixed(0)}%
                          </span>
                          <span className="text-xs uppercase tracking-wide">
                            likelihood of approval
                          </span>
                        </div>
                        <div className="mt-3 w-full h-2 bg-gray-200 dark:bg-gray-700 rounded overflow-hidden">
                          <div
                            className="h-full bg-gradient-to-r from-red-500 via-yellow-500 to-green-500 transition-all"
                            style={probabilityBarStyle}
                          />
                        </div>
                      </div>
                      {isLoadingPrediction && (
                        <span className="text-xs text-gray-400">Updating...</span>
                      )}
                      {isPredictionError && !isLoadingPrediction && (
                        <span className="text-xs text-red-500">(unavailable)</span>
                      )}
                    </div>
                    {prediction.rationale && (
                      <p className="mt-4 text-sm text-gray-700 dark:text-gray-200 leading-relaxed">
                        {renderFormattedRationale(prediction.rationale)}
                      </p>
                    )}
                    {prediction.statusMessage && (
                      <p className="mt-3 text-xs text-gray-400">{prediction.statusMessage}</p>
                    )}
                  </>
                ) : (
                  <div className={`rounded-lg border ${predictionStatusStyle?.border ?? 'border-amber-200'} ${predictionStatusStyle?.bg ?? 'bg-amber-50'} p-4 text-sm ${predictionStatusStyle?.text ?? 'text-amber-800'}`}>
                    <div className="flex items-center gap-2">
                      {isLoadingPrediction && <LoadingSpinner size="small" />}
                      <p>{predictionStatusMessage}</p>
                    </div>
                    {prediction.status === ApprovalPredictionStatus.Error && prediction.rationale && (
                      <p className="mt-2 text-xs text-red-700 dark:text-red-300">
                        {renderFormattedRationale(prediction.rationale)}
                      </p>
                    )}
                  </div>
                )}
              </Card>
            )}
              </>
            )}

            {/* AI Content Summary */}
            {submission.contentSummary && (
              <Card title="AI Content Summary">
                <p className="text-sm leading-relaxed text-gray-700 dark:text-gray-200 whitespace-pre-line">
                  {submission.contentSummary}
                </p>
              </Card>
            )}

            <Card title="Creative Insights">
              <div className="space-y-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div className="flex flex-col gap-1 text-xs text-gray-500">
                    {analyzedTimestamp ? (
                      <p>Last checked {new Date(analyzedTimestamp).toLocaleString()}</p>
                    ) : (
                      <p>No analysis attempt recorded yet.</p>
                    )}
                    {featureStatusDisplay && (
                      <span className={`inline-flex w-fit items-center rounded-full px-3 py-0.5 text-xs font-medium ${featureStatusDisplay.classes}`}>
                        {featureStatusDisplay.label}
                      </span>
                    )}
                    {featureStatus === ContentFeatureStatus.Failed && submission.contentFeature?.failureReason && (
                      <span className="text-red-600 dark:text-red-400">{submission.contentFeature.failureReason}</span>
                    )}
                  </div>
                  <Button
                    size="small"
                    variant="secondary"
                    onClick={() => { void handleReanalyze(); }}
                    loading={isReanalyzing}
                  >
                    {isReanalyzing ? 'Reanalyzing…' : 'Re-run analysis'}
                  </Button>
                </div>

                {canRenderInsights ? (
                  <>
                    {(() => {
                      const themeInsights = feature?.themeInsights;
                      const insightSections = [
                        { label: 'Subjects', items: themeInsights?.subjects ?? [] },
                        { label: 'Vibes', items: themeInsights?.vibes ?? [] },
                        { label: 'Notable elements', items: themeInsights?.notableElements ?? [] },
                        { label: 'Colors', items: themeInsights?.colors ?? [] },
                        { label: 'Keywords', items: themeInsights?.keywords ?? [] },
                      ];
                      const hasInsightChips = insightSections.some(section => section.items.length > 0);
                      const chipColors: Record<string, string> = {
                        Subjects: 'bg-sky-50 text-sky-700',
                        Vibes: 'bg-violet-50 text-violet-700',
                        'Notable elements': 'bg-emerald-50 text-emerald-700',
                        Colors: 'bg-rose-50 text-rose-700',
                        Keywords: 'bg-amber-50 text-amber-700',
                      };

                      if (hasInsightChips) {
                        return (
                          <div className="space-y-3">
                            {insightSections.map(section => (
                              section.items.length > 0 && (
                                <div key={section.label}>
                                  <p className="text-xs font-semibold uppercase tracking-wide text-gray-500">
                                    {section.label}
                                  </p>
                                  <div className="mt-1 flex flex-wrap gap-2">
                                    {section.items.map(item => (
                                      <span
                                        key={`${section.label}-${item}`}
                                        className={`inline-flex items-center rounded-full px-3 py-0.5 text-xs font-medium ${chipColors[section.label] ?? 'bg-gray-100 text-gray-700'}`}
                                      >
                                        {item}
                                      </span>
                                    ))}
                                  </div>
                                </div>
                              )
                            ))}
                          </div>
                        );
                      }

                      if ((feature?.tags || []).length > 0) {
                        return (
                          <div>
                            <p className="text-xs font-semibold uppercase tracking-wide text-gray-500">Themes</p>
                            <div className="mt-2 flex flex-wrap gap-2">
                              {feature!.tags.map(tag => (
                                <span key={tag} className="inline-flex items-center rounded-full bg-gray-100 px-3 py-0.5 text-xs font-medium text-gray-700">
                                  {tag}
                                </span>
                              ))}
                            </div>
                          </div>
                        );
                      }

                      return <p className="text-sm text-gray-500">No visual themes detected yet.</p>;
                    })()}

                    
                  </>
                ) : (
                  <p className="text-sm text-gray-500">
                    {featureStatusMessage || 'Insights will populate after the AI pipeline finishes. Trigger another run if the card stays empty.'}
                  </p>
                )}
              </div>
            </Card>


            
            

            {/* Submission Info */}
            <Card title="Submission Information">
              <dl className="grid grid-cols-1 gap-4">
                <div>
                  <dt className="text-sm font-medium text-gray-500 dark:text-gray-100">Client Email</dt>
                  <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">{submission.clientEmail}</dd>
                </div>
                {submission.message && (
                  <div>
                    <dt className="text-sm font-medium text-gray-500 dark:text-gray-100">Message</dt>
                    <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">{submission.message}</dd>
                  </div>
                )}
                <div>
                  <dt className="text-sm font-medium text-gray-500 dark:text-gray-100">Created</dt>
                  <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">
                    {new Date(submission.createdAt).toLocaleString()}
                  </dd>
                </div>
                <div>
                  <dt className="text-sm font-medium text-gray-500 dark:text-gray-100">Expires</dt>
                  <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">
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

          

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Share Info - Only show if not reviewed */}
            {!submission.review && (
              <Card title="Share with Client">
                <div className="space-y-3">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1 dark:text-gray-100">
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
                      <label className="block text-sm font-medium text-gray-700 mb-1 dark:text-gray-100">
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
                  <p className="text-xs text-gray-500 dark:text-dark-gray-100">
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
                        ? `Hello! \n\nThis is *${senderName}*\n\nPlease review your media files:\n\n*Link:*\n${reviewLink}\n\n*Access Password:*\n\`\`\`${submission.accessPassword}\`\`\`\n\nLooking forward to your feedback!`
                        : `Hello! \n\nThis is *${senderName}*\n\nPlease review your media files:\n\n*Link:*\n${reviewLink}\n\nLooking forward to your feedback!`;
                      
                      const whatsappUrl = `https://wa.me/?text=${encodeURIComponent(message)}`;
                      window.open(whatsappUrl, '_blank');
                    }}
                  >
                    Share via WhatsApp
                  </Button>
                </div>
              </Card>
            )}
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
