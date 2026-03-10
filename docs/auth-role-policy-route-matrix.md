# Auth Role, Policy, and Protected Route Matrix

This document is the canonical authorization baseline for bead `bd-12f.1.2`.
It maps `user/seller/admin` roles to backend policies and frontend route protections.

## Scope

- Applies to auth and profile surfaces in epic `bd-12f`.
- Complements token/session lifecycle decisions in [docs/auth-token-session-lifecycle-matrix.md](./auth-token-session-lifecycle-matrix.md).
- Covers API policies and route-level access expectations.

## Role Definitions

| Role | Intent | Privileges |
| --- | --- | --- |
| `user` | Default authenticated customer account. | Own profile access and common authenticated features. |
| `seller` | Authenticated account with seller capabilities. | All `user` privileges plus seller-only features. |
| `admin` | Operational and governance role. | Full access including user role management and admin endpoints. |

Rules:

- Roles are case-insensitive at ingestion but normalized to lowercase in persisted/auth claims.
- Unknown role values are rejected at domain boundary.
- Multi-role assignment is not required for v1; one effective role per user is authoritative.

## Backend Policy Matrix

| Policy Name | Allowed Roles | Denied Roles | Notes |
| --- | --- | --- | --- |
| `Public` | anonymous + all authenticated roles | none | For health/system/auth entry endpoints that do not require identity. |
| `AuthenticatedUser` | `user`, `seller`, `admin` | anonymous | Baseline for endpoints that require a signed-in user. |
| `SellerOrAdmin` | `seller`, `admin` | `user`, anonymous | For seller control-plane surfaces. |
| `AdminOnly` | `admin` | `user`, `seller`, anonymous | For role management and admin operations. |

Default rule:

- Any endpoint not explicitly marked `Public` must deny anonymous requests.

## API Endpoint Protection Matrix

| Endpoint | Method | Policy | Rationale |
| --- | --- | --- | --- |
| `/health` | `GET` | `Public` | Environment liveness probe. |
| `/api/v1/system/status` | `GET` | `Public` | Non-sensitive status smoke endpoint. |
| `/api/v1/auth/register` | `POST` | `Public` | New account onboarding. |
| `/api/v1/auth/login` | `POST` | `Public` | Authentication entrypoint. |
| `/api/v1/auth/forgot-password` | `POST` | `Public` | Recovery initiation without prior auth. |
| `/api/v1/auth/reset-password` | `POST` | `Public` | One-time token controlled action. |
| `/api/v1/auth/verify-email` | `POST` | `Public` | One-time token controlled action. |
| `/api/v1/auth/logout` | `POST` | `AuthenticatedUser` | Requires active authenticated context to revoke current session. |
| `/api/v1/users/me` | `GET` | `AuthenticatedUser` | Read current user profile. |
| `/api/v1/users/me` | `PUT` | `AuthenticatedUser` | Update current user profile. |
| `/api/v1/admin/users/{id}/role` | `PUT` | `AdminOnly` | Role governance is restricted to admins. |

## Frontend Route Guard Matrix

| Route Category | Allowed Roles | Redirect Rule |
| --- | --- | --- |
| Public pages (`/`, login/register/forgot/reset/verify) | anonymous + all authenticated roles | None. |
| Auth-required pages (`/profile`, `/account`) | `user`, `seller`, `admin` | Anonymous users redirect to login route. |
| Seller pages (`/seller/*`) | `seller`, `admin` | Non-seller roles redirect to forbidden/access-denied page. |
| Admin pages (`/admin/*`) | `admin` | Non-admin roles redirect to forbidden/access-denied page. |

UI behavior:

- Guards rely on backend-issued role claim; never infer privileges from client-only state.
- Role mismatch errors from API must trigger guard fallback and safe UI messaging.

## Error and Status Expectations

- Anonymous access to protected endpoint: `401 Unauthorized`.
- Authenticated but insufficient role: `403 Forbidden`.
- Unknown/invalid role claim: treat as `403` and require re-auth if claim is malformed.
- Error envelope follows [docs/api-contract-guidelines.md](./api-contract-guidelines.md).

## Test Traceability

- `bd-12f.5.1` and `bd-12f.5.4`: enforce policy registration and endpoint annotations.
- `bd-12f.6.4`: implement route guards and role-aware navigation.
- `bd-12f.7.3`: API authorization matrix tests for `user/seller/admin`.
- `bd-12f.7.4`: frontend guard tests for anonymous and role mismatch paths.

