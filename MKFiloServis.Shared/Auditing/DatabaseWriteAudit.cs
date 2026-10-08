using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace MKFiloServis.Shared.Auditing;

/// <summary>Database-side audit for SQL/EF/COPY writers, including writes without a firma scope.</summary>
public static class DatabaseWriteAudit
{
    public const string SqliteJournal = "__MKWriteJournal";
    public static string PostgreSqlInstallSql
    {
        get
        {
            using var stream = typeof(DatabaseWriteAudit).Assembly.GetManifestResourceStream("MKFiloServis.WriteAudit.sql")
                ?? throw new InvalidOperationException("Write audit installation resource missing.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    public static async Task EnsureAsync(DbConnection connection, DbTransaction? transaction = null)
    {
        var close = connection.State != ConnectionState.Open;
        if (close) await connection.OpenAsync();
        try
        {
            if (transaction != null) await InstallAsync(connection, transaction);
            else
            {
                await using var owned = await connection.BeginTransactionAsync();
                await InstallAsync(connection, owned);
                await owned.CommitAsync();
            }
        }
        finally { if (close) await connection.CloseAsync(); }
    }

    private static async Task InstallAsync(DbConnection connection, DbTransaction transaction)
    {
        if (connection.GetType().Name.Contains("Npgsql", StringComparison.Ordinal))
        {
            await ExecuteAsync(connection, transaction, PostgreSqlInstallSql);
            return;
        }
        if (!connection.GetType().Name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Write audit supports PostgreSQL and SQLite.");
        await ExecuteAsync(connection, transaction, """
            CREATE TABLE IF NOT EXISTS "__MKWriteJournal" (
                "Id" INTEGER PRIMARY KEY AUTOINCREMENT, "RecordedAt" TEXT NOT NULL,
                "TableName" TEXT NOT NULL, "Operation" TEXT NOT NULL, "FirmaId" TEXT,
                "EntityId" TEXT, "OldValues" TEXT, "NewValues" TEXT);
            CREATE TRIGGER IF NOT EXISTS "mk_audit_immutable_update" BEFORE UPDATE ON "__MKWriteJournal"
                BEGIN SELECT RAISE(ABORT,'MKFiloServis write journal is append-only'); END;
            CREATE TRIGGER IF NOT EXISTS "mk_audit_immutable_delete" BEFORE DELETE ON "__MKWriteJournal"
                BEGIN SELECT RAISE(ABORT,'MKFiloServis write journal is append-only'); END;
            """);
        var tables = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EFMigrations%' AND name <> '__MKWriteJournal' ORDER BY name";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        }
        foreach (var table in tables)
        {
            var columns = new List<string>();
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = $"PRAGMA table_info({Quote(table)})";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync()) columns.Add(reader.GetString(1));
            }
            string Value(string prefix, string name) => columns.Contains(name) ? $"CAST({prefix}.{Quote(name)} AS TEXT)" : "NULL";
            string Document(string prefix) => "json_object(" + string.Join(",", columns.Select(column =>
                Literal(column) + "," + (IsSensitiveProperty(column) ? "'[GIZLENDI]'" : $"CASE WHEN typeof({prefix}.{Quote(column)})='blob' THEN '[BINARY]' ELSE {prefix}.{Quote(column)} END"))) + ")";
            foreach (var operation in new[] { "INSERT", "UPDATE", "DELETE" })
            {
                var prefix = operation == "DELETE" ? "OLD" : "NEW";
                var firma = columns.Contains("FirmaId") ? Value(prefix,"FirmaId") : Value(prefix,"IsverenFirmaId");
                var trigger = Quote("mk_audit_" + operation.ToLowerInvariant() + "_" + table);
                // Recreate after schema sync so newly added columns are audited too.
                await ExecuteAsync(connection, transaction, $"""
                    DROP TRIGGER IF EXISTS {trigger};
                    CREATE TRIGGER {trigger} AFTER {operation} ON {Quote(table)} BEGIN
                      INSERT INTO "__MKWriteJournal"("RecordedAt","TableName","Operation","FirmaId","EntityId","OldValues","NewValues")
                      VALUES(strftime('%Y-%m-%dT%H:%M:%fZ','now'),{Literal(table)},{Literal(operation)},{firma},{Value(prefix,"Id")},
                        {(operation == "INSERT" ? "NULL" : Document("OLD"))},{(operation == "DELETE" ? "NULL" : Document("NEW"))});
                    END;
                    """);
            }
        }
    }

    public static bool IsSensitiveProperty(string name) => Regex.IsMatch(name,
        "password|sifre|parola|token|secret|connectionstring|key|lisansanahtar|imza|deger|value|payload|body|icerik|dosya|credential|pwd|pass|salt", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    private static string Literal(string value) => "'" + value.Replace("'", "''") + "'";
    private static async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, string sql)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction; command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
