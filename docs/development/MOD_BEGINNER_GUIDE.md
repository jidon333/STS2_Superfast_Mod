# STS2 Speed Mod Beginner Guide

## What This Project Is

This repository is not yet a finished "speed up the whole game" mod.

What it does today:

- It loads local config plus `STS2_SPEED_*` environment variables.
- It creates a safe package under `artifacts/package-layout/Sts2Speed`.
- It snapshots important Steam install and save files before live integration.
- It can restore those snapshots later.
- It can generate a test launcher that starts the game with selected speed-related environment variables.
- It can prepare both a C#-oriented package layout and a GUMM-compatible `mod.cfg` / `mod.gd` layout.

What it does not do yet:

- It does not patch the live game at runtime.
- It does not currently change combat speed by itself inside Slay the Spire 2.
- It does not mutate `settings.save`, `prefs.save`, or any game binary.

That distinction matters. The current codebase is a safe integration toolkit plus a mod scaffold, not a completed runtime patch.

## What "Working" Means Right Now

At the moment, "working" means these parts are real and verified:

1. Config is loaded and environment overrides are normalized.
2. Backup and restore are real file operations, not dry-run only.
3. Package materialization writes real files to `artifacts/`.
4. Deployment logic can copy the package into a chosen mod root.
5. Test profiles can launch the game with `STS2_SPEED_*` variables.
6. A GUMM bootstrap script can at least prove that the environment variables reach the mod loader side.

The actual "speed up animation/action timing" hook is still represented as planned patch targets.

## Repository Structure

### 1. Shared configuration and planning

Main file: [WorkspaceConfiguration.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Configuration/WorkspaceConfiguration.cs)

This file defines:

- `GamePathOptions`
  - Where the Steam install, user data, Steam account ID, and artifacts root live.
- `SpeedModSettings`
  - The mod's own control surface:
    - `enabled`
    - `fastModeOverride`
    - `animationScale`
    - `spineTimeScale`
    - `queueWaitScale`
    - `effectDelayScale`
    - `combatOnly`
    - `preserveGameSettings`
    - `verboseLogging`

Important point:

- These are mod settings, not guaranteed game-native properties.
- `spineTimeScale`, `queueWaitScale`, and `effectDelayScale` are names we chose for the scaffold because they map cleanly to the patch targets we want later.

### 2. Environment variable override layer

Main file: [EnvironmentOverrides.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Configuration/EnvironmentOverrides.cs)

This file makes the launcher-driven workflow possible.

Supported variables:

- `STS2_SPEED_ENABLED`
- `STS2_SPEED_ANIMATION_SCALE`
- `STS2_SPEED_SPINE_TIME_SCALE`
- `STS2_SPEED_QUEUE_WAIT_SCALE`
- `STS2_SPEED_EFFECT_DELAY_SCALE`
- `STS2_SPEED_FAST_MODE_OVERRIDE`

Why this matters:

- We wanted first-play presentation settings to stay untouched.
- So instead of editing live save/config files, we move the first test path to environment variables.
- That lets us test combinations like `spine125` and `queue085` without touching the user's permanent settings.

### 3. Configuration loading

Main file: [SettingsLoader.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Configuration/SettingsLoader.cs)

Load order is:

1. hardcoded defaults
2. local JSON config file
3. environment overrides

That is why the PowerShell launcher can temporarily change behavior without rewriting the tracked sample config.

### 4. Planned runtime targets

Main file: [KnownPatchTargets.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/KnownPatchTargets.cs)

This file is the "map" of where a future runtime patch should hook:

