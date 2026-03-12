# Library API Fallback and Invalid-Query Policy

**Spec status:** Canonical for bead `bd-38g.1.2`
**Related docs:** `docs/library-query-semantics.md`, `docs/library-url-normalization.md`, `docs/library-api-contract.md`
**Last updated:** 2026-03-12

This document defines how the Library feature distinguishes between:

- user-facing malformed deep links handled by the frontend
- direct API misuse handled by backend validation

---

## 1. Product stance

### Frontend route stance

For `/library` page URLs opened in a browser, favor recovery:

- normalize where safe
- coerce malformed query values to defaults where recovery is obvious
- rewrite to canonical URL when the page next owns navigation state

### Backend API stance

For `GET /api/v1/library/drawings`, favor strictness:

- invalid supported parameters are contract violations
- contract violations return `400 Bad Request`
- machine-readable envelope remains `validation_failed`

---

## 2. Status code policy

### Use `400`, not `422`

For this repo and this feature phase:

- query validation failures use `400`
- `422` is **not** introduced for Library list validation in this bead

Reasoning:

- existing backend validation contract already maps request validation to `400`
- introducing `422` only for Library list params would create contract drift and surprise downstream tests

---

## 3. Frontend/browser fallback matrix

| Input kind | Example | Browser-facing result |
|------------|---------|-----------------------|
| invalid page number | `page=0` | fallback to `page=1` |
| invalid page size | `pageSize=999` | fallback to `pageSize=20` |
| unknown sort field | `sortBy=newest` | fallback to `sortBy=createdAt` |
| unknown sort direction | `sortDir=down` | fallback to resolved default direction |
| whitespace search | `search=%20%20` | omit `search` |
| whitespace category | `category=%20%20` | omit `category` |
| case-only sort mismatch | `sortBy=TITLE` | normalize to `sortBy=title` |

Frontend fallback does not change backend API semantics; it prevents users from getting stranded on malformed shared links.

---

## 4. API rejection matrix

| Request kind | Example | API response |
|--------------|---------|--------------|
| unsupported sort field | `sortBy=newest` | `400 validation_failed` |
| unsupported sort direction | `sortDir=down` | `400 validation_failed` |
| invalid page lower bound | `page=0` | `400 validation_failed` |
| invalid page size bound | `pageSize=999` | `400 validation_failed` |
| blank-but-present forbidden value | `search=%20%20` when transport passes blank string | `400 validation_failed` |

Unknown extra query keys may be ignored if they are not part of the validated contract.

---

## 5. Warning header policy

This bead explicitly rejects the idea of API-level silent coercion plus warning headers for supported invalid Library query params.

Conservative rule:

- do **not** coerce invalid supported API params and continue with `200`
- do **not** require a `Warning` header for API query fallback in this phase
- keep all supported invalid query values as strict `400` contract errors

Why:

- warning-header recovery would weaken validation guarantees
- tests and downstream callers need deterministic invalid-input failure
- frontend deep-link recovery already covers the user-friendly path

---

## 6. Error envelope policy

When the API rejects invalid supported query parameters, use the standard envelope shape:

```json
{
  "error": "validation_failed",
  "message": "One or more validation errors occurred.",
  "correlationId": "trace-123",
  "errors": [
    "sortBy.invalid"
  ]
}
```

The exact `errors` detail payload may vary by field, but the top-level contract remains stable.

---

## 7. Canonical examples

### Browser recovery examples

```text
/library?page=abc&pageSize=500
=> frontend state falls back to page=1,pageSize=20
=> canonical URL becomes /library
```

```text
/library?sortBy=TITLE&sortDir=DESC
=> frontend state normalizes to sortBy=title,sortDir=desc
=> canonical URL becomes /library?sortBy=title&sortDir=desc
```

### Direct API strictness examples

```text
GET /api/v1/library/drawings?sortBy=newest
=> 400 validation_failed
```

```text
GET /api/v1/library/drawings?page=0
=> 400 validation_failed
```

---

## 8. Implementation guidance for downstream beads

- frontend query-utils tests should assert normalization and omission behavior
- page-behavior tests should assert malformed browser URLs recover safely
- API validation tests should assert unsupported `sortBy` / `sortDir` remain strict `400`
- downstream code must not introduce `422` or API warning-header fallback unless a later bead explicitly revises this policy
