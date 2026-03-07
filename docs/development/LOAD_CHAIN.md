# STS2 Speed Toolkit 동작 원리

이 문서는 "이 저장소가 실제로 어떤 경로로 동작하는가"를 처음 보는 사람 기준으로 설명합니다.

## 전체 흐름

```text
CLI 명령 실행
-> 설정 로딩
-> 스냅샷 생성
-> 패키지 생성
-> GUMM 로더 설치
-> Steam으로 게임 실행
-> GUMM가 mod.gd 로드
-> mod.gd가 runtime-overrides.json 읽음
-> 로그로 로드 여부 검증
```

## 1. CLI가 시작점이다

핵심 파일:

- [Program.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Tool/Program.cs)

이 파일이 각 서브 명령을 적절한 코드에 연결합니다.

- `show-config`
- `materialize-package`
- `snapshot`
- `verify-snapshot`
- `restore`
- `materialize-gumm-game-entry`
- `install-gumm-loader`

즉, 실제 작업 순서를 사람이 매번 수동으로 조립하지 않게 해주는 진입점입니다.

## 2. 설정은 세 겹으로 합쳐진다

핵심 파일:

- [WorkspaceConfiguration.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Configuration/WorkspaceConfiguration.cs)
- [SettingsLoader.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Configuration/SettingsLoader.cs)
- [EnvironmentOverrides.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Configuration/EnvironmentOverrides.cs)

설정 흐름:

1. 기본값 로딩
2. JSON 설정 반영
3. 환경 변수 오버라이드 반영

하지만 실제 실행에서는 Steam이 중간에 끼기 때문에 환경 변수만 믿을 수 없습니다. 그래서 테스트 런처는 `runtime-overrides.json`을 추가로 씁니다.

## 3. 왜 `runtime-overrides.json`이 필요한가

핵심 파일:

- [PackageMaterialization.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.ModSkeleton/PackageMaterialization.cs)
- [Start-Sts2SpeedTest.ps1](C:/Users/jidon/source/repos/STS2ModeTest/artifacts/package-layout/Sts2Speed/scripts/Start-Sts2SpeedTest.ps1)

처음에는 `STS2_SPEED_*` 환경 변수만으로 프로필을 바꾸려고 했습니다.

그런데 Steam 실행으로 바뀌면서 문제가 생겼습니다.

- PowerShell에서 설정한 환경 변수가 실제 게임 프로세스까지 항상 전달된다고 장담할 수 없었습니다.

그래서 지금은:

1. 테스트 런처가 `runtime-overrides.json`을 쓴다.
2. 게임 시작 후 `mod.gd`가 그 파일을 읽는다.
3. 로그로 읽은 값을 확인한다.

즉, 현재 실제로 검증된 제어 채널은 환경 변수보다 `runtime-overrides.json`입니다.

## 4. 패키지는 어떻게 생기나

패키지 루트:

- [Sts2Speed](C:/Users/jidon/source/repos/STS2ModeTest/artifacts/package-layout/Sts2Speed)

중요 파일:

- `manifest.json`
- `mod.cfg`
- `mod.gd`
- `GUMM_mod.gd`
- `config/runtime-overrides.json`
- `scripts/test-profiles.json`
- `scripts/Start-Sts2SpeedTest.ps1`
- `bin/Sts2Speed.ModSkeleton.dll`
- `bin/Sts2Speed.Core.dll`

이 패키지는 두 방향을 동시에 준비합니다.

- 내장 C# 모딩 루트 후보
- GUMM 로더 루트

현재 실제로 작동이 확인된 것은 GUMM 로더 루트입니다.

## 5. GUMM 설치는 무엇을 바꾸는가

핵심 파일:

- [GummIntegration.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/GummIntegration.cs)

설치 시 라이브 게임 폴더에 추가되는 파일:

- `override.cfg`
- `GUMM_mod_loader.tscn`

`override.cfg` 핵심 값:

- `[application] run/main_scene="res://GUMM_mod_loader.tscn"`
- `[gumm] main_scene="res://scenes/game.tscn"`
- `[gumm] mod_list=PackedStringArray("<패키지 루트>")`

이렇게 하면 게임이 직접 메인 씬으로 가지 않고, 먼저 GUMM 로더 씬을 거칩니다.

## 6. `mod.gd`는 언제 실행되는가

GUMM 로더는 `mod_list`에 등록된 각 폴더의 `mod.cfg`를 읽고, 거기서 지정한 `mod.gd`를 로드합니다.

우리 패키지에서는 다음 로그가 실제로 확인됐습니다.

- `Loading mod: STS2 Speed Skeleton`

그 다음 `mod.gd`의 `_initialize(scene_tree)`가 호출됩니다.

현재 `mod.gd`는 이 함수에서:

- `runtime-overrides.json` 로딩
- `STS2_SPEED_*` 환경 변수 읽기
- 로그 출력

만 수행합니다.

## 7. 왜 `GUMM_mod.gd`가 중요했는가

처음에는 `GUMM_mod.gd`를 최소한의 베이스 스크립트로 만들어도 될 거라고 생각했습니다.

하지만 실제로는 안 됐습니다.

이유:

- `mod.gd`가 `get_full_path("config/runtime-overrides.json")`를 호출하는데
- 얇은 베이스 스크립트에는 `get_full_path()`가 없었습니다.

그 결과:

- GDScript parse error
- 모드 로드 실패

현재는 다음 방식으로 수정했습니다.

- 가능하면 GUMM 저장소의 실제 `System/4.x/GUMM_mod.gd`를 패키지에 복사
- 없을 때만 fallback 스크립트 생성

즉, "모드가 어떻게 동작하는가"의 핵심 중 하나는 사실 `mod.gd`가 아니라 그 기반이 되는 `GUMM_mod.gd`였습니다.

## 8. 실제 검증에서 확인한 로그

실제 검증에서 확인된 핵심 로그는 다음과 같습니다.

- `Loading mod: STS2 Speed Skeleton`
- `STS2 Speed Skeleton GUMM bootstrap loaded. runtime={ ... }`
- `[Startup] Time to main menu: ...`

이 세 줄이 의미하는 것:

1. GUMM가 우리 모드를 발견했다.
2. `mod.gd`가 실제로 실행됐다.
3. 모드가 있어도 게임은 메인 메뉴까지 도달했다.

## 9. 아직 남아 있는 문제

현재 확인된 잔여 문제:

- GUMM 로더 내부에서 `remove_child()` 타이밍 경고가 한 번 발생
- 속도 패치 본체는 아직 없음
- `queueWaitScale`, `effectDelayScale`는 아직 로그 출력 외 효과 없음

즉, 현재 동작 원리는 "로드 체인 검증" 단계까지는 끝났고, "속도 변경" 단계는 아직 남아 있습니다.

## 10. 다음 구현이 붙을 자리

실제 속도 패치가 들어갈 자리는 두 갈래입니다.

1. GUMM 경유 스크립트에서 추가 로더를 호출
2. 내부 C# 모딩 루트를 확인한 뒤 DLL 엔트리포인트를 진입점으로 사용

현재 저장소는 두 경로를 모두 열어 두고 있지만, 실기에서 먼저 성공한 쪽은 1번입니다.
