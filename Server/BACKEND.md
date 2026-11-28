# معماریِ بک‌اند — SQL Server + REST

این سند وضعیتِ فعلی را ثبت می‌کند: پایگاه‌داده رویِ SQL Server، موجودیت‌ها،
ویو‌مدل‌ها، **سرویس‌ها و کنترلرهایِ REST** آماده و آزموده‌اند.

> ساختارِ چهارلایه (کلاینت · هسته · API · داده): [`ARCHITECTURE.md`](ARCHITECTURE.md)

---

## ۱. لایه‌ها

```
Client/Northwind.Client/   ① کلاینت  ← Blazor WASM + Contracts/ (DTOهایِ سیم)
Server/Northwind.Core/     ② هسته    ← سرویس‌هایِ کاری، مدل‌ها، نگاشت
Server/Northwind.Api/      ③ API     ← کنترلرها + Program.cs + appsettings + data/
Server/Northwind.Data/     ④ داده    ← موجودیت‌ها، DbContext، نگاشت‌ها، مهاجرت‌ها
Server/Scripts/                     ← اسکریپتِ SQL تولیدشده برایِ DBA
```

جهتِ وابستگی یک‌طرفه و اجباری است:

```
Northwind.Api ──▶ Northwind.Core ──▶ Northwind.Data
```

`Northwind.Api` خودش اجرا می‌شود (`Microsoft.NET.Sdk.Web`)؛ پروژهٔ جداگانه‌ای به نامِ
«Server» وجود ندارد.

---

## ۲. تصمیم‌هایِ طراحی

| موضوع | تصمیم | چرا |
|---|---|---|
| کلیدِ اصلی | `Guid` با `Guid.CreateVersion7()` | نسخهٔ ۷ زمان‌مرتب است، پس درج در ایندکسِ خوشه‌ایِ SQL Server پرشِ صفحه نمی‌سازد. `NEWID()` این ویژگی را ندارد. |
| مبالغ | `int` به تومان | تومان واحدِ خرد ندارد؛ `decimal` لازم نیست. همهٔ نام‌ها به `Toman` ختم می‌شوند. |
| لیست‌هایِ رشته‌ای | ستونِ JSON (`nvarchar(max)`) | EF Core ۹ مجموعه‌هایِ ابتدایی را خودش JSON می‌کند. نیازی به `FeaturesJson` دستی نیست. |
| هم‌زمانی | `rowversion` رویِ ۲۲ موجودیت | ویرایشِ هم‌زمان دو مدیر را می‌گیرد. رویِ SQLite خودکار غیرفعال می‌شود. |
| حذف | `Restrict` برایِ اسنادِ مالی، `Cascade` برایِ فرزندانِ واقعی | سفارش و لایسنس با حذفِ کاربر از بین نمی‌روند. |
| زمان | همه UTC، `datetime2(3)` | کلاینت خودش به وقتِ محلی تبدیل می‌کند. |
| شِما | `dbo` با نامِ جدولِ جمع | قراردادِ متعارفِ SQL Server. |

### نگاشت به SQLite (حالتِ نمایشی)
`NorthwindDbContext` هنگامِ ساختِ مدل، اگر موتور SQLite باشد، شِما و
نوع‌هایِ اختصاصیِ SQL Server را کنار می‌گذارد (`ResetProviderSpecificMappings`).
یک مدل، دو موتور، بدونِ کلاسِ جداگانه.

---

## ۳. پایگاه‌داده

| سنجه | مقدار |
|---|---|
| جدول | ۲۳ |
| ایندکس | ۴۴ (۱۲ یکتا) |
| کلیدِ خارجی | ۱۸ |
| ستونِ `rowversion` | ۲۲ |
| ستونِ JSON | ۸ |

مهاجرت: `Server/Northwind.Data/Migrations/*_InitialCreate.cs`
اسکریپتِ آماده برایِ اجرا رویِ سرور: `Server/Scripts/001-InitialCreate.sql` (۹۰۵ خط، idempotent).

