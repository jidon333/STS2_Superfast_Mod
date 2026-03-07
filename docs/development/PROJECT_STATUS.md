# 프로젝트 상태

기준 시점:

- 날짜: `2026-03-08`
- 검증 빌드: `STS2 v0.98.2`
- 엔진: `Godot 4.5.1`
- 런타임: `.NET 9`

## 현재 상태

- STS2 내장 `mods` 로더를 사용합니다.
- 배포 형식은 `mods + pck + dll + json` 입니다.
- 실제 배포 파일은 아래 4개입니다.
  - `sts2-speed-skeleton.pck`
  - `sts2-speed-skeleton.dll`
  - `Sts2Speed.Core.dll`
  - `Sts2Speed.config.json`
- 설정 모델은 flat `baseSpeed + ...Speed` 입니다.
- 사용자 규칙은 단순합니다.
  - 모든 숫자는 `클수록 빠름`

## 현재 패치 범위

- Spine 애니메이션
  - `MegaAnimationState.SetTimeScale`
  - `MegaTrackEntry.SetTimeScale`
- wait / timer
  - `Cmd.CustomScaledWait`
  - `CombatState.GodotTimerTask`
- 전투 UI delta
  - `NTargetingArrow._Process`
  - `NIntent._Process`
  - `NStarCounter._Process`
  - `NEnergyCounter._Process`
- 전투 VFX delta
  - `NBezierTrail._Process`
  - `NCardTrail._Process`
  - `NDamageNumVfx._Process`
  - `NHealNumVfx._Process`

## 현재 가능한 작업

- 백업 생성 / 검증 / 복구
- vanilla `profileN` -> `modded/profileN` 동기화 복구
- `.pck` 생성
- 네이티브 `mods` 폴더용 패키지 생성 / 배포
- flat speed JSON 생성
- runtime log 확인

## 설정 파일

현재 패키지에 들어가는 설정 파일:

- `Sts2Speed.config.json`

예시:

```json
{
  "enabled": true,
  "baseSpeed": 3.0,
  "spineSpeed": 1.0,
  "queueSpeed": 1.0,
  "effectSpeed": 1.0,
  "combatUiSpeed": 1.0,
  "combatVfxSpeed": 1.0,
  "combatOnly": true
}
```

## 주의 사항

- STS2는 모드 로드 시 저장 경로를 `modded/profileN` 으로 분리합니다.
- 그래서 진행 데이터가 초기화된 것처럼 보일 수 있습니다.
- 이 경우 `sync-modded-profile` 명령으로 복구할 수 있습니다.
- 과거 문서 일부에는 `Sts2Speed.speed.txt` 기준 설명이 남아 있는데, 그것은 이전 단계 기록입니다.

## 아직 보류한 것

- `CombatManager.WaitForActionThenEndTurn`
- `CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
- `ActionExecutor.ExecuteActions`
- `MathHelper.SmoothDamp`
- 전역 `Engine.TimeScale`
- `NScreenShake`

이 항목들은 체감 개선 가능성은 있지만, 동기화 붕괴나 과한 부작용 위험 때문에 아직 보류 중입니다.
