# STS2 모딩 0부터 설명

이 문서는 `Slay the Spire 2` 모딩을 거의 모르는 사람을 기준으로, 이번 작업에서 실제로 사용한 개념과 원리를 처음부터 끝까지 설명한다.

목표는 두 가지다.

1. "왜 이 게임에서 `pck + dll + txt` 조합이 동작하는가"를 이해한다.
2. "이 저장소가 실제로 무엇을 만들고, 어떤 순서로 배포하고, 어디에 개입하는가"를 이해한다.

## 1. 먼저 큰 그림

이번 모드는 외부 인젝터가 게임 프로세스에 억지로 붙는 구조가 아니다.

핵심은 다음 한 줄로 정리된다.

```text
STS2가 원래 가지고 있는 모드 로더가 우리 DLL을 읽고,
그 DLL 안의 Harmony 패치가 게임 메서드 호출 전에 개입한다.
```

즉 런타임 구조는 다음과 같다.

1. 게임이 `mods` 폴더를 스캔한다.
2. `.pck`와 같은 이름의 `.dll`을 찾는다.
3. 게임이 DLL을 자기 프로세스 안으로 로드한다.
4. 게임이 `Harmony.PatchAll(...)`을 호출한다.
5. 우리 패치 코드가 게임 메서드 앞에서 실행된다.
6. `Sts2Speed.speed.txt` 값을 읽어 실제 속도 관련 인자를 바꾼다.

## 2. 용어부터 정리

### 모드(mod)

게임 본체를 직접 바꾸지 않고, 게임이 로드할 수 있는 별도 파일 묶음이다.

이번 프로젝트에서 모드는 보통 다음 파일을 의미한다.

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `Sts2Speed.speed.txt`
- 필요 시 추가 DLL

### 로더(loader)

게임이 모드 파일을 찾아 읽고 초기화하는 코드다.

이번 작업에서 중요한 로더는 두 종류가 있었다.

- STS2 내장 네이티브 모드 로더
- GUMM 로더

최종적으로 사용한 것은 첫 번째다.

### 패치(patch)

여기서 패치는 "원본 exe를 덮어쓴다"는 의미가 아니다.

이번 프로젝트에서 패치는 다음 의미다.

- 게임의 특정 메서드가 호출될 때
- 그 앞에서 우리 코드가 먼저 실행되고
- 원본 메서드 인자를 바꾸거나
- 필요하면 원본 전후 동작을 바꾸는 것

### 후킹(hook)

특정 함수 호출 지점에 개입하는 행위다.

이번 문맥에서 hook과 patch는 거의 같은 의미로 써도 된다.

### Harmony

`.NET` 런타임에서 메서드 후킹/패치를 도와주는 라이브러리다.

지금 모드는 이 라이브러리를 통해 원본 메서드 호출 전에 `Prefix`를 실행한다.

### Prefix

원본 메서드가 실행되기 전에 먼저 호출되는 패치 함수다.

지금은 가장 안전한 방식인 `Prefix`만 쓰고 있다.

### `.pck`

Godot 리소스 팩이다.

이 파일은 Godot 쪽에서 "이 모드에 들어 있는 리소스 묶음" 역할을 한다. `mod_manifest.json`도 이 안에 들어간다.

### `.dll`

실제 C# 로직이 들어 있는 어셈블리다.

이번 모드에서는 여기 안에 Harmony 패치 클래스와 런타임 설정 처리 코드가 들어 있다.

### `mod_manifest.json`

이 모드를 게임이 정식 모드로 인식하기 위한 메타데이터 파일이다.

중요한 점은 이 파일이 **`.pck` 안에** 있어야 한다는 것이다.

### resolver

추가 DLL 의존성을 모드 폴더에서 찾게 해주는 코드다.

이번 프로젝트에서는 `Sts2Speed.Core.dll`을 찾기 위해 별도 resolver가 필요했다.

## 3. 왜 STS2는 DLL을 읽을 수 있는가

이건 우리가 인젝션해서 억지로 만든 구조가 아니다.

STS2 내부 코드에 이미 네이티브 모드 로더가 있다. `sts2.dll`을 디컴파일해서 확인한 결과, 게임은 자기 설치 폴더 아래 `mods`를 스캔하고, `.pck`와 매칭되는 `.dll`을 직접 읽는다.

이번 조사에서 확정한 규칙은 이렇다.

