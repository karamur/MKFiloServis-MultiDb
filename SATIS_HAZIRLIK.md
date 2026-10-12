# MKFiloServis — Satışa Hazırlık Listesi

**Güncelleme:** 2026-10-12  
**Karar:** 🔴 **NO-GO — v1.0.38 henüz satışa/kuruluma çıkarılmamalı.**  
**Renkler:** 🔴 Satış blokajı · 🟡 Kanıt veya karar bekliyor · 🟢 Bu kapsamda doğrulandı

Bu liste güncel satış kararını ve kapanış koşullarını gösterir. Geçmiş raporlardaki görev renkleri, burada yazılı Go/No-Go kanıtlarının yerine geçmez. Eski müşteri veritabanını yükseltme ve tarihsel kayıt taşıma kullanıcı kararıyla kapsam dışıdır; bu listenin PostgreSQL/SQLite maddeleri yeni ve boş kurulum içindir.

## 🔴 Satış blokajları

| # | Eksik / düzeltilmesi gereken | Kapanış ölçütü |
|---|---|---|
| 1 | **v1.0.38 sabit bir kaynak revizyonuna bağlı değil.** Analiz anında çalışma ağacında 26 değiştirilmiş/kayıtsız yol vardı. | Satılacak kapsamı gözden geçirip bir commit/tag ile sabitle. Build, test, kurulum ve güncelleme paketini aynı revizyondan üret; commit/tag ve paket hash'lerini yayın kaydına yaz. |
| 2 | **Kurulum ve güncelleme EXE'leri imzasız; bu makinede imzalama sertifikası bulunamadı.** Her iki EXE `NotSigned`; CurrentUser/LocalMachine depolarında özel anahtarlı Code Signing sertifikası yok. | Yetkili yayıncı sertifikasını güvenli kanaldan edin/yükle; EXE'leri timestamp ile imzala ve hedef Windows'ta doğrula. İmza doğrulanmadan dağıtma. |

## 🟡 Yayın öncesi kabul / kanıt bekleyenler

| # | Bekleyen doğrulama | Gerekli kayıt |
|---|---|---|
| 4 | **Yeni model/migration için temiz kurulum kabulü.** Mevcut geliştirme PostgreSQL'inde önceden var olan nullable kolon güvenle benimsenip migration kaydedildi; boş PostgreSQL ve SQLite hedeflerinde kurulum provası yapılmadı. | Aday paketle boş PostgreSQL ve SQLite hedefi oluştur; migration dahil başlangıç, giriş, firma oluşturma ve temel okuma-yazma akışlarını çalıştır. Her iki sağlayıcıda şema ve başlangıç loglarını sakla. |
| 5 | **Kimlik ve tenant kabulü.** Normal/Admin, farklı firma, yetki kaldırma, oturum yenileme/sonlanma ve yük senaryoları yerel kaynak kanıtıyla kapanmış sayılmaz. | [Son aşama test planındaki](docs/SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md) senaryoları test hesaplarıyla yürüt; beklenen 401/403, firma sınırı ve oturum iptal sonuçlarını kaydet. |
| 6 | **Yedek ve geri yükleme.** Farklı Windows makine/profilde DB + belgeler + DataProtection key ring geri yükleme/rollback tutanağı yok. | İzole hedefte yedek al, doğrula, geri yükle, uygulamayı aç ve geri dönüşü kanıtla. Üretim verisi yerine maskeli/sentetik fixture kullan. |
| 7 | **Mali işlemler ve gerçek dosya akışları.** Hedef PostgreSQL işlem/commit belirsizliği, Excel import, PDF/SMTP, yerel/S3 depolama ve kısmi hata/tekrar senaryoları saha ortamında kapanmadı. | Kullanılacak sağlayıcı ve özellikler için hata enjeksiyonu/tekrar/rollback sonuçlarını kaydet. Satış kapsamına girmeyen entegrasyonları kapalı ve kullanıcıya görünür biçimde bırak. |
| 8 | **Kurulum, kapasite ve dış servis kabulü.** Temiz Windows kurulum/güncelleme; hedef eşzamanlılık/yük; Redis; çıktı görsel kontrolü; Luca/UBL sandbox senaryolarının tamamlandığına dair tek Go/No-Go tutanağı yok. | Gerçek satış konfigürasyonunda uygulanacak senaryoları seç, yürüt ve kabul sahibi/tarih/sonuç/paket hash'i ile kaydet. Kullanılmayacak servisleri açıkça kapsam dışı kararına bağla. |
| 9 | **Satış raporlarında görev sayımı tutarsız.** Belgelerde 31 yeşil, 30 yeşil + 1 kırmızı ve 24 yeşil + 7 sarı sayımları geçiyor; A-18'in rengi ve kapsamı farklı yerlerde çelişiyor. | Ana durum belgesi, görev envanteri ve test planında tek güncel karar/sayım yayımla. Tarihsel günlükleri koru, yalnız güncel özetleri eşitle. |
| 10 | **Son doğrulama pakete bağlı değil.** Bu analiz sırasında test paketi/build yeniden çalıştırılmadı; belgelerdeki en son test sayıları 9 Ekim tarihli. | Sabitlenmiş aday revizyonda Release build ve otomatik testleri tekrar çalıştır. PostgreSQL'e özgü testleri izole sunucuda da yürüt; raporu aynı commit ve hash'lere bağla. |

