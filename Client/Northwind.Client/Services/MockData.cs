// دادهٔ نمایشیِ فارسی — آینهٔ seed.ts در سمت سرور.
// این داده فقط برای ساختِ «قالب» است و بعداً با پاسخ‌های واقعیِ API جایگزین می‌شود.

using Northwind.Client.Models;

using Northwind.Client.Contracts;

namespace Northwind.Client.Services;

public static class MockData
{
    private static DateTime DaysAgo(int d) => System.DateTime.UtcNow.AddDays(-d);
    private static DateTime DaysAhead(int d) => System.DateTime.UtcNow.AddDays(d);

    public static StoreState Build()
    {
        var s = new StoreState();

        /* ------------------------------ Settings ----------------------------- */
        s.Settings = new SiteSettingsDto(
            "فروغ آریا پارسیان",
            "support@northwindsoft.example",
            "sales@northwindsoft.example",
            "+۱ (۸۸۸) ۵۵۵-۰۱۴۲",
            "نرم‌افزار کسب‌وکاری که واقعاً منتشر می‌شود، مقیاس می‌یابد و می‌ماند",
            "ده ماژول یکپارچه ERP، برنامهٔ دسکتاپ و افزونه — با لایسنس دلخواه شما، روی زیرساخت خودتان، با پشتیبانی مهندسانی که پاسخ می‌دهند.",
            800, 30, false,
            "نسخهٔ ۲۰۲۵٫۲ فروغ آریا پارسیان منتشر شد — موتور قواعد مغایرت‌گیری، API نسخهٔ ۲ و سازندهٔ داشبورد بازطراحی‌شده.");

        /* ------------------------------ Users -------------------------------- */
        s.Users.AddRange(new[]
        {
            new UserDto("u-admin", "admin@northwindsoft.example", "الکساندرا ریز", Role.Admin, "فروغ آریا پارسیان", "+۱ ۵۵۵ ۰۱۴۲", "ایالات متحده", DaysAgo(420), DaysAgo(1)),
            new UserDto("u-casey", "casey.demo@example.com", "کیسی ویتفیلد", Role.Customer, "تولیدی ساحل", "+۱ ۵۵۵ ۰۱۸۸", "ایالات متحده", DaysAgo(365), DaysAgo(2)),
            new UserDto("u-priya", "priya.demo@example.com", "پریا راگاوان", Role.Customer, "توزیع پیشرو", "+۴۴ ۲۰ ۷۹۴۶", "بریتانیا", DaysAgo(300), DaysAgo(5)),
            new UserDto("u-marcus", "marcus.demo@example.com", "مارکوس دلگادو", Role.Customer, "گروه خرده‌فروشی دلگادو", "+۳۴ ۹۱ ۱۲۳", "اسپانیا", DaysAgo(240), DaysAgo(9)),
            new UserDto("u-hana", "hana.demo@example.com", "هانا کوبایاشی", Role.Customer, "دقت کوبایاشی", "+۸۱ ۳ ۱۲۳۴", "ژاپن", DaysAgo(180), DaysAgo(14)),
        });
        foreach (var u in s.Users) s.Passwords[u.Email] = "Password123!";
        s.CurrentUserId = "u-casey";

        /* ---------------------------- Categories ----------------------------- */
        s.Categories.AddRange(new[]
        {
            new CategoryDto("c-erp", "مجموعه ERP", "erp-suite", "ماژول‌های اصلی برنامه‌ریزی منابع سازمان برای مالی، منابع انسانی و عملیات.", "layers", 2),
            new CategoryDto("c-fin", "مالی و حسابداری", "finance-accounting", "دفتر کل، حساب‌های دریافتنی و پرداختنی، مالیات و تجمیع گزارش‌ها.", "calculator", 1),
            new CategoryDto("c-ops", "عملیات و انبار", "operations-inventory", "انبارگردانی، کنترل موجودی و اجرای تولید.", "boxes", 1),
            new CategoryDto("c-desk", "بهره‌وری دسکتاپ", "desktop-productivity", "برنامه‌های مستقل ویندوز برای کارهای روزمرهٔ کسب‌وکار.", "monitor", 3),
            new CategoryDto("c-add", "افزونه‌ها و یکپارچه‌سازی", "addons-integrations", "توسعهٔ پلتفرم با کانکتورها، درگاه‌ها و ابزارهای کمکی.", "puzzle", 3),
        });

        /* ----------------------------- Products ------------------------------ */
        s.Products.AddRange(new[]
        {
            Product(s, "p-erp", "northwind-erp-core", "هسته ERP فروغ آریا پارسیان",
                "منبع واحد حقیقت برای کل سازمان شما",
                "هستهٔ ERP فروغ آریا پارسیان پایهٔ این مجموعه است. داده‌های مالی، خرید، انبار و تولید را در یک هستهٔ تراکنشی واحد با کلاینت دسکتاپ مدرن و مبتنی بر نقش یکپارچه می‌کند.\n\nهر ماژول مجموعهٔ فروغ آریا پارسیان به این هسته متصل می‌شود و یک لایهٔ دادهٔ پایه را به اشتراک می‌گذارد، تا دیگر هرگز مجبور به مغایرت‌گیری دستی صفحات گسترده نباشید. راه‌اندازی با جادوگر نصب یک بعدازظهر طول می‌کشد و ابزار مهاجرت همراه، داده‌ها را از QuickBooks، Sage و پایگاه‌های SQL قدیمی وارد می‌کند.",
                ProductType.Module, "ویندوز ۱۰/۱۱ · سرور ۲۰۱۹+", "2025.2.1", 249000, 4.7m, 128, 982, "#4f46e5",
                new[]{"هستهٔ تراکنشی یکپارچه با تاریخچهٔ کامل حسابرسی","۱۴۰ گزارش آماده و سازندهٔ گزارشِ سفارشی","پشتیبانی چندشرکتی و چندارزی","موتور قواعد مغایرت‌گیری در نسخهٔ ۲۰۲۵٫۲","ابزار مهاجرت از QuickBooks، Sage و SQL"},
                new[]{"ویندوز سرور ۲۰۱۹ یا بالاتر","SQL Server 2019 / PostgreSQL 15","۱۶ گیگابایت رم","۴۰ گیگابایت فضای خالی"},
                "c-erp", "مجموعه ERP", "erp-suite", true, 30, 325,
                Plans(("تک‌کاربره", PlanTier.SingleUser, BillingType.OneTime, 249000, 1, false, "لایسنس دائمی برای یک دستگاه، شامل ۱۲ ماه به‌روزرسانی."),
                      ("۵ کاربر", PlanTier.MultiUser, BillingType.OneTime, 899000, 5, true, "لایسنس دائمی با دادهٔ پایهٔ مشترک برای ۵ کاربر نامی."),
                      ("سازمانی", PlanTier.Enterprise, BillingType.Annual, 199900, 250, false, "اشتراک سالانه، شرکت‌های نامحدود، مدیر موفقیت اختصاصی."))),

            Product(s, "p-fin", "northwind-financials", "مالی فروغ آریا پارسیان",
                "بستن حساب‌ها در چند روز، نه چند هفته",
                "ماژول مالی قلبِ حسابداری مجموعه است: دفتر کل، حساب‌های پرداختنی و دریافتنی، دارایی ثابت و گزارش‌های تجمیعی. صورتحساب بانکی خودکار، اسناد تکراری و چک‌لیست پایان دوره، زمان بستن ماهانه را تا ۶۰٪ کاهش می‌دهد.\n\nاین ماژول با IFRS و GAAP سازگار است و برای تیم‌های مالیِ ۵ تا ۵۰۰ نفره طراحی شده است.",
                ProductType.Module, "ویندوز ۱۰/۱۱ · سرور ۲۰۱۹+", "2025.2.0", 189000, 4.6m, 96, 745, "#0f766e",
                new[]{"دفتر کل چندشرکتی با تجمیع خودکار","تطبیق بانکی خودکار با یادگیریِ الگوها","دارایی ثابت و استهلاک با چند روش","بودجه‌بندی و پیش‌بینی جریان نقد","گزارش‌های مالیاتیِ قابل ارسال"},
                new[]{"نیازمند هستهٔ ERP فروغ آریا پارسیان ۲۰۲۵٫۱+","ویندوز سرور ۲۰۱۹ یا بالاتر","۸ گیگابایت رم"},
                "c-fin", "مالی و حسابداری", "finance-accounting", true, 30, 210,
                Plans(("تک‌کاربره", PlanTier.SingleUser, BillingType.OneTime, 189000, 1, false, "برای یک کاربر مالی با دسترسی کامل به دفتر کل."),
                      ("۵ کاربر", PlanTier.MultiUser, BillingType.OneTime, 649000, 5, true, "تیم مالیِ کوچک با تفکیک وظایف و تأیید دومرحله‌ای."),
                      ("سازمانی", PlanTier.Enterprise, BillingType.Annual, 159900, 250, false, "اشتراک سالانه با تجمیع چندشرکتی."))),

            Product(s, "p-inv", "northwind-inventory-control", "کنترل موجودی فروغ آریا پارسیان",
                "موجودیِ دقیق، بدون شمارش دستی",
                "کنترل موجودی، جریان کالا را از خرید تا مصرف ردیابی می‌کند: سفارش‌های خرید، رسید کالا، انتقال بین انبارها، ردیابی سریال و بچ، و هشدار نقطهٔ سفارش.\n\nبا اسکنر انبار فروغ آریا پارسیان جفت می‌شود تا انبارگردانیِ دوره‌ای بدون توقف خط انجام شود.",
                ProductType.Module, "ویندوز ۱۰/۱۱ · سرور ۲۰۱۹+", "2025.1.4", 149000, 4.4m, 74, 612, "#b45309",
                new[]{"ردیابی سریال و بچ با تاریخ انقضا","نقطهٔ سفارش خودکار بر اساس مصرف","انتقال بین‌انباری با تأیید دومرحله‌ای","انبارگردانی دوره‌ای بدون توقف عملیات","گزارش ارزش موجودی به روش FIFO و میانگین"},
                new[]{"نیازمند هستهٔ ERP فروغ آریا پارسیان","ویندوز سرور ۲۰۱۹ یا بالاتر","۸ گیگابایت رم"},
                "c-ops", "عملیات و انبار", "operations-inventory", true, 30, 122,
                Plans(("تک‌کاربره", PlanTier.SingleUser, BillingType.OneTime, 149000, 1, false, "یک انبار، یک کاربر، ردیابی کامل."),
                      ("۵ کاربر", PlanTier.MultiUser, BillingType.OneTime, 499000, 5, true, "چند انبار با کنترل دسترسیِ نقش‌محور."),
                      ("سازمانی", PlanTier.Enterprise, BillingType.Annual, 129900, 250, false, "اشتراک سالانه با ردیابی چندسایته."))),

            Product(s, "p-hr", "northwind-hr-payroll", "منابع انسانی و حقوق فروغ آریا پارسیان",
                "حقوق و دستمزدِ بی‌دردسر، هر ماه",
                "از قرارداد و مرخصی تا محاسبهٔ حقوق، بیمه و مالیات — در یک ماژول. قوانینِ محاسبه برای هر کشور به‌صورت بستهٔ الحاقی ارائه می‌شود و گزارش‌های قانونی بدون خروج از سیستم تولید می‌شوند.\n\nپورتال خودخدمت کارکنان، درخواست مرخصی و فیش حقوقی را مستقیم در اختیار تیم شما می‌گذارد.",
                ProductType.Module, "ویندوز ۱۰/۱۱ · سرور ۲۰۱۹+", "2025.2.0", 129000, 4.3m, 58, 431, "#be123c",
                new[]{"محاسبهٔ حقوق با بسته‌های قانونیِ هر کشور","پورتال خودخدمت کارکنان","مدیریت مرخصی، شیفت و اضافه‌کاری","گزارش‌های قانونی و فیش حقوقی","یکپارچگی با دفتر کلِ مالی"},
                new[]{"نیازمند هستهٔ ERP فروغ آریا پارسیان","ویندوز سرور ۲۰۱۹ یا بالاتر","۸ گیگابایت رم"},
                "c-erp", "مجموعه ERP", "erp-suite", false, 30, 44,
                Plans(("تک‌کاربره", PlanTier.SingleUser, BillingType.OneTime, 129000, 1, false, "تا ۵۰ کارمند."),
                      ("۵ کاربر", PlanTier.MultiUser, BillingType.OneTime, 429000, 5, true, "تا ۲۵۰ کارمند با پورتال خودخدمت."),
                      ("سازمانی", PlanTier.Enterprise, BillingType.Annual, 99900, 250, false, "اشتراک سالانه، کارمندان نامحدود."))),

            Product(s, "p-crm", "northwind-crm-desktop", "CRM دسکتاپ فروغ آریا پارسیان",
                "یک CRM سریع که روی دسکتاپ شما زندگی می‌کند",
                "CRMای که بدون نیاز به اینترنتِ دائم کار می‌کند و با ایمیل و تقویم شما همگام می‌شود. مدیریت سرنخ، فرصت، پیش‌فاکتور و پیگیری — در یک پنجرهٔ سبک که زیر یک ثانیه باز می‌شود.\n\nبرای تیم‌های فروشی طراحی شده که بیشترِ روز را در حال مکالمه‌اند و از تب‌های مرورگر خسته شده‌اند.",
                ProductType.DesktopApp, "ویندوز ۱۰/۱۱ · مک ۱۳ به بالا", "2025.2.1", 69000, 4.8m, 212, 1187, "#7c3aed",
                new[]{"قیف فروش با پیش‌بینیِ وزن‌دار","همگام‌سازی ایمیل و تقویم","پیش‌فاکتور و پیگیریِ خودکار","کار آفلاین و همگام‌سازی بعدی","گزارش فعالیتِ تیم فروش"},
                new[]{"ویندوز ۱۰ یا بالاتر / مک ۱۳+","۴ گیگابایت رم","۵۰۰ مگابایت فضای خالی"},
                "c-desk", "بهره‌وری دسکتاپ", "desktop-productivity", true, 30, 150,
                Plans(("تک‌کاربره", PlanTier.SingleUser, BillingType.OneTime, 69000, 1, true, "یک فروشنده، دستگاه نامحدود."),
                      ("۵ کاربر", PlanTier.MultiUser, BillingType.OneTime, 249000, 5, false, "تیم فروش کوچک با گزارش اشتراکی."),
                      ("سازمانی", PlanTier.Enterprise, BillingType.Annual, 79900, 250, false, "اشتراک سالانه با مدیریت نقش."))),

            Product(s, "p-pos", "northwind-point-of-sale", "صندوق فروشگاهی فروغ آریا پارسیان",
                "صندوقی که با قطع اینترنت هم می‌فروشد",
                "صندوق فروشگاهیِ مدرن برای خرده‌فروشیِ چندشعبه‌ای: فروش، مرجوعی، تخفیف، وفاداری و بستن شیفت — با پشتیبانی از بارکدخوان، کشوی پول و چاپگر رسید.\n\nتراکنش‌ها به‌صورت محلی ذخیره و با اولین اتصال با انبار و مالی همگام می‌شوند.",
                ProductType.DesktopApp, "ویندوز ۱۰/۱۱", "2025.1.8", 59000, 4.5m, 138, 803, "#0369a1",
                new[]{"فروش آفلاین با همگام‌سازی بعدی","پشتیبانی از بارکدخوان، کشوی پول و چاپگر","برنامهٔ وفاداری و کارت هدیه","بستن شیفت و مغایرت‌گیری صندوق","چندشعبه‌ای با قیمت‌گذاریِ محلی"},
                new[]{"ویندوز ۱۰ یا بالاتر","۴ گیگابایت رم","پورت USB برای سخت‌افزار جانبی"},
                "c-desk", "بهره‌وری دسکتاپ", "desktop-productivity", false, 30, 140,
                Plans(("تک‌کاربره", PlanTier.SingleUser, BillingType.OneTime, 59000, 1, false, "یک صندوق، یک شعبه."),
                      ("۵ کاربر", PlanTier.MultiUser, BillingType.OneTime, 199000, 5, true, "تا ۵ صندوق در یک شعبه."),
                      ("سازمانی", PlanTier.Enterprise, BillingType.Annual, 69900, 250, false, "اشتراک سالانه، شعب نامحدود."))),

            Product(s, "p-doc", "northwind-document-manager", "مدیریت اسناد فروغ آریا پارسیان",
                "هر سند، در جای درست، با نسخهٔ درست",
                "موتور جست‌وجوی تمام‌متن، نسخه‌بندی خودکار، گردش کارِ تأیید و نگهداریِ قانونی — برای سازمان‌هایی که اسنادشان با حسابرسی سر و کار دارد.\n\nپیوست‌ها مستقیم به مشتری، سفارش و فاکتور متصل می‌شوند و از هستهٔ ERP قابل دسترسی‌اند.",
                ProductType.DesktopApp, "ویندوز ۱۰/۱۱", "2025.1.2", 49000, 4.2m, 64, 366, "#4d7c0f",
                new[]{"جست‌وجوی تمام‌متن با OCR فارسی","نسخه‌بندی و قفلِ همزمانی","گردش کارِ تأیید چندمرحله‌ای","نگهداریِ قانونی با دورهٔ انقضا","اتصال به مشتری، سفارش و فاکتور"},
                new[]{"ویندوز ۱۰ یا بالاتر","۸ گیگابایت رم","فضای ذخیره‌سازی بر اساس حجم اسناد"},
                "c-desk", "بهره‌وری دسکتاپ", "desktop-productivity", false, 30, 21,
                Plans(("تک‌کاربره", PlanTier.SingleUser, BillingType.OneTime, 49000, 1, false, "یک کاربر، حجم نامحدود."),
                      ("۵ کاربر", PlanTier.MultiUser, BillingType.OneTime, 169000, 5, true, "تیم کوچک با گردش کار اشتراکی."),
                      ("سازمانی", PlanTier.Enterprise, BillingType.Annual, 59900, 250, false, "اشتراک سالانه با نگهداری قانونی."))),

            Product(s, "p-bi", "northwind-bi-connector", "کانکتور هوش تجاری فروغ آریا پارسیان",
                "دادهٔ زندهٔ ERP در ابزار تحلیلیِ شما",
                "یک لایهٔ معناییِ تمیز روی جداول ERP: جداولِ واقعیت و بُعدِ از پیش تعریف‌شده برای Power BI، Tableau و هر ابزار ODBC.\n\nبدون Query نوشتن، بدون دستکاریِ مستقیمِ جداول تولید — با امنیتِ سطحِ سطر و تازه‌سازیِ افزایشی.",
                ProductType.AddOn, "ویندوز سرور ۲۰۱۹+ · لینوکس", "2025.2.0", 99000, 4.6m, 51, 528, "#0891b2",
                new[]{"لایهٔ معناییِ آماده برای Power BI و Tableau","تازه‌سازی افزایشی با ردیابی تغییرات","امنیتِ سطحِ سطر بر اساس نقش","دسترسی ODBC/JDBC برای هر ابزار","بدون نیاز به Query نویسی"},
                new[]{"نیازمند هستهٔ ERP فروغ آریا پارسیان ۲۰۲۵٫۱+","ویندوز سرور ۲۰۱۹ یا لینوکس","۸ گیگابایت رم"},
                "c-add", "افزونه‌ها و یکپارچه‌سازی", "addons-integrations", true, 14, 90,
                Plans(("تک‌کاربره", PlanTier.SingleUser, BillingType.OneTime, 99000, 1, false, "یک تحلیل‌گر، یک مقصد داده."),
                      ("۵ کاربر", PlanTier.MultiUser, BillingType.OneTime, 349000, 5, true, "تیم داده با مقاصد نامحدود."),
                      ("سازمانی", PlanTier.Enterprise, BillingType.Annual, 89900, 250, false, "اشتراک سالانه با تازه‌سازیِ پرتکرار."))),

            Product(s, "p-einv", "northwind-einvoice-gateway", "درگاه فاکتور الکترونیکی فروغ آریا پارسیان",
                "ارسال فاکتور قانونی، بدون فرمت‌بندی دستی",
                "فاکتورهای فروش را مستقیماً از ERP به درگاه‌های مالیاتیِ محلی می‌فرستد: تبدیل فرمت، امضای دیجیتال، بایگانی و پیگیری وضعیت.\n\nخطاهای رد شدن، پیش از ارسال در صفِ اعتبارسنجی مشخص می‌شوند.",
                ProductType.AddOn, "ویندوز سرور ۲۰۱۹+ · ابری", "2025.1.5", 79000, 4.1m, 33, 297, "#a16207",
                new[]{"تبدیل خودکار به فرمت‌های مورد نیاز","امضای دیجیتال و مهر زمان","بایگانیِ قانونی و پیگیری وضعیت","اعتبارسنجیِ پیش از ارسال","گزارشِ ردیابی برای حسابرسی"},
                new[]{"نیازمند مالی فروغ آریا پارسیان","اتصال اینترنت پایدار","گواهی امضای دیجیتال"},
                "c-add", "افزونه‌ها و یکپارچه‌سازی", "addons-integrations", false, 14, 12,
                Plans(("تک‌کاربره", PlanTier.SingleUser, BillingType.OneTime, 79000, 1, false, "یک شرکت، یک درگاه."),
                      ("۵ کاربر", PlanTier.MultiUser, BillingType.OneTime, 279000, 5, true, "چند شرکت با بایگانی مشترک."),
                      ("سازمانی", PlanTier.Enterprise, BillingType.Annual, 69900, 250, false, "اشتراک سالانه با چند درگاه."))),

            Product(s, "p-scan", "northwind-warehouse-scanner", "اسکنر انبار فروغ آریا پارسیان",
                "انبارگردانی با موبایل، بدون کاغذ",
                "اپلیکیشن اندروید برای دریافت، جابه‌جایی، شمارش و بسته‌بندی — با بارکد و QR. عملیات به‌صورت آفلاین ثبت و بعداً همگام می‌شود.\n\nگزارش‌های انحراف، اختلافِ موجودی را همان لحظه نشان می‌دهند.",
                ProductType.AddOn, "اندروید ۱۰+", "2025.1.3", 39000, 4.4m, 41, 254, "#c2410c",
                new[]{"دریافت، جابه‌جایی و شمارش با بارکد","عملیات آفلاین و همگام‌سازی بعدی","گزارش انحرافِ لحظه‌ای","چاپ برچسبِ محلِ انبار","اتصال مستقیم به کنترل موجودی"},
                new[]{"نیازمند کنترل موجودی فروغ آریا پارسیان","اندروید ۱۰ یا بالاتر","دوربین یا اسکنر سخت‌افزاری"},
                "c-add", "افزونه‌ها و یکپارچه‌سازی", "addons-integrations", false, 14, 30,
                Plans(("تک‌کاربره", PlanTier.SingleUser, BillingType.OneTime, 39000, 1, false, "یک دستگاه در انبار."),
                      ("۵ کاربر", PlanTier.MultiUser, BillingType.OneTime, 149000, 5, true, "تیم انبار با ۵ دستگاه."),
                      ("سازمانی", PlanTier.Enterprise, BillingType.Annual, 39900, 250, false, "اشتراک سالانه، دستگاه نامحدود."))),
        });

        /* ------------------------------ Reviews ------------------------------ */
        s.Reviews.AddRange(new[]
        {
            new ReviewDto("r-1","p-erp","هسته ERP فروغ آریا پارسیان","کیسی ویتفیلد","تولیدی ساحل",5,"پایانِ مغایرت‌گیریِ دستی","بعد از مهاجرت، بستن ماهانه از ۹ روز به ۳ روز رسید. هستهٔ تراکنشی واقعاً یکپارچه است و حسابرسیِ کامل، خیالِ حسابرس را راحت کرد.",ReviewStatus.Approved,DaysAgo(210)),
            new ReviewDto("r-2","p-erp","هسته ERP فروغ آریا پارسیان","پریا راگاوان","توزیع پیشرو",4,"قدرتمند اما نیازمند برنامه‌ریزی","عملکرد عالی است، ولی نصب اولیه را با مشاور انجام دهید تا زمانِ استقرار نصف شود.",ReviewStatus.Approved,DaysAgo(170)),
            new ReviewDto("r-3","p-crm","CRM دسکتاپ فروغ آریا پارسیان","مارکوس دلگادو","گروه خرده‌فروشی دلگادو",5,"سبک، سریع، بی‌دردسر","تیم فروش از تب‌های مرورگر خلاص شد. سرعت باز شدن برنامه در حدِ یک ثانیه است.",ReviewStatus.Approved,DaysAgo(140)),
            new ReviewDto("r-4","p-crm","CRM دسکتاپ فروغ آریا پارسیان","هانا کوبایاشی","دقت کوبایاشی",5,"همگام‌سازیِ ایمیل عالی","پیگیری‌ها خودکار ثبت می‌شوند و دیگر چیزی از قلم نمی‌افتد.",ReviewStatus.Approved,DaysAgo(96)),
            new ReviewDto("r-5","p-fin","مالی فروغ آریا پارسیان","کیسی ویتفیلد","تولیدی ساحل",5,"تطبیق بانکیِ خودکار معجزه می‌کند","۹۰٪ تراکنش‌ها بدون دخالت تطبیق می‌خورند؛ فقط مواردِ استثنا را بررسی می‌کنیم.",ReviewStatus.Approved,DaysAgo(120)),
            new ReviewDto("r-6","p-pos","صندوق فروشگاهی فروغ آریا پارسیان","مارکوس دلگادو","گروه خرده‌فروشی دلگادو",4,"آفلاین واقعاً کار می‌کند","قطعی اینترنت دیگر فروش را متوقف نمی‌کند. همگام‌سازی بی‌دردسر است.",ReviewStatus.Approved,DaysAgo(64)),
            new ReviewDto("r-7","p-bi","کانکتور هوش تجاری فروغ آریا پارسیان","پریا راگاوان","توزیع پیشرو",5,"لایهٔ معناییِ بی‌نقص","داشبورد Power BI در یک روز بالا رفت، بدون نوشتنِ حتی یک Query.",ReviewStatus.Approved,DaysAgo(40)),
            new ReviewDto("r-8","p-inv","کنترل موجودی فروغ آریا پارسیان","سارا مرادی","پخش آریا",4,"نقطهٔ سفارشِ خودکار عالی است","موجودیِ ما دیگر کم نمی‌آورد. گزارش ارزش موجودی هم دقیق است.",ReviewStatus.Pending,DaysAgo(18)),
            new ReviewDto("r-9","p-doc","مدیریت اسناد فروغ آریا پارسیان","رضا کریمی",null,3,"OCR فارسی نیاز به بهبود دارد","جست‌وجو خوب است ولی OCR روی اسنادِ اسکن‌شدهٔ کم‌کیفیت خطا دارد.",ReviewStatus.Pending,DaysAgo(9)),
        });

        /* --------------------------- Testimonials ---------------------------- */
        s.Testimonials.AddRange(new[]
        {
            new TestimonialDto("t-1","کیسی ویتفیلد","مدیر مالی","تولیدی ساحل","زمان بستنِ ماهانهٔ ما از ۹ روز به ۳ روز رسید. این فقط یک تغییرِ نرم‌افزاری نبود، تغییرِ ریتمِ تیم بود.",5),
            new TestimonialDto("t-2","پریا راگاوان","مدیر عملیات","توزیع پیشرو","دادهٔ ما بالاخره در یک جا جمع شد. داشبوردها همان روز اول برای مدیران قابل استفاده بود.",5),
            new TestimonialDto("t-3","مارکوس دلگادو","مدیر فروش","گروه خرده‌فروشی دلگادو","CRM دسکتاپ را تیم فروش خودش انتخاب کرد؛ این بزرگ‌ترین تعریفی است که می‌توانم بکنم.",5),
        });

        /* -------------------------------- FAQs -------------------------------- */
        s.Faqs.AddRange(new[]
        {
            new FaqDto("f-1","آیا لایسنس‌ها دائمی هستند؟","پلن‌های «تک‌کاربره» و «۵ کاربر» لایسنس دائمی با ۱۲ ماه به‌روزرسانی هستند. پلن‌های سازمانی به‌صورت اشتراک سالانه ارائه می‌شوند."),
            new FaqDto("f-2","نصب روی سرور خودمان انجام می‌شود؟","بله. تمام ماژول‌ها روی زیرساخت خودتان نصب می‌شوند و داده‌ها از سازمان خارج نمی‌شود."),
            new FaqDto("f-3","مهاجرت از سیستم قبلی چقدر طول می‌کشد؟","برای یک شرکت متوسط، مهاجرت داده‌ها با ابزار همراه معمولاً یک تا دو هفته زمان می‌برد."),
            new FaqDto("f-4","آیا امکان تست قبل از خرید وجود دارد؟","بله، همهٔ محصولات ۳۰ روز نسخهٔ آزمایشی کامل دارند و نیازی به کارت اعتباری نیست."),
            new FaqDto("f-5","پشتیبانی چگونه است؟","پشتیبانیِ مهندسی از طریق تیکت با زمان پاسخِ اولیهٔ زیر ۴ ساعت در روزهای کاری."),
            new FaqDto("f-6","آیا API برای یکپارچه‌سازی دارید؟","بله، API نسخهٔ ۲ با مستندات OpenAPI در دسترس است و کلیدهای اختصاصی صادر می‌شود."),
        });

        /* ----------------------------- Discounts ------------------------------ */
        s.Discounts.AddRange(new[]
        {
            new DiscountCodeDto("d-1","LAUNCH25","تخفیف ویژهٔ عرضهٔ نسخهٔ ۲۰۲۵",25,500,138,DaysAhead(60),true,DaysAgo(120)),
            new DiscountCodeDto("d-2","BUNDLE15","تخفیف خریدِ بسته‌ایِ دو محصول یا بیشتر",15,0,64,null,true,DaysAgo(90)),
            new DiscountCodeDto("d-3","EDU40","تخفیف نهادهای آموزشی و دانشگاهی",40,100,12,DaysAhead(180),true,DaysAgo(45)),
            new DiscountCodeDto("d-4","WINTER10","کمپین زمستانی — منقضی شده",10,300,300,DaysAgo(20),false,DaysAgo(200)),
        });

        /* ------------------------------- Orders -------------------------------- */
        s.OrderSeq = 0;
        s.InvoiceSeq = 0;

        var orderSpecs = new (string User, string Product, int PlanIndex, int Qty, int Days, OrderStatus Status, string? Code)[]
        {
            ("u-casey","p-erp",1,1,210,OrderStatus.Completed,"LAUNCH25"),
            ("u-casey","p-fin",1,1,208,OrderStatus.Completed,null),
            ("u-casey","p-bi",1,2,96,OrderStatus.Completed,null),
            ("u-casey","p-crm",1,1,5,OrderStatus.Paid,null),
            ("u-priya","p-erp",2,1,175,OrderStatus.Completed,"BUNDLE15"),
            ("u-priya","p-inv",1,1,122,OrderStatus.Completed,null),
            ("u-priya","p-scan",1,1,38,OrderStatus.Paid,null),
            ("u-marcus","p-pos",1,2,150,OrderStatus.Completed,null),
            ("u-marcus","p-crm",0,3,64,OrderStatus.Completed,null),
            ("u-marcus","p-doc",1,1,21,OrderStatus.Paid,null),
            ("u-marcus","p-bi",0,1,30,OrderStatus.Refunded,null),
            ("u-hana","p-erp",1,1,118,OrderStatus.Completed,null),
            ("u-hana","p-hr",1,1,44,OrderStatus.Processing,null),
            ("u-hana","p-einv",0,1,12,OrderStatus.Paid,null),
        };

        foreach (var spec in orderSpecs)
        {
            var product = s.Products.First(p => p.Id == spec.Product);
            var plan = product.Plans[spec.PlanIndex];
            var user = s.Users.First(u => u.Id == spec.User);

            int percent = 0;
            if (spec.Code is not null)
                percent = s.Discounts.FirstOrDefault(d => d.Code == spec.Code)?.PercentOff ?? 0;

            var subtotal = plan.PriceToman * spec.Qty;
            var discount = subtotal * percent / 100;
            var taxable = subtotal - discount;
            var tax = (int)Math.Round(taxable * s.Settings.TaxRateBps / 10_000m);
            var total = taxable + tax;

            var order = new OrderDto(
                $"o-{s.Orders.Count + 1}", s.NextOrderNumber(), user.Id, user.FullName, user.Email, user.Company,
                spec.Status, subtotal, discount, tax, total, "USD", spec.Code, DaysAgo(spec.Days),
                user.FullName, user.Company, user.Email, user.Country, null,
                new[] { new OrderItemDto($"oi-{s.Orders.Count + 1}", product.Id, plan.Id, product.Name, plan.Name,
                        plan.BillingType, plan.PriceToman, spec.Qty, plan.PriceToman * spec.Qty) },
                new PaymentDto($"pay-{s.Orders.Count + 1}", $"o-{s.Orders.Count + 1}", "MockGateway", "card", total,
                        spec.Status == OrderStatus.Refunded ? PaymentStatus.Refunded : PaymentStatus.Succeeded,
                        $"txn_{Guid.NewGuid().ToString("N")[..12]}", DaysAgo(spec.Days)),
                null);

            var invoice = new InvoiceDto($"inv-{s.Invoices.Count + 1}", s.NextInvoiceNumber(), order.Id,
                spec.Status == OrderStatus.Refunded ? InvoiceStatus.Void : InvoiceStatus.Paid,
                DaysAgo(spec.Days), DaysAgo(spec.Days - 14), total, tax, null);

            order = order with { Invoice = invoice };
            s.Orders.Add(order);
            s.Invoices.Add(invoice);

            // صدور لایسنس به‌ازای هر تعداد
            if (spec.Status != OrderStatus.Refunded)
            {
                for (var i = 0; i < spec.Qty; i++)
                {
                    s.Licenses.Add(new LicenseDto(
                        $"lic-{s.Licenses.Count + 1}", Key(), user.Id, user.FullName, user.Email,
                        product.Id, product.Name, product.Slug, plan.Name, plan.Seats,
                        i == 0 ? Math.Min(1, plan.Seats) : 0,
                        LicenseStatus.Active, DaysAgo(spec.Days),
                        plan.BillingType == BillingType.Annual ? DaysAgo(spec.Days - 365) : null));
                }
            }
        }

        // چند لایسنسِ نزدیک‌به‌انقضا برای هشدارهای داشبورد
        s.Licenses.Add(new LicenseDto("lic-x1", Key(), "u-casey", "کیسی ویتفیلد", "casey.demo@example.com",
            "p-crm", "CRM دسکتاپ فروغ آریا پارسیان", "northwind-crm-desktop", "۵ کاربر", 5, 4,
            LicenseStatus.Active, DaysAgo(340), DaysAhead(24)));
        s.Licenses.Add(new LicenseDto("lic-x2", Key(), "u-casey", "کیسی ویتفیلد", "casey.demo@example.com",
            "p-bi", "کانکتور هوش تجاری فروغ آریا پارسیان", "northwind-bi-connector", "۵ کاربر", 5, 2,
            LicenseStatus.Active, DaysAgo(300), DaysAhead(41)));

        /* ----------------------------- Downloads -------------------------------- */
        s.Downloads.AddRange(new[]
        {
            new DownloadDto("dl-1","p-crm","CRM دسکتاپ فروغ آریا پارسیان","northwind-crm-desktop","v-1","2025.2.1","installer","northwind-crm-desktop-2025.2.1-setup.exe",84,DaysAgo(3),true),
            new DownloadDto("dl-2","p-pos","صندوق فروشگاهی فروغ آریا پارسیان","northwind-point-of-sale","v-2","2025.1.8","installer","northwind-pos-2025.1.8-setup.exe",61,DaysAgo(12),true),
            new DownloadDto("dl-3","p-bi","کانکتور هوش تجاری فروغ آریا پارسیان","northwind-bi-connector","v-3","2025.2.0","manual","northwind-bi-connector-guide.pdf",3,DaysAgo(20),false),
        });

        /* ------------------------------ Tickets --------------------------------- */
        s.Tickets.AddRange(new[]
        {
            new TicketDetailDto("tk-1","TK-1042","u-casey","کیسی ویتفیلد","p-erp","هسته ERP فروغ آریا پارسیان",
                "خطا در تجمیعِ پایانِ دوره برای شرکتِ دوم",
                "Technical", TicketPriority.High, TicketStatus.WaitingCustomer,
                "ویندوز سرور ۲۰۲۲ · نسخهٔ ۲۰۲۵٫۲٫۱", DaysAgo(6), DaysAgo(1),
                new[]
                {
                    new TicketMessageDto("m-1","tk-1","کیسی ویتفیلد",false,"هنگام اجرای تجمیعِ پایانِ دوره برای شرکتِ دوم، فرآیند با خطای «حسابِ نامعتبر» متوقف می‌شود. شرکتِ اول بدون مشکل است.",DaysAgo(6)),
                    new TicketMessageDto("m-2","tk-1","الکساندرا ریز (پشتیبانی)",true,"سلام، از گزارشِ خطای دقیق ممنونم. لطفاً فایلِ لاگِ مسیر ProgramData\\Northwind\\Logs را پیوست کنید تا بررسی کنیم.",DaysAgo(5)),
                    new TicketMessageDto("m-3","tk-1","کیسی ویتفیلد",false,"فایل پیوست شد. ظاهراً یک حسابِ بسته‌شده در نقشهٔ تجمیع باقی مانده است.",DaysAgo(1)),
                }),
            new TicketDetailDto("tk-2","TK-1045","u-casey","کیسی ویتفیلد","p-crm","CRM دسکتاپ فروغ آریا پارسیان",
                "درخواست افزایشِ سقفِ لایسنس از ۵ به ۸ کاربر",
                "Licensing", TicketPriority.Normal, TicketStatus.Open,
                "ویندوز ۱۱", DaysAgo(2), DaysAgo(2),
                new[]
                {
                    new TicketMessageDto("m-4","tk-2","کیسی ویتفیلد",false,"تیم فروش به ۸ نفر رسیده است. امکان ارتقای میان‌دوره‌ای با محاسبهٔ مابه‌التفاوت وجود دارد؟",DaysAgo(2)),
                }),
            new TicketDetailDto("tk-3","TK-1039","u-casey","کیسی ویتفیلد","p-pos","صندوق فروشگاهی فروغ آریا پارسیان",
                "چاپگر رسید در شعبهٔ دوم شناسایی نمی‌شود",
                "Technical", TicketPriority.Critical, TicketStatus.InProgress,
                "ویندوز ۱۰ · چاپگر حرارتی", DaysAgo(9), DaysAgo(3),
                new[]
                {
                    new TicketMessageDto("m-5","tk-3","کیسی ویتفیلد",false,"بعد از به‌روزرسانیِ ویندوز، چاپگر در شعبهٔ دوم شناسایی نمی‌شود. در شعبهٔ اول مشکلی نیست.",DaysAgo(9)),
                    new TicketMessageDto("m-6","tk-3","الکساندرا ریز (پشتیبانی)",true,"نسخهٔ ۲۰۲۵٫۱٫۹ وصلهٔ مربوط به درایور را دارد. در حال بررسیِ زمان انتشار هستیم.",DaysAgo(3)),
                }),
            new TicketDetailDto("tk-4","TK-1021","u-casey","کیسی ویتفیلد","p-fin","مالی فروغ آریا پارسیان",
                "سؤال دربارهٔ گزارشِ مالیاتیِ فصلی",
                "Billing", TicketPriority.Low, TicketStatus.Resolved,
                "ویندوز ۱۱", DaysAgo(24), DaysAgo(21),
                new[]
                {
                    new TicketMessageDto("m-7","tk-4","کیسی ویتفیلد",false,"آیا گزارشِ فصلی به فرمتِ مورد تأییدِ سازمان امور مالیاتی خروجی می‌گیرد؟",DaysAgo(24)),
                    new TicketMessageDto("m-8","tk-4","الکساندرا ریز (پشتیبانی)",true,"بله، قالبِ خروجی از منوی گزارش‌ها → مالیاتی → فصلی در دسترس است.",DaysAgo(21)),
                }),
            new TicketDetailDto("tk-5","TK-1048","u-priya","پریا راگاوان","p-bi","کانکتور هوش تجاری فروغ آریا پارسیان",
                "تازه‌سازیِ افزایشی روی جداول بزرگ کند است",
                "Technical", TicketPriority.High, TicketStatus.Open,
                "ویندوز سرور ۲۰۲۲", DaysAgo(1), DaysAgo(1),
                new[]
                {
                    new TicketMessageDto("m-9","tk-5","پریا راگاوان",false,"تازه‌سازیِ افزایشی روی جدولِ حرکات انبار با ۴۰ میلیون رکورد حدود ۳ ساعت طول می‌کشد.",DaysAgo(1)),
                }),
        });

        /* --------------------------- Cart (نمونه) -------------------------------- */
        s.CartLines.AddRange(new[]
        {
            new CartLine { Id = "ci-1", SessionKey = "demo", PlanId = s.Products[0].Plans[1].Id, Quantity = 1 },
            new CartLine { Id = "ci-2", SessionKey = "demo", PlanId = s.Products[4].Plans[0].Id, Quantity = 2 },
        });

        return s;
    }

