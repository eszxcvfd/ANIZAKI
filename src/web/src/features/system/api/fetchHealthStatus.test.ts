import { describe, expect, it, vi } from 'vitest'
import type { ApiClient } from '../../../shared/api/httpClient'
import { fetchHealthStatus } from './fetchHealthStatus'

describe('fetchHealthStatus', () => {
  it('calls /health through shared api client', async () => {
    const client: ApiClient = {
      request: vi.fn().mockResolvedValue({ status: 'healthy', checkedAtUtc: '2026-03-10T00:00:00Z' }),
      getPage: vi.fn(),
    }

    const payload = await fetchHealthStatus(client)

    expect(client.request).toHaveBeenCalledWith('/health')
    expect(payload.status).toBe('healthy')
    expect(payload.checkedAtUtc).toBe('2026-03-10T00:00:00Z')
  })

  it('throws when API payload shape is invalid', async () => {
    const client: ApiClient = {
      request: vi.fn().mockResolvedValue({ invalid: true }),
      getPage: vi.fn(),
    }

    await expect(fetchHealthStatus(client)).rejects.toThrowError(/Invalid health status payload/)
  })
})
