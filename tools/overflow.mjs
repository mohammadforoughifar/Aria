import { chromium } from 'playwright-core';
const b = await chromium.launch({ args:['--no-sandbox'] });
const p = await b.newPage({ viewport:{ width:390, height:844 } });
await p.goto('http://localhost:5210/catalog', { waitUntil:'networkidle', timeout:120000 });
await p.waitForTimeout(3500);
const bad = await p.evaluate(() => {
  const w = window.innerWidth, out = [];
  document.querySelectorAll('*').forEach(e => {
    const r = e.getBoundingClientRect();
    if (r.width > 0 && (r.right > w + 1 || r.left < -1)) {
      out.push({ tag: e.tagName.toLowerCase(), cls: (e.className||'').toString().slice(0,48),
                 left: Math.round(r.left), right: Math.round(r.right), w: Math.round(r.width) });
    }
  });
  return out.slice(0, 14);
});
console.log(`عرضِ صفحه: 390 — عنصرِ بیرون‌زده: ${bad.length}`);
bad.forEach(x => console.log(`   <${x.tag} class="${x.cls}">  چپ:${x.left}  راست:${x.right}  عرض:${x.w}`));
await b.close();
