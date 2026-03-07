# STS2 네이티브 로드 체인

이 문서는 STS2가 실제로 어떤 순서로 우리 모드를 읽고, 어디서 Harmony 패치가 설치되는지 짧고 구조적으로 정리한다.

## 1. 게임 시작

게임 초기화 과정에서 `ModManager.Initialize()`가 호출된다.

여기서부터 STS2 내장 네이티브 모드 로더가 동작한다.

## 2. `mods` 폴더 스캔

로더는 게임 설치 폴더 아래 `mods` 디렉토리를 연다.

현재 패키지 이름은 다음 조합을 기준으로 맞춰져 있다.

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`

## 3. `.pck` basename과 `.dll` 매칭

로더는 `.pck` basename을 구한 뒤, 같은 basename의 `.dll`을 찾는다.

즉 이름이 어긋나면 로드되지 않는다.

현재는 반드시 다음 짝이 맞아야 한다.

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`

## 4. `.pck` 마운트와 `mod_manifest.json`

로더는 `.pck`를 마운트한 뒤 `res://mod_manifest.json`을 읽는다.

이 manifest는 최소한 다음 정보를 가져야 한다.

```json
{
  "pck_name": "sts2-speed-skeleton",
  "name": "STS2 Speed Skeleton",
  "author": "jidon + Codex",
  "description": "Non-invasive animation and wait acceleration scaffold for Slay the Spire 2.",
  "version": "0.1.0-skeleton"
}
```

가장 중요한 규칙:

- `pck_name`은 `.pck` basename과 같아야 한다
- `.pck` 확장자는 넣지 않는다

## 5. DLL 로드

manifest 검사가 끝나면 게임이 matching DLL을 자기 프로세스 안으로 로드한다.

즉 여기서 별도 인젝터는 없다.

현재 이 단계의 실제 payload는 `sts2-speed-skeleton.dll`이다.

## 6. 추가 DLL resolve

문제는 모드 본체 DLL이 참조하는 추가 DLL까지 게임이 자동으로 찾아주지는 않는다는 점이었다.

그래서 모드 본체 안에서 아래 resolver를 설치했다.

```csharp
[ModuleInitializer]
internal static void Initialize()
{
    AppDomain.CurrentDomain.AssemblyResolve += ResolveFromModDirectory;
    AssemblyLoadContext.Default.Resolving += ResolveFromModDirectory;
}
```

역할:

- `mods` 폴더 기준으로 `Sts2Speed.Core.dll` 같은 추가 의존성을 찾는다

## 7. Harmony 패치 등록

현재 모드 DLL에는 `ModInitializerAttribute`를 쓰지 않는다.

그 대신 STS2 로더가 DLL을 읽은 뒤 `Harmony.PatchAll(assembly)`를 호출한다.

즉 로드 체인은 다음과 같이 이해하면 된다.

```text
게임 로더
  -> DLL 로드
  -> Harmony.PatchAll
  -> [HarmonyPatch] 클래스 스캔
  -> Prefix 설치
```

## 8. 런타임 설정 로드

패치 타깃을 resolve 하는 시점에 런타임 설정도 같이 읽는다.

설정 우선순위:

1. `STS2_SPEED_*` 환경 변수
2. `Sts2Speed.speed.txt`
3. 기본값

현재 기본 `Sts2Speed.speed.txt` 값은 `2.0`이다.

## 9. 실제 패치 지점

현재 설치되는 패치는 다음 네 곳이다.

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`

실제 동작 의미:

- `spineTimeScale` -> 애니메이션 속도 직접 배속
- `queueWaitScale` -> wait duration 축소
- `effectDelayScale` -> timer duration 축소

즉 지금은 "전역 time_scale"을 바꾸는 방식이 아니라 **개별 메서드 인자 조정형 패치**다.

## 10. 모드 저장 경로 분리

모드가 하나라도 로드되면 게임은 `modded/profileN` 쪽 저장 경로를 사용한다.

이 때문에:

- vanilla 진행과 modded 진행이 분리되고
- modded 쪽이 비어 있으면 새 프로필처럼 보이며
- 필요 시 `profileN -> modded/profileN` 복구가 필요하다

복구는 다음 명령으로 자동화했다.

```powershell
dotnet run --project src/Sts2Speed.Tool -- sync-modded-profile
```

## 11. 로그 확인 포인트

게임 로그에서 확인할 수 있는 신호:

- `Found mod pck file ...`
- `Loading assembly DLL sts2-speed-skeleton.dll`
- `Finished mod initialization for 'STS2 Speed Skeleton'`
- `--- RUNNING MODDED! ---`

모드 자체 진단 로그:

- `mods\sts2speed.runtime.log`

여기에는 다음 정보가 남는다.

- 초기 설정 로드
- 설정 변경 감지
- 패치 적용 1회 로그
