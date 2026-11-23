import { chromium } from 'playwright-core';
const B='http://localhost:5210';
const b=await chromium.launch({args:['--no-sandbox']});
const p=await b.newPage({viewport:{width:1440,height:1000}});
const errs=[];
p.on('pageerror',e=>errs.push('PAGEERROR: '+e.message.slice(0,100)));
p.on('console',m=>{if(m.type()==='error'&&!/dotnet\.wasm|Failed to load/.test(m.text()))errs.push(m.text().slice(0,100));});

for (const [email,name] of [['admin@northwindsoft.example','مدیر'],['casey.demo@example.com','مشتری']]) {
  await p.goto(B+'/login',{waitUntil:'domcontentloaded',timeout:120000});
  await p.waitForTimeout(2500);
  const ins=p.locator('input');
  await ins.nth(0).fill(email);
  await ins.nth(1).fill('Password123!');
  await p.locator('button').filter({hasText:/ورود|ادامه/}).first().click().catch(async()=>{await p.locator('button[type=submit]').first().click();});
  await p.waitForTimeout(4000);
  const url=p.url().replace(B,'')||'/';
  const warn=await p.locator('.api-warn').count();
  console.log(`■ ${name} (${email})`);
  console.log(`   نشانی پس از ورود: ${url}  ${url.startsWith('/dashboard')?'✅':'❌'}`);
  console.log(`   پیامِ هشدار     : ${warn?'دارد ❌':'ندارد ✅'}`);
  const t=await p.innerText('body');
  const m=t.match(/(ارتباط با سرور برقرار نیست|ایمیل یا رمز عبور نادرست)/);
  console.log(`   پیامِ خطا       : ${m?'❌ '+m[0]:'هیچ ✅'}`);
}
console.log(`\nخطاهایِ ترمینال: ${errs.length?[...new Set(errs)].slice(0,3).join(' | ')+' ❌':'هیچ ✅'}`);
await b.close();
