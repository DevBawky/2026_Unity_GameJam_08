import assert from 'node:assert/strict';
import {mkdtemp, readdir, readFile, rm, writeFile} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import test from 'node:test';
import {main} from './cli.mjs';

const wait = {kind: 'wait', x: 0, y: 0, ticks: 30, target: null};
const longWait = {...wait, ticks: 90};
const press = {...wait, kind: 'press', ticks: 1};
const approach = target => ({...longWait, kind: 'approach', target});
const untilCatch = [approach('stairs'), approach('supply'), longWait, press,
  approach('stairs'), approach('rack'), longWait];

function apiAction(action) {
  return Response.json({
    status: 'completed',
    output: [{type: 'function_call', name: 'play_input', arguments: JSON.stringify(action)}],
  });
}

async function testWorkspace(t) {
  const directory = await mkdtemp(join(tmpdir(), 'goblin-playtest-'));
  t.after(() => rm(directory, {force: true, recursive: true}));
  const output = [];
  const run = (args, options = {}) => main(args, {
    directory, apiKey: 'test-key', onStart: () => {}, out: message => output.push(message), ...options,
  });
  const savedRuns = async () => Promise.all((await readdir(directory)).map(async id =>
    JSON.parse(await readFile(join(directory, id, 'run.json'), 'utf8'))));
  return {run, output, savedRuns, directory};
}

test('runs with Responses input, reads the evidence, replays without API, and reruns with saved settings', async t => {
  const {run, output, savedRuns, directory} = await testWorkspace(t);
  const requests = [];
  const request = async (url, options) => {
    assert.equal(url, 'https://api.openai.com/v1/responses');
    assert.equal(options.headers.Authorization, 'Bearer test-key');
    const body = JSON.parse(options.body);
    assert.equal(body.tool_choice.name, 'play_input');
    assert.equal(body.tools[0].strict, true);
    assert.equal(body.store, false);
    assert.equal(JSON.parse(body.input).observation.tick, 0);
    requests.push(body);
    return apiAction(wait);
  };
  assert.equal(await run(['run', '--seed', '42', '--max-ticks', '24'], {request}), 0);
  const [first] = await savedRuns();
  assert.equal(first.metadata.driver, 'mock');
  assert.equal(first.metadata.config.seed, 42);
  assert.equal(first.decisions.length, 1);
  assert.equal(first.final.tick, 24);
  assert.equal(await run(['latest'], {apiKey: ''}), 0);
  assert.match(output.at(-1), new RegExp(first.metadata.id));
  assert.equal(await run(['show', first.metadata.id], {apiKey: ''}), 0);
  assert.match(output.at(-1), new RegExp(first.metadata.id));
  assert.equal(await run(['replay'], {apiKey: ''}), 0);
  assert.match(output.at(-1), /재현 일치/);
  assert.equal(requests.length, 1);
  assert.equal(await run(['rerun', first.metadata.id], {request}), 0);
  const second = (await savedRuns()).find(result => result.metadata.parentId === first.metadata.id);
  assert.ok(second);
  assert.deepEqual(second.metadata.config, first.metadata.config);
  assert.equal(requests.length, 2);

  first.decisions[0].afterTick++;
  await writeFile(join(directory, first.metadata.id, 'run.json'), JSON.stringify(first));
  await assert.rejects(run(['replay', first.metadata.id]), /첫|1번째 입력/);
});

test('records a partial result when OpenAI stops returning playable inputs', async t => {
  const {run, savedRuns, output} = await testWorkspace(t);
  let attempts = 0;
  const request = async () => ++attempts === 1 ? apiAction(wait) : new Response('test-secret', {status: 401});
  assert.equal(await run(['run'], {request}), 1);
  const [partial] = await savedRuns();
  assert.equal(partial.status, 'api_error');
  assert.equal(partial.calls, 2);
  assert.equal(partial.decisions.length, 1);
  assert.equal(partial.error, 'OpenAI API HTTP 401');
  assert.doesNotMatch(JSON.stringify([partial, output]), /test-secret|test-key/);
  assert.equal(await run(['latest'], {apiKey: ''}), 0);
  assert.match(output.at(-1), /OpenAI API HTTP 401/);
  assert.equal(await run(['replay', partial.metadata.id], {apiKey: ''}), 0);
});

test('replays a shell recovered from cannon recoil through delivery, loading, and its next shot', async t => {
  const {run, savedRuns, output} = await testWorkspace(t);
  const inputs = [...untilCatch, press, approach('rack')];
  let next = 0;
  const request = async () => apiAction(inputs[next++] ?? longWait);

  assert.equal(await run(['run'], {request}), 0);
  const [saved] = await savedRuns();
  const recoil = saved.events.find(event => event.type === 'catch_started' && event.actor === 'p1' && event.source === 'cannon');
  assert.equal(saved.status, 'completed');
  assert.ok(recoil);
  const recovery = saved.events.find(event => event.type === 'catch_succeeded' && event.catchEventId === recoil.id);
  assert.ok(recovery);
  const delivery = saved.events.find(event => event.type === 'ammo_delivered' && event.recoveryEventId === recovery.id);
  const reload = saved.events.find(event => event.type === 'ammo_loaded' && event.recoveredDeliveryConfirmed);
  const fired = saved.events.find(event => event.type === 'cannon_fired' && event.recoveredDeliveryConfirmed);
  assert.ok(delivery && reload && fired);
  assert.ok(recoil.tick < recovery.tick && recovery.tick < delivery.tick && delivery.tick < reload.tick && reload.tick < fired.tick);
  assert.equal(fired.provenance, 'fifo_assumed');
  assert.equal(fired.deliveryEventId, undefined);
  assert.match(output.at(-1), /시나리오 목표 달성/);
  assert.match(output.at(-1), /발사 반동으로 검증용 플레이어 p1/);
  assert.match(output.at(-1), /적재한 포탄이 최소 한 발 발사됐습니다/);
  assert.equal(await run(['replay', saved.metadata.id], {apiKey: ''}), 0);
  assert.match(output.at(-1), /재현 일치/);
});

test('the previous-run report names a missed recoil catch without claiming a recovered shot', async t => {
  const {run, savedRuns, output} = await testWorkspace(t);
  let next = 0;
  const request = async () => apiAction(untilCatch[next++] ?? longWait);
  assert.equal(await run(['run', '--max-ticks', '300'], {request}), 0);
  const [saved] = await savedRuns();
  assert.equal(saved.final.recoveredPlayerShots, 0);
  assert.ok(saved.events.some(event => event.actor === 'p1' && event.reason === 'catch_timeout'));
  assert.equal(await run(['latest'], {apiKey: ''}), 0);
  assert.match(output.at(-1), /붙잡기 시간 초과/);
});

test('missing credentials cannot save a fake playtest', async t => {
  const {run, savedRuns} = await testWorkspace(t);
  await assert.rejects(run(['run'], {apiKey: ''}), /OPENAI_API_KEY/);
  assert.deepEqual(await savedRuns(), []);
});
