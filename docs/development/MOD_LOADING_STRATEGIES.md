# STS2 모드 로딩 방식 비교

이 문서는 현재까지 검토한 세 가지 방식을 비교합니다.

1. GUMM 방식
2. GUMM bootstrap + C# payload 방식
3. 네이티브 `mods + pck + dll + txt` 방식

결론부터 말하면, 현재 이 저장소의 최종 목표는 3번입니다. 1번은 이미 실기 검증에 성공한 우회/진단 경로이고, 2번은 1번에서 3번으로 넘어가기 전의 중간 설계안이었습니다.

## 0. Steam Workshop과 모드 로더는 같은 게 아니다

먼저 개념을 분리해야 합니다.

- Steam Workshop: 배포 채널
- 모드 로더: 게임이 모드 파일을 실제로 읽고 실행하는 방식

즉 Workshop에 올라갔다고 해서 반드시 GUMM을 쓰는 건 아닙니다. 반대로 GUMM 모드도 굳이 Workshop이 없어도 수동 설치로 동작할 수 있습니다.

정리하면:

- Workshop은 "파일을 어떻게 받느냐"
- 로더는 "받은 파일을 게임이 어떻게 실행하느냐"

입니다.

## 1. GUMM 방식

### 원리

GUMM은 Godot 기본 기능이 아니라, Godot의 `override.cfg` 오버라이드 메커니즘을 이용하는 외부 로더입니다.

흐름:

1. 게임 폴더에 `override.cfg`를 둔다.
2. `run/main_scene`를 원래 게임 씬 대신 `GUMM_mod_loader.tscn`로 바꾼다.
3. GUMM 로더가 먼저 뜬다.
4. GUMM 로더가 외부 모드 폴더들의 `mod.cfg`, `mod.gd`를 읽는다.
5. 모드 초기화가 끝나면 원래 게임 씬 `res://scenes/game.tscn`로 넘긴다.

실제 관련 파일:

- [GummIntegration.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/GummIntegration.cs)
- [mod.cfg](C:/Users/jidon/source/repos/STS2ModeTest/artifacts/package-layout/Sts2Speed/mod.cfg)
- [mod.gd](C:/Users/jidon/source/repos/STS2ModeTest/artifacts/package-layout/Sts2Speed/mod.gd)

### 장점

- 게임이 외부 모드 스크립트를 실제로 읽는지 빨리 검증할 수 있다.
- 리소스 교체, 간단한 부트스트랩, 로그 검증에 유리하다.
- 이미 이 저장소에서 메인 메뉴 진입과 bootstrap 로그까지 실기 검증이 끝났다.

### 단점

- 속도 모드의 핵심 타깃은 managed C# 메서드인데, GUMM의 주 무대는 GDScript/리소스 계층이다.
- 실제로 GUMM 로더 내부에서 `remove_child()` 타이밍 경고가 한 번 남아 있다.
- 최종 목적이 `SuperFastMode`류라면 레이어가 한 단계 위에 있어서 정밀한 패치엔 불리하다.

### 언제 쓰는가

- 외부 스크립트가 타는지 먼저 확인할 때
- 로더 경로를 빠르게 검증할 때
- 최종 경로가 막혔을 때 fallback/diagnostics 경로로 유지할 때

## 2. GUMM bootstrap + C# payload 방식

### 원리

이 방식은 GUMM을 최종 해법으로 쓰는 게 아니라, "초기 진입점 확보용"으로만 쓰는 방식입니다.

흐름:

1. GUMM이 `mod.gd`를 로드한다.
2. `mod.gd`는 설정 파일과 런타임 상태를 읽는다.
3. 그 다음 실제 속도 변경은 GDScript가 아니라 managed C# payload가 맡는다.
4. 즉 GUMM은 bootstrap, 진짜 payload는 DLL 패치다.

이 방식이 등장한 이유:

- GUMM bootstrap은 이미 실기에서 살아 있음을 확인했기 때문
- 반대로 우리가 바꾸고 싶은 대상은 C# 메서드들이기 때문

대표 후보:

- [KnownPatchTargets.cs](C:/Users/jidon/source/repos/STS2ModeTest/src/Sts2Speed.Core/Planning/KnownPatchTargets.cs)
  - `MegaAnimationState.SetTimeScale`
  - `MegaTrackEntry.SetTimeScale`
  - `CombatManager.WaitForActionThenEndTurn`
  - `ActionExecutor.ExecuteActions`

### 장점

- GUMM이 이미 살아 있으니 첫 진입점 확보가 쉽다.
- 실제 속도 변경은 managed 레이어에서 수행할 수 있어 목표 적합도가 올라간다.
- 로더/설정 검증과 payload 구현을 분리할 수 있다.

