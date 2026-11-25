using Microsoft.AspNetCore.Http;
namespace Northwind.Core.Services.Common;

/// <summary>پایهٔ خطاهایِ دامنه — میان‌افزار آن‌ها را به پاسخِ HTTP مناسب تبدیل می‌کند.</summary>
public abstract class AppException : Exception
{
    protected AppException(string code, string message) : base(message) => Code = code;

    /// <summary>کدِ ماشین‌خوان برایِ کلاینت.</summary>
    public string Code { get; }

    /// <summary>کدِ وضعیتِ HTTP متناظر.</summary>
    public abstract int StatusCode { get; }
}

/// <summary>۴۰۴ — چیزی که خواسته شده وجود ندارد.</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string what, object? key = null)
        : base("not_found", key is null ? $"{what} پیدا نشد." : $"{what} با شناسهٔ «{key}» پیدا نشد.") { }

    public override int StatusCode => StatusCodes.Status404NotFound;
}

/// <summary>۴۰۹ — با وضعیتِ فعلیِ داده جور درنمی‌آید.</summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message, string code = "conflict") : base(code, message) { }
    public override int StatusCode => StatusCodes.Status409Conflict;
}

/// <summary>۴۰۳ — کاربر شناخته شده اما اجازه ندارد.</summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "به این بخش دسترسی ندارید.")
        : base("forbidden", message) { }
    public override int StatusCode => StatusCodes.Status403Forbidden;
}

/// <summary>۴۰۰ — ورودی از نظرِ منطقی معتبر نیست (فراتر از اعتبارسنجیِ اتریبیوت‌ها).</summary>
public sealed class BadRequestException : AppException
{
    public BadRequestException(string message, string code = "bad_request") : base(code, message) { }
    public override int StatusCode => StatusCodes.Status400BadRequest;
}

/// <summary>۴۲۲ — قاعدهٔ کسب‌وکار نقض شده، مثلِ «کدِ تخفیف منقضی است».</summary>
public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string message, string code = "business_rule") : base(code, message) { }
    public override int StatusCode => StatusCodes.Status422UnprocessableEntity;
}
