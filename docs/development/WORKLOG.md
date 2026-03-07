# STS2 Speed Mod 작업 기록

이 문서는 실제 조사와 구현 과정을 시간 순서대로 남깁니다. 성공한 경로뿐 아니라 실패한 경로도 일부러 기록합니다.

## 1. 시작 조건 정리

처음부터 고정한 제약은 다음과 같았습니다.

- 게임이 실행 중일 수 있으므로 라이브 설치본을 바로 수정하지 않는다.
- 첫 플레이 감상용 옵션은 보존한다.
- 애니메이션/대기시간 가속형 접근을 우선한다.
- 공식 모딩 루트가 확실하지 않다면 바이너리 패치는 미룬다.

그래서 첫 결과물도 "완성 모드"가 아니라 "실험용 툴킷 + 뼈대"로 설계했습니다.

## 2. 커뮤니티 상황 먼저 확인

가장 먼저 한 일은 "지금 STS2 모딩이 실제로 움직이고 있는가" 확인하는 것이었습니다.

확인한 내용:

- 출시 직후부터 커뮤니티에서 이미 STS2 모드 시도가 시작됨
- Mega Crit 인터뷰에서는 STS1보다 모딩 진입이 쉬워질 것이라는 방향성이 보였음
- Steam 토론에서는 Workshop이 전부가 아니라는 신호가 있었음
- `sts2mods.com` 같은 커뮤니티 허브가 이미 생겨 있었음

결론:

- 정식 문서만 기다리지 말고
- 로컬 설치본 분석 + 커뮤니티 로더 조사
- 두 갈래를 동시에 진행

## 3. 로컬 설치본 구조 확인

게임 폴더와 사용자 데이터 폴더를 직접 확인했습니다.

확인된 경로:

- 게임 설치: `D:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2`
- 사용자 데이터: `C:\Users\jidon\AppData\Roaming\SlayTheSpire2`

여기서 확인한 핵심 파일:

- `SlayTheSpire2.exe`
- `SlayTheSpire2.pck`
- `data_sts2_windows_x86_64\sts2.dll`
- `data_sts2_windows_x86_64\sts2.runtimeconfig.json`
- `data_sts2_windows_x86_64\sts2.deps.json`

이 단계에서 내린 결론:

- 슬더스1의 Java 모딩 감각으로 접근하면 안 된다.
- 슬더스2는 Godot + .NET 구조다.

## 4. 세이브와 설정 파일 확인

사용자 데이터 폴더에서 실제 저장 파일을 확인했습니다.

확인된 파일:

- `settings.save`
- `settings.save.backup`
- `prefs.save`
- `prefs.save.backup`
- `progress.save`
- `progress.save.backup`
- `current_run.save.backup`

관찰:

- `current_run.save`는 당시 없었고 `.backup`만 있었다.

이게 왜 중요했는가:

- 스냅샷에서 "파일이 없음"을 오류가 아니라 정상 상태로 처리해야 한다는 뜻이었기 때문입니다.

이 판단이 [SnapshotExecution.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/SnapshotExecution.cs)의 `missing`, `still-missing` 상태 설계로 이어졌습니다.

## 5. 설정 이름을 어떻게 잡았는가

`spineTimeScale`, `queueWaitScale`, `effectDelayScale` 같은 이름은 게임 내부에서 읽어온 공식 프로퍼티가 아닙니다.

실제 과정은 두 갈래였습니다.

1. 게임 세이브/설정 파일에서 실제 저장 키를 조사
2. DLL/메타데이터 조사로 나중에 후킹할 후보 메서드를 추정

그 다음 그 둘을 연결하기 쉬운 "모드 전용 설정 이름"을 직접 정했습니다.

즉:

- `fast_mode`는 실제 세이브 키
- `spineTimeScale`은 우리가 만든 모드 설정

## 6. DLL과 런타임 메타데이터 조사

여기서 한 일은 단순 문자열 검색이 아니라, 실제 설치본 내부 구조를 파악하는 작업이었습니다.

확인한 파일:

- `sts2.runtimeconfig.json`
- `sts2.deps.json`
- `sts2.dll`

여기서 얻은 사실:

- 타깃 프레임워크는 `.NET 9.0`
- `GodotSharp` 존재
- `0Harmony` 존재
- `Steamworks.NET` 존재

