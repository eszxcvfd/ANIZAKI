import { Card } from '../../shared/ui/Card'
import { PageContainer } from '../../shared/ui/PageContainer'

export function ForbiddenPage() {
  return (
    <PageContainer
      title="Access denied"
      subtitle="Your current role does not have permission to view this route."
    >
      <Card title="Role mismatch">
        <p>Try signing in with a role that has access, or return to the home page.</p>
      </Card>
    </PageContainer>
  )
}

