# STS2 Speed 배속 해석

## 요약

현재 모드의 사용자 입력값은 `배속(speed multiplier)`로 해석한다.

현재 배포 기본값은 `2.0`이다.

- `2.0` = 전체 체감 속도를 2배로 빠르게 만든다.
- `0.5` = 전체 체감 속도를 절반으로 느리게 만든다.
- `1.0` = 바닐라와 동일하다.

## 항목별 해석

### Spine 애니메이션

`spineTimeScale`은 애니메이션 재생 속도에 직접 곱한다.

- `2.0` -> `SetTimeScale(scale * 2.0)`
- `0.5` -> `SetTimeScale(scale * 0.5)`

즉 이 축은 일반적인 배속 개념 그대로다.

### Queue wait / effect delay

`queueWaitScale`, `effectDelayScale`은 "대기시간 길이"가 아니라 "진행 속도"로 해석한다.

그래서 내부 계산은 곱셈이 아니라 역수 개념을 사용한다.

- `2.0` -> wait duration을 `1 / 2.0`으로 줄인다.
- `0.5` -> wait duration을 `1 / 0.5`로 늘린다.

공식으로 쓰면 다음과 같다.

```text
adjustedDuration = originalDuration / speedMultiplier
```

## 왜 이렇게 바꿨는가

초기 구현은 `Sts2Speed.speed.txt` 값을 Spine, queue wait, effect delay에 모두 그대로 곱했다.

그 결과:

- 애니메이션은 `2.0`에서 빨라졌지만
- wait / timer는 `2.0`에서 오히려 길어졌다

즉 사용자 입장에서는 "배속을 올렸는데 일부가 더 느려지는" 버그가 생겼다.

이 문제를 막기 위해 wait / timer 계열은 `duration /= multiplier`로 수정했다.

## 코드 위치

- `src/Sts2Speed.Core/Configuration/SpeedScaleMath.cs`
- `src/Sts2Speed.ModSkeleton/Runtime/RuntimePatchContext.cs`

`SpeedScaleMath`는 공통 계산 규칙을 담당하고, `RuntimePatchContext`는 실제 Harmony Prefix에서 그 계산을 호출한다.