이 사실이 의미하는 것:

- 관리 코드 기반 접근이 가능하다
- Harmony 계열 패치 가능성이 높다
- Steam 초기화가 런타임에서 중요하다

## 7. 필요한 타입 이름은 어떻게 알아냈는가

후보 심볼은 여러 방법으로 모았습니다.

- 로컬 설치본 메타데이터 조사
- DLL 문자열/타입 이름 확인
- 관련 JSON 메타데이터 확인

그 결과 기록한 대표 후보:

- `MegaCrit.Sts2.Core.Modding.ModManager`
- `MegaCrit.Sts2.Core.Modding.ModManifest`
- `MegaCrit.Sts2.Core.Modding.ModInitializerAttribute`
- `MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen.NModdingScreen`

그리고 속도 관련 후보 메서드:

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `CombatManager.WaitForActionThenEndTurn`
- `CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
- `CombatState.GodotTimerTask`
- `ActionExecutor.ExecuteActions`

중요:

- 이것은 "실제 패치 성공"이 아니라 "후보 훅 포인트 수집"입니다.

## 8. PowerShell reflection 시도와 실패

다음으로 시도한 것은 PowerShell에서 `sts2.dll`을 직접 반사(reflection)로 여는 것이었습니다.

시도한 것:

- 어셈블리 로드 후 타입 열거
- 특정 타입 직접 조회
- `MetadataLoadContext` 활용 시도

왜 실패했는가:

- 대상은 `.NET 9.0`인데 로컬 개발 환경은 그와 완전히 맞지 않았음
- clean reflection을 하려면 더 많은 런타임 의존성이 필요했음
- `MetadataLoadContext` 경로도 현 환경에서는 바로 쓰기 어려웠음

이 실패가 준 교훈:

- 타입 이름을 얻었다고 해서 "로더 경로가 증명됐다"고 말하면 안 된다.
- 그래서 저장소 코드에서도 확정된 사실과 가정된 계약을 분리해 적기 시작했습니다.

## 9. 모드 경로 추정의 한계

처음에는 게임이 `mods` 폴더나 Workshop 폴더를 바로 읽을 거라고 기대했습니다.

그래서 다음을 확인했습니다.

- `godot.log`
- 사용자 데이터 아래 `mods`
- 게임 폴더 아래 `mods`
- `steamapps/workshop/content/2868840`

하지만 문제:

- 로그에 정확한 scan path가 안 나옴
- 게임이 직접 만든 명확한 `mods` 폴더도 없음

그래서 [ModPathDiscovery.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/ModPathDiscovery.cs)는 지금도 보수적으로 동작합니다.

- 정확한 로그 근거가 있으면 추천
- 없으면 후보만 출력
- 추천 경로는 비움

## 10. 백업 시스템 구현

경로가 불확실할수록 롤백이 중요해졌습니다.

그래서 dry-run만 있던 상태에서 실제 백업 기능을 넣었습니다.

추가한 명령:

- `snapshot`
- `verify-snapshot`
- `restore`

백업 보고서에는:

- 파일 크기
- SHA-256
- 복사 여부
- missing 여부

를 기록합니다.

## 11. 첫 실제 스냅샷

실제 스냅샷을 만든 위치:

- [first-live-test](C:/Users/jidon/source/repos/STS2ModeTest/artifacts/snapshots/first-live-test)

이 단계에서 확인한 점:

- 주요 파일은 정상 복사됨
- `current_run.save` 부재는 정상 처리됨
- 검증 결과 `allEntriesMatch=true`

이 시점부터 라이브 실험을 해도 복구 수단이 있는 상태가 됐습니다.

## 12. 패키지 생성은 dry-run에서 실제 생성으로 전환

초기에는 패키지 계획만 있었고 실제 파일은 없었습니다.

하지만 실기 검증을 하려면 실제 패키지가 있어야 했습니다.

그래서 [PackageMaterialization.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.ModSkeleton/PackageMaterialization.cs)에 다음을 넣었습니다.

- 실제 파일 쓰기
- DLL/PDB 복사
- 테스트 프로필 생성
- PowerShell 런처 생성
- `mod.cfg`, `mod.gd`, `GUMM_mod.gd` 생성

## 13. 테스트 프로필 설계

처음부터 강하게 밀지 않기 위해 프로필을 단계형으로 만들었습니다.

- `vanilla`
- `spine125`
- `spine150`
- `spine175`
- `queue085`
- `queue070`
- `effect085`
- `effect070`

의도:

- 한 번에 하나의 위험 요소만 추가
- 실패 지점을 빠르게 좁히기

## 14. 직접 exe 실행이라는 잘못된 길

실제 게임 실행 검증에서 처음에는 `SlayTheSpire2.exe`를 직접 실행했습니다.

결과:

- Steam 초기화 실패
- 로그에 `No appID found...` 계열 메시지 출력

이 단계에서 분명해진 점:

- 직접 exe 실행은 기본 경로가 아니다.
- `steam_appid.txt`를 라이브 설치에 추가하지 않는 이상 잘못된 접근이다.

그래서 테스트 런처를 수정해 `steam.exe -applaunch 2868840`를 쓰도록 바꿨습니다.

## 15. Steam 실행이 또 다른 문제를 드러냄

Steam 경유 실행은 메인 메뉴 진입까지 성공했습니다.

하지만 곧 다른 문제가 드러났습니다.

- PowerShell에서 잡은 환경 변수가 Steam을 거치며 게임까지 그대로 간다고 확신할 수 없었습니다.

그래서 제어 채널을 바꿨습니다.

- 환경 변수만 사용
- 에서
- `runtime-overrides.json` + 환경 변수 보조

로 변경했습니다.

## 16. `runtime-overrides.json` 추가

이 변경으로 테스트 런처는 실행 전에 다음을 합니다.

1. 선택한 프로필 읽기
2. `config/runtime-overrides.json` 기록
3. 필요하면 환경 변수도 세팅
4. Steam applaunch 실행

그리고 `mod.gd`는 이 파일을 읽어 로그에 출력합니다.

이제 중요한 것은 "Steam이 환경 변수를 전달했는가"가 아니라, "모드가 런타임 오버라이드 파일을 읽었는가"가 됐습니다.

## 17. 로그 락 문제

게임이 실행 중일 때 `godot.log`를 읽으려다가 문제가 생겼습니다.

증상:

- `discover-mod-path` 실행 시 로그 파일 접근 실패

원인:

- 게임 프로세스가 로그 파일을 잡고 있었음

수정:

- [ModPathDiscovery.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/ModPathDiscovery.cs)에서 `FileShare.ReadWrite | FileShare.Delete`로 읽게 변경

이후에는 게임 실행 중에도 로그를 읽을 수 있게 됐습니다.

## 18. 커뮤니티 GUMM 조사

정확한 mod root를 못 찾는 상태에서 커뮤니티 경로를 더 깊게 봤습니다.

한 일:

- GUMM 저장소 클론
- README 읽기
- 예제 모드 확인
- `System/4.x` 스크립트 구조 확인

여기서 얻은 핵심 사실:

- GUMM은 일반적인 `mods` 폴더 자동 탐색과 다르게 동작
- 실제 설치 시 게임 폴더에 `override.cfg`, `GUMM_mod_loader.tscn`가 들어감
- 모드는 원래 위치를 유지한 채 `mod_list`로 참조됨

즉, 이전의 `deploy-package --mod-root ...`는 보조 경로이고, 실기 기준 주 경로는 GUMM 로더 설치였습니다.

## 19. GUMM 설치 기능 추가

그래서 [GummIntegration.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/GummIntegration.cs)를 추가했습니다.

이 코드가 하는 일:

- `override.cfg` 생성 또는 병합
- `run/main_scene`를 GUMM 로더 씬으로 변경
- 원래 메인 씬 `res://scenes/game.tscn` 보존
- `mod_list`에 우리 패키지 루트 등록
- `GUMM_mod_loader.tscn` 복사

