using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

/// <summary>
/// Semantic Search servisi interface'i
/// Belge embedding'leri oluşturma, indeksleme ve semantik arama işlemleri
/// </summary>
public interface ISemanticSearchService
{
    /// <summary>
    /// Tek bir belge için embedding oluşturur ve kaydeder
    /// </summary>
    Task<EbysBelgeEmbedding?> EmbeddingOlusturVeKaydetAsync(EbysAramaKaynak kaynak, int kaynakId, int? dosyaId, string metin);
    
    /// <summary>
    /// Belirli bir kaynak için embedding'i günceller
    /// </summary>
    Task<bool> EmbeddingGuncelleAsync(int embeddingId, string yeniMetin);
    
    /// <summary>
    /// Belirli bir kaynağın embedding'ini siler
    /// </summary>
    Task<bool> EmbeddingSilAsync(int embeddingId);
    
    /// <summary>
    /// Kaynak bazlı embedding siler
    /// </summary>
    Task<int> KaynakEmbeddingSilAsync(EbysAramaKaynak kaynak, int kaynakId);
    
    /// <summary>
    /// Semantic arama yapar - en benzer belgeleri döndürür
    /// </summary>
    Task<List<SemanticAramaSonuc>> SemanticAraAsync(string sorgu, int maxSonuc = 10, double minBenzerlik = 0.5);
    
    /// <summary>
    /// Belirli kaynaklarda semantic arama yapar
    /// </summary>
    Task<List<SemanticAramaSonuc>> SemanticAraAsync(string sorgu, List<EbysAramaKaynak> kaynaklar, int maxSonuc = 10, double minBenzerlik = 0.5);
    
    /// <summary>
    /// Tüm belgeleri indeksler (batch işlem)
    /// </summary>
    Task<EmbeddingIndekslemeRaporu> TumBelgeleriIndeksleAsync(IProgress<EmbeddingIndekslemeProgress>? progress = null);
    
    /// <summary>
    /// Belirli bir kaynak tipinin tüm belgelerini indeksler
    /// </summary>
    Task<int> KaynakBelgeleriniIndeksleAsync(EbysAramaKaynak kaynak, IProgress<EmbeddingIndekslemeProgress>? progress = null);
    
    /// <summary>
    /// Embedding istatistiklerini döndürür
    /// </summary>
    Task<EmbeddingIstatistik> IstatistikleriGetirAsync();
    
    /// <summary>
    /// İki vektör arasındaki cosine similarity hesaplar
    /// </summary>
    double CosineSimilarity(float[] vektor1, float[] vektor2);
    
    /// <summary>
    /// Ollama bağlantı ve embedding model kontrolü
    /// </summary>
    Task<bool> BaglantiKontrolAsync();
}

/// <summary>
/// Embedding indeksleme ilerleme bilgisi
/// </summary>
public class EmbeddingIndekslemeProgress
{
    public string Mesaj { get; set; } = string.Empty;
    public int Tamamlanan { get; set; }
    public int Toplam { get; set; }
    public double Yuzde => Toplam > 0 ? (double)Tamamlanan / Toplam * 100 : 0;
    public string? AktifBelge { get; set; }
    public EbysAramaKaynak? AktifKaynak { get; set; }
}

/// <summary>
/// Embedding indeksleme raporu
/// </summary>
public class EmbeddingIndekslemeRaporu
{
    public DateTime BaslangicZamani { get; set; }
    public DateTime BitisZamani { get; set; }
    public TimeSpan Sure => BitisZamani - BaslangicZamani;
    
    public int ToplamBelge { get; set; }
    public int BasariliIndekslenen { get; set; }
    public int BasarisizIndekslenen { get; set; }
    public int AtlananBelge { get; set; }
    
    public Dictionary<EbysAramaKaynak, int> KaynakBazliSayilar { get; set; } = [];
    public List<string> Hatalar { get; set; } = [];
}

/// <summary>
/// Embedding istatistikleri
/// </summary>
public class EmbeddingIstatistik
{
    public int ToplamEmbedding { get; set; }
    public Dictionary<EbysAramaKaynak, int> KaynakBazliSayilar { get; set; } = [];
    public DateTime? SonIndekslemeTarihi { get; set; }
    public string? KullanilanModel { get; set; }
    public int OrtalamaBoyut { get; set; }
}




