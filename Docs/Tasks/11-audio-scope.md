# 11 사운드 범위와 적용 방식

## 웹판 플레이 점검

- **1인 직접 플레이** · 2026-11-19 로컬 웹판 `wheel-tread-tracks137`에서 효과음을 켜고 테스트 모드와 일반 작전을 플레이했다. 팀 장전 요청, 지하층 이동, 포탄 보급과 확보, 일시 정지·재개, 왕자 하트 보내기를 조작했다. 도로 장애물 경고, 추격대와 투석기 접근, 대포 포구 섬광, 바퀴 이탈·수리 요청, 왕자의 울음 상태를 화면에서 확인했다. 1스테이지 1,000m를 클리어한 뒤 돌봄 평가로 13 AP를 받고, 마왕 보고, 정밀 도구 구매(4 AP), 2스테이지 출발까지 진행했다. [웹 실행 방법](../../goblin-getaway/편집_시작하기.md)
- **한 화면 2인 직접 플레이** · 후속 로컬 빌드 `original-rope138`에서 협동 메뉴와 온라인 방 입력 UI를 확인했다. 한 화면 테스트 모드로 들어가 두 조작 패널과 P2 바퀴 수리 요청의 공용 알림을 확인했다.
- **재현 및 코드 확인** · 시뮬레이션에서 1~3스테이지 클리어, 마왕 보고·구매와 출발, 내구도 소진 패배를 재현했다. 포탄 붙잡기·운반·장전·재발사와 놓침은 입력 재현 테스트에서 확인했다. 온라인 방의 실제 참가·동기화와 나머지 조건은 코드로 대조했다. 브라우저 스피커 출력은 청취하지 않았다. [입력 재현 테스트](../../tools/playtest/playtest.test.mjs)

