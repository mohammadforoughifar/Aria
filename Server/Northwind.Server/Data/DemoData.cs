// دادهٔ تستی — برای اینکه همهٔ بخش‌هایِ برنامه پر و قابلِ آزمایش باشند.
//
// تفاوت با Seed.cs:
//   Seed.cs فقط در نخستین اجرا دادهٔ پایه را می‌سازد و دیگر کاری نمی‌کند.
//   این فایل در هر اجرا فراخوانی می‌شود و فقط جدول‌هایی را که کم‌داده یا خالی
//   هستند پُر می‌کند (top-up). شناسه‌ها ثابت‌اند، بنابراین هیچ رکوردی تکرار
//   نمی‌شود و می‌توان آن را هرچند بار خواست اجرا کرد.
//
// مبالغ به تومان هستند.

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Northwind.Shared.Contracts;

namespace Northwind.Server.Data;

public static class DemoData
{
    private static string Json(params string[] items) => JsonSerializer.Serialize(items);

    private static DateTime DaysAgo(int days) => DateTime.UtcNow.AddDays(-days);
    private static DateTime HoursAgo(int h) => DateTime.UtcNow.AddHours(-h);

    /// <summary>
    /// دادهٔ تستی هیچ‌گاه نباید جلویِ بالا آمدنِ برنامه را بگیرد.
    /// اگر بخشی به هر دلیلی خطا داد (مثلاً شناسه‌ای از پیش وجود داشته باشد)،
    /// بقیهٔ بخش‌ها بی‌وقفه ادامه می‌دهند و فقط خطا در گزارش نوشته می‌شود.
    /// </summary>
    public static void Run(AppDbContext db, Action<string>? log = null)
    {
        Safe("نرم‌افزارها", () => { Products(db); db.SaveChanges(); }, log);
        Safe("دریافت‌ها", () => Downloads(db), log);
        Safe("نظرات", () => Testimonials(db), log);
        Safe("مقالات", () => Articles(db), log);
        Safe("پرسش‌ها", () => Faqs(db), log);
        Safe("تیکت‌ها", () => Tickets(db), log);
        Safe("درخواست‌های دمو", () => DemoRequests(db), log);
        Safe("پیام‌های تماس", () => ContactMessages(db), log);
        Safe("اعلان‌ها", () => Notifications(db), log);
        Safe("ذخیرهٔ نهایی", () => db.SaveChanges(), log);
    }

    private static void Safe(string name, Action action, Action<string>? log)
    {
        try { action(); }
        catch (Exception ex)
        {
            // دادهٔ تستیِ ناقص بهتر از برنامه‌ای است که بالا نمی‌آید
            var msg = $"دادهٔ تستی — «{name}»: {ex.GetBaseException().Message}";
            Console.WriteLine(msg);
            log?.Invoke(msg);
        }
    }

