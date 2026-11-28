# تحلیل مخزن `mohammadforoughifar/Aria`

**تاریخ تحلیل:** ۱۴۰۴/۰۸/۱۳ (۲۰۲۶-۱۱-۰۴) · **آخرین کامیت:** `4b08e23` · **تعداد کامیت‌ها:** ۳

---

## ۱. خلاصهٔ یک‌خطی

مخزن در ظاهر فقط شامل **یک فایل ZIP** است (`full-stack-blazor-software-storefront (1).zip`) که درون آن یک **فروشگاه نرم‌افزار سازمانی تمام‌عیار و فارسی/راست‌به‌چپ** پیاده‌سازی شده با **Next.js 16 (App Router) + React 19 + Drizzle ORM + PostgreSQL** قرار دارد — نه Blazor.

> ⚠️ **نام‌گذاری گمراه‌کننده:** عنوان فایل «full-stack-blazor» است اما هیچ ردی از .NET/Blazor در کد نیست؛ پروژهٔ کامل TypeScript/React است.

---

## ۲. وضعیت خودِ مخزن (Git)

| مورد | مقدار |
|---|---|
| کامیت‌ها | ۳ (`Add files via upload` → `Delete SolutionYarco.rar` → `Add files via upload`) |
| نویسنده | Mohammad (`mohammadforoughifar`) |
| branchها | فقط `main` |
| محتوای واقعی | یک ZIP ۵۹۱ کیلوبایتی (کد منبع به‌صورت بسته‌بندی‌شده) |
| README / LICENSE / .gitignore / CI | **هیچ‌کدام وجود ندارد** |

**نقد:** کد به‌جای اینکه به‌صورت فایل‌های متنی کامیت شود، داخل ZIP است. در نتیجه diff، تاریخچه، review و همکاری تیمی عملاً غیرممکن است. این اولین و مهم‌ترین اصلاحِ پیشنهادی است.

---

## ۳. پشتهٔ فناوری

| لایه | انتخاب |
|---|---|
| فریم‌ورک | **Next.js 16.2.6** (App Router، Turbopack) |
| UI | **React 19.2.6** |
| زبان | **TypeScript 5.9.3** با `strict: true` |
| استایل | **Tailwind CSS 4.1.17** (فونت Vazirmatn از CDN) |
| ORM/DB | **Drizzle ORM 0.45.2** + `pg` 8.20 روی **PostgreSQL** |
| اعتبارسنجی | **Zod 4** |
| احراز هویت | **دست‌ساز** با `node:crypto`: scrypt برای رمز عبور + JWT امضاشده با HMAC-SHA256 + Refresh Token چرخشی در DB |
| ابزارها | ESLint 9 (flat config)، drizzle-kit 0.31 |

---

## ۴. معماری (لایه‌بندی بسیار تمیز)

```
src/
├─ shared/contracts.ts      (366 خط)  قراردادها: DTOها و تایپ‌های مشترک سرور/کلاینت
├─ db/
│  ├─ schema.ts             (478 خط)  ۲۴ جدول + ۱۱ enum
│  ├─ seed.ts               (845 خط)  دادهٔ دموی کامل فارسی
│  └─ index.ts                        Pool + drizzle (singleton در dev)
├─ server/
│  ├─ router.ts            (1435 خط)  جدول مسیرها + دیسپچر دست‌ساز (۶۸ مسیر)
│  ├─ http.ts               (165 خط)  لاگر ساختاریافته، ProblemDetails، CORS، کوکی، صفحه‌بندی
│  ├─ auth.ts               (157 خط)  scrypt / JWT / refresh token
│  ├─ bootstrap.ts                    تزریق دادهٔ اولیه (auto-seed)
│  └─ services/            (1974 خط)  catalog · commerce · support
├─ lib/client/api.tsx       (411 خط)  کلاینت fetch با رفرش خودکار + Providerها
├─ components/             (1774 خط)  ui.tsx · charts.tsx · SiteChrome · ProductCard · ProductDetail
└─ app/                    (6678 خط)  (store) · dashboard · api/[...path] · login · register
```

**نکات مثبت معماری:**

