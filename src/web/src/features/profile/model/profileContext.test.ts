import { describe, expect, it } from 'vitest'
import { ApiClientError } from '../../../shared/api/httpClient'
import {
  buildProfileRequestContext,
  toProfileErrorMessage,
} from './profileContext'

describe('profileContext', () => {
  it('returns null when access token is blank', () => {
    const context = buildProfileRequestContext({
      accessToken: '  ',
      userId: 'user-1',
      userEmail: 'person@example.com',
      userRole: 'user',
    })

    expect(context).toBeNull()
  })

  it('builds request context with only non-empty auth headers', () => {
    const context = buildProfileRequestContext({
      accessToken: 'access-token',
      userId: 'user-1',
      userEmail: ' ',
      userRole: 'admin',
    })

    expect(context).not.toBeNull()

    const headers = new Headers(context?.headers)
    expect(context?.accessToken).toBe('access-token')
    expect(headers.get('X-Anizaki-User-Id')).toBe('user-1')
    expect(headers.get('X-Anizaki-User-Email')).toBeNull()
    expect(headers.get('X-Anizaki-User-Role')).toBe('admin')
  })

  it('formats API client errors with status code', () => {
    const error = new ApiClientError('Unauthorized request', 401, 'unauthorized')

    const message = toProfileErrorMessage(error, 'fallback')

    expect(message).toBe('Unauthorized request (HTTP 401)')
  })

  it('falls back for non-error throw values', () => {
    const message = toProfileErrorMessage({ unknown: true }, 'fallback')

    expect(message).toBe('fallback')
  })
})

