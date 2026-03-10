# Beads Workflow Guide

This repository uses a beads-driven execution flow for task selection and progress tracking.

## Core Loop

1. List actionable work:

```powershell
br ready --json
```

2. Optional prioritization assistance:

```powershell
bv --robot-triage
bv --robot-next
```

3. Claim selected bead:

```powershell
br update <id> --status in_progress --json
```

4. Execute implementation and verification commands.

5. Close with evidence-backed reason:

```powershell
br close <id> --reason "Implemented X and verified with Y" --json
```

6. Flush local tracker updates:

```powershell
br sync --flush-only
```

## Recommended Command Set

- Show details:

```powershell
br show <id> --json
```

- Verify current queue:

```powershell
br ready --json
```

- Recompute priority:

```powershell
bv --robot-next
```

## Working Conventions

- Use small, reversible changes per bead.
- Include concrete verification evidence in close reasons.
- Prefer unblocked beads from `br ready` when a suggested bead cannot be claimed.
- Re-run `br ready` after each close to avoid stale prioritization.
