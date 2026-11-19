# Unity AI Game Jam · Unity CLI 사용 기록

- **프로젝트** `2026_Unity_GameJam_08` · 《그건 님이!!!》
- **기간** 2026년 11월 18일 14:00 ~ 11월 19일 10:14 (KST)
- **확인 범위** 이 프로젝트의 Codex 작업 채팅 18개, 로컬 Git 이력, Windows 빌드와 검증 파일. 채팅에 남아 있는 명령 실행을 기준으로 정리했으며, 별도 터미널이나 다른 팀원의 장비에서 실행한 명령은 집계 범위 밖이다.
- **Unity Editor** 6000.3.28f1

## Unity CLI 사용 내역

아래 명령은 실행 기록에서 반복적으로 확인된 대표 형태다. 프로젝트 경로, 포트 등 실행 환경별 인자는 생략했다.

| 시기 (KST) | 용도 | 실행 및 확인 내용 |
| --- | --- | --- |
| 11/18 14시대 | 프로젝트 연결·패키지 설치 | `unity status`, `unity editors running`, `unity editors path`로 에디터 상태와 설치 경로를 확인했다. `Unity.exe -batchmode`에서 Unity Package Manager를 실행해 Multiplayer Services, Authentication, NGO, Transport, Multiplayer Play Mode를 설치하고 패키지 파일을 갱신했다. |
| 11/18 15~17시 | 플레이어·마차·전투 씬 편집 | `unity command scene open`, `unity command script eval`, `unity command script run`으로 씬의 오브젝트와 참조를 편집·저장했다. Play 모드에서 타이밍 HUD, 포탄 운반·장전·점화·발사, 조준 카메라, 진행률과 마차 체력 표시를 확인했다. |
| 11/18 17~22시 | NGO·Relay 협동 검증 | `unity command editor.playmode start_scenario`와 `get_scenario_status`로 멀티플레이 시나리오와 가상 플레이어를 구동했다. 실제 Relay 방에서 4인 입장·공동 스폰·명단과 위치 동기화를 확인했다. 후속 버전에서는 진행 중 참가, 재접속, 방장 승계와 상점 투표도 확인했다. [멀티플레이 검증 기록](NETWORKING.md) |
| 11/18 밤~11/19 오전 | v8~v13 기능 및 UI 회귀 검증 | `unity command editor.playmode start`·`stop`으로 Play 모드를 제어하고 `script eval`·`script run`으로 대포·마차·왕족·봇의 상태를 확인했다. `capture game_view`로 화면을 저장했고, `unity recompile` 또는 `unity command script recompile`과 `console status`·`console get`으로 컴파일 및 런타임 오류를 점검했다. F6 전환과 가상 터치 입력도 검증했다. |
| 11/19 오전 | v13 Windows 빌드 | `unity command script run`으로 에디터 빌드 스크립트를 실행해 Windows x64 플레이어를 생성했다. 마지막 빌드는 **오류 0건, 경고 486건**으로 성공했다. 초기 v13 검증 로그에 기록된 485건은 앞선 빌드의 수치다. [Windows 실행 파일](Build/V13-Windows/GoblinGetaway-v13.exe) · [초기 v13 검증 로그](Logs/v13-verification.txt) |

초기 작업에서 프로젝트 잠금과 에디터 연결 시간 초과가 발생했다. 패키지 설치는 배치 실행을 재시도해 완료했고, 타이밍·대포 기능은 에디터 연결 후 씬을 다시 열어 Play 모드에서 검증했다. 모바일 UI와 입력은 Unity Game View와 가상 터치로 확인했다. Android APK 빌드와 실기기 동작은 미검증이다.

## 주요 프롬프트

개발 결과에 직접 연결된 요청을 요약했다.

