# 개발 문서 안내

- [초보자용 구조 설명](MOD_BEGINNER_GUIDE.md)
  - 이 저장소가 지금 실제로 무엇을 하고 있는지, 핵심 코드가 어디인지, modded 프로필 복구는 왜 필요한지 설명한다.
- [모드 로딩 방식 비교](MOD_LOADING_STRATEGIES.md)
  - GUMM, GUMM bootstrap + C# payload, STS2 내장 네이티브 로더를 비교한다.
- [네이티브 로드 체인](LOAD_CHAIN.md)
  - 게임 시작부터 `.pck`, `.dll`, manifest, Harmony patch, modded 저장 경로까지 이어지는 순서를 정리한다.
- [작업 기록](WORKLOG.md)
  - DLL 조사, reflection 실패, GUMM 실험, `.pck` 생성, 의존성 문제 해결, modded 프로필 복구까지 시행착오를 시간 순서대로 정리한다.
