# Recruitment Process Management System

A comprehensive full-stack recruitment platform designed to streamline the hiring workflow—from job posting to interview scheduling and feedback collection. Built with modern technologies to ensure security, scalability, and a seamless user experience.

## Features

### Backend (Fully Implemented)

- **Authentication & Authorization**: JWT-based authentication with role-based access control (Candidate, Recruiter, HR, Admin).
- **Job Management**: Complete CRUD operations for job positions, application tracking, and status management.
- **Candidate Management**: Profile creation, application submissions, and workflow monitoring.
- **Interview System**: Automated scheduling with conflict detection, participant management, Google Meet integration, and evaluation forms.
- **Reporting & Analytics**: Insights into interviews, applications, and job metrics.
- **Notifications**: Email services for interview invitations and updates.
- **Background Automation**: Hangfire-powered jobs for offer expiry, job closures, reminders, and system maintenance.
- **Search & Filtering**: Advanced querying for jobs, candidates, and interviews.
- **Security & Architecture**: Clean architecture using EF Core, migrations, and comprehensive service layer.

### Frontend (Implemented)

- **User Interface**: React-based UI with responsive design using Tailwind CSS and Radix UI components.
- **State Management**: Zustand for global state and React Query for server state management.
- **Authentication Flow**: Secure login/register with token lifecycle management.
- **Dashboards**: Role-specific dashboards for candidates, recruiters, and admins.
- **Forms & Validation**: React Hook Form with client-side validation.
- **Real-time Updates**: Optimistic updates and error handling with React Query.

## Technologies Used

### Backend

- **Framework**: ASP.NET Core (.NET 8)
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT Tokens
- **Background Jobs**: Hangfire
- **Email**: SMTP Integration
- **Architecture**: Clean Architecture (Core, Infrastructure, Services, API)

### Frontend

- **Framework**: React 19 with TypeScript
- **Build Tool**: Vite
- **Styling**: Tailwind CSS with Radix UI
- **State Management**: Zustand + TanStack React Query
- **Forms**: React Hook Form
- **Calendar**: FullCalendar
- **HTTP Client**: Axios

## Architecture Overview

The system follows a clean architecture pattern:

- **API Layer**: Controllers handling HTTP requests and responses.
- **Services Layer**: Business logic and domain services.
- **Core Layer**: Entities, DTOs, interfaces, and domain models.
- **Infrastructure Layer**: Data access, external integrations, and persistence.

Frontend uses a component-based architecture with custom hooks for data fetching and state management.

### Frontend Implementation Notes

- **Authentication**: Auth layout with routes for login, register, and forgot password. Uses React Query for API calls and Zustand for state persistence.
- **Forms**: Lightweight client-side validation with React Hook Form; backend handles comprehensive validation.
- **State Management**: React Query for server state, Zustand for client state (auth, user profile).
- **UI Components**: Radix UI primitives with Tailwind CSS for consistent, accessible design.
- **Calendar**: FullCalendar integration for interview scheduling and visualization.

## Future Enhancements

- Mobile app development
- UI Enhancements
- Advanced Google Calendar integration
- Redis caching for performance
- Additional analytics and reporting features

## Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js (LTS)
- SQL Server (or Docker for containerized DB)
- Git

### Setup

1. **Clone and navigate**:

   ```bash
   git clone https://github.com/Harsh260105/Recruitment-Process-Management.git
   cd Recruitment-Process-Management
   ```

2. **Backend setup**:

   - Open `/server/RecruitmentSystem/RecruitmentSystem.sln` in Visual Studio or VS Code.
   - Copy `server/RecruitmentSystem/RecruitmentSystem.API/appsettings.sample.json` to `server/RecruitmentSystem/RecruitmentSystem.API/appsettings.json` and update the placeholders with your actual configuration values (e.g., database connection string, API keys for email and AWS).
   - Configure the `Automation` section (system user id, reminder horizons, token retention) before enabling Hangfire jobs.
   - Run migrations: `dotnet ef database update`
   - Start the API: `dotnet run`

3. **Frontend setup**:

   - Navigate to `/client`: `cd client`
   - Install deps: `npm install`
   - Start dev server: `npm run dev`.

4. **Test it out**:
   - Register/login as a candidate or recruiter.
   - Post a job, apply, schedule an interview—see the flow in action.

## Project Structure

```
RecruitmentSystem/
├── client/                          # React frontend
│   ├── src/
│   │   ├── components/              # UI components (auth, common, interviews, ui)
│   │   ├── constants/               # Application constants
│   │   ├── hooks/                   # Custom hooks (auth, candidate, staff)
│   │   ├── layouts/                 # Layout components
│   │   ├── lib/                     # Utilities
│   │   ├── pages/                   # Page components
│   │   ├── router/                  # Routing configuration
│   │   ├── services/                # API services
│   │   ├── store/                   # Zustand stores
│   │   ├── styles/                  # CSS styles
│   │   ├── types/                   # TypeScript types
│   │   └── utils/                   # Utility functions
│   ├── public/                      # Static assets
│   └── ImplementationGuide.md       # Client implementation details
├── server/RecruitmentSystem/        # .NET backend
│   ├── RecruitmentSystem.API/       # Web API project
│   │   ├── Controllers/             # API controllers
│   │   ├── Properties/              # Launch settings
│   │   └── appsettings.json         # Configuration
│   ├── RecruitmentSystem.Core/      # Domain layer
│   │   ├── DTOs/                    # Data transfer objects
│   │   ├── Entities/                # Domain entities
│   │   ├── Enums/                   # Enumerations
│   │   └── Interfaces/              # Domain interfaces
│   ├── RecruitmentSystem.Services/  # Business logic layer
│   ├── RecruitmentSystem.Infrastructure/  # Infrastructure layer (EF Core, repositories)
│   ├── RecruitmentSystem.Shared/    # Shared utilities and DTOs
│   └── RecruitmentSystem.Tests/     # Unit and integration tests
└── README.md
```
