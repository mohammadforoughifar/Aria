import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for (const [path,name] of [['/catalog','فهرست'],['/','اصلی'],['/systems','سیستم‌ها'],['/blog','وبلاگ'],['/solutions','راهکارها']]) {
  for (const w of [1440, 390]) {
    const ctx=await b.newContext({viewport:{width:w,height:1000},deviceScaleFactor:1});
    const p=await ctx.newPage();
    await p.goto('http://localhost:5210'+path,{waitUntil:'domcontentloaded',timeout:120000});
    await p.waitForTimeout(4000);
    await p.evaluate(()=>window.scrollTo(0, 700));
    await p.waitForTimeout(900);
    await p.screenshot({path:`/home/user/shots/lists/${name}-${w}.png`});
    await ctx.close();
  }
}
await b.close(); console.log('تصویرها ذخیره شد');
