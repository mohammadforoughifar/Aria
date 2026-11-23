using System.Security.Cryptography;

namespace Northwind.Api.Services.Security;

public interface IPasswordHasher
{
    /// <summary>هشِ قابلِ ذخیره می‌سازد؛ قالب: iterations.salt.hash (همه Base64).</summary>
    string Hash(string password);

    /// <summary>رمز را با هشِ ذخیره‌شده مقایسه می‌کند — مقایسهٔ زمان‌ثابت.</summary>
    bool Verify(string password, string stored);
}

/// <summary>
/// PBKDF2 با SHA-256. عمداً از کتابخانهٔ Identity استفاده نشده تا وابستگی زیاد نشود؛
/// الگوریتم همان چیزی است که Identity هم به کار می‌برد (۱۰۰٬۰۰۰ دور).
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string stored)
    {
        var parts = stored.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;

        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
