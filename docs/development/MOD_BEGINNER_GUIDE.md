# STS2 Speed Mod 초보자용 구조 안내

이 문서는 현재 저장소가 실제로 어떤 파일을 만들고, 각 파일이 무슨 역할을 하는지 빠르게 설명합니다.

## 1. 배포 파일

현재 배포 기준 파일은 4개입니다.

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `Sts2Speed.Core.dll`
- `Sts2Speed.config.json`

역할:

- `.pck`
  - STS2가 이 모드를 정식 모드로 인식하게 하는 Godot 패키지입니다.
  - 내부에 `mod_manifest.json`이 들어 있습니다.
- `sts2-speed-skeleton.dll`
  - 실제 Harmony 패치가 들어 있는 메인 payload 입니다.
- `Sts2Speed.Core.dll`
  - 설정 로더, 공용 계산, 백업/복구 관련 공용 코드입니다.
- `Sts2Speed.config.json`
  - 사용자가 직접 수정하는 속도 설정 파일입니다.

## 2. 설정 파일

현재 설정은 JSON 한 파일로 통일했습니다.

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

해석:

- `baseSpeed`
  - 전체 기본 배속
- `spineSpeed`, `queueSpeed`, `effectSpeed`, `combatUiSpeed`, `combatVfxSpeed`
  - 항목별 계수
- 실제 속도
  - `baseSpeed * 각 항목별 Speed`

중요:

- 이 JSON의 숫자는 모두 `클수록 빠름` 입니다.
- 초보자는 `baseSpeed`만 바꾸고 나머지는 `1.0` 그대로 두면 됩니다.

## 3. 실제 코드 위치

### 패치 본체

- `src/Sts2Speed.ModSkeleton/Runtime/SpeedPatches.cs`

여기에 Harmony 패치가 있습니다.

### 설정 로드 / 계산

- `src/Sts2Speed.Core/Configuration/RuntimeSettingsLoader.cs`
- `src/Sts2Speed.Core/Configuration/SpeedScaleMath.cs`
- `src/Sts2Speed.ModSkeleton/Runtime/RuntimePatchContext.cs`

여기서 JSON을 읽고, 각 그룹의 실제 속도를 계산하고, 패치에서 사용할 값으로 변환합니다.

### 패키지 생성

- `src/Sts2Speed.ModSkeleton/NativeModPackaging.cs`

이 파일이 배포 폴더에 `.dll`, `.pck`, `Sts2Speed.config.json`을 배치합니다.

## 4. 현재 패치되는 그룹

### Spine

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`

### Queue wait

- `Cmd.CustomScaledWait`

### Effect delay

- `CombatState.GodotTimerTask`

### Combat UI delta

- `NTargetingArrow._Process`
- `NIntent._Process`
- `NStarCounter._Process`
- `NEnergyCounter._Process`

### Combat VFX delta

- `NBezierTrail._Process`
- `NCardTrail._Process`
- `NDamageNumVfx._Process`
- `NHealNumVfx._Process`

## 5. 왜 전역 time scale을 안 쓰는가

전역 `Engine.TimeScale`은 범위가 너무 넓습니다.

그렇게 하면:

- 메뉴
- 오디오
- 비전투 UI
- 의도하지 않은 타이머

까지 한 번에 흔들릴 수 있습니다.

그래서 현재 모드는 전역 time scale 대신, 필요한 메서드 인자와 `delta`만 선택적으로 조정합니다.

## 6. 왜 진행 데이터가 초기화된 것처럼 보였는가

STS2는 모드가 로드되면 저장 경로를 `modded/profileN` 으로 분리합니다.

즉:

- 바닐라 진행: `profileN`
- 모드 진행: `modded/profileN`

이 구조 때문에 `modded/profileN` 이 비어 있으면 게임이 처음처럼 보일 수 있습니다.

복구는 아래 명령으로 자동화되어 있습니다.

```powershell
dotnet run --project src/Sts2Speed.Tool -- sync-modded-profile
```

## 7. 더 자세한 문서

- 개념부터 읽고 싶으면 `MODDING_FROM_ZERO.md`
- 현재 로드 순서가 궁금하면 `LOAD_CHAIN.md`
- 속도 해석 규칙이 궁금하면 `SPEED_SEMANTICS.md`
- 시행착오 전체는 `DETAILED_INVESTIGATION_LOG.md`
