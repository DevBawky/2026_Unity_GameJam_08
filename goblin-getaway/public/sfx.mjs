const cues = {
  'ui-tick': {asset:'ui-tick', gain:.28},
  'ui-confirm': {asset:'ui-confirm', gain:.36},
  'ui-reject': {asset:'ui-reject', gain:.44},
  'start': {asset:'ui-confirm', gain:.4, rate:.86},
  'pause': {asset:'ui-tick', gain:.34, rate:.75},
  'resume': {asset:'ui-confirm', gain:.35, rate:1.12},
  'room-join': {asset:'ui-confirm', gain:.38, rate:1.08},
  'room-leave': {asset:'ui-tick', gain:.34, rate:.85},
  'room-error': {asset:'ui-reject', gain:.43},
  'room-reconnect': {asset:'ui-confirm', gain:.44, rate:.95},
  'stage-report': {asset:'ui-confirm', gain:.4, rate:.85},
  'stage-purchase': {asset:'ui-confirm', gain:.46, rate:1.07},
  'stage-depart': {asset:'ui-confirm', gain:.48, rate:.9},
  'team-request': {asset:'team-bell', gain:.61, priority:2},
  'team-social': {asset:'team-bell', gain:.32, rate:1.25},
  'road-warning-upper': {asset:'danger-pulse', gain:.62, rate:1.2, pan:-.3, priority:2},
  'road-warning-lower': {asset:'danger-pulse', gain:.62, rate:.84, pan:.3, priority:2},
  'road-hit': {asset:'impact-cart', gain:.61, priority:1},
  'pursuit-wave': {asset:'pursuit-horn', gain:.53, rate:.9, priority:1},
  'pursuit-warning': {asset:'pursuit-horn', gain:.49, rate:1.24, priority:2},
  'pursuit-hit': {asset:'impact-cart', gain:.55, rate:1.14, priority:1},
  'catapult-launch': {asset:'stone-flight', gain:.51, priority:1},
  'catapult-lock': {asset:'danger-pulse', gain:.68, rate:1.03, priority:2},
  'catapult-ground': {asset:'impact-dirt', gain:.53, rate:.89, priority:1},
  'catapult-hit': {asset:'impact-cart', gain:.79, rate:.81, priority:2},
  'hull-critical': {asset:'danger-pulse', gain:.63, rate:.7, priority:2},
  'wheel-break': {asset:'axle-snap', gain:.62, priority:2},
  'axle-break': {asset:'axle-snap', gain:.78, rate:.71, priority:2},
  'wheel-knock': {asset:'impact-dirt', gain:.42, rate:1.3},
  'wheel-pickup': {asset:'wheel-fit', gain:.32, rate:.9},
  'wheel-return': {asset:'wheel-fit', gain:.3, rate:1.17},
  'wheel-mount': {asset:'wheel-fit', gain:.46},
  'wheel-complete': {asset:'wheel-fit', gain:.56, rate:1.14, priority:1},
  'shell-pickup': {asset:'shell-metal', gain:.38, rate:1.18},
  'shell-slip': {asset:'danger-pulse', gain:.67, rate:1.42, priority:2},
  'shell-catch': {asset:'ui-confirm', gain:.45, rate:1.2, priority:1},
  'shell-drop': {asset:'shell-metal', gain:.56, rate:.77, priority:1},
  'shell-deposit': {asset:'shell-metal', gain:.46},
  'shell-load': {asset:'cannon-load', gain:.59, priority:1},
  'shell-empty': {asset:'ui-reject', gain:.36, rate:1.2},
  'work-fail': {asset:'ui-reject', gain:.54, priority:1},
  'timing-ready': {asset:'ui-tick', gain:.34, rate:1.5},
  'cannon-clean': {asset:'brush-scrub', gain:.34},
  'cannon-ready': {asset:'cannon-load', gain:.44, rate:1.33},
  'cannon-fuse': {asset:'fuse-burn', gain:.44, rate:.60, priority:1},
  'cannon-imminent': {asset:'danger-pulse', gain:.48, rate:1.36, priority:2},
  'cannon-fire': {asset:'cannon-fire', gain:.83, priority:2},
  'cannon-ground': {asset:'impact-dirt', gain:.53, priority:1},
  'cannon-hit': {asset:'impact-dirt', gain:.66, rate:1.13, priority:1},
  'whip': {asset:'whip-snap', gain:.46},
  'horse-boost': {asset:'whip-snap', gain:.53, rate:1.25, priority:1},
  'horse-tired': {asset:'whip-snap', gain:.29, rate:.69},
  'horse-recover': {asset:'ui-confirm', gain:.34, rate:1.22},
  'royal-fall': {asset:'royal-whimper', gain:.57, rate:.87, priority:1},
  'royal-cry': {asset:'royal-whimper', gain:.39},
  'royal-wander': {asset:'ui-tick', gain:.33, rate:.86},
  'royal-request': {asset:'team-bell', gain:.4, rate:1.3},
  'royal-bored': {asset:'team-bell', gain:.32, rate:1.16},
  'royal-recovered': {asset:'ui-confirm', gain:.31, rate:1.04},
  'royal-mischief': {asset:'shell-metal', gain:.46, rate:.92, priority:1},
  'royal-pickup': {asset:'ui-confirm', gain:.26, rate:1.1},
  'royal-soothed': {asset:'ui-confirm', gain:.35, rate:.95},
  'royal-seat': {asset:'ui-tick', gain:.36, rate:.82},
  'royal-feed': {asset:'ui-confirm', gain:.34, rate:1.15},
  'royal-heart': {asset:'ui-confirm', gain:.29, rate:1.34},
  'cook-start': {asset:'cook-sizzle', gain:.27},
  'cook-stop': {asset:'ui-tick', gain:.27, rate:.68},
  'cook-shake': {asset:'ui-reject', gain:.39, rate:.87},
  'cook-ready': {asset:'ui-confirm', gain:.47, rate:1.17, priority:1},
  'floor-change': {asset:'ui-tick', gain:.36, rate:.82},
  'station-enter': {asset:'ui-tick', gain:.31, rate:1.1},
  'station-exit': {asset:'ui-tick', gain:.28, rate:.78},
  'stage-clear': {asset:'stage-result', gain:.62, priority:2},
  'chapter-clear': {asset:'stage-result', gain:.68, rate:.9, priority:2},
  'stage-lost': {asset:'defeat-sting', gain:.59, priority:2},
};

