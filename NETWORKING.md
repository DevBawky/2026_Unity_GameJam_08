# 로비 네트워크 구성 및 검증

## 결정

요청한 NGO + Relay 기반 4인 방장 호스팅 방식이다. Relay 할당/참가 코드를 직접 사용하고, NGO의 연결 승인과 서버 전용 NetworkList로 참가 순서를 확정한다. Unity Lobby 서비스나 별도 방 검색은 사용하지 않는다. 방장이 종료하면 참가자는 Lobby로 복귀한다. 방장 이전은 지원하지 않는다.

설치 버전 근거:

- `ProjectSettings/ProjectVersion.txt`: `m_EditorVersion: 6000.3.28f1`
- `Packages/manifest.json`: `"com.unity.netcode.gameobjects": "2.14.0"`, `"com.unity.services.multiplayer": "2.5.0"`, `"com.unity.transport": "2.7.5"`
- `Packages/packages-lock.json`: 동일한 NGO 2.14.0 / Transport 2.7.5로 resolve.
- 설치된 패키지의 `Documentation~/basics/networkvariable.md`: “automatically synchronized on the client side.” 기존 참가자 명단은 일회성 이벤트가 아닌 NetworkList로 전송한다.
- 설치된 `Documentation~/basics/scenemanagement/scene-management-overview.md`: “fully functional netcode scene management solution.” 게임 이동은 `NetworkManager.SceneManager.LoadScene`으로 처리한다.
- Multiplayer 2.5 `Runtime/Relay/Models/AllocationUtils.cs`: `ToRelayServerData(this Allocation allocation, string connectionType)`, JoinAllocation 오버로드 확인. Transport에 DTLS Relay 설정을 전달한다.

## 책임

| 파일 | 책임 |
|---|---|
| `Assets/Scripts/LobbyUI.cs` | 저장된 UI 참조, 패널 표시, 입력, 버튼 상태, 클립보드 |
| `Assets/Scripts/LobbySession.cs` | 인증·Relay 연결 수명, 중복 요청 방지, 연결 승인, 슬롯 배정, 오류/시간 초과, 호스트 씬 이동 및 스폰 |
| `Assets/Scripts/LobbyRoster.cs` | 서버가 기록하는 명단·방 코드·시작 상태 동기화 |
| `Assets/Scripts/MultiplayerGameScene.cs` | 저장된 4개 스폰 지점, 온라인 모드의 기존 싱글 플레이어 비활성화 |
| `Assets/Scripts/LobbyNetworkPlayer.cs` | 소유자별 월드 아바타 입력과 NetworkTransform 위치 동기화 |

UI와 버튼 이벤트는 `LobbyUI.prefab`, 서비스 참조는 `Lobby.unity`에 저장되어 있다. 런타임 UI 생성은 하지 않는다. 복사 버튼은 방 코드 오른쪽의 `Button | CopyRoomCode`이고 코드 텍스트 자체는 클릭되지 않는다. `Panel | Loading`은 전체 화면 Image로 raycast를 차단한다. `Busy` 검사도 별도로 중복 API 호출을 방지한다.

슬롯은 서버가 빈 슬롯을 P1부터 배정한다. 중도 퇴장한 자리만 비고 기존 플레이어의 슬롯은 유지된다. 닉네임은 1~16자, 제어 문자 금지, TMP rich text 비활성화. 생성/입장 서비스 요청은 30초, NGO 접속 대기는 20초 제한이다. `TickRate=30`, 최대 인원은 `LobbySession.Capacity=4`다. 이는 프로젝트 설정이며 성능 보장 수치는 아니다.

## CLI 검증 (2026-11-18)

`LobbyRelayTest` MPPM 시나리오에서 Main Editor + Player 2/3/4 네 프로세스를 사용했다. 확인 코드는 `Temp/`에서 일회 실행했으며 런타임 스크립트에 테스트 훅을 추가하지 않았다.

