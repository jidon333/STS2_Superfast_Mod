# Rollback Plan

The skeleton assumes that no files are copied into the live Steam install while the game is running.

## Before first real integration

Snapshot these files into a workspace-owned folder:

- `release_info.json`
- `settings.save`
- `settings.save.backup`
- `prefs.save`
- `prefs.save.backup`
- `progress.save`
- `progress.save.backup`
- `current_run.save`
- `current_run.save.backup`

Use the dry-run commands first, then create a real snapshot:

```powershell
dotnet run --project src/Sts2Speed.Tool -- dry-run-snapshot
dotnet run --project src/Sts2Speed.Tool -- dry-run-restore
dotnet run --project src/Sts2Speed.Tool -- snapshot
dotnet run --project src/Sts2Speed.Tool -- verify-snapshot --snapshot-root artifacts/snapshots/<timestamp>
```

## Failure criteria

- Boot failure
- Infinite loading
- Combat or reward soft lock
- Save schema corruption
- Save write failure

If one of these happens:

1. Disable the mod or remove the copied mod folder from the test install.
2. Restore the saved snapshot files with `dotnet run --project src/Sts2Speed.Tool -- restore --snapshot-root artifacts/snapshots/<timestamp>`.
3. Verify the Steam install if the copied test install is not being used.

## Out of scope

- Live binary patching
- Multiplayer synchronization patches
- Any automatic write to the user's active settings or save files

## Deployment reminder

- The current workspace assumes the native STS2 route: `<game dir>\mods`.
- Use `build-native-pck` first, then `deploy-native-package`.
- Do not copy files into the live install while the game is running.