    /* ══════════════════════════════════════════════════════════════════
       ۱. نرم‌افزارها — همان هفت سیستمی که در صفحهٔ «سیستم‌های ابری»
          معرفی شده‌اند، به‌علاوهٔ دو ابزارِ جانبی
       ══════════════════════════════════════════════════════════════════ */
    private static void Products(AppDbContext db)
    {
        var items = new[]
        {
            new Demo("p-acc", "حسابداری", "حسابداری فروغ آریا پارسیان", "c-fin",
                "پایهٔ اصلیِ مجموعه؛ از ثبتِ سند تا ترازنامه و گزارش‌های قانونی",
                "حسابداریِ فروغ آریا پارسیان ستونِ فقراتِ مجموعه است. اسنادِ روزانه را ثبت می‌کند، " +
                "دفاترِ قانونی و ترازنامه را بی‌درنگ به‌روز می‌دارد و گزارش‌هایِ مالیاتی را با یک کلیک " +
                "آماده می‌کند. سایرِ سیستم‌ها روی همین پایه سوار می‌شوند.",
                ProductType.Module, "#1E3A8A", 9_800_000, 970,
                new[] { "دفاترِ قانونی و ترازِ آزمایشی", "سرفصل‌هایِ چندسطحی و مرکزِ هزینه",
                        "تطبیقِ خودکارِ بانک و صندوق", "گزارشِ سود و زیان و ترازنامه",
                        "خروجیِ اظهارنامه و صورتِ معاملاتِ فصلی" },
                (6, 5_600_000, 3), (12, 9_800_000, 5)),

            new Demo("p-treasury", "دریافت و پرداخت", "دریافت و پرداخت فروغ آریا پارسیان", "c-fin",
                "چک‌ها، اقساط و نقدینگی؛ هیچ سررسیدی از قلم نمی‌افتد",
                "تمامِ جریانِ نقدِ سازمان را یک‌جا می‌بینید: چک‌هایِ دریافتی و پرداختی، اسنادِ " +
                "دریافتنی، اقساط و سررسیدها. پیش از آنکه موعدی بگذرد، به شما یادآوری می‌شود.",
                ProductType.Module, "#0EA5E9", 5_400_000, 610,
                new[] { "دفترچهٔ چک‌هایِ دریافتی و پرداختی", "یادآوریِ سررسید و واگذاری به بانک",
                        "گردشِ نقدِ روزانه و پیش‌بینیِ نقدینگی", "تطبیقِ خودکار با صورت‌حسابِ بانک" },
                (6, 3_100_000, 3), (12, 5_400_000, 5)),

            new Demo("p-warehouse", "انبارداری", "انبارداری فروغ آریا پارسیان", "c-ops",
                "موجودیِ دقیق در لحظه، در چندین انبار",
                "از رسیدِ خرید تا حوالهٔ فروش، هر کالا ردیابی می‌شود. نقطهٔ سفارش، انبارگردانی و " +
                "گزارشِ کسری و اضافیِ موجودی در دسترس است و قیمتِ تمام‌شده به‌درستی محاسبه می‌شود.",
                ProductType.Module, "#2563EB", 6_200_000, 720,
                new[] { "انبارهایِ نامحدود و جابه‌جاییِ بین‌انباری", "رسید، حواله و برگشت از فروش",
                        "نقطهٔ سفارش و هشدارِ کسریِ موجودی", "انبارگردانی و قیمت‌گذاریِ میانگین" },
                (6, 3_500_000, 3), (12, 6_200_000, 5)),

            new Demo("p-sales", "فروش کالا و خدمت", "فروش و صدور فاکتور", "c-ops",
                "از پیش‌فاکتور تا صدورِ فاکتور و وصول",
                "پیش‌فاکتور، فاکتورِ فروش، برگشت از فروش و صورت‌حسابِ مشتری در یک چرخهٔ پیوسته. " +
                "سقفِ اعتبار و بدهیِ هر مشتری پیش از ثبتِ فاکتور کنترل می‌شود.",
                ProductType.Module, "#7C3AED", 5_800_000, 680,
                new[] { "پیش‌فاکتور و تبدیل به فاکتور", "سقفِ اعتبار و کنترلِ بدهیِ مشتری",
                        "برگشت از فروش و تخفیفات", "صورت‌حسابِ دوره‌ایِ مشتری" },
                (6, 3_300_000, 3), (12, 5_800_000, 5)),

            new Demo("p-tax", "سامانهٔ مؤدیان", "سامانهٔ مؤدیان و فاکتورِ الکترونیکی", "c-addon",
                "اتصالِ مستقیم به سامانهٔ مؤدیان، بدونِ ورودِ دستی",
                "صورت‌حساب‌هایِ الکترونیکی را می‌سازد، امضا می‌کند و به سامانهٔ مؤدیان می‌فرستد. " +
                "وضعیتِ ارسال و پاسخِ سامانه لحظه‌به‌لحظه دنبال می‌شود.",
                ProductType.AddOn, "#F59E0B", 4_200_000, 840,
                new[] { "ساخت و ارسالِ خودکارِ صورت‌حساب", "امضایِ دیجیتال و دریافتِ شناسهٔ یکتا",
                        "پیگیریِ وضعیت و رفعِ خطایِ سامانه", "گزارشِ صورت‌حساب‌هایِ ارسالی" },
                (6, 2_400_000, 2), (12, 4_200_000, 3)),

            new Demo("p-payroll", "حقوق و دستمزد", "حقوق و دستمزد فروغ آریا پارسیان", "c-erp",
                "فیشِ حقوق، بیمه و مالیات؛ دقیق و به‌موقع",
                "کارکرد از حضور و غیاب محاسبه می‌شود، فیشِ حقوق صادر می‌گردد و لیستِ بیمه و " +
                "مالیاتِ حقوق برای ارسال آماده است.",
                ProductType.Module, "#16A34A", 5_200_000, 660,
                new[] { "محاسبهٔ کارکرد، اضافه‌کار و شیفت", "فیشِ حقوق و واریزِ گروهی",
                        "لیستِ بیمه و مالیاتِ حقوق", "تسویه‌حسابِ پایانِ کار" },
                (6, 3_000_000, 3), (12, 5_200_000, 5)),

            new Demo("p-edari", "امور اداری", "دفترِ اداری و مکاتبات", "c-desk",
                "نامه‌ها، قراردادها و پیگیریِ کارهایِ روزمره",
                "مکاتباتِ اداری، قراردادها و پیگیریِ امورِ جاریِ دفتر را یک‌جا نگه می‌دارد تا " +
                "هیچ کاری در میانِ گفتگوها گم نشود.",
                ProductType.DesktopApp, "#64748B", 3_600_000, 480,
                new[] { "دفترِ اندیکاتور و پیگیریِ نامه‌ها", "قراردادها و یادآوریِ تمدید",
                        "درخواست‌هایِ داخلی و گردشِ کار" },
                (6, 2_100_000, 3), (12, 3_600_000, 5)),
        };

        foreach (var d in items)
        {
            if (db.Products.Any(p => p.Id == d.Id)) continue;

            db.Products.Add(new Product
            {
                Id = d.Id,
                Slug = d.Id.Replace("p-", "") + "-system",
                Name = d.Name,
                Tagline = d.Tagline,
                Description = d.Description,
                Type = d.Type,
                Audience = Audience.Both,
                Platform = "وب و ویندوز",
                Version = "1.4.2",
                BasePriceToman = d.Price12,
                AccentColor = d.Color,
                IsActive = true,
                IsFeatured = d.Popularity >= 800,
                TrialDays = 14,
                Popularity = d.Popularity,
                CreatedAt = DaysAgo(120),
                CategoryId = d.CategoryId,
                FeaturesJson = Json(d.Features),
                RequirementsJson = Json("ویندوز ۱۰ یا بالاتر", "مرورگرِ نوین برای نسخهٔ وب",
                                        "۴ گیگابایت حافظه", "۱ گیگابایت فضایِ دیسک"),
            });

            // دو طرحِ اشتراک
            db.Plans.Add(MakePlan($"{d.Id}-6", d.Id, "اشتراک ۶ ماهه", d.Price6, d.Seats6, 6, 1, false));
            db.Plans.Add(MakePlan($"{d.Id}-12", d.Id, "اشتراک ۱۲ ماهه", d.Price12, d.Seats12, 12, 2, true));

            // دو نسخه برای تاریخچه
            db.ProductVersions.Add(new ProductVersion
            {
                Id = $"{d.Id}-v13", ProductId = d.Id, Version = "1.3.0",
                ReleasedAt = DaysAgo(150), Notes = "بهبودِ سرعتِ گزارش‌گیری",
                SizeMb = 148, Platform = "ویندوز", InstallerUrl = $"/downloads/{d.Id}-1.3.0.exe",
            });
            db.ProductVersions.Add(new ProductVersion
            {
                Id = $"{d.Id}-v14", ProductId = d.Id, Version = "1.4.2",
                ReleasedAt = DaysAgo(18), Notes = "سازگاری با سامانهٔ مؤدیان و رفعِ خطاهایِ گزارش",
                SizeMb = 156, Platform = "ویندوز", InstallerUrl = $"/downloads/{d.Id}-1.4.2.exe",
            });
        }
    }

