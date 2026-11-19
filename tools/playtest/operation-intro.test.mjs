import assert from 'node:assert/strict';
import test from 'node:test';
import {stageSpec} from '../../goblin-getaway/public/campaign.mjs';
import {operationIntroFrame, OPERATION_INTRO_TICKS} from '../../goblin-getaway/public/operation-intro.mjs';
import {addRackShell, advance, begin, draftGaitDistance, HZ, initial, isOperationIntroActive,
  leaveStation, nextStage, operate, operationElapsedTicks, restartStage, setInput, step, STATIONS} from '../../goblin-getaway/public/sim.mjs';

test('operation opening types and holds four lines in sequence', () => {
  const lines = [];
  for (let tick = 0; tick < OPERATION_INTRO_TICKS; tick++) {
    const frame = operationIntroFrame(tick);
    (lines[frame.lineIndex] ??= []).push(frame);
  }
  assert.equal(lines.length, 4);
  for (const frames of lines) {
    const {text} = frames[0];
    assert.equal(frames[0].visibleCharacters, 1);
    assert.equal(frames.at(-1).visibleCharacters, text.length);
    assert.ok(frames.filter(frame => frame.visibleCharacters === text.length).length >= 1.8 * HZ);
    assert.ok(frames.every((frame, index) => index === 0 || frame.visibleCharacters >= frames[index - 1].visibleCharacters));
  }
  assert.equal(operationIntroFrame(null), null);
  assert.equal(operationIntroFrame(OPERATION_INTRO_TICKS), null);
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
  assert.equal(state.operationIntroTick, duration);
  assert.deepEqual({...state, road:before.road, operationIntroTick:before.operationIntroTick}, before);

  state.mode = 'paused';
  step(state);
  assert.equal(state.operationIntroTick, duration);
  state.mode = 'run';
  const restored = JSON.parse(JSON.stringify(state));
  advance(restored, OPERATION_INTRO_TICKS - duration);
  assert.equal(isOperationIntroActive(restored), false);
  assert.equal(restored.tick, 0);
  assert.equal(restored.distance, 0);
  assert.equal(restored.wave, 0);
  assert.equal(restored.goal - restored.distance, before.goal);

  const road = restored.road;
  step(restored);
  assert.equal(operationElapsedTicks(restored), OPERATION_INTRO_TICKS + 1);
  assert.equal(restored.tick, 1);
  assert.ok(restored.road > road);
  assert.ok(restored.distance > 0);
  const firstWaveTick = stageSpec(restored.chapter, restored.stage).waves[0].at * HZ;
  advance(restored, firstWaveTick - 2);
  assert.equal(restored.wave, 0);
  step(restored);
  assert.equal(restored.tick, firstWaveTick);
  assert.equal(restored.wave, 1);
});

test('test runs, retries and stage departures start without the operation opening', () => {
  const testRun = initial();
  begin(testRun, {testMode:true, operationIntro:true});
  step(testRun);
  assert.equal(testRun.operationIntroTick, null);
  assert.equal(testRun.tick, 1);

  const campaign = initial();
  begin(campaign, {operationIntro:true});
  advance(campaign, OPERATION_INTRO_TICKS);
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
