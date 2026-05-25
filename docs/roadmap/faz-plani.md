# Operasyon & Puantaj Modülü — Sprint Planı

> Son güncelleme: 25.05.2026

## Tamamlanan Sprintler

### Sprint 1: OperasyonKaydi Entity Mimarisi ✅ (25.05.2026)

| İş | Durum |
|----|:----:|
| OperasyonKaydi entity (39 alan, IFirmaTenant) | ✅ |
| PuantajKayit.OperasyonKayitlari nav | ✅ |
| DbContext: DbSet + fluent config (8 FK Restrict, 13 index) | ✅ |
| OperasyonKaydiValidator (statik input validasyon) | ✅ |
| OperasyonKaydiBusinessRules (domain + çakışma) | ✅ |
| IOperasyonKaydiService + OperasyonKaydiService (CRUD) | ✅ |
| IPuantajEngineService + PuantajEngineService (dönüşüm motoru) | ✅ |
| EF Migration: OperasyonKayitlari tablosu | ✅ |
| DI kayıtları (Program.cs) | ✅ |
| Duplicate check SQL (docs/sql/) | ✅ |
| Audit: CreatedBy, UpdatedBy, DeletedBy, DeletedAt | ✅ |

### Sprint 2: Operasyon Giriş Ekranı ✅ (25.05.2026)

| İş | Durum |
|----|:----:|
| OperasyonGiris.razor + code-behind | ✅ |
| Tarih + Kurum autocomplete + Güzergah cascade filtre | ✅ |
| Grid: günlük liste + inline edit (slot/sefer/durum) | ✅ |
| Yeni kayıt: inline form (araç/şoför autocomplete) | ✅ |
| Slot hızlı seçim (Sabah/Akşam/Mesai toggle) | ✅ |
| Dirty tracking + toplu kaydet | ✅ |
| Çakışma kontrolü entegrasyonu | ✅ |

---

## Yapılacak Sprintler

### Sprint 3: Excel Import + Puantaj Tetikleme 🟡

| İş | Öncelik |
|----|:---:|
| OperasyonKaydi Excel import sayfası | 🔴 |
| PuantajEngine tetikleme butonu (UI) | 🔴 |
| İşlenmiş/işlenmemiş operasyon göstergesi | 🟡 |
| PuantajEngine job (Quartz - ay sonu otomatik) | 🟡 |

### Sprint 4: Toplu İşlemler + Filtreler 🟡

| İş | Öncelik |
|----|:---:|
| Haftalık/aylık toplu operasyon girişi | 🟡 |
| Şablon kopyalama (önceki günden/haftadan) | 🟡 |
| Gelişmiş filtreler (slot, durum, araç tipi) | ⚪ |
| Export (Excel/PDF) | ⚪ |

### Sprint 5: Dashboard + Raporlama ⚪

| İş | Öncelik |
|----|:---:|
| Günlük operasyon özet kartı (Dashboard) | ⚪ |
| Puantaj karşılaştırma (operasyon vs puantaj) | ⚪ |
| Eksik/hatalı operasyon raporu | ⚪ |

---

## Servis Envanteri

| Servis | Tip | Dosya |
|--------|-----|-------|
| OperasyonKaydiValidator | static | `Services/OperasyonKaydiValidator.cs` |
| OperasyonKaydiBusinessRules | scoped | `Services/OperasyonKaydiBusinessRules.cs` |
| IOperasyonKaydiService | scoped | `Services/Interfaces/IOperasyonKaydiService.cs` |
| OperasyonKaydiService | scoped | `Services/OperasyonKaydiService.cs` |
| IPuantajEngineService | scoped | `Services/Interfaces/IPuantajEngineService.cs` |
| PuantajEngineService | scoped | `Services/PuantajEngineService.cs` |

## Sayfa Envanteri

| Route | Sayfa | Yetki |
|-------|-------|-------|
| `/operasyon-giris` | OperasyonGiris.razor | Admin, Operasyon, Muhasebeci, HoldingYoneticisi |
| `/kurum-puantaj` | KurumPuantaj.razor | Admin, Operasyon, Muhasebeci, HoldingYoneticisi |
| `/puantaj/import` | KurumPuantajImport.razor | Admin, Operasyon |
