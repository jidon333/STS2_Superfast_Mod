# STS2 로드 체인

이 문서는 STS2가 현재 모드를 어떤 순서로 읽고, 어디서 Harmony 패치를 설치하는지 현재 기준으로 설명합니다.

## 1. 게임 시작

게임 초기화 중 `ModManager.Initialize()`가 실행됩니다.

여기서 STS2 내장 모드 로더가 동작합니다.

## 2. `mods` 폴더 스캔

로더는 게임 설치 경로 아래 `mods` 폴더를 확인합니다.

현재 패키지 기준 파일:

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `Sts2Speed.Core.dll`
- `Sts2Speed.config.json`

## 3. `.pck` basename과 `.dll` 매칭

STS2는 `.pck` basename과 같은 이름의 `.dll`을 찾습니다.

즉:

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`

이 둘의 basename이 맞아야 합니다.

## 4. `.pck` 내부 manifest 확인

로더는 `.pck`를 마운트한 뒤 `res://mod_manifest.json`을 읽습니다.

중요한 건 `pck_name`이 `.pck` basename과 일치해야 한다는 점입니다.

## 5. 메인 DLL 로드

manifest 검사가 끝나면 게임이 `sts2-speed-skeleton.dll`을 자기 프로세스 안에 로드합니다.

이 단계에서 별도 인젝터는 없습니다.

## 6. 추가 DLL resolve

메인 DLL은 `Sts2Speed.Core.dll`을 참조합니다.

게임이 이 참조 DLL을 자동으로 찾지 못할 수 있으므로, 모드 DLL 안에서 resolver를 등록합니다.

역할:

- 현재 모드 디렉토리 기준으로 추가 DLL 검색
- `Sts2Speed.Core.dll` 로드 보조

## 7. Harmony 패치 등록

현재 모드 DLL은 `ModInitializerAttribute` 대신 `Harmony.PatchAll` 경로를 탑니다.

즉 로드 체인은 대략 이렇습니다.

```text
게임 로더
  -> mods 폴더 스캔
  -> .pck 발견
  -> matching .dll 로드
  -> resolver 등록
  -> Harmony.PatchAll
  -> [HarmonyPatch] 클래스 스캔
  -> Prefix 패치 설치
```

## 8. 런타임 설정 로드

패치 로직은 실행 중 현재 모드 폴더의 설정을 읽습니다.

현재 우선순위:

1. `STS2_SPEED_*` 환경 변수
2. `Sts2Speed.config.json`
3. legacy fallback `Sts2Speed.speed.txt`
4. 기본값

현재 권장 설정 파일은:

- `Sts2Speed.config.json`

형태:

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

## 9. 실제 패치 적용 지점

현재 패치 범위:

- Spine
  - `MegaAnimationState.SetTimeScale`
  - `MegaTrackEntry.SetTimeScale`
- wait / timer
  - `Cmd.CustomScaledWait`
  - `CombatState.GodotTimerTask`
- combat UI delta
  - `NTargetingArrow._Process`
  - `NIntent._Process`
  - `NStarCounter._Process`
  - `NEnergyCounter._Process`
- combat VFX delta
  - `NBezierTrail._Process`
  - `NCardTrail._Process`
  - `NDamageNumVfx._Process`
  - `NHealNumVfx._Process`

사용자 의미는 항상 같습니다.

- 숫자가 클수록 빠름

내부 계산만 항목별로 다릅니다.

- 애니메이션 / delta -> `* effectiveSpeed`
- wait / delay -> `/ effectiveSpeed`

## 10. 저장 경로 분리

모드가 로드되면 STS2는 진행 데이터를 `modded/profileN` 쪽에 분리해 저장합니다.

그래서:

- vanilla 진행은 남아 있는데
- modded 프로필만 비어 있으면
- 게임이 초기화된 것처럼 보일 수 있습니다

이 경우 `profileN -> modded/profileN` 복구가 필요할 수 있습니다.

## 11. 로그 확인

게임 로그에서 확인할 신호:

- `.pck` 발견
- matching DLL 로드
- mod initialization 완료
- `--- RUNNING MODDED! ---`

모드 자체 런타임 로그:

- `mods\\sts2speed.runtime.log`

여기서 확인 가능한 것:

- 설정 로드
- 설정 refresh
- 패치 적용 1회 로그
