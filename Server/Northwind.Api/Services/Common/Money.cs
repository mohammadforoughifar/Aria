namespace Northwind.Api.Services.Common;

/// <summary>
/// گرد کردنِ مبالغ در یک جا.
///
/// مبالغ از نوعِ <c>decimal</c> هستند تا سقفِ int (۲٫۱ میلیارد) مانع نشود.
/// برایِ تومان دو رقمِ اعشار نگه داشته می‌شود تا محاسبهٔ درصدی و مالیات دقیق بماند
/// و در پایان به روشِ «نیم به بالا» گرد می‌شود.
/// </summary>
public static class Money
{
    public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