### اجرا با SQL Server

```bash
docker compose up -d          # SQL Server 2022 روی پورت ۱۴۳۳
```

سپس در `appsettings.json` (یا متغیرِ محیطی):

```json
{
  "Database": {
    "ConnectionString": "Server=localhost,1433;Database=NorthwindStorefront;User Id=sa;Password=Your_password123;TrustServerCertificate=True;",
    "ApplyMigrationsOnStartup": true,
    "SeedDemoData": true
  }
}
```

مهاجرت‌ها هنگامِ شروع خودکار اعمال می‌شوند. برایِ اجرایِ دستی:

```bash
dotnet ef database update --project Server/Northwind.Data
```

### حالتِ نمایشی (بدونِ SQL Server)
اگر `Database:ConnectionString` خالی باشد، برنامه رویِ SQLite بالا می‌آید
(`App_Data/northwind-preview.db`) و طرح را از رویِ مدل می‌سازد. این حالت فقط
برایِ دیدنِ سایت است، نه برایِ بارِ واقعی.

---

## ۴. ویو‌مدل‌ها

| پوشه | محتوا |
|---|---|
| `ViewModels/Common` | `PagedViewModel<T>`، `ListQuery` (صفحه/اندازه/جست‌وجو/مرتب‌سازی)، `ApiErrorViewModel` |
| `ViewModels/Catalog` | دسته، محصول (فهرست و جزئیات)، طرح، نسخه، دیدگاه |
| `ViewModels/Commerce` | سبد، تسویه، سفارش، پرداخت، فاکتور، لایسنس، دریافت، تخفیف |
| `ViewModels/Support` | تیکت و پیام، اعلان، تماس، درخواستِ دمو |
| `ViewModels/Content` | مقاله، نقلِ‌قول، پرسش، تنظیمات، داشبوردِ مدیر و مشتری |
| `ViewModels/Account` | کاربر، مشتری، ورود، ثبت‌نام، پروفایل، تغییرِ رمز |

قواعدی که رعایت شده:

- **نامِ فیلدها عیناً همان قراردادِ فعلیِ کلاینت است** تا وقتی سرویس‌ها وصل
  شوند، کلاینت بدونِ تغییر کار کند.
- **enumها همان ترتیبِ عددیِ قراردادِ کلاینت (`Client/.../Contracts`) را دارند** (با آزمون تأیید شد)؛
  چون رویِ سیم به‌صورتِ عدد می‌روند.
- **ورودی‌ها اعتبارسنجیِ اعلانی دارند** (`[Required]`، `[Range]`، `[EmailAddress]`)
  با پیامِ فارسی؛ کنترلر فقط `ModelState` را بررسی می‌کند.
- **هیچ ویو‌مدلی به موجودیتِ EF وابسته نیست**؛ نگاشت در لایهٔ سرویس انجام می‌شود.

---

## ۵. سرویس‌ها

منطقِ کسب‌وکار در `Northwind.Core/Services` و پشتِ اینترفیس است. کنترلرها فقط
درخواست را می‌گیرند، سرویس را صدا می‌زنند و پاسخِ HTTP می‌سازند.

