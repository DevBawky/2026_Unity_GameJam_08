---
name: goblin-playtest
description: Run and review OpenAI-controlled playtests of the local 《그건 님이!!!》 web game. Use for requests to play or retest cannon recoil, carrying or catching shells, reloading, or to explain what blocked the previous run. 포탄 운반, 발사 반동, 지난 판 분석, 같은 조건 재실행에 사용한다.
---

# 《그건 님이!!!》 웹판 플레이테스트

저장소 루트에서 `node tools/playtest/cli.mjs`로 실행한다. 현재 채팅의 앞선 결과에 실행 ID가 있으면 그 ID를 이어서 사용한다. 실행 ID가 없고 사용자가 지난 판을 가리키면 로컬의 최근 판을 조회한다.

| 요청 | 실행 |
| --- | --- |
| “포탄 운반이랑 발사 반동을 테스트해 줘” | `node tools/playtest/cli.mjs run` |
| “지난 판에서 뭐가 막혔어?” | `node tools/playtest/cli.mjs latest` |
| “이 판의 결과를 보여줘” | `node tools/playtest/cli.mjs show <id>` |
| “같은 조건으로 다시 돌려 봐” | `node tools/playtest/cli.mjs rerun [id]` |
| “그때 입력을 그대로 재현해 봐” | `node tools/playtest/cli.mjs replay [id]` |

`run`의 기본 시나리오는 `cannon-recoil`이다. 사용자가 조건을 지정하면 `--seed`, `--model`, `--max-calls`를 사용하고 `--help`에서 현재 지원 옵션을 확인한다. `rerun`은 저장된 조건으로 OpenAI가 다시 행동을 고르는 새 판이다. `replay`는 같은 게임 코드에 저장된 입력을 재적용한다. 대괄호는 선택 인자를 뜻한다.

API 실행기는 환경변수 `OPENAI_API_KEY`를 읽는다. 키는 채팅, 명령 인자, 저장소, 결과에 넣지 않는다. 키가 없으면 환경변수 설정이 필요하다고 알려 준다. 플레이 시작 후 API 오류나 한도 종료가 발생하면 상태와 실행 ID를 알려 주고, 남아 있는 플레이 기록까지만 해석한다. 실제 OpenAI 플레이의 근거는 API 실행에서 얻은 입력과 이벤트다.

Windows에서 Codex를 연 뒤 사용자 환경변수를 설정했다면, 실행 명령과 같은 PowerShell 호출에서 아래 준비를 먼저 실행해 현재 채팅을 이어간다.

```powershell
if (-not $env:OPENAI_API_KEY) {
    $env:OPENAI_API_KEY = [Environment]::GetEnvironmentVariable('OPENAI_API_KEY', 'User')
}
```

상세 근거는 `.playtest/runs/`의 실행별 JSON 기록과 Markdown 보고서에 있다. 판 사이에 비교할 때 실행 ID와 시드를 함께 적는다. 막힌 지점은 게임 내 시각, 행동한 캐릭터(AI 또는 동료 봇), 당시 단서, 입력과 결과를 기록에서 확인해 설명한다. 시작 장전탄과 적재탄이 있으므로 새로 가져온 탄의 재발사는 해당 포탄의 보급·적재·장전·발사 근거를 확인한다. 이어서 유리했던 플레이와 확인해 볼 규칙 변경을 제안한다. 그래픽 스타일은 평가 범위에서 제외한다.
