using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services;

public interface IPersonelOzlukService
{
    // Evrak Tanımları
    Task<List<OzlukEvrakTanim>> GetEvrakTanimlariAsync();
    Task<List<OzlukEvrakTanim>> GetAktifEvrakTanimlariAsync();
    Task<OzlukEvrakTanim?> GetEvrakTanimByIdAsync(int id);
    Task<OzlukEvrakTanim> CreateEvrakTanimAsync(OzlukEvrakTanim tanim);
    Task<OzlukEvrakTanim> UpdateEvrakTanimAsync(OzlukEvrakTanim tanim);
    Task DeleteEvrakTanimAsync(int id);
    Task SeedDefaultEvrakTanimlariAsync();

    // Personel Evrakları
    Task<List<PersonelOzlukEvrak>> GetPersonelEvraklariAsync(int soforId);
    Task<PersonelOzlukEvrak?> GetPersonelEvrakByIdAsync(int evrakId);
    Task<PersonelOzlukEvrakDurum> GetPersonelEvrakDurumuAsync(int soforId);
    Task<List<PersonelOzlukEvrakDurum>> GetTumPersonelEvrakDurumlariAsync();
    Task<List<PersonelEvrakDosyaListeItem>> GetEvrakDosyaListesiAsync(int soforId, int evrakTanimId);
    Task<PersonelOzlukEvrak> EvrakIsaretle(int soforId, int evrakTanimId, bool tamamlandi, string? aciklama = null);
    Task<PersonelOzlukEvrak> EvrakDosyaYukle(int soforId, int evrakTanimId, string dosyaYolu,
        string? dosyaAdi = null, string? dosyaTipi = null, long? dosyaBoyutu = null);
    Task<PersonelOzlukEvrak> EvrakDuzenle(int soforId, int evrakTanimId, string? aciklama = null,
        DateTime? gecerlilikBitisTarihi = null, string? sonDegisiklikNotu = null);
    Task<PersonelEvrakDosyaIcerik?> GetGuncelEvrakDosyaIcerigiAsync(int soforId, int evrakTanimId);
    Task<PersonelEvrakDosyaIcerik?> GetEvrakDosyaIcerigiAsync(int soforId, int evrakTanimId, int? personelEvrakId, int? versiyonId);
    Task DeleteEvrakDosyaAsync(int soforId, int evrakTanimId, int? personelEvrakId, int? versiyonId);
    Task<PersonelOzlukEvrak?> BelgeAlaniIleDosyaYukleAsync(int soforId, string belgeAlani, string dosyaYolu);
    Task<PersonelOzlukEvrak> UpdatePersonelEvrakAsync(PersonelOzlukEvrak evrak);
    Task SoforBelgeTarihleriniSenkronizeEtAsync(int soforId, DateTime? ehliyetTarihi, DateTime? srcTarihi, DateTime? psikoteknikTarihi, DateTime? saglikTarihi);

    // Raporlama
    Task<List<PersonelOzlukEvrakDurum>> GetEksikEvrakliPersonellerAsync();
    Task<byte[]> ExportChecklistPdfAsync(int soforId);
    Task<byte[]> ExportTumChecklistExcelAsync();
    Task<byte[]> ExportPersonelDosyaPdfAsync(int soforId);
    Task<byte[]> ExportBosPersonelDosyaPdfAsync();
}

public class PersonelOzlukEvrakDurum
{
    public int SoforId { get; set; }
    public string PersonelAdi { get; set; } = string.Empty;
    public string PersonelKodu { get; set; } = string.Empty;
    public PersonelGorev Gorev { get; set; }
    public int ToplamEvrak { get; set; }
    public int TamamlananEvrak { get; set; }
    public int EksikEvrak { get; set; }
    public decimal TamamlanmaYuzdesi { get; set; }
    public List<OzlukEvrakDetay> Evraklar { get; set; } = new();
}

public class OzlukEvrakDetay
{
    public int EvrakTanimId { get; set; }
    public string EvrakAdi { get; set; } = string.Empty;
    public OzlukEvrakKategori Kategori { get; set; }
    public bool Zorunlu { get; set; }
    public bool Tamamlandi { get; set; }
    public DateTime? TamamlanmaTarihi { get; set; }
    public DateTime? GecerlilikBitisTarihi { get; set; }
    public string? DosyaYolu { get; set; }
    public string? DosyaAdi { get; set; }
    public string? DosyaTipi { get; set; }
    public long DosyaBoyutu { get; set; }
    public string? Aciklama { get; set; }
    public int VersiyonNo { get; set; } = 1;
    public string? SonDegisiklikNotu { get; set; }
}

public class PersonelEvrakDosyaIcerik
{
    public int EvrakTanimId { get; set; }
    public string DosyaAdi { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public byte[] Icerik { get; set; } = Array.Empty<byte>();
}

public class PersonelEvrakDosyaListeItem
{
    public int? PersonelEvrakId { get; set; }
    public int? VersiyonId { get; set; }
    public bool GuncelKayit { get; set; }
    public int EvrakTanimId { get; set; }
    public string DosyaAdi { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long DosyaBoyutu { get; set; }
    public DateTime Tarih { get; set; }
}
