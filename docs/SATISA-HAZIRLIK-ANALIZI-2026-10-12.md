# MKFiloServis — Satışa Hazırlık Analizi

**Tarih:** 2026-10-12 (Europe/Istanbul)  
**Karar:** **NO-GO — v1.0.38 şu an müşteriye satış/kurulum için onaylanamaz.**  
**Kapsam:** Yerel kaynak ağacı, mevcut satış belgeleri, v1.0.38 kurulum çıktıları ve 2026-10-12 yerel uygulama başlangıç kaydı. Gerçek müşteri veritabanı, saha kurulumu veya dış servis hesabı kullanılmadı. Model parity bulgusu aynı gün düzeltildi ve aşağıdaki takip kaydıyla güncellendi.

## Yönetici özeti

v1.0.38 kurulum ve güncelleme EXE'leri son parity düzeltmesinden sonra yeniden üretildi; payload'da yerel `setup/output` derleme klasörü yok ve `SHA256SUMS.txt` oluşturuldu. EXE'ler imzasız; kullanıcı/makine sertifika depolarında özel anahtarlı Code Signing sertifikası bulunamadı. İlk başlangıçta görülen EF Core `PendingModelChangesWarning` kök nedeni snapshot'taki yanlış kodlanmış araç kolon adı ve kullanıcı kilit bitiş alanı migration'ının eksik olmasıydı. Snapshot düzeltildi, PostgreSQL'de `ADD COLUMN IF NOT EXISTS` kullanan ve SQLite'ta `TEXT` nullable alan oluşturan migration eklendi; uyarı log seviyesine bastırılmıyor. Web Release 0/0 ve EF `has-pending-model-changes` temiz. Mevcut geliştirme PostgreSQL'inde daha önce migration dışı oluşmuş kolon güvenle benimsendi, migration history'ye işlendi ve startup şema parity kontrolü tamamlandı. Boş PG/SQLite temiz kurulum kabulü yine yapılmadı.

Migration/model parity kod düzeltmesi tamamlandı; şimdi aynı sabit kaynak revizyonundan üretilen imzalı paket temiz PostgreSQL ve SQLite hedeflerinde kabul edilmelidir. Ardından belgelenmiş kimlik, kurtarma, mali akış, dosya, yük ve entegrasyon senaryoları tamamlanıp Go/No-Go kaydı oluşturulmalıdır. Kullanıcı kararıyla eski müşteri veritabanlarını yükseltme ve tarihsel kayıt taşıma bu satış kapsamına dahil değildir; aşağıdaki temiz kurulum kapısı bu kararla çelişmez.

## Öncelikli bulgular

| Öncelik | Bulgu ve kanıt | Satış etkisi | Gerekli kapanış kanıtı |
|---|---|---|---|
| **P0** | **v1.0.38 tekrarlanabilir, sabitlenmiş sürüm olarak kanıtlanmamış.** `git status` güncel analizde **26 değiştirilmiş/kayıtsız yol** gösterdi; son commit `1c5e8290` (2026-10-10). | EXE içeriği bir commit/tag ile birebir eşlenemiyor; sonraki kaynak değişikliğiyle aynı sürüm numarasında farklı paket çıkabilir. | Kapsama alınmış değişiklikleri inceleyip commit/tag veya eşdeğer release revizyonunu sabitle; paketleri o revizyondan yeniden üret; SHA-256 değerlerini yayın kaydına koy. |
| **P0** | **v1.0.38 henüz imzalı bir yayın değil.** Güncel EXE'ler Authenticode imzasız; CurrentUser/LocalMachine certificate store'larında özel anahtarlı Code Signing sertifikası bulunamadı. | Alıcı yayıncı kimliğini doğrulayamaz; SmartScreen/kurum ilkesi kurulumu engelleyebilir. | Yetkili yayıncı sertifikasını edin, timestamp'le imzala ve imzalı dosyaları temiz makinede doğrula. |
| **P1** | **EF model/migration parity düzeltildi; temiz sağlayıcı kabulü açık.** Snapshot kodlaması düzeltildi; lockout expiry migration eklendi. PostgreSQL'de önceden var olan kolon migration tarafından benimsenip history'ye kaydedildi; startup final parity geçti. Web Release 0/0 ve `has-pending-model-changes` temiz. | Mevcut geliştirme DB'si yeni boş kurulum değildir; SQLite uygulaması da henüz çalıştırılmadı. | Paketle boş PostgreSQL ve SQLite hedeflerinde migration ve startup kabulini tamamla; temel akışları ve şema parity'sini kaydet. |
| **P1** | **Saha kabulleri henüz kanıtlanmış değil.** Son aşama test planı ve görev envanteri; normal/Admin yetki-firma matrisi, bağımsız DB+belge+key ring restore/rollback, mali transaction/commit belirsizliği, gerçek dosya ve depolama, temiz Windows kurulumu, eşzamanlılık/yük, çıktı görünümü ve dış servis testlerini Go/No-Go kapıları olarak listeliyor. Belgelerde bunların tamamlandığını gösteren imzalı kabul kaydı yok. | Kaynak/izole SQLite testleri gerçek dağıtım ortamı, hedef DB davranışı veya müşteri iş akışı kabulinin yerine geçmiyor. | [Son aşama test planındaki](SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md) her zorunlu senaryo için sonuç, paket hash'i, ortam/sağlayıcı ve kabul sahibi kaydedilmeli. Harici servisi kullanmayacak ürün kapsamları açıkça kapatılmalı. |
| **P2** | **Yerel doğrulama kanıtları tarihli; bu analizde test paketi yeniden çalıştırılmadı.** Belgelerde son tam paket 2026-10-09 için 139 geçti / 2 PostgreSQL-özel test atlandı; Release derlemesi 0/0 olarak kayıtlı. | Bu sonuçlar 2026-10-12 çalışma ağacını veya v1.0.38 EXE'lerinin kurulu makinede açılışını kanıtlamıyor. | Release adayı sabitlendikten sonra test ve build'i yeniden çalıştır; atlanan PostgreSQL senaryolarını izole PostgreSQL ile çalıştır; sonuçları paket revizyonuna bağla. |

