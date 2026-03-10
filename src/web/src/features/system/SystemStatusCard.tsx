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
    <Card title="System status">
      <p>Frontend scaffold is operational.</p>
      <ul>
        <li>App shell: ready</li>
        <li>Route baseline: ready</li>
        <li>Feature boundaries: ready</li>
        <li>API base URL: {API_BASE_URL}</li>
        {smokeState.kind === 'loading' ? <li>API smoke: checking /health...</li> : null}
        {smokeState.kind === 'ready' ? (
          <li>
            API smoke: {smokeState.status} ({smokeState.checkedAtUtc})
          </li>
        ) : null}
        {smokeState.kind === 'error' ? <li>API smoke: failed ({smokeState.message})</li> : null}
      </ul>
    </Card>
  )
}
