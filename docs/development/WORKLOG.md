# STS2 Speed Mod 작업 기록

이 문서는 조사와 구현 과정을 시간순으로 압축해서 기록합니다.

더 세부적인 근거와 실패 원인까지 보려면 `DETAILED_INVESTIGATION_LOG.md`를 같이 보면 됩니다.

## 1. 초기 가설

처음에는 두 가지를 동시에 열어두고 시작했습니다.

1. STS2가 native mod route를 이미 갖고 있을 수 있다
2. 그렇지 않으면 GUMM 같은 외부 bootstrap이 필요할 수 있다

그래서 초반에는:

- save / settings 조사
- DLL 메타데이터 조사
- GUMM bootstrap 실험

을 병행했습니다.

## 2. 설치 / 저장 경로 확인

먼저 게임과 사용자 데이터 경로를 확인하고, save / config 파일이 JSON 텍스트라는 점을 확인했습니다.

이 단계에서:

- snapshot / restore 설계
- modded profile 분리 가정

이 정리됐습니다.

## 3. 후보 훅 탐색

초기 후보로 다음 메서드를 뽑았습니다.

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `CombatManager.WaitForActionThenEndTurn`
- `CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
- `CombatState.GodotTimerTask`
- `ActionExecutor.ExecuteActions`

여기서 방향은 "전역 time scale"보다 "선택적 leaf hook"으로 굳어졌습니다.

## 4. PowerShell reflection 실패

`sts2.dll`을 PowerShell reflection으로 바로 읽으려 했지만 `.NET 9`와 의존성 구조 때문에 효율이 나쁘다고 판단했습니다.

이후부터는 ILSpy / 디컴파일 중심으로 방향을 바꿨습니다.

## 5. GUMM 실험

내장 로더 규칙이 확정되기 전에는 GUMM으로 먼저 bootstrap을 검증했습니다.

흐름:

- `override.cfg`
- `GUMM_mod_loader.tscn`
- `mod.cfg`
- `mod.gd`
- `GUMM_mod.gd`

처음엔 parse error가 났고, 이후 공식 GUMM base script를 맞춰 넣어 부팅과 로그까지는 성공했습니다.

이 단계의 결론:

- GUMM은 유효한 진단 경로
- 하지만 최종 구현 경로로는 과함

## 6. 커뮤니티 배포 형식 확인

다른 비공식 모드가 `mods + pck + dll + txt` 형태로 배포된다는 점을 보고, native route 가설이 강해졌습니다.

## 7. `ModManager` 디컴파일

`ModManager`를 디컴파일해서 native 규칙을 확정했습니다.

핵심:

- `mods` 폴더 스캔
- `.pck` 발견
- 같은 basename의 `.dll` 탐색
- `mod_manifest.json` 필요
- `pck_name` 검증
- `Harmony.PatchAll`

이 시점부터 최종 경로는 native route로 확정됐습니다.

## 8. `.pck` 생성 시행착오

처음에는 `PCKPacker`를 직접 써서 `.pck`를 만들었지만, STS2 로더가 기대하는 형식과 맞지 않아 실패했습니다.

이후 Godot 공식 `--export-pack` 경로로 전환했고, 이것이 실제로 성공했습니다.

추가로 `pck_name`은 확장자 없는 basename과 일치해야 한다는 점도 이 과정에서 확인했습니다.

## 9. 첫 native 로드 성공

native `.pck`와 matching DLL 로드, `Harmony.PatchAll`, `--- RUNNING MODDED! ---`까지 확인했습니다.

이로써 GUMM은 더 이상 최종 구현 경로가 아니게 됐습니다.

## 10. 첫 payload

처음 payload는 가장 안전한 지점만 선택했습니다.

- Spine 배속
- explicit wait
- timer helper

즉:

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`

## 11. 추가 의존 DLL 문제 해결

메인 DLL이 로드돼도 `Sts2Speed.Core.dll`을 못 찾는 문제가 있었습니다.

이 문제는 assembly resolver를 추가해서 해결했습니다.

## 12. modded profile 복구

모드를 켰더니 진행이 초기화된 것처럼 보였고, 조사 결과 실제 원인은 `profileN`과 `modded/profileN` 분리였습니다.

해결:

- 수동 복구
- 이후 `sync-modded-profile` 자동화

## 13. speed semantics 버그 발견

초기 구현에서 `2.0`을 넣으면:

- animation은 빨라지고
- wait / timer는 오히려 길어지는

버그가 있었습니다.

이후 `SpeedScaleMath`를 만들어:

- animation / delta는 `*`
- duration은 `/`

로 통일했습니다.

## 14. flat config 스키마 도입

초기 설정은 `speed.txt` 하나였고, 이후 grouped JSON을 거쳐 최종적으로 flat `baseSpeed + ...Speed` 구조로 정리했습니다.

핵심 원칙:

- 사용자 기준 규칙은 `클수록 빠름`
- 초보자는 `baseSpeed`만 조절

## 15. delta 계열 추가

Spine + wait만으로는 아직 일부가 느리게 느껴져서, 전투 UI / VFX delta 패치를 추가했습니다.

추가 대상:

- `NTargetingArrow`
- `NIntent`
- `NStarCounter`
- `NEnergyCounter`
- `NBezierTrail`
- `NCardTrail`
- `NDamageNumVfx`
- `NHealNumVfx`

## 16. 인게임 설정 UI 추가

이후 "파일을 열지 않고도 값을 바꿀 수 있으면 좋겠다"는 방향으로 인게임 UI를 붙였습니다.

접근:

- `NModInfoContainer.Fill` Postfix
- 선택된 모드가 우리 모드일 때만 패널 표시
- `+ / -` 버튼과 toggle 버튼으로 config 저장

초기 문제:

- 레이아웃 겹침
- 값은 저장되는데 UI 숫자 즉시 갱신이 안 보임
- generic reflection method를 잘못 집는 예외
- 계산용 `effective*` 값이 config에 저장되는 버그

수정 후:

- 설명 텍스트 숨김
- 값 표시 즉시 갱신
- editable 필드만 JSON 저장
- live 파일과 인게임 UI 둘 다 정상 동작

## 17. 문서와 배포 정리

마지막 단계에서는:

- 사용자 README 정리
- 튜닝 문서 정리
- 배포 폴더 정리
- 인게임 설정 스크린샷 추가
- 기본 추천값 `baseSpeed = 3`

를 반영했습니다.

## 18. 현재 결론

현재 모드는:

- native loader 경로 확정
- 실전 사용 가능
- 인게임 설정 가능
- 문서화 완료

상태입니다.

남은 일은 "필수 구현"보다 "선택적 개선"에 가깝습니다.
