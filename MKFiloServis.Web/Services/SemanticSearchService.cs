using MKFiloServis.Shared.Entities;
using MKFiloServis.Web.Data;
using MKFiloServis.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Semantic Search servisi implementasyonu
/// Ollama embedding API kullanarak belge vektörleri oluşturur ve cosine similarity ile arama yapar
/// </summary>
public class SemanticSearchService : ISemanticSearchService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IOllamaService _ollamaService;
    private readonly ILogger<SemanticSearchService> _logger;

    public SemanticSearchService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IOllamaService ollamaService,
        ILogger<SemanticSearchService> logger)
    {
        _contextFactory = contextFactory;
        _ollamaService = ollamaService;
        _logger = logger;
    }

    /// <summary>
    /// Tek bir belge için embedding oluşturur ve kaydeder
    /// </summary>
    public async Task<EbysBelgeEmbedding?> EmbeddingOlusturVeKaydetAsync(
        EbysAramaKaynak kaynak, int kaynakId, int? dosyaId, string metin)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        if (string.IsNullOrWhiteSpace(metin))
            return null;

        try
        {
            // Mevcut embedding var mı kontrol et
            var mevcutEmbedding = await context.EbysBelgeEmbeddingler
                .FirstOrDefaultAsync(e => e.Kaynak == kaynak && e.KaynakId == kaynakId && e.DosyaId == dosyaId);

            // Embedding oluştur
            var embeddingVektor = await _ollamaService.EmbeddingOlusturAsync(metin);
            if (embeddingVektor == null)
            {
                _logger.LogWarning("Embedding oluşturulamadı: Kaynak={Kaynak}, KaynakId={KaynakId}", kaynak, kaynakId);
                return null;
            }

            if (mevcutEmbedding != null)
            {
                // Güncelle
                mevcutEmbedding.Metin = metin.Length > 8000 ? metin[..8000] : metin;
                mevcutEmbedding.MetinOzet = metin.Length > 500 ? metin[..500] : metin;
                mevcutEmbedding.Embedding = embeddingVektor;
                mevcutEmbedding.ModelAdi = _ollamaService.EmbeddingModelAdi;
                mevcutEmbedding.GuncellemeTarihi = DateTime.Now;
                
                context.EbysBelgeEmbeddingler.Update(mevcutEmbedding);
                await context.SaveChangesAsync();
                return mevcutEmbedding;
            }
            else
            {
                // Yeni oluştur
                var yeniEmbedding = new EbysBelgeEmbedding
                {
                    Kaynak = kaynak,
                    KaynakId = kaynakId,
                    DosyaId = dosyaId,
                    Metin = metin.Length > 8000 ? metin[..8000] : metin,
                    MetinOzet = metin.Length > 500 ? metin[..500] : metin,
                    Embedding = embeddingVektor,
                    ModelAdi = _ollamaService.EmbeddingModelAdi,
                    OlusturmaTarihi = DateTime.Now
                };

                context.EbysBelgeEmbeddingler.Add(yeniEmbedding);
                await context.SaveChangesAsync();
                return yeniEmbedding;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Embedding oluşturma hatası: Kaynak={Kaynak}, KaynakId={KaynakId}", kaynak, kaynakId);
            return null;
        }
    }

    /// <summary>
    /// Belirli bir kaynak için embedding'i günceller
    /// </summary>
    public async Task<bool> EmbeddingGuncelleAsync(int embeddingId, string yeniMetin)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var embedding = await context.EbysBelgeEmbeddingler.FindAsync(embeddingId);
            if (embedding == null) return false;

            var embeddingVektor = await _ollamaService.EmbeddingOlusturAsync(yeniMetin);
            if (embeddingVektor == null) return false;

            embedding.Metin = yeniMetin.Length > 8000 ? yeniMetin[..8000] : yeniMetin;
            embedding.MetinOzet = yeniMetin.Length > 500 ? yeniMetin[..500] : yeniMetin;
            embedding.Embedding = embeddingVektor;
            embedding.ModelAdi = _ollamaService.EmbeddingModelAdi;
            embedding.GuncellemeTarihi = DateTime.Now;

            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Embedding güncelleme hatası: Id={EmbeddingId}", embeddingId);
            return false;
        }
    }

    /// <summary>
    /// Belirli bir kaynağın embedding'ini siler
    /// </summary>
    public async Task<bool> EmbeddingSilAsync(int embeddingId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var embedding = await context.EbysBelgeEmbeddingler.FindAsync(embeddingId);
            if (embedding == null) return false;

            context.EbysBelgeEmbeddingler.Remove(embedding);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Embedding silme hatası: Id={EmbeddingId}", embeddingId);
            return false;
        }
    }

    /// <summary>
    /// Kaynak bazlı embedding siler
    /// </summary>
    public async Task<int> KaynakEmbeddingSilAsync(EbysAramaKaynak kaynak, int kaynakId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        try
        {
            var embeddingler = await context.EbysBelgeEmbeddingler
                .Where(e => e.Kaynak == kaynak && e.KaynakId == kaynakId)
                .ToListAsync();

            context.EbysBelgeEmbeddingler.RemoveRange(embeddingler);
            await context.SaveChangesAsync();
            return embeddingler.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kaynak embedding silme hatası: Kaynak={Kaynak}, KaynakId={KaynakId}", kaynak, kaynakId);
            return 0;
        }
    }

    /// <summary>
    /// Semantic arama yapar - en benzer belgeleri döndürür
    /// </summary>
    public async Task<List<SemanticAramaSonuc>> SemanticAraAsync(string sorgu, int maxSonuc = 10, double minBenzerlik = 0.5)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await SemanticAraAsync(sorgu, [], maxSonuc, minBenzerlik);
    }

    /// <summary>
    /// Belirli kaynaklarda semantic arama yapar
    /// </summary>
    public async Task<List<SemanticAramaSonuc>> SemanticAraAsync(
        string sorgu, List<EbysAramaKaynak> kaynaklar, int maxSonuc = 10, double minBenzerlik = 0.5)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var sonuclar = new List<SemanticAramaSonuc>();

        if (string.IsNullOrWhiteSpace(sorgu))
            return sonuclar;

        try
        {
            // Sorgu için embedding oluştur
            var sorguEmbedding = await _ollamaService.EmbeddingOlusturAsync(sorgu);
            if (sorguEmbedding == null)
            {
                _logger.LogWarning("Sorgu embedding'i oluşturulamadı: {Sorgu}", sorgu);
                return sonuclar;
            }

            // Tüm embedding'leri getir (kaynak filtresiyle)
            var embeddingQuery = context.EbysBelgeEmbeddingler.AsQueryable();
            
            if (kaynaklar.Count > 0)
            {
                embeddingQuery = embeddingQuery.Where(e => kaynaklar.Contains(e.Kaynak));
            }

            var embeddingler = await embeddingQuery.ToListAsync();

            // Cosine similarity hesapla ve sırala
            foreach (var embedding in embeddingler)
            {
                var vektor = embedding.Embedding;
                if (vektor == null) continue;

                var benzerlik = CosineSimilarity(sorguEmbedding, vektor);
                
                if (benzerlik >= minBenzerlik)
                {
                    sonuclar.Add(new SemanticAramaSonuc
                    {
                        Embedding = embedding,
                        BenzerlikSkoru = benzerlik,
                        Kaynak = embedding.Kaynak.ToString(),
                        BelgeAdi = await BelgeAdiniGetirAsync(context, embedding.Kaynak, embedding.KaynakId),
                        Ozet = embedding.MetinOzet,
                        Tarih = embedding.OlusturmaTarihi,
                        DetayUrl = DetayUrlOlustur(embedding.Kaynak, embedding.KaynakId, embedding.DosyaId)
                    });
                }
            }

            // Benzerlik skoruna göre sırala ve limit uygula
            return sonuclar
                .OrderByDescending(s => s.BenzerlikSkoru)
                .Take(maxSonuc)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Semantic arama hatası: {Sorgu}", sorgu);
            return sonuclar;
        }
    }

    /// <summary>
    /// Tüm belgeleri indeksler (batch işlem)
    /// </summary>
    public async Task<EmbeddingIndekslemeRaporu> TumBelgeleriIndeksleAsync(
        IProgress<EmbeddingIndekslemeProgress>? progress = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var rapor = new EmbeddingIndekslemeRaporu
        {
            BaslangicZamani = DateTime.Now
        };

        try
        {
            // Tüm kaynakları sırayla indeksle
            foreach (EbysAramaKaynak kaynak in Enum.GetValues<EbysAramaKaynak>())
            {
                progress?.Report(new EmbeddingIndekslemeProgress
                {
                    Mesaj = $"{kaynak} belgeleri indeksleniyor...",
                    AktifKaynak = kaynak
                });

                var indekslenen = await KaynakBelgeleriniIndeksleAsync(kaynak, progress);
                rapor.KaynakBazliSayilar[kaynak] = indekslenen;
                rapor.BasariliIndekslenen += indekslenen;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Toplu indeksleme hatası");
            rapor.Hatalar.Add($"Genel hata: {ex.Message}");
        }

        rapor.BitisZamani = DateTime.Now;
        return rapor;
    }

    /// <summary>
    /// Belirli bir kaynak tipinin tüm belgelerini indeksler
    /// </summary>
    public async Task<int> KaynakBelgeleriniIndeksleAsync(
        EbysAramaKaynak kaynak, IProgress<EmbeddingIndekslemeProgress>? progress = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var indekslenenSayisi = 0;

        try
        {
            var belgeler = await BelgeMetinleriniGetirAsync(context, kaynak);
            var toplam = belgeler.Count;

            for (int i = 0; i < belgeler.Count; i++)
            {
                var belge = belgeler[i];
                
                progress?.Report(new EmbeddingIndekslemeProgress
                {
                    Mesaj = $"{kaynak}: {belge.Adi} indeksleniyor...",
                    Tamamlanan = i + 1,
                    Toplam = toplam,
                    AktifBelge = belge.Adi,
                    AktifKaynak = kaynak
                });

                var embedding = await EmbeddingOlusturVeKaydetAsync(
                    kaynak, belge.KaynakId, belge.DosyaId, belge.Metin);

                if (embedding != null)
                    indekslenenSayisi++;

                // Rate limiting - Ollama'yı boğmamak için
                await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kaynak indeksleme hatası: {Kaynak}", kaynak);
        }

        return indekslenenSayisi;
    }

    /// <summary>
    /// Embedding istatistiklerini döndürür
    /// </summary>
    public async Task<EmbeddingIstatistik> IstatistikleriGetirAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var istatistik = new EmbeddingIstatistik();

        try
        {
            var embeddingler = await context.EbysBelgeEmbeddingler.ToListAsync();
            
            istatistik.ToplamEmbedding = embeddingler.Count;
            istatistik.KaynakBazliSayilar = embeddingler
                .GroupBy(e => e.Kaynak)
                .ToDictionary(g => g.Key, g => g.Count());
            istatistik.SonIndekslemeTarihi = embeddingler
                .OrderByDescending(e => e.GuncellemeTarihi ?? e.OlusturmaTarihi)
                .Select(e => e.GuncellemeTarihi ?? e.OlusturmaTarihi)
                .FirstOrDefault();
            istatistik.KullanilanModel = embeddingler.FirstOrDefault()?.ModelAdi;
            istatistik.OrtalamaBoyut = embeddingler.Count > 0 
                ? (int)embeddingler.Average(e => e.EmbeddingBoyutu) 
                : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İstatistik getirme hatası");
        }

        return istatistik;
    }

    /// <summary>
    /// İki vektör arasındaki cosine similarity hesaplar
    /// </summary>
    public double CosineSimilarity(float[] vektor1, float[] vektor2)
    {
        if (vektor1.Length != vektor2.Length)
            return 0;

        double dotProduct = 0;
        double norm1 = 0;
        double norm2 = 0;

        for (int i = 0; i < vektor1.Length; i++)
        {
            dotProduct += vektor1[i] * vektor2[i];
            norm1 += vektor1[i] * vektor1[i];
            norm2 += vektor2[i] * vektor2[i];
        }

        var denominator = Math.Sqrt(norm1) * Math.Sqrt(norm2);
        return denominator == 0 ? 0 : dotProduct / denominator;
    }

    /// <summary>
    /// Ollama bağlantı ve embedding model kontrolü
    /// </summary>
    public async Task<bool> BaglantiKontrolAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await _ollamaService.BaglantiKontrolAsync();
    }

    #region Private Helper Methods

    /// <summary>
    /// Belge adını kaynağa göre getirir
    /// </summary>
    private async Task<string> BelgeAdiniGetirAsync(ApplicationDbContext context, EbysAramaKaynak kaynak, int kaynakId)
    {
        return kaynak switch
        {
            EbysAramaKaynak.PersonelOzluk => await GetPersonelOzlukAdiAsync(context, kaynakId),
            EbysAramaKaynak.AracEvrak => await GetAracEvrakAdiAsync(context, kaynakId),
            EbysAramaKaynak.GelenEvrak or EbysAramaKaynak.GidenEvrak => await GetEbysEvrakAdiAsync(context, kaynakId),
            _ => "Bilinmeyen Belge"
        };
    }

    private async Task<string> GetPersonelOzlukAdiAsync(ApplicationDbContext context, int id)
    {
        var evrak = await context.PersonelOzlukEvraklar
            .Include(e => e.Sofor)
            .Include(e => e.EvrakTanim)
            .FirstOrDefaultAsync(e => e.Id == id);
        return evrak != null 
            ? $"{evrak.Sofor?.Ad} {evrak.Sofor?.Soyad} - {evrak.EvrakTanim?.EvrakAdi}" 
            : "Personel Özlük Evrak";
    }

    private async Task<string> GetAracEvrakAdiAsync(ApplicationDbContext context, int id)
    {
        var evrak = await context.AracEvraklari
            .Include(e => e.Arac)
            .FirstOrDefaultAsync(e => e.Id == id);
        return evrak != null 
            ? $"{evrak.Arac?.Plaka} - {evrak.EvrakKategorisi}" 
            : "Araç Evrak";
    }

    private async Task<string> GetEbysEvrakAdiAsync(ApplicationDbContext context, int id)
    {
        var evrak = await context.EbysEvraklar.FirstOrDefaultAsync(e => e.Id == id);
        return evrak?.Konu ?? "EBYS Evrak";
    }

    /// <summary>
    /// Detay URL'si oluşturur
    /// </summary>
    private static string DetayUrlOlustur(EbysAramaKaynak kaynak, int kaynakId, int? dosyaId)
    {
        return kaynak switch
        {
            EbysAramaKaynak.PersonelOzluk => $"/personel/ozluk/{kaynakId}",
            EbysAramaKaynak.AracEvrak => $"/arac/evrak/{kaynakId}",
            EbysAramaKaynak.GelenEvrak => $"/ebys/gelen/{kaynakId}",
            EbysAramaKaynak.GidenEvrak => $"/ebys/giden/{kaynakId}",
            _ => "#"
        };
    }

    /// <summary>
    /// Belge metinlerini kaynağa göre getirir
    /// </summary>
    private async Task<List<BelgeMetin>> BelgeMetinleriniGetirAsync(ApplicationDbContext context, EbysAramaKaynak kaynak)
    {
        return kaynak switch
        {
            EbysAramaKaynak.PersonelOzluk => await GetPersonelOzlukMetinleriAsync(context),
            EbysAramaKaynak.AracEvrak => await GetAracEvrakMetinleriAsync(context),
            EbysAramaKaynak.GelenEvrak => await GetGelenEvrakMetinleriAsync(context),
            EbysAramaKaynak.GidenEvrak => await GetGidenEvrakMetinleriAsync(context),
            _ => []
        };
    }

    private async Task<List<BelgeMetin>> GetPersonelOzlukMetinleriAsync(ApplicationDbContext context)
    {
        var evraklar = await context.PersonelOzlukEvraklar
            .Include(e => e.Sofor)
            .Include(e => e.EvrakTanim)
            .Where(e => !e.IsDeleted)
            .ToListAsync();

        return evraklar.Select(e => new BelgeMetin
        {
            KaynakId = e.Id,
            DosyaId = null,
            Adi = $"{e.Sofor?.Ad} {e.Sofor?.Soyad} - {e.EvrakTanim?.EvrakAdi}",
            Metin = $"Personel: {e.Sofor?.Ad} {e.Sofor?.Soyad}\n" +
                    $"Evrak: {e.EvrakTanim?.EvrakAdi ?? ""}\n" +
                    $"Açıklama: {e.Aciklama ?? ""}\n" +
                    $"Durum: {(e.Tamamlandi ? "Tamamlandı" : "Bekliyor")}\n" +
                    $"Tarih: {e.TamamlanmaTarihi?.ToString("dd.MM.yyyy") ?? "-"}"
        }).ToList();
    }

    private async Task<List<BelgeMetin>> GetAracEvrakMetinleriAsync(ApplicationDbContext context)
    {
        var evraklar = await context.AracEvraklari
            .Include(e => e.Arac)
            .Where(e => !e.IsDeleted)
            .ToListAsync();

        return evraklar.Select(e => new BelgeMetin
        {
            KaynakId = e.Id,
            DosyaId = null,
            Adi = $"{e.Arac?.Plaka} - {e.EvrakKategorisi}",
            Metin = $"Araç: {e.Arac?.Plaka ?? ""}\n" +
                    $"Evrak Kategorisi: {e.EvrakKategorisi}\n" +
                    $"Evrak Adı: {e.EvrakAdi ?? ""}\n" +
                    $"Açıklama: {e.Aciklama ?? ""}\n" +
                    $"Başlangıç: {e.BaslangicTarihi?.ToString("dd.MM.yyyy") ?? "-"}\n" +
                    $"Bitiş: {e.BitisTarihi?.ToString("dd.MM.yyyy") ?? "-"}"
        }).ToList();
    }

    private async Task<List<BelgeMetin>> GetGelenEvrakMetinleriAsync(ApplicationDbContext context)
    {
        var evraklar = await context.EbysEvraklar
            .Include(e => e.Kategori)
            .Where(e => !e.IsDeleted && e.Yon == EvrakYonu.Gelen)
            .ToListAsync();

        return evraklar.Select(e => new BelgeMetin
        {
            KaynakId = e.Id,
            DosyaId = null,
            Adi = e.Konu,
            Metin = $"Konu: {e.Konu}\n" +
                    $"Gönderen: {e.GonderenKurum ?? ""}\n" +
                    $"Kategori: {e.Kategori?.KategoriAdi ?? ""}\n" +
                    $"Özet: {e.Ozet ?? ""}\n" +
                    $"Açıklama: {e.Aciklama ?? ""}\n" +
                    $"Tarih: {e.EvrakTarihi:dd.MM.yyyy}"
        }).ToList();
    }

    private async Task<List<BelgeMetin>> GetGidenEvrakMetinleriAsync(ApplicationDbContext context)
    {
        var evraklar = await context.EbysEvraklar
            .Include(e => e.Kategori)
            .Where(e => !e.IsDeleted && e.Yon == EvrakYonu.Giden)
            .ToListAsync();

        return evraklar.Select(e => new BelgeMetin
        {
            KaynakId = e.Id,
            DosyaId = null,
            Adi = e.Konu,
            Metin = $"Konu: {e.Konu}\n" +
                    $"Alıcı: {e.AliciKurum ?? ""}\n" +
                    $"Kategori: {e.Kategori?.KategoriAdi ?? ""}\n" +
                    $"Özet: {e.Ozet ?? ""}\n" +
                    $"Açıklama: {e.Aciklama ?? ""}\n" +
                    $"Tarih: {e.EvrakTarihi:dd.MM.yyyy}"
        }).ToList();
    }

    #endregion
}

/// <summary>
/// Belge metin bilgisi (indeksleme için)
/// </summary>
internal class BelgeMetin
{
    public int KaynakId { get; set; }
    public int? DosyaId { get; set; }
    public string Adi { get; set; } = string.Empty;
    public string Metin { get; set; } = string.Empty;
}


