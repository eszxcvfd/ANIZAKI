# Feature Addition Playbook

Use this playbook when adding new product features so architecture boundaries remain intact.

## 1) Start with a Bead

- Pick work with `br ready --json` (or `bv --robot-next`).
- Claim with `br update <id> --status in_progress`.
- Keep one coherent feature slice per bead when possible.

## 2) Backend Feature Rules (`src/api`)

### Domain

- Place business invariants and core concepts in `Anizaki.Domain`.
- Avoid references to Application, Infrastructure, or Api.
- Prefer value objects/entities for business semantics.

### Application

- Add request/response contracts and validation boundaries.
- Keep orchestration in handlers/use-cases.
- Depend only on Domain abstractions and feature contracts.

### Infrastructure

- Implement external adapters (database, network, providers).
- Register adapters in `AddInfrastructure`.
- Do not reference Api.

### Api

- Expose versioned HTTP routes in `/api/v1`.
- Use `AddApplication` + `AddInfrastructure` in composition root.
- Keep transport concerns (HTTP/request mapping/exception envelope) in Api only.

## 3) Frontend Feature Rules (`src/web`)

- Keep route composition in `pages`.
- Put cross-feature API contract utilities in `shared/api`.
- Put feature-specific API wrappers in `features/<feature>/api`.
- Use `shared/config/env.ts` for runtime environment access.
- Reuse shared UI primitives from `shared/ui` before creating new variants.

## 4) Contract and Error Consistency

- Follow `docs/api-contract-guidelines.md` for naming and response shapes.
- Use `ApiClientError` in the web client for standardized error behavior.
- Keep pagination shape (`items` + `pagination`) consistent for list endpoints.

## 5) Verification Before Closing

- Run local CI-like matrix:

```powershell
powershell -File scripts/verify-local.ps1
```

- Confirm related tests for changed area are present and passing.
- Capture evidence in docs when required (for example `docs/verification-evidence.md`).

## 6) Close and Sync

- Close bead with evidence-backed reason:

```powershell
br close <id> --reason "..."
```

- Flush tracker state:

```powershell
br sync --flush-only
```
