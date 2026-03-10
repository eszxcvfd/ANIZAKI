# PRD Expanded: Epic User & Authentication (Quản lý người dùng)

## 1) Context và mục tiêu tổng thể

Epic này mở rộng trực tiếp từ PRD gốc:

- `C:/Users/Admin/Documents/data/2026/project/web/anizaki/.omx/plans/prd-epic-user-authentication.md`

Mục tiêu không chỉ là thêm endpoint auth, mà là xây nền tảng identity đầy đủ để các module nghiệp vụ sau này có thể:

1. Xác định được ai đang thao tác (`authentication`).
2. Kiểm soát ai được phép làm gì (`authorization` theo role `user/seller/admin`).
3. Có cơ chế khôi phục truy cập an toàn (`forgot/reset password`, `email verification`).
4. Duy trì boundary sạch theo Clean Architecture hiện có.

Lý do cần làm ở thời điểm này:

- Codebase hiện mới có baseline health/system-status, chưa có auth flow.
- Nếu phát triển business feature trước mà chưa có auth chuẩn, sẽ phát sinh retrofit tốn kém và dễ lỗi security.
- Repo đã có test harness và conventions tốt, phù hợp để “đóng khung” auth ngay từ đầu.

## 2) Hiện trạng kỹ thuật đã điều tra

### Backend

- Kiến trúc 4 lớp đã sẵn sàng: `Domain`, `Application`, `Infrastructure`, `Api`.
- Pattern use case hiện có: `Contracts + Validator + Handler` (feature `SystemStatus`).
- API chạy minimal endpoint trong `Program.cs`, chưa có auth middleware/policy.
- Architecture tests đã enforce dependency direction.

### Frontend

- Có app shell + route resolver cơ bản (`/` và not-found).
- Có shared API client chuẩn hóa query/error/pagination.
- Chưa có auth state, guard, hay auth/profile UI.

### Docs/ops

- Có guideline API contract, env contract, workflow verify.
- Handoff xác nhận chưa có auth/authorization.

## 3) Expanded scope theo user story

### US1 - Đăng ký tài khoản

Mục tiêu:

- Người dùng tạo tài khoản mới với dữ liệu hợp lệ.

Flow chính:

1. Người dùng nhập thông tin đăng ký.
2. Backend validate payload + uniqueness email.
3. Tạo user ở trạng thái chưa xác minh email.
4. Sinh token xác minh email và gửi qua adapter email.
5. Trả response thành công theo contract.

Edge cases:

- Email trùng.
- Payload không hợp lệ.
- Email sender lỗi tạm thời.

### US2 - Đăng nhập / đăng xuất

Mục tiêu:

- Thiết lập và hủy session/token đúng vòng đời.

Flow chính:

1. Login: validate credentials -> issue token/session.
2. Frontend nhận trạng thái authenticated.
3. Logout: revoke/invalidate token/session hiện tại.

Edge cases:

- Sai mật khẩu.
- Tài khoản chưa xác minh email (nếu policy yêu cầu verify trước login hoặc trước hành động nhạy cảm).
- Token đã revoke/expired.

### US3 - Quên mật khẩu

Mục tiêu:

- Người dùng khôi phục truy cập không cần hỗ trợ thủ công.

Flow chính:

1. Request reset với email.
2. Backend sinh reset token có TTL.
3. Gửi link/reset token qua email adapter.
4. Confirm reset với token + mật khẩu mới.
5. Invalidate token đã dùng.

Edge cases:

- Token sai/hết hạn/đã dùng.
- Email không tồn tại (không leak thông tin nhạy cảm).

### US4 - Cập nhật profile

Mục tiêu:

- Người dùng đã đăng nhập có thể xem/cập nhật profile cơ bản.

Flow chính:

1. Frontend gọi endpoint `me`.
2. Hiển thị dữ liệu profile.
3. Submit cập nhật.
4. Backend validate + persist + trả dữ liệu mới.

Edge cases:

- Chưa đăng nhập.
- Payload update không hợp lệ.

### US5 - Xác minh email

Mục tiêu:

- Xác nhận quyền sở hữu email để mở quyền đầy đủ.

Flow chính:

1. User nhận token verify.
2. Gửi token lên endpoint verify.
3. Backend xác minh token + chuyển trạng thái email verified.

Edge cases:

- Token hết hạn/sai/đã dùng.
- User đã verified trước đó.

