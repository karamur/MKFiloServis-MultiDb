# ADR-0004: Finansal Çıktı — Fatura + Snapshot Köprüsü

## Status
Accepted (2026-05-25)

## Context
Sprint 5'te onay workflow tamamlandı. Kilitli hesap döneminden sonra finansal çıktı üretilmesi gerekiyordu:
- Kuruma kesilecek fatura taslağı (Gelir)
- Tedarikçiden alınacak fatura kontrolü (Gider)
- Finansal snapshot (onay anındaki değerler dondurulmalı)
- Cari hareket altyapısı

Projede zaten `Fatura`, `FaturaKalem`, `Hakedis`, `OdemeEslestirme`, `IFaturaService` mevcuttu. Bunlar yeniden kullanılmalıydı.

İş ihtiyacı:
- Onaylanmış puantajdan otomatik fatura üretimi
- Fatura ile PuantajKayit arasında izlenebilirlik
- Fiyat değişse bile finansal kayıt korunmalı
- Çift fatura üretimi engellenmeli
- Cari hareket otomatik oluşmalı

## Decision
**Köprü entity + mevcut Fatura altyapısı:**

1. **PuantajFinansalKayit** (YENİ): PuantajKayit ile Fatura arasında köprü.
   - Snapshot: BirimGelir, BirimGider, KdvTutar, GenelToplam (onay anında dondurulur)
   - Cari bağlantısı: GelirCariId, GiderCariId
   - Fatura bağlantısı: GelirFaturaId, GiderFaturaId (üretilince doldurulur)
   - Unique(PuantajKayitId, HesapDonemiId) → çift fatura engeli

2. **Mevcut Fatura entity'si**: Sıfırdan inşa edilmedi. `IFaturaService.CreateAsync` kullanıldı.

3. **Cari hareket**: Yeni entity YOK. Mevcut `CariHareketTakipService` composite view'i (Fatura + BankaKasaHareket birleşimi) otomatik çalışır.

## Consequences

**Avantajlar:**
- Mevcut Fatura/FaturaKalem entity'leri değişmedi
- IFaturaService kontratı korundu
- Cari hareket için ek işlem gerekmez (Fatura kaydı otomatik harekete dönüşür)
- 3 katmanlı snapshot: PuantajDetay (operasyonel) → PuantajFinansalKayit (finansal) → Fatura (yasal)
- Fatura silinirse sadece Durum güncellenir, finansal kayıt korunur

**Riskler:**
- IFaturaService API değişikliği → mevcut fatura akışı etkilenebilir (kontrat korundu)
- Gelir/Gider faturası aynı anda üretilirse race condition → Unique constraint + transaction

## Alternatives Considered
1. **Yeni fatura entity'si**: Reddedildi — mevcut Fatura zaten tüm ihtiyaçları karşılıyor
2. **PuantajKayit'a doğrudan fatura FK**: Reddedildi — snapshot olmadan fiyat değişince iz kaybolur
3. **FaturaKalem olmadan üretim**: Reddedildi — mevzuat gereği fatura kalemsiz olmaz
4. **CariHareket entity'si**: Reddedildi — mevcut composite view yeterli, yeni entity gereksiz

## Related Components
- `PuantajFinansalKayit.cs` (Shared/Entities)
- `PuantajFinansService.cs` (Web/Services)
- `IFaturaService.cs` (mevcut, değişmedi)
- `Fatura.cs` + `FaturaKalem.cs` (mevcut, değişmedi)
- `CariHareketTakipService.cs` (mevcut, değişmedi)
- `PuantajHesaplama.razor` (finans paneli)

## Migration Impact
- Yeni tablo: `PuantajFinansalKayitlar` (1 tablo)
- Unique constraint: (PuantajKayitId, HesapDonemiId)
- Mevcut tablolarda değişiklik: YOK

## Verification Checklist
- [x] FinansalKayitOlustur: Kilitli dönem → snapshot üretildi
- [x] GelirFaturasiUret: Fatura + FaturaKalem oluştu
- [x] GiderFaturasiUret: Fatura + FaturaKalem oluştu
- [x] TopluFaturaUret: tüm bekleyen kayıtlar işlendi
- [x] Çift fatura engeli: Unique constraint + Durum kontrolü
- [x] Cari hareket: Fatura → otomatik CariHareketTakipService
- [x] Snapshot: fiyat değişse bile PuantajFinansalKayit sabit
- [x] Mevcut IFaturaService kontratı korundu
- [x] Build: 0 hata, 0 uyarı
- [x] Test: 305/305 başarılı
