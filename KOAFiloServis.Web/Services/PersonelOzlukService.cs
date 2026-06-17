using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;

namespace KOAFiloServis.Web.Services;

public class PersonelOzlukService : IPersonelOzlukService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public PersonelOzlukService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    #region Evrak Tanımları

    public async Task<List<OzlukEvrakTanim>> GetEvrakTanimlariAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OzlukEvrakTanimlari
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.Kategori)
            .ThenBy(e => e.SiraNo)
            .ToListAsync();
    }

    public async Task<List<OzlukEvrakTanim>> GetAktifEvrakTanimlariAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OzlukEvrakTanimlari
            .Where(e => !e.IsDeleted && e.Aktif)
            .OrderBy(e => e.Kategori)
            .ThenBy(e => e.SiraNo)
            .ToListAsync();
    }

    public async Task<OzlukEvrakTanim?> GetEvrakTanimByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OzlukEvrakTanimlari.FindAsync(id);
    }

    public async Task<OzlukEvrakTanim> CreateEvrakTanimAsync(OzlukEvrakTanim tanim)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        tanim.CreatedAt = DateTime.UtcNow;
        context.OzlukEvrakTanimlari.Add(tanim);
        await context.SaveChangesAsync();
        return tanim;
    }

    public async Task<OzlukEvrakTanim> UpdateEvrakTanimAsync(OzlukEvrakTanim tanim)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.OzlukEvrakTanimlari.FindAsync(tanim.Id);
        if (existing == null)
            throw new InvalidOperationException("Evrak tanımı bulunamadı.");

        existing.EvrakAdi = tanim.EvrakAdi;
        existing.Aciklama = tanim.Aciklama;
        existing.Kategori = tanim.Kategori;
        existing.Zorunlu = tanim.Zorunlu;
        existing.SiraNo = tanim.SiraNo;
        existing.Aktif = tanim.Aktif;
        existing.GecerliGorevler = tanim.GecerliGorevler;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteEvrakTanimAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var tanim = await context.OzlukEvrakTanimlari.FindAsync(id);
        if (tanim == null)
            return;

        var dahaOnceIslemGormus = await context.PersonelOzlukEvraklar
            .AnyAsync(e => e.EvrakTanimId == id);

        if (dahaOnceIslemGormus)
        {
            tanim.IsDeleted = true;
            tanim.Aktif = false;
            tanim.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return;
        }

        context.OzlukEvrakTanimlari.Remove(tanim);
        await context.SaveChangesAsync();
    }

    public async Task SeedDefaultEvrakTanimlariAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            if (!await TableExistsAsync(context, "OzlukEvrakTanimlari"))
                return;

            var defaultEvraklar = new List<OzlukEvrakTanim>
            {
            // Kimlik Belgeleri
            new() { EvrakAdi = "Kimlik Fotokopisi", Kategori = OzlukEvrakKategori.KimlikBelgeleri, SiraNo = 1, Zorunlu = true },
            new() { EvrakAdi = "İkametgah Belgesi", Kategori = OzlukEvrakKategori.KimlikBelgeleri, SiraNo = 2, Zorunlu = true },
            new() { EvrakAdi = "Vesikalık Fotoğraf (2 Adet)", Kategori = OzlukEvrakKategori.KimlikBelgeleri, SiraNo = 3, Zorunlu = true },
            new() { EvrakAdi = "Nüfus Kayıt Örneği", Kategori = OzlukEvrakKategori.KimlikBelgeleri, SiraNo = 4, Zorunlu = false },

            // Eğitim Belgeleri
            new() { EvrakAdi = "Diploma Fotokopisi", Kategori = OzlukEvrakKategori.EgitimBelgeleri, SiraNo = 1, Zorunlu = true },
            new() { EvrakAdi = "Transkript", Kategori = OzlukEvrakKategori.EgitimBelgeleri, SiraNo = 2, Zorunlu = false },

            // Sağlık Belgeleri
            new() { EvrakAdi = "Sağlık Raporu", Kategori = OzlukEvrakKategori.SaglikBelgeleri, SiraNo = 1, Zorunlu = true },
            new() { EvrakAdi = "Akciğer Filmi", Kategori = OzlukEvrakKategori.SaglikBelgeleri, SiraNo = 2, Zorunlu = false },
            new() { EvrakAdi = "Hepatit B Testi", Kategori = OzlukEvrakKategori.SaglikBelgeleri, SiraNo = 3, Zorunlu = false },

            // Şoför Belgeleri (Sadece şoförler için)
            new() { EvrakAdi = "Ehliyet Fotokopisi", Kategori = OzlukEvrakKategori.SoforBelgeleri, SiraNo = 1, Zorunlu = true, GecerliGorevler = "1" },
            new() { EvrakAdi = "SRC Belgesi", Kategori = OzlukEvrakKategori.SoforBelgeleri, SiraNo = 2, Zorunlu = true, GecerliGorevler = "1" },
            new() { EvrakAdi = "Psikoteknik Belgesi", Kategori = OzlukEvrakKategori.SoforBelgeleri, SiraNo = 3, Zorunlu = true, GecerliGorevler = "1" },
            new() { EvrakAdi = "ADR Belgesi", Kategori = OzlukEvrakKategori.SoforBelgeleri, SiraNo = 4, Zorunlu = false, GecerliGorevler = "1" },
            new() { EvrakAdi = "Sürücü Ceza Barkodlu Belge", Kategori = OzlukEvrakKategori.SoforBelgeleri, SiraNo = 5, Zorunlu = true, GecerliGorevler = "1", Aciklama = "Sürücü ceza puanı barkodlu sorgu belgesi" },

            // SGK Belgeleri
            new() { EvrakAdi = "SGK İşe Giriş Bildirgesi", Kategori = OzlukEvrakKategori.SGKBelgeleri, SiraNo = 1, Zorunlu = true },
            new() { EvrakAdi = "İşyeri Hekimi Muayene Formu", Kategori = OzlukEvrakKategori.SGKBelgeleri, SiraNo = 2, Zorunlu = true },
            new() { EvrakAdi = "Periyodik Sağlık Muayenesi", Kategori = OzlukEvrakKategori.SGKBelgeleri, SiraNo = 3, Zorunlu = true },
            new() { EvrakAdi = "İSG Eğitim Sertifikası", Kategori = OzlukEvrakKategori.SGKBelgeleri, SiraNo = 4, Zorunlu = true },

            // İşe Giriş Belgeleri
            new() { EvrakAdi = "İş Başvuru Formu", Kategori = OzlukEvrakKategori.IseGirisBelgeleri, SiraNo = 1, Zorunlu = true },
            new() { EvrakAdi = "İş Sözleşmesi", Kategori = OzlukEvrakKategori.IseGirisBelgeleri, SiraNo = 2, Zorunlu = true },
            new() { EvrakAdi = "Özgeçmiş (CV)", Kategori = OzlukEvrakKategori.IseGirisBelgeleri, SiraNo = 3, Zorunlu = true },
            new() { EvrakAdi = "Referans Mektubu", Kategori = OzlukEvrakKategori.IseGirisBelgeleri, SiraNo = 4, Zorunlu = false },
            new() { EvrakAdi = "Sabıka Kaydı", Kategori = OzlukEvrakKategori.IseGirisBelgeleri, SiraNo = 5, Zorunlu = true },
            new() { EvrakAdi = "Askerlik Durum Belgesi", Kategori = OzlukEvrakKategori.IseGirisBelgeleri, SiraNo = 6, Zorunlu = true, Aciklama = "Erkek personel için" },
            new() { EvrakAdi = "KVKK Aydınlatma Metni", Kategori = OzlukEvrakKategori.IseGirisBelgeleri, SiraNo = 7, Zorunlu = true },
            new() { EvrakAdi = "Kişisel Verilerin Korunması Onay Formu", Kategori = OzlukEvrakKategori.IseGirisBelgeleri, SiraNo = 8, Zorunlu = true },
            new() { EvrakAdi = "Zimmet Teslim Formu", Kategori = OzlukEvrakKategori.IseGirisBelgeleri, SiraNo = 9, Zorunlu = false },

            // Diğer
            new() { EvrakAdi = "Banka Hesap Bilgileri (IBAN)", Kategori = OzlukEvrakKategori.Diger, SiraNo = 1, Zorunlu = true },
            new() { EvrakAdi = "AGİ Formu", Kategori = OzlukEvrakKategori.Diger, SiraNo = 2, Zorunlu = true },
            new() { EvrakAdi = "Engellilik Belgesi", Kategori = OzlukEvrakKategori.Diger, SiraNo = 3, Zorunlu = false },
            };

            // Mevcut tanımları al (silinmiş olanlar dahil değil) ve eksikleri ekle.
            // Böylece mevcut DB'lere yeni eklenen evrak tanımları otomatik gelir.
            var mevcutAdlar = await context.OzlukEvrakTanimlari
                .Where(t => !t.IsDeleted)
                .Select(t => t.EvrakAdi)
                .ToListAsync();

            var eklenecekler = defaultEvraklar
                .Where(e => !mevcutAdlar.Any(m => string.Equals(m, e.EvrakAdi, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (eklenecekler.Count == 0)
                return;

            foreach (var evrak in eklenecekler)
            {
                evrak.CreatedAt = DateTime.UtcNow;
            }

            context.OzlukEvrakTanimlari.AddRange(eklenecekler);
            await context.SaveChangesAsync();
        }
        catch
        {
            // Startup sırasında tablo/kolon eksikliği varsa uygulamayı düşürme.
        }
    }

    private async Task<bool> TableExistsAsync(ApplicationDbContext context, string tableName)
    {
        var connection = context.Database.GetDbConnection();
        var closeAfter = connection.State != ConnectionState.Open;

        if (closeAfter)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();

            if (context.Database.IsNpgsql())
            {
                command.CommandText = $"SELECT to_regclass('\"{tableName}\"') IS NOT NULL";
            }
            else if (context.Database.IsSqlite())
            {
                command.CommandText = $"SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = '{tableName}')";
            }
            else
            {
                return true;
            }

            var result = await command.ExecuteScalarAsync();
            return result != null && Convert.ToBoolean(result);
        }
        finally
        {
            if (closeAfter)
                await connection.CloseAsync();
        }
    }

    #endregion

    #region Personel Evrakları

    public async Task<List<PersonelOzlukEvrak>> GetPersonelEvraklariAsync(int soforId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PersonelOzlukEvraklar
            .Include(e => e.EvrakTanim)
            .Where(e => e.SoforId == soforId && !e.IsDeleted)
            .OrderBy(e => e.EvrakTanim.Kategori)
            .ThenBy(e => e.EvrakTanim.SiraNo)
            .ToListAsync();
    }

    public async Task<PersonelOzlukEvrak?> GetPersonelEvrakByIdAsync(int evrakId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PersonelOzlukEvraklar
            .Include(e => e.Sofor)
            .Include(e => e.EvrakTanim)
            .FirstOrDefaultAsync(e => e.Id == evrakId && !e.IsDeleted);
    }

    public async Task<PersonelOzlukEvrakDurum> GetPersonelEvrakDurumuAsync(int soforId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var personel = await context.Soforler.FindAsync(soforId);
        if (personel == null)
            throw new InvalidOperationException("Personel bulunamadı.");

        var evrakTanimlari = await GetGecerliEvrakTanimlariAsync(context, personel.Gorev);
        var personelEvraklari = await GetPersonelEvraklariAsync(soforId);

        var durum = new PersonelOzlukEvrakDurum
        {
            SoforId = soforId,
            PersonelAdi = personel.TamAd,
            PersonelKodu = personel.SoforKodu,
            Gorev = personel.Gorev,
            ToplamEvrak = evrakTanimlari.Count,
            Evraklar = new List<OzlukEvrakDetay>()
        };

        foreach (var tanim in evrakTanimlari)
        {
            var personelEvrak = personelEvraklari.FirstOrDefault(e => e.EvrakTanimId == tanim.Id);

            durum.Evraklar.Add(new OzlukEvrakDetay
            {
                EvrakTanimId = tanim.Id,
                EvrakAdi = tanim.EvrakAdi,
                Kategori = tanim.Kategori,
                Zorunlu = tanim.Zorunlu,
                Tamamlandi = personelEvrak?.Tamamlandi ?? false,
                TamamlanmaTarihi = personelEvrak?.TamamlanmaTarihi,
                GecerlilikBitisTarihi = personelEvrak?.GecerlilikBitisTarihi,
                DosyaYolu = personelEvrak?.DosyaYolu,
                Aciklama = personelEvrak?.Aciklama ?? tanim.Aciklama
            });
        }

        durum.TamamlananEvrak = durum.Evraklar.Count(e => e.Tamamlandi);
        durum.EksikEvrak = durum.ToplamEvrak - durum.TamamlananEvrak;
        durum.TamamlanmaYuzdesi = durum.ToplamEvrak > 0 
            ? Math.Round((decimal)durum.TamamlananEvrak / durum.ToplamEvrak * 100, 1) 
            : 0;

        return durum;
    }

    public async Task<List<PersonelOzlukEvrakDurum>> GetTumPersonelEvrakDurumlariAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var personeller = await context.Soforler
            .Where(s => !s.IsDeleted && s.Aktif)
            .OrderBy(s => s.Ad)
            .ThenBy(s => s.Soyad)
            .ToListAsync();

        var result = new List<PersonelOzlukEvrakDurum>();
        foreach (var personel in personeller)
        {
            var durum = await GetPersonelEvrakDurumuAsync(personel.Id);
            result.Add(durum);
        }

        return result;
    }

    public async Task<PersonelOzlukEvrak> EvrakIsaretle(int soforId, int evrakTanimId, bool tamamlandi, string? aciklama = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.PersonelOzlukEvraklar
            .FirstOrDefaultAsync(e => e.SoforId == soforId && e.EvrakTanimId == evrakTanimId && !e.IsDeleted);

        if (existing != null)
        {
            existing.Tamamlandi = tamamlandi;
            existing.TamamlanmaTarihi = tamamlandi ? DateTime.UtcNow : null;
            existing.Aciklama = aciklama;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new PersonelOzlukEvrak
            {
                SoforId = soforId,
                EvrakTanimId = evrakTanimId,
                Tamamlandi = tamamlandi,
                TamamlanmaTarihi = tamamlandi ? DateTime.UtcNow : null,
                Aciklama = aciklama,
                CreatedAt = DateTime.UtcNow
            };
            context.PersonelOzlukEvraklar.Add(existing);
        }

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task<PersonelOzlukEvrak> EvrakDosyaYukle(int soforId, int evrakTanimId, string dosyaYolu,
        string? dosyaAdi = null, string? dosyaTipi = null, long? dosyaBoyutu = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.PersonelOzlukEvraklar
            .FirstOrDefaultAsync(e => e.SoforId == soforId && e.EvrakTanimId == evrakTanimId && !e.IsDeleted);

        if (existing != null)
        {
            // 🔴 Duplicate upload → update (overwrite YOK, versiyon artar)
            existing.VersiyonNo++;
            existing.SonDegisiklikNotu = $"Re-upload: {dosyaAdi}";
            existing.DosyaYolu = dosyaYolu;
            existing.DosyaAdi = dosyaAdi ?? existing.DosyaAdi;
            existing.DosyaTipi = dosyaTipi ?? existing.DosyaTipi;
            existing.DosyaBoyutu = dosyaBoyutu ?? existing.DosyaBoyutu;
            existing.Tamamlandi = true;
            existing.TamamlanmaTarihi = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;

            // Versiyon geçmişi kaydı
            context.PersonelOzlukEvrakVersiyonlar.Add(new PersonelOzlukEvrakVersiyon
            {
                PersonelOzlukEvrakId = existing.Id,
                VersiyonNo = existing.VersiyonNo,
                DosyaYolu = dosyaYolu,
                DosyaAdi = dosyaAdi,
                DosyaTipi = dosyaTipi,
                OlusturmaTarihi = DateTime.UtcNow
            });
        }
        else
        {
            existing = new PersonelOzlukEvrak
            {
                SoforId = soforId,
                EvrakTanimId = evrakTanimId,
                DosyaYolu = dosyaYolu,
                DosyaAdi = dosyaAdi,
                DosyaTipi = dosyaTipi,
                DosyaBoyutu = dosyaBoyutu,
                Tamamlandi = true,
                TamamlanmaTarihi = DateTime.UtcNow,
                VersiyonNo = 1,
                CreatedAt = DateTime.UtcNow
            };
            context.PersonelOzlukEvraklar.Add(existing);
        }

        await context.SaveChangesAsync();
        return existing;
    }

    /// <summary>
    /// Belge alan adına göre (Ehliyet, Kimlik, MykBelgesi, SrcBelgesi, Psikoteknik, AdliSicil, SaglikRaporu, SuruculCezaBarkod)
    /// ilgili özlük evrak tanımını bulup dosyayı yükler. Tanım yoksa otomatik oluşturur.
    /// </summary>
    public async Task<PersonelOzlukEvrak?> BelgeAlaniIleDosyaYukleAsync(int soforId, string belgeAlani, string dosyaYolu)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var (aramaKelimeleri, varsayilanAd, kategori) = belgeAlani switch
        {
            "Ehliyet" => (new[] { "Ehliyet" }, "Ehliyet Fotokopisi", OzlukEvrakKategori.SoforBelgeleri),
            "Kimlik" => (new[] { "Kimlik Fotokopisi", "Nüfus Cüzdanı" }, "Kimlik Fotokopisi", OzlukEvrakKategori.KimlikBelgeleri),
            "MykBelgesi" => (new[] { "MYK", "Mesleki Yeterlilik", "SRC" }, "MYK Belgesi", OzlukEvrakKategori.SoforBelgeleri),
            "Src" => (new[] { "MYK", "Mesleki Yeterlilik", "SRC" }, "MYK Belgesi", OzlukEvrakKategori.SoforBelgeleri),
            "SrcBelgesi" => (new[] { "SRC" }, "SRC Belgesi", OzlukEvrakKategori.SoforBelgeleri),
            "YayginEgitim" => (new[] { "Yaygın Eğitim", "Yaygin Egitim", "Yaygın Eğitim Sertifikası", "Yaygin Egitim Sertifikasi" }, "Yaygın Eğitim Sertifikası", OzlukEvrakKategori.EgitimBelgeleri),
            "Psikoteknik" => (new[] { "Psikoteknik" }, "Psikoteknik Belgesi", OzlukEvrakKategori.SoforBelgeleri),
            "AdliSicil" => (new[] { "Adli Sicil", "Sabıka" }, "Adli Sicil Kaydı", OzlukEvrakKategori.KimlikBelgeleri),
            "SaglikRaporu" => (new[] { "Sağlık Rapor", "Saglik Rapor" }, "Sağlık Raporu", OzlukEvrakKategori.SaglikBelgeleri),
            "SuruculCezaBarkod" => (new[] { "Sürücü Ceza", "Ceza Barkod" }, "Sürücü Ceza Barkodlu Belge", OzlukEvrakKategori.SoforBelgeleri),
            _ => (Array.Empty<string>(), string.Empty, OzlukEvrakKategori.Diger)
        };

        if (aramaKelimeleri.Length == 0) return null;

        var tanim = await context.OzlukEvrakTanimlari
            .Where(t => !t.IsDeleted && t.Aktif)
            .ToListAsync();

        var eslesen = tanim.FirstOrDefault(t =>
            aramaKelimeleri.Any(k => t.EvrakAdi.Contains(k, StringComparison.OrdinalIgnoreCase)));

        if (eslesen == null)
        {
            // Tanım yoksa otomatik oluştur
            eslesen = new OzlukEvrakTanim
            {
                EvrakAdi = varsayilanAd,
                Kategori = kategori,
                SiraNo = 99,
                Zorunlu = true,
                Aktif = true,
                CreatedAt = DateTime.UtcNow
            };
            context.OzlukEvrakTanimlari.Add(eslesen);
            await context.SaveChangesAsync();
        }

        return await EvrakDosyaYukle(soforId, eslesen.Id, dosyaYolu);
    }

    public async Task<PersonelOzlukEvrak> UpdatePersonelEvrakAsync(PersonelOzlukEvrak evrak)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.PersonelOzlukEvraklar
            .FirstOrDefaultAsync(e => e.Id == evrak.Id && !e.IsDeleted);

        if (existing == null)
            throw new InvalidOperationException("Personel evrak kaydı bulunamadı.");

        existing.Tamamlandi = evrak.Tamamlandi;
        existing.TamamlanmaTarihi = evrak.Tamamlandi
            ? (evrak.TamamlanmaTarihi.HasValue ? DateTime.SpecifyKind(evrak.TamamlanmaTarihi.Value, DateTimeKind.Utc) : DateTime.UtcNow)
            : null;
        existing.GecerlilikBitisTarihi = evrak.GecerlilikBitisTarihi.HasValue
            ? DateTime.SpecifyKind(evrak.GecerlilikBitisTarihi.Value, DateTimeKind.Utc)
            : null;
        existing.Aciklama = evrak.Aciklama;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return existing;
    }

    public async Task SoforBelgeTarihleriniSenkronizeEtAsync(int soforId, DateTime? ehliyetTarihi, DateTime? srcTarihi, DateTime? psikoteknikTarihi, DateTime? saglikTarihi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Evrak adına göre eşleşen özlük evraklarını bul ve GecerlilikBitisTarihi'ni güncelle
        var tanimlari = await context.OzlukEvrakTanimlari
            .Where(t => !t.IsDeleted && t.Aktif)
            .ToListAsync();

        var evraklar = await context.PersonelOzlukEvraklar
            .Where(e => e.SoforId == soforId && !e.IsDeleted)
            .ToListAsync();

        var updates = new List<(string araKelime, DateTime? tarih)>
        {
            ("Ehliyet", ehliyetTarihi),
            ("SRC", srcTarihi),
            ("Psikoteknik", psikoteknikTarihi),
            ("Sağlık", saglikTarihi),
            ("Saglik", saglikTarihi),
        };

        foreach (var (araKelime, tarih) in updates)
        {
            var eslesenTanimlar = tanimlari
                .Where(t => t.EvrakAdi.Contains(araKelime, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var tanim in eslesenTanimlar)
            {
                var evrak = evraklar.FirstOrDefault(e => e.EvrakTanimId == tanim.Id);
                if (evrak == null)
                {
                    if (tarih.HasValue)
                    {
                        evrak = new PersonelOzlukEvrak
                        {
                            SoforId = soforId,
                            EvrakTanimId = tanim.Id,
                            Tamamlandi = true,
                            TamamlanmaTarihi = DateTime.UtcNow,
                            GecerlilikBitisTarihi = DateTime.SpecifyKind(tarih.Value.Date, DateTimeKind.Utc),
                            CreatedAt = DateTime.UtcNow
                        };
                        context.PersonelOzlukEvraklar.Add(evrak);
                        evraklar.Add(evrak);
                    }
                }
                else
                {
                    evrak.GecerlilikBitisTarihi = tarih.HasValue
                        ? DateTime.SpecifyKind(tarih.Value.Date, DateTimeKind.Utc)
                        : null;
                    evrak.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<PersonelOzlukEvrakDurum>> GetEksikEvrakliPersonellerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var tumDurumlar = await GetTumPersonelEvrakDurumlariAsync();
        return tumDurumlar.Where(d => d.EksikEvrak > 0).OrderByDescending(d => d.EksikEvrak).ToList();
    }

    public async Task<byte[]> ExportChecklistPdfAsync(int soforId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var durum = await GetPersonelEvrakDurumuAsync(soforId);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text($"PERSONEL ÖZLÜK DOSYASI EVRAK KONTROL LİSTESİ")
                        .FontSize(14).Bold().AlignCenter();
                    col.Item().Text($"{durum.PersonelAdi} ({durum.PersonelKodu})")
                        .FontSize(12).AlignCenter();
                    col.Item().Text($"Görev: {GetGorevAdi(durum.Gorev)} | Tarih: {DateTime.Now:dd.MM.yyyy}")
                        .FontSize(10).AlignCenter();
                    col.Item().PaddingBottom(10);
                });

                page.Content().Column(col =>
                {
                    // Özet
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Background("#e8f5e9").Padding(5).Column(c =>
                        {
                            c.Item().Text("Tamamlanan").Bold();
                            c.Item().Text($"{durum.TamamlananEvrak} / {durum.ToplamEvrak}");
                        });
                        row.RelativeItem().Background("#ffebee").Padding(5).Column(c =>
                        {
                            c.Item().Text("Eksik").Bold();
                            c.Item().Text($"{durum.EksikEvrak}");
                        });
                        row.RelativeItem().Background("#e3f2fd").Padding(5).Column(c =>
                        {
                            c.Item().Text("Tamamlanma").Bold();
                            c.Item().Text($"%{durum.TamamlanmaYuzdesi}");
                        });
                    });

                    col.Item().PaddingVertical(10);

                    // Evrak listesi kategorilere göre
                    var kategoriler = durum.Evraklar.GroupBy(e => e.Kategori).OrderBy(g => g.Key);
                    foreach (var kategori in kategoriler)
                    {
                        col.Item().Background("#f5f5f5").Padding(5)
                            .Text(GetKategoriAdi(kategori.Key)).Bold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(25);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            foreach (var evrak in kategori.OrderBy(e => e.EvrakAdi))
                            {
                                table.Cell().Border(1).Padding(3).AlignCenter()
                                    .Text(evrak.Tamamlandi ? "✓" : "☐");
                                table.Cell().Border(1).Padding(3)
                                    .Text($"{evrak.EvrakAdi}{(evrak.Zorunlu ? " *" : "")}");
                                table.Cell().Border(1).Padding(3).AlignCenter()
                                    .Text(evrak.Tamamlandi ? "Tamam" : "Eksik");
                                table.Cell().Border(1).Padding(3).AlignCenter()
                                    .Text(evrak.TamamlanmaTarihi?.ToString("dd.MM.yyyy") ?? "-");
                            }
                        });

                        col.Item().PaddingVertical(5);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Sayfa ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> ExportTumChecklistExcelAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var durumlar = await GetTumPersonelEvrakDurumlariAsync();
        var evrakTanimlari = await GetAktifEvrakTanimlariAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();

        // Özet Sayfa
        var wsOzet = workbook.Worksheets.Add("Özet");
        wsOzet.Cell(1, 1).Value = "Personel Kodu";
        wsOzet.Cell(1, 2).Value = "Personel Adı";
        wsOzet.Cell(1, 3).Value = "Görev";
        wsOzet.Cell(1, 4).Value = "Toplam Evrak";
        wsOzet.Cell(1, 5).Value = "Tamamlanan";
        wsOzet.Cell(1, 6).Value = "Eksik";
        wsOzet.Cell(1, 7).Value = "Tamamlanma %";
        wsOzet.Range(1, 1, 1, 7).Style.Font.Bold = true;
        wsOzet.Range(1, 1, 1, 7).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGreen;

        int row = 2;
        foreach (var durum in durumlar)
        {
            wsOzet.Cell(row, 1).Value = durum.PersonelKodu;
            wsOzet.Cell(row, 2).Value = durum.PersonelAdi;
            wsOzet.Cell(row, 3).Value = GetGorevAdi(durum.Gorev);
            wsOzet.Cell(row, 4).Value = durum.ToplamEvrak;
            wsOzet.Cell(row, 5).Value = durum.TamamlananEvrak;
            wsOzet.Cell(row, 6).Value = durum.EksikEvrak;
            wsOzet.Cell(row, 7).Value = durum.TamamlanmaYuzdesi;

            if (durum.EksikEvrak > 0)
                wsOzet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightPink;

            row++;
        }
        wsOzet.Columns().AdjustToContents();

        // Detay Sayfa
        var wsDetay = workbook.Worksheets.Add("Detay");
        wsDetay.Cell(1, 1).Value = "Personel Kodu";
        wsDetay.Cell(1, 2).Value = "Personel Adı";
        wsDetay.Cell(1, 3).Value = "Kategori";
        wsDetay.Cell(1, 4).Value = "Evrak Adı";
        wsDetay.Cell(1, 5).Value = "Zorunlu";
        wsDetay.Cell(1, 6).Value = "Durum";
        wsDetay.Cell(1, 7).Value = "Tamamlanma Tarihi";
        wsDetay.Range(1, 1, 1, 7).Style.Font.Bold = true;
        wsDetay.Range(1, 1, 1, 7).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightBlue;

        row = 2;
        foreach (var durum in durumlar)
        {
            foreach (var evrak in durum.Evraklar)
            {
                wsDetay.Cell(row, 1).Value = durum.PersonelKodu;
                wsDetay.Cell(row, 2).Value = durum.PersonelAdi;
                wsDetay.Cell(row, 3).Value = GetKategoriAdi(evrak.Kategori);
                wsDetay.Cell(row, 4).Value = evrak.EvrakAdi;
                wsDetay.Cell(row, 5).Value = evrak.Zorunlu ? "Evet" : "Hayır";
                wsDetay.Cell(row, 6).Value = evrak.Tamamlandi ? "Tamam" : "Eksik";
                wsDetay.Cell(row, 7).Value = evrak.TamamlanmaTarihi?.ToString("dd.MM.yyyy") ?? "";

                if (!evrak.Tamamlandi)
                    wsDetay.Range(row, 1, row, 7).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightPink;

                row++;
            }
        }
        wsDetay.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportPersonelDosyaPdfAsync(int soforId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var personel = await context.Soforler.FirstOrDefaultAsync(s => s.Id == soforId && !s.IsDeleted);
        if (personel == null)
            throw new InvalidOperationException("Personel bulunamadı.");

        var evrakTanimlari = await GetGecerliEvrakTanimlariAsync(context, personel.Gorev);
        return GeneratePersonelDosyaPdf(personel, evrakTanimlari, true);
    }

    public async Task<byte[]> ExportBosPersonelDosyaPdfAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var evrakTanimlari = await GetAktifEvrakTanimlariAsync();
        return GeneratePersonelDosyaPdf(null, evrakTanimlari, false);
    }

    private static byte[] GeneratePersonelDosyaPdf(Sofor? personel, List<OzlukEvrakTanim> evrakTanimlari, bool personelBilgili)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(personelBilgili ? "PERSONEL DOSYASI HAZIRLIK FORMU" : "BOŞ PERSONEL DOSYASI HAZIRLIK FORMU")
                        .FontSize(15).Bold().AlignCenter();
                    col.Item().Text($"Tarih: {DateTime.Now:dd.MM.yyyy}").FontSize(9).AlignRight();
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(10).Text("1. SAYFA - PERSONEL BİLGİLERİ").Bold().FontSize(12);
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(2.8f);
                        });

                        void AddRow(string label, string? value = null)
                        {
                            table.Cell().Border(1).Padding(5).Background("#f8f9fa").Text(label).SemiBold();
                            table.Cell().Border(1).Padding(5).MinHeight(24).Text(value ?? string.Empty);
                        }

                        AddRow("Personel Kodu", personelBilgili ? personel?.SoforKodu : null);
                        AddRow("Ad Soyad", personelBilgili ? personel?.TamAd : null);
                        AddRow("T.C. Kimlik No", personelBilgili ? personel?.TcKimlikNo : null);
                        AddRow("Telefon", personelBilgili ? personel?.Telefon : null);
                        AddRow("E-Posta", personelBilgili ? personel?.Email : null);
                        AddRow("Adres", personelBilgili ? personel?.Adres : null);
                        AddRow("Görev", personelBilgili && personel != null ? GetGorevAdi(personel.Gorev) : null);
                        AddRow("Departman", personelBilgili ? personel?.Departman : null);
                        AddRow("Pozisyon", personelBilgili ? personel?.Pozisyon : null);
                        AddRow("İşe Başlama Tarihi", personelBilgili ? personel?.IseBaslamaTarihi?.ToString("dd.MM.yyyy") : null);
                        AddRow("Ehliyet No", personelBilgili ? personel?.EhliyetNo : null);
                        AddRow("Ehliyet Geçerlilik", personelBilgili ? personel?.EhliyetGecerlilikTarihi?.ToString("dd.MM.yyyy") : null);
                        AddRow("SRC Belgesi", personelBilgili ? ((personel?.YayginEgitimSertifikasiVarMi == true || personel?.SrcBelgesiGecerlilikTarihi.HasValue == true) ? "Var" : "Yok") : null);
                        AddRow("Psikoteknik Geçerlilik", personelBilgili ? personel?.PsikoteknikGecerlilikTarihi?.ToString("dd.MM.yyyy") : null);
                        AddRow("Sağlık Raporu Geçerlilik", personelBilgili ? personel?.SaglikRaporuGecerlilikTarihi?.ToString("dd.MM.yyyy") : null);
                        AddRow("Net Maaş", personelBilgili ? personel?.NetMaas.ToString("N2") : null);
                        AddRow("IBAN", personelBilgili ? personel?.IBAN : null);
                        AddRow("Notlar", personelBilgili ? personel?.Notlar : null);
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Sayfa ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.1f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("2. SAYFA - ÖZLÜK EVRAK CHECKLIST").FontSize(13).Bold().AlignCenter();
                    if (personelBilgili && personel != null)
                        col.Item().Text($"{personel.TamAd} ({personel.SoforKodu})").AlignCenter();
                });

                page.Content().Column(col =>
                {
                    foreach (var kategori in evrakTanimlari.GroupBy(e => e.Kategori).OrderBy(g => g.Key))
                    {
                        col.Item().PaddingTop(6).Text($"{GetKategoriAdi(kategori.Key)}:").Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(5);
                                columns.RelativeColumn(0.8f);
                                columns.RelativeColumn(0.8f);
                                columns.RelativeColumn(1.4f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Border(1).Background("#e9ecef").Padding(4).Text("Evrak").SemiBold();
                                header.Cell().Border(1).Background("#e9ecef").Padding(4).AlignCenter().Text("Var/Yok").SemiBold();
                                header.Cell().Border(1).Background("#e9ecef").Padding(4).AlignCenter().Text("Muaf").SemiBold();
                                header.Cell().Border(1).Background("#e9ecef").Padding(4).AlignCenter().Text("Açıklama").SemiBold();
                            });

                            foreach (var evrak in kategori.OrderBy(e => e.SiraNo).ThenBy(e => e.EvrakAdi))
                            {
                                table.Cell().Border(1).Padding(4).MinHeight(20).Text(evrak.EvrakAdi);
                                table.Cell().Border(1).Padding(4).MinHeight(20).Text(string.Empty);
                                table.Cell().Border(1).Padding(4).MinHeight(20).Text(string.Empty);
                                table.Cell().Border(1).Padding(4).MinHeight(20).Text(string.Empty);
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Sayfa ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    #endregion

    #region Yardımcı Metodlar

    private async Task<List<OzlukEvrakTanim>> GetGecerliEvrakTanimlariAsync(ApplicationDbContext context, PersonelGorev gorev)
    {
        // Görev ayrımı kaldırıldı: Özlük belgeleri tüm personeller için aynı.
        // Eski "GecerliGorevler" kısıtlaması bilinçli olarak yok sayılıyor.
        return await GetAktifEvrakTanimlariAsync();
    }

    private static string GetGorevAdi(PersonelGorev gorev) => gorev switch
    {
        PersonelGorev.Sofor => "Şoför",
        PersonelGorev.OfisCalisani => "Ofis Çalışanı",
        PersonelGorev.Muhasebe => "Muhasebe",
        PersonelGorev.Yonetici => "Yönetici",
        PersonelGorev.Teknik => "Teknik",
        PersonelGorev.Diger => "Diğer",
        _ => "Bilinmiyor"
    };

    private static string GetKategoriAdi(OzlukEvrakKategori kategori) => kategori switch
    {
        OzlukEvrakKategori.Genel => "Genel",
        OzlukEvrakKategori.KimlikBelgeleri => "Kimlik Belgeleri",
        OzlukEvrakKategori.EgitimBelgeleri => "Eğitim Belgeleri",
        OzlukEvrakKategori.SaglikBelgeleri => "Sağlık Belgeleri",
        OzlukEvrakKategori.SoforBelgeleri => "Şoför Belgeleri",
        OzlukEvrakKategori.SGKBelgeleri => "SGK Belgeleri",
        OzlukEvrakKategori.IseGirisBelgeleri => "İşe Giriş Belgeleri",
        OzlukEvrakKategori.Diger => "Diğer",
        _ => "Bilinmiyor"
    };

    #endregion
}
