import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const ctx=await b.newContext({viewport:{width:1440,height:900}});
const p=await ctx.newPage();
const paths=['/','/catalog','/pricing','/blog','/solutions','/systems','/support','/about','/contact','/dashboard'];
const all=[];
for (const path of paths) {
  await p.goto('http://localhost:5210'+path,{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForTimeout(3000);
  const r=await p.evaluate(()=>{
    const out=[];
    document.querySelectorAll('.wrap').forEach((w,i)=>{
      const kids=[...w.children].filter(k=>k.getBoundingClientRect().width>0);
      if(kids.length<2) return;
      const tops={}; kids.forEach(k=>{const t=Math.round(k.getBoundingClientRect().top/5)*5; tops[t]=(tops[t]||0)+1;});
      const maxPer=Math.max(...Object.values(tops));
      if(maxPer>1){
        out.push({i, cls:w.className, maxPer, n:kids.length,
          kids:kids.slice(0,3).map(k=>k.tagName.toLowerCase()+'.'+(k.className||'').toString().trim().split(/\s+/).slice(0,2).join('.'))});
      }
    });
    return out;
  });
  r.forEach(x=>all.push({path, ...x}));
}
console.log('■ .wrapهایی که فرزندانشان هم‌اکنون کنارِ هم اند (خطرِ تغییر):');
if(!all.length) console.log('   هیچ ✅ — یعنی هیچ صفحه‌ای روی flex بودنِ .wrap حساب نکرده');
all.forEach(x=>console.log(`   ${x.path.padEnd(12)} wrap[${x.i}] ${x.n} فرزند، ${x.maxPer} در یک ردیف | ${x.kids.join(' , ')}`));
await b.close();
