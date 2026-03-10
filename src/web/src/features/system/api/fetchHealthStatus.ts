import { apiClient, type ApiClient } from '../../../shared/api/httpClient'

export interface HealthStatusPayload {
  status: string
  checkedAtUtc: string
}

export async function fetchHealthStatus(client: ApiClient = apiClient): Promise<HealthStatusPayload> {
  const payload = await client.request<unknown>('/health')

  if (!isHealthStatusPayload(payload)) {
    throw new Error('Invalid health status payload returned by API.')
  }

  return payload
}

function isHealthStatusPayload(value: unknown): value is HealthStatusPayload {
  if (!value || typeof value !== 'object') {
    return false
  }

  const payload = value as Partial<HealthStatusPayload>
  return typeof payload.status === 'string' && typeof payload.checkedAtUtc === 'string'
}
