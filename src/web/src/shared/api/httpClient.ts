import { API_BASE_URL } from '../config/env'

type QueryValue = string | number | boolean | null | undefined

export type QueryParams = Record<string, QueryValue | QueryValue[]>

export interface ApiRequestOptions extends Omit<RequestInit, 'body'> {
  query?: QueryParams
  body?: unknown
}

export type ApiGetOptions = Omit<ApiRequestOptions, 'body'>

export interface PaginationMeta {
  page: number
  pageSize: number
  totalItems?: number
  totalPages?: number
}

export interface PaginatedResult<TItem> {
  items: TItem[]
  pagination: PaginationMeta
}

export class ApiClientError extends Error {
  public readonly status: number
  public readonly code: string
  public readonly details?: unknown

  constructor(
    message: string,
    status: number,
    code: string,
    details?: unknown,
  ) {
    super(message)
    this.name = 'ApiClientError'
    this.status = status
    this.code = code
    this.details = details
  }
}

interface ApiClientConfig {
  baseUrl?: string
  fetcher?: typeof fetch
}

export interface ApiClient {
  request<TResponse>(path: string, options?: ApiRequestOptions): Promise<TResponse>
  getPage<TItem>(path: string, options?: ApiGetOptions): Promise<PaginatedResult<TItem>>
}

interface ErrorPayload {
  error?: string
  message?: string
  errors?: unknown
}

interface PaginationPayload {
  page?: number
  pageSize?: number
  totalItems?: number
  totalPages?: number
}

interface PaginatedBody<TItem> {
  items?: TItem[]
  pagination?: PaginationPayload
}

export function createApiClient(config: ApiClientConfig = {}): ApiClient {
  const baseUrl = stripTrailingSlash(config.baseUrl ?? API_BASE_URL)
  const fetcher = config.fetcher ?? fetch

  async function request<TResponse>(path: string, options: ApiRequestOptions = {}): Promise<TResponse> {
    const { query, body, headers, ...requestInit } = options
    const url = buildUrl(baseUrl, path, query)
    const normalizedHeaders = new Headers(headers)

    let serializedBody: BodyInit | undefined
    if (body !== undefined) {
      if (body instanceof FormData || body instanceof URLSearchParams || typeof body === 'string') {
        serializedBody = body
      } else {
        if (!normalizedHeaders.has('Content-Type')) {
          normalizedHeaders.set('Content-Type', 'application/json')
        }
        serializedBody = JSON.stringify(body)
      }
    }

    const response = await fetcher(url, {
      ...requestInit,
      headers: normalizedHeaders,
      body: serializedBody,
    })

    if (!response.ok) {
      throw await toApiError(response)
    }

    if (response.status === 204) {
      return undefined as TResponse
    }

    const contentType = response.headers.get('content-type') ?? ''
    if (contentType.includes('application/json')) {
      return (await response.json()) as TResponse
    }

    return (await response.text()) as TResponse
  }

  async function getPage<TItem>(
    path: string,
    options: ApiGetOptions = {},
  ): Promise<PaginatedResult<TItem>> {
    const { query, ...requestOptions } = options
    const url = buildUrl(baseUrl, path, query)
    const response = await fetcher(url, { ...requestOptions, method: requestOptions.method ?? 'GET' })

    if (!response.ok) {
      throw await toApiError(response)
    }

    const body = (await response.json()) as TItem[] | PaginatedBody<TItem>
    const items = Array.isArray(body) ? body : (body.items ?? [])
    const bodyPagination = !Array.isArray(body) ? body.pagination : undefined
    const headerPagination = readPaginationFromHeaders(response.headers)

    return {
      items,
      pagination: {
        page: bodyPagination?.page ?? headerPagination.page ?? 1,
        pageSize: bodyPagination?.pageSize ?? headerPagination.pageSize ?? Math.max(items.length, 1),
        totalItems: bodyPagination?.totalItems ?? headerPagination.totalItems,
        totalPages: bodyPagination?.totalPages ?? headerPagination.totalPages,
      },
    }
  }

  return {
    request,
    getPage,
  }
}

export const apiClient = createApiClient()

function buildUrl(baseUrl: string, path: string, query?: QueryParams): string {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  const url = new URL(`${baseUrl}${normalizedPath}`)

  if (query) {
    for (const [key, value] of Object.entries(query)) {
      appendQueryParam(url.searchParams, key, value)
    }
  }

  return url.toString()
}

function appendQueryParam(
  params: URLSearchParams,
  key: string,
  value: QueryValue | QueryValue[],
): void {
  if (Array.isArray(value)) {
    for (const item of value) {
      appendSingleValue(params, key, item)
    }
    return
  }

  appendSingleValue(params, key, value)
}

function appendSingleValue(params: URLSearchParams, key: string, value: QueryValue): void {
  if (value === null || value === undefined) {
    return
  }

  params.append(key, String(value))
}

function stripTrailingSlash(value: string): string {
  return value.replace(/\/+$/, '')
}

async function toApiError(response: Response): Promise<ApiClientError> {
  const contentType = response.headers.get('content-type') ?? ''
  if (contentType.includes('application/json')) {
    const payload = (await response.json()) as ErrorPayload
    return new ApiClientError(
      payload.message ?? 'API request failed.',
      response.status,
      payload.error ?? 'api_error',
      payload.errors,
    )
  }

  const message = await response.text()
  return new ApiClientError(message || 'API request failed.', response.status, 'api_error')
}

function readPaginationFromHeaders(headers: Headers): Partial<PaginationMeta> {
  return {
    page: toNumber(headers.get('x-page')),
    pageSize: toNumber(headers.get('x-page-size')),
    totalItems: toNumber(headers.get('x-total-count')),
    totalPages: toNumber(headers.get('x-total-pages')),
  }
}

function toNumber(value: string | null): number | undefined {
  if (!value) {
    return undefined
  }

  const parsed = Number.parseInt(value, 10)
  return Number.isNaN(parsed) ? undefined : parsed
}
