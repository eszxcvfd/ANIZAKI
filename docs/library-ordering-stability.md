# Library Ordering and Pagination Stability Rules

**Spec status:** Canonical for bead `bd-38g.1.3`
**Epic bead:** `bd-38g` — Library search/filter/sort epic
**Depends on:** `bd-38g.1.1`, `bd-38g.1.2`
**Last updated:** 2026-03-12

This document defines deterministic ordering rules for all supported Library list sort modes. Its purpose is to prevent item drift across repeated loads, page transitions, and future storage migrations.

---

## 1. Goal

For the same dataset snapshot and same query parameters, the ordered result set MUST be identical across repeated requests.

This stability requirement applies before pagination is sliced into pages.

---

## 2. Primary sort fields

Supported primary sort fields are:

- `createdAt`
- `title`
- `code`

Supported directions are:

- `asc`
- `desc`

Defaults remain defined by `docs/library-query-semantics.md`.

---

## 3. Deterministic secondary key policy

When multiple drawings share the same primary sort value, the backend MUST apply deterministic secondary ordering rather than leaving ordering implementation-defined.

### Canonical secondary key chain

1. primary sort field + requested direction
2. `code` ascending
3. `id` ascending

`id` is the final tie-breaker of last resort and must guarantee total ordering even when upstream fields collide.

Reasoning:

- `code` is human-meaningful and stable for drawings
- `id` guarantees uniqueness and prevents pagination drift on fully-equal visible fields

---

## 4. Per-mode ordering rules

### Sort by `createdAt`

#### `sortBy=createdAt&sortDir=desc`

Order chain:

1. `createdAtUtc` descending
2. `code` ascending
3. `id` ascending

#### `sortBy=createdAt&sortDir=asc`

Order chain:

1. `createdAtUtc` ascending
2. `code` ascending
3. `id` ascending

### Sort by `title`

#### `sortBy=title&sortDir=asc`

Order chain:

1. normalized `title` ascending
2. `code` ascending
3. `id` ascending

#### `sortBy=title&sortDir=desc`

Order chain:

1. normalized `title` descending
2. `code` ascending
3. `id` ascending

### Sort by `code`

#### `sortBy=code&sortDir=asc`

Order chain:

1. normalized `code` ascending
2. `id` ascending

#### `sortBy=code&sortDir=desc`

Order chain:

1. normalized `code` descending
2. `id` ascending

For `code` sort, reusing `code` as both primary and secondary is meaningless, so `id` becomes the immediate tie-breaker.

---

## 5. Normalization before ordering

### Title ordering

For ordering purposes, `title` comparisons should be performed on a normalized comparison value that:

- trims outer whitespace
- compares case-insensitively

The stored/original value must still be returned unchanged in the response.

### Code ordering

For ordering purposes, `code` comparisons should be performed on a normalized comparison value that:

- trims outer whitespace
- compares case-insensitively

If a future implementation introduces locale-aware sorting, it must preserve the deterministic tie-break chain from this document.

---

## 6. Pagination stability rule

Pagination is applied **after** filtering and full deterministic ordering.

This means:

- page boundaries must be derived from the fully ordered list
- repeated requests for `page=2` under the same dataset snapshot must return the same item identities in the same order
- future repository implementations must not rely on storage-engine default order

If the dataset changes between requests, page contents may change. This document only guarantees stability for the same effective dataset snapshot.

---

## 7. Examples

### Example A: duplicate createdAt timestamps

If two drawings share the same `createdAtUtc`, then:

- the lower `code` sorts first
- if `code` also matches, the lower `id` sorts first

### Example B: duplicate titles differing only by case

If titles are `"ARC Layout"` and `"arc layout"`, they compare equal at the title layer and must fall through to:

1. `code` ascending
2. `id` ascending

### Example C: duplicate codes

If two drawings share the same `code`, then for `sortBy=code` both entries fall through directly to `id` ascending.

---

## 8. Downstream implementation guidance

- `InMemoryLibraryRepository` must implement explicit ordering branches for every supported sort mode.
- backend tests should include at least one duplicate-primary-value scenario per supported sort field.
- pagination tests should assert stable item order, not just item counts.
- future MongoDB/SQL implementations must encode the same tie-break chain explicitly in query/order clauses.

---

## 9. Non-goals for this bead

This bead does not define:

- locale-specific collation strategy beyond case-insensitive normalization
- live cursor pagination
- optimistic concurrency or snapshot tokens
- UI wording for sort controls

Those can evolve later as long as deterministic ordering semantics remain intact.