- `MegaCrit.Sts2.Core.Bindings.MegaSpine.MegaAnimationState.SetTimeScale`
- `MegaCrit.Sts2.Core.Bindings.MegaSpine.MegaTrackEntry.SetTimeScale`
- `MegaCrit.Sts2.Core.Combat.CombatManager.WaitForActionThenEndTurn`
- `MegaCrit.Sts2.Core.Combat.CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
- `MegaCrit.Sts2.Core.Combat.CombatState.GodotTimerTask`
- `MegaCrit.Sts2.Core.GameActions.ActionExecutor.ExecuteActions`

Important point:

- This is a target catalog, not active hooking code.
- The mod does not currently patch these methods.
- We explicitly exclude multiplayer-related classes here to keep the scope single-player only.

### 5. Backup and rollback

Main files:

- [SnapshotPlanning.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/SnapshotPlanning.cs)
- [SnapshotExecution.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/SnapshotExecution.cs)

This is one of the most important real pieces today.

What it backs up:

- `release_info.json`
- `settings.save`
- `settings.save.backup`
- `prefs.save`
- `prefs.save.backup`
- `progress.save`
- `progress.save.backup`
- `current_run.save`
- `current_run.save.backup`

How it works:

- `SnapshotPlanner` decides which files should be considered.
- `SnapshotExecutor.ExecuteSnapshot(...)` actually copies them.
- Every copied file gets a SHA-256 hash.
- `VerifySnapshot(...)` compares current files against the saved hash set.
- `ExecuteRestore(...)` copies backup files back into place.

Why this matters:

- This is what makes live-install testing acceptable.
- Without this layer, every experiment would risk save corruption with no clean rollback.

### 6. Package creation

Main files:

- [SpeedModDescriptor.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.ModSkeleton/SpeedModDescriptor.cs)
- [PackageMaterialization.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.ModSkeleton/PackageMaterialization.cs)

This is the current packaging flow:

1. Create a descriptor and package metadata.
2. Write `manifest.json`.
3. Write a GUMM-compatible `mod.cfg`.
4. Write `README.txt`.
5. Write the sample config.
6. Write test profiles.
7. Write a PowerShell launcher for those profiles.
8. Copy built DLLs and PDBs into `bin/`.
9. Write GUMM bootstrap scripts.

Why both C# and GUMM files exist:

- The local game install strongly suggests internal C# mod support.
- The public STS2 community is also using GUMM-style Godot mods today.
- We do not yet know which route will be the first stable one for this machine.
- So the package keeps both directions available while we validate the live load path.

### 7. CLI entrypoint

Main file: [Program.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Tool/Program.cs)

This is the operator console for the repository.

Key commands:

- `show-config`
- `dry-run-package`
- `materialize-package`
- `discover-mod-path`
- `dry-run-snapshot`
- `snapshot`
- `dry-run-restore`
- `restore`
- `verify-snapshot`
- `deploy-package`

This file is important because it wires all the planning and safety subsystems into a repeatable workflow.

### 8. Live test launcher

Generated file:

- [Start-Sts2SpeedTest.ps1](C:/Users/jidon/source/repos/STS2ModeTest/artifacts/package-layout/Sts2Speed/scripts/Start-Sts2SpeedTest.ps1)

What it does:

- Picks a named profile such as `spine125`, `queue085`, or `effect085`.
- Converts numbers using invariant culture.
- Sets only the allowed environment variables.
- Clears unused variables to avoid stale state.
- Starts `SlayTheSpire2.exe`.

Why this matters:

- It gives us reproducible test runs.
- It keeps test configuration outside the live save/config files.

## What Code Actually Makes the Current Toolkit "Work"

If you only read a few files, read these:

1. [SnapshotExecution.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/SnapshotExecution.cs)
   - This is the real backup/restore engine.
2. [PackageMaterialization.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.ModSkeleton/PackageMaterialization.cs)
   - This is the real package builder and test launcher generator.
3. [Program.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Tool/Program.cs)
   - This is the executable workflow.
4. [KnownPatchTargets.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/KnownPatchTargets.cs)
   - This is the future runtime hook map.

## Why We Focused on `spineTimeScale` First

We originally had both `animationScale` and `spineTimeScale` in the scaffold.

The important distinction:

- `animationScale` is a broad mod-side idea.
- `spineTimeScale` is the one tied to specific runtime hook candidates we actually identified:
  - `MegaAnimationState.SetTimeScale`
  - `MegaTrackEntry.SetTimeScale`

That is why the first recommended test profiles focus on `STS2_SPEED_SPINE_TIME_SCALE`.

## What the GUMM Bootstrap Does Today

Generated file:

- [mod.gd](C:/Users/jidon/source/repos/STS2ModeTest/artifacts/package-layout/Sts2Speed/mod.gd)

Right now it does not speed up the game.

It only:

- reads `STS2_SPEED_ENABLED`
- reads `STS2_SPEED_SPINE_TIME_SCALE`
- reads `STS2_SPEED_QUEUE_WAIT_SCALE`
- reads `STS2_SPEED_EFFECT_DELAY_SCALE`
- prints them

Why keep it anyway:

- It proves the mod can be discovered and initialized by a GUMM-style loader.
- That is a safer first milestone than trying to patch timing immediately.

## What Still Needs To Be Built

The missing runtime piece is a real patch or loader bridge that:

1. loads inside the game process
2. resolves the actual game types
3. applies timing changes to the target methods
4. keeps single-player only behavior
5. preserves save integrity

In other words:

- the toolkit side is real
- the patch target catalog is real
- the final in-process speed behavior is not implemented yet

## Safe Way To Use This Repository

Recommended order:

1. Run `show-config`
2. Run `snapshot`
3. Run `verify-snapshot`
4. Run `materialize-package`
5. Confirm the actual mod root in-game
6. Run `deploy-package --mod-root <confirmed path>`
7. Start with the `vanilla` or `spine125` launcher profile
8. If anything looks wrong, use `restore`

## Current Limitations

- The exact live mod scan path is still not confirmed from local logs.
- The internal C# mod contract is still inferred, not fully proven.
- The GUMM route is community-observed, but not yet validated end-to-end on this machine.
- No live game speed patch has been wired yet.
- `fastModeOverride` remains intentionally unused in first integration tests.

That is the honest state of the project today.
