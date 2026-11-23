import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for (let i=1;i<=10;i++){
  const ctx=await b.newContext({viewport:{width:1440,height:900}});
  const p=await ctx.newPage();
  await p.addInitScript(()=>{
    window.__cls=0;
    new PerformanceObserver(l=>{for(const e of l.getEntries()){if(e.hadRecentInput)continue;window.__cls+=e.value;}})
      .observe({type:'layout-shift',buffered:true});
    window.__samples=[];
    const tick=()=>{
      const f=document.querySelector('.site-footer'), m=document.querySelector('main');
      window.__samples.push({t:Math.round(performance.now()),
        footTop: f? Math.round(f.getBoundingClientRect().top+window.scrollY) : null,
        mainH: m? Math.round(m.getBoundingClientRect().height) : null});
      if(performance.now()<4000) requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  });
  await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForTimeout(6000);
  const {cls, jumps}=await p.evaluate(()=>{
    const s=window.__samples; const j=[];
    for(let k=1;k<s.length;k++){
      const a=s[k-1], c=s[k];
      if(a.footTop!=null && c.footTop!=null && Math.abs(c.footTop-a.footTop)>50)
        j.push({t:c.t, from:a.footTop, to:c.footTop, d:c.footTop-a.footTop, mainA:a.mainH, mainB:c.mainH});
    }
    return {cls:+window.__cls.toFixed(4), jumps:j.slice(0,4)};
  });
  if (cls>=0.4){
    console.log(`\n★ اجرای ${i} — CLS ${cls}`);
    console.log('  پرش‌هایِ جایگاهِ پانویس (بیش از ۵۰پیکسل):');
    jumps.forEach(j=>console.log(`    در ${j.t}ms: پانویس از ${j.from} به ${j.to} (${j.d>0?'+':''}${j.d}) | بلندای main ${j.mainA} → ${j.mainB}`));
    await ctx.close(); break;
  } else { console.log(`   اجرای ${i}: ${cls} — کوچک`); await ctx.close(); }
}
await b.close();
