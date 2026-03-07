# STS2 Speed Mod 작업 기록

이 문서는 실제 조사와 시행착오를 순서대로 기록한다. 성공한 경로뿐 아니라 틀린 가정과 실패 원인도 같이 남긴다.

더 세밀한 조사 순서, 사용한 명령, 판단 근거까지 보려면 `DETAILED_INVESTIGATION_LOG.md`를 같이 본다.

## 1. 초기 가정

처음에는 두 가지를 동시에 열어두고 시작했다.

1. STS2가 공식 모딩 경로를 아직 완전히 열지 않았을 수 있다.
2. 그래도 내부에는 이미 `ModManager`, `ModManifest`, `SteamWorkshop`, `ModsDirectory` 같은 코드 흔적이 있을 수 있다.

그래서 초반 전략은 “GUMM으로 먼저 진입을 검증하고, 동시에 내장 모드 구조를 캐는 것”이었다.

## 2. 설치본과 세이브 경로 확인

먼저 실제 설치/저장 경로를 확인했다.

- 게임 설치: `D:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2`
- 사용자 데이터: `C:\Users\jidon\AppData\Roaming\SlayTheSpire2`

이 단계에서 다음을 확인했다.

- 엔진은 Godot 4.5.1
- 실행 구조는 `.NET 9`
- 저장 파일은 JSON 텍스트

## 3. 세이브 구조 조사

`prefs.save`, `settings.save`, `progress.save`, `current_run.save`를 확인하면서 백업 설계를 세웠다.

중요한 관찰:

- `current_run.save`는 항상 있는 파일이 아니었다.
- `.backup` 파일이 별도로 존재했다.
- `settings.save` 안에 `mod_settings`가 있었다.

이 때문에 snapshot/restore는 “파일이 없으면 오류”가 아니라 “missing 상태도 정상”으로 처리하도록 설계했다.

## 4. 후보 메서드 이름 찾기

속도 모드를 만들려면 어디를 건드려야 하는지부터 알아야 했다.

처음엔 저장 파일에서 `fast_mode` 같은 키를 확인했고, 그 다음 DLL 메타데이터와 타입명을 뒤졌다.

그 과정에서 잡힌 후보는 다음과 같았다.

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `CombatManager.WaitForActionThenEndTurn`
- `CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
- `CombatState.GodotTimerTask`
- `ActionExecutor.ExecuteActions`

이 목록은 `KnownPatchTargets.cs`에 정리했다.

## 5. PowerShell reflection 시도와 실패

처음엔 PowerShell에서 직접 `sts2.dll`을 reflection으로 읽어서 필요한 타입을 확인하려고 했다.

하지만 여기서 막혔다.

막힌 이유:

- 타깃 런타임이 `.NET 9`
- 종속 DLL 수가 많음
- `MetadataLoadContext`를 바로 쓰기엔 환경 준비가 번거로움

결론은 명확했다. reflection만으로 밀기보다는 디컴파일 도구를 쓰는 게 빠르다.

## 6. GUMM 가설 채택

당시에는 네이티브 모드 경로가 확정되지 않았으므로, Godot 외부 로더인 GUMM을 먼저 시험했다.

이유:

- Godot 게임이라 `override.cfg` 기반 씬 오버라이드가 가능하다
- 모드가 실제로 실행되는지 빠르게 확인할 수 있다

그래서 다음 구조를 먼저 만들었다.

- `override.cfg`
- `GUMM_mod_loader.tscn`
- `mod.cfg`
- `mod.gd`
- `GUMM_mod.gd`

## 7. GUMM 첫 실패

첫 GUMM 실험은 절반만 성공했다.

로그에는 `Loading mod: STS2 Speed Skeleton`이 찍혔지만, 바로 parse error가 났다.

원인:

- 우리가 만든 최소 `GUMM_mod.gd`에 `get_full_path()` 같은 헬퍼가 없었다
- `mod.gd`는 그 함수를 기대하고 있었다

이 실패에서 배운 점:

- “모드가 발견됨”과 “모드가 실제로 실행됨”은 전혀 다른 단계다

## 8. GUMM 수정과 실기 성공

이후 GUMM 저장소의 실제 `System/4.x/GUMM_mod.gd`를 패키지에 복사하도록 바꿨다.

그 결과 실기에서 다음이 확인됐다.

- `Loading mod: STS2 Speed Skeleton`
- bootstrap 로그 출력
- 메인 메뉴까지 정상 진입

이때 GUMM은 “실행 진입 검증” 역할을 충분히 했다.

## 9. 직접 exe 실행이 틀렸다는 점 확인

처음에는 `SlayTheSpire2.exe`를 직접 실행하기도 했다.

이건 잘못된 경로였다.

문제:

- Steam appID 초기화 실패
- Steamworks 관련 로그 문제

그래서 이후 모든 실기 테스트는 `steam.exe -applaunch 2868840` 기준으로 바꿨다.

## 10. 네이티브 모드 예시를 보고 방향 전환

커뮤니티에서 본 비공식 모드는 설치법이 매우 단순했다.

- `mods` 폴더 생성
- `pck`, `dll`, `txt` 파일 복사
- `txt` 안 숫자를 바꾸면 배속 변경

여기서 강한 단서를 얻었다.

- 이 구조는 GUMM보다 STS2 내장 `mods` 로더에 훨씬 잘 맞는다
- 따라서 GUMM을 최종 경로로 볼 이유가 줄었다

## 11. ILSpy 도입

이 시점부터는 추측보다 디컴파일이 더 효율적이라고 판단했다.

그래서 `ilspycmd`를 로컬 도구로 설치하고 `sts2.dll`을 직접 디컴파일했다.

가장 중요한 수확은 `ModManager`였다.

## 12. `ModManager` 디컴파일로 로더 규칙 확정

`ModManager`를 디컴파일해서 다음 규칙을 확인했다.

- 게임은 `<game dir>\\mods`를 연다
- `.pck`를 재귀적으로 찾는다
- 같은 basename의 `.dll`을 찾는다
- `ProjectSettings.LoadResourcePack`을 호출한다
- `res://mod_manifest.json`이 반드시 필요하다
- `pck_name`은 확장자 없는 basename과 같아야 한다
- `ModInitializerAttribute`가 없으면 `Harmony.PatchAll`을 호출한다

