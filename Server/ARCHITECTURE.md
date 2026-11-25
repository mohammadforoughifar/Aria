# معماریِ آریا — چهار لایه

> نسخهٔ ۴ لایه · تاریخ: ۲۰۲۶-۱۱-۰۴
> این سند مرجعِ «کدام کد کجا برود» است. اگر جایی با این سند نمی‌خواند، سند درست است.

---

## ۱) نمایِ کلی

```
        ┌──────────────────────────────────────────────────────────────┐
        │  ① لایهٔ کلاینت — Northwind.Client  (Blazor WebAssembly)      │
        │     صفحه‌ها، مؤلفه‌ها، وضعیتِ رابط، V1Api (HttpClient)          │
        └───────────────────────────┬──────────────────────────────────┘
                                    │ HTTP/JSON  (api/v1/…)
                                    │ فقط قراردادهایِ Northwind.Shared
        ┌───────────────────────────▼──────────────────────────────────┐
        │  ③ لایهٔ API — Northwind.Api                                  │
        │     کنترلرها، میان‌افزارِ خطا، سیم‌کشیِ JWT و JSON              │
        └───────────────────────────┬──────────────────────────────────┘
                                    │ فراخوانیِ متد (بدونِ HTTP)
        ┌───────────────────────────▼──────────────────────────────────┐
        │  ② لایهٔ هسته — Northwind.Core                                │
        │     سرویس‌هایِ کاری، مدل‌هایِ ورودی/خروجی، نگاشت                │
        └───────────────────────────┬──────────────────────────────────┘
                                    │ DbContext (LINQ)
        ┌───────────────────────────▼──────────────────────────────────┐
        │  ④ لایهٔ داده — Northwind.Data                                │
        │     موجودیت‌ها، DbContext، نگاشت‌ها، مهاجرت‌ها، رشتهٔ اتصال      │
        └──────────────────────────────────────────────────────────────┘

        Shared/Northwind.Shared: قراردادهایِ سیم (DTO) — تنها چیزی که
        کلاینت و سرور هر دو می‌شناسند. کلاینت به هیچ لایهٔ سروری دسترسی ندارد.
```

## ۲) پروژه‌ها و مسئولیت‌ها

| لایه | پروژه | داخلش چه هست | داخلش چه نباید باشد |
|---|---|---|---|
| ① کلاینت | `Client/Northwind.Client` | `Pages/**`, `Shared/**` (مؤلفه‌ها)، `Services/**` (وضعیت + `V1Api`)، `Helpers/**` | دسترسی به EF، رشتهٔ اتصال، منطقِ تجاریِ سرور |
| ② هسته | `Server/Northwind.Core` | `Services/**` (منطقِ کاری)، `Models/**` (ViewModel)، `Mapping/**`، `DependencyInjection.AddNorthwindCore` | کنترلر، `HttpRequest`، کدِ SQL، `IServiceCollection`هایِ وب |
| ③ API | `Server/Northwind.Api` | `Controllers/**`, `Middleware/**`, `DependencyInjection.AddNorthwindApi` | منطقِ کاری، کوئریِ EF، محاسبهٔ مبلغ |
| ④ داده | `Server/Northwind.Data` | `Entities/**`, `Enums/**`, `Persistence/**` (DbContext + Configuration)، `Migrations/**`, `DatabaseOptions`, `DataRegistration` | سرویس، DTO، هر چیزی که «قاعدهٔ کاری» باشد |
| قرارداد | `Shared/Northwind.Shared` | رکوردهایِ DTO و enumهایی که رویِ سیم می‌روند | منطق، EF، وابستگی به سرور |
| میزبان | `Server/Northwind.Server` | `Program.cs` (فقط سیم‌کشی)، و API قدیمیِ در حالِ حذف (موج ۴) | کدِ تازه |

## ۳) قاعدهٔ وابستگی (تک‌جهته)

```
Client ──▶ Shared
Server ──▶ Api ──▶ Core ──▶ Data
```

* هیچ فلشِ برگشتی مجاز نیست. `Data` از `Core` خبر ندارد، `Core` از `Api` خبر ندارد و
  کلاینت هیچ‌کدام را نمی‌بیند.
* این قاعده در سطحِ `ProjectReference` اجبار شده است: اگر کسی در `Core` بنویسد
  `using Northwind.Api…`، بیلد می‌شکند — همان چیزی که می‌خواهیم.
* تنها استثنا: `Northwind.Server` (میزبان) هر دو `Api` و `Data` را می‌بیند، چون وظیفه‌اش
  فقط سیم‌کشیِ آغازین است.

## ۴) نقطه‌هایِ سیم‌کشی

