import type { SortBy, SortDir } from '../model/libraryQueryUtils'
import { SORT_BY_OPTIONS, SORT_DIR_OPTIONS } from '../model/libraryQueryUtils'

interface SortControlProps {
  sortBy: SortBy
  sortDir: SortDir
  onSortByChange: (value: SortBy) => void
  onSortDirChange: (value: SortDir) => void
}

const SORT_BY_LABELS: Record<SortBy, string> = {
  createdAt: 'Ngày tạo',
  title: 'Tên bản vẽ',
  code: 'Mã bản vẽ',
}

const SORT_DIR_LABELS: Record<SortDir, string> = {
  asc: 'Tăng dần ↑',
  desc: 'Giảm dần ↓',
}

/**
 * SortControl renders two accessible <select> elements allowing the user
 * to choose the sort field and direction for the drawing list.
 */
export function SortControl({ sortBy, sortDir, onSortByChange, onSortDirChange }: SortControlProps) {
  return (
    <div className="flex items-center gap-2" role="group" aria-label="Sắp xếp danh sách">
      <span className="text-sm font-medium text-slate-500 shrink-0">Sắp xếp:</span>

      <select
        id="sort-by-select"
        aria-label="Sắp xếp theo"
        value={sortBy}
        onChange={e => onSortByChange(e.target.value as SortBy)}
        className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 shadow-sm hover:border-indigo-300 focus:border-indigo-400 focus:outline-none focus:ring-2 focus:ring-indigo-200 transition-colors"
      >
        {SORT_BY_OPTIONS.map(opt => (
          <option key={opt} value={opt}>
            {SORT_BY_LABELS[opt]}
          </option>
        ))}
      </select>

      <select
        id="sort-dir-select"
        aria-label="Chiều sắp xếp"
        value={sortDir}
        onChange={e => onSortDirChange(e.target.value as SortDir)}
        className="rounded-lg border border-slate-200 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 shadow-sm hover:border-indigo-300 focus:border-indigo-400 focus:outline-none focus:ring-2 focus:ring-indigo-200 transition-colors"
      >
        {SORT_DIR_OPTIONS.map(opt => (
          <option key={opt} value={opt}>
            {SORT_DIR_LABELS[opt]}
          </option>
        ))}
      </select>
    </div>
  )
}
