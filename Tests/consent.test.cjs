const assert=require('node:assert/strict');const vm=require('node:vm');const fs=require('node:fs');const path=require('node:path');const script=fs.readFileSync(path.join(__dirname,'../WebView/Scripts/consent.js'),'utf8');
let clicks=0;const button=text=>({innerText:text,getClientRects:()=>[{}],click(){clicks++;}});
const run=(host,buttons)=>vm.runInNewContext(script,{location:{hostname:host,protocol:'https:'},document:{querySelectorAll:()=>buttons}});
assert.equal(run('consent.youtube.com',[button('Godkänn alla'),button('Avvisa alla')]),true);assert.equal(clicks,1);
assert.equal(run('consent.google.com',[button('Reject all')]),true);assert.equal(clicks,2);
assert.equal(run('consent.youtube.com',[button('Accept all')]),false);assert.equal(clicks,2);
assert.equal(run('example.invalid',[button('Reject all')]),false);assert.equal(clicks,2);
assert.equal(run('consent.youtube.com',[]),false);
console.log('PASS: consent rejects only on trusted hosts, never accepts, safely falls back without a matching button');
