# 📚 Türkiye Puantaj Sistemi - Hızlı Referans Rehberi

**Tarih**: 23 Ocak 2025  
**Durum**: ✅ Implementation Ready  
**Ana Belge**: `TURKIYE-SORUNLARI-PUANTAJ-GUZERGAH-INPUT-YONTEMI.md`  
**Plan Belgesi**: `IMPLEMENTATION-PLAN-HIBRIT-PUANTAJ-SISTEM.md`  
**YENİ - Satır Grid Tasarımı**: `GUNLUK-GRID-TASARIMI-SATIR-BAZLI.md` ⭐ (23 Ocak 2025)

---

## 🎯 Hızlı Özet (1 Dakika)

### Problem
Türkiye'de personel taşıma sektöründe, şu anda **araç başına aylık toplu sayı** giriliyorsa (örn: "Araç A: 22 sefer"), **güzergah breakdown yok** ve **makzul yönetimi zayıf**.

### Çözüm
3-seviye **hibrit input modeli**:
1. **Toplu Giriş** (5 min): "Rota 1'de 12 gün, Rota 2'de 8 gün, Makzul 2 gün" → Auto-calc
2. **Haftalık Özet** (5 min): Gözden geçir, şüpheli noktaları işaretle
3. **Günlük Detay** (opsiyonel, 5-10 min): Fine-tuning (Sefer/Durum/Notlar)

### Fayda
- ✅ **Hızlı**: Toplu giriş 5 min, total 10-15 min (vs. 45 min detaylı)
- ✅ **Doğru**: Güzergah × Gün × Sefer/Gün formula + makzul transparent
- ✅ **Fatura Ready**: Auto-calc: (Rota 1 × Sefer1 × Fiyat1) + (Rota 2 × Sefer2 × Fiyat2)
- ✅ **SGK Ready**: FiloGunlukPuantaj tablosu otomatik doldurulur

### Zaman
**6-7 hafta** (1.5-2 ay), 1 Backend + 1 Frontend + 1 QA engineer

---

## 📂 Belge Haritası

```
MKFiloServis.Web/Docs/
│
├─ TURKIYE-SORUNLARI-PUANTAJ-GUZERGAH-INPUT-YONTEMI.md ← ANA ANALIZ
│  ├─ Türkiye Pazar Yapısı & Pain Points
│  ├─ Mevcut MKFiloServis Durumu
│  ├─ 3 Input Yöntemi Karşılaştırması (Toplu vs Detaylı vs Hibrit)
│  ├─ Önerilen Hibrit Model (Architecture, Data Model, Flow)
│  ├─ Backend Service Kod Örnekleri (C# impl.)
│  ├─ Blazor UI/UX Tasarımı (3 page mock-up)
│  ├─ 3-Faz Uygulama Yol Haritası
│  └─ Operations Summary & Training Plan
│
├─ IMPLEMENTATION-PLAN-HIBRIT-PUANTAJ-SISTEM.md ← DETAYLI PLAN
│  ├─ Executive Summary
│  ├─ 7 Faz Detaylı Görevler
│  │  ├─ Faz 1: Database & Backend (Hafta 1-2)
│  │  ├─ Faz 2: Blazor UI Components (Hafta 3-4)
│  │  ├─ Faz 3: API Controller (Hafta 4, opsiyonel)
│  │  ├─ Faz 4: Integration & Fatura (Hafta 5)
│  │  ├─ Faz 5: Testing & QA (Hafta 5-6)
│  │  ├─ Faz 6: Documentation (Hafta 6.5)
│  │  └─ Faz 7: Deployment (Hafta 6.5-7)
│  ├─ Go-Live Gates & Success Metrics
│  ├─ Ekip & Sorumluluklar
│  ├─ Project Timeline (Gantt visual)
│  ├─ Key Success Factors
│  └─ Riskler & Mitigation
│
└─ TURKIYE-PUANTAJ-QUICK-REFERENCE.md ← BU DOSYA
   ├─ Hızlı Özet
   ├─ Belge Haritası (you are here)
   ├─ Teknik Checklist
   ├─ Key Entities & Fields
   ├─ UI Navigation Map
   ├─ FAQ & Troubleshooting
   ├─ Ekip İçin Ödevler
   └─ Ek Referanslar
```

---

## 🛠️ Teknik Checklist (Copy-Paste for Trello/Jira)