## Satış öncesi sırayla yapılacaklar

1. Yeni lockout expiry migration'ını boş PostgreSQL ve SQLite veritabanlarında uygula; temel başlangıç akışlarını ve şema parity'sini kaydet.
2. Release kaynaklarını commit/tag ile sabitle; çözüm build/test kanıtı ile v1.0.38 paketlerini aynı revizyonda yeniden oluştur.
3. Yetkili Code Signing sertifikasını edin; kurulum ve güncelleme EXE'lerini imzala ve hash manifestini sabit yayın paketine göre güncelle.
5. [Son aşama test planındaki](SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md) gerçek kabul senaryolarını çalıştır; müşteri ortamı gerektirenleri sorumlu ve tarih ile takip et.
6. Güncel özet renkleri 30 yeşil / 0 sarı / 1 kırmızı / 0 beyaz olarak eşitlendi; tarihli tarihsel kayıtlar korunuyor. Açık saha kapıları dururken genel “satışa hazır”/Go kararı verme.

## Kapsam ve kanıt sınırları

- Güncel kodla yeniden üretilen v1.0.38 EXE'leri mevcut; derleme çıktısında boyutlar 234,35 MB ve 122,94 MB olarak raporlandı. SHA-256 manifesti `setup/output/v1.0.38/SHA256SUMS.txt` konumunda.
- Payload içinde `setup/output` klasörü bulunmuyor; yerel çıktı klasörünün payload'a kopyalanmadığı doğrulandı.
- Yerel uygulama PostgreSQL sağlayıcısıyla başladı, lockout expiry migration'ını uygulayıp final schema parity'yi geçti ve `http://localhost:5190` adresinde dinliyor. Bu mevcut geliştirme DB'si başlangıcı müşteri/Production veya boş kurulum kabulü değildir.
- Parity düzeltmesi için Web Release build (0/0), EF `has-pending-model-changes`, mevcut geliştirme PostgreSQL migration'ı ve güncel v1.0.38 installer derlemesi çalıştırıldı; otomatik test paketi ve boş PostgreSQL/SQLite hedef kabulü çalıştırılmadı.
- Bu analiz yeni satış için temiz kurulum kapsamını değerlendirir; eski müşteri DB yükseltme ve tarihsel veri taşıma işini tekrar açmaz.

## İlgili kaynaklar

- [Satışa çıkarım son durum](SATISA-CIKARIM-SON-DURUM-2026-10-05.md)
- [Satışa çıkarım görev envanteri](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md)
- [Test doğrulama kaydı](TEST-DOGRULAMA-2026-10-08.md)
- [Son aşama test planı](SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md)
