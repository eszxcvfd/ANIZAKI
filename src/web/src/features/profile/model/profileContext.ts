import type { AuthenticatedRequestContext } from '../../auth/api/authApi'
import { ApiClientError } from '../../../shared/api/httpClient'

export type ProfileAuthInput = {
  accessToken: string
  userId: string
  userEmail: string
  userRole: string
}

export function buildProfileRequestContext(
  input: ProfileAuthInput,
): AuthenticatedRequestContext | null {
  const accessToken = input.accessToken.trim()
  if (!accessToken) {
    return null
  }

  const headers = new Headers()
  appendHeader(headers, 'X-Anizaki-User-Id', input.userId)
  appendHeader(headers, 'X-Anizaki-User-Email', input.userEmail)
  appendHeader(headers, 'X-Anizaki-User-Role', input.userRole)

  return {
    accessToken,
    headers,
  }
}

export function toProfileErrorMessage(error: unknown, fallbackMessage: string): string {
  if (error instanceof ApiClientError) {
    return `${error.message} (HTTP ${error.status})`
  }

  if (error instanceof Error && error.message.trim()) {
    return error.message
  }

  return fallbackMessage
}

function appendHeader(headers: Headers, key: string, value: string): void {
  const normalized = value.trim()
  if (!normalized) {
    return
  }

  headers.set(key, normalized)
}

