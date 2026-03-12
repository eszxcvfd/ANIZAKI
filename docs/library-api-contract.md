# Library API Resource Contract Map

This document defines the canonical JSON schema and endpoint contracts for the Library feature as part of epic `bd-1ns.1`. It implements guidelines from `docs/api-contract-guidelines.md` and applies specifically to the `/api/v1/library` route group.

## 1. Get Drawing Categories

**Endpoint:** `GET /api/v1/library/categories`
**Access:** Public (or Authenticated per future policy, currently assumed Public for read-only library)

**Request Parameters:** None

**Response Structure (200 OK):**

```json
{
  "items": [
    {
      "id": "string (uuid)",
      "name": "string",
      "slug": "string",
      "order": 1,
      "drawingCount": 42
    }
  ]
}
```

## 2. Get Drawing List

**Endpoint:** `GET /api/v1/library/drawings`
**Access:** Public 

**Query Parameters:**
- `page` (integer, optional) - Default `1`
- `pageSize` (integer, optional) - Default `20`
- `category` (string, optional) - Match category exact `slug`
- `search` (string, optional) - Partial match against title or code
- `sortBy` (string, optional) - Allowed values: `createdAt`, `title`, `code`; default field `createdAt`
- `sortDir` (string, optional) - Allowed values: `asc`, `desc`; canonical default direction `desc`

**Canonical semantics:**
- Full default list ordering is `sortBy=createdAt` + `sortDir=desc`.
- If `sortBy` is present and `sortDir` is omitted, resolve field defaults as:
  - `createdAt` -> `desc`
  - `title` -> `asc`
  - `code` -> `asc`
- If `sortDir` is present without `sortBy`, apply it to the canonical default field `createdAt`.
- User-driven changes to `category`, `search`, `sortBy`, `sortDir`, or `pageSize` reset `page` to `1`.
- User-facing malformed `/library` deep links may be normalized by the frontend to canonical safe defaults; see `docs/library-url-normalization.md`.
- Direct API misuse for supported invalid query params remains strict `400 validation_failed`; see `docs/library-api-fallback.md`.
- Deterministic secondary ordering and pagination stability rules are defined in `docs/library-ordering-stability.md`.

**Response Structure (200 OK):**

```json
{
  "items": [
    {
      "id": "string (uuid)",
      "title": "string",
      "code": "string",
      "categorySlug": "string",
      "categoryName": "string",
      "status": "string (e.g. 'published', 'draft')",
      "createdAtUtc": "string (ISO 8601)",
      "previewUrl": "string (url) | null"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 100,
    "totalPages": 5
  }
}
```

## 3. Get Drawing Detail

**Endpoint:** `GET /api/v1/library/drawings/{drawingId}`
**Access:** Public

**Path Parameters:**
- `drawingId` (string/uuid, required)

**Response Structure (200 OK):**

```json
{
  "id": "string (uuid)",
  "title": "string",
  "code": "string",
  "description": "string | null",
  "categorySlug": "string",
  "categoryName": "string",
  "status": "string",
  "tags": ["string"],
  "createdAtUtc": "string (ISO 8601)",
  "updatedAtUtc": "string (ISO 8601) | null",
  "fileInfo": {
    "fileName": "string",
    "mimeType": "string",
    "sizeBytes": 1048576,
    "checksum": "string (sha256/md5)",
    "uploadedAtUtc": "string (ISO 8601)",
    "previewAvailability": "string ('available' | 'unavailable' | 'generating')"
  }
}
```

**Error Responses:**
- `404 Not Found`:
```json
{
  "error": "not_found",
  "message": "Drawing not found.",
  "correlationId": "trace-123",
  "errors": []
}
```

## 4. Get Drawing Preview

**Endpoint:** `GET /api/v1/library/drawings/{drawingId}/preview`
**Access:** Public

**Path Parameters:**
- `drawingId` (string/uuid, required)

**Response Structure (200 OK - Info):**
*(Note: Use this to get preview metadata before attempting to render a complex viewer/embed, or it can be resolved within drawing list if simplified)* 

```json
{
  "drawingId": "string (uuid)",
  "previewAvailability": "string ('available' | 'unavailable' | 'generating')",
  "previewType": "string ('image' | 'pdf' | null)",
  "previewUrl": "string (url absolute/relative) | null",
  "message": "string | null (e.g., fallback reason 'File type not supported for preview')"
}
```

**Error Envelope (standard repo fallback for all endpoints):**
All 400, 404, 500 errors will be wrapped in the standard API envelope:
```json
{
  "error": "string (error_code)",
  "message": "string (human readable)",
  "correlationId": "string",
  "errors": ["array of optional detail strings"]
}
```