### US6 - Quản lý vai trò (`user` / `seller` / `admin`)

Mục tiêu:

- Áp chính sách truy cập theo role nhất quán.

Flow chính:

1. API định nghĩa policy map theo role.
2. Endpoint admin role-management chỉ cho `admin`.
3. Frontend guard route theo role.

Edge cases:

- Role không hợp lệ.
- Privilege escalation attempt.

## 4) Nguyên tắc thiết kế và quyết định quan trọng

1. Domain-first cho business/security invariants.
2. Application dùng abstraction để tránh leak hạ tầng vào use case.
3. API policy-based authorization, không check role rải rác.
4. Frontend lấy “auth truth” từ backend, tránh local assumptions.
5. Test-driven guardrails cho security-sensitive behavior.

## 5) File impact blueprint (theo layer)

### Domain (`src/api/src/Anizaki.Domain`)

- Thêm `Users/*` cho aggregate/value objects.
- Mở rộng exceptions cho lỗi nghiệp vụ auth.

### Application (`src/api/src/Anizaki.Application`)

- Thêm `Features/Auth/*`, `Features/Users/*`.
- Thêm abstraction cho repo/hasher/token/email/context.
- Cập nhật `DependencyInjection.cs`.

### Infrastructure (`src/api/src/Anizaki.Infrastructure`)

- Thêm adapter persistence/hashing/token/email.
- Cập nhật `DependencyInjection.cs`.

### API (`src/api/src/Anizaki.Api`)

- Cập nhật `Program.cs` để wire auth middleware + policies.
- Thêm endpoint groups `/api/v1/auth/*`, `/api/v1/users/me`, `/api/v1/admin/users/*`.

### Frontend (`src/web/src`)

- Thêm `features/auth/*`, `features/profile/*`.
- Cập nhật `app/routes.tsx`, `app/AppShell.tsx`.
- Reuse `shared/api/httpClient.ts`.

### Tests + Docs

- Mở rộng 4 test projects backend + test frontend.
- Cập nhật docs contract/env/handoff.

## 6) Hệ thống beads thực tế đã tạo (br)

Epic root:

- `bd-12f` - Epic: User & Authentication Foundation

Revision tối ưu (plan-space) đã áp dụng:

1. Tách rõ lane giá trị người dùng cốt lõi khỏi lane admin-RBAC để giảm thời gian chờ triển khai frontend.
2. Bổ sung bead `bd-12f.5.5` để chuẩn hóa error envelope và correlation metadata ở auth API.
3. Bổ sung bead `bd-12f.6.5` để nâng chất lượng UX/a11y cho auth flow thực tế người dùng.
4. Chuyển các hạng mục hardening nâng cao sang nhánh deferred `bd-12f.10.*` để không chặn MVP nhưng không bị thất lạc định hướng bảo mật.
5. Chuẩn hóa metadata tự mô tả (`design`, `acceptance_criteria`, `notes`) cho toàn bộ bead trong epic.

Parent tracks:

1. `bd-12f.1` - Auth architecture and security baseline
2. `bd-12f.2` - Domain identity model (users, roles, verification, reset)
3. `bd-12f.3` - Application use-case layer for auth and profile
4. `bd-12f.4` - Infrastructure adapters for persistence, hashing, token, email
5. `bd-12f.5` - API auth surface, middleware, and RBAC policies
6. `bd-12f.6` - Frontend auth/profile flows and route guards
7. `bd-12f.7` - Automated tests for auth domain, API, RBAC, frontend
8. `bd-12f.8` - Auth documentation and operational runbook updates
9. `bd-12f.9` - Final integration verification and release readiness

Subtasks theo track:

- Track 1:
  - `bd-12f.1.1` token/session lifecycle matrix (`docs/auth-token-session-lifecycle-matrix.md`)
  - `bd-12f.1.2` role-policy and protected route matrix (`docs/auth-role-policy-route-matrix.md`)
  - `bd-12f.1.3` threat model + auth error taxonomy (`docs/auth-threat-model-error-taxonomy.md`)
- Track 2:
  - `bd-12f.2.1` user aggregate + role/email VOs
  - `bd-12f.2.2` verification/reset token primitives
  - `bd-12f.2.3` domain transitions + invariants
- Track 3:
  - `bd-12f.3.1` application abstractions
  - `bd-12f.3.2` register/login/logout use cases
  - `bd-12f.3.3` forgot/reset/verify use cases
  - `bd-12f.3.4` profile use cases + AddApplication wiring
