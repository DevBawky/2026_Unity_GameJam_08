const stopLabels = {
  completed: '시나리오 목표 달성',
  call_limit: '모델 호출 한도 도달',
  time_limit: '실행 시간 한도 도달',
  api_error: 'OpenAI API 호출 오류',
  interrupted: '실행 중단',
  ended: '게임 진행 종료',
  running: '실행 중',
};

const gameLabels = {
  tick_limit: '게임 시간 한도 도달',
  lost: '게임 패배',
  clear: '스테이지 클리어',
};

const roleLabels = { supply: '포탄 꺼내기', carry: '포탄 운반', deliver: '포탄 적재', cleaner: '포신 청소', loader: '장전', catch: '포탄 붙잡기', gunner: '포수석', driver: '마부석' };
const floorLabels = { cellar: '지하층', deck: '1층' };
const inputLabels = { move: '이동', approach: '이동', aim: '조준', press: '행동 버튼', leave: '작업대 이탈', wait: '대기' };
const lossLabels = {
  catch_timing_miss: '붙잡기 타이밍 실패',
  catch_timeout: '붙잡기 시간 초과',
  loading_timing_miss: '장전 타이밍 실패',
  unattributed: '원인 미확인',
};
const catchLosses = new Set(['catch_timing_miss', 'catch_timeout']);

function actorLabel(actor, controller, driver) {
  if (!actor) return '작업자 미확인';
  if (controller === 'openai' || actor === 'p1') {
    return driver === 'mock' ? `검증용 플레이어 ${actor}` : `OpenAI ${actor}`;
  }
  return controller === 'bot' ? `봇 ${actor}` : actor;
}

function eventRef(event) {
  return `${event.seconds.toFixed(2)}초 · 틱 ${event.tick} · ${event.id}`;
}

function ammoLabel(event, driver) {
  const labels = { starting_loaded: '시작 장전탄', starting_rack: '시작 적재탄', unknown: '출처 미확인' };
  const estimate = event.provenance === 'fifo_assumed' ? ' · 적재 순서로 추정' : '';
  if (event.ammoSource !== 'delivered') return `${labels[event.ammoSource] ?? labels.unknown}${estimate}`;
  const delivery = event.deliveryActor ? ` · ${actorLabel(event.deliveryActor, 'bot', driver)} 적재` : '';
  const deliveryId = event.deliveryEventId ?? event.assumedDeliveryEventId;
  const ref = deliveryId ? ` · 적재 기록 ${deliveryId}` : '';
  return `새 보급탄${delivery}${ref}${estimate}`;
}

function eventText(event, driver) {
  const actor = actorLabel(event.actor, event.controller, driver);
  const source = event.actor ? `${actor}이 ` : '';
  const reason = event.reason?.trim().replace(/\.$/, '');
  switch (event.type) {
    case 'ammo_picked_up': return `${source}포탄을 꺼냈습니다.`;
    case 'ammo_delivered': return `${source}운반한 포탄을 적재했습니다.`;
    case 'cannon_cleaned': return `포신 청소 완료 (${actor}).`;
    case 'ammo_loaded': return `장전 완료 (${actor} · ${ammoLabel(event, driver)}).`;
    case 'fuse_lit': return `점화 (${actor}).`;
    case 'cannon_fired': return `대포 자동 발사 (${event.source === 'royal_mischief' ? '아이의 장난' : actor} · ${ammoLabel(event, driver)}).`;
    case 'catch_started': {
      const causes = { cannon: '발사 반동', other_impact: '다른 충격', multiple_impacts: '같은 틱의 여러 충격' };
      const cause = causes[event.source] ?? '원인이 확인되지 않은 충격';
      return `${cause}으로 ${actor}에 포탄 붙잡기 작업이 생겼습니다.`;
    }
    case 'catch_succeeded': return `${source}포탄을 붙잡았습니다.`;
    case 'ammo_lost': return `포탄 ${event.count ?? 1}발 손실 (${actor} · ${lossLabels[event.reason] ?? lossLabels.unattributed}).`;
    case 'timing_missed': return `${actor}의 ${roleLabels[event.role] ?? '작업'} 입력이 타이밍을 벗어났습니다.`;
    case 'input_blocked': return `${actor}의 ${inputLabels[event.action] ?? '조작'} 입력이 막혔습니다${reason ? ` (${reason})` : ''}.`;
    case 'movement_blocked': return `${actor}의 이동이 진행되지 않았습니다${reason ? ` (${reason})` : ''}.`;
    case 'floor_changed': return `${source}${event.carrying ? '포탄을 든 채 ' : ''}${floorLabels[event.to] ?? '다른 층'}으로 이동했습니다.`;
    default: return null;
  }
}

function catchAttempts(events) {
  const active = new Map();
  const attempts = [];
  for (const event of events) {
    if (event.type === 'catch_started') {
      const attempt = { start: event, result: null };
      attempts.push(attempt);
      active.set(event.actor, attempt);
    } else if (event.type === 'catch_succeeded' || (event.type === 'ammo_lost' && catchLosses.has(event.reason))) {
      const attempt = active.get(event.actor);
      if (attempt) attempt.result = event;
      active.delete(event.actor);
    }
  }
  return attempts;
}

