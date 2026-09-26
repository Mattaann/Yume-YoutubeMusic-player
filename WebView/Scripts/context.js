let contextPanel = '';
let contextObserver = null;
let contextRoot = null;
let previousContext = '';
let contextPending = false;
let queueSequence = 0;
const queueKeys = new WeakMap();
let queueRows = new Map();
function textOf(root, selector, limit=300) {
  return (root?.querySelector(selector)?.textContent || '').trim().slice(0,limit);
}
function sendContext() {
  if (!contextPanel) return;
  const rows = [...document.querySelectorAll(selectors.queueItem)].slice(0,80);
  queueRows = new Map();
  const items = rows.map(row=>{
    if (!queueKeys.has(row)) queueKeys.set(row,'q'+(++queueSequence));
    const key=queueKeys.get(row);
    queueRows.set(key,row);
    return {key,title:textOf(row,selectors.queueTitle),artist:textOf(row,selectors.queueArtist),duration:textOf(row,selectors.queueDuration,30),selected:row.hasAttribute('selected')||row.getAttribute('aria-current')==='true'};
  }).filter(item=>item.title);
  const lyrics = contextPanel === 'lyrics' ? (find('lyrics')?.innerText || '').trim().slice(0,20000) : '';
  const data={panel:contextPanel,items:contextPanel==='queue'?items:[],lyrics,message:contextPanel==='queue'&&!items.length?'Start a song to see the queue.':contextPanel==='lyrics'&&!lyrics?'Lyrics are not available yet. Use Open in YouTube Music to view the original panel.':''};
  const serialized=JSON.stringify(data);
  if (serialized!==previousContext) {
    previousContext=serialized;
    window.chrome.webview.postMessage({type:'context',data});
  }
}
function scheduleContext() {
  if(contextPending||!contextPanel)return;
  contextPending=true;
  requestAnimationFrame(()=>{contextPending=false;sendContext();});
}
function attachContext() {
  if(!contextPanel)return;
  const root=find('playerPage');
  if(root!==contextRoot) {
    contextObserver?.disconnect();
    contextRoot=root;
    if(root) {
      contextObserver=new MutationObserver(scheduleContext);
      contextObserver.observe(root,{subtree:true,childList:true,characterData:true,attributes:true,attributeFilter:['selected','aria-current']});
    }
    scheduleContext();
  }
}
function openWebsitePanel(panel) {
  const tabs=[...document.querySelectorAll(selectors.playerTabs)];
  const tab=tabs[panel==='lyrics'?1:0];
  if(tab && !tab.hasAttribute('disabled') && tab.getAttribute('aria-disabled')!=='true')tab.click();
  const page=find('playerPage');
  if(page && (!page.offsetHeight || page.getAttribute('player-page-open')==='false'))find('playerExpand')?.click();
}
function setContextPanel(panel) {
  contextPanel=['queue','lyrics'].includes(panel)?panel:'';
  previousContext='';
  contextObserver?.disconnect();contextRoot=null;
  if(contextPanel==='lyrics')openWebsitePanel('lyrics');
  attachContext();sendContext();
}
function selectQueueItem(key) {
  if(find('advertisement'))return;
  const row=queueRows.get(key);
  if(!row?.isConnected)return;
  const target=row.querySelector(selectors.queueTitle)||row;
  target.click();
}
