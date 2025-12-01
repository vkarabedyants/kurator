using System.Collections.Generic;

namespace Kurator.Core.DTOs;

/// <summary>
/// Represents an encrypted field with keys for multiple recipients
/// </summary>
public class EncryptedFieldDto
{
    /// <summary>
    /// AES-256 encrypted data (base64)
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Initialization vector for AES encryption (base64)
    /// </summary>
    public string Iv { get; set; } = string.Empty;

    /// <summary>
    /// RSA-encrypted AES keys for each recipient
    /// </summary>
    public List<EncryptedKeyDto> Keys { get; set; } = new List<EncryptedKeyDto>();
}

/// <summary>
/// Represents an encrypted AES key for a specific user
/// </summary>
public class EncryptedKeyDto
{
    /// <summary>
    /// User ID of the recipient
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// RSA-encrypted AES key (base64)
    /// </summary>
    public string EncryptedKey { get; set; } = string.Empty;
}