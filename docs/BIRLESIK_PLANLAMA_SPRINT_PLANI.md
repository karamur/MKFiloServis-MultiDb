
# KOAFiloServis — Birleşik Planlama Modülü Sprint Planı

> **Hazırlanma:** 21.05.2026
> **Kaynaklar:** `YAPILMAYAN_PLANLAR_AI_UYGULAMA_PLANI.md`, `YONETIM_SUNUM_SAYFA_ICERIKLERI.md`, `PLANLAMA_GAP_RAPORU.md`, `Kurum_Firma_Bazli_Puantaj_Planlama_Raporu.docx`
> **Durum:** Sprint 1 altyapı tamamlandı, UI çalışması devam ediyor

---

## 1) Yönetim Özeti

**Amaç:** Mevcut puantaj sistemini bozmadan, güzergah bazlı çoklu sefer slotu (Sabah/Akşam/Mesai), sefer CRUD, çakışma önleme ve gelir-gider/fatura takibini tek merkezden yönetilebilir hale getirmek.

**Kapsam Kararı:**
- KurumPuantaj sayfaları ve servisleri çalışır kalacak (kırıcı değişiklik yok)
- Yeni Planlama modülü ayrı sayfa ve servislerle geliştirilecek
- Sefer slot yönetimi, çakışma motoru ve finans takip tek modülde birleşecek

---

## 2) Mevcut Durum ve Boşluk Analizi

| Başlık | Durum | Açıklama |
|--------|:-----:|----------|
| Kurum Puantaj çalışma durumu | ✅ VAR | Mevcut yapıda aktif ve kullanılabilir |
| Puantaj menü erişimi | ✅ VAR | Menü yönlendirmesi mevcut |
| SeferSlot enum + entity alanları | ✅ YENİ | `SeferSlot` (Sabah/Akşam/Mesai), `KurumId`, `IsverenFirmaId` entity'ye eklendi |
| Migration helper | ✅ YENİ | `PuantajSlotMigrationHelper` eklendi |
| Servis Slot desteği | ✅ YENİ | Upsert unique key `(Guzergah+Arac+Yil+Ay+Slot)`, SablonOlustur çoklu slot |
| Ayrı Planlama sayfası | 🔴 YOK | `Planlama.razor` bağımsız ekran henüz yok |
| Planlama Sefer CRUD (Ekle/Düzenle/Sil) | 🔴 YOK | UI katmanı gerekli |
| Çakışma kontrolü (araç/şoför/slot) | 🔴 YOK | Kural/validasyon motoru gerekli |
| Tooltip/hover çakışma uyarıları | 🔴 YOK | Grid ve görsel uyarı mekanizması yok |
| Önceki aydan kopyalama | 🟡 KISMEN | Benzer yaklaşımlar var, planlama özelinde yok |
| Gelir/Gider/Fatura yön takibi | 🟡 KISMEN | Operasyonel finans temeli var, yön bazlı model eksik |
| Kendi firma vs tedarikçi izolasyonu | 🟡 KISMEN | Temel ayrımlar var; net ayrışma eksik |
| Tedarikçi araç-şoför evrak takibi | 🔴 YOK | Ayrık tedarikçi takip akışı yok |

---

## 3) Sprint Planı

### Sprint 1 — Temel Uyum + Sefer CRUD (3-4 gün)

#### ✅ Tamamlanan

| İş | Dosya | Açıklama |
|----|-------|----------|
| SeferSlot enum | `PuantajKayit.cs` | `SeferSlot` (Sabah=1, Aksam=2, Mesai=3) |
| Entity genişletme | `PuantajKayit.cs` | `Slot`, `KurumId`, `IsverenFirmaId` property'leri |
| DbContext update | `ApplicationDbContext.cs` | Yeni FK (Kurum, IsverenFirma), index `(Yil,Ay,GuzergahId,AracId,Slot)` |
| Servis update | `KurumPuantajService.cs` | Upsert unique key Slot içerir, SablonOlustur her slot için satır |
| Migration helper | `PuantajSlotMigrationHelper.cs` | Slot, KurumId, IsverenFirmaId kolonları (idempotent) |
| Startup görevi | `Program.cs` | `PuantajSlotMigration` çalıştırma |

