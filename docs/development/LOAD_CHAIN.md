# STS2 네이티브 로드 체인

이 문서는 현재 기준으로 실제 게임이 우리 모드를 어떤 순서로 읽는지 정리한다.

## 1. 게임 시작

게임이 `OneTimeInitialization.ExecuteEssential()` 안에서 `ModManager.Initialize()` 를 호출한다.

여기서 네이티브 로더가 시작된다.

## 2. `mods` 폴더 스캔

`ModManager`는 게임 설치 폴더 기준 `mods` 디렉토리를 연다.

그 다음 재귀적으로 `.pck` 파일을 찾는다.

현재 우리 패키지 이름은 다음과 같다.

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`

## 3. `.pck`와 `.dll` 매칭

로더는 `.pck` basename을 구하고, 같은 basename의 DLL을 찾는다.

즉 현재 구조에서는 반드시 다음 짝이 맞아야 한다.

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`

## 4. `mod_manifest.json` 검사

로더는 `.pck`를 mount 한 뒤 `res://mod_manifest.json`을 찾는다.

그리고 manifest 안의 `pck_name`이 `.pck` basename과 정확히 같아야 한다.

현재 정상 값은 다음과 같다.

```json
{
  "pck_name": "sts2-speed-skeleton",
  "name": "STS2 Speed Skeleton",
  "author": "jidon + Codex",
  "description": "Non-invasive animation and wait acceleration scaffold for Slay the Spire 2.",
  "version": "0.1.0-skeleton"
}
```

여기서 `pck_name`에 `.pck` 확장자를 넣으면 로더가 거부한다.

## 5. DLL 로드와 Harmony

manifest가 유효하면 로더는 매칭 DLL을 로드한다.

현재 모드 쪽에는 `ModInitializerAttribute`를 쓰지 않았기 때문에, 로더가 자동으로 `Harmony.PatchAll(assembly)` 를 호출한다.

즉 현재 엔트리포인트는 “명시적 initializer”가 아니라 “Harmony patch 클래스 자동 스캔”이다.

## 6. 추가 DLL 의존성 해결

여기서 한 번 막혔다.

문제는 STS2 로더가 `sts2-speed-skeleton.dll` 은 읽었지만, 그 DLL이 참조하는 `Sts2Speed.Core.dll` 까지는 자동으로 찾지 못했다는 점이다.

그래서 지금은 모드 DLL 안에 assembly resolver를 넣었다.

핵심 파일:

- `src/Sts2Speed.ModSkeleton/Runtime/ModAssemblyResolver.cs`

역할:

- 모드 폴더 기준으로 추가 DLL을 찾는다.
- `Sts2Speed.Core.dll` 을 같은 `mods` 폴더에서 resolve 한다.

## 7. 런타임 설정 로드

패치 타깃을 resolve 하는 시점에 런타임 설정도 같이 읽는다.

핵심 파일:

- `src/Sts2Speed.Core/Configuration/RuntimeSettingsLoader.cs`
- `src/Sts2Speed.ModSkeleton/Runtime/RuntimePatchContext.cs`

설정 우선순위:

1. `STS2_SPEED_*` 환경 변수
2. `Sts2Speed.speed.txt` 공유 배속 파일
3. 기본값

특징:

- `enabled` 환경 변수가 없어도, `Sts2Speed.speed.txt` 값이 `1.0`이 아니면 자동 활성화된다.
- 현재 기본 모드는 `combatOnly=true` 이다.

## 8. 패치 적용

현재 패치 대상은 다음과 같다.

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`

역할 분담:

- `spineTimeScale`: Spine 애니메이션 배속
- `queueWaitScale`: `CustomScaledWait` 계열 대기시간 단축
- `effectDelayScale`: timer 기반 지연 단축

## 9. 로그

게임 로그에는 로더 수준의 성공 여부가 남는다.

예:

- `Found mod pck file ...`
- `Loading assembly DLL sts2-speed-skeleton.dll`
- `Finished mod initialization for 'STS2 Speed Skeleton'`
- `--- RUNNING MODDED! ---`

모드 자체 디버그 로그는 다음 파일에 남긴다.

- `mods\\sts2speed.runtime.log`

이 파일에는 다음 종류의 메시지가 들어간다.

- 초기 설정 로드
- 설정 변경 감지
- 패치 적용 1회 로그

## 10. 저장 경로

모드가 하나라도 로드되면 게임은 `modded/profile1` 쪽 저장 경로를 사용한다.

이건 안전상 꽤 중요하다.

- vanilla 진행과 modded 진행이 분리된다.
- 다만 `settings.save` 는 여전히 공용이므로, `mods_enabled` 같은 설정은 공유될 수 있다.
