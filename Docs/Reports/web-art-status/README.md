# 《그건 님이!!!》 픽셀 아트 점검 사이트

[Vercel 프로덕션 사이트](https://goblin-getaway-art-status.vercel.app/)

`status.json`에 검토 결과와 변경 이력을 기록한다. `build.mjs`는 배포용 사이트와 2026-11-19 보고서의 스크린샷을 `dist/`에 묶는다. 새 증거 이미지가 필요하면 `evidence/`에 고유한 PNG 파일명으로 추가하고 해당 항목의 `screenshots`에 연결한다.

## 재점검과 배포

저장소 루트에서 추적 중인 소스가 마지막 검토 이후 달라졌는지 확인한다.

```powershell
node Docs/Reports/web-art-status/build.mjs --check
```

변경된 그룹은 웹 빌드의 실제 렌더링 근거를 확인한 뒤 `status.json`에 적용·잔여 작업·소스 위치를 갱신한다. 시각 상태나 근거에 유의미한 변화가 생기면 `history`에 기록한다. 확인이 끝난 항목의 ID를 `--record`에 전달한다. 관련 소스가 바뀐 항목은 검토를 기록할 때까지 사이트에서 `재확인`으로 표시한다.

```powershell
node Docs/Reports/web-art-status/build.mjs --record 01 07
node Docs/Reports/web-art-status/build.mjs
pnpm.cmd --silent dlx vercel@66.2.0 deploy --prod --yes --scope js-company --cwd Docs/Reports/web-art-status/dist
```

새 작업 폴더에서는 기존 `goblin-getaway-art-status` 프로젝트에 한 번 연결한다. 배포 후 프로덕션 URL의 `/status.json`에서 `checkedAt`, `revision`, 항목 상태를 배포용 파일과 대조한다.

```powershell
pnpm.cmd --silent dlx vercel@66.2.0 link --yes --project goblin-getaway-art-status --scope js-company --cwd Docs/Reports/web-art-status/dist
```
