import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const ctx=await b.newContext({viewport:{width:1440,height:900}});
const p=await ctx.newPage();
await p.addInitScript(()=>{
  window.__sh=[];
  new PerformanceObserver(l=>{for(const e of l.getEntries()){
    if(e.hadRecentInput) continue;
    window.__sh.push({t:+e.startTime.toFixed(0), v:+e.value.toFixed(4),
      nodes:(e.sources||[]).map(s=>{const n=s.node; if(!n||!n.tagName) return '?';
        return n.tagName.toLowerCase()+(typeof n.className==='string'&&n.className?'.'+n.className.trim().split(/\s+/)[0]:'');})});
  }}).observe({type:'layout-shift',buffered:true});
});
await p.goto('http://localhost:5210/catalog',{waitUntil:'domcontentloaded',timeout:120000});
await p.waitForSelector('.orb__chip',{timeout:120000});
await p.waitForTimeout(9000);
const r=await p.evaluate(()=>window.__sh);
console.log('رخدادهایِ جابه‌جایی:');
r.forEach(s=>console.log(`   ${String(s.t).padStart(6)}ms  ${String(s.v).padEnd(8)}  ${[...new Set(s.nodes)].slice(0,4).join(' , ')}`));
console.log('مجموع:', r.reduce((a,x)=>a+x.v,0).toFixed(4));

// اندازهٔ سرصفحه در دو زمان
const h=await p.evaluate(()=>{
  const n=document.querySelector('.site-header__inner');
  const acts=document.querySelector('.header-actions');
  return {navW: n? +n.getBoundingClientRect().width.toFixed(0):null,
          actsW: acts? +acts.getBoundingClientRect().width.toFixed(0):null,
          actsH: acts? +acts.getBoundingClientRect().height.toFixed(0):null,
          actsHtml: acts? acts.textContent.trim().slice(0,60):null};
});
console.log('\nسرصفحه:', JSON.stringify(h));
await b.close();
