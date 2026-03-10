import { AuthFlowPanel } from '../../features/auth/AuthFlowPanel'
import { PageContainer } from '../../shared/ui/PageContainer'

export function LoginRequiredPage() {
  return (
    <PageContainer
      title="Authentication"
      subtitle="Use auth flows below to create or refresh session context for guarded routes."
    >
      <AuthFlowPanel />
    </PageContainer>
  )
}
