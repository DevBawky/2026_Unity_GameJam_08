import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { copyFile, mkdir, readFile, readdir, unlink, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const directory = dirname(fileURLToPath(import.meta.url));
const repository = join(directory, '..', '..', '..');
const reviewedFile = join(directory, 'status.json');
const baselineDirectory = join(directory, '..', 'web-non-pixel-2026-11-19');
const catalog = JSON.parse(await readFile(reviewedFile, 'utf8'));
const sourceFile = reference => reference.split('#')[0];
function watchedPaths(item) {
  const paths = new Set([...item.sources.map(sourceFile), ...(item.watch ?? [])]);
  if (paths.has('goblin-getaway/public/pixel-art.mjs')) paths.add('goblin-getaway/public/pixel-art/');
  return [...paths].sort();
}
const fingerprints = new Map();

async function fingerprint(path) {
  if (!/^goblin-getaway\/(?:[\w.-]+\/)*[\w.-]+\/?$/.test(path) || path.split('/').includes('..')) {
    throw new Error('Invalid watched source: ' + path);
  }
  if (!fingerprints.has(path)) {
    const promise = path.endsWith('/')
      ? readdir(join(repository, path), { withFileTypes: true })
        .then(entries => Promise.all(entries.filter(entry => entry.isFile() || entry.isDirectory()).map(async entry => {
          const name = entry.name + (entry.isDirectory() ? '/' : '');
          return [name, await fingerprint(path + name)];
        })))
        .then(entries => JSON.stringify(entries.sort(([left], [right]) => left.localeCompare(right, 'en'))))
      : readFile(join(repository, path));
    fingerprints.set(path, promise
      .then(body => createHash('sha256').update(body).digest('hex'))
      .catch(error => {
        if (error.code === 'ENOENT') return 'missing';
        throw error;
      }));
  }
  return fingerprints.get(path);
}

async function fingerprintItem(item) {
  const inputs = await Promise.all(watchedPaths(item).map(async path => [path, await fingerprint(path)]));
  return createHash('sha256').update(JSON.stringify(inputs)).digest('hex');
}

const results = await Promise.all(catalog.items.map(async item => ({ item, digest: await fingerprintItem(item) })));
const mode = process.argv[2];
if (mode === '--check') {
  console.log(JSON.stringify({
    checkedAt: catalog.checkedAt,
    revision: catalog.revision,
    changed: results.filter(({ item, digest }) => item.sourceDigest !== digest).map(({ item }) => ({
      id: item.id,
      title: item.title,
      paths: watchedPaths(item),
    })),
  }, null, 2));
} else if (mode === '--record') {
  const ids = process.argv.slice(3);
  if (!ids.length || (ids.includes('all') && ids.length !== 1) || ids.some(id => id !== 'all' && !results.some(({ item }) => item.id === id))) {
    throw new Error('Specify the IDs that were reviewed, or all.');
  }
  for (const { item, digest } of results) {
    if (ids.includes('all') || ids.includes(item.id)) item.sourceDigest = digest;
  }
  const git = (...args) => execFileSync('git', args, { cwd: repository, encoding: 'utf8' }).trim();
  const branch = git('rev-parse', '--abbrev-ref', 'HEAD');
  const commit = git('rev-parse', '--short', 'HEAD');
  const sourcePaths = [...new Set(results.flatMap(({ item }) => watchedPaths(item)))];
  const changed = git('status', '--porcelain', '--', ...sourcePaths);
  catalog.revision = `${branch} · ${commit}${changed ? ' + 로컬 변경분' : ''}`;
  catalog.checkedAt = new Date().toISOString();
  await writeFile(reviewedFile, JSON.stringify(catalog, null, 2) + '\n');
  console.log('Recorded reviews for ' + ids.join(', '));
} else if (mode === undefined) {
  const dist = join(directory, 'dist');
  const screenshots = join(dist, 'screenshots');
  await mkdir(screenshots, { recursive: true });
  const report = await readFile(join(baselineDirectory, 'README.md'), 'utf8');
  const published = {
    ...catalog,
    items: results.map(({ item, digest }) => {
      const { sourceDigest, watch, ...details } = item;
      if (sourceDigest === digest) return details;
      return {
        ...details,
        status: 'review',
        note: ['관련 소스가 변경됐습니다. 아래 적용·남은 내용은 마지막 검토 기준입니다.', details.note].filter(Boolean).join(' '),
      };
    }),
  };
  const archive = new Set(Array.from(report.matchAll(/\]\(screenshots\/([\w-]+\.png)\)/g), match => match[1]));
  const captures = new Set([...archive, catalog.hero, ...catalog.items.flatMap(item => item.screenshots)]);
  for (const name of captures) {
    if (!/^[\w-]+\.png$/.test(name)) throw new Error('Invalid screenshot name: ' + name);
    const evidence = archive.has(name) ? join(baselineDirectory, 'screenshots') : join(directory, 'evidence');
    await copyFile(join(evidence, name), join(screenshots, name));
  }
  for (const entry of await readdir(screenshots, { withFileTypes: true })) {
    if (entry.isFile() && /^[\w-]+\.png$/.test(entry.name) && !captures.has(entry.name)) {
      await unlink(join(screenshots, entry.name));
    }
  }
  const publicReport = report.replace(/\[([^\]]+)\]\(\.\.\/\.\.\/\.\.\/(goblin-getaway\/[^)]+)\)/g, '$1 (`$2`)');
  await Promise.all([
    ...['index.html', 'styles.css', 'dashboard.js', 'vercel.json'].map(name => copyFile(join(directory, 'site', name), join(dist, name))),
    writeFile(join(dist, 'status.json'), JSON.stringify(published, null, 2) + '\n'),
    writeFile(join(dist, 'report.md'), publicReport),
  ]);
  console.log(`Built ${published.items.length} categories and ${captures.size} screenshots in ${dist}`);
} else {
  throw new Error('Expected no arguments, --check, or --record <id ... | all>.');
}
