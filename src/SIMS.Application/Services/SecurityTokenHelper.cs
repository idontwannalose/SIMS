using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SIMS.Domain.Entities;
using SIMS.Domain.Enums;

namespace SIMS.Application.Services;

/// <summary>
/// Provides cryptographic SHA-256 password hashing, strong password policy validation,
/// and RFC 7519 JWT (JSON Web Token - HMAC-SHA256) generation and verification.
/// </summary>
public static class SecurityTokenHelper
{
    public const string DefaultUniversityPassword = "Btec@123";
    private const string JwtSalt = "SIMS_BTEC_UNIT20_APDP_SALT_2025";
    private static readonly byte[] JwtSecretKey = Encoding.UTF8.GetBytes("SIMS_BTEC_PEARSON_UNIT20_APDP_JWT_HMAC_SHA256_SECRET_KEY_2025!");

    private static readonly Regex UppercaseRegex = new(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex LowercaseRegex = new(@"[a-z]", RegexOptions.Compiled);
    private static readonly Regex DigitRegex = new(@"[0-9]", RegexOptions.Compiled);
    private static readonly Regex SpecialCharRegex = new(@"[^a-zA-Z0-9]", RegexOptions.Compiled);

    /// <summary>
    /// Computes a salted SHA-256 hash of a plaintext password so raw passwords are never stored in CSV files.
    /// </summary>
    public static string HashPassword(string plainTextPassword)
    {
        if (string.IsNullOrEmpty(plainTextPassword))
        {
            throw new ArgumentException("Password cannot be empty.", nameof(plainTextPassword));
        }

        byte[] inputBytes = Encoding.UTF8.GetBytes($"{JwtSalt}:{plainTextPassword}");
        byte[] hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes);
    }

    public static bool VerifyPassword(string plainTextPassword, string storedHash)
    {
        if (string.IsNullOrEmpty(plainTextPassword)) return false;

        // If a legacy CSV record had no stored hash yet, compare against the default password hash
        string effectiveStoredHash = string.IsNullOrWhiteSpace(storedHash)
            ? HashPassword(DefaultUniversityPassword)
            : storedHash.Trim();

        string computedHash = HashPassword(plainTextPassword);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash.ToUpperInvariant()),
            Encoding.UTF8.GetBytes(effectiveStoredHash.ToUpperInvariant()));
    }

    /// <summary>
    /// Validates password strength rules when changing password.
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateNewPassword(
        string currentPassword,
        string newPassword,
        string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            return (false, "Current password is required.");
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return (false, "New password is required.");
        }

        if (newPassword.Length < 8)
        {
            return (false, "New password must be at least 8 characters long.");
        }

        if (!UppercaseRegex.IsMatch(newPassword))
        {
            return (false, "New password must contain at least one uppercase letter (A-Z).");
        }

        if (!LowercaseRegex.IsMatch(newPassword))
        {
            return (false, "New password must contain at least one lowercase letter (a-z).");
        }

        if (!DigitRegex.IsMatch(newPassword))
        {
            return (false, "New password must contain at least one digit (0-9).");
        }

        if (!SpecialCharRegex.IsMatch(newPassword))
        {
            return (false, "New password must contain at least one special character (e.g., @, #, $, !, %).");
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            return (false, "Password confirmation does not match the new password.");
        }

        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
        {
            return (false, "New password must be different from your current password.");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Generates a signed JWT (JSON Web Token) using HMAC-SHA256 (HS256) containing user identity &amp; role claims.
    /// </summary>
    public static string GenerateJwtToken(User user)
    {
        var headerJson = JsonSerializer.Serialize(new { alg = "HS256", typ = "JWT" });
        var payloadJson = JsonSerializer.Serialize(new JwtPayloadData
        {
            Sub = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString(),
            MustChangePassword = user.MustChangePassword,
            Exp = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds()
        });

        string encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        string encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        string unsignedToken = $"{encodedHeader}.{encodedPayload}";

        string signature = ComputeHmacSha256Signature(unsignedToken);
        return $"{unsignedToken}.{signature}";
    }

    /// <summary>
    /// Verifies the HMAC-SHA256 signature and expiration of a JWT token and extracts its claims.
    /// </summary>
    public static JwtPayloadData? ValidateAndDecodeJwtToken(string? jwtToken)
    {
        if (string.IsNullOrWhiteSpace(jwtToken)) return null;

        string[] parts = jwtToken.Split('.');
        if (parts.Length != 3) return null;

        string unsignedToken = $"{parts[0]}.{parts[1]}";
        string expectedSignature = ComputeHmacSha256Signature(unsignedToken);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(parts[2]),
                Encoding.UTF8.GetBytes(expectedSignature)))
        {
            return null;
        }

        try
        {
            byte[] payloadBytes = Base64UrlDecode(parts[1]);
            var payload = JsonSerializer.Deserialize<JwtPayloadData>(payloadBytes);
            if (payload == null) return null;

            if (payload.Exp < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return null; // Token expired
            }

            return payload;
        }
        catch
        {
            return null;
        }
    }

    private static string ComputeHmacSha256Signature(string input)
    {
        using var hmac = new HMACSHA256(JwtSecretKey);
        byte[] signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Base64UrlEncode(signatureBytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }
}

public class JwtPayloadData
{
    public string Sub { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public long Exp { get; set; }
}
