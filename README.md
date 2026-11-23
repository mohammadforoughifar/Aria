# نورث‌ویند — فروشگاهِ آنلاینِ نرم‌افزارِ سازمانی (Blazor WebAssembly + ASP.NET Core + SQLite)

یک فروشگاهِ کاملِ فول‌استک برای فروشِ مجموعهٔ نرم‌افزارِ ERP/کسب‌وکار به مشتریانِ
سازمانی (B2B) و شخصی (B2C). رابط به فارسی، کاملاً راست‌به‌چپ، تاریخ‌های جلالی و
قیمت‌ها به **تومان**؛ با پایگاه‌دادهٔ واقعی و مانا (SQLite) و seedِ واقع‌گرایانه در
نخستین اجرا.

---

## ۱. ساختار

```
Aria/
├── Shared/Northwind.Shared/      قراردادها (DTOها و enumهای مشترک بین کلاینت و سرور)
├── Server/Northwind.Server/      Web API روی ASP.NET Core 9 + EF Core + SQLite
│   ├── Data/Entities.cs          ۲۷ موجودیت
│   ├── Data/AppDbContext.cs      DbContext
│   ├── Data/Seed.cs              دادهٔ اولیه (idempotent)
│   ├── Controllers/              Catalog · Commerce · Account · Support · Admin · Blog
│   └── data/aria.db              پایگاه‌داده (در نخستین اجرا ساخته می‌شود)
├── Client/Northwind.Client/      Blazor WebAssembly — ۳۹ صفحه + ۱۹ کامپوننت
│   ├── Pages/Store/              فروشگاه (عمومی)
│   ├── Pages/Auth/               ورود، ثبت‌نام، بازیابی
│   ├── Pages/Dashboard/          پورتالِ مشتری
│   ├── Pages/Admin/              پنلِ مدیریت
│   ├── Shared/UI/                کامپوننت‌های رابط
│   └── wwwroot/css/tokens.css    نشانه‌های طراحی (تمِ سرمه‌ای + حالتِ تاریک)
└── Aria.sln
```

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
