# STS2 Superfast Mod

`Slay the Spire 2` 전투 템포를 빠르게 만드는 속도 모드입니다.

현재 기본값은 `baseSpeed = 3.0` 입니다.

- Spine 애니메이션 가속
- 전투 wait / timer 단축
- 전투 UI delta 가속
- 전투 VFX delta 가속

## Demo

![STS2 3x Demo](sts2_3x.gif)

## 한국어 안내

### 포함 파일

배포 폴더에는 아래 4개 파일이 들어 있습니다.

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `Sts2Speed.Core.dll`
- `Sts2Speed.config.json`

`INSTALL_KO.txt`는 짧은 설치 안내 문서입니다.

### 설치 방법

1. `Slay the Spire 2` 설치 폴더 아래에 `mods` 폴더를 만듭니다.
2. 배포 파일 4개를 `mods` 폴더에 복사합니다.
3. `Sts2Speed.config.json`을 열어서 원하는 속도로 수정합니다.
4. 게임을 실행합니다.

예시 경로:

```text
D:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods
```

### 설정 방법

이제 설정은 `Sts2Speed.config.json` 한 파일만 사용합니다.

기본 예시:

```json
{
  "enabled": true,
  "baseSpeed": 3.0,
  "spineSpeed": 1.0,
  "queueSpeed": 1.0,
  "effectSpeed": 1.0,
  "combatUiSpeed": 1.0,
  "combatVfxSpeed": 1.0,
  "combatOnly": true
}
```

의미:

- `baseSpeed`
  - 전체 기본 배속입니다.
- `spineSpeed`, `queueSpeed`, `effectSpeed`, `combatUiSpeed`, `combatVfxSpeed`
  - 항목별 계수입니다.
  - 실제 적용 속도는 `baseSpeed * <항목별 Speed>` 입니다.

예:

- `baseSpeed = 3.0`, `spineSpeed = 1.0` -> Spine는 3.0배속
- `baseSpeed = 3.0`, `queueSpeed = 0.8` -> queue wait는 2.4배속
- `baseSpeed = 3.0`, `combatVfxSpeed = 1.3` -> 전투 VFX는 3.9배속

중요:

- 이 JSON의 숫자는 전부 `클수록 더 빠름` 입니다.
- `queueSpeed`, `effectSpeed`도 내부적으로는 duration을 나누는 방식으로 처리해서, 사용자 입장에서는 그냥 숫자를 올리면 더 빨라집니다.
- 초보자는 `baseSpeed`만 바꾸고 나머지 `...Speed` 값은 `1.0` 그대로 두면 됩니다.

현재 추천 기본값:

- `baseSpeed = 3.0`
- 나머지 `...Speed = 1.0`

추천 시작값:

- 가볍게: `2.0`
- 기본 추천: `3.0`
- 빠르게: `3.5`
- 과격하게: `4.0`

### 현재 실제로 바꾸는 항목

이 모드는 전역 `time_scale`을 쓰지 않고, 아래 런타임 지점을 직접 패치합니다.

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`
- `NTargetingArrow._Process`
- `NIntent._Process`
- `NStarCounter._Process`
- `NEnergyCounter._Process`
- `NBezierTrail._Process`
- `NCardTrail._Process`
- `NDamageNumVfx._Process`
- `NHealNumVfx._Process`

### 진행 데이터가 초기화된 것처럼 보일 때

STS2는 모드를 읽으면 저장 경로를 `modded/profileN` 쪽으로 분리합니다.

그래서 바닐라 진행은 남아 있는데, 모드 프로필이 비어 있어서 처음처럼 보일 수 있습니다.

이 경우 아래 내용을:

- `AppData\Roaming\SlayTheSpire2\steam\<계정>\profile1`

여기로 복사하면 됩니다:

- `AppData\Roaming\SlayTheSpire2\steam\<계정>\modded\profile1`

이 저장소를 같이 사용 중이면 아래 명령으로 자동 복구할 수 있습니다.

```powershell
dotnet run --project src/Sts2Speed.Tool -- sync-modded-profile
```

### 주의 사항

- 게임 실행 중에는 `mods` 폴더 파일을 교체하지 않는 편이 안전합니다.
- 처음 모드 실행 시 경고 / 동의 창이 한 번 뜰 수 있습니다.
- 모든 연출이 완전히 같은 비율로 빨라지지는 않습니다. 일부 구간은 tween 기반이라 체감이 다를 수 있습니다.

## English Guide

This is a speed mod for `Slay the Spire 2` that makes combat flow faster.

The current default is `baseSpeed = 3.0`.

- Faster Spine animations
- Shorter combat waits / timers
- Faster combat UI delta-driven updates
- Faster combat VFX delta-driven updates

### Included Files

The release folder contains these 4 required files.

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `Sts2Speed.Core.dll`
- `Sts2Speed.config.json`

### Installation

1. Create a `mods` folder under your `Slay the Spire 2` install directory.
2. Copy the 4 files into that `mods` folder.
3. Edit `Sts2Speed.config.json`.
4. Launch the game.

Example path:

```text
D:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods
```

### Configuration

This mod now uses a single file: `Sts2Speed.config.json`.

Example:

```json
{
  "enabled": true,
  "baseSpeed": 3.0,
  "spineSpeed": 1.0,
  "queueSpeed": 1.0,
  "effectSpeed": 1.0,
  "combatUiSpeed": 1.0,
  "combatVfxSpeed": 1.0,
  "combatOnly": true
}
```

Meaning:

- `baseSpeed`
  - Global base speed.
- `spineSpeed`, `queueSpeed`, `effectSpeed`, `combatUiSpeed`, `combatVfxSpeed`
  - Per-group coefficients.
  - Effective speed is `baseSpeed * <groupSpeed>`.

Important:

- Every numeric value in this JSON means `higher = faster`.
- That includes `queueSpeed` and `effectSpeed`; internally the mod inverts duration math so the user-facing rule stays consistent.
- Beginners can change only `baseSpeed` and leave every other `...Speed` value at `1.0`.

### What This Mod Patches

This mod does not use a global time scale. It directly patches selected runtime points:

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`
- `NTargetingArrow._Process`
- `NIntent._Process`
- `NStarCounter._Process`
- `NEnergyCounter._Process`
- `NBezierTrail._Process`
- `NCardTrail._Process`
- `NDamageNumVfx._Process`
- `NHealNumVfx._Process`

### If Your Save Looks Reset

When any mod is loaded, STS2 separates progress into `modded/profileN`.

If your vanilla progress still exists but the modded profile looks empty, copy:

- `AppData\Roaming\SlayTheSpire2\steam\<account>\profile1`

to:

- `AppData\Roaming\SlayTheSpire2\steam\<account>\modded\profile1`

If you are using this repository locally, you can also run:

```powershell
dotnet run --project src/Sts2Speed.Tool -- sync-modded-profile
```

## Development Docs

Technical docs, investigation notes, and patching details live under:

- `docs/development/README.md`
