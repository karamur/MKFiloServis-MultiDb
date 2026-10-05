# MKFiloServis — Satışa Çıkarım Son Durum ve Açık Görevler

**Tarih:** 2026-10-05  
**Esas:** Yerel çalışma ağacı; müşteri dağıtımı veya teslim commit’i olarak değerlendirilmez.  
**Kaynak:** Güncel durum raporunun tamamı, ikinci denetim raporu ve sonraki 2026-10-05 düzeltme ekleri. Son ekran/servis değişiklikleri hedefli kaynak aramasıyla karşılaştırıldı.  
**Bu tur:** Belge incelemesi ve durum birleştirmesi yapıldı; yeni test, derleme, bağımlılık taraması, müşteri lisansı üretimi veya gerçek restore yapılmadı.

## 1. Satışa hazırlık kararı

**🟡 Satış kabulü henüz tamamlanmadı.** Kaynak düzeltmeleri ilerledi; bağımsız kurtarma, müşteri lisans geçişi, güvenlik/tenant kabulü, kurulum ve veri bütünlüğü kanıtları açık. Derleme başarısı bu kabullerin yerine geçmez.

Bu belge tarihsel tekrarları tek görev listesine toplar. Eski raporlardaki “kalan” ifadesi sonradan kod olarak tamamlanmışsa tekrar açık kod işi yapılmaz; yalnız gerçek kalan kapsam listelenir. Başlamış servis yazımları modal kapatılmasıyla geri alınmış sayılmaz.

### Renklerin anlamı

| Renk | Durum |
|---|---|
| 🟢 | Kaynak düzeltmesi veya belirtilen sınırlı kanıt mevcut; tüm ürün kabulü anlamına gelmez |
| 🟡 | Kod kısmen/tamamen mevcut; kabul, geçiş veya kapsam denetimi bekliyor |
| 🔴 | Raporda açık kalan uygulama/kalıcı altyapı/doküman işi |
| ⚪ | Ürün kapsamı veya düşük öncelikli düzenleme kararı |

**Açık görev özeti:** 31 birleşik görev: **8 🔴 açık uygulama/doküman**, **19 🟡 kabul/geçiş/denetim**, **4 ⚪ karar/düzenleme**. Bunlar eski bulgu sayısı değildir; aynı alanın kod ve kabul işleri ayrı görev olabilir. P0 satış öncesi kritik, P1 satış öncesi yüksek, P2 kapsamına göre sürüm planı, P3 takip önceliğidir.

## 2. Tamamlanan kısımlar

