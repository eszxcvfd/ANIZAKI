# Workspace Inventory (2026-03-09)

## Scope and purpose
This document captures the factual current state of the repository before runtime scaffolding work begins.
It serves as execution evidence for bead `bd-172.1`.

## Toolchain availability (verified)
- .NET SDK: `8.0.417`
- Node.js: `v25.4.0`
- pnpm: `10.28.2`

## Repository-level findings
- `AGENTS.md` exists and defines OMX orchestration policy as top-level contract.
- No root `README.md` currently exists.
- `.omx/plans` currently contains 2 canonical planning files:
  - `prd-clean-architecture-dotnet8-react-tailwind.md`
  - `test-spec-clean-architecture-dotnet8-react-tailwind.md`

## Runtime source scaffolding status
- `src/` directory exists.
- `src/` currently has **no files** (no backend/frontend runtime scaffolds yet).
- No `.sln`, `.csproj`, `.ts`, `.tsx` runtime files are present yet.

## Current repository nature
At this timestamp, the workspace is primarily an orchestration/planning repository with:
- skill definitions under `.agents/skills`
- role prompts under `.codex/prompts`
- agent/runtime config under `.omx`
- task graph data under `.beads`

## Immediate implications for execution
1. Phase 1 documentation and convention tasks are prerequisites for deterministic scaffolding.
2. Backend and frontend runtime files must be created from scratch in subsequent beads.
3. Team runtime via `omx team` is currently blocked on missing `tmux` binary in this environment.

## Evidence commands executed
- `dotnet --version`
- `node -v`
- `pnpm.cmd --version`
- `Test-Path src`
- `tilth_files("src/**/*")`
- `tilth_files("README*")`
- `tilth_files(".omx/plans/*.md")`
