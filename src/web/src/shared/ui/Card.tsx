import type { ReactNode } from 'react'
import { cn } from '../lib/cn'

type CardProps = {
  title?: string
  children: ReactNode
  className?: string
}

export function Card({ title, children, className }: CardProps) {
  return (
    <section className={cn('card group', className)}>
      {title ? (
        <h2 className="mb-4 text-lg font-bold tracking-tight text-slate-900 group-hover:text-indigo-600 transition-colors">
          {title}
        </h2>
      ) : null}
      <div className="text-slate-600 leading-relaxed font-medium">
        {children}
      </div>
    </section>
  )
}
