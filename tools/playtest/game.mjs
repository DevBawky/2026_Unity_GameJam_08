import {
  BARREL, CELLAR_FLOOR, DECK_FLOOR, HZ, PLAYER_IDS, ROLES, STAIRS, STATIONS,
  aimAngle, begin, cannonStation, catchPhase, enemyKnockback, feedbackKey, firingAngle, hit, initial, leaveStation, occupiedStation,
  operate, phase, playerFloor, setInput, stationAt, step, target, width,
} from '../../goblin-getaway/public/sim.mjs';
import { cameraViews, workInfo } from '../../goblin-getaway/public/workstation.mjs';

const PLAYER = 'p1';
const MAX_ACTION_TICKS = 90;
const APPROACH_SLOWDOWN_RADIUS = 6;
const ACTIONS = ['move', 'aim', 'press', 'leave', 'wait', 'approach'];
const MARKERS = {
  [DECK_FLOOR]: { stairs: '지하층 계단', rack: '포탄 적재칸', cleaner: '포신 청소대', loader: '장전대', gunner: '포수석', driver: '마부석' },
  [CELLAR_FLOOR]: { stairs: '1층 계단', supply: '포탄 더미', cook: '간이 음식 제작대', 'wheel-pickup': '바퀴 보관함' },
};

const round = (number, digits = 1) => Number(number.toFixed(digits));
const marker = id => id === 'stairs' ? STAIRS : STATIONS[id];

export const actionSchema = {
  type: 'object',
  properties: {
    kind: { type: 'string', enum: ACTIONS, description: 'move, aim, press once, leave the workstation, wait, or approach a visible waypoint.' },
    x: { type: 'number', minimum: -1, maximum: 1, description: 'Left negative, right positive for move or aim. Use 0 for other types.' },
    y: { type: 'number', minimum: -1, maximum: 1, description: 'Up negative, down positive for move or aim. Use 0 for other types.' },
    ticks: { type: 'integer', minimum: 1, maximum: MAX_ACTION_TICKS, description: 'Maximum duration in 30 Hz game ticks. Use 1 for a quick press or exit.' },
    target: { type: ['string', 'null'], description: 'A currently visible waypoint ID for approach; otherwise null.' },
  },
  required: ['kind', 'x', 'y', 'ticks', 'target'],
  additionalProperties: false,
};

function playerView(s, id) {
  const p = s.players[id];
  const role = stationAt(p, s);
  const seat = occupiedStation(p, s);
  const info = role ? workInfo(s, p, role) : null;
  const work = info && {
    action: info.action ?? null,
    hint: info.hint ?? null,
    progress: info.progress === undefined ? null : round(info.progress * 100),
    enabled: !!info.action && !info.blocked && role !== 'cook',
  };
  let timing = null;
  if (info && !info.mode && !info.blocked) {
    const needle = role === 'catch' ? catchPhase(p, s.tick) : phase(s, role);
    const delta = ((needle - target(s, role) + 1.5) % 1) - 0.5;
    timing = {
      ready: hit(s, role, needle) && (role === 'catch' || !(s[`${feedbackKey(role)}Cd`] > s.tick)),
      needleDegrees: round(delta * 360),
      greenWidthDegrees: round(width(s, role) * 360),
    };
  }
  return { role, seat, work, timing };
}

function visibleScene(s, p) {
  const camera = cameraViews(s, false, PLAYER, 1920, 1080)[0];
  const right = camera.left + camera.span / camera.zoom;
  const bottom = camera.top + camera.height / camera.zoom;
  const visible = (x, y, r = 0) => x + r >= camera.left && x - r <= right && y + r >= camera.top && y - r <= bottom;
  const floor = playerFloor(p);
  const activeCannonStation = cannonStation(s);
  const waypoints = Object.entries(MARKERS[floor]).flatMap(([id, label]) => {
    if (['cleaner', 'loader', 'gunner'].includes(id) && id !== activeCannonStation) return [];
    const spot = marker(id);
    return visible(spot.x, s.y + spot.y, spot.r)
      ? [{ id, label, dx: round(spot.x - p.x), dy: round(spot.y - p.y) }]
      : [];
  });
  const targets = camera.role !== 'gunner' ? [] : s.enemies
    .filter(enemy => !enemyKnockback(enemy) && visible(enemy.x, enemy.y))
    .map(enemy => ({ dxFromCannon: round(enemy.x - BARREL.x), dyFromCannon: round(enemy.y - s.y) }));
  const team = camera.role ? [] : PLAYER_IDS.filter(id => id !== PLAYER).flatMap(id => {
    const mate = s.players[id];
    if (playerFloor(mate) !== floor || !visible(mate.x, s.y + mate.y)) return [];
    return [{ id, floor, activity: mate.ball ? '포탄 운반 중' : ROLES[occupiedStation(mate, s)] ?? null }];
  });
  return { waypoints, targets, team };
}

