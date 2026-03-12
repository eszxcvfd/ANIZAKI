import { apiClient } from '../../../shared/api/httpClient'
import type { PaginatedResult } from '../../../shared/api/httpClient'

// ─── Response DTOs (mirrors backend contracts) ───────────────────────────────

export interface CategoryItemDto {
  id: string
  name: string
  slug: string
  order: number
  drawingCount: number
}

export interface DrawingItemDto {
  id: string
  title: string
  code: string
  categorySlug: string
  categoryName: string
  status: string
  createdAtUtc: string
  previewUrl: string | null
}

export interface FileInfoDetailDto {
  fileName: string
  mimeType: string
  sizeBytes: number
  checksum: string
  uploadedAtUtc: string
  previewAvailability: 'available' | 'unavailable' | 'generating'
}

export interface DrawingDetailDto {
  id: string
  title: string
  code: string
  description: string | null
  categorySlug: string
  categoryName: string
  status: string
  tags: string[]
  createdAtUtc: string
  updatedAtUtc: string | null
  fileInfo: FileInfoDetailDto
}

export interface DrawingPreviewDto {
  drawingId: string
  previewAvailability: 'available' | 'unavailable' | 'generating'
  previewType: 'image' | 'pdf' | null
  previewUrl: string | null
  message: string | null
}

// ─── Query Params ─────────────────────────────────────────────────────────────

export interface GetDrawingListParams {
  page?: number
  pageSize?: number
  category?: string
  search?: string
  sortBy?: string
  sortDir?: string
}

// ─── API Wrappers ─────────────────────────────────────────────────────────────

/**
 * Fetch all available drawing categories.
 */
export async function getCategories(): Promise<CategoryItemDto[]> {
  const response = await apiClient.request<{ items: CategoryItemDto[] }>('/api/v1/library/categories')
  return response.items
}

/**
 * Fetch a paginated list of drawings with optional filters.
 */
export async function getDrawingList(
  params: GetDrawingListParams = {},
): Promise<PaginatedResult<DrawingItemDto>> {
  return apiClient.getPage<DrawingItemDto>('/api/v1/library/drawings', {
    query: {
      page: params.page,
      pageSize: params.pageSize,
      category: params.category,
      search: params.search,
      sortBy: params.sortBy,
      sortDir: params.sortDir,
    },
  })
}

/**
 * Fetch a single drawing's full detail (including fileInfo).
 */
export async function getDrawingDetail(drawingId: string): Promise<DrawingDetailDto> {
  return apiClient.request<DrawingDetailDto>(`/api/v1/library/drawings/${drawingId}`)
}

/**
 * Fetch preview metadata for a specific drawing.
 */
export async function getDrawingPreview(drawingId: string): Promise<DrawingPreviewDto> {
  return apiClient.request<DrawingPreviewDto>(`/api/v1/library/drawings/${drawingId}/preview`)
}
