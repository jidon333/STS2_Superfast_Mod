# STS2 Superfast Mod 튜닝 가이드

이 문서는 `Sts2Speed.config.json`의 각 항목이 실제로 무엇을 조절하는지 설명합니다.

기본 추천 시작점은 아래와 같습니다.

```json
{
  "enabled": true,
  "baseSpeed": 3,
  "combatOnly": true,
  "spineSpeed": 1,
  "queueSpeed": 1,
  "effectSpeed": 1,
  "combatUiSpeed": 1,
  "combatVfxSpeed": 1
}
```

공통 규칙:

- JSON 숫자는 전부 `클수록 빠름` 입니다.
- 내부 구현은 항목마다 다르지만, 사용자 기준 규칙은 하나로 통일되어 있습니다.
- 실제 적용 배속은 `baseSpeed x 각 항목 Speed` 입니다.

예시:

- `baseSpeed = 3`, `spineSpeed = 1` -> Spine 계열은 3배속
- `baseSpeed = 3`, `queueSpeed = 0.8` -> Queue 계열은 2.4배속
- `baseSpeed = 3`, `combatUiSpeed = 1.2` -> Combat UI 계열은 3.6배속

## 항목별 설명

### `enabled`

- 모드 전체 활성화 여부입니다.
- `true`면 속도 모드가 동작합니다.
- `false`면 파일은 그대로 두고 효과만 끕니다.

권장:

- 평소에는 `true`
- 바닐라 속도와 비교할 때만 `false`

### `baseSpeed`

- 전체 속도의 기본 배수입니다.
- 가장 먼저 조절할 값입니다.
- 나머지 `...Speed` 항목은 이 값에 추가 계수를 곱합니다.

권장:

- 기본 추천: `3`
- 조금만 빠르게: `2`
- 더 빠르게: `3.5`
- 공격적으로: `4` 이상

설명:

- 대부분의 사용자는 이것만 바꾸고 나머지는 `1`로 두면 충분합니다.
- 실전에서 아직 느린 느낌이 남으면 가장 먼저 `baseSpeed`를 올리는 쪽이 가성비가 좋습니다.
- 현재 플레이 감각 기준으로도 개별 항목을 많이 만지기보다 `baseSpeed`만 올리고 나머지를 `1`로 두는 구성이 가장 자연스럽습니다.

### `combatOnly`

- 전투 중일 때만 패치를 적용할지 결정합니다.
- `true`면 전투 외 화면에는 거의 손대지 않습니다.
- `false`면 더 넓은 구간에 영향이 갈 수 있습니다.

권장:

- 기본값: `true`

설명:

- 현재 모드는 전투 템포 개선이 주목적이라 기본값을 `true`로 두는 편이 안전합니다.
- 상점, 보상 화면, 메뉴까지 더 빠르게 하고 싶다면 나중에 실험적으로 `false`를 시도할 수 있습니다.

### `spineSpeed`

- Spine 애니메이션 계열 속도입니다.
- 캐릭터, 적, 카드 관련 모션의 체감에 직접 영향을 줍니다.

증상별 조절:

- 캐릭터나 적의 움직임이 아직 답답하다 -> `spineSpeed`를 올립니다.
- 공격 모션만 너무 빠르고 나머지는 괜찮다 -> `spineSpeed`를 내립니다.

권장 범위:

- `0.9 ~ 1.2`

### `queueSpeed`

- 행동 사이 wait, 처리 템포 계열 속도입니다.
- 턴 진행, 카드 처리, 다음 행동으로 넘어가는 리듬에 크게 영향을 줍니다.

증상별 조절:

- 행동이 끝난 뒤 다음 처리로 넘어가는 템포가 느리다 -> `queueSpeed`를 올립니다.
- 카드/행동이 너무 휙휙 지나가서 읽기 어렵다 -> `queueSpeed`를 내립니다.

권장 범위:

- `0.8 ~ 1.2`

주의:

- 내부적으로는 duration을 줄이는 방향으로 처리되지만, 사용자 기준 규칙은 여전히 `클수록 빠름` 입니다.

### `effectSpeed`

- 명시적인 effect delay / timer 계열 속도입니다.
- 일부 전투 연출 사이 지연을 줄일 수 있지만, 현재 게임의 모든 연출이 이 경로를 타는 것은 아닙니다.

현재 상태:

- 코드에는 구현되어 있습니다.
- 다만 최근 실전 로그에서는 항상 hit되는 것이 확인되지는 않았습니다.
- 그래서 값을 올려도 체감 변화가 작을 수 있습니다.

증상별 조절:

- 일부 효과 전환이 유독 늘어진다 -> 올려볼 가치가 있습니다.
- 올려도 체감 차이가 거의 없다 -> 현재 빌드에서 다른 경로를 타는 연출일 가능성이 큽니다.

권장 범위:

- `1.0 ~ 1.3`

### `combatUiSpeed`

- 전투 UI 쪽 delta 기반 갱신 속도입니다.
- 조준 화살표, intent, 에너지 / 별 카운터 같은 UI 피드백에 주로 영향을 줍니다.

증상별 조절:

- 조준선, intent, UI 반응이 아직 느리다 -> 올립니다.
- UI가 너무 예민하거나 정신없다 -> 내립니다.

권장 범위:

- `0.9 ~ 1.2`

### `combatVfxSpeed`

- 전투 VFX delta 기반 갱신 속도입니다.
- 데미지 숫자, 힐 숫자, trail 같은 이펙트에 주로 영향을 줍니다.

증상별 조절:

- 데미지 숫자, trail, 카드 잔상 이펙트가 아직 답답하다 -> 올립니다.
- VFX가 너무 빨라져서 타격감이 손해 난다 -> 내립니다.

권장 범위:

- `0.9 ~ 1.2`

## 추천 튜닝 순서

가장 안전한 순서:

1. `baseSpeed`만 조절
2. 그래도 캐릭터 / 적 모션이 느리면 `spineSpeed`
3. 턴 진행이 늘어지면 `queueSpeed`
4. UI가 느리면 `combatUiSpeed`
5. 숫자 / VFX가 느리면 `combatVfxSpeed`
6. 마지막에만 `effectSpeed`

## 추천 프리셋

### 기본 추천

```json
{
  "enabled": true,
  "baseSpeed": 3,
  "combatOnly": true,
  "spineSpeed": 1,
  "queueSpeed": 1,
  "effectSpeed": 1,
  "combatUiSpeed": 1,
  "combatVfxSpeed": 1
}
```

이 프리셋은 "가장 무난하고 자연스러운 시작점" 기준입니다.
개별 항목을 따로 조절하기 전에는 이 상태에서 `baseSpeed`만 바꿔 보시는 것을 권장합니다.

### 조금 더 빠르게

```json
{
  "enabled": true,
  "baseSpeed": 3.5,
  "combatOnly": true,
  "spineSpeed": 1,
  "queueSpeed": 1,
  "effectSpeed": 1,
  "combatUiSpeed": 1,
  "combatVfxSpeed": 1
}
```

### 연출은 조금 남기고 템포만 더 빠르게

```json
{
  "enabled": true,
  "baseSpeed": 3,
  "combatOnly": true,
  "spineSpeed": 0.95,
  "queueSpeed": 1.15,
  "effectSpeed": 1,
  "combatUiSpeed": 1.05,
  "combatVfxSpeed": 1
}
```

## 현재 로그 기준으로 실제 확인된 항목

최근 실전 로그에서 실제 hit가 확인된 항목:

- `spineSpeed`
- `queueSpeed`
- `combatUiSpeed`
- `combatVfxSpeed`

최근 실전 로그에서 아직 항상 hit가 확인되지 않은 항목:

- `effectSpeed`

즉 화면에 따라 어떤 건 빨라 보이는데 어떤 건 덜 빨라 보이는 체감은 현재 빌드 기준으로 완전히 이상한 현상은 아닙니다.