### Faz 1: Database & Backend
```
[ ] Task 1.1.1: PuantajTopluGiriş Entity (MKFiloServis.Shared/Entities/)
    Assignee: Backend Lead
    Fields: FirmaId, KurumFirmaId, AracId, Yil, Ay, DonempBasiTarihi, 
            DonepSonuTarihi, RotaDetaylari (JSON), MakzulGunSayisi, 
            ToplamSeferSayisi, ToplamGelir, ToplamMaliyet, Durum, OnayTarihi
    Deadline: Hafta 1

[ ] Task 1.1.2: PuantajTopluRotaDetay Entity
    Assignee: Backend Lead
    Deadline: Hafta 1

[ ] Task 1.1.3: EF Core Migration
    Assignee: Backend Lead + DBA
    Indices: (FirmaId, AracId, Yil, Ay) UNIQUE, (KurumFirmaId, DonempBasiTarihi)
    Deadline: Hafta 1

[ ] Task 1.2.1: IPuantajTopluGirisiService Interface
    Assignee: Backend Lead
    Methods: CreateTopluGirişAsync, GetHaftalikOzetAsync, UpdateGunlukDetayAsync, 
             ValidateTuturlulukAsync, Onayla_GunlukPuantajlarOlusturAsync
    Deadline: Hafta 1

[ ] Task 1.2.2: PuantajTopluGirisiService Implementation
    Assignee: Backend Lead
    Coverage: >90% unit test
    Deadline: Hafta 2

[ ] Task 1.2.3-4: DTOs & Unit Tests
    Assignee: Backend Lead
    Deadline: Hafta 2

[ ] Task 1.3.1-2: DI & Logger Configuration
    Assignee: Backend Lead
    Deadline: Hafta 2

✅ Faz 1 Gate: All tasks done, build clean, tests pass
```

### Faz 2: Blazor UI
```
[ ] Task 2.1: PuantajTopluGirisi.razor (Toplu Giriş Page)
    Assignee: Frontend Lead
    Components: Dönem (Yıl/Ay), Araç dropdown, Rota dynamic table, 
                Makzul input, Özet cards, Buttons
    Deadline: Hafta 3

[ ] Task 2.2: PuantajHaftalikOzet.razor (Haftalık Özet)
    Assignee: Frontend Lead
    Components: Hafta kartları, Gün × Rota grid, Red flag (sefer>3), 
                "Düzelt" buttons, Summary
    Deadline: Hafta 3-4

[ ] Task 2.3: PuantajGunlukDetay.razor (Günlük Detay)
    Assignee: Frontend Lead
    Components: Responsive table, Inline editing, Durum select, Notlar field, 
                "Uyuşmazlık Kontrol Et" button
    Deadline: Hafta 4

[ ] Task 2.4: NavMenu Update
    Assignee: Frontend Lead
    Menu items: "Araç Puantajı (Güzergah Bazlı)" → Toplu/Haftalık/Günlük
    Deadline: Hafta 4

✅ Faz 2 Gate: All pages render, navigation works, no JavaScript errors
```

### Faz 3-4: Integration & Testing
```
[ ] Task 3.1: PuantajTopluGirisiController (Opsiyonel REST API)
    Deadline: Hafta 4

[ ] Task 4.1: FaturaBandırmaService Integration
    Assignee: Backend Lead
    Logic: Rota × Sefer × Fiyat → HakedisFatura
    Deadline: Hafta 5

[ ] Task 4.2: SGK Report Generation
    Deadline: Hafta 5

[ ] Task 5.1: Functional Testing (3 Scenarios + Makzul + Uyuşmazlık)
    Assignee: QA
    Deadline: Hafta 5-6

[ ] Task 5.2-5.4: Performance, Security, UAT
    Assignee: QA + 3 Pilot Customers
    Deadline: Hafta 6

✅ Faz 4-5 Gate: E2E test passed, UAT feedback integrated, Performance OK
```

### Faz 6-7: Documentation & Go-Live
```
[ ] Task 6.1: User Manual (Türkçe) + Video Tutorials (3 video)
    Assignee: Product Owner + Support Lead
    Deadline: Hafta 6.5

[ ] Task 6.2: Training Sessions (3 session, Zoom)
    Assignee: Support Lead
    Deadline: Hafta 6.5

[ ] Task 7.1: Staging Deployment
    Assignee: DevOps Lead
    Deadline: Hafta 6.5

[ ] Task 7.2: Production Deployment
    Assignee: DevOps Lead
    Support: 24/7 team on standby
    Deadline: Hafta 7

✅ Faz 6-7 Gate: Documentation complete, Training done, Deployment successful
```

---

## 🗂️ Key Entities & Fields

