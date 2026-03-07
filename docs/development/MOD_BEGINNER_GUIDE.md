# STS2 Speed Mod 초보자 안내서

## 1. 이 저장소는 지금 무엇인가

이 저장소는 아직 "슬더스2 전체를 빠르게 만드는 완성 모드"가 아닙니다.

현재 단계는 두 가지입니다.

1. 실험 전에 설치본과 세이브를 안전하게 백업하고 복구할 수 있게 만든다.
2. 나중에 속도 패치를 꽂아 넣을 수 있도록 모드 패키지와 로더 경로를 검증한다.

즉, 지금 저장소의 역할은 "속도 패치 자체"보다 "속도 패치를 안전하게 실험할 수 있는 기반"에 더 가깝습니다.

## 2. 지금 실제로 되는 일

현재 이 저장소가 실제로 하는 일은 다음과 같습니다.

- 설정 파일과 환경 변수를 읽어서 모드 쪽 설정을 구성한다.
- 중요 파일을 `artifacts/snapshots/...` 아래에 백업한다.
- 백업한 파일의 SHA-256 해시를 기록한다.
- 현재 상태와 백업 상태를 비교해 변경 여부를 검증한다.
- 필요하면 백업본으로 복구한다.
- `artifacts/package-layout/Sts2Speed` 아래에 실제 모드 패키지를 생성한다.
- GUMM가 읽을 수 있는 `mod.cfg`, `mod.gd`, `GUMM_mod.gd`를 만든다.
- GUMM 로더 설치용 `override.cfg`, `GUMM_mod_loader.tscn` 통합 절차를 제공한다.
- Steam `-applaunch 2868840`로 게임을 실행시키는 테스트 런처를 생성한다.
- `runtime-overrides.json`을 써서 테스트 프로필 값을 모드 부트스트랩까지 전달한다.

## 3. 지금 아직 안 되는 일

아직 구현되지 않은 것은 명확합니다.

- 게임 속도를 실제로 바꾸는 런타임 패치
- `MegaAnimationState.SetTimeScale` 같은 후보 메서드에 대한 실제 후킹
- `queueWaitScale`, `effectDelayScale`가 전투 흐름에 영향을 주는 코드
- 내장 모딩 루트가 이 PC에서 실제로 열려 있는지에 대한 최종 확정

따라서 `spine125`, `queue085` 같은 프로필은 현재 "실제 속도 변화"보다 "이 값이 모드까지 도달하는지"를 검증하는 용도입니다.

## 4. 핵심 설정은 게임 설정이 아니라 모드 설정이다

핵심 파일:

- [WorkspaceConfiguration.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Configuration/WorkspaceConfiguration.cs)

여기서 정의하는 값은 게임 내부의 공식 옵션이 아니라, 우리가 만든 모드 설정 인터페이스입니다.

- `enabled`
- `fastModeOverride`
- `animationScale`
- `spineTimeScale`
- `queueWaitScale`
- `effectDelayScale`
- `combatOnly`
- `preserveGameSettings`
- `verboseLogging`

중요한 점:

- `spineTimeScale`은 게임이 원래 제공하는 공개 옵션이라고 확인된 값이 아닙니다.
- 이 값은 우리가 나중에 연결하고 싶은 훅 후보와 맞추기 위해 만든 모드 설정 이름입니다.
- 반대로 `fast_mode` 같은 값은 실제 세이브/설정 파일에서 확인한 게임 저장 키입니다.

## 5. 왜 `spineTimeScale`을 먼저 잡았는가

핵심 파일:

- [KnownPatchTargets.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/KnownPatchTargets.cs)

현재 저장소는 여러 후보 훅 지점을 카탈로그로 들고 있습니다.

