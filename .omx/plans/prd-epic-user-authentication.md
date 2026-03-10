# PRD: Epic User & Authentication (Quản lý người dùng)

> Expanded companion document:
> `C:/Users/Admin/Documents/data/2026/project/web/anizaki/.omx/plans/prd-epic-user-authentication-expanded.md`

## Requirements Summary

Epic này triển khai nền tảng quản lý người dùng và xác thực cho hệ thống Anizaki, bao gồm các user stories:

1. Đăng ký tài khoản
2. Đăng nhập / đăng xuất
3. Quên mật khẩu
4. Cập nhật profile
5. Xác minh email
6. Quản lý vai trò (`user` / `seller` / `admin`)

Mục tiêu là cung cấp auth flow đầy đủ, có kiểm soát quyền truy cập cho API và giao diện web, đồng thời giữ đúng boundary của Clean Architecture hiện có.

## Current Workspace Facts

- API hiện chỉ có `health` và `system/status` endpoint, chưa có auth flow: `src/api/src/Anizaki.Api/Program.cs`.
- Application layer đang dùng pattern `Contracts + Handler + Validator`: `src/api/src/Anizaki.Application/Features/SystemStatus/*`.
- Infrastructure layer đang đăng ký adapter qua `AddInfrastructure`: `src/api/src/Anizaki.Infrastructure/DependencyInjection.cs`.
- Frontend hiện có route baseline, shared HTTP client và smoke feature, chưa có auth feature: `src/web/src/app/routes.tsx`, `src/web/src/shared/api/httpClient.ts`, `src/web/src/features/system/*`.
- Handoff dossier xác nhận chưa có auth/authorization: `docs/handoff-dossier.md`.

## Assumptions

1. Epic này ưu tiên nền tảng auth nội bộ (không dùng social login trong scope ban đầu).
2. Môi trường hiện đã định hướng có data store qua connection string MongoDB trong backend env: `src/api/.env.example`.
3. Email gửi xác minh/reset có thể bắt đầu bằng provider abstraction + fake/in-memory adapter cho local/dev, sau đó thay provider thật.

## Scope

### In Scope

- Thiết kế domain cho user identity, role, trạng thái xác minh email, reset password.
- API endpoint cho toàn bộ user stories trong epic.
- Session/token lifecycle cho login/logout và bảo vệ endpoint cần đăng nhập.
- RBAC policy (`user`, `seller`, `admin`) ở API.
- UI/UX cơ bản cho register/login/forgot/reset/verify/profile.
- Test coverage backend + frontend cho luồng chính và luồng lỗi phổ biến.

### Out of Scope

- Social login (Google/Facebook/GitHub).
- MFA/2FA.
- Back-office quản trị role nâng cao (bulk operation, audit dashboard).
- SSO enterprise hoặc external IdP migration.

## Architecture Decision Record

### Decision

Xây auth module theo Clean Architecture hiện có:

- Domain: mô hình `User` + business rules.
- Application: use case theo từng user story.
- Infrastructure: repository + password hasher + token/email adapter.
- API: endpoint versioned `/api/v1/auth/*`, `/api/v1/users/me`, policy-based authorization.
- Web: feature slices trong `src/web/src/features/auth` và guard ở route-level.

### Drivers

1. Cần mở khóa role-based features sớm mà vẫn giữ boundary rõ ràng.
2. Cần luồng auth đủ an toàn để không phải refactor lớn về sau.
3. Cần tích hợp mượt với scaffold test và convention hiện có.

### Alternatives Considered

1. Tích hợp external auth provider ngay từ đầu.
2. Chỉ làm login/register tối giản, hoãn verify/reset/role.
3. Dùng session server-side thuần không có token lifecycle rõ ràng cho API client.

### Why Chosen

- Phương án module nội bộ theo từng lớp phù hợp trực tiếp với cấu trúc hiện tại và giảm coupling.
- Bao đủ 6 user stories ngay trong epic giúp tránh phát sinh “nợ auth” ở các module sau.
- Tách abstraction cho email/token/repository giúp thay thế hạ tầng sau này mà không phá use case.

### Consequences

