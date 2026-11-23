import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
async function trial(hide, n){
  const v=[];
  for(let i=0;i<n;i++){
    const ctx=await b.newContext({viewport:{width:1440,height:900}});
    const p=await ctx.newPage();
    await p.addInitScript((hide)=>{
      window.__cls=0;
      new PerformanceObserver(l=>{for(const e of l.getEntries()){if(e.hadRecentInput)continue;window.__cls+=e.value;}})
        .observe({type:'layout-shift',buffered:true});
      if(!hide) return;
      const s=document.createElement('style'); s.id='hid';
      s.textContent='#app{visibility:hidden !important}';
      (document.head||document.documentElement).appendChild(s);
      // پس از ۲/۵ ثانیه (نشستنِ محتوا) آشکار کن
      setTimeout(()=>{ const e=document.getElementById('hid'); if(e) e.remove(); }, 2500);
    }, hide);
    await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
    await p.waitForTimeout(9000);
    v.push(await p.evaluate(()=>+window.__cls.toFixed(4)));
    await ctx.close();
  }
  return v;
}
const N=8;
const a=await trial(false,N), c=await trial(true,N);
const fmt=(l,v)=>{const m=v.reduce((x,y)=>x+y,0)/v.length; const bad=v.filter(x=>x>=0.05).length;
  console.log(`${l.padEnd(24)} میانگین ${m.toFixed(4)} | ${bad} از ${N} بالای ۰/۰۵ | ${v.join(' ')}`);};
fmt('معمولی', a);
fmt('#app پنهان تا نشستن', c);
await b.close();