    private sealed record Demo(
        string Id, string ShortName, string Name, string CategoryId,
        string Tagline, string Description, ProductType Type, string Color,
        int Price12, int Popularity, string[] Features,
        (int Months, int Price, int Seats) Plan6, (int Months, int Price, int Seats) Plan12)
    {
        public int Price6 => Plan6.Price;
        public int Seats6 => Plan6.Seats;
        public int Seats12 => Plan12.Seats;
    }

    private static Plan MakePlan(string id, string productId, string name, int price, int seats,
                                 int months, int sort, bool popular) => new()
    {
        Id = id, ProductId = productId, Name = name,
        Tier = seats >= 5 ? PlanTier.Enterprise : seats >= 3 ? PlanTier.MultiUser : PlanTier.SingleUser,
        BillingType = BillingType.Annual,
        PriceToman = price, Seats = seats, LicenseMonths = months,
        IsPopular = popular, SortOrder = sort,
        Description = $"{months} ماه دسترسی برای {seats} کاربر",
        FeaturesJson = Json($"{months} ماه دسترسی", $"{seats} کاربرِ هم‌زمان",
                            "پشتیبان‌گیریِ خودکار", "به‌روزرسانیِ رایگان", "پشتیبانیِ تلفنی"),
    };

    /* ══════════════════════════════════════════════════════════════════
       ۲. دریافت‌ها — صفحهٔ «بارگیری‌ها» در پنلِ مشتری خالی بود
       ══════════════════════════════════════════════════════════════════ */
    private static void Downloads(AppDbContext db)
    {

        var rows = new (string User, string Product, string Ver, string Kind, int Mb, int Days, bool NeedsLicense)[]
        {
            ("u-casey", "p-erp",   "2025.2.4", "installer", 184, 3,  true),
            ("u-casey", "p-acc",   "1.4.2",    "installer", 156, 12, true),
            ("u-casey", "p-warehouse","1.4.2", "installer", 142, 26, true),
            ("u-casey", "p-erp",   "2025.2.3", "update",     62, 48, false),
            ("u-priya", "p-fin",   "1.4.2",    "installer", 151, 7,  true),
            ("u-priya", "p-tax",   "1.4.2",    "installer",  88, 9,  true),
            ("u-priya", "p-sales", "1.4.2",    "installer", 133, 21, true),
            ("u-marcus","p-pos",   "3.1.0",    "installer",  74, 5,  true),
            ("u-marcus","p-crm",   "2.0.6",    "installer",  69, 30, true),
            ("u-marcus","p-pos",   "3.0.9",    "update",     28, 61, false),
            ("u-hana",  "p-doc",   "1.2.1",    "installer",  57, 14, true),
            ("u-hana",  "p-hr",    "1.4.2",    "installer", 118, 17, true),
            ("u-hana",  "p-edari", "1.4.2",    "installer",  64, 33, true),
            ("u-casey", "p-payroll","1.4.2",   "installer", 112, 40, true),
            ("u-priya", "p-treasury","1.4.2",  "installer",  97, 55, true),
        };

        foreach (var r in rows)
        {
            var pid = r.Product;
            if (!db.Products.Any(p => p.Id == pid)) continue;

            var id = $"dl-{r.User}-{pid}-{r.Ver}";
            if (db.Downloads.Any(d => d.Id == id)) continue;

            db.Downloads.Add(new Download
            {
                Id = id,
                ProductId = pid,
                UserId = r.User,
                VersionId = $"{pid}-v14",
                Version = r.Ver,
                Kind = r.Kind,
                FileName = $"{pid}-{r.Ver}.{(r.Kind == "update" ? "msp" : "exe")}",
                SizeMb = r.Mb,
                RequiresLicense = r.NeedsLicense,
                DownloadedAt = DaysAgo(r.Days).AddHours(-r.Mb % 11),
            });
        }
    }

