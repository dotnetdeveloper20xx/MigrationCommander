namespace MigrationCommander.Core.Models;

/// <summary>
/// Represents the impact of a rollback operation on a specific table.
/// </summary>
public class TableImpact
{
    /// <summary>
    /// Name of the affected table.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// The action being performed (e.g., "DROP", "ALTER", "TRUNCATE").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Current row count in the table.
    /// </summary>
    public long CurrentRowCount { get; set; }

    /// <summary>
    /// Number of rows that will be deleted by the operation.
    /// </summary>
    public long RowsToBeDeleted { get; set; }

    /// <summary>
    /// List of columns affected by the operation.
    /// </summary>
    public List<string> AffectedColumns { get; set; } = new();

    /// <summary>
    /// Indicates if the entire table will be dropped.
    /// </summary>
    public bool WillDropTable { get; set; }
}
