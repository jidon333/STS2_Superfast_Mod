# STS2 모드 로딩 방식 비교

이 문서는 이번 작업에서 검토한 세 가지 로딩 방식을 비교하고, 왜 현재 구조를 최종 경로로 채택했는지 설명합니다.

비교 대상:

1. `GUMM` 단독 방식
2. `GUMM bootstrap + C# payload`
3. STS2 native `mods + pck + dll + json`

결론부터 말하면 현재 최종 경로는 `3번`입니다.

## 1. GUMM 단독 방식

### 원리

GUMM은 Godot 기본 기능이 아니라, Godot의 `override.cfg`와 start scene override를 활용하는 외부 로더입니다.

동작 순서:

1. `override.cfg`로 main scene을 GUMM loader scene으로 변경
2. GUMM이 `mod.cfg`, `mod.gd`를 읽음
3. 초기화가 끝나면 원래 게임 scene으로 넘김

### 이 저장소에서 실제로 했던 것

- `override.cfg` 작성
- `GUMM_mod_loader.tscn` 배치
- `mod.cfg`, `mod.gd`, `GUMM_mod.gd` 포함 패키지 생성
- Steam `-applaunch 2868840`로 실제 부팅
- 로그에서 `Loading mod: STS2 Speed Skeleton` 확인

### 장점

- 모드 코드가 실제로 실행되는지 빨리 검증하기 좋음
- 리소스 교체 / 진단용 bootstrap으로 유용
- 내장 로더 규칙이 불명확할 때 우회 진입 경로가 됨

### 단점

- 최종 속도 payload는 결국 managed C# 메서드 패치가 핵심
- GUMM은 GDScript / resource layer가 중심
- 결국 bridge가 하나 더 생겨 구조가 불필요하게 복잡해짐
- STS2 내장 native 경로가 확인된 뒤에는 유지할 이유가 크게 줄어듦

### 현재 평가

유효했던 진단 단계였지만, 최종 구현 경로는 아님.

## 2. GUMM bootstrap + C# payload

### 원리

GUMM은 단지 진입과 bootstrap만 맡고, 실제 속도 변경은 managed C# payload가 담당하는 방식입니다.

즉:

- 진입은 `mod.gd`
- 본체는 Harmony DLL

### 이 방식이 한때 유력했던 이유

- GUMM bootstrap은 이미 실기에서 성공했음
- 우리가 만지고 싶은 대상은 대부분 managed C# 메서드였음
- 따라서 "진입은 GUMM, payload는 C#" 구조가 중간 단계로 합리적이었음

### 장점

- GUMM 실험 결과를 버리지 않음
- 진입과 payload를 분리 가능
- native 규칙이 아직 불확실할 때는 좋은 타협점

### 단점

- 최종적으로는 구조가 이중화됨
- `override.cfg` 같은 게임 루트 변경을 계속 안고 가야 함
- native route가 이미 확인되면 장점보다 유지비가 커짐

### 현재 평가

과도기 설계로는 유효했지만, 지금은 fallback 이상의 의미는 없음.

## 3. STS2 native `mods + pck + dll + json`

### 원리

이 방식은 STS2가 원래 제공하는 native mod loader 규칙을 그대로 따릅니다.

실제 규칙:

1. 게임이 `<game dir>\mods`를 스캔
2. `.pck`를 발견
3. 같은 basename의 `.dll`을 찾음
4. `.pck`를 mount
5. `res://mod_manifest.json` 확인
6. `pck_name`과 basename 검증
7. DLL 로드
8. `ModInitializerAttribute`가 없으면 `Harmony.PatchAll(assembly)` 호출

### 현재 배포 표면

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `Sts2Speed.Core.dll`
- `Sts2Speed.config.json`

### 장점

- 현재 게임 구조와 가장 가깝다
- 커뮤니티 배포 형식과도 맞는다
- GUMM처럼 별도 scene override가 필요 없다
- 최종 payload가 같은 런타임 레이어에서 동작한다
- 현재 인게임 설정 UI도 이 경로 위에서 자연스럽게 붙는다

### 단점

- manifest 규칙을 정확히 맞춰야 한다
- 추가 DLL dependency resolve를 스스로 처리해야 한다
- 게임 버전 변화에 더 직접 영향을 받는다

### 현재 평가

최종 구현 경로.

## Workshop과의 관계

헷갈리기 쉬운 부분:

- Steam Workshop은 배포 채널
- GUMM은 외부 로더
- STS2 native `mods` route는 게임 내부 로더

즉 Workshop이 붙는다고 해서 GUMM이 필요한 것은 아닙니다.
오히려 현재 구조상 Workshop이 나중에 붙더라도 "파일을 내려주는 채널"이고, 실제 로딩은 STS2 native route가 맡을 가능성이 더 큽니다.

## 현재 권장 결론

- 최종 구현: STS2 native `mods + pck + dll + json`
- 진단 / 역사적 참고: GUMM
- 더 이상 유지하지 않는 경로: GUMM bootstrap을 전제로 한 최종 설계
