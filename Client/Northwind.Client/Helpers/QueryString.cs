// تجزیهٔ سبکِ رشتهٔ پرس‌وجو (بدون نیاز به Microsoft.AspNetCore.WebUtilities در WASM)

namespace Northwind.Client.Helpers;

public static class QueryString
{
    public static IReadOnlyDictionary<string, string> Parse(string uri)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var idx = uri.IndexOf('?');
        if (idx < 0) return result;

        foreach (var pair in uri[(idx + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq <= 0) continue;
            var key = Uri.UnescapeDataString(pair[..eq].Replace('+', ' '));
            var value = Uri.UnescapeDataString(pair[(eq + 1)..].Replace('+', ' '));
            result[key] = value;
        }
        return result;
    }

    public static string? Get(string uri, string key) =>
        Parse(uri).TryGetValue(key, out var v) ? v : null;
}
