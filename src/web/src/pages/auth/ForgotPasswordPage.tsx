import { useRef, useState, type RefObject } from 'react'
import { ApiClientError } from '../../shared/api/httpClient'
import { forgotPassword } from '../../features/auth/api/authApi'
import { validateForgotPasswordInput, type AuthFieldErrors } from '../../features/auth/model/authFormValidation'
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

export function ForgotPasswordPage() {
  const forgotEmailRef = useRef<HTMLInputElement>(null)

  const [forgotEmail, setForgotEmail] = useState('')
  const [forgotState, setForgotState] = useState<SubmissionState>(IDLE_STATE)
  const [forgotErrors, setForgotErrors] = useState<AuthFieldErrors>({})

  async function handleForgotPassword() {
    const validationErrors = validateForgotPasswordInput(forgotEmail)
    setForgotErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) {
      setForgotState({ status: 'error', message: 'Please enter a valid email address.' })
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
          ? 'Request received. Redirecting to reset password page...'
          : 'Request could not be accepted at this time.',
      })
      setTimeout(() => {
        window.location.href = '/auth/reset-password'
      }, 3000)
    } catch (error) {
      setForgotState({
        status: 'error',
        message: toAuthErrorMessage(error, 'Failed to send recovery code. Please try again.'),
      })
    }
  }

  return (
    <AuthLayout title="Recover Password" subtitle="Enter your email to receive recovery instructions">
      <form
        className="flex flex-col gap-5 mt-8"
        onSubmit={(event) => {
          event.preventDefault()
          void handleForgotPassword()
        }}
        noValidate
      >
        <AuthInput
          ref={forgotEmailRef}
          value={forgotEmail}
          onChange={(event) => setForgotEmail(event.target.value)}
          placeholder="Registered Email"
          type="email"
          autoComplete="email"
          error={forgotErrors.email}
        />

        <div className="pt-2">
          <AuthButton isLoading={forgotState.status === 'loading'} type="submit">
            Send Recovery Code
          </AuthButton>
        </div>

        <AuthStatusMessage state={forgotState} />

        <div className="text-center mt-6 text-slate-600 text-sm font-medium">
          Remembered your password?{' '}
          <a href="/auth/login" className="text-indigo-600 hover:text-indigo-500 font-bold transition-colors">
            Back to Sign In
          </a>
        </div>
      </form>
    </AuthLayout>
  )
}
