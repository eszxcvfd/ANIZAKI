import { useState } from 'react'
import {
  getMyProfile,
  updateMyProfile,
  type MyProfileResponse,
} from '../auth/api/authApi'
import { Card } from '../../shared/ui/Card'
import { readAuthSession } from '../auth/session/authSession'
import {
  buildProfileRequestContext,
  toProfileErrorMessage,
  type ProfileAuthInput,
} from './model/profileContext'

type LoadState = 'idle' | 'loading' | 'loaded' | 'error'
type UpdateState = 'idle' | 'saving' | 'success' | 'error'

const INITIAL_AUTH_INPUT: ProfileAuthInput = {
  accessToken: '',
  userId: '',
  userEmail: '',
  userRole: 'user',
}

export function ProfilePanel() {
  const [authInput, setAuthInput] = useState<ProfileAuthInput>(() => {
    const session = readAuthSession()
    if (!session) {
      return INITIAL_AUTH_INPUT
    }

    return {
      accessToken: session.accessToken,
      userId: session.userId,
      userEmail: session.userEmail,
      userRole: session.role,
    }
  })
  const [profile, setProfile] = useState<MyProfileResponse | null>(null)
  const [editableEmail, setEditableEmail] = useState('')
  const [loadState, setLoadState] = useState<LoadState>('idle')
  const [loadMessage, setLoadMessage] = useState<string | null>(null)
  const [updateState, setUpdateState] = useState<UpdateState>('idle')
  const [updateMessage, setUpdateMessage] = useState<string | null>(null)

  const isLoadingProfile = loadState === 'loading'
  const isUpdatingProfile = updateState === 'saving'
  const inputClassName =
    'w-full rounded border border-slate-300 px-3 py-2 text-slate-800 focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200'

  async function handleLoadProfile() {
    const context = buildProfileRequestContext(authInput)
    if (!context) {
      setLoadState('error')
      setLoadMessage('Access token is required to load profile.')
      return
    }

    setLoadState('loading')
    setLoadMessage(null)
    setUpdateState('idle')
    setUpdateMessage(null)

    try {
      const loadedProfile = await getMyProfile(context)
      setProfile(loadedProfile)
      setEditableEmail(loadedProfile.email)
      setLoadState('loaded')
    } catch (error) {
      setProfile(null)
      setLoadState('error')
      setLoadMessage(toProfileErrorMessage(error, 'Unable to load profile.'))
    }
  }

  async function handleUpdateProfile() {
    const context = buildProfileRequestContext(authInput)
    if (!context) {
      setUpdateState('error')
      setUpdateMessage('Access token is required to update profile.')
      return
    }

    const normalizedEmail = editableEmail.trim()
    if (!normalizedEmail) {
      setUpdateState('error')
      setUpdateMessage('Email is required.')
      return
    }

    setUpdateState('saving')
    setUpdateMessage(null)

    try {
      const updatedProfile = await updateMyProfile(normalizedEmail, context)
      setProfile(updatedProfile)
      setEditableEmail(updatedProfile.email)
      setUpdateState('success')
      setUpdateMessage('Profile updated successfully.')
    } catch (error) {
      setUpdateState('error')
      setUpdateMessage(toProfileErrorMessage(error, 'Unable to update profile.'))
    }
  }

  function handleAuthInputChange(field: keyof ProfileAuthInput, value: string) {
    setAuthInput((current) => ({
      ...current,
      [field]: value,
    }))
  }

  return (
    <>
      <Card title="Authenticated request context">
        <p className="mb-3">
          Provide development auth headers, then load your current profile from
          <code> /api/v1/users/me</code>.
        </p>
        <div className="grid gap-3 md:grid-cols-2">
          <label className="grid gap-1 md:col-span-2">
            <span>Access token</span>
            <input
              className={inputClassName}
              value={authInput.accessToken}
              onChange={(event) => handleAuthInputChange('accessToken', event.target.value)}
              placeholder="Paste bearer access token"
              type="text"
              autoComplete="off"
            />
          </label>
          <label className="grid gap-1">
            <span>User ID header (optional)</span>
            <input
              className={inputClassName}
              value={authInput.userId}
              onChange={(event) => handleAuthInputChange('userId', event.target.value)}
              placeholder="X-Anizaki-User-Id"
              type="text"
              autoComplete="off"
            />
          </label>
          <label className="grid gap-1">
            <span>User email header (optional)</span>
            <input
              className={inputClassName}
              value={authInput.userEmail}
              onChange={(event) => handleAuthInputChange('userEmail', event.target.value)}
              placeholder="X-Anizaki-User-Email"
              type="email"
              autoComplete="off"
            />
          </label>
          <label className="grid gap-1">
            <span>User role header (optional)</span>
            <input
              className={inputClassName}
              value={authInput.userRole}
              onChange={(event) => handleAuthInputChange('userRole', event.target.value)}
              placeholder="user | seller | admin"
              type="text"
              autoComplete="off"
            />
          </label>
          <div className="self-end">
            <button
              className="rounded border border-sky-700 bg-sky-700 px-3 py-2 text-white disabled:cursor-not-allowed disabled:opacity-60"
              onClick={() => void handleLoadProfile()}
              type="button"
              disabled={isLoadingProfile}
            >
              {isLoadingProfile ? 'Loading profile...' : 'Load profile'}
            </button>
          </div>
        </div>

        {loadMessage ? <p className="mt-3 text-rose-700">{loadMessage}</p> : null}
        {loadState === 'loaded' ? (
          <p className="mt-3 text-emerald-700">Profile loaded successfully.</p>
        ) : null}
      </Card>

      {profile ? (
        <Card title="Profile details">
          <ul>
            <li>User ID: {profile.userId}</li>
            <li>Role: {profile.role}</li>
            <li>Email verified: {profile.emailVerified ? 'yes' : 'no'}</li>
            <li>Created: {profile.createdAtUtc}</li>
            <li>Last updated: {profile.updatedAtUtc}</li>
          </ul>

          <div className="mt-4 grid gap-3 md:max-w-xl">
            <label className="grid gap-1">
              <span>Email</span>
              <input
                className={inputClassName}
                value={editableEmail}
                onChange={(event) => setEditableEmail(event.target.value)}
                placeholder="Enter updated email"
                type="email"
                autoComplete="off"
              />
            </label>
            <div>
              <button
                className="rounded border border-emerald-700 bg-emerald-700 px-3 py-2 text-white disabled:cursor-not-allowed disabled:opacity-60"
                onClick={() => void handleUpdateProfile()}
                type="button"
                disabled={isUpdatingProfile}
              >
                {isUpdatingProfile ? 'Saving...' : 'Update profile'}
              </button>
            </div>
            {updateMessage ? (
              <p className={updateState === 'success' ? 'text-emerald-700' : 'text-rose-700'}>
                {updateMessage}
              </p>
            ) : null}
          </div>
        </Card>
      ) : null}
    </>
  )
}
