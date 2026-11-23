// داده‌های نمونهٔ واقعی — فقط در اولین اجرا (وقتی جدولِ تنظیمات خالی است) درج می‌شود.
// مبالغ به تومان هستند.

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Northwind.Shared.Contracts;

namespace Northwind.Server.Data;

public static class Seed
{
    private static string Json(params string[] items) => JsonSerializer.Serialize(items);

    public static void Run(AppDbContext db)
    {
        if (db.Settings.Any()) return;          // قبلاً seed شده

        /* --------------------------- تنظیمات --------------------------- */
        db.Settings.Add(new SiteSettings
        {
            Id = 1,
            SiteName = "نورث‌ویند",
            SupportEmail = "support@northwindsoft.example",
            SalesEmail = "sales@northwindsoft.example",
            SupportPhone = "۰۲۱-۹۱۰۰۰۰۰۰",
            HeroHeadline = "مجموعهٔ نرم‌افزار یکپارچه برای کسب‌وکار شما",
            HeroSubheading = "ماژول‌های ERP، برنامه‌های دسکتاپ و افزونه‌ها — با پشتیبانی، بروزرسانی و نصبِ رایگان.",
            TaxRateBps = 900,
            TrialDays = 14,
            Announcement = "ارسال رایگانِ لایسنسِ آزمایشی تا پایانِ ماه",
        });

        /* --------------------------- کاربران --------------------------- */
        var users = new List<User>
        {
            new() { Id = "u-admin", Email = "admin@northwindsoft.example", FullName = "الکساندرا ریز",
                    PasswordHash = Hash("Password123!"), Role = Role.Admin, AccountType = AccountType.Organization,
                    Company = "نورث‌ویند", Country = "ایران", CreatedAt = DaysAgo(420), LastLoginAt = DaysAgo(1) },
            new() { Id = "u-casey", Email = "casey.demo@example.com", FullName = "کیسی ویتفیلد",
                    PasswordHash = Hash("Password123!"), AccountType = AccountType.Organization,
                    Company = "تولیدی ساحل", Country = "ایران", CreatedAt = DaysAgo(365), LastLoginAt = DaysAgo(2) },
            new() { Id = "u-priya", Email = "priya.demo@example.com", FullName = "پریا راگاوان",
                    PasswordHash = Hash("Password123!"), AccountType = AccountType.Organization,
                    Company = "توزیع پیشرو", Country = "ایران", CreatedAt = DaysAgo(300), LastLoginAt = DaysAgo(5) },
            new() { Id = "u-marcus", Email = "marcus.demo@example.com", FullName = "مارکوس دلگادو",
                    PasswordHash = Hash("Password123!"), AccountType = AccountType.Individual,
                    Country = "ایران", CreatedAt = DaysAgo(240), LastLoginAt = DaysAgo(9) },
            new() { Id = "u-hana", Email = "hana.demo@example.com", FullName = "هانا کوبایاشی",
                    PasswordHash = Hash("Password123!"), AccountType = AccountType.Individual,
                    Country = "ایران", CreatedAt = DaysAgo(180), LastLoginAt = DaysAgo(14) },
        };
        db.Users.AddRange(users);

        /* -------------------------- دسته‌بندی‌ها -------------------------- */
        var cats = new[]
        {
            new Category { Id = "c-erp", Name = "مجموعه ERP", Slug = "erp-suite",
                Description = "ماژول‌های هسته برای مدیریتِ یکپارچهٔ سازمان", Icon = "layers", SortOrder = 1 },
            new Category { Id = "c-fin", Name = "مالی و حسابداری", Slug = "finance-accounting",
                Description = "حسابداری، خزانه‌داری و گزارش‌گری", Icon = "calculator", SortOrder = 2 },
            new Category { Id = "c-ops", Name = "عملیات و انبار", Slug = "operations-inventory",
                Description = "کنترلِ موجودی، زنجیرهٔ تأمین و انبار", Icon = "boxes", SortOrder = 3 },
            new Category { Id = "c-desk", Name = "برنامه‌های دسکتاپ", Slug = "desktop-productivity",
                Description = "ابزارهای رومیزی برای تیم‌ها", Icon = "monitor", SortOrder = 4 },
            new Category { Id = "c-addon", Name = "افزونه‌ها و یکپارچه‌سازی", Slug = "addons-integrations",
                Description = "اتصال به سامانه‌های بیرونی", Icon = "puzzle", SortOrder = 5 },
        };
        db.Categories.AddRange(cats);

        /* ---------------------------- محصولات ---------------------------- */
        var products = new List<Product>
        {
            P("p-erp", "northwind-erp-core", "هستهٔ ERP نورث‌ویند",
              "پایهٔ یکپارچه برای مالی، فروش، خرید و انبار", "c-erp", ProductType.Module, Audience.B2B,
              "#1E3A8A", 49_800_000, 982, true, 4.7m, 128, 1),
            P("p-fin", "northwind-financials", "مالی نورث‌ویند",
              "حسابداریِ عمومی، دریافتنی/پرداختنی و دارایی ثابت", "c-fin", ProductType.Module, Audience.B2B,
              "#0F766E", 42_500_000, 745, true, 4.6m, 96, 2),
            P("p-inv", "northwind-inventory-control", "کنترل موجودی",
              "ردیابیِ موجودی در چند انبار با سریال و بچ", "c-ops", ProductType.Module, Audience.B2B,
              "#B45309", 36_900_000, 612, true, 4.5m, 74, 3),
            P("p-hr", "northwind-hr-payroll", "منابع انسانی و حقوق",
              "کارتِ پرسنلی، حضور و غیاب و محاسبهٔ حقوق", "c-erp", ProductType.Module, Audience.B2B,
              "#BE123C", 31_500_000, 431, false, 4.4m, 58, 4),
            P("p-crm", "northwind-crm-desktop", "CRM دسکتاپ",
              "مدیریتِ سرنخ، فرصت و پیگیریِ مشتریان", "c-desk", ProductType.DesktopApp, Audience.Both,
              "#7C3AED", 12_900_000, 1187, true, 4.8m, 214, 5),
            P("p-pos", "northwind-point-of-sale", "صندوق فروشگاهی",
              "فروشِ حضوری، بارکد و چاپِ فاکتور", "c-desk", ProductType.DesktopApp, Audience.B2C,
              "#0369A1", 8_900_000, 803, false, 4.3m, 141, 6),
            P("p-doc", "northwind-document-manager", "مدیریت اسناد",
              "بایگانی، نسخه‌بندی و جست‌وجوی متن کامل", "c-desk", ProductType.DesktopApp, Audience.Both,
              "#4D7C0F", 6_400_000, 366, false, 4.2m, 62, 7),
            P("p-bi", "northwind-bi-connector", "کانکتور هوش تجاری",
              "اتصالِ زنده به Power BI و Tableau", "c-addon", ProductType.AddOn, Audience.B2B,
              "#0891B2", 18_700_000, 528, true, 4.5m, 47, 8),
            P("p-einv", "northwind-einvoice-gateway", "درگاه فاکتور الکترونیکی",
              "ارسالِ مستقیم به سامانهٔ مودیان", "c-addon", ProductType.AddOn, Audience.B2B,
              "#A16207", 15_200_000, 297, false, 4.4m, 33, 9),
            P("p-scan", "northwind-warehouse-scanner", "اسکنر انبار",
              "جمع‌آوریِ داده با موبایل و بارکدخوان", "c-addon", ProductType.AddOn, Audience.B2B,
              "#C2410C", 11_800_000, 254, false, 4.1m, 28, 10),
        };
        db.Products.AddRange(products);

        /* --------------------------- طرح‌های قیمت --------------------------- */
        var plans = new List<Plan>();
        foreach (var pr in products)
        {
            plans.AddRange(new[]
            {
                new Plan { ProductId = pr.Id, Name = "تک‌کاربره", Tier = PlanTier.SingleUser,
                    BillingType = BillingType.OneTime, PriceToman = pr.BasePriceToman, Seats = 1,
                    Description = "برای یک کاربر روی یک دستگاه", LicenseMonths = 12, SortOrder = 1,
                    FeaturesJson = Json("یک کاربر", "نصب روی یک دستگاه", "پشتیبانی ایمیلی", "بروزرسانی‌های جزئی") },
                new Plan { ProductId = pr.Id, Name = "سازمانی", Tier = PlanTier.MultiUser,
                    BillingType = BillingType.OneTime, PriceToman = (int)(pr.BasePriceToman * 2.4m), Seats = 5,
                    Description = "محبوب‌ترین انتخاب برای تیم‌ها", IsPopular = true, LicenseMonths = 24, SortOrder = 2,
                    FeaturesJson = Json("۵ کاربر هم‌زمان", "نصب نامحدود", "پشتیبانی تلفنی", "بروزرسانی‌های اصلی", "گزارش‌ساز") },
                new Plan { ProductId = pr.Id, Name = "شرکتی", Tier = PlanTier.Enterprise,
                    BillingType = BillingType.Annual, PriceToman = (int)(pr.BasePriceToman * 4.8m), Seats = 25,
                    Description = "برای سازمان‌ها با قراردادِ سالانه", LicenseMonths = 12, SortOrder = 3,
                    FeaturesJson = Json("۲۵ کاربر", "سرور اختصاصی", "مدیرِ حساب", "SLA پاسخ‌گویی", "یکپارچه‌سازی اختصاصی", "آموزش حضوری") },
            });
        }
        db.Plans.AddRange(plans);

        /* ----------------------------- نسخه‌ها ----------------------------- */
        var versions = new List<ProductVersion>();
        foreach (var pr in products)
        {
            versions.AddRange(new[]
            {
                new ProductVersion { ProductId = pr.Id, Version = pr.Version, ReleasedAt = DaysAgo(45),
                    Notes = "بهبودِ عملکرد و رفعِ خطاهای گزارش‌گیری", SizeMb = 184, Platform = pr.Platform },
                new ProductVersion { ProductId = pr.Id, Version = "6.2.0", ReleasedAt = DaysAgo(150),
                    Notes = "نسخهٔ پایدارِ پیشین", SizeMb = 176, Platform = pr.Platform },
            });
        }
        db.ProductVersions.AddRange(versions);

        /* ----------------------------- دیدگاه‌ها ----------------------------- */
        var rnd = new Random(1404);
        var reviews = new List<Review>();
        for (var i = 0; i < 34; i++)
        {
            var pr = products[i % products.Count];
            var status = i % 7 == 0 ? ReviewStatus.Pending : (i % 11 == 0 ? ReviewStatus.Rejected : ReviewStatus.Approved);
            reviews.Add(new Review
            {
                ProductId = pr.Id,
                UserId = users[1 + (i % 4)].Id,
                AuthorName = users[1 + (i % 4)].FullName,
                AuthorCompany = users[1 + (i % 4)].Company,
                Rating = 3 + rnd.Next(3),
                Title = i % 3 == 0 ? "راه‌اندازی سریع و بدون دردسر" : (i % 3 == 1 ? "گزارش‌گیری عالی" : "پشتیبانی پاسخ‌گو"),
                Body = i % 3 == 0
                    ? "نصب روی سرورِ داخلی‌مان کمتر از یک ساعت طول کشید و تیمِ مالی همان روز شروع به کار کرد."
                    : (i % 3 == 1
                        ? "گزارش‌های ترازنامه و سود و زیان دقیقاً همان چیزی است که حسابدار ما می‌خواست."
                        : "تیکت را عصر فرستادم و صبحِ فردا پاسخِ فنی گرفتم؛ روندِ پیگیری شفاف است."),
                Status = status,
                CreatedAt = DaysAgo(rnd.Next(5, 220)),
            });
        }
        db.Reviews.AddRange(reviews);

        db.SaveChanges();      // محصولات/طرح‌ها باید Id داشته باشند تا سفارش‌ها ساخته شوند

        /* -------------------------- کدهای تخفیف -------------------------- */
        db.DiscountCodes.AddRange(
            new DiscountCode { Code = "NOWROUZ20", Description = "کمپینِ نوروز", PercentOff = 20, MaxRedemptions = 200, TimesRedeemed = 63, ExpiresAt = DaysAhead(120) },
            new DiscountCode { Code = "B2B15", Description = "مشتریانِ سازمانی", PercentOff = 15, MaxRedemptions = 100, TimesRedeemed = 28 },
            new DiscountCode { Code = "STUDENT30", Description = "دانشجویی", PercentOff = 30, MaxRedemptions = 50, TimesRedeemed = 41, ExpiresAt = DaysAhead(45) },
            new DiscountCode { Code = "WINTER10", Description = "زمستانه", PercentOff = 10, MaxRedemptions = 500, TimesRedeemed = 187, IsActive = false });

        /* --------------------------- سفارش‌ها --------------------------- */
        var statuses = new[] { OrderStatus.Completed, OrderStatus.Completed, OrderStatus.Paid,
                               OrderStatus.Processing, OrderStatus.Pending, OrderStatus.Refunded };
        var orders = new List<Order>();
        for (var i = 0; i < 46; i++)
        {
            var u = users[1 + (i % 4)];
            var pr = products[rnd.Next(products.Count)];
            var prPlans = plans.Where(p => p.ProductId == pr.Id).ToList();
            var plan = prPlans[rnd.Next(prPlans.Count)];
            var qty = 1 + rnd.Next(3);
            var subtotal = plan.PriceToman * qty;
            var discount = i % 5 == 0 ? (int)(subtotal * 0.15m) : 0;
            var tax = (int)((subtotal - discount) * 0.09m);
            var status = statuses[rnd.Next(statuses.Length)];
            var placed = DaysAgo(rnd.Next(1, 300));

            var order = new Order
            {
                Id = $"o-{i + 1:000}",
                OrderNumber = $"NW-{1403 + i / 20}-{1000 + i}",
                UserId = u.Id,
                Status = status,
                SubtotalToman = subtotal,
                DiscountToman = discount,
                TaxToman = tax,
                TotalToman = subtotal - discount + tax,
                Currency = "IRT",
                DiscountCode = discount > 0 ? "B2B15" : null,
                PlacedAt = placed,
                AccountType = u.AccountType,
                BillingName = u.FullName,
                BillingCompany = u.Company,
                BillingEmail = u.Email,
                BillingCountry = u.Country,
                EconomicCode = u.AccountType == AccountType.Organization ? "۱۰۳۸۰۱۴۵۹۹۰" : null,
                NationalId = u.AccountType == AccountType.Organization ? "۱۰۱۰۳۲۴۵۸۷" : null,
                Items = new List<OrderItem>
                {
                    new() { ProductId = pr.Id, PlanId = plan.Id, ProductName = pr.Name, PlanName = plan.Name,
                            BillingType = plan.BillingType, UnitPriceToman = plan.PriceToman,
                            Quantity = qty, LineTotalToman = subtotal },
                },
            };

            if (status is OrderStatus.Paid or OrderStatus.Completed or OrderStatus.Refunded)
            {
                order.Payment = new Payment
                {
                    OrderId = order.Id, AmountToman = order.TotalToman, Method = "card",
                    Status = status == OrderStatus.Refunded ? PaymentStatus.Refunded : PaymentStatus.Succeeded,
                    TransactionRef = $"TX-{rnd.Next(100000, 999999)}", PaidAt = placed.AddMinutes(4),
                };
            }

            if (status is OrderStatus.Paid or OrderStatus.Completed)
            {
                order.Invoice = new Invoice
                {
                    InvoiceNumber = $"INV-{1403 + i / 20}-{2000 + i}",
                    OrderId = order.Id, Status = status == OrderStatus.Completed ? InvoiceStatus.Paid : InvoiceStatus.Issued,
                    IssuedAt = placed, DueAt = placed.AddDays(30),
                    TotalToman = order.TotalToman, TaxToman = tax,
                };
            }

            orders.Add(order);
        }
        db.Orders.AddRange(orders);
        db.SaveChanges();

        /* --------------------------- لایسنس‌ها --------------------------- */
        var licenses = new List<License>();
        var li = 0;
        foreach (var o in orders.Where(o => (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed)))
        {
            var it = o.Items.First();
            var plan = plans.First(p => p.Id == it.PlanId);
            var expires = o.PlacedAt.AddMonths(plan.LicenseMonths);
            licenses.Add(new License
            {
                Id = $"l-{++li:000}",
                LicenseKey = Key(rnd),
                UserId = o.UserId,
                ProductId = it.ProductId,
                PlanId = plan.Id,
                Seats = plan.Seats,
                ActivationsUsed = rnd.Next(0, plan.Seats + 1),
                Status = expires < DateTime.UtcNow ? LicenseStatus.Expired : LicenseStatus.Active,
                IssuedAt = o.PlacedAt,
                ExpiresAt = expires,
            });
        }
        // یکی باطل‌شده برای دیدنِ همهٔ حالت‌ها
        if (licenses.Count > 3) licenses[^1].Status = LicenseStatus.Revoked;
        db.Licenses.AddRange(licenses);

        /* ---------------------------- تیکت‌ها ---------------------------- */
        var tickets = new List<Ticket>
        {
            T(users[1], products[0], "خطا در صدورِ فاکتورِ فروش", "فاکتور", TicketPriority.High, TicketStatus.InProgress,
              "وقتی فاکتورِ فروش را ثبت می‌کنم، پیام «موجودی کافی نیست» می‌آید در حالی که موجودی هست.",
              "سلام، لطفاً از منوی انبار ← اصلاح موجودی، انبارِ پیش‌فرضِ کالا را بررسی کنید.", 3),
            T(users[2], products[1], "درخواستِ گزارشِ سود و زیانِ تجمیعی", "گزارش‌گیری", TicketPriority.Normal, TicketStatus.WaitingCustomer,
              "نیاز دارم سود و زیان را به تفکیکِ مرکزِ هزینه ببینم.",
              "این امکان در نسخهٔ ۷٫۱ اضافه شده؛ آیا بروزرسانی را انجام داده‌اید؟", 2),
            T(users[3], products[4], "انتقالِ لایسنس به دستگاهِ جدید", "لایسنس", TicketPriority.Low, TicketStatus.Resolved,
              "سیستمِ قدیمی را تعویض کردم و لایسنس فعال نمی‌شود.",
              "لایسنسِ شما آزاد شد؛ اکنون روی دستگاهِ جدید فعال کنید.", 2),
            T(users[4], products[7], "کندی در همگام‌سازی با Power BI", "یکپارچه‌سازی", TicketPriority.Critical, TicketStatus.Open,
              "همگام‌سازیِ شبانه بیش از ۲ ساعت طول می‌کشد.",
              null, 1),
            T(users[1], null, "سؤال دربارهٔ ارتقای طرح", "فروش", TicketPriority.Normal, TicketStatus.Closed,
              "می‌خواهیم از طرحِ سازمانی به شرکتی برویم.",
              "پیش‌فاکتورِ ارتقا برای شما ایمیل شد.", 2),
        };
        db.Tickets.AddRange(tickets);

        /* -------------------------- محتوای سایت -------------------------- */
        db.Testimonials.AddRange(
            new Testimonial { AuthorName = "مریم احمدی", AuthorRole = "مدیر مالی", Company = "تولیدی ساحل",
                Quote = "پیاده‌سازیِ ماژولِ مالی در کمتر از دو هفته انجام شد و خطای انسانی در ثبتِ اسناد تقریباً از بین رفت.", Rating = 5 },
            new Testimonial { AuthorName = "رضا کریمی", AuthorRole = "مدیر فناوری اطلاعات", Company = "توزیع پیشرو",
                Quote = "گزارش‌سازِ اختصاصی باعث شد دیگر منتظرِ تیمِ توسعه نمانیم؛ هر گزارش را خودمان می‌سازیم.", Rating = 5 },
            new Testimonial { AuthorName = "سارا موسوی", AuthorRole = "مدیر فروشگاه", Company = "زنجیرهٔ آفتاب",
                Quote = "صندوقِ فروشگاهی روی همان سخت‌افزارِ قدیمی ما هم روان اجرا شد.", Rating = 4 });

        db.Faqs.AddRange(
            new Faq { Question = "آیا نسخهٔ آزمایشی دارد؟", Answer = "بله، ۱۴ روز نسخهٔ آزمایشیِ کامل در اختیار شماست و برای فعال‌سازی نیازی به کارتِ بانکی نیست.", SortOrder = 1 },
            new Faq { Question = "نصب بر عهدهٔ کیست؟", Answer = "نصب و راه‌اندازیِ اولیه برای طرح‌های سازمانی و شرکتی رایگان انجام می‌شود.", SortOrder = 2 },
            new Faq { Question = "خروجیِ اکسل و PDF دارد؟", Answer = "همهٔ گزارش‌ها را می‌توان به اکسل، PDF و CSV خروجی گرفت.", SortOrder = 3 },
            new Faq { Question = "اتصال به سامانهٔ مودیان چطور انجام می‌شود؟", Answer = "با افزونهٔ درگاهِ فاکتورِ الکترونیکی، ارسال به‌صورت خودکار و در صف انجام می‌شود.", SortOrder = 4 },
            new Faq { Question = "امکانِ انتقالِ لایسنس هست؟", Answer = "بله، در هر لحظه می‌توانید فعال‌سازی را از دستگاهی به دستگاهِ دیگر منتقل کنید.", SortOrder = 5 });

        db.Articles.AddRange(
            A("پیاده‌سازیِ ERP در ۳۰ روز", "erp-30-days", "نقشهٔ راهِ عملی برای استقرارِ سریع بدون توقفِ عملیات.",
              "برای این‌که پروژهٔ ERP در زمان‌بندی بماند، باید قبل از هر چیز فرآیندهای فعلی مستند شوند…", "راهنما", 6),
            A("تفاوتِ طرح‌های سازمانی و شرکتی", "plan-comparison", "چه زمانی ارتقا به طرحِ شرکتی منطقی است؟",
              "اگر بیش از ۱۵ کاربرِ هم‌زمان دارید یا به SLA نیاز دارید، طرحِ شرکتی به‌صرفه‌تر است…", "فروش", 4),
            A("تهیهٔ فاکتورِ رسمی برای شرکت‌ها", "official-invoice", "فیلدهای لازم برای صدورِ فاکتورِ رسمی.",
              "برای فاکتورِ رسمی به شناسهٔ ملی، کدِ اقتصادی و آدرسِ ثبتی نیاز دارید…", "مالی", 5),
            A("بهینه‌سازیِ سرعتِ گزارش‌گیری", "report-performance", "چند تنظیمِ ساده که زمانِ گزارش را نصف می‌کند.",
              "فهرست‌بندیِ ستون‌های پرکاربرد و محدود کردنِ بازهٔ زمانی دو عاملِ اصلیِ سرعت هستند…", "فنی", 7));

        db.DemoRequests.AddRange(
            new DemoRequest { ProductId = "p-erp", Name = "نادر قاسمی", Email = "nader@industrial.example",
                Company = "صنایع فولاد پارس", CompanySize = "250-500", Phone = "۰۲۱-۸۸۰۰۰۰۰۰",
                Message = "برای ۳۰ کاربرِ هم‌زمان در سه سایت نیاز به دمو داریم.", Status = DemoRequestStatus.New, CreatedAt = DaysAgo(3) },
            new DemoRequest { ProductId = "p-crm", Name = "لیلا صادقی", Email = "leila@retail.example",
                Company = "فروشگاه‌های زنجیره‌ای آفتاب", CompanySize = "50-100",
                Message = "امکانِ اتصال به صندوقِ فروشگاهی را هم می‌خواهیم ببینیم.", Status = DemoRequestStatus.Contacted, CreatedAt = DaysAgo(8) });

        db.ContactMessages.AddRange(
            new ContactMessage { Name = "امیر حسینی", Email = "amir@example.com", Company = "شرکت نمونه",
                Topic = "فروش", Message = "برای ۲۰ شعبه قیمت می‌خواهیم.", CreatedAt = DaysAgo(2) },
            new ContactMessage { Name = "زهرا رضایی", Email = "zahra@example.com",
                Topic = "پشتیبانی", Message = "نسخهٔ جدید روی ویندوز ۱۱ نصب نمی‌شود.", CreatedAt = DaysAgo(6), IsRead = true });

        db.Notifications.AddRange(
            new Notification { UserId = "u-casey", Title = "لایسنسِ شما ۲۰ روز دیگر منقضی می‌شود",
                Body = "هستهٔ ERP نورث‌ویند — برای تمدید اقدام کنید.", Kind = "warning", Link = "/dashboard/licenses", CreatedAt = DaysAgo(1) },
            new Notification { UserId = "u-casey", Title = "فاکتورِ جدید صادر شد",
                Body = "فاکتورِ INV-1404-2012 آمادهٔ مشاهده است.", Kind = "info", Link = "/dashboard/invoices", CreatedAt = DaysAgo(4), IsRead = true },
            new Notification { UserId = "u-casey", Title = "پاسخِ جدید در تیکت",
                Body = "تیمِ پشتیبانی به تیکتِ شما پاسخ داد.", Kind = "success", Link = "/dashboard/tickets", CreatedAt = DaysAgo(6) });

        db.SaveChanges();
    }

