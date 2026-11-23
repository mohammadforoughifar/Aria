import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const ctx=await b.newContext({viewport:{width:1440,height:900}});
const p=await ctx.newPage();
await p.goto('http://localhost:5210/catalog',{waitUntil:'domcontentloaded',timeout:120000});
await p.waitForSelector('.cat-card',{timeout:120000});
await p.waitForTimeout(3500);
const r=await p.evaluate(()=>{
  const g=document.querySelector('.cat-grid');
  const gs=getComputedStyle(g);
  const gr=g.getBoundingClientRect();
  const cards=[...document.querySelectorAll('.cat-card')].slice(0,3).map(c=>{
    const b=c.getBoundingClientRect(); const s=getComputedStyle(c);
    return {top:+b.top.toFixed(1), left:+b.left.toFixed(1), w:+b.width.toFixed(1), h:+b.height.toFixed(1),
            display:s.display, width:s.width, gridCol:s.gridColumn};
  });
  // والدین
  const chain=[]; let el=g;
  while(el && el!==document.body){
    const s=getComputedStyle(el), b=el.getBoundingClientRect();
    chain.push({sel:el.tagName.toLowerCase()+'.'+(el.className||'').toString().trim().split(/\s+/)[0],
                w:+b.width.toFixed(0), display:s.display, cols:s.gridTemplateColumns});
    el=el.parentElement;
  }
  return {
    gridCols: gs.gridTemplateColumns, gridDisplay: gs.display, gap: gs.gap,
    gridRect:[+gr.left.toFixed(0),+gr.top.toFixed(0),+gr.width.toFixed(0),+gr.height.toFixed(0)],
    cards, chain: chain.slice(0,5),
  };
});
console.log('.cat-grid  display =', r.gridDisplay);
console.log('.cat-grid  ستون‌ها =', r.gridCols);
console.log('.cat-grid  مستطیل =', JSON.stringify(r.gridRect));
console.log('\nسه کارتِ نخست:');
r.cards.forEach(c=>console.log('   ', JSON.stringify(c)));
console.log('\nزنجیرهٔ والدین:');
r.chain.forEach(c=>console.log(`   ${c.sel.padEnd(28)} عرض=${c.w} display=${c.display} cols=${c.cols}`));
await b.close();
