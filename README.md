# Anizaki Monorepo

Monorepo này là workspace triển khai cho:

- `src/api`: backend `.NET 8` theo Clean Architecture
- `src/web`: frontend `React + TypeScript + Vite + Tailwind CSS`

README này phản ánh **tiến độ thực tế của codebase tại ngày 2026-03-11**, sau khi hoàn thành loạt refactor auth UI và tăng cường backend infrastructure.

---

## Tiến độ hiện tại

### Đã hoàn thành

**Nền tảng & cơ sở hạ tầng**

- Khung monorepo, tài liệu vận hành, và quy ước phát triển đã được thiết lập.
- Backend Clean Architecture đã chạy được end-to-end:
  - `Domain`, `Application`, `Infrastructure`, `Api` tách lớp rõ ràng.
  - Global exception handling middleware (`ExceptionHandlingMiddleware`) với `ApiErrorEnvelope` nhất quán — không rò rỉ stack trace ra ngoài.
  - CorrelationID tracking theo từng request.
  - Endpoint nền tảng: `GET /health`, `GET /api/v1/system/status`

**Auth backend**

- HTTP surface đầy đủ:
  - `POST /api/v1/auth/register`
  - `POST /api/v1/auth/login` — rate-limited
  - `POST /api/v1/auth/logout` (authenticated)
  - `POST /api/v1/auth/forgot-password` — rate-limited
  - `POST /api/v1/auth/reset-password` — rate-limited
  - `POST /api/v1/auth/verify-email`
  - `GET /api/v1/users/me` (authenticated)
  - `PUT /api/v1/users/me` (authenticated)
  - `PUT /api/v1/admin/users/{id}/role` (admin-only)
- Rate limiting cấu hình qua `appsettings.json` theo từng endpoint nhạy cảm.
- Auth adapters: password hashing, token issuing, email sender (no-op), current-user context, in-memory user/session repositories.

**Frontend**

- Design system đã được cập nhật toàn diện (light theme, HSL color tokens, glassmorphism utilities, typography, micro-animations).
- App shell (`AppShell`) và route resolution với role-aware guard:
  - `/profile` yêu cầu authenticated
  - `/admin/console` yêu cầu admin role
- Shared UI primitives: `Button` (variant: primary/outline/ghost), `Input`, `Card`, `Stack`, `PageContainer`.
- Auth flow từ monolithic panel đã được tách thành **các trang riêng biệt**:
  - `LoginPage` — đăng nhập với inline validation và loading states
  - `RegisterPage` — đăng ký với inline validation
  - `ForgotPasswordPage` — quên mật khẩu
  - `ResetPasswordPage` — đặt lại mật khẩu qua token
  - `VerifyEmailPage` — xác thực email
- `AuthLayout` component dùng chung cho toàn bộ auth pages.
- `AuthFormComponents` — library component reusable: `AuthInput`, `AuthButton`, `AuthStatusMessage`.
- `SessionBootstrapCard` — đồng bộ với light theme mới.
- `ProfilePanel` — load/update profile (`GET/PUT /api/v1/users/me`) với success/error states.
- `SystemStatusCard` — smoke check đến `/health` API với visual feedback.
- `HomePage`, `ForbiddenPage`, `NotFoundPage` — đã được cập nhật với premium design.
- `DevBootstrapPage` — trang nội bộ hỗ trợ onboarding và khởi tạo session cho developer.
- Shared HTTP client, env contract với `VITE_API_BASE_URL`.

### Chưa hoàn thành

- Chưa có persistence thật (hiện auth infrastructure vẫn đang ở mức in-memory adapter).
- Chưa có business module đầu tiên như catalog/inventory/order.
- Chưa có deployment pipeline, observability baseline hoàn chỉnh, hoặc production-ready token/session flow.

---

## Kiến trúc hiện tại

### Backend `src/api`

Các layer:

| Layer | Trách nhiệm |
|---|---|
| `Anizaki.Domain` | Primitives, exceptions, user aggregate, auth value objects |
| `Anizaki.Application` | Contracts, validators, handlers, messaging abstractions |
| `Anizaki.Infrastructure` | Auth adapters, system status probe, dependency wiring |
| `Anizaki.Api` | Composition root, middleware, HTTP endpoints, Swagger |

Chiều phụ thuộc:

```
Api → Application, Infrastructure
Infrastructure → Application, Domain
Application → Domain
Domain → (không phụ thuộc layer nội bộ nào)
```

### Frontend `src/web`

Cấu trúc feature-based:

| Thư mục | Vai trò |
|---|---|
| `app/` | Bootstrapping, AppShell, route definitions |
| `pages/` | Các trang gắn với route (auth, home, profile, admin, dev, ...) |
| `features/` | Luồng chức năng có state (auth, profile, system) |
| `entities/` | Thành phần reusable theo domain |
| `shared/` | HTTP client, config, UI primitives, utility functions |

#### Cấu trúc `pages/`

```text
pages/
  auth/
    ForgotPasswordPage.tsx
    LoginPage.tsx
    RegisterPage.tsx
    ResetPasswordPage.tsx
    VerifyEmailPage.tsx
  admin/
  dev/
    DevBootstrapPage.tsx
  forbidden/
  home/
  not-found/
  profile/
```

