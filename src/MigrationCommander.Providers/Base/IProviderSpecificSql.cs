namespace MigrationCommander.Providers.Base;

/// <summary>
/// Interface for provider-specific SQL generation.
/// </summary>
public interface IProviderSpecificSql
{
    /// <summary>
    /// Gets the SQL to retrieve table row count.
    /// </summary>
    string GetRowCountSql(string tableName);

    /// <summary>
    /// Gets the SQL to check if a table exists.
    /// </summary>
    string GetTableExistsSql(string tableName);

    /// <summary>
    /// Gets the SQL to retrieve column information for a table.
    /// </summary>
    string GetColumnInfoSql(string tableName);

    /// <summary>
    /// Gets the SQL to retrieve index information for a table.
    /// </summary>
    string GetIndexInfoSql(string tableName);

    /// <summary>
    /// Gets the SQL to retrieve foreign key information for a table.
    /// </summary>
    string GetForeignKeyInfoSql(string tableName);

    /// <summary>
    /// Escapes an identifier (table name, column name, etc.) for use in SQL.
    /// </summary>
    string EscapeIdentifier(string identifier);

    /// <summary>
    /// Gets the SQL comment prefix for this provider.
    /// </summary>
    string CommentPrefix { get; }
}
