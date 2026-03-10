import { useState } from 'react'
import { Card } from '../../../shared/ui/Card'
import { writeAuthSession, type AuthRole } from './authSession'

type SessionBootstrapCardProps = {
  nextPath: string
}

type FormState = {
  accessToken: string
  userId: string
  userEmail: string
  role: AuthRole
}

const INITIAL_FORM_STATE: FormState = {
  accessToken: '',
  userId: '',
  userEmail: '',
  role: 'user',
}

export function SessionBootstrapCard({ nextPath }: SessionBootstrapCardProps) {
  const [formState, setFormState] = useState<FormState>(INITIAL_FORM_STATE)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  function updateFormState<K extends keyof FormState>(field: K, value: FormState[K]) {
    setFormState((current) => ({
      ...current,
      [field]: value,
    }))
  }

  function handleContinue() {
    const accessToken = formState.accessToken.trim()
    const userId = formState.userId.trim()
    const userEmail = formState.userEmail.trim()

    if (!accessToken || !userId || !userEmail) {
      setErrorMessage('Access token, user ID, and user email are required.')
      return
    }

    writeAuthSession({
      accessToken,
      userId,
      userEmail,
      role: formState.role,
    })

    window.location.assign(nextPath)
  }

  const inputClassName =
    'w-full rounded border border-slate-300 px-3 py-2 text-slate-800 focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200'

  return (
    <Card title="Sign in context bootstrap">
      <p className="mb-3">
        Role-aware routes require an authenticated session context. This temporary
        bootstrap stores the local session used by route guards.
      </p>
      <div className="grid gap-3 md:grid-cols-2">
        <label className="grid gap-1 md:col-span-2">
          <span>Access token</span>
          <input
            className={inputClassName}
            value={formState.accessToken}
            onChange={(event) => updateFormState('accessToken', event.target.value)}
            placeholder="Paste bearer token"
            type="text"
            autoComplete="off"
          />
        </label>
        <label className="grid gap-1">
          <span>User ID</span>
          <input
            className={inputClassName}
            value={formState.userId}
            onChange={(event) => updateFormState('userId', event.target.value)}
            placeholder="User identifier"
            type="text"
            autoComplete="off"
          />
        </label>
        <label className="grid gap-1">
          <span>User email</span>
          <input
            className={inputClassName}
            value={formState.userEmail}
            onChange={(event) => updateFormState('userEmail', event.target.value)}
            placeholder="person@example.com"
            type="email"
            autoComplete="off"
          />
        </label>
        <label className="grid gap-1">
          <span>Role</span>
          <select
            className={inputClassName}
            value={formState.role}
            onChange={(event) => updateFormState('role', event.target.value as AuthRole)}
          >
            <option value="user">user</option>
            <option value="seller">seller</option>
            <option value="admin">admin</option>
          </select>
        </label>
      </div>
      <div className="mt-3">
        <button
          className="rounded border border-sky-700 bg-sky-700 px-3 py-2 text-white disabled:cursor-not-allowed disabled:opacity-60"
          onClick={handleContinue}
          type="button"
        >
          Continue
        </button>
      </div>
      {errorMessage ? <p className="mt-3 text-rose-700">{errorMessage}</p> : null}
    </Card>
  )
}