## 20. 라이브 설치 전 스냅샷 재확장

GUMM 로더를 실제 게임 폴더에 설치하게 되면서 백업 대상도 늘어났습니다.

추가한 백업 대상:

- `override.cfg`
- `GUMM_mod_loader.tscn`

이 변경은 [SnapshotPlanning.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/SnapshotPlanning.cs)에 반영했습니다.

## 21. 첫 GUMM 라이브 설치

실제로 다음 두 파일만 게임 폴더에 추가했습니다.

- `override.cfg`
- `GUMM_mod_loader.tscn`

확인:

- 세이브 파일 해시는 그대로 유지
- 설치본 변경은 의도한 두 파일뿐

## 22. 첫 GUMM 부팅 실패

라이브 부팅 직후 로그에서 실제 오류를 잡았습니다.

오류 내용:

- `Loading mod: STS2 Speed Skeleton`
- 그 직후 `mod.gd` parse error
- `get_full_path()`를 base self에서 찾지 못함

원인:

- 우리가 직접 생성한 `GUMM_mod.gd`가 너무 얇았음
- GUMM 예제 모드가 기대하는 helper 함수들이 없었음

이건 아주 중요한 시행착오였습니다.

왜냐하면:

- "모드가 발견된다"와
- "모드가 실제로 실행된다"