- Khối lượng implementation lớn hơn so với auth tối giản.
- Cần tăng test coverage đáng kể để bảo vệ security behavior.
- Cần định nghĩa rõ lifecycle token và policy map để tránh inconsistency.

### Follow-ups

1. Sau epic này, bổ sung audit log cho hành động nhạy cảm (đổi mật khẩu, đổi role).
2. Bổ sung hardening: rate limit theo endpoint auth, lockout policy, device/session management.
3. Cân nhắc nâng cấp email provider thật ở môi trường staging/prod.

## Acceptance Criteria (Testable)

1. User mới có thể đăng ký tài khoản hợp lệ qua API và frontend form.
2. Người dùng đã đăng ký có thể đăng nhập và nhận trạng thái authenticated cho các route bảo vệ.
3. Đăng xuất làm mất hiệu lực session/token hiện tại.
4. Luồng quên mật khẩu cho phép yêu cầu reset và hoàn tất đặt lại mật khẩu bằng token hợp lệ.
5. Người dùng chưa xác minh email bị chặn ở các thao tác yêu cầu email verified (theo policy đã định nghĩa).
6. Endpoint/profile cho phép xem và cập nhật thông tin profile cơ bản khi đã đăng nhập.
7. Role-based authorization phân biệt tối thiểu 3 vai trò `user`, `seller`, `admin`.
8. Endpoint chỉ dành cho `admin` từ chối truy cập với role không phù hợp.
9. Tất cả endpoint auth/profile trả response theo convention `camelCase` + error envelope hiện có trong repo.
10. Bộ test backend/frontend cho epic pass trong local verification matrix.

## Implementation Plan

### Phase 1: Domain Foundation (User Identity + Role)

1. Tạo aggregate/value objects cho identity:
   - `UserId`, `Email`, `PasswordHash`, `Role`, `VerificationStatus`.
2. Định nghĩa invariants:
   - email unique (enforced qua repository constraint + app validation),
   - role hợp lệ,
   - state transitions cho verify/reset.
3. Bổ sung domain exceptions và semantic methods.

Planned files:

- `src/api/src/Anizaki.Domain/Users/*`
- `src/api/src/Anizaki.Domain/Exceptions/*`

### Phase 2: Application Use Cases per User Story

1. Tạo feature slice `Auth` và `Users` theo pattern hiện có:
   - Contracts, validators, handlers.
2. Use cases tối thiểu:
   - `RegisterUser`
   - `LoginUser`
   - `LogoutUser`
   - `RequestPasswordReset`
   - `ConfirmPasswordReset`
   - `VerifyEmail`
   - `GetMyProfile`
   - `UpdateMyProfile`
3. Định nghĩa application abstractions:
   - user repository/read model,
   - password hasher/verifier,
   - token service,
   - email sender abstraction,
   - current user context abstraction.
4. Đăng ký DI trong `AddApplication`.

Planned files:

- `src/api/src/Anizaki.Application/Features/Auth/*`
- `src/api/src/Anizaki.Application/Features/Users/*`
- `src/api/src/Anizaki.Application/Abstractions/*`
- `src/api/src/Anizaki.Application/DependencyInjection.cs` (update)

### Phase 3: Infrastructure Adapters

1. Implement repository adapters cho user + token state theo persistence strategy hiện hành.
2. Implement password hasher adapter.
3. Implement token/session adapter.
4. Implement email sender adapter (fake/local first, provider-ready abstraction).
5. Đăng ký toàn bộ adapter trong `AddInfrastructure`.

Planned files:

- `src/api/src/Anizaki.Infrastructure/Users/*`
- `src/api/src/Anizaki.Infrastructure/Auth/*`
- `src/api/src/Anizaki.Infrastructure/Email/*`
- `src/api/src/Anizaki.Infrastructure/DependencyInjection.cs` (update)

### Phase 4: API Surface + Authorization Policies

1. Bổ sung route groups:
   - `/api/v1/auth/*`
   - `/api/v1/users/me`
   - `/api/v1/admin/users/*` (minimum role-management endpoints for `admin`).
