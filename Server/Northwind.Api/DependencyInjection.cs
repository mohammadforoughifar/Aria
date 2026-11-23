using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Northwind.Api.Services.Admin;
using Northwind.Api.Services.Catalog;
using Northwind.Api.Services.Commerce;
using Northwind.Api.Services.Content;
using Northwind.Api.Services.Identity;
using Northwind.Api.Services.Security;
using Northwind.Api.Services.Support;
using Northwind.Api.ViewModels.Common;

namespace Northwind.Api;

public static class DependencyInjection
{
    /// <summary>
    /// ثبتِ کاملِ لایهٔ API: سرویس‌ها، احراز هویتِ JWT، کنترلرها و
    /// شکلِ یکسانِ خطاها.
    /// </summary>
    public static IServiceCollection AddNorthwindApi(this IServiceCollection services, IConfiguration configuration)
    {
        /* ------------------------------ امنیت ------------------------------ */
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        services.AddSingleton(jwt);
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = jwt.SigningKey,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();

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

        /* ----------------------------- کنترلرها ----------------------------- */
        services.AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly)
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                // هم‌شکل با API قدیمی: enumها رویِ سیم رشته می‌شوند، نه عدد.
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // اعتبارسنجیِ ورودی هم با همان شکلِ خطایِ یکسان برگردد.
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(kv => kv.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kv => JsonNamingPolicy.CamelCase.ConvertName(kv.Key),
                        kv => kv.Value!.Errors.Select(e =>
                            string.IsNullOrWhiteSpace(e.ErrorMessage) ? "مقدارِ نامعتبر." : e.ErrorMessage).ToArray());

                var payload = new ApiErrorViewModel
                {
                    Code = "validation_failed",
                    Message = "برخی از فیلدها معتبر نیستند.",
                    Status = StatusCodes.Status400BadRequest,
                    Errors = errors,
                    TraceId = context.HttpContext.TraceIdentifier
                };

                return new BadRequestObjectResult(payload);
            };
        });

        return services;
    }
}
