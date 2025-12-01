using OtpNet;
using System;
using System.Text;

namespace Kurator.Infrastructure.Services;

/// <summary>
/// Service for handling TOTP (Time-based One-Time Password) authentication
/// </summary>
public class TotpService
{
    private const int StepSizeInSeconds = 30;
    private const int CodeDigits = 6;
    private const string Issuer = "KURATOR";

    /// <summary>
    /// Generate a new TOTP secret
    /// </summary>
    public string GenerateSecret()
    {
        // Generate a random 20-byte secret
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    /// <summary>
    /// Generate a QR code URI for authenticator apps
    /// </summary>
    public string GenerateQrCodeUri(string secret, string userLogin)
    {
        var encodedIssuer = Uri.EscapeDataString(Issuer);
        var encodedUser = Uri.EscapeDataString(userLogin);
        return $"otpauth://totp/{encodedIssuer}:{encodedUser}?secret={secret}&issuer={encodedIssuer}&digits={CodeDigits}&period={StepSizeInSeconds}";
    }

    /// <summary>
    /// Verify a TOTP code
    /// </summary>
    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        // Remove any spaces from the code
        code = code.Replace(" ", "").Trim();

        // Check if code is 6 digits
        if (code.Length != CodeDigits || !int.TryParse(code, out _))
        {
            return false;
        }

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes, StepSizeInSeconds, OtpHashMode.Sha1, CodeDigits);

            // Verify with time window of ±1 step (allows for time drift)
            var verificationWindow = new VerificationWindow(1, 1);
            return totp.VerifyTotp(code, out _, verificationWindow);
        }
        catch
        {
            // Invalid secret format or other error
            return false;
        }
    }

    /// <summary>
    /// Generate current TOTP code (for testing purposes)
    /// </summary>
    public string GenerateCode(string secret)
    {
        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes, StepSizeInSeconds, OtpHashMode.Sha1, CodeDigits);
            return totp.ComputeTotp();
        }
        catch
        {
            return string.Empty;
        }
    }
}