import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const ctx=await b.newContext({viewport:{width:1440,height:900}});
await ctx.route('**/api/**', r=>r.abort());
const p=await ctx.newPage();
await p.goto('http://localhost:5210/catalog',{waitUntil:'domcontentloaded',timeout:120000});
await p.waitForSelector('.api-warn',{timeout:60000}).catch(()=>console.log('هشداری نیست'));
await p.waitForTimeout(4000);
const r=await p.evaluate(()=>{
  const info=e=>{ if(!e) return null; const b=e.getBoundingClientRect(); const s=getComputedStyle(e);
    return {tag:e.tagName.toLowerCase()+'.'+(e.className||'').toString().trim().split(/\s+/)[0],
      top:+b.top.toFixed(1), bottom:+b.bottom.toFixed(1), h:+b.height.toFixed(1),
      pos:s.position, top0:s.top, z:s.zIndex}; };
  return {
    header: info(document.querySelector('.site-header')),
    banner: info(document.querySelector('.api-warn')),
    firstHeader: info(document.querySelector('header')),
    heroTop: info(document.querySelector('.hero--split')),
    // آیا بنر پیش از سرصفحه در DOM است؟
    order: (()=>{ const h=document.querySelector('.site-header'), w=document.querySelector('.api-warn');
      if(!h||!w) return '—';
      return (h.compareDocumentPosition(w) & Node.DOCUMENT_POSITION_PRECEDING) ? 'بنر پیش از سرصفحه' : 'سرصفحه پیش از بنر'; })(),
  };
});
console.log(JSON.stringify(r,null,2));
await b.close();