### 단점

- 로더는 GUMM, payload는 C#이라서 구조가 이중화된다.
- 최종적으로 내장 모드 시스템이 열려 있다면, 굳이 GUMM을 앞단에 둘 이유가 약하다.
- 유지보수 관점에서는 "중간 단계 설계"에 가깝다.

### 언제 유력했는가

- GUMM 부트스트랩은 성공했지만
- 내장 모드 경로는 아직 실증 전이었을 때

즉, 네이티브 경로를 확정하기 전까지 가장 현실적인 중간안이었습니다.

## 3. 네이티브 `mods + pck + dll + txt` 방식

### 원리

이 방식은 게임 자체의 내장 모드 로더가 `mods` 폴더를 보고 `.pck`와 `.dll`을 읽는다고 가정하는 방식입니다.

현재 이 방식이 최종 방향이 된 근거는 두 가지입니다.

1. 커뮤니티 배포 예시
   - 설치법이 `Slay the Spire 2\\mods` 폴더에 `pck`, `dll`, `txt`를 넣는 형태
   - GUMM 특유의 `override.cfg` 설치 요구가 없었음
2. 로컬 설치본 문자열 단서
   - `TryLoadModFromPck`
   - `LoadMods`
   - `ModsDirectory`
   - `SteamWorkshop`
   - `ModInitializerAttribute`

즉, 게임 내부에 이미:

- `mods` 디렉토리
- `.pck` 로드
- managed initializer

개념이 들어 있다는 뜻으로 해석하는 것이 가장 자연스럽습니다.

### 예상 로드 흐름

1. 게임이 `Slay the Spire 2\\mods`를 스캔
2. `.pck`를 마운트
3. 연결된 managed DLL을 로드
4. DLL이 모드 초기화 코드를 실행
5. 같은 폴더의 `.txt` 설정 파일에서 배속 값을 읽음
6. 실제 속도 패치를 수행

### 장점

- 목표 레이어와 가장 가깝다.
- 커뮤니티 배포 형태와도 맞는다.
- GUMM 없이도 돌아가는 "게임 본래의 모드 경로"일 가능성이 높다.
- Workshop이 붙더라도 결국 이 구조를 배포하는 식으로 이어질 가능성이 높다.

### 단점

- 현재 워크스페이스에는 `.pck` 생성기가 아직 없다.
- 파일명 규약과 폴더 평탄화 여부가 아직 100% 확정은 아니다.
- live 검증은 아직 못 했다.

### 현재 구현 상태

이 저장소는 이제 네이티브 스테이징까지는 합니다.

생성 명령:

```powershell
dotnet run --project src/Sts2Speed.Tool -- materialize-native-package --layout subdir
dotnet run --project src/Sts2Speed.Tool -- materialize-native-package --layout flat
```

출력 위치 예시:

- [native-package-layout/subdir/mods/Sts2Speed](C:/Users/jidon/source/repos/STS2ModeTest/artifacts/native-package-layout/subdir/mods/Sts2Speed)

현재 생성하는 파일:

- `manifest.json`
- `README.native.txt`
- `Sts2Speed.speed.txt`
- `native-loader-hints.json`
- `Sts2Speed.ModSkeleton.dll`
- `Sts2Speed.Core.dll`

현재 일부러 빠져 있는 파일:

- `sts2-speed-skeleton.pck`

즉, live deploy가 아니라 "배치 규약 고정 + 누락 필수 아티팩트 명시" 단계까지 구현된 상태입니다.

## 4. 왜 최종 방향을 3번으로 바꿨는가

이유는 단순합니다.

- GUMM은 외부 로더다.
- 우리가 본 커뮤니티 모드는 GUMM 설치 절차 없이 돌아간다.
- 로컬 설치본에도 `mods`/`.pck`/initializer 흔적이 있다.

그래서 지금 판단은:

- GUMM: 진단/백업/우회 경로
- GUMM bootstrap + C# payload: 과도기 설계
- 네이티브 `mods + pck + dll + txt`: 최종 목표

입니다.

## 5. 현재 추천 전략

현재 기준 추천 우선순위:

1. 네이티브 패키지 규약을 고정한다.
2. `.pck` 생성 또는 대체 경로를 확인한다.
3. 그 뒤 live `mods` 폴더에서 내장 로더를 검증한다.
4. GUMM은 디버깅/우회 경로로만 유지한다.

즉, 이제 핵심 blocker는 로더 자체가 아니라 `.pck` 생성/패키징 규약 확정입니다.