1. `<game dir>\mods`를 연다.
2. 재귀적으로 `.pck`를 찾는다.
3. `.pck` basename과 같은 이름의 `.dll`을 찾는다.
4. `.pck`를 마운트한다.
5. `res://mod_manifest.json`을 찾는다.
6. `pck_name`이 `.pck` basename과 같아야 한다.
7. DLL을 로드한다.
8. `ModInitializerAttribute`가 없으면 `Harmony.PatchAll(assembly)`를 호출한다.

즉 "게임이 DLL을 읽는가?"라는 질문의 답은 "원래 그렇게 만들어져 있다"다.

## 4. `.pck`는 정확히 뭘 하는가

`.pck`는 **Godot 쪽 모드 패키지 단위**다.

여기서 중요한 점은 `.pck`가 모든 로직을 실행하는 파일은 아니라는 것이다.

이번 프로젝트 기준으로 `.pck`는 주로 다음 역할을 한다.

- 게임이 모드 하나를 인식하게 함
- `mod_manifest.json` 제공
- 필요하면 Godot 리소스 포함

현재 manifest 예시는 다음과 같다.

```json
{
  "pck_name": "sts2-speed-skeleton",
  "name": "STS2 Speed Skeleton",
  "author": "jidon + Codex",
  "description": "Non-invasive animation and wait acceleration scaffold for Slay the Spire 2.",
  "version": "0.1.0-skeleton"
}
```

주의점:

- `pck_name`에는 `.pck` 확장자를 넣으면 안 된다.
- 이 값이 실제 파일 basename과 다르면 로더가 거부한다.

## 5. `.dll`은 정확히 뭘 하는가

`.dll`은 이번 모드의 실제 본체다.

현재 DLL이 하는 일은 크게 네 가지다.

1. 모드 추가 의존성 resolve
2. 런타임 설정 읽기
3. Harmony 패치 등록
4. 실제 메서드 인자 변경

핵심 코드는 이렇다.

```csharp
[HarmonyPatch]
internal static class SpineTimeScalePatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var candidate in new[]
                 {
                     RuntimePatchContext.TryResolveMethod("MegaCrit.Sts2.Core.Bindings.MegaSpine.MegaAnimationState", "SetTimeScale", typeof(float)),
                     RuntimePatchContext.TryResolveMethod("MegaCrit.Sts2.Core.Bindings.MegaSpine.MegaTrackEntry", "SetTimeScale", typeof(float)),
                 })
        {
            if (candidate is not null)
            {
                yield return candidate;
            }
        }
    }

    [HarmonyPrefix]
    private static void Prefix(ref float scale)
    {
        RuntimePatchContext.TryApplySpineScale(ref scale);
    }
}
```

이 코드는 다음 의미다.

- 게임이 `SetTimeScale(float)`를 호출하려고 하면
- Harmony가 먼저 `Prefix(ref float scale)`를 호출하고
- 우리가 `scale` 값을 바꾼 뒤
- 원본 `SetTimeScale`가 그 값으로 실행된다

즉 DLL이 "게임 함수를 감싸는" 구조라는 이해는 맞다.

## 6. Harmony는 정확히 어떤 역할을 하는가

Harmony는 ".NET 메서드 패치를 쉽게 걸어주는 라이브러리"다.

이번 프로젝트에서 Harmony가 해주는 일은 다음과 같다.

1. 패치 클래스 탐색
2. 어떤 메서드에 붙을지 결정
3. 원본 메서드 호출 전에 `Prefix` 실행

우리가 직접 호출하는 쪽이 아니라, STS2의 모드 로더가 `Harmony.PatchAll(assembly)`를 실행해 준다.

즉 구조는 이렇다.

```text
게임 로더 -> 우리 DLL 로드 -> Harmony.PatchAll -> 패치 설치 완료
```

그 뒤부터는 게임이 원래 메서드를 부를 때마다 우리 Prefix가 같이 돈다.

## 7. 이 저장소에서 실제로 바꾸는 값은 무엇인가

지금 모드는 "프로퍼티를 직접 set"하는 것보다 **메서드 인자를 바꾸는 방식**에 가깝다.

현재 실제로 건드리는 지점은 다음 네 곳이다.

- `MegaAnimationState.SetTimeScale(float scale)`
- `MegaTrackEntry.SetTimeScale(float scale)`
- `Cmd.CustomScaledWait(float fastSeconds, float standardSeconds, ...)`
- `CombatState.GodotTimerTask(double timeSec)`

실제 계산은 여기로 모아뒀다.

```csharp
public static bool TryApplySpineScale(ref float scale)
{
    var settings = GetSettings();
    if (!ShouldApply(settings) || !SpeedScaleMath.HasMeaningfulEffect(settings.SpineTimeScale))
    {
        return false;
    }

    scale = SpeedScaleMath.ApplyAnimationSpeedMultiplier(scale, settings.SpineTimeScale);
    return true;
}
```

