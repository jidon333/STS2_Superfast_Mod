# STS2 상세 조사 로그

이 문서는 이번 모딩 작업의 상세 조사 기록이다.

목적은 다음과 같다.

1. 다음 작업자가 "왜 지금 구조가 이렇게 생겼는가"를 빠르게 이해하게 한다.
2. 같은 실수를 다시 반복하지 않게 한다.
3. 어떤 명령과 어떤 증거로 현재 결론에 도달했는지 남긴다.

짧은 요약본은 `WORKLOG.md`를 보고, 실제 조사 흐름과 판단 근거를 끝까지 따라가려면 이 문서를 본다.

## 0. 기본 전제

### 목표

최종 목표는 STS2용 `SuperFastMode` 계열 속도 모드를 만드는 것이다.

단, 이번 작업에서는 다음 제약을 유지했다.

- 라이브 설치본 `.exe` / 게임 DLL 직접 패치 금지
- 저장 파일 직접 변경 최소화
- 원복 가능한 방식 우선
- Steam 원본 설치 기준으로 동작해야 함
- 사용자 첫 플레이 감상은 최대한 보존

### 실제 설치 경로

- 게임 설치: `D:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2`
- 사용자 데이터: `C:\Users\jidon\AppData\Roaming\SlayTheSpire2`

### 실제 기술 스택 결론

초기 조사 결과:

- 엔진: `Godot 4.5.1`
- 런타임: `.NET 9`
- 저장 파일: JSON 텍스트 기반

이 결론이 이후 선택을 거의 전부 결정했다.

## 1. 첫 가설 수립

처음에는 두 가설을 동시에 열어뒀다.

### 가설 A

STS2는 아직 공식 모딩 루트를 완전히 열지 않았을 수 있다.

이 경우:

- 외부 로더가 필요할 수 있다
- GUMM 같은 우회 경로가 필요할 수 있다

### 가설 B

STS2 내부에는 이미 모드 로더가 들어 있을 수 있다.

이 경우:

- `mods` 폴더
- `.pck`
- managed `.dll`
- internal mod manifest

같은 경로가 존재할 수 있다.

### 당시 전략

그래서 초기 전략은 이랬다.

1. GUMM으로 "모드 코드가 실제로 실행되는지" 먼저 검증
2. 동시에 로컬 설치본에서 내장 모딩 구조를 조사
3. 둘 중 더 자연스러운 경로를 최종 구현 경로로 채택

## 2. 세이브/설정 조사

### 왜 먼저 세이브를 봤는가

모드가 저장 데이터를 망가뜨릴 수 있으므로, 코드 작성 전에 롤백 경로를 먼저 확보해야 했다.

### 확인한 파일

- `settings.save`
- `prefs.save`
- `progress.save`
- `current_run.save`
- 각종 `.backup`

### 관찰 결과

1. `current_run.save`는 항상 존재하지 않았다.
2. `.backup` 파일들이 따로 있었다.
3. `settings.save` 안에 `mod_settings`가 있었다.
4. `prefs.save` 안에 `fast_mode` 같은 속도 관련 키가 있었다.

### 이때 내린 결론

- 백업 툴은 "파일이 없으면 실패"가 아니라 "missing도 정상 상태"로 처리해야 한다.
- 설정/세이브는 JSON이므로 텍스트 diff와 해시 검증이 가능하다.
- 모딩 경고/동의 플래그가 저장 파일에 있을 가능성이 높다.

### 이후 구현으로 이어진 것

- snapshot/restore/verify 기능
- modded profile sync 기능

## 3. 후보 훅 포인트 찾기

### 첫 접근

게임 속도를 올리려면 뭘 건드릴지 먼저 알아야 했다.

가장 먼저 한 일은 다음 두 축이었다.

1. 저장 파일에서 속도 관련 키 찾기
2. 실행 바이너리에서 속도 관련 타입명/메서드명 찾기

