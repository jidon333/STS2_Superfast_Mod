# STS2 상세 조사 로그

이 문서는 이번 모딩 작업의 상세 조사 기록입니다.

목적:

1. 다음 작업자가 "지금 구조가 왜 이렇게 생겼는지"를 빠르게 이해하도록 한다.
2. 이미 실패한 가설과 시행착오를 반복하지 않게 한다.
3. 어떤 증거와 어떤 판단으로 현재 구조에 도달했는지 남긴다.

## 0. 초기 조건

작업 제약:

- live 바이너리 직접 패치 금지
- save / config 직접 변경 최소화
- Steam 원본 설치 기준으로 동작해야 함
- 되돌릴 수 있는 방법 우선

목표:

- STS2의 전투 속도를 자연스럽게 올리는 모드
- 처음 플레이 감상을 크게 해치지 않는 방향

## 1. 첫 가설 세우기

초기에는 세 가지 가능성을 열어두었습니다.

1. STS2가 native mod loader를 이미 갖고 있다
2. 그렇지 않으면 GUMM 같은 외부 bootstrap이 필요하다
3. 속도 구현은 결국 managed C# 메서드 패치가 될 가능성이 높다

즉 초반 질문은 두 개였습니다.

- 진입은 어디로 할 것인가
- payload는 어디를 건드릴 것인가

## 2. 설치 / 저장 경로 조사

가장 먼저 한 일은 설치본과 사용자 데이터를 확인하는 것이었습니다.

이유:

- 백업 / 복구 계획을 먼저 세워야 위험을 줄일 수 있음
- config / save 형식이 텍스트인지 바이너리인지 알아야 이후 조사 방식이 달라짐

확인한 내용:

- 게임 설치: `D:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2`
- 사용자 데이터: `C:\Users\jidon\AppData\Roaming\SlayTheSpire2`
- save / settings는 JSON 텍스트

결론:

- snapshot / restore 도구를 만들 수 있음
- save diff와 설정 diff가 쉬움

## 3. save / settings 파일 읽기

초기에는 다음 파일을 확인했습니다.

- `settings.save`
- `prefs.save`
- `progress.save`
- `current_run.save`
- `.backup` 파일들

여기서 나온 중요한 사실:

- `current_run.save`는 항상 존재하는 파일이 아님
- `settings.save` 안에 `mod_settings`가 있음
- `prefs.save` 안에 `fast_mode` 같은 속도 관련 값이 있음

이 단계에서 얻은 결론:

- snapshot 도구는 missing 파일을 정상 상태로 다뤄야 함
- 모드 경고 / 동의 플래그는 `settings.save`에 있을 가능성이 큼

## 4. 후보 훅 메서드 찾기

속도 모드를 만들려면 결국 어디를 빠르게 해야 하는지 알아야 했습니다.

초반 후보:

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `CombatManager.WaitForActionThenEndTurn`
- `CombatManager.WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
- `CombatState.GodotTimerTask`
- `ActionExecutor.ExecuteActions`

이 목록은 이후 `KnownPatchTargets.cs`의 바탕이 됐습니다.

## 5. PowerShell reflection 시도와 포기

처음에는 PowerShell reflection으로 `sts2.dll`을 읽어 메서드 시그니처를 바로 보려 했습니다.

하지만 실제로는:

- `.NET 9`
- 다수의 의존 DLL
- `MetadataLoadContext` 구성 비용

때문에 가성비가 좋지 않았습니다.

판단:

- "간단한 reflection"으로 끝날 단계가 아님
- ILSpy 기반 디컴파일이 더 빠르고 정확함

이 시점에서 조사는 reflection 중심에서 디컴파일 중심으로 이동했습니다.

## 6. GUMM을 먼저 붙인 이유

내장 로더 규칙이 확정되기 전에는 "모드 코드가 실제로 게임 시작 과정에 들어갈 수 있는가"를 먼저 검증해야 했습니다.

GUMM을 택한 이유:

- STS2는 Godot 게임
- `override.cfg` 기반 bootstrap이 가능
- 모드 코드 실행 여부를 빨리 검증하기 좋음

초기 구성:

- `override.cfg`
- `GUMM_mod_loader.tscn`
- `mod.cfg`
- `mod.gd`
- `GUMM_mod.gd`

## 7. GUMM 첫 실패

첫 GUMM 시도는 "모드 발견"까지는 갔지만 parse error로 끝났습니다.

핵심 원인:

- 우리가 만든 최소 `GUMM_mod.gd`에 GUMM이 기대하는 helper가 부족했음
- `get_full_path()` 같은 베이스 동작이 빠져 있었음

교훈:

- bootstrap 로더는 "대충 비슷한 파일"로는 안 된다
- base script를 정확히 맞춰야 한다

## 8. GUMM 두 번째 성공

공식 GUMM 4.x 쪽 base script를 맞춰 넣은 뒤:

- `Loading mod: STS2 Speed Skeleton`
- bootstrap 로그
- 메인 메뉴 진입

까지 확인했습니다.

이 단계에서 확인한 것:

- Godot 레벨 bootstrap은 가능
- 로그 삽입과 진단용 경로로는 충분히 유효

하지만 동시에:

- 최종 구현을 GUMM 위에 얹어야 할 필요는 여전히 불명확

했습니다.

## 9. direct exe 실행이 틀린 경로라는 점

한때 `SlayTheSpire2.exe`를 직접 실행하는 시도를 했습니다.

결과:

- Steam app ID 초기화 실패
- Steamworks 관련 로그 문제

결론:

- 실기 테스트는 반드시 Steam 경유여야 함
- 이후 기준은 `steam.exe -applaunch 2868840` 또는 Steam URI

로 고정했습니다.

## 10. 커뮤니티 배포 형식에서 얻은 단서

다른 비공식 모드가:

- `mods` 폴더 생성
- `pck`, `dll`, `txt` 복사
- `txt` 숫자 수정

만으로 동작한다는 사례가 나왔습니다.

여기서 얻은 강한 신호:

- GUMM이 정석 경로가 아닐 가능성이 큼
- STS2 자체가 이미 native `mods` route를 갖고 있을 가능성이 높음

이것이 native route 재조사를 가속했습니다.

## 11. ILSpy로 `ModManager`를 깐 결정적 순간

이후 `ModManager`를 디컴파일해서 native 규칙을 확인했습니다.

실제 규칙:

1. `<game dir>\mods`를 스캔
2. `.pck` 발견
3. 같은 basename의 `.dll` 탐색
4. resource pack load
5. `res://mod_manifest.json` 확인
6. `pck_name` 검증
7. DLL 로드
8. `Harmony.PatchAll`