이 시점부터는 네이티브 `mods + pck + dll + txt`가 사실상 정답이라고 볼 수 있었다.

## 13. 모드 동의 플래그 확인

다음으로 디컴파일한 것은 `ModSettings`와 mod warning UI였다.

여기서 `settings.save` 안의 `mod_settings.mods_enabled`가 실제 게이트라는 걸 확인했다.

처음 네이티브 `.pck`를 넣었을 때 로더가 모드를 건너뛴 이유도 여기서 설명됐다.

## 14. `.pck`를 어떻게 만들지 고민

처음에는 `.pck`가 없는 것이 가장 큰 blocker였다.

시도한 방법은 두 가지였다.

1. Godot `PCKPacker`
2. 공식 `--export-pack`

## 15. `PCKPacker` 시도와 실패

우선 headless Godot 프로젝트를 만들어 `PCKPacker`로 `.pck`를 생성했다.

겉보기에는 성공했지만, STS2 로더는 이 파일을 읽고도 `mod manifest가 없다`고 판단했다.

공식 Godot 쪽에서 직접 inspect 하면 `res://mod_manifest.json`이 보였기 때문에, STS2 로더가 기대하는 형태와 raw `PCKPacker` 출력이 완전히 같지는 않다는 결론을 냈다.

## 16. 공식 `--export-pack` 경로 확인

그래서 Godot 4.5.1 공식 콘솔 에디터를 내려받아 `--export-pack "Windows Desktop"`를 시험했다.

이 경로는 실제로 동작했다.

그리고 여기서 두 번째 중요한 오류를 잡았다.

오류:

- `PCK name in mod manifest ... does not match`

원인:

- `pck_name`에 `.pck` 확장자를 넣고 있었다

수정:

- `pck_name = Path.GetFileNameWithoutExtension(pckName)`

## 17. 네이티브 모드 실기 첫 성공

manifest 수정 후 다시 배치했을 때, 로그는 다음 단계까지 갔다.

- `.pck` 발견
- DLL 로드
- `Harmony.PatchAll` 호출
- `Finished mod initialization`
- `--- RUNNING MODDED! ---`

그리고 저장 경로가 `modded/profile1`로 분리되는 것까지 확인했다.

## 18. 첫 실제 payload 추가

이제 로더가 확실히 살아 있으므로 첫 payload를 넣었다.

