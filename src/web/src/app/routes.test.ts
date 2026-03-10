import { describe, expect, it } from 'vitest'
import { resolveRoute } from './routes'
import type { AuthSession } from '../features/auth/session/authSession'

const USER_SESSION: AuthSession = {
  accessToken: 'access-token',
  userId: 'user-1',
  userEmail: 'person@example.com',
  role: 'user',
}

describe('resolveRoute', () => {
  it('returns Home route for root path', () => {
    expect(resolveRoute('/').title).toBe('Home')
  })

  it('redirects anonymous requests from protected route to login-required route', () => {
    expect(resolveRoute('/profile').title).toBe('Login Required')
  })

  it('returns Profile route for authenticated /profile path', () => {
    expect(resolveRoute('/profile', USER_SESSION).title).toBe('Profile')
  })

  it('blocks non-admin users from admin routes', () => {
    expect(resolveRoute('/admin/console', USER_SESSION).title).toBe('Access Denied')
  })

  it('allows admin users through admin route guards', () => {
    expect(resolveRoute('/admin/console', { ...USER_SESSION, role: 'admin' }).title).toBe('Admin Console')
  })

  it('returns Not Found route for unknown path', () => {
    expect(resolveRoute('/does-not-exist').title).toBe('Not Found')
  })
})
