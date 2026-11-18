---
name: final-check
description: 오늘 작업한 범위(커밋 전 변경 + 오늘 커밋)의 C# 코드를 ponytail 관점으로 최종 점검한다. 버그, 과설계·죽은 코드, 분리·리팩토링 판단, 성능·GC, 트윈·수명 관리, 런타임 생성 대신 프리팹·씬 배치, 컨벤션을 항목별로 보고하고, 프로파일러로 실측한다. 수정은 하지 않는다. "/final-check", "최종확인", "오늘 작업 점검해줘" 요청 시 사용.
---

# 최종 확인

오늘 작업한 코드를 커밋 전에 점검한다. **보고만 하고 고치지 않는다.** 수정은 보고 끝에 물어보고 사용자가 고르면 한다.

## 1. 범위 정하기

```bash
git status --short
git log --since=midnight --name-only --pretty=format:"%h %s"
git diff HEAD --stat
```

- 대상: 커밋 전 변경(수정·새 파일) + 오늘 커밋에 들어간 `.cs` 파일. 씬·프리팹은 참조·배치 확인용으로만 본다.
- 인자로 경로가 오면(`/final-check Assets/Scripts/Feature/Gacha/Test`) 그 안으로 좁힌다.
- 범위를 맨 처음 한 줄로 알린다(예: "4c07a51 이후 미커밋 + 오늘 커밋 2개, 스크립트 7개").

## 2. 준비

1. Skill 툴로 `ponytail:ponytail`을 불러 그 관점(YAGNI, 지우기 우선, 있는 것 재사용)으로 본다.
2. `Docs/coding-convention.md`가 있으면 읽는다(잼 프로젝트엔 없을 수 있음 → 아래 "컨벤션" 항목 기준). 메모리의 코딩 컨벤션·추후 변경점 주석 규칙도 따른다.
3. 대상 파일은 **전부 끝까지 읽는다.** diff만 보고 판단하지 않는다. 수정한 메서드의 호출부는 Grep으로 찾아 같이 본다.

## 3. 점검 항목

### 버그·주의
- 연속 입력·도중 재시작 시 겹침, 순서 의존(카운트 증가 전후), null, 경계값.
- 표로: 위치(`파일:줄`) / 내용 / 심각도(높음·중간·낮음).

### 과설계·죽은 코드 (지울 것)
- 아무도 안 쓰는 필드·설정·분기(씬·프리팹 직렬화 값까지 Grep으로 확인), 구현 하나짜리 추상화, 쓸 데 없는 설정값.
- 코드와 안 맞는 오래된 주석, 같은 값을 두 번 구하는 등 군더더기.
- 이미 코드베이스에 있는 헬퍼를 다시 만든 곳.

### 분리·리팩토링 판단
- 중복 코드, 너무 큰 클래스, 책임이 섞인 곳을 짚되 **지금 할지 말지 결론**을 낸다.
- 곧 지워질 코드나 구조가 바뀔 코드는 "지금은 안 함 + 언제 할지"로 답한다. 실제로 쓰일 가능성이 있는 크리티컬·큰 범주는 테스트 코드라도 지금 하자고 권한다.

### 성능·GC
- `Update`/`LateUpdate`/`FixedUpdate`나 자주 불리는 콜백 안의 할당(`new`, 배열·리스트 생성, 문자열 조합, 박싱), 람다 캡처, LINQ.
- 반복 호출되는 `FindObjectsByType`/`FindObjectOfType`/`GameObject.Find`/`GetComponent(s)InChildren` — 캐싱하거나 목록을 직접 들고 있을 수 있는지.
- `Camera.main` 반복 호출, `renderer.material` 접근(머티리얼 복제), 매 프레임 `SetPropertyBlock`.
- 한 번만 하는 초기화·연출 한 번당 몇 번 정도는 문제 아님 — **빈도**를 같이 적는다.

### 트윈·수명 관리
- DOTween `Kill`/`SetLink`/`SetTarget` 누락, 같은 대상에 트윈이 겹쳐 쌓이는 곳, 끝 콜백이 새 상태를 덮는 곳.
- 파괴된 오브젝트 참조, `Destroy` 뒤 같은 프레임 접근.
- 씬 종료·비활성화 시 남는 전역 상태(`Time.timeScale`, static, 싱글턴, RenderSettings, 카메라 값) — `OnDisable`/`OnDestroy`에서 되돌리는지.
- 이벤트 구독(`+=`)의 해제 짝.

### 런타임 생성 → 프리팹·씬 배치
- `new GameObject`, `AddComponent`, `Instantiate` 후 코드로 모양·값을 다 잡는 것, `CreatePrimitive`, 코드로 만드는 머티리얼·메시·LineRenderer 등.
- 매번 똑같이 생기는 것이면 씬에 배치하거나 프리팹으로 두는 게 나은지(인스펙터에서 바로 다듬을 수 있고, 생성 비용·코드가 준다).
- 자주 생겼다 사라지는 것(입자, 충격파 복제본 등)은 풀링이나 하나를 재사용할 수 있는지.
- 반대로 경로·개수에 따라 모양이 바뀌는 절차적 생성(파이프 메시 등)은 런타임이 맞다 — 그렇게 판단한 근거를 적는다.

### 컨벤션
- 네이밍(`_camelCase`, `UPPER_CASE`, PascalCase), 접근 제어자 명시, using 길이순 정렬, 정렬용 공백 금지, XML 주석, 매직 넘버, 당연한 꼬리 주석 금지, `// 추후 변경점:` 표기(`ponytail:` 표기 금지).
- 지켜진 것은 한 줄로, 어긋난 것은 위치와 함께.

## 4. 프로파일러 측정

코드 점검을 마친 뒤 실측한다(공식 `unity-editor-mcp`).

1. 대상 씬이 열려 있는지 확인(`scene_list_open`)하고 `editor_playmode_start`.
2. `diagnostics_profiler_record frames=300 output-path=Temp/final-check.data`로 대상 기능이 도는 구간을 잰다 → `diagnostics_profiler_record_status`로 완료 대기. 입력은 `runtime_simulate_key`/`runtime_simulate_pointer`로 직접 넣는다. 드래그처럼 시뮬레이션이 어려운 조작만 사용자에게 해 달라고 하고 그 동안 잰다.
3. `diagnostics_profiler_session`으로 열어 sessionId → `diagnostics_profiler_summary`를 sort=cpu, sort=gc로 본다. 스파이크 프레임은 `diagnostics_profiler_frame`으로 쪼개 위 "성능·GC"에서 짚은 곳과 맞는지 대조한다.
4. `editor_playmode_stop`.
5. Unity가 백그라운드라 시간이 안 흐르거나 측정이 비면, 억지로 해석하지 말고 "측정 못 함 + 이유"로 남긴다.

## 5. 보고 형식

한국어로, 항목별 제목(`## 1. 버그·주의` … `## 8. 프로파일러`)으로 나눈다. 문제가 없는 항목도 "없음 + 근거 한 줄"로 남긴다.

마지막에 결론 한 줄과 함께 고칠 것을 묶어 물어본다:
"바로 고칠 것: A, B (지우는 쪽이 더 많은 작은 수정). 할까요?"
