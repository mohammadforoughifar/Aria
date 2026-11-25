# نورث‌ویند — فروشگاهِ آنلاینِ نرم‌افزارِ سازمانی (Blazor WebAssembly + ASP.NET Core + SQLite)

یک فروشگاهِ کاملِ فول‌استک برای فروشِ مجموعهٔ نرم‌افزارِ ERP/کسب‌وکار به مشتریانِ
سازمانی (B2B) و شخصی (B2C). رابط به فارسی، کاملاً راست‌به‌چپ، تاریخ‌های جلالی و
قیمت‌ها به **تومان**؛ با پایگاه‌دادهٔ واقعی و مانا (SQLite) و seedِ واقع‌گرایانه در
نخستین اجرا.

---

## ۱. ساختار (چهار لایه)

```
Aria/
├── Client/Northwind.Client/      ① لایهٔ کلاینت — Blazor WebAssembly
│   ├── Pages/Store/              فروشگاه (عمومی)
│   ├── Pages/Auth/               ورود، ثبت‌نام، بازیابی
│   ├── Pages/Dashboard/          پورتالِ مشتری
│   ├── Pages/Admin/              پنلِ مدیریت
│   ├── Services/V1/              کلاینتِ تایپ‌دارِ API + مدل‌هایِ پاسخ
│   └── wwwroot/css/tokens.css    نشانه‌های طراحی (تمِ سرمه‌ای + حالتِ تاریک)
│
├── Shared/Northwind.Shared/      قراردادهایِ سیم (DTO) — تنها نقطهٔ اشتراکِ کلاینت و سرور
│
├── Server/Northwind.Core/       ② لایهٔ هسته — منطقِ کاری و مدل‌ها
│   ├── Services/                 سرویس‌هایِ کاری (سفارش، لایسنس، کاتالوگ، پشتیبانی…)
│   ├── Models/                   مدل‌هایِ ورودی/خروجیِ API
│   └── Mapping/                  نگاشتِ موجودیت → مدل
│
├── Server/Northwind.Api/        ③ لایهٔ API — کنترلرها
│   ├── Controllers/              ۴ فایل، ۸۴ مسیرِ REST روی /api/v1
│   └── Middleware/               خطاهایِ دامنه → پاسخِ یکسانِ JSON
│
├── Server/Northwind.Data/       ④ لایهٔ داده — پایگاه‌داده
│   ├── Entities/ · Enums/        ۲۷ موجودیت
│   ├── Persistence/              DbContext، نگاشت‌ها، DatabaseOptions (رشتهٔ اتصال)
│   └── Migrations/               مهاجرت‌هایِ EF (SQL Server)
│
├── Server/Northwind.Server/      میزبان — فقط سیم‌کشی (API قدیمی: در حالِ حذف)
└── Aria.sln
```

**قاعدهٔ وابستگی تک‌جهته است:** `Client → Shared` و `Server → Api → Core → Data`.
هیچ لایه‌ای از لایهٔ بالاتر خبر ندارد و این قاعده با `ProjectReference` اجبار شده است.
توضیحِ کامل، الگوها و چک‌لیستِ افزودنِ قابلیت: [`Server/ARCHITECTURE.md`](Server/ARCHITECTURE.md).

داده پشتِ اینترفیسِ `IStoreApi` است، بنابراین می‌توانید `HttpStoreApi`
(پیش‌فرض) را با `MockStoreApi` یا هر پیاده‌سازیِ دیگر در
`Client/Northwind.Client/Program.cs` عوض کنید — بدون دست‌زدن به یک خط از صفحات.

---

## ۲. اجرا

### یک دستور (پیش‌نمایشِ زنده در این محیط ابری)
```bash
bash start-preview.sh     # رابط + API روی یک مبدأ: http://0.0.0.0:5210
```
این اسکریپت SDK را در صورت نیاز نصب می‌کند، کلاینت را publish می‌کند، سپس
سرور را با `ASPNETCORE_WEBROOT` به همان خروجی اجرا می‌کند؛ در نتیجه رابط و API
روی **یک پورت** هستند و هیچ مشکلِ CORS یا پروکسی پیش نمی‌آید.

### روی سیستمِ خودتان
```bash
# ۱) API
cd Server/Northwind.Server
dotnet run                              # http://localhost:5080 — Swagger در /swagger

# ۲) کلاینت (ترمینالِ دوم)
cd Client/Northwind.Client
dotnet run                              # http://localhost:5081
```
پایگاه‌داده در `Server/Northwind.Server/data/aria.db` ساخته و پُر می‌شود.
برای بازسازی از صفر، سه فایل `aria.db*` را پاک کنید و دوباره اجرا کنید.

---

## ۳. حساب‌های آماده (برای تست)

