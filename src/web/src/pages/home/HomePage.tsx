import { SystemStatusCard } from '../../features/system/SystemStatusCard'
import { ProjectSummary } from '../../entities/project/ProjectSummary'
import { Card } from '../../shared/ui/Card'

export function HomePage() {
  return (
    <div className="flex flex-col gap-16 py-10">
      <section className="flex flex-col items-center text-center gap-6 py-12">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-indigo-50 border border-indigo-100 text-indigo-600 text-xs font-bold uppercase tracking-widest animate-in fade-in slide-in-from-top-4 duration-500">
          <span className="relative flex h-2 w-2">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-indigo-400 opacity-75"></span>
            <span className="relative inline-flex rounded-full h-2 w-2 bg-indigo-600"></span>
          </span>
          v1.0.0 Live
        </div>
        <h1 className="text-5xl md:text-7xl font-black text-slate-900 tracking-tight max-w-4xl leading-[1.1] animate-in fade-in slide-in-from-bottom-4 duration-700 delay-100">
          Crafting the Future of <span className="text-transparent bg-clip-text bg-gradient-to-r from-indigo-600 to-violet-600">Digital Architecture.</span>
        </h1>
        <p className="text-lg md:text-xl text-slate-600 max-w-2xl leading-relaxed animate-in fade-in slide-in-from-bottom-4 duration-700 delay-200 font-medium">
          A high-performance monorepo foundation built with .NET 8 and React. 
          Scalable, secure, and designed for the modern web.
        </p>
        <div className="flex items-center gap-4 mt-4 animate-in fade-in slide-in-from-bottom-4 duration-700 delay-300">
          <a href="/auth/register" className="btn-primary py-3 px-8 text-base">Get Started Free</a>
          <a href="/auth/login" className="btn-outline py-3 px-8 text-base">Sign In</a>
        </div>
      </section>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 animate-in fade-in slide-in-from-bottom-8 duration-1000 delay-500">
        <ProjectSummary />
        <SystemStatusCard />
      </div>

      <section className="animate-in fade-in slide-in-from-bottom-8 duration-1000 delay-700">
        <Card title="Quick Navigation" className="border-indigo-100 bg-indigo-50/30">
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-6">
            <div className="flex flex-col gap-2">
              <div className="text-slate-900 font-bold">Authentication</div>
              <a href="/auth/login" className="text-sm text-slate-600 hover:text-indigo-600 font-medium">Login Flow</a>
              <a href="/auth/register" className="text-sm text-slate-600 hover:text-indigo-600 font-medium">Registration</a>
            </div>
            <div className="flex flex-col gap-2">
              <div className="text-slate-900 font-bold">User Space</div>
              <a href="/profile" className="text-sm text-slate-600 hover:text-indigo-600 font-medium">Profile Settings</a>
              <a href="/forbidden" className="text-sm text-slate-600 hover:text-indigo-600 font-medium">Identity Guards</a>
            </div>
            <div className="flex flex-col gap-2">
              <div className="text-slate-900 font-bold">Administration</div>
              <a href="/admin/console" className="text-sm text-slate-600 hover:text-indigo-600 font-medium">System Dashboard</a>
              <a href="/health" className="text-sm text-slate-600 hover:text-indigo-600 font-medium">Health Probes</a>
            </div>
          </div>
        </Card>
      </section>
    </div>
  )
}
