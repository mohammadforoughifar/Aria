using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Northwind.Core.Services.Admin;
using Northwind.Core.Services.Catalog;
using Northwind.Core.Services.Commerce;
using Northwind.Core.Services.Content;
using Northwind.Core.Services.Identity;
using Northwind.Core.Services.Security;
using Northwind.Core.Services.Support;

namespace Northwind.Core;

public static class DependencyInjection
{
    /// <summary>
    /// ثبتِ لایهٔ هسته: سرویس‌هایِ کاری و ابزارهایِ امنیت.
    /// هیچ چیزی از ASP.NET MVC اینجا ثبت نمی‌شود؛ کنترلرها کارِ لایهٔ API است.
    /// </summary>
    public static IServiceCollection AddNorthwindCore(this IServiceCollection services, IConfiguration configuration)
    {
        /* ------------------------------ امنیت ------------------------------ */
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        services.AddSingleton(jwt);
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        /* ----------------------------- سرویس‌ها ----------------------------- */
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ILicenseService, LicenseService>();
        services.AddScoped<IDownloadService, DownloadService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
