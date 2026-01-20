namespace MigrationCommander.Core.Models;

/// <summary>
/// Represents the difference between two database schemas.
/// </summary>
public class SchemaDiff
{
    /// <summary>
    /// Tables that will be created.
    /// </summary>
    public List<TableDiff> TablesToCreate { get; set; } = new();

    /// <summary>
    /// Tables that will be dropped.
    /// </summary>
    public List<TableDiff> TablesToDrop { get; set; } = new();

    /// <summary>
    /// Tables that will be modified.
    /// </summary>
    public List<TableDiff> TablesToModify { get; set; } = new();

    /// <summary>
    /// Indexes that will be created.
    /// </summary>
    public List<IndexDiff> IndexesToCreate { get; set; } = new();

    /// <summary>
    /// Indexes that will be dropped.
    /// </summary>
    public List<IndexDiff> IndexesToDrop { get; set; } = new();

    /// <summary>
    /// Foreign keys that will be created.
    /// </summary>
    public List<ForeignKeyDiff> ForeignKeysToCreate { get; set; } = new();

    /// <summary>
    /// Foreign keys that will be dropped.
    /// </summary>
    public List<ForeignKeyDiff> ForeignKeysToDrop { get; set; } = new();

    /// <summary>
    /// Indicates if there are any changes.
    /// </summary>
    public bool HasChanges =>
        TablesToCreate.Count > 0 ||
        TablesToDrop.Count > 0 ||
        TablesToModify.Count > 0 ||
        IndexesToCreate.Count > 0 ||
        IndexesToDrop.Count > 0 ||
        ForeignKeysToCreate.Count > 0 ||
        ForeignKeysToDrop.Count > 0;
}

/// <summary>
/// Represents a table change in a schema diff.
/// </summary>
public class TableDiff
{
    public string TableName { get; set; } = string.Empty;
    public string? Schema { get; set; }
    public List<ColumnDiff> Columns { get; set; } = new();
}

/// <summary>
/// Represents a column change in a schema diff.
/// </summary>
public class ColumnDiff
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsPrimaryKey { get; set; }
    public ColumnChangeType ChangeType { get; set; }
}

/// <summary>
/// Type of column change.
/// </summary>
public enum ColumnChangeType
{
    Added,
    Removed,
    Modified
}

/// <summary>
/// Represents an index change in a schema diff.
/// </summary>
public class IndexDiff
{
    public string IndexName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public bool IsUnique { get; set; }
}

/// <summary>
/// Represents a foreign key change in a schema diff.
/// </summary>
public class ForeignKeyDiff
{
    public string ConstraintName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ReferencedTable { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public List<string> ReferencedColumns { get; set; } = new();
}