| سرویس | مسئولیت | نکتهٔ مهم |
|---|---|---|
| `CategoryService` | دسته‌ها و شمارشِ محصولات | حذفِ دستهٔ دارایِ محصول → ۴۰۹ |
| `ProductService` | فهرست/جزئیات/CRUD، طرح‌ها، نسخه‌ها | میانگینِ امتیاز در یک پرس‌وجویِ جدا (بدونِ N+1) |
| `ReviewService` | ثبت و صفِ بررسیِ دیدگاه | دیدگاه تازه همیشه `Pending` است |
| `CartService` | سبدِ مهمان و کاربر | سبد موجودیتِ پایدار نیست؛ شناسه از کلیدِ نشست |
| `OrderService` | تسویه، سفارش، فاکتور | مبالغ فقط سمتِ سرور؛ همه‌چیز در یک تراکنش |
| `LicenseService` | صدور، تمدید، ابطال، فعال‌سازی | ظرفیتِ `Seats` کنترل می‌شود |
| `DownloadService` | فایل‌هایِ در دسترس و تاریخچه | پرچمِ «لایسنس دارد» از لایسنس‌هایِ فعال |
| `DiscountService` | CRUD و اعتبارسنجیِ کد | چهار حالتِ خطا با کدِ ماشین‌خوان |
| `TicketService` | تیکت و رشتهٔ گفتگو | وضعیت با هر پیام جلو می‌رود |
| `NotificationService` | اعلان‌ها و شمارشِ خوانده‌نشده | — |
| `ContentService` | مقاله، نقلِ‌قول، پرسش، تنظیمات | تنظیمات یک رکوردِ تکی |
| `ContactService` | تماس و درخواستِ دمو | — |
| `AccountService` | ورود، ثبت‌نام، پروفایل، داشبورد | هشِ PBKDF2؛ توکنِ JWT |
| `AdminService` | شاخص‌هایِ پنلِ مدیر | درآمدِ ۱۲ ماه و پرفروش‌ها |

### قواعدی که در همهٔ سرویس‌ها رعایت شده

- **خطا با استثنا**: `NotFoundException`، `ConflictException`، `ForbiddenException`،
  `BusinessRuleException`. میان‌افزار آن‌ها را به ۴۰۴/۴۰۹/۴۰۳/۴۲۲ و
  `ApiErrorViewModel` تبدیل می‌کند.
- **مالکیت بررسی می‌شود**: هر سرویسِ شخصی، `UserId` را با مالکِ داده مقایسه
  می‌کند؛ نه فقط با `[Authorize]` بسنده می‌کند.
- **مبالغ سمتِ سرور**: هیچ مبلغی از بدنهٔ درخواست پذیرفته نمی‌شود؛ قیمت از
  طرح خوانده و مالیات از تنظیمات حساب می‌شود.
- **بدونِ N+1**: نگاشت‌ها با `Include` یا پرس‌وجویِ تجمیعیِ جدا انجام می‌شوند.

## ۶. کنترلرها و مسیرها

همهٔ مسیرها زیرِ `/api/v1` هستند و **API قدیمی وجود ندارد** (موج ۴). نسخه‌بندی
عمداً نگه داشته شده تا اگر روزی تغییرِ ناسازگار لازم شد، `v2` کنارِ `v1` بیاید.

