using Northwind.Domain.Common;
using Northwind.Domain.Entities.Catalog;

namespace Northwind.Domain.Entities.Cart;

/// <summary>
/// یک ردیفِ سبدِ خرید. سبد برایِ کاربرِ مهمان با <see cref="SessionKey"/> و برایِ
/// کاربرِ واردشده با <see cref="UserId"/> شناخته می‌شود.
/// </summary>
public class CartItem : EntityBase
{
    public string SessionKey { get; set; } = "";
    public Guid? UserId { get; set; }

    public Guid PlanId { get; set; }
    public Plan? Plan { get; set; }

    public int Quantity { get; set; } = 1;
}
