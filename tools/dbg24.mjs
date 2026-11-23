import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const OLD='.wrap{display:flex!important;flex-wrap:wrap!important}';
async function runs(inject, n){
  const v=[];
  for(let i=0;i<n;i++){
    const ctx=await b.newContext({viewport:{width:1440,height:900}});
    const p=await ctx.newPage();
    await p.addInitScript(()=>{window.__cls=0;
      new PerformanceObserver(l=>{for(const e of l.getEntries()){if(e.hadRecentInput)continue;window.__cls+=e.value;}})
        .observe({type:'layout-shift',buffered:true});});
    if(inject) await p.addInitScript(()=>{document.addEventListener('DOMContentLoaded',()=>{
      const s=document.createElement('style'); s.textContent='.wrap{display:flex!important;flex-wrap:wrap!important}';
      document.head.appendChild(s);});});
    await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
    await p.waitForTimeout(9000);
    v.push(await p.evaluate(()=>+window.__cls.toFixed(4)));
    await ctx.close();
  }
  return v;
}
const N=8;
for (const [label,inject] of [['جدید (بدون flex)',false],['قدیم (با flex)',true]]) {
  const v=await runs(inject,N);
  const mean=(v.reduce((a,x)=>a+x,0)/v.length);
  const bad=v.filter(x=>x>=0.05).length;
  console.log(`${label.padEnd(20)} میانگین ${mean.toFixed(4)} | میانه ${v.slice().sort((a,x)=>a-x)[Math.floor(N/2)]} | ${bad} از ${N} بالای ۰/۰۵`);
  console.log(`   ${v.join('  ')}`);
}
await b.close();
