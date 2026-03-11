import { useRef, useState, type RefObject } from 'react'
import { ApiClientError } from '../../shared/api/httpClient'
import { login } from '../../features/auth/api/authApi'
import { validateLoginInput, type AuthFieldErrors } from '../../features/auth/model/authFormValidation'
import { writeAuthSession, type AuthRole } from '../../features/auth/session/authSession'
import { AuthLayout } from '../../features/auth/ui/AuthLayout'
import { AuthInput, AuthButton, AuthStatusMessage, IDLE_STATE, type SubmissionState } from '../../features/auth/ui/AuthFormComponents'

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
  if (!firstErrorField) return
  refs[firstErrorField]?.current?.focus()
}

export function LoginPage() {
  const loginEmailRef = useRef<HTMLInputElement>(null)
  const loginPasswordRef = useRef<HTMLInputElement>(null)

  const [loginEmail, setLoginEmail] = useState('')
  const [loginPassword, setLoginPassword] = useState('')
  const [loginState, setLoginState] = useState<SubmissionState>(IDLE_STATE)
  const [loginErrors, setLoginErrors] = useState<AuthFieldErrors>({})

  async function handleLogin() {
    const validationErrors = validateLoginInput(loginEmail, loginPassword)
    setLoginErrors(validationErrors)
    
    if (Object.keys(validationErrors).length > 0) {
      setLoginState({ status: 'error', message: 'Please check your login credentials.' })
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
          message: `Error: unsupported role (${response.role})`,
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
        message: 'Signed in successfully!',
      })
      
      // We wait briefly so user can see success message
      setTimeout(() => {
        window.location.href = '/profile'
      }, 500)
    } catch (error) {
      setLoginState({
        status: 'error',
        message: toAuthErrorMessage(error, 'Login failed. Please try again.'),
      })
    }
  }

  return (
    <AuthLayout title="Sign In" subtitle="Welcome back to Anizaki">
      <form
        className="flex flex-col gap-6 mt-8"
        onSubmit={(event) => {
          event.preventDefault()
          void handleLogin()
        }}
        noValidate
      >
        <div className="flex flex-col gap-4">
          <AuthInput
            ref={loginEmailRef}
            value={loginEmail}
            onChange={(event) => setLoginEmail(event.target.value)}
            placeholder="Email address"
            type="email"
            autoComplete="username"
            error={loginErrors.email}
          />
          
          <div className="flex flex-col gap-2">
            <AuthInput
              ref={loginPasswordRef}
              value={loginPassword}
              onChange={(event) => setLoginPassword(event.target.value)}
              placeholder="Password"
              type="password"
              autoComplete="current-password"
              error={loginErrors.password}
            />
            <div className="flex justify-end pr-1">
              <a href="/auth/forgot-password" className="text-xs text-indigo-600 hover:text-indigo-500 font-medium transition-colors">
                Forgot password?
              </a>
            </div>
          </div>
        </div>

        <AuthButton isLoading={loginState.status === 'loading'} type="submit">
          Continue to Platform
        </AuthButton>

        <AuthStatusMessage state={loginState} />
        
        <div className="text-center mt-4 text-slate-600 text-sm font-medium">
          Don't have an account?{' '}
          <a href="/auth/register" className="text-indigo-600 hover:text-indigo-500 font-bold transition-colors">
            Sign up
          </a>
        </div>
      </form>
    </AuthLayout>
  )
}
