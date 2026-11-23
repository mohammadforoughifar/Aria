import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const ctx=await b.newContext({viewport:{width:1440,height:900}});
const p=await ctx.newPage();
await p.goto('http://localhost:5210/catalog',{waitUntil:'domcontentloaded',timeout:120000});
await p.waitForSelector('.orb',{state:'attached',timeout:120000});
await p.waitForTimeout(2500);
const r=await p.evaluate(()=>{
  const inner=document.querySelector('.hero__inner');
  const s=getComputedStyle(inner);
  const kids=[...inner.children].map(c=>({
    cls:c.className.toString().slice(0,40),
    w:+c.getBoundingClientRect().width.toFixed(1),
    order:getComputedStyle(c).order,
  }));
  // آیا قانونِ ما اعمال شده؟
  const matched=[];
  for(const sheet of document.styleSheets){
    try{
      for(const rule of sheet.cssRules){
        if(rule.selectorText && /hero__inner/.test(rule.selectorText))
          matched.push(rule.selectorText+' { '+rule.style.cssText.slice(0,90)+' }');
      }
    }catch(e){}
  }
  return {display:s.display, cols:s.gridTemplateColumns, gap:s.gap, kids, matched};
});
console.log('display:', r.display);
console.log('ستون‌ها :', r.cols);
console.log('فاصله   :', r.gap);
console.log('فرزندان :'); r.kids.forEach(k=>console.log(`   ${k.cls.padEnd(42)} عرض=${k.w}  order=${k.order}`));
console.log('\nقانون‌هایِ یافت‌شده برای .hero__inner:');
r.matched.forEach(m=>console.log('   '+m));
await b.close();
