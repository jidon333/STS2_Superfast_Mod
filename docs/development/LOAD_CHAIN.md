# STS2 로드 체인

이 문서는 현재 STS2가 우리 모드를 어떤 순서로 읽고, 어떻게 설정과 인게임 UI까지 연결되는지 설명합니다.

## 1. 게임 시작

게임 초기화 중 `ModManager.Initialize()`가 호출됩니다.

여기서 STS2 내장 native mod loader가 동작합니다.

## 2. `mods` 폴더 스캔

로더는 게임 설치 경로 아래 `mods` 폴더를 봅니다.

현재 배포 기준 파일:

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`
- `Sts2Speed.Core.dll`
- `Sts2Speed.config.json`

## 3. `.pck`와 `.dll` 매칭

STS2는 `.pck` basename과 같은 이름의 `.dll`을 찾습니다.

즉:

- `sts2-speed-skeleton.pck`
- `sts2-speed-skeleton.dll`

이 둘의 basename이 같아야 합니다.

## 4. `.pck` 내부 manifest 확인

로더는 `.pck`를 mount한 뒤 `res://mod_manifest.json`을 읽습니다.

중요한 규칙:

- `pck_name`은 `.pck` 확장자를 뺀 basename과 같아야 합니다

## 5. 메인 DLL 로드

manifest 검사가 끝나면 게임은 `sts2-speed-skeleton.dll`을 자기 프로세스 안으로 로드합니다.

이 단계까지는 별도 인젝션이 없습니다.

## 6. 추가 DLL resolve

메인 DLL은 `Sts2Speed.Core.dll`을 참조합니다.

STS2 로더는 메인 DLL은 읽어도 그 의존 DLL까지 자동 probing하지 않으므로, 모드 DLL 안에서 resolver를 등록합니다.

역할:

- 현재 모드 디렉토리 기준으로 추가 DLL 탐색
- `Sts2Speed.Core.dll` 로드 보조

## 7. Harmony 패치 등록

현재 모드 DLL에는 `ModInitializerAttribute`가 없으므로, STS2 로더가 `Harmony.PatchAll(assembly)`를 호출합니다.

즉 실제 실행 체인은 대략 이렇습니다.

```text
게임 로더
  -> mods 폴더 스캔
  -> .pck 발견
  -> matching .dll 로드
  -> resolver 등록
  -> Harmony.PatchAll
  -> [HarmonyPatch] 클래스 탐색
  -> Prefix/Postfix 패치 설치
```

현재 설치되는 핵심 패치:

- Spine time scale Prefix
- wait/timer Prefix
- combat UI delta Prefix
- combat VFX delta Prefix
- Modding Screen config UI Postfix

## 8. 런타임 설정 로드

패치 로직은 실행 중 `RuntimePatchContext.GetSettings()`를 통해 설정을 읽습니다.

현재 우선순위:

1. `STS2_SPEED_*` 환경 변수
2. `Sts2Speed.config.json`
3. legacy fallback `Sts2Speed.speed.txt`
4. 기본값

핵심 포인트:

- `RuntimePatchContext`는 설정을 500ms 캐시합니다
- config 파일의 마지막 write time이 바뀌면 다시 읽습니다
- 그래서 인게임 UI가 config 파일만 저장해도 런타임 값이 갱신됩니다

## 9. 인게임 설정 UI 주입

현재 인게임 UI는 별도 공식 mod-settings API가 아니라 Harmony UI 주입 방식입니다.

주입 지점:

- `MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen.NModInfoContainer.Fill(Mod mod)`

방식:

- `Postfix`에서 `InGameConfigUi.RefreshForSelection(__instance, mod)` 호출
- 선택된 모드가 `STS2 Speed Skeleton`이면 설정 패널 표시
- 아니면 패널 숨기고 원래 설명 라벨 복원

UI 구현 방식:

- reflection으로 `Godot.VBoxContainer`, `HBoxContainer`, `Button`, `Label` 생성
- `+ / -` 버튼으로 값 조절
- `Enabled`, `Combat only`는 toggle 버튼
- 값 저장은 `Sts2Speed.config.json`

## 10. 값 적용 흐름

예를 들어 사용자가 인게임 UI에서 `Base speed`를 올리면:

1. `InGameConfigUi`가 `Sts2Speed.config.json` 저장
2. `RuntimePatchContext`가 config write time 변화를 감지
3. 다음 패치 hit 시 최신 설정 다시 로드
4. `settings refreshed: ...` 로그 기록
5. 이후 패치 Prefix가 새 값으로 인자 조정

## 11. 실제 패치 적용 방식

현재 의미는 이렇게 통일되어 있습니다.

- animation / delta 계열: `* effectiveSpeed`
- wait / delay 계열: `/ effectiveSpeed`

즉 사용자 입장에서는 모든 숫자가 `클수록 빠름`입니다.

## 12. 저장 경로 분리

모드가 로드되면 STS2는 저장 데이터를 `modded/profileN` 쪽으로 분리합니다.

그래서:

- vanilla 진행은 남아 있는데
- modded 프로필만 비어 있으면
- 게임이 초기화된 것처럼 보일 수 있습니다

이 경우 `profileN -> modded/profileN` 복구가 필요합니다.

## 13. 로그 확인

게임 로그에서 확인할 수 있는 신호:

- `.pck` 발견
- matching DLL 로드
- mod initialization 완료
- `--- RUNNING MODDED! ---`

모드 자체 로그:

- `mods\sts2speed.runtime.log`

여기서 확인 가능한 것:

- 설정 로드 소스
- 설정 refresh
- 패치 실제 적용 1회 로그
- 인게임 UI 저장 로그
