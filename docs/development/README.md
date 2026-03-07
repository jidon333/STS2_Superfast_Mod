# 개발 문서 안내

권장 읽기 순서는 다음과 같다.

1. [모딩 0부터 설명](MODDING_FROM_ZERO.md)
   - 모드를 거의 모르는 사람 기준으로 `pck`, `dll`, Harmony, 패치, resolver, modded 프로필 분리까지 처음부터 설명한다.
2. [모드 로딩 방식 비교](MOD_LOADING_STRATEGIES.md)
   - GUMM, GUMM bootstrap + C# payload, STS2 내장 네이티브 로더를 비교한다.
3. [네이티브 로드 체인](LOAD_CHAIN.md)
   - 게임 시작부터 `.pck`, `.dll`, manifest, Harmony patch, modded 저장 경로까지 이어지는 순서를 짧고 구조적으로 정리한다.
4. [초보자용 구조 설명](MOD_BEGINNER_GUIDE.md)
   - 이 저장소의 실제 파일 구조와 핵심 코드 위치를 빠르게 훑는다.
5. [배속 해석](SPEED_SEMANTICS.md)
   - `2.0`이 애니메이션에는 어떻게 적용되고, wait/timer에는 왜 역수 개념으로 적용되는지 설명한다.
6. [작업 기록](WORKLOG.md)
   - 어떤 시행착오를 거쳐 지금 구조에 도달했는지 시간 순서대로 정리한다.
7. [상세 조사 로그](DETAILED_INVESTIGATION_LOG.md)
   - 실제로 어떤 가설을 세웠고, 어떤 커맨드와 증거로 틀린 길을 버리고 현재 구조에 도달했는지 자세히 남긴다.