이 시점에서 내린 결론:

- 최종 구현은 native route로 간다
- GUMM은 bootstrap / fallback / 조사 기록으로만 남긴다

## 12. 모드 동의 플래그 조사

native `.pck`를 넣었는데 로더가 처음에는 모드를 건너뛰는 상황이 있었습니다.

원인은 `settings.save` 안의 `mod_settings.mods_enabled`였습니다.

즉:

- 모드 경고 / 동의 UI가 한 번 필요
- 그 플래그가 켜지기 전에는 로더가 모드를 실제 활성화하지 않을 수 있음

이 사실은 이후 사용자 안내문에 반영했습니다.

## 13. `.pck` 생성 실험

### 시도 1: `PCKPacker`

처음에는 headless Godot 프로젝트를 만들어 `PCKPacker`를 직접 썼습니다.

겉보기에는 `.pck`가 만들어졌지만, STS2는:

- manifest가 없다고 하거나
- 내부 구조를 기대와 다르게 보거나

했습니다.

결론:

- 임의 `PCKPacker` 산출물과 STS2 로더가 기대하는 export pack은 같다고 가정하면 안 됨

### 시도 2: 공식 `--export-pack`

Godot 4.5.1 공식 `--export-pack` 경로로 전환했습니다.

여기서는 실제로 로더가 pack을 읽기 시작했습니다.

추가로 드러난 문제:

- `pck_name`에 확장자 없는 basename을 써야 함

이 수정 후 native 로드가 성공했습니다.

## 14. 첫 native 로드 성공

확인한 로그:

- `.pck` 발견
- DLL 로드
- `Harmony.PatchAll`
- `Finished mod initialization`
- `--- RUNNING MODDED! ---`

이 시점부터 GUMM은 더 이상 주 경로가 아니게 됐습니다.

## 15. 첫 payload 설계

첫 payload는 보수적으로 시작했습니다.

선정 이유:

- animation / explicit wait / timer helper는 상대적으로 안전
- 바로 `CombatManager` / `ActionExecutor`로 가면 범위가 너무 큼

