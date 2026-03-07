# STS2 Speed Toolkit

이 저장소는 `Slay the Spire 2` 속도 모드를 안전하게 실험하고, 백업/복구와 패키징까지 같이 관리하기 위한 작업 저장소다.

현재 기준일은 `2026-03-07`이고, 실기 검증 대상 빌드는 `v0.98.2`, 엔진은 `Godot 4.5.1`, 런타임은 `.NET 9.0.7`이다.

## 현재 상태

- 게임 내장 네이티브 모드 로더가 실제로 존재함을 `sts2.dll` 디컴파일로 확인했다.
- `Slay the Spire 2\mods` 폴더에 `pck + dll`을 넣으면 게임이 이를 스캔하고 로드한다.
- `mod_manifest.json` 안의 `pck_name`은 반드시 확장자 없는 basename 이어야 한다.
- `.pck`는 Godot 공식 `--export-pack` 경로로 생성 가능하다.
- `sts2-speed-skeleton.pck + sts2-speed-skeleton.dll` 조합이 실제로 로드되고, 게임 로그에 `Finished mod initialization`과 `--- RUNNING MODDED! ---`가 찍힌다.
- 모드가 켜지면 저장 경로는 `user://steam/<계정>/modded/profile1`로 분리된다.
- 이 때문에 모드를 처음 켰을 때 진행이 초기화된 것처럼 보일 수 있다.
- `sync-modded-profile` 명령으로 `profile1 -> modded/profile1` 복구를 자동화했다. 기존 modded 프로필은 먼저 백업한다.
- 현재 실제 패치는 들어가 있다.
  - `MegaAnimationState.SetTimeScale`
  - `MegaTrackEntry.SetTimeScale`
  - `Cmd.CustomScaledWait`
  - `CombatState.GodotTimerTask`
- 다만 자동화로 전투까지 밀어 넣지는 않았기 때문에, “전투 중 체감 속도 변화”는 아직 사용자가 직접 플레이로 최종 확인해야 한다.

## 지금 되는 것

- 백업 생성, 검증, 복구
- vanilla 프로필을 modded 프로필로 복제하는 복구
- GUMM 경로 부트스트랩 검증
- 네이티브 `mods + pck + dll + txt` 경로 검증
- Godot 공식 `.pck` 생성
- 라이브 게임에 네이티브 패키지 배치
- 텍스트 파일 `Sts2Speed.speed.txt` 하나로 기본 배속 제어
- 환경 변수 `STS2_SPEED_SPINE_TIME_SCALE`, `STS2_SPEED_QUEUE_WAIT_SCALE`, `STS2_SPEED_EFFECT_DELAY_SCALE` 지원

## 아직 남은 것

- 실제 전투 플레이에서 패치가 원하는 강도로 동작하는지 수동 검증
- `queueWaitScale`, `effectDelayScale` 체감이 충분한지 조정
- 필요하면 `ActionExecutor` 쪽 후크를 추가해서 더 공격적인 대기시간 단축
- 라이브 `mods` 폴더 정리 자동화

## 핵심 명령

```powershell
dotnet build Sts2Speed.sln
dotnet run --project src/Sts2Speed.SelfTest
dotnet run --project src/Sts2Speed.Tool -- snapshot --snapshot-root artifacts/snapshots/<name>
dotnet run --project src/Sts2Speed.Tool -- verify-snapshot --snapshot-root artifacts/snapshots/<name>
dotnet run --project src/Sts2Speed.Tool -- restore-snapshot-state --snapshot-root artifacts/snapshots/<name>
dotnet run --project src/Sts2Speed.Tool -- sync-modded-profile
dotnet run --project src/Sts2Speed.Tool -- build-native-pck --layout flat
dotnet run --project src/Sts2Speed.Tool -- deploy-native-package --layout flat
```

## 실기에서 확인된 사실

- 네이티브 모드 동의 플래그는 `settings.save` 안의 `mod_settings.mods_enabled` 이다.
- 모드 로더는 `.pck`를 찾은 뒤 동일 basename의 `.dll`을 찾는다.
- `Sts2Speed.Core.dll`은 자동으로 로드되지 않아서, 모드 DLL 안에서 별도 assembly resolver를 등록해야 했다.
- 현재 resolver는 `mods` 폴더를 기준으로 추가 DLL을 찾는다.
- 런타임 진단 로그는 `mods\sts2speed.runtime.log` 에 기록된다.
- 진행 데이터가 vanilla `profileN`에만 있고 modded `profileN`이 비어 있으면, 모드 실행 시 새 프로필처럼 보인다.

## 문서

- [개발 문서 안내](docs/development/README.md)
- [모드 로딩 방식 비교](docs/development/MOD_LOADING_STRATEGIES.md)
- [초보자용 구조 설명](docs/development/MOD_BEGINNER_GUIDE.md)
- [로드 체인](docs/development/LOAD_CHAIN.md)
- [작업 기록](docs/development/WORKLOG.md)
