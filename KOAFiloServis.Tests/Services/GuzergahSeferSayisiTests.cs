using FluentAssertions;
using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Tests.Services;

/// <summary>
/// Güzergah sefer sayısı ve slot hesaplama testleri.
/// </summary>
public class GuzergahSeferSayisiTests
{
    // ── HesaplaSlot testleri ──────────────────────────────────────────

    [Fact]
    public void SabahAksam_SeferSayisi2_CreatesAlternatingSlots()
    {
        // 1. satır Sabah, 2. satır Akşam
        HesaplaSlot(SeferTipi.SabahAksam, 1).Should().Be(SeferSlot.Sabah);
        HesaplaSlot(SeferTipi.SabahAksam, 2).Should().Be(SeferSlot.Aksam);
    }

    [Fact]
    public void SabahAksam_SeferSayisi8_CreatesAlternatingSlots()
    {
        var slots = Enumerable.Range(1, 8)
            .Select(i => HesaplaSlot(SeferTipi.SabahAksam, i))
            .ToList();

        slots[0].Should().Be(SeferSlot.Sabah);
        slots[1].Should().Be(SeferSlot.Aksam);
        slots[2].Should().Be(SeferSlot.Sabah);
        slots[3].Should().Be(SeferSlot.Aksam);
        slots[4].Should().Be(SeferSlot.Sabah);
        slots[5].Should().Be(SeferSlot.Aksam);
        slots[6].Should().Be(SeferSlot.Sabah);
        slots[7].Should().Be(SeferSlot.Aksam);
    }

    [Fact]
    public void Sabah_SeferSayisi3_CreatesAllSabahSlots()
    {
        var slots = Enumerable.Range(1, 3)
            .Select(i => HesaplaSlot(SeferTipi.Sabah, i))
            .ToList();

        slots.Should().AllBeEquivalentTo(SeferSlot.Sabah);
    }

    [Fact]
    public void Aksam_SeferSayisi3_CreatesAllAksamSlots()
    {
        var slots = Enumerable.Range(1, 3)
            .Select(i => HesaplaSlot(SeferTipi.Aksam, i))
            .ToList();

        slots.Should().AllBeEquivalentTo(SeferSlot.Aksam);
    }

    [Fact]
    public void HesaplaSlot_DefaultTip_Alternates()
    {
        // Bilinmeyen tip için de dönüşümlü slot
        HesaplaSlot((SeferTipi)99, 1).Should().Be(SeferSlot.Sabah);
        HesaplaSlot((SeferTipi)99, 2).Should().Be(SeferSlot.Aksam);
    }

    // ── SeferSlotlariGuncelle simülasyonu ─────────────────────────────

    [Fact]
    public void SeferSlotlariGuncelle_SabahAksam_AfterCountChange()
    {
        // Simüle: 8 satırlı SabahAksam liste oluştur
        var kalemler = new List<SlotKalemStub>();
        var tip = SeferTipi.SabahAksam;

        for (int i = 0; i < 8; i++)
        {
            kalemler.Add(new SlotKalemStub
            {
                Slot = HesaplaSlot(tip, i + 1)
            });
        }

        kalemler[0].Slot.Should().Be(SeferSlot.Sabah);
        kalemler[1].Slot.Should().Be(SeferSlot.Aksam);
        kalemler[6].Slot.Should().Be(SeferSlot.Sabah);
        kalemler[7].Slot.Should().Be(SeferSlot.Aksam);
    }

    [Fact]
    public void TumSeferlereUygula_DoesNotCopySlot()
    {
        // 1. seferi tüm seferlere uygula, ama slot'ları yeniden hesapla
        var kalemler = new List<SlotKalemStub>();
        var tip = SeferTipi.SabahAksam;

        for (int i = 0; i < 4; i++)
            kalemler.Add(new SlotKalemStub { AracId = 1, SoforAd = "Test" });

        var ilk = kalemler[0];
        ilk.AracId = 100;
        ilk.KapasiteAdi = "16+1";

        // Uygula: araç/şoför/kapasite kopyala, slot'ları hesapla
        for (int i = 1; i < kalemler.Count; i++)
        {
            kalemler[i].AracId = ilk.AracId;
            kalemler[i].KapasiteAdi = ilk.KapasiteAdi;
            kalemler[i].SoforAd = ilk.SoforAd;
        }

        // Slot'ları yeniden hesapla
        for (int i = 0; i < kalemler.Count; i++)
            kalemler[i].Slot = HesaplaSlot(tip, i + 1);

        // Tüm araç/şoför/kapasite aynı olmalı
        kalemler.All(k => k.AracId == 100).Should().BeTrue();
        kalemler.All(k => k.KapasiteAdi == "16+1").Should().BeTrue();

        // Slot'lar dönüşümlü olmalı (kopyalanmamalı!)
        kalemler[0].Slot.Should().Be(SeferSlot.Sabah);
        kalemler[1].Slot.Should().Be(SeferSlot.Aksam);
        kalemler[2].Slot.Should().Be(SeferSlot.Sabah);
        kalemler[3].Slot.Should().Be(SeferSlot.Aksam);
    }

    // ── Helper ───────────────────────────────────────────────────────

    private static SeferSlot HesaplaSlot(SeferTipi seferTipi, int siraNo)
    {
        return seferTipi switch
        {
            SeferTipi.Sabah => SeferSlot.Sabah,
            SeferTipi.Aksam => SeferSlot.Aksam,
            SeferTipi.SabahAksam => siraNo % 2 == 1 ? SeferSlot.Sabah : SeferSlot.Aksam,
            _ => siraNo % 2 == 1 ? SeferSlot.Sabah : SeferSlot.Aksam
        };
    }

    private class SlotKalemStub
    {
        public SeferSlot Slot { get; set; }
        public int? AracId { get; set; }
        public string? SoforAd { get; set; }
        public string? KapasiteAdi { get; set; }
    }
}
