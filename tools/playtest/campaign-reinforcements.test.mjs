import assert from 'node:assert/strict';
import test from 'node:test';
import {stageSpec} from '../../goblin-getaway/public/campaign.mjs';
import {begin, HZ, initial, step} from '../../goblin-getaway/public/sim.mjs';

const waves = stageSpec().waves;
const finalWaveTick = waves.at(-1).at * HZ;
const reinforcementInterval = 12 * HZ;

function campaignAtFinalWave(testMode = false){
  const state = initial();
  begin(state, {testMode});
  state.nextRock = state.nextCatapult = Number.MAX_SAFE_INTEGER;
  state.wave = waves.length - 1;
  stepAt(state, finalWaveTick);
  return state;
}

function stepAt(state,tick){
  state.tick = tick - 1;
  step(state);
}

test('campaign pursuit continues after the final wave while respecting the active-enemy cap', () => {
  const state = campaignAtFinalWave();
  const authored = waves.at(-1).groups.reduce((sum,group) => sum + group.count,0);
  assert.equal(state.enemies.length, authored);
  assert.equal(state.wave, waves.length);

  stepAt(state, finalWaveTick + reinforcementInterval);
  assert.equal(state.enemies.length, authored);

  state.enemies.length = 9;
  stepAt(state, finalWaveTick + 2 * reinforcementInterval);
  assert.equal(state.enemies.length, 12);
  assert.equal(state.wave, waves.length);

  const resumed = JSON.parse(JSON.stringify(state));
  resumed.enemies.length = 0;
  resumed.distance = resumed.goal - 5;
  stepAt(resumed, finalWaveTick + 3 * reinforcementInterval - 1);
  assert.equal(resumed.enemies.length, 0);
  step(resumed);
  assert.equal(resumed.mode, 'run');
  assert.equal(resumed.enemies.length, 6);
  assert.equal(resumed.enemies[0].sfxWave, waves.length + 3);

  resumed.enemies.length = 0;
  resumed.distance = resumed.goal - .01;
  stepAt(resumed, finalWaveTick + 4 * reinforcementInterval);
  assert.equal(resumed.mode, 'clear');
  assert.equal(resumed.distance, resumed.goal);
  assert.equal(resumed.enemies.length, 0);
});

test('test mode continues repeating the authored waves', () => {
  const state = campaignAtFinalWave(true);
  state.enemies.length = 0;
  const lastGap = waves.at(-1).at - waves.at(-2).at;
  stepAt(state, finalWaveTick + lastGap * HZ);
  assert.equal(state.wave, waves.length + 1);
  assert.deepEqual(state.enemies.map(({profile}) => profile), Array(waves[0].groups[0].count).fill('cavalry'));
});
