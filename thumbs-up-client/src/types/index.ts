export const SubmissionStatus = {
  Pending: 0,
  Approved: 1,
  Rejected: 2,
  Expired: 3,
} as const;

export type SubmissionStatus = typeof SubmissionStatus[keyof typeof SubmissionStatus];

export const MediaFileType = {
  Image: 0,
  Video: 1,
} as const;

export type MediaFileType = typeof MediaFileType[keyof typeof MediaFileType];

export const ReviewStatus = {
  Approved: 0,
  Rejected: 1,
} as const;

export type ReviewStatus = typeof ReviewStatus[keyof typeof ReviewStatus];

export interface User {
  email: string;
  firstName?: string;
  lastName?: string;
}

export interface AuthResponse {
  token: string;
  email: string;
  firstName?: string;
  lastName?: string;
  expiresAt: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface MediaFileResponse {
  id: string;
  fileName: string;
  fileUrl: string;
  fileType: MediaFileType;
  fileSize: number;
  uploadedAt: string;
}

export interface ReviewResponse {
  id: string;
  status: ReviewStatus;
  comment?: string;
  reviewedAt: string;
}

export interface SubmissionResponse {
  id: string;
  clientEmail: string;
  accessToken: string;
  message?: string;
  status: SubmissionStatus;
  createdAt: string;
  expiresAt: string;
  mediaFiles: MediaFileResponse[];
  review?: ReviewResponse;
}

export interface CreateSubmissionRequest {
  clientEmail: string;
  accessPassword: string;
  message?: string;
  files: File[];
}

export interface ValidateAccessRequest {
  accessToken: string;
  accessPassword: string;
}

export interface SubmitReviewRequest {
  accessToken: string;
  accessPassword: string;
  status: ReviewStatus;
  comment?: string;
}
