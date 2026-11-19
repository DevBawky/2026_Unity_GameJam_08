const LINES = [
  '이 자식들! 실적이 안나온다고 이런 무모한 짓을 벌이면 어떻게 하냔 말이야!!',
  '하... 이렇게 된 이상 확실하게 왕자와 공주를 데리고 안전지대까지 빠져나와.',
  '너희들이 붙잡혀도 나는 모르는 일이다. 알겠어?!',
  '그리고 애들을 울리기라도 하면 니네 부서는 진짜 끝장날 줄 알아!!',
];

const CHARACTERS_PER_SECOND = 27;
const TICKS_PER_SECOND = 30;
const LEGACY_READING_TICKS = 54;
const legacyLineDuration = text => Math.ceil(text.length * TICKS_PER_SECOND / CHARACTERS_PER_SECOND) + LEGACY_READING_TICKS;
export const OPERATION_INTRO_TICKS = LINES.reduce((ticks, text) => ticks + legacyLineDuration(text), 0);

export function operationIntroTypingTicks(lineIndex) {
  const text = LINES[lineIndex];
  return text ? Math.ceil((text.length - 1) * TICKS_PER_SECOND / CHARACTERS_PER_SECOND) : 0;
}

function lineFrame(lineTick, lineIndex) {
  const text = LINES[lineIndex];
  if (!text) return null;
  const visibleCharacters = Math.min(text.length, 1 + Math.floor(lineTick * CHARACTERS_PER_SECOND / TICKS_PER_SECOND));
  return {lineIndex, text, visibleCharacters, canAdvance:visibleCharacters === text.length, finalLine:lineIndex === LINES.length - 1, lineTick};
}

export function operationIntroFrame(introTick, lineIndex) {
  if (!Number.isInteger(introTick) || introTick < 0) return null;
  if (lineIndex !== undefined) return Number.isInteger(lineIndex) ? lineFrame(introTick, lineIndex) : null;
  // Old room saves have a single absolute clock and no current dialogue line.
  let lineTick = introTick;
  for (const [lineIndex, text] of LINES.entries()) {
    const duration = legacyLineDuration(text);
    if (lineTick < duration) return lineFrame(lineTick, lineIndex);
    lineTick -= duration;
  }
  return null;
}
