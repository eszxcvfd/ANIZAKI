# Local Environment Contract

This document defines the canonical local environment contract for API and Web development.

## Reserved Local Ports

- API HTTP: `5080`
- API HTTPS: `7080`
- Web dev server: `5173`

## Canonical Environment Keys

### Backend (`src/api`)
- `ASPNETCORE_ENVIRONMENT=Development`
- `ASPNETCORE_URLS=http://localhost:5080`
- `ANIZAKI_CORS_ORIGINS=http://localhost:5173`
- `ConnectionStrings__MongoDb=<mongodb-connection-string>`

### Frontend (`src/web`)
- `VITE_API_BASE_URL=http://localhost:5080`
- `VITE_APP_ENV=development`

## Naming Rules

- Use `ANIZAKI_` prefix for repository-specific server-side settings.
- Use `VITE_` prefix for frontend-exposed values.
- Keep key names uppercase with underscores.
- Keep `.env.example` files committed; keep real `.env` files uncommitted.

## URL and Contract Policy

- Frontend must call API through `VITE_API_BASE_URL`.
- API must allow web origin from `ANIZAKI_CORS_ORIGINS`.
- Health-check endpoint is expected at `/health`.

## Dev Auth Header Contract (Local/Test)

When calling protected endpoints in local/test mode using the development bearer handler, include:

- `Authorization: Bearer <token>`
- `X-Anizaki-User-Id: <guid>`
- `X-Anizaki-User-Email: <email>`
- `X-Anizaki-User-Role: user|seller|admin`

## Artifacts

- `src/api/.env.example`
- `src/web/.env.example`
