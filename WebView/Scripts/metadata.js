function snapshot() {
  const metadata = navigator.mediaSession?.metadata;
  const bar = find('bar');
  const repeatValue = bar?.repeatMode ?? bar?.queue?.store?.store?.getState?.()?.queue?.repeatMode;
  const repeatModes = {NONE:0,OFF:0,ALL:1,ONE:2,REPEAT_MODE_NONE:0,REPEAT_MODE_OFF:0,REPEAT_MODE_ALL:1,REPEAT_MODE_ONE:2};
  const repeatMode = typeof repeatValue === 'number' && repeatValue >= 0 && repeatValue <= 2 ? repeatValue : repeatModes[repeatValue] ?? -1;
  const duration = Number.isFinite(media?.duration) ? media.duration : 0;
  return {
    title: metadata?.title || find('title')?.textContent?.trim() || 'Choose something to play',
    artist: metadata?.artist || find('artist')?.textContent?.trim() || 'YouTube Music',
    album: metadata?.album || '',
    artwork: metadata?.artwork?.at(-1)?.src || find('artwork')?.src || '',
    playing: !!media && !media.paused && !media.ended,
    available: !!media && media.readyState > 0,
    canSeek: !!media?.seekable?.length && Number.isFinite(media.duration) && !find('advertisement'),
    canShuffle: !find('advertisement') && !!(find('shuffle') || typeof bar?.onShuffleButtonClick === 'function' || typeof bar?.queue?.shuffle === 'function'),
    canRepeat: !find('advertisement') && !!(find('repeat') || typeof bar?.onRepeatButtonClick === 'function'),
    shuffleActive: bar?.hasAttribute?.('shuffle-on') ?? false,
    repeatMode,
    position: Number.isFinite(media?.currentTime) ? media.currentTime : 0,
    duration,
    volume: media?.volume ?? 1,
    muted: media?.muted ?? false
  };
}
let lastSent = 0;
function sendState(event) {
  if (event?.type === 'timeupdate' && performance.now() - lastSent < 700) return;
  lastSent = performance.now();
  document.documentElement?.toggleAttribute('data-yume-player', !!media && !!find('play') && !!find('next') && !!find('previous'));
  const state=snapshot();
  document.documentElement?.toggleAttribute('data-yume-playing',state.playing);
  window.chrome.webview.postMessage({type:'state', data:state});
  if(event?.type !== 'timeupdate') refreshContextTrack();
}
