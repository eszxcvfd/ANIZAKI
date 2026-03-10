import { describe, expect, it, vi } from 'vitest'
import type { ApiClient } from '../../../shared/api/httpClient'
import {
  forgotPassword,
  getMyProfile,
  login,
  logout,
  registerUser,
  resetPassword,
  updateMyProfile,
  verifyEmail,
} from './authApi'

describe('authApi', () => {
  it('registerUser posts payload and returns typed response', async () => {
    const request = vi.fn().mockResolvedValue({
      userId: '3156b1dc-f293-4414-b771-0f9ebb01a3ea',
      email: 'person@example.com',
      verificationRequired: true,
      verificationTokenExpiresAtUtc: '2026-03-10T08:00:00Z',
    })
    const client = createClient(request as unknown as ApiClient['request'])

    const result = await registerUser({ email: 'person@example.com', password: 'Password123!' }, client)

    expect(request).toHaveBeenCalledWith('/api/v1/auth/register', {
      method: 'POST',
      body: { email: 'person@example.com', password: 'Password123!' },
    })
    expect(result.email).toBe('person@example.com')
    expect(result.verificationRequired).toBe(true)
  })

  it('login rejects invalid response payload shape', async () => {
    const request = vi.fn().mockResolvedValue({ invalid: true })
    const client = createClient(request as unknown as ApiClient['request'])

    await expect(login({ email: 'person@example.com', password: 'Password123!' }, client)).rejects.toThrowError(
      /Invalid login response/,
    )
  })

  it('logout includes bearer token in headers', async () => {
    const request = vi.fn().mockResolvedValue({ revoked: true })
    const client = createClient(request as unknown as ApiClient['request'])

    const result = await logout(
      { refreshToken: 'refresh-token' },
      { accessToken: 'access-token', headers: { 'X-Anizaki-User-Id': 'test-user' } },
      client,
    )

    const options = request.mock.calls[0][1] as { headers: Headers; method: string }
    const headers = new Headers(options.headers)

    expect(options.method).toBe('POST')
    expect(headers.get('Authorization')).toBe('Bearer access-token')
    expect(headers.get('X-Anizaki-User-Id')).toBe('test-user')
    expect(result.revoked).toBe(true)
  })

  it('forgotPassword posts request and returns accepted flag', async () => {
    const request = vi.fn().mockResolvedValue({ accepted: true })
    const client = createClient(request as unknown as ApiClient['request'])

    const result = await forgotPassword({ email: 'person@example.com' }, client)

    expect(request).toHaveBeenCalledWith('/api/v1/auth/forgot-password', {
      method: 'POST',
      body: { email: 'person@example.com' },
    })
    expect(result.accepted).toBe(true)
  })

  it('resetPassword and verifyEmail guard required fields', async () => {
    const request = vi
      .fn()
      .mockResolvedValueOnce({ passwordReset: true, passwordChangedAtUtc: '2026-03-10T08:00:00Z' })
      .mockResolvedValueOnce({ verified: true, verifiedAtUtc: '2026-03-10T08:00:00Z', email: 'person@example.com' })
    const client = createClient(request as unknown as ApiClient['request'])

    const resetResult = await resetPassword({ token: 'token', newPassword: 'NewPassword123!' }, client)
    const verifyResult = await verifyEmail({ token: 'token' }, client)

    expect(resetResult.passwordReset).toBe(true)
    expect(verifyResult.verified).toBe(true)
    expect(verifyResult.email).toBe('person@example.com')
  })

  it('getMyProfile and updateMyProfile return typed payload', async () => {
    const request = vi
      .fn()
      .mockResolvedValueOnce({
        userId: '3156b1dc-f293-4414-b771-0f9ebb01a3ea',
        email: 'person@example.com',
        role: 'user',
        emailVerified: false,
        emailVerifiedAtUtc: null,
        createdAtUtc: '2026-03-10T08:00:00Z',
        updatedAtUtc: '2026-03-10T08:00:00Z',
      })
      .mockResolvedValueOnce({
        userId: '3156b1dc-f293-4414-b771-0f9ebb01a3ea',
        email: 'updated@example.com',
        role: 'user',
        emailVerified: false,
        emailVerifiedAtUtc: null,
        createdAtUtc: '2026-03-10T08:00:00Z',
        updatedAtUtc: '2026-03-10T09:00:00Z',
      })
    const client = createClient(request as unknown as ApiClient['request'])

    const context = { accessToken: 'access-token' }
    const profile = await getMyProfile(context, client)
    const updated = await updateMyProfile('updated@example.com', context, client)

    expect(profile.email).toBe('person@example.com')
    expect(updated.email).toBe('updated@example.com')
    expect(updated.updatedAtUtc).toBe('2026-03-10T09:00:00Z')
  })
})

function createClient(request: ApiClient['request']): ApiClient {
  return {
    request,
    getPage: vi.fn() as ApiClient['getPage'],
  }
}