| کجا | چه چیزی ثبت می‌شود | متد |
|---|---|---|
| `Northwind.Data/DataRegistration.cs` | `DatabaseOptions`، `NorthwindDbContext` (SQL Server یا SQLite)، ساخت/مهاجرتِ طرح | `AddNorthwindData()` · `InitializeNorthwindDatabaseAsync()` |
| `Northwind.Core/DependencyInjection.cs` | امنیت (`ITokenService`, `IPasswordHasher`, `ICurrentUser`) و همهٔ سرویس‌هایِ کاری | `AddNorthwindCore()` |
| `Northwind.Api/DependencyInjection.cs` | `AddNorthwindCore`، طرحِ احراز هویتِ JWT، کنترلرها، شکلِ یکسانِ خطا و اعتبارسنجی | `AddNorthwindApi()` |
| `Northwind.Server/Program.cs` | ترتیب: `AddNorthwindData` → `AddNorthwindApi` → `InitializeNorthwindDatabaseAsync` → Seed | — |

## ۵) الگویِ لایه‌ها در عمل

**کنترلر نازک است.** نمونهٔ واقعی:

```csharp
[HttpGet("orders")]
public Task<PagedViewModel<OrderViewModel>> Mine([FromQuery] ListQuery query,
    [FromQuery] OrderStatus? status, CancellationToken ct)
    => _orders.ListMineAsync(_current.Id, query, status, ct);
```

هیچ `if`، هیچ `Where`، هیچ محاسبه‌ای. اگر کنترلر بیش از یک خط شد، یعنی منطق جامانده است.

**سرویس، صاحبِ قاعده است.** `OrderService.CheckoutAsync` می‌داند مالیات چطور حساب می‌شود،
کدِ تخفیف چه وقت منقضی است و موجودیِ سبد کِی کم می‌شود.

**موجودیت از لایه بیرون نمی‌زند.** سرویس‌ها موجودیت را به `ViewModel` نگاشت می‌کنند
(`Mapping/Mappers.cs`)؛ API هرگز `Product` خام برنمی‌گرداند و کلاینت هرگز آن را نمی‌بیند.

**مدل‌ها دو دسته‌اند:**
* `Core/Models/**` — شکلِ پاسخِ API (این‌ها سرور را ترک می‌کنند).
* `Shared/Contracts` — DTOهایی که کلاینت مصرف می‌کند (مرزِ سیم).

**خطاها یک‌جا.** `Core/Services/Common/AppExceptions.cs` استثناهایِ دامنه
(`NotFoundException`, `ConflictException`, `ForbiddenException` …) را تعریف می‌کند؛
`Api/Middleware/ApiExceptionMiddleware` آن‌ها را به `{code, message, status, traceId}` با
پیامِ فارسیِ آمادهٔ نمایش تبدیل می‌کند. سرویس هیچ‌وقت `IActionResult` نمی‌سازد.

**دسترسی به پایگاه‌داده فقط از Core.** کنترلر و کلاینت `DbContext` نمی‌بینند؛
`NorthwindDbContext` تنها در `Northwind.Data` و سرویس‌هایِ `Northwind.Core` تزریق می‌شود.

## ۶) چک‌لیستِ افزودنِ یک قابلیتِ تازه

1. **داده** — اگر شکلِ داده عوض می‌شود: موجودیت در `Data/Entities`، نگاشت در
   `Data/Persistence/Configurations`، مهاجرت تازه.
2. **هسته** — سرویس در `Core/Services/<حوزه>/`، اینترفیس + پیاده‌سازی، مدلِ ورودی/خروجی در
   `Core/Models/<حوزه>/`، نگاشت در `Core/Mapping/Mappers.cs`، ثبت در `AddNorthwindCore`.
3. **API** — کنترلر یک‌خطی در `Api/Controllers/`، `[Authorize]`/`[AllowAnonymous]` مناسب،
   `[ProducesResponseType]` برایِ کدهایِ خطا.
4. **کلاینت** — DTO در `Services/V1/V1Models.cs`، متد در `IStoreApi` + `StoreApiRouter`،
   بعد صفحه.
5. **آزمون** — `python3 tools/api-smoke.py` (قرارداد) و آزمونِ Playwright همان صفحه.

## ۷) وضعیت: چهار لایه، بدونِ بدهیِ قدیمی

API قدیمیِ `/api/...` (۶ کنترلرِ میزبان + `AppDbContext` + `aria.db`) در **موج ۴**
کامل حذف شد. امروز:

* تنها لایهٔ API، `Northwind.Api` است و تنها پایگاه‌داده، `northwind-api.db`.
* میزبان (`Northwind.Server`) هیچ کنترلر و هیچ کدِ کاری ندارد؛ فقط `Program.cs`.
* `Northwind.Data` تنها جایی است که `DbContext` و رشتهٔ اتصال را می‌شناسد.

`Shared/Northwind.Shared` از نظرِ مفهومی «قرارداد» است نه لایهٔ پنجم؛ عمداً جدا
مانده چون کلاینتِ WASM نمی‌تواند به پروژه‌ای که EF دارد ارجاع بدهد.