### PuantajTopluGiriş
```csharp
public class PuantajTopluGiriş : BaseEntity, IFirmaTenant
{
    // Tenant & Reference
    public int FirmaId { get; set; }              // Multi-tenant
    public int KurumFirmaId { get; set; }          // İşi aldığımız kurum
    public int AracId { get; set; }                // Hangi araç

    // Dönem (26. dönem yönetimi)
    public int Yil { get; set; }                   // 2025
    public int Ay { get; set; }                    // 1 (Ocak)
    public DateTime DonempBasiTarihi { get; set; } // 2024-12-26 (önceki ay 26'si)
    public DateTime DonepSonuTarihi { get; set; }  // 2025-01-25 (bu ay 25'i)

    // Rota Detayları
    public List<PuantajTopluRotaDetay> RotaDetaylari { get; set; }
    // [
    //   { GuezergahId: 1, GuezergahAdi: "Ankara → TRT", GunSayisi: 12, SeferSayisiPerGun: 1.0 },
    //   { GuezergahId: 2, GuezergahAdi: "Ankara → Banka", GunSayisi: 8, SeferSayisiPerGun: 3.0 }
    // ]

    // Makzul Yönetimi
    public int MakzulGunSayisi { get; set; }       // 2 gün makzul
    public decimal MakzulToplamSefer { get; set; } // -2 sefer (kesinti)

    // Hesaplanan Değerler
    public decimal ToplamSeferSayisi { get; set; } // (12×1) + (8×3) - 2 = 34 sefer
    public decimal ToplamGelir { get; set; }       // 12×150 + 8×100 = 2400₺
    public decimal ToplamMaliyet { get; set; }     // 12×80 + 8×60 = 1440₺

    // Durum & Onay
    public PuantajTopluGirisDurumu Durum { get; set; }
    // Değerleri: TaslakOlusturuldu, InceleniyorGunlukFormat, GunlukDetayOnayi, Onaylandi, Hesaplandi
    public DateTime OnayTarihi { get; set; }
    public int? OnayanKullaniciId { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PuantajTopluRotaDetay
{
    public int GuezergahId { get; set; }
    public string GuezergahAdi { get; set; }           // "Ankara → TRT"
    public int GunSayisi { get; set; }                 // 12 gün
    public decimal SeferSayisiPerGun { get; set; }     // 1.0 sefer/gün
    public decimal TahminiTotalSefer { get; set; }     // 12 sefer (computed)
    public decimal BirimFiyatSnapshot { get; set; }    // 150₺ (Rota fiyatı o dönem)
    public decimal GiderFiyatSnapshot { get; set; }    // 80₺ (Şoför/Araç maliyeti)
}

public enum PuantajTopluGirisDurumu
{
    TaslakOlusturuldu = 1,        // Toplu giriş yapıldı
    InceleniyorGunlukFormat = 2,  // Haftalık özet review ediliyor
    GunlukDetayOnayi = 3,         // Günlük detay kontrol ediliyor
    Onaylandi = 4,                // Tüm onaylardan geçti
    Hesaplandi = 5                // Fatura & SGK hesaplaması yapıldı
}
```

### FiloGunlukPuantaj (Mevcut, Genişlete Edilecek)
```csharp
public class FiloGunlukPuantaj : BaseEntity, IFirmaTenant
{
    // Existing fields (unchanged)
    public int FirmaId { get; set; }
    public DateTime Tarih { get; set; }
    public int FiloGuzergahEslestirmeId { get; set; }   // Template reference (opsiyonel)
    public int KurumFirmaId { get; set; }
    public int AracId { get; set; }
    public int GuezergahId { get; set; }

    // Values
    public decimal SeferSayisi { get; set; }            // 1, 2, 3, etc.
    public OperasyonDurumu Durum { get; set; }          // Gitti, Makzul, Taksi, vb.
    public string? Notlar { get; set; }                 // "Makzul (K)" vs notlar

    // New field (FK to PuantajTopluGiriş)
    public int? PuantajTopluGirisiId { get; set; }      // Source from hybrid entry
    public virtual PuantajTopluGiriş? PuantajTopluGirisi { get; set; }

    // Tahakkuk
    public decimal TahakkukEdenKurumUcreti { get; set; }
    public decimal TahakkukEdenTaseronUcreti { get; set; }
    public bool Onaylandi { get; set; }
}

public enum OperasyonDurumu
{
    Gitti = 1,      // Sefer yapıldı
    Makzul = 2,     // İzin/Hastalık/Diğer
    Taksi = 3,      // Taksi ile gönderildi
    Tatil = 4,      // Resmî tatil
    Isyok = 5,      // İş yok (W/O - unscheduled)
    Iptal = 6       // Sefer iptal
}
```

