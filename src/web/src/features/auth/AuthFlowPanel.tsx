import { useRef, useState, type RefObject } from 'react'
import { ApiClientError } from '../../shared/api/httpClient'
import { Card } from '../../shared/ui/Card'
import {
  forgotPassword,
  login,
  registerUser,
  resetPassword,
  verifyEmail,
} from './api/authApi'
import {
  validateForgotPasswordInput,
  validateLoginInput,
  validateRegisterInput,
  validateResetPasswordInput,
  validateVerifyEmailInput,
  type AuthFieldErrors,
} from './model/authFormValidation'
import { writeAuthSession, type AuthRole } from './session/authSession'

type SubmissionState = {
  status: 'idle' | 'loading' | 'success' | 'error'
  message: string | null
}

const IDLE_STATE: SubmissionState = {
  status: 'idle',
  message: null,
}

export function AuthFlowPanel() {
  const registerEmailRef = useRef<HTMLInputElement>(null)
  const registerPasswordRef = useRef<HTMLInputElement>(null)
  const loginEmailRef = useRef<HTMLInputElement>(null)
  const loginPasswordRef = useRef<HTMLInputElement>(null)
  const forgotEmailRef = useRef<HTMLInputElement>(null)
  const resetTokenRef = useRef<HTMLInputElement>(null)
  const resetPasswordRef = useRef<HTMLInputElement>(null)
  const verifyTokenRef = useRef<HTMLInputElement>(null)

  const [registerEmail, setRegisterEmail] = useState('')
  const [registerPassword, setRegisterPassword] = useState('')
  const [registerState, setRegisterState] = useState<SubmissionState>(IDLE_STATE)
  const [registerErrors, setRegisterErrors] = useState<AuthFieldErrors>({})

  const [loginEmail, setLoginEmail] = useState('')
  const [loginPassword, setLoginPassword] = useState('')
  const [loginState, setLoginState] = useState<SubmissionState>(IDLE_STATE)
  const [loginErrors, setLoginErrors] = useState<AuthFieldErrors>({})

  const [forgotEmail, setForgotEmail] = useState('')
  const [forgotState, setForgotState] = useState<SubmissionState>(IDLE_STATE)
  const [forgotErrors, setForgotErrors] = useState<AuthFieldErrors>({})

  const [resetToken, setResetToken] = useState('')
  const [resetPasswordValue, setResetPasswordValue] = useState('')
  const [resetState, setResetState] = useState<SubmissionState>(IDLE_STATE)
  const [resetErrors, setResetErrors] = useState<AuthFieldErrors>({})

  const [verifyToken, setVerifyToken] = useState('')
  const [verifyState, setVerifyState] = useState<SubmissionState>(IDLE_STATE)
  const [verifyErrors, setVerifyErrors] = useState<AuthFieldErrors>({})

  const inputClassName =
    'w-full rounded border border-slate-300 px-3 py-2 text-slate-800 focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200'
  const buttonClassName =
    'rounded border border-sky-700 bg-sky-700 px-3 py-2 text-white disabled:cursor-not-allowed disabled:opacity-60'

  async function handleRegister() {
    const validationErrors = validateRegisterInput(registerEmail, registerPassword)
    setRegisterErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) {
      setRegisterState({ status: 'error', message: 'Please fix validation errors and retry.' })
      focusFirstInvalidField(validationErrors, {
        email: registerEmailRef,
        password: registerPasswordRef,
      })
      return
    }

    const email = registerEmail.trim()
    const password = registerPassword.trim()

    setRegisterState({ status: 'loading', message: null })

    try {
      const response = await registerUser({ email, password })
      setRegisterState({
        status: 'success',
        message: `Registration accepted for ${response.email}. Verification expires at ${response.verificationTokenExpiresAtUtc}.`,
      })
    } catch (error) {
      setRegisterState({
        status: 'error',
        message: toAuthErrorMessage(error, 'Registration failed.'),
      })
    }
  }

  async function handleLogin() {
    const validationErrors = validateLoginInput(loginEmail, loginPassword)
    setLoginErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) {
      setLoginState({ status: 'error', message: 'Please fix validation errors and retry.' })
      focusFirstInvalidField(validationErrors, {
        email: loginEmailRef,
        password: loginPasswordRef,
      })
      return
    }

    const email = loginEmail.trim()
    const password = loginPassword.trim()

    setLoginState({ status: 'loading', message: null })

    try {
      const response = await login({ email, password })
      const role = toAuthRole(response.role)
      if (!role) {
        setLoginState({
          status: 'error',
          message: `Unsupported role claim returned by API: ${response.role}`,
        })
        return
      }

      writeAuthSession({
        accessToken: response.tokens.accessToken,
        refreshToken: response.tokens.refreshToken,
        userId: response.userId,
        userEmail: response.email,
        role,
      })

      setLoginState({
        status: 'success',
        message: 'Login successful. Session saved for guarded routes.',
      })
    } catch (error) {
      setLoginState({
        status: 'error',
        message: toAuthErrorMessage(error, 'Login failed.'),
      })
    }
  }

  async function handleForgotPassword() {
    const validationErrors = validateForgotPasswordInput(forgotEmail)
    setForgotErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) {
      setForgotState({ status: 'error', message: 'Please fix validation errors and retry.' })
      focusFirstInvalidField(validationErrors, { email: forgotEmailRef })
      return
    }

    const email = forgotEmail.trim()

    setForgotState({ status: 'loading', message: null })

    try {
      const response = await forgotPassword({ email })
      setForgotState({
        status: 'success',
        message: response.accepted
          ? 'Reset request accepted. Check your inbox for the reset token.'
          : 'Reset request was not accepted.',
      })
    } catch (error) {
      setForgotState({
        status: 'error',
        message: toAuthErrorMessage(error, 'Forgot-password request failed.'),
      })
    }
  }

  async function handleResetPassword() {
    const validationErrors = validateResetPasswordInput(resetToken, resetPasswordValue)
    setResetErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) {
      setResetState({ status: 'error', message: 'Please fix validation errors and retry.' })
      focusFirstInvalidField(validationErrors, {
        token: resetTokenRef,
        newPassword: resetPasswordRef,
      })
      return
    }

    const token = resetToken.trim()
    const newPassword = resetPasswordValue.trim()

    setResetState({ status: 'loading', message: null })

    try {
      const response = await resetPassword({ token, newPassword })
      setResetState({
        status: 'success',
        message: response.passwordReset
          ? `Password reset completed at ${response.passwordChangedAtUtc}.`
          : 'Password reset was not completed.',
      })
    } catch (error) {
      setResetState({
        status: 'error',
        message: toAuthErrorMessage(error, 'Reset-password request failed.'),
      })
    }
  }

  async function handleVerifyEmail() {
    const validationErrors = validateVerifyEmailInput(verifyToken)
    setVerifyErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) {
      setVerifyState({ status: 'error', message: 'Please fix validation errors and retry.' })
      focusFirstInvalidField(validationErrors, { token: verifyTokenRef })
      return
    }

    const token = verifyToken.trim()

    setVerifyState({ status: 'loading', message: null })

    try {
      const response = await verifyEmail({ token })
      setVerifyState({
        status: 'success',
        message: response.verified
          ? `Email ${response.email} verified at ${response.verifiedAtUtc}.`
          : 'Email verification was not completed.',
      })
    } catch (error) {
      setVerifyState({
        status: 'error',
        message: toAuthErrorMessage(error, 'Verify-email request failed.'),
      })
    }
  }

  return (
    <div className="grid gap-4">
      <Card title="Register">
        <form
          className="grid gap-2"
          onSubmit={(event) => {
            event.preventDefault()
            void handleRegister()
          }}
          noValidate
        >
          <input
            ref={registerEmailRef}
            className={inputClassName}
            value={registerEmail}
            onChange={(event) => setRegisterEmail(event.target.value)}
            placeholder="Email"
            type="email"
            autoComplete="off"
            aria-invalid={Boolean(registerErrors.email)}
            aria-describedby={registerErrors.email ? 'register-email-error' : undefined}
          />
          <FieldError id="register-email-error" message={registerErrors.email} />
          <input
            ref={registerPasswordRef}
            className={inputClassName}
            value={registerPassword}
            onChange={(event) => setRegisterPassword(event.target.value)}
            placeholder="Password"
            type="password"
            autoComplete="new-password"
            aria-invalid={Boolean(registerErrors.password)}
            aria-describedby={registerErrors.password ? 'register-password-error' : undefined}
          />
          <FieldError id="register-password-error" message={registerErrors.password} />
          <button
            className={buttonClassName}
            type="submit"
            disabled={registerState.status === 'loading'}
          >
            {registerState.status === 'loading'
              ? 'Submitting...'
              : registerState.status === 'error'
                ? 'Retry register'
                : 'Register'}
          </button>
          <StatusLine state={registerState} />
        </form>
      </Card>

      <Card title="Login">
        <form
          className="grid gap-2"
          onSubmit={(event) => {
            event.preventDefault()
            void handleLogin()
          }}
          noValidate
        >
          <input
            ref={loginEmailRef}
            className={inputClassName}
            value={loginEmail}
            onChange={(event) => setLoginEmail(event.target.value)}
            placeholder="Email"
            type="email"
            autoComplete="off"
            aria-invalid={Boolean(loginErrors.email)}
            aria-describedby={loginErrors.email ? 'login-email-error' : undefined}
          />
          <FieldError id="login-email-error" message={loginErrors.email} />
          <input
            ref={loginPasswordRef}
            className={inputClassName}
            value={loginPassword}
            onChange={(event) => setLoginPassword(event.target.value)}
            placeholder="Password"
            type="password"
            autoComplete="current-password"
            aria-invalid={Boolean(loginErrors.password)}
            aria-describedby={loginErrors.password ? 'login-password-error' : undefined}
          />
          <FieldError id="login-password-error" message={loginErrors.password} />
          <button
            className={buttonClassName}
            type="submit"
            disabled={loginState.status === 'loading'}
          >
            {loginState.status === 'loading'
              ? 'Signing in...'
              : loginState.status === 'error'
                ? 'Retry login'
                : 'Login'}
          </button>
          {loginState.status === 'success' ? (
            <p className="text-emerald-700">
              {loginState.message} Continue to <a href="/profile">/profile</a>.
            </p>
          ) : (
            <StatusLine state={loginState} />
          )}
        </form>
      </Card>

      <Card title="Forgot password">
        <form
          className="grid gap-2"
          onSubmit={(event) => {
            event.preventDefault()
            void handleForgotPassword()
          }}
          noValidate
        >
          <input
            ref={forgotEmailRef}
            className={inputClassName}
            value={forgotEmail}
            onChange={(event) => setForgotEmail(event.target.value)}
            placeholder="Email"
            type="email"
            autoComplete="off"
            aria-invalid={Boolean(forgotErrors.email)}
            aria-describedby={forgotErrors.email ? 'forgot-email-error' : undefined}
          />
          <FieldError id="forgot-email-error" message={forgotErrors.email} />
          <button
            className={buttonClassName}
            type="submit"
            disabled={forgotState.status === 'loading'}
          >
            {forgotState.status === 'loading'
              ? 'Submitting...'
              : forgotState.status === 'error'
                ? 'Retry reset request'
                : 'Request reset token'}
          </button>
          <StatusLine state={forgotState} />
        </form>
      </Card>

      <Card title="Reset password">
        <form
          className="grid gap-2"
          onSubmit={(event) => {
            event.preventDefault()
            void handleResetPassword()
          }}
          noValidate
        >
          <input
            ref={resetTokenRef}
            className={inputClassName}
            value={resetToken}
            onChange={(event) => setResetToken(event.target.value)}
            placeholder="Reset token"
            type="text"
            autoComplete="off"
            aria-invalid={Boolean(resetErrors.token)}
            aria-describedby={resetErrors.token ? 'reset-token-error' : undefined}
          />
          <FieldError id="reset-token-error" message={resetErrors.token} />
          <input
            ref={resetPasswordRef}
            className={inputClassName}
            value={resetPasswordValue}
            onChange={(event) => setResetPasswordValue(event.target.value)}
            placeholder="New password"
            type="password"
            autoComplete="new-password"
            aria-invalid={Boolean(resetErrors.newPassword)}
            aria-describedby={resetErrors.newPassword ? 'reset-password-error' : undefined}
          />
          <FieldError id="reset-password-error" message={resetErrors.newPassword} />
          <button
            className={buttonClassName}
            type="submit"
            disabled={resetState.status === 'loading'}
          >
            {resetState.status === 'loading'
              ? 'Submitting...'
              : resetState.status === 'error'
                ? 'Retry password reset'
                : 'Reset password'}
          </button>
          <StatusLine state={resetState} />
        </form>
      </Card>

      <Card title="Verify email">
        <form
          className="grid gap-2"
          onSubmit={(event) => {
            event.preventDefault()
            void handleVerifyEmail()
          }}
          noValidate
        >
          <input
            ref={verifyTokenRef}
            className={inputClassName}
            value={verifyToken}
            onChange={(event) => setVerifyToken(event.target.value)}
            placeholder="Verification token"
            type="text"
            autoComplete="off"
            aria-invalid={Boolean(verifyErrors.token)}
            aria-describedby={verifyErrors.token ? 'verify-token-error' : undefined}
          />
          <FieldError id="verify-token-error" message={verifyErrors.token} />
          <button
            className={buttonClassName}
            type="submit"
            disabled={verifyState.status === 'loading'}
          >
            {verifyState.status === 'loading'
              ? 'Submitting...'
              : verifyState.status === 'error'
                ? 'Retry verify email'
                : 'Verify email'}
          </button>
          <StatusLine state={verifyState} />
        </form>
      </Card>
    </div>
  )
}