// Fetch the sounds that can matter in the first few interactions before loading the rest.
const firstAssets = [
  'ui-tick', 'ui-confirm', 'ui-reject', 'danger-pulse', 'team-bell',
  'impact-cart', 'axle-snap', 'fuse-burn', 'cannon-fire',
];
export const SFX_ASSET_NAMES = Object.freeze([...new Set(Object.values(cues).map(cue => cue.asset))]);
const allAssets = [...new Set([...firstAssets, ...SFX_ASSET_NAMES])];
const maxVoices = 10;
const masterLevel = .64;
const sampleWaitMs = 120;

function clamp(value, min, max) {
  return Math.max(min, Math.min(max, value));
}

function numeric(value, fallback) {
  return Number.isFinite(value) ? value : fallback;
}

function decode(context, data) {
  // Some older WebKit versions only expose the callback form of decodeAudioData.
  return new Promise((resolve, reject) => {
    const result = context.decodeAudioData(data, resolve, reject);
    result?.then(resolve, reject);
  });
}

export function createSfxPlayer() {
  let enabled = false;
  let audio;
  let resuming;
  let preloaded = false;
  let generation = 0;
  const files = new Map();
  const pending = new Map();
  const voices = new Set();
  const loops = new Map();
  const lastCue = new Map();
  const cueGeneration = new Map();

  function getAudio() {
    return enabled && audio?.context.state === 'running' ? audio : null;
  }

  function load(asset) {
    if (files.has(asset)) return Promise.resolve(files.get(asset));
    if (pending.has(asset)) return pending.get(asset);
    const {context} = audio;
    const request = fetch('/sfx/' + asset + '.wav')
      .then(response => {
        if (!response.ok) throw new Error('SFX request failed');
        return response.arrayBuffer();
      })
      .then(data => decode(context, data))
      .catch(() => null)
      .then(buffer => {
        pending.delete(asset);
        files.set(asset, buffer);
        return buffer;
      });
    pending.set(asset, request);
    return request;
  }

  async function preload() {
    if (preloaded) return;
    preloaded = true;
    for (let index = 0; index < allAssets.length; index += 4) {
      await Promise.all(allAssets.slice(index, index + 4).map(load));
    }
  }

  function onUnlock() {
    if (!getAudio()) return false;
    void preload();
    for (const [key, loop] of loops) startLoop(key, loop);
    return true;
  }

  function unlock() {
    if (!enabled) return Promise.resolve(false);
    try {
      if (!audio) {
        const Context = globalThis.AudioContext ?? globalThis.webkitAudioContext;
        if (!Context) return Promise.resolve(false);
        const context = new Context();
        const destination = context.createGain();
        destination.gain.value = masterLevel;
        if (typeof context.createDynamicsCompressor === 'function') {
          const limiter = context.createDynamicsCompressor();
          limiter.threshold.value = -13;
          limiter.knee.value = 14;
          limiter.ratio.value = 5;
          limiter.attack.value = .003;
          limiter.release.value = .16;
          destination.connect(limiter);
          limiter.connect(context.destination);
        } else {
          destination.connect(context.destination);
        }
        audio = {context, destination};
      }
      if (audio.context.state === 'running') {
        return Promise.resolve(onUnlock());
      }
      resuming ??= Promise.resolve(audio.context.resume()).then(onUnlock)
        .catch(() => false).finally(() => { resuming = undefined; });
      return resuming;
    } catch {
      return Promise.resolve(false);
    }
  }

  function removeVoice(voice) {
    voices.delete(voice);
    voice.source.disconnect();
    voice.gain.disconnect();
    voice.panner?.disconnect();
    if (loops.get(voice.key)?.voice === voice) loops.delete(voice.key);
  }

  function stopVoice(voice) {
    if (!voices.delete(voice)) return;
    const now = audio.context.currentTime;
    voice.gain.gain.setTargetAtTime(0, now, .008);
    voice.source.stop(now + .035);
  }

  function stop(key) {
    cueGeneration.set(key, (cueGeneration.get(key) ?? 0) + 1);
    loops.delete(key);
    for (const voice of voices) {
      if (voice.key === key) stopVoice(voice);
    }
  }

  function stopAll() {
    generation++;
    loops.clear();
    lastCue.clear();
    for (const voice of voices) stopVoice(voice);
  }

  function setEnabled(value) {
    const next = Boolean(value);
    if (enabled === next) return;
    enabled = next;
    if (audio) {
      audio.destination.gain.setValueAtTime(next ? masterLevel : 0, audio.context.currentTime);
    }
    if (!next) stopAll();
  }

  function startVoice(key, cue, buffer, options, loop = false) {
    const output = getAudio();
    if (!output) return null;
    const priority = cue.priority ?? 0;
    if (voices.size >= maxVoices) {
      let quietest;
      for (const voice of voices) {
        if (!voice.loop && voice.priority <= priority && (!quietest || voice.priority < quietest.priority)) {
          quietest = voice;
        }
      }
      if (!quietest) return null;
      stopVoice(quietest);
    }
    const {context, destination} = output;
    const source = context.createBufferSource();
    source.buffer = buffer;
    const rateScale = numeric(options.rate, 1);
    const rate = clamp((cue.rate ?? 1) * rateScale, .35, 2.5);
    source.playbackRate.value = rate;
    if (cue.endRate) {
      source.playbackRate.setValueAtTime(rate, context.currentTime);
      source.playbackRate.linearRampToValueAtTime(
        clamp(cue.endRate * rateScale, .35, 2.5),
        context.currentTime + Math.min(.7, buffer.duration / rate),
      );
    }
    const gain = context.createGain();
    const level = clamp(cue.gain * numeric(options.gain, 1), 0, 1);
    const pan = clamp(numeric(options.pan, cue.pan ?? 0), -1, 1);
    const panner = pan !== 0 && typeof context.createStereoPanner === 'function' ? context.createStereoPanner() : null;
    gain.gain.value = loop ? 0 : level;
    source.connect(gain);
    if (panner) {
      panner.pan.value = pan;
      gain.connect(panner);
      panner.connect(destination);
    } else {
      gain.connect(destination);
    }
    const voice = {key, source, gain, panner, priority, loop};
    source.onended = () => removeVoice(voice);
    if (loop) {
      // Use the sustained center of the cook recording, away from its transient and tail.
      source.loop = true;
      source.loopStart = buffer.duration * .12;
      source.loopEnd = buffer.duration * .88;
      gain.gain.setTargetAtTime(level, context.currentTime, .02);
      source.start(0, source.loopStart);
    } else {
      source.start();
    }
    voices.add(voice);
    return voice;
  }

  function play(key, options = {}) {
    if (!enabled || (!getAudio() && !resuming)) return;
    const cue = Object.hasOwn(cues, key) ? cues[key] : null;
    const now = performance.now();
    if (now - (lastCue.get(key) ?? -Infinity) < 55) return;
    lastCue.set(key, now);
    const startedIn = generation;
    const startedCue = cueGeneration.get(key);
    let played = false;
    function finish(buffer) {
      if (played || startedIn !== generation || startedCue !== cueGeneration.get(key) || !getAudio()) return;
      played = true;
      try {
        if (buffer) startVoice(key, cue, buffer, options);
        else options.fallback?.();
      } catch {
        if (buffer) {
          try { options.fallback?.(); } catch {}
        }
      }
    }
    function ready() {
      if (startedIn !== generation || startedCue !== cueGeneration.get(key) || !getAudio()) return;
      if (!cue || files.has(cue.asset)) {
        finish(cue ? files.get(cue.asset) : null);
        return;
      }
      const timer = setTimeout(() => finish(null), sampleWaitMs);
      load(cue.asset).then(buffer => {
        clearTimeout(timer);
        finish(performance.now() - now <= sampleWaitMs ? buffer : null);
      });
    }
    if (getAudio()) ready();
    else resuming.then(() => { if (performance.now() - now <= sampleWaitMs) ready(); });
  }

  function startLoop(key, loop) {
    if (!getAudio() || loop.loading || loop.voice || files.get(loop.cue.asset) === null) return;
    loop.loading = true;
    load(loop.cue.asset).then(buffer => {
      loop.loading = false;
      if (!buffer || !getAudio() || loops.get(key) !== loop) return;
      try { loop.voice = startVoice(key, loop.cue, buffer, loop.options, true); } catch {}
    });
  }

  function setLoop(key, active, options = {}) {
    if (!active) {
      if (loops.has(key)) stop(key);
      return;
    }
    const cue = Object.hasOwn(cues, key) ? cues[key] : null;
    if (!cue || !enabled || loops.has(key)) return;
    const loop = {cue, options, voice:null, loading:false};
    loops.set(key, loop);
    startLoop(key, loop);
  }

  return {setEnabled, unlock, getAudio, play, setLoop, stop, stopAll};
}