---

## 🖼️ UI Navigation Map

```
┌─────────────────────────────────────────────────────────┐
│  NavMenu: "Araç Puantajı (Güzergah Bazlı)"              │
└────────────────┬────────────────────────────────────────┘
                 │
                 ├─ Toplu Giriş ──────────────┐
                 │                            │
                 ├─ Haftalık Özet             │
                 │                            ▼
                 └─ Günlük Detay         /operasyon/
                                         puantaj-toplu-girisi

     Level 1: Toplu Giriş
     ─────────────────────
     ├─ Dönem seç (Yıl, Ay)
     ├─ Araç seç
     ├─ Rota detayları (dynamic table)
     │  └─ + Rota Ekle
     ├─ Makzul günü
     ├─ Özet (Sefer, Gelir, Gider, Marj)
     └─ [Devam Et] ──→ (Haftalık Özet'e git)

         Level 2: Haftalık Özet
         ─────────────────────────
         ├─ Hafta 1: 26 Oca - 1 Şub
         │  ├─ Pazartesi | Rota 1 | 1 sefer | Gitti | [Düzelt]
         │  ├─ Salı | Rota 1 | 1 sefer | Gitti | [Düzelt]
         │  └─ ... Cuma
         ├─ Hafta 2: 2 Şub - 8 Şub
         │  └─ ... (similar)
         ├─ [Geri] (Toplu Giriş'e dön)
         └─ [Onayla & Devam Et] ──→ (Günlük Detay'a git)

             Level 3: Günlük Detay
             ──────────────────────
             ├─ Responsive Table:
             │  ├─ Tarih | Rota | Sefer | Durum | Notlar | İşlem
             │  ├─ 26 Oca | Rota 1 | 1 | Gitti | | [✎ Düzelt]
             │  ├─ 27 Oca | Rota 1 | 1 | Gitti | | [✎ Düzelt]
             │  └─ ... 25 Şub (22 iş günü)
             │
             │  (Eğer user [✎ Düzelt] tıklarsa)
             │  ├─ Sefer: [input number]
             │  ├─ Durum: [select] (Gitti, Makzul, Taksi, ...)
             │  ├─ Notlar: [input text]
             │  └─ [✓ Kaydet] [✕ İptal]
             │
             ├─ [🔍 Uyuşmazlık Kontrol Et]
             │  └─ "Toplu: 34 sefer, Günlük: 34 sefer ✓ Uyuşmazlık yok!"
             │  └─ "OR: ⚠️ Uyuşmazlık: -2 sefer farkı (Düzelt → Save)"
             │
             ├─ [← Haftalık Özete Dön]
             └─ [✓ Tamamla & Faturaya Git] ──→ /operasyon/puantaj-fatura-mutabakat
                (Auto-navigate: FiloGunlukPuantaj tablosu dolduruldu)
```

---

## ❓ FAQ & Troubleshooting

### Q1: "Makzul'ü nasıl yönetiyorum?"
```
A: Toplu Giriş'te "Makzul Günleri" alanına sayı gir.
   Örn: 2 gün makzul → 22 gün = 20 net sefer

   Günlük Detay'da istersen özel gün için "Makzul" durumunu set edebilirsin.
   İkisi birden conflict'e girerlerse: Tamamla button alert verir.
```

### Q2: "Araç ortası Rota değiştirdiyse?"
```
A: İki ayrı PuantajTopluGiriş oluştur:
   - PPG 1: "Araç A, Rota 1, 15 gün"
   - PPG 2: "Araç A, Rota 2, 7 gün"

   İkisi otomatik SUM'lanır raporlama'da.
```

### Q3: "Haftalık başında uyuşmazlık fark ettim, ne yapacağım?"
```
A: Günlük Detay'a git, [Düzelt] tıkla, doğru sefer/durum/notları yaz, [Kaydet].
   Tekrar [🔍 Kontrol Et] tıkla, hata kaybolana kadar repeat et.
```

### Q4: "Fatura otomatik üretilmedi?"
```
A: PuantajTopluGiriş.Durum = "Hesaplandi" olması lazım.
   Tamamla & Faturaya Git'i tıkladın mı?

   Eğer tıkladıysan: Fatura sayfasını refresh et, 
   application logs'ta hata varsa support'a bildir.
```

### Q5: "SGK bildirimi nerede?"
```
A: Fatura sayfasında (Muhasebe menüsü) "SGK Raporu Oluştur" button'u var.
   1-click'le otomatik FiloGunlukPuantaj'dan SGK format'ı generate edilecek.
```

