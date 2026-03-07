# 남은 훅과 위험도

이 문서는 현재 모드가 왜 `spineSpeed`, `queueSpeed`, `effectSpeed` 축 위주로 구현되어 있고, 왜 `CombatManager` / `ActionExecutor` 계열 훅을 아직 붙이지 않았는지 설명한다.

핵심 요약은 다음과 같다.

1. `effectSpeed`는 현재 실제 후크가 있지만, 생각보다 "자주 맞는 지점"이 아닐 가능성이 높다.
2. `CombatManager.WaitForActionThenEndTurn`, `WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`는 단순 대기 함수가 아니라 전투 동기화 게이트에 가깝다.
3. `ActionExecutor.ExecuteActions`는 액션 큐의 중심 루프라서, 잘못 건드리면 자연스러움보다 soft lock 위험이 먼저 올라간다.

즉 "더 빠르게 만들 수 있는 후보"와 "지금 바로 패치해도 되는 후보"는 다르다.

## 1. 현재 실제 패치 지점

현재 런타임 payload가 실제로 건드리는 메서드는 다음 네 곳이다.

- `MegaAnimationState.SetTimeScale(float scale)`
- `MegaTrackEntry.SetTimeScale(float scale)`
- `Cmd.CustomScaledWait(float fastSeconds, float standardSeconds, ...)`
- `CombatState.GodotTimerTask(double timeSec)`

앞의 두 개는 애니메이션 배속이고, 뒤의 두 개는 명시적인 wait/timer duration 조정이다.

공통점은 둘 다 "원래 의미가 시간/속도 인자"라는 점이다. 그래서 Prefix에서 인자만 바꿔도 상대적으로 안전하다.

## 2. `effectSpeed`가 실전에서 잘 안 보이는 이유

초기에는 `CombatState.GodotTimerTask(double timeSec)`가 전투 중 각종 이펙트 지연을 광범위하게 담당할 거라고 예상했다.

하지만 `sts2.dll`을 다시 디컴파일해 보면 이 메서드 본체는 매우 단순하다.

```csharp
private async Task GodotTimerTask(double timeSec)
{
    SceneTreeTimer sceneTreeTimer = ((SceneTree)Engine.GetMainLoop()).CreateTimer(timeSec);
    await sceneTreeTimer.ToSignal(sceneTreeTimer, SceneTreeTimer.SignalName.Timeout);
}
```

즉 이 메서드는 "SceneTree timer 하나 만들고 timeout을 기다리는 헬퍼"다.

중요한 점은, 이게 존재한다는 사실만으로 "모든 전투 이펙트가 다 이 메서드를 지난다"는 뜻은 아니라는 점이다.

실제로 확인한 호출 맥락 중 하나는 creature spawn 대기/timeout 계열이었다. 이런 유틸리티성 대기는 맞지만, 카드 하나하나의 핵심 전투 템포를 전부 대표한다고 보기 어렵다.

그래서 현재 증상은 자연스럽다.

- 설정은 `effectSpeed=1.0`, `baseSpeed=2.0` 조합으로 정상 로드된다.
- 하지만 실제 플레이 로그에서는 `effect delay scale applied`가 자주 안 찍힐 수 있다.

결론:

- `effectSpeed`는 "완전히 죽은 설정"은 아니다.
- 다만 현재 후크 하나만으로는 체감 대부분을 설명하지 못할 가능성이 높다.

## 3. 왜 `CombatManager.WaitForActionThenEndTurn`를 안 붙였는가

이 메서드 이름만 보면 단순히 "턴 종료 전에 조금 기다리는 함수"처럼 보인다.

하지만 디컴파일 결과는 다르다.

핵심 구조는 다음과 같다.

```csharp
await action.CompletionTask;
await AfterAllPlayersReadyToEndTurn(actionDuringEnemyTurn);
```

그리고 이어지는 `AfterAllPlayersReadyToEndTurn` 안에서는 다시:

```csharp
await WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction();
await EndPlayerTurnPhaseOneInternal();
```

즉 이 메서드는 "애니메이션 시간"을 기다리는 함수가 아니라, 특정 action의 완료와 전투 큐 상태를 기다린 뒤 턴 종료 phase를 진행하는 동기화 게이트다.

여기를 공격적으로 줄이면 생길 수 있는 문제:

- 아직 끝나지 않은 action completion을 건너뛴 것처럼 보이는 상태
- end turn phase가 너무 빨리 진행되면서 연출과 실제 상태가 어긋나는 문제
- 특정 조합에서 soft lock 또는 phase mismatch

