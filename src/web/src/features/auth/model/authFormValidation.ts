export type AuthFieldErrors = Record<string, string>

export function validateRegisterInput(email: string, password: string): AuthFieldErrors {
  return {
    ...validateEmailField(email),
    ...validatePasswordField(password),
  }
}

export function validateLoginInput(email: string, password: string): AuthFieldErrors {
  return {
    ...validateEmailField(email),
    ...validatePasswordField(password),
  }
}

export function validateForgotPasswordInput(email: string): AuthFieldErrors {
  return validateEmailField(email)
}

export function validateResetPasswordInput(token: string, newPassword: string): AuthFieldErrors {
  const errors: AuthFieldErrors = {}
  if (!token.trim()) {
    errors.token = 'Reset token is required.'
  }

  if (!newPassword.trim()) {
    errors.newPassword = 'New password is required.'
  }

  return errors
}

export function validateVerifyEmailInput(token: string): AuthFieldErrors {
  if (!token.trim()) {
    return { token: 'Verification token is required.' }
  }

  return {}
}

function validateEmailField(email: string): AuthFieldErrors {
  const normalized = email.trim()
  if (!normalized) {
    return { email: 'Email is required.' }
  }

  if (!isLikelyEmail(normalized)) {
    return { email: 'Email format is invalid.' }
  }

  return {}
}

function validatePasswordField(password: string): AuthFieldErrors {
  if (!password.trim()) {
    return { password: 'Password is required.' }
  }

  return {}
}

function isLikelyEmail(value: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/u.test(value)
}

