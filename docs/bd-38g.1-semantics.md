# Phase 1 Semantics Baseline: Library Search / Filter / Sort

**Bead ID:** `bd-38g.1`  
**Epic:** `bd-38g` — Library search/filter/sort epic  
**Depends on:** `bd-38g.1.1`, `bd-38g.1.2`, `bd-38g.1.3`, `bd-38g.1.4`  
**Status:** canonical — supersedes earlier partial notes  
**Estimated effort:** 4 h  
**Last updated:** 2026-03-12  

---

## Purpose

This document is the single authoritative phase-1 semantics baseline for the library drawing list endpoint.  
It locks query semantics, sort contract, filtering rules, pagination, backward-compatibility guarantees, and acceptance criteria **before** implementation fans out across frontend and backend.

All downstream beads (`bd-38g.2.*`, `bd-38g.3.*`) treat this as their source of truth. No layer may add or relax rules without a PR that also updates this file.

---

## 1. API Contract — `GET /api/v1/library/drawings`

### 1.1 Request Parameters

| Parameter  | Type      | Required | Default      | Restrictions |
|------------|-----------|----------|--------------|--------------|
| `page`     | `integer` | No       | `1`          | >= 1 |
| `pageSize` | `integer` | No       | `20`         | 1 – 100 (inclusive) |
| `category` | `string`  | No       | *(all)*      | Trim + lowercase slug; blank → `400` |
| `search`   | `string`  | No       | *(none)*     | Trim; blank → `400`; max 200 chars |
| `sortBy`   | `string`  | No       | `createdAt`  | One of: `createdAt`, `title`, `code` |
| `sortDir`  | `string`  | No       | field default | One of: `asc`, `desc` |

### 1.2 Field-Default `sortDir`

When `sortBy` is present but `sortDir` is omitted, resolve using:

| `sortBy`    | Resolved `sortDir` |
|-------------|-------------------|
| `createdAt` | `desc`            |
| `title`     | `asc`             |
| `code`      | `asc`             |

When both are omitted: `sortBy=createdAt`, `sortDir=desc`.

### 1.3 Validation Failures → `400 Bad Request`

All parameter violations return `400` with body:
```json
{
  "type": "validation_failed",
  "errors": [
    { "field": "sortBy", "code": "sortBy.invalid", "message": "sortBy must be one of: createdAt, title, code." }
  ]
}
```

Unknown `sortBy` values MUST fail validation (no silent fallback to default).

### 1.4 Example Requests

```
GET /api/v1/library/drawings
    → page=1, pageSize=20, no filter, createdAt desc

GET /api/v1/library/drawings?category=kien-truc&search=Mặt&page=1&pageSize=10
    → category filter + search, default sort

GET /api/v1/library/drawings?sortBy=title&sortDir=asc
    → explicit sort by title ascending

GET /api/v1/library/drawings?sortBy=title
    → sortBy=title, sortDir resolved to asc (field default)

GET /api/v1/library/drawings?sortBy=invalid
    → 400 validation_failed (sortBy.invalid)
```

### 1.5 Response Shape (unchanged)

```json
{
  "items": [ DrawingItemDto… ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 42,
    "totalPages": 3
  }
}
```

Out-of-range `page` (> `totalPages`): `200 OK` with empty `items`; metadata reflects actual totals.

---

## 2. Sorting Contract

### 2.1 `SortBy` Enum

| Value       | Sort field         | Phase |
|-------------|-------------------|-------|
| `createdAt` | `Drawing.CreatedAtUtc` | 1 |
| `title`     | `Drawing.Title` (case-insensitive) | 1 |
| `code`      | `Drawing.Code` (case-insensitive) | 1 |

No other values are accepted in Phase 1. Future phases may add `updatedAt`, `fileSize`, etc. by updating this document and the validator.

### 2.2 `SortDir` Enum

| Value  | Meaning |
|--------|---------|
| `asc`  | Ascending |
| `desc` | Descending |

### 2.3 Stable Ordering Guarantee

For any fixed dataset snapshot and fixed query parameters, the result MUST be identical across repeated requests and across page transitions.

**Tie-break rule (mandatory):** append `id ASC` as the final sort key after the primary sort field. This guarantees stable pagination even when multiple rows share the same primary-field value (e.g., two drawings created at the same second, or two drawings with the same title).

```
ORDER BY <sortBy_field> <sortDir>, id ASC
```

This rule applies to both the in-memory repository (LINQ `.ThenBy(d => d.Id)`) and the future Mongo repository (compound sort index).

---

## 3. Filtering Semantics

### 3.1 `category` Filter

