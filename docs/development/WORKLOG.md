# STS2 Speed Mod Work Log

This file records the actual investigation and implementation sequence, including failed paths.

## 1. Initial design constraint

The project started from four non-negotiable constraints:

- do not touch the live Steam install while the game is running
- do not change first-play presentation options by default
- prefer animation/wait acceleration over global config edits
- avoid binary patching unless every safer route fails

That is why the repository started as a scaffold and not a direct patcher.

## 2. Community and public signal check

Before touching local files, we checked whether STS2 modding was active at all.

What we found:

- community discussion showed people were already trying to mod STS2 shortly after release
- public articles suggested Mega Crit expected easier modding than STS1
- Steam discussion suggested Workshop was not the whole story yet
- community sites like `sts2mods.com` already existed, which meant waiting for "official perfect documentation" was unnecessary

Result:

- continue with a mixed strategy:
  - inspect the local install for native mod support
  - keep an eye on community loader formats

## 3. Local install inspection

We inspected the actual game directory and user data root.

Key facts discovered:

- game install:
  - `D:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2`
- user data:
  - `C:\Users\jidon\AppData\Roaming\SlayTheSpire2`
- runtime files showed:
  - `SlayTheSpire2.exe`
  - `SlayTheSpire2.pck`
  - `data_sts2_windows_x86_64\sts2.dll`
  - `data_sts2_windows_x86_64\sts2.runtimeconfig.json`
  - `data_sts2_windows_x86_64\sts2.deps.json`

Conclusion:

- this is a Godot + .NET title, not the original Java-style STS1 modding environment

## 4. Save/config inspection

We checked save and settings files in the user data folder.

Important findings:

- `settings.save` exists
- `prefs.save` exists
- `progress.save` exists
- `current_run.save.backup` exists
- `current_run.save` itself was missing at the time of inspection

Why that mattered:

- it showed which files must be backed up before testing
- it also told us that "missing file" must be a normal snapshot status, not an automatic error

This directly led to the `missing` handling in [SnapshotExecution.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/SnapshotExecution.cs).

## 5. Runtime contract clues from the install

We checked `sts2.runtimeconfig.json` and `sts2.deps.json`.

Key findings:

- target framework is `.NET 9.0`
- dependency list includes `GodotSharp`
- dependency list includes `0Harmony`
- dependency list includes `Steamworks.NET`

Why this mattered:

- `.NET 9.0` explained later reflection issues in the local `.NET 7` dev environment
- `0Harmony` strongly suggested Harmony-style patching is part of the game's runtime universe
- `GodotSharp` confirmed managed code and Godot integration

## 6. First mod-support hypothesis

Earlier install inspection suggested internal types and names related to modding were present.

From local analysis we recorded expected contracts such as:

- `MegaCrit.Sts2.Core.Modding.ModManager`
- `MegaCrit.Sts2.Core.Modding.ModManifest`
- `MegaCrit.Sts2.Core.Modding.ModInitializerAttribute`
- `MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen.NModdingScreen`

We encoded those assumptions in [ExpectedGameContracts.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.ModSkeleton/Integration/ExpectedGameContracts.cs).

Important note:

- these strings are not the same thing as a working load path
- they are "contract hypotheses" used to guide the scaffold

## 7. Speed-setting hypothesis

We needed a way to describe speed behavior without altering live settings.

What we did:

- created mod-owned settings such as `spineTimeScale`, `queueWaitScale`, and `effectDelayScale`
- kept `fastModeOverride` optional and off by default

Why this was chosen:

- we wanted first-play visuals preserved
- environment-variable-driven testing is safer than rewriting save/config files

These settings were defined in [WorkspaceConfiguration.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Configuration/WorkspaceConfiguration.cs).

## 8. Hunting for runtime hook targets

We needed candidate methods for future patching.

How we approached it:

- used local binary/type investigation from the installed game
- collected plausible timing-related method names
- turned them into a catalog instead of writing a patch immediately

