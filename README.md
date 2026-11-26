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

### ویندوز — یک کلیک، یک پورت (پیشنهادی)

```bat
start-windows.cmd
```

کلاینت را Release منتشر می‌کند و API را روی **http://localhost:5182** بالا می‌آورد؛
مرورگر خودش باز می‌شود. فقط **یک** پروسه لازم است — نه پنجرهٔ دوم، نه CORS، نه
تنظیمِ نشانیِ API.

### خط فرمان — فقط API، بدونِ هیچ اسکریپتی

```bash
dotnet build Aria.sln                            # یک‌بار: کلاینت هم بیلد می‌شود
dotnet run --project Server/Northwind.Api        # http://localhost:5182
```

API خودش سایت را هم سرو می‌کند: اگر `ASPNETCORE_WEBROOT` ست نشده باشد،
`Client/Northwind.Client/wwwroot` را به‌عنوانِ ریشهٔ فایل‌هایِ استاتیک می‌گیرد و
`_framework` را از خروجیِ بیلدِ کلاینت. پس با **یک** دستور، هم رابط و هم API
رویِ یک نشانی بالا می‌آید. (Swagger: `/swagger`)

> اگر کلاینت بیلد نشده باشد، لاگِ سرور صریح هشدار می‌دهد؛ چون لایهٔ API عمداً به
> پروژهٔ کلاینت ارجاع نمی‌دهد (قاعدهٔ وابستگیِ تک‌جهته در `Server/ARCHITECTURE.md`).

### حالتِ توسعه — کلاینت و سرور جدا

```bat
اجرای-همه.bat        :: دو پنجره: API روی ۵۱۸۲ و کلاینت روی ۵۲۱۲
```

یا دستی، در دو ترمینال:

```bash
dotnet run --project Server/Northwind.Api        # 5182
dotnet run --project Client/Northwind.Client     # 5212
```

نشانیِ API در `Client/Northwind.Client/wwwroot/appsettings.json` تعیین می‌شود
(پیش‌فرض `http://localhost:5182/`). اگر پورتِ سرور را عوض کردید، همان مقدار را
هم‌اهنگ کنید.

پایگاه‌داده در `Server/Northwind.Api/data/northwind-api.db` ساخته و پُر می‌شود.
برای بازسازی از صفر، فایل‌های `northwind-api.db*` را پاک کنید و دوباره اجرا کنید.

### اسکریپتهایِ تک‌پورتی (لینوکس/مک و پیش‌نمایشِ ابری)

```bash
bash start-preview.sh    # انتشارِ کلاینت + اجرا روی 0.0.0.0:5210
bash start-server.sh     # فقط API روی 0.0.0.0:5211
```

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
