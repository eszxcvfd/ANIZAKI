export type AuthRole = 'user' | 'seller' | 'admin'

export type AuthSession = {
  accessToken: string
  refreshToken?: string
  userId: string
  userEmail: string
  role: AuthRole
}

export const AUTH_SESSION_STORAGE_KEY = 'anizaki.auth.session'

export function readAuthSession(
  storage: Pick<Storage, 'getItem'> | undefined = window.localStorage,
): AuthSession | null {
  const rawValue = storage?.getItem(AUTH_SESSION_STORAGE_KEY)
  if (!rawValue) {
    return null
  }

  try {
    return asAuthSession(JSON.parse(rawValue))
  } catch {
    return null
  }
}

export function writeAuthSession(
  session: AuthSession,
  storage: Pick<Storage, 'setItem'> | undefined = window.localStorage,
): void {
  storage?.setItem(AUTH_SESSION_STORAGE_KEY, JSON.stringify(session))
}

export function clearAuthSession(
  storage: Pick<Storage, 'removeItem'> | undefined = window.localStorage,
): void {
  storage?.removeItem(AUTH_SESSION_STORAGE_KEY)
}

function asAuthSession(value: unknown): AuthSession | null {
  if (!value || typeof value !== 'object') {
    return null
  }

  const payload = value as Record<string, unknown>
  const accessToken = readNonEmptyString(payload.accessToken)
  const refreshToken = readNonEmptyString(payload.refreshToken)
  const userId = readNonEmptyString(payload.userId)
  const userEmail = readNonEmptyString(payload.userEmail)
  const role = readRole(payload.role)

  if (!accessToken || !userId || !userEmail || !role) {
    return null
  }

  return {
    accessToken,
    refreshToken: refreshToken ?? undefined,
    userId,
    userEmail,
    role,
  }
}

function readNonEmptyString(value: unknown): string | null {
  if (typeof value !== 'string') {
    return null
  }

  const normalized = value.trim()
  return normalized || null
}

function readRole(value: unknown): AuthRole | null {
  if (value === 'user' || value === 'seller' || value === 'admin') {
    return value
  }

  return null
}
