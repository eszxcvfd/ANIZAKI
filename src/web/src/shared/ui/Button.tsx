import type { ButtonHTMLAttributes } from 'react'
import { cn } from '../lib/cn'

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: 'primary' | 'outline' | 'ghost'
}

export function Button({ className, variant = 'primary', ...props }: ButtonProps) {
  const variants = {
    primary: 'btn-primary',
    outline: 'btn-outline',
    ghost: 'hover:bg-slate-100 text-slate-500 hover:text-slate-900 px-4 py-2 rounded-xl transition-all font-medium',
  }

  return (
    <button
      className={cn(variants[variant], className)}
      {...props}
    />
  )
}
