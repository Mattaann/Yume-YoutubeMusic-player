const libraryState = { playlists: [], dragVideo: '', busy: false };
async function musicRequest(path, data = {}) {
  const config = window.ytcfg?.get?.('INNERTUBE_CONTEXT');
  if (!config) throw new Error('YouTube Music is still loading.');
  const headers = {'Content-Type':'application/json','X-Goog-AuthUser':String(window.ytcfg.get('SESSION_INDEX') || 0)};
  const cookie = document.cookie.split(';').map(s=>s.trim()).find(s=>s.startsWith('SAPISID=')) || document.cookie.split(';').map(s=>s.trim()).find(s=>s.startsWith('__Secure-3PAPISID='));
  if (cookie) {
    const secret = cookie.slice(cookie.indexOf('=')+1);
    const time = Math.floor(Date.now()/1000);
    const bytes = await crypto.subtle.digest('SHA-1',new TextEncoder().encode(`${time} ${secret} ${location.origin}`));
    headers.Authorization = `SAPISIDHASH ${time}_${[...new Uint8Array(bytes)].map(b=>b.toString(16).padStart(2,'0')).join('')}`;
  }
  const response = await fetch('/youtubei/v1/'+path+'?prettyPrint=false',{method:'POST',credentials:'same-origin',signal:typeof AbortSignal!=='undefined'?AbortSignal.timeout(10000):undefined,headers,body:JSON.stringify({context:config,...data})});
  if(!response.ok)throw new Error('YouTube Music could not complete the request.');
  const result=await response.json();
  if(result.error)throw new Error('YouTube Music could not complete the request.');
  return result;
}
function walkMusic(value, callback) {
  if(!value || typeof value!=='object')return;
  callback(value);
  for(const child of Object.values(value)) {
    if(Array.isArray(child))child.forEach(v=>walkMusic(v,callback));
    else if(child && typeof child==='object')walkMusic(child,callback);
  }
}
function musicText(value) { return value?.simpleText || value?.runs?.map(r=>r.text||'').join('') || ''; }
async function readAccount() {
  try {
    const data=await musicRequest('account/account_menu');let header;
    walkMusic(data,v=>{if(v.activeAccountHeaderRenderer)header=v.activeAccountHeaderRenderer;});
    if(!header)return {name:'Not signed in',email:'',photo:'',signedIn:false};
    return {name:musicText(header.accountName),email:musicText(header.email),photo:header.accountPhoto?.thumbnails?.at(-1)?.url||'',signedIn:true};
  } catch {return {name:'Account unavailable',email:'Open the YouTube Music account menu to check your session.',photo:'',signedIn:false};}
}
function openAccountMenu() {
  const button=document.querySelector('ytmusic-nav-bar ytmusic-settings-button button, ytmusic-nav-bar ytmusic-settings-button tp-yt-paper-icon-button, ytmusic-nav-bar #avatar-btn, ytmusic-nav-bar button[aria-label*="Account"], ytmusic-nav-bar button[aria-label*="Konto"], ytmusic-nav-bar ytmusic-settings-button');
  if(button){button.click();return true;}
  const signIn=[...document.querySelectorAll('ytmusic-nav-bar a')].find(a=>a.href.includes('accounts.google.com'));
  if(signIn){signIn.click();return true;}
  return false;
}
async function readLibraryPlaylists() {
  if (window.ytcfg?.get?.("LOGGED_IN") === false) { libraryState.playlists=[]; return {items:[],error:"Log in to see your playlists."}; }
  try {
    let data=await musicRequest('browse',{browseId:'FEmusic_liked_playlists'});const items=new Map();
    for(let page=0;page<10;page++) {
      walkMusic(data,v=>{
        const row=v.musicTwoRowItemRenderer || v.musicResponsiveListItemRenderer;
        if(!row)return;
        const endpoint=row.navigationEndpoint?.browseEndpoint;
        const id=endpoint?.browseId?.replace(/^VL/,'');
        const title=musicText(row.title)||musicText(row.flexColumns?.[0]?.musicResponsiveListItemFlexColumnRenderer?.text);
        if(id && /^[\w-]+$/.test(id) && endpoint.browseId.startsWith('VL') && title && id!=='LM')items.set(id,{id,title:title.slice(0,200)});
      });
      let continuation;
      walkMusic(data,v=>{if(v.nextContinuationData?.continuation)continuation=v.nextContinuationData.continuation;});
      if(!continuation)break;
      data=await musicRequest('browse',{continuation});
    }
    libraryState.playlists=[...items.values()];return {items:libraryState.playlists,error:''};
  } catch {
    const account=await readAccount();
    return {items:[],error:account.name==='Not signed in'?'Log in to see your playlists.':'Could not load playlists. Open Playlists and try Refresh.'};
  }
}
async function addDraggedSong(playlistId,videoId) {
  if(libraryState.busy)return 'An add request is already in progress.';
  if(videoId!==libraryState.dragVideo || !/^[\w-]{11}$/.test(videoId) || !libraryState.playlists.some(p=>p.id===playlistId))return 'Drag a song from the music view first.';
  libraryState.dragVideo='';libraryState.busy=true;
  try {
    const result=await musicRequest('browse/edit_playlist',{playlistId,actions:[{action:'ACTION_ADD_VIDEO',addedVideoId:videoId}]});
    if(result.status!=='STATUS_SUCCEEDED')return 'YouTube Music did not add the song. Check that you own this playlist.';
    return 'Song added to playlist.';
  } catch {return 'Could not add the song. Check your connection and playlist permissions.';}
  finally {libraryState.busy=false;}
}
document.addEventListener('dragstart',event=>{
  const row=event.target?.closest?.('ytmusic-responsive-list-item-renderer, ytmusic-two-row-item-renderer, ytmusic-playlist-panel-video-renderer');
  const link=event.target?.closest?.('a[href*="watch?"]') || row?.querySelector('a[href*="watch?"]');
  let id;
  try {id=link?new URL(link.href,location.origin).searchParams.get('v'):row?.data?.navigationEndpoint?.watchEndpoint?.videoId;}catch{}
  if(!/^[\w-]{11}$/.test(id||'') || !event.dataTransfer)return;
  libraryState.dragVideo=id;
  event.dataTransfer.setData('text/plain','https://music.youtube.com/watch?v='+id);
  event.dataTransfer.effectAllowed='copy';
},true);

function addQueuedSong(playlistId,key) {
  const row=queueRows.get(key);
  const id=queueEndpoint(row||{})?.watchEndpoint?.videoId;
  if(!id)return Promise.resolve('This queue item is no longer available.');
  libraryState.dragVideo=id;return addDraggedSong(playlistId,id);
}
document.addEventListener('pointerover',event=>{
  const row=event.target?.closest?.('ytmusic-responsive-list-item-renderer, ytmusic-two-row-item-renderer, ytmusic-playlist-panel-video-renderer');
  if(row && !row.draggable && (row.querySelector('a[href*="watch?"]') || row.data?.navigationEndpoint?.watchEndpoint))row.draggable=true;
},true);
