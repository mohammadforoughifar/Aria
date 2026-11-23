// پیکربندیِ دیاگرامِ اکوسیستم — پیش‌بارگیری‌شده در آغازِ برنامه.
//
// چرا؟
//   اگر مؤلفه خودش در OnInitializedAsync فایلِ JSON را بخواند، نخستین
//   رندر با دادهٔ تهی انجام می‌شود: صحنه‌ای بدون کارت. وقتی داده می‌رسد،
//   ده کارت ناگهان سبز می‌شوند و کلِ صفحه با شدتِ زیاد جابه‌جا می‌شود
//   (جابه‌جاییِ چیدمان). خواندنِ فایل پیش از نخستین رندر این پرش را از
//   بین می‌برد: مؤلفه همان بارِ اول، کامل رسم می‌شود.

using System.Text.Json;
using Northwind.Client.Models;

namespace Northwind.Client.Services;

public sealed class EcosystemData
{
    public EcosystemConfig Config { get; private set; } = new();

    public void Set(EcosystemConfig cfg)
    {
        Config = cfg ?? new EcosystemConfig();
        foreach (var p in Config.People) p.IsPerson = true;
    }

    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