function catchFinding(attempt, driver, decisions) {
  const { start, result } = attempt;
  const actor = actorLabel(start.actor, start.controller, driver);
  const startRef = eventRef(start);
  if (!result) return `${actor}에게 발사 반동으로 붙잡기 작업이 생겼고, 실행 기록은 결과 확인 전까지입니다 (${startRef}).`;
  const elapsed = (result.seconds - start.seconds).toFixed(2);
  const press = result.controller === 'openai' && decisions.find(decision => (
    decision.before?.tick === result.tick && decision.before.role === 'catch' && decision.action.kind === 'press' && decision.accepted
  ));
  const cue = Number.isFinite(press?.before.timing?.needleDegrees)
    ? ` 버튼을 눌렀을 때 게이지 바늘은 성공 구간 중심에서 ${Math.abs(press.before.timing.needleDegrees)}도 떨어져 있었습니다.`
    : '';
  return `발사 반동으로 ${actor}에게 붙잡기 작업이 생겼습니다 (${startRef}). ${elapsed}초 뒤 ${result.type === 'catch_succeeded' ? '포탄을 확보했습니다' : `포탄 1발을 잃었습니다 (${lossLabels[result.reason]})`} (${eventRef(result)}).${cue}`;
}

function decisionFor(event, decisions) {
  if (event.controller !== 'openai') return null;
  return decisions.find(({before, action, afterTick}) => (
    before?.tick <= event.tick && afterTick >= event.tick &&
    (event.action ? action.kind === event.action && before.tick === event.tick : before.tick < event.tick || action.kind === 'press') &&
    (event.target == null || action.target === event.target)
  ));
}

function blockerFindings(events, decisions, driver) {
  const groups = new Map();
  for (const event of events) {
    const key = JSON.stringify([event.type, event.actor, event.reason, event.action, event.target, event.role]);
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key).push(event);
  }
  return [...groups.values()].sort((a, b) => b.length - a.length || b.at(-1).tick - a.at(-1).tick).slice(0, 3).map(group => {
    const event = group.at(-1);
    const decision = decisionFor(event, decisions);
    const {role, floor, work, notice} = decision?.before ?? {};
    const situation = [floorLabels[floor], roleLabels[event.role ?? role]].filter(Boolean).join(' · ');
    const condition = [work?.hint, work?.title, work?.detail, notice?.self].find(text => text?.trim() && text.trim() !== event.reason?.trim());
    const count = group.length > 1 ? ` 같은 문제가 ${group.length}회 발생했습니다. 마지막 기록` : '';
    return `${situation ? `**${situation}** ` : ''}${eventText(event, driver)}${count} (${eventRef(event)})${condition ? ` 당시 안내는 “${condition}”였습니다.` : ''}`;
  });
}

const showFirstAndLast = (events, limit) => events.length <= limit
  ? events
  : [...events.slice(0, Math.floor(limit / 2)), ...events.slice(-Math.ceil(limit / 2))];

