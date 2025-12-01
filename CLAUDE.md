# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

KURATOR is a governance relationships management system (MVP) built with .NET 9, Next.js 14, and PostgreSQL 16. The application manages contacts and interactions across sectors with role-based access control (Admin, Curator, Threat Analyst).

## Development Commands

### Backend (.NET 9)

Build and run the API:
```bash
cd backend
dotnet build
dotnet run --project Kurator.Api
```

Run tests:
```bash
dotnet test                                    # All tests
dotnet test Kurator.Core.Tests                # Unit tests only
dotnet test Kurator.Tests                     # Integration tests only
dotnet test --filter "FullyQualifiedName~ContactService"  # Specific test class
```

Database migrations:
```bash
# Create new migration
dotnet ef migrations add MigrationName --project Kurator.Infrastructure --startup-project Kurator.Api

# Apply migrations
dotnet ef database update --project Kurator.Infrastructure --startup-project Kurator.Api

# Rollback to previous migration
dotnet ef database update PreviousMigrationName --project Kurator.Infrastructure --startup-project Kurator.Api
```

### Frontend (Next.js 14)

Development and build:
```bash
cd frontend
npm install                    # Install dependencies
npm run dev                    # Development server (port 3000)
npm run build                  # Production build
npm run start                  # Start production build
```

Testing:
```bash
npm run test                   # Jest unit tests
npm run test:watch            # Watch mode
npm run test:coverage         # Coverage report
npm run test:e2e              # Playwright E2E tests
npm run test:e2e:headed       # E2E tests with browser UI
```

Code quality:
```bash
npm run lint                   # ESLint check
npm run lint:fix              # Auto-fix linting issues
npm run format                # Prettier format check
npm run format:fix            # Auto-format code
```

### Docker Operations

Full stack deployment:
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f [service_name]  # backend, frontend, or postgres

# Restart specific service
docker-compose restart backend

# Complete rebuild
docker-compose down
docker-compose up -d --build

# Database operations
docker-compose exec backend dotnet ef database update --project Kurator.Infrastructure --startup-project Kurator.Api
```

## Architecture

### Backend Architecture (Clean Architecture)

```
backend/
├── Kurator.Api/              # Presentation Layer
│   ├── Controllers/          # API endpoints (~40 endpoints)
│   ├── Middleware/           # JWT auth, error handling, encryption
│   └── Extensions/           # Service registration
│
├── Kurator.Core/             # Domain Layer
│   ├── Entities/            # Domain models
│   ├── Interfaces/          # Repository/service contracts
│   ├── Services/            # Business logic
│   └── DTOs/                # Data transfer objects
│
└── Kurator.Infrastructure/   # Data Access Layer
    ├── Data/                # DbContext, migrations
    ├── Repositories/        # EF Core repositories
    └── Services/            # External services (encryption)
```

Key architectural patterns:
- **Repository Pattern**: All data access through repository interfaces
- **Service Layer**: Business logic isolated in Core.Services
- **Dependency Injection**: Interface-based DI throughout
- **DTOs**: Separate models for API requests/responses
- **Middleware Pipeline**: Authentication, encryption, error handling

### Frontend Architecture (Next.js App Router)

```
frontend/
├── app/                      # Next.js 14 App Router
│   ├── (auth)/              # Public authentication pages
│   ├── (dashboard)/         # Protected dashboard pages
│   └── [feature]/           # Feature-specific routes
│
├── components/              # Reusable UI components
│   ├── common/             # Generic components
│   └── [feature]/          # Feature-specific components
│
├── services/               # API integration
│   ├── api.ts             # Axios instance with interceptors
│   └── [feature]Service.ts # Feature-specific API calls
│
└── lib/                    # Utilities
    ├── encryption.ts       # RSA/AES client-side encryption
    └── auth.ts            # JWT token management