- **یک API مرکزیِ REST-مانند** با دیسپچر دستی (به‌جای ده‌ها Route Handler)، به همراه تولید **OpenAPI 3.0 خودکار** از همان جدول مسیرها و یک صفحهٔ `/api-docs` که مستندات را زنده از `/api/openapi` می‌خواند.
- **قرارداد یکپارچهٔ پاسخ**: پاکت `{ success, data, meta, error }` برای همهٔ مسیرها؛ خطاها با استایل **ProblemDetails** (`code`, `message`, `details[field][]`) و `ZodError → 422 validation_failed`.
- **سطح دسترسی اعلانی**: هر مسیر یک برچسب `auth: "public" | "user" | "admin"` دارد که قبل از اجرای هندلر اعمال می‌شود.
- **Auto-seed ایمن**: `ensureSeeded()` فقط یک‌بار در هر پروسه اجرا می‌شود، promise آن memoize شده و هیچ‌وقت پرتاب خطا نمی‌کند تا DB نیمه‌آماده کل API را پایین نیاورد.
- **صفحه‌بندی/مرتب‌سازی/فیلتر استاندارد** (`listParams`) با محدود کردن `pageSize` به حداکثر ۱۰۰ و whitelist برای کلیدهای مرتب‌سازی (جلوگیری از SQL injection در ORDER BY).
- **لاگ ساختاریافته JSON** (شبیه Serilog) برای هر درخواست، سفارش، خطا و seed.

---

## ۵. مدل داده (۲۴ جدول)

- **هویت:** `users`، `refresh_tokens`
- **کاتالوگ:** `categories`، `products`، `product_plans`، `product_versions`، `reviews`
- **تجارت:** `carts`، `cart_items`، `discount_codes`، `orders`، `order_items`، `payments`، `invoices`
- **لایسنس:** `licenses`، `license_activations`، `downloads`
- **پشتیبانی/محتوا:** `tickets`، `ticket_messages`، `testimonials`، `faqs`، `contact_messages`، `demo_requests`، `site_settings`

طراحی ستون‌ها حرفه‌ای است: پول به‌صورت **سنت (integer)**، نرخ مالیات به‌صورت **bps**، امتیازها `rating*10`، ایندکس‌های مناسب روی کلیدهای خارجی و فیلدهای پرتکرار، enumهای PostgreSQL برای وضعیت‌ها، و `onDelete` متناسب با معنای هر رابطه.

**منطق تجاریِ قابل‌توجه:** پلن‌ها (SingleUser/MultiUser/Enterprise × OneTime/Annual)، صدور خودکار لایسنس به‌ازای هر seat هنگام پرداخت *(این بخش در کامیت‌هایِ بعدی برداشته شد: از این پس لایسنس بیرون از سایت صادر می‌شود و پرداخت فقط سفارش/فاکتور/حقِ دانلود می‌سازد)*، محدودیت تعداد فعال‌سازی (`activationsUsed < seats`)، تجمیع مجدد امتیاز محصول بعد از تأیید نقد، ادغام سبد خرید مهمان با کاربر هنگام ورود (`resolveCart`).

---

## ۶. سطح API: ۶۸ مسیر

| سطح دسترسی | تعداد |
|---|---|
| عمومی (`public`) | ۲۲ |
| کاربر لاگین‌کرده (`user`) | ۲۰ |
| ادمین (`admin`) | ۲۶ |

| متد | تعداد |
|---|---|
| GET | ۲۶ · POST | ۲۲ · PATCH | ۱۱ · DELETE | ۸ · PUT | ۱ |

گروه‌ها: Auth (۷)، Products (۶)، Cart (۵)، Tickets (۵)، Categories/Discounts/Orders/Reviews/Content (هرکدام ۴)، Plans و Customers و Licenses و Downloads (هرکدام ۳)، System (۳)، Versions/Payments/Invoices/Dashboard/Settings (هرکدام ۲).

امکانات: ثبت‌نام/ورود/خروج/رفرش چرخشی/تغییر رمز، کاتالوگ با جست‌وجو و مرتب‌سازی، مقایسهٔ محصول، سبد خرید با کد تخفیف، تسویه‌حساب، سفارش‌ها، پرداخت (شبیه‌ساز)، فاکتورها، لایسنس و فعال‌سازی، دانلودها، تیکت‌های پشتیبانی با رشتهٔ پیام، نقدها با moderating، مدیریت مشتریان/محصولات/پلن‌ها/نسخه‌ها/دسته‌ها/تخفیف‌ها، تنظیمات سایت، فرم تماس و درخواست دمو، و داشبورد KPI.

---

## ۷. فرانت‌اند

