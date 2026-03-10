import { describe, expect, it, vi } from 'vitest'
import { ApiClientError, createApiClient } from './httpClient'

describe('httpClient', () => {
  it('builds request URL with query params and parses JSON payload', async () => {
    const fetcher = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ id: 7, name: 'ok' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    const client = createApiClient({ baseUrl: 'http://localhost:5080', fetcher })

    const result = await client.request<{ id: number; name: string }>('/api/v1/items', {
      query: { page: 2, pageSize: 10, tags: ['a', 'b'] },
    })

    expect(result).toEqual({ id: 7, name: 'ok' })
    expect(fetcher).toHaveBeenCalledTimes(1)
    expect(fetcher.mock.calls[0][0]).toContain('/api/v1/items?page=2&pageSize=10&tags=a&tags=b')
  })

  it('throws ApiClientError with standardized fields for JSON error payload', async () => {
    const fetcher = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ error: 'validation_failed', message: 'Invalid input' }), {
        status: 400,
        headers: { 'Content-Type': 'application/json' },
      }),
    )
    const client = createApiClient({ baseUrl: 'http://localhost:5080', fetcher })

    await expect(client.request('/api/v1/items')).rejects.toMatchObject({
      name: 'ApiClientError',
      status: 400,
      code: 'validation_failed',
      message: 'Invalid input',
    } satisfies Partial<ApiClientError>)
  })

  it('parses pagination from headers when body only contains items', async () => {
    const fetcher = vi.fn().mockResolvedValue(
      new Response(JSON.stringify([{ id: 1 }, { id: 2 }]), {
        status: 200,
        headers: {
          'Content-Type': 'application/json',
          'x-page': '3',
          'x-page-size': '2',
          'x-total-count': '10',
          'x-total-pages': '5',
        },
      }),
    )
    const client = createApiClient({ baseUrl: 'http://localhost:5080', fetcher })

    const page = await client.getPage<{ id: number }>('/api/v1/items')

    expect(page.items).toEqual([{ id: 1 }, { id: 2 }])
    expect(page.pagination).toEqual({
      page: 3,
      pageSize: 2,
      totalItems: 10,
      totalPages: 5,
    })
  })

  it('prefers pagination metadata from response body when present', async () => {
    const fetcher = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({
          items: [{ id: 1 }],
          pagination: { page: 9, pageSize: 1, totalItems: 30, totalPages: 30 },
        }),
        {
          status: 200,
          headers: { 'Content-Type': 'application/json', 'x-page': '1', 'x-page-size': '99' },
        },
      ),
    )
    const client = createApiClient({ baseUrl: 'http://localhost:5080', fetcher })

    const page = await client.getPage<{ id: number }>('/api/v1/items')

    expect(page.pagination).toEqual({
      page: 9,
      pageSize: 1,
      totalItems: 30,
      totalPages: 30,
    })
  })
})