function capture(s) {
  return {
    recoilTick: s.cannonRecoil?.tick,
    fuse: { ...s.fuse },
    rack: s.ammo.rack,
    loaded: s.ammo.loaded,
    cleaning: s.ammo.cleaning,
    lost: s.ammo.lost,
    royalShells: new Set(s.royals.filter(royal => royal.shellPlayUntil).map(royal => royal.id)),
    hp: s.hp,
    kill: s.kill,
    serial: s.serial,
    players: Object.fromEntries(PLAYER_IDS.map(id => {
      const p = s.players[id];
      return [id, { floor: playerFloor(p), role: stationAt(p, s), ball: p.ball, slip: p.slipUntil, noteUntil: p.noteUntil }];
    })),
  };
}

function actionError(action) {
  if (!action || !ACTIONS.includes(action.kind)) return '지원하지 않는 입력입니다.';
  if (!Number.isInteger(action.ticks) || action.ticks < 1 || action.ticks > MAX_ACTION_TICKS) return 'ticks는 1~90 사이의 정수여야 합니다.';
  if (![action.x, action.y].every(value => Number.isFinite(value) && Math.abs(value) <= 1)) return 'x와 y는 -1~1 범위여야 합니다.';
  if (action.kind === 'approach' && typeof action.target !== 'string') return 'approach에 보이는 지점의 ID가 필요합니다.';
  return null;
}

