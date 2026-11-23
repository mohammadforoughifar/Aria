import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});

async function probe(blockFonts){
  const ctx=await b.newContext({viewport:{width:1440,height:900}});
  if(blockFonts){ for(const p of ['**/*.woff2','**/*.woff','**/*.ttf']) await ctx.route(p,r=>r.abort()); }
  const p=await ctx.newPage();
  await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForSelector('.eco-card',{timeout:120000});
  await p.waitForTimeout(3500);
  const r=await p.evaluate(()=>{
    const cs=[...document.querySelectorAll('.eco-card')].map(c=>+c.getBoundingClientRect().height.toFixed(1));
    const st=document.querySelector('.eco__stage');
    return {cards:cs, stage:+st.getBoundingClientRect().height.toFixed(1),
            maxCard:Math.max(...cs), minCard:Math.min(...cs)};
  });
  await ctx.close();
  return r;
}
const withFonts = await probe(false);
const noFonts   = await probe(true);
console.log('■ ارتفاعِ کارت‌ها (پیکسل)');
console.log('  با فونتِ وب :', withFonts.cards.join(' '), '| صحنه:', withFonts.stage);
console.log('  بی‌فونت     :', noFonts.cards.join(' '), '| صحنه:', noFonts.stage);
console.log('  بیشینه/کمینه با فونت:', withFonts.maxCard, '/', withFonts.minCard);

// ترتیبِ بالا آمدن: فوتر کجا بود پیش از رندر؟
const ctx=await b.newContext({viewport:{width:1440,height:900}});
const p=await ctx.newPage();
await p.addInitScript(()=>{
  window.__shifts=[];
  new PerformanceObserver(l=>{for(const e of l.getEntries()){
    if(e.hadRecentInput) continue;
    window.__shifts.push({t:+e.startTime.toFixed(0), v:+e.value.toFixed(4),
      nodes:(e.sources||[]).map(s=>s.node?(s.node.tagName||'?').toLowerCase()+
        (s.node.className&&typeof s.node.className==='string'?'.'+s.node.className.trim().split(/\s+/)[0]:''):'?')});
  }}).observe({type:'layout-shift',buffered:true});
});
await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
await p.waitForSelector('.eco-card',{timeout:120000});
await p.waitForTimeout(4000);
const sh=await p.evaluate(()=>window.__shifts);
console.log('\n■ رخدادهایِ جابه‌جایی به ترتیبِ زمان');
sh.slice(0,12).forEach(s=>console.log(`   ${String(s.t).padStart(6)}ms  ${String(s.v).padEnd(8)} ${s.nodes.slice(0,3).join(' , ')}`));
console.log('   مجموع:', sh.reduce((a,x)=>a+x.v,0).toFixed(4));
await b.close();
