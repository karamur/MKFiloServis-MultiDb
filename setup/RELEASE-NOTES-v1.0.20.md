# KOAFiloServis v1.0.20

**Yayın Tarihi:** 2026-05-18  
**Tema:** Legacy `Sirket` tenant mimarisinin emekliye alınması — büyük temizlik sprint'i

---

## 📊 Genel Bakış

Bu sürüm, eski `Sirket`/`SirketId`/`TenantService` mimarisinin son kalıntılarını temizlemek üzerine yoğunlaştı:

- **~1470 satır legacy kod silindi** (TenantService.cs ~700 + Sirket.cs / SirketTransferLog.cs + 14 entity navigation + DbContext mapping'leri)
- **Build warning sayısı**: 38 → **5** (planlı obsolete uyarılarının çoğu temizlendi)
- **DbContext'te `Sirket` kelimesi tek bir aktif kullanım olarak kalmadı**
- 3 yeni migration PostgreSQL'e başarıyla uygulandı

---

## 🆕 Yeni Migration'lar

| Migration | Amaç |
|-----------|------|
| `TenantB1_AddFirmaIdToCariSeferUcreti` | `CariSeferUcretleri` tablosuna `FirmaId` nullable kolon + Restrict FK |
| `TenantCExt2_AddFirmaIdToKapasite` (HOTFIX) | `Kapasiteler.FirmaId` eksik kolon (`42703` hatası) — PL/pgSQL idempotent |
| `TenantB3i_DropSirketNavigationAndEntity` | 21 FK drop + indeksler drop + Sirketler/SirketTransferLoglari **RENAME** (`_LEGACY_` prefix) |

---

## 🧹 Silinen Dosyalar

- `KOAFiloServis.Web/Services/TenantService.cs` (~700 satır)
- `KOAFiloServis.Web/Services/Interfaces/ITenantService.cs`
- `KOAFiloServis.Shared/Entities/Sirket.cs`
- `KOAFiloServis.Shared/Entities/SirketTransferLog.cs`

---

## 🔒 Veri Güvenliği

- `Sirketler` ve `SirketTransferLoglari` tabloları **DROP edilmedi**, sadece `_LEGACY_` prefix ile **RENAME** edildi.
- Veri tamamen korunuyor. Faz 5.3-B4'te (yedek alındıktan sonra) fiziksel DROP yapılacak.
- Tüm migration'lar PL/pgSQL idempotent (tekrar çalıştırılabilir).

---

## ⚠️ Bilinen Borçlar (1.0.21 için)

- **Faz 5.3-B4** — 14+ tablodan `int? SirketId` kolon DROP + `_LEGACY_*` tabloları DROP. **DB backup şart**, geri dönüş yok.
- **Faz 5.2** — `Firma.CariId` drop. İş tarafı onayı önerilir (Unvan fallback regresyon riski).
- **AuditLog.SirketId** semantik kararı (FirmaId'ye rename mi, semantik koru mu?).

---

## 📥 Kurulum

### Yeni kurulum
```
KOAFiloServisKurulum-1.0.20.exe (admin olarak çalıştırın)
```

### Mevcut kurulumu güncelleme
```
KOAFiloServisGuncelle-1.0.20.exe (admin olarak çalıştırın)
```

**SHA256:** `<sha256-hash-buraya>`

---

## 🔗 Detay

Detaylı teknik döküman için: `docs/TENANT_MIGRATION_PLAN.md` — "FAZ 5.3-B3-i TAMAMLANDI" bölümü.