---

## 📝 Ekip İçin Ödevler

### Backend Lead
```
Hafta 1:
  [ ] PuantajTopluGiriş + PuantajTopluRotaDetay entity'leri oluştur
  [ ] Migration script yaz
  [ ] IPuantajTopluGirisiService interface tanımla

Hafta 2:
  [ ] CreateTopluGirişAsync() + GetHaftalikOzetAsync() impl.
  [ ] UpdateGunlukDetayAsync() + ValidateTuturlulukAsync() impl.
  [ ] Unit tests yaz (>90%)
  [ ] Program.cs'te DI kaydet

Hafta 5:
  [ ] FaturaBandırmaService ile entegr. (Rota × Sefer × Fiyat)
  [ ] SGK rapor generation güncelle
  [ ] Integration tests

Hafta 6-7:
  [ ] Production deployment support
  [ ] Monitoring setup
```

### Frontend Lead
```
Hafta 3:
  [ ] PuantajTopluGirisi.razor (Toplu Giriş page)
  [ ] Responsive design, validation

Hafta 3-4:
  [ ] PuantajHaftalikOzet.razor (Haftalık Özet)
  [ ] PuantajGunlukDetay.razor (Günlük Detay)

Hafta 4:
  [ ] NavMenu update
  [ ] E2E manual testing

Hafta 6:
  [ ] UI tweaks (feedback based)
  [ ] Light/dark theme test (opsiyonel)
```

### QA Lead
```
Hafta 5:
  [ ] Test plan oluştur (3 scenario + edge cases)
  [ ] Manual testing (Toplu → Haftalık → Günlük)
  [ ] Bug logging + prioritization

Hafta 5-6:
  [ ] UAT coordination (3 pilot firm)
  [ ] Performance testing (1000+ records)
  [ ] Security checklist

Hafta 6:
  [ ] UAT feedback integration
  [ ] Regression testing
```

### Product Owner
```
Ongoing:
  [ ] Requirement clarification (as needed)
  [ ] Pilot customer recruitment
  [ ] Feedback integration
  [ ] Prioritization

Hafta 6:
  [ ] Training session 1: Operatörlere
  [ ] Training session 2: Muhasebecilere
```

### Support Lead
```
Hafta 6:
  [ ] User Manual (Türkçe) oluştur
  [ ] FAQ oluştur
  [ ] Support runbook
  [ ] Video tutorials (opsiyonel)

Hafta 6.5:
  [ ] Helpdesk team training
  [ ] Support script test
  [ ] Go-live support plan

Hafta 7:
  [ ] 24/7 support standby
  [ ] Daily health check (1 hafta)
```

---

## 🔗 Ek Referanslar

- **Teknik Deep-Dive**: `TURKIYE-SORUNLARI-PUANTAJ-GUZERGAH-INPUT-YONTEMI.md`
- **Detaylı Proje Planı**: `IMPLEMENTATION-PLAN-HIBRIT-PUANTAJ-SISTEM.md`
- **Mevcut Puantaj Analizi**: `OPERASYONEL-PUANTAJ-GUZERGAH-DIMENSION.md`
- **Raporlama Stratejisi**: `RAPORLAMA-STRATEJISI-REVIZE-OZETI.md`
- **Best Practices**: `BEST-PRACTICE-ANALIZI-TEKNIK-DETAYLAR.md`

---

## ✅ Başlama Checklist

```
Pre-Implementation:
  [ ] Tüm belgeleri oku (30 min)
  [ ] Team standup (15 min) - Plan tartışması
  [ ] Database backup al (pre-migration)
  [ ] Dev environment'ı setup et
  [ ] Git branch oluştur: feature/hibrit-puantaj

Sprint 1 Kickoff:
  [ ] Backlog oluştur (Jira/Trello)
  [ ] Task assign et (Backend/Frontend/QA)
  [ ] Daily standup schedule belirle
  [ ] Sprint retrospective date set (Hafta 2 sonu)

Go!
  [ ] Backend Lead: Start Task 1.1.1
  [ ] Frontend Lead: Mockup review, design finalize
  [ ] QA Lead: Test plan draft
```

---

**Hazırladı**: Claude Code Analysis  
**Tarih**: 23 Ocak 2025  
**Versiyon**: 1.0 - Quick Reference  
**Durum**: ✅ Ready to Share with Team

**Yazdırma**: PDF + Print-friendly (A4, color)  
**Paylaşım**: Slack + Email + Team Portal