    /* ------------------------------ کمکی‌ها ------------------------------ */

    private static Product P(string id, string slug, string name, string tagline, string catId,
        ProductType type, Audience audience, string accent, int price, int popularity,
        bool featured, decimal rating, int ratingCount, int order) => new()
    {
        Id = id, Slug = slug, Name = name, Tagline = tagline,
        Description = $"{name} با معماریِ ماژولار طراحی شده تا دقیقاً با فرآیندهای شما هماهنگ شود؛ "
                    + "از استقرارِ داخلی تا دسترسیِ دورکاری، امنیت و گزارش‌گیریِ لحظه‌ای.",
        Type = type, Audience = audience, Platform = "Windows / Linux (سرور)",
        Version = "7.1.0", BasePriceToman = price, AccentColor = accent,
        IsFeatured = featured, IsActive = true, Popularity = popularity,
        CategoryId = catId, CreatedAt = DaysAgo(order * 30),
        FeaturesJson = Json(
            "یکپارچه با سایرِ ماژول‌های نورث‌ویند",
            "گزارش‌سازِ اختصاصی با خروجیِ اکسل و PDF",
            "سطحِ دسترسیِ نقش‌محور",
            "پشتیبان‌گیریِ خودکار",
            "رابطِ فارسیِ کامل با تقویمِ شمسی",
            "ثبتِ رخدادها برای ممیزی"),
        RequirementsJson = Json(
            "ویندوز ۱۰/۱۱ یا ویندوز سرور ۲۰۱۹ به بعد",
            "پردازندهٔ ۴ هسته‌ای",
            "۸ گیگابایت حافظه (۱۶ گیگابایت پیشنهاد می‌شود)",
            "۵۰ گیگابایت فضای دیسک",
            "SQL Server 2019 یا PostgreSQL 14"),
        GalleryJson = Json("داشبورد", "گزارش‌ها", "تنظیمات", "فرم‌ها"),
    };

