using Kurator.Core.Enums;

namespace Kurator.Core.Entities;

/// <summary>
/// Пользователь системы
/// </summary>
public class User
{
    public int Id { get; set; }

    /// <summary>
    /// Уникальный логин для входа
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Хеш пароля (BCrypt)
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Роль пользователя
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Флаг первого входа (true пока не настроена MFA)
    /// </summary>
    public bool IsFirstLogin { get; set; } = true;

    /// <summary>
    /// Публичный ключ RSA для шифрования данных (PEM формат)
    /// </summary>
    public string? PublicKey { get; set; }

    /// <summary>
    /// Секрет для двухфакторной аутентификации (TOTP)
    /// </summary>
    public string? MfaSecret { get; set; }

    /// <summary>
    /// Флаг активации MFA
    /// </summary>
    public bool MfaEnabled { get; set; } = false;

    /// <summary>
    /// Статус пользователя
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата последнего входа
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    /// <summary>
    /// Связь с блоками через таблицу BlockCurator
    /// </summary>
    public ICollection<BlockCurator> BlockAssignments { get; set; } = new List<BlockCurator>();

    public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
    public ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<Watchlist> WatchlistItems { get; set; } = new List<Watchlist>();
    public ICollection<InfluenceStatusHistory> InfluenceStatusChanges { get; set; } = new List<InfluenceStatusHistory>();
    public ICollection<WatchlistHistory> WatchlistChanges { get; set; } = new List<WatchlistHistory>();
}

