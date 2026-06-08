# KOAFiloServis PreProd Test Planı
## Tarih: 2026-06-09 | Ortam: KOAFiloServis_PreProd

---

## AŞAMA 2: Dashboard Testleri (7 dashboard × 3 firma)

### Test Verisi
| Tablo | Kayıt | Dashboard |
|-------|-------|-----------|
| BudgetOdemeler | 389 | BudgetAnaliz |
| MuhasebeFisleri | 499 | MuhasebeDashboard |
| Faturalar | 375 | NakitAkisDashboard |
| BankaKasaHareketleri | 65 | NakitAkisDashboard |
| StokKartlari | 121 | StokDashboard |
| Araclar | 18 | FiloKpiDashboard |
| Personeller | 32 | Home Dashboard |
| Cariler | 178 | Cari listeleri |

### Test Adımları

#### D1 - Budget Dashboard (BudgetAnaliz)
- [ ] FirmaId=1 (Üstün Grup) ile aç → ödemeler geliyor
- [ ] FirmaId=2 (Üstün Filo) ile aç → ödemeler geliyor
- [ ] FirmaId=3 (Recep Üstün) ile aç → ödemeler geliyor
- [ ] Aylık/hediyeli özet sekmesi
- [ ] Takvim görünümü
- [ ] Kategori özeti
- [ ] Yükleme süresi < 3 saniye

#### D2 - Holding Dashboard
- [ ] "Tüm Firmalar" modunda aç
- [ ] Toplam ciro / gider görünüyor
- [ ] Firma bazlı karlılık
- [ ] Yükleme süresi < 3 saniye

#### D3 - Muhasebe Dashboard
- [ ] FirmaId=1 → fiş listesi
- [ ] FirmaId=2 → fiş listesi
- [ ] FirmaId=3 → fiş listesi
- [ ] Hesap planı görünüyor
- [ ] Borç/alacak özeti

#### D4 - Filo Operasyon Dashboard
- [ ] Araç listesi (18 araç)
- [ ] Operasyon kayıtları
- [ ] Firma bazlı filtreleme

#### D5 - Filo KPI Dashboard
- [ ] Hakediş durumları
- [ ] Araç performansı
- [ ] Sofor performansı
- [ ] Yükleme süresi < 5 saniye (6 servis bağımlılığı)

#### D6 - Nakit Akış Dashboard
- [ ] Banka hesapları
- [ ] Yaklaşan ödemeler
- [ ] Gelir/gider grafiği

#### D7 - Stok Dashboard
- [ ] 121 stok kartı listeleniyor
- [ ] Kategori filtresi
- [ ] Yükleme süresi (37 Include uyarısı — izle)

### Hata Kaydı
| Dashboard | Firma | Hata | Süre |
|-----------|-------|------|------|
| | | | |

---

## AŞAMA 3: Firma İzolasyon Testi

### Test Kullanıcıları
| Kullanıcı | Rol | Yetki |
|-----------|-----|-------|
| Admin | Holding Yöneticisi | Tüm firmalar |
| FirmaYonetici | Firma Yöneticisi (FirmaId=1) | Sadece Üstün Grup |
| Personel | Sofor/Personel | Yetkili kayıtlar |

### Test Adımları

#### I1 - Holding Yöneticisi
- [ ] Giriş yap
- [ ] Tüm firmaları görebiliyor
- [ ] Firma değiştirince veri değişiyor
- [ ] "Tüm Firmalar" modu çalışıyor
- [ ] Tarayıcı kapat/aç → firma seçimi korunuyor

#### I2 - Firma Yöneticisi
- [ ] Giriş yap, FirmaId=1 seç
- [ ] Sadece FirmaId=1 verileri görünüyor
- [ ] FirmaId=2'ye geçmeyi dene → engellenmeli veya boş
- [ ] URL manipülasyonu → yetkisiz erişim engelleniyor

#### I3 - Personel
- [ ] Giriş yap
- [ ] Sadece yetkili olduğu kayıtları görebiliyor
- [ ] Firma değiştiremiyor
- [ ] Dashboard sınırlı görünüm

#### I4 - Sınır Testleri
- [ ] `/api/dosya/download?path=...` → yetkisiz → 401
- [ ] `/api/health` → herkese açık → 200
- [ ] Doğrudan `/uploads/...` URL → 404

---

## AŞAMA 4: Backup + Restore Testi

### Backup Testi
- [ ] `pg_dump KOAFiloServis_PreProd > preprod_backup_$(date +%Y%m%d).sql`
- [ ] Dosya boyutu kontrolü (> 0)
- [ ] Backup süresi ölçümü

### Restore Testi
- [ ] `createdb KOAFiloServis_RestoreTest`
- [ ] `psql KOAFiloServis_RestoreTest < preprod_backup.sql`
- [ ] Tablo sayısı karşılaştır: PreProd vs RestoreTest
- [ ] Kayıt sayısı karşılaştır
- [ ] Firma listesi karşılaştır
- [ ] `dropdb KOAFiloServis_RestoreTest`

---

## AŞAMA 5: UAT (Kullanıcı Kabul Testi)

### İş Akışları
- [ ] Firma seç → Dashboard'u gör
- [ ] Cari kart oluştur → FirmaId otomatik atandı mı?
- [ ] Fatura oluştur → FirmaId doğru mu?
- [ ] Tahsilat/ödeme yap → BankaHareket oluştu mu?
- [ ] Personel ekle → FirmaId atandı mı?
- [ ] Stok kartı ekle → FirmaId atandı mı?
- [ ] Dosya yükle → şifreli kaydedildi mi?
- [ ] Dosya indir → auth kontrol edildi mi?

### Rapor Doğrulama
- [ ] FirmaId=1 verileri sadece FirmaId=1'de görünüyor
- [ ] Holding dashboard tüm firmaları konsolide ediyor
- [ ] Bütçe raporu firma bazlı doğru

---

## AŞAMA 6: Canlı Yayın

### Go/No-Go Kriterleri
- [ ] Tüm dashboard testleri BAŞARILI
- [ ] Firma izolasyon testi BAŞARILI
- [ ] Backup/restore BAŞARILI
- [ ] UAT onayı alındı
- [ ] Build 0 hata, 0 uyarı

### Canlıya Çıkış Adımları
1. Production connection string güncelle
2. JWT secret production değeri ata
3. `appsettings.Production.json` → `DetailedErrors: false`
4. `appsettings.Production.json` → `LogLevel: Information`
5. `VACUUM ANALYZE` production DB'de
6. Son backup al
7. Deploy
8. Health check: `/api/health`
9. Dashboard kontrolü
10. Kullanıcı girişi testi
