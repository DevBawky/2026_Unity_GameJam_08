const labels = {
  all: '전체',
  done: '완료',
  partial: '부분 적용',
  remaining: '남음',
  review: '재확인',
};

const screenshotLabels = {
  '01-ready.png': '출발 대기',
  '02-settings.png': '환경설정',
  '03-coop.png': '협동 플레이',
  '04-guide.png': '조작 가이드',
  '05-card-index.png': '정비 카드 도감',
  '06-emote-menu.png': '이모티콘 선택창',
  '07-emote-bubble.png': '이모티콘 말풍선',
  '08-road-and-route.png': '도로 바위와 작전 진행',
  '09-catapult-impact.png': '투석기 바위 착탄',
  '10-road-log.png': '도로의 쓰러진 통나무',
  '11-road-deer.png': '도로의 사슴',
  '12-cellar.png': '마차 지하층 전경',
  '13-cellar-timing.png': '포탄 보급 타이밍',
  '14-cellar-cook.png': '지하 조리대',
  '15-driver-aim.png': '마부 조준',
  '16-cannon-aim.png': '대포 조준',
  '17-cannon-blast.png': '대포 착탄',
  '18-royal-watch.png': '왕실 근접 상태',
  '19-defeat.png': '패배 결과',
  '20-report-to-king.png': '마왕 보고',
  '21-upgrade-shop.png': '팀 정비 상점',
  '22-catapult-warning.png': '투석기 착탄 경고',
  '23-status.png': '작전 현황',
  '24-approach-alert.png': '도로 장애물 접근 경고',
  '25-cannon-burst.png': '대포 발사',
  '26-local-split.png': '로컬 2인 분할 화면',
};

const date = new Intl.DateTimeFormat('ko-KR', {
  timeZone: 'Asia/Seoul',
  dateStyle: 'medium',
});
const dateTime = new Intl.DateTimeFormat('ko-KR', {
  timeZone: 'Asia/Seoul',
  dateStyle: 'medium',
  timeStyle: 'short',
});

const checklist = document.querySelector('#checklist');
const filters = document.querySelector('#filters');
const itemList = document.querySelector('#item-list');
const resultCount = document.querySelector('#result-count');
const completionSummary = document.querySelector('#completion-summary');
const completionCount = document.querySelector('#completion-count');
const completionHint = document.querySelector('#completion-hint');
const loadState = document.querySelector('#load-state');
const loadMessage = document.querySelector('#load-message');
const retry = document.querySelector('#retry');

let activeFilter = 'all';
let issues = [];

function element(tag, attributes = {}, text) {
  const node = document.createElement(tag);
  for (const [name, value] of Object.entries(attributes)) node.setAttribute(name, value);
  if (text != null) node.textContent = text;
  return node;
}

function makeScreenshot(file, { hero = false } = {}) {
  if (typeof file !== 'string' || !/^[\p{L}\p{N}_. -]+\.(png|jpe?g|webp|avif)$/iu.test(file)) return null;

  const screenshotPath = './screenshots/' + encodeURIComponent(file);
  const title = screenshotLabels[file] ?? file.replace(/\.\w+$/, '').replaceAll('-', ' ');
  const link = element('a', { href: screenshotPath, 'aria-label': title + ' 원본 이미지' });
  const image = element('img', {
    src: screenshotPath,
    alt: title,
    decoding: 'async',
    loading: hero ? 'eager' : 'lazy',
  });
  image.addEventListener('error', () => image.remove(), { once: true });
  if (hero) link.append(image);
  else {
    const figure = element('figure');
    figure.append(image, element('figcaption', {}, title));
    link.append(figure);
  }
  return link;
}

function addFact(facts, label, value) {
  if (!value) return;
  const group = element('div');
  const description = element('dd');
  description.append(value);
  group.append(element('dt', {}, label), description);
  facts.append(group);
}

function updateCompletionCount() {
  const checked = itemList.querySelectorAll('.issue-check input:checked').length;
  completionCount.textContent = `완료 체크 ${checked} / ${issues.length}`;
}

function makeCompletionCheck(item, status) {
  const label = element('label', { class: 'issue-check' });
  const input = element('input', { type: 'checkbox', 'aria-label': item.title + ' 작업 완료' });
  const key = 'goblin-art-status:complete:' + item.id;
  input.checked = status === 'done';
  try {
    const saved = localStorage.getItem(key);
    if (saved === 'true' || saved === 'false') input.checked = saved === 'true';
  } catch {
    completionHint.textContent = '현재 페이지가 열려 있는 동안 체크 상태가 유지됩니다.';
  }
  input.addEventListener('change', () => {
    try {
      localStorage.setItem(key, String(input.checked));
    } catch {
      completionHint.textContent = '현재 페이지가 열려 있는 동안 체크 상태가 유지됩니다.';
    }
    updateCompletionCount();
  });
  label.append(input);
  return label;
}

