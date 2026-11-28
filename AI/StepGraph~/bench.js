// the test set: per view, after the search settles: crossings, turns, straight lines, one-neighbour boxes with lines close by
const {chromium}=require('playwright');(async()=>{const b=await chromium.launch();
const dev=process.argv[2], fl=process.argv[3]||'center';
const p=await (await b.newContext(dev==='phone'?{viewport:{width:384,height:675},isMobile:true,hasTouch:true}:{viewport:{width:1300,height:950}})).newPage();p.on('pageerror',e=>console.log('ERR',e.message));
await p.goto('file://'+process.cwd()+'/'+(dev==='phone'?'phone-test.html':'out.html'));await p.waitForTimeout(800);
if(process.argv[5])await p.evaluate(t=>Object.assign(RT,JSON.parse(t)),process.argv[5]);if(process.argv[6])await p.evaluate(t=>window.LRB=JSON.parse(t),process.argv[6]);if(process.argv[7])await p.evaluate(t=>window.SW=JSON.parse(t),process.argv[7]);await p.click(`#flow button[data-l="${fl}"]`);await p.click('#fold button[data-f="off"]');
let tot={cross:0,bends:0,near:0,len:0,labels:0,total:0};const rows=[];
for(const [st,pt] of [['Update','GameManager'],['Update','Player'],['Update','Enemy'],['Trigger','HitBox'],['Trigger','Loot'],['LateUpdate','BuffEffect']]){
  await p.locator('#tabs button',{hasText:new RegExp('^'+st)}).click();await p.locator('#parts button',{hasText:new RegExp('^'+pt)}).first().click();await p.waitForFunction(()=>!layout.job||layout.stop, null, {timeout:120000}).catch(()=>{});await p.waitForTimeout(300);
  const r=await p.evaluate(()=>{const between=edges.filter(e=>!e.route.inner).map(e=>({e,r:e.route}));const res=orthoRoute(between);const s=routeScore(res);return {cross:s.cross,bends:s.bends,near:s.near,labels:s.labels,len:Math.round(s.len),total:Math.round(s.total),straight:res.filter(o=>o.straight).length,lines:res.length}});
  for(const k in tot)tot[k]+=r[k];rows.push(`${st}/${pt}: cross ${r.cross}, turns ${r.bends}, straight ${r.straight}/${r.lines}, boxed-in ${r.near}, labels on boxes ${r.labels}, score ${r.total}`);
  if(process.argv[4])await p.locator('#scroll').screenshot({path:`b-${dev}-${fl}-${pt}.png`});
}
console.log(`== ${dev} ${fl}: TOTAL cross ${tot.cross}, turns ${tot.bends}, length ${tot.len}, boxed-in ${tot.near}, labels on boxes ${tot.labels}, score ${Math.round(tot.total)}\n  `+rows.join('\n  '));
await b.close()})()
