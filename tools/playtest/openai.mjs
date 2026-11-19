import {actionSchema} from './game.mjs';

const ACTION_NAME = 'play_input';

export const DEFAULT_MODEL = 'gpt-6-luna';

const inputTool = {
  type: 'function',
  name: ACTION_NAME,
  description: 'Apply one short player input to the web game. Coordinates point right (+x) and down (+y).',
  strict: true,
  parameters: actionSchema,
};

const instructions = `You control one slot in the Korean web game 《그건 님이!!!》. Other slots may be bots.
Your playtest objective is to obtain a shell at the cellar supply, carry it upstairs, respond to cannon recoil while carrying, then deliver it to the rack so the team can load and fire it. Once you deliver a shell you caught, give the crew time to load and fire it. Try another supply run if you lose the shell or deliver without encountering recoil. Use the observation and the provided controls to choose the next playable input.
The cannon cycles through cleaning, loading and firing. If you operate the cleaning spot, each press adds progress once. The active cannon spot appears among the visible waypoints when in view.
When the stick mode is steer, use move with x=0 and y for the wagon's up/down direction. Press at the driver seat to whip the horses and raise their rage meter.
Use approach to walk toward a waypoint shown in the current view; it sends ordinary movement and can stop early on arrival, a floor or role transition, or a catch cue. Move with the stick to get around obstacles or to step clear of a stair before crossing again. Use leave if a workstation locks movement. If the work requires timing, wait for the visible gauge to enter its marked region, then press (use 1 tick so you can see the result). Catch has priority over walking when its cue is visible. While carrying, walking into the rack delivers automatically. Keep playing when a teammate performs a task, and use the last input results to avoid repeating a blocked action. In a quiet area, use longer movement or waiting chunks; during an urgent cue use shorter inputs. Game time advances only when an input is played.`;

export class PlayApiError extends Error {}

export function openAIPlayer(apiKey, model, request = fetch) {
  return async (observation, recentInputs, signal) => {
    const requestSignal = AbortSignal.any([signal, AbortSignal.timeout(30_000)]);
    let response;
    let payload;
    try {
      response = await request('https://api.openai.com/v1/responses', {
        method: 'POST',
        headers: {'Authorization': `Bearer ${apiKey}`, 'Content-Type': 'application/json'},
        redirect: 'error',
        signal: requestSignal,
        body: JSON.stringify({
          model,
          ...(model.startsWith('gpt-6') ? {reasoning: {effort: 'none'}} : {}),
          instructions,
          input: JSON.stringify({observation, recentInputs}),
          tools: [inputTool],
          tool_choice: {type: 'function', name: ACTION_NAME},
          parallel_tool_calls: false,
          max_output_tokens: 1024,
          store: false,
        }),
      });
      if (!response.ok) {
        await response.body?.cancel();
        throw new PlayApiError(`OpenAI API HTTP ${response.status}`);
      }
      payload = await response.json();
    } catch (error) {
      if (signal.aborted || error instanceof PlayApiError) throw error;
      if (requestSignal.aborted) throw new PlayApiError('OpenAI API 응답 시간 초과');
      throw new PlayApiError('OpenAI API 연결 또는 응답 읽기 실패');
    }

    const calls = Array.isArray(payload?.output) ? payload.output.filter(item => item?.type === 'function_call') : [];
    if (payload?.status !== 'completed' || calls.length !== 1 || calls[0].name !== ACTION_NAME) {
      throw new PlayApiError('OpenAI API에서 실행할 게임 입력을 받지 못했습니다');
    }
    let action;
    try {
      action = JSON.parse(calls[0].arguments);
    } catch {
      throw new PlayApiError('OpenAI API가 게임 입력을 읽을 수 없는 형식으로 반환했습니다');
    }
    if (!action || !actionSchema.properties.kind.enum.includes(action.kind) ||
        ![action.x, action.y].every(axis => Number.isFinite(axis) && Math.abs(axis) <= 1) ||
        !Number.isInteger(action.ticks) || action.ticks < 1 || action.ticks > actionSchema.properties.ticks.maximum ||
        (action.target !== null && (typeof action.target !== 'string' || action.target.length > 40))) {
      throw new PlayApiError('OpenAI API가 유효 범위를 벗어난 게임 입력을 반환했습니다');
    }
    const {kind, x, y, ticks, target} = action;
    return {kind, x, y, ticks, target};
  };
}
