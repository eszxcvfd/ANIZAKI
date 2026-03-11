import { useRef, useState, type RefObject } from 'react'
import { ApiClientError } from '../../shared/api/httpClient'
import { resetPassword } from '../../features/auth/api/authApi'
import { validateResetPasswordInput, type AuthFieldErrors } from '../../features/auth/model/authFormValidation'
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

export function ResetPasswordPage() {
  const resetTokenRef = useRef<HTMLInputElement>(null)
  const resetPasswordRef = useRef<HTMLInputElement>(null)

  const [resetToken, setResetToken] = useState('')
  const [resetPasswordValue, setResetPasswordValue] = useState('')
  const [resetState, setResetState] = useState<SubmissionState>(IDLE_STATE)
  const [resetErrors, setResetErrors] = useState<AuthFieldErrors>({})

  async function handleResetPassword() {
    const validationErrors = validateResetPasswordInput(resetToken, resetPasswordValue)
    setResetErrors(validationErrors)
    if (Object.keys(validationErrors).length > 0) {
      setResetState({ status: 'error', message: 'Please check the provided information.' })
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
          ? `Password successfully reset at ${new Date(response.passwordChangedAtUtc).toLocaleString()}.`
          : 'Failed to reset password.',
      })
      setTimeout(() => {
        window.location.href = '/auth/login'
      }, 3000)
    } catch (error) {
      setResetState({
        status: 'error',
        message: toAuthErrorMessage(error, 'Password reset request failed.'),
      })
    }
  }

  return (
    <AuthLayout title="Reset Password" subtitle="Enter your recovery token and choose a new password">
      <form
        className="flex flex-col gap-5 mt-8"
        onSubmit={(event) => {
          event.preventDefault()
          void handleResetPassword()
        }}
        noValidate
      >
        <div className="flex flex-col gap-4">
          <AuthInput
            ref={resetTokenRef}
            value={resetToken}
            onChange={(event) => setResetToken(event.target.value)}
            placeholder="Recovery Token"
            type="text"
            autoComplete="off"
            error={resetErrors.token}
          />
          
          <AuthInput
            ref={resetPasswordRef}
            value={resetPasswordValue}
            onChange={(event) => setResetPasswordValue(event.target.value)}
            placeholder="New Password"
            type="password"
            autoComplete="new-password"
            error={resetErrors.newPassword}
          />
        </div>

        <div className="pt-2">
          <AuthButton isLoading={resetState.status === 'loading'} type="submit">
            Reset Password
          </AuthButton>
        </div>

        <AuthStatusMessage state={resetState} />

        <div className="text-center mt-6 text-slate-600 text-sm font-medium">
          Remembered your password?{' '}
          <a href="/auth/login" className="text-indigo-600 hover:text-indigo-500 font-bold transition-colors">
            Sign In
          </a>
        </div>
      </form>
    </AuthLayout>
  )
}