| نقش | ایمیل | رمز |
|---|---|---|
| مدیر | `admin@northwindsoft.example` | `Password123!` |
| مشتری | `casey.demo@example.com` | `Password123!` |
| مشتری | `priya.demo@example.com` | `Password123!` |
| مشتری | `marcus.demo@example.com` | `Password123!` |
| مشتری | `hana.demo@example.com` | `Password123!` |

برای فراخوانیِ مستقیمِ API از هدرِ `X-User-Id: u-admin` استفاده کنید.

---

## ۴. مسیرها

| بخش | نشانی |
|---|---|
| خانه، فهرست، جزئیات | `/` · `/catalog` · `/products/{slug}` |
| مقایسه و قیمت | `/compare` · `/pricing` |
| راهکارِ سازمانی | `/solutions` |
| وبلاگ | `/blog` · `/blog/{slug}` |
| سبد و تسویه | `/cart` · `/checkout` |
| پشتیبانی، درباره، تماس | `/support` · `/about` · `/contact` |
| حقوقی | `/legal/{slug}` · مستندات: `/api-docs` |
| پورتالِ مشتری | `/dashboard` · `/orders` · `/licenses` · `/invoices` · `/downloads` · `/tickets` · `/profile` |
| پنلِ مدیریت | `/dashboard/admin` · `products` · `categories` · `customers` · `orders` · `licenses` · `discounts` · `reviews` · `tickets` · `demo-requests` · `blog` · `settings` |

---

## ۵. طراحی

- **نشانه‌ها** (در `wwwroot/css/tokens.css`):
  `--navy-900:#0B1F3A` · `--navy-700:#1E3A8A` · `--blue-600:#2563EB` ·
  `--accent:#F59E0B` · `--bg-soft:#EFF4FB` · `--border:#E2E8F0` ·
  `--text:#0F172A` · `--text-muted:#475569` ·
  `--success:#16A34A` · `--warning:#D97706` · `--danger:#DC2626`
- **قاعدهٔ ۶۰/۳۰/۱۰**: پس‌زمینهٔ روشن ۶۰٪، کارت‌ها و سطوح ۳۰٪، کهربایی ۱۰٪ —
  و کهربایی **فقط** برای «خرید»، «افزودن به سبد» و «درخواست دمو» به‌کار رفته است
  (کلاسِ `.btn--buy`).
- **حالتِ تاریک**: پس‌زمینه `#0A1128`، کارت‌ها `#14213D`، تأکیدی `#6366F1`.
  کلیدِ آن بالای هر صفحه است؛ انتخاب در `localStorage["nw-theme"]` ذخیره و
  پیش از بالا آمدنِ WASM اعمال می‌شود (بدون فلشِ سفید).
- **فونت**: وزیرمتن (Vazirmatn) میزبانیِ محلی، با Tahoma به‌عنوان جایگزین.
- **اعداد و تاریخ**: همهٔ عددها و تاریخ‌ها با ارقامِ فارسی و تقویمِ جلالی از
  طریقِ `Helpers/Fa.cs` نمایش داده می‌شوند.
- **RTL**: `dir="rtl"` روی `<html lang="fa">` و استفاده از ویژگی‌های منطقیِ CSS؛
  `code`، `kbd`، `pre` و ورودی‌های ایمیل/عدد به‌صورت LTR نگه داشته شده‌اند.

---

## ۶. نکته‌های فنی

- پول در همه‌جا **عددِ صحیحِ تومان** است (نه اعشاری)؛ مالیات ۹٪ (`TaxRateBps: 900`).
- خروجیِ ساخت در این محیط ابری به `~/.cache/build` و `~/.cache/publish` می‌رود
  (با `ARIA_SANDBOX=true` در `Directory.Build.props`) تا حجمِ پروژه بالا نرود؛
  روی سیستمِ شما بیلد در همان `bin/` و `obj/` معمول انجام می‌شود.
- لاگ‌های EF خاموش‌اند (`appsettings.Development.json`).
- مستنداتِ کاملِ API در مسیر `/api-docs` داخل خود برنامه و در `/swagger` روی سرور.

---

## ۷. کارهای باقی‌مانده (در صورت نیازِ شما)

1. **احرازِ هویتِ واقعی**: سرور اکنون از هدرِ سادهٔ `X-User-Id` استفاده می‌کند.
   جایگزینی با JWT + Refresh Tokenِ چرخان و رمزنگاریِ scrypt توصیه می‌شود.
2. **تست**: پروژه تستِ خودکار ندارد.
3. **مهاجرت (Migrations)**: دیتابیس با `EnsureCreated` ساخته می‌شود؛ برای
   تغییراتِ بعدیِ طرح، `dotnet ef migrations add` لازم است.
4. **محدودسازیِ نرخ** و قفل‌شدنِ حساب پس از تلاش‌های ناموفق پیاده‌سازی نشده است.
