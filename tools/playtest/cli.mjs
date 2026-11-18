import {createHash, randomUUID} from 'node:crypto';
import {mkdir, readFile, readdir, rename, writeFile} from 'node:fs/promises';
import {resolve, join} from 'node:path';
import {fileURLToPath} from 'node:url';
import {parseArgs, isDeepStrictEqual} from 'node:util';
import {createGame} from './game.mjs';
import {DEFAULT_MODEL, openAIPlayer, PlayApiError} from './openai.mjs';
import {generateReport} from './report.mjs';

const project = fileURLToPath(new URL('../../', import.meta.url));
const records = join(project, '.playtest', 'runs');
const runIdPattern = /^\d{8}T\d{9}Z-[a-f0-9]{8}$/;
const schemaVersion = 1;

const defaultConfig = {
  scenario: 'cannon-recoil',
  seed: 718,
  model: DEFAULT_MODEL,
  maxCalls: 40,
  maxTicks: 2700,
  timeoutSeconds: 240,
};

const help = `《그건 님이!!!》 로컬 웹판 플레이테스터

node tools/playtest/cli.mjs run [--seed N] [--model NAME] [--max-calls N] [--max-ticks N] [--timeout-seconds N]
node tools/playtest/cli.mjs latest
node tools/playtest/cli.mjs show <id>
node tools/playtest/cli.mjs rerun [id]    같은 설정에서 OpenAI가 입력을 새로 선택합니다.
node tools/playtest/cli.mjs replay [id]   저장된 입력으로 판정과 로그를 재현합니다.

run과 rerun에는 OPENAI_API_KEY 환경변수가 필요합니다. 결과는 .playtest/runs/에 저장됩니다.`;

class UsageError extends Error {}

function integer(value, name, min, max) {
  if (value === undefined) return undefined;
  if (!/^\d+$/.test(value) || Number(value) < min || Number(value) > max) {
    throw new UsageError(`${name}에는 ${min}에서 ${max} 사이의 정수를 입력하세요.`);
  }
  return Number(value);
}

function configFrom(flags) {
  const model = flags.model ?? defaultConfig.model;
  if (!/^[\w:.-]{1,128}$/.test(model)) throw new UsageError('--model 값이 올바르지 않습니다.');
  return {
    ...defaultConfig,
    model,
    seed: integer(flags.seed, '--seed', 0, 0xffffffff) ?? defaultConfig.seed,
    maxCalls: integer(flags['max-calls'], '--max-calls', 1, 120) ?? defaultConfig.maxCalls,
    maxTicks: integer(flags['max-ticks'], '--max-ticks', 1, 5400) ?? defaultConfig.maxTicks,
    timeoutSeconds: integer(flags['timeout-seconds'], '--timeout-seconds', 1, 600) ?? defaultConfig.timeoutSeconds,
  };
}

async function sourceFingerprint() {
  const web = 'goblin-getaway/public';
  const modules = await readdir(join(project, web));
  const files = modules.filter(name => name.endsWith('.mjs')).sort().map(name => `${web}/${name}`);
  files.push('tools/playtest/game.mjs', 'tools/playtest/openai.mjs', 'tools/playtest/cli.mjs');
  const hash = createHash('sha256');
  for (const name of files) {
    const bytes = await readFile(join(project, name));
    hash.update(`${name}\0${bytes.length}\0`).update(bytes);
  }
  return hash.digest('hex');
}

async function saveRun(run, directory) {
  await mkdir(directory, {recursive: true});
  const id = run.metadata.id;
  const pending = join(directory, `.${id}.tmp`);
  await mkdir(pending);
  const report = generateReport(run);
  await writeFile(join(pending, 'run.json'), JSON.stringify(run, null, 2), 'utf8');
  await writeFile(join(pending, 'report.md'), report, 'utf8');
  await rename(pending, join(directory, id));
  return report;
}

async function loadRun(id, directory) {
  if (id === undefined) {
    const entries = await readdir(directory, {withFileTypes: true}).catch(error => {
      if (error.code !== 'ENOENT') throw error;
      return [];
    });
    id = entries.filter(entry => entry.isDirectory() && runIdPattern.test(entry.name)).map(entry => entry.name).sort().at(-1);
    if (id === undefined) throw new UsageError('저장된 플레이테스트가 없습니다.');
  }
  if (!runIdPattern.test(id)) throw new UsageError('실행 ID 형식이 올바르지 않습니다.');
  const json = await readFile(join(directory, id, 'run.json'), 'utf8').catch(error => {
    if (error.code !== 'ENOENT') throw error;
    throw new UsageError(`실행 기록 ${id}을 찾을 수 없습니다.`);
  });
  const run = JSON.parse(json);
  if (run.schemaVersion !== schemaVersion || run.metadata?.id !== id) {
    throw new UsageError('이 실행 기록의 형식을 지원하지 않습니다.');
  }
  return run;
}

