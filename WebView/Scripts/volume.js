if (!window.__yumeVolume) {
  let desired = null;
  const clamp = value => Math.max(0, Math.min(value, 1));
  function apply(media) {
    if(desired !== null && Number.isFinite(media?.volume) && Math.abs(media.volume-desired)>0.001)media.volume=desired;
  }
  function syncPlayer(value) {
    const player=document.querySelector('#movie_player');
    if(typeof player?.setVolume==='function')player.setVolume(value*100);
  }
  function set(value) {
    if(!Number.isFinite(value))return;
    value=clamp(value);
    if(desired!==null)desired=value;
    syncPlayer(value);
    document.querySelectorAll('video, audio').forEach(media=>{media.volume=value;});
  }
  function configure(value, remember) {
    desired=remember && Number.isFinite(value)?clamp(value):null;
    if(desired!==null)set(desired);
  }
  for(const event of ['volumechange','loadedmetadata','play'])document.addEventListener(event,e=>apply(e.target),true);
  const observer=new MutationObserver(records=>{
    if(desired===null)return;
    for(const record of records)for(const node of record.addedNodes) {
      if(node.matches?.('video, audio'))apply(node);
      node.querySelectorAll?.('video, audio').forEach(apply);
    }
  });
  observer.observe(document,{childList:true,subtree:true});
  window.__yumeVolume=Object.freeze({set,configure});
}
