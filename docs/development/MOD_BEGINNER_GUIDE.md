# STS2 Speed Mod 초보자용 구조 안내

이 문서는 "모딩 개념은 아직 낯설지만, 코드 구조는 빨리 파악하고 싶다"는 사람을 위한 빠른 안내다.

개념부터 길게 읽고 싶으면 먼저 `MODDING_FROM_ZERO.md`를 보는 편이 좋다. 이 문서는 그보다 더 실무적으로 "이 저장소에서 어느 파일이 무슨 역할을 하는가"에 집중한다.

## 1. 이 저장소가 실제로 하는 일

현재 이 저장소는 완성형 배포판 하나만 만드는 프로젝트가 아니다.

다음 다섯 가지를 동시에 담당한다.

1. 백업/복구
2. Godot `.pck` 생성
3. STS2 네이티브 모드 패키징
4. Harmony 기반 속도 패치
5. `profileN -> modded/profileN` 진행 복구

즉 목표는 "속도 모드 본체" 하나만 만드는 게 아니라, **안전하게 배포하고 되돌릴 수 있는 전체 작업 흐름**을 만드는 것이다.

## 2. 최종 방식은 무엇인가

현재 최종 구현 방향은 `STS2 내장 네이티브 로더`다.

즉 구조는 다음이다.

```text
mods 폴더
  -> .pck 발견
  -> 같은 basename의 .dll 발견
  -> mod_manifest.json 확인
  -> DLL 로드
  -> Harmony.PatchAll
  -> Prefix 패치 실행
```

GUMM은 초기에 로더 진입을 검증하기 위한 우회 경로였고, 지금은 fallback / diagnostics 용도로만 의미가 있다.

## 3. 실제 배포 파일은 무엇인가

현재 live `mods` 폴더 기준 핵심 파일은 다음이다.

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `Sts2Speed.Core.dll`
- `Sts2Speed.speed.txt`

각 역할:

- `.pck`
  - Godot 모드 패키지 단위
  - `mod_manifest.json` 포함
- `sts2-speed-skeleton.dll`
  - 실제 Harmony 패치 payload
- `Sts2Speed.Core.dll`
  - 설정 로더, 백업/복구 공용 로직 등 공통 코드
- `Sts2Speed.speed.txt`
  - 기본 배속 설정 파일

현재 기본값은 `2.0`이다.

## 4. 핵심 코드 위치

### 패키징 / 배포

- `src/Sts2Speed.ModSkeleton/NativeModPackaging.cs`

역할:

- 네이티브 `mods` 폴더용 파일 배치
- `mod_manifest.json` 생성
- `.pck` 생성용 구조 준비
- live `mods` 폴더 배포

### 런타임 설정 로더

- `src/Sts2Speed.Core/Configuration/RuntimeSettingsLoader.cs`

역할:

- `STS2_SPEED_*` 환경 변수 읽기
- `Sts2Speed.speed.txt` 읽기
- 공통 fallback 배속 결정
- 명시적 `enabled`가 없어도 배속 값이 `1.0`이 아니면 자동 활성화

### 추가 DLL resolve

- `src/Sts2Speed.ModSkeleton/Runtime/ModAssemblyResolver.cs`

역할:

- 모드 DLL이 자기 옆 폴더의 `Sts2Speed.Core.dll`을 찾게 함

### 패치 계산 / 로깅

- `src/Sts2Speed.ModSkeleton/Runtime/RuntimePatchContext.cs`
- `src/Sts2Speed.Core/Configuration/SpeedScaleMath.cs`

역할:

- 현재 설정 캐시
- 전투 중일 때만 적용하는 `combatOnly` 판단
- speed multiplier 계산
- `mods\sts2speed.runtime.log` 기록

### 실제 Harmony 패치

- `src/Sts2Speed.ModSkeleton/Runtime/SpeedPatches.cs`

현재 붙어 있는 패치:

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`

## 5. 지금 실제로 바꾸는 값은 무엇인가

현재는 "게임 설정 프로퍼티를 저장 파일에 써넣는 방식"이 아니라 **메서드 인자를 런타임에 바꾸는 방식**이다.

즉 지금 직접 바꾸는 값은 대체로 다음과 같다.

- `scale`
- `fastSeconds`
- `standardSeconds`
- `timeSec`

예를 들면:

```csharp
[HarmonyPrefix]
private static void Prefix(ref float scale)
{
    RuntimePatchContext.TryApplySpineScale(ref scale);
}
```

즉 원본 함수 전에 `scale` 값을 바꾸고, 원본 함수는 그 변경된 값을 받는다.

## 6. `2.0`은 현재 정확히 어떤 의미인가

초기 구현에는 버그가 있었다.

- Spine 애니메이션은 `2.0`에서 빨라졌지만
- wait / timer는 `2.0`에서 더 길어졌다

지금은 이를 고쳤다.

현재 의미:

- `2.0` -> 애니메이션은 2배속, wait / timer는 절반 길이
- `0.5` -> 애니메이션은 반속, wait / timer는 2배 길이

공식:

```text
animation = animation * multiplier
duration = duration / multiplier
```

자세한 설명은 `SPEED_SEMANTICS.md`에 있다.

## 7. 왜 진행이 초기화된 것처럼 보였는가

이건 실제로 겪었던 문제다.

모드가 하나라도 로드되면 STS2는 저장 경로를 `modded/profileN`으로 분리한다.

즉:

- 바닐라 저장은 `profileN`
- 모드 저장은 `modded/profileN`

그래서 `modded/profileN`이 비어 있으면 게임은 새 프로필처럼 보인다.

복구는 다음 명령으로 자동화했다.

```powershell
dotnet run --project src/Sts2Speed.Tool -- sync-modded-profile
```

이 명령은:

1. 기존 `modded/profileN` 백업
2. `profileN` 전체를 `modded/profileN`으로 복제
3. 보고서 JSON 생성

## 8. 지금까지 검증된 것

- `.pck`를 게임이 실제로 찾는다
- matching `.dll`을 실제로 로드한다
- `Harmony.PatchAll`이 실제로 호출된다
- 모드 켠 상태로 메인 메뉴까지 진입한다
- `Sts2Speed.speed.txt` 값이 런타임 설정으로 반영된다
- 진행 데이터를 `profileN -> modded/profileN`으로 복구할 수 있다
- speed semantics는 self-test로 고정했다

## 9. 아직 남은 것

- 실제 전투 플레이에서 체감 강도 확인
- `CombatManager.WaitForActionThenEndTurn`
- `CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
- `ActionExecutor.ExecuteActions`

이 후보 훅은 아직 미구현이거나 추가 검증이 필요하다.

즉 지금은 "네이티브 모드 로더 + 첫 payload + 복구/배포 루트"까지는 끝났고, 더 공격적인 SuperFastMode 재현은 다음 단계다.
