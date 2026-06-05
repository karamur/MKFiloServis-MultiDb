using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// DestekCRMServisBlazorDb → KOAFiloServis veri aktarım servisi.
/// Talimat Bölüm 16-23: Legacy DB READ ONLY, FirmaId=1 varsayılan.
/// </summary>
public class LegacyDataTransferService
{
    private readonly string _sourceConnStr;
    private readonly string _targetConnStr;
    private readonly ILogger<LegacyDataTransferService> _logger;

    public LegacyDataTransferService(
        IConfiguration configuration,
        ILogger<LegacyDataTransferService> logger)
    {
        _targetConnStr = configuration.GetConnectionString("DefaultConnection")!;
        _sourceConnStr = _targetConnStr.Replace("Database=KOAFiloServis", "Database=DestekCRMServisBlazorDb");
        _logger = logger;
    }

    /// <summary>
    /// KOAFiloServis veritabanını mevcut model snapshot'ından oluşturur (EnsureCreated).
    /// Migration zincirini bypass eder.
    /// </summary>
    public async Task EnsureSchemaAsync()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(_targetConnStr);
        using var ctx = new ApplicationDbContext(optionsBuilder.Options);

        _logger.LogInformation("EnsureCreated basliyor...");
        await ctx.Database.EnsureCreatedAsync();
        _logger.LogInformation("EnsureCreated tamamlandi.");
    }

    /// <summary>
    /// Tüm verileri sırayla aktarır (Talimat Bölüm 21).
    /// </summary>
    public async Task<TransferResult> TransferAllAsync()
    {
        var result = new TransferResult();
        var firmaIdVarsayilan = 1;

        _logger.LogInformation("=== Veri aktarimi basladi ===");

        // Her adım bağımsız — birindeki hata diğerini etkilemez
        async Task<TransferResult> SafeTransferAsync(Func<Task<TransferResult>> transfer, string name)
        {
            try { return await transfer(); }
            catch (PostgresException ex) when (ex.SqlState == "42P01")
            { _logger.LogWarning("{Name}: kaynak tablo yok, atlandi ({Msg})", name, ex.MessageText); return new(); }
            catch (Exception ex)
            { _logger.LogWarning(ex, "{Name}: aktarim hatasi", name); return new(); }
        }

        // 1. Organizasyonlar
        result.Add(await SafeTransferAsync(TransferOrganizasyonlarAsync, "Organizasyonlar"));

        // 2. Firmalar
        result.Add(await SafeTransferAsync(TransferFirmalarAsync, "Firmalar"));

        // 3. Kullanicilar ve Roller (bağımlılık: Rol önce)
        result.Add(await SafeTransferAsync(TransferRollerAsync, "Roller"));
        result.Add(await SafeTransferAsync(TransferKullanicilarAsync, "Kullanicilar"));
        result.Add(await SafeTransferAsync(TransferRolYetkileriAsync, "RolYetkileri"));

        // 4. Cariler
        result.Add(await SafeTransferAsync(() => TransferCarilerAsync(firmaIdVarsayilan), "Cariler"));

        // 5. Araclar
        result.Add(await SafeTransferAsync(() => TransferAraclarAsync(firmaIdVarsayilan), "Araclar"));

        // 6. Soforler (Personel)
        result.Add(await SafeTransferAsync(() => TransferSoforlerAsync(firmaIdVarsayilan), "Soforler"));

        // 7. Kurumlar
        result.Add(await SafeTransferAsync(() => TransferKurumlarAsync(firmaIdVarsayilan), "Kurumlar"));

        // 8. Guzergahlar
        result.Add(await SafeTransferAsync(() => TransferGuzergahlarAsync(firmaIdVarsayilan), "Guzergahlar"));

        // 9. Faturalar
        result.Add(await SafeTransferAsync(() => TransferFaturalarAsync(firmaIdVarsayilan), "Faturalar"));

        // 10. PuantajKayitlar
        result.Add(await SafeTransferAsync(() => TransferPuantajKayitlarAsync(firmaIdVarsayilan), "PuantajKayitlar"));

        // 11. Bütçe
        result.Add(await SafeTransferAsync(() => TransferBudgetOdemelerAsync(firmaIdVarsayilan), "BudgetOdemeler"));
        result.Add(await SafeTransferAsync(() => TransferBudgetHedeflerAsync(firmaIdVarsayilan), "BudgetHedefler"));
        result.Add(await SafeTransferAsync(() => TransferBudgetMasrafKalemleriAsync(firmaIdVarsayilan), "BudgetMasrafKalemleri"));

        // 12. Banka/Kasa
        result.Add(await SafeTransferAsync(() => TransferBankaHesaplariAsync(firmaIdVarsayilan), "BankaHesaplari"));
        result.Add(await SafeTransferAsync(() => TransferBankaKasaHareketleriAsync(firmaIdVarsayilan), "BankaKasaHareketleri"));

        // 13. Stok
        result.Add(await SafeTransferAsync(() => TransferStokKartlariAsync(firmaIdVarsayilan), "StokKartlari"));

        // 14. Hakedis
        result.Add(await SafeTransferAsync(() => TransferHakedislerAsync(firmaIdVarsayilan), "Hakedisler"));

        // 15. Diğer
        result.Add(await SafeTransferAsync(() => TransferBordrolarAsync(firmaIdVarsayilan), "Bordrolar"));
        result.Add(await SafeTransferAsync(() => TransferAracMasraflariAsync(firmaIdVarsayilan), "AracMasraflari"));
        result.Add(await SafeTransferAsync(() => TransferPersonelMaaslariAsync(firmaIdVarsayilan), "PersonelMaaslari"));
        result.Add(await SafeTransferAsync(() => TransferPersonelIzinleriAsync(firmaIdVarsayilan), "PersonelIzinleri"));
        result.Add(await SafeTransferAsync(() => TransferOperasyonKayitlariAsync(firmaIdVarsayilan), "OperasyonKayitlari"));

        _logger.LogInformation("=== Veri aktarimi tamamlandi: {Total} kayit ===", result.TotalTransferred);
        return result;
    }

    // ── Tablo bazlı transfer metodları ───────────────────────────────

    private async Task<TransferResult> TransferOrganizasyonlarAsync()
    {
        var result = new TransferResult();
        using var source = await OpenSourceAsync();
        using var target = await OpenTargetAsync();

        using var cmd = new NpgsqlCommand(
            @"SELECT ""Id"", ""Ad"", ""Kod"", ""Aciklama"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""
              FROM ""Organizasyonlar""", source);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            await InsertIfNotExistsAsync(target,
                @"INSERT INTO ""Organizasyonlar"" (""Id"", ""Ad"", ""Kod"", ""Aciklama"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt"")
                  VALUES (@id, @ad, @kod, @aciklama, @isDeleted, @createdAt, @updatedAt)
                  ON CONFLICT (""Id"") DO NOTHING",
                new NpgsqlParameter("@id", reader.GetInt32(0)),
                new NpgsqlParameter("@ad", reader.GetString(1)),
                new NpgsqlParameter("@kod", reader.IsDBNull(2) ? DBNull.Value : reader.GetString(2)),
                new NpgsqlParameter("@aciklama", reader.IsDBNull(3) ? DBNull.Value : reader.GetString(3)),
                new NpgsqlParameter("@isDeleted", reader.GetBoolean(4)),
                new NpgsqlParameter("@createdAt", reader.GetDateTime(5)),
                new NpgsqlParameter("@updatedAt", reader.IsDBNull(6) ? DBNull.Value : reader.GetDateTime(6)));
            result.Transferred++;
        }
        _logger.LogInformation("Organizasyonlar: {Count} kayit", result.Transferred);
        return result;
    }

    private async Task<TransferResult> TransferFirmalarAsync()
    {
        var result = new TransferResult();
        using var source = await OpenSourceAsync();
        using var target = await OpenTargetAsync();

        using var cmd = new NpgsqlCommand(
            @"SELECT ""Id"", ""FirmaKodu"", ""FirmaAdi"", ""UnvanTam"", ""VergiNo"", ""VergiDairesi"",
                     ""Aktif"", ""AktifDonemYil"", ""AktifDonemAy"", ""OrganizasyonId"",
                     ""IsDeleted"", ""CreatedAt"", ""UpdatedAt"", ""Telefon"", ""Email"", ""Adres""
              FROM ""Firmalar""", source);
        using var reader = await cmd.ExecuteReaderAsync();

        var colMap = new Dictionary<string, int>();
        for (int i = 0; i < reader.FieldCount; i++)
            colMap[reader.GetName(i)] = i;

        while (await reader.ReadAsync())
        {
            var id = reader.GetInt32(colMap["Id"]);
            var firmaKodu = SafeGetString(reader, colMap, "FirmaKodu") ?? "";
            var firmaAdi = SafeGetString(reader, colMap, "FirmaAdi") ?? "";
            var unvanTam = SafeGetString(reader, colMap, "UnvanTam");
            var vergiNo = SafeGetString(reader, colMap, "VergiNo");
            var vergiDairesi = SafeGetString(reader, colMap, "VergiDairesi");
            var aktif = SafeGetBool(reader, colMap, "Aktif", true);
            var donemYil = SafeGetInt(reader, colMap, "AktifDonemYil", DateTime.Now.Year);
            var donemAy = SafeGetInt(reader, colMap, "AktifDonemAy", DateTime.Now.Month);
            var orgId = SafeGetInt(reader, colMap, "OrganizasyonId", 1);
            var isDeleted = SafeGetBool(reader, colMap, "IsDeleted");
            var createdAt = SafeGetDateTime(reader, colMap, "CreatedAt", DateTime.UtcNow);
            var updatedAt = SafeGetDateTimeNullable(reader, colMap, "UpdatedAt");
            var telefon = SafeGetString(reader, colMap, "Telefon");
            var email = SafeGetString(reader, colMap, "Email");
            var adres = SafeGetString(reader, colMap, "Adres");

            await InsertIfNotExistsAsync(target,
                @"INSERT INTO ""Firmalar"" (""Id"", ""FirmaKodu"", ""FirmaAdi"", ""UnvanTam"", ""VergiNo"", ""VergiDairesi"",
                  ""Aktif"", ""AktifDonemYil"", ""AktifDonemAy"", ""OrganizasyonId"",
                  ""IsDeleted"", ""CreatedAt"", ""UpdatedAt"", ""Telefon"", ""Email"", ""Adres"")
                  VALUES (@id,@kod,@ad,@unvan,@vno,@vd,@aktif,@dyil,@day,@oid,@isdel,@ca,@ua,@tel,@em,@adr)
                  ON CONFLICT (""Id"") DO NOTHING",
                new NpgsqlParameter("@id", id),
                new NpgsqlParameter("@kod", firmaKodu),
                new NpgsqlParameter("@ad", firmaAdi),
                new NpgsqlParameter("@unvan", (object?)unvanTam ?? DBNull.Value),
                new NpgsqlParameter("@vno", (object?)vergiNo ?? DBNull.Value),
                new NpgsqlParameter("@vd", (object?)vergiDairesi ?? DBNull.Value),
                new NpgsqlParameter("@aktif", aktif),
                new NpgsqlParameter("@dyil", donemYil),
                new NpgsqlParameter("@day", donemAy),
                new NpgsqlParameter("@oid", orgId),
                new NpgsqlParameter("@isdel", isDeleted),
                new NpgsqlParameter("@ca", createdAt),
                new NpgsqlParameter("@ua", (object?)updatedAt ?? DBNull.Value),
                new NpgsqlParameter("@tel", (object?)telefon ?? DBNull.Value),
                new NpgsqlParameter("@em", (object?)email ?? DBNull.Value),
                new NpgsqlParameter("@adr", (object?)adres ?? DBNull.Value));
            result.Transferred++;
        }
        _logger.LogInformation("Firmalar: {Count} kayit", result.Transferred);
        return result;
    }

    private async Task<TransferResult> TransferRollerAsync()
    {
        var result = new TransferResult();
        using var source = await OpenSourceAsync();
        using var target = await OpenTargetAsync();

        using var cmd = new NpgsqlCommand(
            @"SELECT ""Id"", ""RolAdi"", ""Aciklama"", ""Renk"", ""SistemRolu"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""
              FROM ""Roller""", source);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            await InsertIfNotExistsAsync(target,
                @"INSERT INTO ""Roller"" (""Id"", ""RolAdi"", ""Aciklama"", ""Renk"", ""SistemRolu"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt"")
                  VALUES (@id,@ad,@aciklama,@renk,@sistem,@isdel,@ca,@ua)
                  ON CONFLICT (""Id"") DO NOTHING",
                new NpgsqlParameter("@id", reader.GetInt32(0)),
                new NpgsqlParameter("@ad", reader.GetString(1)),
                new NpgsqlParameter("@aciklama", reader.IsDBNull(2) ? DBNull.Value : reader.GetString(2)),
                new NpgsqlParameter("@renk", reader.IsDBNull(3) ? DBNull.Value : reader.GetString(3)),
                new NpgsqlParameter("@sistem", reader.GetBoolean(4)),
                new NpgsqlParameter("@isdel", reader.GetBoolean(5)),
                new NpgsqlParameter("@ca", reader.GetDateTime(6)),
                new NpgsqlParameter("@ua", reader.IsDBNull(7) ? DBNull.Value : reader.GetDateTime(7)));
            result.Transferred++;
        }
        _logger.LogInformation("Roller: {Count} kayit", result.Transferred);
        return result;
    }

    private async Task<TransferResult> TransferKullanicilarAsync()
    {
        var result = new TransferResult();
        using var source = await OpenSourceAsync();
        using var target = await OpenTargetAsync();

        using var cmd = new NpgsqlCommand(
            @"SELECT ""Id"", ""KullaniciAdi"", ""AdSoyad"", ""SifreHash"", ""Email"", ""RolId"",
                     ""Aktif"", ""Kilitli"", ""BasarisizGirisSayisi"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""
              FROM ""Kullanicilar""", source);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            await InsertIfNotExistsAsync(target,
                @"INSERT INTO ""Kullanicilar"" (""Id"", ""KullaniciAdi"", ""AdSoyad"", ""SifreHash"", ""Email"", ""RolId"",
                  ""Aktif"", ""Kilitli"", ""BasarisizGirisSayisi"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt"")
                  VALUES (@id,@ka,@ad,@sh,@em,@rid,@aktif,@kilit,@bgs,@isdel,@ca,@ua)
                  ON CONFLICT (""Id"") DO NOTHING",
                new NpgsqlParameter("@id", reader.GetInt32(0)),
                new NpgsqlParameter("@ka", reader.GetString(1)),
                new NpgsqlParameter("@ad", reader.GetString(2)),
                new NpgsqlParameter("@sh", reader.GetString(3)),
                new NpgsqlParameter("@em", reader.IsDBNull(4) ? DBNull.Value : reader.GetString(4)),
                new NpgsqlParameter("@rid", reader.GetInt32(5)),
                new NpgsqlParameter("@aktif", reader.GetBoolean(6)),
                new NpgsqlParameter("@kilit", reader.GetBoolean(7)),
                new NpgsqlParameter("@bgs", reader.GetInt32(8)),
                new NpgsqlParameter("@isdel", reader.GetBoolean(9)),
                new NpgsqlParameter("@ca", reader.GetDateTime(10)),
                new NpgsqlParameter("@ua", reader.IsDBNull(11) ? DBNull.Value : reader.GetDateTime(11)));
            result.Transferred++;
        }
        _logger.LogInformation("Kullanicilar: {Count} kayit", result.Transferred);
        return result;
    }

    private async Task<TransferResult> TransferRolYetkileriAsync()
    {
        var result = new TransferResult();
        using var source = await OpenSourceAsync();
        using var target = await OpenTargetAsync();

        using var cmd = new NpgsqlCommand(
            @"SELECT ""Id"", ""RolId"", ""YetkiKodu"", ""Izin"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""
              FROM ""RolYetkileri""", source);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            await InsertIfNotExistsAsync(target,
                @"INSERT INTO ""RolYetkileri"" (""Id"", ""RolId"", ""YetkiKodu"", ""Izin"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt"")
                  VALUES (@id,@rid,@yk,@izin,@isdel,@ca,@ua)
                  ON CONFLICT (""Id"") DO NOTHING",
                new NpgsqlParameter("@id", reader.GetInt32(0)),
                new NpgsqlParameter("@rid", reader.GetInt32(1)),
                new NpgsqlParameter("@yk", reader.GetString(2)),
                new NpgsqlParameter("@izin", reader.GetBoolean(3)),
                new NpgsqlParameter("@isdel", reader.GetBoolean(4)),
                new NpgsqlParameter("@ca", reader.GetDateTime(5)),
                new NpgsqlParameter("@ua", reader.IsDBNull(6) ? DBNull.Value : reader.GetDateTime(6)));
            result.Transferred++;
        }
        _logger.LogInformation("RolYetkileri: {Count} kayit", result.Transferred);
        return result;
    }

    private async Task<TransferResult> TransferCarilerAsync(int firmaId)
    {
        var result = new TransferResult();
        using var source = await OpenSourceAsync();
        using var target = await OpenTargetAsync();

        using var cmd = new NpgsqlCommand(
            @"SELECT ""Id"", ""CariKodu"", ""CariAdi"", ""VergiNo"", ""VergiDairesi"",
                     ""Telefon"", ""Email"", ""Adres"", ""Aktif"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""
              FROM ""Cariler""", source);
        using var reader = await cmd.ExecuteReaderAsync();

        var colMap = new Dictionary<string, int>();
        for (int i = 0; i < reader.FieldCount; i++)
            colMap[reader.GetName(i)] = i;

        while (await reader.ReadAsync())
        {
            var id = reader.GetInt32(colMap["Id"]);
            var cariKodu = SafeGetString(reader, colMap, "CariKodu") ?? "";
            var cariAdi = SafeGetString(reader, colMap, "CariAdi") ?? "";
            var isDeleted = SafeGetBool(reader, colMap, "IsDeleted");
            var createdAt = SafeGetDateTime(reader, colMap, "CreatedAt", DateTime.UtcNow);

            await InsertIfNotExistsAsync(target,
                @"INSERT INTO ""Cariler"" (""Id"", ""CariKodu"", ""CariAdi"", ""FirmaId"", ""Aktif"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt"")
                  VALUES (@id,@kod,@ad,@fid,@aktif,@isdel,@ca,@ua)
                  ON CONFLICT (""Id"") DO NOTHING",
                new NpgsqlParameter("@id", id),
                new NpgsqlParameter("@kod", cariKodu),
                new NpgsqlParameter("@ad", cariAdi),
                new NpgsqlParameter("@fid", firmaId),
                new NpgsqlParameter("@aktif", true),
                new NpgsqlParameter("@isdel", isDeleted),
                new NpgsqlParameter("@ca", createdAt),
                new NpgsqlParameter("@ua", (object?)DBNull.Value));
            result.Transferred++;
        }
        _logger.LogInformation("Cariler: {Count} kayit", result.Transferred);
        return result;
    }

    private async Task<TransferResult> TransferAraclarAsync(int firmaId)
    {
        return await TransferSimpleAsync("Araclar", firmaId,
            @"""Id"", ""Plaka"", ""Aktif"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""",
            @"""Id"", ""Plaka"", ""FirmaId"", ""Aktif"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""",
            reader => new NpgsqlParameter[]
            {
                new("@id", reader.GetInt32(0)), new("@plaka", SafeGetString(reader, 1) ?? ""),
                new("@fid", firmaId), new("@aktif", SafeGetBool(reader, 2, true)),
                new("@isdel", SafeGetBool(reader, 3)), new("@ca", SafeGetDateTime(reader, 4, DateTime.UtcNow)),
                new("@ua", (object?)DBNull.Value)
            });
    }

    private async Task<TransferResult> TransferSoforlerAsync(int firmaId)
    {
        return await TransferSimpleAsync("Personeller", firmaId,
            @"""Id"", ""AdSoyad"", ""Aktif"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""",
            @"""Id"", ""AdSoyad"", ""FirmaId"", ""Aktif"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""",
            reader => new NpgsqlParameter[]
            {
                new("@id", reader.GetInt32(0)), new("@ad", SafeGetString(reader, 1) ?? ""),
                new("@fid", firmaId), new("@aktif", SafeGetBool(reader, 2, true)),
                new("@isdel", SafeGetBool(reader, 3)), new("@ca", SafeGetDateTime(reader, 4, DateTime.UtcNow)),
                new("@ua", (object?)DBNull.Value)
            });
    }

    private async Task<TransferResult> TransferKurumlarAsync(int firmaId)
    {
        return await TransferSimpleAsync("Kurumlar", firmaId,
            @"""Id"", ""KurumAdi"", ""Aktif"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""",
            @"""Id"", ""KurumAdi"", ""FirmaId"", ""Aktif"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""",
            reader => new NpgsqlParameter[]
            {
                new("@id", reader.GetInt32(0)), new("@ad", SafeGetString(reader, 1) ?? ""),
                new("@fid", firmaId), new("@aktif", SafeGetBool(reader, 2, true)),
                new("@isdel", SafeGetBool(reader, 3)), new("@ca", SafeGetDateTime(reader, 4, DateTime.UtcNow)),
                new("@ua", (object?)DBNull.Value)
            });
    }

    private async Task<TransferResult> TransferGuzergahlarAsync(int firmaId)
    {
        return await TransferSimpleAsync("Guzergahlar", firmaId,
            @"""Id"", ""GuzergahAdi"", ""Aktif"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""",
            @"""Id"", ""GuzergahAdi"", ""FirmaId"", ""Aktif"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""",
            reader => new NpgsqlParameter[]
            {
                new("@id", reader.GetInt32(0)), new("@ad", SafeGetString(reader, 1) ?? ""),
                new("@fid", firmaId), new("@aktif", SafeGetBool(reader, 2, true)),
                new("@isdel", SafeGetBool(reader, 3)), new("@ca", SafeGetDateTime(reader, 4, DateTime.UtcNow)),
                new("@ua", (object?)DBNull.Value)
            });
    }

    private async Task<TransferResult> TransferFaturalarAsync(int firmaId)
    {
        return await TransferSimpleAsync("Faturalar", firmaId,
            @"""Id"", ""FaturaNo"", ""FaturaTarihi"", ""GenelToplam"", ""FaturaTipi"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""",
            @"""Id"", ""FaturaNo"", ""FaturaTarihi"", ""GenelToplam"", ""FaturaTipi"", ""FirmaId"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""",
            reader => new NpgsqlParameter[]
            {
                new("@id", reader.GetInt32(0)), new("@no", SafeGetString(reader, 1) ?? ""),
                new("@tarih", SafeGetDateTime(reader, 2, DateTime.UtcNow)), new("@toplam", SafeGetDecimal(reader, 3)),
                new("@tip", SafeGetInt(reader, 4, 0)), new("@fid", firmaId),
                new("@isdel", SafeGetBool(reader, 5)), new("@ca", SafeGetDateTime(reader, 6, DateTime.UtcNow)),
                new("@ua", (object?)DBNull.Value)
            });
    }

    private async Task<TransferResult> TransferPuantajKayitlarAsync(int firmaId)
    {
        return await TransferSimpleAsync("PuantajKayitlar", firmaId,
            @"""Id"", ""Yil"", ""Ay"", ""KurumAdi"", ""GuzergahAdi"", ""Plaka"", ""SoforAdi"", ""BirimGelir"",
              ""BirimGider"", ""ToplamGider"", ""Odenecek"", ""Alinacak"", ""OnayDurum"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""",
            @"""Id"", ""Yil"", ""Ay"", ""KurumAdi"", ""GuzergahAdi"", ""Plaka"", ""SoforAdi"", ""BirimGelir"",
              ""BirimGider"", ""ToplamGider"", ""Odenecek"", ""Alinacak"", ""OnayDurum"", ""IsverenFirmaId"", ""IsDeleted"", ""CreatedAt"", ""UpdatedAt""",
            reader => new NpgsqlParameter[]
            {
                new("@id", reader.GetInt32(0)), new("@yil", reader.GetInt32(1)), new("@ay", reader.GetInt32(2)),
                new("@kurum", (object?)SafeGetString(reader, 3) ?? DBNull.Value), new("@guzergah", (object?)SafeGetString(reader, 4) ?? DBNull.Value),
                new("@plaka", (object?)SafeGetString(reader, 5) ?? DBNull.Value), new("@sofor", (object?)SafeGetString(reader, 6) ?? DBNull.Value),
                new("@bgelir", SafeGetDecimal(reader, 7)), new("@bgider", SafeGetDecimal(reader, 8)),
                new("@tgelir", SafeGetDecimal(reader, 9)), new("@odenecek", SafeGetDecimal(reader, 11)),
                new("@alinacak", SafeGetDecimal(reader, 12)), new("@onay", SafeGetInt(reader, 13, 0)),
                new("@fid", firmaId), new("@isdel", SafeGetBool(reader, 14)),
                new("@ca", SafeGetDateTime(reader, 15, DateTime.UtcNow)), new("@ua", (object?)DBNull.Value)
            });
    }

    // ── Ek tablo transferleri (TransferSimpleAsync pattern) ──────────

    private async Task<TransferResult> TransferBudgetOdemelerAsync(int firmaId)
        => await TransferSimpleWithFirmaIdAsync("BudgetOdemeler", firmaId);

    private async Task<TransferResult> TransferBudgetHedeflerAsync(int firmaId)
        => await TransferSimpleWithFirmaIdAsync("BudgetHedefler", firmaId);

    private async Task<TransferResult> TransferBudgetMasrafKalemleriAsync(int firmaId)
        => await TransferSimpleWithFirmaIdAsync("BudgetMasrafKalemleri", firmaId);

    private async Task<TransferResult> TransferBankaHesaplariAsync(int firmaId)
        => await TransferSimpleWithFirmaIdAsync("BankaHesaplari", firmaId);

    private async Task<TransferResult> TransferBankaKasaHareketleriAsync(int firmaId)
        => await TransferSimpleWithFirmaIdAsync("BankaKasaHareketleri", firmaId);

    private async Task<TransferResult> TransferStokKartlariAsync(int firmaId)
        => await TransferSimpleWithFirmaIdAsync("StokKartlari", firmaId);

    private async Task<TransferResult> TransferHakedislerAsync(int firmaId)
        => await TransferSimpleWithFirmaIdAsync("Hakedisler", firmaId);

    private async Task<TransferResult> TransferBordrolarAsync(int firmaId)
        => await TransferSimpleWithFirmaIdAsync("Bordrolar", firmaId);

    private async Task<TransferResult> TransferAracMasraflariAsync(int firmaId)
        => await TransferSimpleWithFirmaIdAsync("AracMasraflari", firmaId);

    private async Task<TransferResult> TransferPersonelMaaslariAsync(int firmaId)
        => await TransferSimpleWithFirmaIdAsync("PersonelMaaslari", firmaId);

    private async Task<TransferResult> TransferPersonelIzinleriAsync(int firmaId)
        => await TransferSimpleWithFirmaIdAsync("PersonelIzinleri", firmaId);

    private async Task<TransferResult> TransferOperasyonKayitlariAsync(int firmaId)
        => await TransferSimpleWithFirmaIdAsync("OperasyonKayitlari", firmaId);

    /// <summary>
    /// Kaynak ve hedef tablo aynı isimde, sadece FirmaId eklenerek transfer.
    /// Kaynaktaki TÜM kolonları okur, FirmaId ekler, hedefe yazar.
    /// </summary>
    private async Task<TransferResult> TransferSimpleWithFirmaIdAsync(string tableName, int firmaId)
    {
        var result = new TransferResult();
        using var source = await OpenSourceAsync();
        using var target = await OpenTargetAsync();

        try
        {
            // Kaynak kolonları keşfet
            using var schemaCmd = new NpgsqlCommand(
                $@"SELECT column_name FROM information_schema.columns
                   WHERE table_schema='public' AND table_name='{tableName}'
                   ORDER BY ordinal_position", source);
            var sourceCols = new List<string>();
            using (var r = await schemaCmd.ExecuteReaderAsync())
                while (await r.ReadAsync()) sourceCols.Add(r.GetString(0));

            if (sourceCols.Count == 0)
            {
                _logger.LogWarning("{Table}: kaynak tablo bos veya yok", tableName);
                return result;
            }

            // ID kolonunu başa al
            var cols = sourceCols.Where(c => c != "FirmaId").ToList();
            var sourceColList = string.Join(", ", cols.Select(c => $"\"{c}\""));
            var targetColList = string.Join(", ", cols.Select(c => $"\"{c}\"")) + ", \"FirmaId\"";

            using var cmd = new NpgsqlCommand($"SELECT {sourceColList} FROM \"{tableName}\"", source);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var parms = new List<NpgsqlParameter>();
                var values = new List<string>();
                for (int i = 0; i < cols.Count; i++)
                {
                    var name = $"@p{i}";
                    values.Add(name);
                    parms.Add(new NpgsqlParameter(name, reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i)));
                }
                values.Add("@fid");
                parms.Add(new NpgsqlParameter("@fid", firmaId));

                try
                {
                    await InsertIfNotExistsAsync(target,
                        $"INSERT INTO \"{tableName}\" ({targetColList}) VALUES ({string.Join(",", values)}) ON CONFLICT (\"Id\") DO NOTHING",
                        parms.ToArray());
                    result.Transferred++;
                }
                catch (PostgresException ex) when (ex.SqlState == "23505" || ex.SqlState == "42703")
                {
                    // duplicate key veya kolon uyuşmazlığı — sessiz geç
                }
            }
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            _logger.LogWarning("{Table}: kaynak tablo yok", tableName);
        }

        _logger.LogInformation("{Table}: {Count} kayit", tableName, result.Transferred);
        return result;
    }

    // ── Yardımcılar ──────────────────────────────────────────────────

    private async Task<NpgsqlConnection> OpenSourceAsync()
    {
        var conn = new NpgsqlConnection(_sourceConnStr);
        await conn.OpenAsync();
        return conn;
    }

    private async Task<NpgsqlConnection> OpenTargetAsync()
    {
        var conn = new NpgsqlConnection(_targetConnStr);
        await conn.OpenAsync();
        return conn;
    }

    private static async Task InsertIfNotExistsAsync(NpgsqlConnection conn, string sql, params NpgsqlParameter[] parameters)
    {
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddRange(parameters);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<TransferResult> TransferSimpleAsync(
        string sourceTable, int firmaId,
        string sourceCols, string targetCols,
        Func<NpgsqlDataReader, NpgsqlParameter[]> paramBuilder)
    {
        var result = new TransferResult();
        using var source = await OpenSourceAsync();
        using var target = await OpenTargetAsync();

        try
        {
            using var cmd = new NpgsqlCommand($"SELECT {sourceCols} FROM \"{sourceTable}\"", source);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                try
                {
                    var parms = paramBuilder(reader);
                    await InsertIfNotExistsAsync(target,
                        $"INSERT INTO \"{sourceTable}\" ({targetCols}) VALUES ({string.Join(",", parms.Select(p => p.ParameterName))}) ON CONFLICT (\"Id\") DO NOTHING",
                        parms);
                    result.Transferred++;
                }
                catch (PostgresException ex) when (ex.SqlState == "23505") { /* duplicate */ }
            }
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            _logger.LogWarning("{Table}: kaynak tablo yok, atlandi", sourceTable);
        }

        _logger.LogInformation("{Table}: {Count} kayit", sourceTable, result.Transferred);
        return result;
    }

    // Safe accessors
    private static string? SafeGetString(NpgsqlDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static string? SafeGetString(NpgsqlDataReader reader, Dictionary<string, int> map, string key)
        => map.TryGetValue(key, out var i) && !reader.IsDBNull(i) ? reader.GetString(i) : null;

    private static bool SafeGetBool(NpgsqlDataReader reader, int ordinal, bool defaultValue = false)
        => !reader.IsDBNull(ordinal) && reader.GetBoolean(ordinal);

    private static bool SafeGetBool(NpgsqlDataReader reader, Dictionary<string, int> map, string key, bool defaultValue = false)
        => map.TryGetValue(key, out var i) && !reader.IsDBNull(i) ? reader.GetBoolean(i) : defaultValue;

    private static int SafeGetInt(NpgsqlDataReader reader, int ordinal, int defaultValue = 0)
        => !reader.IsDBNull(ordinal) ? reader.GetInt32(ordinal) : defaultValue;

    private static int SafeGetInt(NpgsqlDataReader reader, Dictionary<string, int> map, string key, int defaultValue = 0)
        => map.TryGetValue(key, out var i) && !reader.IsDBNull(i) ? reader.GetInt32(i) : defaultValue;

    private static DateTime SafeGetDateTime(NpgsqlDataReader reader, int ordinal, DateTime defaultValue)
        => !reader.IsDBNull(ordinal) ? reader.GetDateTime(ordinal) : defaultValue;

    private static DateTime SafeGetDateTime(NpgsqlDataReader reader, Dictionary<string, int> map, string key, DateTime defaultValue)
        => map.TryGetValue(key, out var i) && !reader.IsDBNull(i) ? reader.GetDateTime(i) : defaultValue;

    private static DateTime? SafeGetDateTimeNullable(NpgsqlDataReader reader, Dictionary<string, int> map, string key)
        => map.TryGetValue(key, out var i) && !reader.IsDBNull(i) ? reader.GetDateTime(i) : null;

    private static decimal SafeGetDecimal(NpgsqlDataReader reader, int ordinal)
        => !reader.IsDBNull(ordinal) ? reader.GetDecimal(ordinal) : 0m;
}

public class TransferResult
{
    public int Transferred { get; set; }
    public int TotalTransferred { get; set; }
    public List<string> Errors { get; set; } = new();

    public void Add(TransferResult other)
    {
        Transferred += other.Transferred;
        TotalTransferred += other.Transferred;
        Errors.AddRange(other.Errors);
    }
}