    /* ══════════════════════════════════════════════════════════════════
       ۳. نظرِ مشتریان
       ══════════════════════════════════════════════════════════════════ */
    private static void Testimonials(AppDbContext db)
    {
        var rows = new (string Name, string Role, string Company, string Quote, int Rating)[]
        {
            ("مهندس رضا کریمی", "مدیر مالی", "گروه تولیدی ساحل",
             "بستنِ ماهانه که سه روز طول می‌کشید، الان نیم‌ساعته انجام می‌شود. ترازِ آزمایشی همان لحظه درست است.", 5),
            ("سارا احمدی", "مدیرعامل", "توزیع پیشرو",
             "انبار و فروشِ ما بالاخره با حسابداری یکی شد. دیگر کسی مجبور نیست دو بار یک چیز را وارد کند.", 5),
            ("علیرضا موسوی", "رئیس حسابداری", "شرکت بازرگانی پارس",
             "اتصال به سامانهٔ مؤدیان بی‌دردسر بود. خطاها همان‌جا توضیح داده می‌شود و راهِ حل نشان می‌دهد.", 5),
            ("زهرا نیک‌بخت", "مدیر منابع انسانی", "کارخانهٔ کاشی اردکان",
             "فیشِ حقوق و لیستِ بیمه بدونِ خطا صادر می‌شود. وقتِ آزادشده صرفِ کارهایِ مهم‌تر شد.", 5),
            ("حمیدرضا صادقی", "مدیر فناوری اطلاعات", "هلدینگ نیکان",
             "پشتیبانی واقعاً پاسخ‌گوست. تیکتی که شب می‌زنیم، صبح پاسخ دارد.", 4),
            ("مریم قاسمی", "صاحبِ فروشگاه", "فروشگاه زنجیره‌ایِ گلستان",
             "صندوقِ فروشگاهی با انبار هماهنگ است؛ موجودی لحظه‌ای درست می‌ماند و کمبودها زود خبر می‌دهد.", 5),
            ("کامران وفایی", "مدیر مالی", "شرکت خدماتی آریا",
             "گزارش‌هایِ مدیریتی تصویرِ روشنی از جریانِ نقد می‌دهد. تصمیم‌گیری ساده‌تر شده است.", 5),
            ("نسترن بهرامی", "حسابدار ارشد", "مجموعهٔ آموزشیِ فرهنگ",
             "یادگیری‌اش برای تیمِ ما آسان بود. راهنماها و ویدئوها واقعاً کمک کرد.", 4),
        };

        foreach (var (i, r) in rows.Select((r, i) => (i, r)))
        {
            var id = $"t-demo-{i}";
            if (db.Testimonials.Any(t => t.Id == id)) continue;
            db.Testimonials.Add(new Testimonial
            {
                Id = id, AuthorName = r.Name, AuthorRole = r.Role,
                Company = r.Company, Quote = r.Quote, Rating = r.Rating,
            });
        }
    }

