import { describe, it, expect, vi, beforeEach } from 'vitest'
import { getCategories, getDrawingList, getDrawingDetail, getDrawingPreview } from './libraryApi'
import { apiClient } from '../../../shared/api/httpClient'
import type { PaginationMeta } from '../../../shared/api/httpClient'

// Mock the API client
vi.mock('../../../shared/api/httpClient', () => {
  return {
    apiClient: {
      request: vi.fn(),
      getPage: vi.fn(),
    },
  }
})

describe('libraryApi', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('getCategories fetches correctly', async () => {
    vi.mocked(apiClient.request).mockResolvedValueOnce({ items: [{ id: 'cat1' }] })
    
    const result = await getCategories()
    expect(result).toEqual([{ id: 'cat1' }])
    expect(apiClient.request).toHaveBeenCalledWith('/api/v1/library/categories')
  })

  it('getDrawingList fetches correctly with no params', async () => {
    vi.mocked(apiClient.getPage).mockResolvedValueOnce({ items: [], pagination: {} as PaginationMeta })
    
    await getDrawingList()
    expect(apiClient.getPage).toHaveBeenCalledWith('/api/v1/library/drawings', {
      query: {
        page: undefined,
        pageSize: undefined,
        category: undefined,
        search: undefined,
        sortBy: undefined,
        sortDir: undefined,
      },
    })
  })

  it('getDrawingList maps query params', async () => {
    vi.mocked(apiClient.getPage).mockResolvedValueOnce({ items: [], pagination: {} as PaginationMeta })
    
    await getDrawingList({ page: 2, pageSize: 10, category: 'kien-truc', search: 'Mat Bang' })
    expect(apiClient.getPage).toHaveBeenCalledWith('/api/v1/library/drawings', {
      query: {
        page: 2,
        pageSize: 10,
        category: 'kien-truc',
        search: 'Mat Bang',
        sortBy: undefined,
        sortDir: undefined,
      },
    })
  })

  it('getDrawingList passes sortBy and sortDir params', async () => {
    vi.mocked(apiClient.getPage).mockResolvedValueOnce({ items: [], pagination: {} as PaginationMeta })

    await getDrawingList({ sortBy: 'title', sortDir: 'desc' })
    expect(apiClient.getPage).toHaveBeenCalledWith('/api/v1/library/drawings', {
      query: {
        page: undefined,
        pageSize: undefined,
        category: undefined,
        search: undefined,
        sortBy: 'title',
        sortDir: 'desc',
      },
    })
  })

  it('getDrawingDetail fetches the correct ID', async () => {
    vi.mocked(apiClient.request).mockResolvedValueOnce({ id: 'doc-123' })
    
    const result = await getDrawingDetail('doc-123')
    expect(result).toEqual({ id: 'doc-123' })
    expect(apiClient.request).toHaveBeenCalledWith('/api/v1/library/drawings/doc-123')
  })

  it('getDrawingPreview fetches for the correct ID', async () => {
    vi.mocked(apiClient.request).mockResolvedValueOnce({ previewUrl: 'foo.png' })
    
    const result = await getDrawingPreview('doc-456')
    expect(result).toEqual({ previewUrl: 'foo.png' })
    expect(apiClient.request).toHaveBeenCalledWith('/api/v1/library/drawings/doc-456/preview')
  })
})
