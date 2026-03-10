# Repository Conventions

This document defines baseline repository conventions for the .NET + React monorepo scaffold.

## Line Endings and Formatting
- Default line ending is LF for all text files.
- Windows script files keep CRLF:
  - `*.bat`
  - `*.cmd`
  - `*.ps1`
- UTF-8 text encoding is the default.
- Final newline is required in text files.

## Ignore Rules
Root `.gitignore` excludes:
- frontend build artifacts and package caches
- backend build outputs (`bin/`, `obj/`, `TestResults/`)
- local environment files (`.env*`, except `.env.example`)
- machine-specific editor folders (`.vscode/`, `.idea/`)

Tracker-specific ignore behavior is managed in `.beads/.gitignore`.

## Naming and Structure

### Backend (`src/api`)
- Solution name: `Anizaki.Api.sln`
- Projects follow `Anizaki.<Layer>` naming:
  - `Anizaki.Domain`
  - `Anizaki.Application`
  - `Anizaki.Infrastructure`
  - `Anizaki.Api`
- Test projects follow `Anizaki.<Layer>.Tests` naming.

### Frontend (`src/web`)
- Use lowercase folder names for architecture slices:
  - `app`, `pages`, `features`, `entities`, `shared`
- Keep route composition in `pages`.
- Keep reusable primitives/utilities in `shared`.

## Change Discipline
- Prefer small, reversible changes.
- Keep architecture boundary rules explicit and testable.
- Update docs when conventions or commands change.

