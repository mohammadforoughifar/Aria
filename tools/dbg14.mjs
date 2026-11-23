import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const ctx=await b.newContext({viewport:{width:1440,height:900}});
const p=await ctx.newPage();
const targets=[
  ['/',            ['.cat-card','.prod-card','.card','.grid-3 > *','.eco-card']],
  ['/catalog',     ['.cat-card','.sw-card']],
  ['/pricing',     ['.plan-card','.price-card','.card']],
  ['/blog',        ['.post-card','.article-card','.card']],
  ['/solutions',   ['.card','.cat-card']],
  ['/systems',     ['.card','.cat-card']],
];
for (const [path, sels] of targets) {
  await p.goto('http://localhost:5210'+path,{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForTimeout(3500);
  const r = await p.evaluate((sels)=>{
    const out={};
    for(const s of sels){
      const els=[...document.querySelectorAll(s)];
      if(els.length<2) continue;
      const tops={}; els.forEach(e=>{const t=Math.round(e.getBoundingClientRect().top/5)*5; tops[t]=(tops[t]||0)+1;});
      const per=Object.values(tops);
      out[s]={n:els.length, maxPerRow:Math.max(...per), rows:per.length};
    }
    return out;
  }, sels);
  console.log(`\n■ ${path}`);
  for(const [k,v] of Object.entries(r)) console.log(`   ${k.padEnd(16)} ${String(v.n).padStart(3)} مورد | بیشینه در یک ردیف: ${v.maxPerRow} | ${v.rows} ردیف`);
  if(!Object.keys(r).length) console.log('   (چیزی یافت نشد)');
}
await b.close();
