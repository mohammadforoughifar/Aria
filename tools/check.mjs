import { chromium } from 'playwright-core';
const B='http://localhost:5210';
const b=await chromium.launch({args:['--no-sandbox']});
const p=await b.newPage({viewport:{width:1440,height:1200}});
const errs=[];
p.on('pageerror',e=>errs.push('PAGEERROR: '+e.message.slice(0,110)));
p.on('console',m=>{if(m.type()==='error'&&!/dotnet\.wasm|ERR_CONNECTION|Failed to load/.test(m.text()))errs.push(m.text().slice(0,110));});
await p.goto(B+'/catalog',{waitUntil:'networkidle',timeout:120000});
await p.waitForTimeout(4500);
const body=await p.innerText('body');
console.log('── نرم‌افزارهایِ جدید در صفحهٔ «نرم‌افزارها» ──');
['حسابداری فروغ آریا','انبارداری فروغ آریا','سامانهٔ مؤدیان','حقوق و دستمزد فروغ','دریافت و پرداخت فروغ','فروش و صدور فاکتور','دفترِ اداری'].forEach(n=>
  console.log(`   ${body.includes(n)?'✅':'❌'} ${n}`));
console.log(`\n   کارت‌ها در صفحه: ${await p.locator('.sw-card').count()}`);
console.log(`   کارت‌های گروه  : ${await p.locator('.cat-card').count()}`);
// صفحهٔ ۲ (بُردارِ صفحه‌بندی حالا معنا دارد)
const pg=p.locator('.pager__btn');
if(await pg.count()>1){ await pg.nth(1).click(); await p.waitForTimeout(2500);
  console.log(`   صفحهٔ ۲       : ${await p.locator('.sw-card').count()} کارت ✅`); }
await p.goto(B+'/blog',{waitUntil:'domcontentloaded'}); await p.waitForTimeout(2500);
console.log(`\n   وبلاگ: ${(await p.innerText('body')).trim().length} کاراکتر`);
console.log(`\nخطاها: ${errs.length?[...new Set(errs)].slice(0,4).join(' | ')+' ❌':'هیچ ✅'}`);
await b.close();
