import { Card } from '../../shared/ui/Card'

export function ProjectSummary() {
  return (
    <Card title="Architecture Blueprint">
      <div className="relative overflow-hidden">
        <div className="absolute top-0 right-0 -mr-4 -mt-4 opacity-[0.05]">
          <svg width="120" height="120" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1" strokeLinecap="round" strokeLinejoin="round" className="text-slate-900"><rect width="18" height="18" x="3" y="3" rx="2" ry="2"/><line x1="3" y1="9" x2="21" y2="9"/><line x1="9" y1="21" x2="9" y2="9"/></svg>
        </div>
        <p className="text-slate-600 leading-relaxed max-w-lg font-medium">
          This UI baseline mirrors the planned monorepo architecture for a <span className="text-slate-900 font-bold">.NET 8 API</span> and <span className="text-slate-900 font-bold">React web application</span>. 
          Engineered for scalability and maintainability using Feature-Sliced Design.
        </p>
      </div>
    </Card>
  )
}
