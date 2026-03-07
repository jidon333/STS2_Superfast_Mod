# STS2 Speed Semantics

## 요약

현재 사용자 설정은 flat `...Speed` 모델입니다.

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

핵심 규칙은 하나입니다.

- 사용자 입장에서는 모든 숫자가 `클수록 빠름`

`baseSpeed`는 전체 기본 배속이고, 나머지 `...Speed`는 각 그룹의 계수입니다.

실제 적용 속도는 이렇게 계산합니다.

```text
effectiveSpeed = baseSpeed * groupSpeed
```

예:

- `baseSpeed = 3.0`
- `queueSpeed = 0.8`

이면 queue 관련 실제 적용 속도는 `1.6배속`입니다.

## 왜 이름을 이렇게 바꿨는가

초기 구현에서는 어떤 값은 올릴수록 빨라지고, 어떤 값은 줄여야 빨라지는 모양이 섞여 있었습니다.

그 구조는 사용자 입장에서 매번 "이 값은 speed인가 duration인가"를 해석해야 해서 UX가 좋지 않았습니다.

지금은 외부 설정 파일에서 이 문제를 제거했습니다.

- 외부 설정: 전부 `higher = faster`
- 내부 구현: 항목 성격에 따라 곱셈 또는 나눗셈 처리

즉 사용자는 `...Speed` 숫자만 조절하면 되고, wait/delay의 inverse 처리 같은 건 코드가 담당합니다.

## 항목별 내부 처리

### Spine

적용 대상:

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`

내부 계산:

```text
patchedScale = originalScale * effectiveSpeed
```

### Queue

적용 대상:

- `Cmd.CustomScaledWait`

내부 계산:

```text
patchedDuration = originalDuration / effectiveSpeed
```

즉 `queueSpeed`를 올리면 대기시간이 줄어듭니다.

### Effect

적용 대상:

- `CombatState.GodotTimerTask`

내부 계산:

```text
patchedDuration = originalDuration / effectiveSpeed
```

즉 `effectSpeed`를 올리면 타이머 기반 연출 지연이 줄어듭니다.

### Combat UI

적용 대상:

- `NTargetingArrow._Process`
- `NIntent._Process`
- `NStarCounter._Process`
- `NEnergyCounter._Process`

내부 계산:

```text
patchedDelta = originalDelta * effectiveSpeed
```

즉 `combatUiSpeed`를 올리면 전투 UI 반응이 더 빨라집니다.

### Combat VFX

적용 대상:

- `NBezierTrail._Process`
- `NCardTrail._Process`
- `NDamageNumVfx._Process`
- `NHealNumVfx._Process`

내부 계산:

```text
patchedDelta = originalDelta * effectiveSpeed
```

즉 `combatVfxSpeed`를 올리면 수치, trail, 일부 전투 이펙트가 더 빨라집니다.

## 사용자 튜닝 가이드

### 초보자

- `baseSpeed`만 바꿉니다.
- 나머지 `...Speed`는 모두 `1.0`으로 둡니다.

### 조금 더 세밀하게

- 애니메이션만 더 빠르게: `spineSpeed`
- 액션 템포만 더 빠르게: `queueSpeed`
- 전투 UI를 조금 덜 빠르게: `combatUiSpeed`
- 전투 이펙트를 더 공격적으로 빠르게: `combatVfxSpeed`

예:

```json
{
  "enabled": true,
  "baseSpeed": 3.0,
  "spineSpeed": 1.1,
  "queueSpeed": 1.0,
  "effectSpeed": 0.9,
  "combatUiSpeed": 0.8,
  "combatVfxSpeed": 1.2,
  "combatOnly": true
}
```

이 설정은:

- Spine는 조금 더 빠르게
- queue는 기본 2배속 유지
- effect는 약간 덜 공격적으로
- combat UI는 조금 덜 빠르게
- combat VFX는 더 빠르게

라는 의미입니다.

## 하위 호환

런타임 로더는 예전 `groups` 구조도 계속 읽을 수 있습니다.

즉 기존 설정 파일이 아래 형식이어도 바로 깨지지 않습니다.

```json
{
  "enabled": true,
  "baseSpeed": 3.0,
  "combatOnly": true,
  "groups": {
    "spine": 1.0,
    "queueWait": 1.0,
    "effectDelay": 1.0,
    "combatUiDelta": 1.0,
    "combatVfxDelta": 1.0
  }
}
```

다만 앞으로 문서와 배포 기본값은 flat `...Speed` 형식을 기준으로 유지합니다.
