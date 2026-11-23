import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const vals=[];
for (let i=1;i<=6;i++){
  const ctx=await b.newContext({viewport:{width:1440,height:900}});
  const p=await ctx.newPage();
  await p.addInitScript(()=>{window.__cls=0;
    new PerformanceObserver(l=>{for(const e of l.getEntries()){if(e.hadRecentInput)continue;window.__cls+=e.value;}})
      .observe({type:'layout-shift',buffered:true});});
  await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForTimeout(9000);
  const c=await p.evaluate(()=>+window.__cls.toFixed(4));
  vals.push(c);
  console.log(`   اجرای ${i}: ${c}`);
  await ctx.close();
}
const bad=vals.filter(v=>v>=0.05).length;
console.log(`\nمیانگین ${(vals.reduce((a,x)=>a+x,0)/vals.length).toFixed(4)} | بیشینه ${Math.max(...vals)} | ${bad} از ${vals.length} اجرا بالایِ ۰/۰۵`);
await b.close();
