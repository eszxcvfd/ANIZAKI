import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, fireEvent, cleanup } from '@testing-library/react'
import { LibraryListPage } from './LibraryListPage'
import { getDrawingList, getCategories } from '../../features/library/api/libraryApi'

vi.mock('../../features/library/api/libraryApi', () => ({
  getDrawingList: vi.fn(),
  getCategories: vi.fn(),
}))

describe('LibraryListPage', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    window.history.pushState({}, '', '/library')
  })

  afterEach(() => {
    cleanup()
  })

  it('renders loading state initially', () => {
    vi.mocked(getCategories).mockImplementation(() => new Promise(() => {}))
    vi.mocked(getDrawingList).mockImplementation(() => new Promise(() => {}))

    render(<LibraryListPage />)
    expect(screen.getByText('Đang tải bản vẽ...')).toBeDefined()
  })

  it('renders categories and drawings on success', async () => {
    vi.mocked(getCategories).mockResolvedValue([
      { id: '1', name: 'Cat 1', slug: 'cat-1', order: 1, drawingCount: 2 }
    ])
    vi.mocked(getDrawingList).mockResolvedValue({
      items: [
        { id: 'd1', title: 'Drawing 1', code: 'D-01', categorySlug: 'cat-1', categoryName: 'Cat 1', status: 'published', createdAtUtc: '', previewUrl: null }
      ],
      pagination: { page: 1, pageSize: 20, totalItems: 1, totalPages: 1 }
    })

    render(<LibraryListPage />)

    await waitFor(() => {
      expect(screen.getAllByText('Cat 1').length).toBeGreaterThan(0)
      expect(screen.getByText('Drawing 1')).toBeDefined()
      expect(screen.queryByText('Đang tải bản vẽ...')).toBeNull()
    })
  })

  it('renders empty state when no drawings found', async () => {
    vi.mocked(getCategories).mockResolvedValue([])
    vi.mocked(getDrawingList).mockResolvedValue({
      items: [],
      pagination: { page: 1, pageSize: 20, totalItems: 0, totalPages: 0 }
    })

    render(<LibraryListPage />)

    await waitFor(() => {
      expect(screen.getByText('Không tìm thấy bản vẽ phù hợp.')).toBeDefined()
    })
  })

  it('navigates with search parameter on form submit', async () => {
    vi.mocked(getCategories).mockResolvedValue([])
    vi.mocked(getDrawingList).mockResolvedValue({
      items: [],
      pagination: { page: 1, pageSize: 20, totalItems: 0, totalPages: 0 }
    })

    render(<LibraryListPage />)

    // Wait for the form to render
    const input = screen.getByPlaceholderText('Tìm theo tên bản vẽ hoặc mã...')
    const btn = screen.getByText('Tìm')

    fireEvent.change(input, { target: { value: 'demo' } })
    fireEvent.click(btn)

    await waitFor(() => {
      expect(window.location.search).toContain('search=demo')
    })
  })

  it('updates URL with sortBy when sort-by select changes', async () => {
    vi.mocked(getCategories).mockResolvedValue([])
    vi.mocked(getDrawingList).mockResolvedValue({
      items: [],
      pagination: { page: 1, pageSize: 20, totalItems: 0, totalPages: 0 }
    })

    render(<LibraryListPage />)

    await waitFor(() => {
      expect(screen.getByLabelText('Sắp xếp theo')).toBeDefined()
    })

    const sortBySelect = screen.getByLabelText('Sắp xếp theo')
    fireEvent.change(sortBySelect, { target: { value: 'title' } })

    await waitFor(() => {
      expect(window.location.search).toContain('sortBy=title')
    })
  })

  it('updates URL with sortDir when sort-dir select changes', async () => {
    vi.mocked(getCategories).mockResolvedValue([])
    vi.mocked(getDrawingList).mockResolvedValue({
      items: [],
      pagination: { page: 1, pageSize: 20, totalItems: 0, totalPages: 0 }
    })

    render(<LibraryListPage />)

    await waitFor(() => {
      expect(screen.getByLabelText('Chiều sắp xếp')).toBeDefined()
    })

    const sortDirSelect = screen.getByLabelText('Chiều sắp xếp')
    fireEvent.change(sortDirSelect, { target: { value: 'desc' } })

    await waitFor(() => {
      expect(window.location.search).toContain('sortDir=desc')
    })
  })
})
