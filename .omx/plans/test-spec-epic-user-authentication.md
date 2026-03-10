# Test Spec: Epic User & Authentication (Quản lý người dùng)

## Goal

Định nghĩa ma trận kiểm thử cho epic User & Authentication để đảm bảo:

- Auth flows hoạt động đúng theo user stories.
- Role-based authorization (`user` / `seller` / `admin`) nhất quán.
- Hành vi bảo mật cốt lõi được kiểm chứng bằng automated tests.

## Scope Under Test

1. Register
2. Login / Logout
3. Forgot Password + Reset Password
4. Update Profile
5. Email Verification
6. Role Management + Authorization

## Test Strategy

- Ưu tiên test nhanh và deterministic ở Domain/Application trước.
- Dùng API integration tests để xác nhận HTTP contract + policy enforcement.
- Dùng frontend tests để khóa form behavior, auth state và route guard.
- Mỗi user story có tối thiểu 1 success path + 1 failure path.

## Backend Test Matrix

### 1) Domain Tests (`src/api/tests/Anizaki.Domain.Tests`)

Coverage:

- Invariants cho email, role, verification state transitions.
- Password-reset/email-verify token state (valid/expired/used).

Representative cases:

- `Role` chỉ cho phép giá trị hợp lệ.
- Không cho phép chuyển trạng thái verify/reset trái quy tắc.
- Reject email format không hợp lệ ở domain boundary (nếu domain enforce format).

Exit criteria:

- Domain test suite pass với các invariant mới.

### 2) Application Tests (`src/api/tests/Anizaki.Application.Tests`)

Coverage:

- Validator và handler cho từng use case auth/profile.
- Quy tắc nghiệp vụ: duplicate email, login sai mật khẩu, token hết hạn, profile update hợp lệ.

Representative cases:

- Register thành công với payload hợp lệ.
- Register thất bại khi email đã tồn tại.
- Login thất bại khi credentials không hợp lệ.
- Verify email thất bại với token sai/hết hạn.
- Reset password thành công với token hợp lệ.

Exit criteria:

- Toàn bộ handler/validator tests pass.

### 3) API Integration Tests (`src/api/tests/Anizaki.Api.Tests`)

Coverage:

- HTTP endpoints cho auth/profile/role management.
- AuthN/AuthZ middleware behavior.
- Error envelope consistency (`error`, `message`, `errors`).

Representative cases:

- `POST /api/v1/auth/register` trả success shape đúng.
- `POST /api/v1/auth/login` + `POST /api/v1/auth/logout` hoạt động đúng lifecycle.
- `POST /api/v1/auth/forgot-password` và `POST /api/v1/auth/reset-password` xử lý đúng.
- `GET/PUT /api/v1/users/me` yêu cầu authenticated context.
- Endpoint admin từ chối `user`/`seller`, chấp nhận `admin`.

Exit criteria:

- API integration suite pass.
- Không có endpoint bảo vệ nào trả sai status code cho unauthorized/forbidden cases.

### 4) Architecture Tests (`src/api/tests/Anizaki.Architecture.Tests`)

Coverage:

- Đảm bảo Auth feature mới không phá dependency direction:
  - Domain không phụ thuộc Application/Infrastructure/Api.
  - Application không phụ thuộc Api.
  - Infrastructure không phụ thuộc Api.

Exit criteria:

- Architecture tests pass với Auth module mới.

## Frontend Test Matrix

### 1) Auth Feature Tests (`src/web/src/features/auth/**/*.test.ts*`)

Coverage:

- Form validation behavior (register/login/forgot/reset).
- API error mapping sang UI state.
- Verify-email and auth-flow transitions.

Representative cases:

- Login form submit success dẫn tới authenticated state.
- Login fail hiển thị error hợp lệ.
- Forgot-password request success/failure hiển thị thông báo phù hợp.

Exit criteria:

- Auth feature tests pass.

### 2) Profile Feature Tests (`src/web/src/features/profile/**/*.test.ts*`)

Coverage:

- Load profile từ endpoint `me`.
- Update profile success/failure state.

