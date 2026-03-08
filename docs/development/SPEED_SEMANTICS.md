# STS2 Speed Semantics

## 요약

현재 사용자 설정 모델은 flat `baseSpeed + ...Speed` 구조입니다.

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

공통 규칙은 하나입니다.

- 사용자 기준으로 모든 숫자는 `클수록 빠름`

실제 적용 배속:

```text
effectiveSpeed = baseSpeed * groupSpeed
```

예:

- `baseSpeed = 3.0`
- `queueSpeed = 0.8`

이면 queue 관련 실제 배속은 `2.4배속`입니다.

## 왜 이름을 이렇게 바꿨는가

초기 구현에서는 어떤 값은 올려야 빨라지고, 어떤 값은 내려야 빨라지는 구조가 섞여 있었습니다.

그 구조는 사용자 입장에서 계속 "이 값이 속도인가 duration인가?"를 해석해야 해서 UX가 좋지 않았습니다.

현재는 이 문제를 이렇게 해결했습니다.

- 외부 설정: 전부 `...Speed`, 전부 `higher = faster`
- 내부 구현: 항목 성격에 따라 곱하거나 나눔

즉 wait / delay inverse 계산 같은 것은 전부 내부 코드가 처리합니다.

## 항목별 처리 방식

### Spine

적용 대상:

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`

계산:

```text
patchedScale = originalScale * effectiveSpeed
```

### Queue

적용 대상:

- `Cmd.CustomScaledWait`

계산:

```text
patchedDuration = originalDuration / effectiveSpeed
```

즉 `queueSpeed`를 올리면 wait duration이 줄어듭니다.

### Effect

적용 대상:

- `CombatState.GodotTimerTask`

계산:

```text
patchedDuration = originalDuration / effectiveSpeed
```

즉 `effectSpeed`를 올리면 timer 기반 effect delay가 줄어듭니다.

단, 이 경로가 모든 연출을 대표하지는 않을 수 있습니다.

### Combat UI

적용 대상:

- `NTargetingArrow._Process`
- `NIntent._Process`
- `NStarCounter._Process`
- `NEnergyCounter._Process`

계산:

```text
patchedDelta = originalDelta * effectiveSpeed
```

### Combat VFX

적용 대상:

- `NBezierTrail._Process`
- `NCardTrail._Process`
- `NDamageNumVfx._Process`
- `NHealNumVfx._Process`

계산:

```text
patchedDelta = originalDelta * effectiveSpeed
```

## `combatOnly`

`combatOnly = true`이면:

- 전투 중(`CombatManager.Instance.IsInProgress`)일 때만 패치를 적용

이 값은 "속도" 값은 아니지만, 적용 범위를 제한하는 중요한 안전 장치입니다.

## 인게임 설정 UI와 semantics

인게임 UI에서 조절하는 값도 완전히 같은 semantics를 따릅니다.

즉:

- 파일에서 바꾸든
- 인게임 UI에서 바꾸든

모든 숫자는 동일하게 `클수록 빠름`입니다.

## 하위 호환

현재 로더는 여전히 아래 legacy 입력을 읽습니다.

- `Sts2Speed.speed.txt`
- 구형 `groups` JSON
- legacy 환경 변수 이름

하지만 문서와 배포 기본값은 flat `...Speed` 형식을 기준으로 합니다.

## 현재 추천 UX

사용자 관점에서 가장 자연스러운 시작점:

1. `baseSpeed`만 조절
2. 나머지 `...Speed`는 `1.0` 유지
3. 체감상 특정 그룹만 느릴 때만 개별 조정

즉 지금 semantics의 핵심은:

- 강한 기본 규칙 하나
- 내부 구현 복잡성은 코드가 흡수

입니다.