export function createGame({ seed = 718, maxTicks = 3600, scenario = 'cannon-recoil' } = {}) {
  if (!Number.isInteger(seed) || seed < 0 || seed > 0xffffffff) throw new RangeError('seed는 부호 없는 32비트 정수여야 합니다.');
  if (!Number.isInteger(maxTicks) || maxTicks < 1 || maxTicks > 7200) throw new RangeError('maxTicks는 1~7200 사이의 정수여야 합니다.');
  if (scenario !== 'cannon-recoil') throw new RangeError('지원하는 시나리오는 cannon-recoil입니다.');

  const s = initial(1, undefined, seed);
  begin(s);
  const events = [];
  const initialAmmo = { ...s.ammo };
  const rack = Array.from({length: initialAmmo.rack}, () => ({ ammoSource: 'starting_rack' }));
  const royalShells = new Map();
  const pendingCatch = new Map();
  const recoveredCarry = new Map();
  let rackOrderUncertain = false;
  let possiblePlayerRounds = 0;
  let possibleRecoveredRounds = 0;
  let chamber = { ammoSource: initialAmmo.loaded ? 'starting_loaded' : 'unknown' };
  let shots = 0;
  let deliveredShots = 0;
  let playerDeliveredShots = 0;
  let playerCannonCatches = 0;
  let playerCannonCatchSuccesses = 0;
  let recoveredPlayerShots = 0;

  function event(type, actor, fields = {}) {
    const result = {
      id: `E${String(events.length + 1).padStart(4, '0')}`,
      tick: s.tick,
      seconds: round(s.tick / HZ, 3),
      type,
      actor,
      controller: actor === PLAYER ? 'openai' : actor ? 'bot' : null,
      ...fields,
    };
    events.push(result);
    return result;
  }

  function takeRackShell() {
    const unambiguous = rack.length === 1 && !rackOrderUncertain;
    const shell = { ...(rack.shift() ?? { ammoSource: 'unknown' }), provenance: unambiguous ? 'unambiguous' : 'fifo_assumed' };
    if (!unambiguous && shell.deliveryEventId) {
      shell.assumedDeliveryEventId = shell.deliveryEventId;
      delete shell.deliveryEventId;
    }
    rackOrderUncertain = rack.length > 0;
    return shell;
  }

  function track(before) {
    const fired = s.cannonRecoil && s.cannonRecoil.tick !== before.recoilTick;
    const impacts = s.serial - before.serial;
    const impactNote = s.eventUntil > s.tick ? s.event : null;
    if (Number.isInteger(before.cleaning) && s.ammo.cleaning === null) {
      const cleaner = PLAYER_IDS.find(id => before.players[id].role === 'cleaner') ?? null;
      event('cannon_cleaned', cleaner);
    }
    if (s.fuse.end && !before.fuse.end) event('fuse_lit', s.fuse.owner, { fireTick: s.fuse.end });
    if (fired) {
      const gunner = before.fuse.end ? PLAYER_IDS.find(id => before.players[id].role === 'gunner') ?? null : null;
      event('cannon_fired', gunner, { ...chamber, fuseOwner: before.fuse.owner, source: before.fuse.end ? 'fuse' : 'royal_mischief', angle: round(s.cannonRecoil.angle, 3) });
      shots++;
      if (chamber.ammoSource === 'delivered') deliveredShots++;
      if (chamber.playerDeliveryConfirmed) playerDeliveredShots++;
      if (chamber.recoveredDeliveryConfirmed) recoveredPlayerShots++;
      chamber = { ammoSource: 'unknown' };
    }

    const delivered = [];
    let accountedLoss = 0;
    for (const id of PLAYER_IDS) {
      const old = before.players[id];
      const p = s.players[id];
      const role = stationAt(p, s);
      const floor = playerFloor(p);
      const newNote = p.noteUntil !== old.noteUntil ? p.note : '';
      if (floor !== old.floor) event('floor_changed', id, { from: old.floor, to: floor, carrying: p.ball });
      if (role !== old.role && id === PLAYER) event('role_changed', id, { from: old.role, to: role });
      if (p.ball && !old.ball) event('ammo_picked_up', id, { floor });
      if (p.ball && p.slipUntil && !old.slip) {
        const source = impacts > 1 ? 'multiple_impacts' : fired && impacts === 1 ? 'cannon' : impacts === 1 ? 'other_impact' : 'unknown';
        const start = event('catch_started', id, { source, impactNote, floor, deadlineTick: p.slipUntil });
        pendingCatch.set(id, start);
        if (id === PLAYER && source === 'cannon') playerCannonCatches++;
      }
      if (p.ball && old.slip && !p.slipUntil) {
        const started = pendingCatch.get(id);
        const recovery = event('catch_succeeded', id, { floor, catchEventId: started?.id, source: started?.source });
        if (started?.source === 'cannon') {
          recoveredCarry.set(id, recovery.id);
          if (id === PLAYER) playerCannonCatchSuccesses++;
        }
        pendingCatch.delete(id);
      }
      if (!p.ball && old.ball && newNote === '적재칸에 포탄을 내려놓았습니다') delivered.push(id);
      if (!p.ball && old.slip && !delivered.includes(id)) {
        event('ammo_lost', id, { reason: s.tick >= old.slip ? 'catch_timeout' : 'catch_timing_miss', note: newNote, floor, catchEventId: pendingCatch.get(id)?.id });
        pendingCatch.delete(id);
        recoveredCarry.delete(id);
        accountedLoss++;
      }
      if (newNote === '타이밍 실패') event('timing_missed', id, { role: old.role, note: newNote });
    }

    const reservations = [];
    let returned = 0;
    for (const royal of s.royals) {
      const wasPlaying = before.royalShells.has(royal.id);
      if (royal.shellPlayUntil) {
        if (!wasPlaying) reservations.push(royal.id);
      } else if (wasPlaying) {
        const held = royalShells.get(royal.id);
        royalShells.delete(royal.id);
        if (royal.state === 'carried') {
          returned++;
          rack.push(held?.shell ?? { ammoSource: 'unknown' });
          rackOrderUncertain ||= held?.shell.provenance !== 'unambiguous';
          if (held?.playerRound) possiblePlayerRounds++;
          if (held?.recoveredRound) possibleRecoveredRounds++;
        } else {
          event('ammo_lost', royal.id, { ...held?.shell, controller: 'royal', reason: 'royal_shell_drop' });
          accountedLoss++;
        }
      }
    }

    // Bot loading happens before automatic deliveries and royal reservations.
    const rackSpent = before.rack + delivered.length + returned - reservations.length - s.ammo.rack;
    if (rackSpent > 0) {
      const loaded = s.ammo.loaded && !before.loaded;
      const loader = PLAYER_IDS.find(id => {
        const p = s.players[id];
        return p.noteUntil !== before.players[id].noteUntil && p.note?.startsWith(loaded ? '장전 완료' : '장전 실패');
      }) ?? null;
      const spent = takeRackShell();
      // Keep the maximum possible player stock in the rack and attribute failed loads to it first.
      // A successful load that cannot fit that maximum in the remaining rack proves cumulative use.
      const playerDeliveryConfirmed = loaded && possiblePlayerRounds > rack.length;
      const recoveredDeliveryConfirmed = loaded && possibleRecoveredRounds > rack.length;
      possiblePlayerRounds = Math.min(rack.length, loaded ? possiblePlayerRounds : Math.max(0, possiblePlayerRounds - 1));
      possibleRecoveredRounds = Math.min(rack.length, loaded ? possibleRecoveredRounds : Math.max(0, possibleRecoveredRounds - 1));
      if (loaded) {
        chamber = { ...spent, playerDeliveryConfirmed, recoveredDeliveryConfirmed };
        event('ammo_loaded', loader, chamber);
      } else {
        event('ammo_lost', loader, { ...spent, reason: 'loading_timing_miss' });
        accountedLoss++;
      }
    }
    for (const id of delivered) {
      const recoveryEventId = recoveredCarry.get(id);
      const delivery = event('ammo_delivered', id, { floor: DECK_FLOOR, recoveryEventId });
      recoveredCarry.delete(id);
      rack.push({ ammoSource: 'delivered', deliveryActor: id, deliveryEventId: delivery.id });
      if (id === PLAYER) {
        possiblePlayerRounds++;
        if (recoveryEventId) possibleRecoveredRounds++;
      }
    }
    for (const id of reservations) {
      const playerRound = rack.length > 0 && possiblePlayerRounds === rack.length;
      const recoveredRound = rack.length > 0 && possibleRecoveredRounds === rack.length;
      royalShells.set(id, { shell: takeRackShell(), playerRound, recoveredRound });
      possiblePlayerRounds = Math.max(0, possiblePlayerRounds - 1);
      possibleRecoveredRounds = Math.max(0, possibleRecoveredRounds - 1);
    }
    if (s.ammo.lost - before.lost > accountedLoss) event('ammo_lost', null, { reason: 'unattributed', count: s.ammo.lost - before.lost - accountedLoss });
    if (s.hp < before.hp) event('damage', null, { amount: before.hp - s.hp, note: impacts ? impactNote : null });
    if (s.kill > before.kill) event('enemy_defeated', null, { count: s.kill - before.kill });
  }

  function endReason() {
    if (recoveredPlayerShots > 0) return 'objective_complete';
    if (s.mode !== 'run') return s.mode;
    return s.tick >= maxTicks ? 'tick_limit' : null;
  }

  function status() {
    const reason = endReason();
    return reason === 'objective_complete' ? 'completed' : reason ? 'ended' : 'running';
  }

  function observe() {
    const p = s.players[PLAYER];
    const view = playerView(s, PLAYER);
    const camera = visibleScene(s, p);
    return {
      tick: s.tick,
      seconds: round(s.tick / HZ, 3),
      status: status(),
      endReason: endReason(),
      mode: s.mode,
      distance: round(s.distance),
      hp: s.hp,
      floor: playerFloor(p),
      role: view.role,
      position: { x: round(p.x), y: round(p.y) },
      carrying: !!p.ball,
      work: view.work,
      timing: view.timing,
      notice: { self: p.noteUntil > s.tick ? p.note : null, team: s.eventUntil > s.tick ? s.event : null },
      team: camera.team,
      waypoints: camera.waypoints,
      ...(view.seat === 'gunner' ? { aim: { angleDegrees: round(firingAngle(s, s.fuse.end ? s.gunAngle : aimAngle(p)) * 180 / Math.PI), targets: camera.targets } } : {}),
      inputs: { stick: view.role === 'catch' ? 'idle' : !view.seat ? 'move' : ['gunner', 'driver'].includes(view.seat) ? 'aim' : 'idle', leave: !!view.seat },
    };
  }

  function act(input) {
    if (status() !== 'running') return { accepted: false, reason: '이 플레이는 종료됐습니다.', observation: observe() };
    const invalid = actionError(input);
    if (invalid) return { accepted: false, reason: invalid, observation: observe() };

    const { kind, ticks, x, y } = input;
    const action = { kind, ticks, x, y, target: kind === 'approach' ? input.target : null };
    const p = s.players[PLAYER];
    const first = observe();
    const initialView = playerView(s, PLAYER);
    const destination = kind === 'approach' && first.waypoints.some(item => item.id === action.target) ? marker(action.target) : null;
    let accepted = true;
    let reason = null;
    if (kind === 'press') {
      const before = capture(s);
      setInput(s, PLAYER, { mx: 0, my: 0, ax: p.ax, ay: p.ay });
      accepted = !!initialView.work?.enabled && operate(s, PLAYER);
      if (!accepted) reason = initialView.work?.hint || '현재 누를 수 있는 행동이 없습니다.';
      track(before);
    } else if (kind === 'leave') {
      const before = capture(s);
      accepted = leaveStation(s, PLAYER);
      if (!accepted) reason = '이탈할 작업대가 없습니다.';
      track(before);
    } else if (kind === 'aim' && first.inputs.stick !== 'aim') {
      accepted = false;
      reason = '조준 스틱을 사용할 수 있는 자리가 아닙니다.';
    } else if (['move', 'approach'].includes(kind) && first.inputs.stick !== 'move') {
      accepted = false;
      reason = first.role === 'catch' ? '붙잡기 중에는 이동할 수 없습니다.' : '이동하려면 작업대에서 먼저 이탈하세요.';
    } else if (kind === 'approach' && !destination) {
      accepted = false;
      reason = '현재 보이는 지점을 선택하세요.';
    }

    if (!accepted) event('input_blocked', PLAYER, { action: kind, reason, target: action.target, role: first.role });
    let stuck = 0;
    for (let i = 0; accepted && i < ticks && status() === 'running'; i++) {
      const before = capture(s);
      const oldX = p.x;
      const oldY = p.y;
      let [mx, my] = kind === 'move' ? [x, y] : [0, 0];
      let [ax, ay] = [p.ax, p.ay];
      if (destination) {
        const dx = destination.x - oldX;
        const dy = destination.y - oldY;
        const len = Math.hypot(dx, dy);
        if (len <= 2) {
          reason = action.target === 'stairs' ? '계단에 도착했습니다. 층을 바꾸려면 계단 밖으로 나갔다가 다시 밟으세요.' : '표시된 지점에 도착했습니다.';
          break;
        }
        [mx, my] = [dx / Math.max(APPROACH_SLOWDOWN_RADIUS, len), dy / Math.max(APPROACH_SLOWDOWN_RADIUS, len)];
      }
      if (kind === 'aim') {
        if (initialView.seat === 'driver') [ax, ay] = [ax + x * 1.05 / HZ, ay + y * 1.05 / HZ];
        if (initialView.seat === 'gunner' && Math.hypot(x, y) > 0.08) [ax, ay] = [x, y];
      }
      setInput(s, PLAYER, { mx, my, ax, ay, aimRole: initialView.seat });
      step(s);
      track(before);

      const current = playerView(s, PLAYER);
      if (current.role !== first.role || playerFloor(p) !== first.floor) break;
      if (kind === 'wait' && current.timing?.ready && !first.timing?.ready) break;
      if (kind === 'aim' && initialView.seat === 'gunner' && before.fuse.end && !s.fuse.end) break;
      if (Math.hypot(mx, my) > 0.1 && Math.hypot(p.x - oldX, p.y - oldY) <= 0.005) {
        if (++stuck < 6) continue;
        reason = '이 방향으로 더 움직이지 않았습니다.';
        event('movement_blocked', PLAYER, { target: action.target, reason });
        break;
      }
      stuck = 0;
    }

    return { accepted, reason, observation: observe() };
  }

  function result() {
    return {
      seed, maxTicks, scenario,
      tick: s.tick,
      seconds: round(s.tick / HZ, 3),
      status: status(),
      endReason: endReason(),
      mode: s.mode,
      shots,
      deliveredShots,
      playerDeliveredShots,
      playerCannonCatches,
      playerCannonCatchSuccesses,
      recoveredPlayerShots,
      hp: s.hp,
      distance: round(s.distance),
      ammo: { ...s.ammo },
      initialAmmo: { ...initialAmmo },
    };
  }

  return { observe, act, result, events };
}
