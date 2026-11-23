import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const ctx=await b.newContext({viewport:{width:1440,height:900}});
const p=await ctx.newPage();
await p.goto('http://localhost:5210/',{waitUntil:'domcontentloaded',timeout:120000});
const snap = async () => p.evaluate(()=>{
  const o={};
  document.querySelectorAll('main > section, main > div').forEach((s,i)=>{
    const k=(s.id||s.className||('sec'+i)).toString().split(/\s+/).slice(0,2).join('.');
    o[k]=+s.getBoundingClientRect().height.toFixed(0);
  });
  o.__footer = +(document.querySelector('footer')?.getBoundingClientRect().top ?? 0).toFixed(0);
  o.__doc = document.documentElement.scrollHeight;
  return o;
});
await p.waitForSelector('.eco-card',{timeout:120000});
await p.waitForTimeout(120); const a=await snap();
await p.waitForTimeout(7000);  const c=await snap();
console.log('■ ارتفاعِ هر بخش — بلافاصله پس از رندر در برابرِ پس از نشستن\n');
const keys=[...new Set([...Object.keys(a),...Object.keys(c)])];
for(const k of keys){
  const x=a[k]??0, y=c[k]??0;
  const d=y-x;
  const flag = Math.abs(d)>4 ? (d>0?`  ← بزرگ شده ${d}+` : `  ← کوچک شده ${d}`) : '';
  console.log(`   ${String(k).padEnd(28)} ${String(x).padStart(6)} → ${String(y).padStart(6)}${flag}`);
}
await b.close();