그래서 이 함수는 "속도를 높이면 더 자연스러워질 수도 있는 후보"가 아니라, "잘못 건드리면 자연스러움을 바로 잃는 후보"에 가깝다.

## 4. 왜 `WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`도 위험한가

이 메서드도 이름만 보면 queue drain wait를 줄이기 좋아 보인다.

하지만 실제 구조는 다음 의미에 더 가깝다.

- 현재 액션이 player-driven인지 확인
- 필요하면 `TaskCompletionSource`를 걸어 둠
- `AfterActionExecuted` 이벤트를 구독
- "큐가 비었는지" 또는 "다음 action이 non-player-driven인지"를 관찰

즉 이것도 단순 sleep이 아니라 "큐 상태가 특정 조건에 도달할 때까지" 기다리는 함수다.

여기를 억지로 빠르게 만들면:

- player-driven action 경계가 무너질 수 있고
- 큐 관찰 조건 자체를 깨먹을 수 있으며
- 결국 기다림을 줄이는 게 아니라 동기화 의미를 건드리게 된다.

현재 `Cmd.CustomScaledWait`처럼 명시적인 duration 인자를 줄이는 것과는 성격이 완전히 다르다.

## 5. 왜 `ActionExecutor.ExecuteActions`는 더 위험한가

이 메서드는 이름 그대로 액션 큐 실행의 중심이다.

디컴파일 구조를 요약하면 대략 다음과 같다.

1. ready action을 가져온다.
2. `WaitForUnpause()`를 기다린다.
3. action을 실행한다.
4. action task가 끝날 때까지 frame 단위로 기다린다.
5. `CombatManager.Instance.CheckWinCondition()`를 호출한다.
6. 다음 action으로 넘어간다.

즉 이 함수는 "중간에 짧은 대기 하나 줄이면 되는 함수"가 아니라, 액션 실행 순서와 프레임 진행, 승패 체크까지 엮인 핵심 루프다.

여기에 손대면 얻을 수 있는 건 분명 있다.

- 더 공격적인 큐 drain
- 더 빠른 overall 템포

하지만 잃을 수 있는 것도 크다.

- action completion 타이밍 붕괴
- 승리/패배 체크 타이밍 어긋남
- 특정 액션이 한 프레임 덜 보이거나 더 보이는 문제
- 희귀 케이스 soft lock

따라서 이 메서드는 "나중에 꼭 다시 볼 가치가 있는 곳"은 맞지만, 현재처럼 1차 payload를 자연스럽게 유지하는 단계에서는 우선순위를 낮게 둔 게 맞다.

## 6. 그래서 지금 모드가 자연스럽게 느껴지는 이유

현재 모드가 비교적 자연스럽게 느껴지는 건 우연이 아니다.

일부러 다음 성격의 지점만 골라서 패치했기 때문이다.

- 애니메이션 스케일 인자
- 명시적인 wait duration 인자
- 유틸리티 timer duration 인자

즉 "동기화 조건"이 아니라 "시간 값"에 가까운 곳만 먼저 손댔다.

또한 전역 `Engine.time_scale`이나 오디오 pitch를 건드리지 않았기 때문에, 시각/오디오가 한 번에 같이 깨질 확률도 낮췄다.

## 7. 지금 무엇을 바꾸지 않기로 했는가

이번 정리에서는 다음 변경을 일부러 하지 않았다.

- `CombatManager.WaitForActionThenEndTurn` 패치
- `CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction` 패치
- `ActionExecutor.ExecuteActions` 패치
- 전역 `Engine.time_scale` 조작
- 오디오 API 조작

이유는 하나다.

지금 플레이 체감이 이미 "자연스럽다"는 피드백이 나온 상태에서, 더 공격적인 후크를 붙이는 건 개선보다 회귀 가능성이 더 크기 때문이다.

## 8. 다음에 안전하게 확장하려면

더 자연스러운 가속을 원한다면 다음 순서가 안전하다.

1. 실제 플레이 로그에서 아직 느린 장면을 구체적으로 분리한다.
2. 그 장면이 "단순 duration" 문제인지 "큐 동기화" 문제인지 먼저 구분한다.
3. duration 계열 leaf helper를 더 찾는다.
4. 마지막까지 leaf helper로 해결이 안 될 때만 `CombatManager` / `ActionExecutor`를 다시 검토한다.

즉 지금 단계에서 정답은 "더 많이 패치"가 아니라 "더 정확한 leaf hook을 찾기 전까지 core queue를 보존"이다.