## 🟢 Tamamlanmış sınırlı teslimler

- EF snapshot'taki bozuk kodlanmış araç alanı düzeltildi; var olmayan kolon adına `RenameColumn` üreten taslak kaldırıldı. `KilitlenmeBitisUtc` alanı için sağlayıcı türü PostgreSQL/SQLite olarak seçilen veri koruyucu migration eklendi.
- `PendingModelChangesWarning` artık özel olarak log seviyesine düşürülmüyor. `dotnet ef migrations has-pending-model-changes --context ApplicationDbContext` “No changes have been made to the model since the last migration” döndü; Web Release derlemesi 0 uyarı / 0 hata.
- Yerel geliştirme PostgreSQL'inde `KilitlenmeBitisUtc` kolonu önceki initializer tarafından migration geçmişi dışında oluşturulmuştu. Yeni `ADD COLUMN IF NOT EXISTS` migration'ı mevcut kolonu benimseyip migration history'ye kaydetti; `FinalSchemaParity` tamamlandı.
- Güncel kodla v1.0.38 kurulum ve güncelleme EXE'leri yeniden üretildi; yayın SHA-256 listesi `setup/output/v1.0.38/SHA256SUMS.txt` içinde.
- Payload kontrolünde yerel `setup/output` derleme klasörünün pakete eklenmediği görüldü.
- Yerel uygulama PostgreSQL yapılandırmasıyla `http://localhost:5190` adresinde çalışıyor. Bu mevcut geliştirme DB'si başlangıcıdır; boş PostgreSQL/SQLite temiz kurulum veya Production kabulü değildir.

## Kapanış sırası

1. Yeni migration'ı PostgreSQL ve SQLite temiz hedeflerinde uygula; açılış ve temel akışları kanıtla.
2. Release kaynak revizyonunu sabitle; o revizyondan build/test ve güncel EXE üret.
3. Yetkili imzalama sertifikasını edin; EXE'leri imzala ve doğrula. Hash manifestini sabit sürüm çıktısı için güncelle.
4. Uygulanabilir saha senaryolarını yürüt; açık maddeler için kabul sahibi/tarih belirle.
5. Rapor renklerini eşitle ve kanıtlı Go/No-Go kararı yayımla.

## Kanıt kaynakları

- [2026-10-12 satışa hazırlık analizi](docs/SATISA-HAZIRLIK-ANALIZI-2026-10-12.md)
- [Satışa çıkarım son durum](docs/SATISA-CIKARIM-SON-DURUM-2026-10-05.md)
- [Satışa çıkarım görev envanteri](docs/SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md)
- [Test doğrulama kaydı](docs/TEST-DOGRULAMA-2026-10-08.md)
- [Son aşama test planı](docs/SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md)
