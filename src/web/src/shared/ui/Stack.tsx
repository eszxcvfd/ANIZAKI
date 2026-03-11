import type { ReactNode } from 'react'
import { cn } from '../lib/cn'

type StackProps = {
  children: ReactNode
  className?: string
}

export function Stack({ children, className }: StackProps) {
  return <div className={cn('flex flex-col gap-4', className)}>{children}</div>
}
