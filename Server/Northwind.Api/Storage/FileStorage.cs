using Microsoft.AspNetCore.Hosting;
using Northwind.Core.Services.Common;

namespace Northwind.Api.Storage;

/* ======================= ذخیرهٔ فایل‌هایِ بارگذاری‌شده =======================
   یک جا، یک قاعده: هر فایلی که مدیر بارگذاری می‌کند در پوشهٔ
   <ریشهٔ برنامه>/wwwroot/files ذخیره می‌شود و از نشانیِ /files/<نام> خوانده
   می‌شود. نامِ فایل با GUID ساخته می‌شود تا نام‌هایِ تکراری/فارسیِ کاربر
   مشکلِ مسیر نسازد، اما پسوندِ اصلی حفظ می‌شود.                              */

public interface IFileStorage
{
    /// <summary>پوشهٔ فیزیکیِ فایل‌ها (همان که به /files سرو می‌شود).</summary>
    string RootPath { get; }

    Task<StoredFile> SaveAsync(Stream content, string originalName, string? contentType, long length, CancellationToken ct = default);
}

/// <summary>مشخصاتِ فایلِ ذخیره‌شده.</summary>
public sealed record StoredFile(string Url, string FileName, long SizeBytes, string ContentType);

public sealed class FileStorage : IFileStorage
{
    /// <summary>سقفِ حجمِ هر فایل — ۴ مگابایت برایِ تصویرِ شاخص کافی است.</summary>
    public const long MaxBytes = 4 * 1024 * 1024;

    /// <summary>نشانیِ عمومیِ پوشه.</summary>
    public const string RequestPath = "/files";

    // فقط تصویر؛ هر پسوندِ دیگری با پیامِ فارسی رد می‌شود.
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp",
        [".avif"] = "image/avif",
        [".gif"] = "image/gif"
    };

    private readonly string _root;

    public FileStorage(IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "wwwroot", "files");
        Directory.CreateDirectory(_root);
    }

    public string RootPath => _root;

    public async Task<StoredFile> SaveAsync(Stream content, string originalName, string? contentType, long length, CancellationToken ct = default)
    {
        if (length <= 0)
            throw new BadRequestException("فایل خالی است؛ یک تصویرِ دیگر انتخاب کنید.", "file_empty");

        if (length > MaxBytes)
            throw new BadRequestException(
                $"حجمِ فایل بیش از حدِ مجاز است (حداکثر {MaxBytes / (1024 * 1024)} مگابایت).",
                "file_too_large");

        var ext = Path.GetExtension(originalName);
        if (string.IsNullOrWhiteSpace(ext) || !Allowed.ContainsKey(ext))
            throw new BadRequestException(
                "فقط تصویر با پسوندِ png، jpg، jpeg، webp، avif یا gif پذیرفته می‌شود.",
                "file_type_not_allowed");

        var type = Allowed[ext];
        var name = $"{Guid.NewGuid():n}{ext.ToLowerInvariant()}";
        var path = Path.Combine(_root, name);

        await using (var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
        {
            await content.CopyToAsync(target, ct);
        }

        // اگر نوشتن نیمه‌کاره ماند، فایلِ ناقص رویِ دیسک نمی‌مانَد.
        var written = new FileInfo(path).Length;
        if (written == 0)
        {
            File.Delete(path);
            throw new BadRequestException("فایل ذخیره نشد؛ دوباره تلاش کنید.", "file_write_failed");
        }

        return new StoredFile($"{RequestPath}/{name}", name, written, type);
    }
}
