const routes = Object.freeze({
  '/':'FEmusic_home',
  '/explore':'FEmusic_explore',
  '/library':'FEmusic_library_landing',
  '/library/playlists':'FEmusic_liked_playlists',
  '/playlist?list=LM':'VLLM',
  '/library/albums':'FEmusic_liked_albums',
  '/library/artists':'FEmusic_library_corpus_track_artists'
});
function navigate(path) {
  if (typeof path !== 'string') return;
  const target = new URL(path,location.origin);
  if (target.origin !== location.origin) return;
  const anchor = [...document.querySelectorAll('a[href]')].find(a=>a.href === target.href);
  if (anchor) { anchor.click(); return; }
  const app = document.querySelector('ytmusic-app');
  const endpoint = path.startsWith('/search?')
    ? {searchEndpoint:{query:target.searchParams.get('q')||''}}
    : routes[path] ? {browseEndpoint:{browseId:routes[path]}} : null;
  if (app && endpoint) app.dispatchEvent(new CustomEvent('yt-navigate',{bubbles:true,composed:true,detail:{endpoint}}));
}