는 완전히 다른 문제라는 것이 여기서 드러났기 때문입니다.

## 23. GUMM 베이스 스크립트 수정

이 문제를 고친 방법:

- [PackageMaterialization.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.ModSkeleton/PackageMaterialization.cs)에서
- 가능하면 `artifacts/tools/Godot-Universal-Mod-Manager/System/4.x/GUMM_mod.gd`를 그대로 복사
- 없으면 fallback 스크립트 사용

추가로 self-test에도 `get_full_path()` 존재 여부를 넣었습니다.

즉, 이제는 패키징 단계에서부터 "실제 GUMM 4.x 베이스를 패키지에 포함시키는가"를 검증합니다.

## 24. 두 번째 라이브 부팅 성공

수정 후 다시 패키징하고 Steam으로 실행했습니다.

로그에서 확인한 사실:

- `Loading mod: STS2 Speed Skeleton`
- `STS2 Speed Skeleton GUMM bootstrap loaded. runtime={ "profile": "vanilla", ... }`
- 메인 메뉴까지 정상 진입

그리고 `spine125` 프로필에서도:

- `runtime={ "profile": "spine125", "enabled": true, "spineTimeScale": 1.25, ... }`

가 실제로 찍혔습니다.

이 단계에서 증명된 것:

- GUMM가 우리 모드를 읽는다
- `mod.gd`가 실제로 실행된다
- `runtime-overrides.json` 값이 부트스트랩까지 전달된다

## 25. 아직 남아 있는 경고

현재 로그에 남아 있는 이슈:

- GUMM 로더 쪽 `remove_child()` 타이밍 경고

현재 판단:

- 메인 메뉴 진입은 성공
- 세이브 추적 파일 해시는 변하지 않음
- 따라서 즉시 치명적인 blocker는 아님

하지만 나중에 전투 진입 테스트 전에 한 번 더 검토할 가치가 있습니다.

## 26. 지금 시점의 사실 정리

현재 확정된 사실:

- 백업/복구는 실기 기준으로 동작한다.
- GUMM 설치는 실기 기준으로 동작한다.
- GUMM 부트스트랩은 실기 기준으로 로드된다.
- `runtime-overrides.json` 프로필 전달은 실기 기준으로 동작한다.
- 세이브 추적 파일은 현재까지 변하지 않았다.

현재 아직 미해결:

- 실제 속도 패치 구현
- 내장 C# 모딩 루트 확정
- GUMM 경로와 내장 루트 중 최종 선택

## 27. 다음 구현자가 헷갈리기 쉬운 포인트

다음 혼동을 피해야 합니다.

1. `spineTimeScale`은 게임 공식 설정이 아니다.
2. DLL에서 본 타입 이름은 실제 로더 성공 증명이 아니다.
3. `Loading mod` 로그가 떠도 `mod.gd`가 parse error면 모드는 사실상 실패다.
4. Steam 경유 실행에서는 환경 변수만으로 제어하려고 하면 안 된다.
5. 지금 기준 실기에서 검증된 경로는 `deploy-package --mod-root`가 아니라 GUMM 로더 설치다.

## 28. 다음 단계

이제 실제로 남은 일은 하나입니다.

- `runtime-overrides.json` 값을 실제 속도 변경 코드에 연결하는 것

구체적으로는 다음 둘 중 하나입니다.

1. GUMM 스크립트에서 추가 로더를 호출해 게임 내부 코드에 접근
2. 내장 C# 모딩 루트를 더 검증해서 DLL 엔트리포인트를 실제로 진입시키기

현재까지는 1번 경로가 먼저 살아 있음을 실기에서 확인했습니다.
