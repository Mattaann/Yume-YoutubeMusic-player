window.__yume = Object.freeze({command,navigate,setContextPanel,selectQueueItem,openWebsitePanel,requestSuggestions});
document.addEventListener('keydown',event => {
  const typing = event.composedPath().some(el => el?.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(el?.tagName));
  if (event.ctrlKey && (event.key.toLowerCase() === 'k' || event.key.toLowerCase() === 'l')) {
    event.preventDefault(); event.stopImmediatePropagation(); command({command:'search'});
  } else if (!typing && event.code === 'Space') {
    event.preventDefault(); event.stopImmediatePropagation(); command({command:'toggle'});
  } else if (event.ctrlKey && event.shiftKey && event.key.toLowerCase() === 'm') {
    event.preventDefault(); command({command:'mute'});
  } else if (event.ctrlKey && event.key === ',') {
    event.preventDefault(); window.chrome.webview.postMessage({type:'shortcut',key:'settings'});
  } else if (event.ctrlKey && ['q','j'].includes(event.key.toLowerCase())) {
    event.preventDefault(); window.chrome.webview.postMessage({type:'shortcut',key:event.key.toLowerCase()==='q'?'queue':'lyrics'});
  }
},true);
sendState();
