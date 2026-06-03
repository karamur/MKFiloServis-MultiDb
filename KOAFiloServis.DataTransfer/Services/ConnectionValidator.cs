using Npgsql;

namespace KOAFiloServis.DataTransfer.Services;

public class ConnectionValidator
{
    public static async Task<(bool Success, string Message)> TestAsync(Models.ConnectionInfo info)
    {
        try
        {
            var connStr = info.BuildConnectionString();
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT version();", conn);
            var version = (string?)await cmd.ExecuteScalarAsync();
            return (true, $"Bağlantı başarılı. {version}");
        }
        catch (Exception ex)
        {
            return (false, $"Bağlantı hatası: {ex.Message}");
        }
    }
}