```

Key patterns:
- **Server Components**: Default for all pages (RSC)
- **Client Components**: Only for interactivity (use client directive)
- **API Integration**: NSwag-generated TypeScript client from OpenAPI
- **State Management**: TanStack Query for server state
- **Form Handling**: React Hook Form with Zod validation

### Database Schema

Core entities with relationships:
- **Users**: System users with roles and RSA public keys
- **Blocks**: Organizational sectors with curator assignments
- **Contacts**: Encrypted personal data with influence tracking
- **Interactions**: Communication history with contacts
- **Watchlist**: Threat monitoring registry
- **AuditLogs**: Complete change history with encryption

Performance optimizations:
- Strategic indexes on foreign keys and search fields
- Compound indexes for complex queries
- Soft deletes for data retention
- Optimistic locking with version fields

## Security Implementation

### Encryption Architecture

Two-layer encryption system:
1. **Client-side RSA-2048**: User key pairs for field-level encryption
2. **Server-side AES-256**: Additional encryption at rest

Sensitive fields encrypted:
- Contact names (ФИО)
- Notes and comments
- File attachments
- Audit log old/new values

### Authentication Flow

JWT-based authentication:
1. Login with credentials → JWT access token (1 hour)
2. Refresh token (7 days) for seamless renewal
3. MFA using TOTP (optional but recommended)
4. Role-based access control (Admin, Curator, Threat Analyst)

## Key Development Considerations

### API Design
- RESTful endpoints with consistent naming
- Pagination on all list endpoints (default 20 items)
- Standardized error responses with error codes
- OpenAPI/Swagger documentation auto-generated

### Frontend Development
- All UI text in Russian (except technical fields)
- Responsive design for desktop and tablet
- Accessibility: ARIA labels, keyboard navigation
- Dark mode support (theme switching)

### Testing Requirements
- Minimum 80% code coverage target
- Unit tests for all services and utilities
- Integration tests for API endpoints
- E2E tests for critical user flows

### Performance Guidelines
- Database queries optimized with includes/projections
- Frontend: React.memo for expensive components
- API responses cached with appropriate TTL
- Lazy loading for large datasets

## Common Development Tasks

### Adding a New Entity

Backend:
1. Create entity in Kurator.Core/Entities
2. Add DbSet to ApplicationDbContext
3. Create repository interface and implementation
4. Add service with business logic
5. Create controller with CRUD endpoints
6. Add DTOs for requests/responses
7. Create and apply EF migration

Frontend:
1. Add TypeScript types
2. Create API service methods
3. Build UI components
4. Add to navigation/routing
5. Implement form with validation
6. Add success/error handling

### Implementing a New Feature

1. Review business requirements in business-analysis.md
2. Design database schema changes if needed
3. Implement backend API endpoints
4. Generate TypeScript client with NSwag
5. Build frontend components and pages
6. Write unit and integration tests
7. Update API documentation

### Debugging Common Issues

JWT Token Issues:
- Check token expiration (1 hour lifetime)
- Verify JWT_SECRET matches in .env
- Ensure Authorization header format: "Bearer {token}"

Encryption Errors:
- Verify user has valid RSA key pair
- Check ENCRYPTION_KEY in .env (32+ characters)
- Ensure encrypted fields are base64 encoded

Database Connection:
- Verify PostgreSQL is running (port 5432)
- Check connection string in appsettings.json
- Ensure database migrations are applied

## Production Deployment

Prerequisites:
- Docker and Docker Compose installed
- PostgreSQL 16 (or use Docker container)
- .NET 9 Runtime
- Node.js 18+

Deployment steps:
1. Clone repository
2. Copy .env.example to .env and configure
3. Run `docker-compose up -d`
4. Apply database migrations
5. Access frontend at http://localhost:3000

Default admin credentials:
- Username: admin
- Password: admin123
- **Important**: Change on first login

## Code Quality Standards

Backend (.NET):
- StyleCop.Analyzers enforced
- SonarAnalyzer.CSharp for code quality
- XML documentation for public APIs
- Async/await for I/O operations

Frontend (TypeScript):
- Strict mode enabled
- No `any` without justification
- ESLint + Prettier enforced
- Component prop types defined

General:
- Meaningful variable/function names
- Comments for complex logic only
- Git commit messages follow conventional commits
- PR reviews required for main branch