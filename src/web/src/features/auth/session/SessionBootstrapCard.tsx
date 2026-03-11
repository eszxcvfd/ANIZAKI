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

  return (
    <Card title="Sign in context bootstrap">
      <p className="text-slate-600 text-sm mb-6 font-medium">
        Role-aware routes require an authenticated session context. This temporary
        bootstrap stores the local session used by route guards.
      </p>
      <div className="grid gap-5 md:grid-cols-2">
        <div className="flex flex-col gap-2 md:col-span-2">
          <label htmlFor="bootstrap-token" className="text-xs font-black text-slate-500 uppercase tracking-widest pl-1">Access token</label>
          <input
            id="bootstrap-token"
            className="input-field"
            value={formState.accessToken}
            onChange={(event) => updateFormState('accessToken', event.target.value)}
            placeholder="Paste bearer token"
            type="text"
            autoComplete="off"
          />
        </div>
        <div className="flex flex-col gap-2">
          <label htmlFor="bootstrap-userid" className="text-xs font-black text-slate-500 uppercase tracking-widest pl-1">User ID</label>
          <input
            id="bootstrap-userid"
            className="input-field"
            value={formState.userId}
            onChange={(event) => updateFormState('userId', event.target.value)}
            placeholder="User identifier"
            type="text"
            autoComplete="off"
          />
        </div>
        <div className="flex flex-col gap-2">
          <label htmlFor="bootstrap-email" className="text-xs font-black text-slate-500 uppercase tracking-widest pl-1">User email</label>
          <input
            id="bootstrap-email"
            className="input-field"
            value={formState.userEmail}
            onChange={(event) => updateFormState('userEmail', event.target.value)}
            placeholder="person@example.com"
            type="email"
            autoComplete="off"
          />
        </div>
        <div className="flex flex-col gap-2">
          <label htmlFor="bootstrap-role" className="text-xs font-black text-slate-500 uppercase tracking-widest pl-1">Role</label>
          <select
            id="bootstrap-role"
            className="input-field block w-full"
            value={formState.role}
            onChange={(event) => updateFormState('role', event.target.value as AuthRole)}
            title="Select user role"
          >
            <option value="user">user</option>
            <option value="seller">seller</option>
            <option value="admin">admin</option>
          </select>
        </div>
      </div>
      <div className="mt-8">
        <button
          className="btn-primary w-full py-3 shadow-lg shadow-indigo-500/20 font-black"
          onClick={handleContinue}
          type="button"
        >
          Initialize Context
        </button>
      </div>
      {errorMessage ? (
        <div className="mt-4 p-4 rounded-xl bg-red-50 border border-red-100 text-red-600 text-sm font-semibold animate-in fade-in slide-in-from-top-2">
          {errorMessage}
        </div>
      ) : null}
    </Card>
  )
}
