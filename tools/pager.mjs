import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const p=await b.newPage({viewport:{width:1440,height:1200}});
await p.goto('http://localhost:5210/catalog',{waitUntil:'networkidle',timeout:120000});
await p.waitForTimeout(4000);
const btns=await p.locator('.pager__btn').allInnerTexts();
console.log('دکمه‌هایِ صفحه‌بندی:', JSON.stringify(btns));
const meta=await p.evaluate(async()=>{const r=await fetch('api/products?page=2&pageSize=12');const j=await r.json();
  return {page:j.meta.page,total:j.meta.totalCount,pages:j.meta.totalPages,items:j.items.length,
          names:j.items.map(x=>x.name).slice(0,4)};});
console.log('صفحهٔ ۲ از API:', JSON.stringify(meta,null,1));
// کلیک روی آخرین دکمه
const n=await p.locator('.pager__btn').count();
if(n>0){ await p.locator('.pager__btn').nth(n-1).click(); await p.waitForTimeout(3500);
  const names=await p.locator('.sw-card__name').allInnerTexts();
  console.log(`\nپس از کلیک روی آخرین دکمه — ${names.length} کارت:`);
  names.slice(0,12).forEach(x=>console.log('   • '+x.trim()));
}
await b.close();
