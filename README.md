# Recruitment Process Management System

Hey there! This is my take on building a full-stack recruitment platform as part of a pre-internship challenge. It's designed to handle the whole hiring workflow—from posting jobs to scheduling interviews and collecting feedback—all while keeping things secure and scalable. I built this to showcase modern development practices, and who knows, it might even get used for real someday.

## What's Built (Backend Ready)

The backend is fully implemented with a robust API:

- **Authentication & Roles**: JWT-based login/register with role-based access (Candidate, Recruiter, HR, Admin).
- **Job Management**: CRUD for job positions, applicant tracking, and status updates.
- **Candidate & Applications**: Profile management, application submissions, and workflow tracking.
- **Interview System**: Scheduling with conflict detection, participant management, Google Meet integration, and evaluations.
- **Reporting**: Analytics on interviews, applications, and job metrics.
- **Notifications**: Email services for interview invites and updates.
- **Search & Filtering**: Advanced queries for jobs, candidates, and interviews.
- **Security**: Clean architecture with EF Core, migrations, and comprehensive service coverage.

Frontend is in progress—building with React, Zustand, and React Query for a smooth user experience.
Frontend is currently in early development—core UI and features are being built from scratch. Most functionality is not yet complete.

## Future Plans (Frontend & Enhancements)

- Complete React UI for all features (dashboards, forms, scheduling).
- Implement mobile responsiveness.
- Add Google Calendar integration for event management.
- Build automated background functions for reminders and data cleanups.
- Integrate Redis for caching to improve performance.
- Dockerize the stack for easy deployment.

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
   - Update connection string in `appsettings.json` for your SQL Server.
   - Run migrations: `dotnet ef database update`
   - Start the API: `dotnet run` (runs on http://localhost:5000 by default).

3. **Frontend setup**:

   - Navigate to `/client`: `cd client`
   - Install deps: `npm install`
   - Start dev server: `npm run dev` (opens on http://localhost:5173).

4. **Test it out**:
   - Register/login as a candidate or recruiter.
   - Post a job, apply, schedule an interview—see the flow in action.

## Project Structure

```
RecruitmentSystem/
├── client/                 # React frontend
│   ├── src/
│   │   ├── components/     # UI components
│   │   ├── store/          # Zustand stores
│   │   ├── hooks/          # Custom hooks
│   │   └── types/          # TypeScript interfaces
├── server/RecruitmentSystem/  # .NET backend
│   ├── RecruitmentSystem.API/  # Controllers, Program.cs
│   ├── RecruitmentSystem.Core/ # Entities, DTOs, interfaces
│   ├── RecruitmentSystem.Services/ # Business logic
│   └── RecruitmentSystem.Tests/ # Unit tests
└── README.md
```

Built with care—feedback welcome! Reach out if you spot issues or have ideas. Happy coding!
