# Thumbs Up - Media Approval System

A full-stack web application for social media professionals to send images/videos to clients for approval.

## Tech Stack

### Backend
- .NET 9 Web API
- ASP.NET Core Identity (Authentication)
- Entity Framework Core
- PostgreSQL
- JWT Authentication
- BCrypt for password hashing

### Frontend
- React 18 with TypeScript
- Vite
- React Router
- Axios
- Zustand (State Management)

## Features

### For Professionals
- Register and login with ASP.NET Core Identity
- Create submissions with multiple files (images or videos)
- Generate shareable links with password protection
- View submission status (Pending, Approved, Rejected, Expired)
- Track all submissions in dashboard

### For Clients
- Access submissions via link (no account needed)
- Password-protected review access
- View all media files (images with lightbox, videos with player)
- Approve or reject with optional comments
- Single submission per link

## Setup Instructions

### Prerequisites
- .NET 9 SDK
- Node.js (v18+)
- PostgreSQL database

### Backend Setup

1. Navigate to the API directory:
   ```bash
   cd ThumbsUpApi
   ```

2. Update the connection string in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=thumbsup;Username=YOUR_USER;Password=YOUR_PASSWORD"
   }
   ```

3. Create the database and run migrations:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

4. Run the API:
   ```bash
   dotnet run
   ```
   
   The API will be available at `https://localhost:7154` or `http://localhost:5000`

### Frontend Setup

1. Navigate to the client directory:
   ```bash
   cd thumbs-up-client
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Create a `.env` file (optional, defaults to localhost:7154):
   ```
   VITE_API_URL=https://localhost:7154/api
   ```

4. Run the development server:
   ```bash
   npm run dev
   ```
   
   The app will be available at `http://localhost:5173`

## Configuration

### File Storage
Currently configured for local storage. To switch to cloud storage (Azure Blob or AWS S3):

1. Implement a new service implementing `IFileStorageService`
2. Update `appsettings.json`:
   ```json
   "FileStorage": {
     "Provider": "Azure", // or "AWS"
     "LocalPath": "wwwroot/uploads"
   }
   ```
3. Register the new service in `Program.cs`

### Email Service
Currently using mock email service (logs to console). To integrate Resend:

1. Update `appsettings.json`:
   ```json
   "Email": {
     "Provider": "Resend",
     "ResendApiKey": "your-api-key"
   }
   ```
2. Implement `ResendEmailService` implementing `IEmailService`
3. Register the service in `Program.cs`

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register a new professional
- `POST /api/auth/login` - Login

### Submissions (Authenticated)
- `POST /api/submission` - Create new submission
- `GET /api/submission` - Get all user submissions
- `GET /api/submission/{id}` - Get submission details
- `DELETE /api/submission/{id}` - Delete submission

### Review (Public with password)
- `POST /api/review/validate` - Validate access token and password
- `GET /api/review/{token}` - Get submission by token
- `POST /api/review/submit` - Submit review (approve/reject)

## Default Settings

- Link expiration: 7 days
- Max file size: 100MB
- JWT token expiration: 24 hours
- Password requirements: min 6 characters, requires digit, uppercase, lowercase

## Development Notes

- CORS is configured to allow `localhost:5173` and `localhost:5174`
- SSL certificate validation is disabled in development for local testing
- File uploads are stored in `wwwroot/uploads` by default
- All passwords (both user and client access) are hashed with BCrypt

## Next Steps

1. Install npm dependencies: `cd thumbs-up-client && npm install`
2. Set up PostgreSQL database
3. Run EF migrations: `cd ThumbsUpApi && dotnet ef database update`
4. Start both backend and frontend
5. Register a professional account
6. Create your first submission!

## Future Enhancements

- Email integration with Resend
- Cloud file storage (Azure Blob Storage)
- File compression and thumbnail generation
- Submission templates
- Analytics dashboard
- Batch operations
- Client comment threads
