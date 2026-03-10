import { PageContainer } from '../../shared/ui/PageContainer'

export function NotFoundPage() {
  return (
    <PageContainer title="Page not found">
      <p>
        The requested route does not exist. Go back to <a href="/">home</a>.
      </p>
    </PageContainer>
  )
}