#### Cấu trúc `features/auth/`

```text
features/auth/
  api/          ← HTTP wrappers cho auth endpoints
  model/        ← State models
  session/      ← SessionBootstrapCard
  ui/
    AuthFormComponents.tsx
    AuthLayout.tsx
  types.ts      ← Shared auth types/interfaces
```

---

## Trạng thái xác minh gần nhất

Đã xác minh local bằng:

```powershell
powershell -File scripts/verify-local.ps1 -SkipRestore
```

Kết quả:

- Backend build: **pass**
- Backend tests: **pass**
  - `Anizaki.Domain.Tests`: 29 tests passed
  - `Anizaki.Application.Tests`: 47 tests passed
  - `Anizaki.Architecture.Tests`: 6 tests passed
  - `Anizaki.Api.Tests`: 38 tests passed
- Frontend lint: **pass**
- Frontend tests: **pass**
  - `8` test files passed / `32` tests passed
- Frontend build: **pass**

Để chạy full matrix có restore:

```powershell
powershell -File scripts/verify-local.ps1
```

---

## Công cụ đã xác minh

- `.NET SDK 8.0.417`
- `Node v25.4.0`
- `pnpm 10.28.2`

---

## Thiết lập môi trường local

Reserved ports:

| Service | Port |
|---|---|
| API HTTP | `5080` |
| API HTTPS | `7080` |
| Web dev server | `5173` |

Quy ước biến môi trường:

- Backend/repository keys dùng prefix `ANIZAKI_`
- Frontend-exposed keys dùng prefix `VITE_`
- Commit `.env.example`, **không commit** `.env`

Các file mẫu:

- `src/api/.env.example`
- `src/web/.env.example`

---

## Cài đặt

1. Cài toolchain theo đúng version hoặc tương đương tương thích.

2. Restore backend dependencies:

```powershell
dotnet restore src/api/Anizaki.Api.sln
```

3. Cài frontend dependencies:

```powershell
pnpm.cmd --dir src/web install
```

4. Copy file mẫu môi trường nếu cần local overrides:

```powershell
cp src/api/.env.example src/api/.env
cp src/web/.env.example src/web/.env
```

---

## Chạy local

### Backend API

```powershell
dotnet run --project src/api/src/Anizaki.Api --urls http://localhost:5080
```

### Frontend app

```powershell
pnpm.cmd --dir src/web dev
```

### Smoke check nhanh

Khi backend đang chạy:

```powershell
Invoke-RestMethod http://localhost:5080/health
Invoke-RestMethod "http://localhost:5080/api/v1/system/status?correlationId=manual-smoke"
```

---

## Cấu trúc thư mục chính

```text
src/
  api/
    src/
      Anizaki.Api/
        Errors/        ← ExceptionHandlingMiddleware
      Anizaki.Application/
      Anizaki.Domain/
      Anizaki.Infrastructure/
        Auth/
    tests/
      Anizaki.Api.Tests/
      Anizaki.Application.Tests/
      Anizaki.Architecture.Tests/
      Anizaki.Domain.Tests/
  web/
    src/
      app/             ← AppShell, routes
      entities/
      features/
        auth/          ← api, model, session, ui, types.ts
        profile/
        system/
      pages/
        auth/          ← LoginPage, RegisterPage, ...
        dev/           ← DevBootstrapPage
        home/
        profile/
        ...
      shared/
        ui/            ← Button, Input, Card, Stack, PageContainer
```

---

## Tài liệu liên quan

- `docs/local-environment-contract.md`
- `docs/api-contract-guidelines.md`
- `docs/auth-role-policy-route-matrix.md`
- `docs/auth-threat-model-error-taxonomy.md`
- `docs/auth-token-session-lifecycle-matrix.md`
- `docs/feature-addition-playbook.md`
- `docs/verification-evidence.md`
- `docs/handoff-dossier.md`
- `src/api/README.md`

---

## Quy trình phát triển

Issue tracking:

```bash
br ready --json            # lấy việc sẵn sàng làm
br update <id> --status in_progress   # claim
br close <id> --reason "..."          # đóng việc
br sync --flush-only                  # flush tracker state
```

Prioritization:

```bash
bv --robot-triage
bv --robot-next
```

---

## Hướng phát triển tiếp theo

Ưu tiên hợp lý hiện tại:

1. **Chuyển auth/persistence** từ in-memory sang storage thật (MongoDB đã có connection string cấu hình sẵn).
2. **Triển khai business module đầu tiên** (catalog/inventory/order) để xuyên suốt đủ 4 layer backend + UI frontend.
3. **Observability baseline**: structured logging, distributed tracing, health check endpoint chi tiết hơn.
4. **Deployment pipeline**: Dockerfile, CI/CD, staging environment.

---

## Ghi chú cho contributor

- Giữ chặt boundary giữa các layer; đừng để dependency "đi dạo" sai tầng.
- Ưu tiên diff nhỏ, reversible, và có verification evidence sau khi đổi.
- Đồng bộ tài liệu với code thật; README không nên sống trong vũ trụ song song.
- Mọi thay đổi UI phải qua design system — không dùng ad-hoc color/spacing.
