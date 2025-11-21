/**
 * Shared TypeScript types and interfaces for the application
 * Following strict typing best practices
 */

// ===== Enums =====
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

export const UserRole = {
  User: 'User',
  Admin: 'Admin',
} as const;

export type UserRole = typeof UserRole[keyof typeof UserRole];

// ===== User Types =====
export interface User {
  id?: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role?: UserRole;
}

export interface AuthResponse {
  token: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role?: UserRole;
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

// ===== Media File Types =====
export interface MediaFileResponse {
  id: string;
  fileName: string;
  fileUrl: string;
  fileType: MediaFileType;
  fileSize: number;
  uploadedAt: string;
}

export interface MediaFileUpload {
  file: File;
  preview?: string;
}

// ===== Review Types =====
export interface ReviewResponse {
  id: string;
  status: ReviewStatus;
  comment?: string;
  reviewedAt: string;
  reviewerEmail?: string;
}

export interface SubmitReviewRequest {
  accessToken: string;
  accessPassword: string;
  status: ReviewStatus;
  comment?: string;
}

export interface ValidateAccessRequest {
  accessToken: string;
  accessPassword: string;
}

export interface ValidateAccessResponse {
  valid: boolean;
  message: string;
}

// ===== Submission Types =====
export interface SubmissionResponse {
  id: string;
  clientEmail: string;
  accessToken: string;
  accessPassword?: string;
  message?: string;
  status: SubmissionStatus;
  createdAt: string;
  expiresAt: string;
  mediaFiles: MediaFileResponse[];
  review?: ReviewResponse;
  createdBy?: string;
}

export interface CreateSubmissionRequest {
  clientEmail: string;
  message?: string;
  files: File[];
}

export interface CreateSubmissionResponse extends SubmissionResponse {
  reviewLink: string;
}

export interface UpdateSubmissionRequest {
  clientEmail?: string;
  message?: string;
  status?: SubmissionStatus;
}

// ===== Filter Types =====
export interface SubmissionFilters {
  status?: SubmissionStatus;
  searchTerm?: string;
  dateFrom?: string;
  dateTo?: string;
  sortBy?: 'createdAt' | 'clientEmail' | 'status';
  sortOrder?: 'asc' | 'desc';
}

// ===== Pagination Types =====
export interface PaginationParams {
  page: number;
  pageSize: number;
}

export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// ===== API Response Types =====
export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
  statusCode?: number;
}

export interface ApiResponse<T = unknown> {
  data?: T;
  message?: string;
  success: boolean;
}

// ===== Hook Return Types =====
export interface UseQueryResult<T> {
  data: T | undefined;
  isLoading: boolean;
  isError: boolean;
  error: ApiError | null;
  refetch: () => void;
}

export interface UseMutationResult<TData, TVariables> {
  mutate: (variables: TVariables) => Promise<TData>;
  mutateAsync: (variables: TVariables) => Promise<TData>;
  isLoading: boolean;
  isError: boolean;
  isSuccess: boolean;
  error: ApiError | null;
  reset: () => void;
}

// ===== Component Props Types =====
export interface BaseComponentProps {
  className?: string;
  children?: React.ReactNode;
}

export interface LoadingProps extends BaseComponentProps {
  size?: 'small' | 'medium' | 'large';
  fullScreen?: boolean;
}

export interface ErrorProps extends BaseComponentProps {
  error: ApiError | Error | string;
  onRetry?: () => void;
}

export interface ModalProps extends BaseComponentProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
}

// ===== Form Types =====
export interface FormFieldProps {
  label: string;
  name: string;
  error?: string;
  required?: boolean;
  helperText?: string;
}

export interface SubmissionFormData {
  clientEmail: string;
  message: string;
  files: File[];
}

export interface ReviewFormData {
  status: ReviewStatus;
  comment: string;
}

// ===== Utility Types =====
export type Nullable<T> = T | null;
export type Optional<T> = T | undefined;
export type DeepPartial<T> = {
  [P in keyof T]?: DeepPartial<T[P]>;
};
