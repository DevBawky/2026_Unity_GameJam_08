const LINES = [
  '이 자식들! 실적이 안나온다고 이런 무모한 짓을 벌이면 어떻게 하냔 말이야!!',
  '하... 이렇게 된 이상 확실하게 왕자와 공주를 데리고 안전지대까지 빠져나와.',
  '너희들이 붙잡혀도 나는 모르는 일이다. 알겠어?!',
  '그리고 애들을 울리기라도 하면 니네 부서는 진짜 끝장날 줄 알아!!',
];

// The simulation advances 30 ticks per second.
const TICKS_PER_CHARACTER = 30 / 27;
const READING_TICKS = 54;
const lineDuration = text => Math.ceil(text.length * TICKS_PER_CHARACTER) + READING_TICKS;
export const OPERATION_INTRO_TICKS = LINES.reduce((ticks, text) => ticks + lineDuration(text), 0);

export function operationIntroFrame(introTick) {
  if (!Number.isInteger(introTick) || introTick < 0 || introTick >= OPERATION_INTRO_TICKS) return null;
  let lineTick = introTick;
  for (const [lineIndex, text] of LINES.entries()) {
    const duration = lineDuration(text);
    if (lineTick < duration) {
      const visibleCharacters = Math.min(text.length, 1 + Math.floor(lineTick / TICKS_PER_CHARACTER));
      return {lineIndex, text, visibleCharacters};
    }
    lineTick -= duration;
  }
  return null;
}
