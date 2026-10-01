function command(message) {
  const value = Number(message.value);
  switch (message.command) {
    case 'toggle': find('play')?.click(); break;
    case 'play': if (media?.paused) find('play')?.click(); break;
    case 'pause': if (media && !media.paused) find('play')?.click(); break;
    case 'next': find('next')?.click(); break;
    case 'previous': find('previous')?.click(); break;
    case 'shuffle': {
      const bar = find('bar');
      if (find('advertisement')) break;
      if (typeof bar?.onShuffleButtonClick === 'function') bar.onShuffleButtonClick();
      else if (typeof bar?.queue?.shuffle === 'function') bar.queue.shuffle();
      else find('shuffle')?.click();
      break;
    }
    case 'repeat': {
      const bar = find('bar');
      if (find('advertisement')) break;
      if (typeof bar?.onRepeatButtonClick === 'function') bar.onRepeatButtonClick();
      else find('repeat')?.click();
      break;
    }
    case 'seek':
      if (media?.seekable?.length && !find('advertisement') && Number.isFinite(value) && Number.isFinite(media.duration)) {
        const start = media.seekable.start(0);
        const end = Math.min(media.duration,media.seekable.end(media.seekable.length-1));
        media.currentTime = Math.max(start,Math.min(value,end));
      }
      break;
    case 'volume':
      if(Number.isFinite(value)) {
        if(window.__yumeVolume)window.__yumeVolume.set(value);
        else if(media)media.volume=Math.max(0,Math.min(value,1));
      }
      break;
    case 'mute': if (media) media.muted = !media.muted; break;
    case 'search': window.chrome.webview.postMessage({type:'shortcut',key:'search'}); break;
    case 'account': find('account')?.click(); break;
  }
  sendState();
}