| Alan | Güncel tamamlanmış kısım | Açık görev |
|---|---|---|
| 🟢 Lisans/anahtar | LisansDesktop içinde DPAPI depo, parola korumalı yedek/doğrulama/içe alma; Web açık anahtarla doğrular; harici lisanslama akışı yok | A-01 |
| 🟢 Modül ve sürüm hakları | 15 modül seçimi, imzalı v3 haklar, modül politikaları; ortak sürüm/kimlik/imza verisi, tam makine eşleşmesi | A-01, A-02, A-30 |
| 🟢 Kimlik doğrulama | Global sayfa/API/hub koruması, framework anonim metadata, parola/kilit/hash yükseltmesi, kaynak sabit sırlarının temizliği | A-05, A-06 |
| 🟢 Kurtarma hazırlığı | Manifest/hash/yol kontrolü, izole staging ve key ring probu; DB restore koruması; izole N-1/N-2 kanıtı | A-03, A-04 |
| 🟢 Audit ve mali kayıt | Audit PK/rollback/retry düzeltmeleri; hızlı muhasebe transaction’ı; belirli toplu yazımlar tracking’e taşındı | A-08, A-09 |
| 🟢 Firma ilişkileri | Filo, banka şablonu, maaş snapshot ve fatura/grup şablonlarında kaynak kapsam kontrolleri | A-15, A-16, A-17 |
| 🟢 Banka dosya okuyucusu | CSV/Excel ayrıştırma, hatalı satır ve borç/alacak belirsizliği kontrolü | A-15, A-17, A-29 |
| 🟢 Maaş ekranları | Görünür snapshot hatası, liste/detay seçim ve sürüm koruması | A-17 |
| 🟢 Fatura sunumu | Firma/şablon/kullanıcı kapsamı, ortak şablon yetkisi ve API hata yanıtları; PDF/önizleme/e-posta kontrolü | A-15, A-17 |
| 🟢 Dosya yaşam döngüsü | Özlük hata bildirimi; araç/tedarikçi evrakında DB önce, fiziksel silme sonra; kısmi temizlik hatası görünür | A-10, A-11 |
| 🟢 Araç belge/plaka | Tarih senkronizasyonu ve plaka/aktif plaka ortak SaveChanges; firma/cari doğrulaması; commit sonrası cache temizliği | A-14, A-15, A-20 |
| 🟢 Cache ve araç listesi | Süreç içi eski factory yayınlama koruması; yakalanmış firma/cache anahtarı; liste yükleme sürümü ve kalıcı hata bildirimi | A-24, A-25 |
| 🟢 Plaka ve silme modalları | Bekleyen sonuçlara modal/firma sürümü kontrolü, çift işlem koruması; araç silmede tracking ve açık firma koşulu | A-12, A-13, A-14 |
| 🟢 Personel banka raporu | Sayfaya özel A4 yatay print stili; menü gizleme, 15 sütun genişliği, tekrarlanan başlık ve son toplam | A-26 |
| 🟢 İhale Excel/PDF | Projeksiyon, gerçekleşen analiz ve operasyon özeti; XLSX sayfaları/sayısal hücreler ve gerçek PDF dosyası üretimi | A-26 |
| 🟢 Başlangıç/DataSync/paket/CI | Başlangıç sınıflandırması, aktarım bütünlük kontrolleri, üreticiyi müşteri paketinden ayırma ve CI tetikleme düzeltmeleri | A-07, A-18, A-19, A-21 |
| 🟢 Paket güvenliği/HTTP | Kayıtlı SQLite/XML sürüm düzeltmeleri; taze isteklerle sınırlı retry, Selenium await düzeltmesi | A-22, A-27 |

## 3. Kalan açık görevler

Her satırın son sütunu yeşile geçiş ölçütüdür. Yalnız kodun derlenmesi kabul görevini kapatmaz.