    private static int _ticketSeq;

    private static Ticket T(User u, Product? p, string subject, string cat, TicketPriority pr,
        TicketStatus st, string first, string? reply, int msgCount) => new()
    {
        Id = $"t-{Guid.NewGuid().ToString("N")[..6]}",
        Reference = $"TK-1404-{1000 + (++_ticketSeq)}",
        UserId = u.Id, ProductId = p?.Id, Subject = subject, Category = cat,
        Priority = pr, Status = st, Environment = "ویندوز ۱۱",
        CreatedAt = DaysAgo(msgCount * 3), UpdatedAt = DaysAgo(1),
        Messages = new List<TicketMessage>
        {
            new() { AuthorName = u.FullName, IsStaff = false, Body = first, CreatedAt = DaysAgo(msgCount * 3) },
            new() { AuthorName = "پشتیبانی نورث‌ویند", IsStaff = true,
                    Body = reply ?? "در حال بررسی هستیم.", CreatedAt = DaysAgo(msgCount * 2) },
        },
    };

    private static Article A(string title, string slug, string excerpt, string body, string? cat, int read) => new()
    {
        Slug = slug, Title = title, Excerpt = excerpt, Body = body,
        Category = cat, ReadMinutes = read, PublishedAt = DaysAgo(read * 7),
    };