- Track 4:
  - `bd-12f.4.1` persistence adapters users/tokens
  - `bd-12f.4.2` hashing adapter
  - `bd-12f.4.3` token + email adapters + AddInfrastructure wiring
- Track 5:
    - `bd-12f.5.1` auth middleware + policy registration
    - `bd-12f.5.2` auth register/login/logout endpoints
    - `bd-12f.5.3` forgot/reset/verify + users/me endpoints
    - `bd-12f.5.4` admin role-management endpoints
    - `bd-12f.5.5` chuẩn hóa auth error envelope + correlation metadata
- Track 6:
    - `bd-12f.6.1` frontend auth API wrappers
    - `bd-12f.6.2` register/login/forgot/reset/verify UI
    - `bd-12f.6.3` profile UI flow
    - `bd-12f.6.4` auth bootstrap + role guards/navigation
    - `bd-12f.6.5` UX resilience + accessibility states
- Track 7:
  - `bd-12f.7.1` domain/application unit tests
  - `bd-12f.7.2` API integration tests auth/profile
  - `bd-12f.7.3` RBAC authorization matrix tests
  - `bd-12f.7.4` frontend tests auth/profile/guards
- Track 8:
  - `bd-12f.8.1` env/auth config docs
  - `bd-12f.8.2` API contract + error catalog update
  - `bd-12f.8.3` README/handoff runbook update
- Track 9:
    - `bd-12f.9.1` full verification matrix + focused auth suites
    - `bd-12f.9.2` manual smoke + release-readiness notes
- Track 10 (deferred hardening):
    - `bd-12f.10.1` rate-limiting + brute-force protection
    - `bd-12f.10.2` security audit logging
    - `bd-12f.10.3` advanced session/device management

## 7) Dependency graph rationale (critical path)

Critical path chính:

`bd-12f.1` -> `bd-12f.2` -> `bd-12f.3` -> `bd-12f.4` -> `bd-12f.5` -> `bd-12f.6` -> `bd-12f.7` -> `bd-12f.8` -> `bd-12f.9`

Cross-dependency nổi bật:

1. `bd-12f.5.4` phụ thuộc `bd-12f.2.3` để role-management dựa trên domain invariant.
2. `bd-12f.6.4` phụ thuộc `bd-12f.5.4` để route guard bám policy thật.
3. `bd-12f.7.3` phụ thuộc `bd-12f.5.4` để test ma trận quyền admin/user/seller.
4. `bd-12f.8.1` phụ thuộc cả `bd-12f.5.4` và `bd-12f.6.4` để docs phản ánh đầy đủ backend + frontend behavior.
5. `bd-12f.9.1` phụ thuộc `bd-12f.7.4` và `bd-12f.8.3` để verification chạy trên code + docs đã hoàn tất.

Tại sao cấu trúc này quan trọng:

- Buộc quyết định security được chốt sớm.
- Tránh frontend “đi trước hợp đồng” gây rework.
- Đảm bảo test/docs không bị xem là phần phụ, mà là điều kiện hoàn thành chính thức.

## 8) Tiêu chí hoàn thành ở mức Epic

Epic chỉ được đóng khi đồng thời thỏa:

1. Sáu user stories chạy end-to-end.
2. RBAC matrix hoạt động đúng với `user/seller/admin`.
3. Full verification matrix pass.
4. Không có violation kiến trúc layer dependency.
5. Docs runbook/env/contract đã đồng bộ với code thực tế.
6. Có bằng chứng manual smoke cho các luồng auth chính.

## 9) Ghi chú cho “future us”

1. Đây là nền tảng auth v1 có chủ đích “bounded scope”; đừng tự mở rộng social login/MFA trong cùng epic nếu chưa có bead mới.
2. Mọi thay đổi liên quan role/policy phải cập nhật cả backend tests (`bd-12f.7.3`) và frontend guard tests (`bd-12f.7.4`).
3. Nếu cần đổi persistence/email/token provider, sửa trong track Infrastructure trước, không phá abstraction ở Application.
4. Nếu xuất hiện security concern mới, thêm bead trong nhánh `bd-12f.1` hoặc tạo follow-up epic hardening riêng.
5. Giữ nguyên discipline: `br update` theo trạng thái, hoàn tất thì `br close`, và cuối session `br sync --flush-only`.
