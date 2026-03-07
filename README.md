# STS2 Speed Toolkit

This workspace contains a non-invasive Slay the Spire 2 speed-mod toolkit.

- It does not touch the live Steam install.
- It does not mutate live save or settings files.
- It keeps first-play presentation settings intact by default.
- It prepares package artifacts, snapshot and restore reports, test launcher scripts, and a patch target catalog.

Development notes:

- [docs/development/README.md](C:/Users/jidon/source/repos/STS2ModeTest/docs/development/README.md)

## Projects

- `src/Sts2Speed.Core`
  - Shared configuration, environment override, patch target, and snapshot/restore planning logic.
- `src/Sts2Speed.ModSkeleton`
  - In-game mod entrypoint skeleton, manifest template, and dry-run package layout planner.
- `src/Sts2Speed.Tool`
  - Console tool for config inspection, package materialization, backup/restore, and mod path discovery.
- `src/Sts2Speed.SelfTest`
  - Lightweight self-test runner without external test packages.

## Commands

```powershell
dotnet run --project src/Sts2Speed.Tool -- show-config
dotnet run --project src/Sts2Speed.Tool -- dry-run-package
dotnet run --project src/Sts2Speed.Tool -- materialize-package
dotnet run --project src/Sts2Speed.Tool -- discover-mod-path
dotnet run --project src/Sts2Speed.Tool -- dry-run-snapshot
dotnet run --project src/Sts2Speed.Tool -- snapshot
dotnet run --project src/Sts2Speed.Tool -- dry-run-restore
dotnet run --project src/Sts2Speed.Tool -- restore --snapshot-root artifacts/snapshots/<timestamp>
dotnet run --project src/Sts2Speed.Tool -- verify-snapshot --snapshot-root artifacts/snapshots/<timestamp>
dotnet run --project src/Sts2Speed.Tool -- deploy-package --mod-root <confirmed mod folder>
dotnet run --project src/Sts2Speed.SelfTest
```

`materialize-package` writes a generic PowerShell launcher to `artifacts/package-layout/Sts2Speed/scripts/Start-Sts2SpeedTest.ps1`
and a profile catalog to `artifacts/package-layout/Sts2Speed/scripts/test-profiles.json`.
It also writes `mod.cfg`, `mod.gd`, and `GUMM_mod.gd` so the package can be inspected by GUMM-style loaders while the C# DLL route is still being validated.

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

## Notes

- The first runtime axis is `STS2_SPEED_SPINE_TIME_SCALE`. `animationScale` remains a mod-side placeholder until a concrete runtime hook is confirmed.
- `discover-mod-path` is intentionally conservative. If it cannot find an exact path from logs, it prints heuristic candidates and keeps deployment blocked until `--mod-root` is explicit.