    private static string Key(Random r)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var blocks = new List<string>();
        for (var b = 0; b < 4; b++)
        {
            var s = new char[5];
            for (var i = 0; i < 5; i++) s[i] = chars[r.Next(chars.Length)];
            blocks.Add(new string(s));
        }
        return string.Join("-", blocks);
    }

    /* =====================================================================
       به‌روزرسانیِ افزاینده (idempotent)

       Seed.Run فقط در اولین اجرا کار می‌کند؛ اما گاهی لازم است داده‌هایِ
       موجود را بدون پاک‌کردنِ پایگاه‌داده تغییر دهیم (تغییرِ نامِ برند،
       افزودنِ بسته‌های تازه). این متد هر بار اجرا می‌شود و فقط آنچه را
       که نیست می‌سازد یا باید به‌روزرسانی شود، اصلاح می‌کند.
       ===================================================================== */

    public const string Brand = "فروغ آریا پارسیان";

    /// <summary>نام‌های قدیمی که باید به نامِ کنونی ارتقا یابند (به‌ترتیبِ قدمت).</summary>
    private static readonly string[] LegacyBrands = { "نورث‌ویند", "Northwind", "فروغ آریا" };

    public static void Patch(AppDbContext db)
    {
        /* ------------------- ۱. یکسان‌سازیِ نامِ برند ------------------- */
        var settings = db.Settings.FirstOrDefault();
        if (settings is not null && LegacyBrands.Contains(settings.SiteName))
        {
            settings.SiteName = Brand;
            settings.SupportEmail = "support@forougharia.example";
            settings.SalesEmail = "sales@forougharia.example";
        }

        foreach (var u in db.Users.ToList())
            if (u.Company is not null && LegacyBrands.Contains(u.Company))
                u.Company = Brand;

        // نامِ محصولاتی که نامِ برند را یدک می‌کشند هم به‌روزرسانی می‌شود،
        // مگر آنکه از قبل نامِ کامل را داشته باشند.
        foreach (var p in db.Products.ToList())
        {
            if (p.Name.Contains(Brand)) continue;
            foreach (var legacy in LegacyBrands)
            {
                if (!p.Name.Contains(legacy)) continue;
                p.Name = p.Name.Replace(legacy, Brand);
                break;
            }
        }

        /* ---------------------- ۲. بسته‌های اشتراک ---------------------- */
        if (!db.Categories.Any(c => c.Id == "c-bundle"))
            db.Categories.Add(new Category
            {
                Id = "c-bundle", Name = "بسته‌های آماده", Slug = "bundles",
                Description = "بستهٔ متناسب با نوعِ فعالیت شما — ۶ و ۱۲ ماهه",
                Icon = "package", SortOrder = 6,
            });

        var bundles = new[]
        {
            new Bundle("b-khadamati", "بستهٔ خدماتی", "bundle-services",
                "برای شرکت‌های خدماتی، مشاوره‌ای و پیمانکاری",
                new[] { "صورت‌حساب و پیش‌فاکتور", "قرارداد و پروژه", "حقوق و دستمزد",
                        "گزارشِ سود و زیانِ پروژه", "پنلِ مشتریان" },
                4_800_000, 8_400_000, "#2563EB"),

            new Bundle("b-bazargani", "بستهٔ بازرگانی", "bundle-trading",
                "برای بازرگانی، پخش و فروشگاه‌های زنجیره‌ای",
                new[] { "خرید و فروشِ چندارزی", "انبارِ چندگانه", "صندوق فروشگاهی",
                        "ردیابیِ سریال و بچ", "اتصال به بارکدخوان", "گزارشِ گردشِ کالا" },
                5_400_000, 9_600_000, "#1E3A8A"),

            new Bundle("b-tolidi", "بستهٔ تولیدی", "bundle-manufacturing",
                "برای کارگاه‌ها و کارخانه‌های تولیدی",
                new[] { "فرمولِ تولید و BOM", "برنامه‌ریزیِ تولید", "بهای تمام‌شده",
                        "کنترلِ ضایعات", "انبارِ مواد و کالا", "گزارشِ بهره‌وریِ خط" },
                7_200_000, 12_800_000, "#B45309"),

            new Bundle("b-edari", "بستهٔ اداری", "bundle-office",
                "برای دفتر، امورِ اداری و تیم‌های کوچک",
                new[] { "حسابداریِ ساده", "خزانه‌داری و چک", "اموال و دارایی ثابت",
                        "آرشیوِ اسناد", "کارتابل و یادآوری" },
                3_600_000, 6_400_000, "#0F766E"),

            new Bundle("b-ekhtesasi", "بستهٔ اختصاصی", "bundle-custom",
                "سیستم‌های مورد نیازتان را خودتان کنار هم بگذارید",
                new[] { "انتخابِ آزادِ ماژول‌ها", "سفارشی‌سازیِ فرم و گزارش",
                        "یکپارچه‌سازی با سامانه‌های شما", "مدیرِ حسابِ اختصاصی",
                        "آموزشِ اختصاصیِ تیم", "استقرارِ درون‌سازمانی" },
                12_000_000, 21_000_000, "#7C3AED"),
        };

        foreach (var b in bundles)
        {
            var exists = db.Products.Any(p => p.Id == b.Id);
            if (!exists)
            {
                db.Products.Add(new Product
                {
                    Id = b.Id, Slug = b.Slug, Name = b.Name, Tagline = b.Tagline,
                    Description = b.Tagline, Type = ProductType.Module,
                    Audience = Audience.Both, Platform = "وب و ویندوز", Version = "1.0.0",
                    BasePriceToman = b.Price12, AccentColor = b.Color,
                    IsFeatured = true, IsActive = true, TrialDays = 14,
                    Popularity = 900, CategoryId = "c-bundle", CreatedAt = DateTime.UtcNow,
                    FeaturesJson = Json(b.Features),
                    RequirementsJson = Json("مرورگرِ به‌روز برای نسخهٔ وب", "ویندوز ۱۰ به بالا برای نسخهٔ دسکتاپ"),
                    GalleryJson = Json(),
                });
                db.SaveChanges();     // محصول باید پیش از طرح‌ها ذخیره شود
            }

            UpsertPlan(db, b.Id, "bp-" + b.Id[2..] + "-6", "اشتراک ۶ ماهه", 6,
                       b.Price6, 3, false);
            UpsertPlan(db, b.Id, "bp-" + b.Id[2..] + "-12", "اشتراک ۱۲ ماهه", 12,
                       b.Price12, 5, true);

            if (!db.ProductVersions.Any(v => v.ProductId == b.Id))
                db.ProductVersions.Add(new ProductVersion
                {
                    ProductId = b.Id, Version = "1.0.0", ReleasedAt = DaysAgo(20),
                    Notes = "نسخهٔ آغازینِ بسته", SizeMb = 120, Platform = "وب و ویندوز",
                });
        }

        db.SaveChanges();
    }

    /// <summary>طرح را می‌سازد یا اگر هست، فقط قیمتش را هم‌گام می‌کند (ویرایش از پنل را حفظ می‌کند).</summary>
    private static void UpsertPlan(AppDbContext db, string productId, string planId,
                                   string name, int months, int price, int seats, bool popular)
    {
        var plan = db.Plans.FirstOrDefault(p => p.Id == planId);
        if (plan is null)
        {
            db.Plans.Add(new Plan
            {
                Id = planId, ProductId = productId, Name = name,
                Tier = PlanTier.MultiUser, BillingType = BillingType.Annual,
                PriceToman = price, Seats = seats, Description = name,
                IsPopular = popular, LicenseMonths = months,
                SortOrder = months == 6 ? 1 : 2,
                FeaturesJson = Json($"{months} ماه دسترسی", $"{seats} کاربر هم‌زمان",
                                    "پشتیبان‌گیری خودکار", "به‌روزرسانی رایگان"),
            });
        }
        else if (plan.PriceToman == 0)
        {
            plan.PriceToman = price;
        }
    }

    private sealed record Bundle(string Id, string Name, string Slug, string Tagline,
                                 string[] Features, int Price6, int Price12, string Color);

    /// <summary>درهم‌سازیِ ساده برای نسخهٔ نمونه — در نسخهٔ تولید باید PBKDF2/Argon2 باشد.</summary>
    public static string Hash(string password)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(password + "::northwind-demo-salt"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static DateTime DaysAgo(int days) => DateTime.UtcNow.AddDays(-days);
    private static DateTime DaysAhead(int days) => DateTime.UtcNow.AddDays(days);
}
