# Verification Evidence

Captured on: 2026-03-10

## Full Matrix Command

```powershell
powershell -File scripts/verify-local.ps1 -SkipRestore
```

## Backend Results

- `dotnet restore src/api/Anizaki.Api.sln`: success
- `dotnet build src/api/Anizaki.Api.sln`: success (`0 Warning(s)`, `0 Error(s)`)
- `dotnet test src/api/Anizaki.Api.sln`: success
  - `Anizaki.Domain.Tests`: 29 passed
  - `Anizaki.Application.Tests`: 47 passed
  - `Anizaki.Architecture.Tests`: 6 passed
  - `Anizaki.Api.Tests`: 38 passed

## Frontend Results

- `pnpm.cmd --dir src/web lint`: success
- `pnpm.cmd --dir src/web test`: success
  - `Test Files`: 8 passed
  - `Tests`: 32 passed
- `pnpm.cmd --dir src/web build`: success
  - production build emitted successfully from Vite

## Focused Auth Suites

- `dotnet test src/api/tests/Anizaki.Api.Tests/Anizaki.Api.Tests.csproj --filter "FullyQualifiedName~AuthEndpointsTests|FullyQualifiedName~AuthCompositionTests"`: success
  - `18` passed
- `dotnet test src/api/tests/Anizaki.Application.Tests/Anizaki.Application.Tests.csproj --filter "FullyQualifiedName~Auth|FullyQualifiedName~UpdateUserRoleHandlerTests"`: success
  - `36` passed
- `pnpm.cmd --dir src/web test -- src/features/auth/api/authApi.test.ts src/features/auth/session/authSession.test.ts src/features/auth/model/authFormValidation.test.ts src/features/profile/model/profileContext.test.ts src/app/routes.test.ts`: success
  - frontend auth/profile/guard suite passed (32 tests)

## Manual Smoke Notes (Release Readiness)

- UI smoke on Vite preview (`http://127.0.0.1:4173`) confirms:
  - `/` renders updated navigation and next-flow links.
  - anonymous access to `/profile` is guard-redirected to authentication route.
  - auth page renders all flows (`register/login/forgot/reset/verify`).
  - empty register submit shows inline validation alerts, focuses first invalid field, and exposes retry state.

## Notes

- Matrix completed in one CI-like local sequence with no failing steps.
- One TypeScript typing issue in auth focus helper was detected and fixed before final successful run.
