import { Card } from '../../shared/ui/Card'
import { PageContainer } from '../../shared/ui/PageContainer'

export function AdminConsolePage() {
  return (
    <PageContainer
      title="Admin console"
      subtitle="This route is visible only to admin-authenticated sessions."
    >
      <Card title="Guarded route baseline">
        <p>Role-based routing is active. Future admin-only workflows can be added here.</p>
      </Card>
    </PageContainer>
  )
}

