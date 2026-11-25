using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Northwind.Core;
using Northwind.Core.Models.Common;
using Northwind.Core.Services.Security;

namespace Northwind.Api;

public static class DependencyInjection
{
    /// <summary>
    /// ثبتِ لایهٔ API: احراز هویت، کنترلرها و شکلِ یکسانِ خطاها.
    /// منطقِ کاری از <see cref="Northwind.Core.DependencyInjection.AddNorthwindCore"/> می‌آید.
    /// </summary>
    public static IServiceCollection AddNorthwindApi(this IServiceCollection services, IConfiguration configuration)
    {
        /* ------------------------ منطقِ کاری (لایهٔ هسته) ------------------------ */
        services.AddNorthwindCore(configuration);

        /* ------------------------------ امنیتِ HTTP --------------------------- */
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

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
