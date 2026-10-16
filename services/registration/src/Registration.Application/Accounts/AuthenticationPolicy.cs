using System.Security.Cryptography;
using System.Text;

namespace Registration.Application.Accounts;

public sealed class AuthenticationPolicy
{
    private readonly byte[] _codeHashKey;

    public AuthenticationPolicy(byte[] codeHashKey)
    {
        if (codeHashKey.Length < 32)
            throw new ArgumentException("The email code HMAC key must contain at least 32 bytes.", nameof(codeHashKey));
        _codeHashKey = (byte[])codeHashKey.Clone();
    }

    public TimeSpan CodeLifetime => TimeSpan.FromMinutes(10);
    public TimeSpan ResendInterval => TimeSpan.FromMinutes(1);
    public int MaxFailedAttempts => 5;
    public TimeSpan SessionLifetime => TimeSpan.FromHours(12);

    public string HashCode(CorporateEmail email, EmailCodePurpose purpose, string code)
    {
        var data = Encoding.UTF8.GetBytes($"{purpose}:{email.Value}:{code}");
        return Convert.ToHexString(HMACSHA256.HashData(_codeHashKey, data));
    }

    public static string NewCode() => RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8");

    public static (string Token, string Hash) NewSessionToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (token, HashSessionToken(token));
    }

    public static string HashSessionToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token)));
}
