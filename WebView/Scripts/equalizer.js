const equalizer = { enabled:false, gains:[0,0,0,0,0,0], context:null, graphs:new WeakMap(), active:null, pending:false };
const eqClamp = value => Number.isFinite(value) ? Math.max(-12,Math.min(12,value)) : 0;
function updateEqualizerGraph(graph) {
  const state=equalizer;
  const values=state.enabled?state.gains:[0,0,0,0,0,0];
  const now=state.context.currentTime;
  graph.filters.forEach((node,i)=>node.gain.setTargetAtTime(values[i],now,.04));
  const headroom=values.reduce((sum,gain)=>sum+Math.max(0,gain),0);
  graph.preamp.gain.setTargetAtTime(Math.pow(10,-headroom/20),now,.04);
}
function equalizerSourceSafe(element) {
  const src=element?.currentSrc || element?.src;
  if(!src)return false;
  try {const url=new URL(src,location.origin);return url.origin===location.origin || (!!element.crossOrigin && url.protocol==='https:');}catch{return false;}
}
async function attachEqualizer() {
  const state=equalizer;
  if(state.pending)return;
  const element=find('media');
  if(!element)return;
  let graph=state.graphs.get(element);
  if(graph){state.active=graph;updateEqualizerGraph(graph);if(state.context.state==='suspended')state.context.resume().catch(()=>{});return;}
  if(!state.enabled || !equalizerSourceSafe(element))return;
  state.pending=true;
  try {
    state.context ??= new AudioContext();
    if(state.context.state!=='running') {
      state.context.resume().then(()=>{if(state.context.state==='running')void attachEqualizer();}).catch(()=>{});
      return;
    }
    if(state.context.state!=='running' || element!==find('media') || !equalizerSourceSafe(element) || !state.enabled)return;
    const ctx=state.context;
    const filters=[30,120,350,1200,3500,9000].map((frequency,index)=>{
      const node=ctx.createBiquadFilter();node.type=index===0?'lowshelf':index===5?'highshelf':'peaking';node.frequency.value=frequency;node.Q.value=.7;return node;
    });
    const preamp=ctx.createGain();
    preamp.connect(filters[0]);filters.forEach((node,i)=>node.connect(filters[i+1]||ctx.destination));
    const source=ctx.createMediaElementSource(element);
    source.connect(preamp);
    graph={source,preamp,filters};state.graphs.set(element,graph);state.active=graph;
    updateEqualizerGraph(graph);
  } catch { }
  finally {state.pending=false;}
}
function configureEqualizer(options) {
  equalizer.enabled=options?.enabled===true;
  equalizer.gains=Array.from({length:6},(_,i)=>eqClamp(options?.gains?.[i]));
  if(equalizer.active)updateEqualizerGraph(equalizer.active);
  void attachEqualizer();
}
function equalizerStatus() {
  if(!equalizer.enabled)return 'Equalizer off.';
  const element=find('media');
  if(element && equalizer.graphs.has(element) && equalizer.context?.state==='running')return 'Equalizer active.';
  if(element && !equalizerSourceSafe(element))return 'Equalizer is waiting for a compatible audio stream. Original audio is unchanged.';
  return 'Equalizer saved. Start playback or click inside the music view to activate it.';
}
document.addEventListener('play',()=>void attachEqualizer(),true);
document.addEventListener('loadedmetadata',()=>void attachEqualizer(),true);
document.addEventListener('pointerdown',()=>void attachEqualizer(),true);
document.addEventListener('keydown',()=>void attachEqualizer(),true);
