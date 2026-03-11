import { useRef, useState, type RefObject } from 'react'
import { ApiClientError } from '../../shared/api/httpClient'
import { registerUser } from '../../features/auth/api/authApi'
import { validateRegisterInput, type AuthFieldErrors } from '../../features/auth/model/authFormValidation'
import { AuthLayout } from '../../features/auth/ui/AuthLayout'
import { AuthInput, AuthButton, AuthStatusMessage, IDLE_STATE, type SubmissionState } from '../../features/auth/ui/AuthFormComponents'

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

export function RegisterPage() {
  const registerEmailRef = useRef<HTMLInputElement>(null)
  const registerPasswordRef = useRef<HTMLInputElement>(null)

  const [registerEmail, setRegisterEmail] = useState('')
  const [registerPassword, setRegisterPassword] = useState('')
  const [registerState, setRegisterState] = useState<SubmissionState>(IDLE_STATE)
  const [registerErrors, setRegisterErrors] = useState<AuthFieldErrors>({})

  async function handleRegister() {
    const validationErrors = validateRegisterInput(registerEmail, registerPassword)
    setRegisterErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) {
      setRegisterState({ status: 'error', message: 'Please check the form for errors.' })
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
        message: `Account created successfully for ${response.email}. Please check your inbox for verification (link expires at ${new Date(response.verificationTokenExpiresAtUtc).toLocaleString()}).`,
      })
      
      // Delay to let them read success message then forward to verify or login
      setTimeout(() => {
        window.location.href = '/auth/verify-email'
      }, 5000)
    } catch (error) {
      setRegisterState({
        status: 'error',
        message: toAuthErrorMessage(error, 'Registration failed, please try again later.'),
      })
    }
  }

  return (
    <AuthLayout title="Create Account" subtitle="Join our premium community today">
      <form
        className="flex flex-col gap-5 mt-8"
        onSubmit={(event) => {
          event.preventDefault()
          void handleRegister()
        }}
        noValidate
      >
        <div className="flex flex-col gap-4">
          <AuthInput
            ref={registerEmailRef}
            value={registerEmail}
            onChange={(event) => setRegisterEmail(event.target.value)}
            placeholder="Email Address"
            type="email"
            autoComplete="email"
            error={registerErrors.email}
          />
          
          <AuthInput
            ref={registerPasswordRef}
            value={registerPassword}
            onChange={(event) => setRegisterPassword(event.target.value)}
            placeholder="Password"
            type="password"
            autoComplete="new-password"
            error={registerErrors.password}
          />
        </div>

        <div className="pt-2">
          <AuthButton isLoading={registerState.status === 'loading'} type="submit">
            Create Account
          </AuthButton>
        </div>

        <AuthStatusMessage state={registerState} />
        
        <div className="text-center mt-6 text-slate-600 text-sm font-medium">
          Already have an account?{' '}
          <a href="/auth/login" className="text-indigo-600 hover:text-indigo-500 font-bold transition-colors">
            Sign In
          </a>
        </div>
      </form>
    </AuthLayout>
  )
}
