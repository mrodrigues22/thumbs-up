# Thumbs Up - Frontend Architecture Documentation

## Overview
This document outlines the improved architecture implemented for the Thumbs Up client application, following React best practices and modern development patterns.

## Architecture Principles

1. **Separation of Concerns**: Clear separation between business logic, presentation, and data management
2. **Component Reusability**: Modular, reusable components that can be composed together
3. **Type Safety**: Strict TypeScript usage with no `any` types
4. **Single Responsibility**: Each hook, component, and module has one clear purpose
5. **Scalability**: Easy to add new features following established patterns

## Project Structure

```
src/
├── shared/
│   ├── types.ts              # All TypeScript type definitions
│   └── api/
│       ├── client.ts         # Axios instance with interceptors
│       ├── config.ts         # API endpoints and configuration
│       ├── utils.ts          # API utility functions
│       └── index.ts          # Central export
│
├── hooks/
│   ├── auth/
│   │   ├── useAuth.ts        # Auth operations wrapper
│   │   └── index.ts
│   ├── submissions/
│   │   ├── useSubmissions.ts        # List/fetch submissions
│   │   ├── useSubmissionDetail.ts   # Single submission
│   │   ├── useCreateSubmission.ts   # Create operation
│   │   ├── useDeleteSubmission.ts   # Delete operation
│   │   └── index.ts
│   └── reviews/
│       ├── useValidateAccess.ts     # Validate client access
│       ├── useReviewSubmission.ts   # Fetch for review
│       ├── useSubmitReview.ts       # Submit review
│       └── index.ts
│
├── components/
│   ├── common/                # Reusable UI components
│   │   ├── LoadingSpinner.tsx
│   │   ├── ErrorMessage.tsx
│   │   ├── Button.tsx
│   │   ├── Card.tsx
│   │   ├── Input.tsx
│   │   ├── Textarea.tsx
│   │   ├── Modal.tsx
│   │   └── index.ts
│   ├── layout/                # Layout components
│   │   ├── Layout.tsx
│   │   ├── Navbar.tsx
│   │   ├── ProtectedRoute.tsx
│   │   └── index.ts
│   └── submissions/           # Feature-specific components
│       ├── SubmissionStatusBadge.tsx
│       ├── SubmissionCard.tsx
│       ├── SubmissionList.tsx
│       ├── SubmissionFilters.tsx
│       ├── MediaGallery.tsx
│       └── index.ts
│
├── pages/                     # Page components
│   ├── LoginPage.tsx
│   ├── RegisterPage.tsx
│   ├── SubmissionsPage.tsx
│   ├── SubmissionDetailPage.tsx
│   ├── CreateSubmissionPage.tsx
│   └── ClientReviewPage.tsx
│
├── services/                  # API service functions
│   ├── authService.ts
│   ├── submissionService.ts
│   └── reviewService.ts
│
├── stores/                    # Zustand state management
│   └── authStore.ts
│
├── App.tsx                    # Main app component with routing
├── main.tsx                   # App entry point
└── index.css                  # Global styles with Tailwind
```

## Key Technologies

- **React 19**: Latest React with hooks
- **TypeScript**: Strict type checking
- **React Router v6**: Client-side routing
- **Zustand**: Lightweight state management
- **Axios**: HTTP client with interceptors
- **Tailwind CSS**: Utility-first CSS framework
- **React Toastify**: Toast notifications
- **Vite**: Fast build tool

## Design Patterns

### 1. Custom Hooks Pattern
Each feature has dedicated hooks for data operations:

```typescript
// Example: useSubmissions hook
const { 
  filteredSubmissions,
  isLoading,
  isError,
  error,
  refetch 
} = useSubmissions({ filters });
```

**Benefits:**
- Encapsulates business logic
- Reusable across components
- Easy to test
- Consistent state management

### 2. Presentational/Container Pattern
Components are split into:
- **Container Components** (Pages): Handle data fetching and state
- **Presentational Components**: Pure UI components that receive props

### 3. Composition Pattern
Small, focused components composed together:

```tsx
<Layout>
  <Card title="Submissions">
    <SubmissionFilters onFiltersChange={handleFilters} />
    <SubmissionList submissions={data} />
  </Card>
</Layout>
```

## Component Guidelines

### Common Components (`src/components/common/`)
Reusable UI components used throughout the app:
- **LoadingSpinner**: Loading indicator with size variants
- **ErrorMessage**: Error display with retry option
- **Button**: Button with variants (primary, secondary, danger, ghost)
- **Card**: Container component with optional title and footer
- **Input/Textarea**: Form inputs with label and error handling
- **Modal**: Modal dialog with overlay

### Layout Components (`src/components/layout/`)
- **Layout**: Main wrapper with navbar
- **Navbar**: Top navigation with user menu
- **ProtectedRoute**: Route wrapper requiring authentication

### Feature Components (`src/components/submissions/`)
Domain-specific components:
- **SubmissionStatusBadge**: Status indicator
- **SubmissionCard**: Submission summary card
- **SubmissionList**: Grid of submission cards
- **SubmissionFilters**: Filter and sort controls
- **MediaGallery**: Media file gallery with preview

## Hook Guidelines

### Structure
Each hook should:
1. Have a single, clear responsibility
2. Return consistent shape: `{ data, isLoading, isError, error, refetch/mutate }`
3. Handle errors internally
4. Show toast notifications for user feedback
5. Support AbortController for cancellation

