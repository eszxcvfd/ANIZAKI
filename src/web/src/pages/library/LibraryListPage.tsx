import { useState, useEffect } from 'react'
import { getDrawingList, getCategories } from '../../features/library/api/libraryApi'
import type { DrawingItemDto, CategoryItemDto } from '../../features/library/api/libraryApi'
import type { PaginatedResult } from '../../shared/api/httpClient'
import { ApiClientError } from '../../shared/api/httpClient'
import { parseLibraryListQuery, toApiParams, toQueryString, defaultQueryState } from '../../features/library/model/libraryQueryUtils'
import type { LibraryListQueryState, SortBy, SortDir } from '../../features/library/model/libraryQueryUtils'
import { SortControl } from '../../features/library/components/SortControl'

type LoadState = 'idle' | 'loading' | 'success' | 'error'

interface LibraryListState {
  loadState: LoadState
  result: PaginatedResult<DrawingItemDto> | null
  categories: CategoryItemDto[]
  error: string | null
}

export function LibraryListPage() {
  const [query, setQuery] = useState<LibraryListQueryState>(() =>
    parseLibraryListQuery(new URLSearchParams(window.location.search))
  )
  const [searchInput, setSearchInput] = useState(query.search ?? '')
  const [state, setState] = useState<LibraryListState>({
    loadState: 'idle',
    result: null,
    categories: [],
    error: null,
  })

  useEffect(() => {
    const controller = new AbortController()
    setState(s => ({ ...s, loadState: 'loading', error: null }))

    Promise.all([
      getDrawingList(toApiParams(query)),
      state.categories.length === 0 ? getCategories() : Promise.resolve(state.categories),
    ])
      .then(([result, categories]) => {
        setState({ loadState: 'success', result, categories, error: null })
      })
      .catch(err => {
        console.error('Test ERROR:', err)
        if (!controller.signal.aborted) {
          const msg = err instanceof ApiClientError ? err.message : 'Không thể tải danh sách bản vẽ.'
          setState(s => ({ ...s, loadState: 'error', error: msg }))
        }
      })

    return () => controller.abort()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query])

  function navigate(newQuery: LibraryListQueryState) {
    const qs = toQueryString(newQuery)
    window.history.pushState(null, '', `/library${qs}`)
    setQuery(newQuery)
  }

  function handleCategoryClick(slug: string | undefined) {
    navigate({ ...defaultQueryState(), category: slug })
    setSearchInput('')
  }

  function handleSearchSubmit(e: React.FormEvent) {
    e.preventDefault()
    navigate({ ...query, page: 1, search: searchInput.trim() || undefined })
  }

  function handlePageChange(page: number) {
    navigate({ ...query, page })
  }

  function handleSortByChange(sortBy: SortBy) {
    navigate({ ...query, page: 1, sortBy })
  }

  function handleSortDirChange(sortDir: SortDir) {
    navigate({ ...query, page: 1, sortDir })
  }

  const { loadState, result, categories, error } = state

  return (
    <div className="flex flex-col gap-8 py-8">
      {/* Header */}
      <div className="flex flex-col gap-2 animate-in fade-in slide-in-from-top-4 duration-500">
        <h1 className="text-4xl font-black text-slate-900 tracking-tight">
          Thư Viện Bản Vẽ
        </h1>
        <p className="text-slate-500 font-medium">
          Tra cứu và xem bản vẽ kỹ thuật theo danh mục và từ khóa.
        </p>
      </div>

      {/* Search + Filters */}
      <div className="flex flex-col sm:flex-row gap-4 animate-in fade-in slide-in-from-top-4 duration-600 delay-100">
        <form onSubmit={handleSearchSubmit} className="flex flex-1 gap-2">
          <input
            id="library-search-input"
            type="search"
            value={searchInput}
            onChange={e => setSearchInput(e.target.value)}
            placeholder="Tìm theo tên bản vẽ hoặc mã..."
            className="input-field flex-1"
          />
          <button type="submit" className="btn-primary px-6">Tìm</button>
        </form>
        <SortControl
          sortBy={query.sortBy}
          sortDir={query.sortDir}
          onSortByChange={handleSortByChange}
          onSortDirChange={handleSortDirChange}
        />
      </div>

      {/* Categories */}
      {categories.length > 0 && (
        <div className="flex flex-wrap gap-2 animate-in fade-in duration-700 delay-200">
          <button
            id="category-all"
            onClick={() => handleCategoryClick(undefined)}
            className={`px-4 py-1.5 rounded-full text-sm font-semibold transition-all ${
              !query.category
                ? 'bg-indigo-600 text-white shadow'
                : 'bg-slate-100 text-slate-600 hover:bg-indigo-50 hover:text-indigo-600'
            }`}
          >
            Tất cả
          </button>
          {categories.map(cat => (
            <button
              key={cat.slug}
              id={`category-${cat.slug}`}
              onClick={() => handleCategoryClick(cat.slug)}
              className={`px-4 py-1.5 rounded-full text-sm font-semibold transition-all ${
                query.category === cat.slug
                  ? 'bg-indigo-600 text-white shadow'
                  : 'bg-slate-100 text-slate-600 hover:bg-indigo-50 hover:text-indigo-600'
              }`}
            >
              {cat.name}
              <span className="ml-1 text-xs opacity-60">({cat.drawingCount})</span>
            </button>
          ))}
        </div>
      )}

      {/* Loading */}
      {loadState === 'loading' && (
        <div className="flex items-center justify-center py-24">
          <div className="text-slate-400 animate-pulse font-medium">Đang tải bản vẽ...</div>
        </div>
      )}

      {/* Error */}
      {loadState === 'error' && (
        <div className="rounded-xl bg-red-50 border border-red-100 text-red-700 px-6 py-4 font-medium">
          {error}
        </div>
      )}

      {/* Results Grid */}
      {loadState === 'success' && result && (
        <>
          {result.items.length === 0 ? (
            <div className="flex flex-col items-center gap-4 py-20 text-slate-400">
              <div className="text-5xl">📂</div>
              <p className="font-medium">Không tìm thấy bản vẽ phù hợp.</p>
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 animate-in fade-in duration-500">
              {result.items.map(drawing => (
                <a
                  key={drawing.id}
                  id={`drawing-card-${drawing.id}`}
                  href={`/library/drawings/${drawing.id}`}
                  className="card group block hover:border-indigo-200 hover:shadow-md transition-all"
                >
                  <div className="flex flex-col gap-3">
                    {drawing.previewUrl && (
                      <div className="w-full h-36 rounded-lg overflow-hidden bg-slate-100">
                        <img
                          src={drawing.previewUrl}
                          alt={drawing.title}
                          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                        />
                      </div>
                    )}
                    {!drawing.previewUrl && (
                      <div className="w-full h-36 rounded-lg bg-gradient-to-br from-indigo-50 to-slate-100 flex items-center justify-center">
                        <span className="text-4xl opacity-30">📄</span>
                      </div>
                    )}
                    <div>
                      <span className="inline-block text-xs font-bold uppercase tracking-wider px-2 py-0.5 rounded-full bg-indigo-50 text-indigo-600 mb-1">
                        {drawing.categoryName}
                      </span>
                      <h3 className="font-bold text-slate-900 group-hover:text-indigo-600 transition-colors leading-snug">
                        {drawing.title}
                      </h3>
                      <p className="text-xs text-slate-400 mt-1 font-mono">{drawing.code}</p>
                    </div>
                  </div>
                </a>
              ))}
            </div>
          )}

          {/* Pagination */}
          {result.pagination.totalPages && result.pagination.totalPages > 1 && (
            <div className="flex items-center justify-center gap-2 mt-4">
              <button
                id="pagination-prev"
                onClick={() => handlePageChange(query.page - 1)}
                disabled={query.page <= 1}
                className="btn-outline py-1.5 px-4 text-sm disabled:opacity-40"
              >
                ← Trước
              </button>
              <span className="text-sm text-slate-500 font-medium">
                Trang {result.pagination.page} / {result.pagination.totalPages}
              </span>
              <button
                id="pagination-next"
                onClick={() => handlePageChange(query.page + 1)}
                disabled={query.page >= (result.pagination.totalPages ?? 1)}
                className="btn-outline py-1.5 px-4 text-sm disabled:opacity-40"
              >
                Tiếp →
              </button>
            </div>
          )}
        </>
      )}
    </div>
  )
}
