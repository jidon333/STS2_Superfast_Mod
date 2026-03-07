# STS2 Superfast Mod

`Slay the Spire 2` 전투 템포를 더 빠르게 만드는 속도 모드다.

현재 기본값은 `2.0`이고, 의미는 다음과 같다.

- Spine 애니메이션: 2배속
- 전투 wait / timer: 절반 길이

이 저장소 안에서 GitHub 배포용 최소 패키지는 `release/STS2_Superfast_Mod` 폴더에 들어 있다.

## 배포 파일

배포용 폴더 안에는 아래 4개 파일이 있다.

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `Sts2Speed.Core.dll`
- `Sts2Speed.speed.txt`

`INSTALL_KO.txt`는 설치 안내용 문서다.

## 설치 방법

1. `Slay the Spire 2` 설치 폴더 아래에 `mods` 폴더를 만든다.
2. `release/STS2_Superfast_Mod` 안의 파일 4개를 `mods` 폴더에 복사한다.
3. `Sts2Speed.speed.txt`를 메모장으로 열어 원하는 배속 숫자로 수정한다.
4. 게임을 실행한다.

예시 경로:

```text
D:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods
```

## 배속 설정

`Sts2Speed.speed.txt`에는 숫자 하나만 들어간다.

예시:

```txt
2.0
```

의미:

- `2.0` = 2배속
- `3.0` = 3배속
- `1.0` = 바닐라와 동일
- `0.5` = 반속

## 지금 실제로 적용되는 것

현재 패치는 아래 지점에 걸려 있다.

- `MegaAnimationState.SetTimeScale`
- `MegaTrackEntry.SetTimeScale`
- `Cmd.CustomScaledWait`
- `CombatState.GodotTimerTask`

즉 지금은 전역 시간 배속이 아니라, 애니메이션과 일부 wait/timer 인자를 직접 조정하는 방식이다.

## 진행 데이터가 초기화된 것처럼 보일 때

STS2는 모드가 로드되면 저장 경로를 `modded/profileN` 쪽으로 분리한다.

그래서 바닐라 진행은 남아 있는데 모드 쪽 진행이 비어 있으면, 새 프로필처럼 보일 수 있다.

이 경우:

- `AppData\Roaming\SlayTheSpire2\steam\<계정>\profile1`
- 내용을
- `AppData\Roaming\SlayTheSpire2\steam\<계정>\modded\profile1`
- 로 복사하면 된다

이 저장소를 로컬에서 같이 쓰는 경우에는 아래 명령으로 자동 복구할 수 있다.

```powershell
dotnet run --project src/Sts2Speed.Tool -- sync-modded-profile
```

## 주의 사항

- 게임이 실행 중일 때 `mods` 폴더 파일을 교체하지 않는 편이 안전하다.
- 모드를 처음 켜면 게임이 모드 경고/동의를 한 번 요구할 수 있다.
- 현재 모드는 애니메이션과 일부 wait/timer를 우선 가속하는 방식이라, 모든 연출이 동일 비율로 빨라지지는 않을 수 있다.

## 개발 문서

저장소 구조, Harmony 패치 원리, `.pck` 생성, 시행착오, 남은 훅 위험도는 아래 개발 문서에 정리돼 있다.

- `docs/development/README.md`