| Görev | Öncelik | Durum | Eski bulgu/alan | Yapılacak iş | Kapanış ölçütü |
|---|---|---|---|---|---|
| A-01 | P0 | 🟡 | K-1 / N-1 / O-14 | **Lisans anahtarı ve müşteri geçişi:** Yetkili müşteri/lisans envanterini belirle; LisansDesktop üzerinden parola korumalı .mkkey oluştur, doğrula ve bağımsız profilde geri yükle. Müşteri modülleri/sürüm haklarıyla v3 yeniden basım ve teslim kabulü yap. | Program içi işlem, Web açık anahtar eşleşmesi ve bağımsız geri yükleme kanıtı; müşteri hakları ve teslim kaydı. |
| A-02 | P0 | 🟡 | K-1 / K-2 / modüller | **Modül lisansı erişim kabulü:** Lisanslı/lisanssız modülleri normal/Admin kullanıcıyla sayfa, API, dosya, hub, menü ve açık sayfada lisans değişimi için kontrol et. | Lisans dışı modül erişimi engellenir; Admin lisans hakkını aşamaz. |
| A-03 | P0 | 🔴 | N-2 / O-6 / D-5 | **Tam kurtarma uygulaması:** Mevcut staging hazırlığından ayrı olarak DB, dosya, ayar ve key ring canlı uygulama sırasını ve geri dönüşünü tamamla; DB-only restore tam kurtarma olarak sunulmamalı. | Uygulanabilir tam kurtarma akışı ve başarısız aşamada geri dönüş yöntemi. |
| A-04 | P0 | 🟡 | N-2 / Y-3 / D-5 | **Bağımsız gerçek kurtarma kabulü:** İzole kopyada gerçek DB dump, şifreli belge ve credential ile farklı makine/profil kurtarmasını doğrula; DPAPI/sertifika ve S3 kapsamını dahil et. | Belgeler ve credential çözülebilir; DB-dosya ilişkileri tutarlı. Sentetik dump probu yeterli kanıt değildir. |
| A-05 | P0 | 🟡 | K-2 / K-3 / K-5 / K-6 / R-4..R-6 | **Giriş, oturum ve tenant kabulü:** Anonim/normal/Admin, firma A/B, Bearer/circuit, çıkış ve oturum iptali, bootstrap kapanışı, eski hash yükseltme, kilit ve zaman aşımı senaryolarını çalıştır. | Beklenen 401/403 ve firma izolasyonu; hesap kilidi/çözülmesi, token ve circuit davranışı kanıtlanır. |
| A-06 | P0 | 🟡 | K-4 / O-12 / R-7 | **İfşa olmuş sırların kapanışı:** Aktif kimlik bilgilerinin ifşa durumunu belirle; gerekli rotasyonu yap ve repo/geçmiş denetimini kaydet. Yeni Production sırlarını güvenli yapılandır. | Aktif eski sır kullanılmaz; rotasyon ve geçmiş temizliği kararı belgeli. Rapordaki eski temiz kaynak tespiti rotasyon kanıtı değildir. |
| A-07 | P1 | 🔴 | Y-7 / N-3 | **Kalıcı test projesi ve CI kapsamı:** Kritik lisans, tenant, audit, restore ve mali işlem testlerini kalıcı projeye taşı; CI’de gerçekten çalıştır. | Kritik test projesi/test sonuçları ve GitHub workflow çalışması mevcut. Tetikleme ve derleme başarısı tek başına yeterli değildir. |
| A-08 | P1 | 🔴 | O-3 | **Kalan doğrudan SQL/audit yazımları:** Repo genelindeki ExecuteUpdate/Raw SQL yazımlarını envanterle; iş değişikliği ve audit için kayıt/transaction garantisini tamamla. | Her kritik yazımın audit ve rollback kapsamı belgeli; açık atlayan yol kalmaz. |
| A-09 | P1 | 🟡 | O-3 / Y-1 | **Gerçek DB audit ve mali bütünlük kabulü:** Tam model PostgreSQL/SQL Server üzerinde retry, commit hatası, savepoint ve rollback kabulü yap; cari/hesap ön hazırlığının ayrı transaction etkisini değerlendir. | İş/audit/fiş/banka/stok değişiklikleri beklenen sınırda birlikte geri alınır; minimal SQLite kanıtının kapsamı aşılmaz. |
| A-10 | P1 | 🔴 | Dosya yaşam döngüsü | **Kalıcı dosya temizleme ve upload telafisi:** Başarısız fiziksel silme için kalıcı kuyruk/yeniden deneme ekle; mevcut yetim dosyaları güvenli biçimde envanterle; belirsiz upload commit telafisini ve diğer servislerin DB/dosya sırasını tamamla. | DB’de başvurulan dosya yanlışlıkla silinmez; bekleyen temizlik izlenir ve yeniden denenir. |
| A-11 | P1 | 🟡 | Özlük / tedarikçi / araç evrak | **Dosya silme kabulü:** DB/audit rollback, NoTracking, çok dosyalı kısmi hata, disk kilidi/izin, iptal ve S3 404/403/5xx ile ekran yenilemesini kontrol et. | DB kaydı ve dosya sonucu ayrı doğru bildirilir; hata sonraki dosyaların temizliğini durdurmaz. |
| A-12 | P1 | 🔴 | Araç Excel aktarımı | **Import sonuçlarının seçim kontrolü:** Dosya okuma, modal/firma değişimi ve servis sonucu için yakalanmış seçim/sürüm korumasını tamamla; çift başlatma ve yeni modalın eski sonuçla değişmesini engelle. | Eski import yeni modalı/listeyi etkilemez; kısmi commit sonuçları doğru gösterilir. Plaka ve silme modalı düzeltmesi importu kapsamaz. |
| A-13 | P1 | 🟡 | Araç backfill / firma değiştirme | **Firma atama yetki ve kapsam denetimi:** IgnoreQueryFilters kullanan firmasız araç ataması ve genel araç firma değiştirme yollarını yetki/hedef firma/ilişkili kayıt bakımından incele; bulunan eksikleri düzelt. | Yetkisiz veya farklı firma ilişkilerini bozan atama engellenir; kabul kanıtı mevcut. Tüm bu yollar düzeltilmiş sayılmıyor. |
| A-14 | P1 | 🟡 | Araç liste / plaka / silme / belge | **Araç ekranı ve servis kabulü:** A→B→A, cache hit/miss, modal değişimi, çift tıklama, hata/Dispose, plaka ekleme/silme/kapatma ve araç soft delete ilişkilerini kontrol et. | Eski sonuç güncel ekranı değiştirmez; gerçek kayıt/audit rollback, tek firma reddi ve tarih senkronizasyonu kanıtlı. |
| A-15 | P1 | 🔴 | Banka / plaka / snapshot / şablon | **Eşzamanlı DB tekillik ve ilişki kısıtları:** Banka referansı, aktif plaka, dönem snapshot ve varsayılan fatura/grup şablonu için eşzamanlı yazım politikasını ve DB kısıtlarını tamamla; firma ilişkilerini güçlendir. | İki eşzamanlı context aynı tekil kaydı oluşturamaz veya kuralı bozamaz. Servis ön sorgusu tekillik garantisi değildir. |
| A-16 | P1 | 🟡 | Eski veri / tenant ilişkileri | **Mevcut kayıt tutarlılığı:** Filo, snapshot, fatura, cari/plaka ilişkileri, çoklu varsayılanlar ve eksik dönem kayıtlarını tarayıp onarım planı hazırla. | Eski bozuk ilişkiler listelenir; kontrollü onarım ve öncesi/sonrası tutarlılık kanıtı mevcut. |
| A-17 | P1 | 🟡 | Banka import / maaş / fatura | **Mali ekran ve API kabulü:** Gerçek CSV/XLSX banka örnekleri; maaş dönem/personel hızlı geçişleri; fatura API 400/403/404, rol değişimi, firma kurulumu ve iptal; PDF/önizleme/SMTP akışlarını kontrol et. | Satır yönü/sayı/tarih ve kullanıcı/firma kapsamı doğru; başarısız işlemler görünür; gerçek gönderim ve rollback sınırları belgeli. |
| A-18 | P1 | 🟡 | Y-6 / O-4 / R-1 | **Şema ve başlangıç kabulü:** Temiz ve eski kurulumda model/migration farkını, zorunlu başlangıç görevlerini ve kurtarılamayan hata davranışını doğrula. | PendingModelChangesWarning nedeni giderilmiş veya açıklanmış; yarım kurulum başarı gibi açılmaz. |
| A-19 | P1 | 🟡 | O-7 | **DataSync iki yönlü kabulü:** SQLite/PostgreSQL izole kopyalarında iki yönlü aktarım, eksik kolon, satır sayısı, FK/sequence ve hata rollback provasını yap. | Hata tüm aktarımı geri alır; satır/FK/sequence tutarlılığı korunur. |
| A-20 | P1 | 🟡 | Y-10 / plaka tarihi | **Tarih semantiği ve geçiş:** Legacy timestamp için sütunların gerçek UTC/yerel saat anlamını belirle; gün sınırı ve saat içeren plaka tarihlerinin politikasını yaz ve veri geçişini planla. | Saat kayması yaratmayan doğrulanmış dönüşüm/geri dönüş planı. Legacy anahtar yalnız uyarıyı susturmak için kapatılmaz. |
| A-21 | P1 | 🟡 | R-3 / paketleme | **Müşteri kurulum ve güncelleme kabulü:** Temiz makinede müşteri paketi kurulumu/güncellemesi yap; lisans üreticinin pakete girmediğini, lisans sürüm hakkını ve güncel çıktıların kullanıldığını kontrol et. | Gerçek müşteri paketi içerik/kullanım kanıtı; dahili LisansDesktop publish tek başına yeterli değildir. |
| A-22 | P1 | 🟡 | Y-11 | **Güncel bağımlılık taraması:** Son çalışma ağacı/publish için tam doğrudan ve transitif zafiyet taramasını yeniden kaydet. | Güncel tarama sonucu ve gerekli düzeltmeler mevcut. Önceki SQLite/XML düzeltmeleri korunur; bu belge yeni tarama yapmaz. |
| A-23 | P1 | 🔴 | O-13 / R-8 / D-7 / D-8 | **Doküman ve teslim temizliği:** Eksik yeniden analiz bağlantısını düzelt veya dosyayı geri getir; silinmiş raporlar ve Rent-a-Car belgesi için gerekçeyi belirle; yerel çıktı/ham logların teslim durumunu kararlaştır. | Geçerli doküman bağlantıları ve gerekçeli silme listesi; gözden geçirilmiş teslim commit’i. Bu turda dosya geri getirme/silme/commit yapılmadı. |
| A-24 | P2 | 🔴 | Cache / Redis | **Süreçler arası önbellek tazeliği:** Çok süreçli Redis anahtar takibi/sürümünü ve backend arızasında yeniden denemeyi tamamla; diğer servislerden araç yazımlarını kapsa. | Bir süreçteki değişiklik diğer sürecin eski factory sonucunu yayınlamasını engeller; arıza davranışı izlenir. |
| A-25 | P2 | 🟡 | Cache / ekran yüklemesi | **Önbellek yarış ve yük kabulü:** Gerçek backend üzerinde factory/set/remove yarışı, iptal, kesinti ve ortak kilit gecikmesini kontrol et; diğer ekran/provider doğrudan mutasyonlarını değerlendir. | Süreç içi koruma doğrulanır; performans ve stale backend sınırı belgeli. |
| A-26 | P2 | 🟡 | Personel banka / ihale / Y-2 | **Excel ve PDF çıktı kabulü:** Uzun metin, büyük/negatif tutar, çok sayfa, SGK ayrı/birleşik, boş risk listesi ve ekran/çıktı toplam eşitliğini kontrol et; proforma görsel kabulünü dahil et. | XLSX/PDF açılır ve baskıda kesilmez; Türkçe karakterler, başlık/filtre/toplamlar doğru. Çıktı kodu tamamlandı; görsel kabul bekliyor. |
| A-27 | P2 | 🟡 | Y-2 / Y-3 / Y-9 | **Luca ve dış entegrasyon kabulü:** Gerçek UBL/portal, eski credential geçişi, HTTPS/retry ve belirsiz POST hatalarını kabul et; diğer entegrasyonların kapsamını incele. | Tekrar deneme istekleri taze; belirsiz mali POST çift gönderilmez; eski sır dosyası geçişi ve portal uyumu kanıtlı. |
| A-28 | P2 | ⚪ | O-1 | **DB sağlayıcı ürün kapsamı:** SQL Server/MySQL otomatik şema yükseltmesini geliştirme veya destek kapsamını PostgreSQL/SQLite ile sınırlama kararını yaz. | Vaat edilen sağlayıcı için temiz kurulum/yükseltme kabulü mevcut; desteklenmeyen otomatik migration vaat edilmez. |
| A-29 | P2 | ⚪ | Banka / maaş | **Mali ürün politikaları:** Referanssız banka hareketi, imzalı tutardan yön türetme, muhasebeleştirilmiş snapshot silme ve kontrol sonrası rol değişimi politikasını kararlaştır. | Kararlar belgeli, gerekli kod/DB kontrolleri ve kabul senaryoları tanımlı. |
| A-30 | P3 | ⚪ | O-8 / O-9 / O-10 / D-2 / D-3 / D-4 | **Kod ve dokümantasyon düzeni:** Kalan introspeksiyon/seed/firma adı tekrarları, eski context dosyaları, DTO doğrulama ve render mode tekrarlarını değerlendir. D-2’de yalnız tanım iddiasını düzelt. | Gerekçeli refactor/uyumluluk kararı; IKopyalanabilirTenant entity uygulamaları ve firma kopyalamada kullanımı doğru belgeli. |
| A-31 | P3 | ⚪ | D-1 / D-6 | **Çevrimdışı ve depolama kapsamı:** Sunucuya bağlı çalışma/çevrimdışı sınırı ile local/S3 seçim, yedekleme ve işletim kurallarını belgele. | Müşteriye sunulan ürün kapsamı, depolama sorumluluğu ve log saklama kararı açık. |

