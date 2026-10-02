const assert=require('node:assert/strict');const fs=require('node:fs');const vm=require('node:vm');const path=require('node:path');
function fixture(state='running') {
  let sources=0,filters=[],gains=[],element={currentSrc:'blob:https://music.youtube.com/fixture'},handlers={};
  const param=()=>({value:0,setTargetAtTime(v){this.value=v;}});
  class AudioContext {
    constructor(){this.state=state;this.currentTime=0;this.destination={};}
    resume(){return this.state==='running'?Promise.resolve():new Promise(()=>{});}
    createBiquadFilter(){const n={gain:param(),frequency:param(),Q:param(),connect(){}};filters.push(n);return n;}
    createGain(){const n={gain:param(),connect(){}};gains.push(n);return n;}
    createMediaElementSource(){sources++;return {connect(){}};}
  }
  const context={URL,AudioContext,location:{origin:'https://music.youtube.com'},find:()=>element,document:{addEventListener:(name,fn)=>handlers[name]=fn}};
  vm.createContext(context);vm.runInContext(fs.readFileSync(path.join(__dirname,'../WebView/Scripts/equalizer.js'),'utf8'),context);
  return {context,filters,gains,handlers,get sources(){return sources;},setElement:e=>element=e};
}
(async()=>{
const f=fixture();f.context.configureEqualizer({enabled:false,gains:[12,0,0,0,0,0]});assert.equal(f.sources,0);
f.context.configureEqualizer({enabled:true,gains:[8,4,-4,0,2,3]});assert.equal(f.sources,1);assert.deepEqual(f.filters.map(n=>n.type),['lowshelf','peaking','peaking','peaking','peaking','highshelf']);assert.deepEqual(f.filters.map(n=>n.gain.value),[8,4,-4,0,2,3]);assert.ok(f.gains[0].gain.value<1);assert.equal(f.context.equalizerStatus(),'Equalizer active.');
f.context.configureEqualizer({enabled:true,gains:[100,NaN,-100,0,0,0]});assert.equal(f.sources,1);assert.deepEqual(f.filters.map(n=>n.gain.value),[12,0,-12,0,0,0]);
f.context.configureEqualizer({enabled:false});assert.deepEqual(f.filters.map(n=>n.gain.value),[0,0,0,0,0,0]);assert.equal(f.gains[0].gain.value,1);
f.setElement({currentSrc:'https://untrusted.invalid/audio'});f.context.configureEqualizer({enabled:true});assert.equal(f.sources,1);assert.match(f.context.equalizerStatus(),/waiting/);
f.setElement({currentSrc:'blob:https://music.youtube.com/second'});await f.context.attachEqualizer();assert.equal(f.sources,2);
const suspended=fixture('suspended');suspended.context.configureEqualizer({enabled:true});assert.equal(suspended.sources,0);assert.match(suspended.context.equalizerStatus(),/Start playback/);suspended.handlers.pointerdown();assert.equal(suspended.sources,0);
console.log('PASS: six bands, gain limits, headroom, neutral bypass, source reuse/replacement, cross-origin and suspended-context protection');
})().catch(e=>{console.error(e);process.exitCode=1;});
