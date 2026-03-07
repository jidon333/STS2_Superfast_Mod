# STS2 Speed Skeleton

This workspace contains a non-invasive Slay the Spire 2 speed-mod skeleton.

- It does not touch the live Steam install.
- It does not mutate live save or settings files.
- It keeps first-play presentation settings intact by default.
- It prepares a dry-run package plan, snapshot plan, restore plan, and patch target catalog.

## Projects

- `src/Sts2Speed.Core`
  - Shared configuration, environment override, patch target, and snapshot/restore planning logic.
- `src/Sts2Speed.ModSkeleton`
  - In-game mod entrypoint skeleton, manifest template, and dry-run package layout planner.
- `src/Sts2Speed.Tool`
  - Console tool for config inspection and dry-run planning.
- `src/Sts2Speed.SelfTest`
  - Lightweight self-test runner without external test packages.

## Commands

```powershell
dotnet run --project src/Sts2Speed.Tool -- show-config
dotnet run --project src/Sts2Speed.Tool -- dry-run-package
dotnet run --project src/Sts2Speed.Tool -- dry-run-snapshot
dotnet run --project src/Sts2Speed.Tool -- dry-run-restore
dotnet run --project src/Sts2Speed.SelfTest
```

## Defaults

- `enabled=false`
- `fastModeOverride=null`
- `animationScale=1.0`
- `spineTimeScale=1.0`
- `queueWaitScale=1.0`
- `effectDelayScale=1.0`
- `combatOnly=true`
- `preserveGameSettings=true`
- `verboseLogging=false`

Environment variable overrides:

- `STS2_SPEED_ENABLED`
- `STS2_SPEED_ANIMATION_SCALE`
- `STS2_SPEED_SPINE_TIME_SCALE`
- `STS2_SPEED_QUEUE_WAIT_SCALE`
- `STS2_SPEED_EFFECT_DELAY_SCALE`
- `STS2_SPEED_FAST_MODE_OVERRIDE`
