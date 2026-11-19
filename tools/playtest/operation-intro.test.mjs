import assert from 'node:assert/strict';
import test from 'node:test';
import {stageSpec} from '../../goblin-getaway/public/campaign.mjs';
import {operationIntroFrame, operationIntroTypingTicks, OPERATION_INTRO_TICKS} from '../../goblin-getaway/public/operation-intro.mjs';
import {addRackShell, advance, advanceOperationIntro, begin, draftGaitDistance, ensureDeck, HZ, initial, isOperationIntroActive,
  leaveStation, nextStage, operate, operationElapsedTicks, restartStage, setInput, step, STATIONS} from '../../goblin-getaway/public/sim.mjs';

test('operation opening holds all four typed lines until their own advance press', () => {
  const state = initial();
  begin(state, {operationIntro:true});
  for (let lineIndex = 0; lineIndex < 4; lineIndex++) {
    assert.equal(state.operationIntroLine, lineIndex);
    assert.equal(operationIntroFrame(state.operationIntroTick, lineIndex).visibleCharacters, 1);
    assert.equal(advanceOperationIntro(state, lineIndex), false);

    advance(state, operationIntroTypingTicks(lineIndex) - 1);
    assert.equal(operationIntroFrame(state.operationIntroTick, lineIndex).canAdvance, false);
    step(state);
    const full = operationIntroFrame(state.operationIntroTick, lineIndex);
    assert.equal(full.visibleCharacters, full.text.length);
    assert.equal(full.canAdvance, true);
    assert.equal(full.finalLine, lineIndex === 3);

    const elapsed = operationElapsedTicks(state);
    const road = state.road;
    advance(state, 15 * HZ);
    assert.deepEqual(operationIntroFrame(state.operationIntroTick, state.operationIntroLine), full);
    assert.equal(operationElapsedTicks(state), elapsed + 15 * HZ);
    assert.ok(state.road > road);
    assert.equal(state.tick, 0);
    assert.equal(state.distance, 0);
    assert.equal(state.wave, 0);
    assert.equal(advanceOperationIntro(state, lineIndex), true);
    assert.equal(advanceOperationIntro(state, lineIndex), false);
  }
  assert.equal(isOperationIntroActive(state), false);
  assert.equal(operationIntroFrame(state.operationIntroTick, state.operationIntroLine), null);
});

test('only the road and opening clock advance during the operation opening', () => {
  const state = initial();
  Object.assign(state.players.p1, STATIONS.driver, {station:'driver'});
  begin(state, {operationIntro:true});
  const before = structuredClone(state);
  setInput(state, 'p1', {mx:0, my:1, ax:-1, ay:0});
  assert.equal(operate(state, 'p1'), false);
  assert.equal(leaveStation(state, 'p1'), false);
  assert.equal(addRackShell(state), false);

  const duration = Math.max(state.nextCatapult, stageSpec(state.chapter, state.stage).waves[0].at * HZ) + 1;
  advance(state, duration);
  assert.ok(state.road > before.road);
  assert.ok(draftGaitDistance(state) > 0);
  assert.equal(operationElapsedTicks(state), duration);
  assert.equal(state.operationIntroTick, operationIntroTypingTicks(0));
  assert.deepEqual({...state, road:before.road, operationIntroTick:before.operationIntroTick, operationIntroElapsed:0}, before);

  state.mode = 'paused';
  step(state);
  assert.equal(operationElapsedTicks(state), duration);
  assert.equal(advanceOperationIntro(state, 0), false);
  state.mode = 'run';
  const restored = JSON.parse(JSON.stringify(state));
  for (let line = 0; line < 4; line++) {
    advance(restored, operationIntroTypingTicks(line));
    assert.equal(advanceOperationIntro(restored, line), true);
  }
  assert.equal(isOperationIntroActive(restored), false);
  assert.equal(restored.tick, 0);
  assert.equal(restored.distance, 0);
  assert.equal(restored.wave, 0);
  assert.equal(restored.goal - restored.distance, before.goal);

  const elapsed = operationElapsedTicks(restored);
  const road = restored.road;
  const gaitDistance = draftGaitDistance(restored);
  step(restored);
  assert.equal(operationElapsedTicks(restored), elapsed + 1);
  assert.equal(restored.tick, 1);
  assert.ok(restored.road > road);
  assert.ok(restored.distance > 0);
  assert.ok(draftGaitDistance(restored) > gaitDistance);
  const firstWaveTick = stageSpec(restored.chapter, restored.stage).waves[0].at * HZ;
  advance(restored, firstWaveTick - 2);
  assert.equal(restored.wave, 0);
  step(restored);
  assert.equal(restored.tick, firstWaveTick);
  assert.equal(restored.wave, 1);
});

test('older snapshots migrate their visible line without rewinding the clock or reopening completed introductions', () => {
  const old = initial();
  delete old.operationIntroLine;
  delete old.operationIntroElapsed;
  old.operationIntroTick = OPERATION_INTRO_TICKS - 20;
  old.mode = 'run';
  const expected = operationIntroFrame(old.operationIntroTick);
  const elapsed = operationElapsedTicks(old);
  const finished = {...old, operationIntroTick:OPERATION_INTRO_TICKS, tick:42};

  ensureDeck(old);
  assert.equal(operationElapsedTicks(old), elapsed);
  assert.equal(old.operationIntroLine, expected.lineIndex);
  assert.equal(operationIntroFrame(old.operationIntroTick, old.operationIntroLine).visibleCharacters, expected.visibleCharacters);
  advance(old, 15 * HZ);
  assert.equal(isOperationIntroActive(old), true);
  assert.equal(advanceOperationIntro(old, expected.lineIndex), true);

  ensureDeck(finished);
  assert.equal(isOperationIntroActive(finished), false);
  assert.equal(operationElapsedTicks(finished), OPERATION_INTRO_TICKS + 42);
});

test('test runs, retries and stage departures start without the operation opening', () => {
  const testRun = initial();
  begin(testRun, {testMode:true, operationIntro:true});
  step(testRun);
  assert.equal(testRun.operationIntroTick, null);
  assert.equal(testRun.tick, 1);

  const campaign = initial();
  begin(campaign, {operationIntro:true});
  advance(campaign, operationIntroTypingTicks(0));
  campaign.mode = 'lost';
  const retry = restartStage(campaign);
  assert.equal(retry.operationIntroTick, null);
  assert.equal(retry.road, 0);
  step(retry);
  assert.equal(retry.tick, 1);

  campaign.mode = 'clear';
  campaign.intermission = {phase:'shop'};
  const departure = nextStage(campaign);
  assert.equal(departure.stage, campaign.stage + 1);
  assert.equal(departure.operationIntroTick, null);
  assert.equal(departure.road, 0);
});
