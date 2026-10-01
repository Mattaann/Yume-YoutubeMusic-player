const routes = Object.freeze({
  '/':'FEmusic_home',
  '/explore':'FEmusic_explore',
  '/library':'FEmusic_library_landing',
  '/library/playlists':'FEmusic_liked_playlists',
  '/playlist?list=LM':'VLLM',
  '/library/albums':'FEmusic_liked_albums',
  '/library/artists':'FEmusic_library_corpus_track_artists'
});
let lastBrowsePath = typeof location === 'undefined' ? '/' : location.pathname + location.search;
let reportedPath = null;
let keepHomeUntil = 0;
function reportNavigation() {
  if(typeof location==='undefined')return;
  const path=location.pathname+location.search;
  if(location.pathname!=='/watch')lastBrowsePath=path;
  const selected=Object.hasOwn(routes,lastBrowsePath)?lastBrowsePath:'';
  if(selected!==reportedPath) {
    reportedPath=selected;
    window.chrome.webview.postMessage({type:'navigation',path:selected});
  }
}
function retainHomeView() {
  if(performance.now()>keepHomeUntil || !keepHomeUntil)return;
  const app=document.querySelector('ytmusic-app');
  const bar=find('bar');
  const expanded=bar?.playerPageOpen===true || app?.playerPageOpen===true || (app?.hasAttribute('player-page-open') && app.getAttribute('player-page-open')!=='false');
  if(expanded) { keepHomeUntil=0; find('playerExpand')?.click(); }
}
document.addEventListener('yt-navigate',event=>{
  if(event.detail?.endpoint && !event.detail.endpoint.searchEndpoint)window.chrome.webview.postMessage({type:'shortcut',key:'clear-search'});
  if(event.detail?.endpoint?.watchEndpoint && lastBrowsePath==='/') {
    keepHomeUntil=performance.now()+5000;
    queueMicrotask(retainHomeView);
  }
},true);
document.addEventListener('yt-navigate-finish',()=>{
  retainHomeView();reportNavigation();
  if(typeof location!=='undefined' && location.pathname!=='/search')window.chrome.webview.postMessage({type:'shortcut',key:'clear-search'});
});
let navigationApp=null;
const navigationObserver=new MutationObserver(()=>{retainHomeView();reportNavigation();});
function attachNavigation() {
  const app=document.querySelector('ytmusic-app');
  if(app!==navigationApp) {
    navigationObserver.disconnect();navigationApp=app;
    if(app)navigationObserver.observe(app,{attributes:true,attributeFilter:['player-page-open']});
  }
  retainHomeView();reportNavigation();
}
function navigate(path) {
  if (typeof path !== 'string') return;
  const target = new URL(path,location.origin);
  if (target.origin !== location.origin) return;
  keepHomeUntil=0;
  const anchor = [...document.querySelectorAll('a[href]')].find(a=>a.href === target.href);
  if (anchor) { anchor.click(); return; }
  const app = document.querySelector('ytmusic-app');
  const endpoint = path.startsWith('/search?')
    ? {searchEndpoint:{query:target.searchParams.get('q')||''}}
    : routes[path] ? {browseEndpoint:{browseId:routes[path]}} : null;
  if (app && endpoint) app.dispatchEvent(new CustomEvent('yt-navigate',{bubbles:true,composed:true,detail:{endpoint}}));
}