- **۳۳ مسیر** شامل: فروشگاه (`/`, `/catalog`, `/products/[slug]`, `/cart`, `/checkout`, `/compare`, `/about`, `/support`, `/contact`, `/legal/[slug]`, `/api-docs`) و پورتال/پنل (`/dashboard`, `/dashboard/orders|invoices|licenses|downloads|tickets|profile`, `/dashboard/admin/*` شامل محصولات، دسته‌ها، مشتریان، سفارش‌ها، لایسنس‌ها، تخفیف‌ها، نقدها، تنظیمات).
- RTL کامل (`<html lang="fa" dir="rtl">`) با اصلاحات ظریف CSS: غیرفعال کردن `uppercase` و `letter-spacing` برای فارسی، نگه‌داشتن `code/kbd/pre` به‌صورت LTR، ورودی‌های ایمیل/عدد LTR، و اسکرول‌بار سمت راست.
- کتابخانهٔ کامپوننت **دست‌ساز و بدون وابستگی خارجی** (`ui.tsx`، ۶۲۳ خط): Button، Input، Select، Field، Badge + `statusTone`، Card، Modal، EmptyState، Skeleton*، Pagination، Tabs، Toast (Context)، StatCard، RatingStars، CopyButton + نمودار میله‌ای/حلقه‌ای سفارشی (`charts.tsx`) برای داشبورد.
- داده‌ها همه سمت کلاینت با هوک `useApi` (loading/error/data + re-fetch) گرفته می‌شوند؛ توکن دسترسی در حافظه و رفرش‌توکن در کوکی `HttpOnly`.

---

## ۸. بررسی عملی (روی این محیط اجرا شد) ✅

| آزمون | نتیجه |
|---|---|
| `npm install` | ✅ ۳۸۹ بسته، بدون خطا |
| `tsc --noEmit` | ✅ **۰ خطا** (TypeScript strict) |
| `next build` | ✅ موفق — ۳۳ مسیر (۲۳ dynamic، ۱۰ static) |
| `drizzle-kit push` | ✅ اسکیما روی PostgreSQL 17 اعمال شد |
| اجرای واقعی + auto-seed | ✅ دادهٔ دمو ساخته شد (`demo_data_seeded`) |
| `/api/health` | ✅ `{"ok":true}` |
| ورود دمو + `/api/orders` + `/api/reports/customer` | ✅ پاسخ صحیح؛ دسترسی بدون توکن → **401** |
| همهٔ صفحات (`/`, `/catalog`, `/checkout`, `/dashboard/admin`, …) | ✅ ۲۰۰ |

**حساب‌های دمو** (رمز مشترک: `Password123!`)
- ادمین: `admin@northwindsoft.example`
- مشتری: `casey.demo@` / `priya.demo@` / `marcus.demo@` / `hana.demo@` (`example.com`)

> یک نسخهٔ زنده از پروژه روی پورت ۳۰۰۰ در حال اجراست و در پیش‌نمایش قابل مشاهده است (PostgreSQL محلی + اسکیما + دادهٔ دمو).

---

## ۹. مشکلات و ریسک‌ها (به ترتیب اهمیت)

### 🔴 بحرانی
1. **کلید امضای JWT پیش‌فرض در کد** — `src/server/auth.ts:30`
   ```ts
   const SECRET = process.env.AUTH_SECRET ?? "northwind-storefront-dev-secret-key-2026";
   ```
   اگر `AUTH_SECRET` تنظیم نشود، هر کسی می‌تواند توکن ادمین جعل کند. باید در production الزامی شود (fail-fast).
2. **بدون Transaction در تسویه‌حساب** — `checkout()` حدود ۶ عملیات نوشتن (سفارش، آیتم‌ها، پرداخت، فاکتور، چند لایسنس، بروزرسانی تخفیف، حذف سبد) را بدون `db.transaction()` انجام می‌دهد. خرابی در میانه ⇒ سفارشِ ناقص. همچنین درگاه پرداخت `MockGateway` همیشه `Succeeded` برمی‌گرداند.
3. **`POST /api/downloads` عمومی است** و بدون بررسی مالکیت لایسنس، یک manifest با `licenseKey: "TRIAL"` و «چک‌سام» ساختگی با `Math.random()` برمی‌گرداند. در ظاهر قرار است مسیر واقعیِ تحویل فایل باشد اما هیچ کنترل دسترسی ندارد.

### 🟠 مهم
4. **بدون محدودیت نرخ (rate limit)** روی ورود/ثبت‌نام/فرم‌ها؛ فیلد `isLocked` در اسکیما هست اما هیچ‌جا به‌طور خودکار ست نمی‌شود.
5. **`scryptSync` همزمان** در مسیر درخواست (ورود/ثبت‌نام/تغییر رمز) ⇒ بلوکه‌شدن event loop زیر بار. استفاده از نسخهٔ async توصیه می‌شود.
6. **هیچ تستی وجود ندارد** (نه unit، نه integration، نه E2E) و CI هم ندارد.
7. **هیچ مهاجرت (migration) ای در مخزن نیست** — فقط `drizzle.config.json` و اسکیما. باید `drizzle-kit generate` اجرا و پوشهٔ `migrations/` کامیت شود (و اسکریپت‌های `db:generate`/`db:migrate` به `package.json` اضافه گردد).
8. **CORS با `Access-Control-Allow-Origin: *`** روی همهٔ پاسخ‌ها، در حالی که کوکیِ HttpOnly هم استفاده می‌شود.