| 요청 | 반영 결과 |
| --- | --- |
| 플레이어가 작업 구역에 들어가면 타이밍 HUD를 열고, 스페이스 입력으로 성공·실패를 판정하며 카메라를 전환해 달라. | 작업 구역 판정, 원형 타이밍 UI, 포수 조준 카메라와 포탄 프리팹을 연결했다. |
| NGO와 Relay로 전환하고 `Lobby`에서 방 생성·코드 참가·플레이어 배정·공동 씬 입장을 구현해 달라. | 4인 협동 로비를 구현하고 실제 Relay 연결에서 입장, 스폰과 상태 동기화를 확인했다. |
| v10 개발 가이드와 웹 레퍼런스를 기준으로 아이 돌봄과 대포 흐름이 Unity에서 어떻게 다른지 확인하고 반영해 달라. | 안기·걸어서 달래기·좌석 복귀와 포탄 운반·장전·착탄 흐름을 대조하고 수정했다. |
| 에디터와 Windows는 PC 조작으로, Android는 모바일 조작으로 시작하게 하고 에디터의 F6 전환을 고쳐 달라. | 플랫폼별 기본 조작과 입력 분리를 적용했다. 모바일 조이스틱 이동 범위를 화면 너비의 30%를 반지름으로 하는 90도 부채꼴로 조정했다. |
| v13 ZIP의 웹 UI 레이아웃·크기와 게임 규칙을 Unity에 구현하고 검증해 달라. | 랭킹, 왕실 카드와 작업 HUD를 반영했다. 대포 직격·충격파와 시청각 반응, 채찍 8회 폭주를 맞추고 Play 모드·Relay·Windows 빌드에서 확인했다. |

## 주요 생성물

| 생성·수정 항목 | 주요 내용 및 근거 |
| --- | --- |
| **4인 협동 로비** | `Lobby.unity`, `LobbyUI.prefab`, `LobbySession.cs`, `LobbyRelayTest.asset`. 방 생성·코드 참가, 명단과 좌석 배정, 공동 씬 입장. [구현 커밋](https://github.com/DevBawky/2026_Unity_GameJam_08/commit/40010d8) · [검증 기록](NETWORKING.md) |
| **게임 씬·프리팹 및 v10~v12 작업** | `mainGame.unity`, `YMH_Scene.unity`, `PLAYER.prefab`, `Cart.prefab`, `Cannonball.prefab`, `Canvas HUD.prefab`. 포탄 운반·청소·장전·발사, 바퀴 수리, 왕족 돌봄, 분할 화면과 UI 스프라이트·한글 TMP 폰트 적용. [v10 커밋](https://github.com/DevBawky/2026_Unity_GameJam_08/commit/bc9d786) · [v12 커밋](https://github.com/DevBawky/2026_Unity_GameJam_08/commit/32ce56b) |
| **PC·모바일 조작** | `GameplayControlMode.cs`, `MobileJoystick.cs`, `MobileInteractionUI.cs`. 플랫폼별 기본 입력, F6 전환, 터치 행동 버튼과 조이스틱 제한 범위. [입력 분리 커밋](https://github.com/DevBawky/2026_Unity_GameJam_08/commit/a8ede76) · [조이스틱 커밋](https://github.com/DevBawky/2026_Unity_GameJam_08/commit/3d6d761) |
| **v13 HUD·게임 규칙** | `CrewRankingHud.cs`, `CartRules.asset`, `CannonballHit.cs`, `CartWheelTracks.cs`와 대포 발사·착탄음. 인게임 HUD, 기병 피격과 마차 반동, 채찍 8회 후 160% 속도로 5초간 가속, 네 바퀴의 주행 흔적. [v13 커밋](https://github.com/DevBawky/2026_Unity_GameJam_08/commit/94d9b4f) |
| **빌드·검증 자료** | [v13 Windows 실행 파일](Build/V13-Windows/GoblinGetaway-v13.exe) · [해상도별 HUD 실측 및 검증 로그](Logs/v13-verification.txt) · [2인 분할 화면](Logs/v13-unity-split.png) · [확대된 순위표](Logs/crew-ranking-enlarged.png) · [바퀴자국 캡처](Build/V13-ground-tracks.png) |

대표 검증 결과는 **실제 4인 Relay 동기화**, **v13 Play 모드 콘솔 오류 0건**, **Windows x64 빌드 성공**이다. v13 연습 모드에서 채찍 8회 후 마차 속도가 `13.0 → 20.8 → 7.15 → 13.0`으로 바뀌는 것도 확인했다. 빌드와 캡처의 상대 경로는 프로젝트의 로컬 작업 폴더 기준이다.
