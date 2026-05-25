# ADR-0001: OperasyonKaydi + PuantajKayit İki Katmanlı Mimari

## Status
Accepted (2026-05-25)

## Context
Mevcut `PuantajKayit` entity'si hem ham operasyon verisini (araç, şoför, güzergah, günlük sefer) hem de hesaplanmış finansal çıktıyı (birim fiyat, KDV, toplam gelir/gider) aynı anda taşıyan "şişman" bir entity idi. Bu iki sorumluluğun ayrıştırılması gerekiyordu.

İş ihtiyacı:
- Operasyon personeli günlük sefer girişini hızlı yapabilmeli
- Muhasebe/finans sadece hesaplanmış puantaj çıktısını görmeli
- Ham veri ile finansal çıktı birbirine karışmamalı

## Decision
İki katmanlı entity mimarisi:

1. **OperasyonKaydi**: Günlük ham operasyon verisi (Tarih + Araç + Güzergah + Slot + SeferSayisi). Finansal alan içermez. Saf veri.
2. **PuantajKayit**: Aylık hesaplanmış puantaj çıktısı (Gun01-Gun31 + BirimGelir/Gider + KDV + Fatura/Ödeme takibi). Mevcut entity korundu, sadece `OperasyonKayitlari` navigasyonu eklendi.

Akış: `OperasyonKaydi (günlük)` → `PuantajEngine` → `PuantajKayit (aylık)`

## Consequences

**Avantajlar:**
- Sorumluluk ayrımı: Operasyon ekibi ham veriyle, finans ekibi hesaplanmış çıktıyla çalışır
- OperasyonKaydi saf ham veri, hesap döngüsünden bağımsız
- PuantajKayit mevcut yapısı korundu, geriye dönük uyumlu
- Her katman bağımsız audit + soft delete

**Riskler:**
- İki entity arasında veri tutarsızlığı olasılığı → PuantajEngine + transaction scope ile önlendi
- Mevcut KurumPuantajService direkt PuantajKayit yazıyor → kademeli geçiş gerekli

## Alternatives Considered
1. **Tek entity (PuantajKayit) genişletme**: Reddedildi — sorumluluk karmaşası devam eder
2. **Event-driven ayrıştırma**: Reddedildi — V1 için over-engineering, sync transaction yeterli
3. **PuantajKayit'ı tamamen kaldırma**: Reddedildi — mevcut finansal akış (KurumPuantaj, Excel import) kırılır

## Related Components
- `OperasyonKaydi.cs` (Shared/Entities)
- `PuantajKayit.cs` (Shared/Entities)
- `PuantajEngineService.cs` (Web/Services)
- `OperasyonKaydiService.cs` (Web/Services)
- `ApplicationDbContext.cs` (Web/Data)
- `/operasyon-giris` (UI sayfası)
- `/kurum-puantaj` (UI sayfası)

## Migration Impact
- Yeni tablo: `OperasyonKayitlari`
- FK: 8 adet (GuzergahId, AracId, SoforId, KurumId, IsverenFirmaId, OdemeYapilacakCariId, FaturaKesiciCariId, PuantajKayitId)
- İndex: 13 adet (1 unique + 4 standalone + 3 composite + FK oto-indexler)
- Mevcut PuantajKayit tablosu: değişiklik yok

## Verification Checklist
- [x] Build: 0 hata, 0 uyarı
- [x] Test: 305/305 başarılı
- [x] Migration: `AddOperasyonKaydi` başarılı
- [x] FK: tümü Restrict (cascade delete yok)
- [x] Soft delete: IsDeleted + DeletedAt + DeletedBy
- [x] Audit: CreatedBy + UpdatedBy + DeletedBy
- [x] Multi-tenant: IFirmaTenant desteği
- [x] `/operasyon-giris` sayfası çalışıyor
- [x] `/kurum-puantaj` sayfası çalışıyor (mevcut akış korundu)