첫 패치:

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`

## 16. `Sts2Speed.Core.dll` 누락 문제

메인 DLL은 읽히지만 `Sts2Speed.Core.dll`을 못 찾는 문제가 발생했습니다.

원인:

- STS2 로더는 메인 DLL까지는 읽음
- 그 이후 dependency probing은 자동으로 다 해주지 않음

해결:

- `ModAssemblyResolver.cs`
- `AssemblyResolve` / `AssemblyLoadContext.Default.Resolving`

를 등록해 모드 폴더를 기준으로 dependency를 찾게 했습니다.

## 17. runtime log 추가

게임 로그만으로는:

- 로더 문제
- 설정 로드 문제
- 패치 적용 문제

를 분리하기 어려웠습니다.

그래서:

- `mods\sts2speed.runtime.log`

를 추가했습니다.

이 로그는 이후 모든 실전 검증의 핵심 도구가 됐습니다.

## 18. modded profile 분리 문제와 복구

모드를 켰더니 진행이 초기화된 것처럼 보였고, 조사 결과 원인은 `profileN`과 `modded/profileN` 분리였습니다.

복구 절차:

1. 기존 modded 프로필 백업
2. `profileN`을 `modded/profileN`으로 복제
3. 해시 비교

이후 이 과정을 `sync-modded-profile`로 자동화했습니다.

## 19. speed semantics 버그 발견

초기 구현에서 `2.0`을 넣으면:

- animation은 빨라지고
- wait / timer는 길어지는

버그가 있었습니다.

원인:

- wait / timer를 duration이 아니라 speed처럼 같은 곱셈으로 처리했음

해결:

- `SpeedScaleMath.cs` 도입
- animation / delta는 `*`
- duration은 `/`

## 20. flat config로 UX 정리

중간 단계에서는:

- `Sts2Speed.speed.txt`
- grouped JSON

등이 섞여 있었습니다.

이후 최종 UX는:

- `Sts2Speed.config.json`
- flat `baseSpeed + ...Speed`
- 모든 숫자는 `클수록 빠름`

으로 정리했습니다.

legacy 입력은 fallback으로만 남겼습니다.

## 21. delta 패치 확대

Spine + wait만으로는 아직 일부가 느리게 느껴졌습니다.

그래서 전투 UI / VFX 쪽 `_Process(delta)` 패치를 추가했습니다.

추가 지점:

- `NTargetingArrow`
- `NIntent`
- `NStarCounter`
- `NEnergyCounter`
- `NBezierTrail`
- `NCardTrail`
- `NDamageNumVfx`
- `NHealNumVfx`

이 단계에서 체감이 크게 좋아졌습니다.

## 22. 인게임 설정 UI 조사

사용자가 Modding Screen에서 우리 모드가 보인다는 피드백이 있었고, 이때부터 "인게임에서 값까지 바꿀 수 있는가"를 조사했습니다.

조사 대상:

- `NModdingScreen`
- `NModInfoContainer`
- `NModMenuRow`
- `NSettingsSlider`
- `NSettingsTickbox`

결론:

- 공식 per-mod custom settings API는 뚜렷하지 않음
- 하지만 `NModInfoContainer.Fill`에 Harmony로 UI를 주입하는 것은 가능

## 23. 인게임 설정 UI 첫 구현

처음 UI는:

- `+ / -` 버튼
- 값 표시 라벨
- 설명 텍스트와 같은 영역 공유

형태였습니다.

바로 드러난 문제:

- 원래 설명 텍스트와 레이아웃이 겹침
- `+ / -`를 눌러도 저장은 되는데 숫자 피드백이 안 보임
- generic reflection method를 잘못 집어서 refresh 예외 발생
- 계산용 `effective*` 필드가 config에 저장되는 버그

## 24. 인게임 설정 UI 수정

수정 내용:

- 원래 description 라벨은 숨기고 패널만 노출
- 값 표시를 button text 기반으로 바꿔 즉시 갱신
- reflection invoke에서 generic method 제외
- 저장 시 editable 필드만 직렬화
- live DLL, core DLL, pck까지 다시 배포

결과:

- 패널 표시 성공
- 값 저장 성공
- UI 숫자 즉시 반영 성공
- live config 오염 버그 제거

## 25. 실제 런타임 검증

실전 로그 기준 확인된 것:

- `settings refreshed: ...`
- `spine time scale applied`
- `queue wait scale applied`
- `combat ui delta scale applied`
- `combat vfx delta scale applied`

반면 아직 꾸준히 보이지 않는 것:

- `effect delay scale applied`

즉 현재 남은 과제는 "패치가 아예 안 붙는다"가 아니라 "effect path의 실제 커버 범위가 좁을 수 있다" 쪽입니다.

## 26. `CombatManager` / `ActionExecutor` 재검토

초기에는 이 메서드들이 "다음에 붙이면 될 wait 훅"처럼 보였습니다.

하지만 재디컴파일 결과:

- `WaitForActionThenEndTurn`
  - action completion과 turn-end phase를 잇는 동기화 게이트
- `WaitUntilQueueIsEmptyOrWaitingOnNonPlayerDrivenAction`
  - queue state barrier
- `ExecuteActions`
  - 액션 실행 루프의 중심

즉 단순 duration hook이 아니었습니다.

그래서 현재 결론은:

- 지금은 붙이지 않는 것이 맞다
- 필요해도 마지막 단계에서 다시 검토한다

입니다.

## 27. 문서와 배포 마감

마지막 단계에서는:

- 사용자 README 정리
- 튜닝 문서 추가
- 배포 폴더 정리
- 인게임 설정 스크린샷 추가
- README에 스크린샷 위치 조정

을 진행했습니다.

## 28. 현재 구조에 대한 최종 판단

현재 구조는 다음 이유로 유지 가치가 높습니다.

- native loader 경로를 따름
- UI까지 live에서 조절 가능
- 사용자가 읽을 설정 규칙이 단순함
- 남은 느린 구간은 무리한 코어 루프 패치보다 leaf hook 추가로 해결하는 쪽이 안전함

즉 지금 상태는 "프로토타입"이 아니라 "실사용 가능한 1차 완성형"에 가깝습니다.
