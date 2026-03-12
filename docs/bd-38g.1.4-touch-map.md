# Implementation Touch-Map: Library Search / Filter / Sort

**Bead ID:** `bd-38g.1.4`  
**Epic:** `bd-38g` — Library search/filter/sort epic  
**Spec parents:** `bd-38g.1.1` (query semantics), `bd-38g.1.2` (URL normalization), `bd-38g.1.3` (ordering stability)  
**Status:** documentation / planning  
**Estimated effort:** ~6–9 h implementation (see §8)  
**Last updated:** 2026-03-12  

---

## Purpose

This document is the concrete file-touch guide for implementing the full search/filter/sort feature on the library drawing list (`GET /api/v1/library/drawings`). It answers: **which files do I open, what do I change, and in what order?**

It does NOT restate the semantic rules (see `docs/library-query-semantics.md` and `docs/library-ordering-stability.md`). Implementers must read those documents first; this file maps those rules onto actual source paths and sequencing steps.

---

## 1. Backend Files to Modify

### 1.1 `src/api/src/Anizaki.Application/Features/Library/Contracts/GetDrawingListQuery.cs`

**Rationale:** Add `sortBy` and `sortDir` parameters to the query record to carry sort intent from the endpoint binding through to the repository.

**Change:**
```csharp
// Before
public sealed record GetDrawingListQuery(
    int Page,
    int PageSize,
    string? Category,
    string? Search) : IRequest<GetDrawingListResponse>;

// After
public sealed record GetDrawingListQuery(
    int Page,
    int PageSize,
    string? Category,
    string? Search,
    string? SortBy,
    string? SortDir) : IRequest<GetDrawingListResponse>;
```

**Boundary:** No business logic here — pure data-carrier record.

---

### 1.2 `src/api/src/Anizaki.Application/Features/Library/GetDrawingListQueryValidator.cs`

**Rationale:** Enforce `sortBy` and `sortDir` allowed-value contracts per `library-query-semantics.md §4`. Both are optional; when supplied they must be valid enum values.

**Change:** Add two new validation blocks after the existing `search` block:

```csharp
private static readonly IReadOnlySet<string> AllowedSortBy  =
    new HashSet<string> { "createdAt", "title", "code" };
private static readonly IReadOnlySet<string> AllowedSortDir =
    new HashSet<string> { "asc", "desc" };

// In Validate():
if (request.SortBy is not null && !AllowedSortBy.Contains(request.SortBy))
{
    errors.Add(new ValidationError("sortBy", "sortBy.invalid",
        "sortBy must be one of: createdAt, title, code."));
}
if (request.SortDir is not null && !AllowedSortDir.Contains(request.SortDir))
{
    errors.Add(new ValidationError("sortDir", "sortDir.invalid",
        "sortDir must be 'asc' or 'desc'."));
}
```

**Boundary:** Validation only. Resolving field defaults (e.g., `title → asc`) lives in the handler or repository, not here.

---

### 1.3 `src/api/src/Anizaki.Application/Features/Library/Contracts/ILibraryRepository.cs`

**Rationale:** The repository interface must accept sort intent so both in-memory and future Mongo implementations honour the same contract.

**Change:** Update `GetDrawingsAsync` signature:

```csharp
Task<(IReadOnlyCollection<DrawingItemDto> Items, int TotalItems)> GetDrawingsAsync(
    int page,
    int pageSize,
    string? categorySlug,
    string? search,
    string sortBy,          // resolved default already applied
    string sortDir,         // resolved default already applied
    CancellationToken cancellationToken);
```

The handler resolves field-default `sortDir` (per `library-ordering-stability.md §2`) before calling the repository, so the repository always receives a fully resolved pair.

---

### 1.4 `src/api/src/Anizaki.Infrastructure/Library/InMemoryLibraryRepository.cs`

**Rationale:** Implement sort logic for `createdAt`, `title`, and `code` fields with stable tie-breaking (`id` always appended last per `library-ordering-stability.md §3`).

**Change:**
- Update `GetDrawingsAsync` signature to match interface.
- Add a private `ApplySort` method or inline LINQ expression:

```csharp
private static IQueryable<DrawingRecord> ApplySort(
    IQueryable<DrawingRecord> query, string sortBy, string sortDir)
{
    bool asc = sortDir == "asc";
    IOrderedQueryable<DrawingRecord> sorted = sortBy switch
    {
        "title"     => asc ? query.OrderBy(d => d.Title)       : query.OrderByDescending(d => d.Title),
        "code"      => asc ? query.OrderBy(d => d.Code)        : query.OrderByDescending(d => d.Code),
        _           => asc ? query.OrderBy(d => d.CreatedAtUtc): query.OrderByDescending(d => d.CreatedAtUtc),
    };
    // Stable tie-break by Id (always ascending)
    return sorted.ThenBy(d => d.Id);
}
```

Filter (`category`, `search`) is applied BEFORE sort. Pagination slice is applied AFTER sort.

---

### 1.5 `src/api/src/Anizaki.Application/Features/Library/GetDrawingListHandler.cs`

**Rationale:** The handler already bridges query → repository. It must now resolve the sort-dir default and pass the two resolved sort fields.

**Change:** Before calling `_repository.GetDrawingsAsync(...)`, add:

```csharp
string resolvedSortBy  = request.SortBy ?? "createdAt";
string resolvedSortDir = request.SortDir ?? resolvedSortBy switch
{
    "title" or "code" => "asc",
    _                 => "desc",
};
```

Pass `resolvedSortBy` and `resolvedSortDir` to the repository call.

---

### 1.6 `src/api/src/Anizaki.Infrastructure/DependencyInjection.cs`

**Rationale:** No change needed for the in-memory baseline. When switching to Mongo, this file registers `MongoLibraryRepository` in place of `InMemoryLibraryRepository` (see §6 Migration Notes).

**Touch only when:** migrating to Mongo.

---

### 1.7 `src/api/src/Anizaki.Api/Program.cs` — library endpoint binding

**Rationale:** The `GET /library/drawings` minimal-API endpoint must bind the two new query-string parameters and pass them to the query.

**Change:** Update the `MapGet("/drawings", ...)` handler:

```csharp
library.MapGet("/drawings", async (
        int? page,
        int? pageSize,
        string? category,
        string? search,
        string? sortBy,       // new
        string? sortDir,      // new
        IRequestHandler<GetDrawingListQuery, GetDrawingListResponse> handler,
        CancellationToken cancellationToken) =>
    {
        var query = new GetDrawingListQuery(
            Page:     page ?? 1,
            PageSize: pageSize ?? 20,
            Category: category,
            Search:   search,
            SortBy:   sortBy,   // new
            SortDir:  sortDir); // new
        // … rest unchanged
    });
```

**No route change** — still `GET /api/v1/library/drawings`. Sort fields are query-string parameters only.

---

## 2. Frontend Files to Modify

### 2.1 `src/web/src/features/library/model/libraryQueryUtils.ts`

**Rationale:** Extend `LibraryListQueryState` and all normalization helpers with `sortBy` and `sortDir`. Parse/validate/serialize them in `parseLibraryListQuery`, `toApiParams`, `toQueryString`, and `defaultQueryState`.

**Allowed values (mirror backend):**
```typescript
export const ALLOWED_SORT_BY  = ['createdAt', 'title', 'code'] as const
export type  SortBy  = typeof ALLOWED_SORT_BY[number]

export const ALLOWED_SORT_DIR = ['asc', 'desc'] as const
export type  SortDir = typeof ALLOWED_SORT_DIR[number]
```

**Default resolution (mirror `library-ordering-stability.md §2`):**
```typescript
export function defaultSortDir(sortBy: SortBy): SortDir {
  return sortBy === 'title' || sortBy === 'code' ? 'asc' : 'desc'
}
```

**`LibraryListQueryState` additions:**
```typescript
sortBy:  SortBy
sortDir: SortDir
```

**`defaultQueryState` update:**
```typescript
{ page: 1, pageSize: 20, category: undefined, search: undefined,
  sortBy: 'createdAt', sortDir: 'desc' }
```

