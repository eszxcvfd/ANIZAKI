import { forwardRef, type InputHTMLAttributes } from 'react'
import type { SubmissionState } from '../types'

export { IDLE_STATE } from '../types'
export type { SubmissionState }

export const AuthInput = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement> & {
  error?: string
}>(({ className, error, ...props }, ref) => {
  return (
    <div className="flex flex-col gap-1.5 w-full">
      <input
        ref={ref}
        className={
          `w-full bg-white border rounded-xl px-4 py-3 text-slate-900 shadow-sm transition-all focus:outline-none focus:ring-2 disabled:opacity-50 ` +
          (error 
            ? 'border-red-400 focus:ring-red-500/10' 
            : 'border-slate-200 focus:border-indigo-500 focus:ring-indigo-500/10') +
          ` ${className || ''}`
        }
        aria-invalid={Boolean(error)}
        {...props}
      />
      {error && (
        <p role="alert" className="text-xs text-red-500 font-medium pl-1 animate-in fade-in slide-in-from-top-1">
          {error}
        </p>
      )}
    </div>
  )
})

AuthInput.displayName = 'AuthInput'

export function AuthButton({ 
  children, 
  isLoading, 
  ...props 
}: React.ButtonHTMLAttributes<HTMLButtonElement> & { isLoading?: boolean }) {
  return (
    <button
      {...props}
      disabled={isLoading || props.disabled}
      className={`w-full relative overflow-hidden btn-primary py-3.5 font-bold tracking-wide uppercase text-xs shadow-lg shadow-indigo-500/20 active:scale-[0.98] transition-all ${props.className || ''}`}
    >
      <span className={`transition-opacity ${isLoading ? 'opacity-0' : 'opacity-100'}`}>
        {children}
      </span>
      {isLoading && (
        <span className="absolute inset-0 flex items-center justify-center">
          <svg className="animate-spin h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
        </span>
      )}
    </button>
  )
}

export function AuthStatusMessage({ state }: { state: SubmissionState }) {
  if (!state.message) return null

  if (state.status === 'error') {
    return (
      <div role="alert" className="p-4 bg-red-50 text-red-600 text-sm font-semibold rounded-xl border border-red-100 animate-in fade-in slide-in-from-top-2">
        {state.message}
      </div>
    )
  }

  if (state.status === 'success') {
    return (
      <div role="status" className="p-4 bg-emerald-50 text-emerald-600 text-sm font-semibold rounded-xl border border-emerald-100 animate-in fade-in slide-in-from-top-2">
        {state.message}
      </div>
    )
  }

  return null
}