### 저장 파일 쪽에서 잡은 단서

- `fast_mode`
- `settings.save`의 modding 관련 설정

### 바이너리 쪽에서 잡은 후보

나중에 `KnownPatchTargets.cs`에 정리한 목록은 다음이다.

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `CombatManager.WaitForActionThenEndTurn`
- `CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
- `CombatState.GodotTimerTask`
- `ActionExecutor.ExecuteActions`

### 이 단계에서의 의미

이 시점에 이미 "전역 time scale 한 방"보다 **정밀한 메서드 후킹**이 더 적합하다는 방향이 보였다.

## 4. PowerShell reflection 시도와 포기

### 시도 이유

가장 싸게 끝내는 방법은 PowerShell에서 직접 `sts2.dll`을 읽는 것이었다.

예상한 흐름은 다음과 같았다.

1. `Assembly.LoadFrom(...)`
2. 타입 이름 열람
3. 메서드 시그니처 확인

### 왜 실패했는가

실제론 막혔다.

이유:

- 게임 런타임이 `.NET 9`
- 의존성 DLL이 많음
- 즉석 reflection 환경을 구성하기 번거로움
- `MetadataLoadContext`를 바로 얹는 것보다 디컴파일이 더 빠름

### 여기서 배운 점

- "타입명만 보면 된다" 수준이면 reflection으로도 될 수 있지만
- 지금은 로더 규칙까지 확인해야 하므로 IL 디컴파일이 낫다

## 5. GUMM 경로 채택

### 당시 상황

내장 로더 규칙은 아직 확정되지 않았다.

그래서 먼저 살아 있는 진입점을 확보해야 했다.

### 왜 GUMM이 유력해 보였는가

- STS2는 Godot 게임이다
- Godot는 `override.cfg`를 통해 시작 씬 오버라이드가 가능하다
- 외부 스크립트가 실제로 실행되는지 확인하기 쉽다

### 실제 구성한 파일

- `override.cfg`
- `GUMM_mod_loader.tscn`
- `mod.cfg`
- `mod.gd`
- `GUMM_mod.gd`

### 당시 기대한 구조

```text
게임 시작
  -> override.cfg가 main_scene 변경
  -> GUMM loader 실행
  -> mod.gd 실행
  -> 로그 출력
  -> 원래 게임 씬으로 복귀
