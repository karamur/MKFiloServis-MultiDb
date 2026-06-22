using System.IO.Compression;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Helpers;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace KOAFiloServis.Web.Services;

public class BelgeUyariService : IBelgeUyariService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IPersonelOzlukService _ozlukService;
    private readonly ISecureFileService _secureFileService;
    private readonly IEvrakArsivService _evrakArsivService;

    public BelgeUyariService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IPersonelOzlukService ozlukService,
        ISecureFileService secureFileService,
        IEvrakArsivService evrakArsivService)
    {
        _contextFactory = contextFactory;
        _ozlukService = ozlukService;
        _secureFileService = secureFileService;
        _evrakArsivService = evrakArsivService;
    }

    public async Task<BelgeUyariOzet> GetBelgeUyarilarAsync(int yaklasanGunSayisi = 30)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var ozet = new BelgeUyariOzet();

        try
        {
            var bugun = DateTime.Today;
            var limitTarih = bugun.AddDays(yaklasanGunSayisi);

            // Aktif tüm personeli al
            var soforler = await context.Soforler
                .Where(s => s.Aktif && !s.IsDeleted)
                .ToListAsync();

        // Tüm personel özlük evraklarını tek sorguda al (GecerlilikBitisTarihi olan ve yaklaşan/geçmiş)
        var tumOzlukEvraklar = await context.PersonelOzlukEvraklar
            .AsNoTracking()
            .Include(e => e.Sofor)
                .ThenInclude(s => s!.TasimaTedarikci)
            .Include(e => e.EvrakTanim)
            .Where(e => !e.IsDeleted
                && e.Sofor != null && e.Sofor.Aktif && !e.Sofor.IsDeleted
                && e.GecerlilikBitisTarihi.HasValue
                && e.GecerlilikBitisTarihi.Value <= limitTarih)
            .OrderBy(e => e.GecerlilikBitisTarihi)
            .ToListAsync();

        // Özlük evraklarını uyarı listelerine dağıt
        foreach (var evrak in tumOzlukEvraklar)
        {
            var evrakAdi = evrak.EvrakTanim?.EvrakAdi ?? string.Empty;
            var uyari = new BelgeUyari
            {
                Id = evrak.Id,
                Kaynak = "Personel",
                Baslik = evrak.Sofor?.TamAd ?? "Personel",
                BelgeTuru = evrakAdi,
                BitisTarihi = evrak.GecerlilikBitisTarihi!.Value,
                DetayUrl = $"/personel/{evrak.SoforId}",
                TasimaTedarikciId = evrak.Sofor?.TasimaTedarikciId,
                TasimaTedarikciUnvan = evrak.Sofor?.TasimaTedarikci?.Unvan
            };

            if (evrakAdi.Contains("Ehliyet", StringComparison.OrdinalIgnoreCase))
                ozet.EhliyetUyarilari.Add(uyari);
            else if (evrakAdi.Contains("MYK", StringComparison.OrdinalIgnoreCase)
                || evrakAdi.Contains("Mesleki Yeterlilik", StringComparison.OrdinalIgnoreCase))
                ozet.MykBelgesiUyarilari.Add(uyari);
            else if (evrakAdi.Contains("Psikoteknik", StringComparison.OrdinalIgnoreCase))
                ozet.PsikoteknikUyarilari.Add(uyari);
            else if (evrakAdi.Contains("Sağlık", StringComparison.OrdinalIgnoreCase) ||
                     evrakAdi.Contains("Saglik", StringComparison.OrdinalIgnoreCase))
                ozet.SaglikRaporuUyarilari.Add(uyari);
            else
                ozet.DigerPersonelEvrakUyarilari.Add(uyari);
        }

        // TEKİL KAYNAK: Araç uyarıları yalnızca AracEvrak (Filo > Araçlar > Evraklar) tablosundan gelir.
        var tumAracEvraklari = await context.AracEvraklari
            .AsNoTracking()
            .Include(x => x.Arac)
                .ThenInclude(a => a!.TasimaTedarikci)
            .Where(x => !x.IsDeleted
                && x.Arac != null
                && !x.Arac.IsDeleted
                && x.Arac.Aktif
                && x.Durum != EvrakDurum.Pasif
                && x.EvrakKategorisi != EvrakKategorileri.Ruhsat
                && x.BitisTarihi.HasValue
                && x.BitisTarihi.Value <= limitTarih)
            .OrderBy(x => x.BitisTarihi)
            .ToListAsync();

        // AracEvrak tablosundan gelen tüm evrakleri kategoriye göre dağıt
        foreach (var evrak in tumAracEvraklari)
        {
            var baslik = evrak.Arac?.AktifPlaka ?? evrak.Arac?.SaseNo ?? "Araç";
            var belgeTuru = string.IsNullOrWhiteSpace(evrak.EvrakAdi) ? evrak.EvrakKategorisi : evrak.EvrakAdi!;
            var detayUrl = $"/araclar/{evrak.AracId}/evraklar";

            BelgeUyari uyari = new()
            {
                Id = evrak.Id,
                Kaynak = "Araç",
                Baslik = baslik,
                BelgeTuru = belgeTuru,
                BitisTarihi = evrak.BitisTarihi!.Value,
                DetayUrl = detayUrl,
                TasimaTedarikciId = evrak.Arac?.TasimaTedarikciId,
                TasimaTedarikciUnvan = evrak.Arac?.TasimaTedarikci?.Unvan
            };

            if (evrak.EvrakKategorisi == EvrakKategorileri.Muayene)
                ozet.MuayeneUyarilari.Add(uyari);
            else if (evrak.EvrakKategorisi == EvrakKategorileri.Kasko)
                ozet.KaskoUyarilari.Add(uyari);
            else if (evrak.EvrakKategorisi == EvrakKategorileri.TrafikSigortasi)
                ozet.TrafikSigortasiUyarilari.Add(uyari);
            else
                ozet.DigerAracEvrakUyarilari.Add(uyari);
        }

        // Tedarikçi sözleşme bitiş uyarıları – yeni kaynak: Cari (Tedarikci / MusteriTedarikci)
        var cariTedarikciler = await context.Cariler
            .AsNoTracking()
            .Where(c => c.Aktif && !c.IsDeleted
                && (c.CariTipi == CariTipi.Tedarikci || c.CariTipi == CariTipi.MusteriTedarikci)
                && c.SozlesmeBitisTarihi.HasValue
                && c.SozlesmeBitisTarihi.Value <= limitTarih)
            .OrderBy(c => c.SozlesmeBitisTarihi)
            .ToListAsync();

        foreach (var cari in cariTedarikciler)
        {
            ozet.TedarikciSozlesmeUyarilari.Add(new BelgeUyari
            {
                Id = cari.Id,
                Kaynak = "Tedarikçi",
                Baslik = cari.Unvan,
                BelgeTuru = string.IsNullOrWhiteSpace(cari.SozlesmeNo)
                    ? "Sözleşme Bitiş"
                    : $"Sözleşme Bitiş ({cari.SozlesmeNo})",
                BitisTarihi = cari.SozlesmeBitisTarihi!.Value,
                DetayUrl = $"/cariler/{cari.Id}",
                TasimaTedarikciUnvan = cari.Unvan
            });
        }

        // Kiralık C Plaka sözleşme/kira bitiş uyarıları
        var kiralikPlakalar = await context.KiralikPlakaTakipler
            .AsNoTracking()
            .Where(k => !k.IsDeleted && k.BitisTarihi <= limitTarih)
            .OrderBy(k => k.BitisTarihi)
            .Select(k => new
            {
                k.Id,
                k.Plaka,
                k.IsimSoyisim,
                k.BitisTarihi,
                k.AracId
            })
            .ToListAsync();

        foreach (var k in kiralikPlakalar)
        {
            ozet.KiralikPlakaUyarilari.Add(new BelgeUyari
            {
                Id = k.Id,
                Kaynak = "Kiralık Plaka",
                Baslik = $"{k.Plaka} - {k.IsimSoyisim}",
                BelgeTuru = "Kiralama Bitiş",
                BitisTarihi = k.BitisTarihi,
                DetayUrl = k.AracId.HasValue
                    ? $"/araclar/{k.AracId.Value}/evraklar"
                    : $"/araclar/plaka-takip/{k.Id}/duzenle"
            });
        }


        // Tum personeller icin "Diger" kategorisindeki evrak durumlarini cek (uyari filtresi yok - tam liste)
        var digerEvrakTanimlari = await context.OzlukEvrakTanimlari
            .AsNoTracking()
            .Where(t => t.Aktif && t.Kategori == OzlukEvrakKategori.Diger)
            .OrderBy(t => t.SiraNo)
            .ThenBy(t => t.EvrakAdi)
            .ToListAsync();

        if (digerEvrakTanimlari.Count > 0)
        {
            var digerEvrakTanimIds = digerEvrakTanimlari.Select(t => t.Id).ToHashSet();

            var mevcutDigerEvraklar = await context.PersonelOzlukEvraklar
                .AsNoTracking()
                .Include(e => e.Sofor)
                .Where(e => !e.IsDeleted
                    && e.Sofor != null && e.Sofor.Aktif && !e.Sofor.IsDeleted
                    && digerEvrakTanimIds.Contains(e.EvrakTanimId))
                .ToListAsync();

            foreach (var sofor in soforler.OrderBy(s => s.Ad).ThenBy(s => s.Soyad))
            {
                foreach (var tanim in digerEvrakTanimlari)
                {
                    var kayit = mevcutDigerEvraklar
                        .FirstOrDefault(e => e.SoforId == sofor.Id && e.EvrakTanimId == tanim.Id);

                    ozet.DigerTumPersonelBelgeler.Add(new PersonelBelgeDetay
                    {
                        EvrakId = kayit?.Id ?? 0,
                        SoforId = sofor.Id,
                        PersonelAdi = sofor.TamAd,
                        PersonelKodu = sofor.SoforKodu ?? sofor.Id.ToString(),
                        EvrakAdi = tanim.EvrakAdi,
                        Kategori = tanim.Kategori,
                        Tamamlandi = kayit?.Tamamlandi ?? false,
                        TamamlanmaTarihi = kayit?.TamamlanmaTarihi,
                        GecerlilikBitisTarihi = kayit?.GecerlilikBitisTarihi,
                        Zorunlu = tanim.Zorunlu,
                        DosyaYolu = kayit?.DosyaYolu,
                        DetayUrl = $"/personel/ozluk-evrak"
                    });
                }
            }
        }
                // Özet sayıları hesapla
                ozet.ToplamKritikUyari = ozet.TumUyarilar.Count(u => u.Seviye == BelgeUyariSeviye.Kritik || u.Seviye == BelgeUyariSeviye.Acil);
                ozet.ToplamUyari = ozet.TumUyarilar.Count;

                return ozet;
            }
            catch (PostgresException ex) when (ex.SqlState == "42703")
            {
                // Eski tenant şemasında eksik kolon/tablo varyasyonlarında dashboard'ı kırma.
                return new BelgeUyariOzet();
            }
        }

    public async Task<List<PersonelBelgeTabloKalemi>> GetPersonelBelgeTablosuAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // 1. Tüm aktif personelleri çek
        var soforler = await context.Soforler
            .AsNoTracking()
            .Include(s => s.TasimaTedarikci)
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.SiralamaNo == 0 ? int.MaxValue : s.SiralamaNo)
            .ThenBy(s => s.Ad)
            .ToListAsync();

        if (!soforler.Any()) return new List<PersonelBelgeTabloKalemi>();

        var soforIdler = soforler.Select(s => s.Id).ToList();

        // 2. Tüm aktif evrak tanımlarını tek sorguda çek
        var tumTanimlar = await context.OzlukEvrakTanimlari
            .AsNoTracking()
            .Where(t => !t.IsDeleted && t.Aktif)
            .OrderBy(t => t.Kategori)
            .ThenBy(t => t.SiraNo)
            .ToListAsync();

        // 3. Tüm personellerin evraklarını tek sorguda çek
        var tumEvraklar = await context.PersonelOzlukEvraklar
            .AsNoTracking()
            .Where(e => soforIdler.Contains(e.SoforId) && !e.IsDeleted)
            .ToListAsync();

        // Grup: soforId → evraklar
        var evraklarByPersonel = tumEvraklar.GroupBy(e => e.SoforId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<PersonelBelgeTabloKalemi>();
        foreach (var s in soforler)
        {
            var gorevStr = ((int)s.Gorev).ToString();
            var gecerliTanimlar = tumTanimlar
                .Where(t => string.IsNullOrEmpty(t.GecerliGorevler)
                    || t.GecerliGorevler.Split(',').Contains(gorevStr))
                .ToList();

            var personelEvraklar = evraklarByPersonel.TryGetValue(s.Id, out var pe) ? pe : new();

            var dosyalar = gecerliTanimlar.Select(tanim =>
            {
                var evrak = personelEvraklar.FirstOrDefault(e => e.EvrakTanimId == tanim.Id);
                return new OzlukEvrakDosyaBilgisi
                {
                    EvrakTanimId = tanim.Id,
                    EvrakAdi = tanim.EvrakAdi,
                    DosyaYolu = evrak?.DosyaYolu,
                    DosyaAdi = BuildIndirmeDosyaAdi(tanim.EvrakAdi, evrak?.DosyaYolu)
                };
            }).ToList();

            result.Add(new PersonelBelgeTabloKalemi
            {
                SoforId = s.Id,
                PersonelAdi = s.TamAd,
                PersonelKodu = s.SoforKodu,
                Gorev = s.Gorev.ToString(),
                Aktif = s.Aktif,
                TasimaTedarikciId = s.TasimaTedarikciId,
                TasimaTedarikciUnvan = s.TasimaTedarikci?.Unvan,
                ToplamEvrakSayisi = gecerliTanimlar.Count,
                YuklenmisEvrakSayisi = personelEvraklar.Count(e =>
                    gecerliTanimlar.Any(t => t.Id == e.EvrakTanimId) && !string.IsNullOrEmpty(e.DosyaYolu)),
                EvrakDosyalari = dosyalar,
                EhliyetGecerlilik = s.EhliyetGecerlilikTarihi,
                KimlikGecerlilik = s.KimlikGecerlilikTarihi,
                MykBelgesiGecerlilik = s.MykBelgesiGecerlilikTarihi,
                SrcBelgesiVarMi = s.SrcBelgesiGecerlilikTarihi.HasValue,
                YayginEgitimGecerlilik = null,
                YayginEgitimSertifikasiVarMi = s.YayginEgitimSertifikasiVarMi,
                PsikoteknikGecerlilik = s.PsikoteknikGecerlilikTarihi,
                AdliSicilGecerlilik = s.AdliSicilGecerlilikTarihi,
                SaglikRaporuGecerlilik = s.SaglikRaporuGecerlilikTarihi,
                SuruculCezaBarkodGecerlilik = s.SuruculCezaBarkodluBelgeTarihi
            });
        }
        return result;
    }

    public async Task<PersonelBelgeTabloKalemi?> GetTekPersonelBelgeAsync(int soforId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var s = await context.Soforler
            .AsNoTracking()
            .Where(x => x.Id == soforId && !x.IsDeleted)
            .FirstOrDefaultAsync();
        if (s == null) return null;

        var evrakDurum = await _ozlukService.GetPersonelEvrakDurumuAsync(s.Id);
        var dosyalar = evrakDurum.Evraklar
            .Select(e => new OzlukEvrakDosyaBilgisi
            {
                EvrakTanimId = e.EvrakTanimId,
                EvrakAdi = e.EvrakAdi,
                DosyaYolu = e.DosyaYolu,
                DosyaAdi = BuildIndirmeDosyaAdi(e.EvrakAdi, e.DosyaYolu)
            }).ToList();

        return new PersonelBelgeTabloKalemi
        {
            SoforId = s.Id,
            PersonelAdi = s.TamAd,
            PersonelKodu = s.SoforKodu,
            Gorev = s.Gorev.ToString(),
            Aktif = s.Aktif,
            ToplamEvrakSayisi = evrakDurum.ToplamEvrak,
            YuklenmisEvrakSayisi = evrakDurum.TamamlananEvrak,
            EvrakDosyalari = dosyalar,
            EhliyetGecerlilik = s.EhliyetGecerlilikTarihi,
            KimlikGecerlilik = s.KimlikGecerlilikTarihi,
            MykBelgesiGecerlilik = s.MykBelgesiGecerlilikTarihi,
            SrcBelgesiVarMi = s.SrcBelgesiGecerlilikTarihi.HasValue,
            YayginEgitimGecerlilik = null,
            YayginEgitimSertifikasiVarMi = s.YayginEgitimSertifikasiVarMi,
            PsikoteknikGecerlilik = s.PsikoteknikGecerlilikTarihi,
            AdliSicilGecerlilik = s.AdliSicilGecerlilikTarihi,
            SaglikRaporuGecerlilik = s.SaglikRaporuGecerlilikTarihi,
            SuruculCezaBarkodGecerlilik = s.SuruculCezaBarkodluBelgeTarihi
        };
    }

    public async Task<bool> PersonelBelgeTarihGuncelleAsync(int soforId, string belgeAlani, DateTime? tarih)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sofor = await context.Soforler.FirstOrDefaultAsync(s => s.Id == soforId && !s.IsDeleted);
        if (sofor == null) return false;

        switch (belgeAlani)
        {
            case "Ehliyet": sofor.EhliyetGecerlilikTarihi = tarih; break;
            case "Kimlik": sofor.KimlikGecerlilikTarihi = tarih; break;
            case "MykBelgesi": sofor.MykBelgesiGecerlilikTarihi = tarih; break;
            case "YayginEgitim":
                sofor.SrcBelgesiGecerlilikTarihi = tarih;
                sofor.YayginEgitimSertifikasiVarMi = tarih.HasValue;
                break;
            case "Psikoteknik": sofor.PsikoteknikGecerlilikTarihi = tarih; break;
            case "AdliSicil": sofor.AdliSicilGecerlilikTarihi = tarih; break;
            case "SaglikRaporu": sofor.SaglikRaporuGecerlilikTarihi = tarih; break;
            case "SuruculCezaBarkod": sofor.SuruculCezaBarkodluBelgeTarihi = tarih; break;
            default: return false;
        }

        sofor.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<byte[]> PersonelBelgePdfAsync(int soforId)
    {
        return await _ozlukService.ExportPersonelDosyaPdfAsync(soforId);
    }

    public async Task<byte[]> SeciliPersonelBelgelerZipAsync(List<int> soforIdler, List<string>? seciliDosyaYollari = null)
    {
        using var zipMs = new MemoryStream();
        var kullanilanZipYollari = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seciliDosyalar = seciliDosyaYollari?.Count > 0
            ? new HashSet<string>(seciliDosyaYollari, StringComparer.OrdinalIgnoreCase)
            : null;
        var eklenenDosyaSayisi = 0;

        using (var archive = new ZipArchive(zipMs, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var soforId in soforIdler)
            {
                PersonelOzlukEvrakDurum evrakDurum;
                try
                {
                    evrakDurum = await _ozlukService.GetPersonelEvrakDurumuAsync(soforId);
                }
                catch
                {
                    // Tek bir personeldeki veri sorunu toplu ZIP akışını durdurmamalı.
                    continue;
                }

                var personelKlasoru = SanitizeZipSegment(
                    evrakDurum.PersonelAdi?.Length > 0 ? evrakDurum.PersonelAdi : evrakDurum.PersonelKodu ?? soforId.ToString(),
                    $"personel_{soforId}");

                // Seçili dosya yolu filtresi varsa uygula
                var evraklar = evrakDurum.Evraklar
                    .Where(e => !string.IsNullOrEmpty(e.DosyaYolu))
                    .Where(e => seciliDosyalar == null || seciliDosyalar.Contains(e.DosyaYolu!))
                    .ToList();

                foreach (var evrak in evraklar)
                {
                    try
                    {
                        var icerik = await _secureFileService.ReadDecryptedAsync(evrak.DosyaYolu);
                        if (icerik == null || icerik.Length == 0) continue;

                        var uzanti = GetGercekUzanti(evrak.DosyaYolu);
                        var guvenliEvrakAd = SanitizeZipSegment(evrak.EvrakAdi, "belge");
                        var zipDosyaAdi = string.IsNullOrWhiteSpace(uzanti) ? guvenliEvrakAd : $"{guvenliEvrakAd}{uzanti}";
                        var zipYolu = soforIdler.Count > 1
                            ? $"{personelKlasoru}/{zipDosyaAdi}"
                            : zipDosyaAdi;
                        var entryYolu = BuildUniqueZipEntryPath(zipYolu, kullanilanZipYollari);

                        var entry = archive.CreateEntry(entryYolu, CompressionLevel.Optimal);
                        await using var entryStream = entry.Open();
                        await entryStream.WriteAsync(icerik);
                        eklenenDosyaSayisi++;
                    }
                    catch
                    {
                        // Tek bir bozuk dosya toplu ZIP indirmeyi durdurmamalı.
                        continue;
                    }
                }
            }
        }

        if (eklenenDosyaSayisi == 0)
            return Array.Empty<byte>();

        zipMs.Position = 0;
        return zipMs.ToArray();
    }

    /// <summary>
    /// Saklanan dosya yolundaki '.enc' uzantısını kaldırır, gerçek (ör. .pdf, .jpg) uzantıyı döndürür.
    /// </summary>
    private static string GetGercekUzanti(string? dosyaYolu)
    {
        if (string.IsNullOrWhiteSpace(dosyaYolu)) return string.Empty;
        var ad = Path.GetFileName(dosyaYolu);
        if (ad.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
            ad = ad.Substring(0, ad.Length - 4);
        return Path.GetExtension(ad);
    }

    /// <summary>
    /// İndirme için kullanıcı dostu, .enc içermeyen dosya adı üretir.
    /// </summary>
    private static string? BuildIndirmeDosyaAdi(string evrakAdi, string? dosyaYolu)
    {
        if (string.IsNullOrWhiteSpace(dosyaYolu)) return null;
        var uzanti = GetGercekUzanti(dosyaYolu);
        var guvenliAd = string.Join("_", (evrakAdi ?? "belge").Split(Path.GetInvalidFileNameChars()));
        return string.IsNullOrEmpty(uzanti) ? guvenliAd : $"{guvenliAd}{uzanti}";
    }

    private static string SanitizeZipSegment(string? value, string fallback)
    {
        var raw = string.IsNullOrWhiteSpace(value) ? fallback : value;
        var cleaned = string.Join("_", raw.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
            .Replace("/", "_")
            .Replace("\\", "_")
            .Trim()
            .Trim('.');
        return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
    }

    private static string BuildUniqueZipEntryPath(string basePath, HashSet<string> usedPaths)
    {
        var candidate = basePath.Replace("\\", "/");
        if (usedPaths.Add(candidate))
            return candidate;

        var sonSlash = candidate.LastIndexOf('/');
        var klasor = sonSlash >= 0 ? candidate[..sonSlash] : string.Empty;
        var dosyaAdi = sonSlash >= 0 ? candidate[(sonSlash + 1)..] : candidate;
        var adGovdesi = Path.GetFileNameWithoutExtension(dosyaAdi);
        var uzanti = Path.GetExtension(dosyaAdi);

        var sayac = 1;
        while (true)
        {
            var yeniDosyaAdi = $"{adGovdesi}_{sayac}{uzanti}";
            var yeniAday = string.IsNullOrWhiteSpace(klasor)
                ? yeniDosyaAdi
                : $"{klasor}/{yeniDosyaAdi}";
            if (usedPaths.Add(yeniAday))
                return yeniAday;
            sayac++;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // Araç Belge Tablosu
    // ─────────────────────────────────────────────────────────────────

    // Sütun anahtarı → AracEvrak.EvrakKategorisi eşlemesi
    private static string KategoriEslestir(string belgeAlani) => belgeAlani switch
    {
        "Ruhsat" => EvrakKategorileri.Ruhsat,
        "Sigorta" => EvrakKategorileri.TrafikSigortasi,
        "Muayene" => EvrakKategorileri.Muayene,
        "Uygunluk" => EvrakKategorileri.UygunlukBelgesi,
        "KoltukSigortasi" => EvrakKategorileri.KoltukSigortasi,
        "Kasko" => EvrakKategorileri.Kasko,
        _ => belgeAlani
    };

    private static string KategoriAdi(string alan) => alan switch
    {
        "Ruhsat" => "Ruhsat",
        "Sigorta" => "Trafik Sigortası",
        "Muayene" => "Muayene",
        "Uygunluk" => "Uygunluk Belgesi",
        "KoltukSigortasi" => "Koltuk Sigortası",
        "Kasko" => "Kasko",
        _ => alan
    };

    public async Task<List<AracBelgeTabloKalemi>> GetAracBelgeTablosuAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Tüm araçlar (Tedarikçi sahipli olanlar dahil) bu listede döner;
        // "Araç Belge Tablosu" ile "Ted. Araçları" sekmeleri UI tarafında
        // SahiplikTipi'ne göre ayrıştırılır.
        var araclar = await context.Araclar
            .AsNoTracking()
            .Include(a => a.TasimaTedarikci)
            .Include(a => a.Firma)
            .Where(a => !a.IsDeleted)
            .OrderBy(a => a.AktifPlaka ?? a.SaseNo)
            .ToListAsync();

        if (!araclar.Any()) return new List<AracBelgeTabloKalemi>();

        var aracIdler = araclar.Select(a => a.Id).ToList();

        var tumEvraklar = await context.AracEvraklari
            .AsNoTracking()
            .Include(e => e.Dosyalar.Where(d => !d.IsDeleted))
            .Where(e => aracIdler.Contains(e.AracId) && !e.IsDeleted && e.Durum != EvrakDurum.Pasif)
            .ToListAsync();

        var evraklarByArac = tumEvraklar.GroupBy(e => e.AracId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var sutunlar = new[] { "Ruhsat", "Sigorta", "Muayene", "Uygunluk", "KoltukSigortasi", "Kasko" };
        var result = new List<AracBelgeTabloKalemi>();

        foreach (var a in araclar)
        {
            var aracEvraklari = evraklarByArac.TryGetValue(a.Id, out var ae) ? ae : new();

            // Her sütun için en güncel (en geç bitiş tarihli) evrak kaydını bul
            AracEvrak? Bul(string alan)
            {
                var kategori = KategoriEslestir(alan);
                return aracEvraklari
                    .Where(e => string.Equals(e.EvrakKategorisi, kategori, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(e => e.BitisTarihi ?? DateTime.MinValue)
                    .FirstOrDefault();
            }

            var ruhsatEv = Bul("Ruhsat");
            var sigortaEv = Bul("Sigorta");
            var muayeneEv = Bul("Muayene");
            var uygunlukEv = Bul("Uygunluk");
            var koltukEv = Bul("KoltukSigortasi");
            var kaskoEv = Bul("Kasko");

            var dosyalar = new List<AracEvrakDosyaBilgisi>();
            // Her kategoriyi, kayıt olmasa bile her zaman listeye ekle ("Yok" placeholder)
            void DosyaEkle(AracEvrak? evrak, string kategoriKodu, string goruntulemeAdi)
            {
                var dosya = evrak?.Dosyalar?.OrderByDescending(d => d.CreatedAt).FirstOrDefault(d => !d.IsDeleted);
                dosyalar.Add(new AracEvrakDosyaBilgisi
                {
                    AracEvrakId = evrak?.Id ?? 0,
                    EvrakKategorisi = evrak != null && !string.IsNullOrEmpty(evrak.EvrakKategorisi) ? evrak.EvrakKategorisi : kategoriKodu,
                    EvrakAdi = evrak?.EvrakAdi ?? goruntulemeAdi,
                    DosyaYolu = dosya?.DosyaYolu,
                    DosyaAdi = BuildIndirmeDosyaAdi(evrak?.EvrakAdi ?? goruntulemeAdi, dosya?.DosyaYolu)
                });
            }

            DosyaEkle(ruhsatEv, EvrakKategorileri.Ruhsat, "Ruhsat");
            DosyaEkle(sigortaEv, EvrakKategorileri.TrafikSigortasi, "Trafik Sigortası");
            DosyaEkle(muayeneEv, EvrakKategorileri.Muayene, "Muayene");
            DosyaEkle(uygunlukEv, EvrakKategorileri.UygunlukBelgesi, "Uygunluk Belgesi");
            DosyaEkle(koltukEv, EvrakKategorileri.KoltukSigortasi, "Koltuk Sigortası");
            DosyaEkle(kaskoEv, EvrakKategorileri.Kasko, "Kasko");

            // Sigorta tarihi öncelik: Arac entity > AracEvrak
            // Muayene/Kasko aynı şekilde fallback
            DateTime? sigortaTarihi = a.TrafikSigortaBitisTarihi ?? sigortaEv?.BitisTarihi;
            DateTime? muayeneTarihi = a.MuayeneBitisTarihi ?? muayeneEv?.BitisTarihi;
            DateTime? kaskoTarihi = a.KaskoBitisTarihi ?? kaskoEv?.BitisTarihi;
            DateTime? koltukTarihi = a.KoltukSigortasiBitisTarihi ?? koltukEv?.BitisTarihi;

            result.Add(new AracBelgeTabloKalemi
            {
                AracId = a.Id,
                Plaka = a.AktifPlaka ?? string.Empty,
                SaseNo = a.SaseNo,
                MarkaModel = $"{a.Marka} {a.Model}".Trim(),
                AracTipi = a.AracTipi,
                Aktif = a.Aktif,
                SahiplikTipi = a.SahiplikTipi,
                TasimaTedarikciId = a.TasimaTedarikciId,
                TasimaTedarikciUnvan = a.TasimaTedarikci?.Unvan,
                FirmaId = a.FirmaId,
                FirmaAdi = a.Firma?.FirmaAdi,
                ToplamEvrakSayisi = sutunlar.Length,
                YuklenmisEvrakSayisi = dosyalar.Count(d => d.DosyaVar),
                EvrakDosyalari = dosyalar,
                RuhsatVarMi = ruhsatEv != null,
                SigortaGecerlilik = sigortaTarihi,
                MuayeneGecerlilik = muayeneTarihi,
                UygunlukGecerlilik = uygunlukEv?.BitisTarihi,
                KoltukSigortasiGecerlilik = koltukTarihi,
                KaskoGecerlilik = kaskoTarihi
            });
        }
        return result;
    }

    public async Task<AracBelgeTabloKalemi?> GetTekAracBelgeAsync(int aracId)
    {
        var liste = await GetAracBelgeTablosuAsync();
        return liste.FirstOrDefault(x => x.AracId == aracId);
    }

    public async Task<bool> AracBelgeTarihGuncelleAsync(int aracId, string belgeAlani, DateTime? bitisTarihi)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var arac = await context.Araclar.FirstOrDefaultAsync(a => a.Id == aracId && !a.IsDeleted);
        if (arac == null) return false;

        // Doğrudan Arac entity'de tutulan tarihler
        switch (belgeAlani)
        {
            case "Ruhsat":
                // Ruhsat tarihi Arac entity'de ayrı alan olarak tutulmuyor.
                // Bu akışta AracEvrak kaydı güncellenerek tarih yönetilir.
                break;
            case "Sigorta":
                arac.TrafikSigortaBitisTarihi = bitisTarihi.HasValue
                    ? DateTime.SpecifyKind(bitisTarihi.Value, DateTimeKind.Utc) : null;
                break;
            case "Muayene":
                arac.MuayeneBitisTarihi = bitisTarihi.HasValue
                    ? DateTime.SpecifyKind(bitisTarihi.Value, DateTimeKind.Utc) : null;
                break;
            case "Kasko":
                arac.KaskoBitisTarihi = bitisTarihi.HasValue
                    ? DateTime.SpecifyKind(bitisTarihi.Value, DateTimeKind.Utc) : null;
                break;
            case "KoltukSigortasi":
                arac.KoltukSigortasiBitisTarihi = bitisTarihi.HasValue
                    ? DateTime.SpecifyKind(bitisTarihi.Value, DateTimeKind.Utc) : null;
                break;
            case "Uygunluk":
                // Bu tarihler Arac entity'de yok → AracEvrak üzerine kaydedilir/güncellenir
                break;
            default: return false;
        }

        // AracEvrak güncelle/ekle (her durumda iz olarak; uyarı sayfası için)
        var kategori = KategoriEslestir(belgeAlani);
        var evrak = await context.AracEvraklari
            .Where(e => e.AracId == aracId && !e.IsDeleted && e.EvrakKategorisi == kategori)
            .OrderByDescending(e => e.BitisTarihi)
            .FirstOrDefaultAsync();

        if (evrak == null && bitisTarihi.HasValue)
        {
            evrak = new AracEvrak
            {
                AracId = aracId,
                EvrakKategorisi = kategori,
                EvrakAdi = kategori,
                Durum = EvrakDurum.Aktif,
                BitisTarihi = DateTime.SpecifyKind(bitisTarihi.Value, DateTimeKind.Utc),
                CreatedAt = DateTime.UtcNow
            };
            context.AracEvraklari.Add(evrak);
        }
        else if (evrak != null)
        {
            evrak.BitisTarihi = bitisTarihi.HasValue
                ? DateTime.SpecifyKind(bitisTarihi.Value, DateTimeKind.Utc) : null;
            evrak.UpdatedAt = DateTime.UtcNow;
        }

        arac.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AracBelgeDosyaYukleAsync(int aracId, string belgeAlani, string dosyaAdi, byte[] icerik)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var arac = await context.Araclar
            .Include(a => a.Firma)
            .FirstOrDefaultAsync(a => a.Id == aracId && !a.IsDeleted);
        if (arac == null) return false;

        var kategori = KategoriEslestir(belgeAlani);

        // Aynı kategoride aktif evrak var mı?
        var evrak = await context.AracEvraklari
            .Where(e => e.AracId == aracId && !e.IsDeleted && e.EvrakKategorisi == kategori)
            .OrderByDescending(e => e.BitisTarihi)
            .FirstOrDefaultAsync();

        if (evrak == null)
        {
            evrak = new AracEvrak
            {
                AracId = aracId,
                EvrakKategorisi = kategori,
                EvrakAdi = kategori,
                Durum = EvrakDurum.Aktif,
                CreatedAt = DateTime.UtcNow
            };
            context.AracEvraklari.Add(evrak);
            await context.SaveChangesAsync();
        }

        // Araç arşiv klasörü: {Plaka} - {FirmaAdi}
        var uzanti = Path.GetExtension(dosyaAdi);
        var plaka = arac.AktifPlaka ?? arac.SaseNo ?? aracId.ToString();
        var aracKlasoru = AppStoragePaths.BuildAracArsivKlasoru(plaka, arac.Firma?.FirmaAdi);

        // Dosya adı: {PLAKA}{BelgeTipi}_{yyyyMMdd_HHmmss}.uzanti (boşluksuz)
        var normPlaka = AppStoragePaths.NormalizeFolderName(plaka).Replace(" ", "").Replace("-", "");
        var normBelge = AppStoragePaths.NormalizeFolderName(belgeAlani).Replace(" ", "").Replace("-", "");
        var arsivDosyaAdi = $"{normPlaka}{normBelge}_{DateTime.Now:yyyyMMdd_HHmmss}{uzanti}";
        string? storedPath = null;
        try
        {
            storedPath = await _secureFileService.SaveEncryptedAsync(
                $"{AppStoragePaths.AracEvrakRelativeRoot}/{aracKlasoru}",
                arsivDosyaAdi,
                icerik);

            // Arşiv kopyaları (şifreli + şifresiz)
            var sasiNo = arac.SaseNo ?? aracId.ToString();
            try
            {
                await _evrakArsivService.ArsivleAracEvrakAsync(plaka, sasiNo, belgeAlani, icerik, uzanti);
            }
            catch
            {
                // Arşiv hatası ana upload'ı engellememeli (EvrakArsivService içinde loglanır)
            }

            var evrakDosya = new AracEvrakDosya
            {
                AracEvrakId = evrak.Id,
                DosyaAdi = arsivDosyaAdi,
                DosyaYolu = storedPath,
                DosyaTipi = uzanti.TrimStart('.').ToLowerInvariant(),
                DosyaBoyutu = icerik.LongLength,
                CreatedAt = DateTime.UtcNow
            };
            context.AracEvrakDosyalari.Add(evrakDosya);
            await context.SaveChangesAsync();
            return true;
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(storedPath))
            {
                try { await _secureFileService.DeleteAsync(storedPath); } catch { }
            }

            throw;
        }
    }

    public async Task<byte[]> SeciliAracBelgelerZipAsync(List<int> aracIdler, List<string>? seciliDosyaYollari = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        using var zipMs = new MemoryStream();
        var kullanilanZipYollari = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seciliDosyalar = seciliDosyaYollari?.Count > 0
            ? new HashSet<string>(seciliDosyaYollari, StringComparer.OrdinalIgnoreCase)
            : null;
        var eklenenDosyaSayisi = 0;

        using (var archive = new ZipArchive(zipMs, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var aracId in aracIdler)
            {
                var arac = await context.Araclar.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == aracId && !a.IsDeleted);
                if (arac == null) continue;

                var aracKlasoru = SanitizeZipSegment(arac.AktifPlaka ?? arac.SaseNo ?? aracId.ToString(), $"arac_{aracId}");

                var evraklar = await context.AracEvraklari
                    .AsNoTracking()
                    .Include(e => e.Dosyalar.Where(d => !d.IsDeleted))
                    .Where(e => e.AracId == aracId && !e.IsDeleted)
                    .ToListAsync();

                foreach (var evrak in evraklar)
                {
                    foreach (var dosya in evrak.Dosyalar)
                    {
                        if (string.IsNullOrEmpty(dosya.DosyaYolu)) continue;
                        if (seciliDosyalar != null && !seciliDosyalar.Contains(dosya.DosyaYolu)) continue;

                        try
                        {
                            var icerik = await _secureFileService.ReadDecryptedAsync(dosya.DosyaYolu);
                            if (icerik == null || icerik.Length == 0) continue;

                            var uzanti = GetGercekUzanti(dosya.DosyaYolu);
                            if (string.IsNullOrWhiteSpace(uzanti))
                                uzanti = Path.GetExtension(dosya.DosyaAdi);

                            var temelAd = !string.IsNullOrWhiteSpace(evrak.EvrakAdi) ? evrak.EvrakAdi : evrak.EvrakKategorisi;
                            var guvenliAd = SanitizeZipSegment(temelAd, "belge");
                            var zipDosyaAdi = string.IsNullOrWhiteSpace(uzanti) ? guvenliAd : $"{guvenliAd}{uzanti}";
                            var zipYolu = aracIdler.Count > 1
                                ? $"{aracKlasoru}/{zipDosyaAdi}"
                                : zipDosyaAdi;
                            var entryYolu = BuildUniqueZipEntryPath(zipYolu, kullanilanZipYollari);

                            var entry = archive.CreateEntry(entryYolu, CompressionLevel.Optimal);
                            await using var entryStream = entry.Open();
                            await entryStream.WriteAsync(icerik);
                            eklenenDosyaSayisi++;
                        }
                        catch
                        {
                            // Tek bir bozuk dosya toplu ZIP indirmeyi durdurmamalı.
                            continue;
                        }
                    }
                }
            }
        }

        if (eklenenDosyaSayisi == 0)
            return Array.Empty<byte>();

        zipMs.Position = 0;
        return zipMs.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────
    // Tedarikçi Evrak Tablosu
    // ─────────────────────────────────────────────────────────────────

    public async Task<List<TedarikciEvrakTabloKalemi>> GetTedarikciEvrakTablosuAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var tedarikciler = await context.TasimaTedarikciler
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.Unvan)
            .ToListAsync();

        if (!tedarikciler.Any()) return new List<TedarikciEvrakTabloKalemi>();

        var tedarikciIdler = tedarikciler.Select(t => t.Id).ToList();

        var tumEvraklar = await context.TedarikciEvraklari
            .AsNoTracking()
            .Where(e => tedarikciIdler.Contains(e.TasimaTedarikciId) && !e.IsDeleted)
            .ToListAsync();

        var evraklarByTedarikci = tumEvraklar
            .GroupBy(e => e.TasimaTedarikciId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<TedarikciEvrakTabloKalemi>();

        foreach (var t in tedarikciler)
        {
            var evraklar = evraklarByTedarikci.TryGetValue(t.Id, out var ev) ? ev : new();

            var belgeler = new Dictionary<string, DateTime?>();
            foreach (var kategori in TedarikciEvrakKategorileri.TumKategoriler)
            {
                var enYeniEvrak = evraklar
                    .Where(e => string.Equals(e.EvrakKategorisi, kategori, StringComparison.OrdinalIgnoreCase)
                                && e.Durum != EvrakDurum.Pasif)
                    .OrderByDescending(e => e.BitisTarihi ?? DateTime.MinValue)
                    .FirstOrDefault();
                belgeler[kategori] = enYeniEvrak?.BitisTarihi;
            }

            result.Add(new TedarikciEvrakTabloKalemi
            {
                TedarikciId = t.Id,
                TedarikciUnvan = t.Unvan,
                Aktif = t.Aktif,
                Belgeler = belgeler
            });
        }

        return result;
    }
}











