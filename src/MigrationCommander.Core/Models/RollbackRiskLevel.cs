namespace MigrationCommander.Core.Models;

/// <summary>
/// Indicates the risk level of a rollback operation.
/// </summary>
public enum RollbackRiskLevel
{
    /// <summary>
    /// No data loss expected.
    /// </summary>
    Low,

    /// <summary>
    /// Changes are reversible with minimal impact.
    /// </summary>
    Medium,

    /// <summary>
    /// Potential data loss may occur.
    /// </summary>
    High,

    /// <summary>
    /// Definite data loss will occur.
    /// </summary>
    Critical
}