| گروه | مسیر | دسترسی |
|---|---|---|
| محصولات | `GET /api/v1/products`، `/{id}`، `/slug/{slug}`، `/featured` | عمومی |
| محصولات | `POST`، `PUT /{id}`، `DELETE /{id}` | مدیر |
| طرح‌ها | `GET/POST /products/{id}/plans`، `PUT/DELETE /plans/{id}` | عمومی / مدیر |
| نسخه‌ها | `GET/POST /products/{id}/versions` | عمومی / مدیر |
| دسته‌ها | `GET /categories`، `POST`، `PUT`، `DELETE` | عمومی / مدیر |
| دیدگاه | `GET /products/{id}/reviews`، `POST /reviews` | عمومی / کاربر |
| دیدگاه | `GET /admin/reviews`، `PATCH /admin/reviews/{id}/status` | مدیر |
| سبد | `GET /cart`، `POST /cart/items`، `PUT/DELETE /cart/items/{id}`، `POST /cart/discount` | عمومی |
| تسویه | `POST /checkout` | کاربر |
| سفارش | `GET /orders`، `/orders/{id}`، `/invoices`، `/invoices/{id}` | کاربر |
| سفارش | `GET /admin/orders`، `PATCH /admin/orders/{id}/status` | مدیر |
| لایسنس | `GET /licenses`، `POST /licenses/{id}/action` | کاربر |
| لایسنس | `GET /admin/licenses` | مدیر (فهرست؛ صدورِ دستی برداشته شده) |
| دریافت | `GET /downloads/available`، `POST /downloads`، `GET /downloads` | عمومی / کاربر |
| تخفیف | `POST /discounts/validate` | عمومی |
| تخفیف | `GET/POST/PUT/DELETE /admin/discounts` | مدیر |
| تیکت | `GET/POST /tickets`، `/{id}`، `/{id}/messages` | کاربر |
| تیکت | `GET /admin/tickets`، `PATCH /admin/tickets/{id}/status` | مدیر |
| اعلان | `GET /notifications`، `/unread-count`، `POST /{id}/read`، `/read-all` | کاربر |
| محتوا | `GET /articles`، `/articles/{slug}`، `/faqs`، `/testimonials`، `/settings` | عمومی |
| محتوا | `POST/PUT/DELETE /admin/articles`، `PUT /admin/settings` | مدیر |
| ارتباط | `POST /contact`، `POST /demo-requests` | عمومی |
| ارتباط | `GET /admin/contact-messages`، `GET /admin/demo-requests` | مدیر |
| حساب | `POST /auth/login`، `/auth/register` | عمومی |
| حساب | `GET/PUT /account/me`، `POST /account/password`، `GET /account/overview` | کاربر |
| حساب | `GET /admin/customers`، `PUT /admin/customers/{id}`، `GET /admin/overview` | مدیر |

### احراز هویت
- رمز با **PBKDF2-SHA256** (۱۰۰٬۰۰۰ دور، نمکِ تصادفی، مقایسهٔ زمان‌ثابت).
- توکنِ **JWT** با HMAC-SHA256؛ نقش در claim می‌آید و `[Authorize(Roles="Admin")]`
  از آن استفاده می‌کند.
- پیامِ خطایِ ورود برایِ «کاربرِ ناموجود» و «رمزِ غلط» یکسان است تا ایمیل‌هایِ
  ثبت‌شده قابلِ شناسایی نباشند.
- کلیدِ امضا در `Jwt:Key` است؛ **در محیطِ واقعی باید از متغیرِ محیطی بیاید.**

### شکلِ خطا
```json
{
  "code": "category_not_empty",
  "message": "این دسته محصول دارد و حذف نمی‌شود. اول محصولاتش را جابه‌جا یا حذف کنید.",
  "status": 409,
  "traceId": "0HN7..."
}
```
خطاهایِ اعتبارسنجی هم فیلدبه‌فیلد برمی‌گردند (`code: validation_failed` + `errors`).

## ۷. دادهٔ نمایشی

`DemoSeeder` (در `Northwind.Core/Services/Seed`) فقط وقتی اجرا می‌شود که جدولِ
محصولات خالی باشد: ۸ دسته، ۱۲ محصول با ۳۶ طرح و ۲۴ نسخه، ۵ کاربر، ۷ سفارش،
۳ تیکت، ۶ مقاله، ۵ دیدگاه، ۲ کدِ تخفیف و چند پیام و اعلان.

**رمزِ همهٔ حساب‌هایِ نمایشی:** `Password123!`
- مدیر: `admin@northwindsoft.example`
- مشتری: `casey.demo@example.com`

## ۸. آزمون

`/home/user/scratch/api-smoke.py` — ۸۴ بررسیِ سرتاسری رویِ سرورِ در حالِ اجرا:
کاتالوگ، صفحه‌بندی، فیلتر، محتوا، اعتبارسنجی، ورود، مجوزها، چرخهٔ کاملِ
سبد→تسویه→لایسنس، و عملیاتِ مدیریتی. نتیجه: **۸۴/۸۴**.

## ۹. گامِ بعد

1. اتصالِ رابطِ Blazor به `/api/v1` صفحه‌به‌صفحه (شکلِ پاسخ‌ها هم‌تراز است).
2. بازنشسته‌کردنِ کنترلرها و `DbContext` و `Seed`/`DemoData` قدیمی.
3. Refresh Token و خروجِ سمتِ سرور.
4. آزمونِ واحد برایِ سرویس‌ها و آزمونِ یکپارچگی با `WebApplicationFactory`.

