import type { ReactNode } from 'react'
import { Stack } from './Stack'

type PageContainerProps = {
  title: string
  subtitle?: string
  children: ReactNode
}

export function PageContainer({ title, subtitle, children }: PageContainerProps) {
  return (
    <div className="page-container flex flex-col gap-10">
      <header className="flex flex-col gap-2">
        <h1 className="text-4xl font-black tracking-tight text-slate-900 md:text-5xl">
          {title}
        </h1>
        {subtitle ? (
          <p className="text-lg text-slate-600 max-w-2xl font-medium">
            {subtitle}
          </p>
        ) : null}
      </header>
      <Stack className="gap-8">{children}</Stack>
    </div>
  )
}
