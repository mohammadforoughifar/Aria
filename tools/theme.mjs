import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});

// شبیه‌سازیِ سیستم‌عاملی که حالتِ تاریک را ترجیح می‌دهد (وضعیتِ شما)
const ctx=await b.newContext({ colorScheme:'dark' });
const p=await ctx.newPage({viewport:{width:1440,height:1000}});
await p.goto('http://localhost:5210/',{waitUntil:'networkidle',timeout:120000});
await p.waitForTimeout(4000);
const r=await p.evaluate(()=>({
  theme: document.documentElement.getAttribute('data-theme'),
  body: getComputedStyle(document.body).backgroundColor,
  isDark: window.nw ? window.nw.isDark() : '—',
}));
console.log('■ سیستم‌عامل روی «تاریک»، کاربر هنوز چیزی انتخاب نکرده:');
console.log(`   data-theme     : ${r.theme}`);
console.log(`   رنگِ پس‌زمینه  : ${r.body}  ${r.body==='rgb(255, 255, 255)'?'✅ سفیدِ خالص':'❌'}`);
console.log(`   nw.isDark()    : ${r.isDark}`);

// چند صفحهٔ دیگر
for (const u of ['/catalog','/systems','/blog']) {
  await p.goto('http://localhost:5210'+u,{waitUntil:'domcontentloaded'});
  await p.waitForTimeout(2500);
  const bg=await p.evaluate(()=>getComputedStyle(document.body).backgroundColor);
  console.log(`   ${u.padEnd(10)} → ${bg} ${bg==='rgb(255, 255, 255)'?'✅':'❌'}`);
}

// دکمهٔ تغییرِ تم هنوز کار می‌کند؟
await p.goto('http://localhost:5210/',{waitUntil:'networkidle'}); await p.waitForTimeout(3000);
const before=await p.evaluate(()=>getComputedStyle(document.body).backgroundColor);
await p.evaluate(()=>window.nw.setTheme('dark'));
await p.waitForTimeout(1200);
const after=await p.evaluate(()=>getComputedStyle(document.body).backgroundColor);
console.log(`\n■ تغییرِ تم با دکمه:`);
console.log(`   پیش: ${before}  →  پس: ${after}  ${after!==before?'✅ کار می‌کند':'❌'}`);
// برگشت به روشن
await p.evaluate(()=>window.nw.setTheme('light'));
await p.waitForTimeout(1200);
console.log(`   بازگشت به روشن: ${await p.evaluate(()=>getComputedStyle(document.body).backgroundColor)}`);
await b.close();
