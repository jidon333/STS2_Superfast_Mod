# STS2 모딩 0부터 설명

이 문서는 `Slay the Spire 2` 모딩을 거의 모르는 사람 기준으로, 이번 저장소가 실제로 무엇을 만들고 어떻게 동작하는지 처음부터 설명합니다.

## 1. 먼저 큰 그림

이 모드는 외부 치트 엔진이 게임 프로세스에 붙는 구조가 아닙니다.

현재 구조는 다음 한 줄로 요약할 수 있습니다.

```text
STS2가 원래 가진 native mod loader가 우리 DLL을 읽고,
그 DLL 안의 Harmony 패치가 게임 메서드 호출 전후에 개입한다.
```

즉:

- 게임이 먼저 모드를 읽고
- 모드 DLL이 프로세스 안에서 실행되고
- Harmony가 메서드 hook을 설치합니다

## 2. 모드란 무엇인가

여기서 모드는 게임 본체를 갈아엎는 파일이 아니라, 게임이 읽을 수 있는 별도 패키지 묶음입니다.

현재 배포 기준 파일은 4개입니다.

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `Sts2Speed.Core.dll`
- `Sts2Speed.config.json`

## 3. `.pck`는 무엇인가

`.pck`는 Godot 리소스 팩입니다.

이번 저장소에서 `.pck`의 역할:

- STS2가 이 모드를 정식 모드로 인식하게 함
- `mod_manifest.json`을 포함함
- 필요한 Godot 리소스를 같이 담을 수 있음

중요:

- 실제 속도 조절 로직의 중심은 `.dll`
- `.pck`는 STS2 로더가 모드를 식별하고 mount하는 진입점에 가깝습니다

## 4. `.dll`은 무엇인가

`.dll`은 C# 코드가 들어 있는 실제 payload입니다.

이번 저장소에서 메인 DLL이 하는 일:

1. 추가 의존 DLL resolve
2. 설정 로드
3. Harmony 패치 설치
4. 인게임 설정 UI 주입
5. 런타임에서 함수 인자 조정

즉 실제 속도 변경은 DLL이 합니다.

## 5. Harmony는 무엇인가

Harmony는 .NET 런타임 메서드 패치 라이브러리입니다.

쉽게 말하면:

- 어떤 메서드가 호출될 때
- 그 앞이나 뒤에
- 내 코드를 끼워 넣을 수 있게 해줍니다

이번 저장소에서 쓰는 형태:

- `Prefix`
  - 원본 메서드 실행 전
- `Postfix`
  - 원본 메서드 실행 후

현재는 대부분 `Prefix`로 속도 관련 인자만 바꿉니다.
인게임 설정 UI는 `Postfix`로 붙입니다.

## 6. hook / patch는 무엇인가

hook은 특정 함수 호출 지점에 개입하는 것이고, patch는 실제로 그 개입 코드를 설치한 결과라고 생각하면 됩니다.

예를 들어:

- 게임이 `SetTimeScale(scale)`를 호출하려고 함
- Harmony Prefix가 먼저 실행됨
- 우리가 `scale` 값을 바꿈
- 원본 `SetTimeScale`가 바뀐 값으로 실행됨

즉 원본 코드를 통째로 교체하는 게 아니라, 현재는 주로 원본이 받는 인자만 바꿉니다.

## 7. 왜 `Sts2Speed.Core.dll`이 따로 필요한가

메인 DLL은 설정 로더, 공용 계산 코드, 일부 공유 타입을 별도 어셈블리로 분리해 두었습니다.

그래서:

- 게임이 직접 읽는 건 `sts2-speed-skeleton.dll`
- 이 DLL이 간접적으로 `Sts2Speed.Core.dll`을 참조합니다

STS2는 의존 DLL probing을 알아서 다 해주지 않으므로 resolver가 필요합니다.

## 8. resolver는 무엇인가

resolver는 "추가 DLL을 어디서 찾을지" 알려주는 코드입니다.

이번 저장소에서는 모드 폴더를 기준으로:

- `Sts2Speed.Core.dll`

을 찾게 만듭니다.

이게 없으면 메인 DLL은 로드돼도 의존성 단계에서 실패할 수 있습니다.

## 9. 설정 파일은 어디서 읽는가

현재 기본 설정 파일은:

- `mods\Sts2Speed.config.json`

입니다.

기본 예시:

```json
{
  "enabled": true,
  "baseSpeed": 3.0,
  "spineSpeed": 1.0,
  "queueSpeed": 1.0,
  "effectSpeed": 1.0,
  "combatUiSpeed": 1.0,
  "combatVfxSpeed": 1.0,
  "combatOnly": true
}
```

핵심 규칙:

- 모든 숫자는 `클수록 빠름`
- 실제 적용 배속은 `baseSpeed x 각 항목 Speed`

## 10. 인게임에서도 값을 바꿀 수 있는가

현재는 가능합니다.

경로:

- `설정 -> 모드 -> STS2 Speed Skeleton`

여기서 `+ / -` 버튼으로 값을 바꾸면, 결국 같은 `Sts2Speed.config.json` 파일이 저장됩니다.

즉:

- 파일 직접 수정
- 인게임 UI 수정

둘 다 같은 설정 표면을 건드리는 셈입니다.

## 11. 왜 재시작 없이 반영될 수 있는가

`RuntimePatchContext`가 설정을 500ms 캐시하고, config 파일 write time이 바뀌면 다시 읽습니다.

그래서:

- 인게임 UI가 config를 저장
- 런타임이 새 값을 감지
- 다음 패치 hit부터 새 값 적용

이라는 흐름이 가능합니다.

## 12. 실제로 무엇을 빠르게 만드는가

현재 패치 범위:

- Spine 애니메이션
- 행동 사이 wait
- 일부 timer 기반 effect delay
- 전투 UI delta
- 전투 VFX delta

즉 전역 time scale을 바꾸는 것이 아니라, 필요한 지점만 선택적으로 빠르게 만듭니다.

## 13. 왜 전역 `Engine.TimeScale`을 안 쓰는가

전역 time scale은 너무 넓습니다.

그렇게 하면:

- 메뉴
- 비전투 UI
- 의도하지 않은 타이머
- 오디오 체감

까지 한 번에 흔들릴 수 있습니다.

지금 모드는 "전투 템포만 자연스럽게"를 목표로 하기 때문에, 전역 time scale보다 선택적 메서드 패치가 맞습니다.

## 14. 왜 세이브가 사라진 것처럼 보일 수 있는가

STS2는 모드를 읽으면 저장 경로를 `modded/profileN`로 분리합니다.

즉:

- 바닐라 진행: `profileN`
- 모드 진행: `modded/profileN`

그래서 바닐라 저장은 살아 있어도 modded 쪽이 비어 있으면 초기화된 것처럼 보입니다.

이번 저장소에는 이 문제를 해결하는 `sync-modded-profile` 도구가 들어 있습니다.

## 15. 현재 구현에서 기억할 핵심

- 최종 로더 경로는 STS2 native `mods` route
- `.pck`는 모드 식별과 Godot 패키지
- `.dll`은 실제 로직 payload
- Harmony가 메서드 hook 설치
- 설정은 `config.json` 또는 인게임 UI에서 변경
- 런타임은 파일 변경을 감지해서 다시 읽음
- save는 `modded/profileN`으로 분리됨
