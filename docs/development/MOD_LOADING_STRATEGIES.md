# STS2 모드 로딩 방식 비교

이 문서는 지금까지 검토한 세 가지 방식의 원리, 장단점, 현재 판단을 정리한다.

비교 대상은 다음 세 가지다.

1. `GUMM` 단독 방식
2. `GUMM bootstrap + C# payload` 방식
3. STS2 내장 `mods + pck + dll + txt` 방식

결론부터 말하면 현재 최종 목표는 `3번`이다. `1번`은 진단과 우회 경로로 유효했고, `2번`은 중간 단계로는 합리적이었지만 지금은 우선순위가 내려갔다.

## 1. GUMM 단독 방식

### 원리

GUMM은 Godot 기본 제공 기능이 아니라, Godot의 `override.cfg` 메커니즘을 이용하는 외부 로더다.

동작 순서는 다음과 같다.

1. 게임 폴더에 `override.cfg`를 둔다.
2. `run/main_scene`를 원래 씬 대신 `GUMM_mod_loader.tscn`로 바꾼다.
3. GUMM 로더가 `mod_list`에 등록된 디렉토리의 `mod.cfg`, `mod.gd`를 읽는다.
4. 각 모드 초기화가 끝나면 원래 게임 씬 `res://scenes/game.tscn`로 넘긴다.

### 이 저장소에서 실제로 한 일

- `override.cfg` 작성
- `GUMM_mod_loader.tscn` 배치
- `mod.cfg`, `mod.gd`, `GUMM_mod.gd`가 포함된 패키지 생성
- Steam `-applaunch 2868840`로 실제 부팅
- 로그에서 `Loading mod: STS2 Speed Skeleton` 확인

### 장점

- 외부 스크립트가 실제로 게임 시작 시점에 실행되는지 빠르게 검증할 수 있다.
- 리소스 교체, 초기 스크립트 실행, 진단용 로그 삽입에 적합하다.
- 공식 내장 모딩 경로가 불명확할 때 우회 경로로 쓰기 좋다.

### 단점

- 우리가 실제로 건드리고 싶은 대상은 대부분 managed C# 메서드다.
- GUMM의 기본 강점은 GDScript와 리소스 레이어지, Harmony 기반 C# 메서드 패치는 아니다.
- 구조가 `Godot script -> bridge -> managed payload` 형태가 되기 쉬워서 불필요하게 한 층 더 거친다.

### 현재 판단

지금은 “정석 구현 경로”가 아니라 “진단/부트스트랩 경로”로 유지한다.

## 2. GUMM bootstrap + C# payload

### 원리

이 방식은 GUMM을 최종 구현으로 쓰지 않고, “모드 진입점 확보”에만 사용한다.

흐름은 다음과 같다.

1. GUMM이 `mod.gd`를 로드한다.
2. `mod.gd`가 설정 파일이나 외부 DLL 경로를 준비한다.
3. 실제 속도 변경은 managed C# payload가 담당한다.

즉 GUMM은 부트스트랩이고, 본체는 DLL 패치다.

### 왜 한때 유력했는가

- GUMM 경로가 실기에서 먼저 살아 있음을 확인했다.
- 속도 모드의 본체는 `MegaAnimationState.SetTimeScale`, `Cmd.CustomScaledWait` 같은 C# 메서드 후킹이 더 자연스럽다.
- 그래서 “진입은 GUMM, 실제 속도 변경은 C#” 구조가 중간 단계로 합리적이었다.

### 장점

- GUMM 검증 성과를 버리지 않고 활용할 수 있다.
- GDScript와 managed patch 역할을 분리할 수 있다.
- 내장 모드 경로가 막혀 있을 때 fallback으로 좋다.

### 단점

- 최종 경로가 명확해진 뒤에는 구조가 이중화된다.
- 유지보수 포인트가 늘어난다.
- `override.cfg` 같은 게임 폴더 수정이 계속 필요하다.

### 현재 판단

내장 네이티브 로더가 실제로 확인된 시점부터 우선순위가 내려갔다. 지금은 fallback 설계로만 남겨둔다.

## 3. STS2 내장 `mods + pck + dll + txt`

### 원리

이 방식은 게임 자체가 `mods` 폴더를 스캔하고, `.pck`와 `.dll`을 로드하는 구조를 그대로 따른다.

실제 디컴파일로 확인한 규칙은 다음과 같다.

1. 게임은 `<game dir>\\mods`를 연다.
2. 재귀적으로 `.pck`를 찾는다.
3. `.pck` basename과 같은 이름의 `.dll`을 찾는다.
4. `ProjectSettings.LoadResourcePack(...)`로 `.pck`를 마운트한다.
5. `res://mod_manifest.json`을 읽는다.
6. manifest의 `pck_name`이 `.pck` basename과 같아야 한다.
7. `ModInitializerAttribute`가 없으면 로더가 `Harmony.PatchAll(assembly)`를 호출한다.

### 필요한 파일

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `mod_manifest.json` inside pck
- `Sts2Speed.speed.txt`
- 추가 의존성이 있다면 같은 폴더의 `.dll`

### 이 저장소에서 실제로 검증한 것

- `.pck`는 Godot 공식 `--export-pack`으로 생성 가능
- `mods` 폴더에서 실제로 `.pck`를 찾는 로그 확인
- 매칭 `.dll` 로드 로그 확인
- `Finished mod initialization` 로그 확인
- 모드 켠 상태에서 저장 경로가 `modded/profile1`로 분리되는 것 확인

### 장점

- 가장 게임 구조에 가깝다.
- 커뮤니티 배포 예시와 일치한다.
- GUMM처럼 게임 시작 씬을 가로채지 않아도 된다.
- 장기적으로 Steam Workshop이 붙더라도 이 구조 위에 배포만 얹힐 가능성이 높다.

### 단점

- loader 규칙을 정확히 맞춰야 한다.
- 추가 DLL 의존성은 자동으로 안 잡힐 수 있어서 resolver가 필요하다.
- 게임 패치로 로더 규칙이 바뀌면 바로 영향을 받는다.

### 현재 판단

최종 구현 방향은 이 방식이다.

## 왜 Workshop과 GUMM은 다른가

혼동하기 쉬운 부분이라 분리해서 적는다.

- Steam Workshop은 배포 채널이다.
- GUMM은 로더다.
- STS2 내장 `mods` 경로도 로더다.

즉 Workshop이 열린다고 해서 GUMM이 필요한 것은 아니다. 오히려 지금까지 확인한 구조를 보면, Workshop은 파일을 내려주고 실제 로딩은 게임 내장 `mods` 시스템이 담당할 가능성이 더 높다.

## 현재 권장 흐름

1. 백업 생성
2. `build-native-pck`
3. `deploy-native-package`
4. `mods\Sts2Speed.speed.txt` 또는 `STS2_SPEED_*`로 배속 설정
5. 실기 검증
6. 문제가 생기면 스냅샷 복구
