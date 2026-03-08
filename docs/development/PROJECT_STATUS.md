# 프로젝트 상태

기준 시점:

- 날짜: `2026-03-08`
- 확인 빌드: `STS2 v0.98.2`
- 엔진: `Godot 4.5.1`
- 런타임: `.NET 9`

## 현재 상태

이 저장소는 STS2의 native `mods` 로더를 사용하는 속도 모드입니다.

현재 배포 형식:

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `Sts2Speed.Core.dll`
- `Sts2Speed.config.json`

즉 현재 상태는:

- GUMM 실험 단계는 지나감
- native `mods + pck + dll + json` 경로로 고정됨
- 런타임 payload는 Harmony 패치 기반
- 설정은 파일과 인게임 UI 둘 다 지원

## 현재 실제 패치 범위

### Spine 애니메이션

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`

### wait / timer

- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`

### 전투 UI delta

- `NTargetingArrow._Process`
- `NIntent._Process`
- `NStarCounter._Process`
- `NEnergyCounter._Process`

### 전투 VFX delta

- `NBezierTrail._Process`
- `NCardTrail._Process`
- `NDamageNumVfx._Process`
- `NHealNumVfx._Process`

### Modding Screen UI 주입

- `NModInfoContainer.Fill`
  - Harmony Postfix로 `InGameConfigUi.RefreshForSelection` 호출

## 현재 설정 표면

사용자 설정 파일:

- `mods\Sts2Speed.config.json`

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

설정 규칙:

- 모든 숫자는 `클수록 빠름`
- 실제 적용 배속은 `baseSpeed x 각 항목 Speed`
- 초보자 기준 권장 시작점은 `baseSpeed = 3`, 나머지 `1.0`

legacy 호환:

- `Sts2Speed.speed.txt`는 여전히 fallback으로 읽습니다
- `groups` 구조 JSON도 계속 읽습니다
- 환경 변수 `STS2_SPEED_*`도 계속 우선 적용됩니다

## 인게임 설정 UI

현재 모드는 인게임 `설정 -> 모드` 화면에서 우리 모드를 선택했을 때 간단한 설정 패널을 표시합니다.

조절 가능한 항목:

- `Enabled`
- `Base speed`
- `Spine speed`
- `Queue speed`
- `Effect speed`
- `Combat UI speed`
- `Combat VFX speed`
- `Combat only`

현재 구현 특성:

- `+ / -` 버튼 기반
- 저장 대상은 `Sts2Speed.config.json`
- 값 변경 후 런타임이 파일 변경 시각을 보고 다시 읽습니다
- 별도 재시작 없이 반영되는 경로를 목표로 합니다

## 현재 동작 확인 상태

최근 실전 로그 기준 확인된 항목:

- `spineSpeed`
- `queueSpeed`
- `combatUiSpeed`
- `combatVfxSpeed`

최근 실전 로그 기준 아직 일관되게 hit가 보이지 않은 항목:

- `effectSpeed`

즉 현재 모드는 충분히 usable하지만, 모든 느린 구간을 완전히 커버했다고 보긴 어렵습니다.

## 현재 가능한 도구/워크플로

- snapshot / restore / verify
- native `.pck` 생성
- live `mods` 폴더 배포
- `profileN -> modded/profileN` 동기화 복구
- self-test
- 인게임 설정 UI

## 남아 있는 리스크 / 보류 훅

아직 보류 중:

- `CombatManager.WaitForActionThenEndTurn`
- `CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
- `ActionExecutor.ExecuteActions`
- 전역 `Engine.TimeScale`
- 오디오 조작

이유:

- 현재 체감은 이미 충분히 자연스러움
- 위 훅들은 단순 duration이 아니라 상태 동기화 / 액션 루프에 가까움
- 잘못 건드리면 soft lock이나 phase mismatch 가능성이 큼