    /* ══════════════════════════════════════════════════════════════════
       ۴. مقالاتِ وبلاگ
       ══════════════════════════════════════════════════════════════════ */
    private static void Articles(AppDbContext db)
    {
        var rows = new (string Slug, string Title, string Excerpt, string Cat, int Read, string Body)[]
        {
            ("moadian-connection", "اتصال به سامانهٔ مؤدیان، گام‌به‌گام",
             "چه چیزهایی لازم دارید، چطور امضا می‌کنید و خطاهایِ رایج را چگونه رفع کنید.",
             "مالیات", 7,
             "برای اتصال به سامانهٔ مؤدیان سه چیز لازم است: شناسهٔ ملیِ شرکت، گواهیِ امضایِ دیجیتال " +
             "و تعریفِ کالاها و خدمات در پایگاهِ اطلاعات. پس از ثبت، هر فاکتور هم‌زمان با صدور " +
             "به سامانه فرستاده می‌شود. اگر سامانه خطا بدهد، متنِ خطا در همان صفحه نمایش داده می‌شود."),
            ("inventory-accuracy", "چرا موجودیِ انبار با حسابداری فرق می‌کند؟",
             "پنج علتِ رایجِ مغایرت و راهِ پیشگیری از هر کدام.",
             "انبار", 6,
             "بیشترِ مغایرت‌ها از ثبتِ دستی، زمان‌بندیِ متفاوتِ اسناد و برگشت‌هایِ ثبت‌نشده می‌آید. " +
             "با ثبتِ خودکارِ اسناد از روی رسید و حواله و انجامِ انبارگردانیِ دوره‌ای، اختلاف به نزدیکِ صفر می‌رسد."),
            ("payroll-checklist", "چک‌لیستِ پایانِ ماه برای حقوق و دستمزد",
             "پیش از صدورِ فیش، این ده مورد را بررسی کنید.",
             "منابع انسانی", 5,
             "کارکرد و مرخصی‌ها، اضافه‌کار و شیفت، مساعده و وام، کسوراتِ بیمه و مالیات، " +
             "تغییراتِ پرسنلی در ماه، محاسبهٔ سنوات و عیدی، کنترلِ فیش‌هایِ نمونه، " +
             "تأییدِ مدیر و در نهایت صدورِ لیستِ بیمه."),
            ("cash-flow", "پیش‌بینیِ جریانِ نقد با داده‌هایِ واقعی",
             "چطور بفهمیم سه ماهِ دیگر چقدر پول در دست داریم.",
             "مدیریت", 8,
             "با داشتنِ سررسیدِ چک‌ها، اقساط و مطالباتِ معوق می‌توان جریانِ نقدِ آینده را پیش‌بینی کرد. " +
             "گزارشِ نقدینگی در فروغ آریا پارسیان این سه را کنار هم می‌گذارد."),
            ("erp-selection", "انتخابِ نرم‌افزارِ مالی: هفت پرسشِ پیش از خرید",
             "معیارهایی که بعداً پشیمانی نمی‌آورند.",
             "راهنما", 6,
             "آیا داده‌هایتان قابلِ انتقال است؟ چند کاربر هم‌زمان خواهید داشت؟ به چه گزارش‌هایی " +
             "نیاز دارید؟ پشتیبانی چه سطحی ارائه می‌شود؟ به‌روزرسانی رایگان است؟ " +
             "نسخهٔ آزمایشی دارید؟ و مهم‌تر از همه: آیا با رشدِ شما رشد می‌کند؟"),
            ("cost-price", "قیمتِ تمام‌شده را درست حساب کنید",
             "تفاوتِ سودِ واقعی و سودِ رویِ کاغذ.",
             "حسابداری", 7,
             "اگر هزینه‌هایِ حمل، تخفیفات و ضایعات در قیمتِ تمام‌شده لحاظ نشود، سودِ گزارش‌شده " +
             "بیش از واقعیت نشان داده می‌شود. سیستمِ انبار این موارد را به بهایِ کالا اضافه می‌کند."),
            ("backup-routine", "پشتیبان‌گیری؛ ساده اما حیاتی",
             "یک روالِ سه‌لایه که خیال را راحت می‌کند.",
             "فناوری", 4,
             "هر روز یک نسخهٔ خودکار، هر هفته یک نسخهٔ جداگانه و هر ماه یک نسخه در محلی دیگر. " +
             "و مهم‌تر: هر چند ماه یک‌بار بازیابی را آزمایش کنید."),
            ("year-end-closing", "بستنِ سالِ مالی بدونِ استرس",
             "چه کارهایی را باید از نیمهٔ اسفند شروع کنید.",
             "حسابداری", 6,
             "تطبیقِ بانک‌ها، انبارگردانی، کنترلِ مطالباتِ مشکوک، بررسیِ پیش‌پرداخت‌ها، " +
             "محاسبهٔ ذخیرهٔ مالیات و در نهایت صدورِ سندِ اختتامیه."),
        };

        foreach (var (i, r) in rows.Select((r, i) => (i, r)))
        {
            var id = $"a-demo-{i}";
            if (db.Articles.Any(a => a.Id == id)) continue;
            db.Articles.Add(new Article
            {
                Id = id, Slug = r.Slug, Title = r.Title, Excerpt = r.Excerpt,
                Body = r.Body, Category = r.Cat, IsPublished = true,
                PublishedAt = DaysAgo(r.Read * 6 + 3), ReadMinutes = r.Read,
            });
        }
    }

