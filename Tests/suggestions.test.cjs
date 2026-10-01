const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const vm=require('node:vm');
let messages=[],requests=[];
const context={AbortController,setTimeout,clearTimeout,window:{ytcfg:{get:()=>({client:{clientName:'WEB_REMIX'}})},chrome:{webview:{postMessage:m=>messages.push(m)}}},fetch:(url,options)=>new Promise(resolve=>requests.push({url,options,resolve}))};
vm.createContext(context);
vm.runInContext(fs.readFileSync(path.join(__dirname,'../WebView/Scripts/suggestions.js'),'utf8'),context);
function response(text){return {ok:true,json:async()=>({contents:[{searchSuggestionsSectionRenderer:{contents:[{searchSuggestionRenderer:{suggestion:{runs:[{text}]}}},{searchSuggestionRenderer:{suggestion:{runs:[{text}]}}}]}}]})};}
(async()=>{
const first=context.requestSuggestions('one');
const second=context.requestSuggestions('two');
assert.equal(requests[0].options.signal.aborted,true);
assert.equal(requests[1].url,'/youtubei/v1/music/get_search_suggestions?prettyPrint=false');
assert.equal(JSON.parse(requests[1].options.body).input,'two');
requests[1].resolve(response('two songs'));await second;
assert.equal(messages.at(-1).items.length,1);assert.equal(messages.at(-1).items[0],'two songs');
requests[0].resolve(response('old response'));await first;
assert.equal(messages.at(-1).query,'two');
await context.requestSuggestions('');assert.equal(messages.at(-1).items.length,0);
const failure=context.requestSuggestions('offline');requests.at(-1).resolve({ok:false});await failure;assert.equal(messages.at(-1).items.length,0);
console.log('PASS: same-origin suggestions, deduplication, cancellation, stale responses, clearing, errors');
})().catch(error=>{console.error(error);process.exitCode=1;});