This produced [KnownPatchTargets.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/KnownPatchTargets.cs) with targets like:

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `CombatManager.WaitForActionThenEndTurn`
- `CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
- `CombatState.GodotTimerTask`
- `ActionExecutor.ExecuteActions`

Important limitation:

- these are candidate hook points, not yet validated live patches

## 9. Failed reflection attempt

We then tried to inspect `sts2.dll` more directly from PowerShell.

What we attempted:

- loading the assembly and listing types
- loading specific types such as `ModSource` and `ModManager`
- trying to use `MetadataLoadContext` / `PathAssemblyResolver`

What failed:

- direct type loading failed because the target assembly expected `.NET 9.0` core runtime types
- `MetadataLoadContext` was not available in the local shell environment we were using
- type inspection therefore could not be completed in the clean way we wanted

Why this mattered:

- it forced us to stop pretending we had perfect reflection coverage
- it also pushed us toward a more conservative implementation: catalog the expected contracts, but do not claim the loader is fully proven

This is one of the most important failures in the project so far.

## 10. Live mod path search failed

We tried to discover the actual mod scan path from the local machine.

What we checked:

- `godot.log`
- install directories
- user data directories
- likely `mods` and `workshop/content/2868840` locations

What happened:

- the log did not expose an exact mod path
- there was no obvious local `mods` directory created by the game yet

Why this mattered:

- automatic live deployment would have been reckless
- we implemented [ModPathDiscovery.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/ModPathDiscovery.cs) to:
  - parse exact paths from `godot.log` if available
  - otherwise provide heuristic candidates only
  - refuse to recommend a path when evidence is weak

This is why `discover-mod-path` currently warns instead of guessing.

## 11. Snapshot system became mandatory

Because live path discovery was still uncertain, backup quality became more important.

So we moved from a dry-run snapshot plan to real execution:

- `snapshot`
- `verify-snapshot`
- `restore`

Implementation details:

- copy important files into `artifacts/snapshots/<name>`
- record SHA-256 checksums
- track `missing` and `still-missing` states explicitly

This was implemented in [SnapshotExecution.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/SnapshotExecution.cs).

## 12. First real snapshot

We created a real snapshot at:

- [first-live-test](C:/Users/jidon/source/repos/STS2ModeTest/artifacts/snapshots/first-live-test)

Observed result:

- expected files were copied
- `current_run.save` was still absent
- verification passed with `allEntriesMatch=true`

That proved the rollback path is usable before any live deployment.

## 13. Package materialization

The original project only generated dry-run package plans.

That was not enough for real integration, so we added:

- real file emission into `artifacts/package-layout/Sts2Speed`
- DLL and PDB copying
- script generation
- package reports

This logic lives in [PackageMaterialization.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.ModSkeleton/PackageMaterialization.cs).

## 14. Test profile design

We wanted first integration to be gradual, not "full speed mode immediately".

So we added profiles:

- `vanilla`
- `spine125`
- `spine150`
- `spine175`
- `queue085`
- `queue070`
- `effect085`
- `effect070`

Why:

- this makes it possible to add one risk factor at a time
- if `spine125` fails, there is no need to test queue or effect delays yet

## 15. GUMM compatibility investigation

At this point we still did not know whether the internal C# mod route or the community GUMM route would be easier to validate first.

So we downloaded a real community STS2 sample mod from `sts2mods.com` and inspected the archive.

What we found:

- the sample had:
  - `mod.cfg`
  - `mod.gd`
  - `GUMM_mod.gd`
- `mod.cfg` used a simple INI-like `[Godot Mod]` format
- `mod.gd` extended `GUMM_mod.gd`

Why this mattered:

- our package previously had only `manifest.json` plus DLL placeholders
- that was not enough to resemble current community mods

This directly led to adding:

- generated `mod.cfg`
- generated `mod.gd`
- generated `GUMM_mod.gd`

to [PackageMaterialization.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.ModSkeleton/PackageMaterialization.cs).

## 16. Honest current runtime status

At this point the repository can:

- back up real files
- verify backups
- restore backups
- generate a deployable package tree
- copy that tree into a chosen mod root
- start the game with controlled environment variables
- expose a GUMM bootstrap that logs those variables

What it still cannot do:

- actually patch `MegaAnimationState.SetTimeScale`
- actually patch `CombatManager.WaitForActionThenEndTurn`
- actually accelerate action timing in-process

This is deliberate honesty, not missing documentation.

## 17. Self-test expansion

Once the real file operations existed, dry-run-only tests were not enough.

We expanded [Program.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.SelfTest/Program.cs) to cover:

- environment override precedence
- mutation policy
- snapshot planning
- snapshot copy and verification
- restore flow
- package materialization
- mod path discovery

That is why the self-test count increased and became more useful than a simple smoke check.

## 18. Why the implementation is intentionally conservative

The code is conservative because the facts are asymmetric:

- backup/restore behavior can be fully proven locally
- package emission can be fully proven locally
- mod path discovery can only be partially proven locally right now
- live runtime patch behavior cannot yet be fully proven

So the repository was shaped to maximize the parts we can prove, and to isolate the parts we still need to validate.

## 19. Practical lesson for future work

The next engineer should keep these lessons in mind:

1. Do not confuse scaffold settings with game-native settings.
2. Do not confuse contract strings with confirmed runtime contracts.
3. Do not deploy to a guessed mod path.
4. Do not add live timing patches before the loader path is confirmed.
5. Keep snapshot/restore working before every integration step.

## 20. Next unresolved item

The main unresolved item is still:

- confirm the exact live mod root on this machine

After that, the next implementation branch should be:

1. prove the package is discovered by the chosen loader
2. prove `mod.gd` or managed entry code executes
3. then wire the first real `spineTimeScale` runtime patch

That is the current state of the project, including the mistakes and dead ends that shaped it.