웹 효과음 기본값은 꺼짐이다. 켜면 Web Audio로 공용 UI·작업 확인음, 공용 충격음, 대포·투석기 착탄음, 가까운 아이 울음, 클리어·실패음을 합성한다. 1인과 한 화면 2인의 작업 판정에는 공용음이 쓰이며 온라인 작업 결과와 자동 적재·제작 완료는 이 재생 경로를 거치지 않는다. 아래 우선순위는 플레이와 코드 점검을 바탕으로 한 제안이다. [현재 웹 재생 코드](../../goblin-getaway/public/game.mjs#L17)

## 1차 작업 · 판단과 행동에 필요한 소리

| 구간 | 재생 지점 | 현재 웹판 |
| --- | --- | --- |
| 앞길 장애물 | 통나무·바위·야생동물 경고가 처음 뜰 때 위·아래 위험 방향을 구분한다. 실제 충돌은 별도 음으로 알린다. | 경고는 시각 표시. 실제 충돌은 공용 충격음 |
| 추격대 | 새 웨이브, 후방 접근, 적의 공격 예고, 마차 피격을 구분한다. 같은 무리의 말마다 접근음을 반복할 필요는 없다. | 접근은 시각 표시. 피격은 공용 충격음 |
| 투석기 | 발사·비행 접근, 착탄 지점 고정, 지면에 빗맞음과 마차에 명중. 피할 수 있는 마지막 단서인 표적 고정을 강조한다. | 지면 착탄과 마차 명중에 같은 착탄 합성음 |
| 마차 내구도 | 처음 위험 구간에 진입했을 때. | 피격에 공용음. 위험 구간 진입음 없음 |
| 바퀴 이탈 | 첫 바퀴 이탈·수리 필요, 같은 차축의 나머지 바퀴 이탈과 급감속. 빠진 바퀴가 추격대를 치는 순간에는 짧은 명중음. | 이탈은 공용 충격음. 수리 필요는 시각 표시 |
| 바퀴 보급·수리 | 예비 바퀴 확보·반납·부족, 타이밍 실패, 바퀴 장착, 핀 고정, 수리 완료. | 일부 오프라인 단계에 공용음 |
| 포탄 더미 | 포탄 확보, 타이밍 실패, 보급고가 비었거나 포탄을 들 수 없는 상황에서 입력. | 타이밍 시각 표시. 오프라인 입력은 공용음 |
| 포탄 미끄러짐 | 손에서 미끄러지기 시작할 때 긴급 신호. 약 1.6초 안에 붙잡기 성공, 수동 실패, 시간 초과·실제 낙하를 구분한다. | 시작·시간 초과에 전용음 없음. 수동 실패 문구가 `포탄을 놓쳤습니다`라 공용 성공음이 날 수 있음 |
| 포탄 적재·장전 | 적재칸 자동 투입, 장전 가능 전환, 장전 성공, 실패로 포탄이 떨어지는 순간, 적재탄 부족. | 자동 적재에 전용음 없음. 장전 입력은 오프라인 공용음 |
| 대포 점화·발사 | 점화, 약 2.5초간 타는 심지와 발사 직전, 실제 발사·반동, 포탄 착탄. 점화와 포구 발사를 구분한다. | 점화는 오프라인 공용음, 반동은 공용 충격음, 착탄은 별도 합성음 |
| 포격 명중 | 지면 착탄, 적에 직접 또는 충격파 명중, 적 격퇴. 여러 적이 동시에 맞으면 한 포격 결과로 들리게 한다. | 착탄 합성음. 적 명중 여부의 소리 구분 없음 |
| 포신 청소 | 유효한 청소 입력에는 짧은 소리, 마지막 청소와 장전 가능 전환에는 완료음 한 번. | 오프라인 입력 공용음 |
| 마부·말 상태 | 채찍질, 분노 상한과 폭주 진입, 피로로 인한 급감속, 회복. | 채찍질에 오프라인 공용음. 폭주·피로·회복 구분음 없음 |
| 아이의 위험 상태 | 왕자·공주가 떨어지거나 좌석에서 벗어남, 관심·간식 요구가 시작됨, 울기 시작함. 포탄을 훔치거나 떨어뜨림, 무단 발사·채찍질은 팀에 알린다. | 같은 층 근거리 울음 합성. 두 아이가 약 4.2초 재생 간격을 공유 |
| 팀 요청 | 포탄 운반, 장전, 수리, 놀아주기, 채찍질 요청을 받은 순간. 다른 층에 있는 팀원도 알아차릴 수 있게 한다. | 말풍선·팀 알림 표시. 전용음 없음 |
| 작전 결과 | 스테이지 클리어, 챕터 완료, 작전 실패. 결과 화면을 열 때 한 번씩 재생한다. | 클리어와 실패는 서로 다른 공용음. 스테이지·챕터 클리어는 같은 음 |

## 2차 작업 · 조작과 화면 전환 피드백

| 구간 | 재생 지점 | 현재 웹판 |
| --- | --- | --- |
| 메뉴·설정 | 환경설정 열기·뒤로·닫기, 효과음 켬·끔, 전체화면 전환·실패, 조작 가이드·정비 카드 도감·작전 현황 열기, 1인·한 화면 2인 모드와 탑승원 선택. 열기·뒤로·확정·오류음을 공유한다. 효과음 끄기는 설정 변경 즉시 반영한다. | 효과음을 켤 때만 확인음 |
| 작전 제어 | 일반·테스트 모드 시작, 일시 정지·재개, 테스트 종료, 재도전·챕터 재시작. 메뉴 열기와 정지가 함께 발생하면 확인음 한 번. | 오프라인 시작에 공용음 |
| 온라인 로비 | 방 생성·입장·진행 중 합류·퇴장, 초대 링크·코드 복사, 방 코드 오류나 방 사용 불가. | 화면과 토스트 표시. 전용음 없음 |
| 온라인 세션 | 팀원 입퇴장, 방장 변경, 연결 끊김·복구·방 종료, 방장의 시작·정지·재개로 내 플레이 상태가 바뀔 때. | 재생 분기 없음 |
| 이모티콘 UI | 목록 열기·선택·전송 확인, 칭찬·놀람 수신. 긴급 요청과 일반 감정 반응의 소리를 구분한다. | 목록·말풍선·팀 알림 표시. 전용음 없음 |
| 층·작업대·망원경 | 1층과 지하층 전환, 작업대 맡기·이탈, 망원경 올림·내림, 실제로 막힌 의도적인 입력. 대포는 청소→장전→포격에서 가능한 행동이 바뀌는 때만 알린다. | 층 이동·이탈·상태 변화는 시각 안내 중심 |
| 타이밍 보조 | 보급·장전·붙잡기·수리의 `지금!` 진입을 선택형으로 알린다. 한 화면 2인에서는 담당자 구분이 필요하다. | 게이지와 텍스트 표시 |
| 아이 돌봄 | 안아 올림, 달래기 중 지루해짐·장소 전환 필요, 달래기 완료와 울음 멈춤, 다시 앉힘, 먹이기, 하트 반응. | 울음 합성음과 일부 오프라인 행동의 공용음 |
| 간식 제작 | 길게 누르기 시작·해제, 만드는 동안의 짧은 지속음, 충격으로 진행도가 깎임, 완성·전달. | 제작은 오프라인 공용 확인음 분기도 건너뜀 |
| 마왕 보고·정산 | 마왕에게 보고, 회신과 정비 상점 열기, 돌봄 평가 애정 포인트 지급. | 오프라인 보고에 공용음 |
| 정비 상점·출발 | 카드 보기, 찬성 등록, 과반 확정, 구매·포인트 차감·수리 또는 강화 적용, 구매 실패, 다음 스테이지·챕터 출발. 온라인에서는 투표 등록과 팀 구매 확정을 구분한다. | 오프라인 투표·구매·출발에 공용음. 온라인 확정과 종류별 효과음 없음 |

## 보류 · 배경과 반복 소리

- 메뉴·주행·결과·정비 화면의 BGM을 검토한다. 발표용 두 곡은 후보로 들어보고 반복 재생 시 시작과 끝이 자연스러운지 확인한다.
- 말발굽·바퀴 구름·나무 삐걱임·바람은 실제 속도와 일시 정지에 맞춘다. 층별 발걸음, 빠진 바퀴 구름, 장애물 종류별 충돌음은 경고와 구분할 수 있을 때 추가한다.
- 순위 추월음은 필요성을 다시 확인한다. 순위·점수·거리·속도·조준률·게이지의 잦은 갱신, 버튼 호버, 비활성 버튼, 이름·코드 입력과 일반 조준 이동은 무음으로 둔다.

## 음원과 연결 기준

- 대포 전용 발사음 후보로 Unity의 `Cannon.wav`를 비교한다. `Impact.wav`는 공용 피격의 임시 후보로 쓸 수 있다. 웹의 별도 착탄음과 아이 울음은 우선 활용한다. UI 조작과 작업 결과는 계열별로 공유하되 다음 행동이 달라지는 경고는 구분한다. [Unity 효과음](../../Assets/Audio/Cart/) · [웹 사운드 코드](../../goblin-getaway/public/game.mjs#L28)
- 발표용 BGM 〈고삐는 누가 잡았어〉와 〈바퀴야 버텨라〉는 MP3·WAV로 보관돼 있다. `handoff/`는 Vercel 배포에서 제외된다. 적용을 결정하면 선택한 편집본을 웹 공개 에셋으로 연결하고 BGM과 효과음 설정을 따로 둔다. [BGM 원본](../../goblin-getaway/handoff/music/) · [배포 설정](../../goblin-getaway/.vercelignore#L12)
- 팀 경고·발사·바퀴 이탈·결과는 확정 이벤트당 한 번. 작업대 조작과 타이밍 결과는 담당자 중심, 아이 울음과 발걸음은 층·거리 기준으로 처리한다. 한 화면 2인, 온라인 재접속과 상태 재수신, 피격과 바퀴 이탈이 겹치는 순간에는 중복 재생과 경고음 우선순위를 확인한다.
- 작업 성공·실패·자동 완료는 화면 문구 대신 판정 결과로 구분한다. 기존 시각 경고도 제공한다. 브라우저의 첫 사용자 입력 뒤 재생과 정지·음소거·재개 시 루프 종료·재시작을 확인한다. [온라인·작업 입력](../../goblin-getaway/public/game.mjs#L494) · [월드 재생 분기](../../goblin-getaway/public/game.mjs#L978)

## Suno 생성용 원본

판타지 마차의 나무·쇠·흙 소리를 중심으로 20개 원본 후보를 준비한다. UI 확인음은 짧고 부드럽게, 팀 요청과 긴급 경고는 작은 스피커에서도 구분되는 음역으로 잡는다. 기본 프롬프트에 `Dry game sound effect, clean isolated recording, no music bed, no spoken words, short clean tail.`을 붙인다. 아래 시간은 게임에 넣을 편집본 기준이다.

| 원본 이름 | Suno Sounds 프롬프트 | 연결할 지점 |
| --- | --- | --- |
| `ui-tick` | Tiny hollow birch button tap, slight felt softness, one crisp transient. 0.12 s. | 메뉴·가이드·도감·카드·이모티콘 열기, 뒤로·닫기, 작업대 진입·이탈 |
| `ui-confirm` | Two soft rising wooden marimba notes with a small brushed brass ring. 0.28 s. | 모드·설정 변경, AP 지급, 보고·구매·출발, 연결 성공·복구, 돌봄과 타이밍 성공 |
| `ui-reject` | Two short descending muted block notes, dry and unobtrusive. 0.25 s. | 제약·타이밍 실패, 재고 부족, 방·정비 오류, 연결 단절 |
| `team-bell` | Small hand-held bronze bell with two uneven taps, clear midrange and muted tail. 0.35 s. | 포탄·장전·수리·조작·돌봄 요청, 얕은 변형은 칭찬·놀람 |
| `danger-pulse` | Short urgent three-part wooden alarm with one bright restrained metallic accent. 0.45 s. | 도로 상·하 방향, 투석기 표적 고정, 낮은 내구도, 포탄 미끄러짐 |
| `pursuit-horn` | A distant medieval hunting horn pair with one low hoof thud, tight tail. 0.7 s. | 추격대 웨이브와 후방 접근, 근접 공격 예고 |
| `stone-flight` | Fast airborne boulder passing overhead, airy rough-grit sweep with a short low thump; no landing. 0.8 s. | 투석기 발사·접근 |
| `impact-dirt` | Heavy stone burial in packed earth, short sub thump and gravel scattering, no exaggerated reverb. 0.65 s. | 지면 착탄, 대포 충격파와 적 명중, 날아간 바퀴의 추격대 명중 |
| `impact-cart` | Moving wooden wagon slammed sideways, hollow timber thud followed by a tight iron rattle. 0.6 s. | 장애물·적·투석기 실제 마차 피격 |
| `axle-snap` | Hardwood axle snapping with an iron pin springing loose and one short wheel chatter. 0.6 s. | 바퀴 이탈, 겹친 차축 손상과 급감속 |
| `wheel-fit` | Heavy wooden wheel hub sliding onto an axle, followed by a distinct iron pin click. 0.5 s. | 예비 바퀴 확보·반납, 장착과 핀 고정·수리 완료 |
| `shell-metal` | Dense iron ball lifted off rough boards, then one separated metallic knock. 0.45 s. | 포탄 확보·붙잡기·자동 적재, 놓침·낙하, 아이가 포탄을 훔치거나 떨어뜨림 |
| `cannon-load` | Two weighty clicks as round iron ammunition seats inside a thick metal barrel. 0.45 s. | 장전과 장전 가능 전환, 포신 청소 완료와 구분 |
| `fuse-burn` | Single igniting spark, dry hiss and fine accelerating crackle. No explosion. 2.5 s. | 점화와 심지 연소, 끝부분을 발사 직전 신호로 사용 |
| `cannon-fire` | Fantasy black-powder wagon cannon blast, sharp muzzle crack over a compact low boom and recoil creak. 0.85 s. | 실제 발사와 반동, 아이의 무단 발사 |
| `brush-scrub` | One coarse brush stroke scraping a soot-covered cannon barrel, short and soft. 0.20 s. | 유효한 포신 청소 입력 |
| `whip-snap` | A short leather whip swish and sharp but restrained snap in open air. 0.30 s. | 채찍질, 폭주·피로·회복은 현재 단계에 맞춰 짧은 변형과 결합 |
| `royal-whimper` | Soft startled cartoon toddler whimper, one breathy sob, no recognizable words. 0.65 s. | 아이 낙상과 울음, 왕자·공주는 피치로 구분. 들기·달래기·착석·먹이기는 확인음 변형 |
| `cook-sizzle` | Quiet hot skillet sizzle with a soft wood spoon contact, even texture and no room noise. 0.85 s. | 간식 제작 중 지속음, 해제·충격 시 끊고 완성·전달에는 확인음을 사용 |
| `stage-result` | Short medieval adventure game result sting, clean three-note brass and wooden percussion accent. No sustained backing track. 0.9 s. | 스테이지 통과와 챕터 완료, 패배는 별도 하강 편집 |

원본은 Suno의 공식 다운로드·Export로 확보한 뒤 무음과 잔향을 정리한다. 도로 경고의 방향, 타이밍 성패, 장착과 핀 고정처럼 다음 행동을 바꾸는 소리는 재생 타이밍·위치·음형을 구분한다. 웹용 파일은 `goblin-getaway/public/sfx/`에, Unity용 파일과 메타데이터는 `Assets/Audio/SFX/`에 둔다. 위 원본 이름은 제작 순서를 잡기 위한 것으로, Export 후 실제 파일과 이벤트 연결을 확인한다. [Suno Sounds 사용법](https://help.suno.com/en/articles/10625537) · [공식 다운로드 한도](https://help.suno.com/en/articles/13876865)

## 다음 단계

- [ ] 웹 1차 적용 목록을 확정하고 음원 제작·재사용 여부를 결정한다.
- [ ] 브라우저에서 실제 출력음과 볼륨을 확인한다. 1인, 한 화면 2인, 온라인 협동에서 성공·실패·재연결 시 재생을 확인한다.
- [ ] Unity에서는 기존 메인 씬의 발사·공용 충격 연결을 기준으로 같은 사건의 적용 지점을 정한다. [메인 씬](../../Assets/Scenes/mainGame.unity#L22053) · [대포 태스크](02-cannon.md) · [마차 태스크](03-carriage.md)
