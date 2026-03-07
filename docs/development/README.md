# 개발 문서 안내

현재 구조를 빠르게 파악하려면 아래 순서로 읽는 것이 좋습니다.

1. `PROJECT_STATUS.md`
   - 저장소의 현재 상태와 실제 배포 형식을 요약합니다.
2. `MODDING_FROM_ZERO.md`
   - 모드 로더, `.pck`, `.dll`, Harmony 같은 개념을 처음부터 설명합니다.
3. `LOAD_CHAIN.md`
   - STS2가 `mods` 폴더의 파일을 어떤 순서로 읽고 패치를 적용하는지 정리합니다.
4. `SPEED_SEMANTICS.md`
   - 현재 설정 모델인 flat `baseSpeed + ...Speed`와 “클수록 빠름” 규칙을 설명합니다.
5. `PENDING_HOOKS_AND_RISKS.md`
   - 왜 일부 후보 훅을 아직 보류했는지 위험도 관점에서 설명합니다.
6. `WORKLOG.md`
   - 시행착오를 시간순으로 정리한 작업 로그입니다.
7. `DETAILED_INVESTIGATION_LOG.md`
   - 실제 조사/가설/반례/결론을 가장 상세하게 기록한 문서입니다.

주의:

- `WORKLOG.md`, `DETAILED_INVESTIGATION_LOG.md`에는 과거 단계의 `Sts2Speed.speed.txt` 기반 실험 기록이 남아 있습니다.
- 현재 배포 기준의 설정 파일은 `Sts2Speed.config.json` 입니다.
