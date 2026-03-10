import { apiClient, type ApiClient } from '../../../shared/api/httpClient'

export interface RegisterUserRequest {
  email: string
  password: string
}

export interface RegisterUserResponse {
  userId: string
  email: string
  verificationRequired: boolean
  verificationTokenExpiresAtUtc: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface AuthSessionTokens {
  accessToken: string
  accessTokenExpiresAtUtc: string
  refreshToken: string
  refreshTokenExpiresAtUtc: string
}

export interface LoginResponse {
  userId: string
  email: string
  role: string
  tokens: AuthSessionTokens
}

export interface LogoutRequest {
  refreshToken?: string
}

export interface LogoutResponse {
  revoked: boolean
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ForgotPasswordResponse {
  accepted: boolean
}

export interface ResetPasswordRequest {
  token: string
  newPassword: string
}

export interface ResetPasswordResponse {
  passwordReset: boolean
  passwordChangedAtUtc: string
}

export interface VerifyEmailRequest {
  token: string
}

export interface VerifyEmailResponse {
  verified: boolean
  verifiedAtUtc: string
  email: string
}

export interface MyProfileResponse {
  userId: string
  email: string
  role: string
  emailVerified: boolean
  emailVerifiedAtUtc: string | null
  createdAtUtc: string
  updatedAtUtc: string
}

export interface AuthenticatedRequestContext {
  accessToken: string
  headers?: HeadersInit
}

export async function registerUser(
  request: RegisterUserRequest,
  client: ApiClient = apiClient,
): Promise<RegisterUserResponse> {
  const payload = await client.request<unknown>('/api/v1/auth/register', {
    method: 'POST',
    body: request,
  })

  return asRegisterUserResponse(payload)
}

export async function login(
  request: LoginRequest,
  client: ApiClient = apiClient,
): Promise<LoginResponse> {
  const payload = await client.request<unknown>('/api/v1/auth/login', {
    method: 'POST',
    body: request,
  })

  return asLoginResponse(payload)
}

export async function logout(
  request: LogoutRequest,
  context: AuthenticatedRequestContext,
  client: ApiClient = apiClient,
): Promise<LogoutResponse> {
  const payload = await client.request<unknown>('/api/v1/auth/logout', {
    method: 'POST',
    body: request,
    headers: withAccessToken(context),
  })

  return asLogoutResponse(payload)
}

export async function forgotPassword(
  request: ForgotPasswordRequest,
  client: ApiClient = apiClient,
): Promise<ForgotPasswordResponse> {
  const payload = await client.request<unknown>('/api/v1/auth/forgot-password', {
    method: 'POST',
    body: request,
  })

  return asForgotPasswordResponse(payload)
}

export async function resetPassword(
  request: ResetPasswordRequest,
  client: ApiClient = apiClient,
): Promise<ResetPasswordResponse> {
  const payload = await client.request<unknown>('/api/v1/auth/reset-password', {
    method: 'POST',
    body: request,
  })

  return asResetPasswordResponse(payload)
}

export async function verifyEmail(
  request: VerifyEmailRequest,
  client: ApiClient = apiClient,
): Promise<VerifyEmailResponse> {
  const payload = await client.request<unknown>('/api/v1/auth/verify-email', {
    method: 'POST',
    body: request,
  })

  return asVerifyEmailResponse(payload)
}

export async function getMyProfile(
  context: AuthenticatedRequestContext,
  client: ApiClient = apiClient,
): Promise<MyProfileResponse> {
  const payload = await client.request<unknown>('/api/v1/users/me', {
    method: 'GET',
    headers: withAccessToken(context),
  })

  return asMyProfileResponse(payload)
}

export async function updateMyProfile(
  email: string,
  context: AuthenticatedRequestContext,
  client: ApiClient = apiClient,
): Promise<MyProfileResponse> {
  const payload = await client.request<unknown>('/api/v1/users/me', {
    method: 'PUT',
    body: { email },
    headers: withAccessToken(context),
  })

  return asMyProfileResponse(payload)
}

function withAccessToken(context: AuthenticatedRequestContext): Headers {
  const headers = new Headers(context.headers)
  headers.set('Authorization', `Bearer ${context.accessToken}`)
  return headers
}

function asRegisterUserResponse(value: unknown): RegisterUserResponse {
  const payload = asObject(value, 'register response')
  return {
    userId: readString(payload, 'userId', 'register response'),
    email: readString(payload, 'email', 'register response'),
    verificationRequired: readBoolean(payload, 'verificationRequired', 'register response'),
    verificationTokenExpiresAtUtc: readString(payload, 'verificationTokenExpiresAtUtc', 'register response'),
  }
}

function asLoginResponse(value: unknown): LoginResponse {
  const payload = asObject(value, 'login response')
  const tokens = asObject(payload.tokens, 'login response.tokens')

  return {
    userId: readString(payload, 'userId', 'login response'),
    email: readString(payload, 'email', 'login response'),
    role: readString(payload, 'role', 'login response'),
    tokens: {
      accessToken: readString(tokens, 'accessToken', 'login response.tokens'),
      accessTokenExpiresAtUtc: readString(tokens, 'accessTokenExpiresAtUtc', 'login response.tokens'),
      refreshToken: readString(tokens, 'refreshToken', 'login response.tokens'),
      refreshTokenExpiresAtUtc: readString(tokens, 'refreshTokenExpiresAtUtc', 'login response.tokens'),
    },
  }
}

function asLogoutResponse(value: unknown): LogoutResponse {
  const payload = asObject(value, 'logout response')
  return {
    revoked: readBoolean(payload, 'revoked', 'logout response'),
  }
}

function asForgotPasswordResponse(value: unknown): ForgotPasswordResponse {
  const payload = asObject(value, 'forgot-password response')
  return {
    accepted: readBoolean(payload, 'accepted', 'forgot-password response'),
  }
}

function asResetPasswordResponse(value: unknown): ResetPasswordResponse {
  const payload = asObject(value, 'reset-password response')
  return {
    passwordReset: readBoolean(payload, 'passwordReset', 'reset-password response'),
    passwordChangedAtUtc: readString(payload, 'passwordChangedAtUtc', 'reset-password response'),
  }
}

function asVerifyEmailResponse(value: unknown): VerifyEmailResponse {
  const payload = asObject(value, 'verify-email response')
  return {
    verified: readBoolean(payload, 'verified', 'verify-email response'),
    verifiedAtUtc: readString(payload, 'verifiedAtUtc', 'verify-email response'),
    email: readString(payload, 'email', 'verify-email response'),
  }
}

function asMyProfileResponse(value: unknown): MyProfileResponse {
  const payload = asObject(value, 'my-profile response')
  return {
    userId: readString(payload, 'userId', 'my-profile response'),
    email: readString(payload, 'email', 'my-profile response'),
    role: readString(payload, 'role', 'my-profile response'),
    emailVerified: readBoolean(payload, 'emailVerified', 'my-profile response'),
    emailVerifiedAtUtc: readNullableString(payload, 'emailVerifiedAtUtc', 'my-profile response'),
    createdAtUtc: readString(payload, 'createdAtUtc', 'my-profile response'),
    updatedAtUtc: readString(payload, 'updatedAtUtc', 'my-profile response'),
  }
}

function asObject(value: unknown, context: string): Record<string, unknown> {
  if (!value || typeof value !== 'object') {
    throw new Error(`Invalid ${context} payload returned by API.`)
  }

  return value as Record<string, unknown>
}

function readString(payload: Record<string, unknown>, key: string, context: string): string {
  const value = payload[key]
  if (typeof value !== 'string') {
    throw new Error(`Invalid ${context} payload returned by API.`)
  }

  return value
}

function readNullableString(payload: Record<string, unknown>, key: string, context: string): string | null {
  const value = payload[key]
  if (value === null) {
    return null
  }

  if (typeof value !== 'string') {
    throw new Error(`Invalid ${context} payload returned by API.`)
  }

  return value
}

function readBoolean(payload: Record<string, unknown>, key: string, context: string): boolean {
  const value = payload[key]
  if (typeof value !== 'boolean') {
    throw new Error(`Invalid ${context} payload returned by API.`)
  }

  return value
}