**`parseLibraryListQuery` update:** Validate that URL values are in allowed sets; fall back to defaults otherwise.

**`toQueryString` update:** Omit `sortBy`/`sortDir` from URL when they equal the canonical defaults (`createdAt`/`desc`) to keep URLs clean.

---

### 2.2 `src/web/src/features/library/api/libraryApi.ts`

**Rationale:** `GetDrawingListParams` and `getDrawingList` must carry `sortBy` and `sortDir` to the API.

**Change:** Extend `GetDrawingListParams`:
```typescript
export interface GetDrawingListParams {
  page?:     number
  pageSize?: number
  category?: string
  search?:   string
  sortBy?:   string   // validated upstream in libraryQueryUtils
  sortDir?:  string
}
```

Update `getDrawingList` to include them in the `query` map passed to `apiClient.getPage`.

---

### 2.3 `src/web/src/pages/library/LibraryListPage.tsx`

**Rationale:** The list page owns query state and navigation. It must expose a sort control UI and call `navigate` with updated `sortBy`/`sortDir` on change.

**New handler:**
```typescript
function handleSortChange(sortBy: SortBy, sortDir: SortDir) {
  navigate({ ...query, page: 1, sortBy, sortDir })
}
```

**UI addition:** A `<SortControl>` (or inline `<select>` pair) rendered above the drawing grid. Keeps the sort selection visible on page reload via URL state.

**Minimal implementation:** A `<select>` for `sortBy` with labels "Ngày thêm", "Tiêu đề", "Mã bản vẽ", and a toggle button or `<select>` for direction "↑ Tăng dần" / "↓ Giảm dần". Full component extraction is optional for Phase 1.

---

### 2.4 `src/web/src/features/library/components/` (new component: `SortControl.tsx`)

**Rationale:** Optional but recommended — isolates sort-control rendering and its own vitest. If inline in `LibraryListPage.tsx`, no new file is required. This file is a Phase 1 boundary decision; the component is small (~40 lines).

**Props interface (if extracted):**
```typescript
interface SortControlProps {
  sortBy:  SortBy
  sortDir: SortDir
  onChange: (sortBy: SortBy, sortDir: SortDir) => void
}
```

---

### 2.5 `src/web/src/app/routes.tsx` and `src/web/src/app/AppRoot.tsx`

**Rationale:** No route change expected for sort. The `/library` and `/library/:slug` routes already exist. Touch only if `LibraryCategoryPage` needs to propagate sort state through category navigation (likely no change).

**Touch only when:** verifying category page also honours sort (Phase 2 scope).

---

## 3. Tests to Add / Update

### 3.1 Backend — Unit Tests

**File:** `src/api/tests/Anizaki.Application.Tests/Features/Library/LibraryHandlerTests.cs`

New test cases:
- `GetDrawingList_SortByTitle_Asc_ReturnsAlphabeticOrder`
- `GetDrawingList_SortByCreatedAt_Desc_ReturnsMostRecentFirst`
- `GetDrawingList_SortByCode_Asc_ReturnsAlphabeticByCode`
- `GetDrawingList_NullSortBy_DefaultsToCreatedAtDesc`
- `GetDrawingList_SortByTitleNullSortDir_DefaultsToAsc`

**File (new or existing):** `Anizaki.Application.Tests/.../GetDrawingListQueryValidatorTests.cs`

New test cases for validator:
- `Validate_InvalidSortBy_ReturnsError`
- `Validate_InvalidSortDir_ReturnsError`
- `Validate_ValidSortPair_ReturnsNoError`
- `Validate_NullSortBy_ReturnsNoError` (optional is valid)

**File:** `src/api/tests/Anizaki.Api.Tests/LibraryEndpointsTests.cs`

New integration tests:
- `GET /drawings?sortBy=title&sortDir=asc` returns 200 with title-sorted items
- `GET /drawings?sortBy=invalid` returns 400 `validation_failed`

---

### 3.2 Frontend — Vitest

**File:** `src/web/src/features/library/model/libraryQueryUtils.test.ts`

