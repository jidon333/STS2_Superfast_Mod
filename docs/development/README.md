# 개발 문서 안내

현재 저장소의 최신 상태를 빠르게 이해하려면 아래 순서로 읽는 것이 좋습니다.

1. `PROJECT_STATUS.md`
   - 현재 배포 형식, 적용 범위, 설정 표면, 인게임 UI 상태를 요약합니다.
2. `MODDING_FROM_ZERO.md`
   - `.pck`, `.dll`, Harmony, resolver, 인게임 설정 UI 같은 개념을 처음부터 설명합니다.
3. `LOAD_CHAIN.md`
   - STS2가 `mods` 폴더를 읽고 패치를 설치한 뒤, 설정 파일과 인게임 UI를 어떻게 연결하는지 설명합니다.
4. `SPEED_SEMANTICS.md`
   - `baseSpeed + ...Speed`가 실제 런타임에 어떤 방식으로 해석되는지 정리합니다.
5. `PENDING_HOOKS_AND_RISKS.md`
   - 왜 어떤 훅은 아직 붙이지 않았는지, 어떤 리스크가 남아 있는지 설명합니다.
6. `WORKLOG.md`
   - 조사와 구현 과정을 시간순으로 압축해서 기록한 로그입니다.
7. `DETAILED_INVESTIGATION_LOG.md`
   - 시행착오, 판단 근거, 실패 원인까지 더 자세하게 남긴 문서입니다.
8. `MOD_LOADING_STRATEGIES.md`
   - GUMM, GUMM bootstrap + C# payload, 최종 native route를 비교합니다.
9. `MOD_BEGINNER_GUIDE.md`
   - 저장소 파일 구조와 실제 코드 위치를 빠르게 확인할 때 봅니다.

추가 사용자 문서:

- `docs\TUNING_KO.md`
  - 각 설정 항목이 실제로 무엇을 의미하는지, 어떤 값을 먼저 만져야 하는지 설명합니다.

현재 문서 기준 핵심 변화:

- 배포 표면은 `mods + pck + dll + json`
- 설정 표면은 flat `baseSpeed + ...Speed`
- `Sts2Speed.speed.txt`는 legacy fallback
- 모드 설정은 `mods\Sts2Speed.config.json` 또는 인게임 `설정 -> 모드 -> STS2 Speed Skeleton` 화면에서 변경 가능
- 인게임 UI는 `NModInfoContainer.Fill`에 Harmony Postfix로 주입됩니다