    /* ------------------------------ helpers -------------------------------- */

    private static string Key()
    {
        const string charset = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rnd = new Random(Guid.NewGuid().GetHashCode());
        string Group(int n) => new(Enumerable.Range(0, n).Select(_ => charset[rnd.Next(charset.Length)]).ToArray());
        return $"NW-{Group(5)}-{Group(5)}-{Group(5)}-{Group(5)}";
    }

    private static IReadOnlyList<PlanDto> Plans(params (string Name, PlanTier Tier, BillingType Billing,
        int Price, int Seats, bool Popular, string Desc)[] defs) =>
        defs.Select((d, i) => new PlanDto(
            $"{d.Name}-{i}", d.Name, d.Tier, d.Billing, d.Price, d.Seats, d.Desc, d.Popular)).ToList();

    private static ProductDetailDto Product(
        StoreState _,
        string id, string slug, string name, string tagline, string description,
        ProductType type, string platform, string version, int basePrice,
        decimal rating, int ratingCount, int popularity, string accent,
        string[] features, string[] requirements,
        string categoryId, string categoryName, string categorySlug,
        bool featured, int trialDays, int createdDaysAgo,
        IReadOnlyList<PlanDto> plans)
    {
        var vBase = version.Split('.')[0] + "." + version.Split('.')[1];
        var versions = new[]
        {
            new ProductVersionDto($"{id}-v3", version, DaysAgo(createdDaysAgo / 4),
                "بهبود عملکرد، رفع خطاهای گزارش‌شده و سازگاری با آخرین نسخهٔ هسته.",
                $"https://cdn.northwindsoft.example/{slug}-{version}-setup.exe", 84, platform),
            new ProductVersionDto($"{id}-v2", $"{vBase}.0", DaysAgo(createdDaysAgo / 2),
                "نسخهٔ اصلیِ این خط شامل تغییراتِ ساختاریِ پایگاه داده.",
                $"https://cdn.northwindsoft.example/{slug}-{vBase}.0-setup.exe", 79, platform),
            new ProductVersionDto($"{id}-v1", $"{vBase}.0-beta", DaysAgo(createdDaysAgo),
                "نسخهٔ آزمایشیِ عمومی برای مشتریانِ فعلی.",
                null, 76, platform),
        };

        return new ProductDetailDto(
            id, slug, name, tagline, description, type, Audience.Both, platform, version, basePrice,
            rating, ratingCount, popularity, accent, null, true, featured, trialDays, DaysAgo(createdDaysAgo),
            categoryId, categoryName, categorySlug,
            features, requirements,
            new[] { accent, accent + "cc", "#1e293b" },
            "https://cdn.northwindsoft.example/demos/product-tour.mp4",
            plans, versions, Array.Empty<ReviewDto>());
    }
}
