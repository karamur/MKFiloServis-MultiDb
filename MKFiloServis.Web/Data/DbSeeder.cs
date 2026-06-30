using MKFiloServis.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace MKFiloServis.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Firma
        if (!await context.Firmalar.IgnoreQueryFilters().AnyAsync())
        {
            var firma = new Firma
            {
                FirmaAdi = "Ana Firma",
                VergiDairesi = "Merkez VD",
                VergiNo = "1234567890",
                Adres = "İstanbul, Türkiye",
                Telefon = "+90 212 000 00 00",
                Email = "info@firma.com",
                VarsayilanFirma = true,
                Aktif = true,
                AktifDonemYil = DateTime.Today.Year,
                AktifDonemAy = DateTime.Today.Month,
                CreatedAt = DateTime.UtcNow
            };
            context.Firmalar.Add(firma);
            await context.SaveChangesAsync();
        }

        // Roller
        if (!await context.Roller.IgnoreQueryFilters().AnyAsync())
        {
            var roller = new List<Rol>
            {
                new Rol { RolAdi = "Admin", Aciklama = "Sistem yöneticisi", CreatedAt = DateTime.UtcNow },
                new Rol { RolAdi = "Muhasebe", Aciklama = "Muhasebe personeli", CreatedAt = DateTime.UtcNow },
                new Rol { RolAdi = "Kullanici", Aciklama = "Standart kullanıcı", CreatedAt = DateTime.UtcNow }
            };
            context.Roller.AddRange(roller);
            await context.SaveChangesAsync();
        }

        // Kullanici (Admin)
        if (!await context.Kullanicilar.IgnoreQueryFilters().AnyAsync())
        {
            var adminRol = await context.Roller.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.RolAdi == "Admin");
            if (adminRol is null)
            {
                Console.WriteLine("⚠️ Admin rolü bulunamadı, admin kullanıcı oluşturulamadı.");
            }
            else
            {
                var admin = new Kullanici
                {
                    KullaniciAdi = "admin",
                    SifreHash = "admin123", // Production'da düzgün hash'lenmiş olmalı
                    AdSoyad = "Sistem Yöneticisi",
                    Email = "admin@firma.com",
                    RolId = adminRol.Id,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Kullanicilar.Add(admin);
                await context.SaveChangesAsync();
            }
        }

        // Muhasebe Hesap Planı (Tek Düzen Hesap Planı)
        if (!await context.MuhasebeHesaplari.IgnoreQueryFilters().AnyAsync())
        {
            var hesaplar = new List<MuhasebeHesap>
            {
                // 1XX - DÖNEN VARLIKLAR
                new MuhasebeHesap { HesapKodu = "100", HesapAdi = "Kasa", HesapTuru = HesapTuru.Aktif, HesapGrubu = HesapGrubu.DonenVarliklar, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "100.01", HesapAdi = "TL Kasa", HesapTuru = HesapTuru.Aktif, HesapGrubu = HesapGrubu.DonenVarliklar, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "100.02", HesapAdi = "Döviz Kasa", HesapTuru = HesapTuru.Aktif, HesapGrubu = HesapGrubu.DonenVarliklar, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "102", HesapAdi = "Bankalar", HesapTuru = HesapTuru.Aktif, HesapGrubu = HesapGrubu.DonenVarliklar, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "102.01", HesapAdi = "TL Banka", HesapTuru = HesapTuru.Aktif, HesapGrubu = HesapGrubu.DonenVarliklar, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "120", HesapAdi = "Alıcılar", HesapTuru = HesapTuru.Aktif, HesapGrubu = HesapGrubu.DonenVarliklar, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "120.01", HesapAdi = "Müşteriler", HesapTuru = HesapTuru.Aktif, HesapGrubu = HesapGrubu.DonenVarliklar, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "153", HesapAdi = "Ticari Mallar", HesapTuru = HesapTuru.Aktif, HesapGrubu = HesapGrubu.DonenVarliklar, Aktif = true, CreatedAt = DateTime.UtcNow },

                // 2XX - DURAN VARLIKLAR
                new MuhasebeHesap { HesapKodu = "253", HesapAdi = "Taşıtlar", HesapTuru = HesapTuru.Aktif, HesapGrubu = HesapGrubu.DuranVarliklar, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "253.01", HesapAdi = "Araçlar", HesapTuru = HesapTuru.Aktif, HesapGrubu = HesapGrubu.DuranVarliklar, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "257", HesapAdi = "Birikmiş Amortismanlar", HesapTuru = HesapTuru.Aktif, HesapGrubu = HesapGrubu.DuranVarliklar, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },

                // 3XX - KISA VADELİ YABANCI KAYNAKLAR
                new MuhasebeHesap { HesapKodu = "320", HesapAdi = "Satıcılar", HesapTuru = HesapTuru.Pasif, HesapGrubu = HesapGrubu.KisaVadeliYabanciKaynaklar, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "320.01", HesapAdi = "Tedarikçiler", HesapTuru = HesapTuru.Pasif, HesapGrubu = HesapGrubu.KisaVadeliYabanciKaynaklar, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "360", HesapAdi = "Ödenecek Vergi ve Fonlar", HesapTuru = HesapTuru.Pasif, HesapGrubu = HesapGrubu.KisaVadeliYabanciKaynaklar, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "360.01", HesapAdi = "KDV Borcu", HesapTuru = HesapTuru.Pasif, HesapGrubu = HesapGrubu.KisaVadeliYabanciKaynaklar, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "361", HesapAdi = "Ödenecek Sosyal Güv. Kes.", HesapTuru = HesapTuru.Pasif, HesapGrubu = HesapGrubu.KisaVadeliYabanciKaynaklar, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "370", HesapAdi = "Dönem Karı Vergi ve Diğer Yasal Yük. Karşılığı", HesapTuru = HesapTuru.Pasif, HesapGrubu = HesapGrubu.KisaVadeliYabanciKaynaklar, Aktif = true, CreatedAt = DateTime.UtcNow },

                // 5XX - ÖZKAYNAK
                new MuhasebeHesap { HesapKodu = "500", HesapAdi = "Sermaye", HesapTuru = HesapTuru.Pasif, HesapGrubu = HesapGrubu.Ozkaynaklar, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "590", HesapAdi = "Dönem Net Karı/Zararı", HesapTuru = HesapTuru.Pasif, HesapGrubu = HesapGrubu.Ozkaynaklar, Aktif = true, CreatedAt = DateTime.UtcNow },

                // 6XX - GELİR HESAPLARI
                new MuhasebeHesap { HesapKodu = "600", HesapAdi = "Yurt İçi Satışlar", HesapTuru = HesapTuru.Gelir, HesapGrubu = HesapGrubu.GelirTablosu, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "600.01", HesapAdi = "Servis Gelirleri", HesapTuru = HesapTuru.Gelir, HesapGrubu = HesapGrubu.GelirTablosu, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "602", HesapAdi = "Diğer Gelirler", HesapTuru = HesapTuru.Gelir, HesapGrubu = HesapGrubu.GelirTablosu, Aktif = true, CreatedAt = DateTime.UtcNow },

                // 7XX - GİDER HESAPLARI
                new MuhasebeHesap { HesapKodu = "710", HesapAdi = "Direkt İlk Madde ve Malzeme Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "720", HesapAdi = "Direkt İşçilik Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "720.01", HesapAdi = "Şoför Maaşları", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "730", HesapAdi = "Genel Üretim Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "730.01", HesapAdi = "Yakıt Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "730.02", HesapAdi = "Araç Bakım Onarım", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "730.03", HesapAdi = "Sigorta Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "750", HesapAdi = "Araştırma ve Geliştirme Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "760", HesapAdi = "Pazarlama Satış ve Dağıtım Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "760.01", HesapAdi = "Reklam Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "770", HesapAdi = "Genel Yönetim Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "770.01", HesapAdi = "Kira Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "770.02", HesapAdi = "Elektrik Su Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "770.03", HesapAdi = "Haberleşme Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "770.04", HesapAdi = "Kırtasiye Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, Aktif = true, CreatedAt = DateTime.UtcNow },

                new MuhasebeHesap { HesapKodu = "780", HesapAdi = "Finansman Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, AltHesapVar = true, Aktif = true, CreatedAt = DateTime.UtcNow },
                new MuhasebeHesap { HesapKodu = "780.01", HesapAdi = "Kredi Faiz Giderleri", HesapTuru = HesapTuru.Gider, HesapGrubu = HesapGrubu.MaliyetHesaplari, Aktif = true, CreatedAt = DateTime.UtcNow }
            };

            context.MuhasebeHesaplari.AddRange(hesaplar);
            await context.SaveChangesAsync();

            // Parent-child ilişkilerini güncelle
            foreach (var hesap in hesaplar.Where(h => h.HesapKodu.Contains(".")))
            {
                var parentKod = hesap.HesapKodu.Substring(0, hesap.HesapKodu.LastIndexOf('.'));
                var parent = await context.MuhasebeHesaplari.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.HesapKodu == parentKod);
                if (parent != null)
                {
                    hesap.UstHesapId = parent.Id;
                }
            }
            await context.SaveChangesAsync();
        }

        // Bütçe Masraf Kalemleri
        if (!await context.BudgetMasrafKalemleri.IgnoreQueryFilters().AnyAsync())
        {
            var masrafKalemleri = new List<BudgetMasrafKalemi>
            {
                new BudgetMasrafKalemi { KalemAdi = "Yakıt", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Araç Bakım/Onarım", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Araç Sigorta", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "MTV", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Şoför Maaşları", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Kira", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Elektrik", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Su", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Doğalgaz", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "İnternet", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Telefon", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Kırtasiye", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Temizlik", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Muhasebe/Danışmanlık", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Reklam/Pazarlama", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Kredi Taksiti", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Vergi/SGK Ödemeleri", Aktif = true, CreatedAt = DateTime.UtcNow },
                new BudgetMasrafKalemi { KalemAdi = "Diğer", Aktif = true, CreatedAt = DateTime.UtcNow }
            };

            context.BudgetMasrafKalemleri.AddRange(masrafKalemleri);
            await context.SaveChangesAsync();
        }

        // Araç Markaları
        if (!await context.AracMarkalari.IgnoreQueryFilters().AnyAsync())
        {
            var markalar = new List<AracMarka>
            {
                new AracMarka { MarkaAdi = "Mercedes-Benz", Aktif = true, CreatedAt = DateTime.UtcNow },
                new AracMarka { MarkaAdi = "Ford", Aktif = true, CreatedAt = DateTime.UtcNow },
                new AracMarka { MarkaAdi = "Volkswagen", Aktif = true, CreatedAt = DateTime.UtcNow },
                new AracMarka { MarkaAdi = "Fiat", Aktif = true, CreatedAt = DateTime.UtcNow },
                new AracMarka { MarkaAdi = "Hyundai", Aktif = true, CreatedAt = DateTime.UtcNow },
                new AracMarka { MarkaAdi = "Iveco", Aktif = true, CreatedAt = DateTime.UtcNow },
                new AracMarka { MarkaAdi = "BMC", Aktif = true, CreatedAt = DateTime.UtcNow },
                new AracMarka { MarkaAdi = "Otokar", Aktif = true, CreatedAt = DateTime.UtcNow }
            };

            context.AracMarkalari.AddRange(markalar);
            await context.SaveChangesAsync();
        }

        // Araç Modelleri
        if (!await context.AracModelleri.IgnoreQueryFilters().AnyAsync())
        {
            var mercedes = await context.AracMarkalari.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.MarkaAdi == "Mercedes-Benz");
            var ford = await context.AracMarkalari.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.MarkaAdi == "Ford");
            var vw = await context.AracMarkalari.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.MarkaAdi == "Volkswagen");

            // Markalar yoksa model ekleme
            if (mercedes is null || ford is null || vw is null)
            {
                Console.WriteLine("⚠️ Araç markaları bulunamadı, modeller eklenemedi.");
            }
            else
            {
                var modeller = new List<AracModelTanim>
                {
                    new AracModelTanim { MarkaId = mercedes.Id, ModelAdi = "Sprinter", Aktif = true, CreatedAt = DateTime.UtcNow },
                    new AracModelTanim { MarkaId = mercedes.Id, ModelAdi = "Tourismo", Aktif = true, CreatedAt = DateTime.UtcNow },
                    new AracModelTanim { MarkaId = ford.Id, ModelAdi = "Transit", Aktif = true, CreatedAt = DateTime.UtcNow },
                    new AracModelTanim { MarkaId = ford.Id, ModelAdi = "Transit Custom", Aktif = true, CreatedAt = DateTime.UtcNow },
                    new AracModelTanim { MarkaId = vw.Id, ModelAdi = "Crafter", Aktif = true, CreatedAt = DateTime.UtcNow },
                    new AracModelTanim { MarkaId = vw.Id, ModelAdi = "Caravelle", Aktif = true, CreatedAt = DateTime.UtcNow }
                };

                context.AracModelleri.AddRange(modeller);
                await context.SaveChangesAsync();
            }
        }

        // Banka Hesapları
        var bankaHesapSet = context.Set<BankaHesap>();
        if (!await TableHasAnyRowsAsync(context, "BankaHesaplari"))
        {
            var hesaplar = new List<BankaHesap>
            {
                new BankaHesap 
                { 
                    HesapKodu = "KASA01",
                    HesapAdi = "Nakit Kasa", 
                    HesapTipi = HesapTipi.Kasa,
                    AcilisBakiye = 0,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow 
                },
                new BankaHesap 
                { 
                    HesapKodu = "BANKA01",
                    HesapAdi = "Ziraat Bankası Ticari Hesap", 
                    HesapTipi = HesapTipi.VadesizHesap,
                    BankaAdi = "Ziraat Bankası",
                    SubeKodu = "001",
                    HesapNo = "1234567890",
                    Iban = "TR000000000000000000000000",
                    AcilisBakiye = 0,
                    Aktif = true,
                    CreatedAt = DateTime.UtcNow 
                }
            };

            bankaHesapSet.AddRange(hesaplar);
            await context.SaveChangesAsync();
        }

        // Gelen Faturaları E-Fatura olarak güncelle
        await UpdateGelenFaturalarToEFaturaAsync(context);

        Console.WriteLine("? Seed verileri başarıyla eklendi!");
    }

    /// <summary>
    /// Tüm gelen faturaları E-Fatura olarak günceller
    /// </summary>
    public static async Task UpdateGelenFaturalarToEFaturaAsync(ApplicationDbContext context)
    {
        try
        {
            if (context.Database.IsNpgsql())
            {
                var affected = await context.Database.ExecuteSqlRawAsync(@"
                    UPDATE ""Faturalar""
                    SET ""EFaturaTipi"" = @p0,
                        ""UpdatedAt"" = @p1
                    WHERE ""IsDeleted"" = false
                      AND ""FaturaYonu"" = @p2
                      AND ""EFaturaTipi"" <> @p3;",
                    (int)EFaturaTipi.EFatura,
                    DateTime.UtcNow,
                    (int)FaturaYonu.Gelen,
                    (int)EFaturaTipi.EFatura);

                if (affected > 0)
                {
                    Console.WriteLine($"? {affected} adet gelen fatura E-Fatura olarak güncellendi!");
                }

                return;
            }

            var gelenFaturalar = await context.Faturalar
                .IgnoreQueryFilters()
                .Where(f => !f.IsDeleted && f.FaturaYonu == FaturaYonu.Gelen && f.EFaturaTipi != EFaturaTipi.EFatura)
                .ToListAsync();

            if (gelenFaturalar.Any())
            {
                foreach (var fatura in gelenFaturalar)
                {
                    fatura.EFaturaTipi = EFaturaTipi.EFatura;
                    fatura.UpdatedAt = DateTime.Now;
                }

                await context.SaveChangesAsync();
                Console.WriteLine($"? {gelenFaturalar.Count} adet gelen fatura E-Fatura olarak güncellendi!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateGelenFaturalarToEFatura atlandi: {ex.Message}");
        }
    }

    private static async Task<bool> TableHasAnyRowsAsync(ApplicationDbContext context, string tableName)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();

            if (context.Database.IsNpgsql())
            {
                command.CommandText = $"SELECT CASE WHEN to_regclass('\"{tableName}\"') IS NULL THEN FALSE ELSE EXISTS (SELECT 1 FROM \"{tableName}\") END";
            }
            else if (context.Database.IsSqlite())
            {
                command.CommandText = $"SELECT CASE WHEN EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = '{tableName}') THEN EXISTS (SELECT 1 FROM \"{tableName}\") ELSE 0 END";
            }
            else
            {
                return await context.Set<BankaHesap>().IgnoreQueryFilters().AnyAsync();
            }

            var result = await command.ExecuteScalarAsync();
            return result != null && Convert.ToBoolean(result);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}


