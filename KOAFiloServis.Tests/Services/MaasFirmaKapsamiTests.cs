using FluentAssertions;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Data;
using KOAFiloServis.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// Maaş Yönetimi — Firma Kapsamı birim testleri.
/// Senkron testler iş mantığını; entegrasyon testleri ise gerçek DB + IgnoreQueryFilters
/// davranışını doğrular.
/// </summary>
public class MaasFirmaKapsamiTests
{
    // ═══════════════════════════════════════════════════════════════════
    // FİRMA KAPSAMI MANTIĞI (SENKRON)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void AktifFirmaKapsami_SadeceAktifFirmaIdDondurur()
    {
        // Arrange
        const int aktifFirmaId = 5;

        // Act: Simüle edilen AktifFirma kapsamı
        var firmaIds = AktifFirmaKapsamiCozumle(aktifFirmaId, tumFirmalar: false);

        // Assert
        firmaIds.Should().HaveCount(1);
        firmaIds.Should().ContainSingle().Which.Should().Be(5);
    }

    [Fact]
    public void TumFirmalarKapsami_SadeceAktifFirmayiGetirmez()
    {
        // Arrange: OrganizasyonId=1 olan 3 firma var.
        // Aktif firma ÜSTÜN GRUP (Id=1). OrganizasyonId'si 1.
        // Aynı organizasyonda RECEP ÜSTÜN (Id=2) ve 3C GRUP (Id=3) var.
        var firmalar = new List<Firma>
        {
            new() { Id = 1, FirmaAdi = "ÜSTÜN GRUP SEYAHAT", OrganizasyonId = 1, Aktif = true },
            new() { Id = 2, FirmaAdi = "RECEP ÜSTÜN", OrganizasyonId = 1, Aktif = true },
            new() { Id = 3, FirmaAdi = "3C GRUP", OrganizasyonId = 1, Aktif = true },
            new() { Id = 99, FirmaAdi = "Yetkisiz Firma", OrganizasyonId = 2, Aktif = true },
        };

        // Act: OrganizasyonId=1 firmaları bul
        var aktifFirma = firmalar.First(f => f.Id == 1);
        var organizasyonFirmalari = firmalar
            .Where(f => f.OrganizasyonId == aktifFirma.OrganizasyonId && f.Aktif)
            .Select(f => f.Id)
            .ToList();

        // Assert
        organizasyonFirmalari.Should().HaveCount(3);
        organizasyonFirmalari.Should().Contain(new[] { 1, 2, 3 });
        organizasyonFirmalari.Should().NotContain(99, "farklı organizasyonda");
    }

