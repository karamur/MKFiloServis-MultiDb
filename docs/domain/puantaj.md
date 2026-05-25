# Puantaj Modülü — Domain Mimarisi

> Son güncelleme: 25.05.2026 — Sprint 2

## Entity Katmanları

```
┌─────────────────────────────────────────────────┐
│                OperasyonKaydi                     │
│            (Ham Operasyon Verisi)                 │
│  • Günlük kayıt (Tarih + Araç + Güzergah + Slot) │
│  • Sefer sayısı, operasyon durumu                │
│  • Kaynak takibi (manuel/Excel/otomatik)          │
│  • Audit: CreatedBy, UpdatedBy, DeletedBy        │
│  • Engine: Islendi, PuantajKayitId               │
└──────────────────┬──────────────────────────────┘
                   │  PuantajEngineService
                   │  (grupla → aggregate → hesapla)
                   ▼
┌─────────────────────────────────────────────────┐
│                PuantajKayit                       │
│          (Hesaplanmış Puantaj Çıktısı)            │
│  • Aylık kayıt (Yil + Ay + Gun01..Gun31)         │
│  • Finansal: BirimGelir/Gider, KDV, Toplam       │
│  • Fatura/ödeme takibi                            │
│  • Onay durumu                                    │
└─────────────────────────────────────────────────┘
```

## Akış

1. **Operasyon Girişi**: `/operasyon-giris` → `OperasyonKaydi` tablosuna günlük kayıt
2. **Puantaj Engine**: `PuantajEngineService.ProcessDonemAsync()` → günlük kayıtları gruplayıp aylık `PuantajKayit` üretir
3. **Puantaj Görüntüleme**: `/kurum-puantaj` → `PuantajKayit` üzerinden aylık grid

## OperasyonKaydi

### Alanlar

| Grup | Alan | Tip |
|------|------|-----|
| Tenant | FirmaId | int? |
| Tarih | Tarih | DateTime |
| Güzergah/Araç/Şoför | GuzergahId, AracId, SoforId | int, int, int? |
| Slot/Yön | Slot, SlotAdi, Yon | SeferSlot, string?, PuantajYon |
| Kurum | KurumId, IsverenFirmaId | int?, int? |
| Sefer | SeferSayisi, PuantajCarpani | int, decimal |
| Durum | OperasyonDurumu | enum |
| Kaynak/Finans | KaynakTipi, FinansYonu, SoforOdemeTipi | enum |
| Referans | BelgeNo, TransferDurum | string? |
| Engine | Islendi, IslenmeTarihi, PuantajKayitId | bool, DateTime?, int? |
| Audit | CreatedBy, UpdatedBy | string? |
| Soft Delete | DeletedAt, DeletedBy | DateTime?, string? |

### FK İlişkileri (tümü Restrict)

- GuzergahId → Guzergahlar
- AracId → Araclar
- SoforId → Personeller
- KurumId → Kurumlar
- IsverenFirmaId → Firmalar
- OdemeYapilacakCariId → Cariler
- FaturaKesiciCariId → Cariler
- PuantajKayitId → PuantajKayitlar

### İndexler

- Unique: (Tarih, GuzergahId, AracId, Slot)
- Standalone: Tarih, OperasyonDurumu, Slot, Islendi
- Composite: (FirmaId, Tarih), (Tarih, KurumId), (Tarih, AracId)

## Servisler

| Servis | Sorumluluk |
|--------|-----------|
| `OperasyonKaydiValidator` | Input validasyon (statik) |
| `OperasyonKaydiBusinessRules` | Domain kuralları + çakışma kontrolü |
| `IOperasyonKaydiService` | CRUD + şablon + migrasyon |
| `IPuantajEngineService` | OperasyonKaydi → PuantajKayit dönüşüm |
| `IKurumPuantajService` | PuantajKayit CRUD (mevcut, korundu) |

## Sayfalar

| Route | Sayfa | Açıklama |
|-------|-------|----------|
| `/operasyon-giris` | OperasyonGiris.razor | Günlük operasyon girişi (grid + inline edit) |
| `/kurum-puantaj` | KurumPuantaj.razor | Aylık puantaj grid (mevcut) |
| `/puantaj/import` | KurumPuantajImport.razor | Excel import (mevcut) |
