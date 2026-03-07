# STS2 Speed Mod 초보자용 안내

이 문서는 모딩 경험은 적지만 클라이언트 코드 구조를 이해하는 사람을 기준으로, 지금 저장소가 어떤 원리로 동작하는지 설명한다.

## 1. 이 저장소가 지금 하는 일

현재 이 저장소는 완성된 “배속 모드 제품”이라기보다 다음 다섯 가지를 묶은 작업 저장소다.

1. 백업과 복구
2. Godot `.pck` 생성
3. STS2 네이티브 모드 패키징
4. 실제 속도 패치 실험
5. vanilla 프로필을 modded 프로필로 복구

즉 “모드가 안전하게 로드되고, 되돌릴 수 있고, 패치를 추가할 수 있고, 저장 데이터도 복구할 수 있는 구조”를 만드는 것이 1차 목적이었다.

## 2. 슬더스2 모드는 실제로 어떤 구조로 로드되는가

현재 확인된 구조는 다음과 같다.

1. 게임이 `Slay the Spire 2\\mods` 폴더를 스캔한다.
2. `.pck`를 발견하면 basename을 구한다.
3. 같은 basename의 `.dll`을 찾는다.
4. `.pck` 안의 `res://mod_manifest.json`을 읽는다.
5. manifest의 `pck_name`이 `.pck` basename과 같아야 한다.
6. DLL을 로드하고, `ModInitializerAttribute`가 없으면 `Harmony.PatchAll`을 호출한다.

즉 커뮤니티에서 본 `pck + dll + txt` 구조는 그냥 편의적 관행이 아니라, 게임 로더 구조와 매우 잘 맞는다.

## 3. 각 파일은 왜 필요한가

### `.pck`

Godot 리소스 팩이다. STS2 로더는 이 파일이 있어야 모드 하나를 정식 단위로 인식한다.

이 저장소에서는 `build-native-pck` 명령이 공식 Godot `--export-pack` 경로를 이용해서 만든다.

### `.dll`

실제 로직이 들어가는 managed payload다.

현재 `sts2-speed-skeleton.dll` 안에는 Harmony patch 클래스가 들어 있다.

### `Sts2Speed.speed.txt`

공유 배속 설정 파일이다.

현재는 이 파일에 `1.25`, `2.0`, `0.5` 같은 값을 넣으면, 별도 환경 변수를 주지 않았을 때 다음 세 값의 공통 fallback으로 사용한다.

- `spineTimeScale`
- `queueWaitScale`
- `effectDelayScale`

## 4. 코드에서 핵심 파일은 어디인가

### 패키징

- `src/Sts2Speed.ModSkeleton/NativeModPackaging.cs`

하는 일:

- 네이티브 `mods` 폴더용 파일 배치
- `mod_manifest.json` 작성
- `.pck` 생성용 export project 준비
- live `mods` 폴더로 복사

### 런타임 설정 로더

- `src/Sts2Speed.Core/Configuration/RuntimeSettingsLoader.cs`

하는 일:

- `STS2_SPEED_*` 환경 변수 읽기
- `Sts2Speed.speed.txt` 읽기
- 명시적 `enabled`가 없을 때 공유 배속 값이 `1.0`이 아니면 자동 활성화

### 모드 의존성 로더

- `src/Sts2Speed.ModSkeleton/Runtime/ModAssemblyResolver.cs`

왜 필요한가:

STS2는 매칭 DLL 하나는 로드하지만, 그 DLL이 참조하는 추가 DLL을 자동으로 `mods` 폴더에서 찾아주지 않았다. 그래서 `Sts2Speed.Core.dll`을 모드 폴더 기준으로 직접 resolve 하도록 넣었다.

### 패치 컨텍스트

- `src/Sts2Speed.ModSkeleton/Runtime/RuntimePatchContext.cs`

하는 일:

- 현재 런타임 설정 캐시
- combat-only 조건 판단
- 스케일 적용
- `sts2speed.runtime.log` 기록

### 실제 Harmony 패치

- `src/Sts2Speed.ModSkeleton/Runtime/SpeedPatches.cs`