- `MegaCrit.Sts2.Core.Bindings.MegaSpine.MegaAnimationState.SetTimeScale`
- `MegaCrit.Sts2.Core.Bindings.MegaSpine.MegaTrackEntry.SetTimeScale`
- `MegaCrit.Sts2.Core.Combat.CombatManager.WaitForActionThenEndTurn`
- `MegaCrit.Sts2.Core.Combat.CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
- `MegaCrit.Sts2.Core.Combat.CombatState.GodotTimerTask`
- `MegaCrit.Sts2.Core.GameActions.ActionExecutor.ExecuteActions`

이 중 `spineTimeScale`은 첫 번째 두 후보와 가장 직접적으로 연결됩니다.

그래서 현재 테스트 축도 다음 순서로 설계돼 있습니다.

1. `spineTimeScale`
2. `queueWaitScale`
3. `effectDelayScale`

## 6. 설정이 실제로 적용되는 순서

핵심 파일:

- [SettingsLoader.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Configuration/SettingsLoader.cs)
- [EnvironmentOverrides.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Configuration/EnvironmentOverrides.cs)

설정 우선순위는 다음과 같습니다.

1. 하드코딩 기본값
2. JSON 설정 파일
3. 환경 변수 오버라이드

다만 실제 게임을 Steam으로 실행하면 환경 변수만으로는 제어가 불안정합니다. 그래서 지금은 테스트 런처가 `runtime-overrides.json`을 쓰고, `mod.gd`가 그 파일을 읽는 방식이 주 경로입니다.

## 7. 백업과 롤백은 왜 중요한가

핵심 파일:

- [SnapshotPlanning.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/SnapshotPlanning.cs)
- [SnapshotExecution.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/SnapshotExecution.cs)

이 저장소에서 가장 먼저 믿을 수 있어야 하는 코드는 백업과 복구입니다.

백업 대상:

- `release_info.json`
- `override.cfg`
- `GUMM_mod_loader.tscn`
- `settings.save`
- `settings.save.backup`
- `prefs.save`
- `prefs.save.backup`
- `progress.save`
- `progress.save.backup`
- `current_run.save`
- `current_run.save.backup`

왜 이게 중요하냐면, 지금은 모드가 완성되지 않았기 때문입니다. 실험 중간에 실패할 가능성이 높으므로, 실패했을 때 "무조건 원상복구가 가능하다"가 먼저 보장돼야 합니다.

## 8. 패키지를 만드는 핵심 코드

핵심 파일:

- [PackageMaterialization.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.ModSkeleton/PackageMaterialization.cs)

이 파일이 실제로 하는 일:

1. `manifest.json` 생성
2. `mod.cfg` 생성
3. `config/sts2speed.sample.json` 생성
4. `config/runtime-overrides.json` 생성
5. `README.txt` 생성
6. `scripts/test-profiles.json` 생성
7. `scripts/Start-Sts2SpeedTest.ps1` 생성
8. `mod.gd` 생성
9. `GUMM_mod.gd` 생성 또는 GUMM 4.x 시스템 스크립트 복사
10. 빌드된 DLL/PDB를 `bin/`에 복사

이 파일이 현재 저장소에서 가장 중요한 이유는, "코드가 실제 실행 가능한 모드 패키지 형태를 갖추는 지점"이기 때문입니다.

## 9. GUMM 경로는 지금 어떻게 동작하는가

핵심 파일:

- [GummIntegration.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/GummIntegration.cs)

현재 실기 기준으로 검증된 경로는 `mods` 폴더 추정치가 아니라 GUMM 방식입니다.

GUMM 방식은 다음처럼 동작합니다.

1. 게임 폴더에 `override.cfg`를 둔다.
2. 게임 폴더에 `GUMM_mod_loader.tscn`를 둔다.
3. `override.cfg`의 `run/main_scene`를 `res://GUMM_mod_loader.tscn`로 바꾼다.
4. 원래 게임 진입 씬인 `res://scenes/game.tscn`는 `gumm/main_scene`로 보존한다.
5. `gumm/mod_list`에 모드 패키지 루트를 `PackedStringArray(...)` 형태로 넣는다.
6. 게임 시작 시 GUMM 로더가 각 모드의 `mod.cfg`와 `mod.gd`를 읽는다.

즉, 지금 실기에서 검증된 로더 경로는 "게임이 스스로 `mods` 폴더를 찾는다"가 아니라, "GUMM 로더가 대신 모드를 읽어준다"입니다.

## 10. `mod.gd`가 지금 하는 일

생성 파일:

- [mod.gd](C:/Users/jidon/source/repos/STS2ModeTest/artifacts/package-layout/Sts2Speed/mod.gd)

현재 `mod.gd`는 속도를 바꾸지 않습니다.

지금 하는 일은 다음뿐입니다.

