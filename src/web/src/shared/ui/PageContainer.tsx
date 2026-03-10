import type { ReactNode } from 'react'
import { Stack } from './Stack'

type PageContainerProps = {
  title: string
  subtitle?: string
  children: ReactNode
}

export function PageContainer({ title, subtitle, children }: PageContainerProps) {
  return (
    <div className="page-container">
      <header>
        <h1>{title}</h1>
        {subtitle ? <p>{subtitle}</p> : null}
      </header>
      <Stack>{children}</Stack>
    </div>
  )
}