New test cases:
- `normalizeSortBy` accepts valid values, falls back to `createdAt` on invalid
- `normalizeSortDir` accepts valid values, falls back to field default on invalid
- `parseLibraryListQuery` with `?sortBy=title` resolves `sortDir` to `asc`
- `toQueryString` omits `sortBy`/`sortDir` when defaults; includes them when non-default
- `defaultQueryState` returns `sortBy: 'createdAt', sortDir: 'desc'`

**File:** `src/web/src/pages/library/LibraryListPage.test.tsx`

New test cases:
- Sort control renders with current state
- Changing sort calls `navigate` with `page: 1` reset
- URL with `?sortBy=title&sortDir=asc` initialises sort control correctly

**File (if extracted):** `src/web/src/features/library/components/SortControl.test.tsx`

- Renders expected option labels
- `onChange` is called with correct (sortBy, sortDir) pair on selection

---

## 4. Sequencing Order (Dependency-Safe)

Implement in this order to keep each commit buildable:

1. **`GetDrawingListQuery.cs`** — add `SortBy`/`SortDir` fields (pure record change, backward compat via default-null binding)
2. **`GetDrawingListQueryValidator.cs`** — add validation rules + unit tests
3. **`ILibraryRepository.cs`** — extend interface signature
4. **`InMemoryLibraryRepository.cs`** — implement sort + unit test via handler tests
5. **`GetDrawingListHandler.cs`** — resolve defaults + pass to repository
6. **`Program.cs`** — bind new endpoint params
7. **Backend integration tests** — cover sort e2e through HTTP
8. **`libraryQueryUtils.ts`** — extend model + tests
9. **`libraryApi.ts`** — extend params type
10. **`LibraryListPage.tsx`** (+ optional `SortControl.tsx`) — render UI + tests

Each numbered step above is a valid atomic commit boundary (follow `docs/repository-conventions.md` commit style).

---

## 5. Migration Notes — In-Memory → MongoDB

When the project graduates from `InMemoryLibraryRepository` to a real Mongo-backed repository:

### 5.1 New file

`src/api/src/Anizaki.Infrastructure/Library/MongoLibraryRepository.cs`

Implements `ILibraryRepository` using the MongoDB C# driver. Sort is expressed as:

```csharp
var sort = sortBy switch
{
    "title"    => sortDir == "asc" ? Builders<DrawingDocument>.Sort.Ascending(d => d.Title)
                                   : Builders<DrawingDocument>.Sort.Descending(d => d.Title),
    "code"     => sortDir == "asc" ? Builders<DrawingDocument>.Sort.Ascending(d => d.Code)
                                   : Builders<DrawingDocument>.Sort.Descending(d => d.Code),
    _          => sortDir == "asc" ? Builders<DrawingDocument>.Sort.Ascending(d => d.CreatedAtUtc)
                                   : Builders<DrawingDocument>.Sort.Descending(d => d.CreatedAtUtc),
};
// Append stable tie-break
sort = Builders<DrawingDocument>.Sort.Combine(sort,
    Builders<DrawingDocument>.Sort.Ascending(d => d.Id));
```

### 5.2 `src/api/src/Anizaki.Infrastructure/DependencyInjection.cs`

Replace:
```csharp
services.AddSingleton<ILibraryRepository, InMemoryLibraryRepository>();
```
with:
```csharp
services.AddSingleton<ILibraryRepository, MongoLibraryRepository>();
```

Add MongoClient registration (e.g., `services.AddSingleton<IMongoClient>(...)`).

### 5.3 Configuration

Add to `appsettings.json` (non-sensitive placeholder):
```json
{
  "ConnectionStrings": {
    "MongoDb": ""
  },
  "MongoDb": {
    "DatabaseName": "anizaki",
    "LibraryCollectionName": "library_drawings"
  }
}
```

Supply the real connection string via:
- **Local dev:** User Secrets (`dotnet user-secrets set "ConnectionStrings:MongoDb" "..."`)
- **CI/CD:** Environment variable `ConnectionStrings__MongoDb`
- **Production:** Azure Key Vault / Secret Manager

