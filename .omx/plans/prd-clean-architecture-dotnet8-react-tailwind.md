# PRD: Clean Architecture .NET 8 API + React/Tailwind Web

## Requirements Summary

- Build a backend in `src/api` using .NET 8 and Clean Architecture.
- Build a frontend in `src/web` using React and Tailwind CSS.
- Keep the codebase organized so backend and frontend can evolve independently inside the same repository.
- Start from the current workspace state where `src/api` and `src/web` exist but are empty.
- Use a structure that is easy to test, easy to extend, and safe for future feature work.

## Current Workspace Facts

- `src/api` exists and is currently empty.
- `src/web` exists and is currently empty.
- `.NET SDK 8.0.417` is available in the environment.
- `Node v25.4.0` is available in the environment.
- `pnpm.cmd 10.28.2` is available in the environment.

## Architecture Decision Record

### Decision

Use a monorepo with two app roots:

- `src/api` for the backend solution
- `src/web` for the frontend app

Use Clean Architecture for the backend and feature-oriented UI structure for the frontend.

### Drivers

- Strong separation of business rules from infrastructure code
- Easy onboarding and maintainability for future modules
- Independent build, test, and deploy paths for API and web

### Alternatives Considered

1. Single ASP.NET project plus embedded frontend
2. Backend layered by folders only
3. Separate repositories for API and web

### Why Chosen

- Clean Architecture gives clearer dependency rules than a folder-only layering approach.
- Keeping `api` and `web` separate avoids coupling release cycles.
- A monorepo keeps local development and shared documentation simple at this stage.

### Consequences

- Slightly more initial setup effort
- More projects and folders to maintain
- Better long-term control over dependencies, testing, and feature boundaries

### Follow-ups

- Decide the first business module to implement after scaffolding
- Decide authentication approach before adding protected flows
- Add CI after local bootstrap is stable

## Target Structure

```text
src/
  api/
    Anizaki.Api.sln
    src/
      Anizaki.Api/
      Anizaki.Application/
      Anizaki.Domain/
      Anizaki.Infrastructure/
    tests/
      Anizaki.Api.Tests/
      Anizaki.Application.Tests/
      Anizaki.Domain.Tests/
      Anizaki.Architecture.Tests/
  web/
    package.json
    vite.config.ts
    tailwind.config.ts
    postcss.config.js
    src/
      app/
      pages/
      widgets/
      features/
      entities/
      shared/
```

## Scope

### In Scope

- Scaffold backend solution and projects under `src/api`
- Wire project references to enforce Clean Architecture direction
- Configure backend dependency injection, configuration, and persistence entry points
- Scaffold React app with Tailwind under `src/web`
- Establish frontend folder conventions for app shell, routing, shared UI, and features
- Add baseline tests and developer commands
- Document the structure and local run steps

### Out of Scope

- Shipping a complete business feature set
- Authentication and authorization implementation
- Production deployment infrastructure
- Complex state management selection beyond a minimal foundation

## Acceptance Criteria

1. `src/api` contains a working .NET 8 solution with separate Domain, Application, Infrastructure, and Api projects.
2. Dependency direction is enforced so Domain does not depend on Infrastructure or Api.
3. `src/api/tests` contains at least one test project for unit tests and one architecture rule test project.
4. `src/web` contains a working React app with Tailwind configured and styles compiling correctly.
5. `src/web/src` is organized into stable app and feature boundaries rather than a flat component dump.
6. The repository includes documented local startup steps for running API and web together.
7. Basic verification commands exist for build, lint, and tests on both sides.

## Implementation Plan

### Phase 1: Repository Baseline

1. Confirm root conventions for line endings, ignores, and naming.
2. Add or update root docs for local development and architecture overview.
3. Reserve ports and environment variable names for local API and web development.

Files to create or update:

- `README.md`
- `.gitignore`
- `src/api/`
- `src/web/`

### Phase 2: Backend Solution Bootstrap

1. Create a .NET 8 solution file in `src/api`.
2. Create backend projects:
   - `Anizaki.Domain`
   - `Anizaki.Application`
   - `Anizaki.Infrastructure`
   - `Anizaki.Api`
3. Add test projects for domain, application, API, and architecture rules.
4. Wire project references so dependencies flow inward only:
   - Api -> Application, Infrastructure
   - Infrastructure -> Application, Domain
   - Application -> Domain
   - Domain -> no internal dependency

Files to create:

