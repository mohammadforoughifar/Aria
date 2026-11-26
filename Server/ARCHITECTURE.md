# معماریِ آریا — چهار لایه

> نسخهٔ ۴ لایه · تاریخ: ۲۰۲۶-۱۱-۰۴
> این سند مرجعِ «کدام کد کجا برود» است. اگر جایی با این سند نمی‌خواند، سند درست است.

---

## ۱) نمایِ کلی

```
        ┌──────────────────────────────────────────────────────────────┐
        │  ① Northwind.Client — لایهٔ کلاینت (Blazor WebAssembly)        │
        │     صفحه‌ها، کامپوننت‌ها، وضعیتِ رابط، V1Api، و قراردادهایِ سیم     │
        │     (Contracts/) — تنها لایهٔ اجراشده در مرورگر                 │
        └───────────────────────────┬──────────────────────────────────┘
                                    │ HTTP/JSON  (api/v1/…)
        ┌───────────────────────────▼──────────────────────────────────┐
        │  ③ Northwind.Api — لایهٔ API (و نقطهٔ اجرایِ برنامه)           │
        │     کنترلرها، میان‌افزارِ خطا، سیم‌کشیِ JWT/JSON، Program.cs      │
        └───────────────────────────┬──────────────────────────────────┘
                                    │ فراخوانیِ متد (بدونِ HTTP)
        ┌───────────────────────────▼──────────────────────────────────┐
        │  ② Northwind.Core — لایهٔ هسته                                │
        │     سرویس‌هایِ کاری، مدل‌هایِ ورودی/خروجی، نگاشت                │
        └───────────────────────────┬──────────────────────────────────┘
                                    │ DbContext (LINQ)
        ┌───────────────────────────▼──────────────────────────────────┐
        │  ④ Northwind.Data — لایهٔ داده                                │
        │     موجودیت‌ها، DbContext، نگاشت‌ها، مهاجرت‌ها، رشتهٔ اتصال      │
        └──────────────────────────────────────────────────────────────┘

        دقیقاً چهار پروژه در Solution — نه یکی بیشتر.
```

## ۲) پروژه‌ها و مسئولیت‌ها

| لایه | پروژه | داخلش چه هست | داخلش چه نباید باشد |
|---|---|---|---|
| ① کلاینت | `Client/Northwind.Client` | `Pages/**`، `Components/**`، `Services/**` (وضعیت + `V1Api`)، `Helpers/**`، `Contracts/` (DTOهایِ سیم) | دسترسی به EF، رشتهٔ اتصال، منطقِ تجاریِ سرور |
| ② هسته | `Server/Northwind.Core` | `Services/**` (منطقِ کاری)، `Models/**` (ViewModel)، `Mapping/**`، `AddNorthwindCore` | کنترلر، `HttpRequest`، کدِ SQL |
| ③ API | `Server/Northwind.Api` | `Controllers/**`، `Middleware/**`، `DependencyInjection`، **`Program.cs`**، `appsettings*.json`، `data/` | منطقِ کاری، کوئریِ EF، محاسبهٔ مبلغ |
| ④ داده | `Server/Northwind.Data` | `Entities/**`، `Enums/**`، `Persistence/**`، `Migrations/**`، `DatabaseOptions`، `AddNorthwindData` | سرویس، DTO، هر چیزی که «قاعدهٔ کاری» باشد |

## ۳) قاعدهٔ وابستگی (تک‌جهته)

```
Northwind.Api ──▶ Northwind.Core ──▶ Northwind.Data
        (و Api همان میزبان است: Program.cs همین‌جاست)
```

* هیچ فلشِ برگشتی مجاز نیست: `Data` از `Core` خبر ندارد، `Core` از `Api` خبر ندارد و
  کلاینت هیچ‌کدام را نمی‌بیند.
* این قاعده در سطحِ `ProjectReference` اجبار شده است؛ اگر کسی در `Core` بنویسد
  `using Northwind.Api…`، بیلد می‌شکند — همان چیزی که می‌خواهیم.
* لایهٔ کلاینت هیچ ارجاعِ پروژه‌ای به سرور ندارد؛ فقط HTTP می‌شناسد.

## ۴) نقطه‌هایِ سیم‌کشی

| کجا | چه چیزی ثبت می‌شود | متد |
|---|---|---|
| `Northwind.Data/DataRegistration.cs` | `DatabaseOptions`، `NorthwindDbContext` (SQL Server یا SQLite)، ساخت/مهاجرتِ طرح | `AddNorthwindData()` · `InitializeNorthwindDatabaseAsync()` |
| `Northwind.Core/DependencyInjection.cs` | امنیت (`ITokenService`, `IPasswordHasher`, `ICurrentUser`) و همهٔ سرویس‌هایِ کاری | `AddNorthwindCore()` |
| `Northwind.Api/DependencyInjection.cs` | `AddNorthwindCore`، طرحِ احراز هویتِ JWT، کنترلرها، شکلِ یکسانِ خطا و اعتبارسنجی | `AddNorthwindApi()` |
| `Northwind.Api/Program.cs` | ترتیب: `AddNorthwindData` → `AddNorthwindApi` → `InitializeNorthwindDatabaseAsync` → Seed | — |

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

## ۷) وضعیت: چهار لایه، چهار پروژه، بدونِ بدهیِ قدیمی

* **چهار پروژه**، هر کدام یک لایه: `Northwind.Client` · `Northwind.Core` ·
  `Northwind.Api` · `Northwind.Data`.
* **`Northwind.Api` نقطهٔ اجراست** (`Microsoft.NET.Sdk.Web`). پروژهٔ جداگانه‌ای به نامِ
  «Server» وجود ندارد؛ `Program.cs`، `appsettings.json` و پوشهٔ `data/` داخلِ همین
  لایه‌اند چون میزبان همان API است.
* **قراردادهایِ سیم داخلِ کلاینت‌اند** (`Client/Northwind.Client/Contracts/`): سرور
  هیچ‌گاه آن‌ها را نمی‌بیند — سرور `Core/Models` را برمی‌گرداند و کلاینت آن‌ها را به
  مدلِ خودش نگاشت می‌کند. این‌طور پروژهٔ پنجم لازم نمی‌شود.
* API قدیمیِ `/api/…` و `aria.db` در موج ۴ کامل حذف شدند؛ تنها پایگاه‌داده
  `data/northwind-api.db` است.