    /* ══════════════════════════════════════════════════════════════════
       ۵. پرسش‌هایِ پرتکرار
       ══════════════════════════════════════════════════════════════════ */
    private static void Faqs(AppDbContext db)
    {
        var rows = new (string Q, string A)[]
        {
            ("آیا می‌توانم فقط یک سیستم را بخرم؟",
             "بله. حسابداری پایهٔ اصلی است و بقیهٔ سیستم‌ها را هر زمان بخواهید اضافه می‌کنید. " +
             "همه روی یک پایگاهِ داده کار می‌کنند."),
            ("نسخهٔ آزمایشی چه محدودیتی دارد؟",
             "هیچ محدودیتی در امکانات ندارد؛ تنها ۱۴ روز اعتبار دارد و نیازی به کارتِ اعتباری نیست."),
            ("انتقالِ داده‌هایِ نرم‌افزارِ قبلی چطور انجام می‌شود؟",
             "خروجیِ اکسل از نرم‌افزارِ قبلی می‌گیرید و با قالبِ آماده وارد می‌کنیم. " +
             "برای مشتریانِ سازمانی این کار را تیمِ ما انجام می‌دهد."),
            ("روی چند رایانه می‌توانم نصب کنم؟",
             "تعدادِ نصب محدود نیست، اما تعدادِ کاربرِ هم‌زمان بر اساسِ طرحِ خریداری‌شده مشخص می‌شود."),
            ("به‌روزرسانی‌ها رایگان است؟",
             "در طولِ دورهٔ اشتراک، همهٔ به‌روزرسانی‌ها — از جمله تغییراتِ قوانینِ مالیاتی — رایگان است."),
            ("پشتیبانی چه ساعاتی پاسخ‌گوست؟",
             "شنبه تا چهارشنبه ۸ تا ۱۸ و پنج‌شنبه ۸ تا ۱۳. تیکت‌هایِ بحرانی در اولویتِ نخست بررسی می‌شوند."),
            ("آیا اینترنت قطع شود، کارم می‌ماند؟",
             "نسخهٔ رومیزی بدونِ اینترنت هم کار می‌کند و پس از اتصال، داده‌ها همگام می‌شوند."),
        };

        var order = db.Faqs.Any() ? db.Faqs.Max(f => f.SortOrder) + 1 : 1;
        foreach (var (i, r) in rows.Select((r, i) => (i, r)))
        {
            var id = $"f-demo-{i}";
            if (db.Faqs.Any(f => f.Id == id)) continue;
            db.Faqs.Add(new Faq { Id = id, Question = r.Q, Answer = r.A, SortOrder = order + i });
        }
    }

    /* ══════════════════════════════════════════════════════════════════
       ۶. تیکت‌هایِ پشتیبانی (با گفتگو)
       ══════════════════════════════════════════════════════════════════ */
    private static void Tickets(AppDbContext db)
    {
        var rows = new (string User, string Subject, string Cat, TicketPriority Prio, TicketStatus Status,
                        int Days, (string Author, bool Staff, string Body)[] Msgs)[]
        {
            ("u-casey", "خطا در ارسالِ صورت‌حساب به سامانهٔ مؤدیان", "مالیات", TicketPriority.High, TicketStatus.InProgress, 2,
                new[] { ("کیسی ویتفیلد", false, "هنگامِ ارسالِ فاکتورِ شماره ۱۲۰۴ خطای «کدِ کالا نامعتبر است» می‌گیریم."),
                        ("پشتیبانی — سارا", true, "کدِ کالایِ این فاکتور در سامانه تعریف نشده. از مسیرِ تعاریف > کالا و خدمات، کد را ثبت کنید."),
                        ("کیسی ویتفیلد", false, "انجام شد، ولی هنوز دو فاکتور در صف مانده‌اند."),
                        ("پشتیبانی — سارا", true, "آن‌ها را بررسی می‌کنم و تا پایانِ امروز نتیجه را اعلام می‌دهم.") }),

            ("u-priya", "گزارشِ سود و زیان با تراز نمی‌خواند", "حسابداری", TicketPriority.Normal, TicketStatus.WaitingCustomer, 5,
                new[] { ("پریا راگاوان", false, "جمعِ سود و زیانِ ماه با سندِ اختتامیه یکسان نیست."),
                        ("پشتیبانی — مجید", true, "احتمالاً اسنادی با تاریخِ ماهِ بعد ثبت شده‌اند. از گزارشِ کنترلِ اسناد استفاده کنید."),
                        ("پریا راگاوان", false, "بررسی می‌کنم و نتیجه را می‌گویم.") }),

            ("u-marcus", "درخواستِ افزایشِ کاربرِ هم‌زمان", "لایسنس", TicketPriority.Low, TicketStatus.Resolved, 9,
                new[] { ("مارکوس دلگادو", false, "می‌خواهیم از ۳ کاربر به ۶ کاربر برویم."),
                        ("پشتیبانی — سارا", true, "ارتقا انجام شد؛ صورتحسابِ مابه‌التفاوت صادر گردید.") }),

            ("u-hana", "کندی در باز شدنِ انبار", "عملکرد", TicketPriority.Normal, TicketStatus.Closed, 14,
                new[] { ("هانا کوبایاشی", false, "صفحهٔ موجودی حدودِ ۲۰ ثانیه طول می‌کشد."),
                        ("پشتیبانی — مجید", true, "شاخصِ پایگاه داده بازسازی شد. حالا زیرِ ۳ ثانیه است."),
                        ("هانا کوبایاشی", false, "تأیید شد، مشکل برطرف شده است.") }),

            ("u-casey", "نصب روی ویندوز ۱۱ ناموفق است", "نصب", TicketPriority.Critical, TicketStatus.Open, 1,
                new[] { ("کیسی ویتفیلد", false, "در مرحلهٔ پایانی نصب، خطایِ دسترسی به پوشهٔ Program Files می‌گیریم.") }),

            ("u-priya", "آموزشِ ماژولِ حقوق و دستمزد", "آموزش", TicketPriority.Low, TicketStatus.Open, 3,
                new[] { ("پریا راگاوان", false, "برای تیمِ منابع انسانی یک جلسهٔ آموزشی می‌خواهیم.") }),

            ("u-hana", "خروجیِ اکسل از دفترِ روزنامه", "گزارش‌گیری", TicketPriority.Normal, TicketStatus.Resolved, 20,
                new[] { ("هانا کوبایاشی", false, "ستون‌هایِ خروجیِ اکسل به‌هم‌ریخته است."),
                        ("پشتیبانی — مجید", true, "نسخهٔ ۱.۴.۲ این مشکل را برطرف کرده؛ به‌روزرسانی کنید."),
                        ("هانا کوبایاشی", false, "به‌روزرسانی کردم و درست شد. ممنون.") }),
        };

        foreach (var (i, r) in rows.Select((r, i) => (i, r)))
        {
            var id = $"tk-demo-{i}";
            if (db.Tickets.Any(t => t.Id == id)) continue;

            var created = DaysAgo(r.Days);
            db.Tickets.Add(new Ticket
            {
                Id = id,
                Reference = $"FR-{1400 + i}-{100 + i * 7}",
                UserId = r.User,
                Subject = r.Subject,
                Category = r.Cat,
                Priority = r.Prio,
                Status = r.Status,
                Environment = "ویندوز ۱۱ — نسخهٔ ۱.۴.۲",
                CreatedAt = created,
                UpdatedAt = created.AddHours(r.Msgs.Length * 6),
                Messages = r.Msgs.Select((m, k) => new TicketMessage
                {
                    Id = $"tkm-demo-{i}-{k}",
                    AuthorName = m.Author,
                    IsStaff = m.Staff,
                    Body = m.Body,
                    CreatedAt = created.AddHours(k * 5),
                }).ToList(),
            });
        }
    }