export function generateReport({ metadata, status, final = {}, events = [], decisions = [], error }) {
  const { id, config, driver } = metadata;
  const shots = events.filter(event => event.type === 'cannon_fired');
  const newShots = shots.filter(event => event.ammoSource === 'delivered');
  const confirmedShots = newShots.filter(event => event.provenance === 'unambiguous');
  const startingAmmo = final.initialAmmo;
  const minimumNewShots = startingAmmo ? Math.max(0, shots.length - startingAmmo.rack - Number(startingAmmo.loaded)) : 0;
  const confirmedCount = Math.max(confirmedShots.length, minimumNewShots);
  const attempts = catchAttempts(events);
  const recoil = attempts.filter(attempt => attempt.start.source === 'cannon');
  const playerRecoil = recoil.filter(attempt => attempt.start.actor === 'p1');
  const relevantRecoil = playerRecoil.length ? playerRecoil : recoil;
  const keyCatch = relevantRecoil.find(attempt => attempt.result?.type === 'ammo_lost') ??
    relevantRecoil.find(attempt => !attempt.result) ?? relevantRecoil[0];
  const blocked = events.filter(event => ['timing_missed', 'input_blocked', 'movement_blocked'].includes(event.type) || event.reason === 'loading_timing_miss');
  const deliveries = events.filter(event => event.type === 'ammo_delivered');
  const stopReason = status === 'ended' ? gameLabels[final.endReason ?? final.status] ?? stopLabels.ended : stopLabels[status] ?? status;
  const summary = [
    `실행 \`${id}\` · 시드 \`${config.seed}\` · 모델 \`${config.model}\` · ${stopReason}`,
    `발사 ${shots.length}회 · 새 보급탄 발사 확인 ${confirmedCount}회${newShots.length > confirmedCount ? ` (적재 순서 추정 ${newShots.length}회)` : ''} · 적재 ${deliveries.length}회 · 발사 반동에 의한 붙잡기 ${recoil.length}회`,
    `기록된 ${driver === 'mock' ? '검증용 플레이어' : 'OpenAI'} 결정 ${decisions.length}회${Number.isFinite(final.seconds) ? ` · 게임 시간 ${final.seconds.toFixed(2)}초` : ''}`,
  ];
  const findings = [];
  if (status === 'api_error' && error) findings.push(error);
  if (keyCatch) findings.push(catchFinding(keyCatch, driver, decisions));
  else if (decisions.length) findings.push('이번 기록에서 발사 반동으로 운반자에게 붙잡기 작업이 생긴 사례는 0회였습니다.');
  if (!decisions.length) findings.push('기록된 플레이 입력이 없습니다.');
  findings.push(...blockerFindings(blocked, decisions, driver));
  if (confirmedShots.length) {
    const first = confirmedShots[0];
    findings.push(`새 보급탄을 사용한 첫 발사 (${eventRef(first)} · ${ammoLabel(first, driver)}).`);
  } else if (minimumNewShots > 0) {
    findings.push(`시작 포탄 수를 넘겨 발사해 새 보급탄이 최소 ${minimumNewShots}발 쓰였음을 확인했습니다 (${eventRef(shots.at(-1))}).`);
  } else if (newShots.length) {
    findings.push(`적재 순서대로 포탄을 꺼냈다고 가정하면 새 보급탄이 발사됐습니다 (${eventRef(newShots[0])}).`);
  } else if (events.some(event => event.type === 'ammo_loaded' && event.ammoSource === 'delivered')) {
    const load = events.findLast(event => event.type === 'ammo_loaded' && event.ammoSource === 'delivered');
    findings.push(`보급 후 장전 기록 (${eventRef(load)} · ${ammoLabel(load, driver)}).`);
  } else if (deliveries.length) {
    findings.push(`운반한 포탄은 적재까지 확인됐습니다 (${eventRef(deliveries.at(-1))}).`);
  } else {
    const pickup = events.findLast(event => event.type === 'ammo_picked_up');
    if (pickup) findings.push(`보급 단계에서는 포탄을 꺼낸 기록까지 확인했습니다 (${eventRef(pickup)}).`);
  }
  const recoveryProof = shots.find(event => event.recoveredDeliveryConfirmed);
  if (recoveryProof) {
    const carrier = actorLabel('p1', 'openai', driver);
    findings.push(`${eventRef(recoveryProof)}까지 ${carrier}이 발사 반동에서 붙잡아 적재한 포탄이 최소 한 발 발사됐습니다. 적재칸 재고와 장전·발사 기록을 합산해 확인했습니다.`);
  }
  const lastView = decisions.at(-1)?.after;
  if (lastView && status !== 'completed') {
    const lastRole = roleLabels[lastView.role] ?? lastView.work?.action ?? (lastView.carrying ? '포탄 운반' : '이동');
    findings.push(`마지막 입력 직후 ${actorLabel('p1', 'openai', driver)}은 ${floorLabels[lastView.floor] ?? '현재 층'}에서 ${lastRole} 상태였습니다 (틱 ${lastView.tick}).`);
  }
  const timeline = events.filter(event => eventText(event, driver) !== null);
  const evidence = showFirstAndLast(timeline, 12).map(event => `${eventRef(event)} · ${eventText(event, driver)}`);
  const sections = [
    '# 포탄 운반과 발사 반동 플레이테스트',
    summary.map(line => `- ${line}`).join('\n'),
    '## 진행이 막힌 지점과 확인한 결과',
    findings.map(line => `- ${line}`).join('\n'),
  ];
  if (evidence.length) sections.push('## 주요 장면', evidence.map(line => `- ${line}`).join('\n'));
  if (keyCatch?.result?.type === 'ammo_lost') {
    sections.push(
      '## 다음 비교 실험',
      '- 같은 시드에서 지금 점화하기와 운반자가 적재한 직후 점화하기를 비교합니다. 포탄 손실, 기다리는 동안 받은 피해, 새 포탄으로 다시 쏘기까지 걸린 시간을 함께 기록합니다.\n- 점화 시점을 유지한 채 다른 층에도 점화를 알려주는 조건을 따로 시험하면, 안내가 붙잡기 성공률에 영향을 주는지 확인할 수 있습니다.',
    );
  } else if (keyCatch?.result?.type === 'catch_succeeded') {
    const delivery = events.find(event => event.type === 'ammo_delivered' && event.recoveryEventId === keyCatch.result.id);
    sections.push(
      '## 플레이 팁',
      `- 반동으로 붙잡기 게이지가 뜨면 운반을 멈추고 바늘이 성공 구간에 들어갈 때 행동 버튼을 누릅니다. 이번 판의 붙잡기 성공은 ${keyCatch.result.id}${delivery ? `, 이후 같은 운반자의 적재는 ${delivery.id}` : ''}에서 확인했습니다.`,
    );
  }
  return `${sections.join('\n\n')}\n`;
}
