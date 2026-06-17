using Microsoft.Data.Sqlite;
using KOAFiloServis.LisansDesktop.Models;

namespace KOAFiloServis.LisansDesktop.Data;

/// <summary>
/// SQLite veritabani islemleri.
/// Tek EXE icinde calisir, external dependency yok.
/// </summary>
public class LicenseDb : IDisposable
{
    private readonly SqliteConnection _conn;

    public LicenseDb(string dbPath)
    {
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        InitTable();
    }

    private void InitTable()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Licenses (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                FirmaKodu       TEXT NOT NULL,
                FirmaAdi        TEXT,
                MachineId       TEXT NOT NULL,
                ExpireDate      TEXT NOT NULL,
                AllowedVersion  TEXT DEFAULT '1.0.99',
                IsDemo          INTEGER DEFAULT 0,
                CreatedAt       TEXT NOT NULL,
                Signature       TEXT NOT NULL,
                LisansTipi      TEXT DEFAULT 'Standard',
                MaxKullanici    INTEGER DEFAULT 10,
                YetkiliKisi     TEXT,
                Email           TEXT,
                Notlar          TEXT,
                KayitTarihi     TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();
    }

    public void Insert(LicenseRecord lic)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Licenses
                (FirmaKodu, FirmaAdi, MachineId, ExpireDate, AllowedVersion,
                 IsDemo, CreatedAt, Signature, LisansTipi, MaxKullanici,
                 YetkiliKisi, Email, Notlar, KayitTarihi)
            VALUES
                (@fk, @fa, @mk, @ed, @av,
                 @id, @ca, @sg, @lt, @mx,
                 @yk, @em, @nt, @kt)
            """;
        cmd.Parameters.AddWithValue("@fk", lic.FirmaKodu);
        cmd.Parameters.AddWithValue("@fa", lic.FirmaAdi);
        cmd.Parameters.AddWithValue("@mk", lic.MachineId);
        cmd.Parameters.AddWithValue("@ed", lic.ExpireDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@av", lic.AllowedVersion);
        cmd.Parameters.AddWithValue("@id", lic.IsDemo ? 1 : 0);
        cmd.Parameters.AddWithValue("@ca", lic.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
        cmd.Parameters.AddWithValue("@sg", lic.Signature);
        cmd.Parameters.AddWithValue("@lt", lic.LisansTipi);
        cmd.Parameters.AddWithValue("@mx", lic.MaxKullanici);
        cmd.Parameters.AddWithValue("@yk", lic.YetkiliKisi ?? "");
        cmd.Parameters.AddWithValue("@em", lic.Email ?? "");
        cmd.Parameters.AddWithValue("@nt", lic.Notlar ?? "");
        cmd.Parameters.AddWithValue("@kt", lic.KayitTarihi);
        cmd.ExecuteNonQuery();
    }

    public List<LicenseRecord> GetAll(string? search = null, int limit = 100)
    {
        var list = new List<LicenseRecord>();
        var where = "1=1";
        if (!string.IsNullOrWhiteSpace(search))
            where = "(FirmaKodu LIKE @s OR FirmaAdi LIKE @s OR MachineId LIKE @s)";

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = $"SELECT * FROM Licenses WHERE {where} ORDER BY Id DESC LIMIT {limit}";
        if (!string.IsNullOrWhiteSpace(search))
            cmd.Parameters.AddWithValue("@s", $"%{search}%");

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            DateTime.TryParse(reader["ExpireDate"]?.ToString(), out var exp);
            DateTime.TryParse(reader["CreatedAt"]?.ToString(), out var created);

            list.Add(new LicenseRecord
            {
                Id = Convert.ToInt32(reader["Id"]),
                FirmaKodu = reader["FirmaKodu"]?.ToString() ?? "",
                FirmaAdi = reader["FirmaAdi"]?.ToString() ?? "",
                MachineId = reader["MachineId"]?.ToString() ?? "",
                ExpireDate = exp,
                AllowedVersion = reader["AllowedVersion"]?.ToString() ?? "1.0.99",
                IsDemo = Convert.ToInt32(reader["IsDemo"]) == 1,
                CreatedAt = created,
                Signature = reader["Signature"]?.ToString() ?? "",
                LisansTipi = reader["LisansTipi"]?.ToString() ?? "",
                MaxKullanici = Convert.ToInt32(reader["MaxKullanici"]),
                YetkiliKisi = reader["YetkiliKisi"]?.ToString(),
                Email = reader["Email"]?.ToString(),
                Notlar = reader["Notlar"]?.ToString(),
                KayitTarihi = reader["KayitTarihi"]?.ToString() ?? ""
            });
        }
        return list;
    }

    public LicenseRecord? GetById(int id)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Licenses WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        DateTime.TryParse(reader["ExpireDate"]?.ToString(), out var exp);
        DateTime.TryParse(reader["CreatedAt"]?.ToString(), out var created);

        return new LicenseRecord
        {
            Id = Convert.ToInt32(reader["Id"]),
            FirmaKodu = reader["FirmaKodu"]?.ToString() ?? "",
            FirmaAdi = reader["FirmaAdi"]?.ToString() ?? "",
            MachineId = reader["MachineId"]?.ToString() ?? "",
            ExpireDate = exp,
            AllowedVersion = reader["AllowedVersion"]?.ToString() ?? "1.0.99",
            IsDemo = Convert.ToInt32(reader["IsDemo"]) == 1,
            CreatedAt = created,
            Signature = reader["Signature"]?.ToString() ?? "",
            LisansTipi = reader["LisansTipi"]?.ToString() ?? "",
            MaxKullanici = Convert.ToInt32(reader["MaxKullanici"]),
            YetkiliKisi = reader["YetkiliKisi"]?.ToString(),
            Email = reader["Email"]?.ToString(),
            Notlar = reader["Notlar"]?.ToString(),
            KayitTarihi = reader["KayitTarihi"]?.ToString() ?? ""
        };
    }

    public List<LicenseRecord> ExportAll()
    {
        return GetAll(null, int.MaxValue);
    }

    public void Dispose() => _conn?.Dispose();
}
