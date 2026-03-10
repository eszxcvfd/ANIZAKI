import { SystemStatusCard } from '../../features/system/SystemStatusCard'
import { ProjectSummary } from '../../entities/project/ProjectSummary'
import { PageContainer } from '../../shared/ui/PageContainer'
import { Card } from '../../shared/ui/Card'

export function HomePage() {
  return (
    <PageContainer
      title="Web Bootstrap Baseline"
      subtitle="App shell and feature boundaries are now in place."
    >
      <ProjectSummary />
      <Card title="Next flow">
        <p>
          Start with auth flows at <a href="/auth/login">/auth/login</a>, then continue to{' '}
          <a href="/profile">/profile</a>.
        </p>
      </Card>
      <SystemStatusCard />
    </PageContainer>
  )
}