## 4. Önerilen çalışma sırası

1. **P0:** Lisans envanteri ve bağımsız anahtar kurtarma; tam belge/DB kurtarma; güvenlik/tenant kabulü; aktif sırların kapanışı.
2. **P1 uygulama:** Araç import sonuç koruması, audit kalan yazımlar, kalıcı dosya temizliği ve DB tekillik kısıtları. Backfill/firma değiştirme kapsam denetimiyle bulunan eksikler giderilir.
3. **P1 kabul:** Gerçek DB/audit, mali ekran/API, temiz/eski kurulum, DataSync ve müşteri paketi. Tarih geçişi kaynak semantiği belirlendikten sonra yürütülür.
4. **P2/P3:** Rapor görsel kabulü, cache/Redis ve entegrasyon kabulü; sağlayıcı/çevrimdışı/depolama ürün kararları ve kod düzeni.
5. **Teslim:** Geçerli doküman bağlantıları, silme gerekçeleri, güncel paket taraması, kabul kanıtları ve teslim commit’i kaydedilir.

## 5. Kanıtların sınırı ve doküman tutarlılığı

- Önceki kayıtlı izole kontroller N-1/N-2 için 18, audit için 12, filo güncelleme için 11, filo ilişki kapsamı için 57 ve banka şablonu/kapsamı için 46 kontrol içerir. Bunlar aynı test kümesi değildir; önceki banka 16 kontrol güncel 46 içinde tekrar kapsanır. Sayılar tek bir toplam başarı sayısına eklenmedi.
- Son araç silme ve ihale çıktısı Web derlemeleri ayrı geçici klasörde `UseAppHost=false` ile 0 uyarı/0 hata tamamlandı. Normal personel banka raporu derlemesi çalışan EXE kilidi nedeniyle durdu; ayrı çıktı derlemesi başarılı oldu. Çalışan uygulamanın en son kaynakla yeniden başlatıldığına dair kabul yok.
- Kök kritik birim test projesi açık olarak kayıtlıdır; çalışma ağacında Rent-a-Car kontrol projesi bulunması bu kapsamı kapatmaz. Bu tur test çalıştırılmadı.
- **Eksik kaynak:** `SATISA-CIKARIM-YENIDEN-ANALIZ-2026-10-02.md` çalışma ağacında bulunamadı. İçeriği yeniden okunmuş gibi değerlendirilmedi; bu belge mevcut güncel rapor/denetim kayıtlarını esas alır. Eksik bağlantının kapanışı A-23’tür.
- Git durumunda önceki denetim/faz/değerlendirme raporları ve Rent-a-Car analiz belgesi silinmiş görünüyor. Bu değişiklikler geri alınmadı veya onaylanmış teslim sayılmadı.
- K-1 anahtar butonları güncel LisansDesktop **Anahtar ve Yedek** sekmesindedir; eski “başlıkta dört buton” notları tarihsel ekranı anlatır. Tarihsel DPAPI yedeği, güncel parola korumalı .mkkey bağımsız kurtarma kabulü olarak kullanılmaz.
- Ortak `LicenseIdentity` normalizasyonu tamamlandı; O-8’in açık kısmı kalan firma adı/introspeksiyon/seed tekrarlarıdır. “Tüm normalizasyon hâlâ açık” ifadesi kullanılmamalıdır.
- Araç silme kaynak düzeltmesi tamamlandı; önceki modalların kalan iş listesindeki genel “araç silme açık” ifadesi artık yalnız runtime/ilişki kabulü anlamına gelir. Araç import koruması ayrıca açık kalır.

## 6. Mevcut kaynaklar

- [Güncel durum raporu ve tarihsel devam ekleri](SATISA-CIKARIM-GUNCEL-DURUM-RAPORU.md)
- [İkinci düzeltme denetim raporu](DUZELTME-DENETIM-RAPORU-2.md)
- [İlk satışa çıkarım analizi — tarihsel bulgular](SATISA-CIKARIM-ANALIZ-RAPORU.md)
- [Lisans programı ve anahtar geçişi](LISANS-IMZA-GECIS.md)
- [Şifreli belge yedek/kurtarma kapsamı](SIFRELI-BELGE-YEDEK-KURTARMA.md)

**Sonuç:** Tamamlanan kod kısımları 🟢; kalan kabul/geçişler 🟡, açık uygulama işleri 🔴, ürün/düzenleme kararları ⚪ olarak izlenir. Açık görevlerin kapanışı somut kanıtla işlenmeden toplu yeşil veya satış kabulü verilmez.
