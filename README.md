# STS2 Speed Toolkit

이 저장소는 `Slay the Spire 2` 속도 모드를 바로 완성하는 저장소가 아니라, 안전하게 실험하기 위한 통합 도구와 모드 뼈대를 만드는 저장소입니다.

현재 실제로 되는 것:

- 라이브 설치본과 세이브를 스냅샷으로 백업하고 해시까지 검증
- 필요하면 백업본으로 복구
- `artifacts/package-layout/Sts2Speed` 아래에 실제 패키지 생성
- GUMM 호환 `mod.cfg`, `mod.gd`, `GUMM_mod.gd` 생성
- `override.cfg` + `GUMM_mod_loader.tscn` 기반으로 라이브 게임 폴더에 GUMM 로더 설치
- Steam `-applaunch 2868840` 경로로 게임을 실행하고, 모드 부트스트랩이 로드되는지 로그로 검증

현재 아직 안 되는 것:

- 전투 애니메이션과 대기시간을 실제로 가속하는 인게임 패치
- `MegaAnimationState.SetTimeScale` 같은 후보 메서드에 대한 실패 없는 실시간 훅
- 정식 내장 모딩 루트와 GUMM 중 어느 쪽이 최종 경로인지 확정

핵심 원칙:

- 라이브 세이브와 게임 설정은 자동 수정하지 않음
- 첫 플레이 감상용 옵션(`skip_intro_logo`, `screenshake`, `text_effects_enabled`, 기본 `fast_mode`)은 건드리지 않음
- 바이너리 패치는 아직 하지 않음
- 실험 전에는 항상 스냅샷을 먼저 생성

개발 문서:

- [개발 문서 안내](C:/Users/jidon/source/repos/STS2ModeTest/docs/development/README.md)

## 프로젝트 구성

- `src/Sts2Speed.Core`
  - 설정 로딩, 환경 변수 오버라이드, 스냅샷/복구 계획, GUMM 설치 계획
- `src/Sts2Speed.ModSkeleton`
  - 모드 패키지 생성, GUMM 부트스트랩 스크립트 생성, 테스트 런처 생성
- `src/Sts2Speed.Tool`
  - CLI 진입점
- `src/Sts2Speed.SelfTest`
  - 외부 테스트 프레임워크 없이 빠르게 검증하는 self-test

## 주요 명령

```powershell
dotnet run --project src/Sts2Speed.Tool -- show-config
dotnet run --project src/Sts2Speed.Tool -- dry-run-package
dotnet run --project src/Sts2Speed.Tool -- materialize-package
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
- `runtime-overrides.json` 값이 `mod.gd` 부트스트랩까지 전달되는 것이 확인됐다.
- 현재 남아 있는 런타임 이슈는 GUMM 로더 자체의 `remove_child()` 타이밍 경고다.

## 주의

- `deploy-package --mod-root ...`는 지금 기준 주 경로가 아니다.
- 현재 실기 검증된 로더 경로는 `override.cfg` + `GUMM_mod_loader.tscn`을 쓰는 GUMM 방식이다.
- 속도 패치 자체는 아직 구현되지 않았으므로, `spine125` 같은 프로필은 현재 "설정 전달 검증" 단계에 가깝다.
