namespace Kurator.Core.Enums;

public enum UserRole
{
    Admin,
    Curator,
    ThreatAnalyst
}

public enum BlockStatus
{
    Active,
    Archived
}

public enum CuratorType
{
    Primary,
    Backup
}

public enum AuditActionType
{
    Create,
    Update,
    Delete,
    StatusChange
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum MonitoringFrequency
{
    Weekly,
    Monthly,
    Quarterly,
    AdHoc
}
