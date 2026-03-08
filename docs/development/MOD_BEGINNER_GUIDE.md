# STS2 Speed Mod 구조 안내

이 문서는 현재 저장소가 실제로 어떤 파일을 만들고, 각 파일이 무엇을 하는지 빠르게 설명합니다.

## 1. 배포 파일

현재 배포 기준 파일은 4개입니다.

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `Sts2Speed.Core.dll`
- `Sts2Speed.config.json`

역할:

- `.pck`
  - STS2가 이 모드를 정식 모드로 인식하게 하는 Godot 패키지
  - 내부에 `mod_manifest.json` 포함
- `sts2-speed-skeleton.dll`
  - 실제 Harmony 패치와 UI 주입이 들어 있는 메인 payload
- `Sts2Speed.Core.dll`
  - 설정 로더, 공용 계산, 일부 공유 타입
- `Sts2Speed.config.json`
  - 사용자가 직접 수정하는 속도 설정 파일

## 2. 현재 설정 표면

현재 설정은 flat JSON 구조입니다.

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
- 실제 배속
  - `baseSpeed x 각 항목 Speed`

중요:

- 모든 숫자는 `클수록 빠름`
- 초보자는 `baseSpeed`만 바꾸고 나머지는 `1.0` 유지 권장

## 3. 설정 변경 방법

두 가지가 있습니다.

1. `mods\Sts2Speed.config.json` 직접 수정
2. 인게임 `설정 -> 모드 -> STS2 Speed Skeleton`에서 수정

둘 다 같은 설정 파일을 바꾸는 인터페이스입니다.

## 4. 실제 코드 위치

### 패치 본체

- `src\Sts2Speed.ModSkeleton\Runtime\SpeedPatches.cs`

여기에는 Harmony 패치가 있습니다.

### 인게임 설정 UI

- `src\Sts2Speed.ModSkeleton\Runtime\InGameConfigUi.cs`

여기에는 Modding Screen 우측 패널에 붙는 `+ / -` UI가 있습니다.

### 런타임 설정 로드 / 캐시 / 로그

- `src\Sts2Speed.ModSkeleton\Runtime\RuntimePatchContext.cs`
- `src\Sts2Speed.Core\Configuration\RuntimeSettingsLoader.cs`
- `src\Sts2Speed.Core\Configuration\SpeedScaleMath.cs`
- `src\Sts2Speed.Core\Configuration\WorkspaceConfiguration.cs`

### 패키지 생성 / 배포

- `src\Sts2Speed.ModSkeleton\NativeModPackaging.cs`
- `src\Sts2Speed.Tool\Program.cs`

## 5. 현재 패치 그룹

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

### Modding Screen UI

- `NModInfoContainer.Fill`

## 6. 왜 전역 time scale을 안 쓰는가

전역 `Engine.TimeScale`은 범위가 너무 넓습니다.

그렇게 하면:

- 메뉴
- 오디오 체감
- 비전투 UI
- 의도하지 않은 타이머

까지 한 번에 흔들릴 수 있습니다.

그래서 지금 모드는 전역 time scale 대신 선택적 메서드 인자 조정 방식을 씁니다.

## 7. 왜 세이브가 초기화된 것처럼 보이는가

STS2는 모드를 로드하면 저장 경로를 `modded/profileN`으로 분리합니다.

즉:

- 바닐라 저장: `profileN`
- 모드 저장: `modded/profileN`

그래서 `modded/profileN`이 비어 있으면 게임이 처음처럼 보입니다.

복구는 아래 명령으로 자동화돼 있습니다.

```powershell
dotnet run --project src/Sts2Speed.Tool -- sync-modded-profile
```

## 8. 어디를 더 읽으면 되는가

- 개념부터 읽고 싶으면 `MODDING_FROM_ZERO.md`
- 실제 로드 순서가 궁금하면 `LOAD_CHAIN.md`
- 속도 해석 규칙이 궁금하면 `SPEED_SEMANTICS.md`
- 최근 시행착오까지 보고 싶으면 `DETAILED_INVESTIGATION_LOG.md`
