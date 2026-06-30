# Operasyon Modülü — Entity & Config Raporu

> Son güncelleme: 25.05.2026 — Sprint 2

## OperasyonKaydi Entity

**Dosya:** `MKFiloServis.Shared/Entities/OperasyonKaydi.cs`
**Base:** `BaseEntity` + `IFirmaTenant`

### Alan Listesi (39 alan)

| # | Alan | Tip | Null | Varsayılan | Açıklama |
|---|------|-----|:----:|-----------|----------|
| 1 | Id | int | | auto | BaseEntity |
| 2 | CreatedAt | DateTime | | UtcNow | BaseEntity |
| 3 | UpdatedAt | DateTime? | ✅ | | BaseEntity |
| 4 | IsDeleted | bool | | false | BaseEntity |
| 5 | FirmaId | int? | ✅ | | IFirmaTenant |
| 6 | Tarih | DateTime | | | Günlük operasyon tarihi |
| 7 | GuzergahId | int | | | FK → Guzergahlar |
| 8 | AracId | int | | | FK → Araclar |
| 9 | SoforId | int? | ✅ | | FK → Personeller |
| 10 | Slot | SeferSlot | | Sabah | Sabah=1, Aksam=2, Mesai=3, Diger1-5 |
| 11 | SlotAdi | string(50) | ✅ | | Özel slot adı |
| 12 | Yon | PuantajYon | | SabahAksam | Sabah=1, Aksam=2, SabahAksam=3 |
| 13 | KurumId | int? | ✅ | | FK → Kurumlar |
| 14 | IsverenFirmaId | int? | ✅ | | FK → Firmalar |
| 15 | SeferSayisi | int | | 1 | Günlük sefer adedi |
| 16 | PuantajCarpani | decimal(10,2) | | 1.0 | 0.5=yarım gün |
| 17 | OperasyonDurumu | enum | | Gitti | Gitti, Gitmedi_Mazeretli, Gitmedi_Mazeretsiz, Taksiyle_Gidildi, Arizalandi_YoldaKaldi, Iptal_KurumTarafindan |
| 18 | KaynakTipi | enum | | Kendi | Kendi=1, Tedarikci=2 |
| 19 | FinansYonu | enum | | Giden | Gelen=1, Giden=2, IcDagitim=3 |
| 20 | SoforOdemeTipi | enum | | Ozmal | Ozmal=1, Kiralik=2, Komisyoncu=3 |
| 21 | OdemeYapilacakCariId | int? | ✅ | | FK → Cariler |
| 22 | FaturaKesiciCariId | int? | ✅ | | FK → Cariler |
| 23 | BelgeNo | string(50) | ✅ | | Referans belge |
| 24 | TransferDurum | string(50) | ✅ | | İç transfer durumu |
| 25 | Kaynak | enum | | Manuel | Manuel=0, ExcelImport=1, ServisCalismaOtomatik=2 |
| 26 | ExcelImportId | int? | ✅ | | Import batch referansı |
| 27 | ExcelSatirNo | int? | ✅ | | Excel satır numarası |
| 28 | Islendi | bool | | false | Engine tarafından işlendi mi? |
| 29 | IslenmeTarihi | DateTime? | ✅ | | Engine işleme zamanı |
| 30 | PuantajKayitId | int? | ✅ | | FK → PuantajKayitlar (engine çıktısı) |
| 31 | CreatedBy | string(100) | ✅ | | Audit |
| 32 | UpdatedBy | string(100) | ✅ | | Audit |
| 33 | DeletedAt | DateTime? | ✅ | | Soft delete |
| 34 | DeletedBy | string(100) | ✅ | | Soft delete |
| 35 | Notlar | string(1000) | ✅ | | Serbest not |

**Computed (NotMapped):** Yil, Ay, Gun → Tarih'ten türetilir

---

## FK İlişkileri (8 adet — tümü Restrict)

| FK | Hedef Tablo | DeleteBehavior | Açıklama |
|----|-------------|:---:|-----------|
| GuzergahId | Guzergahlar | **Restrict** | Güzergah silinirse operasyon ENGEL |
| AracId | Araclar | **Restrict** | Araç silinirse operasyon ENGEL |
| SoforId | Personeller | **Restrict** | Şoför silinirse operasyon ENGEL |
| KurumId | Kurumlar | **Restrict** | Kurum silinirse operasyon ENGEL |
| IsverenFirmaId | Firmalar | **Restrict** | Firma silinirse operasyon ENGEL |
| OdemeYapilacakCariId | Cariler | **Restrict** | Cari silinirse operasyon ENGEL |
| FaturaKesiciCariId | Cariler | **Restrict** | Cari silinirse operasyon ENGEL |
| PuantajKayitId | PuantajKayitlar | **Restrict** | PuantajKayit silinirse operasyon ENGEL |