```

## 6. GUMM 첫 실패

### 실제 증상

로그에는 `Loading mod: STS2 Speed Skeleton`이 찍혔는데, 곧바로 parse error가 났다.

즉:

- "모드 발견" 단계는 통과
- "모드 실제 실행" 단계는 실패

### 원인

우리가 만든 최소 `GUMM_mod.gd`에 GUMM이 기대하는 헬퍼가 빠져 있었다.

대표적으로:

- `get_full_path()`

같은 함수가 없었다.

### 여기서 내린 결론

- GUMM은 최소 껍데기만 흉내 내면 되는 구조가 아니다
- 실제 베이스 스크립트를 그대로 맞춰야 한다

## 7. GUMM 실기 성공

### 수정 내용

공식 GUMM 저장소의 `System/4.x/GUMM_mod.gd`를 실제 패키지에 포함하도록 바꿨다.

### 확인한 신호

- `Loading mod: STS2 Speed Skeleton`
- bootstrap 로그
- 메인 메뉴 정상 진입

### 이 단계의 결론

GUMM은 최소한 다음 용도로는 성공했다.

- 부트스트랩 진입 검증
- 로그 삽입
- fallback 경로 확보

하지만 아직 이건 최종 해법이 아니었다.

## 8. 게임 실행 경로 수정

### 실수

처음에는 `SlayTheSpire2.exe`를 직접 실행하는 시도도 했다.

### 왜 틀렸는가

- Steam appID 초기화가 꼬인다
- Steamworks 관련 로그가 정상적이지 않다

### 수정

이후 실기 실행 기준을 다음으로 고정했다.

```powershell
steam.exe -applaunch 2868840
```

### 의미

이후의 "실제로 게임이 그렇게 동작했다"는 검증은 모두 Steam applaunch 기준이다.

## 9. 커뮤니티 배포 형식에서 얻은 단서

### 관찰된 설치법

다른 사람이 만든 비공식 모드 설치법은 다음처럼 단순했다.

1. `mods` 폴더 생성
2. `pck`, `dll`, `txt` 복사
3. `txt` 안 숫자로 배속 조절

### 여기서 얻은 강한 신호

이건 GUMM 구조보다 **게임 내장 `mods` 로더**와 훨씬 더 잘 맞는다.

왜냐하면 GUMM이라면 보통 다음이 더 필요하다.

- `override.cfg`
- GUMM loader scene
- 별도 bootstrap 스크립트

그런데 커뮤니티 배포엔 그런 게 없었다.

### 이때 내린 결론

- "실제 정석 경로는 GUMM이 아니라 네이티브 `mods`일 가능성이 높다"

이게 이후 디컴파일 방향을 결정했다.

## 10. ILSpy 도입과 `ModManager` 조사

### 왜 ILSpy를 택했는가

이 시점부터는 추측보다 디컴파일이 훨씬 효율적이었다.

### 조사 대상

- `sts2.dll`
- 특히 `MegaCrit.Sts2.Core.Modding.ModManager`

### 실제로 확인한 핵심 규칙

1. 게임은 `<game dir>\mods`를 연다
2. `.pck`를 재귀적으로 찾는다
3. 같은 basename의 `.dll`을 찾는다
4. `ProjectSettings.LoadResourcePack(...)`를 호출한다
5. `res://mod_manifest.json`이 필요하다
6. `pck_name`은 확장자 없는 basename과 같아야 한다
7. `ModInitializerAttribute`가 없으면 `Harmony.PatchAll(assembly)`를 호출한다

### 이 단계의 의미

여기서 사실상 최종 구조가 확정됐다.

```text
STS2 내장 네이티브 로더 + pck + dll + txt
```

GUMM은 이 시점부터 주경로가 아니라 fallback이 됐다.

## 11. 모드 동의 플래그 확인

### 왜 필요했는가

네이티브 `.pck`를 넣었는데 처음에 로드가 안 되거나 건너뛰는 경우가 있었다.

### 조사 결과

`settings.save` 안의 `mod_settings.mods_enabled`가 실제 게이트였다.

### 의미

- 모드를 처음 읽을 때 경고/동의 UI가 뜰 수 있다
- 동의 저장 전후 동작이 달라질 수 있다

이건 배포 문서에도 반드시 넣어야 하는 포인트가 됐다.

## 12. `.pck` 생성 경로 실험

### 문제 정의

내장 로더가 있어도 `.pck`를 만들 수 없으면 네이티브 방식은 막힌다.

### 시도 1: `PCKPacker`

headless Godot 프로젝트에서 `PCKPacker`를 직접 사용했다.

#### 결과

겉보기엔 `.pck`가 생성되지만, STS2는 `mod manifest가 없다`고 판단했다.

#### 해석

- Godot 자체에서 보기엔 정상 팩일 수 있다
- 하지만 STS2 로더가 기대하는 내보내기 형식과 완전히 같지 않을 수 있다

즉 "임의로 만든 PCK"와 "정식 export pack"은 다를 수 있다.

### 시도 2: 공식 `--export-pack`

Godot 4.5.1 콘솔 에디터를 받아서 공식 `--export-pack "Windows Desktop"`를 사용했다.

#### 결과

이 경로는 실제로 STS2에서 먹혔다.

#### 여기서 추가로 잡은 오류

오류 메시지:

- `PCK name in mod manifest ... does not match`

원인:

- `pck_name`에 `.pck` 확장자를 포함하고 있었다

수정:

```csharp
pck_name = Path.GetFileNameWithoutExtension(pckName)
```

