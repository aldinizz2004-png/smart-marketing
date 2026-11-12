export type FieldErrors = Record<string, string[]>
export type ProblemBody = { title?: string | null; detail?: string | null; errors?: FieldErrors } | null

// What the API returns as ProblemDetails / ValidationProblemDetails, or a network failure (status 0).
export class ApiError extends Error {
  readonly status: number
  readonly title: string | null
  readonly detail: string | null
  readonly fieldErrors: FieldErrors
  readonly retryAfterSeconds: number | null

  constructor(status: number, body: ProblemBody, retryAfterSeconds: number | null = null) {
    super(body?.detail ?? body?.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.title = body?.title ?? null
    this.detail = body?.detail ?? null
    this.fieldErrors = body?.errors ?? {}
    this.retryAfterSeconds = retryAfterSeconds
  }

  firstFieldError(): string | null {
    const first = Object.values(this.fieldErrors)[0]
    return first?.[0] ?? null
  }
}

type FetchResult<T> = { data?: T; error?: unknown; response: Response }

function toApiError(error: unknown, response: Response) {
  const retryAfter = Number(response.headers.get('Retry-After'))
  const body = typeof error === 'object' && error !== null ? (error as ProblemBody) : null
  return new ApiError(response.status, body, Number.isFinite(retryAfter) && retryAfter > 0 ? retryAfter : null)
}

async function settle<T>(request: Promise<FetchResult<T>>): Promise<FetchResult<T>> {
  try {
    return await request
  } catch (cause) {
    if (cause instanceof ApiError) throw cause
    throw new ApiError(0, { detail: cause instanceof Error ? cause.message : null })
  }
}

// openapi-fetch never throws on HTTP errors; these turn them into exceptions for TanStack Query.
export async function unwrap<T>(request: Promise<FetchResult<T>>): Promise<NonNullable<T>> {
  const result = await settle(request)
  if (!result.response.ok) throw toApiError(result.error, result.response)
  return result.data as NonNullable<T>
}

export async function unwrapEmpty(request: Promise<FetchResult<unknown>>): Promise<void> {
  const result = await settle(request)
  if (!result.response.ok) throw toApiError(result.error, result.response)
}