### 5.4 Seeding

Reference `docs/library-seeded-dataset.md` for the canonical fixture dataset. A `SeedLibraryCommand` or a hosted service can replicate `InMemoryLibraryRepository` fixtures into Mongo on first-start in dev/staging.

### 5.5 Indexes

Create at minimum:
```js
db.library_drawings.createIndex({ categorySlug: 1 })
db.library_drawings.createIndex({ title: 1, _id: 1 })
db.library_drawings.createIndex({ code: 1, _id: 1 })
db.library_drawings.createIndex({ createdAtUtc: -1, _id: 1 })
db.library_drawings.createIndex({ "$**": "text" })   // full-text search on title/description/tags
```

---

## 6. Security Note — Exposed Credential

**CRITICAL:** `src/api/src/Anizaki.Api/appsettings.Development.json` currently contains a plaintext MongoDB connection string (including password). This credential is tracked in git history.

**Required actions (outside this bead, but must not be deferred):**

1. **Revoke / rotate** the exposed credential in the MongoDB Atlas dashboard immediately.
2. **Remove** the connection string value from `appsettings.Development.json` — replace with an empty string or placeholder:
   ```json
   { "ConnectionStrings": { "MongoDb": "" } }
   ```
3. **Add** the real connection string to `.NET User Secrets` locally:
   ```bash
   dotnet user-secrets set "ConnectionStrings:MongoDb" "<new-connection-string>" \
       --project src/api/src/Anizaki.Api
   ```
4. **Add** `appsettings.Development.json` connection string key to the `.gitignore` if the file itself must remain tracked — or ensure the secrets rotation is complete before merging.
5. **Audit** git history for other exposed secrets (`git log -S "mongodb+srv://" --all`).

Do NOT use the current exposed credential for any new environment or production rollout.

---

## 7. Acceptance Criteria

The bead `bd-38g.1.4` is done when:

- [ ] This touch-map file exists at `docs/bd-38g.1.4-touch-map.md` and is committed to `main`.
- [ ] All referenced file paths exist or are confirmed new-to-create.
- [ ] No existing file has been modified by this bead (documentation-only change).
- [ ] Future implementers can read this file and identify every file to open without inspecting any other planning document.

The downstream execution beads (`bd-38g.1`, `bd-38g.3.1`, `bd-38g.2.1`) may treat this file as a concrete checklist and tick off each touch point as they are implemented.

---

## 8. Estimated Effort

| Area | Subtask | Estimate |
|------|---------|----------|
| Backend | `GetDrawingListQuery` + `Validator` + tests | 1 h |
| Backend | `ILibraryRepository` + `InMemoryLibraryRepository` sort + tests | 1.5 h |
| Backend | `GetDrawingListHandler` default resolution + `Program.cs` binding | 0.5 h |
| Backend | Integration tests (`LibraryEndpointsTests`) | 0.5 h |
| Frontend | `libraryQueryUtils.ts` extensions + tests | 1 h |
| Frontend | `libraryApi.ts` + `LibraryListPage.tsx` sort UI | 1 h |
| Frontend | `SortControl.tsx` (optional extraction) + tests | 0.5 h |
| Docs / review | This document + PR review | 0.5 h |
| **Total** | | **~6.5 h** |

Buffer to 9 h if Mongo migration is included in the same sprint.

---

## 9. Suggested Next Bead Picks

After this bead is closed, the highest-value next picks are:

| Bead | Title | Reason |
|------|-------|--------|
| `bd-38g.1` | Phase 1: Search/filter/sort semantics baseline | Parent epic — all semantic specs now documented; begin execution |
| `bd-38g.3.1` | (sorting UI component work) | Directly uses this touch-map §2.4 |
| `bd-38g.2.1` | (Mongo repository implementation) | Directly uses this touch-map §5 migration notes |

Pick `bd-38g.1` first to close the semantic baseline, then proceed to `bd-38g.3.1` (frontend sort UI) and `bd-38g.2.1` (backend persistence) in parallel if two agents are available.