Exit criteria:

- Profile tests pass.

### 3) App/Route Guard Tests (`src/web/src/app/**/*.test.ts*`)

Coverage:

- Route không bảo vệ: truy cập bình thường.
- Route yêu cầu login: redirect/chặn khi chưa auth.
- Route theo role: chặn role không hợp lệ, cho phép role đúng.

Exit criteria:

- Guard behavior pass cho cả `user`, `seller`, `admin`.

## Security-Oriented Negative Tests (Minimum Set)

1. Login với mật khẩu sai không leak chi tiết nhạy cảm.
2. Token verify/reset đã dùng rồi không thể tái sử dụng.
3. Token hết hạn bị từ chối rõ ràng.
4. Truy cập endpoint protected khi chưa auth trả unauthorized.
5. Truy cập endpoint admin bằng role non-admin trả forbidden.

## Lifecycle Matrix Assertions (bd-12f.1.1)

Reference source:

- `docs/auth-token-session-lifecycle-matrix.md`

Minimum assertions that must exist in automated suites:

1. Access token hết hạn -> refresh thành công -> request gốc retry thành công.
2. Refresh token dùng lại lần hai -> bị từ chối và session bị revoke.
3. Logout current session -> refresh/access từ session đó không dùng được nữa.
4. Password reset confirm -> toàn bộ session đang active của user bị revoke.
5. Email verify token chỉ dùng được một lần (lần 2 phải fail rõ ràng).

## Role-Policy Matrix Assertions (bd-12f.1.2)

Reference source:

- `docs/auth-role-policy-route-matrix.md`

Minimum assertions that must exist in automated suites:

1. Anonymous truy cập endpoint `AuthenticatedUser` phải trả `401`.
2. Role `user` truy cập endpoint `AdminOnly` phải trả `403`.
3. Role `seller` truy cập endpoint `SellerOrAdmin` phải pass.
4. Role `admin` truy cập endpoint admin role-management phải pass.
5. Frontend guard phải redirect đúng cho anonymous và role mismatch.

## Threat Model and Error Taxonomy Assertions (bd-12f.1.3)

Reference source:

- `docs/auth-threat-model-error-taxonomy.md`

Minimum assertions that must exist in automated suites:

1. Login sai credential trả `401` với `error=auth_invalid_credentials`.
2. Access token hết hạn hoặc session revoked trả `401` với code phân biệt đúng (`auth_token_expired` vs `auth_session_revoked`).
3. Thiếu quyền truy cập endpoint admin trả `403` với `error=auth_forbidden`.
4. Reset/verify token đã dùng hoặc hết hạn trả `400` với error code đúng taxonomy.
5. Envelope lỗi auth luôn giữ shape `{ error, message, errors? }` và không leak dữ liệu nhạy cảm.

## Contract Verification

Áp dụng guideline hiện có:

- Route versioning dưới `/api/v1` (trừ `/health`).
- JSON fields dùng `camelCase`.
- Error response theo envelope chuẩn.

Reference:

- `docs/api-contract-guidelines.md`

## Verification Commands

### Full Matrix

```powershell
powershell -File scripts/verify-local.ps1
```

### Backend Focus

```powershell
dotnet test src/api/Anizaki.Api.sln
```

### Frontend Focus

```powershell
pnpm.cmd --dir src/web lint
pnpm.cmd --dir src/web test
pnpm.cmd --dir src/web build
```

## Exit Criteria for Epic

1. Tất cả test suites backend/frontend liên quan auth pass.
2. Mỗi user story có coverage cho success + failure path.
3. Role matrix (`user`/`seller`/`admin`) được kiểm chứng ở API và route guard.
4. Không có vi phạm architecture dependency rules.
5. Contract guideline được giữ nhất quán ở endpoint mới.

## Known Test Gaps to Track (if deferred)

1. End-to-end browser tests toàn luồng auth (nếu chưa setup e2e framework).
2. Load/performance baseline cho login/register endpoints.
3. Security hardening nâng cao (rate limit, lockout, brute-force defenses) nếu chưa implement trong iteration đầu.