- Trim whitespace and lower-case before matching.
- Match against `categorySlug` (exact equality).
- If `category` is provided but no matching slug exists → `200 OK` with empty `items`.
- Blank/whitespace-only value → `400 validation_failed` (`category.empty`).
- Max 100 characters; over limit → `400` (`category.tooLong`).

### 3.2 `search` Filter

- Trim whitespace before matching.
- Match against `title` OR `code` (case-insensitive `contains`).
- No multi-field weighting in Phase 1 (relevance ranking is Phase 2+).
- Blank/whitespace-only value → `400 validation_failed` (`search.empty`).
- Max 200 characters; over limit → `400` (`search.tooLong`).

### 3.3 Combined Filters

When both `category` and `search` are provided, apply category filter first, then search filter within results. This is consistent with database query optimization (category narrows index scan; search narrows further).

---

## 4. Pagination Semantics

### 4.1 Strategy: Page-Based (Offset)

Phase 1 uses **page-based pagination** (offset/limit). Cursor-based pagination is out of scope for Phase 1.

| Concept | Value |
|---------|-------|
| Default page | `1` |
| Default page size | `20` |
| Max page size | `100` |
| Min page | `1` |

### 4.2 Empty / Over-Range Behavior

| Condition | Behavior |
|-----------|----------|
| `totalItems = 0` | `200 OK`, `items: []`, `totalPages: 0` |
| `page > totalPages` | `200 OK`, `items: []`, `pagination.page` reflects the requested value |
| `page = totalPages` | Normal result (may be partial page) |

### 4.3 Pagination Stability

Since sort order is deterministic (§2.3), page-based pagination is stable for any fixed dataset snapshot. Live inserts during pagination (future Mongo) may shift results; this is an accepted limitation of offset pagination. Cursor-based pagination may be adopted in a future epic.

---

## 5. Deep-Linking and URL State

URL query-string parameters are the canonical state store for the library list view. The frontend (`libraryQueryUtils.ts`) MUST:

1. Read state from URL on mount.
2. Push updated URL on every query-state change (filter, sort, page).
3. Omit parameters that equal their canonical defaults to keep URLs clean:
   - Omit `page` when `page = 1`
   - Omit `pageSize` when `pageSize = 20`
   - Omit `sortBy` + `sortDir` when both equal `createdAt` + `desc`
4. Normalize invalid URL values to defaults (no crash, no `400` from frontend).

---

## 6. Backward Compatibility

| Change | Impact | Safe? |
|--------|--------|-------|
| Adding `sortBy` / `sortDir` query params (optional, null default) | Existing callers omit them → behaviour unchanged | ✅ Yes |
| Existing `GetDrawingListQuery` callers (positional 4-arg) | New params are optional with defaults | ✅ Yes |
| `ILibraryRepository.GetDrawingsAsync` signature (future change) | Not changed in Phase 1 | ✅ N/A |
| Existing integration tests | Pass `sortBy=null`, `sortDir=null` implicitly → validator accepts | ✅ Yes |

Phase 1 introduces **no breaking changes**. The sort fields flow through the application layer (query record + validator) but are not yet forwarded to the repository; the in-memory repository continues to return results in its existing order. Forwarding to the repository is Phase 2 (`bd-38g.2.1`).

---

## 7. Acceptance Criteria

- [ ] `GetDrawingListQuery` record carries optional `SortBy` and `SortDir` fields (null → defaults resolved downstream).
- [ ] `GetDrawingListQueryValidator` accepts all valid combinations of `sortBy` and `sortDir`.
- [ ] Validator rejects unknown `sortBy` with error code `sortBy.invalid`.
- [ ] Validator rejects unknown `sortDir` with error code `sortDir.invalid`.
- [ ] All existing handler/validator unit tests still pass (zero regressions).
- [ ] New validator unit tests cover: valid pairs, null (omitted) inputs, and invalid values.
- [ ] `dotnet build` passes with zero warnings.
- [ ] This document committed to `main` under `docs/`.
- [ ] Bead `bd-38g.1` marked closed.

---

## 8. Out of Scope for Phase 1

- Forwarding `sortBy`/`sortDir` to the repository (Phase 2: `bd-38g.2.1`)
- Frontend sort UI (Phase 2: `bd-38g.3.1`)
- Mongo repository implementation
- Cursor-based pagination
- Relevance ranking for search
- Multi-field sort

---

## 9. Recommended Next Beads

| Bead | Title | Depends on |
|------|-------|-----------|
| `bd-38g.2.1` | Backend: forward sort to in-memory repo + tests | This bead |
| `bd-38g.3.1` | Frontend: sort UI in `LibraryListPage` | This bead |
| `bd-38g.2.2` | Backend: Mongo repository implementation | `bd-38g.2.1` |