```csharp
public static bool TryApplyQueueWaitScale(ref float fastSeconds, ref float standardSeconds)
{
    var settings = GetSettings();
    if (!ShouldApply(settings) || !SpeedScaleMath.HasMeaningfulEffect(settings.QueueWaitScale))
    {
        return false;
    }

    fastSeconds = SpeedScaleMath.ApplyDurationSpeedMultiplier(fastSeconds, settings.QueueWaitScale);
    standardSeconds = SpeedScaleMath.ApplyDurationSpeedMultiplier(standardSeconds, settings.QueueWaitScale);
    return true;
}
```

즉 지금의 핵심은:

- 애니메이션 재생 속도는 직접 배속
- wait / timer는 지속시간을 역수 방식으로 축소

## 8. 왜 `2.0`이 이제 맞는 의미인가

한 번 버그가 있었다.

초기 구현은 `2.0`을 wait 시간에도 그대로 곱했다. 그 결과:

- 애니메이션은 빨라짐
- 대기시간은 오히려 길어짐

지금은 이걸 고쳤다.

현재 의미는 다음과 같다.

- `spineTimeScale = 2.0` -> 애니메이션 2배속
- `queueWaitScale = 2.0` -> 대기시간 절반
- `effectDelayScale = 2.0` -> 타이머 지연 절반

공식은 다음과 같다.

```text
animation = animation * speedMultiplier
duration = duration / speedMultiplier
```

현재 기본값은 `2.0`이다.

## 9. 설정은 어디서 읽는가

설정 소스는 두 가지다.

1. 환경 변수
2. `Sts2Speed.speed.txt`

현재 우선순위는 다음과 같다.

1. `STS2_SPEED_*`
2. `Sts2Speed.speed.txt`
3. 기본값

`Sts2Speed.speed.txt`에 `2.0`만 적어 두면, 별도 환경 변수 없이 공통 배속 fallback으로 들어간다.

현재 코드에 실제로 남겨 둔 런타임 설정 표면은 다음뿐이다.

- `enabled`
- `spineTimeScale`
- `queueWaitScale`
- `effectDelayScale`
- `combatOnly`

초기 skeleton에 있던 `fastModeOverride`, `animationScale`, `preserveGameSettings`, `verboseLogging`은 최종 네이티브 경로에서 실제로 쓰이지 않아 정리했다.

핵심 코드는 다음과 같다.

```csharp
var sharedSpeedFilePath = Path.Combine(modDirectory, "Sts2Speed.speed.txt");
var sharedMultiplier = TryReadSharedMultiplier(sharedSpeedFilePath, warnings);

if (sharedMultiplier.HasValue)
{
    sources.Add("Sts2Speed.speed.txt");
    settings = ApplySharedMultiplierFallback(settings, sharedMultiplier.Value, environmentOverrides.AppliedOverrideNames);
}
```

## 10. 왜 `Sts2Speed.Core.dll`이 따로 필요한가

처음에는 `sts2-speed-skeleton.dll` 하나만 있으면 될 줄 알았다.

하지만 실제로는 모드 DLL이 참조하는 추가 DLL을 STS2가 자동으로 찾아주지 않았다.

그래서 다음 코드가 필요해졌다.

```csharp
[ModuleInitializer]
internal static void Initialize()
{
    AppDomain.CurrentDomain.AssemblyResolve += ResolveFromModDirectory;
    AssemblyLoadContext.Default.Resolving += ResolveFromModDirectory;
}
```

이 resolver는 모드 DLL이 자기 옆 폴더의 `Sts2Speed.Core.dll`을 찾을 수 있게 해준다.

## 11. `.pck`는 어떻게 만들었는가

이 부분도 시행착오가 있었다.

처음엔 `PCKPacker`를 직접 써봤지만, STS2 로더가 기대하는 형태와 완전히 맞지 않았다.

최종적으로는 Godot 공식 콘솔 에디터의 `--export-pack` 경로를 썼다.

현재 사용 명령은 툴로 감싸져 있고, 사용자는 보통 아래 명령만 쓰면 된다.

```powershell
dotnet run --project src/Sts2Speed.Tool -- build-native-pck --layout flat
```

이 명령은 내부적으로:

1. export용 임시 Godot 프로젝트를 준비하고
2. `mod_manifest.json`을 넣고
3. Godot 콘솔 에디터를 호출해서
4. 최종 `.pck`를 만든다

## 12. 배포는 어떤 순서인가

현재 표준 순서는 이렇다.

