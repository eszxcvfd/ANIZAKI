import { describe, expect, it, vi } from 'vitest'
import {
  AUTH_SESSION_STORAGE_KEY,
  clearAuthSession,
  readAuthSession,
  writeAuthSession,
} from './authSession'

describe('authSession', () => {
  it('reads valid session payload from storage', () => {
    const storage = {
      getItem: vi.fn().mockReturnValue(
        JSON.stringify({
          accessToken: 'token-1',
          refreshToken: 'refresh-1',
          userId: 'user-1',
          userEmail: 'person@example.com',
          role: 'admin',
        }),
      ),
    }

    const session = readAuthSession(storage)

    expect(storage.getItem).toHaveBeenCalledWith(AUTH_SESSION_STORAGE_KEY)
    expect(session?.role).toBe('admin')
    expect(session?.refreshToken).toBe('refresh-1')
  })

  it('returns null for invalid payload', () => {
    const storage = {
      getItem: vi.fn().mockReturnValue('{"accessToken":"token-1","role":"owner"}'),
    }

    expect(readAuthSession(storage)).toBeNull()
  })

  it('writes and clears storage', () => {
    const storage = {
      setItem: vi.fn(),
      removeItem: vi.fn(),
    }

    writeAuthSession(
      {
        accessToken: 'token-1',
        refreshToken: 'refresh-1',
        userId: 'user-1',
        userEmail: 'person@example.com',
        role: 'user',
      },
      storage,
    )
    clearAuthSession(storage)

    expect(storage.setItem).toHaveBeenCalledTimes(1)
    expect(storage.removeItem).toHaveBeenCalledWith(AUTH_SESSION_STORAGE_KEY)
  })
})
