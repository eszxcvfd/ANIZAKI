# Test Spec: Clean Architecture .NET 8 API + React/Tailwind Web

## Goal

Define the minimum verification matrix for scaffolding a Clean Architecture backend in `src/api` and a React + Tailwind frontend in `src/web`.

## Test Strategy

- Use fast feedback first: build, unit tests, lint, and smoke tests
- Enforce architectural boundaries early
- Keep verification lightweight for scaffolding but strict on dependency direction

## Backend Test Matrix

### Unit Tests

Targets:

- `src/api/tests/Anizaki.Domain.Tests`
- `src/api/tests/Anizaki.Application.Tests`

Coverage goals:

- Domain rules and invariants
- Application handlers and validation logic
- Mapping and result behavior for core use cases

Exit criteria:

- `dotnet test` passes for unit test projects

### API Integration Tests

Target:

- `src/api/tests/Anizaki.Api.Tests`

Coverage goals:

- App boots successfully
- Health endpoint returns success
- Dependency injection container resolves app startup path

Exit criteria:

- Test host starts without missing dependency errors
- Health endpoint returns expected success response

### Architecture Tests

Target:

- `src/api/tests/Anizaki.Architecture.Tests`

Rules to enforce:

1. Domain must not reference Application, Infrastructure, or Api.
2. Application must not reference Api.
3. Infrastructure must not be referenced by Domain.
4. Api may depend on Application and Infrastructure only through composition root wiring.

Exit criteria:

- Architecture test suite passes
- Introducing a forbidden reference causes a test failure

## Frontend Test Matrix

### Build Verification

Target:

- `src/web`

Checks:

- Dependency install succeeds
- Production build succeeds
- Tailwind classes compile into output CSS

Exit criteria:

- `pnpm.cmd install` succeeds
- `pnpm.cmd build` succeeds

### Frontend Unit or Component Smoke Tests

Suggested target:

- `src/web/src/app`
- `src/web/src/shared/ui`

Coverage goals:

- App root renders
- Main route renders baseline content
- Shared UI wrapper components render without crashing

Exit criteria:

- Test runner passes for baseline render coverage

### API Client Smoke Test

Target:

- `src/web/src/shared/api`

Coverage goals:

- Base URL is read from environment
- Health-check client path is callable

Exit criteria:

- Mocked API client tests pass

## Manual Verification Checklist

1. Run backend locally and confirm the health endpoint responds.
2. Run frontend locally and confirm Tailwind styles appear on screen.
3. Confirm frontend can reach the backend via the configured local base URL.
4. Confirm folder structure matches the plan and no feature files are dumped at the root of `src/web/src`.

## Verification Commands

### Backend

```powershell
dotnet restore src/api/Anizaki.Api.sln
dotnet build src/api/Anizaki.Api.sln
dotnet test src/api/Anizaki.Api.sln
```

### Frontend

```powershell
pnpm.cmd install --dir src/web
pnpm.cmd build --dir src/web
```

## Quality Gates

1. No forbidden backend project references
2. No broken frontend production build
3. No missing local setup steps in documentation
4. No unverified API-web connection assumptions

## Remaining Test Gaps After Scaffold

- Real feature-level end-to-end flows
- Authentication and authorization scenarios
- Error handling contract coverage beyond the health endpoint
- Performance baselines for API and web
