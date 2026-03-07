# STS2 Speed Toolkit

이 저장소는 `Slay the Spire 2` 속도 모드를 바로 완성하는 저장소가 아니라, 안전하게 실험하기 위한 통합 도구와 모드 뼈대를 만드는 저장소입니다.

현재 실제로 되는 것:

- 라이브 설치본과 세이브를 스냅샷으로 백업하고 해시까지 검증
- 필요하면 백업본으로 복구
- `artifacts/package-layout/Sts2Speed` 아래에 GUMM 기준 패키지 생성
- `artifacts/native-package-layout/...` 아래에 네이티브 `mods` 폴더 기준 스테이징 생성
- `override.cfg` + `GUMM_mod_loader.tscn` 기반 GUMM 로더 설치
- Steam `-applaunch 2868840` 경로로 실행 후 bootstrap 로그 검증

현재 아직 안 되는 것:

- 전투 애니메이션과 대기시간을 실제로 바꾸는 payload
- `MegaAnimationState.SetTimeScale`, `ActionExecutor.ExecuteActions` 같은 후보 메서드의 실시간 패치
- 네이티브 `mods + pck + dll + txt` 경로의 live 검증

핵심 원칙:

- 라이브 세이브와 게임 설정은 자동 수정하지 않음
- 첫 플레이 감상용 옵션(`skip_intro_logo`, `screenshake`, `text_effects_enabled`, 기본 `fast_mode`)은 건드리지 않음
- 실험 전에는 항상 스냅샷을 먼저 생성
- GUMM은 현재 검증된 부트스트랩 경로이지만, 최종 목표는 게임 내장 `mods` 경로

개발 문서:

- [개발 문서 안내](C:/Users/jidon/source/repos/STS2ModeTest/docs/development/README.md)

## 프로젝트 구성

- `src/Sts2Speed.Core`
  - 설정 로딩, 환경 변수 오버라이드, 스냅샷/복구 계획, GUMM 설치 계획
- `src/Sts2Speed.ModSkeleton`
  - GUMM 패키지 생성, 네이티브 `mods` 스테이징 생성, 테스트 런처 생성
- `src/Sts2Speed.Tool`
  - CLI 진입점
- `src/Sts2Speed.SelfTest`
  - 외부 테스트 프레임워크 없이 빠르게 검증하는 self-test

## 주요 명령

```powershell
dotnet run --project src/Sts2Speed.Tool -- show-config
dotnet run --project src/Sts2Speed.Tool -- dry-run-package
dotnet run --project src/Sts2Speed.Tool -- materialize-package
dotnet run --project src/Sts2Speed.Tool -- materialize-native-package --layout subdir
dotnet run --project src/Sts2Speed.Tool -- materialize-native-package --layout flat
dotnet run --project src/Sts2Speed.Tool -- materialize-gumm-game-entry
dotnet run --project src/Sts2Speed.Tool -- discover-mod-path
dotnet run --project src/Sts2Speed.Tool -- dry-run-snapshot
dotnet run --project src/Sts2Speed.Tool -- snapshot --snapshot-root artifacts/snapshots/<name>
dotnet run --project src/Sts2Speed.Tool -- verify-snapshot --snapshot-root artifacts/snapshots/<name>
dotnet run --project src/Sts2Speed.Tool -- dry-run-restore --snapshot-root artifacts/snapshots/<name>
dotnet run --project src/Sts2Speed.Tool -- restore --snapshot-root artifacts/snapshots/<name>
dotnet run --project src/Sts2Speed.Tool -- install-gumm-loader
dotnet run --project src/Sts2Speed.SelfTest
```

## 현재 기본값

- `enabled=false`
- `fastModeOverride=null`
- `animationScale=1.0`
- `spineTimeScale=1.0`
- `queueWaitScale=1.0`
- `effectDelayScale=1.0`
- `combatOnly=true`
- `preserveGameSettings=true`
- `verboseLogging=false`

## 현재 확인된 사실

- 직접 `SlayTheSpire2.exe`를 실행하면 Steam appID 초기화가 실패한다.
- Steam으로 실행하면 메인 메뉴까지 정상 진입한다.
- GUMM 로더를 설치한 상태에서 `Loading mod: STS2 Speed Skeleton` 로그가 찍힌다.
- `runtime-overrides.json` 값이 `mod.gd` bootstrap까지 전달되는 것이 확인됐다.
- `sts2.dll` 문자열에는 `TryLoadModFromPck`, `LoadMods`, `ModsDirectory`, `SteamWorkshop`, `ModInitializerAttribute`가 실제로 존재한다.
- 커뮤니티 배포 예시는 `mods + pck + dll + txt` 형태를 강하게 시사한다.

## 주의

- 현재 실기 검증이 끝난 건 GUMM bootstrap까지다.
- 최종 구현 목표는 GUMM이 아니라 게임 내장 `mods` 폴더 경로다.
- 네이티브 경로는 아직 `.pck` 생성기가 없어서 live deploy를 막아둔 상태다.
- `spine125` 같은 프로필은 지금 기준으로는 "속도 변경"보다 "설정 전달/로더 검증"의 의미가 더 크다.
