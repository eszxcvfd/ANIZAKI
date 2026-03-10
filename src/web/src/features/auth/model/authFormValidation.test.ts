import { describe, expect, it } from 'vitest'
import {
  validateForgotPasswordInput,
  validateLoginInput,
  validateRegisterInput,
  validateResetPasswordInput,
  validateVerifyEmailInput,
} from './authFormValidation'

describe('authFormValidation', () => {
  it('validates email and password for register/login', () => {
    expect(validateRegisterInput('', '')).toEqual({
      email: 'Email is required.',
      password: 'Password is required.',
    })
    expect(validateLoginInput('invalid', 'Password123!')).toEqual({
      email: 'Email format is invalid.',
    })
  })

  it('validates forgot-password email', () => {
    expect(validateForgotPasswordInput('')).toEqual({
      email: 'Email is required.',
    })
    expect(validateForgotPasswordInput('person@example.com')).toEqual({})
  })

  it('validates reset-password token and new password', () => {
    expect(validateResetPasswordInput('', '')).toEqual({
      token: 'Reset token is required.',
      newPassword: 'New password is required.',
    })
    expect(validateResetPasswordInput('token', 'password')).toEqual({})
  })

  it('validates verify-email token', () => {
    expect(validateVerifyEmailInput('')).toEqual({
      token: 'Verification token is required.',
    })
    expect(validateVerifyEmailInput('token')).toEqual({})
  })
})

