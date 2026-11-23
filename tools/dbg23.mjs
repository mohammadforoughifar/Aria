import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for (let i=1;i<=12;i++){
  const ctx=await b.newContext({viewport:{width:1440,height:900}});
  const p=await ctx.newPage();
  await p.addInitScript(()=>{window.__s=[];
    new PerformanceObserver(l=>{for(const e of l.getEntries()){ if(e.hadRecentInput) continue;
      window.__s.push({t:Math.round(e.startTime), v:+e.value.toFixed(4),
        src:(e.sources||[]).map(s=>{const n=s.node; if(!n) return '?';
          const cn=(typeof n.className==='string'&&n.className.trim())?'.'+n.className.trim().split(/\s+/).slice(0,2).join('.'):'';
          const id=n.id?'#'+n.id:'';
          return (n.tagName||'?').toLowerCase()+id+cn;}).slice(0,4)});
    }}).observe({type:'layout-shift',buffered:true});});
  await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForTimeout(9000);
  const s=await p.evaluate(()=>window.__s.sort((a,x)=>x.v-a.v));
  const tot=await p.evaluate(()=>+window.__s.reduce((a,x)=>a+x.v,0).toFixed(4));
  if (tot>=0.4){
    console.log(`\n★ اجرای ${i} — لغزشِ بزرگ، کل=${tot}`);
    s.slice(0,8).forEach(x=>console.log(`   ${String(x.v).padStart(8)} در ${String(x.t).padStart(6)}ms | ${x.src.join(' , ')}`));
    // وضعیتِ صفحه در آن لحظه
    const st=await p.evaluate(()=>{
      const boot=document.querySelector('.boot');
      return {boot: boot? (getComputedStyle(boot).display+'/'+getComputedStyle(boot).opacity) : 'نیست',
              mainH: Math.round((document.querySelector('main')?.getBoundingClientRect().height)||0),
              docH: Math.round(document.documentElement.scrollHeight)};
    });
    console.log('   وضعیت:', JSON.stringify(st));
    await ctx.close(); break;
  } else {
    console.log(`   اجرای ${i}: ${tot} — کوچک`);
    await ctx.close();
  }
}
await b.close();