function makeIssue(item) {
  const status = Object.hasOwn(labels, item.status) && item.status !== 'all' ? item.status : 'review';
  const row = element('article', { class: 'issue', 'data-status': status });
  const details = element('details', { class: 'issue-details' });
  const summary = element('summary');
  summary.append(
    element('span', { class: 'issue-title' }, item.title),
    element('span', { class: 'badge state-' + status }, labels[status]),
  );
  const body = element('div', { class: 'issue-body' });
  const facts = element('dl', { class: 'issue-facts' });
  addFact(facts, '적용', item.applied);
  addFact(facts, '남은 작업', item.remaining);
  addFact(facts, '점검 메모', item.note);

  if (item.sources?.length) {
    const sources = element('ul', { class: 'sources' });
    for (const reference of item.sources) {
      const line = element('li');
      line.append(element('code', {}, reference));
      sources.append(line);
    }
    addFact(facts, '소스 위치', sources);
  }
  body.append(facts);

  const gallery = element('div', { class: 'gallery', 'aria-label': '근거 스크린샷' });
  for (const file of item.screenshots ?? []) {
    const screenshot = makeScreenshot(file);
    if (screenshot) gallery.append(screenshot);
  }
  if (gallery.childElementCount) body.append(gallery);
  details.append(summary, body);
  row.append(makeCompletionCheck(item, status), details);
  return row;
}

function updateFilter() {
  let visible = 0;
  for (const issue of issues) {
    issue.hidden = activeFilter !== 'all' && issue.dataset.status !== activeFilter;
    if (!issue.hidden) visible += 1;
  }
  for (const button of filters.querySelectorAll('button')) {
    button.setAttribute('aria-pressed', String(button.dataset.filter === activeFilter));
  }
  itemList.querySelector('.empty-state').hidden = visible > 0;
  resultCount.textContent = activeFilter === 'all'
    ? `전체 ${issues.length}개 항목`
    : `${labels[activeFilter]} ${visible}개 항목`;
}

function renderChecklist(items) {
  const empty = element('p', { class: 'empty-state', hidden: '' }, '선택한 상태의 항목이 없습니다.');
  issues = items.map(makeIssue);
  itemList.replaceChildren(...issues, empty);

  const counts = { all: issues.length, done: 0, partial: 0, remaining: 0, review: 0 };
  for (const issue of issues) counts[issue.dataset.status] += 1;
  const buttons = Object.entries(labels).map(([status, label]) => {
    const button = element('button', {
      type: 'button',
      class: status === 'all' ? 'filter' : 'filter state-' + status,
      'data-filter': status,
      'aria-controls': 'item-list',
      'aria-label': label + ' ' + counts[status] + '개',
    });
    button.append(
      element('span', { class: 'filter-label' }, label),
      element('strong', {}, counts[status]),
    );
    return button;
  });
  filters.replaceChildren(...buttons);
  filters.hidden = false;
  updateFilter();
  updateCompletionCount();
  completionSummary.hidden = false;
}

function renderSnapshot(data) {
  const time = document.querySelector('#checked-at');
  time.dateTime = data.checkedAt;
  time.textContent = dateTime.format(new Date(data.checkedAt)) + ' KST';
  document.querySelector('#baseline').textContent = data.baseline;
  document.querySelector('#revision').textContent = data.revision;
  document.querySelector('#snapshot').hidden = false;

  const hero = makeScreenshot(data.hero, { hero: true });
  if (hero) {
    const figure = document.querySelector('#hero-shot');
    figure.replaceChildren(hero, element('figcaption', {}, '보고서 기준 화면'));
    figure.hidden = false;
  }
}

function renderHistory(history) {
  const entries = history.slice().sort((a, b) => new Date(b.date) - new Date(a.date));
  const rows = entries.map(entry => {
    const row = element('li');
    row.append(
      element('time', { datetime: entry.date }, date.format(new Date(entry.date))),
      element('p', {}, entry.summary),
    );
    return row;
  });
  document.querySelector('#history-list').replaceChildren(...rows);
}

async function loadDashboard() {
  checklist.setAttribute('aria-busy', 'true');
  loadState.hidden = false;
  retry.hidden = true;
  loadMessage.textContent = '점검 데이터를 불러오는 중입니다.';
  try {
    const response = await fetch('./status.json', {
      cache: 'no-store',
      signal: AbortSignal.timeout(10000),
    });
    if (!response.ok) throw new Error('점검 데이터 응답 오류');
    const data = await response.json();
    renderSnapshot(data);
    renderChecklist(data.items);
    renderHistory(data.history);
    loadState.hidden = true;
  } catch {
    loadMessage.textContent = '점검 데이터를 불러오지 못했습니다. 잠시 후 다시 시도하세요.';
    retry.hidden = false;
  } finally {
    checklist.setAttribute('aria-busy', 'false');
  }
}

filters.addEventListener('click', event => {
  const button = event.target.closest('button[data-filter]');
  if (!button) return;
  activeFilter = button.dataset.filter;
  updateFilter();
});

retry.addEventListener('click', loadDashboard);
loadDashboard();
