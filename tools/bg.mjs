import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
for (const theme of ['light','dark']) {
  const p=await b.newPage({viewport:{width:1440,height:1000}});
  await p.addInitScript(t=>{try{localStorage.setItem('nw-theme',t)}catch(e){}}, theme);
  await p.goto('http://localhost:5210/',{waitUntil:'networkidle',timeout:120000});
  await p.waitForTimeout(3500);
  const r=await p.evaluate(()=>{
    const cs=getComputedStyle(document.body);
    const out={body:cs.backgroundColor, sections:[], nonWhite:[]};
    document.querySelectorAll('section').forEach((el,i)=>{
      const c=getComputedStyle(el).backgroundColor;
      if(c!=='rgba(0, 0, 0, 0)') out.sections.push(`بخش ${i}: ${c}`);
    });
    return out;
  });
  console.log(`\n■ حالتِ «${theme}»`);
  console.log('   پس‌زمینهٔ body :', r.body);
  console.log('   بخش‌هایِ رنگی  :');
  r.sections.slice(0,8).forEach(s=>console.log('      ', s));
}
await b.close();
