namespace Northwind.Data.Common;

/// <summary>
/// پایهٔ همهٔ موجودیت‌ها.
///
/// <para>
/// شناسه از نوعِ <see cref="Guid"/> است و با <c>Guid.CreateVersion7()</c> ساخته می‌شود.
/// نسخهٔ ۷ گواهِ زمانی است، پس مقادیرِ تازه همیشه بزرگ‌تر از قبلی‌ها هستند و درج در
/// ایندکسِ خوشه‌ایِ SQL Server پرشِ صفحه (page split) نمی‌سازد — همان مشکلی که
/// <c>NEWID()</c> دارد. اگر روزی درج از سمتِ پایگاه‌داده انجام شد،
/// <c>NEWSEQUENTIALID()</c> همین ویژگی را می‌دهد.
/// </para>
///
/// <para>
/// <c>RowVersion</c> توکنِ هم‌زمانیِ خوش‌بینانه است؛ فقط روی SQL Server فعال می‌شود
/// (SQLite نوعِ rowversion ندارد).
/// </para>
/// </summary>
public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>زمانِ ساخت — همیشه UTC.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>زمانِ آخرین تغییر — همیشه UTC.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>توکنِ هم‌زمانی — روی SQL Server ستونِ <c>rowversion</c> است.</summary>
    public byte[]? RowVersion { get; set; }
}
