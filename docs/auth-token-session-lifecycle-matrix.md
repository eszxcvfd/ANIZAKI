# Auth Token and Session Lifecycle Matrix

This document is the canonical lifecycle baseline for bead `bd-12f.1.1`.
It defines how access credentials, refresh state, and user sessions must behave across backend and frontend layers.

## Scope

- Applies to auth flows in epic `bd-12f`:
  - register
  - login/logout
  - forgot/reset password
  - email verification
  - authenticated profile (`/api/v1/users/me`)
- Assumes API contract rules in [docs/api-contract-guidelines.md](./api-contract-guidelines.md).
- Role-policy mapping is handled separately by `bd-12f.1.2` in [docs/auth-role-policy-route-matrix.md](./auth-role-policy-route-matrix.md).

## Lifecycle Matrix

| Artifact | Purpose | Issued By | Client Handling | Server Handling | Expiration | Rotation | Invalidated When |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Access token | Authorize API calls for authenticated user. | Login success and refresh success. | Sent as `Authorization: Bearer <token>`. Keep in memory by default; if persistence is required, use secure `httpOnly` cookie. | Signature validation plus session status check for protected routes. | `15m` from issue time. | Re-issued on every refresh. | Session revoked, token expired, password reset confirmation, or explicit logout-all. |
| Refresh token | Renew access token without forcing user re-login. | Login success and every refresh success. | Never store in `localStorage`; prefer secure `httpOnly` cookie. | Store only token hash + metadata (`sessionId`, `userId`, `expiresAtUtc`, `revokedAtUtc`). | `14d` absolute, with `24h` inactivity timeout. | One-time use; rotate on every successful refresh. | Logout current session, logout-all, reuse detection, password reset confirmation. |
| Session record | Canonical server-side state for auth continuity and revocation. | Created at login. | Client references indirectly through tokens only. | Persist state: `active/revoked/expired`, reason, and timestamps. | Aligns to refresh token absolute TTL. | Updated on refresh and protected activity heartbeat. | Explicit revoke, inactivity timeout, absolute expiry, security events. |
| Email verification token | Verify email ownership. | Register and resend-verify flow. | Follow one-time link/action only. | Store hash + `usedAtUtc` for single-use enforcement. | `24h` absolute. | Re-issue on resend request. | Successful verification, expiry, or replacement by newer token. |
| Password reset token | Authorize password reset operation. | Forgot-password flow. | Follow one-time link/action only. | Store hash + single-use marker. | `30m` absolute. | Re-issue on each new forgot-password request. | Successful reset, expiry, or replacement by newer token. |

## State Transitions

| Event | Access Token | Refresh Token | Session Record | Required Behavior |
| --- | --- | --- | --- | --- |
| Register success | Not issued. | Not issued. | Not created. | User remains unauthenticated until login. |
| Login success | Issue new token. | Issue new token. | Create active session. | Return auth payload using API contract envelope. |
| Access token expired | Becomes invalid. | Unchanged. | Remains active. | Client must attempt refresh once before forcing re-login. |
| Refresh success | Issue replacement token. | Rotate token and revoke previous. | Update `lastSeenAtUtc` and token hash. | Old refresh token can no longer be accepted. |
| Refresh token reuse detected | Revoke token chain. | Revoke current and suspicious descendants. | Mark compromised and revoke affected sessions. | Return machine-readable auth error code; require full login. |
| Logout current session | Invalidate by session revocation check. | Revoke current token. | Mark revoked with reason `user_logout`. | Client clears local auth state immediately. |
| Logout all sessions | Invalidate all session-linked tokens. | Revoke all user refresh tokens. | Revoke all active sessions for user. | Requires authenticated user intent; return count for observability. |
| Password reset confirmed | Revoke all session-linked tokens. | Revoke all user refresh tokens. | Revoke all active sessions for user. | Force re-authentication on all devices. |
| Email verified | Unchanged. | Unchanged. | Session unchanged. | Update user verification state only. |

## Validation and Security Rules

- Apply UTC timestamps for all comparisons.
- Allow maximum clock skew of `60s` for token validation.
- Refresh token is single-use; replay is treated as compromise.
- Forgot/reset and verify endpoints must not leak account existence details.
- Return stable machine-readable error codes in `error` field and safe summaries in `message`.

## Frontend Contract Expectations

- Protected API requests always send bearer access token.
- On `401` with token-expired semantics, perform one refresh attempt then retry the original request once.
- If refresh fails, clear auth state and route user to login flow.
- Logout success must clear in-memory auth context and any non-`httpOnly` auth hints.

## Traceability to Beads

- `bd-12f.1.1`: this matrix (source of truth).
- `bd-12f.3.1`: application abstractions must support lifecycle semantics above.
- `bd-12f.4.1` and `bd-12f.4.3`: persistence/token adapters must implement server handling and rotation rules.
- `bd-12f.5.1` to `bd-12f.5.3`: API middleware/endpoints must enforce transition and invalidation behavior.
- `bd-12f.7.1` to `bd-12f.7.3`: tests must cover success, expiry, revocation, and replay failure paths.
