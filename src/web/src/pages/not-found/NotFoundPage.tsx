import { PageContainer } from '../../shared/ui/PageContainer'

export function NotFoundPage() {
  return (
    <PageContainer title="Page not found">
      <p className="text-slate-600 font-medium">
        The requested route does not exist. Go back to <a href="/" className="text-indigo-600 hover:text-indigo-500 font-bold transition-colors">home</a>.
      </p>
    </PageContainer>
  )
}

