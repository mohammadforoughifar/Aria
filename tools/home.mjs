import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const p=await b.newPage({viewport:{width:1440,height:1200}});
await p.goto('http://localhost:5210/',{waitUntil:'networkidle',timeout:120000});
await p.waitForTimeout(5000);
const s=await p.evaluate(()=>{
  const q=s=>document.querySelectorAll(s).length;
  return {
    hero: q('.hero__title'), stats: q('.hero__stat'),
    eco: q('.eco'), ecoCards: q('.eco-card'), ecoTabs: q('.eco-tab'),
    sections: q('section'), cards: q('.card'),
    testimonials: q('.testimonial, .tst, .quote'),
    faq: q('.faq__item'),
    bodyLen: document.body.innerText.trim().length,
  };
});
console.log(JSON.stringify(s,null,1));
// آیا بخشی خالی مانده؟
const empty=await p.evaluate(()=>{
  const out=[];
  document.querySelectorAll('section').forEach((el,i)=>{
    const t=el.innerText.trim();
    if(t.length<40) out.push(`بخش ${i}: «${t.slice(0,40)}»`);
  });
  return out;
});
console.log('\nبخش‌هایِ خالی یا کم‌متن:', empty.length?empty:'هیچ ✅');
await b.close();
