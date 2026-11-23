import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const CANDIDATES = {
  'معمولی': '',
  'min-height روی main': 'main{min-height:calc(100vh - var(--header-h))!important}',
};
for (const [label, css] of Object.entries(CANDIDATES)) {
  const v=[];
  for(let i=0;i<8;i++){
    const ctx=await b.newContext({viewport:{width:1440,height:900}});
    const p=await ctx.newPage();
    await p.addInitScript((css)=>{
      window.__cls=0;
      new PerformanceObserver(l=>{for(const e of l.getEntries()){if(e.hadRecentInput)continue;window.__cls+=e.value;}})
        .observe({type:'layout-shift',buffered:true});
      if(!css) return;
      const add=()=>{const s=document.createElement('style'); s.textContent=css; document.head.appendChild(s);};
      if(document.head) add(); else document.addEventListener('DOMContentLoaded',add);
    }, css);
    await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
    await p.waitForTimeout(9000);
    v.push(await p.evaluate(()=>+window.__cls.toFixed(4)));
    await ctx.close();
  }
  const m=v.reduce((a,x)=>a+x,0)/v.length, bad=v.filter(x=>x>=0.05).length;
  console.log(`${label.padEnd(24)} میانگین ${m.toFixed(4)} | ${bad} از 8 بالای ۰/۰۵ | ${v.join(' ')}`);
}
await b.close();
