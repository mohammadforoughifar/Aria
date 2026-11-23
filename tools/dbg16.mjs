import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const widths=[1920,1440,1280,1024,900,768,600,390];
console.log('عرض  | دسته‌ها | نرم‌افزار | کمینه‌پهنایِ کارت | سرریز | هم‌پوشانی');
console.log('-----+---------+-----------+-------------------+--------+----------');
for (const w of widths) {
  const ctx=await b.newContext({viewport:{width:w,height:1000}});
  const p=await ctx.newPage();
  await p.goto('http://localhost:5210/catalog',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForSelector('.cat-card',{timeout:120000});
  await p.waitForTimeout(3000);
  const r=await p.evaluate(()=>{
    const rowCount = (sel)=>{
      const els=[...document.querySelectorAll(sel)];
      if(!els.length) return 0;
      const tops={}; els.forEach(e=>{const t=Math.round(e.getBoundingClientRect().top/4)*4; tops[t]=(tops[t]||0)+1;});
      return Math.max(...Object.values(tops));
    };
    const minW = (sel)=>{
      const els=[...document.querySelectorAll(sel)];
      return els.length? Math.round(Math.min(...els.map(e=>e.getBoundingClientRect().width))) : 0;
    };
    // هم‌پوشانی میان کارت‌های یک شبکه
    let overlap=0;
    for (const g of document.querySelectorAll('.cat-grid,.sw-grid,.prod-grid,.grid-3,[class*=grid]')) {
      const kids=[...g.children].map(k=>k.getBoundingClientRect()).filter(r=>r.width>0&&r.height>0);
      for(let i=0;i<kids.length;i++) for(let j=i+1;j<kids.length;j++){
        const a=kids[i],c=kids[j];
        const ox=Math.max(0,Math.min(a.right,c.right)-Math.max(a.left,c.left));
        const oy=Math.max(0,Math.min(a.bottom,c.bottom)-Math.max(a.top,c.top));
        if(ox>1&&oy>1) overlap++;
      }
    }
    return {
      cats:rowCount('.cat-card'), sws:rowCount('.sw-card'),
      minCard: Math.min(minW('.cat-card'), minW('.sw-card')),
      overflow: document.documentElement.scrollWidth - window.innerWidth,
      overlap,
    };
  });
  const bad = r.overflow>1 ? '❌ '+r.overflow : '✅';
  const ov  = r.overlap>0 ? '❌ '+r.overlap : '✅';
  console.log(`${String(w).padEnd(5)}| ${String(r.cats).padStart(7)} | ${String(r.sws).padStart(9)} | ${String(r.minCard).padStart(17)} | ${bad.padEnd(6)} | ${ov}`);
  await ctx.close();
}
await b.close();
