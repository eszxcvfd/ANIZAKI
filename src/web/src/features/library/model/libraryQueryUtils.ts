import type { GetDrawingListParams } from '../api/libraryApi'

// ─── Constants ──────────────────────────────────────────────────────────────

export const DEFAULT_PAGE_SIZE = 20
export const MIN_PAGE = 1
export const MAX_PAGE_SIZE = 100

export const SORT_BY_OPTIONS = ['createdAt', 'title', 'code'] as const
export type SortBy = (typeof SORT_BY_OPTIONS)[number]

export const SORT_DIR_OPTIONS = ['asc', 'desc'] as const
export type SortDir = (typeof SORT_DIR_OPTIONS)[number]

export const DEFAULT_SORT_BY: SortBy = 'createdAt'
export const DEFAULT_SORT_DIR: SortDir = 'asc'

// ─── Normalization ────────────────────────────────────────────────────────────

/**
 * Normalizes a page number from a raw string or number.
 * Falls back to MIN_PAGE (1) if invalid.
 */
export function normalizePage(value: string | number | null | undefined): number {
  if (value === null || value === undefined || value === '') return MIN_PAGE
  const parsed = typeof value === 'number' ? value : parseInt(value, 10)
  if (isNaN(parsed) || parsed < MIN_PAGE) return MIN_PAGE
  return parsed
}

/**
 * Normalizes a pageSize value, clamping within [1, MAX_PAGE_SIZE].
 * Falls back to DEFAULT_PAGE_SIZE if invalid.
 */
export function normalizePageSize(value: string | number | null | undefined): number {
  if (value === null || value === undefined || value === '') return DEFAULT_PAGE_SIZE
  const parsed = typeof value === 'number' ? value : parseInt(value, 10)
  if (isNaN(parsed) || parsed < 1) return DEFAULT_PAGE_SIZE
  return Math.min(parsed, MAX_PAGE_SIZE)
}

/**
 * Normalizes a category slug: trims whitespace and lowercases.
 * Returns undefined if empty after trimming.
 */
export function normalizeCategorySlug(value: string | null | undefined): string | undefined {
  if (!value) return undefined
  const trimmed = value.trim().toLowerCase()
  return trimmed.length > 0 ? trimmed : undefined
}

/**
 * Normalizes a search term: trims whitespace.
 * Returns undefined if empty/whitespace-only.
 */
export function normalizeSearch(value: string | null | undefined): string | undefined {
  if (!value) return undefined
  const trimmed = value.trim()
  return trimmed.length > 0 ? trimmed : undefined
}

// ─── Normalization (sort) ─────────────────────────────────────────────────────

/**
 * Normalizes a sortBy value. Returns DEFAULT_SORT_BY if not a recognized value.
 */
export function normalizeSortBy(value: string | null | undefined): SortBy {
  if (!value) return DEFAULT_SORT_BY
  const trimmed = value.trim().toLowerCase() as SortBy
  return (SORT_BY_OPTIONS as readonly string[]).includes(trimmed) ? (trimmed as SortBy) : DEFAULT_SORT_BY
}

/**
 * Normalizes a sortDir value. Returns DEFAULT_SORT_DIR if not 'asc' or 'desc'.
 */
export function normalizeSortDir(value: string | null | undefined): SortDir {
  if (!value) return DEFAULT_SORT_DIR
  const trimmed = value.trim().toLowerCase() as SortDir
  return (SORT_DIR_OPTIONS as readonly string[]).includes(trimmed) ? (trimmed as SortDir) : DEFAULT_SORT_DIR
}

// ─── Query Params Model ───────────────────────────────────────────────────────

export interface LibraryListQueryState {
  page: number
  pageSize: number
  category: string | undefined
  search: string | undefined
  sortBy: SortBy
  sortDir: SortDir
}

/**
 * Build a normalized LibraryListQueryState from raw URL search params.
 */
export function parseLibraryListQuery(urlSearchParams: URLSearchParams): LibraryListQueryState {
  return {
    page: normalizePage(urlSearchParams.get('page')),
    pageSize: normalizePageSize(urlSearchParams.get('pageSize')),
    category: normalizeCategorySlug(urlSearchParams.get('category')),
    search: normalizeSearch(urlSearchParams.get('search')),
    sortBy: normalizeSortBy(urlSearchParams.get('sortBy')),
    sortDir: normalizeSortDir(urlSearchParams.get('sortDir')),
  }
}

/**
 * Convert a LibraryListQueryState to an API-ready GetDrawingListParams object.
 */
export function toApiParams(state: LibraryListQueryState): GetDrawingListParams {
  return {
    page: state.page,
    pageSize: state.pageSize,
    category: state.category,
    search: state.search,
    sortBy: state.sortBy,
    sortDir: state.sortDir,
  }
}

/**
 * Build a URL query string from query state (for pushing to history, sharing links).
 */
export function toQueryString(state: LibraryListQueryState): string {
  const params = new URLSearchParams()
  if (state.page !== MIN_PAGE) params.set('page', String(state.page))
  if (state.pageSize !== DEFAULT_PAGE_SIZE) params.set('pageSize', String(state.pageSize))
  if (state.category) params.set('category', state.category)
  if (state.search) params.set('search', state.search)
  if (state.sortBy !== DEFAULT_SORT_BY) params.set('sortBy', state.sortBy)
  if (state.sortDir !== DEFAULT_SORT_DIR) params.set('sortDir', state.sortDir)
  const qs = params.toString()
  return qs ? `?${qs}` : ''
}

/**
 * Default empty query state (no filters applied).
 */
export function defaultQueryState(): LibraryListQueryState {
  return {
    page: MIN_PAGE,
    pageSize: DEFAULT_PAGE_SIZE,
    category: undefined,
    search: undefined,
    sortBy: DEFAULT_SORT_BY,
    sortDir: DEFAULT_SORT_DIR,
  }
}
