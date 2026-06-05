using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace KOAFiloServis.Web.Data.Migrations;

/// <summary>
/// Mevcut tüm Personel cariler için 195.01.XXX (İş Avansları) hesabını otomatik açar.
/// Yeni personel için CariService.CreateMuhasebeHesapAsync zaten açıyor.
/// </summary>
public static class PersonelAvansHesapMigrationHelper
{
    public static async Task ApplyPersonelAvansHesaplariAsync(ApplicationDbContext context)
    {
        try
        {
            if (!context.Database.IsNpgsql()) return;

            var avansPrefix = "195.01";
            try
            {
                var conn = context.Database.GetDbConnection();
                var shouldClose = conn.State != System.Data.ConnectionState.Open;
                if (shouldClose)
                {
                    await conn.OpenAsync();
                }

                try
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT ""PersonelAvansPrefix""
                        FROM ""MuhasebeAyarlari""
                        ORDER BY ""Id""
                        LIMIT 1;";

                    var value = await cmd.ExecuteScalarAsync();
                    if (value != null)
                    {
                        var prefix = value.ToString();
                        if (!string.IsNullOrWhiteSpace(prefix))
                        {
                            avansPrefix = prefix;
                        }
                    }
                }
                finally
                {
                    if (shouldClose)
                    {
                        await conn.CloseAsync();
                    }
                }
            }
            catch
            {
                // MuhasebeAyarlari tablosu/kolonu eski tenant şemasında eksik olabilir.
                // Varsayılan prefix ile devam edilir.
            }

            // 195 ana hesabını kontrol et/oluştur
            var kod195 = avansPrefix.Split('.')[0];
            var hesap195 = await context.MuhasebeHesaplari
                .OrderBy(h => h.Id)
                .FirstOrDefaultAsync(h => h.HesapKodu == kod195);
            if (hesap195 == null)
            {
                hesap195 = new MuhasebeHesap
                {
                    HesapKodu = kod195,
                    HesapAdi = "IS AVANSLARI",
                    HesapTuru = HesapTuru.Aktif,
                    HesapGrubu = HesapGrubu.DonenVarliklar,
                    AltHesapVar = true,
                    SistemHesabi = true,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.MuhasebeHesaplari.Add(hesap195);
                await context.SaveChangesAsync();
            }

            // 195.01 alt hesabını kontrol et/oluştur
            var hesap195_01 = await context.MuhasebeHesaplari
                .OrderBy(h => h.Id)
                .FirstOrDefaultAsync(h => h.HesapKodu == avansPrefix);
            if (hesap195_01 == null)
            {
                hesap195_01 = new MuhasebeHesap
                {
                    HesapKodu = avansPrefix,
                    HesapAdi = "Personel Avanslari",
                    HesapTuru = HesapTuru.Aktif,
                    HesapGrubu = HesapGrubu.DonenVarliklar,
                    UstHesapId = hesap195.Id,
                    AltHesapVar = true,
                    SistemHesabi = true,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.MuhasebeHesaplari.Add(hesap195_01);
                await context.SaveChangesAsync();
            }

            // PersonelAvansHesapId eksik olan personel carilerini getir
            var personelCariler = await context.Cariler
                .IgnoreQueryFilters()
                .Where(c => c.CariTipi == CariTipi.Personel && !c.IsDeleted && !c.PersonelAvansHesapId.HasValue)
                .ToListAsync();

            int olusturulan = 0;
            foreach (var cari in personelCariler)
            {
                // Zaten bu isimde 195.01.XXX var mı?
                var mevcutAvansHesap = await context.MuhasebeHesaplari
                    .OrderBy(h => h.Id)
                    .FirstOrDefaultAsync(h => h.HesapKodu.StartsWith(avansPrefix + ".") && h.HesapAdi == cari.Unvan);

                if (mevcutAvansHesap == null)
                {
                    // Sıradaki numarayı bul
                    var sonAltHesap = await context.MuhasebeHesaplari
                        .Where(h => h.HesapKodu.StartsWith(avansPrefix + "."))
                        .OrderByDescending(h => h.HesapKodu)
                        .FirstOrDefaultAsync();
                    int nextNum = 1;
                    if (sonAltHesap != null)
                    {
                        var parts = sonAltHesap.HesapKodu.Split('.');
                        var sadeceSayi = new string(parts[parts.Length - 1].Where(char.IsDigit).ToArray());
                        if (int.TryParse(sadeceSayi, out var lastNum)) nextNum = lastNum + 1;
                    }

                    mevcutAvansHesap = new MuhasebeHesap
                    {
                        HesapKodu = $"{avansPrefix}.{nextNum:D3}",
                        HesapAdi = cari.Unvan,
                        HesapTuru = HesapTuru.Aktif,
                        HesapGrubu = HesapGrubu.DonenVarliklar,
                        UstHesapId = hesap195_01.Id,
                        AltHesapVar = false,
                        Aktif = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.MuhasebeHesaplari.Add(mevcutAvansHesap);
                    await context.SaveChangesAsync();
                }

                cari.PersonelAvansHesapId = mevcutAvansHesap.Id;
                cari.UpdatedAt = DateTime.UtcNow;
                olusturulan++;
            }

            if (olusturulan > 0)
            {
                await context.SaveChangesAsync();
                Console.WriteLine($"PersonelAvansHesap: {olusturulan} personel için 195.01.XXX hesabı açıldı.");
            }
            else
            {
                Console.WriteLine("PersonelAvansHesap: Tüm personel hesapları zaten mevcut.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PersonelAvansHesap migration hatası: {ex.Message}");
        }
    }
}