> **Kural:** Hiçbir FK'da Cascade veya SetNull yok. Referans edilen entity silinemez.

---

## Performans İndexleri (13 adet)

| # | İndex | Tip | Sütunlar |
|---|-------|-----|----------|
| 1 | PK_OperasyonKayitlari | Primary | Id |
| 2 | IX_Tarih_Guzergah_Arac_Slot | **Unique** | (Tarih, GuzergahId, AracId, Slot) |
| 3 | IX_Tarih | Standalone | Tarih |
| 4 | IX_OperasyonDurumu | Standalone | OperasyonDurumu |
| 5 | IX_Slot | Standalone | Slot |
| 6 | IX_Islendi | Standalone | Islendi |
| 7 | IX_FirmaId_Tarih | Composite | (FirmaId, Tarih) |
| 8 | IX_Tarih_KurumId | Composite | (Tarih, KurumId) |
| 9 | IX_Tarih_AracId | Composite | (Tarih, AracId) |
| 10 | IX_PuantajKayitId | FK ref | PuantajKayitId |
| 11-18 | FK auto-index (8 adet) | Foreign Key | GuzergahId, AracId, SoforId, KurumId, IsverenFirmaId, OdemeYapilacakCariId, FaturaKesiciCariId |

---

## Servis Mimarisi (3 Katman)

```
Controller / Razor Page
        │
        ▼
┌──────────────────────┐
│  OperasyonKaydiValidator  │  ← Katman 1: Input validasyonu (statik)
│  • Validate(entity)       │
│  • ValidateToplu(list)    │
└──────────┬───────────────┘
           ▼
┌──────────────────────────┐
│ OperasyonKaydiBusinessRules│  ← Katman 2: Domain kuralları (DI)
│ • CheckConflictsAsync()   │
│ • CheckOperationalRules() │
└──────────┬───────────────┘
           ▼
┌──────────────────────┐
│  OperasyonKaydiService│  ← Katman 3: Data access (DI)
│  • CRUD               │
│  • Şablon oluşturma   │
│  • Migrasyon import   │
└──────────────────────┘
```

---

## PuantajEngine Akışı

```
OperasyonKaydi (günlük)
  • Tarih=01.06, Araç=34AB12, Guzergah=1, Slot=Sabah, Sefer=1
  • Tarih=02.06, Araç=34AB12, Guzergah=1, Slot=Sabah, Sefer=1
  • Tarih=03.06, Araç=34AB12, Guzergah=1, Slot=Sabah, Sefer=0  ← İptal
        │
        ▼ PuantajEngineService.ProcessDonemAsync()
        │
        ▼
PuantajKayit (aylık)
  • Yil=2026, Ay=6, GuzergahId=1, AracId=34AB12, Slot=Sabah
  • Gun01=1, Gun02=1, Gun03=0, ...
  • BirimGelir=Guzergah.GelirFiyat
  • ToplamGelir=BirimGelir*Gun
```

---

## Operasyon Giriş Sayfası

**Route:** `/operasyon-giris`
**Dosya:** `Components/Pages/Operasyon/OperasyonGiris.razor`
**Yetki:** `Admin, Operasyon, Muhasebeci, HoldingYoneticisi`

**Filtreler:**
- Tarih: date input + Bugün/←/→ butonları
- Kurum: autocomplete (list-group + @oninput)
- Güzergah: cascade dropdown (kuruma göre filtrelenir)

**Grid:**
- Sütunlar: #, Araç, Şoför, Slot (dropdown), Sefer (number), Durum (dropdown), Sil
- Inline edit: slot/sefer/durum değişince dirty tracking
- Değişen satırlar: yeşil vurgu (`satir-degisik` class)
- Toplu kaydet: değişiklik varsa buton aktif

**Yeni Kayıt:**
- Inline form: Araç autocomplete + Şoför autocomplete + Slot toggle + Sefer sayısı
- Ekle butonu → listeye ekler + dirty tracking

