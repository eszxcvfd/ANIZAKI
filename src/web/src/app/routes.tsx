import type { ComponentType } from 'react'
import type { AuthRole, AuthSession } from '../features/auth/session/authSession'
import { AdminConsolePage } from '../pages/admin/AdminConsolePage'
import { ForbiddenPage } from '../pages/forbidden/ForbiddenPage'
import { HomePage } from '../pages/home/HomePage'
import { NotFoundPage } from '../pages/not-found/NotFoundPage'
import { ProfilePage } from '../pages/profile/ProfilePage'

type RouteAccess =
  | { kind: 'public' }
  | { kind: 'authenticated' }
  | { kind: 'roles'; allowedRoles: ReadonlyArray<AuthRole> }

type RouteConfig = {
  title: string
  component: ComponentType
  access: RouteAccess
}

import { RegisterPage } from '../pages/auth/RegisterPage'
import { ForgotPasswordPage } from '../pages/auth/ForgotPasswordPage'
import { ResetPasswordPage } from '../pages/auth/ResetPasswordPage'
import { VerifyEmailPage } from '../pages/auth/VerifyEmailPage'
import { LoginPage } from '../pages/auth/LoginPage'
import { DevBootstrapPage } from '../pages/dev/DevBootstrapPage'

const ROUTES: Record<string, RouteConfig> = {
  '/': {
    title: 'Home',
    component: HomePage,
    access: { kind: 'public' },
  },
  '/profile': {
    title: 'User Profile',
    component: ProfilePage,
    access: { kind: 'authenticated' },
  },
  '/auth/login': {
    title: 'Sign In',
    component: LoginPage,
    access: { kind: 'public' },
  },
  '/auth/register': {
    title: 'Create Account',
    component: RegisterPage,
    access: { kind: 'public' },
  },
  '/auth/forgot-password': {
    title: 'Recover Password',
    component: ForgotPasswordPage,
    access: { kind: 'public' },
  },
  '/auth/reset-password': {
    title: 'Reset Password',
    component: ResetPasswordPage,
    access: { kind: 'public' },
  },
  '/auth/verify-email': {
    title: 'Verify Email',
    component: VerifyEmailPage,
    access: { kind: 'public' },
  },
  '/admin/console': {
    title: 'Admin Console',
    component: AdminConsolePage,
    access: { kind: 'roles', allowedRoles: ['admin'] },
  },
  '/dev/bootstrap': {
    title: 'Dev Bootstrap',
    component: DevBootstrapPage,
    access: { kind: 'public' },
  },
}

const NOT_FOUND_ROUTE: RouteConfig = {
  title: 'Not Found',
  component: NotFoundPage,
  access: { kind: 'public' },
}

const LOGIN_REQUIRED_ROUTE: RouteConfig = {
  title: 'Authentication Required',
  component: LoginPage,
  access: { kind: 'public' },
}

const FORBIDDEN_ROUTE: RouteConfig = {
  title: 'Access Denied',
  component: ForbiddenPage,
  access: { kind: 'public' },
}

export function resolveRoute(path: string, session: AuthSession | null = null): RouteConfig {
  const route = ROUTES[path] ?? NOT_FOUND_ROUTE

  if (route.access.kind === 'public') {
    return route
  }

  if (!session) {
    return LOGIN_REQUIRED_ROUTE
  }

  if (route.access.kind === 'roles' && !route.access.allowedRoles.includes(session.role)) {
    return FORBIDDEN_ROUTE
  }

  return route
}
