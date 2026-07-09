# 🎯 Uygulama Planı: Hibrit Puantaj Input Sistemi
## Eğilim Modeli → Haftalık Özet → Günlük Detay

**Durum**: Ready for Implementation  
**Başlama Tarihi**: 23 Ocak 2025  
**Tahmini Tamamlama**: 1.5-2 ay (7 hafta)  
**Ekip**: Backend (C#/EF), Frontend (Blazor), DBA (PostgreSQL), QA

---

## 📋 Executive Summary

MKFiloServis'e **Türkiye Personel Taşıma Sektörü** için optimize edilmiş, **araç-başına güzergah-tabanlı puantaj** giriş sistemi eklenecek.

**Problem**: Şu anda toplu sayı giriş yapılıyor (ay × araç = 1 sayı), güzergah breakdown yok, makzul yönetimi zayıf.

**Çözüm**: 3-seviye hibrit input:
1. **Toplu Giriş (5 min)**: "Bu ay Rota 1'de 12 gün, Rota 2'de 8 gün" → Auto-calc sefer
2. **Haftalık Özet (5 min)**: Gözden geçir, şüpheli noktaları işaretle
3. **Günlük Detay (opsiyonel 5-10 min)**: Her gün sefer/durum fine-tuning

**Fayda**: ✅ Hızlı, ✅ Doğru, ✅ Makzul transparent, ✅ Fatura ready, ✅ SGK ready

---

## 🏗️ Faz Başına Detaylı Görevler

### **Faz 1: Database & Backend (Hafta 1-2)**

#### **Sprint 1.1: Entity Tasarım & Migration**

```
Task 1.1.1: PuantajTopluGiriş Entity Oluştur
├─ Dosya: MKFiloServis.Shared/Entities/PuantajTopluGiriş.cs
├─ Fields:
│  ├─ FirmaId (IFirmaTenant)
│  ├─ KurumFirmaId
│  ├─ AracId
│  ├─ Yil, Ay (Dönem)
│  ├─ DonempBasiTarihi (26-day period start)
│  ├─ DonepSonuTarihi (25-day period end)
│  ├─ RotaDetaylari (JSON array)
│  ├─ MakzulGunSayisi
│  ├─ ToplamSeferSayisi, ToplamGelir, ToplamMaliyet
│  ├─ Durum (Enum: Taslak, Incele Gunluk, Onayla, Hesaplandi)
│  ├─ OnayTarihi, OnayanKullaniciId
│  └─ Timestamps (Created, Updated)
├─ Navigation: Arac, Firma, GunlukPuantajlar (ICollection)
└─ Status: 🟢 TO DO

Task 1.1.2: PuantajTopluRotaDetay Entity Oluştur
├─ Dosya: MKFiloServis.Shared/Entities/PuantajTopluRotaDetay.cs
├─ Fields:
│  ├─ GuezergahId
│  ├─ GuezergahAdi (snapshot)
│  ├─ GunSayisi (kaç gün çalıştı)
│  ├─ SeferSayisiPerGun
│  ├─ TahminiTotalSefer (computed)
│  ├─ BirimFiyatSnapshot
│  ├─ GiderFiyatSnapshot
│  └─ Navigation: Guzergah
└─ Status: 🟢 TO DO

Task 1.1.3: EF Core Migration Oluştur
├─ Komut: dotnet ef migrations add AddPuantajTopluGiriş --project MKFiloServis.Web
├─ Dosya: MKFiloServis.Web/Data/Migrations/[timestamp]_AddPuantajTopluGiriş.cs
├─ Index'ler:
│  ├─ (FirmaId, AracId, Yil, Ay) - UNIQUE
│  ├─ (KurumFirmaId, DonempBasiTarihi)
│  └─ (Durum, OnayTarihi)
├─ Migration apply: dotnet ef database update --project MKFiloServis.Web
└─ Status: 🟢 TO DO
```

#### **Sprint 1.2: Service Layer**

```
Task 1.2.1: IPuantajTopluGirisiService Interface Oluştur
├─ Dosya: MKFiloServis.Web/Services/Interfaces/IPuantajTopluGirisiService.cs
├─ Methods:
│  ├─ CreateTopluGirişAsync(...)
│  ├─ GetHaftalikOzetAsync(...)
│  ├─ UpdateGunlukDetayAsync(...)
│  ├─ ValidateTuturlulukAsync(...)
│  └─ Onayla_GunlukPuantajlarOlusturAsync(...)
└─ Status: 🟢 TO DO

Task 1.2.2: PuantajTopluGirisiService Impl.
├─ Dosya: MKFiloServis.Web/Services/PuantajTopluGirisiService.cs
├─ Create() logic:
│  ├─ Dönem tarih hesaplama (26. günden başla)
│  ├─ Rota × Gün × Sefer/Gün hesaplama
│  ├─ Makzul kesintisi
│  ├─ Fiyat snapshot'ı (BirimFiyat, GiderFiyat)
│  └─ DB'ye kaydet
├─ GetHaftalikOzet() logic:
│  ├─ Haftalık breakdown (Mon-Sun)
│  ├─ Her gün/rota için sefer sayısı
│  └─ DTO return
├─ UpdateGunlukDetay() logic:
│  ├─ FiloGunlukPuantaj create or update
│  ├─ Tarih, Güzergah, Sefer, Durum, Notlar set
│  └─ DB'ye kaydet
└─ Status: 🟢 TO DO

Task 1.2.3: DTOs Oluştur
├─ PuantajHaftalikOzet.cs
├─ PuantajHaftaOzet.cs
├─ PuantajGunuOzet.cs
├─ PuantajGunRotaOzet.cs
├─ PuantajTopluGirisiCreateDTO.cs
└─ Status: 🟢 TO DO

Task 1.2.4: Unit Tests Ekle
├─ Dosya: MKFiloServis.Tests/Services/PuantajTopluGirisiServiceTests.cs
├─ Test Cases:
│  ├─ CreateTopluGiriş_ValidData_Success
│  ├─ CreateTopluGiriş_InvalidArac_Throws
│  ├─ GetHaftalikOzet_ReturnsCorrectWeeks
│  ├─ UpdateGunlukDetay_CreatesFiloGunlukPuantaj
│  └─ ValidateTuturluluk_Detects Uyuşmazlık
├─ Target Coverage: >90%
└─ Status: 🟢 TO DO
```

#### **Sprint 1.3: Dependency Injection & Configuration**

```
Task 1.3.1: Program.cs'te Service Kayıt
├─ Dosya: MKFiloServis.Web/Program.cs
├─ Add:
│  └─ services.AddScoped<IPuantajTopluGirisiService, PuantajTopluGirisiService>();
└─ Status: 🟢 TO DO

Task 1.3.2: Logger Configuration
├─ Blazor component'lerde ILogger<T> inject et
└─ Status: 🟢 TO DO

✅ Faz 1 Tamamlama Kriteri:
  □ All 4 tasks completed
  □ Unit tests pass (>90% coverage)
  □ Database migration applied
  □ No build errors
```

---

### **Faz 2: Blazor UI Components (Hafta 3-4)**

#### **Sprint 2.1: Toplu Giriş Sayfası**

```
Task 2.1.1: PuantajTopluGirisi.razor Oluştur
├─ Dosya: MKFiloServis.Web/Components/Pages/Puantaj/PuantajTopluGirisi.razor
├─ UI Elements:
│  ├─ Dönem seçimi (Yıl, Ay combobox)
│  ├─ Araç seçimi (dropdown, active araclar filtered)
│  ├─ Rota detayları (dynamic table)
│  │  ├─ Güzergah (dropdown)
│  │  ├─ Gün Sayısı (input)
│  │  ├─ Sefer/Gün (input)
│  │  ├─ Total Sefer (computed, read-only)
│  │  ├─ Birim Fiyat (snapshot display)
│  │  └─ Gider Fiyat (snapshot display)
│  ├─ Makzul Günü (input, 0-22)
│  ├─ Toplam Özet (Sefer, Gelir, Gider, Marj)
│  └─ Buttons: Toplu Ekle, Sıfırla, Devam Et
├─ Logic:
│  ├─ OnInitializedAsync(): Arac + Guzergah list yükle
│  ├─ AddRota(): Row ekle
│  ├─ RemoveRota(): Row sil
│  ├─ Calculate() method: Auto-update totals
│  └─ HandleSubmit(): CreateTopluGirişAsync() call + navigate to Haftalık
├─ Styling: Bootstrap 5, Responsive (mobile-friendly)
└─ Status: 🟢 TO DO

Task 2.1.2: PuantajTopluGirisi.razor.cs Code-Behind
├─ Properties:
│  ├─ _selectedYear, _selectedMonth, _selectedAracId
│  ├─ _rotaDetaylari (List<PuantajTopluRotaDetay>)
│  ├─ _makzulGunSayisi
│  ├─ _aracList, _rotaList
│  └─ Computed: _toplamSefer, _toplamGelir, _toplamMaliyet
├─ Methods:
│  ├─ LoadData()
│  ├─ AddRota(), RemoveRota()
│  ├─ UpdateRotaDetay()
│  └─ HandleSubmit()
└─ Status: 🟢 TO DO

Task 2.1.3: Test
├─ Manual test: Toplu giriş formuyla scenario'lar
│  ├─ 1 Araç, 1 Rota
│  ├─ 1 Araç, 2-3 Rota
│  ├─ Makzul yönetimi
│  └─ Form validation
└─ Status: 🟢 TO DO
```

#### **Sprint 2.2: Haftalık Özet Sayfası**

```
Task 2.2.1: PuantajHaftalikOzet.razor Oluştur
├─ Dosya: MKFiloServis.Web/Components/Pages/Puantaj/PuantajHaftalikOzet.razor
├─ UI Elements:
│  ├─ Route parameter: @page "/operasyon/puantaj-haftalik-ozet/{PuantajTopluGirisiId:int}"
│  ├─ Haftalık kartlar (foreach hafta)
│  │  ├─ Hafta başlığı (Hafta 1, Tarih aralığı)
│  │  ├─ Günlük tablo (thead: Gün | Rota | Sefer | Durum | İşlem)
│  │  ├─ Inline row'lar (her gün × rota = 1 row)
│  │  ├─ Red flag (sefer > 3 → text-danger)
│  │  └─ "Düzelt" button → navigate to günlük detail
│  ├─ Buttons: Geri, Onayla & Devam
│  └─ Summary: Toplam hafta sayısı, Toplam sefer
├─ Logic:
│  ├─ OnInitializedAsync(): GetHaftalikOzetAsync() call
│  ├─ EditDay(): Navigate to PuantajGunlukDetay + TarihStr param
│  └─ ApproveAndContinue(): Navigate to full günlük detay
└─ Status: 🟢 TO DO

Task 2.2.2: PuantajHaftalikOzet.razor.cs
├─ Properties:
│  ├─ [Parameter] PuantajTopluGirisiId
│  └─ _haftalikOzet (PuantajHaftalikOzet)
├─ Methods:
│  ├─ OnInitializedAsync()
│  ├─ EditDay(), ApproveAndContinue()
│  └─ GoBack()
└─ Status: 🟢 TO DO
```

#### **Sprint 2.3: Günlük Detay Sayfası**

```
Task 2.3.1: PuantajGunlukDetay.razor Oluştur
├─ Dosya: MKFiloServis.Web/Components/Pages/Puantaj/PuantajGunlukDetay.razor
├─ Route Parameters:
│  ├─ @page "/operasyon/puantaj-gunluk-detay/{PuantajTopluGirisiId:int}"
│  └─ @page "/operasyon/puantaj-gunluk-detay/{PuantajTopluGirisiId:int}/{TarihStr:int}"
├─ UI Elements:
│  ├─ Responsive table (table-responsive div)
│  ├─ Columns:
│  │  ├─ Tarih (ddd, dd MMM yyyy)
│  │  ├─ Rota / Güzergah
│  │  ├─ Sefer Sayısı (edit: input[number])
│  │  ├─ Durum (edit: select, display: badge)
│  │  ├─ Notlar (edit: input[text])
│  │  └─ İşlemler (Edit / Kaydet-İptal)
│  ├─ Inline editing: @if (IsEditing) → edit controls else → display
│  ├─ Buttons:
│  │  ├─ Haftalık Özete Dön
│  │  ├─ Uyuşmazlık Kontrol Et (validation check)
│  │  └─ Tamamla & Faturaya Git
│  └─ Toastr notifications (Success/Warning)
├─ Logic:
│  ├─ LoadGunlukDetaylar(): if(TarihStr) filter else all days
│  ├─ EditDetay(), SaveDetay(), CancelEdit()
│  ├─ ShowValidationCheck(): ValidateTuturlulukAsync() → ShowWarning if fark
│  └─ FinalizeAndGoToFatura()
└─ Status: 🟢 TO DO

Task 2.3.2: PuantajGunlukDetay.razor.cs
├─ Properties:
│  ├─ [Parameter] PuantajTopluGirisiId, TarihStr
│  ├─ _gunlukDetaylar (List<PuantajGunlukDetayDTO>)
│  └─ _rotaList (List<Guzergah>)
├─ Methods:
│  ├─ OnInitializedAsync()
│  ├─ EditDetay(), SaveDetay(), CancelEdit()
│  ├─ ShowValidationCheck()
│  └─ FinalizeAndGoToFatura()
└─ Status: 🟢 TO DO

Task 2.3.3: DTOs
├─ PuantajGunlukDetayDTO.cs
│  ├─ Tarih
│  ├─ GuezergahId, GuezergahAdi
│  ├─ SeferSayisi
│  ├─ Durum, DurumAdi
│  ├─ Notlar
│  └─ IsEditing (UI state)
└─ Status: 🟢 TO DO
```

#### **Sprint 2.4: Navigation Menu & Routing**

```
Task 2.4.1: NavMenu.razor Güncelle
├─ Dosya: MKFiloServis.Web/Components/Layout/NavMenu.razor
├─ Ekle:
│  ├─ "Araç Puantajı (Güzergah Bazlı)" menu item
│  │  ├─ Sub-item: "Toplu Giriş"
│  │  ├─ Sub-item: "Haftalık Özet"
│  │  └─ Sub-item: "Günlük Detay"
│  └─ Link: /operasyon/puantaj-toplu-girisi
└─ Status: 🟢 TO DO

✅ Faz 2 Tamamlama Kriteri:
  □ All 3 pages (Toplu, Haftalık, Günlük) rendered
  □ Navigation working
  □ Form submissions successful
  □ No JavaScript errors (F12 console clean)
  □ Responsive on mobile (375px width test)
```

---

### **Faz 3: API Controller (Hafta 4, Opsiyonel)**

```
Task 3.1: PuantajTopluGirisiController Oluştur
├─ Dosya: MKFiloServis.Web/Controllers/PuantajTopluGirisiController.cs
├─ Attributes: [ApiController], [Route("api/[controller]")], [Authorize]
├─ Endpoints:
│  ├─ [POST] /api/puantaj-toplu-girisleri
│  ├─ [GET] /api/puantaj-toplu-girisleri/{id}
│  ├─ [GET] /api/puantaj-toplu-girisleri/{id}/haftalik-ozet
│  ├─ [PUT] /api/puantaj-toplu-girisleri/{id}/gunluk-detay
│  └─ [POST] /api/puantaj-toplu-girisleri/{id}/onayla
├─ Error Handling: Try-catch + ModelState validation
└─ Status: 🟢 TO DO

✅ Faz 3 Tamamlama:
  □ All endpoints working (Postman test)
  □ Authorization checked
  □ Response DTOs match Blazor expectations
```

---

### **Faz 4: Integration & FaturaBandırma (Hafta 5)**

#### **Sprint 4.1: FaturaBandırma Integration**

```
Task 4.1.1: FaturaBandırmaService Güncelle
├─ Method: CalculateFaturaTahakkuku()
├─ New Logic:
│  ├─ PuantajTopluGiriş + FiloGunlukPuantaj verilerini oku
│  ├─ Her Güzergah × Sefer × Fiyat hesapla
│  ├─ ToplamGelir oluştur (Kurumdan tahsil)
│  ├─ ToplamMaliyet oluştur (Şoför/Araç)
│  └─ HakedisFatura tablosuna yaz
└─ Status: 🟢 TO DO

Task 4.1.2: SGK Report Generation Güncelle
├─ Method: GenerateSGKReport()
├─ New Logic:
│  ├─ FiloGunlukPuantaj'dan işçi puantaj verilerini oku (Durum = Gitti)
│  ├─ Makzul günleri exclude et (Durum = Makzul)
│  └─ SGK elektronik bildirisi format'ına convert
└─ Status: 🟢 TO DO

Task 4.1.3: Test
├─ E2E test: Toplu Giriş → Haftalık → Günlük → Fatura → SGK
├─ Verification:
│  ├─ FiloGunlukPuantaj tablosu dolu mu?
│  ├─ HakedisFatura hesapları doğru mu?
│  └─ SGK raporu generate oluyor mu?
└─ Status: 🟢 TO DO
```

---

### **Faz 5: Testing & Quality Assurance (Hafta 5-6)**

#### **Sprint 5.1: Functional Testing**

```
Task 5.1.1: Test Scenarios (Manual)
├─ Scenario 1: Basit Araç, 1 Rota, 22 gün
│  ├─ Toplu Giriş: 22 gün → 22 sefer
│  ├─ Haftalık Özet: 5 hafta görülsün
│  ├─ Günlük Detay: Her gün "Gitti" durumunda
│  └─ Fatura: 22 × Fiyat = Toplam Gelir ✓
│
├─ Scenario 2: 2 Rota, Makzul
│  ├─ Toplu Giriş: Rota 1: 12 gün, Rota 2: 8 gün, Makzul: 2 gün
│  ├─ Haftalık: Hafta 3'te Makzul günleri "Makzul" durumunda
│  ├─ Günlük: Her Rota separate row
│  └─ Fatura: (12 × Rota1Fiyat) + (8 × Rota2Fiyat) ✓
│
├─ Scenario 3: Uyuşmazlık
│  ├─ Toplu: 30 sefer
│  ├─ Günlük: 28 sefer (2 seferi unuttuk)
│  └─ Validation: Warning "Uyuşmazlık: -2 sefer" ✓
│
└─ Scenario 4: Mid-month Araç Değişimi
   ├─ Toplu: Araç A for 15 days
   ├─ Sonra: Araç B for 7 days (yeni Toplu Giriş)
   └─ Raporlama: Toplam 22 gün (Araç A + B split) ✓

Status: 🟢 TO DO
```

#### **Sprint 5.2: Performance Testing**

```
Task 5.2.1: Load Test (PostgreSQL Optimization)
├─ Setup: 1000 araç × 22 gün = 22.000 FiloGunlukPuantaj record
├─ Query Test:
│  ├─ GetHaftalikOzetAsync() < 1 sec
│  ├─ UpdateGunlukDetayAsync() < 500 ms
│  └─ ValidateTuturlulukAsync() < 1 sec
├─ Index Verification:
│  ├─ idx_guzergah_firma_kurum
│  ├─ idx_puantaj_tarih_filo
│  └─ idx_eslestirme_arac_sofor
└─ Status: 🟢 TO DO

Task 5.2.2: UI Performance
├─ Blazor rendering time:
│  ├─ Toplu Giriş form: < 500 ms
│  ├─ Haftalık Özet (5 hafta): < 1 sec
│  └─ Günlük Grid (22 row): < 1 sec
├─ Browser DevTools:
│  ├─ No memory leaks
│  ├─ Component disposal proper
│  └─ No excessive re-renders
└─ Status: 🟢 TO DO
```

#### **Sprint 5.3: Security Testing**

```
Task 5.3.1: Authorization
├─ Only Operatör & Yönetici roles can access
├─ Multi-tenant isolation: Firma filter applied
├─ Kurumlar: KurumFirmaId != 0 (not null)
└─ Status: 🟢 TO DO

Task 5.3.2: Data Validation
├─ XSS protection in Notlar field
├─ SQL injection resistance (EF Core parameterized queries)
├─ CSRF tokens on form submit
└─ Status: 🟢 TO DO
```

#### **Sprint 5.4: UAT (User Acceptance Testing)**

```
Task 5.4.1: Pilot Customers (3 firmalar)
├─ Customer 1: Küçük firma (1-2 araç)
├─ Customer 2: Orta firma (5-10 araç)
├─ Customer 3: Büyük firma (20+ araç)
├─ Feedback collection: Forms + Calls
├─ Issues fix: P1 (critical) next day, P2 (normal) in 2 days
└─ Status: 🟢 TO DO

Task 5.4.2: Feedback Integration
├─ UI/UX improvements (if needed)
├─ Performance tweaks
├─ Documentation updates
└─ Status: 🟢 TO DO
```

---

### **Faz 6: Documentation & Training (Hafta 6.5)**

#### **Sprint 6.1: User Documentation**

```
Task 6.1.1: User Manual (Türkçe)
├─ Dosya: MKFiloServis.Web/Docs/PUANTAJ-KULLANICI-KILAVUZU-TR.md
├─ Sections:
│  ├─ Giriş (Nedir bu sistem, neden lazım?)
│  ├─ Adım Adım Toplu Giriş
│  ├─ Adım Adım Haftalık Özet
│  ├─ Adım Adım Günlük Detay
│  ├─ Makzul Yönetimi
│  ├─ Sık Sorulan Sorular (FAQ)
│  └─ Troubleshooting (Hata mesajları)
│
├─ Screenshots: Türkçe arayüz + arrow/highlight'lar
├─ Printing: PDF-friendly format
└─ Status: 🟢 TO DO

Task 6.1.2: Video Tutorials (3 video)
├─ Video 1: Toplu Giriş (2 min)
│  ├─ Screen recording (Blazor sayfası)
│  ├─ Voice-over (Türkçe)
│  └─ Subtitle (Türkçe)
│
├─ Video 2: Haftalık & Günlük (3 min)
│  └─ Özet + Detay flow
│
└─ Video 3: Fatura & SGK Hazırlığı (2 min)
   └─ Muhasebeci perspektifi

Upload: YouTube (private link) / Company portal

Status: 🟢 TO DO (Opsiyonel, eğitim yapılabilir)
```

#### **Sprint 6.2: Support & Operations**

```
Task 6.2.1: Support Script Oluştur
├─ Common Issues:
│  ├─ "Toplu giriş formu submit edilmiyor"
│  ├─ "Haftalık özet 4 hafta gösteriyor, 5 olmalı"
│  ├─ "Makzul günü çıkartılmadı"
│  └─ "Fatura ile puantaj uyuşmuyor"
│
├─ Troubleshooting flowchart (Karar ağacı)
└─ Status: 🟢 TO DO

Task 6.2.2: Training Sessions
├─ Live Training (Zoom): 2-3 session
│  ├─ Session 1: Operatörlere (Toplu Giriş demo)
│  ├─ Session 2: Muhasebecilere (Fatura & SGK)
│  └─ Session 3: Q&A session
│
├─ Duration: 30-45 min her session
├─ Recording: Archive untuk future employees
└─ Status: 🟢 TO DO

Task 6.2.3: Knowledge Base Yazı
├─ Wiki/Portal entry'si
├─ Linking: Help menu'den accessible
└─ Status: 🟢 TO DO
```

---

### **Faz 7: Deployment & Go-Live (Hafta 6.5 - 7)**

#### **Sprint 7.1: Staging Deployment**

```
Task 7.1.1: Staging Ortamında Deploy
├─ Database: Copy production schema'yı, dummy data ekle
├─ Code: Deploy latest build
├─ Configuration: Staging app settings (email, URLs)
└─ Status: 🟢 TO DO

Task 7.1.2: Post-Deployment Validation
├─ All URLs accessible
├─ Database migrations applied
├─ Service endpoints healthy
├─ Logging working
├─ Error handling tested
└─ Status: 🟢 TO DO

Task 7.1.3: Staging Testing (48 hours)
├─ Full end-to-end test
├─ Performance baseline
├─ User access test (admin, operatör, muhasebe)
└─ Status: 🟢 TO DO
```

#### **Sprint 7.2: Production Deployment**

```
Task 7.2.1: Pre-Deployment Checklist
├─ Database backup: ✓
├─ Rollback plan: ✓ (Previous version ready)
├─ Monitoring alerts: ✓ (Configured for new endpoints)
├─ Support team: ✓ (On standby)
├─ Maintenance window: ✓ (Off-peak time scheduled)
└─ Status: 🟢 TO DO

Task 7.2.2: Production Deploy
├─ Run migrations
├─ Deploy application
├─ Smoke test (Basic functionality)
├─ Monitor logs (First 2 hours)
└─ Status: 🟢 TO DO

Task 7.2.3: Post-Go-Live Support (1 week)
├─ 24/7 support team
├─ Daily health check'ler
├─ Users'dan feedback topla
├─ Quick fixes (if any issues)
└─ Status: 🟢 TO DO
```

---

## ✅ Tamamlama Kriterleri & Success Metrics

### **Go-Live Gates**

```
Gate 1: Backend Ready
├─ [ ] Database migration successful
├─ [ ] Service layer tested (>90% coverage)
├─ [ ] No build errors
└─ Target: End of Hafta 2

Gate 2: Frontend Ready
├─ [ ] All 3 Blazor pages rendering
├─ [ ] Navigation working
├─ [ ] Form submissions successful
├─ [ ] Mobile responsive
└─ Target: End of Hafta 4

Gate 3: Integration Ready
├─ [ ] FaturaBandırma integration working
├─ [ ] SGK export working
├─ [ ] E2E test passed
└─ Target: End of Hafta 5

Gate 4: Quality Ready
├─ [ ] UAT passed (3 pilot customers)
├─ [ ] Performance baseline met
├─ [ ] Security checklist passed
├─ [ ] Documentation complete
└─ Target: End of Hafta 6

Gate 5: Production Ready
├─ [ ] Staging deployment successful
├─ [ ] Production rollback plan ready
├─ [ ] Support team trained
├─ [ ] Monitoring configured
└─ Target: Hafta 7 Go-Live
```

### **Success Metrics**

```
Metric 1: Adoption
├─ Target: >80% active firms using by week 2
└─ Measure: Login count, form submissions

Metric 2: Data Quality
├─ Target: <5% uyuşmazlık rate (TopluGiriş vs Günlük)
└─ Measure: ValidateTuturluluk() warnings ratio

Metric 3: User Satisfaction
├─ Target: 4.0+ / 5.0 NPS score
└─ Measure: Post-launch survey

Metric 4: Performance
├─ Target: Haftalık Özet < 1 sec load time
└─ Measure: Application Insights metrics

Metric 5: Support Tickets
├─ Target: <10 support tickets/day by week 2
└─ Measure: Help desk tracking
```

---

## 👥 Ekip & Sorumluluklar

```
Backend Engineer (1 person):
├─ Database design + Migration
├─ Service layer implementation
├─ Integration with FaturaBandırma
└─ ~3-4 weeks

Frontend Engineer (1 person):
├─ 3 Blazor page implementation
├─ Responsive design
├─ Client-side validation
└─ ~2-3 weeks

QA Engineer (1 person):
├─ Test plan creation
├─ Functional testing
├─ UAT coordination
└─ ~2-3 weeks

Database Admin (0.5 person):
├─ Migration script review
├─ Index optimization
├─ Backup/recovery plan
└─ ~1 week

Product Owner:
├─ Requirements clarification
├─ Pilot customer coordination
├─ Feedback integration
└─ ~ongoing

Support Lead:
├─ Documentation
├─ Training material creation
├─ Support team training
└─ ~1 week
```

---

## 📊 Project Timeline (Visual)

```
Week 1    [█████] Sprint 1.1-1.2 (DB + Service)
Week 2    [█████] Sprint 1.3 + Testing (Backend complete)
Week 3    [█████] Sprint 2.1 (Toplu Giriş page)
Week 4    [█████] Sprint 2.2-2.3 (Haftalık + Günlük pages)
Week 5    [█████] Sprint 4.1 (Integration) + 5.1-5.2 (Testing)
Week 6    [█████] Sprint 5.3-5.4 (UAT) + 6.1-6.2 (Training)
Week 6.5  [██  ] Sprint 7.1 (Staging)
Week 7    [████ ] Sprint 7.2 (Production Go-Live + Support)

Total: 7 hafta, 1.5-2 ay
```

---

## 🎯 Key Success Factors

1. **Clear Scope**: Hibrit model tanımı net, fuzzy requirement yok
2. **Strong Pilot**: 3 different-sized firm ile UAT zorunlu
3. **Training Early**: Week 5'ten başla, go-live'dan 2 hafta önce
4. **Performance First**: Index'ler + Query optimization commit 1'de
5. **Support Ready**: FAQ + Runbook önceden prepared
6. **Monitoring Active**: Day 1'den başla, health check hourly

---

## 📝 Notlar & Riskler

```
Risk 1: Durum Yönetimi Karmaşıklığı
└─ Mitigation: "Makzul = Sefer kesintisi" kuralı crystal clear

Risk 2: Mid-Month Araç Değişimi
└─ Mitigation: Separate PuantajTopluGiriş records (her araç)

Risk 3: Fatura Uyuşmazlığı
└─ Mitigation: ValidateTuturluluk() warning'i, manual review approval

Risk 4: Mobile/Tablet UI
└─ Mitigation: Week 5'te responsive test, bootstrap grid system

Risk 5: Database Performance (1000+ records)
└─ Mitigation: Index'ler week 1, Load test week 5
```

---

**Hazırladı**: Claude Code Analysis  
**Tarih**: 23 Ocak 2025  
**Versiyonu**: 1.0 - Ayrıntılı Proje Planı  
**Durum**: ✅ Ready for Sprint Planning
