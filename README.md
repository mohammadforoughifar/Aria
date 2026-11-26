# نورث‌ویند — فروشگاهِ آنلاینِ نرم‌افزارِ سازمانی (Blazor WebAssembly + ASP.NET Core + SQLite)

یک فروشگاهِ کاملِ فول‌استک برای فروشِ مجموعهٔ نرم‌افزارِ ERP/کسب‌وکار به مشتریانِ
سازمانی (B2B) و شخصی (B2C). رابط به فارسی، کاملاً راست‌به‌چپ، تاریخ‌های جلالی و
قیمت‌ها به **تومان**؛ با پایگاه‌دادهٔ واقعی و مانا (SQLite) و seedِ واقع‌گرایانه در
نخستین اجرا.

---

## ۱. ساختار (چهار لایه)

```
Aria/                             چهار پروژه = چهار لایه
├── Client/Northwind.Client/      ① کلاینت — Blazor WebAssembly
│   ├── Pages/                    فروشگاه، پورتالِ مشتری، پنلِ مدیریت
│   ├── Components/               کامپوننت‌های رابط (Layout · Store · UI)
│   ├── Services/                 وضعیت + کلاینتِ تایپ‌دارِ API
│   ├── Contracts/                DTOهایِ سیم (فقط سمتِ کلاینت)
│   └── wwwroot/css/tokens.css    نشانه‌های طراحی
│
├── Server/Northwind.Core/        ② هسته — منطقِ کاری
│   ├── Services/                 سفارش، لایسنس، کاتالوگ، پشتیبانی…
│   ├── Models/                   مدل‌هایِ ورودی/خروجیِ API
│   └── Mapping/                  نگاشتِ موجودیت → مدل
│
├── Server/Northwind.Api/         ③ API — کنترلرها + نقطهٔ اجرا
│   ├── Controllers/              ۸۴ مسیرِ REST روی /api/v1
│   ├── Middleware/               خطاهایِ دامنه → پاسخِ یکسانِ JSON
│   ├── Program.cs                سیم‌کشیِ لایه‌ها
│   └── data/                     northwind-api.db
│
├── Server/Northwind.Data/        ④ داده — پایگاه‌داده
│   ├── Entities/ · Enums/        ۲۷ موجودیت
│   ├── Persistence/              DbContext، نگاشت‌ها، DatabaseOptions
│   └── Migrations/               مهاجرت‌هایِ EF
│
└── Aria.sln
```

**قاعدهٔ وابستگی تک‌جهته است:** `Api → Core → Data` و کلاینت فقط از راهِ HTTP.
هیچ لایه‌ای از لایهٔ بالاتر خبر ندارد و این قاعده با `ProjectReference` اجبار شده است.
توضیحِ کامل، الگوها و چک‌لیستِ افزودنِ قابلیت: [`Server/ARCHITECTURE.md`](Server/ARCHITECTURE.md).

داده پشتِ اینترفیسِ `IStoreApi` است. پیاده‌سازیِ پیش‌فرض `StoreApiRouter` است که
همهٔ کارها را روی `/api/v1` انجام می‌دهد؛ برایِ کارِ بدونِ سرور می‌توانید در
`Client/Northwind.Client/Program.cs` آن را با `MockStoreApi` عوض کنید — بدونِ
دست‌زدن به یک خط از صفحه‌ها.

**فقط یک API وجود دارد.** API قدیمیِ `/api/...` در موجِ ۴ کامل حذف شد؛ جزئیات:
[`Server/CLIENT-V1.md`](Server/CLIENT-V1.md).

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
cd Server/Northwind.Api
dotnet run                              # http://localhost:5080 — Swagger در /swagger

# ۲) کلاینت (ترمینالِ دوم)
cd Client/Northwind.Client
dotnet run                              # http://localhost:5081
```
پایگاه‌داده در `Server/Northwind.Api/data/northwind-api.db` ساخته و پُر می‌شود.
برای بازسازی از صفر، فایل‌های `northwind-api.db*` را پاک کنید و دوباره اجرا کنید.

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

---

## عیب‌یابی

### خطایِ NU1100 برایِ `Microsoft.NETCore.App.Runtime.Mono.browser-wasm`

> Unable to resolve 'Microsoft.NETCore.App.Runtime.Mono.browser-wasm (= 9.0.11)' …
> PackageSourceMapping is enabled, the following source(s) were not considered: nuget.org

این خطا **از پروژه نیست، از تنظیماتِ NuGet رویِ سیستمِ شماست**: «Package Source
Mapping» روشن است ولی برایِ این بسته الگویی به `nuget.org` نداده، بنابراین NuGet
آن منبع را حتی نگاه هم نمی‌کند.

سه راهِ رفع (به ترتیبِ سادگی):

1. **همین ریپو**: فایلِ `NuGet.config` در ریشهٔ Solution هست و همه‌چیز را به
   `nuget.org` نگاشت می‌کند. کافی است آخرین نسخهٔ پروژه را داشته باشید و
   `obj/` و `bin/` کلاینت را پاک کنید و دوباره Restore بزنید.
2. **تنظیماتِ ماشین**: در ویژوال استودیو →
   `Tools › NuGet Package Manager › Package Manager Settings` →
   `Package Sources` (مطمئن شوید `nuget.org` تیک دارد) و بعد
   `Package Source Mapping` → اگر الگویی مثلِ `*` به `nuget.org` نیست، اضافه کنید
   یا Mapping را کامل بردارید.
3. **خط فرمان**: `dotnet nuget list source` و بعد
   `dotnet nuget remove source <نامِ فیدِ خصوصی>`؛ یا فایلِ
   `%APPDATA%\NuGet\NuGet.Config` را باز کنید و بخشِ `<packageSourceMapping>`
   را حذف کنید.

بعد از هر سه راه: `dotnet nuget locals http-cache --clear` و سپس
`dotnet restore Aria.sln`.