현재 넣은 첫 패치는 다음과 같다.

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`

설정 소스는 다음 두 가지다.

- `STS2_SPEED_*` 환경 변수
- `Sts2Speed.speed.txt`

## 19. 추가 DLL 의존성 문제

여기서 또 한 번 막혔다.

`sts2-speed-skeleton.dll`은 로드됐지만, 그 DLL이 참조하는 `Sts2Speed.Core.dll`은 못 찾았다.

로그에는 `Could not load file or assembly 'Sts2Speed.Core'`가 찍혔다.

원인:

- STS2 로더는 “모드 엔트리 DLL”은 로드하지만, 거기서 이어지는 추가 DLL probing은 자동으로 안 해줬다

수정:

- `ModAssemblyResolver.cs` 추가
- 모드 폴더 기준 assembly resolve 등록

## 20. 런타임 설정 로그 추가

게임 로그만으로는 우리 내부 상태를 보기 불편했다.

그래서 `mods\\sts2speed.runtime.log` 파일을 별도로 남기도록 바꿨다.

이 로그는 다음을 기록한다.

- 초기 설정
- 설정 변경 감지
- 패치가 실제로 한 번이라도 적용된 순간

## 21. 저장 데이터가 사라진 것처럼 보인 문제

이건 실제로 겪은 문제였다.

모드를 켜고 나서 게임이 `modded/profile1`을 보기 시작하는데, 그쪽에 진행 데이터가 없어서 완전히 초기화된 프로필처럼 보였다.

실제 확인 결과:

- vanilla `profile1\\saves\\progress.save` 는 정상
- `modded\\profile1\\saves\\progress.save` 는 사실상 새 프로필 상태

즉 데이터가 증발한 게 아니라 슬롯이 분리된 것이었다.

## 22. 실제 복구

복구는 다음 순서로 진행했다.

1. 기존 `modded/profile1` 백업
2. `profile1` 전체를 `modded/profile1`에 복제
3. `progress.save` 해시 비교

결과:

- source hash와 destination hash가 동일해졌다
- 진행 데이터가 modded 슬롯으로 복구됐다

## 23. 복구 자동화

같은 문제가 반복되지 않게 `sync-modded-profile` 명령을 추가했다.

이 명령은 다음을 수행한다.

1. 기존 `modded/profileN` 전체를 `artifacts/profile-sync-backups/<timestamp>/` 아래에 백업
2. `profileN` 내용을 `modded/profileN`으로 미러링
3. 보고서 JSON 작성

즉 앞으로는 수동 복사 대신 툴로 처리할 수 있다.

## 24. 현재 실기 상태

현재 실기에서 확정된 것은 다음이다.

- 네이티브 로더가 실제로 작동한다
- `.pck` 생성 경로는 `--export-pack`로 확정됐다
- `Sts2Speed.speed.txt` 값이 런타임 설정 로그에 실제로 반영된다
- 추가 DLL 의존성 문제는 assembly resolver로 해결됐다
- vanilla 진행을 modded 프로필로 복구할 수 있다

아직 남은 것은 다음이다.

- 실제 전투에서 `spine time scale applied` / `queue wait scale applied` 로그가 찍히는지 확인
- 체감 강도를 조정할지 판단

## 25. 배속 의미 버그 발견

첫 payload를 넣은 뒤 설명 문서를 쓰면서 중요한 버그를 발견했다.

초기 구현은 `Sts2Speed.speed.txt = 2.0`일 때:

- Spine 애니메이션은 `x 2.0`
- queue wait / effect delay도 그대로 `x 2.0`

으로 처리하고 있었다.

문제는 wait / timer 계열은 "속도"가 아니라 "지속시간"이라서, 이 계산은 사용자 기대와 반대로 동작한다는 점이다.

즉:

- 애니메이션은 빨라지지만
- 대기시간은 길어진다

이건 "배속을 올리면 전체가 빨라져야 한다"는 사용자 의미와 맞지 않았다.

## 26. speed semantics 수정

이 문제를 고치기 위해 공통 계산 로직을 분리했다.

추가한 파일:

- `src/Sts2Speed.Core/Configuration/SpeedScaleMath.cs`

핵심 규칙:

- animation = `value * multiplier`
- duration = `value / multiplier`

즉 현재 `2.0`의 의미는 다음과 같이 고정됐다.

- 애니메이션 2배속
- wait / timer 절반 길이

이후 `RuntimePatchContext`는 직접 곱셈하지 않고 `SpeedScaleMath`를 호출하도록 바꿨다.

## 27. self-test 추가

이 버그가 다시 생기지 않게 self-test를 추가했다.

테스트 포인트:

- `2.0` speed multiplier는 float wait duration을 `0.5`로 줄여야 한다
- `0.5` speed multiplier는 double wait duration을 `2.0`으로 늘려야 한다
- 잘못된 `0.0` multiplier는 duration을 그대로 둬야 한다

즉 지금은 semantics가 코드와 테스트 양쪽에서 고정되어 있다.

## 28. 기본 배속을 2.0으로 상향

원래 생성 기본값은 `1.0`이었다.

이후 사용자 요청에 맞춰:

- 생성되는 `Sts2Speed.speed.txt` 기본값
- live `mods` 폴더의 `Sts2Speed.speed.txt`

둘 다 `2.0`으로 올렸다.

따라서 현재 기준 기본 배포 상태는 "2배속 의도"다.

## 29. 현재 판단

지금 시점의 결론은 명확하다.

- GUMM은 유효한 진단 경로였다
- 최종 경로는 STS2 내장 네이티브 로더다
- `.pck`는 공식 Godot export로 만들 수 있다
- 첫 실제 payload는 이미 라이브 로더까지 통과했다
- 저장 데이터가 사라진 것처럼 보이는 문제는 `profileN`과 `modded/profileN` 분리 문제다
- speed semantics는 수정돼서 `2.0`의 의미가 현재 일관적이다
- 다음 단계는 플레이 기반 검증과 추가 후보 훅 확장이다
