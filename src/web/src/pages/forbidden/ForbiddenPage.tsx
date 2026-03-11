import { Card } from '../../shared/ui/Card'
import { PageContainer } from '../../shared/ui/PageContainer'

export function ForbiddenPage() {
  return (
    <PageContainer
      title="Access denied"
      subtitle="Your current role does not have permission to view this route."
    >
      <Card title="Restricted Area">
        <div className="space-y-4">
          <p className="text-slate-600">You don't have the required permissions to access this page. Please ensure you are logged in with the correct account or role.</p>
          <div className="flex flex-wrap gap-4 pt-2">
            <a href="/" className="btn-outline px-6 py-2">Return Home</a>
            <a href="/dev/bootstrap" className="btn-primary-ghost px-6 py-2 text-indigo-600 font-bold hover:bg-slate-50 border border-indigo-200">
              Dev Bootstrap (Skip Auth)
            </a>
          </div>
        </div>
      </Card>
    </PageContainer>
  )
}

