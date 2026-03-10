# Anizaki Monorepo

Monorepo này hiện là workspace triển khai cho:

- `src/api`: backend `.NET 8` theo Clean Architecture
- `src/web`: frontend `React + TypeScript + Vite + Tailwind`

README này phản ánh **tiến độ thực tế của codebase tại ngày 2026-03-10**, không còn chỉ dừng ở mức bootstrap ban đầu.

## Tiến độ hiện tại

### Đã hoàn thành

- Khung monorepo, tài liệu vận hành, và quy ước phát triển đã được thiết lập.
- Backend Clean Architecture đã chạy được end-to-end ở mức nền tảng:
  - `Domain`, `Application`, `Infrastructure`, `Api` đã tách lớp rõ ràng.
  - Có global exception envelope cho validation/runtime errors.
  - Đã expose HTTP endpoints nền tảng:
    - `GET /health`
    - `GET /api/v1/system/status`
- Auth backend foundation + HTTP surface đã có:
  - `POST /api/v1/auth/register`
  - `POST /api/v1/auth/login`
  - `POST /api/v1/auth/logout` (authenticated)
  - `POST /api/v1/auth/forgot-password`
  - `POST /api/v1/auth/reset-password`
  - `POST /api/v1/auth/verify-email`
  - `GET /api/v1/users/me` (authenticated)
  - `PUT /api/v1/users/me` (authenticated)
  - `PUT /api/v1/admin/users/{id}/role` (admin-only)
  - Hỗ trợ auth adapters: password hashing, token issuing, email sender, current-user context, in-memory user/token repositories.
- Frontend baseline đã hoạt động:
  - app shell
  - route resolution
  - home page + auth page + profile page + not-found page
  - auth session bootstrap từ login response vào route guards
  - role-aware guard fallback (`/profile` yêu cầu auth, `/admin/console` yêu cầu admin)
  - auth API wrappers + payload guards cho register/login/logout/forgot/reset/verify/profile
  - auth UI flows: register/login/forgot/reset/verify với loading/retry/inline validation + a11y feedback
  - profile UI flow: load/update `/api/v1/users/me` với success/error states
  - shared HTTP client
  - env contract với `VITE_API_BASE_URL`
  - system status card gọi smoke check đến API `/health`
- Bộ test và verification matrix đã chạy pass local.

### Chưa hoàn thành

- Chưa có persistence thật (hiện auth infrastructure vẫn đang ở mức in-memory adapter).
- Chưa có business module đầu tiên như catalog/inventory/order.
- Chưa có deployment pipeline, observability baseline hoàn chỉnh, hoặc production-ready auth/session flow.

## Kiến trúc hiện tại

### Backend `src/api`

Các layer hiện có:

- `Anizaki.Domain`: primitives, exceptions, user aggregate, auth-related value objects
- `Anizaki.Application`: contracts, validators, handlers, messaging abstractions
- `Anizaki.Infrastructure`: auth adapters, system status probe, dependency wiring
- `Anizaki.Api`: composition root, middleware, HTTP endpoints, Swagger

Chiều phụ thuộc:

- `Api -> Application, Infrastructure`
- `Infrastructure -> Application, Domain`
- `Application -> Domain`
- `Domain -> (không phụ thuộc layer nội bộ nào khác)`

### Frontend `src/web`

Cấu trúc theo feature:

- `app`: bootstrapping, app shell, route resolution
- `pages`: trang mức route
- `features`: luồng chức năng như auth/profile/system
- `entities`: thành phần reusable theo domain
- `shared`: API client, config, UI primitives, utility functions

## Trạng thái xác minh gần nhất

Đã xác minh local lại trong phiên làm việc này bằng:

```powershell
powershell -File scripts/verify-local.ps1 -SkipRestore
```

Kết quả mới nhất:

- Backend build: **pass**
- Backend tests: **pass**
  - `Anizaki.Domain.Tests`: 29 tests passed
  - `Anizaki.Application.Tests`: 47 tests passed
  - `Anizaki.Architecture.Tests`: 6 tests passed
  - `Anizaki.Api.Tests`: 38 tests passed
- Frontend lint: **pass**
- Frontend tests: **pass**
  - `8` test files passed
  - `32` tests passed
- Frontend build: **pass**

Nếu cần chạy full matrix có restore:

```powershell
powershell -File scripts/verify-local.ps1
```

## Công cụ đã xác minh

- `.NET SDK 8.0.417`
- `Node v25.4.0`
- `pnpm 10.28.2`

## Thiết lập môi trường local

Reserved ports:

- API HTTP: `5080`
- API HTTPS: `7080`
- Web dev server: `5173`

Quy ước biến môi trường:

- Backend/repository keys dùng prefix `ANIZAKI_`
- Frontend-exposed keys dùng prefix `VITE_`
- Commit `.env.example`, không commit `.env`

Các file mẫu hiện có:

- `src/api/.env.example`
- `src/web/.env.example`

## Cài đặt

1. Cài toolchain theo đúng version hoặc tương đương tương thích.
2. Restore backend dependencies:

```powershell
dotnet restore src/api/Anizaki.Api.sln
```

1. Cài frontend dependencies:

```powershell
pnpm.cmd --dir src/web install
```

1. Copy file mẫu môi trường nếu cần local overrides:

- `src/api/.env.example`
- `src/web/.env.example`

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

## Cấu trúc thư mục chính

```text
src/
  api/
    src/
      Anizaki.Api/
      Anizaki.Application/
      Anizaki.Domain/
      Anizaki.Infrastructure/
    tests/
      Anizaki.Api.Tests/
      Anizaki.Application.Tests/
      Anizaki.Architecture.Tests/
      Anizaki.Domain.Tests/
  web/
    src/
      app/
      entities/
      features/
      pages/
      shared/
```

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

## Quy trình phát triển

Issue tracking:

- `br ready --json` để lấy việc sẵn sàng làm
- `br update <id> --status in_progress` để claim
- `br close <id> --reason "..."` để đóng việc
- `br sync --flush-only` để flush tracker state

Prioritization:

- `bv --robot-triage`
- `bv --robot-next`

## Hướng phát triển tiếp theo được khuyến nghị

Ưu tiên hợp lý hiện tại:

1. Expose auth HTTP surface dựa trên foundation đã có.
2. Chuyển auth/persistence từ in-memory sang storage thật.
3. Triển khai business module đầu tiên để xuyên suốt đủ 4 layer backend + UI frontend.
4. Bổ sung observability và deployment baseline.

## Ghi chú cho contributor

- Giữ chặt boundary giữa các layer; đừng để dependency “đi dạo” sai tầng.
- Ưu tiên diff nhỏ, reversible và có evidence sau khi đổi.
- Đồng bộ tài liệu với code thật; README không nên sống trong vũ trụ song song.
