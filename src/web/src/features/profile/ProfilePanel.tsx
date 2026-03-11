import { useState } from 'react'
import {
  getMyProfile,
  updateMyProfile,
  type MyProfileResponse,
} from '../auth/api/authApi'
import { readAuthSession } from '../auth/session/authSession'
import {
  buildProfileRequestContext,
  toProfileErrorMessage,
  type ProfileAuthInput,
} from './model/profileContext'
import { Card } from '../../shared/ui/Card'

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

  function handleLoadProfile() {
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

    getMyProfile(context)
      .then((data) => {
        setProfile(data)
        setEditableEmail(data.email)
        setLoadState('loaded')
      })
      .catch((error) => {
        setProfile(null)
        setLoadState('error')
        setLoadMessage(toProfileErrorMessage(error, 'Failed to sync profile data.'))
      })
  }

  function handleUpdateProfile() {
    const context = buildProfileRequestContext(authInput)
    if (!context) {
      setUpdateState('error')
      setUpdateMessage('Access token is required to update profile.')
      return
    }

    setUpdateState('saving')
    setUpdateMessage(null)

    updateMyProfile(editableEmail, context)
      .then((data) => {
        setProfile(data)
        setEditableEmail(data.email)
        setUpdateState('success')
        setUpdateMessage('Profile updated successfully.')
      })
      .catch((error) => {
        setUpdateState('error')
        setUpdateMessage(toProfileErrorMessage(error, 'Failed to apply profile changes.'))
      })
  }

  function handleAuthInputChange(field: keyof ProfileAuthInput, value: string) {
    setAuthInput((prev) => ({
      ...prev,
      [field]: value,
    }))
  }

  return (
    <div className="flex flex-col gap-10">
      {/* Auth Info Loader */}
      <Card title="Session Control" className="max-w-xl">
        <p className="text-slate-600 text-sm mb-6 font-medium">Environment-aware profile bootstrapping.</p>
        <div className="flex flex-col gap-5">
          <div className="flex flex-col gap-2">
            <label className="text-xs font-black text-slate-500 uppercase tracking-widest pl-1">Target User ID</label>
            <input
              className="input-field"
              value={authInput.userId}
              onChange={(event) => handleAuthInputChange('userId', event.target.value)}
              placeholder="e.g. user-123"
              type="text"
              autoComplete="off"
            />
          </div>
          <div className="flex flex-col gap-2">
            <label className="text-xs font-black text-slate-500 uppercase tracking-widest pl-1">Target Email</label>
            <input
              className="input-field"
              value={authInput.userEmail}
              onChange={(event) => handleAuthInputChange('userEmail', event.target.value)}
              placeholder="e.g. user@anizaki.dev"
              type="email"
              autoComplete="off"
            />
          </div>
          <div className="flex flex-col gap-2">
            <label className="text-xs font-black text-slate-500 uppercase tracking-widest pl-1">Mock Role</label>
            <input
              className="input-field"
              value={authInput.userRole}
              onChange={(event) => handleAuthInputChange('userRole', event.target.value)}
              placeholder="user | seller | admin"
              type="text"
              autoComplete="off"
            />
          </div>
          <button
            className="btn-primary mt-2 py-3 shadow-lg shadow-indigo-500/20"
            onClick={() => handleLoadProfile()}
            type="button"
            disabled={isLoadingProfile}
          >
            {isLoadingProfile ? (
              <span className="flex items-center justify-center gap-2">
                <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none"/><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/></svg>
                Syncing Profile...
              </span>
            ) : 'Load Profile Data'}
          </button>
        </div>

        {loadMessage && (
          <div className="mt-4 p-4 rounded-xl bg-red-50 border border-red-100 text-red-600 text-sm font-semibold animate-in fade-in slide-in-from-top-2">
            {loadMessage}
          </div>
        )}
        {loadState === 'loaded' && (
          <div className="mt-4 p-4 rounded-xl bg-emerald-50 border border-emerald-100 text-emerald-600 text-sm font-semibold animate-in fade-in slide-in-from-top-2">
            Profile synchronized successfully.
          </div>
        )}
      </Card>

      {/* Profile Details */}
      {profile && (
        <div className="animate-in fade-in slide-in-from-bottom-8 duration-700">
          <Card title="User Persona" className="border-indigo-100 bg-indigo-50/30">
            <div className="flex flex-col sm:flex-row items-center gap-8 mb-10">
              <div className="relative">
                <div className="h-24 w-24 rounded-3xl bg-gradient-to-tr from-indigo-600 to-violet-600 flex items-center justify-center text-3xl font-black text-white shadow-2xl shadow-indigo-500/40 border-4 border-white">
                  {profile.email.charAt(0).toUpperCase()}
                </div>
                <div className="absolute -bottom-1 -right-1 h-6 w-6 rounded-lg bg-emerald-500 border-4 border-[#f8fafc] shadow-lg"></div>
              </div>
              <div className="text-center sm:text-left">
                <h3 className="text-2xl font-black text-slate-900 mb-1">{profile.email}</h3>
                <div className="flex items-center gap-3">
                  <span className="px-2 py-0.5 rounded-md bg-indigo-100 text-indigo-600 text-[10px] font-black uppercase tracking-wider border border-indigo-200">
                    {profile.role}
                  </span>
                  <span className={`px-2 py-0.5 rounded-md text-[10px] font-black uppercase tracking-wider border ${profile.emailVerified ? 'bg-emerald-100 text-emerald-600 border-emerald-200' : 'bg-amber-100 text-amber-600 border-amber-200'}`}>
                    {profile.emailVerified ? 'Verified Identity' : 'Pending Verification'}
                  </span>
                </div>
              </div>
            </div>
            
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3 mb-10">
              <div className="p-4 bg-white rounded-2xl border border-slate-100 shadow-sm group hover:border-indigo-200 transition-colors">
                <span className="block text-[10px] font-black text-slate-500 uppercase tracking-widest mb-2">Internal Identifier</span>
                <span className="text-sm font-mono text-slate-600 break-all font-bold">{profile.userId}</span>
              </div>
              <div className="p-4 bg-white rounded-2xl border border-slate-100 shadow-sm group hover:border-indigo-200 transition-colors">
                <span className="block text-[10px] font-black text-slate-500 uppercase tracking-widest mb-2">Registration Epoch</span>
                <span className="text-sm font-bold text-slate-600">{new Date(profile.createdAtUtc).toLocaleDateString()}</span>
              </div>
              <div className="p-4 bg-white rounded-2xl border border-slate-100 shadow-sm group hover:border-indigo-200 transition-colors">
                <span className="block text-[10px] font-black text-slate-500 uppercase tracking-widest mb-2">Account Authority</span>
                <span className="text-sm font-bold text-slate-600 capitalize">{profile.role} Access</span>
              </div>
            </div>

            <div className="flex flex-col gap-4 border-t border-slate-200 pt-8">
              <h3 className="text-lg font-black text-slate-900">Update Credentials</h3>
              <div className="flex flex-col sm:flex-row gap-4 max-w-2xl">
                <div className="flex-1">
                  <input
                    className="input-field h-full"
                    value={editableEmail}
                    onChange={(event) => setEditableEmail(event.target.value)}
                    placeholder="Update security email"
                    type="email"
                    autoComplete="off"
                  />
                </div>
                <button
                  className="btn-primary px-8 py-3 whitespace-nowrap shadow-lg shadow-indigo-500/20 font-black"
                  onClick={() => handleUpdateProfile()}
                  type="button"
                  disabled={isUpdatingProfile}
                >
                  {isUpdatingProfile ? 'Processing...' : 'Apply Changes'}
                </button>
              </div>
              {updateMessage && (
                <div className={`max-w-2xl mt-2 p-4 rounded-xl border text-sm font-semibold animate-in fade-in slide-in-from-top-2 ${updateState === 'success' ? 'bg-emerald-50 text-emerald-600 border-emerald-100' : 'bg-red-50 text-red-600 border-red-100'}`}>
                  {updateMessage}
                </div>
              )}
            </div>
          </Card>
        </div>
      )}
    </div>
  )
}
