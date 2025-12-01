# Generated API Client

This directory contains auto-generated TypeScript client code from the Kurator API OpenAPI/Swagger specification.

## Generation

### Prerequisites

1. The backend API must be running and accessible
2. Swagger endpoint must be available at: `http://localhost:5000/swagger/v1/swagger.json`

### Commands

**Generate from running API:**
```bash
npm run generate:api
```

**Generate from local swagger.json file:**
```bash
npm run generate:api:file
```

## Generated Structure

After generation, this directory will contain:

```
generated/
  core/           - Core API utilities (ApiError, CancelablePromise, etc.)
  models/         - TypeScript interfaces/types from API DTOs
  services/       - API service classes for each controller
  index.ts        - Main export file
```

## Usage Example

```typescript
import { ContactsService, AuthService } from '@/services/generated';

// Authentication
const loginResponse = await AuthService.login({
  requestBody: { login: 'admin', password: 'password' }
});

// Get contacts
const contacts = await ContactsService.getAll({
  page: 1,
  pageSize: 20
});
```

## Configuration

The generated client uses axios by default. Configure the base URL in your environment:

```env
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

## Regeneration

Regenerate the client whenever the API changes:

1. Update backend API
2. Run `npm run generate:api`
3. Commit the regenerated files

## Notes

- Do not manually edit files in this directory (except this README)
- All changes will be overwritten on regeneration
- Generated types provide full type safety for API interactions
