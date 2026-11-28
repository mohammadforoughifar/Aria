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
using Northwind.Core.Services.Common;
using Northwind.Api.Storage;
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

        // نگهبانِ نشست نتیجهٔ بررسیِ وجودِ کاربر را کوتاه‌مدت کش می‌کند
        // تا هر درخواست یک پرس‌وجویِ اضافی به پایگاه‌داده نزند.
        services.AddMemoryCache();

        /* --------------------------- فایل‌هایِ بارگذاری‌شده --------------------------- */
        // پوشهٔ wwwroot/files پشتِ همین سرویس است؛ هم کنترلرِ بارگذاری و هم
        // میان‌افزارِ فایل‌هایِ ایستا از یک مسیر می‌خوانند.
        services.AddSingleton<IFileStorage, FileStorage>();

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
            // پیش‌فرضِ چارچوب، پاسخ‌هایِ ۴۰۰/۴۰۴/۴۱۵ و مانندِ آن را به
            // ProblemDetails انگلیسی تبدیل می‌کند؛ خاموشش می‌کنیم تا همهٔ
            // خطاها از یک مسیر (میان‌افزارِ خطا) با پیامِ فارسی بیرون بروند.
            options.SuppressMapClientErrors = true;

            options.InvalidModelStateResponseFactory = context =>
            {
                // کلیدهایِ «$» و «input» از خودِ JSON‌خوان می‌آیند و پیامشان
                // انگلیسی و فنی است؛ کاربر چیزی از آن‌ها نمی‌فهمد، پس جدا می‌شوند
                // و یک پیامِ فارسیِ روشن جایش می‌نشیند.
                var parserFailed = context.ModelState.Any(kv =>
                    kv.Key is "$" or "input" || kv.Key.Contains("$.", StringComparison.Ordinal));

                var errors = context.ModelState
                    .Where(kv => kv.Value?.Errors.Count > 0 && kv.Key is not ("$" or "input"))
                    .ToDictionary(
                        // کلیدهایِ JSON‌خوان به‌شکلِ «$.type» می‌آیند؛ همان نامِ
                        // فیلد برایِ کلاینت گویاتر است.
                        kv => JsonNamingPolicy.CamelCase.ConvertName(CleanKey(kv.Key)),
                        kv => kv.Value!.Errors
                            .Select(e => ErrorText.Persian(e.ErrorMessage))
                            .Where(m => m.Length > 0)
                            .Distinct()
                            .ToArray())
                    .Where(kv => kv.Value.Length > 0)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

                if (parserFailed && errors.Count == 0)
                {
                    return new BadRequestObjectResult(new ApiErrorViewModel
                    {
                        Code = "invalid_json",
                        Message = "بدنهٔ درخواست JSON معتبر نیست.",
                        Status = StatusCodes.Status400BadRequest,
                        TraceId = context.HttpContext.TraceIdentifier
                    });
                }

                return new BadRequestObjectResult(new ApiErrorViewModel
                {
                    Code = "validation_failed",
                    Message = "برخی از فیلدها معتبر نیستند.",
                    Status = StatusCodes.Status400BadRequest,
                    Errors = errors,
                    TraceId = context.HttpContext.TraceIdentifier
                });
            };
        });

        return services;
    }

    /// <summary>«$.type» ← «type» و «$» ← «input».</summary>
    private static string CleanKey(string key)
    {
        if (key is "$" or "" ) return "input";
        var cleaned = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;
        return cleaned.Contains('.') ? cleaned[(cleaned.LastIndexOf('.') + 1)..] : cleaned;
    }
}