#### 🔴 Yapılacak

| # | İş | Dosya | Açıklama |
|---|-----|-------|----------|
| 1.1 | `Planlama.razor` ana sayfa | YENİ | Filtre paneli (Firma/Kurum/Tarih/Slot/Kaynak) + grid |
| 1.2 | `PlanlamaGrid.razor` tablo | YENİ | Kolonlar: Tarih, Slot, Firma, Kurum, Güzergah, Araç, Şoför, Durum |
| 1.3 | `PlanlamaEditModal.razor` | YENİ | Ekle/Düzenle modal: Firma, Kurum, Güzergah, Tarih, Slot, Araç, Şoför, KaynakTipi, FinansYonu |
| 1.4 | Menü + yetki bağlantısı | `NavMenu.razor` | Planlama menü grubu ekle |
| 1.5 | UI testi | — | CRUD akış, filtre çalışması |

---

### Sprint 2 — Çakışma Motoru + Validasyon (2-3 gün)

| # | İş | Dosya | Açıklama |
|---|-----|-------|----------|
| 2.1 | `CheckConflictsAsync` servis | `KurumPuantajService.cs` | (Tarih+Güzergah+Slot)→tek araç, (Tarih+Araç+Slot)→tek güzergah |
| 2.2 | Blocking/Warning model | YENİ | `ConflictResult` sınıfı: tip, mesaj, etkilenen kayıtlar |
| 2.3 | Kapasite validasyonu | `KurumPuantajService.cs` | Kurum/güzergah üst limit kontrolü |
| 2.4 | DB index optimizasyonu | `ApplicationDbContext.cs` | FiloGunlukPuantaj için `(Tarih,GuzergahId,Slot)` ve `(Tarih,AracId,Slot)` |
| 2.5 | Kaydet öncesi conflict check | UI + Service | Modal kaydetmeden önce otomatik kontrol |
| 2.6 | Birim testler | Tests | Çakışma senaryoları (aynı araç, aynı şoför, kapasite) |

---

### Sprint 3 — Çakışma UX + Önceki Ay Kopyalama (3-4 gün)

| # | İş | Dosya | Açıklama |
|---|-----|-------|----------|
| 3.1 | Çakışma renklendirme | `PlanlamaGrid.razor` | Blocking=kırmızı, Warning=sarı rozet |
| 3.2 | Hover tooltip | `PlanlamaGrid.razor` | Çakışan kayıt detayları, önerilen aksiyon |
| 3.3 | `CopyPreviousMonthModal.razor` | YENİ | Kaynak ay/hedef ay, Firma/Kurum/Slot filtre |
| 3.4 | `CopyFromPreviousMonthAsync` | `KurumPuantajService.cs` | Filtreli kopyalama + çakışma simülasyonu |
| 3.5 | Kopya sonuç özeti | UI | Başarılı/atlanan/çakışan sayıları |
| 3.6 | Basit/İleri mod toggle | `Planlama.razor` | Kullanıcı tercihine göre UI karmaşıklığı |

---

### Sprint 4 — Finans + Tedarikçi İzolasyonu (2-3 gün)

| # | İş | Dosya | Açıklama |
|---|-----|-------|----------|
| 4.1 | FinansYonu enum + entity | `PuantajKayit.cs` | `PlanlamaFinansYonu`: Gelen, Giden, IcDagitim |
| 4.2 | KaynakTipi enum + entity | `PuantajKayit.cs` | `PlanlamaKaynakTipi`: Kendi, Tedarikci |
| 4.3 | Gelir/gider satır bazlı görünüm | `PlanlamaGrid.razor` | Marj, fatura durumu rozetleri |
| 4.4 | Tedarikçi izolasyon validasyonu | `KurumPuantajService.cs` | Kendi araç + tedarikçi şoför karışımını engelle |
| 4.5 | `FinansDurumPanel.razor` | YENİ | Gelen/Giden/İçDağıtım dağılımı, fatura bekleyenler |
| 4.6 | `EvrakUyariPanel.razor` | YENİ | Eksik/süresi yaklaşan evrak rozetleri |
| 4.7 | İç transfer alanları | `PuantajKayit.cs` | KaynakFirmaId, HedefFirmaId, TransferDurum, BelgeNo |

