const assert=require('node:assert/strict');
const fs=require('node:fs');const vm=require('node:vm');const path=require('node:path');
let calls=[],handlers={},result={},fail=false;
const context={URL,TextEncoder,Uint8Array,Date,console,location:{origin:'https://music.youtube.com'},window:{ytcfg:{get:k=>k==='INNERTUBE_CONTEXT'?{client:{clientName:'WEB_REMIX'}}:0}},document:{cookie:'',addEventListener:(k,f)=>handlers[k]=f,querySelector:()=>null,querySelectorAll:()=>[]},fetch:async (url,options)=>{calls.push({url,body:JSON.parse(options.body)});return {ok:!fail,json:async()=>result};}};
vm.createContext(context);vm.runInContext(fs.readFileSync(path.join(__dirname,'../WebView/Scripts/library.js'),'utf8'),context);
(async()=>{
result={header:{activeAccountHeaderRenderer:{accountName:{simpleText:'Fixture account'},email:{simpleText:'fixture@example.invalid'},accountPhoto:{thumbnails:[{url:'https://example.invalid/avatar'}]}}}};
let account=await context.readAccount();assert.equal(account.signedIn,true);assert.equal(account.name,'Fixture account');
result={items:[{musicTwoRowItemRenderer:{title:{runs:[{text:'Fixture playlist'}]},navigationEndpoint:{browseEndpoint:{browseId:'VLPLfixture'}}}},{musicTwoRowItemRenderer:{title:{simpleText:'Album'},navigationEndpoint:{browseEndpoint:{browseId:'MPREalbum'}}}}]};
let library=await context.readLibraryPlaylists();assert.equal(library.items.length,1);assert.equal(library.items[0].id,'PLfixture');
let count=calls.length;assert.match(await context.addDraggedSong('PLfixture','abcdefghijk'),/Drag a song/);assert.equal(calls.length,count);
let data;handlers.dragstart({target:{closest:selector=>selector.startsWith('a')?{href:'https://music.youtube.com/watch?v=abcdefghijk'}:null},dataTransfer:{setData:(format,value)=>data=value}});assert.equal(data,'https://music.youtube.com/watch?v=abcdefghijk');
result={status:'STATUS_SUCCEEDED'};assert.equal(await context.addDraggedSong('PLfixture','abcdefghijk'),'Song added to playlist.');assert.equal(calls.at(-1).body.actions[0].addedVideoId,'abcdefghijk');
count=calls.length;assert.match(await context.addDraggedSong('PLfixture','abcdefghijk'),/Drag a song/);assert.equal(calls.length,count);
handlers.dragstart({target:{closest:selector=>selector.startsWith('a')?{href:'https://music.youtube.com/watch?v=abcdefghijk'}:null},dataTransfer:{setData(){}}});result={status:'STATUS_FAILED'};assert.match(await context.addDraggedSong('PLfixture','abcdefghijk'),/did not add/);
fail=true;library=await context.readLibraryPlaylists();assert.ok(library.error);account=await context.readAccount();assert.equal(account.signedIn,false);
context.window.ytcfg.get=k=>k==='LOGGED_IN'?false:{client:{clientName:'WEB_REMIX'}};count=calls.length;library=await context.readLibraryPlaylists();assert.equal(library.error,'Log in to see your playlists.');assert.equal(calls.length,count);
console.log('PASS: account parsing, playlist filtering, drag validation, one add per drag, confirmed success and API failure handling');
})().catch(e=>{console.error(e);process.exitCode=1;});