- `src/api/Anizaki.Api.sln`
- `src/api/src/Anizaki.Domain/*`
- `src/api/src/Anizaki.Application/*`
- `src/api/src/Anizaki.Infrastructure/*`
- `src/api/src/Anizaki.Api/*`
- `src/api/tests/Anizaki.Domain.Tests/*`
- `src/api/tests/Anizaki.Application.Tests/*`
- `src/api/tests/Anizaki.Api.Tests/*`
- `src/api/tests/Anizaki.Architecture.Tests/*`

### Phase 3: Backend Architectural Foundations

1. Define core domain primitives and conventions in Domain.
2. Define use case contracts, DTO boundaries, and service interfaces in Application.
3. Add dependency injection registration entry points for Application and Infrastructure.
4. Configure persistence and external service adapters in Infrastructure.
5. Configure API composition root, exception handling, health endpoint, and versioned route baseline in Api.

Recommended conventions:

- Domain contains entities, value objects, domain events, and business rules
- Application contains use cases, interfaces, validation, and mapping boundaries
- Infrastructure contains data access and external integrations
- Api contains controllers or minimal endpoints, middleware, and startup composition

### Phase 4: Frontend Bootstrap

1. Create a React app in `src/web` with TypeScript and Vite.
2. Install and configure Tailwind CSS and PostCSS.
3. Add a global app shell and base route structure.
4. Create the first shared design tokens and utility classes.
5. Establish frontend folder boundaries:
   - `app` for providers, routes, app bootstrap
   - `pages` for route-level composition
   - `features` for user actions and flows
   - `entities` for reusable business-facing UI/domain slices
   - `shared` for UI kit, lib, config, hooks, assets

Files to create:

- `src/web/package.json`
- `src/web/vite.config.ts`
- `src/web/tailwind.config.ts`
- `src/web/postcss.config.js`
- `src/web/src/app/*`
- `src/web/src/pages/*`
- `src/web/src/features/*`
- `src/web/src/entities/*`
- `src/web/src/shared/*`

### Phase 5: API-Web Integration Contract

1. Define API base URL handling through environment files.
2. Create a shared HTTP client layer in the web app.
3. Add one health-check flow from web to API for smoke verification.
4. Document naming, error shape, and pagination conventions before feature work starts.

Files to create:

- `src/web/.env.example`
- `src/web/src/shared/api/*`
- `src/api/src/Anizaki.Api/*` for health endpoint baseline

### Phase 6: Quality Gates

1. Add backend build and test commands.
2. Add frontend lint and build commands.
3. Add architecture tests that fail on forbidden backend references.
4. Add minimal smoke tests for frontend render and backend health response.

### Phase 7: Documentation and Handoff

1. Document folder responsibilities and dependency rules.
2. Document how to run API and web locally.
3. Document how future features should be added without breaking boundaries.

## Suggested Command Sequence

### Backend

1. Create solution and projects in `src/api`
2. Add project references
3. Add test projects
4. Run `dotnet build`
5. Run `dotnet test`

### Frontend

1. Scaffold Vite React TypeScript app in `src/web`
2. Add Tailwind CSS and baseline styles
3. Run `pnpm.cmd install`
4. Run `pnpm.cmd build`

## Risks and Mitigations

### Risk: Backend layers get bypassed early

Mitigation:

- Add architecture tests immediately
- Keep Infrastructure references out of Domain and Application by default

### Risk: Frontend becomes a flat component bucket

Mitigation:

- Lock folder conventions before the first feature
- Review PRs against `app/pages/features/entities/shared` boundaries

### Risk: API and web drift on contract naming

Mitigation:

- Add a small shared contract note in docs
- Start with a health endpoint and one typed client path

### Risk: Tooling drift between local machines

Mitigation:

- Document required SDK and package manager versions
- Add repeatable commands in README

## Verification Steps

1. Verify the backend solution restores and builds successfully.
2. Verify all backend tests pass.
3. Verify architecture tests fail when an invalid dependency is introduced.
4. Verify the frontend installs, builds, and renders Tailwind styles.
5. Verify the frontend can call the backend health endpoint in local development.
6. Verify the documentation matches the actual folder and command layout.

## Simplifications Chosen

- React app uses Vite instead of a heavier framework to keep initial setup fast.
- Frontend architecture stays feature-oriented instead of forcing backend-style Clean Architecture onto the UI.
- Initial integration is limited to one health-check contract before business features begin.

## Recommended Next Execution Order

1. Bootstrap backend solution and test projects
2. Add backend dependency rules and architecture tests
3. Bootstrap frontend React + Tailwind
4. Wire web-to-API health check
5. Add docs and verification commands