- 최신 코드 컴파일: `compilationFailed: false`, `compiling: false` 확인. CLI recompile 호출 하나는 도메인 리로드 중 120초 timeout을 냈으며, 이후 Console ground truth와 실제 Play Mode 실행으로 컴파일 상태를 확인했다.
- UI 검증 결과 `PASS`: Title→Join, Settings 열기/닫기, 빈 닉네임 거부, 잘못된 코드 형식 거부, 중복 생성 즉시 무시, 생성 중 로딩 패널/raycast 차단/버튼 비활성화, 생성 완료 시 Room 표시, 별도 복사 버튼의 클립보드 일치.
- 실제 Relay 생성/입장 후 네 프로세스의 명단 일치: `(ClientId, Slot, Nickname)` = `(0,0,방장)`, `(1,1,P2검증)`, `(2,2,P3검증)`, `(3,3,P4검증)`.
- 세 클라이언트가 `StartGame()`을 호출해도 모두 `unauthorizedStartBlocked: true`.
- 만원 상태의 서버 승인 콜백 검증: `fullRoomRejected: true`, `방이 가득 찼습니다. (최대 4명)`.
- 호스트 시작 후 네 프로세스 모두 `scene: mainGame`, `connected: true`, 아바타 4개. 소유자 0~3의 슬롯 값 0~3과 스폰 좌표 `(0,0)`, `(0.6,0)`, `(0,0.6)`, `(0.6,0.6)`가 모든 프로세스에서 일치.
- Player 2 소유 아바타의 NetworkTransform 위치를 `(1.80,0.25,0)`로 변경한 뒤 네 프로세스 모두 같은 위치를 읽었다.
- 호스트 퇴장 후 네 프로세스 모두 `scene: Lobby`, `InRoom: false`, `managers: 1`. 클라이언트 상태 메시지 `방장과 연결이 끊어졌습니다.` 확인.
- Relay 요청 `Request timeout` 발생 시 `InRoom: false`, `RoomCode: ""`, 로딩 해제 및 오류 표시를 확인했다.
- 최초 2인 연결 테스트에서 프리팹 등록 목록이 메모리에만 남아 NetworkConfig mismatch가 발생했다. 에디터에서 목록을 dirty 처리하고 실제 asset에 저장한 뒤 4인 Relay 연결로 재검증했다.
- 초기 테스트의 검증용 LINQ 식은 NetworkList에 직접 Select를 호출해 컴파일에 실패했다. 인덱스로 읽도록 수정해 재검증했으며 게임 코드의 컴파일 오류는 아니었다.
- 테스트 중 Play Mode 수동 종료 및 MPPM 시작/도메인 리로드 과정에서 패키지 내부 예외가 있었다. 최종 연결~스폰~방장 퇴장 구간에서 Main Editor의 새 Console 오류는 0이었다. 가상 플레이어에서 발견된 동적 TMP 폰트의 읽기 전용 AssetDatabase 갱신 문제는 미리 구운 정적 로비 폰트로 수정했다.
- 최종 정적 폰트는 한글 완성형 전체를 포함한 11,363문자 / 4096px 아틀라스 1개다. 에디터에서 굽는 작업은 CLI의 120초 응답 제한을 넘겼으나 작업을 중복 실행하지 않고 완료를 확인했다. 이후 가상 플레이어에서 `staticFonts: true`, `hangul: true`, 한글 닉네임 입력 뒤 신규 오류 0 / Console ground truth 오류 0을 확인했다.

## 범위와 남은 검증

이번 작업은 로비, 공동 씬 입장, 소유자별 아바타 스폰/기본 이동까지다. 기존 마차·포탄·적·불안도·타이밍·실패/성장 로직의 서버 권한 및 동기화는 다음 작업이다. 온라인에서는 기존 싱글 플레이어 오브젝트를 끄며, 해당 오브젝트에 연결된 HUD/상호작용도 아직 온라인 플레이어에 이관하지 않았다. mainGame 직접 실행 시 기존 싱글 모드는 유지한다.

전용 서버, 호스트 이전, 진행 중 게임 재접속, 모바일 아바타 조이스틱 연동은 구현 범위 밖이다. 지연/손실 주입, 서로 다른 실제 기기 및 최종 빌드 검증은 수행하지 않았다. NGO 2.14의 UnityTransport `SetDebugSimulatorParameters`는 deprecated이며 “has no effect”로 표시되어 있어 유효한 지연 테스트로 주장하지 않는다.

작업 전 원본 씬/프리팹/빌드 목록 사본은 `Temp/LobbyBackup/`에 있다. 기존 사용자 변경 사항인 YMH 씬과 VersionControlSettings는 이번 작업에서 수정하지 않았다.