    /* ══════════════════════════════════════════════════════════════════
       ۷. درخواست‌هایِ دمو
       ══════════════════════════════════════════════════════════════════ */
    private static void DemoRequests(AppDbContext db)
    {
        var rows = new (string Name, string Email, string Company, string Size, string Phone,
                        string Message, DemoRequestStatus Status, int Days)[]
        {
            ("فریبا نوری", "f.nouri@sanat-co.example", "صنایع فلزی نور", "۵۰ تا ۲۰۰ نفر",
             "۰۹۱۲۳۴۵۶۷۸۹", "برای خطِ تولیدِمان به انبار و بهایِ تمام‌شده نیاز داریم.", DemoRequestStatus.New, 1),
            ("بهمن رستمی", "b.rostami@pars-trade.example", "بازرگانی پارس‌یار", "۱۰ تا ۵۰ نفر",
             "۰۹۱۳۱۱۲۲۳۳۴", "فروشِ اینترنتی داریم و می‌خواهیم با انبار هماهنگ شود.", DemoRequestStatus.Contacted, 4),
            ("الهام صالحی", "e.salehi@omran-co.example", "عمران و ساختمان صالحی", "۱۰ تا ۵۰ نفر",
             "۰۹۳۵۱۲۳۴۵۶۷", "پروژه‌محور کار می‌کنیم؛ گزارشِ هزینهٔ پروژه مهم است.", DemoRequestStatus.New, 2),
            ("سعید مرادی", "s.moradi@zafaran.example", "کارخانهٔ بسته‌بندی زعفران", "۵۰ تا ۲۰۰ نفر",
             "۰۹۱۹۸۷۶۵۴۳۲", "قصدِ تغییر از نرم‌افزارِ قبلی را داریم؛ انتقالِ داده هم می‌خواهیم.", DemoRequestStatus.Qualified, 7),
            ("پرستو کیانی", "p.kiani@digi-shop.example", "فروشگاه آنلاین کیان", "کمتر از ۱۰ نفر",
             "۰۹۰۱۲۳۴۵۶۷۸", "صندوقِ فروشگاهی و اتصال به سامانهٔ مؤدیان.", DemoRequestStatus.New, 1),
            ("مهدی یوسفی", "m.yousefi@nikangroup.example", "هلدینگ نیکان", "بیش از ۲۰۰ نفر",
             "۰۲۱۸۸۷۷۶۵۴۳", "چند شرکتِ تابعه داریم؛ گزارشِ تجمیعی لازم داریم.", DemoRequestStatus.Closed, 12),
        };

        foreach (var (i, r) in rows.Select((r, i) => (i, r)))
        {
            var id = $"dr-demo-{i}";
            if (db.DemoRequests.Any(d => d.Id == id)) continue;
            db.DemoRequests.Add(new DemoRequest
            {
                Id = id, Name = r.Name, Email = r.Email, Company = r.Company,
                CompanySize = r.Size, Phone = r.Phone, Message = r.Message,
                Status = r.Status, CreatedAt = DaysAgo(r.Days),
            });
        }
    }