### 1. 백업

```powershell
dotnet run --project src/Sts2Speed.Tool -- snapshot --snapshot-root artifacts/snapshots/<name>
```

### 2. `.pck` 생성

```powershell
dotnet run --project src/Sts2Speed.Tool -- build-native-pck --layout flat
```

### 3. 게임 `mods` 폴더에 배포

```powershell
dotnet run --project src/Sts2Speed.Tool -- deploy-native-package --layout flat
```

### 4. 필요하면 modded 프로필 복구

```powershell
dotnet run --project src/Sts2Speed.Tool -- sync-modded-profile
```

### 5. 게임 실행 후 플레이 검증

실제 전투에서 체감을 본다.

## 13. 왜 진행 데이터가 사라진 것처럼 보였는가

이건 모딩을 처음 하는 사람이 가장 많이 헷갈릴 수 있는 부분이다.

STS2는 모드가 하나라도 로드되면 저장 경로를 `modded/profileN`으로 분리한다.

즉:

- 바닐라 저장: `profileN`
- 모드 저장: `modded/profileN`

그래서 바닐라 진행만 있고 modded 쪽이 비어 있으면, 모드 실행 시 새 프로필처럼 보인다.

이건 "데이터가 삭제된 것"이 아니라 "게임이 다른 저장 슬롯을 보고 있는 것"이다.

그래서 복구 명령이 필요했다.

## 14. GUMM은 왜 썼고, 왜 최종 경로가 아니게 됐는가

초기에는 STS2 내장 로더 규칙이 확정되지 않았기 때문에 GUMM도 시도했다.

GUMM은 Godot 기본 기능이 아니라, Godot의 `override.cfg`를 이용해 시작 씬을 바꾸고 `mod.gd`를 로드하는 외부 로더다.

당시 GUMM이 유효했던 이유:

- "모드 코드가 실제로 실행되는가"를 빨리 확인할 수 있었다
- 로그/부트스트랩 검증이 쉬웠다

하지만 최종적으로는 STS2 자체가 `mods + pck + dll` 구조를 이미 지원한다는 게 확정됐다.

그래서 지금은:

- GUMM = 조사 과정에서 유효했던 역사적 진단 경로
- 네이티브 로더 = 최종 구현 경로

로 정리돼 있다.

## 15. 지금 남은 기술 과제

현재 패치는 다음 단계까지 왔다.

- 로더 검증 완료
- `.pck` 생성 경로 확정
- 런타임 payload 탑재 완료
- 기본 배속 semantics 수정 완료
- 기본 배속 `2.0` 반영 완료

하지만 더 보완할 여지는 있다.

- `CombatManager.WaitForActionThenEndTurn`
- `CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
- `ActionExecutor.ExecuteActions`

이 후보들은 "더 빠르게 만들 수 있는 곳"인 동시에 "자연스러움을 잃기 쉬운 곳"이기도 하다.

이번에 다시 디컴파일해서 확인한 결과:

- `CombatManager.WaitForActionThenEndTurn`는 action completion과 turn-end phase를 기다리는 동기화 게이트다.
- `WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`는 큐 상태와 player-driven action 경계를 관찰하는 함수다.
- `ActionExecutor.ExecuteActions`는 액션 실행과 frame 진행, 승패 체크를 묶은 핵심 루프다.

즉 이 셋은 단순 "대기시간 숫자 줄이기" 문제가 아니라, 전투 상태기계와 액션 순서 보존 문제에 가깝다.

반대로 현재 `effectDelayScale`가 겨냥한 `CombatState.GodotTimerTask(double)`는 utility timer helper라서, 설정은 살아 있어도 체감에 항상 크게 보이지는 않을 수 있다.

이 차이를 별도로 정리한 문서는 `PENDING_HOOKS_AND_RISKS.md`다.

## 16. 이 문서 다음에 읽을 문서

- `MOD_LOADING_STRATEGIES.md`
  - GUMM, GUMM bootstrap + C# payload, 네이티브 로더 비교
- `LOAD_CHAIN.md`
  - 게임이 실제로 어떤 순서로 모드를 읽는지 더 짧고 구조적으로 정리
- `PENDING_HOOKS_AND_RISKS.md`
  - 왜 일부 훅은 아직 안 붙였는지, 왜 `effectDelayScale`가 덜 눈에 띄는지 설명
- `MOD_BEGINNER_GUIDE.md`
  - 이 저장소 파일 구성을 빠르게 훑고 싶을 때
- `WORKLOG.md`
  - 어떤 시행착오를 거쳐 여기까지 왔는지 시간 순서로 보고 싶을 때
