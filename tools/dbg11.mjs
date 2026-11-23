import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const ctx=await b.newContext({viewport:{width:1440,height:900}});
const p=await ctx.newPage();
await p.goto('http://localhost:5210/catalog',{waitUntil:'domcontentloaded',timeout:120000});
await p.waitForSelector('.orb__chip',{timeout:120000});
const snap = () => p.evaluate(()=>{
  const st=document.querySelector('.orb');
  const db=st.querySelector('.orb__db svg').getBoundingClientRect();
  const s=st.getBoundingClientRect();
  return {
    cap: st.querySelector('.orb__cap').textContent.trim(),
    drawn: [...st.querySelectorAll('.orb__link')].map(l=>+(l.style.strokeDashoffset||1)),
    dots: [...st.querySelectorAll('.orb__dot')].filter(d=>+d.getAttribute('opacity')>0.05).length,
    pos: [...st.querySelectorAll('.orb__chip')].map(c=>{
      const r=c.getBoundingClientRect();
      return [Math.round(r.left+r.width/2-s.left), Math.round(r.top+r.height/2-s.top)];
    }),
    dbc:[Math.round(db.left+db.width/2-s.left), Math.round(db.top+db.height/2-s.top)],
  };
});
const show=(t,r)=>{
  console.log(`\n■ ${t}`);
  console.log('   زیرنویس :', r.cap);
  console.log('   سرهایِ خط کشیده‌شده (۰ = کامل):', r.drawn.map(v=>v.toFixed(2)).join(' '));
  console.log('   ذراتِ روشن:', r.dots);
  console.log('   مرکزِ چیپ‌ها:', r.pos.map(q=>`(${q[0]},${q[1]})`).join(' '));
  console.log('   مرکزِ پایگاه:', `(${r.dbc[0]},${r.dbc[1]})`);
};
await p.waitForTimeout(1200); const d=await snap(); show('نخستین قاب (نشسته)', d);
await p.waitForTimeout(6800); const f=await snap(); show('حدودِ ۸ ثانیه (آزاد)', f);
const moved = JSON.stringify(d.pos)!==JSON.stringify(f.pos);
console.log('\nچیپ‌ها جابه‌جا شده‌اند:', moved ? '✅ بله' : '❌ خیر');
console.log('خطوط در حالتِ آزاد محو شده‌اند:', f.drawn.every(v=>v>0.99)?'✅':'❌ '+f.drawn.join(','));
console.log('خطوط در حالتِ نشسته کامل‌اند:', d.drawn.every(v=>v<0.01)?'✅':'❌ '+d.drawn.join(','));
await b.close();
