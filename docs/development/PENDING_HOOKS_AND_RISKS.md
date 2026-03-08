# 남은 훅과 리스크

이 문서는 현재 모드가 이미 적용 중인 훅과, 아직 의도적으로 붙이지 않은 훅을 구분해서 설명합니다.

핵심 요약:

1. 현재 모드는 `spine`, `queue`, `effect`, `combat UI`, `combat VFX`, `config UI`까지 구현돼 있습니다.
2. 하지만 `CombatManager` / `ActionExecutor` 계열은 아직 보류 중입니다.
3. 이유는 "못 찾아서"가 아니라 "건드리면 더 자연스럽기보다 깨질 가능성이 커서"입니다.

## 1. 현재 실제 패치 지점

### 속도 관련

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`
- `NTargetingArrow._Process`
- `NIntent._Process`
- `NStarCounter._Process`
- `NEnergyCounter._Process`
- `NBezierTrail._Process`
- `NCardTrail._Process`
- `NDamageNumVfx._Process`
- `NHealNumVfx._Process`

### UI 관련

- `NModInfoContainer.Fill`
  - Postfix로 인게임 설정 패널 주입

## 2. `effectSpeed`가 체감상 약할 수 있는 이유

현재 `effectSpeed`는 `CombatState.GodotTimerTask(double)` 하나에 연결돼 있습니다.

이 메서드는 다시 보면 `SceneTree.CreateTimer()`를 감싼 utility timer helper에 가깝습니다.

즉:

- 경로 자체는 맞음
- 하지만 모든 전투 연출이 이 helper를 타는 것은 아닐 수 있음

그래서 최근 로그에서:

- `spine`
- `queue`
- `combatUi`
- `combatVfx`

는 자주 hit되지만,

- `effect`

는 항상 hit가 보이지 않을 수 있습니다.

## 3. 왜 `CombatManager.WaitForActionThenEndTurn`를 안 붙였는가

이름만 보면 단순 wait처럼 보이지만, 실제로는:

- `action.CompletionTask`
- 이후 turn-end phase 진입

을 연결하는 동기화 게이트입니다.

여길 공격적으로 줄이면 생길 수 있는 문제:

- turn-end phase가 너무 빨리 넘어감
- 연출과 실제 상태가 어긋남
- 특정 조합에서 phase mismatch
- soft lock

즉 "시간을 줄이는 것"보다 "상태 경계"를 건드리는 성격이 강합니다.

## 4. 왜 `WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`를 안 붙였는가

이 메서드는 단순 sleep이 아니라 queue barrier에 가깝습니다.

실제 역할:

- 현재 action이 player-driven인지 확인
- 필요 시 `TaskCompletionSource`를 걸고
- `AfterActionExecuted` 이벤트를 기다리며
- 다음 ready action 상태를 관찰

즉 "queue가 특정 조건에 도달할 때까지 기다리는 함수"입니다.

이걸 단순 duration처럼 줄이는 건 위험합니다.

## 5. 왜 `ActionExecutor.ExecuteActions`를 안 붙였는가

이 메서드는 액션 실행 루프의 중심입니다.

대략:

1. ready action 획득
2. unpause 대기
3. action 실행
4. action completion 대기
5. win condition 체크
6. 다음 action

즉 여기까지 건드리면 "남은 느린 구간"도 줄일 수 있겠지만, 동시에:

- action completion 순서
- frame progression
- 승패 체크 타이밍

까지 같이 건드리게 됩니다.

현재 체감이 이미 자연스럽다는 피드백이 나온 상태에서 여기로 들어가는 건 개선보다 회귀 확률이 큽니다.

## 6. 전역 time scale / 오디오를 안 건드리는 이유

아직 안 붙인 것:

- 전역 `Engine.TimeScale`
- 오디오 pitch / tempo 조작
- `NScreenShake`

이유:

- 전역 배속은 너무 넓음
- 오디오는 바로 어색해질 수 있음
- screen shake는 빨라짐보다 이질감이 먼저 날 가능성이 큼

## 7. 인게임 설정 UI의 현재 한계

현재 UI는 usable하지만 완성형 위젯 시스템은 아닙니다.

현재 특성:

- `+ / -` 버튼 기반
- reflection으로 Godot control 생성
- 선택된 모드에만 패널 표시
- 값은 config 파일에 저장

남은 한계:

- native slider 스타일 아님
- Modding Screen의 공식 per-mod settings API를 쓰는 구조가 아님
- 현재는 우리가 UI를 주입하는 방식

## 8. 현재 가장 현실적인 다음 단계

속도 자체를 더 건드린다면 우선순위는 이렇습니다.

1. `effectSpeed`가 실제로 어디서 덜 먹는지 더 세분화해서 찾기
2. `Tween` 기반 남은 느린 구간 찾기
3. 그래도 부족할 때만 `CombatManager` / `ActionExecutor` 재검토

즉 지금 단계에서 정답은 "더 많은 훅"이 아니라 "더 정확한 leaf hook"입니다.