### 결론

현재 `.pck` 생성의 정답은 공식 `--export-pack`이다.

## 13. 네이티브 로드 첫 실기 성공

### 확인한 로그 신호

- `.pck` 발견
- 매칭 DLL 로드
- `Harmony.PatchAll`
- `Finished mod initialization`
- `--- RUNNING MODDED! ---`

### 여기서 확정된 것

- 내장 네이티브 로더가 실제로 작동한다
- `mods + pck + dll` 구조는 가설이 아니라 실증이다

## 14. 첫 payload 추가

### 왜 이 메서드들을 먼저 골랐는가

처음부터 `ActionExecutor`나 큐 핵심부를 건드리면 부작용 범위가 크다.

그래서 첫 payload는 비교적 안전한 지점부터 시작했다.

### 1차 패치 지점

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`

### 의미

- 애니메이션 배속
- 명시적 wait 축소
- timer 기반 delay 축소

이 정도면 "체감 속도 변화"를 만들기 위한 첫 단계로 충분하다고 판단했다.

## 15. 추가 DLL 의존성 문제

### 실제 증상

`sts2-speed-skeleton.dll`은 로드되는데, 그 DLL이 참조하는 `Sts2Speed.Core.dll`은 못 찾았다.

에러는 대략 다음 의미였다.

- `Could not load file or assembly 'Sts2Speed.Core'`

### 원인

STS2 로더는 "모드 엔트리 DLL"은 읽어도, 그 DLL이 참조하는 추가 어셈블리까지 같은 폴더에서 자동 probing하지 않았다.

### 해결

`ModAssemblyResolver.cs`를 넣었다.

핵심 아이디어:

```csharp
AppDomain.CurrentDomain.AssemblyResolve += ResolveFromModDirectory;
AssemblyLoadContext.Default.Resolving += ResolveFromModDirectory;
```

즉 모드 DLL의 위치를 기준으로 추가 DLL을 직접 찾게 만들었다.

### 교훈

- 게임이 "한 개의 모드 DLL"을 읽는다고 해서 그 DLL의 dependency graph까지 자동으로 해결해주진 않는다

## 16. 런타임 로그 추가

### 왜 필요했는가

게임 로그만으로는 "패치가 설치됐는지"와 "실제로 적용됐는지"를 구분하기 어려웠다.

### 추가한 것

- `mods\sts2speed.runtime.log`

### 기록하는 내용

- 초기 설정
- 설정 변경 감지
- 패치 적용 1회 로그

### 의미

이 로그가 생기면서 "로더 문제", "설정 문제", "실제 전투 중 패치 적용 문제"를 분리해서 볼 수 있게 됐다.

## 17. modded 프로필 분리 문제 발견

### 실제 사용자 문제

모드를 켰더니 진행이 초기화된 것처럼 보였다.

### 조사 결과

- 바닐라 진행은 `profile1`
- 모드 진행은 `modded/profile1`

즉 저장이 사라진 게 아니라, 게임이 다른 루트를 보고 있었다.

### 실제 확인

- vanilla `progress.save`는 정상 해시
- modded `progress.save`는 사실상 새 프로필 상태

### 해결

1. 기존 `modded/profile1` 백업
2. `profile1` 전체를 `modded/profile1`에 복제
3. `progress.save` 해시 비교

### 왜 중요한가

이건 모드 본체의 기능 못지않게 중요한 "실제 사용자 데이터 안전" 이슈였다.

## 18. modded profile 복구 자동화

### 이유

같은 문제를 계속 수동 복사로 처리하는 건 위험하다.

### 추가한 명령

```powershell
dotnet run --project src/Sts2Speed.Tool -- sync-modded-profile
```

### 이 명령이 하는 일

1. 기존 `modded/profileN` 전체 백업
2. `profileN` 내용을 `modded/profileN`으로 복제
3. 리포트 JSON 생성

### 의미

이제 이 문제는 문서 설명만이 아니라 도구로도 해결된다.

## 19. speed semantics 버그 발견

### 발견 계기

코드를 설명하는 문서를 쓰다가 semantics가 어긋나 있다는 걸 발견했다.

### 당시 구현

`Sts2Speed.speed.txt = 2.0`이면:

- `spineTimeScale = 2.0`
- `queueWaitScale = 2.0`
- `effectDelayScale = 2.0`

그리고 이 값들을 전부 "곱셈"으로 적용하고 있었다.

### 문제

- 애니메이션은 빨라진다
- wait / timer는 길어진다

즉 사용자는 "2배속"을 기대했는데 실제론 일부가 느려졌다.

### 결론

배속 설정값은 "애니메이션 재생 속도"와 "지속시간"에 같은 방식으로 적용하면 안 된다.

## 20. speed semantics 수정

### 해결 전략

공통 계산을 한 파일로 분리해서 의미를 고정했다.

추가 파일:

- `src/Sts2Speed.Core/Configuration/SpeedScaleMath.cs`

### 현재 규칙

```text
animation = animation * multiplier
duration = duration / multiplier
```

즉:

- `2.0` = 애니메이션 2배속, wait/timer 절반
- `0.5` = 애니메이션 반속, wait/timer 2배

### 코드 반영 위치

- `RuntimePatchContext.TryApplySpineScale`
- `RuntimePatchContext.TryApplyQueueWaitScale`
- `RuntimePatchContext.TryApplyEffectDelayScale`

## 21. semantics 회귀 방지

### 왜 테스트가 필요했는가

이 버그는 설명 문서를 쓰다가 발견될 정도로, 눈으로만 보면 놓치기 쉽다.

### 추가한 self-test 포인트

- `2.0` speed multiplier는 float wait duration을 `0.5`로 만들어야 한다
- `0.5` speed multiplier는 double wait duration을 `2.0`으로 만들어야 한다
- `0.0` multiplier는 무효값으로 간주하고 duration을 바꾸지 않아야 한다

### 의미

이제 semantics는 설명이 아니라 테스트로 고정됐다.

## 22. 기본 배속 2.0 상향

### 이유

사용자 요청으로 기본 배속을 `2.0`으로 맞췄다.

### 반영 대상

- 패키지 생성 시 만들어지는 `Sts2Speed.speed.txt`
- live `mods` 폴더의 `Sts2Speed.speed.txt`

### 결과

현재 기본 배포 상태는 "2배속 의도"다.

## 23. 문서 구조 개편

### 문제

기존 문서는 요약은 있었지만, 다음 작업자가 조사 과정을 그대로 따라가기엔 부족했다.

### 추가/개편한 문서

- `MODDING_FROM_ZERO.md`
  - 개념 설명용
- `MOD_BEGINNER_GUIDE.md`
  - 코드 구조 빠른 파악용
- `LOAD_CHAIN.md`
  - 최종 메커니즘 요약용
- `SPEED_SEMANTICS.md`
  - 배속 해석 전용
- `WORKLOG.md`
  - 중간 길이 요약용
- `DETAILED_INVESTIGATION_LOG.md`
  - 이 문서, 상세 엔지니어링 로그

### 의도

- 사용자: 개념과 구조를 쉽게 파악
- 다음 AI/개발자: 재현 가능한 조사 흐름 확보

## 24. 실제 커밋 흐름

이 저장소의 주요 커밋 흐름은 다음과 같다.

- `ade7705` `Initial STS2 superfast mod skeleton`
- `3a20b9e` `Add integration tooling and backup workflow`
- `9795ed7` `Add development guide and investigation log`
- `a385247` `Validate GUMM live load path and update Korean docs`
- `e5e47f4` `Pivot docs and tooling toward native STS2 mods layout`
- `a26fd7f` `Validate native STS2 mod loading and add first runtime payload`
- `3c7e93d` `Add modded profile recovery workflow`
- `f4b140e` `Fix speed multiplier semantics and default to 2x`
- `bc8790e` `Expand Korean modding documentation`

이 순서를 보면:

1. skeleton 생성
2. 백업/툴링 확보
3. GUMM 검증
4. 네이티브 로더 피벗
5. payload 탑재
6. save 복구 자동화
7. semantics 수정
8. 문서 정리

라는 흐름으로 진화했다.

## 25. 아직 남은 기술 과제

현재 "로더 + 첫 payload + 복구 + 기본 semantics"는 정리됐다.

하지만 더 빠른 체감을 위해 남은 후보는 여전히 있다.

- `CombatManager.WaitForActionThenEndTurn`
- `CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
- `ActionExecutor.ExecuteActions`

