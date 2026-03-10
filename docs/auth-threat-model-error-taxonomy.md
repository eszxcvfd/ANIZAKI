# Auth Threat Model and Error Taxonomy

This document is the canonical security baseline for bead `bd-12f.1.3`.
It defines key abuse scenarios for auth flows and standardized error codes that downstream layers must use consistently.

## Scope

- Applies to auth APIs and route guards in epic `bd-12f`.
- Complements:
  - [docs/auth-token-session-lifecycle-matrix.md](./auth-token-session-lifecycle-matrix.md)
  - [docs/auth-role-policy-route-matrix.md](./auth-role-policy-route-matrix.md)
- Focus is on practical v1 protections and deterministic error behavior.

## Trust Boundaries

- Client boundary: browser/mobile app is untrusted.
- API boundary: `/api/v1/auth/*`, `/api/v1/users/me`, `/api/v1/admin/*`.
- Token boundary: access/refresh/verify/reset tokens are sensitive credentials.
- Persistence boundary: hashed secret material must stay server-side only.

## Threat Matrix

| Threat | Attack Surface | Impact | Baseline Mitigations | Verification Expectation |
| --- | --- | --- | --- | --- |
| Token replay | Access/refresh token reuse | Account takeover or unauthorized actions | Short-lived access tokens, single-use refresh tokens, replay detection revokes session chain | Replay test must return auth failure and revoke affected session(s) |
| Credential brute force | Login endpoint | Unauthorized access attempts | Rate limiting, bounded failure responses, optional temporary lockout in hardening phase | Repeated invalid login attempts eventually return rate-limit error |
| Privilege escalation | Admin endpoints and role updates | Unauthorized admin actions | Policy-based authorization (`AdminOnly`), role claim validation, deny-by-default | Non-admin access to admin endpoint must return `403` |
| Reset-token abuse | Forgot/reset flows | Password takeover | One-time reset tokens, short TTL, revoke all sessions on successful reset | Reused or expired reset token must fail deterministically |
| Verify-token abuse | Email verification flow | Incorrect account verification | One-time verify token, expiry enforcement, replacement invalidates previous token | Second use of same verify token must fail |
| Account enumeration | Register/forgot/login error messages | User privacy leakage | Generic safe messages for account existence sensitive paths | Responses must not reveal whether email exists |
| Session fixation/hijack | Login/refresh lifecycle | Session theft | Session ID regeneration on login, refresh rotation, logout and reset revoke sessions | Post-revocation token use must fail |
| Correlation loss during incidents | Error handling and logs | Delayed incident response | Include correlation metadata in error/log pipeline | Error responses and logs must carry correlation reference where applicable |

## Auth Error Taxonomy

All auth-related error responses must follow the standard envelope:

```json
{
  "error": "machine_readable_code",
  "message": "Safe human-readable summary",
  "errors": []
}
```

| Error Code | HTTP Status | When Used | Client Behavior |
| --- | --- | --- | --- |
| `validation_failed` | `400` | Input validation failure. | Show form-level validation hints. |
| `auth_invalid_credentials` | `401` | Login credentials invalid. | Show generic login failure message. |
| `auth_account_unverified` | `403` | Account must verify email before protected action. | Prompt verify-email flow. |
| `auth_token_expired` | `401` | Access or one-time token expired. | Attempt refresh if applicable, else re-auth/retry flow. |
| `auth_token_invalid` | `401` | Token malformed, tampered, or unknown. | Clear auth context and require login. |
| `auth_token_replayed` | `401` | Refresh token replay detected. | Force full re-auth and show security warning. |
| `auth_session_revoked` | `401` | Session revoked or logged out. | Clear auth state and route to login. |
| `auth_forbidden` | `403` | Authenticated user lacks required role/policy. | Show access denied UX. |
| `auth_reset_token_invalid` | `400` | Reset token malformed or not recognized. | Keep user in reset flow with retry option. |
| `auth_reset_token_expired` | `400` | Reset token expired. | Prompt user to request new reset token. |
| `auth_verify_token_invalid` | `400` | Verify token invalid or already used. | Offer resend verification option. |
| `auth_rate_limited` | `429` | Request throttled due to abuse controls. | Respect cooldown/backoff messaging. |
| `internal_server_error` | `500` | Unexpected system failure. | Show generic fallback and retry guidance. |

## Error-Handling Rules

- Never include secrets, hashes, token values, or internal stack traces in `message` or `errors`.
- For account-sensitive flows (forgot/reset/register), message content must avoid revealing account existence.
- Error code strings are stable contracts; wording in `message` may change without breaking clients.
- `auth_forbidden` and `auth_session_revoked` are distinct and must not be collapsed.

## Logging and Correlation Requirements

- Emit structured logs with:
  - `correlationId`
  - `userId` (when available)
  - `sessionId` (when available)
  - `errorCode`
  - `policyName` (for authorization failures)
- Keep log redaction for sensitive payload fields by default.

## Test Traceability

- `bd-12f.5.5`: consistent error envelope + correlation metadata.
- `bd-12f.7.2`: API integration tests for error code/status correctness.
- `bd-12f.7.3`: authorization-negative tests (`403` matrix).
- `bd-12f.7.4`: frontend behavior mapping for `401`/`403`/`429`.

