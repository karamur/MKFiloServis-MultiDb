# ADR-0002: PuantajEngine V1 — Revizyon + Detay + Audit Mimarisi

## Status
Accepted (2026-05-25)

## Context
Sprint 1'de `OperasyonKaydi` → `PuantajKayit` dönüşümü için basit bir engine yazılmıştı (`Islendi` flag'i ile çalışan, tek seferlik hesaplama). Bu yeterli değildi:

- Revizyon geçmişi yok (her hesap öncekini eziyordu)
- Aynı operasyonun hangi hesap döngüsünde nasıl değerlendirildiği izlenemiyordu
- Finansal audit trail eksikti
- OperasyonKaydi'da `Islendi`/`PuantajKayitId` alanları entity'yi kirletiyordu

İş ihtiyacı:
- Puantaj tekrar hesaplanabilir olmalı (operasyon düzeltmeleri sonrası)
- Revizyon geçmişi tutulmalı (hangi versiyon, kim hesapladı)
- Finansal iz sürülebilirlik zorunlu (hangi fiyatla hesaplandı)

## Decision
Üç entity'li revizyon mimarisi:

1. **PuantajHesapDonemi**: Hesap döngüsü/batch. Unique(FirmaId, Yil, Ay, KurumId, Versiyon). Self-FK `OncekiDonemId` ile revizyon zinciri.
2. **PuantajDetay**: Operasyon ↔ Puantaj bağlantısı. Unique(OperasyonKaydiId, HesapDonemiId). Hesaplama anındaki BirimGelir/BirimGider snapshot'ını dondurur.
3. **PuantajKayit**: +3 alan (HesapDonemiId, OncekiVersiyonId self-FK, Versiyon).

**OperasyonKaydi sadeleştirme**: `Islendi`, `IslenmeTarihi`, `PuantajKayitId` alanları kaldırıldı. OperasyonKaydi saf ham veri.

**Transaction**: `BeginTransactionAsync` ile HesapDonemi + PuantajKayit + PuantajDetay tek seferde. Hata → `RollbackAsync`.

## Consequences

**Avantajlar:**
- Tam revizyon zinciri: V1 → V2 → V3 izlenebilir
- Finansal audit: PuantajDetay'da fiyat snapshot'ı, sonradan değişmez
- OperasyonKaydi temiz: saf ham veri, hesap döngüsü kirliliği yok
- Her hesap döngüsü bağımsız, yan yana karşılaştırılabilir

**Riskler:**
- PuantajDetay tablosu büyüyebilir (her operasyon × her hesap döngüsü = 1 satır)
- Self-FK zinciri derinleşirse sorgu performansı → indeksler optimize edildi

## Alternatives Considered
1. **Tek HesapDonemi, overwrite**: Reddedildi — audit trail yok
2. **Event sourcing**: Reddedildi — V1 için over-engineering, altyapı maliyeti yüksek
3. **OperasyonKaydi'da Islendi flag koruma**: Reddedildi — entity kirliliği, çoklu hesap döngüsü desteği yok

## Related Components
- `PuantajHesapDonemi.cs` (Shared/Entities)
- `PuantajDetay.cs` (Shared/Entities)
- `PuantajEngineService.cs` (Web/Services)
- `PreviewEngineService.cs` (Web/Services)
- `PuantajHesaplama.razor` (UI sayfası)

## Migration Impact
- Yeni tablolar: `PuantajHesapDonemleri`, `PuantajDetaylari`
- OperasyonKayitlari: 3 kolon sil (Islendi, IslenmeTarihi, PuantajKayitId), 1 FK sil
- PuantajKayitlar: 3 kolon ekle (HesapDonemiId, OncekiVersiyonId, Versiyon)
- Unique constraint: (FirmaId, Yil, Ay, KurumId, Versiyon)

## Verification Checklist
- [x] Revizyon zinciri: V1 → V2 self-FK çalışıyor
- [x] Superseded: yeni hesap → eski Superseded
- [x] Snapshot: PuantajDetay.BirimGelir fiyat değişse bile korunuyor
- [x] Transaction: Rollback → partial veri yok
- [x] Unique constraint: aynı versiyon iki kez oluşmuyor
- [x] PreviewEngine: DB write yok, AsNoTracking
- [x] Comparison: V1 vs V2 delta (grup + operasyon seviyesi)
- [x] Drill-down: preview grid → günlük operasyon detayı
- [x] Build: 0 hata, 0 uyarı
- [x] Test: 305/305 başarılı
