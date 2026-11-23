import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const p=await b.newPage({viewport:{width:1440,height:1200}});
let found=new Set();
for (const page of [1,2]) {
  await p.goto(`http://localhost:5210/catalog?search=`, {waitUntil:'networkidle',timeout:120000});
  await p.waitForTimeout(3000);
  if (page===2) {
    const btns=p.locator('.pager__btn');
    const n=await btns.count();
    for(let i=0;i<n;i++){ const t=(await btns.nth(i).innerText()).trim(); if(t==='2'){ await btns.nth(i).click(); break; } }
    await p.waitForTimeout(3000);
  }
  const names=await p.locator('.sw-card__name').allInnerTexts();
  names.forEach(n=>found.add(n.trim()));
}
console.log(`در مجموع ${found.size} نرم‌افزار در دو صفحه نمایش داده شد:`);
[...found].forEach(n=>console.log('   • '+n));
const want=['حسابداری فروغ آریا','انبارداری فروغ آریا','سامانهٔ مؤدیان','حقوق و دستمزد فروغ','دریافت و پرداخت فروغ','فروش و صدور فاکتور','دفترِ اداری'];
console.log('\nنرم‌افزارهایِ جدید:');
want.forEach(w=>console.log(`   ${[...found].some(f=>f.includes(w))?'✅':'❌'} ${w}`));
await b.close();