### Example Hook Pattern

```typescript
export const useCreateSubmission = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [isError, setIsError] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);
  const [data, setData] = useState<SubmissionResponse | null>(null);

  const createSubmission = useCallback(async (data) => {
    setIsLoading(true);
    try {
      const result = await submissionService.createSubmission(data);
      setData(result);
      setIsSuccess(true);
      toast.success('Success!');
      return result;
    } catch (err) {
      setIsError(true);
      setError(formatError(err));
      toast.error(formatError(err));
      return null;
    } finally {
      setIsLoading(false);
    }
  }, []);

  return { createSubmission, isLoading, isError, isSuccess, error, data, reset };
};
```

## Type Safety

### Shared Types (`src/shared/types.ts`)
All types are centrally defined:
- **Enums**: SubmissionStatus, MediaFileType, ReviewStatus
- **DTOs**: API request/response types
- **Component Props**: Reusable prop interfaces
- **Utility Types**: Generic helper types

### Best Practices
1. Always define explicit types, avoid `any`
2. Use `type` for unions and intersections
3. Use `interface` for object shapes
4. Export types with DTOs for API operations
5. Use const assertions for enum-like objects

## API Layer

### Configuration (`src/shared/api/config.ts`)
Centralized API configuration:
- **API_CONFIG**: Base URL, timeout, headers
- **API_ENDPOINTS**: All endpoint definitions
- **QUERY_KEYS**: React Query cache keys
- **STORAGE_KEYS**: LocalStorage keys

### Client (`src/shared/api/client.ts`)
Axios instance with:
- **Request Interceptor**: Adds auth token
- **Response Interceptor**: Handles 401 errors
- **Helper Functions**: `get()`, `post()`, `put()`, `delete()`
- **AbortController**: Support for cancellable requests

## State Management

### Zustand Store (`src/stores/authStore.ts`)
Lightweight global state for authentication:
- User information
- Authentication status
- Token management
- Persist to localStorage

### Local State
Use `useState` for:
- Form inputs
- UI state (modals, dropdowns)
- Component-specific data

## Styling

### Tailwind CSS
Utility-first approach with:
- Responsive modifiers (`sm:`, `md:`, `lg:`)
- Custom utility classes in `index.css`
- Component-specific styles when needed

### Custom Classes
Defined in `@layer components`:
- `.btn-primary`, `.btn-secondary`, `.btn-danger`
- `.input-field`
- `.card`

## Routing

### Protected Routes
Use `<ProtectedRoute>` wrapper:

```tsx
<Route
  path="/dashboard"
  element={
    <ProtectedRoute>
      <SubmissionsPage />
    </ProtectedRoute>
  }
/>
```

### Route Structure
- `/login` - Public
- `/register` - Public
- `/review/:token` - Public (with password)
- `/dashboard` - Protected
- `/submissions` - Protected
- `/submissions/create` - Protected
- `/submissions/:id` - Protected

## Best Practices

### 1. Component Organization
- One component per file
- Co-locate related components
- Export from index.ts files

### 2. Error Handling
- Try-catch in async functions
- Show user-friendly error messages
- Log errors to console in development
- Use ErrorBoundary for component errors

### 3. Performance
- Use `useCallback` for functions passed as props
- Use `useMemo` for expensive calculations
- Implement virtual scrolling for large lists (future)
- Lazy load routes (future optimization)

### 4. Accessibility
- Semantic HTML elements
- ARIA labels where needed
- Keyboard navigation support
- Focus management in modals

### 5. Code Quality
- Consistent naming conventions
- JSDoc comments for complex logic
- ESLint rules enforcement
- Type-check before commit

## Testing Strategy (Future)

### Unit Tests
- Test hooks in isolation
- Test utility functions
- Test API client helpers

### Component Tests
- Test component rendering
- Test user interactions
- Test error states

### Integration Tests
- Test complete user flows
- Test API integration
- Test authentication flow

## Future Enhancements

1. **React Query**: Replace custom hooks with React Query for better caching
2. **Form Library**: Add Formik or React Hook Form for complex forms
3. **State Machine**: Use XState for complex UI states
4. **Error Boundary**: Add error boundaries for graceful degradation
5. **Analytics**: Add analytics tracking
6. **Offline Support**: Service worker for offline functionality
7. **Dark Mode**: Theme switching support
8. **i18n**: Internationalization support

## Development Workflow

### Adding a New Feature

1. **Define Types** in `src/shared/types.ts`
2. **Create Hooks** in `src/hooks/[feature]/`
3. **Create Components** in `src/components/[feature]/`
4. **Create Pages** in `src/pages/`
5. **Add Routes** in `src/App.tsx`
6. **Add Navigation** in `src/components/layout/Navbar.tsx`
7. **Test Feature** thoroughly

### Code Review Checklist
- [ ] Types defined and exported
- [ ] Hooks follow consistent pattern
- [ ] Components are properly typed
- [ ] Error handling implemented
- [ ] Loading states handled
- [ ] Responsive design tested
- [ ] Accessibility considered
- [ ] No console errors or warnings
- [ ] Code follows established patterns

## Conclusion

This architecture provides a solid foundation for building scalable React applications with:
- Clear separation of concerns
- Type safety throughout
- Reusable, composable components
- Consistent patterns and practices
- Easy onboarding for new developers

For questions or suggestions, refer to this documentation and existing code examples.