2. Bổ sung authentication middleware + authorization policies.
3. Chuẩn hóa response/error envelope theo guideline hiện có.
4. Cập nhật startup/config cho auth options.

Planned files:

- `src/api/src/Anizaki.Api/Program.cs` (update)
- `src/api/src/Anizaki.Api/appsettings.json` (update)
- `src/api/src/Anizaki.Api/Auth/*`

### Phase 5: Frontend Auth UX + Route Guard

1. Tạo `features/auth` cho API wrappers + UI forms:
   - register, login, forgot/reset, verify-email.
2. Tạo `features/profile` cho view/update profile.
3. Tạo auth state management nhẹ trong `app` layer.
4. Bổ sung route guards theo auth status + role.
5. Mở rộng menu/header theo user context.

Planned files:

- `src/web/src/features/auth/*`
- `src/web/src/features/profile/*`
- `src/web/src/app/routes.tsx` (update)
- `src/web/src/app/AppShell.tsx` (update)
- `src/web/src/shared/api/httpClient.ts` (reuse existing client)

### Phase 6: Tests + Docs + Verification

1. Backend tests:
   - domain invariants,
   - application validators/handlers,
   - API integration cho endpoint auth/profile/role policies,
   - architecture tests đảm bảo không vi phạm layering.
2. Frontend tests:
   - auth form behavior,
   - route guard behavior,
   - API wrapper success/error handling.
3. Cập nhật docs:
   - API contracts cho auth endpoints,
   - local env keys cho auth/email/token configs,
   - handoff notes.

Planned files:

- `src/api/tests/Anizaki.Domain.Tests/*` (update/add)
- `src/api/tests/Anizaki.Application.Tests/*` (update/add)
- `src/api/tests/Anizaki.Api.Tests/*` (update/add)
- `src/api/tests/Anizaki.Architecture.Tests/*` (update if new boundary checks needed)
- `src/web/src/**/*.test.ts*` (update/add)
- `docs/api-contract-guidelines.md` (update)
- `docs/local-environment-contract.md` (update)
- `docs/handoff-dossier.md` (update)

## Risks and Mitigations

### Risk 1: Security gaps trong token/password flow

Mitigation:

- Dùng abstraction rõ cho password hashing và token issue/validate/revoke.
- Bắt buộc có negative tests cho invalid/expired/replayed token cases.
- Áp dụng authorization policies rõ ràng thay vì check role thủ công rải rác.

### Risk 2: State drift giữa frontend auth state và backend session state

Mitigation:

- Chuẩn hóa endpoint `me` để frontend bootstrap identity từ server truth.
- Tập trung auth state ở một nơi trong `app` layer, không duplicate theo feature.

### Risk 3: Role explosion hoặc policy không nhất quán

Mitigation:

- Khóa role enum/value object ở domain.
- Chuẩn hóa mapping policy-name -> required role trong API startup.
- Test ma trận quyền tối thiểu cho `user`, `seller`, `admin`.

### Risk 4: Scope quá rộng cho một iteration

Mitigation:

- Chia implementation thành các vertical slices theo user story.
- Ưu tiên MVP endpoint + test pass trước, sau đó mở rộng UX/edge cases.

## Verification Steps

1. Chạy matrix mặc định:

```powershell
powershell -File scripts/verify-local.ps1
```

2. Chạy lại backend tests tập trung auth khi cần:

```powershell
dotnet test src/api/Anizaki.Api.sln --filter "Auth|User|Profile|Role"
```

3. Chạy frontend lint/test/build sau khi thêm auth UI:

```powershell
pnpm.cmd --dir src/web lint
pnpm.cmd --dir src/web test
pnpm.cmd --dir src/web build
```

4. Manual smoke checklist:
   - Register -> verify email -> login -> access profile -> logout.
   - Forgot password -> reset -> login bằng mật khẩu mới.
   - Kiểm tra 3 role vào các route bảo vệ khác nhau.

## Execution Sequence Recommendation

1. Domain + Application contracts (khóa business language trước).
2. Infrastructure adapters + API endpoints.
3. Frontend auth/profile flows.
4. Policy hardening + full verification + docs update.
