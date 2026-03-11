import type { ReactNode } from 'react'
export function AuthLayout({ children, title, subtitle }: { children: ReactNode, title: string, subtitle?: string }) {
  return (
    <div className="min-h-screen flex items-center justify-center p-4 sm:p-8 relative overflow-hidden bg-[#f8fafc]">
      {/* Premium Decorative Background Effects */}
      <div className="absolute top-[-10%] left-[-10%] w-[60%] h-[60%] bg-indigo-500 rounded-full mix-blend-multiply filter blur-[150px] opacity-[0.07] animate-pulse"></div>
      <div className="absolute bottom-[-10%] right-[-10%] w-[60%] h-[60%] bg-violet-500 rounded-full mix-blend-multiply filter blur-[150px] opacity-[0.07] animate-pulse animation-delay-3000"></div>
      
      {/* Content Container */}
      <div className="relative w-full max-w-md glass-panel rounded-3xl p-8 sm:p-10 z-10 animate-in fade-in zoom-in-95 duration-500 border border-white/40">
        <div className="mb-10 text-center">
          <div className="inline-flex h-12 w-12 items-center justify-center rounded-2xl bg-indigo-600 font-black text-2xl text-white mb-6 shadow-xl shadow-indigo-500/20">A</div>
          <h2 className="text-3xl font-bold text-slate-900 tracking-tight">{title}</h2>
          {subtitle && <p className="mt-3 text-slate-500 font-medium">{subtitle}</p>}
        </div>
        
        {children}
      </div>
    </div>
  )
}
