# Library Query Semantics: Filter, Search, Sort, Pagination

**Spec status:** Canonical — supersedes any earlier partial note.
**Epic bead:** `bd-38g` — Library search/filter/sort epic
**Authored by task:** `bd-38g.1.1` (Define canonical query semantics for search/filter/sort/page)
**Last updated:** 2026-03-12

This document is the authoritative semantic contract for all parameters that control the drawing list query at `GET /api/v1/library/drawings`. Both frontend query utilities and backend validator/repository consume this spec directly. No layer may add or relax rules without updating this document and its downstream beads.

---

## 1. Pagination

| Parameter  | Type      | Default | Restrictions         | Backend validation action (if invalid) |
|------------|-----------|---------|----------------------|----------------------------------------|
| `page`     | `integer` | `1`     | Must be >= 1         | `400 Bad Request` (`validation_failed`) |
| `pageSize` | `integer` | `20`    | Must be in `[1, 100]`| `400 Bad Request` (`validation_failed`) |

**Out-of-bounds page behavior:**
If `page` exceeds `totalPages`, the API MUST return `200 OK` with an empty `items` array. The `pagination` metadata reflects the actual `totalItems`/`totalPages`.

---

## 2. Filtering (`category`)

| Parameter  | Type     | Normalization                              | Match type                         | Not-found strategy                  |
|------------|----------|--------------------------------------------|------------------------------------|------------------------------------|
| `category` | `string` | Trim whitespace. Convert to lower slug.    | Exact match against `categorySlug` | Return empty `items` (no 400/404)  |

When omitted or null/empty, all categories are included.

---

## 3. Searching (`search`)

| Parameter | Type     | Normalization                      | Match type                         | Min length        |
|-----------|----------|------------------------------------|------------------------------------|--------------------|
| `search`  | `string` | Trim whitespace. Case-insensitive. | Substring OR on `title` and `code` | 1 char after trim |

Backend LINQ equivalent: `(Title ILIKE '%term%') OR (Code ILIKE '%term%')`

If the value is blank after trimming, it is discarded without error (same as omitted).

---

## 4. Sorting (`sortBy`, `sortDir`)

| Parameter | Type     | Allowed values                | Default when omitted | Backend validation action (if invalid) |
|-----------|----------|-------------------------------|----------------------|----------------------------------------|
| `sortBy`  | `string` | `createdAt`, `title`, `code`  | `createdAt`          | `400 Bad Request` (`validation_failed`) |
| `sortDir` | `string` | `asc`, `desc`                 | field default        | `400 Bad Request` (`validation_failed`) |

### Canonical default sort pair

- Full default state: `sortBy=createdAt` + `sortDir=desc`
- This is the canonical result ordering whenever the user has not explicitly chosen another sort.

### Field-specific default direction

If `sortBy` is present and `sortDir` is omitted, the backend/frontend MUST resolve `sortDir` using the field default below:

| `sortBy`     | Resolved default `sortDir` |
|--------------|----------------------------|
| `createdAt`  | `desc`                     |
| `title`      | `asc`                      |
| `code`       | `asc`                      |

### Partial-input handling

- `sortDir` without `sortBy` means: keep the canonical default field (`createdAt`) and apply the provided direction.
- `sortBy` and `sortDir` together must be interpreted exactly as provided once validated.

### Scope note

This bead defines the **primary sort contract only**. Deterministic secondary tie-breakers for pagination stability are defined by `docs/library-ordering-stability.md` (bead `bd-38g.1.3`) and must not contradict the primary field/direction rules above.

---

## 5. Combination rules

The drawings list MUST support the following combinations together without one parameter disabling another:

- `category` + `search`
- `category` + `sortBy` + `sortDir`
- `search` + `sortBy` + `sortDir`
- `category` + `search` + `sortBy` + `sortDir`
- any of the above with `page` + `pageSize`

Combination semantics:

- `category` narrows the dataset first.
- `search` further narrows the already-filtered dataset.
- `sortBy`/`sortDir` order the filtered result set.
- `page`/`pageSize` are applied after filtering and sorting.

---

## 6. Reset expectations for user-driven navigation

To keep deep-link behavior predictable and avoid empty/stranded pages after query changes:

- changing `category` MUST reset `page` to `1`
- changing `search` MUST reset `page` to `1`
- changing `sortBy` MUST reset `page` to `1`
- changing `sortDir` MUST reset `page` to `1`
- changing `pageSize` MUST reset `page` to `1`
- moving between pages MUST preserve the active `category`, `search`, `sortBy`, `sortDir`, and `pageSize`

These reset rules apply to user-initiated UI state changes. Direct deep-link requests may still arrive with any valid `page`/`pageSize` combination and must be evaluated as-is.

---

## 7. Canonical query-state examples

### Default list

```text
/library
```

Equivalent semantic state:

```text
page=1&pageSize=20&sortBy=createdAt&sortDir=desc
```

### Search within a category, first page, explicit sort

```text
/library?category=ket-cau&search=arc&sortBy=title&sortDir=asc
```

### Custom page size with canonical date-desc sort

```text
/library?pageSize=40&sortBy=createdAt&sortDir=desc
```

---

## 8. Implementation notes for downstream beads

- Frontend query utilities must normalize blank `category`/`search` away before serializing URLs.
- Backend validators must reject unsupported `sortBy`/`sortDir` values with machine-readable validation details.
- Frontend controls must treat the canonical default pair (`createdAt`, `desc`) as the initial visual state.
- Downstream beads may refine URL normalization and invalid-query fallback behavior, but they must preserve the semantic contract documented here.
