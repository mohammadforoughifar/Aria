import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const cases=[['/blog',768],['/blog',390],['/solutions',360],['/contact',360]];
const OLD = '.wrap{display:flex!important;flex-wrap:wrap!important}';
console.log('صفحه        عرض | بدونِ قانونِ قدیم | با قانونِ قدیم (flex)');
console.log('------------+-----+--------------------+----------------------');
for (const [path,w] of cases) {
  const res=[];
  for (const inject of [false,true]) {
    const ctx=await b.newContext({viewport:{width:w,height:900}});
    const p=await ctx.newPage();
    await p.goto('http://localhost:5210'+path,{waitUntil:'domcontentloaded',timeout:120000});
    await p.waitForTimeout(2800);
    if (inject) await p.addStyleTag({content:OLD});
    await p.waitForTimeout(400);
    const r=await p.evaluate(()=>({vw:document.documentElement.clientWidth, sw:document.documentElement.scrollWidth}));
    res.push(r.sw - r.vw);
    await ctx.close();
  }
  console.log(`${path.padEnd(12)} ${String(w).padStart(4)} | ${String(res[0]).padStart(18)} | ${String(res[1]).padStart(20)}`);
}
await b.close();