- `config/runtime-overrides.json` 읽기
- `STS2_SPEED_*` 환경 변수 읽기
- 읽은 값을 로그에 출력하기

이렇게 해두는 이유:

- 실제 속도 패치를 넣기 전에 "모드가 로드된다"는 사실을 먼저 증명해야 하기 때문입니다.
- 현재는 `Loading mod: STS2 Speed Skeleton`과 함께 `runtime={...}` 로그가 찍히는 것으로 1차 검증을 끝낸 상태입니다.

## 11. 왜 `GUMM_mod.gd`를 직접 만들어 넣었다가 다시 바꿨는가

처음에는 `GUMM_mod.gd`를 최소 기능만 가진 얇은 스크립트로 생성했습니다.

하지만 실기에서 바로 문제가 났습니다.

- `mod.gd`가 `get_full_path()`를 호출했는데, 우리가 만든 얇은 베이스 스크립트에는 그 함수가 없었습니다.
- 결과적으로 GDScript parse error가 발생했고, 모드는 로드되지만 실행되지 않았습니다.

이 문제를 고친 방식:

- GUMM 저장소의 실제 `System/4.x/GUMM_mod.gd`를 패키지에 복사하도록 변경
- 저장소가 없을 때만 fallback 스크립트를 사용

즉, 이 저장소는 이제 "우리가 추정한 GUMM 베이스"가 아니라 "가능하면 실제 GUMM 4.x 베이스"를 패키지에 넣습니다.

## 12. 테스트 런처가 왜 Steam으로만 실행하는가

생성 파일:

- [Start-Sts2SpeedTest.ps1](C:/Users/jidon/source/repos/STS2ModeTest/artifacts/package-layout/Sts2Speed/scripts/Start-Sts2SpeedTest.ps1)

처음에는 `SlayTheSpire2.exe`를 직접 실행했습니다.

그런데 실제 로그에서 다음 문제가 확인됐습니다.

- Steam appID를 찾지 못해 Steam 초기화 실패

그래서 지금 런처는 다음 순서로 동작합니다.

1. 선택한 테스트 프로필 읽기
2. `runtime-overrides.json` 쓰기
3. 필요하면 `STS2_SPEED_*` 환경 변수도 설정
4. `steam.exe -applaunch 2868840` 실행

즉, 지금은 "직접 exe 실행"이 아니라 "Steam 경유 실행"이 정답입니다.

## 13. 초보자가 가장 먼저 읽어야 할 코드

아래 파일 네 개를 먼저 보면 저장소 구조가 잡힙니다.

1. [SnapshotExecution.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/SnapshotExecution.cs)
2. [PackageMaterialization.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.ModSkeleton/PackageMaterialization.cs)
3. [GummIntegration.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/GummIntegration.cs)
4. [Program.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Tool/Program.cs)

이 네 파일이 각각 맡는 역할:

- 백업/복구
- 패키지 생성
- GUMM 로더 설치
- CLI 실행 흐름

## 14. 현재 기준 안전한 사용 순서

추천 순서는 다음과 같습니다.

1. `dotnet run --project src/Sts2Speed.Tool -- show-config`
2. `dotnet run --project src/Sts2Speed.Tool -- snapshot --snapshot-root artifacts/snapshots/<name>`
3. `dotnet run --project src/Sts2Speed.Tool -- verify-snapshot --snapshot-root artifacts/snapshots/<name>`
4. `dotnet run --project src/Sts2Speed.Tool -- materialize-package`
5. `dotnet run --project src/Sts2Speed.Tool -- install-gumm-loader`
6. `artifacts/package-layout/Sts2Speed/scripts/Start-Sts2SpeedTest.ps1 -Profile vanilla`
7. 로그에서 `Loading mod: STS2 Speed Skeleton` 확인
8. 그 다음에만 `spine125` 같은 프로필을 시도

## 15. 지금 시점의 솔직한 결론

현재 저장소는 다음 두 가지를 이미 증명했습니다.

- 안전한 실험 기반은 준비됐다.
- GUMM 경유 모드 부트스트랩은 실기에서 실제로 로드된다.

하지만 다음 한 단계는 아직 남아 있습니다.

- `runtime-overrides.json` 값을 실제 속도 변화 코드에 연결하는 것

즉, 기반 공사는 끝났고, 이제부터가 진짜 속도 패치 작업입니다.
