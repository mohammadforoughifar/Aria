import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const ctx=await b.newContext({viewport:{width:1440,height:900}});
const p=await ctx.newPage();
p.on('pageerror',e=>console.log('PAGEERROR:',e.message.slice(0,200)));
p.on('console',m=>{if(m.type()==='error')console.log('CONSOLE:',m.text().slice(0,200));});
await p.goto('http://localhost:5210/catalog',{waitUntil:'domcontentloaded',timeout:120000});
await p.waitForSelector('.orb__chip',{timeout:120000});
await p.waitForTimeout(3000);
const r=await p.evaluate(()=>{
  const st=document.querySelector('.orb');
  return {
    clientW: st.clientWidth, clientH: st.clientHeight,
    frames: window.__heroOrbitFrames,
    viewBox: st.querySelector('.orb__fx')?.getAttribute('viewBox'),
    chips: [...st.querySelectorAll('.orb__chip')].map(c=>({
      t:c.textContent.trim().slice(0,12),
      tr:c.style.transform,
      r:(()=>{const b=c.getBoundingClientRect();return [Math.round(b.left),Math.round(b.top),Math.round(b.width)];})(),
      cs:(()=>{const s=getComputedStyle(c);return {left:s.left,top:s.top,pos:s.position};})(),
    })),
  };
});
console.log('clientWidth/Height:', r.clientW, r.clientH, '| frames:', r.frames, '| viewBox:', r.viewBox);
r.chips.forEach(c=>console.log('  ', c.t.padEnd(13), 'rect', JSON.stringify(c.r), '| css', JSON.stringify(c.cs), '| tr:', (c.tr||'(ندارد)').slice(0,60)));
await b.close();
