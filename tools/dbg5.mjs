import { chromium } from 'playwright-core';
const b=await chromium.launch({args:['--no-sandbox']});
const ctx=await b.newContext({viewport:{width:1440,height:900}});
const p=await ctx.newPage();
await p.goto('http://localhost:5210/catalog',{waitUntil:'domcontentloaded',timeout:120000});
await p.waitForSelector('.orb',{state:'attached',timeout:120000});
await p.waitForTimeout(2500);
const r=await p.evaluate(()=>{
  const chain=[];
  let el=document.querySelector('.orb');
  while(el && el!==document.body){
    const s=getComputedStyle(el), b=el.getBoundingClientRect();
    chain.push({
      sel: el.tagName.toLowerCase()+'.'+(el.className||'').toString().trim().split(/\s+/).join('.'),
      rect:[Math.round(b.left),Math.round(b.top),Math.round(b.width),Math.round(b.height)],
      client:[el.clientWidth,el.clientHeight],
      display:s.display, width:s.width, height:s.height,
      aspect:s.aspectRatio, maxWidth:s.maxWidth, minWidth:s.minWidth,
      gridCol:s.gridColumn, pos:s.position,
    });
    el=el.parentElement;
  }
  return chain;
});
r.forEach(c=>{
  console.log(`\n${c.sel.slice(0,60)}`);
  console.log(`   rect ${JSON.stringify(c.rect)}  client ${JSON.stringify(c.client)}`);
  console.log(`   display=${c.display} w=${c.width} h=${c.height} aspect=${c.aspect} maxW=${c.maxWidth} minW=${c.minWidth} gridCol=${c.gridCol} pos=${c.pos}`);
});
await b.close();
