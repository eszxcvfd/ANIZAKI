import { useRef, useState, type RefObject } from 'react'
import { ApiClientError } from '../../shared/api/httpClient'
import { verifyEmail } from '../../features/auth/api/authApi'
import { validateVerifyEmailInput, type AuthFieldErrors } from '../../features/auth/model/authFormValidation'
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

export function VerifyEmailPage() {
  const verifyTokenRef = useRef<HTMLInputElement>(null)

  const [verifyToken, setVerifyToken] = useState('')
  const [verifyState, setVerifyState] = useState<SubmissionState>(IDLE_STATE)
  const [verifyErrors, setVerifyErrors] = useState<AuthFieldErrors>({})

  async function handleVerifyEmail() {
    const validationErrors = validateVerifyEmailInput(verifyToken)
    setVerifyErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) {
      setVerifyState({ status: 'error', message: 'Please enter a valid verification token.' })
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
          ? `Account ${response.email} has been successfully verified at ${new Date(response.verifiedAtUtc).toLocaleString()}.`
          : 'Email verification failed.',
      })
      setTimeout(() => {
        window.location.href = '/auth/login'
      }, 3000)
    } catch (error) {
      setVerifyState({
        status: 'error',
        message: toAuthErrorMessage(error, 'Verification request failed.'),
      })
    }
  }

  return (
    <AuthLayout title="Verify Email" subtitle="Enter the verification code sent to your inbox">
      <form
        className="flex flex-col gap-5 mt-8"
        onSubmit={(event) => {
          event.preventDefault()
          void handleVerifyEmail()
        }}
        noValidate
      >
        <AuthInput
          ref={verifyTokenRef}
          value={verifyToken}
          onChange={(event) => setVerifyToken(event.target.value)}
          placeholder="Verification Token"
          type="text"
          autoComplete="off"
          error={verifyErrors.token}
        />

        <div className="pt-2">
          <AuthButton isLoading={verifyState.status === 'loading'} type="submit">
            Verify Account
          </AuthButton>
        </div>

        <AuthStatusMessage state={verifyState} />

        <div className="text-center mt-6 text-slate-600 text-sm font-medium">
          Already verified?{' '}
          <a href="/auth/login" className="text-indigo-600 hover:text-indigo-500 font-bold transition-colors">
            Sign In Now
          </a>
        </div>
      </form>
    </AuthLayout>
  )
}