    /* ══════════════════════════════════════════════════════════════════
       ۸. پیام‌هایِ تماس
       ══════════════════════════════════════════════════════════════════ */
    private static void ContactMessages(AppDbContext db)
    {
        var rows = new (string Name, string Email, string Company, string Topic, string Msg, bool Read, int Days)[]
        {
            ("نادر فتحی", "n.fathi@toolid.example", "تولیدی فتحی", "فروش",
             "برای ۱۵ کاربر چه طرحی پیشنهاد می‌کنید؟", false, 1),
            ("سمیه رضوی", "s.razavi@hesab.example", "مؤسسه حسابرسی رضوی", "همکاری",
             "می‌خواهیم نمایندهٔ شما در استان باشیم.", false, 2),
            ("کیوان اکبری", "k.akbari@shop.example", "فروشگاه اکبری", "پشتیبانی",
             "نسخهٔ آزمایشی‌ام تمام شد؛ چطور تمدید کنم؟", true, 5),
            ("لیلا سلیمانی", "l.solimani@daroo.example", "شرکت پخش دارو", "فروش",
             "قیمتِ بستهٔ بازرگانی برای ۱۲ ماه را می‌خواهم.", false, 3),
            ("امیرحسین تقوی", "a.taghavi@sazeh.example", "سازه‌های فولادی تقوی", "فنی",
             "امکانِ اتصال به دستگاهِ حضور و غیابِ ما وجود دارد؟", true, 8),
            ("راضیه محمودی", "r.mahmoodi@charity.example", "مؤسسه خیریه مهر", "عمومی",
             "برای مؤسساتِ غیرانتفاعی تخفیف دارید؟", false, 1),
        };

        foreach (var (i, r) in rows.Select((r, i) => (i, r)))
        {
            var id = $"cm-demo-{i}";
            if (db.ContactMessages.Any(c => c.Id == id)) continue;
            db.ContactMessages.Add(new ContactMessage
            {
                Id = id, Name = r.Name, Email = r.Email, Company = r.Company,
                Topic = r.Topic, Message = r.Msg, IsRead = r.Read, CreatedAt = DaysAgo(r.Days),
            });
        }
    }

    /* ══════════════════════════════════════════════════════════════════
       ۹. اعلان‌ها
       ══════════════════════════════════════════════════════════════════ */
    private static void Notifications(AppDbContext db)
    {
        var rows = new (string User, string Title, string Body, string Kind, bool Read, string Link, int Hours)[]
        {
            ("u-casey", "نسخهٔ ۱.۴.۲ منتشر شد", "سازگاری با سامانهٔ مؤدیان و رفعِ خطاهایِ گزارش.", "info", false, "/dashboard/downloads", 6),
            ("u-casey", "تیکت شما به‌روزرسانی شد", "پشتیبانی به تیکتِ FR-1400-100 پاسخ داد.", "info", false, "/dashboard/tickets/tk-demo-0", 20),
            ("u-casey", "لایسنس رو به اتمام است", "اشتراکِ انبارداری ۲۱ روز دیگر پایان می‌یابد.", "warning", false, "/dashboard/licenses", 48),
            ("u-priya", "صورتحساب صادر شد", "فاکتورِ شماره ۱۲۰۴ به مبلغ ۵٬۴۰۰٬۰۰۰ تومان.", "success", true, "/dashboard/invoices", 30),
            ("u-priya", "درخواستِ دمو دریافت شد", "کارشناسِ ما تا ۲۴ ساعت با شما تماس می‌گیرد.", "success", true, null, 72),
            ("u-marcus", "پرداخت با موفقیت انجام شد", "اشتراکِ صندوقِ فروشگاهی فعال شد.", "success", true, "/dashboard/licenses", 96),
            ("u-marcus", "به‌روزرسانیِ امنیتی", "لطفاً نسخهٔ ۳.۱.۰ را نصب کنید.", "warning", false, "/dashboard/downloads", 10),
            ("u-hana", "فیشِ حقوقِ این ماه آماده است", "تعدادِ ۴۲ فیش صادر شده است.", "info", false, "/dashboard", 14),
            ("u-hana", "مهلتِ ارسالِ صورت‌حساب", "تا پایانِ ماه ۵ روز باقی است.", "warning", false, null, 40),
            ("u-casey", "گزارشِ ماهانه آماده شد", "گزارشِ عملکردِ ماه را ببینید.", "info", true, "/dashboard", 120),
        };

        foreach (var (i, r) in rows.Select((r, i) => (i, r)))
        {
            var id = $"nt-demo-{i}";
            if (db.Notifications.Any(n => n.Id == id)) continue;
            db.Notifications.Add(new Notification
            {
                Id = id, UserId = r.User, Title = r.Title, Body = r.Body,
                Kind = r.Kind, IsRead = r.Read, Link = r.Link, CreatedAt = HoursAgo(r.Hours),
            });
        }
    }
}