이 축은 지금보다 더 공격적인 SuperFastMode 재현을 위해 다음 단계에서 검토해야 한다.

## 26. 다음 작업자에게 남기는 핵심 메모

1. 먼저 `mods` 네이티브 로더를 기준으로 생각해라. GUMM은 fallback이다.
2. `.pck`는 반드시 공식 `--export-pack` 경로를 우선 써라.
3. `pck_name`에 확장자를 넣지 마라.
4. 모드 DLL이 추가 DLL을 참조하면 resolver를 먼저 의심해라.
5. 모드 적용 후 진행이 사라진 것처럼 보이면 `profileN`과 `modded/profileN`을 비교해라.
6. speed multiplier는 animation과 duration에 같은 방식으로 적용하면 안 된다.
7. 실제 체감 품질은 결국 전투 플레이 검증이 필요하다.

## 27. 남은 훅을 다시 디컴파일한 이유

실제 플레이 체감이 이미 꽤 자연스러웠는데도, 초기 문서에는 여전히 다음 후보가 "다음 단계에서 붙일 만한 훅"처럼 남아 있었다.

- `CombatManager.WaitForActionThenEndTurn`
- `CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
- `ActionExecutor.ExecuteActions`

이 상태는 좋지 않았다.

- 사용자 입장에서는 "왜 아직 안 붙였지?"가 불명확했고
- 다음 AI 입장에서는 "그냥 추가 구현하면 되는 TODO"처럼 보일 수 있었다

그래서 이 셋이 정말 "남은 대기시간 훅"인지, 아니면 위험한 동기화 지점인지 다시 디컴파일로 확인했다.

## 28. `CombatManager` 재조사 결과

`ilspycmd`로 `MegaCrit.Sts2.Core.Combat.CombatManager`를 다시 열었다.

핵심 관찰:

### `WaitForActionThenEndTurn`

구조 요약:

```csharp
await action.CompletionTask;
await AfterAllPlayersReadyToEndTurn(actionDuringEnemyTurn);
```

여기서 이미 첫 판단이 바뀌었다.

이건 단순 `Task.Delay` 계열이 아니라 "특정 action이 끝날 때까지" 기다리는 함수다.

### `AfterAllPlayersReadyToEndTurn`

이 함수 안에서는 다시:

```csharp
await WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction();
await EndPlayerTurnPhaseOneInternal();
```

즉 turn end 직전 동기화 관문이다.

### `WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`

여기서는:

- 현재 action이 player-driven인지 확인
- 필요하면 `TaskCompletionSource`를 생성
- `AfterActionExecuted` 이벤트를 구독
- 다음 ready action이 없거나 non-player-driven이 될 때까지 관찰

즉 이것도 단순 sleep이 아니라 queue state barrier다.

### 이 조사로 얻은 결론

처음 이름만 보고 "queue drain wait 줄이기 좋은 곳"이라고 생각했던 판단은 너무 낙관적이었다.

이 함수들을 직접 빠르게 만들면 얻는 것은 "덜 기다림"이 아니라, 잘못하면:

- phase 전환이 너무 빨라짐
- player-driven action 경계 붕괴
- soft lock
- 시각적 자연스러움 저하

즉 이쪽은 "보류 이유가 단순 미구현이 아니라 안전성"이라고 정리해야 맞다.

## 29. `ActionExecutor` 재조사 결과

다음으로 `MegaCrit.Sts2.Core.GameActions.ActionExecutor`의 `ExecuteActions()`를 다시 열었다.

구조 요약:

1. ready action을 찾는다.
2. `WaitForUnpause()`를 기다린다.
3. action을 실행한다.
4. action task가 끝날 때까지 frame 단위로 기다린다.
5. `CombatManager.Instance.CheckWinCondition()`를 호출한다.
6. 다음 action으로 넘어간다.

이 함수는 이름보다 훨씬 더 핵심에 있다.

즉 "전투 대기시간을 조금 줄일 수 있는 함수"가 아니라:

- 액션 실행 루프
- 프레임 진행 경계
- 승패 체크 타이밍

을 모두 포함한다.

여기까지 보고 나면 판단은 명확하다.

- 이 함수를 건드리면 더 공격적인 템포는 만들 수 있다
- 하지만 지금 단계에서 자연스러운 모드를 유지하려면 가장 나중에 봐야 한다

## 30. `effectDelayScale`가 왜 체감상 약할 수 있는가

`effectDelayScale`는 현재 이미 구현되어 있는데도, 런타임 로그에서 자주 보이지 않았다.

그래서 `CombatState.GodotTimerTask(double timeSec)`도 다시 확인했다.

본체는 다음과 같이 매우 단순했다.

```csharp
SceneTreeTimer sceneTreeTimer = ((SceneTree)Engine.GetMainLoop()).CreateTimer(timeSec);
await sceneTreeTimer.ToSignal(sceneTreeTimer, SceneTreeTimer.SignalName.Timeout);
```

이건 "timer helper" 자체는 맞지만, 그 자체로 모든 전투 연출이 여길 지나간다는 뜻은 아니다.

실제로 확인한 호출 맥락 중 하나는 spawn timeout 계열이었다.

그래서 현재 해석은 다음이 맞다.

- `effectDelayScale`는 죽은 설정이 아니다
- 하지만 현재 후크 하나만으로는 플레이 체감 대부분을 설명하지 못할 수 있다
- 즉 "실전 hit가 적다"는 진단은 코드가 안 붙어서가 아니라 훅 지점 자체의 범위 문제일 가능성이 높다

## 31. 구현을 안 한 것도 결정이었다

재조사 후 선택지는 두 개였다.

1. `CombatManager` / `ActionExecutor`까지 바로 패치해 본다
2. 지금의 자연스러운 체감을 우선 보존하고, 보류 이유를 문서화한다

이번에는 2번을 택했다.

이유:

- 이미 플레이 체감이 "자연스럽다"는 피드백이 있었다
- 남은 후보들은 단순 duration hook이 아니라 queue/core loop였다
- 여기서 더 욕심내면 개선보다 회귀 가능성이 더 크다고 판단했다

즉 "수정하지 않음"도 이번엔 명시적인 설계 결정이다.

## 32. 실제 사용되지 않는 코드 제거

마지막으로 저장소 전체를 훑어, 최종 네이티브 경로와 무관한 scaffold를 걷어냈다.

제거한 것:

- `fastModeOverride`
- `animationScale`
- `preserveGameSettings`
- `verboseLogging`
- GUMM 전용 CLI/패키징 코드
- 예전 skeleton dry-run 패키징 코드
- 관련 self-test

남긴 것:

- 네이티브 `mods + pck + dll + txt` 패키징
- runtime Harmony payload
- snapshot / restore / verify
- `sync-modded-profile`
- 조사 문서와 역사 기록

결과적으로 지금 저장소는 "조사 흔적은 문서로 남기고, 실행 가능한 코드는 최종 경로만 남긴 상태"에 가깝다.
