import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for (const path of ['/login','/register','/dashboard','/contact','/about']) {
  const ctx=await b.newContext({viewport:{width:1440,height:900}});
  const p=await ctx.newPage();
  await p.goto('http://localhost:5210'+path,{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForTimeout(3500);
  const r=await p.evaluate(()=>{
    const f=document.querySelector('.site-footer'), m=document.querySelector('main');
    return {footTop: f?Math.round(f.getBoundingClientRect().top+window.scrollY):null,
            mainH: m?Math.round(m.getBoundingClientRect().height):null,
            docH: Math.round(document.documentElement.scrollHeight),
            vw: document.documentElement.clientWidth,
            over: document.documentElement.scrollWidth - document.documentElement.clientWidth};
  });
  console.log(`   ${path.padEnd(12)} بلندای main=${String(r.mainH).padStart(5)} | پانویس در ${String(r.footTop).padStart(5)} | کلِ سند ${String(r.docH).padStart(5)} | سرریز ${r.over>1?'❌ '+r.over:'✅'}`);
  await ctx.close();
}
await b.close();
