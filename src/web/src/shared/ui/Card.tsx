import type { ReactNode } from 'react'
import { cn } from '../lib/cn'

type CardProps = {
  title?: string
  children: ReactNode
  className?: string
}

export function Card({ title, children, className }: CardProps) {
  return (
    <section className={cn('card', className)}>
      {title ? <h2>{title}</h2> : null}
      {children}
    </section>
  )
}

