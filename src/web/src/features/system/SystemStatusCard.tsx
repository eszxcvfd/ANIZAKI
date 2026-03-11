import { useEffect, useState } from 'react'
import { fetchHealthStatus } from './api/fetchHealthStatus'
import { Card } from '../../shared/ui/Card'
import { API_BASE_URL } from '../../shared/config/env'

type SmokeState =
  | { kind: 'loading' }
  | { kind: 'ready'; status: string; checkedAtUtc: string }
  | { kind: 'error'; message: string }

export function SystemStatusCard() {
  const [smokeState, setSmokeState] = useState<SmokeState>({ kind: 'loading' })

  useEffect(() => {
    let active = true

    const runSmokeCheck = async () => {
      try {
        const payload = await fetchHealthStatus()
        if (!active) {
          return
        }

        setSmokeState({
          kind: 'ready',
          status: payload.status,
          checkedAtUtc: payload.checkedAtUtc,
        })
      } catch (error) {
        if (!active) {
          return
        }

        const message = error instanceof Error ? error.message : 'Unknown smoke-check error'
        setSmokeState({ kind: 'error', message })
      }
    }

    void runSmokeCheck()

    return () => {
      active = false
    }
  }, [])

  return (
    <Card title="System Status" className="overflow-hidden">
      <div className="flex flex-col gap-6">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="flex items-center gap-3 p-3 rounded-xl bg-slate-50 border border-slate-100 shadow-sm">
            <div className="h-2 w-2 rounded-full bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.3)]" />
            <span className="text-sm font-bold text-slate-900">Frontend Scaffold</span>
            <span className="ml-auto text-[10px] text-slate-500 uppercase font-black">Ready</span>
          </div>
          <div className="flex items-center gap-3 p-3 rounded-xl bg-slate-50 border border-slate-100 shadow-sm">
            <div className="h-2 w-2 rounded-full bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.3)]" />
            <span className="text-sm font-bold text-slate-900">Route Baseline</span>
            <span className="ml-auto text-[10px] text-slate-500 uppercase font-black">Active</span>
          </div>
        </div>

        <div className="p-4 rounded-xl bg-indigo-50 border border-indigo-100 shadow-sm">
          <div className="text-xs font-black text-indigo-600 uppercase tracking-wider mb-2">API Connection</div>
          <div className="flex items-center gap-2 mb-3">
            <code className="text-[10px] bg-white border border-indigo-100 px-2 py-1 rounded text-slate-600 font-bold">{API_BASE_URL}</code>
          </div>
          
          {smokeState.kind === 'loading' && (
            <div className="flex items-center gap-2 text-sm text-slate-500 animate-pulse font-medium">
              <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none"/><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/></svg>
              Checking /health...
            </div>
          )}

          {smokeState.kind === 'ready' && (
            <div className="flex flex-col gap-1">
              <div className="flex items-center gap-2 text-sm text-emerald-600 font-bold">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round"><path d="M20 6 9 17l-5-5"/></svg>
                {smokeState.status}
              </div>
              <div className="text-[10px] text-slate-500 pl-6 font-medium">
                Verified at {new Date(smokeState.checkedAtUtc).toLocaleTimeString()}
              </div>
            </div>
          )}

          {smokeState.kind === 'error' && (
            <div className="flex items-center gap-2 text-sm text-red-600 font-bold">
              <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
              {smokeState.message}
            </div>
          )}
        </div>
      </div>
    </Card>
  )
}
