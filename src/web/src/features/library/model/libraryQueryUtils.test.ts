import { describe, it, expect } from 'vitest'
import {
  normalizePage,
  normalizePageSize,
  normalizeCategorySlug,
  normalizeSearch,
  normalizeSortBy,
  normalizeSortDir,
  parseLibraryListQuery,
  toApiParams,
  toQueryString,
  defaultQueryState,
  DEFAULT_PAGE_SIZE,
  MIN_PAGE,
  DEFAULT_SORT_BY,
  DEFAULT_SORT_DIR,
} from './libraryQueryUtils'

describe('normalizePage', () => {
  it('returns 1 for null', () => expect(normalizePage(null)).toBe(1))
  it('returns 1 for undefined', () => expect(normalizePage(undefined)).toBe(1))
  it('returns 1 for empty string', () => expect(normalizePage('')).toBe(1))
  it('returns 1 for "0"', () => expect(normalizePage('0')).toBe(1))
  it('returns 1 for "-1"', () => expect(normalizePage('-1')).toBe(1))
  it('returns 3 for "3"', () => expect(normalizePage('3')).toBe(3))
  it('returns 3 for 3 (number)', () => expect(normalizePage(3)).toBe(3))
  it('returns 1 for "abc"', () => expect(normalizePage('abc')).toBe(1))
})

describe('normalizePageSize', () => {
  it('returns DEFAULT_PAGE_SIZE for null', () => expect(normalizePageSize(null)).toBe(DEFAULT_PAGE_SIZE))
  it('returns DEFAULT_PAGE_SIZE for undefined', () => expect(normalizePageSize(undefined)).toBe(DEFAULT_PAGE_SIZE))
  it('returns DEFAULT_PAGE_SIZE for invalid', () => expect(normalizePageSize('foo')).toBe(DEFAULT_PAGE_SIZE))
  it('returns 10 for "10"', () => expect(normalizePageSize('10')).toBe(10))
  it('clamps to 100 for oversized input', () => expect(normalizePageSize('500')).toBe(100))
  it('returns DEFAULT_PAGE_SIZE for 0', () => expect(normalizePageSize('0')).toBe(DEFAULT_PAGE_SIZE))
})

describe('normalizeCategorySlug', () => {
  it('returns undefined for null', () => expect(normalizeCategorySlug(null)).toBeUndefined())
  it('returns undefined for empty string', () => expect(normalizeCategorySlug('')).toBeUndefined())
  it('returns undefined for whitespace-only', () => expect(normalizeCategorySlug('  ')).toBeUndefined())
  it('trims and lowercases', () => expect(normalizeCategorySlug('  Kien-Truc  ')).toBe('kien-truc'))
})

describe('normalizeSearch', () => {
  it('returns undefined for null', () => expect(normalizeSearch(null)).toBeUndefined())
  it('returns undefined for empty string', () => expect(normalizeSearch('')).toBeUndefined())
  it('returns undefined for whitespace only', () => expect(normalizeSearch('   ')).toBeUndefined())
  it('trims the string', () => expect(normalizeSearch('  Mặt Bằng  ')).toBe('Mặt Bằng'))
})

describe('normalizeSortBy', () => {
  it('returns DEFAULT_SORT_BY for null', () => expect(normalizeSortBy(null)).toBe(DEFAULT_SORT_BY))
  it('returns DEFAULT_SORT_BY for undefined', () => expect(normalizeSortBy(undefined)).toBe(DEFAULT_SORT_BY))
  it('returns DEFAULT_SORT_BY for empty string', () => expect(normalizeSortBy('')).toBe(DEFAULT_SORT_BY))
  it('returns DEFAULT_SORT_BY for unknown value', () => expect(normalizeSortBy('unknown')).toBe(DEFAULT_SORT_BY))
  it('accepts "createdAt"', () => expect(normalizeSortBy('createdAt')).toBe('createdAt'))
  it('accepts "title"', () => expect(normalizeSortBy('title')).toBe('title'))
  it('accepts "code"', () => expect(normalizeSortBy('code')).toBe('code'))
  it('lowercases input', () => expect(normalizeSortBy('TITLE')).toBe('title'))
  it('trims whitespace', () => expect(normalizeSortBy('  code  ')).toBe('code'))
})

