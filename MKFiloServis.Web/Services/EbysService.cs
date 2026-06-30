using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using MKFiloServis.Web.Services.Interfaces;

namespace MKFiloServis.Web.Services;

public class EbysService : IEbysService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ISecureFileService _secureFileService;
    private readonly IAracService _aracService;
    private readonly IPersonelOzlukService _personelOzlukService;

    public EbysService(IDbContextFactory<ApplicationDbContext> contextFactory, ISecureFileService secureFileService, IAracService aracService, IPersonelOzlukService personelOzlukService)
    {
        _contextFactory = contextFactory;
        _secureFileService = secureFileService;
        _aracService = aracService;
        _personelOzlukService = personelOzlukService;
    }

    public async Task<List<EbysBelgeKaydi>> GetBelgeKayitlariAsync(EbysBelgeListeFiltre filtre)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kayitlar = await GetTumBelgeKayitlariAsync();

        if (filtre.SadeceDosyasiOlanlar)
        {
            kayitlar = kayitlar.Where(x => x.DosyaVar).ToList();
        }

        if (filtre.Kaynak != EbysBelgeKaynakFiltre.Tumu)
        {
            kayitlar = kayitlar.Where(x => (int)x.Kaynak == (int)filtre.Kaynak).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filtre.AramaMetni))
        {
            var arama = filtre.AramaMetni.Trim();
            kayitlar = kayitlar
                .Where(x => BelgeAramadaEslesiyor(x, arama))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(filtre.Kategori))
        {
            kayitlar = kayitlar
                .Where(x => x.Kategori.Equals(filtre.Kategori, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        kayitlar = filtre.Risk switch
        {
            EbysBelgeRiskFiltre.Risksiz => kayitlar.Where(x => x.RiskDurumu == "Normal").ToList(),
            EbysBelgeRiskFiltre.Yaklasan => kayitlar.Where(x => x.YaklasanMi).ToList(),
            EbysBelgeRiskFiltre.SuresiDolmus => kayitlar.Where(x => x.SuresiDolmusMu).ToList(),
            EbysBelgeRiskFiltre.DosyaEksik => kayitlar.Where(x => !x.DosyaVar).ToList(),
            _ => kayitlar
        };

        return kayitlar
            .OrderByDescending(x => x.BelgeTarihi ?? DateTime.MinValue)
            .ThenBy(x => x.IlgiliKayitAdi)
            .ToList();
    }

    public async Task<List<string>> GetKategorilerAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kayitlar = await GetTumBelgeKayitlariAsync();

        return kayitlar
            .Select(x => x.Kategori)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    public async Task<List<EbysKategoriOzet>> GetKategoriOzetleriAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kayitlar = await GetTumBelgeKayitlariAsync();

        return kayitlar
            .GroupBy(x => x.Kategori)
            .Select(g => new EbysKategoriOzet
            {
                Kategori = g.Key,
                ToplamKayit = g.Count(),
                PersonelSayisi = g.Count(x => x.Kaynak == EbysBelgeKaynak.Personel),
                AracSayisi = g.Count(x => x.Kaynak == EbysBelgeKaynak.Arac),
                DosyaliKayit = g.Count(x => x.DosyaVar),
                RiskliKayit = g.Count(x => x.SuresiDolmusMu || x.YaklasanMi || !x.DosyaVar)
            })
            .OrderByDescending(x => x.ToplamKayit)
            .ThenBy(x => x.Kategori)
            .ToList();
    }

    public async Task<EbysBelgeOlusturmaSecenekleri> GetBelgeOlusturmaSecenekleriAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var personeller = await context.Soforler
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Aktif)
            .OrderBy(x => x.Ad)
            .ThenBy(x => x.Soyad)
            .Select(x => new EbysSecimItem
            {
                Id = x.Id,
                Ad = x.TamAd,
                Kod = x.SoforKodu
            })
            .ToListAsync();

        var araclar = await context.Araclar
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Aktif)
            .OrderBy(x => x.AktifPlaka)
            .ThenBy(x => x.SaseNo)
            .Select(x => new EbysSecimItem
            {
                Id = x.Id,
                Ad = x.AktifPlaka ?? x.SaseNo ?? "Araç",
                Kod = x.SaseNo
            })
            .ToListAsync();

        var evrakTanimlari = await context.OzlukEvrakTanimlari
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Aktif)
            .OrderBy(x => x.Kategori)
            .ThenBy(x => x.SiraNo)
            .Select(x => new EbysSecimItem
            {
                Id = x.Id,
                Ad = x.EvrakAdi,
                Kod = GetOzlukKategoriAdi(x.Kategori)
            })
            .ToListAsync();

        return new EbysBelgeOlusturmaSecenekleri
        {
            Personeller = personeller,
            Araclar = araclar,
            PersonelEvrakTanimlari = evrakTanimlari,
            AracKategorileri = [.. EvrakKategorileri.TumKategoriler]
        };
    }

    public async Task<EbysBelgeKaydi> BelgeOlusturAsync(EbysBelgeOlusturmaModeli model)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return model.Kaynak switch
        {
            EbysBelgeKaynak.Personel => await CreatePersonelBelgeAsync(model),
            EbysBelgeKaynak.Arac => await CreateAracBelgeAsync(model),
            _ => throw new InvalidOperationException("Geçersiz belge kaynağı.")
        };
    }

    private async Task<List<EbysBelgeKaydi>> GetTumBelgeKayitlariAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var kayitlar = new List<EbysBelgeKaydi>();

        var personelEvraklari = await context.PersonelOzlukEvraklar
            .AsNoTracking()
            .Include(x => x.Sofor)
            .Include(x => x.EvrakTanim)
            .Where(x => !x.IsDeleted && !x.Sofor.IsDeleted)
            .ToListAsync();

        kayitlar.AddRange(personelEvraklari.Select(x => new EbysBelgeKaydi
        {
            Kaynak = EbysBelgeKaynak.Personel,
            BelgeId = x.Id,
            BelgeAdi = x.EvrakTanim.EvrakAdi,
            Kategori = GetOzlukKategoriAdi(x.EvrakTanim.Kategori),
            IlgiliKayitAdi = x.Sofor.TamAd,
            IlgiliKayitKodu = x.Sofor.SoforKodu,
            Durum = x.Tamamlandi ? "Tamamlandı" : "Eksik",
            Aciklama = x.Aciklama,
            DosyaVar = !string.IsNullOrWhiteSpace(x.DosyaYolu),
            DosyaAdi = !string.IsNullOrWhiteSpace(x.DosyaYolu)
                ? BuildPersonelDosyaAdi(x.EvrakTanim.EvrakAdi, x.DosyaYolu)
                : null,
            BelgeTarihi = x.TamamlanmaTarihi,
            RiskDurumu = !string.IsNullOrWhiteSpace(x.DosyaYolu) ? "Normal" : "Dosya Eksik",
            KaynakDetayUrl = "/personel/ozluk-evrak"
        }));

        var aracEvraklari = await context.AracEvraklari
            .AsNoTracking()
            .Include(x => x.Arac)
            .Include(x => x.Dosyalar.Where(d => !d.IsDeleted))
            .Where(x => !x.IsDeleted && x.Arac != null && !x.Arac.IsDeleted)
            .ToListAsync();

        kayitlar.AddRange(aracEvraklari.Select(x =>
        {
            var aktifDosya = x.Dosyalar
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefault();

            var suresiDolmus = x.BitisTarihi.HasValue && x.BitisTarihi.Value.Date < DateTime.Today;
            var yaklasan = !suresiDolmus && x.BitisTarihi.HasValue && x.BitisTarihi.Value.Date <= DateTime.Today.AddDays(30);
            var riskDurumu = aktifDosya == null
                ? "Dosya Eksik"
                : suresiDolmus
                    ? "Süresi Dolmuş"
                    : yaklasan
                        ? "Yaklaşan"
                        : "Normal";

            return new EbysBelgeKaydi
            {
                Kaynak = EbysBelgeKaynak.Arac,
                BelgeId = x.Id,
                DosyaId = aktifDosya?.Id,
                BelgeAdi = string.IsNullOrWhiteSpace(x.EvrakAdi) ? x.EvrakKategorisi : x.EvrakAdi,
                Kategori = x.EvrakKategorisi,
                IlgiliKayitAdi = x.Arac?.AktifPlaka ?? x.Arac?.SaseNo ?? "Araç",
                IlgiliKayitKodu = x.Arac?.SaseNo ?? string.Empty,
                Durum = GetAracDurumAdi(x.Durum),
                Aciklama = x.Aciklama,
                DosyaVar = aktifDosya != null,
                DosyaAdi = aktifDosya?.DosyaAdi,
                BelgeTarihi = x.BitisTarihi ?? x.CreatedAt,
                BitisTarihi = x.BitisTarihi,
                RiskDurumu = riskDurumu,
                YaklasanMi = yaklasan,
                SuresiDolmusMu = suresiDolmus,
                KaynakDetayUrl = x.AracId > 0 ? $"/araclar/{x.AracId}/evraklar" : "/araclar"
            };
        }));

        return kayitlar;
    }

    public async Task<EbysBelgeDosya?> GetBelgeDosyasiAsync(EbysBelgeKaynak kaynak, int belgeId, int? dosyaId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return kaynak switch
        {
            EbysBelgeKaynak.Personel => await GetPersonelBelgeDosyasiAsync(belgeId),
            EbysBelgeKaynak.Arac => await GetAracBelgeDosyasiAsync(belgeId, dosyaId),
            _ => null
        };
    }

    public async Task<EbysBelgeDuzenlemeModeli?> GetBelgeDuzenlemeModeliAsync(EbysBelgeKaynak kaynak, int belgeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return kaynak switch
        {
            EbysBelgeKaynak.Personel => await GetPersonelBelgeDuzenlemeModeliAsync(belgeId),
            EbysBelgeKaynak.Arac => await GetAracBelgeDuzenlemeModeliAsync(belgeId),
            _ => null
        };
    }

    public async Task BelgeGuncelleAsync(EbysBelgeDuzenlemeModeli model)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        switch (model.Kaynak)
        {
            case EbysBelgeKaynak.Personel:
                await UpdatePersonelBelgeAsync(model);
                break;
            case EbysBelgeKaynak.Arac:
                await UpdateAracBelgeAsync(model);
                break;
        }
    }

    public async Task BelgeDosyasiYukleAsync(EbysBelgeKaynak kaynak, int belgeId, IBrowserFile file)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        switch (kaynak)
        {
            case EbysBelgeKaynak.Personel:
                await UploadPersonelBelgeDosyasiAsync(belgeId, file);
                break;
            case EbysBelgeKaynak.Arac:
                await _aracService.UploadEvrakDosyaAsync(belgeId, file);
                break;
        }
    }

    private async Task<EbysBelgeDosya?> GetPersonelBelgeDosyasiAsync(int belgeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var belge = await context.PersonelOzlukEvraklar
            .AsNoTracking()
            .Include(x => x.EvrakTanim)
            .FirstOrDefaultAsync(x => x.Id == belgeId && !x.IsDeleted);

        if (belge == null || string.IsNullOrWhiteSpace(belge.DosyaYolu))
        {
            return null;
        }

        var icerik = await _secureFileService.ReadDecryptedAsync(belge.DosyaYolu);
        if (icerik == null)
        {
            return null;
        }

        var dosyaAdi = BuildPersonelDosyaAdi(belge.EvrakTanim.EvrakAdi, belge.DosyaYolu);
        return new EbysBelgeDosya
        {
            DosyaAdi = dosyaAdi,
            MimeTipi = GetMimeType(dosyaAdi),
            Icerik = icerik
        };
    }

    private async Task<EbysBelgeDosya?> GetAracBelgeDosyasiAsync(int belgeId, int? dosyaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var belge = await context.AracEvraklari
            .AsNoTracking()
            .Include(x => x.Dosyalar.Where(d => !d.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == belgeId && !x.IsDeleted);

        if (belge == null)
        {
            return null;
        }

        var hedefDosya = dosyaId.HasValue
            ? belge.Dosyalar.FirstOrDefault(x => x.Id == dosyaId.Value)
            : belge.Dosyalar.OrderByDescending(x => x.CreatedAt).FirstOrDefault();

        if (hedefDosya == null)
        {
            return null;
        }

        var icerik = await _aracService.GetEvrakDosyaAsync(hedefDosya.Id);
        return new EbysBelgeDosya
        {
            DosyaAdi = hedefDosya.DosyaAdi,
            MimeTipi = GetMimeType(hedefDosya.DosyaAdi),
            Icerik = icerik
        };
    }

    private async Task<EbysBelgeDuzenlemeModeli?> GetPersonelBelgeDuzenlemeModeliAsync(int belgeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var belge = await _personelOzlukService.GetPersonelEvrakByIdAsync(belgeId);
        if (belge == null)
        {
            return null;
        }

        return new EbysBelgeDuzenlemeModeli
        {
            Kaynak = EbysBelgeKaynak.Personel,
            BelgeId = belge.Id,
            BelgeAdi = belge.EvrakTanim.EvrakAdi,
            Kategori = GetOzlukKategoriAdi(belge.EvrakTanim.Kategori),
            Aciklama = belge.Aciklama,
            BelgeTarihi = belge.TamamlanmaTarihi,
            Tamamlandi = belge.Tamamlandi,
            Durum = belge.Tamamlandi ? "Tamamlandı" : "Eksik",
            DosyaVar = !string.IsNullOrWhiteSpace(belge.DosyaYolu),
            TarihDuzenlenebilir = true
        };
    }

    private async Task<EbysBelgeDuzenlemeModeli?> GetAracBelgeDuzenlemeModeliAsync(int belgeId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var belge = await _aracService.GetAracEvrakByIdAsync(belgeId);
        if (belge == null)
        {
            return null;
        }

        return new EbysBelgeDuzenlemeModeli
        {
            Kaynak = EbysBelgeKaynak.Arac,
            BelgeId = belge.Id,
            BelgeAdi = string.IsNullOrWhiteSpace(belge.EvrakAdi) ? belge.EvrakKategorisi : belge.EvrakAdi,
            Kategori = belge.EvrakKategorisi,
            Aciklama = belge.Aciklama,
            BelgeTarihi = belge.BaslangicTarihi,
            BitisTarihi = belge.BitisTarihi,
            Durum = GetAracDurumAdi(belge.Durum),
            DosyaVar = belge.Dosyalar.Any(x => !x.IsDeleted),
            BelgeAdiDuzenlenebilir = true,
            KategoriDuzenlenebilir = true,
            DurumDuzenlenebilir = true,
            TarihDuzenlenebilir = true,
            BitisTarihiDuzenlenebilir = true
        };
    }

    private async Task UpdatePersonelBelgeAsync(EbysBelgeDuzenlemeModeli model)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var belge = await _personelOzlukService.GetPersonelEvrakByIdAsync(model.BelgeId)
            ?? throw new InvalidOperationException("Personel belge kaydı bulunamadı.");

        belge.Tamamlandi = model.Tamamlandi;
        belge.TamamlanmaTarihi = model.Tamamlandi
            ? (model.BelgeTarihi?.Date ?? DateTime.UtcNow.Date)
            : null;
        belge.Aciklama = model.Aciklama;

        await _personelOzlukService.UpdatePersonelEvrakAsync(belge);
    }

    private async Task UpdateAracBelgeAsync(EbysBelgeDuzenlemeModeli model)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var belge = await _aracService.GetAracEvrakByIdAsync(model.BelgeId)
            ?? throw new InvalidOperationException("Araç belge kaydı bulunamadı.");

        belge.EvrakAdi = string.IsNullOrWhiteSpace(model.BelgeAdi) ? null : model.BelgeAdi.Trim();
        belge.EvrakKategorisi = string.IsNullOrWhiteSpace(model.Kategori) ? belge.EvrakKategorisi : model.Kategori.Trim();
        belge.Aciklama = model.Aciklama;
        belge.BaslangicTarihi = model.BelgeTarihi?.Date;
        belge.BitisTarihi = model.BitisTarihi?.Date;
        belge.Durum = model.Durum switch
        {
            "Aktif" => EvrakDurum.Aktif,
            "Pasif" => EvrakDurum.Pasif,
            "Süresi Dolmuş" => EvrakDurum.SuresiDolmus,
            _ => EvrakDurum.Aktif
        };

        await _aracService.UpdateAracEvrakAsync(belge);
    }

    private async Task UploadPersonelBelgeDosyasiAsync(int belgeId, IBrowserFile file)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var belge = await _personelOzlukService.GetPersonelEvrakByIdAsync(belgeId)
            ?? throw new InvalidOperationException("Personel belge kaydı bulunamadı.");

        if (!string.IsNullOrWhiteSpace(belge.DosyaYolu))
        {
            await _secureFileService.DeleteAsync(belge.DosyaYolu);
        }

        await using var stream = file.OpenReadStream(10 * 1024 * 1024);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        var relativePath = await _secureFileService.SaveEncryptedAsync(
            $"personel-ozluk/{belge.SoforId}",
            file.Name,
            memoryStream.ToArray());

        await _personelOzlukService.EvrakDosyaYukle(belge.SoforId, belge.EvrakTanimId, relativePath);
    }

    private async Task<EbysBelgeKaydi> CreatePersonelBelgeAsync(EbysBelgeOlusturmaModeli model)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (!model.IlgiliKayitId.HasValue || !model.EvrakTanimId.HasValue)
            throw new InvalidOperationException("Personel ve evrak seçimi zorunludur.");

        var personel = await context.Soforler
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == model.IlgiliKayitId.Value && !x.IsDeleted);
        if (personel == null)
            throw new InvalidOperationException("Personel bulunamadı.");

        var evrakTanim = await context.OzlukEvrakTanimlari
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == model.EvrakTanimId.Value && !x.IsDeleted && x.Aktif);
        if (evrakTanim == null)
            throw new InvalidOperationException("Evrak tanımı bulunamadı.");

        if (!string.IsNullOrWhiteSpace(evrakTanim.GecerliGorevler))
        {
            var gorevler = evrakTanim.GecerliGorevler.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!gorevler.Contains(((int)personel.Gorev).ToString(), StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Seçilen evrak bu personel görevi için geçerli değil.");
        }

        var mevcutKayit = await context.PersonelOzlukEvraklar
            .AsNoTracking()
            .AnyAsync(x => x.SoforId == model.IlgiliKayitId.Value && x.EvrakTanimId == model.EvrakTanimId.Value && !x.IsDeleted);
        if (mevcutKayit)
            throw new InvalidOperationException("Seçilen personel için bu evrak zaten mevcut.");

        var belge = new PersonelOzlukEvrak
        {
            SoforId = model.IlgiliKayitId.Value,
            EvrakTanimId = model.EvrakTanimId.Value,
            Tamamlandi = model.Tamamlandi,
            TamamlanmaTarihi = model.Tamamlandi ? (model.BelgeTarihi?.Date ?? DateTime.UtcNow.Date) : null,
            Aciklama = model.Aciklama,
            CreatedAt = DateTime.UtcNow
        };

        context.PersonelOzlukEvraklar.Add(belge);
        await context.SaveChangesAsync();

        var kayitlar = await GetBelgeKayitlariAsync(new EbysBelgeListeFiltre { Kaynak = EbysBelgeKaynakFiltre.Personel });
        return kayitlar.First(x => x.Kaynak == EbysBelgeKaynak.Personel && x.BelgeId == belge.Id);
    }

    private async Task<EbysBelgeKaydi> CreateAracBelgeAsync(EbysBelgeOlusturmaModeli model)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (!model.IlgiliKayitId.HasValue)
            throw new InvalidOperationException("Araç seçimi zorunludur.");

        if (string.IsNullOrWhiteSpace(model.Kategori))
            throw new InvalidOperationException("Kategori seçimi zorunludur.");

        var belge = await _aracService.CreateAracEvrakAsync(new AracEvrak
        {
            AracId = model.IlgiliKayitId.Value,
            EvrakKategorisi = model.Kategori.Trim(),
            EvrakAdi = string.IsNullOrWhiteSpace(model.BelgeAdi) ? null : model.BelgeAdi.Trim(),
            Aciklama = model.Aciklama,
            BaslangicTarihi = model.BelgeTarihi?.Date,
            BitisTarihi = model.BitisTarihi?.Date,
            Durum = model.Durum switch
            {
                "Pasif" => EvrakDurum.Pasif,
                "Süresi Dolmuş" => EvrakDurum.SuresiDolmus,
                _ => EvrakDurum.Aktif
            },
            HatirlatmaAktif = true,
            HatirlatmaGunOnce = 15
        });

        var kayitlar = await GetBelgeKayitlariAsync(new EbysBelgeListeFiltre { Kaynak = EbysBelgeKaynakFiltre.Arac });
        return kayitlar.First(x => x.Kaynak == EbysBelgeKaynak.Arac && x.BelgeId == belge.Id);
    }

    private static string BuildPersonelDosyaAdi(string evrakAdi, string relativePath)
    {
        var cleanedPath = relativePath.Replace(".enc", string.Empty, StringComparison.OrdinalIgnoreCase);
        var extension = Path.GetExtension(cleanedPath);
        var safeName = string.Concat(evrakAdi.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        return string.IsNullOrWhiteSpace(extension) ? safeName : $"{safeName}{extension}";
    }

    private static string GetOzlukKategoriAdi(OzlukEvrakKategori kategori) => kategori switch
    {
        OzlukEvrakKategori.Genel => "Genel",
        OzlukEvrakKategori.KimlikBelgeleri => "Kimlik Belgeleri",
        OzlukEvrakKategori.EgitimBelgeleri => "Eğitim Belgeleri",
        OzlukEvrakKategori.SaglikBelgeleri => "Sağlık Belgeleri",
        OzlukEvrakKategori.SoforBelgeleri => "Şoför Belgeleri",
        OzlukEvrakKategori.SGKBelgeleri => "SGK Belgeleri",
        OzlukEvrakKategori.IseGirisBelgeleri => "İşe Giriş Belgeleri",
        OzlukEvrakKategori.Diger => "Diğer",
        _ => kategori.ToString()
    };

    private static string GetAracDurumAdi(EvrakDurum durum) => durum switch
    {
        EvrakDurum.Aktif => "Aktif",
        EvrakDurum.Pasif => "Pasif",
        EvrakDurum.SuresiDolmus => "Süresi Dolmuş",
        _ => durum.ToString()
    };

    private static string GetMimeType(string dosyaAdi)
    {
        return Path.GetExtension(dosyaAdi).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }

    private static bool BelgeAramadaEslesiyor(EbysBelgeKaydi kayit, string arama)
    {
        var normalizedArama = NormalizeSearchText(arama);
        if (string.IsNullOrWhiteSpace(normalizedArama))
            return true;

        var searchableText = BuildSearchableText(kayit);
        return searchableText.Contains(normalizedArama, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSearchableText(EbysBelgeKaydi kayit)
    {
        var kaynakAdi = kayit.Kaynak == EbysBelgeKaynak.Personel ? "Personel" : "Arac";
        var dosyaDurumu = kayit.DosyaVar ? "Dosyali Dosya Var" : "Dosyasiz Dosya Eksik";

        var parcalar = new[]
        {
            kayit.BelgeAdi,
            kayit.Kategori,
            kayit.IlgiliKayitAdi,
            kayit.IlgiliKayitKodu,
            kayit.Durum,
            kayit.RiskDurumu,
            kaynakAdi,
            dosyaDurumu,
            kayit.DosyaAdi,
            kayit.Aciklama,
            FormatAramaTarihi(kayit.BelgeTarihi),
            FormatAramaTarihi(kayit.BitisTarihi)
        };

        return NormalizeSearchText(string.Join(' ', parcalar.Where(x => !string.IsNullOrWhiteSpace(x))));
    }

    private static string FormatAramaTarihi(DateTime? tarih)
        => tarih.HasValue
            ? $"{tarih.Value:dd.MM.yyyy} {tarih.Value:yyyy-MM-dd} {tarih.Value:MM.yyyy} {tarih.Value:yyyy}"
            : string.Empty;

    private static string NormalizeSearchText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim()
            .Replace('İ', 'I')
            .Replace('I', 'I')
            .Replace('ı', 'i')
            .Replace('Ş', 'S')
            .Replace('ş', 's')
            .Replace('Ğ', 'G')
            .Replace('ğ', 'g')
            .Replace('Ü', 'U')
            .Replace('ü', 'u')
            .Replace('Ö', 'O')
            .Replace('ö', 'o')
            .Replace('Ç', 'C')
            .Replace('ç', 'c');
    }
}



