# از کجا شروع کنم؟ (بعد از دانلودِ سورس)

## کوتاه‌ترین پاسخ

**فایلی که باید باز کنید:**

```
Client/Northwind.Client/Northwind.Client.csproj
```

دابل‌کلیک کنید (یا راست‌کلیک ← Open with) — Visual Studio، Rider و VS Code
همگی فایلِ `.csproj` را مستقیماً باز می‌کنند.

> فایلِ `Aria.sln` هم در ریشه هست اما **اختیاری** است. فایلِ Solution فقط یک
> فهرست از پروژه‌هاست؛ وقتی یک پروژه بیشتر نداریم، باز کردنِ خودِ `.csproj`
> دقیقاً همان نتیجه را می‌دهد. اگر در دانلودِ شما نیست، مشکلی نیست —
> یا دوباره دانلود کنید، یا خودتان با دو دستور بسازید (پایینِ همین صفحه).
>
> **برای خواندنِ کد:** از `Client/Northwind.Client/Program.cs` شروع کنید.

این یک پروژهٔ **Blazor WebAssembly** است، نه یک سایتِ HTML ساده — پس فایلِ `index.html`
را به‌تنهایی نمی‌توان در مرورگر باز کرد؛ باید با `dotnet` اجرا شود.

---

## ۱. اجرا کردن

### راهِ اول — خط فرمان (همهٔ سیستم‌ها)

```bash
cd Aria
dotnet run --project Client/Northwind.Client
```

سپس مرورگر را باز کنید روی: **http://localhost:5210**

> اگر گفت `dotnet یافت نشد`: باید **.NET 9 SDK** را نصب کنید
> (از [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) — نسخهٔ SDK 9).

### راهِ دوم — IDE

| محیط | چه کار کنید |
|---|---|
| **Visual Studio** | `Aria.sln` را باز کنید ← F5 |
| **JetBrains Rider** | `Aria.sln` را باز کنید ← Run |
| **VS Code** | پوشهٔ `Aria` را باز کنید + افزونهٔ *C# Dev Kit* ← F5 |

### راهِ سوم — اسکریپت (روی لینوکس/مک)

```bash
bash Aria/start-preview.sh
```

این اسکریپت خودش SDK را چک می‌کند، در نبودش نصب می‌کند، بیلد می‌گیرد و روی پورت ۵۲۱۰ اجرا می‌کند.

### اگر `Aria.sln` را می‌خواهید ولی در فایلِ شما نیست

دو حالت دارد — یا دوباره دانلود کنید (اکنون در ریشه هست)، یا خودتان بسازید:

```bash
cd Aria
dotnet new sln -n Aria
dotnet sln add Client/Northwind.Client/Northwind.Client.csproj
```

یا ساده‌تر: بدونِ آن کار کنید، چون با یک پروژه تفاوتی نمی‌کند.

---

## ۲. ترتیبِ خواندنِ کد (اگر می‌خواهید بفهمید چه خبر است)

```
Aria/
├─ Aria.sln                    ← نقطهٔ ورود برای IDE
├─ ANALYSIS.md                 ← تحلیلِ پروژهٔ اصلی (فارسی)
├─ ROUTES.md                   ← نقشهٔ همهٔ صفحات و مسیرها
├─ START-HERE.md               ← همین فایل
└─ Client/Northwind.Client/
   ├─ Program.cs               ← ① از اینجا شروع کنید: ثبتِ سرویس‌ها در DI
   ├─ App.razor                ← ② مسیریاب (Router) و قالبِ کلی
   ├─ _Imports.razor           ← usingهای سراسری
   ├─ wwwroot/index.html       ← ③ شِلِ HTML (فونت و CSS اینجا وصل شده)
   ├─ wwwroot/css/             ← سیستمِ طراحی
   │   ├─ tokens.css           ← ④ متغیرهای رنگ/فاصله/سایه (اول این را ببینید)
   │   ├─ base.css             ← ریست و تایپوگرافی
   │   ├─ components.css       ← دکمه، کارت، جدول، فرم، سگمنت…
   │   ├─ app.css              ← هدر، قهرمان، پوستهٔ پورتال
   │   └─ utilities.css        ← کلاس‌های کمکی
   ├─ Models/Contracts.cs      ← ⑤ مدل‌های داده (DTOها)
   ├─ Services/
   │   ├─ IStoreApi.cs         ← ⑥ قراردادِ دسترسی به داده (این خطِ جداکننده است!)
   │   ├─ MockStoreApi.cs      ← پیاده‌سازیِ فعلی: دادهٔ ساختگی در حافظه
   │   ├─ MockData.cs          ← داده‌های نمونه
   │   └─ AppState.cs          ← احراز هویت، سبد، علاقه‌مندی، toast
   ├─ Shared/Layout/           ← سه پوسته: فروشگاه، پورتال، سایدبار
   ├─ Shared/UI/               ← ۱۷ کامپوننتِ بازاستفاده (Modal، Segmented، ستاره، نمودار…)
   └─ Pages/                   ← ۳۱ صفحه: Store/ · Auth/ · Dashboard/ · Admin/
```

### سه فایلی که بیش از بقیه مهم‌اند

1. **`Services/IStoreApi.cs`** — مرزِ بینِ ظاهر و داده. تمامِ صفحات فقط این را می‌شناسند.
2. **`wwwroot/css/tokens.css`** — تمامِ تصمیم‌های طراحی (رنگِ برند Indigo، سایه‌های نرم، شتابِ انیمیشن) اینجا است.
3. **`Models/Contracts.cs`** — شکلِ داده‌ها؛ بعداً همان‌ها از API واقعی پر می‌شوند.

---

## ۳. پوشه‌هایی که می‌توانید پاک کنید

| مسیر | چیست | حجم |
|---|---|---|
| `extracted/` | سورسِ اصلیِ پروژه (Next.js) که تحلیل شد | بزرگ |
| `full-stack-blazor-software-storefront (1).zip` | همان فایلِ زیپِ اولیه | ~۵۹۰KB |
| `Client/Northwind.Client/bin/` و `obj/` | خروجیِ بیلد — با اولین `dotnet build` دوباره ساخته می‌شود | بزرگ |

**برای کار روی پروژه فقط این لازم است:**

```
Aria.sln
Client/Northwind.Client/     ← کلِ پروژهٔ Blazor (۷۳ فایل، حدود ۹۰۰KB)
```

---

## ۴. حساب‌های آماده برای ورود

بعد از اجرا به `/login` بروید (رمزِ همه: `Password123!`)

| ایمیل | نقش |
|---|---|
| `admin@northwindsoft.example` | مدیر — پنلِ مدیریت را هم می‌بیند |
| `casey.demo@example.com` | مشتری |
| `priya.demo@example.com` | مشتری |
| `marcus.demo@example.com` | مشتری |
| `hana.demo@example.com` | مشتری |

---

## ۵. نکتهٔ مهم دربارهٔ داده

الان همهٔ داده‌ها **در حافظه** هستند (`MockStoreApi`). یعنی:

- هر تغییری که در پنلِ مدیریت بدهید، تا وقتی صفحه را رفرش نکنید زنده است
- بعد از رفرش همه‌چیز به حالتِ اول برمی‌گردد
- برای وصل کردن به بک‌اندِ واقعی، فقط کافی است یک کلاسِ `HttpStoreApi`
  با همان امضاهای `IStoreApi` بنویسید و یک خط در `Program.cs` عوض شود —
  هیچ‌کدام از صفحات تغییر نمی‌کند.
