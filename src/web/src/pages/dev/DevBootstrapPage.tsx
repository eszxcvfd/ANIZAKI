import { SessionBootstrapCard } from '../../features/auth/session/SessionBootstrapCard'
import { PageContainer } from '../../shared/ui/PageContainer'

export function DevBootstrapPage() {
  return (
    <PageContainer
      title="Development Bootstrap"
      subtitle="Bypass authentication and initialize a direct session context for development."
    >
      <div className="max-w-2xl mx-auto">
        <SessionBootstrapCard nextPath="/admin/console" />
      </div>
    </PageContainer>
  )
}