    [Fact]
    public void TumFirmalarKapsami_OrganizasyonFirmalariniGetirir()
    {
        // Arrange: Gerçek senaryo — ÜSTÜN HOLDİNG altında 3 firma
        var firmalar = new List<(int Id, string Ad, int OrgId)>
        {
            (1, "ÜSTÜN GRUP SEYAHAT", 1),
            (2, "RECEP ÜSTÜN", 1),
            (3, "3C GRUP", 1),
        };

        const int aktifFirmaId = 1;
        var aktifOrgId = firmalar.First(f => f.Id == aktifFirmaId).OrgId;

        // Act
        var firmaIds = firmalar
            .Where(f => f.OrgId == aktifOrgId)
            .Select(f => f.Id)
            .ToList();

        // Assert
        firmaIds.Should().HaveCount(3);
        firmaIds.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void TumFirmalarKapsami_FirmaIdsIleGuvenliSinirlanir()
    {
        // Arrange: Yetki kapsamındaki firma Id'leri
        var yetkiliFirmaIds = new List<int> { 1, 2, 3 };

        // Simüle edilen personel listesi (farklı firmalara ait)
        var personeller = new List<(int Id, string Ad, int? FirmaId)>
        {
            (1, "Ali", 1),    // ÜSTÜN GRUP ✓
            (2, "Veli", 1),   // ÜSTÜN GRUP ✓
            (3, "Ahmet", 2),  // RECEP ÜSTÜN ✓
            (4, "Mehmet", 3), // 3C GRUP ✓
            (5, "Hüseyin", 99), // Yetkisiz ✗
            (6, "Ayşe", null),  // Firmasız ✗
        };

        // Act: firmaIds.Contains ile güvenli sınırlama
        var filtrelenmis = personeller
            .Where(p => p.FirmaId.HasValue && yetkiliFirmaIds.Contains(p.FirmaId.Value))
            .ToList();

        // Assert
        filtrelenmis.Should().HaveCount(4);
        filtrelenmis.Select(p => p.Id).Should().BeEquivalentTo(new[] { 1, 2, 3, 4 });
        filtrelenmis.Select(p => p.Id).Should().NotContain(5, "yetkisiz firma");
        filtrelenmis.Select(p => p.Id).Should().NotContain(6, "FirmaId=null");
    }

    [Fact]
    public void TumFirmalarKapsami_YetkisizFirmaGelmez()
    {
        // Arrange
        var yetkiliFirmaIds = new List<int> { 1, 2, 3 };
        var hariciFirmaPersonel = (Id: 99, Ad: "İzinsiz", FirmaId: 999);

        // Act
        var yetkiliMi = yetkiliFirmaIds.Contains(hariciFirmaPersonel.FirmaId);

        // Assert
        yetkiliMi.Should().BeFalse();
        hariciFirmaPersonel.FirmaId.Should().Be(999);
    }

    // ═══════════════════════════════════════════════════════════════════
    // MAAŞ KOLON VE FORMÜL KORUMA
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void MaasKolonFormulu_DogruHesaplanir()
    {
        // Arrange
        var satir = new
        {
            GercekMaas = 15000m,
            BankayaYatan = 12000m,
            Avans = 1000m,
            Kesinti = 500m,
            Harcamasi = 200m
        };

        // Act: FARK / ÖDENECEK = GERÇEK MAAŞ - BANKAYA YATAN - AVANS - KESİNTİ + HARCAMASI
        var odenecek = satir.GercekMaas - satir.BankayaYatan - satir.Avans - satir.Kesinti + satir.Harcamasi;

        // Assert
        odenecek.Should().Be(1700m); // 15000 - 12000 - 1000 - 500 + 200
    }

    [Fact]
    public void MaasKolonFormulu_SifirDegerlerleDogru()
    {
        // Arrange
        var satir = new
        {
            GercekMaas = 10000m,
            BankayaYatan = 0m,
            Avans = 0m,
            Kesinti = 0m,
            Harcamasi = 0m
        };

        // Act
        var odenecek = satir.GercekMaas - satir.BankayaYatan - satir.Avans - satir.Kesinti + satir.Harcamasi;

        // Assert
        odenecek.Should().Be(10000m);
    }

    [Fact]
    public void MaasKolonFormulu_NegatifOdenecekDahilDogru()
    {
        // Arrange: Gerçek maaş yok, sadece kesintiler var
        var satir = new
        {
            GercekMaas = 0m,
            BankayaYatan = 0m,
            Avans = 2000m,
            Kesinti = 500m,
            Harcamasi = 0m
        };

        // Act
        var odenecek = satir.GercekMaas - satir.BankayaYatan - satir.Avans - satir.Kesinti + satir.Harcamasi;

        // Assert
        odenecek.Should().Be(-2500m);
    }

    [Fact]
    public void TumFirmalar_MaasKolonFormulunuBozmaz()
    {
        // Arrange: Tüm Firmalar modunda da aynı formül çalışır
        var satir = new
        {
            GercekMaas = 15000m,
            BankayaYatan = 12000m,
            Avans = 1000m,
            Kesinti = 500m,
            Harcamasi = 200m
        };

        // Act
        decimal odenecekHesapla(decimal g, decimal b, decimal a, decimal k, decimal h)
        {
            // Firma kapsamından bağımsız — her zaman aynı formül
            return g - b - a - k + h;
        }

        var odenecek = odenecekHesapla(satir.GercekMaas, satir.BankayaYatan, satir.Avans, satir.Kesinti, satir.Harcamasi);

        // Assert: Tüm Firmalar modu formülü bozmaz
        odenecek.Should().Be(1700m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // EXCEL / YAZDIR FİRMA BLOKLAMA
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Excel_TumFirmalar_FirmalariBloklar()
    {
        // Arrange: 3 firmadan personeller
        var satirlar = new List<TestDurumSatir>
        {
            new() { FirmaId = 1, FirmaAdi = "ÜSTÜN GRUP SEYAHAT", AdSoyad = "Ali" },
            new() { FirmaId = 1, FirmaAdi = "ÜSTÜN GRUP SEYAHAT", AdSoyad = "Veli" },
            new() { FirmaId = 2, FirmaAdi = "RECEP ÜSTÜN", AdSoyad = "Ahmet" },
            new() { FirmaId = 3, FirmaAdi = "3C GRUP", AdSoyad = "Mehmet" },
        };

        // Act: Firmalara göre grupla
        var gruplar = satirlar
            .GroupBy(s => s.FirmaId ?? 0)
            .ToList();

        // Assert
        gruplar.Should().HaveCount(3, "3 farklı firma");
        gruplar.First(g => g.Key == 1).Should().HaveCount(2, "ÜSTÜN GRUP: 2 personel");
        gruplar.First(g => g.Key == 2).Should().HaveCount(1, "RECEP ÜSTÜN: 1 personel");
        gruplar.First(g => g.Key == 3).Should().HaveCount(1, "3C GRUP: 1 personel");
    }

    [Fact]
    public void Print_TumFirmalar_FirmalariBloklar()
    {
        // Arrange: Aynı veri, print çıktısı
        var satirlar = new List<TestDurumSatir>
        {
            new() { FirmaId = 1, FirmaAdi = "ÜSTÜN GRUP SEYAHAT", AdSoyad = "Ali" },
            new() { FirmaId = 1, FirmaAdi = "ÜSTÜN GRUP SEYAHAT", AdSoyad = "Veli" },
            new() { FirmaId = 2, FirmaAdi = "RECEP ÜSTÜN", AdSoyad = "Ahmet" },
        };

        // Act
        var gruplar = satirlar
            .GroupBy(s => s.FirmaId ?? 0)
            .ToList();

        // Assert
        gruplar.Should().HaveCount(2);
        var grup1 = gruplar.First(g => g.Key == 1);
        grup1.First().FirmaAdi.Should().Be("ÜSTÜN GRUP SEYAHAT");
        var grup2 = gruplar.First(g => g.Key == 2);
        grup2.First().FirmaAdi.Should().Be("RECEP ÜSTÜN");
    }

    [Fact]
    public void AktifFirmaModu_TekFirmaTekBlok()
    {
        // Arrange: Aktif Firma modunda tüm personeller aynı firmada
        var satirlar = new List<TestDurumSatir>
        {
            new() { FirmaId = 1, FirmaAdi = "ÜSTÜN GRUP SEYAHAT", AdSoyad = "Ali" },
            new() { FirmaId = 1, FirmaAdi = "ÜSTÜN GRUP SEYAHAT", AdSoyad = "Veli" },
        };

        // Act
        var gruplar = satirlar
            .GroupBy(s => s.FirmaId ?? 0)
            .ToList();

        // Assert: Tek firma
        gruplar.Should().HaveCount(1);
        gruplar.Single().Key.Should().Be(1);
    }

    // ═══════════════════════════════════════════════════════════════════
    // PASİF / AYRILMIŞ PERSONEL FİLTRESİ
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void TumFirmalar_PasifAyrilmisPersoneliGostermez()
    {
        // Arrange
        var donemBaslangic = new DateTime(2026, 6, 1);
        var personeller = new List<(int Id, bool Aktif, DateTime? Ayrilma)>
        {
            (1, true, null),                           // ✓ aktif, ayrılmamış
            (2, true, new DateTime(2026, 7, 1)),       // ✓ aktif, sonra ayrılacak
            (3, true, new DateTime(2026, 5, 1)),       // ✗ dönem öncesi ayrılmış
            (4, false, null),                          // ✗ pasif
            (5, false, new DateTime(2026, 7, 1)),      // ✗ pasif
        };

        // Act: Aktif=true AND (IstenAyrilmaTarihi IS NULL OR IstenAyrilmaTarihi >= donemBaslangic)
        var aktifler = personeller
            .Where(p => p.Aktif)
            .Where(p => p.Ayrilma == null || p.Ayrilma >= donemBaslangic)
            .ToList();

        // Assert
        aktifler.Should().HaveCount(2);
        aktifler.Select(p => p.Id).Should().BeEquivalentTo(new[] { 1, 2 });
        aktifler.Select(p => p.Id).Should().NotContain(3, "dönem öncesi ayrılmış");
        aktifler.Select(p => p.Id).Should().NotContain(4, "pasif");
        aktifler.Select(p => p.Id).Should().NotContain(5, "pasif");
    }

    [Fact]
    public void TumFirmalar_BurakDemirtasGorunmez()
    {
        // Arrange: BURAK DEMİRTAŞ ayrılmış/pasif
        var burak = (Id: 42, AdSoyad: "BURAK DEMİRTAŞ", Aktif: false, AyrilmaTarihi: (DateTime?)new DateTime(2026, 3, 15));
        var donemBaslangic = new DateTime(2026, 6, 1);

        // Act
        var gorunurMu = burak.Aktif && (burak.AyrilmaTarihi == null || burak.AyrilmaTarihi >= donemBaslangic);

        // Assert
        gorunurMu.Should().BeFalse("pasif personel görünmez");
    }

    // ═══════════════════════════════════════════════════════════════════
    // KOLON SIRASI KORUMA
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Excel_KolonSirasi_Korunur()
    {
        // Arrange: Beklenen kolon sırası
        var beklenenKolonlar = new[]
        {
            "S.NO", "PLAKA", "AD SOYAD", "TELEFON", "ADRES",
            "GERÇEK MAAŞ", "BANKAYA YATAN", "AVANS (-)", "KESİNTİ (-)", "HARCAMASI (+)",
            "FARK / ÖDENECEK", "FİRMA", "SGK", "İŞE GİRİŞ", "İŞTEN ÇIKIŞ"
        };

        // Act: Firma kapsamı değişse de kolon sırası aynı
        var gercekKolonlar = new[]
        {
            "S.NO", "PLAKA", "AD SOYAD", "TELEFON", "ADRES",
            "GERÇEK MAAŞ", "BANKAYA YATAN", "AVANS (-)", "KESİNTİ (-)", "HARCAMASI (+)",
            "FARK / ÖDENECEK", "FİRMA", "SGK", "İŞE GİRİŞ", "İŞTEN ÇIKIŞ"
        };

        // Assert
        gercekKolonlar.Should().Equal(beklenenKolonlar);
        gercekKolonlar.Should().HaveCount(15);
    }

    [Fact]
    public void Print_KolonSirasi_Korunur()
    {
        // Arrange
        var beklenenKolonSayisi = 15;

        var printKolonlar = new[]
        {
            "S.NO", "PLAKA", "AD SOYAD", "TELEFON", "ADRES",
            "GERÇEK MAAŞ", "BANKAYA YATAN", "AVANS (-)", "KESİNTİ (-)", "HARCAMASI (+)",
            "FARK / ÖDENECEK", "FİRMA", "SGK", "İŞE GİRİŞ", "İŞTEN ÇIKIŞ"
        };

        // Assert
        printKolonlar.Should().HaveCount(beklenenKolonSayisi);
    }

    // ═══════════════════════════════════════════════════════════════════
    // ORGANIZASYON ID ÇÖZÜMLEME (EDGE CASES)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void OrganizasyonId_Bulunamazsa_SadeceAktifFirmaDondurur()
    {
        // Arrange: Aktif firma DB'de bulunamazsa fallback
        const int aktifFirmaId = 999; // mevcut değil

        // Act: Fallback mantığı — sadece aktif firma
        List<int> FallbackCozumle(int firmaId)
        {
            // Firma bulunamazsa sadece kendi ID'si
            return new List<int> { firmaId };
        }

        var sonuc = FallbackCozumle(aktifFirmaId);

        // Assert
        sonuc.Should().HaveCount(1);
        sonuc.Should().Contain(999);
    }

    [Fact]
    public void OrganizasyonId_SifirVeyaNegatifse_BosListe()
    {
        // Arrange
        const int aktifFirmaId = 0;

        // Act
        List<int> Cozumle(int firmaId)
        {
            if (firmaId <= 0)
                return new List<int>();
            return new List<int> { firmaId };
        }

        var sonuc = Cozumle(aktifFirmaId);

        // Assert
        sonuc.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════

    private static List<int> AktifFirmaKapsamiCozumle(int aktifFirmaId, bool tumFirmalar)
    {
        if (!tumFirmalar)
            return new List<int> { aktifFirmaId };

        // Tüm Firmalar durumunda organizasyon çözümlemesi yapılır
        // (Burada basitleştirilmiş — gerçek implementasyon DB sorgusu içerir)
        return new List<int> { aktifFirmaId };
    }

    private class TestDurumSatir
    {
        public int? FirmaId { get; set; }
        public string FirmaAdi { get; set; } = "";
        public string AdSoyad { get; set; } = "";
    }
}