### 🟡 متوسط
9. **خطاهای ESLint (۸ مورد)** که بیلد را نمی‌شکند ولی با قواعد React Compiler مغایرند:
   - `react-hooks/set-state-in-effect` در `lib/client/api.tsx` (۱۶۵، ۳۲۱)، `checkout/page.tsx`، `dashboard/layout.tsx`، `dashboard/profile/page.tsx`، `dashboard/admin/settings/page.tsx`
   - `react-hooks/immutability` در `components/charts.tsx:154`
   - `@next/next/no-html-link-for-pages` در `api-docs/page.tsx:67` (`<a href="/api/openapi">`)
10. **تولید شمارهٔ فاکتور اشتباه است** — `nextSequence("INV")` تعداد **سفارش‌ها** را می‌شمارد نه فاکتورها (`commerce.ts:405`)؛ شماره‌ها یک واحد جلوتر از تعداد واقعی فاکتورها هستند. ضمن این‌که در نبود transaction، دو تسویهٔ همزمان می‌توانند شمارهٔ یکسان تولید کنند و با ایندکس یکتا برخورد کنند.
11. **همهٔ داده‌ها سمت کلاینت fetch می‌شوند** (هیچ pageای داده را در سرور render نمی‌کند) ⇒ SEO ضعیف و flashِ loading در صفحات عمومی فروشگاه.
12. فایل کانفیگ `drizzle.config.json` شامل نام‌کاربری/رمز پیش‌فرضِ `postgres:postgres` است؛ باید از env خوانده شود.
13. فونت Vazirmatn از CDN بارگذاری می‌شود (وابستگی خارجی + تأخیر).

### 🔵 سبک/نگهداری
14. نام‌گذاری فایل «blazor» در حالی که پروژه Next.js است.
15. `ratingAvg` در واقع امتیاز ×۱۰ است؛ نام بهتر `ratingSumX10` یا ذخیره با دقت اعشار.
16. مشتری می‌تواند هنگام ارسال پاسخ تیکت، `status` را هم تغییر دهد (بهتر است فقط برای ادمین مجاز باشد).
17. نبود README (راهنمای راه‌اندازی، متغیرهای محیطی، حساب‌های دمو)، نبود `.env.example`، `LICENSE` و `docker-compose` برای بالا آوردن سریع PostgreSQL.

---

## ۱۰. جمع‌بندی

پروژه از نظر **کیفیت کد، یکپارچگی معماری و وسعت امکانات** در سطحی بسیار بالاتر از یک نمونه‌کار معمولی است: تایپ‌اسکریپت strict بدون حتی یک خطا، لایه‌بندی تمیز، قرارداد پاسخ یکپارچه، مستندات OpenAPI خودکار، احراز هویت کامل با refresh چرخشی، مدل داده‌ای غنی و رابط کاربری فارسی/RTL با کتابخانهٔ کامپوننت اختصاصی. این در واقع **یک محصول آماده (وایت‌لیبلِ فروشگاه نرم‌افزار)** است، نه صرفاً یک دموی آموزشی.

نقطه‌ضعف اصلی آن **بسته‌بندی/توزیع** است — کد داخل ZIP است، مستندات و تست و مهاجرت ندارد، و چند مورد امنیتی (کلید پیش‌فرض، نبود transaction و rate-limit) باید پیش از استقرار واقعی برطرف شود.

**اولویت‌های پیشنهادی:**
1. استخراج فایل‌ها از ZIP و کامیت مستقیمِ سورس + افزودن README/`.env.example`/`.gitignore`/`LICENSE`.
2. الزامی‌کردن `AUTH_SECRET` و حذف مقدار پیش‌فرض.
3. قراردادن `checkout()` داخل `db.transaction()` + استفاده از `scrypt` ناهمگام + افزودن rate-limit برای ورود.
4. تولید و کامیت مهاجرت‌های Drizzle + افزودن چند تست smoke روی API.
5. رفع ۸ خطای ESLint و تبدیل صفحات عمومی فروشگاه به Server Component برای SEO.