describe('normalizeSortDir', () => {
  it('returns DEFAULT_SORT_DIR for null', () => expect(normalizeSortDir(null)).toBe(DEFAULT_SORT_DIR))
  it('returns DEFAULT_SORT_DIR for undefined', () => expect(normalizeSortDir(undefined)).toBe(DEFAULT_SORT_DIR))
  it('returns DEFAULT_SORT_DIR for empty string', () => expect(normalizeSortDir('')).toBe(DEFAULT_SORT_DIR))
  it('returns DEFAULT_SORT_DIR for unknown value', () => expect(normalizeSortDir('sideways')).toBe(DEFAULT_SORT_DIR))
  it('accepts "asc"', () => expect(normalizeSortDir('asc')).toBe('asc'))
  it('accepts "desc"', () => expect(normalizeSortDir('desc')).toBe('desc'))
  it('lowercases input', () => expect(normalizeSortDir('DESC')).toBe('desc'))
  it('trims whitespace', () => expect(normalizeSortDir('  asc  ')).toBe('asc'))
})

describe('parseLibraryListQuery', () => {
  it('parses all provided params', () => {
    const sp = new URLSearchParams('page=2&pageSize=10&category=kien-truc&search=mat')
    const result = parseLibraryListQuery(sp)
    expect(result.page).toBe(2)
    expect(result.pageSize).toBe(10)
    expect(result.category).toBe('kien-truc')
    expect(result.search).toBe('mat')
  })

  it('applies defaults when params are absent', () => {
    const sp = new URLSearchParams('')
    const result = parseLibraryListQuery(sp)
    expect(result.page).toBe(MIN_PAGE)
    expect(result.pageSize).toBe(DEFAULT_PAGE_SIZE)
    expect(result.category).toBeUndefined()
    expect(result.search).toBeUndefined()
    expect(result.sortBy).toBe(DEFAULT_SORT_BY)
    expect(result.sortDir).toBe(DEFAULT_SORT_DIR)
  })

  it('parses sortBy and sortDir', () => {
    const sp = new URLSearchParams('sortBy=title&sortDir=desc')
    const result = parseLibraryListQuery(sp)
    expect(result.sortBy).toBe('title')
    expect(result.sortDir).toBe('desc')
  })
})

describe('toQueryString', () => {
  it('omits defaults from query string', () => {
    const state = defaultQueryState()
    expect(toQueryString(state)).toBe('')
  })

  it('includes only non-default values', () => {
    const state = defaultQueryState()
    state.page = 2
    state.search = 'abc'
    const qs = toQueryString(state)
    expect(qs).toContain('page=2')
    expect(qs).toContain('search=abc')
    expect(qs).not.toContain('pageSize')
  })

  it('includes sortBy when non-default', () => {
    const state = defaultQueryState()
    state.sortBy = 'title'
    const qs = toQueryString(state)
    expect(qs).toContain('sortBy=title')
  })

  it('includes sortDir when non-default', () => {
    const state = defaultQueryState()
    state.sortDir = 'desc'
    const qs = toQueryString(state)
    expect(qs).toContain('sortDir=desc')
  })

  it('omits sortBy and sortDir when default', () => {
    const state = defaultQueryState()
    const qs = toQueryString(state)
    expect(qs).not.toContain('sortBy')
    expect(qs).not.toContain('sortDir')
  })
})

describe('toApiParams', () => {
  it('maps query state directly to API params', () => {
    const state = { page: 3, pageSize: 10, category: 'ket-cau', search: 'bridge', sortBy: 'title' as const, sortDir: 'desc' as const }
    const params = toApiParams(state)
    expect(params.page).toBe(3)
    expect(params.pageSize).toBe(10)
    expect(params.category).toBe('ket-cau')
    expect(params.search).toBe('bridge')
    expect(params.sortBy).toBe('title')
    expect(params.sortDir).toBe('desc')
  })

  it('maps default sort values to API params', () => {
    const state = defaultQueryState()
    const params = toApiParams(state)
    expect(params.sortBy).toBe(DEFAULT_SORT_BY)
    expect(params.sortDir).toBe(DEFAULT_SORT_DIR)
  })
})