---

### Sprint 5 — Dashboard + UAT + Canlı Geçiş (2-3 gün)

| # | İş | Dosya | Açıklama |
|---|-----|-------|----------|
| 5.1 | `PlanlamaDashboard.razor` | YENİ | KPI kartları: toplam sefer, çakışan, evrak riski, fatura bekleyen |
| 5.2 | Firma/Kurum grafikleri | `PlanlamaDashboard.razor` | Haftalık trend, yoğunluk dağılımı |
| 5.3 | UAT senaryoları | Döküman | CRUD, çakışma, kopyalama, faturalaşma |
| 5.4 | Performans iyileştirme | Service + UI | Sorgu optimizasyonu, sanal listeleme |
| 5.5 | Release checklist | Döküman | Rollback planı, izleme, destek |
| 5.6 | Smoke + regresyon testi | Test | Puantaj ekranları etkilenmemiş olmalı |

---

## 4) Kritik Operasyon Kuralları

| # | Kural | Tip |
|---|-------|-----|
| 1 | Aynı gün + aynı güzergah + aynı slotta birden fazla araç olamaz | **Blocking** |
| 2 | Aynı gün + aynı slotta aynı araç birden fazla güzergaha yazılamaz | **Blocking** |
| 3 | Aynı gün + aynı slotta aynı şoför birden fazla görev alamaz | **Blocking** |
| 4 | Kendi araç + tedarikçi şoför kombinasyonu onay gerektirir | **Warning** |
| 5 | Evrak süresi geçmiş araç/şoför ile kayıt onay gerektirir | **Warning** |

---

## 5) Risk Yönetimi

| Risk | Aksiyon | Sprint |
|------|---------|:------:|
| Mevcut puantajın bozulması | Feature flag + paralel ekran + geri dönüş planı | S1-S3 |
| İşveren Firma / Kurum ayrımı karışıyor | `KurumId` ve `IsverenFirmaId` zorunlu ayrım + form validasyon | S1 |
| Çakışma kontrolü yavaşlar | DB indexleri + sadece etkilenen kayıtta kontrol | S2 |
| Eski veriler slot kurallarına uymuyor | Dry-run migration + varsayılan slot=Sabah + manuel düzeltme | S2 |
| 3 firma arası yansıtma izlenemiyor | İç transfer alanları ekle | S4 |
| Yayın riski | UAT senaryoları + release checklist | S5 |

---

## 6) Başarı Kriterleri (Go/No-Go)

1. Puantaj mevcut akışları kırılmadan çalışmalı
2. Çoklu slot + sefer CRUD sorunsuz çalışmalı
3. Çakışmalar teknik olarak engellenmeli ve görsel olarak anlaşılır olmalı
4. Önceki ay kopyalama güvenli ve izlenebilir olmalı
5. 3 firma + kurum + işveren senaryosu finansal olarak takip edilebilmeli
6. `dotnet build` başarılı, `dotnet test` geçer, kritik hata bulunmamalı

---

## 7) Yeni Eklenecek Dosya Listesi

```
Web/Components/Pages/Planlama/
  Planlama.razor                  (ana sayfa)
  PlanlamaGrid.razor              (tablo bileşeni)
  PlanlamaEditModal.razor         (ekle/düzenle)
  PlanlamaConflictPanel.razor     (çakışma uyarı)
  CopyPreviousMonthModal.razor    (aydan kopyalama)
  FinansDurumPanel.razor          (finans durum)
  EvrakUyariPanel.razor           (evrak uyarı)
  PlanlamaDashboard.razor         (yönetim dashboard)
```

---

## 8) Zorunlu Kontrol Listesi (Her Sprint Sonu)

- [ ] `dotnet build` başarılı (0 hata, 0 uyarı)
- [ ] `dotnet test` başarılı (291+ test)
- [ ] KurumPuantaj ekranında regresyon yok
- [ ] Loglarda kritik hata yok
- [ ] Yeni ekranlar `[Authorize]` altında çalışıyor
- [ ] KAYIT-DEFTERI güncellendi
