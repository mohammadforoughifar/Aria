import { chromium } from 'playwright-core';
const B='http://localhost:5290';   // کلاینتِ تنها — سرورِ API خاموش است
const b=await chromium.launch({args:['--no-sandbox']});
const p=await b.newPage({viewport:{width:1440,height:1000}});
const msgs=[];
p.on('console',m=>{ if(m.type()==='error') msgs.push(m.text().slice(0,90)); });
p.on('pageerror',e=>msgs.push('PAGEERROR: '+e.message.slice(0,90)));

await p.goto(B+'/login',{waitUntil:'networkidle',timeout:120000});
await p.waitForTimeout(4500);

console.log('■ کلاینت روشن، سرورِ API خاموش');
console.log('  پیامِ هشدار:', await p.locator('.api-warn').count() ? 'نمایش داده شد ✅' : 'نیست ❌');
if (await p.locator('.api-warn').count())
  console.log('  متن: ' + (await p.locator('.api-warn').innerText()).replace(/\s+/g,' ').slice(0,150));

// تلاشِ ورود
const ins=p.locator('input');
await ins.nth(0).fill('admin@northwindsoft.example');
await ins.nth(1).fill('Password123!');
await p.locator('button').filter({hasText:/ورود|ادامه/}).first().click().catch(async()=>{await p.locator('button[type=submit]').first().click();});
await p.waitForTimeout(3500);

const txt=await p.innerText('body');
console.log('\n■ تلاشِ ورود:');
console.log('  پیامِ نمایش‌داده‌شده:');
const m=txt.match(/(ارتباط با سرور برقرار نیست[^\n]*|ایمیل یا رمز عبور نادرست[^\n]*)/);
console.log('     «' + (m ? m[0] : '—') + '»');
console.log('  ' + (m && m[0].includes('ارتباط') ? '✅ پیامِ درست — مشکل را درست می‌گوید' : '❌ هنوز می‌گوید رمز اشتباه است'));

// شمارشِ خطاهایِ تکراری
const conn = msgs.filter(x=>/ERR_CONNECTION_REFUSED|5182/.test(x)).length;
console.log(`\n■ خطاهایِ «۵۱۸۲ در دسترس نیست» در ترمینال: ${conn} مورد`);
console.log(`  ${conn<=6 ? '✅ کم شده (پیش از این هر ۳ ثانیه تکرار می‌شد)' : '⚠ هنوز زیاد است'}`);
await p.screenshot({path:'/home/user/shots/noapi-login.png'});
await b.close();
