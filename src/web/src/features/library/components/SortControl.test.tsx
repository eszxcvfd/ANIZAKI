import { describe, it, expect, vi, afterEach } from 'vitest'
import { render, screen, fireEvent, cleanup } from '@testing-library/react'
import { SortControl } from './SortControl'

afterEach(() => {
  cleanup()
})

describe('SortControl', () => {
  it('renders sort-by and sort-dir selects', () => {
    render(
      <SortControl
        sortBy="createdAt"
        sortDir="asc"
        onSortByChange={vi.fn()}
        onSortDirChange={vi.fn()}
      />
    )
    expect(screen.getByLabelText('Sắp xếp theo')).toBeDefined()
    expect(screen.getByLabelText('Chiều sắp xếp')).toBeDefined()
  })

  it('displays the current sortBy value', () => {
    render(
      <SortControl
        sortBy="title"
        sortDir="asc"
        onSortByChange={vi.fn()}
        onSortDirChange={vi.fn()}
      />
    )
    const sortBySelect = screen.getByLabelText('Sắp xếp theo') as HTMLSelectElement
    expect(sortBySelect.value).toBe('title')
  })

  it('displays the current sortDir value', () => {
    render(
      <SortControl
        sortBy="createdAt"
        sortDir="desc"
        onSortByChange={vi.fn()}
        onSortDirChange={vi.fn()}
      />
    )
    const sortDirSelect = screen.getByLabelText('Chiều sắp xếp') as HTMLSelectElement
    expect(sortDirSelect.value).toBe('desc')
  })

  it('calls onSortByChange when sort-by select changes', () => {
    const onSortByChange = vi.fn()
    render(
      <SortControl
        sortBy="createdAt"
        sortDir="asc"
        onSortByChange={onSortByChange}
        onSortDirChange={vi.fn()}
      />
    )
    const sortBySelect = screen.getByLabelText('Sắp xếp theo')
    fireEvent.change(sortBySelect, { target: { value: 'code' } })
    expect(onSortByChange).toHaveBeenCalledWith('code')
  })

  it('calls onSortDirChange when sort-dir select changes', () => {
    const onSortDirChange = vi.fn()
    render(
      <SortControl
        sortBy="createdAt"
        sortDir="asc"
        onSortByChange={vi.fn()}
        onSortDirChange={onSortDirChange}
      />
    )
    const sortDirSelect = screen.getByLabelText('Chiều sắp xếp')
    fireEvent.change(sortDirSelect, { target: { value: 'desc' } })
    expect(onSortDirChange).toHaveBeenCalledWith('desc')
  })

  it('renders all sortBy options', () => {
    render(
      <SortControl
        sortBy="createdAt"
        sortDir="asc"
        onSortByChange={vi.fn()}
        onSortDirChange={vi.fn()}
      />
    )
    expect(screen.getByText('Ngày tạo')).toBeDefined()
    expect(screen.getByText('Tên bản vẽ')).toBeDefined()
    expect(screen.getByText('Mã bản vẽ')).toBeDefined()
  })

  it('renders all sortDir options', () => {
    render(
      <SortControl
        sortBy="createdAt"
        sortDir="asc"
        onSortByChange={vi.fn()}
        onSortDirChange={vi.fn()}
      />
    )
    expect(screen.getByText('Tăng dần ↑')).toBeDefined()
    expect(screen.getByText('Giảm dần ↓')).toBeDefined()
  })
})