현재 들어간 패치:

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`

## 5. 왜 이 메서드들을 건드리는가

### `MegaAnimationState.SetTimeScale`, `MegaTrackEntry.SetTimeScale`

Spine 애니메이션 배속 진입점이다. 전투 중에만 `spineTimeScale`을 곱해서 애니메이션 속도를 올린다.

### `Cmd.CustomScaledWait`

전투 턴 배너, 카드 턴 종료 처리 등 “의도적으로 넣은 대기시간”을 줄이기 좋은 지점이다.

현재는 `queueWaitScale`을 곱한다.

### `CombatState.GodotTimerTask`

Godot timer 기반의 비동기 지연을 줄이는 후보다.

현재는 `effectDelayScale`을 곱한다. 체감 기여도는 아직 실기 검증이 더 필요하다.

## 6. 왜 `ActionExecutor`를 아직 안 건드렸는가

`ActionExecutor.ExecuteActions`는 더 공격적인 속도 모드에는 유력하지만, 잘못 건드리면 액션 큐 의미 자체를 바꿔버릴 수 있다.

지금 단계에서는 다음 순서를 택했다.

1. 로더가 안정적으로 뜨는지 확인
2. 명백한 배속 메서드부터 패치
3. 전투 실기에서 부족하면 큐 자체로 내려간다

즉 아직은 보수적으로 접근 중이다.

## 7. 모드를 켜면 왜 진행이 초기화된 것처럼 보이는가

이 부분이 현재 가장 중요하다.

모드가 하나라도 로드되면 STS2는 저장 경로를 `modded/profileN`으로 분리한다. vanilla 진행은 원래 `profileN`에 남아 있고, modded 쪽이 비어 있으면 게임은 새 프로필처럼 보인다.

즉 “데이터가 사라진 것”이 아니라 “게임이 다른 슬롯을 보고 있는 것”에 가깝다.

실제 복구 방법은 다음과 같다.

1. 게임 종료
2. `profile1` 내용을 `modded/profile1`에 복제
3. 모드 실행

이 저장소에서는 이 과정을 `sync-modded-profile` 명령으로 자동화했다. 이 명령은 기존 `modded/profile1`을 먼저 백업한 다음, `profile1` 전체를 미러링한다.

## 8. 지금 어디까지 검증됐는가

실기 기준으로 확정된 것은 다음이다.

- `.pck`를 게임이 찾는다
- matching `.dll`을 로드한다
- `Harmony.PatchAll`이 실제로 호출된다
- 모드가 켜진 상태로 메인 메뉴까지 진입한다
- `Sts2Speed.speed.txt = 1.25` 를 넣으면 모드가 활성화된 설정으로 인식된다
- vanilla `profile1`을 `modded/profile1`로 복제하면 진행 데이터가 복구된다

아직 직접 확인하지 못한 것은 다음이다.

- 전투 한가운데서 `spine time scale applied` 로그가 찍히는지
- `queueWaitScale`, `effectDelayScale` 체감이 원하는 수준인지

## 9. 왜 GUMM을 완전히 버리지 않았는가

GUMM은 이제 최종 구현 경로는 아니지만, 두 가지 용도로 여전히 가치가 있다.

1. 게임 내장 모드 경로가 갑자기 깨졌을 때 fallback
2. 초기 부트스트랩/로그 진단

다만 지금 최종 구현 목표는 분명히 STS2 내장 네이티브 로더 쪽이다.

## 10. 직접 만져볼 때 가장 중요한 포인트

- 기본 rollback은 항상 snapshot 기준으로 한다.
- `mods` 폴더에 들어가는 파일명은 basename 규칙이 매우 중요하다.
- `mod_manifest.json`의 `pck_name`은 `.pck` 확장자를 빼야 한다.
- `Sts2Speed.Core.dll` 같은 추가 DLL은 그냥 복사만 하면 충분하지 않을 수 있다. resolver가 필요할 수 있다.
- 모드 사용 시 진행이 새로 시작된 것처럼 보이면 `profileN`과 `modded/profileN`을 먼저 비교한다.
- 전투 체감까지 보려면 이제 실제 플레이 검증이 필요하다.
