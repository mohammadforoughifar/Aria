# معماریِ بک‌اند — SQL Server + REST

این سند وضعیتِ **گامِ یک** را ثبت می‌کند: موجودیت‌ها و ویو‌مدل‌ها آماده‌اند و
پایگاه‌داده رویِ SQL Server تعریف شده است. سرویس‌ها و کنترلرها گامِ بعدی‌اند.

---

## ۱. لایه‌ها

```
Server/
├── Northwind.Domain/           ← موجودیت‌ها و enumها. به هیچ پروژه‌ای وابسته نیست.
├── Northwind.Infrastructure/   ← DbContext، پیکربندیِ نگاشت، مهاجرت‌ها، ثبتِ DI.
├── Northwind.Api/              ← ویو‌مدل‌ها + (در گامِ بعد) کنترلرها و سرویس‌ها.
├── Northwind.Server/           ← میزبان: ترکیبِ قطعات و سرو کردنِ کلاینتِ Blazor.
└── Scripts/                    ← اسکریپتِ SQL تولیدشده برایِ DBA.
```

جهتِ وابستگی یک‌طرفه است:

```
Northwind.Server ──▶ Northwind.Api ──▶ Northwind.Domain
        │                                   ▲
        └────────▶ Northwind.Infrastructure ┘
```

لایهٔ `Domain` هیچ بستهٔ بیرونی ندارد؛ نه EF، نه ASP.NET. همین باعث می‌شود
موجودیت‌ها مستقل از پایگاه‌داده و فریم‌ورک بمانند.

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

مهاجرت: `Server/Northwind.Infrastructure/Migrations/*_InitialCreate.cs`
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
dotnet ef database update --project Server/Northwind.Infrastructure
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
- **enumها همان ترتیبِ عددیِ `Northwind.Shared` را دارند** (با آزمون تأیید شد)؛
  چون رویِ سیم به‌صورتِ عدد می‌روند.
- **ورودی‌ها اعتبارسنجیِ اعلانی دارند** (`[Required]`، `[Range]`، `[EmailAddress]`)
  با پیامِ فارسی؛ کنترلر فقط `ModelState` را بررسی می‌کند.
- **هیچ ویو‌مدلی به موجودیتِ EF وابسته نیست**؛ نگاشت در لایهٔ سرویس انجام می‌شود.

---

## ۵. گامِ بعد — سرویس‌ها و کنترلرها

```
Northwind.Api/
├── Controllers/
│   ├── ProductsController.cs      GET/POST /api/products، GET/PUT/DELETE /api/products/{id}
│   ├── CategoriesController.cs
│   ├── CartController.cs
│   ├── OrdersController.cs
│   ├── LicensesController.cs
│   ├── TicketsController.cs
│   ├── ArticlesController.cs
│   └── Admin/…
└── Services/                      ← منطقِ کسب‌وکار، پشتِ اینترفیس
    ├── IProductService.cs / ProductService.cs
    ├── ICartService.cs   / CartService.cs
    ├── IOrderService.cs  / OrderService.cs
    └── …
```

قراردادِ REST که پیاده می‌شود:

| عمل | مسیر | پاسخ |
|---|---|---|
| فهرست | `GET /api/products?page=1&pageSize=12&search=…&sort=price_desc` | `200` + `PagedViewModel<ProductListItemViewModel>` |
| یک آیتم | `GET /api/products/{id}` | `200` / `404` |
| ساخت | `POST /api/products` | `201` + `Location` |
| جایگزینی | `PUT /api/products/{id}` | `204` / `404` / `409` |
| حذف | `DELETE /api/products/{id}` | `204` / `404` / `409` (وقتی سفارشی دارد) |
| خطا | — | `ApiErrorViewModel` با کدِ ماشین‌خوان و پیامِ فارسی |

نکتهٔ مهم: کلیدها `Guid` شده‌اند. کلاینت فعلی شناسه را رشته می‌فرستد و
دریافت می‌کند، پس در JSON همان رشته می‌ماند و مسیرها (`/products/{id}`) دست‌نخورده کار می‌کنند.
