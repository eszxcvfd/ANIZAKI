# API Contract Naming and Response Guidelines

This document is the canonical contract guide for API/Web integration in this repository.

## Route Naming

- All business endpoints are versioned under `/api/v1`.
- Use lowercase, slash-separated resource paths (example: `/api/v1/system/status`).
- Keep route segments nouns whenever possible.
- Reserve `/health` as the environment smoke endpoint (outside versioned group).

## Current Auth Endpoint Surface

| Endpoint | Method | Access |
| --- | --- | --- |
| `/api/v1/auth/register` | `POST` | Public |
| `/api/v1/auth/login` | `POST` | Public |
| `/api/v1/auth/logout` | `POST` | Authenticated (`AuthorizationPolicies.AuthenticatedUser`) |
| `/api/v1/auth/forgot-password` | `POST` | Public |
| `/api/v1/auth/reset-password` | `POST` | Public |
| `/api/v1/auth/verify-email` | `POST` | Public |
| `/api/v1/users/me` | `GET` | Authenticated (`AuthorizationPolicies.AuthenticatedUser`) |
| `/api/v1/users/me` | `PUT` | Authenticated (`AuthorizationPolicies.AuthenticatedUser`) |
| `/api/v1/admin/users/{id}/role` | `PUT` | Admin-only (`AuthorizationPolicies.AdminOnly`) |

## Query and Field Naming

- Use `camelCase` for query parameters (example: `pageSize`, `correlationId`).
- Use `camelCase` for JSON field names in request and response payloads.
- Use explicit time suffixes for UTC timestamps (example: `checkedAtUtc`).

## Success Response Shapes

### Single-resource or action response

Return an object payload directly:

```json
{
  "status": "healthy",
  "checkedAtUtc": "2026-03-10T03:00:00Z",
  "correlationId": "trace-123"
}
```

### Paginated collection response

Prefer a body shape with `items` and `pagination`:

```json
{
  "items": [],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 0,
    "totalPages": 0
  }
}
```

Optional pagination headers supported by the frontend client:

- `x-page`
- `x-page-size`
- `x-total-count`
- `x-total-pages`

## Error Response Shape

Use a standard error envelope:

```json
{
  "error": "validation_failed",
  "message": "Human-readable summary",
  "correlationId": "trace-123",
  "errors": []
}
```

Notes:

- `error` is a stable machine-readable code.
- `message` is human-readable and safe to display in logs/UI.
- `correlationId` must be present for troubleshooting and log correlation.
- `errors` is optional structured detail for validation or domain-level failures.

Common top-level API error codes:

- `validation_failed`
- `unauthorized`
- `forbidden`
- `internal_server_error`

Common auth-related validation/detail codes:

- `email.invalid`
- `email.duplicate`
- `email.alreadyVerified`
- `auth.invalidCredentials`
- `auth.unauthenticated`
- `auth.userNotFound`
- `token.invalid`
- `token.invalidOrExpired`

## Frontend Consumption Conventions

- Resolve base URL from `VITE_API_BASE_URL` via `src/web/src/shared/config/env.ts`.
- Use `src/web/src/shared/api/httpClient.ts` for all API calls.
- Use `ApiClientError` for consistent error handling.
- Use `getPage` for list flows requiring standardized pagination metadata.
