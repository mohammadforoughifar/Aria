using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Northwind.Api.Storage;
using Northwind.Core.Models.Common;
using Northwind.Core.Services.Common;

namespace Northwind.Api.Controllers;

/* ============================ بارگذاریِ فایل ============================
   مدیر از فرمِ محصول (و هر فرمی که فایل بخواهد) فایل را از سیستمِ خودش
   انتخاب می‌کند و همین‌جا بارگذاری می‌شود. فایل رویِ دیسک در پوشهٔ
   wwwroot/files می‌نشیند و از همان مسیر با نشانیِ /files/<name> خوانده
   می‌شود؛ در پایگاه‌داده فقط همان نشانی ذخیره می‌شود.                        */

[ApiController]
[Route("api/v1/admin/files")]
[Tags("فایل‌ها")]
[Authorize(Roles = "Admin")]
public sealed class FilesController : ControllerBase
{
    private readonly IFileStorage _storage;

    public FilesController(IFileStorage storage) => _storage = storage;

    /// <summary>بارگذاریِ یک فایل و برگرداندنِ نشانیِ آماده برایِ ذخیره در محصول.</summary>
    [HttpPost]
    [RequestSizeLimit(FileStorage.MaxBytes + 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<FileUploadResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status400BadRequest)]
    public async Task<FileUploadResult> Upload(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new BadRequestException("فایلی انتخاب نشده است. یک تصویر انتخاب و دوباره تلاش کنید.",
                "file_missing");

        await using var stream = file.OpenReadStream();
        var saved = await _storage.SaveAsync(stream, file.FileName, file.ContentType, file.Length, ct);

        return new FileUploadResult
        {
            Url = saved.Url,
            FileName = saved.FileName,
            SizeBytes = saved.SizeBytes,
            ContentType = saved.ContentType
        };
    }
}

/// <summary>پاسخِ بارگذاری — نشانیِ عمومیِ فایل برایِ ذخیره در رکورد.</summary>
public sealed record FileUploadResult
{
    /// <summary>نشانیِ نسبی، مثلِ <c>/files/2f1c….png</c>.</summary>
    public string Url { get; init; } = "";

    /// <summary>نامِ فایلی که ذخیره شده (بدونِ مسیر).</summary>
    public string FileName { get; init; } = "";

    public long SizeBytes { get; init; }

    public string ContentType { get; init; } = "";
}
