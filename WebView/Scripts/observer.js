const events = ['play','pause','ended','durationchange','loadedmetadata','timeupdate','volumechange','emptied'];
let observedBar = null;
const metadataObserver = new MutationObserver(() => sendState());
function attach() {
  attachContext();
  const next = find('media');
  if (next !== media) {
    if (media) events.forEach(name => media.removeEventListener(name,sendState));
    media = next;
    if (media) events.forEach(name => media.addEventListener(name,sendState));
    sendState();
  }
  const bar = find('bar');
  if (bar !== observedBar) {
    metadataObserver.disconnect();
    observedBar = bar;
    if (bar) metadataObserver.observe(bar,{subtree:true,childList:true,characterData:true,attributes:true,attributeFilter:['src','title','shuffle-on','repeat-mode','aria-pressed','icon']});
    sendState();
  }
}
let pending = false;
const observer = new MutationObserver(() => {
  if (pending) return;
  pending = true;
  queueMicrotask(() => { pending = false; attach(); });
});
observer.observe(document.body,{subtree:true,childList:true});
attach();
