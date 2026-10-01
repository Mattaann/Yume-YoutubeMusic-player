let suggestionRequest = null;
let suggestionSequence = 0;
async function requestSuggestions(query) {
  const sequence=++suggestionSequence;
  suggestionRequest?.abort();
  query=typeof query==='string'?query.trim().slice(0,200):'';
  const publish=items=>{if(sequence===suggestionSequence)window.chrome.webview.postMessage({type:'suggestions',query,items});};
  if(!query) { publish([]);return; }
  const context=window.ytcfg?.get?.('INNERTUBE_CONTEXT');
  if(!context) { publish([]);return; }
  suggestionRequest=new AbortController();
  const controller=suggestionRequest;
  const timeout=setTimeout(()=>controller.abort(),5000);
  try {
    const response=await fetch('/youtubei/v1/music/get_search_suggestions?prettyPrint=false',{
      method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/json'},
      body:JSON.stringify({context,input:query}),signal:controller.signal
    });
    if(!response.ok) {publish([]);return;}
    const result=await response.json();
    const items=[];
    for(const section of result.contents||[]) {
      for(const entry of section.searchSuggestionsSectionRenderer?.contents||[]) {
        const suggestion=entry.searchSuggestionRenderer;
        const text=suggestion?.suggestion?.runs?.map(run=>run.text||'').join('') || suggestion?.navigationEndpoint?.searchEndpoint?.query;
        if(typeof text==='string' && text.trim() && !items.includes(text.trim()))items.push(text.trim().slice(0,200));
        if(items.length===10)break;
      }
      if(items.length===10)break;
    }
    publish(items);
  } catch { publish([]); }
  finally {clearTimeout(timeout);}
}