async function play(config, decide, source, parentId, onStart, driver) {
  const createdAt = new Date().toISOString();
  const id = `${createdAt.replace(/[-:.]/g, '')}-${randomUUID().slice(0, 8)}`;
  const metadata = {id, createdAt, config, sourceFingerprint: source, parentId, driver};
  const game = createGame(config);
  const decisions = [];
  const interrupt = new AbortController();
  const stop = () => interrupt.abort();
  const deadline = AbortSignal.timeout(config.timeoutSeconds * 1000);
  const signal = AbortSignal.any([interrupt.signal, deadline]);
  let calls = 0;
  let error;
  let status = 'call_limit';

  process.on('SIGINT', stop);
  try {
    onStart(id);
    while (calls < config.maxCalls && game.result().status === 'running') {
      signal.throwIfAborted();
      const before = game.observe();
      const recentInputs = decisions.slice(-5).map(({afterTick, action, accepted, reason, after}) => ({
        tick: afterTick, action, accepted, reason, notice: after.notice,
      }));
      calls++;
      const action = await decide(before, recentInputs, signal);
      signal.throwIfAborted();
      const {accepted, reason, observation: after} = game.act(action);
      decisions.push({before, action, accepted, reason, afterTick: after.tick, after});
    }
    const finalStatus = game.result().status;
    if (finalStatus !== 'running') status = finalStatus;
  } catch (cause) {
    if (interrupt.signal.aborted) status = 'interrupted';
    else if (deadline.aborted) status = 'time_limit';
    else if (cause instanceof PlayApiError) {
      status = 'api_error';
      error = cause.message;
    } else {
      throw cause;
    }
  } finally {
    process.off('SIGINT', stop);
  }

  return {schemaVersion, metadata, status, calls, decisions, events: game.events, final: game.result(), error};
}

function serializable(value) {
  return JSON.parse(JSON.stringify(value));
}

function replay(run) {
  const game = createGame(run.metadata.config);
  for (const [index, saved] of run.decisions.entries()) {
    const before = game.observe();
    const {accepted, reason, observation: after} = game.act(saved.action);
    const decision = {before, action: saved.action, accepted, reason, afterTick: after.tick, after};
    if (!isDeepStrictEqual(serializable(decision), saved)) {
      throw new UsageError(`재현 결과가 ${index + 1}번째 입력에서 저장된 기록과 달랐습니다.`);
    }
  }
  if (!isDeepStrictEqual(serializable(game.events), run.events) ||
      !isDeepStrictEqual(serializable(game.result()), run.final)) {
    throw new UsageError('재현한 이벤트 또는 마지막 게임 상태가 저장된 기록과 달랐습니다.');
  }
  return `재현 일치 · ${run.metadata.id} · 입력 ${run.decisions.length}개 · 이벤트 ${run.events.length}개 · ${run.final.tick}틱`;
}

export async function main(argv, {directory = records, request = fetch, apiKey = process.env.OPENAI_API_KEY, out = console.log, onStart = id => console.error(`플레이테스트 시작 · ${id}`)} = {}) {
  const {values, positionals} = parseArgs({args: argv, allowPositionals: true, options: {
    help: {type: 'boolean', short: 'h'},
    seed: {type: 'string'},
    model: {type: 'string'},
    'max-calls': {type: 'string'},
    'max-ticks': {type: 'string'},
    'timeout-seconds': {type: 'string'},
  }});
  if (values.help || positionals.length === 0) {
    out(help);
    return 0;
  }
  const [command, id] = positionals;
  const invalidId = command === 'show' ? id === undefined : ['run', 'latest'].includes(command) && id !== undefined;
  if (!['run', 'latest', 'show', 'rerun', 'replay'].includes(command) || positionals.length > 2 || invalidId ||
      (command !== 'run' && Object.keys(values).length !== 0)) {
    throw new UsageError('명령과 옵션을 확인하세요. 사용법은 --help에 있습니다.');
  }

  const previous = command === 'run' ? null : await loadRun(id, directory);
  if (command === 'latest' || command === 'show') {
    out(await readFile(join(directory, previous.metadata.id, 'report.md'), 'utf8'));
    return 0;
  }

  const source = await sourceFingerprint();
  if (previous && previous.metadata.sourceFingerprint !== source) {
    throw new UsageError('저장 당시와 게임 또는 실행기 코드가 달라 같은 조건을 보장할 수 없습니다. 현재 코드로 새 플레이테스트를 시작할 수 있습니다.');
  }
  if (command === 'replay') {
    out(replay(previous));
    return 0;
  }

  const config = previous?.metadata.config ?? configFrom(values);
  if (!apiKey?.trim()) throw new UsageError('OPENAI_API_KEY 환경변수가 필요합니다. 키 값은 이 채팅에 입력하지 마세요.');
  const decide = openAIPlayer(apiKey.trim(), config.model, request);
  const run = await play(config, decide, source, previous?.metadata.id ?? null, onStart, request === fetch ? 'openai' : 'mock');
  out(await saveRun(run, directory));
  return ['interrupted', 'time_limit', 'api_error'].includes(run.status) ? 1 : 0;
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    process.exitCode = await main(process.argv.slice(2));
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}