function FieldError({ id, message }: { id: string; message?: string }) {
  if (!message) {
    return null
  }

  return (
    <p id={id} role="alert" className="text-sm text-rose-700">
      {message}
    </p>
  )
}

function StatusLine({ state }: { state: SubmissionState }) {
  if (!state.message) {
    return null
  }

  if (state.status === 'error') {
    return (
      <p role="alert" className="text-rose-700">
        {state.message}
      </p>
    )
  }

  return (
    <p role="status" aria-live="polite" className={state.status === 'success' ? 'text-emerald-700' : ''}>
      {state.message}
    </p>
  )
}

function toAuthRole(role: string): AuthRole | null {
  if (role === 'user' || role === 'seller' || role === 'admin') {
    return role
  }

  return null
}

function toAuthErrorMessage(error: unknown, fallbackMessage: string): string {
  if (error instanceof ApiClientError) {
    return `${error.message} (HTTP ${error.status})`
  }

  if (error instanceof Error && error.message.trim()) {
    return error.message
  }

  return fallbackMessage
}

function focusFirstInvalidField(
  errors: AuthFieldErrors,
  refs: Record<string, RefObject<HTMLInputElement | null>>,
): void {
  const firstErrorField = Object.keys(errors)[0]
  if (!firstErrorField) {
    return
  }

  refs[firstErrorField]?.current?.focus()
}
