const DEFAULT_API_BASE_URL = 'http://localhost:5080'

function normalizeApiBaseUrl(value: string): string {
  const trimmed = value.trim()
  if (!trimmed) {
    return DEFAULT_API_BASE_URL
  }

  const withoutTrailingSlash = trimmed.replace(/\/+$/, '')

  try {
    new URL(withoutTrailingSlash)
  } catch {
    throw new Error(
      `Invalid VITE_API_BASE_URL value: "${value}". Expected an absolute URL such as http://localhost:5080.`,
    )
  }

  return withoutTrailingSlash
}

export function resolveApiBaseUrl(
  env: Pick<ImportMetaEnv, 'VITE_API_BASE_URL'> = import.meta.env,
): string {
  return normalizeApiBaseUrl(env.VITE_API_BASE_URL ?? '')
}

export const API_BASE_URL = resolveApiBaseUrl()
