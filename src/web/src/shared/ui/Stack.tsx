import type { ReactNode } from 'react'
import { cn } from '../lib/cn'

type StackProps = {
  children: ReactNode
  className?: string
}

export function Stack({ children, className }: StackProps) {
  return <div className={cn('stack', className)}>{children}</div>
}

