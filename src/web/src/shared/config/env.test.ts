import { describe, expect, it } from 'vitest'
import { resolveApiBaseUrl } from './env'

describe('resolveApiBaseUrl', () => {
  it('uses fallback API url when env value is empty', () => {
    const result = resolveApiBaseUrl({ VITE_API_BASE_URL: '' })

    expect(result).toBe('http://localhost:5080')
  })

  it('normalizes valid URL by removing trailing slash', () => {
    const result = resolveApiBaseUrl({ VITE_API_BASE_URL: 'http://localhost:5080/' })

    expect(result).toBe('http://localhost:5080')
  })

  it('throws when URL is invalid', () => {
    expect(() => resolveApiBaseUrl({ VITE_API_BASE_URL: 'not-a-url' })).toThrowError(
      /Invalid VITE_API_BASE_URL/,
    )
  })
})
