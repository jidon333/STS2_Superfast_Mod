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

#### 어떤 식으로 붙는가

이 UI는 게임이 원래 제공하는 공식 "모드 설정 API"를 쓰는 것이 아닙니다. 현재 구현은 Harmony `Postfix`로 기존 Modding Screen의 상세 패널에 새 컨트롤을 덧붙이는 방식입니다.

구체적인 주입 지점은 다음입니다.

- `MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen.NModInfoContainer.Fill(Mod mod)`

의미:

- 사용자가 Modding Screen에서 어떤 모드를 선택할 때마다
- 게임이 원래 우측 설명 패널을 채우는 함수가 호출되고
- 그 함수가 끝난 직후 `Postfix`가 실행됩니다
- 이 `Postfix`가 `InGameConfigUi.RefreshForSelection(__instance, mod)`를 호출합니다

즉 인게임 설정 UI는 "별도 화면"이 아니라 "모드 상세 패널에 나중에 덧붙는 UI"입니다.

#### `InGameConfigUi` 내부 흐름

핵심 진입점은 `RefreshForSelection(object infoContainer, object? mod)`입니다.

이 함수가 하는 일:

1. 현재 `infoContainer`에 대응하는 패널 상태를 `ConditionalWeakTable`에서 찾거나 새로 만듭니다
2. 아직 UI를 안 만들었다면 `EnsurePanelExists(...)`로 최초 1회 생성합니다
3. 현재 선택된 모드가 우리 모드인지 `IsTargetMod(mod)`로 검사합니다
4. 우리 모드가 아니면 패널을 숨기고 원래 설명 라벨을 다시 보여줍니다
5. 우리 모드면 `LoadEditableSettings()`로 `Sts2Speed.config.json`을 읽고
6. `UpdateLayout(...)`과 `UpdateTexts(...)`로 현재 UI를 갱신합니다

즉:

- 생성은 한 번
- 표시/숨김과 값 갱신은 선택이 바뀔 때마다

입니다.

#### 실제로 만드는 Control

`EnsurePanelExists(...)`가 reflection으로 Godot UI 객체를 직접 만듭니다.

만드는 타입:

- `Godot.VBoxContainer`
- `Godot.HBoxContainer`
- `Godot.Button`
- `Godot.Label`

패널 구성:

- `Enabled` 토글 버튼
- `Base speed` 조절 행
- `Spine speed` 조절 행
- `Queue speed` 조절 행
- `Effect speed` 조절 행
- `Combat UI speed` 조절 행
- `Combat VFX speed` 조절 행
- `Combat only` 토글 버튼

각 조절 행은 `CreateAdjustRow(...)`로 만듭니다.

한 행의 구성:

- 좌측 제목 `Label`
- `-` 버튼
- 현재 값 표시 버튼
- `+` 버튼

여기서 값 표시도 `Label`이 아니라 `Button`을 쓰고 있습니다. 초기 구현에서는 값 저장은 됐지만 숫자 표시가 즉시 갱신되지 않는 문제가 있었고, 이후 버튼 텍스트를 직접 다시 쓰는 식으로 정리했습니다.

#### 클릭하면 실제로 무슨 일이 일어나는가

예를 들어 `Base speed`의 `+`를 누르면:

1. `BindPressed(plusButton, ...)`에 연결된 콜백이 실행됩니다
2. `LoadEditableSettings()`가 현재 `Sts2Speed.config.json`을 읽습니다
3. 현재 값에 `0.25`를 더합니다
4. 최소/최대 범위 안으로 `ClampAndRound(...)` 합니다
5. `SaveEditableSettings(updated)`로 JSON 파일을 다시 씁니다
6. 중앙 값 텍스트를 즉시 다시 씁니다
7. `RefreshPanel(root)`가 전체 버튼/레이아웃을 다시 갱신합니다

그룹 속도들은 같은 구조이고 step만 `0.1`입니다.

토글 버튼도 비슷합니다.

- `Enabled`는 `settings.Enabled = !settings.Enabled`
- `Combat only`는 `settings.CombatOnly = !settings.CombatOnly`

로 바꾼 뒤 다시 저장합니다.

#### 왜 reflection으로 UI를 만드는가

이 모드 DLL은 게임이 가진 Godot 타입을 컴파일 타임에 직접 참조하지 않습니다. 대신 런타임에 다음처럼 타입을 찾습니다.

- `AccessTools.TypeByName("Godot.Button")`
- `Activator.CreateInstance(...)`
- `AccessTools.Property(...)`
- `MethodInfo.Invoke(...)`

이 방식을 택한 이유:

- 모드 프로젝트를 게임 내부 Godot 어셈블리에 강하게 묶지 않기 위해
- 런타임에 게임이 가진 타입을 그대로 재사용하기 위해
- UI 주입 실험을 빠르게 진행하기 위해

즉 `InGameConfigUi`는 "강한 정적 참조 기반 위젯 코드"보다는 "런타임 reflection 기반 주입 코드"에 가깝습니다.

#### 저장은 어떤 형식으로 하나

`SaveEditableSettings(...)`는 `SpeedModSettings` 전체를 그대로 직렬화하지 않고, 실제 사용자 편집 필드만 가진 익명 객체를 만들어 저장합니다.

저장되는 필드:

- `enabled`
- `baseSpeed`
- `combatOnly`
- `spineSpeed`
- `queueSpeed`
- `effectSpeed`
- `combatUiSpeed`
- `combatVfxSpeed`

이 구조를 따로 만든 이유는, 한때 계산용 `effective*` 필드까지 config에 써버리는 버그가 있었기 때문입니다. 현재는 `ToEditableDocument(...)`로 저장 대상을 명확히 제한합니다.

#### 왜 변경이 바로 적용되는가

UI 자체는 게임 내부 값을 직접 바꾸는 것이 아니라 `Sts2Speed.config.json`만 저장합니다.

그 다음은 `RuntimePatchContext`가 이어받습니다.

- 패치 코드가 `GetSettings()`를 부를 때
- 최근 500ms 안이면 캐시 재사용
- 500ms가 지났으면 config 파일의 write time 확인
- 저장 시간이 바뀌었으면 새 JSON 다시 읽음

즉 인게임 UI와 런타임 패치의 연결점은 "공유 메모리"가 아니라 "config 파일 + write time 감지"입니다.

#### 지금 구조의 장점과 한계

장점:

- 별도 설정 화면을 새로 만들지 않아도 됨
- 게임 재시작 없이 수치 조정 가능
- 외부 파일 수정과 인게임 수정을 같은 저장 경로로 통일 가능

한계:

- 원래 Modding Screen 레이아웃에 기대어 붙기 때문에 패치에 약함
- `NModInfoContainer.Fill` 시그니처나 내부 구조가 바뀌면 먼저 깨질 가능성이 큼
- 공식 모드 설정 API가 아니라서 UI 품질은 우리가 직접 맞춰야 함

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
