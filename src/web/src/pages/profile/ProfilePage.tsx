import { ProfilePanel } from '../../features/profile/ProfilePanel'
import { PageContainer } from '../../shared/ui/PageContainer'

export function ProfilePage() {
  return (
    <PageContainer
      title="My profile"
      subtitle="Load and update your authenticated profile using backend auth contracts."
    >
      <ProfilePanel />
    </PageContainer>
  )
}

