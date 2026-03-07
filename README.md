# STS2 Superfast Mod

`Slay the Spire 2`의 전투 템포를 더 빠르게 만들어 주는 속도 모드입니다.

현재 기본 배속은 `2.0`입니다.

- Spine 애니메이션: 2배속
- 전투 wait / timer: 절반 길이

이 저장소 안에서 GitHub 배포용 최소 패키지는 `release/STS2_Superfast_Mod` 폴더에 들어 있습니다.

## 한국어 안내

### 포함 파일

배포용 폴더 안에는 아래 4개 파일이 들어 있습니다.

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `Sts2Speed.Core.dll`
- `Sts2Speed.speed.txt`

`INSTALL_KO.txt`는 간단한 설치 안내 문서입니다.

### 설치 방법

1. `Slay the Spire 2` 설치 폴더 아래에 `mods` 폴더를 만들어 주세요.
2. `release/STS2_Superfast_Mod` 안의 파일 4개를 `mods` 폴더에 복사해 주세요.
3. `Sts2Speed.speed.txt`를 메모장으로 열고 원하는 배속 숫자로 수정해 주세요.
4. 게임을 실행해 주세요.

예시 경로:

```text
D:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods
```

### 배속 설정

`Sts2Speed.speed.txt`에는 숫자 하나만 들어갑니다.

예시:

```txt
2.0
```

의미:

- `2.0` = 2배속
- `3.0` = 3배속
- `1.0` = 바닐라와 동일
- `0.5` = 반속

### 현재 실제로 적용되는 것

현재 모드는 아래 지점에 패치를 적용합니다.

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`

즉 전역 시간 배속이 아니라, 애니메이션과 일부 wait/timer 인자를 직접 조정하는 방식입니다.

### 진행 데이터가 초기화된 것처럼 보일 때

STS2는 모드가 로드되면 저장 경로를 `modded/profileN` 쪽으로 분리합니다.

그래서 바닐라 진행은 남아 있는데 모드 쪽 진행이 비어 있으면, 새 프로필처럼 보일 수 있습니다.

이 경우에는:

- `AppData\Roaming\SlayTheSpire2\steam\<계정>\profile1`
- 내용을
- `AppData\Roaming\SlayTheSpire2\steam\<계정>\modded\profile1`
- 으로 복사해 주세요

이 저장소를 로컬에서 같이 사용 중이시라면 아래 명령으로 자동 복구할 수 있습니다.

```powershell
dotnet run --project src/Sts2Speed.Tool -- sync-modded-profile
```

### 주의 사항

- 게임이 실행 중일 때는 `mods` 폴더 파일을 교체하지 않는 편이 안전합니다.
- 모드를 처음 켜면 게임이 모드 경고 또는 동의를 한 번 요구할 수 있습니다.
- 현재 버전은 애니메이션과 일부 wait/timer를 우선 가속하는 방식이라, 모든 연출이 완전히 같은 비율로 빨라지지는 않을 수 있습니다.

## English Guide

This is a speed mod for `Slay the Spire 2` that makes combat flow faster.

The current default multiplier is `2.0`.

- Spine animations: 2x speed
- Combat waits / timers: half duration

### Included Files

The distributable folder contains these 4 required files.

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `Sts2Speed.Core.dll`
- `Sts2Speed.speed.txt`

`INSTALL_KO.txt` is a short Korean install note.

### Installation

1. Create a `mods` folder under your `Slay the Spire 2` install directory.
2. Copy the 4 files from `release/STS2_Superfast_Mod` into that `mods` folder.
3. Open `Sts2Speed.speed.txt` and change the number to the speed you want.
4. Launch the game.

Example path:

```text
D:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods
```

### Speed Setting

`Sts2Speed.speed.txt` should contain a single number.

Example:

```txt
2.0
```

Meaning:

- `2.0` = 2x speed
- `3.0` = 3x speed
- `1.0` = vanilla speed
- `0.5` = half speed

### What This Mod Currently Changes

The current version patches these runtime points.

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`

So this mod does not use a global time scale. Instead, it directly adjusts animation speed and selected wait/timer arguments.

### If Your Save Looks Reset

STS2 separates modded progress into `modded/profileN` when any mod is loaded.

Because of that, your vanilla progress may still exist while the modded profile looks empty.

If that happens, copy:

- `AppData\Roaming\SlayTheSpire2\steam\<account>\profile1`

to:

- `AppData\Roaming\SlayTheSpire2\steam\<account>\modded\profile1`

If you are also using this repository locally, you can automate that recovery with:

```powershell
dotnet run --project src/Sts2Speed.Tool -- sync-modded-profile
```

### Notes

- It is safer not to replace files in `mods` while the game is running.
- The first modded launch may show a warning / consent prompt.
- This version mainly accelerates animations and selected combat waits/timers, so not every effect will speed up by exactly the same ratio.

## Development Docs

If you want the technical details, including Harmony patching, `.pck` generation, investigation notes, and remaining hook risks, see:

- `docs/development/README.md`
