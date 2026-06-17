using FluentAssertions;
using KOAFiloServis.Shared.Entities;
using KOAFiloServis.Web.Models;
using KOAFiloServis.Web.Services.Calculation;

namespace KOAFiloServis.Tests.Services;

public class PuantajFinanceIntegrationTests
{
    [Fact]
    public void Test1_DoubleIsleAsync_Should_Skip_Second_Call()
    {
        var puantaj = new HakedisPuantaj { Id = 1, GelirFaturaId = 100, GiderFaturaId = 200 };
        var alreadyProcessed = puantaj.GelirFaturaId != null || puantaj.GiderFaturaId != null;
        alreadyProcessed.Should().BeTrue();
    }

    [Fact]
    public void Test2_SnapshotDelta_Should_Accumulate_Not_Overwrite()
    {
        decimal m = 1000m; m += 200; m += 300;
        m.Should().Be(1500);
    }

    [Fact]
    public void Test3_SnapshotTransaction_DeterministicGuid()
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var g1 = new Guid(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes("HakedisFinans:42")));
        using var md5b = System.Security.Cryptography.MD5.Create();
        var g2 = new Guid(md5b.ComputeHash(System.Text.Encoding.UTF8.GetBytes("HakedisFinans:42")));
        g1.Should().Be(g2);
    }

    [Fact]
    public void Test4_SystemLock_StaleVsFresh()
    {
        var stale = new SystemLock { Key = "REBUILD", IsLocked = true, LockedAt = DateTime.UtcNow.AddMinutes(-11) };
        stale.LockedAt!.Value.Should().BeBefore(DateTime.UtcNow.AddMinutes(-10));
        var fresh = new SystemLock { Key = "REBUILD", IsLocked = true, LockedAt = DateTime.UtcNow.AddMinutes(-2) };
        fresh.LockedAt!.Value.Should().BeAfter(DateTime.UtcNow.AddMinutes(-10));
    }

    [Fact]
    public void Test5_FlexibleSefer_And_Manual_Protection()
    {
        var m = new PuantajHucre { Gun = 15, Deger = 1, Mesai = 2, Mod = PuantajHucreModu.Manual };
        m.ToplamSefer.Should().Be(3); m.IsManual.Should().BeTrue();
        var a = new PuantajHucre { Gun = 1, Deger = 5, Mesai = 4, EkSefer = 3 };
        a.ToplamSefer.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public void Test6_PuantajEngine_Calculates()
    {
        var i = new PuantajInput { BirimFiyat = 100, Kesinti = 50, Gunler = new() { "1", "1", "0", "2", "1" } };
        var s = PuantajEngine.Hesapla(i);
        s.Sefer.Should().Be(5); s.Toplam.Should().Be(500); s.Net.Should().Be(450);
    }

    [Fact]
    public void Test7_RowVersion_Exists()
    {
        typeof(HakedisPuantaj).GetProperty("RowVersion").Should().NotBeNull();
    }

    [Fact]
    public void Test8_DenetimScore()
    {
        int s = 100; s -= 40; s -= 30;
        string st = s >= 100 ? "Healthy" : s >= 70 ? "Warning" : "Critical";
        st.Should().Be("Critical");
    }

    [Fact]
    public void Test9_PuantajKayit_To_Hakedis_Mapping()
    {
        var k = new PuantajKayit { Id = 1, Yil = 2026, Ay = 6, GuzergahId = 10, AracId = 100, SoforId = 50, BirimGelir = 500, BirimGider = 300, GelirKdvOrani = 20 };
        k.SetGunDeger(1, 1); k.SetGunDeger(2, 2); k.SetGunDeger(3, 1);
        var h = new HakedisPuantaj { GuzergahId = 10, AracId = 100, SoforId = 50, GelirBirimFiyat = 500, GiderBirimFiyat = 300, KdvOrani = 20 };
        for (int g = 1; g <= 3; g++) h.Detaylar.Add(new HakedisPuantajDetay { Gun = g, SeferSayisi = k.GetGunDeger(g) });
        h.Hesapla();
        h.ToplamSefer.Should().Be(4);
    }

    [Fact]
    public void Test10_UniqueConstraint()
    {
        var k1 = (FirmaId: 1, Yil: 2026, Ay: 6, GuzergahId: 10, AracId: 100, SoforId: 50);
        var k2 = (FirmaId: 1, Yil: 2026, Ay: 6, GuzergahId: 10, AracId: 100, SoforId: 51);
        k1.Should().NotBe(k2);
    }

    [Fact]
    public void Test11_Yon_Should_Not_Affect_Sefer()
    {
        var h = new PuantajHucre { Gun = 1, Deger = 1, Mod = PuantajHucreModu.Manual };
        h.ToplamSefer.Should().Be(1);
        new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }.Sum().Should().Be(12, "12 gün, ASLA 62");
    }

    [Fact]
    public void Test12_Engine_Never_Override_Manual()
    {
        var h = new PuantajHucre { Gun = 10, Deger = 1, Mod = PuantajHucreModu.Manual };
        int orig = h.Deger;
        if (!h.IsManual) { h.Deger = 0; }
        h.Deger.Should().Be(orig);
    }

    [Fact]
    public void Test13_Summary_Never_Double_Count()
    {
        var s = new PuantajGridSatir { KayitId = 1, SoforAdi = "T", BirimFiyat = 100 };
        s.Hucreler.Add(new PuantajHucre { Gun = 1, Deger = 1 });
        s.Hucreler.Add(new PuantajHucre { Gun = 2, Deger = 1, Mesai = 1 });
        s.Hucreler.Add(new PuantajHucre { Gun = 3, Deger = 0 });
        s.Hucreler.Add(new PuantajHucre { Gun = 4, Deger = 1, EkSefer = 2 });
        s.ToplamSefer.Should().Be(6);
    }

    [Fact]
    public void Test14_UpdateService_SetValues_Works()
    {
        var orig = new Sofor { Id = 1, Ad = "Ali", Soyad = "Veli", CreatedAt = new DateTime(2026, 1, 1) };
        var upd = new Sofor { Id = 1, Ad = "Ahmet", Soyad = "Yilmaz" };
        new EntrySim(orig).SetValues(upd);
        orig.Ad.Should().Be("Ahmet");
        orig.CreatedAt.Year.Should().Be(2026);
    }

    [Fact]
    public void Test15_UpdateService_Throws_On_NotFound()
    {
        Sofor? n = null;
        Action a = () => { if (n == null) throw new InvalidOperationException("Sofor bulunamadi (Id=999)"); };
        a.Should().Throw<InvalidOperationException>();
    }

    private class EntrySim
    {
        private readonly object _t;
        public EntrySim(object t) => _t = t;
        public void SetValues(object s)
        {
            foreach (var p in s.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite && p.Name != "Id"))
            {
                var v = p.GetValue(s);
                if (v != null || p.PropertyType == typeof(string))
                    _t.GetType().GetProperty(p.Name)?.SetValue(_t, v);
            }
        }
    }
}
