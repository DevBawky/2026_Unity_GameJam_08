const tracks = {
  waiting: {loopSeconds: 41.739, gain: .48},
  chase: {loopSeconds: 59.259, gain: .57},
  'stage-clear': {gain: .60},
  'game-over': {gain: .56},
};
const fadeOutSeconds = .12;
const fadeInSeconds = .11;
const musicLevel = .65;

function decode(context, bytes) {
  // Keep support for WebKit versions exposing the callback signature.
  return new Promise((resolve, reject) => {
    context.decodeAudioData(bytes, resolve, reject)?.then(resolve, reject);
  });
}

export function createMusicPlayer({enabled = true} = {}) {
  let context, master, resuming, voice, tail;
  let phase = 'waiting', paused = false, hidden = false;
  let generation = 0, loading = -1, failed = -1;
  let offset = 0, nextStart = 0, finished = false, warmed = false;
  const buffers = new Map();

  function load(name) {
    if (!buffers.has(name)) {
      const request = fetch('/music/' + name + '.mp3')
        .then(response => {
          if (!response.ok) throw new Error('Music request failed');
          return response.arrayBuffer();
        })
        .then(bytes => decode(context, bytes))
        .catch(() => {
          buffers.delete(name);
          return null;
        });
      buffers.set(name, request);
    }
    return buffers.get(name);
  }

  function canPlay() {
    return enabled && !hidden && context?.state === 'running';
  }

  function musicVolume() {
    const track = tracks[phase];
    return musicLevel * track.gain * (paused && track.loopSeconds ? .28 : 1);
  }

  function updateGain() {
    if (master) {
      master.gain.setTargetAtTime(enabled && !hidden ? musicVolume() : 0, context.currentTime, .045);
    }
  }

  function stopVoice(remember, fade = false) {
    if (!voice) return;
    const previous = voice;
    voice = null;
    const now = context.currentTime;
    if (remember) offset = previous.offset + Math.max(0, now - previous.startedAt);
    if (fade && now >= previous.startedAt) {
      // Finish the outgoing fade before allowing the next cue to start.
      nextStart = now + fadeOutSeconds;
      tail = previous;
      previous.gain.gain.setTargetAtTime(0, now, fadeOutSeconds / 5);
      previous.source.stop(nextStart);
    } else {
      previous.source.stop();
    }
  }

  function suspendMusic() {
    stopVoice(true);
    if (tail) {
      tail.source.stop();
      tail = null;
    }
    nextStart = 0;
  }

  function startVoice(buffer, track) {
    const looping = Boolean(track.loopSeconds);
    if (!looping && offset >= buffer.duration) {
      finished = true;
      return;
    }
    const source = context.createBufferSource();
    const gain = context.createGain();
    const startedAt = Math.max(context.currentTime, nextStart);
    const duration = looping ? Math.min(track.loopSeconds, buffer.duration) : buffer.duration;
    // MP3 exports can retain a short encoder delay and end padding. Use the
    // edited PCM length when the browser has not already removed that padding.
    const loopStart = looping ? Math.min(.024, (buffer.duration - duration) / 2) : 0;
    const startOffset = looping ? offset % duration : offset;
    const current = {source, gain, startedAt, offset: startOffset};
    source.buffer = buffer;
    source.loop = looping;
    if (looping) {
      source.loopStart = loopStart;
      source.loopEnd = loopStart + duration;
    }
    source.connect(gain);
    gain.connect(master);
    gain.gain.setValueAtTime(0, startedAt);
    gain.gain.linearRampToValueAtTime(1, startedAt + fadeInSeconds);
    source.onended = () => {
      source.disconnect();
      gain.disconnect();
      if (tail === current) tail = null;
      if (voice === current) {
        voice = null;
        finished = !looping;
      }
    };
    source.start(startedAt, loopStart + startOffset);
    voice = current;
  }

  function reconcile() {
    if (!canPlay() || voice || finished || loading === generation || failed === generation) return;
    const request = generation;
    const track = tracks[phase];
    loading = request;
    void load(phase).then(buffer => {
      if (loading === request) loading = -1;
      if (request !== generation) return;
      if (!buffer?.duration) {
        failed = request;
        return;
      }
      if (canPlay() && !voice && !finished) startVoice(buffer, track);
    }).catch(() => { if (request === generation) failed = request; });
  }

  function onUnlock() {
    if (!canPlay()) return false;
    reconcile();
    if (!warmed) {
      warmed = true;
      // Let the currently requested cue lead the first batch, then warm the
      // other gameplay states for transitions on a slow connection.
      for (const name of Object.keys(tracks)) if (name !== phase) void load(name);
    }
    return true;
  }

  function unlock() {
    if (hidden || !enabled) return Promise.resolve(false);
    try {
      if (!context) {
        const Context = globalThis.AudioContext ?? globalThis.webkitAudioContext;
        if (!Context) return Promise.resolve(false);
        context = new Context();
        master = context.createGain();
        master.gain.value = musicVolume();
        master.connect(context.destination);
        context.addEventListener('statechange', onUnlock);
      }
      if (context.state === 'running') return Promise.resolve(onUnlock());
      if (!resuming) {
        resuming = context.resume().then(onUnlock, () => false).finally(() => { resuming = null; });
      }
      return resuming;
    } catch {
      return Promise.resolve(false);
    }
  }

  function setEnabled(value) {
    if (enabled === value) return;
    enabled = value;
    if (!enabled) suspendMusic();
    else failed = -1;
    updateGain();
    reconcile();
  }

  function setState(nextPhase, {paused: nextPaused = false, hidden: nextHidden = false} = {}) {
    if (!Object.hasOwn(tracks, nextPhase)) return;
    if (phase === nextPhase && paused === nextPaused && hidden === nextHidden) return;
    const returningToPage = hidden && !nextHidden;
    paused = nextPaused;
    hidden = nextHidden;
    if (phase !== nextPhase) {
      stopVoice(false, canPlay());
      phase = nextPhase;
      generation++;
      offset = 0;
      finished = false;
    }
    if (hidden) suspendMusic();
    updateGain();
    reconcile();
    if (returningToPage && context && context.state !== 'running') void unlock();
  }

  return {unlock, setEnabled, setState};
}
