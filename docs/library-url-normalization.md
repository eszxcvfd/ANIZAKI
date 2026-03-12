# Library URL Normalization Rules

**Spec status:** Canonical for bead `bd-38g.1.2`
**Epic bead:** `bd-38g` — Library search/filter/sort epic
**Depends on:** `bd-38g.1.1`
**Last updated:** 2026-03-12

This document defines how user-facing Library URLs are normalized before state is hydrated into the frontend list flow. It exists to keep shared links resilient for humans while preserving strict API validation for direct callers.

---

## 1. Scope

This policy applies to user-facing routes under:

- `/library`
- `/library/categories/:categorySlug`
- `/library/drawings/:drawingId`

Primary concern for this bead: query-string normalization for the list flow on `/library` and any route that preserves list-like query state.

---

## 2. Canonical route shape

### List route

Canonical path:

```text
/library
```

Canonical default query state:

```text
page=1&pageSize=20&sortBy=createdAt&sortDir=desc
```

Default-valued query parameters SHOULD be omitted from the visible URL whenever the frontend writes a canonical URL.

### Category route

Canonical path shape:

```text
/library/categories/:categorySlug
```

Rules:

- `categorySlug` is lowercase kebab-case in canonical form.
- the path segment is authoritative for category context; a duplicate `category` query param should not override it.

### Detail route

Canonical path shape:

```text
/library/drawings/:drawingId
```

Rules:

- `drawingId` must be a UUID string in canonical API-compatible format.
- query-string state may be preserved for navigation context, but must not change the identity of the detail resource.

---

## 3. Path normalization rules

### Trailing slash

- `/library/` normalizes to `/library`
- `/library/categories/ket-cau/` normalizes to `/library/categories/ket-cau`
- `/library/drawings/<id>/` normalizes to `/library/drawings/<id>`

Frontend behavior: remove trailing slash and replace browser history with the canonical path.

### Path case

- Route segments are treated case-insensitively for recovery purposes.
- Canonical emitted routes are lowercase.

Examples:

- `/Library` -> `/library`
- `/LIBRARY/CATEGORIES/KET-CAU` -> `/library/categories/ket-cau`

### Percent-encoding

- Reserved characters in query values may remain percent-encoded in the URL.
- Decoded values must be normalized before state hydration.
- Invalid percent-encoding in the browser URL is treated as malformed input and falls back to the safe default state for user-facing pages.

### Path param formats

- `categorySlug`: lowercase kebab-case canonical form
- `drawingId`: UUID string canonical form

If a path parameter is structurally malformed:

- category path: render the category route shell but resolve to an empty dataset / not-found-style category state per page policy
- drawing detail path: treat as invalid resource identity and render the existing not-found detail behavior

This bead does not redefine detail-page UX; it only constrains normalization expectations.

---

## 4. Query parameter normalization rules

### Known parameters

Known list parameters are:

- `page`
- `pageSize`
- `category`
- `search`
- `sortBy`
- `sortDir`

Unknown query parameters:

- are ignored for frontend state hydration
- are not forwarded to the API
- may remain in the browser URL until the frontend next writes a canonical URL

### Pagination

- `page` and `pageSize` are parsed as base-10 integers.
- numeric strings are accepted (`"2"`, `"40"`).
- leading/trailing whitespace is trimmed before parse.
- canonical serialized values are plain decimal integers with no leading zeros.

### Category

- trim whitespace
- lowercase
- preserve kebab-case slug meaning
- blank-after-trim becomes omitted

### Search

- trim whitespace
- preserve internal spaces
- blank-after-trim becomes omitted
- case-insensitive semantics are applied by consumers, not by URL rewriting

### Sorting

- `sortBy` is matched against canonical field names: `createdAt`, `title`, `code`
- `sortDir` is matched against canonical direction names: `asc`, `desc`
- case-insensitive recovery is allowed in the frontend (`TITLE` -> `title`, `DESC` -> `desc`)
- canonical emitted values use exact case shown above

---

## 5. User-facing malformed deep-link fallback policy

Goal: shared `/library` links should recover safely for humans whenever possible.

### Safe fallback principle

For browser-entered or shared deep links routed through the frontend:

- malformed but recoverable query values should be coerced to canonical safe defaults
- truly syntactically invalid values that cannot be meaningfully parsed should drop to safe defaults for the frontend route state
- the frontend may rewrite the URL to the canonical normalized form after hydration

### Query fallback matrix (frontend route layer)

| Parameter | Malformed example | Frontend behavior |
|-----------|-------------------|-------------------|
| `page` | `0`, `-2`, `abc` | coerce to default `1` |
| `pageSize` | `0`, `999`, `abc` | coerce to default `20` |
| `category` | whitespace only | omit parameter |
| `search` | whitespace only | omit parameter |
| `sortBy` | `newest`, `DROP TABLE` | revert to canonical default `createdAt` |
| `sortDir` | `down`, `sideways` | revert to resolved default for active/default sort field |

### Example recoveries

```text
/library?page=0&pageSize=999
-> /library
```

```text
/library?sortBy=TITLE&sortDir=DESC
-> /library?sortBy=title&sortDir=desc
```

```text
/library?search=%20%20%20&category=KET-CAU
-> /library?category=ket-cau
```

```text
/library?sortBy=unknown&sortDir=asc
-> /library?sortBy=createdAt&sortDir=asc
```

---

## 6. Direct API fallback and rejection policy

The API is stricter than the frontend route layer.

### Conservative contract

- unsupported semantic values for known validated parameters return `400 Bad Request`
- machine-readable envelope remains `validation_failed`
- this project does **not** use `422 Unprocessable Entity` for Library query validation in the current contract

### Status-code policy

| Situation | API status | Notes |
|-----------|------------|-------|
| unsupported `sortBy` / `sortDir` | `400` | strict rejection |
| invalid numeric pagination value reaching validated transport contract | `400` | strict rejection |
| blank-but-present validated string where forbidden | `400` | strict rejection |
| syntactically malformed resource id/path param | `400` or route miss -> existing endpoint behavior | do not silently reinterpret identity |
| unknown extra query keys | `200` / ignored | not part of validation contract |

### Warning header stance

The frontend route layer may silently canonicalize browser URLs.

The API layer SHOULD NOT rely on warning headers for invalid query fallback in this phase. If a request reaches `/api/v1/library/drawings` with invalid supported parameters, the safer and already-established policy is strict `400 validation_failed`.

This means the conservative default for this repo is:

- **frontend route:** coerce malformed deep links to safe defaults
- **API endpoint:** reject invalid supported params with `400`

---

## 7. Canonical omission rules

When the frontend writes a canonical URL, omit parameters that equal the default semantic state:

- omit `page=1`
- omit `pageSize=20`
- omit `sortBy=createdAt` and `sortDir=desc` when they together represent the untouched default list
- omit blank/undefined `category`
- omit blank/undefined `search`

Do not omit explicit non-default sort state.

Example:

```text
/library?sortBy=title&sortDir=asc
```

must remain explicit because it differs from the canonical default.

---

## 8. Cross-layer distinction to preserve

Downstream beads must preserve this split:

1. **Frontend resilience** for human-facing deep links
2. **Backend strictness** for direct API callers and validation tests

This distinction is intentional and must not be collapsed into a single lenient or single strict policy without an explicit follow-up decision.