---

## ۱۰) مبالغ پولی: `decimal(18,2)`

همهٔ مبلغ‌ها **`decimal`** هستند، نه `int`. دلیل: سقفِ `int` حدودِ ۲٫۱ میلیارد است و
برایِ قراردادهایِ سازمانی (میلیاردی) کم می‌آید. با `decimal(18,2)` تا حدودِ ۱۰^۱۶ تومان
جا دارد و دو رقمِ اعشار هم برایِ محاسبهٔ درصدِ تخفیف و مالیات لازم است.

| لایه | جزئیات |
|---|---|
| دامنه | `Plan.PriceToman`, `Product.BasePriceToman`, `Order.Subtotal/Discount/Tax/TotalToman`, `OrderItem.UnitPrice/LineTotalToman`, `Invoice.Tax/TotalToman`, `Payment.AmountToman` — همه `decimal` |
| پایگاه‌داده (SQL Server) | قاعده‌ای یک‌جا در `NorthwindDbContext.OnModelCreating`: هر ستونِ `decimal` با `HasPrecision(18, 2)` |
| پایگاه‌داده (SQLite) | برایِ اینکه مرتب‌سازی و `SUM` رویِ مبلغ کار کند، به `double` نگاشت می‌شود (`SetProviderClrType`) |
| گرد کردن | فقط در یک جا: `Services/Common/Money.cs` → `Math.Round(x, 2, AwayFromZero)` |
| مالیات | `TaxRateBps` هنوز `int` است (۹۰۰ = ۹٪)؛ محاسبه `taxable * bps / 10_000m` و بعد گرد |
| اعتبارسنجی | `[Range(typeof(decimal), "0", "9999999999999999.99")]` — `[Range(0, int.MaxValue)]` رویِ فیلدِ decimal خودش `OverflowException` می‌دهد |
| قراردادِ JSON | همان عدد (بدونِ رشته)، پس کلاینت‌ها چیزی نمی‌فهمند |

مهاجرت: `Migrations/*_MoneyToDecimal.cs` (۲۲ `ALTER COLUMN`)؛ اسکریپتِ SQL Server:
`Server/Scripts/002-MoneyToDecimal.sql` (idempotent، ۱۶۷ خط).

> **توجه:** پشتهٔ قدیمی (که مبلغ‌ها را `int` نگه می‌داشت) در موج ۴ کامل حذف شد؛
> امروز تنها پشتهٔ `decimal` وجود دارد.

---

## ۱۱) خریدِ مهمان و تصاحبِ حساب

`POST /api/v1/checkout` حالا `[AllowAnonymous]` است:

| حالت | رفتار |
|---|---|
| با توکن | سفارش به همان کاربر وصل می‌شود (مثلِ قبل) |
| بدونِ توکن (مهمان) | سبد با `sessionKey` خوانده می‌شود؛ خریدار با **ایمیلِ صورتحساب** پیدا یا ساخته می‌شود |
| حسابِ ساختهٔ مهمان | رمزِ تصادفی دارد؛ تا وقتی صاحبش ثبت‌نام نکند کسی با آن وارد نمی‌شود |
| ثبت‌نام با همان ایمیل | حسابِ «بی‌صاحب» (بدونِ `LastLoginAt`) تصاحب می‌شود و سفارش‌هایِ مهمان به کاربر می‌رسند |

منطق: `OrderService.ResolveBuyerAsync` و `AccountService.RegisterAsync`.

> ⚠️ این تصاحب به «تأییدِ ایمیل» وابسته نیست چون کلِ پروژه هنوز سرویسِ ایمیل
> ندارد. پیش از بهره‌برداریِ واقعی، تصاحب باید پشتِ لینکِ تأییدِ ایمیل برود.
