using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Northwind.Api.Services.Security;
using Northwind.Domain.Entities.Catalog;
using Northwind.Domain.Entities.Commerce;
using Northwind.Domain.Entities.Content;
using Northwind.Domain.Entities.Identity;
using Northwind.Domain.Entities.Support;
using Northwind.Domain.Enums;
using Northwind.Infrastructure.Persistence;

namespace Northwind.Api.Services.Seed;

/// <summary>
/// دادهٔ نمایشیِ پایگاه‌دادهٔ تازه (SQL Server یا فالبکِ SQLite).
///
/// فقط وقتی اجرا می‌شود که جدولِ محصولات خالی باشد، پس تکرارِ آن رکورد
/// تکراری نمی‌سازد. مثلِ نسخهٔ قبلی، شکستِ یک بخش بقیه را متوقف نمی‌کند.
/// </summary>
public static class DemoSeeder
{
    public const string DemoPassword = "Password123!";

    public static async Task<bool> SeedAsync(NorthwindDbContext db, IPasswordHasher hasher, ILogger logger, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct))
        {
            logger.LogInformation("دادهٔ نمایشی از پیش هست — رد شد.");
            return false;
        }

        var now = DateTime.UtcNow;

        try
        {
            /* ------------------------------ تنظیمات ------------------------------ */
            db.SiteSettings.Add(new SiteSettings
            {
                SiteName = "فروغ آریا پارسیان",
                SupportEmail = "support@aria.example",
                SalesEmail = "sales@aria.example",
                SupportPhone = "021-91002000",
                HeroHeadline = "نرم‌افزارهایِ یکپارچه روی یک پایگاه داده",
                HeroSubheading = "از حسابداری و فروش تا منابعِ انسانی — یک اکوسیستم، یک زبانِ مشترک.",
                TaxRateBps = 900,
                TrialDays = 14,
                MaintenanceMode = false,
                Announcement = "ارسال رایگانِ لایسنسِ آزمایشی تا پایانِ ماه"
            });

            /* ------------------------------ دسته‌ها ------------------------------ */
            var categories = new[]
            {
                new Category { Name = "مالی و حسابداری", Slug = "finance", Icon = "wallet", SortOrder = 1 },
                new Category { Name = "فروش و بازاریابی", Slug = "sales", Icon = "chart", SortOrder = 2 },
                new Category { Name = "منابع انسانی", Slug = "hr", Icon = "users", SortOrder = 3 },
                new Category { Name = "انبار و لجستیک", Slug = "warehouse", Icon = "package", SortOrder = 4 },
                new Category { Name = "تولید و کارخانه", Slug = "manufacturing", Icon = "factory", SortOrder = 5 },
                new Category { Name = "ابزارهایِ مدیریتی", Slug = "tools", Icon = "settings", SortOrder = 6 },
                new Category { Name = "دستیار و موبایل", Slug = "mobile", Icon = "smartphone", SortOrder = 7 },
                new Category { Name = "زیرساخت و امنیت", Slug = "infra", Icon = "shield", SortOrder = 8 }
            };
            db.Categories.AddRange(categories);
            await db.SaveChangesAsync(ct);

            /* ------------------------------ محصولات ------------------------------ */
            // (نام، اسلاگ، شعار، دسته، قیمتِ پایه، نوع، رنگ)
            var productSpecs = new (string Name, string Slug, string Tagline, int Cat, decimal Price, ProductType Type, string Color, bool Featured, int Trial)[]
            {
                ("سپیدار", "sepidaar", "حسابداریِ یکپارچه با دفترِ روزنامه و ترازنامه", 0, 24_500_000, ProductType.DesktopApp, "#1E3A8A", true, 14),
                ("مهراز", "mehraz", "حقوق و دستمزد با محاسبهٔ خودکارِ بیمه و مالیات", 0, 18_900_000, ProductType.Module, "#0F766E", true, 14),
                ("رایان‌فروش", "rayanforoush", "مدیریتِ فروش، پیش‌فاکتور و ویزیتورِ موبایل", 1, 21_000_000, ProductType.DesktopApp, "#B45309", true, 21),
                ("کانون", "kanoon", "CRM سبک برایِ پیگیریِ سرنخ و قیفِ فروش", 1, 14_800_000, ProductType.Module, "#7C3AED", false, 14),
                ("نیرو", "niroo", "حضور و غیاب، مرخصی و ارزیابیِ کارکنان", 2, 16_400_000, ProductType.Module, "#BE123C", true, 14),
                ("انباردار", "anbardar", "موجودیِ چند انباره با بارکد و شمارشِ دوره‌ای", 3, 19_600_000, ProductType.DesktopApp, "#0369A1", false, 14),
                ("کارگاه", "kargah", "برنامه‌ریزیِ تولید، درختِ محصول و مصرفِ مواد", 4, 32_500_000, ProductType.DesktopApp, "#134E4A", true, 30),
                ("کنترل", "kontrol", "کنترلِ کیفیت و ثبتِ عدم‌انطباق", 4, 15_900_000, ProductType.AddOn, "#4338CA", false, 14),
                ("داشبورد آریا", "aria-dashboard", "شاخص‌هایِ زنده روی یک صفحه", 5, 9_900_000, ProductType.Module, "#0E7490", false, 14),
                ("آریا موبایل", "aria-mobile", "نسخهٔ موبایل برایِ ویزیتور و انباردار", 6, 7_500_000, ProductType.DesktopApp, "#9333EA", true, 14),
                ("آریا کلود", "aria-cloud", "پشتیبان‌گیریِ ابری و بازیابیِ رمزنگاری‌شده", 7, 12_000_000, ProductType.Module, "#1D4ED8", false, 14),
                ("نگهبان", "negahban", "گزارشِ رخداد و ردگیریِ تغییراتِ حساس (Audit)", 7, 11_200_000, ProductType.AddOn, "#374151", false, 14)
            };

            var products = new List<Product>();
            var index = 0.0;

            foreach (var spec in productSpecs)
            {
                index += 1;
                var product = new Product
                {
                    Name = spec.Name,
                    Slug = spec.Slug,
                    Tagline = spec.Tagline,
                    Description = $"{spec.Tagline}. نسخهٔ {DateTime.UtcNow.Year} با پشتیبانیِ فارسی، گزارش‌هایِ رسمی و راه‌اندازیِ همراه.",
                    Type = spec.Type,
                    Audience = spec.Featured ? Audience.Both : Audience.B2B,
                    Platform = spec.Type == ProductType.AddOn ? "All" : "Windows",
                    Version = "4.2.0",
                    BasePriceToman = spec.Price,
                    AccentColor = spec.Color,
                    IsFeatured = spec.Featured,
                    IsActive = true,
                    TrialDays = spec.Trial,
                    Popularity = (int)(100 - index * 6),
                    CategoryId = categories[spec.Cat].Id,
                    Features = new List<string>
                    {
                        "نصب و راه‌اندازیِ ساده",
                        "گزارش‌هایِ رسمی و قابلِ سفارشی‌سازی",
                        "دسترسیِ نقش‌محور",
                        "پشتیبان‌گیریِ خودکار"
                    },
                    Requirements = new List<string> { "ویندوز ۱۰ یا جدیدتر", "رم ۸ گیگابایت", "فضایِ ۲ گیگابایت" },
                    Gallery = new List<string>(),

                    // ویدیویِ معرفی: لینکِ نمونه رویِ آپارات. مدیر از پنل با لینکِ
                    // واقعیِ خودش عوضش می‌کند؛ صفحهٔ محصول همان‌جا پخشش می‌کند.
                    DemoVideoUrl = spec.Featured ? "https://www.aparat.com/v/nlk1rob" : null
                };

                product.Plans.Add(new Plan
                {
                    Name = "تک‌کاربره", Tier = PlanTier.SingleUser, BillingType = BillingType.OneTime,
                    PriceToman = spec.Price, Seats = 1, SortOrder = 1, LicenseMonths = 0,
                    Description = "برایِ کسب‌وکارهایِ کوچک",
                    Features = new List<string> { "یک کاربر", "پشتیبانیِ ایمیلی", "به‌روزرسانیِ یک‌ساله" }
                });

                product.Plans.Add(new Plan
                {
                    Name = "چندکاربره", Tier = PlanTier.MultiUser, BillingType = BillingType.OneTime,
                    PriceToman = spec.Price * 2.4m, Seats = 5, SortOrder = 2, IsPopular = true, LicenseMonths = 12,
                    Description = "پرفروش‌ترین گزینه برایِ دفترهایِ متوسط",
                    Features = new List<string> { "پنج کاربر", "پشتیبانیِ تلفنی", "آموزشِ آنلاین", "به‌روزرسانیِ دائمی" }
                });

                product.Plans.Add(new Plan
                {
                    Name = "سازمانی", Tier = PlanTier.Enterprise, BillingType = BillingType.Annual,
                    PriceToman = spec.Price * 6.5m, Seats = 25, SortOrder = 3, LicenseMonths = 12,
                    Description = "برایِ سازمان‌هایِ چند شعبه",
                    Features = new List<string> { "۲۵ کاربر", "پشتیبانیِ اختصاصی", "استقرارِ رویِ سرورِ شما", "شخصی‌سازیِ گزارش‌ها" }
                });

                product.Versions.Add(new ProductVersion
                {
                    ProductId = product.Id, Version = "4.2.0", ReleasedAt = now.AddDays(-12),
                    Notes = "رفعِ اشکالِ گزارشِ مالیاتی و بهبودِ سرعتِ بارگذاری", SizeMb = 184,
                    Platform = "Windows", InstallerUrl = null
                });

                product.Versions.Add(new ProductVersion
                {
                    ProductId = product.Id, Version = "4.1.3", ReleasedAt = now.AddMonths(-3),
                    Notes = "افزودنِ خروجیِ اکسل و اصلاحِ چند خطایِ نمایشی", SizeMb = 181,
                    Platform = "Windows", InstallerUrl = null
                });

                products.Add(product);
            }

            db.Products.AddRange(products);
            await db.SaveChangesAsync(ct);

            /* ------------------------------ کاربران ------------------------------ */
            var hash = hasher.Hash(DemoPassword);

            var admin = new User
            {
                Email = "admin@northwindsoft.example", FullName = "مدیر سامانه", PasswordHash = hash,
                Role = Role.Admin, AccountType = AccountType.Organization, Company = "فروغ آریا پارسیان",
                Country = "Iran", Phone = "09120000000"
            };

            var customers = new[]
            {
                new User { Email = "casey.demo@example.com", FullName = "کیان رستگار", PasswordHash = hash, Company = "گروه صنعتی رستگار", Country = "Iran", Phone = "09121111111" },
                new User { Email = "priya.demo@example.com", FullName = "پریا نادری", PasswordHash = hash, Company = "بازرگانی نادر", Country = "Iran", Phone = "09122222222" },
                new User { Email = "marcus.demo@example.com", FullName = "مرتضی کریمی", PasswordHash = hash, Company = "پخش کریمی", Country = "Iran", Phone = "09123333333", AccountType = AccountType.Organization },
                new User { Email = "hana.demo@example.com", FullName = "هانا صدیقی", PasswordHash = hash, Country = "Iran", Phone = "09124444444" }
            };

            db.Users.Add(admin);
            db.Users.AddRange(customers);
            await db.SaveChangesAsync(ct);

            /* ------------------------------ کدِ تخفیف ------------------------------ */
            var discount = new DiscountCode
            {
                Code = "ARIA20", Description = "۲۰٪ تخفیفِ راه‌اندازی", PercentOff = 20,
                MaxRedemptions = 100, ExpiresAt = now.AddMonths(3), IsActive = true
            };
            var expiredDiscount = new DiscountCode
            {
                Code = "NOWRUZ", Description = "کدِ نوروزیِ گذشته", PercentOff = 15,
                MaxRedemptions = 50, ExpiresAt = now.AddMonths(-1), IsActive = true
            };
            db.DiscountCodes.AddRange(discount, expiredDiscount);

            /* ------------------------------ سفارش‌ها ------------------------------ */
            var orderSeeds = new (int Customer, int Product, int Plan, int MonthsAgo, OrderStatus Status)[]
            {
                (0, 0, 1, 0, OrderStatus.Paid),
                (0, 4, 2, 1, OrderStatus.Completed),
                (1, 2, 1, 2, OrderStatus.Paid),
                (2, 6, 2, 3, OrderStatus.Completed),
                (2, 5, 0, 4, OrderStatus.Paid),
                (3, 9, 1, 5, OrderStatus.Paid),
                (1, 8, 0, 0, OrderStatus.Pending)
            };

            var orderNo = 1;
            var invoiceNo = 1;

            foreach (var seed in orderSeeds)
            {
                var customer = customers[seed.Customer];
                var product = products[seed.Product];
                var plan = product.Plans.ElementAt(seed.Plan);
                var placedAt = now.AddMonths(-seed.MonthsAgo).AddDays(-seed.Customer * 2);

                var subtotal = plan.PriceToman;
                var tax = Math.Round(subtotal * 0.09m, 2, MidpointRounding.AwayFromZero);

                var order = new Order
                {
                    OrderNumber = $"NW-{now.Year}-{orderNo++:D4}",
                    UserId = customer.Id,
                    Status = seed.Status,
                    SubtotalToman = subtotal,
                    TaxToman = tax,
                    TotalToman = subtotal + tax,
                    PlacedAt = placedAt,
                    BillingName = customer.FullName,
                    BillingCompany = customer.Company,
                    BillingEmail = customer.Email,
                    BillingCountry = "Iran"
                };

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id, PlanId = plan.Id,
                    ProductName = product.Name, PlanName = plan.Name,
                    BillingType = plan.BillingType, UnitPriceToman = plan.PriceToman,
                    Quantity = 1, LineTotalToman = plan.PriceToman
                });

                if (seed.Status != OrderStatus.Pending)
                {
                    order.Payment = new Payment
                    {
                        AmountToman = subtotal + tax, Status = PaymentStatus.Succeeded,
                        Provider = "mock", Method = "card", PaidAt = placedAt,
                        TransactionRef = $"MOCK-{placedAt:yyyyMMdd}{orderNo:D4}"
                    };

                    order.Invoice = new Invoice
                    {
                        InvoiceNumber = $"INV-{now.Year}-{invoiceNo++:D4}",
                        Status = InvoiceStatus.Paid, IssuedAt = placedAt,
                        TotalToman = subtotal + tax, TaxToman = tax
                    };

                    db.Licenses.Add(new License
                    {
                        LicenseKey = LicenseKeyFor(orderNo, seed.Customer),
                        UserId = customer.Id,
                        ProductId = product.Id,
                        PlanId = plan.Id,
                        Seats = plan.Seats,
                        Status = LicenseStatus.Active,
                        IssuedAt = placedAt,
                        ExpiresAt = plan.LicenseMonths <= 0 ? null : placedAt.AddMonths(plan.LicenseMonths)
                    });
                }

                db.Orders.Add(order);
            }

            await db.SaveChangesAsync(ct);

            /* ------------------------------ تیکت‌ها ------------------------------ */
            var tickets = new[]
            {
                new Ticket
                {
                    Reference = $"TK-{now.Year}-0001", UserId = customers[0].Id, ProductId = products[0].Id,
                    Subject = "خطا در ثبتِ سندِ حسابداری", Category = "فنی - نرم‌افزار", Priority = TicketPriority.High,
                    Status = TicketStatus.InProgress, Environment = "ویندوز ۱۱، نسخهٔ ۴.۲.۰",
                    LastActivityAt = now.AddHours(-6),
                    Messages =
                    {
                        new TicketMessage { AuthorName = "کیان رستگار", IsStaff = false, CreatedAt = now.AddDays(-1),
                            Body = "هنگامِ ثبتِ سندِ افتتاحیه پیامِ «دورهٔ مالی قفل است» می‌گیرم." },
                        new TicketMessage { AuthorName = "پشتیبانی", IsStaff = true, CreatedAt = now.AddHours(-6),
                            Body = "دورهٔ مالی را از تنظیمات باز کنید؛ راهنمایِ گام‌به‌گام را هم ایمیل کردم." }
                    }
                },
                new Ticket
                {
                    Reference = $"TK-{now.Year}-0002", UserId = customers[1].Id, ProductId = products[2].Id,
                    Subject = "درخواستِ افزودنِ کاربرِ جدید", Category = "حساب کاربری", Priority = TicketPriority.Normal,
                    Status = TicketStatus.Open, LastActivityAt = now.AddHours(-2),
                    Messages =
                    {
                        new TicketMessage { AuthorName = "پریا نادری", IsStaff = false, CreatedAt = now.AddHours(-2),
                            Body = "دو کاربرِ تازه به تیمِ فروش اضافه شده؛ دسترسیِ آن‌ها را باز می‌کنید؟" }
                    }
                },
                new Ticket
                {
                    Reference = $"TK-{now.Year}-0003", UserId = customers[2].Id, ProductId = products[6].Id,
                    Subject = "کندیِ گزارشِ مصرفِ مواد", Category = "کارایی", Priority = TicketPriority.Critical,
                    Status = TicketStatus.WaitingCustomer, LastActivityAt = now.AddDays(-3),
                    Messages =
                    {
                        new TicketMessage { AuthorName = "مرتضی کریمی", IsStaff = false, CreatedAt = now.AddDays(-4),
                            Body = "گزارشِ مصرفِ مواد برایِ یک ماه بیش از دو دقیقه طول می‌کشد." },
                        new TicketMessage { AuthorName = "پشتیبانی", IsStaff = true, CreatedAt = now.AddDays(-3),
                            Body = "فایلِ پشتیبان و لاگ را برایِ بررسی ارسال می‌کنید؟" }
                    }
                }
            };
            db.Tickets.AddRange(tickets);

            /* ------------------------------ محتوا ------------------------------ */
            var articles = new[]
            {
                new Article { Slug = "rahnamaye-entekhabe-narmafzar", Title = "راهنمایِ انتخابِ نرم‌افزارِ حسابداری", Excerpt = "پیش از خرید، این هفت پرسش را از فروشنده بپرسید.", Body = "متنِ کاملِ مقاله…", Category = "راهنما", ReadMinutes = 6, PublishedAt = now.AddDays(-5) },
                new Article { Slug = "maliyat-bar-afzude", Title = "مالیاتِ بر ارزشِ افزوده در فروشِ نرم‌افزار", Excerpt = "چه زمانی نرم‌افزار مشمولِ مالیات است؟", Body = "متنِ کاملِ مقاله…", Category = "مالی", ReadMinutes = 4, PublishedAt = now.AddDays(-12) },
                new Article { Slug = "hemkari-do-narmafzar", Title = "وقتی دو نرم‌افزار باید حرف بزنند", Excerpt = "یکپارچگیِ داده بدونِ کدنویسی.", Body = "متنِ کاملِ مقاله…", Category = "فنی", ReadMinutes = 8, PublishedAt = now.AddDays(-20) },
                new Article { Slug = "poshtibani-dorosti", Title = "پشتیبانیِ خوب چه شکلی است؟", Excerpt = "چهار نشانه که قراردادِ پشتیبانی‌ات را نجات می‌دهد.", Body = "متنِ کاملِ مقاله…", Category = "تجربه", ReadMinutes = 5, PublishedAt = now.AddDays(-30) },
                new Article { Slug = "rahandazi-bi-dard", Title = "راه‌اندازیِ بی‌دردسر", Excerpt = "چک‌لیستِ آماده‌سازی پیش از استقرار.", Body = "متنِ کاملِ مقاله…", Category = "راهنما", ReadMinutes = 7, PublishedAt = now.AddDays(-40) },
                new Article { Slug = "amniat-etelaat", Title = "امنیتِ اطلاعات در نرم‌افزارِ سازمانی", Excerpt = "از رمزنگاری تا ثبتِ رخداد.", Body = "متنِ کاملِ مقاله…", Category = "امنیت", ReadMinutes = 9, PublishedAt = now.AddDays(-50) }
            };
            db.Articles.AddRange(articles);

            db.Testimonials.AddRange(
                new Testimonial { AuthorName = "سعید مرادی", AuthorRole = "مدیر مالی", Company = "زعفرانِ خراسان", Quote = "بستنِ حسابِ پایانِ ماه از یک هفته به دو روز رسید.", Rating = 5 },
                new Testimonial { AuthorName = "لیلا سلیمانی", AuthorRole = "مدیر فروش", Company = "دارو پخش", Quote = "ویزیتورها با موبایل سفارش می‌گیرند؛ گزارش صبحِ فردا آماده است.", Rating = 5 },
                new Testimonial { AuthorName = "نادر فتحی", AuthorRole = "مدیر تولید", Company = "صنایعِ تولیدی", Quote = "برنامه‌ریزیِ تولید از حالتِ اکسل بیرون آمد و قابلِ اتکا شد.", Rating = 4 },
                new Testimonial { AuthorName = "کامران اکبری", AuthorRole = "مدیرعامل", Company = "بازرگانی اکسیر", Quote = "پشتیبانی واقعاً پاسخ می‌دهد — چیزی که جایِ دیگری پیدا نکردیم.", Rating = 5 }
            );

            db.Faqs.AddRange(
                new Faq { Question = "نسخهٔ آزمایشی چقدر است؟", Answer = "۱۴ روز، تمامِ امکانات، بدونِ نیاز به کارتِ بانکی.", SortOrder = 1 },
                new Faq { Question = "امکانِ انتقالِ داده از اکسل هست؟", Answer = "بله. قالبِ ورودی را در اختیارتان می‌گذاریم و انتقال با تیمِ راه‌اندازی انجام می‌شود.", SortOrder = 2 },
                new Faq { Question = "لایسنس روی چند سیستم نصب می‌شود؟", Answer = "به تعدادِ کاربرانِ طرح. در طرحِ تک‌کاربره یک سیستم، در چندکاربره پنج سیستم.", SortOrder = 3 },
                new Faq { Question = "به‌روزرسانی چطور انجام می‌شود؟", Answer = "در طرح‌هایِ دارایِ اشتراک خودکار، وگرنه با بستهٔ به‌روزرسانیِ ایمیلی.", SortOrder = 4 },
                new Faq { Question = "فاکتورِ رسمی صادر می‌کنید؟", Answer = "بله، با اطلاعاتِ اقتصادی، کدِ ملی و فاکتورِ الکترونیکی.", SortOrder = 5 },
                new Faq { Question = "امکانِ نصب رویِ سرورِ خودمان هست؟", Answer = "در طرحِ سازمانی، استقرار رویِ سرورِ مشتری یا ابرِ خصوصی انجام می‌شود.", SortOrder = 6 }
            );

            /* ------------------------------ نظرات ------------------------------ */
            db.Reviews.AddRange(
                new Review { ProductId = products[0].Id, UserId = customers[0].Id, AuthorName = "کیان رستگار", AuthorCompany = "گروه رستگار", Rating = 5, Title = "پایدار و دقیق", Body = "سه سال است استفاده می‌کنیم؛ گزارش‌هایِ مالیاتی‌اش بی‌نقص بیرون می‌آید.", Status = ReviewStatus.Approved },
                new Review { ProductId = products[0].Id, AuthorName = "سمیرا احمدی", Rating = 4, Title = "خوب، اما آموزشِ بیشتری می‌خواهد", Body = "قابلیت‌ها زیاد است و آدم باید وقت بگذارد.", Status = ReviewStatus.Approved },
                new Review { ProductId = products[2].Id, AuthorName = "لیلا سلیمانی", AuthorCompany = "دارو پخش", Rating = 5, Title = "ویزیتورها راضی‌اند", Body = "کار با موبایل ساده است و هم‌گام‌سازی خوب کار می‌کند.", Status = ReviewStatus.Approved },
                new Review { ProductId = products[1].Id, AuthorName = "کاربرِ مهمان", Rating = 3, Title = "باید بهتر شود", Body = "محاسبهٔ بیمه در چند مورد دستی اصلاح شد.", Status = ReviewStatus.Pending },
                new Review { ProductId = products[6].Id, AuthorName = "نادر فتحی", AuthorCompany = "صنایع تولیدی", Rating = 5, Title = "دقیقاً چیزی که لازم داشتیم", Body = "درختِ محصول و مصرفِ مواد را کامل پوشش می‌دهد.", Status = ReviewStatus.Pending }
            );

            /* ------------------------------ ارتباط ------------------------------ */
            db.DemoRequests.AddRange(
                new DemoRequest { ProductId = products[6].Id, Name = "حسین رجبی", Email = "h.rajabi@factory.example", Company = "صنایعِ فلزیِ البرز", CompanySize = "۲۰۰-۵۰۰", Phone = "09127777777", Message = "برایِ خطِ تولیدِ دوم دمو می‌خواهیم.", Status = DemoRequestStatus.New },
                new DemoRequest { Name = "مریم اسماعیلی", Email = "m.esmaili@retail.example", Company = "خرده‌فروشیِ زنجیره‌ای", CompanySize = "۵۰-۲۰۰", Status = DemoRequestStatus.Contacted }
            );

            db.ContactMessages.AddRange(
                new ContactMessage { Name = "امیر توکلی", Email = "a.tavakoli@example.com", Topic = "پیش‌فاکتور", Message = "برایِ ۱۲ کاربر قیمتِ نهایی می‌خواستم.", IsRead = false },
                new ContactMessage { Name = "زهرا کاظمی", Email = "z.kazemi@example.com", Topic = "پشتیبانی", Message = "شمارهٔ تماسِ واحدِ پشتیبانی را می‌خواستم.", IsRead = true }
            );

            /* ------------------------------ اعلان‌ها ------------------------------ */
            db.Notifications.AddRange(
                new Notification { UserId = customers[0].Id, Title = "لایسنسِ شما تمدید شد", Body = "اعتبار تا سالِ آینده تمدید شد.", Kind = "success", Link = "/dashboard/licenses" },
                new Notification { UserId = customers[0].Id, Title = "پاسخِ تیکتِ شما", Body = "پشتیبانی به تیکتِ «خطا در ثبتِ سند» پاسخ داد.", Kind = "info", Link = "/dashboard/tickets" },
                new Notification { UserId = customers[1].Id, Title = "فاکتورِ جدید", Body = "فاکتورِ سفارشِ اخیر صادر شد.", Kind = "info", Link = "/dashboard/invoices", IsRead = true }
            );

            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "دادهٔ نمایشی ساخته شد: {Products} محصول، {Users} کاربر، {Orders} سفارش، {Tickets} تیکت",
                products.Count, customers.Length + 1, orderSeeds.Length, tickets.Length);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ساختِ دادهٔ نمایشی شکست خورد — برنامه ادامه می‌دهد.");
            return false;
        }
    }

    private static string LicenseKeyFor(int seed, int customer)
    {
        var alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random(seed * 31 + customer);
        var groups = new string[4];

        for (var g = 0; g < 4; g++)
        {
            var chars = new char[4];
            for (var i = 0; i < 4; i++) chars[i] = alphabet[random.Next(alphabet.Length)];
            groups[g] = new string(chars);
        }

        return string.Join('-', groups);
    }
}
