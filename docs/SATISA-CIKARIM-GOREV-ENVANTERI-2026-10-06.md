# MKFiloServis — Satışa Çıkarım Görev Envanteri

**Güncelleme:** 2026-10-09
**Esas:** Bu commit'teki yerel kod ve raporlar. Derleme veya izole doğrulama müşteri kabulü sayılmaz.  
**Karşılaştırma kaynakları:** [Son durum ve açık görevler](SATISA-CIKARIM-SON-DURUM-2026-10-05.md), [güncel durum raporu](SATISA-CIKARIM-GUNCEL-DURUM-RAPORU.md) ve [ikinci denetim](DUZELTME-DENETIM-RAPORU-2.md). Bu envanterin aşağıdaki renkleri son yeniden sınıflandırmadır.

Bu dosya bundan sonraki satışa çıkarım düzeltmelerinin **görev bazlı takip noktasıdır**. Her düzeltmede ilgili satırın yapılan/kalan alanı ve aşağıdaki değişiklik günlüğü birlikte güncellenir. Önceki 2026-10-02 tarihli 39 bulguluk yeniden analiz dosyası çalışma ağacında bulunmadığından içeriği burada yeniden kurulmuş gibi gösterilmez. Aşağıdaki 31 satır, mevcut birleşik A-01…A-31 görevleridir.

**Test sırası (2026-10-08):** Kalan kod düzeltmeleri sonrası toplu nihai doğrulama; [son aşama test planı](SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md).

## Durum özeti

| Durum | Adet | Anlam |
|---|---:|---|
| 🟢 Tamamlandı | 5 | Tanımlı teknik kapsam/karar tamamlandı; açık saha kabulü ayrıca belirtilir |
| 🟡 Kısmi / kabul bekliyor | 22 | Ana teknik akış mevcut; geçiş veya çalışma zamanı kabulü açık |
| 🔴 Açık uygulama | 4 | Temel uygulama, veri onarımı veya kritik kurtarma kabulü açık; bazı alt parçalar yapılmış olabilir |
| ⚪ Karar bekliyor | 0 | Ürün/refactor kararları bu sürüm için kayda alındı |
| **Toplam** | **31** | **Satış kabulü verilmedi** |

### Renk denetimi — 2026-10-09

| Renk | Görevler | Yeniden sınıflandırma gerekçesi |
|---|---|---|
| 🟢 | A-08, A-22, A-23, A-30, A-31 | A-23 belge/teslim ve kurulum girdisi temizliği kanıtlandı. A-08/A-22 teknik kapsamı tamamlandı; A-30/A-31 ürün kararları kaydedildi. Saha kabuli ayrı görevlerdedir. |
| 🟡 | A-01, A-02, A-04, A-05, A-06, A-07, A-09, A-11, A-12, A-13, A-14, A-17, A-18, A-19, A-20, A-21, A-24, A-25, A-26, A-27, A-28, A-29 | Kod veya sınırlı kanıt mevcut; satırdaki geçiş, gerçek veri ya da çalışma zamanı kabulü açık. |
| 🔴 | A-03, A-10, A-15, A-16 | Tam kurtarma kabulü, dosya yaşam döngüsü veya veri bağı/onarımları satış öncesi açık. |
| ⚪ | — | Karar bekleyen kalem yok. |

Renkler **görevin tamamı** içindir. Satır içinde yeşil kanıt bulunması, kırmızı veya sarı görevin kapandığı anlamına gelmez. Gerçek müşteri restore'u, sır rotasyonu, kurulum ve saha kabulü bu çalışma ağacında kanıtlanmış sayılmaz.

## P0 — Satış öncesi kritik

| Görev | Durum | Yapılan | Kalan iş / kapanış kanıtı |
|---|---|---|---|
| A-01 Lisans geçişi | 🟡 | Program içi imzalama, DPAPI depo, parola korumalı `.mkkey` yedeği ve v3 modül/sürüm hakları; ortak imza/sürüm protokolü regresyonları kalıcı teste alındı | Yetkili müşteri/lisans envanteri, bağımsız profilde yedek geri yükleme, gerçek v3 yeniden basım ve teslim kaydı |
| A-02 Modül erişimi | 🟡 | İmzalı modül hakları, menü/sayfa/API/dosya/hub politikaları; modül zarfı ve seçilen modüle imza bağı kalıcı teste alındı | Normal/Admin kullanıcı ve lisans değişimi için uçtan uca erişim kabulü |
| A-03 Tam kurtarma | 🔴 | Aktarım, arşiv uygulama ve kesinti sonrası rollback betikleri; IIS havuzu durdurma kontrolü. 2026-10-09'da altı transfer/kurtarma PowerShell betiği parser kontrolünden geçti. | İzole gerçek DB+şifreli belge restore, hata enjeksiyonu, konfigürasyon ve yeniden başlatma işletim kabulü |
| A-04 Bağımsız kurtarma | 🟡 | Arşiv/manifest/hash ve key ring hazırlığı; sınırlı izole kontroller | Farklı makine/profilde gerçek belge, credential ve key ring çözme; DB/dosya tutarlılığı |
| A-05 Giriş/tenant | 🟡 | Global yetkilendirme, Bearer/circuit ayrımı, parola ve kilit korumaları | Anonim/normal/Admin, firma A/B, oturum iptali, zaman aşımı ve bootstrap kabulü |
| A-06 İfşa olmuş sırlar | 🟡 | Kaynaktaki sabit sır temizliği ve restore betiğinde maskeli DB parolası | Aktif sır rotasyonu, geçmiş kararı ve güvenli Production yapılandırması |

## P1 — Uygulama, veri ve müşteri kabulü

| Görev | Durum | Yapılan | Kalan iş / kapanış kanıtı |
|---|---|---|---|
| A-07 Kalıcı test/CI | 🟡 | `MKFiloServis.Tests` xUnit projesi çözüme eklendi; A-15, lisans protokolü, A-10 dosya temizleme ve A-11 depo hata ayrımı dahil GitHub Linux Release işi **102/102** geçti. Eksik audit SQL kaynağı ve işletim sistemi yol kuralı testleri düzeltildi. Docker/GHCR/Trivy ve Windows CodeQL işleri geçti. [CI kanıtı](A-07-CI-DOGRULAMA-2026-10-08.md). | Gerçek lisans doğrulama/erişim, PostgreSQL, tenant, audit, müşteri restore ve mali işlem regresyonları ile saha kabulü |
| A-08 Veritabanı audit | 🟢 | PostgreSQL/SQLite ortak audit motoru ve izole SQL, restore, COPY, rollback kanıtı | Müşteri hacmi ve mali zincir kabulü ayrı A-04/A-09/A-18/A-19 kapsamındadır |
| A-09 Mali transaction | 🟡 | Transfer/cari mahsupta zorunlu kalıcı işlem kimliği ve aynı içerikte tekrar dönüşü eklendi; banka kimlik migration ve yedi servis senaryosu SQLite üzerinde geçti. Personel geri ödeme ve ortak iptal aynı Serializable transaction içinde; dokuz SQLite senaryosu geçti. Transfer/cari mahsup iptali aynı transaction içinde ters fiş ve soft-delete ile düzeltildi; net defter/banka bakiyesi ve hata rollback testleri geçti. Audit ve bazı mali işlem sınırları düzenlendi; personel avans/borç oluşturma, borç ödeme ve tekil mahsupta kayıt+fiş+bağlantı aynı Serializable transaction içinde; commit hatasında tekrar yazma engeli ve bağımsız kayıt doğrulaması, avans/borç oluşturma ve ödeme/mahsupta kalıcı benzersiz işlem kimliği eklendi (migration henüz uygulanmadı) | Tam model gerçek DB'de retry, commit hatası, savepoint ve rollback kabulü |
| A-10 Dosya yaşam döngüsü | 🔴 | Atomik şifreli upload, sürüm/soft-delete koruması ve lease/revizyon kontrollü cleanup kuyruğu mevcut. Referanssız şifreli içerik fiziksel silinmek yerine yedeklenen karantinaya taşınır; aynı DB yolundan okuma/varlık/kopyalama geri döner. Kullanıcı kararı (2026-10-09): karantina ve legacy dosyalar süresiz tutulacak, otomatik purge yapılmayacak. İzole testte gerçek `RecoveryArchive` ZIP'i oluşturulup doğrulandı, farklı depolama köküne çıkarılan karantina dosyası aynı yoldan çözüldü. 2026-10-09 odaklı cleanup journal, SQLite referans tarama, orphan tarama ve atomik dosya testleri **11/11 başarılı**. Legacy temizlik yeni şifreli kopyayı SHA-256 doğrular ve kaynak düz dosyayı yerinde tutar; önceki tam model SQLite EBYS geçişi ve 100.000 satır yerel sorgu probu mevcut. Depolama hacmi için Sistem Sağlığı ekranında %80 uyarı/%90 kritik eşikleri ve Admin ayrıntı API'si vardır; bu turda ekranın renk/durum tutarsızlığı düzeltildi. | Süresiz saklama için harici/operasyonel alarm ve müdahale prosedürü; SecureFileService dışındaki uygulama içi fiziksel yol okuyucularının uçtan uca kapsam kabulü; PostgreSQL/iki sunucu/paylaşımlı depo, gerçek müşteri restore/geçiş ve hedef hacim kabulü. EBYS genel kapsam kararı, cari bağı olmayan destek kaydı ve eski kök eşleme; Unicode sorgu maliyeti ve Windows sembolik bağlantı kabulü. |
| A-11 Dosya silme kabulü | 🟡 | Servislerde dosya/DB ayrımı ve koruma çalışmaları mevcut. S3 imza başlığı HTTP istemcisinden geçiyor; indirme/varlık sorgusunda yalnızca 404 eksik nesne sayılıyor, 403/5xx çağırana hata olarak iletiliyor. Silmede 404 idempotent, 403/5xx hata. Yerel depoda okuma/varlık kontrolü yalnız bulunamayan dosyayı eksik sayıyor; erişim hatası gizlenmiyor. Her iki depoda sahte/boş imzalı URL üretimi durduruldu. Araç, tedarikçi ve özlük evrakı ekranları geri alınabilir kaldırmayı doğru bildiriyor. Yerel regresyonlar ve Web Release derlemesi geçti. | Gerçek S3/MinIO hizmetinde imza ve izin kabulü; disk kilidi/izin, çoklu dosya kısmi hata, iptal ve UI bildirim/yenileme davranışı kabulü |
| A-12 Araç Excel | 🟡 | Modal/firma/dosya sürümü ve aktarım kilidi mevcut. Modal veya firma servis yazımı sırasında değişirse eski sonuç yeni modala yazılmıyor; kaydedilen satır/hata sayısı kullanıcıya bildiriliyor ve güncel liste yenileniyor. İşlem sürerken ikinci aktarım modalı açılamıyor; tarayıcı dosya akışı kapatılıyor. Web Release derlemesi geçti. | Gerçek Excel dosyasıyla okuma/yazma yarışı, firma değişimi ve bileşen Dispose çalışma zamanı kabulü; kısmi commit sonrası firma verisi doğrulaması |
| A-13 Araç taşıma | 🟡 | Yetki ve kaynak/hedef kontrolleri, seçilen ilişkilerde tek kayıt sınırı | Diğer ilişkilerin politikası, hedef eşleme, eski veri ve runtime rollback kabulü |
| A-14 Araç ekranı | 🟡 | Liste ve plaka kaynak düzeltmeleri; araç listesi her okumada DB'den alınıyor, başarısız cache geçersizleştirmesi sonrası eski araç verisi gösterme yolu kaldırıldı | A→B→A, modal, çift işlem, belge ve audit rollback kabulü; büyük araç listesinin sorgu yükü |
| A-15 Veri bütünlüğü | 🔴 | Aktif plaka, import, snapshot ve varsayılan şablon tekillikleri; EF + DB banka hareketi/fatura-ödeme firma korumaları. 2026-10-09'da banka hareketinin personel cebinden, araç, araç masrafı, mahsup ve geri ödeme bağlantıları için SQLite/PostgreSQL guard migration'ı eklendi; hem yardımcı hem ana hareket-firma migration'ı PostgreSQL'de ön taramadan trigger kurulumuna kadar ilgili tabloları kilitliyor. SQLite migration regresyonları 14/14; banka hareketi/ödeme eşleştirme/servis ve kilit sırası regresyonları **68/68** geçti. Önceki banka ve ödeme eşleştirme migration'larının [izole iki sağlayıcı doğrulaması](A-15-IZOLE-FIRMA-BAGI-DOGRULAMA-2026-10-06.md) mevcut. | PostgreSQL migration'larının bu oturumda çalışma zamanı kabulü, diğer tenant ilişkileri, tüm eski veri taraması, tam model/müşteri migration zinciri ve çoklu bağlantı eşzamanlılık kabulü |
| A-16 Eski veri | 🔴 | Migration ön kontrollerine ek olarak DataSync'te SQLite/PostgreSQL için 15 sabit kontrol, her iki sağlayıcıda tekli/bileşik sahipsiz FK ve composite `FirmaId` uyuşmazlığı taraması içeren salt okunur [ön envanter](A-16-ESKI-VERI-ENVANTERI.md) var. A16-11–A16-15 banka hareketi/personel/araç/masraf/mahsup/geri ödeme ve fatura-ödeme firma bağlarını kapsar. A16-05 ayrıca boş, sıfır/negatif ve sahipsiz banka hesap kimliklerini kapsayacak şekilde düzeltildi; Release CLI sentetik kabulinde dört geçersiz hareketin tamamı bulundu. | FK'siz veya FirmaId'siz iş ilişkileri, gerçek müşteri bulgu listesi, yetkili kontrollü onarım ve öncesi/sonrası tutarlılık kanıtı yok |
| A-17 Mali ekran/API | 🟡 | Banka okuyucu, maaş ve fatura kod düzeltmeleri | Gerçek CSV/XLSX, API hata, rol/firma, PDF/SMTP kabulü |
| A-18 Şema/başlangıç | 🟡 | Başlangıç hata sınıflandırması ve şema hazırlığı düzenlendi | Temiz/eski kurulumda migration ve başarısız başlangıç davranışı kabulü |
| A-19 DataSync | 🟡 | İki yönlü aktarım bütünlük kontrolleri eklendi | Gerçek SQLite/PostgreSQL veri, FK/sequence, eksik kolon ve hata rollback provası |
| A-20 Tarih semantiği | 🟡 | Plaka tarihi senkronizasyonu ve legacy uyarısı | UTC/yerel saat sütun envanteri, geçiş ve geri dönüş planı |
| A-21 Müşteri paketi | 🟡 | Lisans üreticisi müşteri paketinden ayrıldı; paketleme düzenlendi | Temiz hedef makinede kurulum, güncelleme ve lisans sürüm hakkı kabulü |
| A-22 Bağımlılıklar | 🟢 | Yeni test projesi dahil çözümdeki yedi proje ve çözüm dışı Rent-a-Car kontrolü doğrudan/geçişli NuGet taraması kapsamındadır; LisansDesktop'un açık bildirimli SQLite kütüphanesi 2.1.13'e yükseltildi. Windows CI NuGet işi restore ve taramayı başarıyla tamamladı; [tarama kaydı](A-22-BAGIMLILIK-TARAMASI-2026-10-06.md), [CI kanıtı](A-07-CI-DOGRULAMA-2026-10-08.md). | Tarama anında bilinen NuGet açıkları bulunmadı. Yeni bildirimler için CI taraması sürer; müşteri paketinin kurulum kabulü A-21'dedir |
| A-23 Doküman/teslim | 🟢 | [Belge/teslim kararı](A-23-TESLIM-KARARI-2026-10-08.md) ile güncel 31 görev kaynağı ve tarihsel belgelerin yeri sabitlendi. Yerel çalışma zamanı ayarları Git/publish/kurulum girdisinden çıkarıldı; Web publish çıktısında bulunmadıkları doğrulandı. | Hedef makine kurulum ve müşteri kabulü A-21, eski sırların rotasyonu A-06 kapsamında sürer. |

## P2/P3 — Ürün kapsamı ve sonraki kabul

| Görev | Durum | Yapılan | Kalan iş / kapanış kanıtı |
|---|---|---|---|
| A-24 Çok süreçli cache | 🟡 | Üretim `CRMFilo:` anahtarlı iş listeleri ve dashboard artık her istekte veri kaynağından okunuyor; `Get/Exists/Set/Refresh/Remove` bu anahtarlarda önbellek kullanmıyor. Böylece cache erişimi veya kaybolan invalidation iş verisini bayat gösteremiyor. Araç listesi de doğrudan DB okuyor. Jenerik cache protokolü ayrı anahtarlar için korunuyor. | Hedef müşteri hacminde doğrudan DB sorgu yükü ve çoklu sunucu çalışma zamanı kabulü; kalıcı, transaction bağlı cache yeniden devreye alınacaksa ayrı tasarım. A-25'in Redis kabulü yalnız jenerik cache protokolü için açıktır. |
| A-25 Cache kabulü | 🟡 | Ortak MemoryDistributedCache kullanan bağımsız servis örneklerinde prefix invalidation, bekleyen factory, iptal ve backend arızası için 4 regresyon geçti; güncel tam Release paketi 42/42. | Gerçek Redis/ağ kesintisi ve yeniden bağlanma, çok süreçli yük ve geniş kapsamlı invalidation maliyeti. Docker istemcisi var; Docker daemon bağlantısı bu ortamda kullanılamadı. |
| A-26 Excel/PDF | 🟡 | Personel banka baskı stili ve ihale XLSX/PDF üretimi | Uzun metin, çok sayfa, negatif tutar, SGK ve toplam eşitliği görsel kabulü |
| A-27 Dış entegrasyon | 🟡 | HTTP retry ve Luca kaynak düzeltmeleri | Gerçek UBL/portal, eski credential ve belirsiz mali POST kabulü |
| A-28 DB sağlayıcıları | 🟡 | Bu sürümün desteklenen veritabanları PostgreSQL ve SQLite olarak kararlaştırıldı. Web başlangıcı ve DB ayar servisi PostgreSQL/SQLite dışındaki sağlayıcıları fail-fast reddediyor; ayar ekranında SQL Server/MySQL seçenekleri kaldırıldı. Desteklenen/eski sağlayıcı ayrımı ve ayar dosyasına yazmama davranışı için regresyonlar eklendi. | PostgreSQL ve SQLite için temiz kurulum/yükseltme hedef kabulü; gerçek müşteri veritabanı doğrulaması |
| A-29 Mali politikalar | 🟡 | [Muhafazakar mali kurallar](A-29-31-URUN-KARARLARI.md) karara bağlandı; çift yönlü veya tutarsız banka satırları reddediliyor; kilitli/fişli maaş snapshot silinemiyor. Güncel yetki sorguları etkin kullanıcı ve rol/yetkiyi DB'den yeniliyor. Fatura yazımları, manuel fiş oluşturma/düzenleme/silme/onay, hesap planı düzenleme/silme, araç masrafı, kolay muhasebe kaydetme ve personel finans yazım girişleri korunuyor. Banka/kasa ve banka hesabı yazım servislerine güncel izin kontrolü, genel banka hareketi güncelleme/kaldırmaya bağlantı ve kayıt sürümü koruması eklendi. Banka hesabı oluşturma seçili firma ve Serializable ortak yazım sınırına alındı; iki SQLite regresyonu geçti. Puantaj finans snapshot oluşturma seçili firmaya bağlandı; dönem kontrolü ve kayıt aynı Serializable transaction'da, tekrar çağrı regresyonu geçti. Transfer/cari mahsup iptali ve ters fiş kayıtları atomik; manuel fiş müdahalesi engelli; altı SQLite iptal senaryosu geçti. Personel geri ödeme/ortak iptal ve ekran izin/durum kuralları düzeltildi; dokuz ek SQLite senaryosu geçti. Transfer/cari mahsup kalıcı kimlikleri ve ekran bekleyen istek saklaması eklendi. | Puantaj fatura/kalem/link ve hakediş fatura/snapshot zincirinin çok context'li atomikliği, diğer mali yazımların ortak yetki sınırı ve gerçek müşteri rol değişimi kabulü açık; A-29 bu nedenle sarı. |
| A-30 Kod/belge düzeni | 🟢 | [Satış sürümü refactor kararı](A-29-31-URUN-KARARLARI.md): davranış değiştirmeyen P3 temizliği ertelendi, hata düzeltmesi kapsamı ayrı tutuldu. | Refactor backlog'a ertelendi; satış engeli olarak izlenmiyor. |
| A-31 Çevrimdışı/depolama | 🟢 | [Ürün kapsamı](A-29-31-URUN-KARARLARI.md): çevrimdışı kullanım yok; tek düğümde Local, yapılandırılmış ortak depoda S3; yedek sınırı ve log/audit ayrımı tanımlandı. | A-04/A-10/A-11'deki gerçek restore, S3 ve çok sunucu kabulleri ayrı görevlerde sürer. |

## Çalışma sırası

1. P0 bağımsız kurtarma ve müşteri lisans/sır geçişi kabulü.
2. P1 açık uygulama: A-10, A-15 ve A-16; A-07 test kapsamını genişlet; ardından gerçek DB ve müşteri kurulum kabulü.
3. P2/P3 kararları [ürün kararları belgesinde](A-29-31-URUN-KARARLARI.md) sabitlendi; kalan uygulama ve saha kabuli görev bazında sürer.

## Değişiklik günlüğü

### 2026-10-09 — A-15 banka hareketi yardımcı tenant bağlantıları

- 🟢 `BankaKasaHareketleri` için uygulama katmanındaki yardımcı tenant bağlantı kontrolleri daha önce mevcutken bunları veritabanında zorunlu kılan `20261009150000_GuardBankMovementAuxiliaryTenantLinks` migration'ı eklendi. Korunan bağlar: `PersonelCebindenId`, `AracId`, `AracMasrafId`, `MahsupHareketId` ve `PersonelGeriOdemeHareketId`; referanslı araç, masraf veya şoförün firma değiştirmesi de engellenir.
- 🟢 Hem `20261009150000_GuardBankMovementAuxiliaryTenantLinks` hem de ana `20261006200000_GuardBankMovementTenantLinks` PostgreSQL migration'ı ilgili tabloları `SHARE ROW EXCLUSIVE` modunda önceden kilitler. Her iki migration'da eski satır preflight'ı ile trigger kurulumu arasında yeni yazım yarışı engellenir; kilit/preflight/trigger sırası regresyon testinde sabitlenmiştir.
- 🟢 SQLite migration testleri **14/14**; banka hareketi, ödeme eşleştirmesi, servis ve PostgreSQL kilit sırası regresyonları birlikte **68/68** geçti. İzole test build'i **0 uyarı / 0 hata**.
- 🟡 PostgreSQL credential'ı görev sürecinde bulunmadığından PostgreSQL test/uygulama kabulü ve müşteri DB geçişi yapılmadı. Başka iş ilişkileri ve genel A-15 kapsamı açık; A-15 🔴 ve renk sayımı değişmedi.

### 2026-10-09 — A-28 kapsam kararı ve odaklı regresyon doğrulaması

- 🟢 PostgreSQL/SQLite destek kapsamı ürün kararı olarak sabitlendi; SQL Server/MySQL desteği bu sürümün kapsamına alınmadı. Ayar ekranı, runtime sağlayıcı denetimi ve desteklenmeyen eski ayarla test/apply sırasında dosyaya yazmama davranışı güncellendi.
- 🟢 Web derlemesi **0 hata / 0 uyarı**; `DatabaseProviderScopeTests` **6/6** geçti. A-03/A-10/A-15/A-28 hedefli test filtresi **32/32** geçti. A-03 aktarım/kurtarma PowerShell betiklerinin altısı parser denetiminden geçti.
- 🟡 Gerçek PostgreSQL/SQLite temiz kurulum-yükseltme, IIS kurtarma, çoklu sunucu dosya yaşam döngüsü ve müşteri verisi kabulü yapılmadı. Kabul DB'sine bağlanılmadı veya değişiklik uygulanmadı. A-03/A-10/A-15/A-16 kırmızı; A-28 sarı. Güncel toplam **5 yeşil / 22 sarı / 4 kırmızı / 0 beyaz**.

### 2026-10-08 — A-28 desteklenmeyen DB sağlayıcısında güvenli duruş

- 🟢 Başlangıç, `DbInitializer` çalışmadan önce SQL Server/MySQL seçimini reddediyor; hata mesajı desteklenen sağlayıcıları ve migration yapılmadığını belirtiyor.
- 🟢 Veritabanı ayar servisi desteklenmeyen sağlayıcıda bağlantı testi, ayar kaydı ve geçiş başlatmayı reddediyor. Ayarlar ekranı SQL Server/MySQL seçeneklerini yeni seçim için kapatıyor; eski ayarla açılmışsa kırmızı uyarı ve PostgreSQL/SQLite'a dönüş yolu gösteriyor.
- 🟡 Web Release derlemesi ve gerçek PostgreSQL/SQLite temiz kurulum-yükseltme hedef kabulü tamamlanmadan A-28 kapanmaz. SQL Server/MySQL ürün desteği de tamamlanmadı; görev 🔴, toplam renkler **4 yeşil / 21 sarı / 6 kırmızı / 0 beyaz**.

### 2026-10-08 — A-16 PostgreSQL yabancı anahtar ihlal taraması

- 🟢 Salt okunur envanter, PostgreSQL `information_schema` metadata'sından tekli ve composite FK'leri bulup referans kayıtları kontrol ediyor. Örnek kimlik alanı metadata'daki gerçek harf biçimiyle okunuyor; tenant FK keşif sorgusunun metadata join'i de düzeltildi.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**. Geçici PostgreSQL 17 kümesinde sentetik tekli ve composite sahipsiz FK kayıtları ayrı ayrı **1'er** bulundu; deneme şemasında diğer sabit tablolar eksik olduğu için rapor `Complete=false`/çıkış 3 verdi.
- 🔴 Gerçek müşteri şeması, composite FK firma uyuşmazlığı, tanımsız iş ilişkileri ve kontrollü onarım kabulü açık; A-16 kırmızı kalır, renk sayımı değişmez.

### 2026-10-08 — A-16 composite tenant FK denetimi

- 🟢 SQLite ve PostgreSQL tenant taramaları tek sütunlu FK sınırlamasından çıkarıldı; tüm kaynak/hedef FK sütunları beraber eşleştirilir, ardından `FirmaId` karşılaştırılır.
- 🟢 Geçici PostgreSQL 17 verisinde farklı firma kimliğine bağlanan tek composite FK `A16-TENANT-*` ile **1** bulgu verdi. DataSync Release derlemesi **0 uyarı / 0 hata**.
- 🔴 Gerçek müşteri verisi ve kontrollü onarım kabulü yok; A-16 kırmızı, toplam renk sayımı değişmedi.

### 2026-10-06 — A-10 yerel nesne deposu yol ve upload güvenliği

- 🟢 `LocalObjectStorageService` artık boş/köklenmiş anahtarları reddeder ve normalize edilmiş hedefin uploads dizini altında kaldığını denetler. Dosya upload'ı geçici dosyadan son ada taşınır; silme öncesi `File.Exists` kontrolü kaldırıldı.
- 🟢 Başarılı atomik upload, kök dışına çıkma reddi ve var olmayan dosya silme davranışı için 2 kalıcı test eklendi. Release test paketi **25/25 başarılı**.
- 🔴 Kalıcı cleanup kuyruğu, çok süreçli yarış/TOCTOU ve gerçek storage hataları ayrıca doğrulanmadı; A-10 kırmızı kalır.

### 2026-10-06 — A-10 depolama yollarında sembolik bağlantı kontrolü

- 🟢 `StorageFilePath.Resolve` artık depolama içindeki mevcut yol bileşenlerini inceliyor ve sembolik bağlantı üzerinden hedefe gitmeyi reddediyor. `LocalObjectStorageService` de aynı ortak çözümleyiciyi kullanıyor.
- 🟢 Release test paketi **25/25 başarılı**, `git diff --check` temiz. Sembolik bağlantı testi Windows ayrıcalık hatasıyla çalışmadı; bu ortamda saldırı senaryosu doğrulanmış sayılmaz.
- 🔴 Var olan bağlantı kontrolü ile dosya işlemi arasında TOCTOU penceresi, kalıcı cleanup kuyruğu, yetim envanteri ve gerçek depoda arıza kabulü açık; A-10 kırmızı kalır. Sayım değişmedi: **2 yeşil / 20 sarı / 6 kırmızı / 3 beyaz**.

### 2026-10-06 — A-10 eksik üst klasör için idempotent silme

- 🟢 Yerel nesne deposu silmeden önce üst dizini sorgular; dizin yoksa dosya da yok kabul edip döner. `File.Exists` kullanılmadığından erişim/IO hataları yutulmaz.
- 🟢 Üst dizin hiç oluşturulmamış senaryo için regresyon testi eklendi; Release paketi **26/26 başarılı**, `git diff --check` temiz.
- 🔴 Bu davranış retry kuyruğu değildir; kalıcı temizleme kuyruğu, çok süreçli yarış/TOCTOU, yetim envanteri ve gerçek storage kesinti kabulü açık. A-10 🔴 kalır; görev sayımı değişmez.

### 2026-10-06 — A-10 personel özlükte paylaşılan dosya yolunu koruma

- 🟢 `DeleteEvrakDosyaAsync`, DB satırını kaldırdıktan sonra fiziksel dosyayı silmeden önce `PersonelOzlukEvraklar` ve `PersonelOzlukEvrakVersiyonlar` tablolarını filtreleri yok sayarak kontrol eder. Eski aktif/sürüm kaydı aynı yolu kullanıyorsa dosya tutulur.
- 🟢 Release build geçti; kalıcı test takımının tamamı **23/23 başarılı**. Bu tur için aynı yol senaryosuna özel DB entegrasyon testi yoktur.
- 🔴 Kontrol ile silme arasındaki çok süreçli yarış, kalıcı kuyruk/yeniden deneme ve diğer tablo referansları açık; A-10 kırmızı kalır.

### 2026-10-06 — A-10 şifreli upload'ın atomik yayımlanması

- 🟢 `SecureFileService.SaveEncryptedAsync` artık şifreli içeriği son dosya adına doğrudan yazmaz. Aynı klasörde benzersiz geçici dosyaya yazar ve tamamlandıktan sonra son ada taşır; hata/iptalde geçici dosyayı silmeyi dener.
- 🟢 `SecureFileAtomicSaveTests` başarılı upload'da tam içeriği ve geçici dosya kalmadığını; iptal edilen upload'da yayımlanmış/geçici dosya kalmadığını doğruladı. Güncel Release test toplamı **23/23 başarılı**.
- 🔴 Bu, kalıcı cleanup kuyruğu ve diğer dosya yaşam döngüsü risklerini kapatmaz. A-10 kırmızı kalır; sayım değişmez.

### 2026-10-06 — A-07 lisans protokolü regresyonları

- 🟢 `LicenseProtocolTests` ile modül zarflarının kanonik biçimi, bozuk/tekrarlı/bilinmeyen modüllerin reddi, RSA-PSS imzasının seçilen modüllere bağı ve sürüm hakkı sınırları kalıcı teste alındı. Release `dotnet test` sonucu **21 başarılı / 0 başarısız**.
- 🟡 Test geçici RSA anahtarıyla ortak imza protokolünü sınar; üretim açık anahtarıyla gerçek müşteri lisansının yüklenmesi, modül yetki ekranı/API uçtan uca kabulü ve yedek geri yükleme açık. A-01, A-02 ve A-07 renkleri 🟡 kalır; toplam sayım değişmez.

### 2026-10-06 — A-07 kalıcı test projesi ve zorunlu CI

- 🟢 `MKFiloServis.Tests` çözümde oluşturuldu. A-15 SQLite migration SQL'i için eski veri ön kontrolü, geçerli ekleme ve yedi hatalı firma bağı değişikliğini kapsayan **9 xUnit testi** Release olarak yerelde geçti.
- 🟢 `.github/workflows/tests.yml` artık test projesi yoksa sessizce geçmiyor; restore, build ve `dotnet test` zorunlu. Kapsam toplama bu büyük Web assembly'sinde yerelde uzun süre bitmediği için CI'den çıkarıldı; TRX sonucu saklanır.
- 🟢 Yeni test projesi eklendikten sonra çözümdeki yedi projenin doğrudan/geçişli NuGet bildirimi yeniden tarandı; bildirilen açık yok. A-22 🟢 durumu korunur.
- 🟡 GitHub CI sonucu ve diğer kritik regresyonlar henüz yok. A-07 🟡; sayım **2 yeşil / 20 sarı / 6 kırmızı / 3 beyaz**.

### 2026-10-06 — Envanter ve A-23 belge bağlantısı

- 🟢 A-01…A-31 satırları yeniden sayıldı: 1 yeşil, 20 sarı, 7 kırmızı, 3 beyaz. Önceki üst özetin 6 kırmızı/4 beyaz sayım hatası düzeltildi.
- 🟢 2026-10-02 tarihli eksik yeniden analiz dosyasına giden bağlantılar bu **yeni güncel envantere** yönlendirildi. Eski 39 bulguluk dosyanın içeriğinin geri getirildiği iddia edilmez.
- 🟢 İkinci denetim raporundaki bulunamayan ilk denetim dosyasına giden bağlantı kaldırıldı. `docs/*.md` içindeki yerel Markdown hedefleri tarandı; kalan kırık bağlantı bulunmadı.
- 🟢 `git ls-files` taramasında izlenen `.log`, `test_all.txt`, `setup/output`, `payload`, `.exe`, `.zip`, `.bak` veya `.dump` teslim artığı bulunmadı. İzlenen iki `.sql` dosyası kaynak betiğidir (`scripts/run-engine.sql`, `scripts/seed-test-data.sql`). `.gitignore` yerel çıktı, veritabanı ve yedek dosyalarını dışlıyor.
- 🟡 Çalışma ağacında çok sayıda değiştirilmiş kaynak ve rapor, yeni migration/restore betikleri ve ayrıca kullanıcıya ait olabilecek `.kilo/` yapılandırması izlenmemiş durumdadır. Bunlar otomatik silinmedi, stage edilmedi veya teslim commit'i yapılmadı. A-23 kırmızı kalır.
- 🟡 A-23 teslim kapanışı için belge saklama kararı, yerel çıktı/ham logların gözden geçirilmesi ve teslim commit'i kalır. Güncel Git durumunda izlenen dosya silinmesi görülmedi.

### 2026-10-06 — A-15 doğrudan veritabanı yazımlarında firma bağı

- 🟢 `20261006200000_GuardBankMovementTenantLinks` migration'ı PostgreSQL ve SQLite için eklendi. Banka/Kasa hareketindeki banka hesabı, isteğe bağlı cari ve personel geri ödeme hesabı firma eşleşmesi INSERT/UPDATE tetikleyicilerinde denetlenir. İlişkili hesap veya carinin firma alanını sonradan değiştirme de reddedilir.
- 🟢 Migration, mevcut hareketlerde uyuşmayan veya bulunmayan ilişkileri önceden tarar ve veri onarmadan durur. `Down` yalnız bu migration'ın tetikleyici ve fonksiyonlarını kaldırır.
- 🟢 Web Debug derlemesi `setup/output/a15-db-tenant-guard-build` konumuna 0 uyarı, 0 hata ile tamamlandı.
- 🟡 Gerçek PostgreSQL/SQLite migration, hata enjeksiyonu, eski kayıt onarımı ve eşzamanlı DB yazımı kabulü yapılmadı. Diğer A-15 tenant bağlantıları da açık; görev 🔴 kalır.

### 2026-10-06 — A-22 güncel NuGet taraması ve lisans aracı kütüphanesi

- 🟢 Çözümdeki altı proje ve çözüm dışındaki Rent-a-Car kontrol projesi doğrudan/geçişli NuGet güvenlik bildirimi için tarandı. İlk tarama LisansDesktop'un geçişli `SQLitePCLRaw.lib.e_sqlite3 2.1.11` sürümünde yüksek önem dereceli bir bildirim gösterdi.
- 🟢 LisansDesktop projesi native kütüphaneyi 2.1.13'e yükseltti; Release/win-x64 dahili EXE yeniden yayımlandı. Son taramada yedi projede geçerli kaynakların bildirdiği bilinen açık kalmadı. CI taraması komut hatasında artık başarısız olur ve çözüm dışı projeyi de kapsar. [A-22 kanıtı](A-22-BAGIMLILIK-TARAMASI-2026-10-06.md).
- 🟢 A-22 görev rengi yeşile güncellendi; envanter **2 yeşil / 19 sarı / 7 kırmızı / 3 beyaz**. Müşteri paketi kurulum kabulü A-21 kapsamında sarı kalır.

### 2026-10-06 — A-15 PostgreSQL/SQLite izole DB doğrulaması

- 🟢 Migration SQL'i SQLite bellek DB ve ayrı PostgreSQL 17 geçici kümesinde uygulandı. Eski farklı firma hesabı ön kontrolde durdu; geçerli hareket yazıldı. Her sağlayıcıda yedi hatalı değişiklik reddedildi; mevcut hareketin hesap/cariyle birlikte firma taşıması da DB sınırında engellendi.
- 🟢 Son Web Debug derlemesi 0 uyarı/0 hata. [Senaryo ve sınırlar](A-15-IZOLE-FIRMA-BAGI-DOGRULAMA-2026-10-06.md).
- 🟡 Asgari şema doğrulaması tam model/müşteri migration ve eşzamanlılık kabulü değildir. Diğer A-15 bağlantıları açık; A-15 🔴 kalır. Renk sayımı değişmedi.

### 2026-10-06 — A-10 idempotent dosya silmeyi ortaklaştırma

- 🟢 Eksik üst klasör denetimi `StorageFilePath.DeleteIdempotently` içine alındı ve hem şifreli dosya servisinde hem yerel nesne deposunda kullanıldı.
- 🟢 Release test paketi **26/26 başarılı**, `git diff --check` temiz. Üst dizin yok testi ortak metodu yerel nesne deposu üzerinden doğruluyor.
- 🔴 Şifreli depoda bu durum için ayrı servis entegrasyonu testi, kalıcı cleanup kuyruğu, TOCTOU/çok süreçli yarış, yetim envanteri ve gerçek storage arıza kabulü açık. A-10 🔴 kalır; sayım değişmedi.

### 2026-10-06 — A-10 legacy dosya taşımasında DB öncelikli silme

- 🟢 `DosyaMigrasyonService`, eski açık dosyayı artık yeni şifreli yolu DB'ye yazmadan silmiyor. SaveChanges hatasında yeni yol taze bağlamla doğrulanmadan eski dosyayı silmez; eski yol başka bir DB kaydınca kullanılıyorsa korur.
- 🟢 Eski uploads yolu çözümlemesi uploads köküyle sınırlandı ve sembolik bağlantı bileşenlerini reddeden ortak çözümleyiciden geçiyor. Release test paketi **26/26 başarılı**, `git diff --check` temiz.
- 🔴 Commit sonrası süreç kesilirse kalan eski açık dosyalar için kalıcı yeniden deneme kuyruğu yoktur. Gerçek eski DB/dosya migration kabulü, orphan taraması ve TOCTOU/çok süreçli kontrol açık; A-10 🔴 kalır.

- 🟢 Referans koruması aynı zamanda ortak evrak, özlük aktif/sürüm, destek eki, tedarikçi eki, fatura PDF/XML ve proforma PDF yollarını da kapsar; soft-delete filtreleri yok sayılarak kontrol edilir. Son Release test sonucu bu ek sorgularla da 26/26 başarılıdır.

### 2026-10-06 — A-10 migration ekranı yönetici sınırı ve kapsam açıklığı

- 🟢 `/ayarlar/dosya-migrasyonu` artık `Admin` rolü gerektirir; modül lisans politikası da korunur. Yetkisiz kullanıcılar lisanslı EBYS modülüne sahip olsa bile sistem dosyalarını taşıyamaz.
- 🟢 Ekran ve servis açıklaması gerçek kapsamı söylüyor: aktif firma filtreleri altındaki EBYS/araç ana ve sürüm kayıtları. Önceki “tüm düz metin dosyaları” ifadesi düzeltildi. Release test paketi **26/26 başarılı**, `git diff --check` temiz.
- 🔴 Soft-deleted kayıtlar ve diğer belge türleri kapsam dışı; tenant/rol gerçek kullanıcı kabulü, kalıcı temizleme kuyruğu ve orphan taraması açık. A-10 kırmızı kalır.

### 2026-10-06 — A-10 şifreli dosya yetim envanteri

- 🟢 Admin evrak bakım raporuna uploads, Arsiv ve Depo köklerinde sembolik bağlantı olmayan `.enc` dosyalarının envanteri eklendi. DB'deki bilinen `DosyaYolu`, `PdfDosyaYolu`, `XmlDosyaYolu` alanları soft-delete filtreleri yok sayılarak okunur; eşleşmeyen dosyalar yol/boyut/değişiklik tarihiyle gösterilir.
- 🟢 Bu envanter salt okunurdur: bakım ekranı orphan dosyalarını otomatik silmez. Scanner testi uploads/Arsiv/Depo karşılaştırmasını ve diske dokunulmadığını doğrular. Release paketi **27/27 başarılı**, `git diff --check` temiz.
- 🔴 Yalnız `.enc` dosyaları ve bilinen alanlar taranır; eski düz metin dosyaları, gerçek müşteri DB'si, dosya sistemindeki yarış ve kalıcı cleanup/retry akışı kapsanmaz. A-10 kırmızı kalır; toplam renk sayımı değişmez.

### 2026-10-06 — A-10 kalıcı dosya temizleme kuyruğu

- 🟢 Silme isteği, Data Protection ile şifrelenen JSON günlüğüne atomik yazılıyor; süreçler arası günlük kilidi, idempotent kayıt/tamamlamalar ve artan retry aralığı mevcut. Web worker başlangıçtan bir dakika sonra kuyruğu tarıyor; silmeden önce bilinen DB dosya yolu alanlarını `IgnoreQueryFilters()` ile denetliyor.
- 🟢 Kalıcı test kuyruğun yeniden açılışta korunmasını, retry zamanının ertelenmesini ve tamamlanan girdinin kalkmasını doğruluyor. Release test paketi **28/28 başarılı**; `git diff --check` temiz (yalnız çalışma ağacındaki CRLF dönüşüm uyarıları var).
- 🔴 Worker gerçek müşteri DB/deposunda çalıştırılmadı. DB commit ile kuyruğa alma transaction'a bağlı değil; aradaki süreç çökmesi yetim bırakabilir. DB referans kontrolü ile fiziksel unlink atomik değil. Bu nedenle A-10 kırmızı kalır.
- 🟢 Journal claim'i 5 dakikalık lease ile serileştirildi; lease süresince diğer worker aynı girdiyi alamaz. Release testi aynı girdinin ikinci kez claim edilmediğini ve lease bitince tekrar alınabildiğini doğrular.
- 🟢 Worker artık tek girdiyi claim edip işler; işlem boyunca lease'i dakikada bir yeniler. Böylece sıradaki girdinin lease'i beklerken dolmaz. Test yenileme sonrasında eski lease bitişinde ikinci claim'i ve eski sahibin yenilemesini reddeder.
- 🔴 İşlem sırasında uzun bir kesinti veya yenileme hatası hâlâ lease kaybına neden olabilir. Referans kontrolü/unlink yarışı ve DB commit-kuyruk crash aralığı sürdüğünden A-10 kırmızı kalır.

### 2026-10-06 — A-10 depolama yolu eşleştirme koruması

- 🟢 Depolama kökü altındaki mutlak dosya yolu kanonik anahtara doğru çevrilir. Silmeden önce SQL eşleşmesine ek olarak bilinen DB yolları kanonikleştirilerek karşılaştırılır; Windows'taki harf büyüklüğü farkı referansın kaçmasına neden olmaz.
- 🟢 Mutlak yol regresyonu dahil Release test paketi **28/28 başarılı**. Gerçek müşteri DB'sinde bu tam yol taramasının süresi ve SQL sağlayıcıları doğrulanmadı; A-10 🔴 kalır.

### 2026-10-06 — A-10 şifresiz dosya aday raporu

- 🟢 Admin evrak bakım ekranı uploads ve Arsiv altındaki geçici olmayan, `.enc` dışındaki dosyaları bilinen DB yol alanlarıyla karşılaştırıp referanssız adayları ayrı listeler. Sembolik bağlantılar izlenmez; dosyalar otomatik silinmez.
- 🟢 Şifreli ve şifresiz tarama aynı DB yol envanterini kullanır. Dosyanın listede görünmesi silinebileceği anlamına gelmez; kaynak sahibinin manuel incelemesi gerekir. Release test paketi **28/28 başarılı**.
- 🔴 Referanslı eski açık dosyaların taşıma kapsamı, diğer dosya alanları, gerçek müşteri DB/depo kabulü ve commit-kuyruk yarışları açık; A-10 rengi değişmedi.

### 2026-10-06 — A-10 tam model SQLite dosya referansı kabulü

- 🟢 `ApplicationDbContext` tam modeliyle geçici SQLite DB oluşturulup soft-delete edilmiş ve farklı harf büyüklüğünde kaydedilmiş dosya yolu eklendi. `SecureFileReferenceChecker` bu dosyayı referanslı saydı; DB'de olmayan yolun serbest olduğunu doğruladı.
- 🟢 Yeni kalıcı regresyonla Release test paketi **29/29 başarılı**. Bu doğrulama gerçek müşteri DB/deposunda worker çalışması, PostgreSQL çevirisi veya referans kontrolü ile fiziksel silme yarışının kabulü değildir; A-10 🔴.

### 2026-10-06 — A-10 izole worker ve fiziksel silme kabulü

- 🟢 Tam `ApplicationDbContext` SQLite şemasında iki fiziksel dosya ve kalıcı kuyruk girdisi oluşturuldu. Worker, soft-delete edilmiş DB kaydının yoluna bağlı dosyayı korudu; referanssız dosyayı `SecureFileService` ile sildi ve kuyruktan çıkardı.
- 🟢 İşlem turu üretim worker'ıyla aynı kod yolunu kullanacak biçimde izole çalıştırıldı. Release test paketi **30/30 başarılı**.
- 🔴 Bu kabul gerçek müşteri DB/deposu, PostgreSQL, eşzamanlı yükleme-silme yarışı veya DB commit ile kuyruğa alma arasındaki çökme aralığını kapsamaz. A-10 kırmızı kalır.

### 2026-10-06 — A-10 geri alınabilir dosyayı koruma kararı

- 🟢 Kullanıcı kararı: soft-delete edilmiş kayıtların dosyaları geri alma için tutulur. `SecureFileService.DeleteAsync` doğrudan çağrıldığında ve worker'dan çağrıldığında bilinen DB referanslarını denetler; referans varsa fiziksel dosyayı korur ve gereksiz yeniden denemeyi önlemek için journal isteğini tamamlar.
- 🟢 Tam model SQLite senaryosu doğrudan çağrıyı ve ayrıca kuyruğa yeniden eklenmiş referanslı dosyanın worker tarafından korunmasını, referanssız dosyanın silinmesini doğrular. DB referans sorgusu hata verdiğinde dosyanın korunup journal isteğinin kaldığı da test edildi. Release testleri **31/31 başarılı**.
- 🔴 Gerçek müşteri verisinde çalışma, DB commit ile journal yazımı arasındaki kesinti, sorgu ile fiziksel silme arasındaki yarış ve eski dosya alanlarının tam kapsamı açık olduğu için A-10 henüz kapatılmadı.

### 2026-10-06 — A-10 eski düz dosya silme yolları

- 🟢 Ortak evrak ekranındaki `FileService.Delete` ve destek eki servisindeki doğrudan eski dosya silmesi, soft-delete sonrasından çıkarıldı. Bu kayıtlar geri alınırken düz dosyaları da kalır; DB kaydı olmayan yeni yüklemenin telafi temizliği ayrı akış olarak sürer.
- 🟢 Ortak evrakın yeni yüklemesi `SecureFileService.SaveEncryptedAsync` kullanır. Görüntüleme/indirme şifreli yeni yolu ve geçmiş düz dosya adını destekler; bakım ekranı iki türde dosya varlığını denetler. Web Release derlemesi **0 uyarı / 0 hata**.
- 🟡 Geçmiş düz dosyaların toplu şifreli geçişi ve gerçek ortam kabulü gereklidir. A-10 🔴 kalır.

### 2026-10-06 — A-10 ortak evrak eski dosya geçişi

- 🟢 Yönetici geçiş ekranı, aktif firma kapsamındaki ortak evrakın eski tek dosya adı yollarını önizlemede sayar ve şifreli depoya taşır. Yeni yol DB'de doğrulanmadan eski dosya silinmez. Tüm bilinen dosya yolu alanları soft-delete filtreleri yok sayılarak taranır; başka bir kayıt eski yolu tutuyorsa dosya korunur.
- 🟢 Ekrandaki kapsam açıklaması ve toplam bekleyen sayısı güncellendi. Web Release derlemesi **0 uyarı / 0 hata**. Bu değişiklik için çalışma zamanı geçiş testi yapılmadı.
- 🔴 Soft-delete kayıtlarının kendilerinin geçişi, gerçek müşteri veri/depo kabulü, çok süreçli sorgu-silme yarışı ve büyük DB'de tam yol taraması performansı açık. A-10 genel durumu değişmedi.

- 🟢 Geçiş tek aktif firma seçilmeden veya “Tüm Firmalar” modundayken başlatılmaz. Firma alanı olmayan ortak evrak, bağlı personel/araç kaydının firma kimliğiyle sınırlandırılır; bağı kopuk veya farklı firmaya bağlı kayıt otomatik taşınmaz. Eski `FileService` yol çözümü de sembolik bağlantı geçişlerini reddeden ortak depolama korumasına bağlandı. Önizleme hatası ekranda gösterilir.

### 2026-10-06 — A-10 silinmiş ortak evrak dosyalarının geçişi

- 🟢 Geri alınabilir silinmiş ortak evrak satırları, bağlı personel/araç silinmiş olsa bile açık firma kimliği eşleşiyorsa önizlemeye ve şifreli geçişe alınır. Ekran aktif ve silinmiş ortak evrak sayılarını ayrı gösterir.
- 🟢 Silinmiş satırın dosya yolu yeni şifreli yola yazılır; eski dosya, bilinen herhangi bir DB kaydı eski yola başvuruyorsa korunur. Web Release derlemesi **0 uyarı / 0 hata**. Bu değişiklik için çalışma zamanı geçiş testi yapılmadı.
- 🔴 Bağı kopuk eski ortak evrak kayıtları otomatik geçirilmez. EBYS/araç silinmiş belge geçmişi o tarihte kapsam dışıydı; sonraki düzeltme bu kapsamı genişletti. Gerçek müşteri verisiyle kabul ve sorgu-silme yarışı açık; A-10 🔴 kalır.

- 🟢 Araç ana dosya ve sürüm geçişinde seçili `FirmaId` açıkça denetlenir. EBYS kayıtlarında firma bağı bulunmadığı için geçiş ekranı EBYS bölümünün yönetici genel kapsamında olduğunu belirtir; bu veri modelinin firma sahipliği A-15 kapsamında ayrıca ele alınmalıdır.

### 2026-10-06 — A-10 şifreli geçiş kopyasını geri okuma

- 🟢 EBYS, araç ve ortak evrak geçişinde yeni şifreli dosya uygulamanın okuma yoluyla geri açılıp kaynak içerikle byte düzeyinde karşılaştırılır. Karşılaştırma geçmeden DB yolu değiştirilmez; doğrulanamayan yeni kopya temizlenmeye çalışılır ve eski dosya korunur.
- 🟢 Dosya okuma/yazma/doğrulama iptal belirtecine bağlandı. Web Release derlemesi **0 uyarı / 0 hata**; bu değişikliğin çalışma zamanı geçiş testi yapılmadı.
- 🔴 Müşteri verisiyle gerçek geçiş, DB commit sonrası eski dosya temizliği ve eşzamanlı sorgu-silme yarışı açık; A-10 genel durumu değişmedi.

### 2026-10-06 — A-10 eski dosya temizliği durumunun ayrılması

- 🟢 DB'de yeni şifreli yol başarıyla kaydedilip yalnız eski düz dosya silinemediğinde geçiş sonucu artık `Temizlik Bekliyor` olarak gösterilir. Başarı ve hata sayılarından ayrı sarı sayılır; operatör eski dosyanın kalmış olduğunu görür.
- 🟢 Bakım raporunun salt okunur şifresiz aday envanteri eski ortak evrak yükleme kökünü ve `wwwroot/uploads` klasörünü de kapsar; bilinen DB yol alanlarıyla karşılaştırır, sembolik bağlantıları izlemez. Adaylar otomatik silinmez.
- 🔴 Bu satırdaki kalıcı retry eksikliği aşağıdaki sonraki düzeltmeyle giderildi. Rapor referanssız dosyanın silinebileceği garantisi değildir; gerçek veri kabulü ve çok süreçli yarış açık. A-10 🔴 kalır.

### 2026-10-06 — A-10 eski düz dosya temizliğine kalıcı yeniden deneme

- 🟢 Geçişte DB yeni şifreli yola kaydedildikten sonra eski ortak evrak ve `wwwroot/uploads` dosyası için tipli bir istek DP korumalı `FileCleanupJournal` içine yazılıyor. Anlık temizleme başarısız olursa mevcut `FileCleanupRetryWorker` isteği tekrar işliyor.
- 🟢 Her denemede bilinen dosya yolu alanları silinmiş kayıtlar dahil yeniden okunuyor. Hâlâ başvurulan eski dosya geri alma için korunuyor; DB sorgusu veya disk işlemi hata verirse istek günlükte kalıyor. Web Release derlemesi **0 uyarı / 0 hata**; bu son değişiklik için çalışma zamanı testi yapılmadı.
- 🔴 Bu satırdaki eski dosya geçişine özgü commit-journal kesintisi aşağıdaki sonraki düzeltmeyle giderildi. Referans sorgusu ile fiziksel silme yarışı, müşteri DB/depo kabulü ve tam yol taramasının büyük verideki süresi açık. A-10 🔴 kalır; durum sayımı değişmez.

### 2026-10-06 — A-10 geçiş temizleme isteğini DB yazımından önce kaydetme

- 🟢 Yeni şifreli kopya geri okunduktan sonra eski/yeni yol çifti kalıcı journal'a yazılıyor; ancak bundan sonra kayıt yeni yola çevrilip DB'ye kaydediliyor. Böylece bu geçişte DB commit'i tamamlanıp süreç hemen kesilirse temizleme isteği önceden kayıtlıdır.
- 🟢 Worker yeni yol DB'de doğrulanana kadar, eski yolun durumundan bağımsız olarak isteği erteliyor ve eski dosyayı koruyor. Yeni yol kayıtlıysa, eski yola başka başvuru olup olmadığını denetleyip koruma veya silme kararı veriyor. Önceki v1 journal girdileri okunmaya devam eder. Web Release derlemesi **0 uyarı / 0 hata**; bu son değişiklik için çalışma zamanı testi yapılmadı.
- 🔴 DB yazımı başarısız olup eski yol korunursa istek güvenli biçimde bekleyebilir; bu tür bekleyen girdilerin işletim temizliği, diğer dosya silme akışlarının commit-kuyruk aralığı, sorgu-silme yarışı ve gerçek müşteri kabulü açık. A-10 🔴 kalır.

### 2026-10-06 — A-10 geri alınabilir EBYS ve araç dosya geçmişi

- 🟢 Geçiş önizlemesi ve uygulaması `IgnoreQueryFilters` ile soft-delete edilmiş EBYS/araç ana dosyalarını ve iki modülün dosya sürümlerini de kapsar. Önizlemede aktif ve silinmiş ana/sürüm kayıtları ayrı sayılır; yönetici ekranı kapsam metni güncellendi.
- 🟢 Araç dosyasında doğrudan `FirmaId` seçili firmayla eşleşmeli; eski/null firma alanında bağlı araç ve evrak ilişkisi üzerinden aynı firma doğrulanır. Uyuşmayan firma bağı geçiş dışında bırakılır. Web Release derlemesi **0 uyarı / 0 hata**; gerçek veriyle geçiş testi yapılmadı.
- 🔴 EBYS veri modelinde firma sahipliği yok ve bölüm yönetici genel kapsamındadır. Gerçek müşteri DB/storage kabulü, diğer dosya alanları, tam yol tarama performansı ve referans kontrolü-silme yarışı açık; A-10 🔴 kalır.

### 2026-10-06 — A-10 personel özlük ve fatura dosyalarının geçişi

- 🟢 Seçili firmaya ait personel özlük belgeleri ve sürümleri (silinmiş satırlar ve silinmiş sürücüler dahil) ile faturaların PDF/XML yolları geçişe eklendi. Faturada doğrudan `FirmaId`, personelde bağlı sürücünün `FirmaId` değeri açıkça doğrulanır.
- 🟢 Eski web yolu `/uploads/...`, `uploads/...` ve ters eğik çizgili biçimlerde okunur; personel özlükte eski tek dosya adı ortak yükleme kökünden taşınabilir. Önizleme modül gruplarını ve aktif/silinmiş adetleri gösterir. Web Release derlemesi **0 uyarı / 0 hata**; gerçek veriyle geçiş testi yapılmadı.
- 🔴 Destek/tedarikçi dosyalarının sahiplik ve kök eşlemesi, diğer yazım/silme akışları, EBYS firma sahipliği, büyük veri performansı ve gerçek müşteri kabulü açık. A-10 🔴.

### 2026-10-06 — A-10 fatura eski dosyalarında doğru depo kökü

- 🟢 Fatura PDF/XML eski yolları `AppStoragePaths.GetUploadsRoot(...)` altından okunur ve cleanup journal'ında ayrı, sabit bir `storage-uploads` kök anahtarıyla temsil edilir. EBYS/araç web upload'ları `wwwroot/uploads`, personel tek dosya adları ortak dosya servisi kökünde kalır. Aynı `/uploads/...` metni farklı fiziksel köklerle karıştırılmaz.
- 🟢 Cleanup worker her kökte yolu kendi sabit kökü içinde çözer; eski/yeni yol DB doğrulaması ve eski yolun başka kayıtlarda aranması korunur. Web Release derlemesi **0 uyarı / 0 hata**; `git diff --check` temiz. Bu turda çalışma zamanı testi veya müşteri dosyası taşınmadı.
- 🔴 Destek/tedarikçi kök ve sahiplik eşlemesi, EBYS firma sahipliği, diğer yazım/silme akışları, sorgu-silme yarışı, büyük DB performansı ve saha kabulü açık. A-10 🔴 kalır.

### 2026-10-06 — A-10 tenant bağı doğrulanan tedarikçi dosyaları

- 🟢 Tedarikçi eki migrasyonu eklendi. Sorgu soft-delete filtrelerini yok sayar; tedarikçi → cari → `Cari.FirmaId` zinciri seçili firmayı doğrulamadıkça dosya kapsam dışıdır. Aktif/silinmiş sayaçlar ekrana yansıtılır.
- 🟢 Yalnız ortak dosya servisinin kökünde bulunan eski tekil dosya adları taşınır. Yeni şifreli kopya doğrulaması ve kalıcı eski dosya temizleme akışı uygulanır. Web Release derlemesi **0 uyarı / 0 hata**; çalışma zamanı veya gerçek veri testi yapılmadı.
- 🔴 Destek eklerinin cari bağlantısız talepleri ve izin verilen `wwwroot/uploads/destek` altındaki mutlak eski yollar henüz güvenli şekilde taşınmıyor. Diğer tedarikçi yol biçimleri, büyük DB performansı ve müşteri kabulü de açık; A-10 🔴 kalır.

### 2026-10-06 — A-10 cari bağı doğrulanan destek ekleri

- 🟢 Destek taleplerine doğrudan bağlı ve yanıt ekleri seçili firmaya yalnız `DestekTalebi.CariId → Cari.FirmaId` eşleşmesiyle alınır; sorgu silinmiş talep/cari/ek kayıtlarını da kapsar.
- 🟢 Eski mutlak dosya yolu yalnız `wwwroot/uploads/destek` altındaysa taşınır. Cleanup journal göreli yolu tipli destek köküyle kaydeder; worker DB'deki eski yol referansını kontrol etmeden silmez. Ekranda aktif/silinmiş ek adetleri gösterilir. Web Release derlemesi **0 uyarı / 0 hata**; runtime/gerçek veri testi yapılmadı.
- 🔴 Cari bağlantısı olmayan destek talepleri, destek kökü dışındaki/eski formatı farklı yollar ve gerçek müşteri kabulü açık. Tedarikçi eski ortak kök dışı biçimleri, DB-silme yarışları ve performans da A-10'u açık tutuyor.

### 2026-10-06 — A-10 personel özlük silmesinde commit öncesi cleanup günlüğü

- 🟢 Personel özlük sürümü fiziksel silinecek veya aktif evrak yol alanı temizlenecekse cleanup isteği SaveChanges öncesinde dayanıklı günlüğe ekleniyor. İstek, DB yazımı istisna verse veya süreç kesilse de kaybolmuyor.
- 🟢 Yeni `WaitForReferenceRemoval` journal modu worker'ın DB başvurusu dururken isteği tamamlamasını engelliyor; başvuru devam ederse artan gecikmeyle yeniden deniyor. Paylaşılan/silinmiş geçmiş yolu saptanırsa koruma kararı isteği tamamlıyor. Release derlemesi **0 uyarı / 0 hata**; çalışma zamanı testi yapılmadı.
- 🔴 Bu sıra düzeltmesi yalnız personel özlük silme akışına uygulandı. Diğer DB-hard-delete dosya akışları, worker saha kabulü, referans kontrolü-unlink yarışı ve büyük envanter performansı açık; A-10 🔴 kalır.

### 2026-10-06 — A-10 EBYS dosya güncellemesinde commit öncesi cleanup

- 🟢 EBYS dosya güncellemesinde yeni dosya yolu DB'ye yazılmadan önce eski yol için `WaitForReferenceRemoval` journal isteği kalıcılaştırılıyor. Worker DB güncellemesi commit olmadan isteği alırsa eski yol başvurusu nedeniyle silmeyi tamamlamaz, yeniden dener.
- 🟢 SaveChanges sonucu belirsiz olduğunda DB'deki mevcut yol kontrol edilir; değişiklik eski yolda kaldıysa yalnız yeni dosya temizlenir ve eski yolun bekleyen isteği kaldırılır. Değişiklik commit olduysa eski yol isteği korunur; tarihçe/başka tablo referansı dosyayı tutar. Web Release derlemesi **0 uyarı / 0 hata**; runtime testi yapılmadı.
- 🔴 Commit öncesi günlükleme şu an personel özlük ve EBYS dosya güncellemesinde var. Diğer dosya yolu değiştirme/silme akışları, gerçek müşteri worker kabulü ve referans kontrolü-unlink yarışı açık; A-10 🔴 kalır.

### 2026-10-06 — A-10 fatura PDF/XML değiştirmede eski dosyayı güvenli temizleme

- 🟢 Fatura PDF ve XML değişiminde eski yol, DB `SaveChanges` öncesi kalıcı temizleme günlüğüne eklenir. Şifreli yollar DB referansı kalkana kadar yeniden denenir; legacy `/uploads/...` dosyaları doğru storage köküyle, yeni DB yolu doğrulandıktan sonra işlenir.
- 🟢 DB yazımı başarısız/belirsizse mevcut fatura yolu yeniden okunur. Eski yol hâlâ kayıtlıysa yeni dosya telafi edilir ve eski yolun isteği kaldırılır; yeni yol commit olduysa eski temizleme isteği korunur. Web Release derlemesi **0 uyarı / 0 hata**; çalışma zamanı/fatura verisiyle doğrulama yapılmadı.
- 🔴 A-10 kırmızı kalır: diğer dosya yolu değiştirme/silme akışlarının tamamı, gerçek müşteri DB/storage worker kabulü, referans sorgusu-unlink yarışı ve büyük envanter performansı açıktır.

### 2026-10-06 — A-10 yönetici arşiv geçişinde belirsiz commit telafisi

- 🟢 Yönetici arşiv geçişinde personel/araç dosyasının kopyalanması veya DB transaction sonucu hata verirse, yeni dosya silinmeden önce taze DB bağlamında hedef satırın yolu doğrulanır. DB yeni yolu gösteriyorsa işlem `Copied` sayılır; eski dosya kopya-güvenli strateji gereği korunur.
- 🟢 DB hedef yolu göstermiyorsa yeni kopya temizlenir. Doğrulama hatasında şifreli dosya silme servisi kalıcı cleanup günlüğüne başvuruyu ekler; referans kontrolü yapılamazsa dosya korunur ve worker tekrar dener. Web Release derlemesi **0 uyarı / 0 hata**; yönetici ekranında gerçek geçiş/commit hata enjeksiyonu yapılmadı.
- 🔴 A-10 kırmızı kalır: diğer dosya akışlarının tam envanteri, gerçek DB/storage worker kabulü, referans kontrolü-unlink yarışı ve büyük envanter performansı açık.

### 2026-10-06 — A-10 personel/araç arşiv dosyalarının atomik yazımı

- 🟢 `EvrakArsivService` şifreli personel/araç arşivini benzersiz geçici `.tmp.enc` dosyasına yazar ve tamamlanınca aynı klasörde son `.enc` adına taşır. İptal/yazım hatasında geçici dosya temizlenmeye çalışılır; tamamlanmamış içerik DB’ye döndürülen yol olarak yayımlanmaz.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Gerçek disk kesintisi/iptal çalıştırma testi yapılmadı.
- 🔴 A-10 kırmızı kalır: arşiv kopyası ile DB kaydı arasındaki süreç kesintisi yetim bırakabilir (yetim envanteriyle görünür), bütün dosya akışlarının runtime kabulü, TOCTOU ve büyük envanter performansı açık.

### 2026-10-06 — A-10 e-Fatura/Luca dosyalarında yeni yazımları şifreleme

- 🟢 GİB e-Fatura XML üretimi artık `wwwroot/efatura` yerine atomik şifreli depoya yazar; XML okuma şifreli yolu açar ve eski e-Fatura webroot yollarını sınırlandırılmış geriye dönük okuma olarak destekler. Luca’dan gelen yeni XML/PDF dosyaları da `wwwroot/belgeler/efatura` yerine şifreli depoya yazılır; kaydetme hatasında DB referansı denetlenerek cleanup kuyruğu kullanılır.
- 🟢 Fatura indirme yolu eski `/belgeler/efatura/...` ve `efatura/...` kayıtlarını güvenli `wwwroot` çözümlemesiyle okuyabilir. Yönetici dosya geçişi artık bu iki eski webroot yol ailesini de önizler, şifreli kopyayı doğrular, DB yolunu günceller ve yeni yol doğrulanınca referanssız eski açık dosya için dayanıklı temizlik isteği oluşturur. Web Release derlemesi **0 uyarı / 0 hata**; gerçek Luca/GİB bağlantısı veya eski müşteri dosyası geçişi çalıştırılmadı.
- 🔴 Var olan müşteri webroot dosyaları otomatik toplu taşınmadı; yönetici geçişi seçili firma ile çalıştırılmalı. ETTN/fatura ilişkilendirme ve canlı entegrasyon kabulü, hata enjeksiyonu, TOCTOU ve büyük veri performansı açık; A-10 🔴 kalır.



### 2026-10-07 — A-10 kalan yaşam döngüsü açıklarının kök düzeltmeleri

- 🟢 Eski düz e-Fatura/Luca çıktıları artık anonim statik dosya olarak sunulmaz; yeni GİB XML ve Luca XML/PDF üretimleri şifreli depodadır. Eski `wwwroot/efatura` ve `wwwroot/belgeler/efatura` yolları seçili firma migrasyonuna alındı; legacy `FileService` düz metin yazma ve doğrudan silme API'leri kaldırılıp salt-okunur yapıldı.
- 🟢 Dosya migrasyonu DB commit'inin gerçekleşmediğini taze bağlamla doğrularsa spekülatif şifreli kopyayı güvenli temizleme servisine verir ve eski-yeni yol bekleme isteğini tamamlar; böylece hiçbir zaman yazılmayacak yeni yolu bekleyen kalıcı kuyruk birikmez. Eski anahtar dosya kurtarması orijinali ezmeden geçici şifreli kopya üretip atomik değiştirir.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; `git diff --check` temiz. Bu turda test veya canlı müşteri geçişi çalıştırılmadı.
- 🔴 A-10 henüz kapanmaz: mevcut müşteri legacy dosyalarının firma bazlı geçişi/restore kabulü, aynı dosya yoluna eşzamanlı yeni DB referansı ile fiziksel silme arasındaki TOCTOU yarışı ve büyük DB referans taraması yükü saha kanıtı ister. Müşteri geçişi yapılmadan legacy açık dosyalar diskte korunur.

### 2026-10-07 — A-10 personel evrak sürümleme ve kaldırma tutarlılığı

- 🟢 Yeniden yüklemede geçmiş satırı artık yeni dosya yerine önceki yol/ad/tip/boyut bilgilerini tutuyor. Hızlı kaldırma akışı DB’de aktif yol ve dosya alanlarını gerçekten temizliyor; eski dosya aynı DB işleminde sürüm geçmişine bağlandığı için geri alınabilir ve fiziksel temizlik tarafından silinmiyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; canlı personel verisiyle geri alma kabulü yapılmadı.
- 🔴 A-10 açık: müşteri DB/storage geçiş/restore kabulü, genel TOCTOU, büyük DB tarama maliyeti ve modüller arası kalan silme sözleşmeleri.

### 2026-10-07 — A-10 soft-delete dosyalarının geri alınabilir tutulması

- 🟢 Araç evrakı, taşıma tedarikçisi eki ve EBYS dosyası soft-delete işlemleri fiziksel şifreli dosyayı artık silmiyor. DB kaydı geri alınabilir işaretli kaldığı sürece içeriği de geri alma için korunuyor. Hard purge ayrı ve açık bir operasyon olmalı.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Restore işlemi müşteri DB’sinde çalıştırılmadı.
- 🔴 A-10 açık: gerçek müşteri geçiş/restore kabulü, eşzamanlı referans-silme yarışı, büyük DB tarama maliyeti ve kalan hard-delete sözleşmeleri.

### 2026-10-07 — A-10 dosya yolu referans kontrolü ve indeksleme

- 🟢 `DosyaYolu`, `PdfDosyaYolu`, `XmlDosyaYolu` taşıyan 12 DB kolonu için model indeksi ve sağlayıcı bağımsız migration eklendi. Referans kontrolü artık her silmede DB’deki tüm dosya yollarını uygulamaya yüklemiyor; mutlak storage aliasları da eşleşme sorgusuna dahil. Windows harf/ayraç normalizasyonu DB tarafında yapılıyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; test çalıştırılmadı.
- 🔴 A-10 açık: hedef büyüklükte migration/index performansı, Windows case-normalized sorgu ve çoklu sunucuda eşzamanlı silme kabulü; müşteri dosyalarının geçiş/restore kabulü ve firma bağı olmayan legacy destek ekleri.

### 2026-10-07 — A-10 personel dosya yolu değişikliğini amaca özel hale getirme

- 🟢 Genel personel evrak güncellemesi artık dosya yolu yazmıyor; eski/detached form nesnesiyle temizlenmiş dosya başvurusunu geri getiremez. Bozuk dosya referansı için ayrı DB metodu eklendi; aktif dosya kaldırma ve eski yolu sürüme alma mevcut tek işlemde kalıyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**.
- 🔴 A-10 müşteri restore/geçiş kabulü, hedef DB hacim performansı ve çoklu sunucu kabulü bekliyor.

### 2026-10-07 — A-10 restore ve çoklu worker izole kabulü

- 🟢 İki bağımsız `FileCleanupJournal` örneği aynı paylaşılan depoda eşzamanlı claim yaptı; lease/file lock nedeniyle tek işçi kazandı. Test `Independent_workers_cannot_claim_the_same_due_file` Release paketine eklendi.
- 🟢 Tam model SQLite kabulinde soft-delete edilmiş dosya cleanup turundan sonra fiziksel olarak duruyor; kayıt geri açıldığında yol referansı doğrulanıyor ve dosya korunuyor. Release test paketi **32/32 başarılı** (0 başarısız).
- 🟡 Bu kanıt yerel SQLite ve aynı host dosya sistemiyle sınırlı. Gerçek müşteri DB/dosyalarıyla geçiş/restore, hedef hacimde migration ve performans ölçümü ile iki fiziksel sunucu/SMB paylaşım kabulü burada çalıştırılmadı. Bu yüzden A-10 genel kapanış iddiası verilmedi; bu saha kabulleri açıktır; kaynak incelemesi tamamlanmadan başka uygulama açığı olmadığı sonucuna varılmaz. Müşteri verisine yazım yapılmadı.
### 2026-10-07 — A-10 kuyruk sahipliği, eski dosya doğrulaması ve geçiş kabulü

- 🟢 **Eski worker sonucu:** Tamamlama lease kimliği, süresi ve istek revizyonuyla denetleniyor. Yeni isteğe eski tamamlamanın uygulanması ve aynı yol silinip yeniden kuyruğa alındığında eski isteğin yenisini kaldırması engellendi. Doğrudan silme, `WaitForReferenceRemoval` isteğini koruyor.
- 🟢 **Eski dosyanın güvenli temizliği:** Yeni DB yolunun varlığına ek olarak şifreli kopya yeniden çözülüyor ve kaynakla SHA-256 karşılaştırılıyor. Yeni kopya eksikse veya kaynak içeriği değişmişse dosya ve kuyruk kaydı korunuyor. Legacy kontrol bütün yol listesini belleğe almıyor. Yol boşlukları/ters ayraçlar ve Türkçe harfler için regresyon var; Unicode eşleşme gerektiğinde sabit bellekle akış halinde kontrol ediliyor.
- 🟢 **Gerçek servis akışı, izole veri:** Tam model SQLite üzerinde `DosyaMigrasyonService.OnizlemeAsync/MigrateAsync` çalıştırıldı. Soft-delete EBYS belgesi şifrelendi, eski kaynak temizlendi, satır geri açılıp aynı içerik çözüldü; tekrar geçiş sıfır işlem üretti. Bu bir müşteri yedeği restore'u değildir.
- 🟢 **Hacim probu:** Tek dosya tablosunda 100.000 sentetik satır; bu Windows makinesindeki SQLite ölçümü: doğrudan referans **2,7 ms**, normalize referans **28,0 ms**, referanssız yol **37,5 ms**. Tek çalıştırma ölçümüdür; hedef müşteri SLA'sı veya PostgreSQL performans kabulü değildir.
- 🟢 **Doğrulama:** Release test paketi **34/34 başarılı**. Web/Shared/test projesi test komutunda derlendi. Testler yalnız GUID ile ayrılmış geçici klasör ve SQLite verisi kullanır.
- 🔴 **Genel durum:** A-10'un yukarıdaki alt düzeltmeleri yeşildir. DB'ye yeni başvuru ekleme ile fiziksel silmeyi bütün servislerde ortak kilitle sıralama, firma/sahiplik kapsamı ve tabloda belirtilen saha kabulü tamamlanmadı. Önceki “yalnız üç saha kabulü kaldı” ifadesi bu yüzden düzeltildi. Diğer A görevleri bu testlerle kapatılmadı.
### 2026-10-07 — A-24/A-25 ortak cache nesli ve hata davranışı

- 🟢 Statik süreç içi key tracker/kilit yerine ortak `IDistributedCache` nesli getirildi. Her nesil benzersizdir; marker kaybolunca eski ad alanına geri dönülmez. Okuma sırasında nesil yeniden kontrol edilir; invalidation öncesi başlayan factory yeni nesli dolduramaz.
- 🟢 `RemoveAsync` ve `RemoveByPrefixAsync` tüm uygulama neslini değiştirir. Bu bilinçli geniş geçersizleştirme, farklı süreçlerdeki bilinmeyen anahtarları da kapsar; daha fazla cache miss yaratabilir. Eski nesil kayıtları TTL ile biter; sliding cache için ayrıca 24 saat üst sınırı var.
- 🟢 İptal cache miss'e çevrilmez. GET arızasında veri kaynağı kullanılır; invalidation arızası loglanıp çağırana iletilir. DB commit sonrası çağıranın hata politikası ayrıca geçerlidir; kalıcı retry/outbox bu değişiklikte yoktur.
- 🟢 Ortak memory backend ile bağımsız iki servis örneği, factory/invalidation yarışı, hata ve iptal için dört regresyon geçti. Güncel Release test paketi **38/38 başarılı**.
- 🟡 Gerçek Redis testi yapılmadı: Docker istemcisi mevcut ancak daemon named pipe bağlantısı başarısız. İki memory backend'e sahip ayrı sunucular ortaklaşmaz; çok sunuculu kullanım ortak Redis gerektirir. Ağ kesintisinde kaybolan invalidation ve diğer araç yazımlarının kapsamı nedeniyle A-24 🔴, A-25 🟡 kalır.

### 2026-10-07 — A-24 iş verisi önbelleğinde doğruluk sınırı

- 🟢 `CRMFilo:` anahtarlı üretim iş listeleri ve dashboard için cache okuma/yazma ve invalidation kullanılmıyor; her `GetOrSetAsync` veri kaynağını çalıştırıyor. Bu tercih, DB commit'i sonrası cache sunucusu kesilirse eski liste gösterilmesini ve başka yazım yollarının invalidation unutmasını önler. Jenerik cache davranışı diğer anahtarlar için korunur.
- 🟢 `AracService` araç listesini ayrıca doğrudan DB'den okur; firma seçimi sürümü kontrolü korunur. Excel aktarımının sonuç döndürme yolu cache kaldırma sonrasında düzeltildi.
- 🟡 Bedel her okumada DB sorgusudur. Hedef müşteri hacminde sorgu/yük ve çoklu sunucu kabulü yapılmadığından A-24 🟡, A-25 🟡 kalır; genel satış kabulü verilmez. Daha sonra cache yeniden açılacaksa DB transaction ile bağlı invalidation veya outbox gerekir.

### 2026-10-07 — A-10 şifreli dosyada geri alınabilir karantina

- 🟢 Referans denetiminden sonra yeni DB başvurusu eklenebilme penceresinde şifreli içerik artık `File.Delete` ile geri dönülmez biçimde silinmiyor. `SecureFileService` aynı depolama kökü altındaki `.deleted-file-quarantine-v1` alanına atomik taşıyor; normal dosya yolu sonradan referanslanırsa okuma, varlık ve kopyalama karantinaya geri düşüyor. Okuma/kopyalama sırasında taşıma olursa dosya bulunamadı durumunda tekrar deniyor.
- 🟢 Karantina `RecoveryArchive` tarafından yedeklenen `storage/uploads` ağacında. Salt okunur yetim tarayıcı bu tutulan dosyaları yeni yetim olarak saymıyor.
- 🟡 Web Release derlemesi 0 uyarı/0 hata. Bu turda karantina ve eşzamanlı taşıma için çalışma zamanı testi yapılmadı. Kalıcı saklama disk/yedek hacmini artırır; kapasite takibi gerekir. Legacy düz dosya fiziksel silme yolu ve `SecureFileService` dışından doğrudan yola erişen tüketiciler ayrıca incelenmelidir. A-10 🔴 kalır.

### 2026-10-07 — A-10 karantina eşlemesinde farklı kurulum yolu düzeltmesi

- 🟢 Karantina yolu artık sunucunun mutlak klasöründen değil, depolama köküne göre mantıksal yoldan türetilir. `.deleted-file-quarantine-v1` altında özgün dizin yapısı korunur; DB satırı kaybolsa da operatör dosyayı özgün yolla eşleyebilir. Aynı arşiv başka makine veya klasöre açıldığında eşleme değişmez; kök dışına çıkan yol reddedilir. Düz dosyanın yanlışlıkla şifreli karantinaya taşınması engellenir; mevcutsa eski dosya temizlik akışına yönlendiren görünür hata üretilir. Master key kurtarma varsayılan taraması karantinayı da kapsar. Disk sağlık kontrolü uygulama diski yerine yapılandırılmış depolama sürücüsünü ölçer.
- 🟡 Farklı makinede gerçek restore ve karantinadan okuma çalışma zamanı kabulü bu turda yapılmadı; A-10 🔴 kalır.

### 2026-10-07 — A-10 karantina ZIP hazırlama ve farklı kök kabulü

- 🟢 Yeni kalıcı regresyon testi şifreli içeriği kaydedip karantinaya taşır, özgün yoldan çözer, `RecoveryArchive.CreateAsync` ile gerçek kurtarma ZIP'i üretir ve `PrepareAsync` ile manifest/hash/key ring probunu doğrular. Hazırlanan arşivdeki karantina dosyası farklı depolama köküne aktarıldığında aynı mantıksal yoldan çözüldü.
- 🟢 Hedefli test **1/1**, tam Release test paketi **39/39** başarılı. Bu kanıt sentetik dosya ve yerel dosya sistemidir; müşteri dump'ı, farklı fiziksel sunucu/SMB veya PostgreSQL restore kabulü değildir.
- 🔴 Legacy düz dosya yarışı, karantina kapasite/purge politikası ve tabloda belirtilen müşteri kabulleri açık olduğundan A-10 rengi değişmez. (2026-10-09 kullanıcı kararıyla saklama süresiz/otomatik purge yok olarak sabitlenmiştir; kapasite kabulü ve müşteri kabulleri sürer.)

### 2026-10-07 — A-11 S3 hata sınıfları ve imza başlığı

- 🟢 `S3ObjectStorageService` AWS SigV4 `Authorization` değerini HTTP başlığına doğrulama atlayarak koyuyor; .NET'in bu sözdizimini reddetmesi nedeniyle istek gönderilmeden oluşan `FormatException` giderildi.
- 🟢 İndirme ve varlık sorgusunda yalnızca 404 eksik nesne olarak dönüyor. 403, 5xx ve bağlantı hataları çağırana iletiliyor. Silme 404 için idempotent, diğer hatalarda başarısız. Gerçekte imzalanmamış URL yerine açık `NotSupportedException` veriliyor; bu API için üretim çağıranı bulunmadı.
- 🟢 Hedefli HTTP durum testleri **2/2**, tam Release paketi **41/41** başarılı. Test sahte HTTP handler kullanır; gerçek S3/MinIO imzasının sunucuda kabulü ve UI/çoklu dosya davranışı doğrulanmadı. A-11 🟡 kalır.

### 2026-10-07 — A-11 yerel depo hata ayrımı

- 🟢 Yerel nesne deposunda `DownloadAsync` dosyayı doğrudan açıyor; yalnız `FileNotFoundException`/`DirectoryNotFoundException` eksik nesne olarak dönüyor. `ExistsAsync` öznitelik sorguluyor; izin ve I/O hataları `File.Exists` tarafından sessizce `false` sonucuna çevrilmiyor. İptal, okuma/varlık kontrolünden önce denetleniyor.
- 🟢 İmzalı URL desteklenmeyen yerel sağlayıcı artık boş bağlantı üretmiyor; açık `NotSupportedException` dönüyor. Hedefli yerel depo testleri **4/4**, tam Release paketi **42/42** başarılı.
- 🟡 Windows izin/kilit hatası, çoklu dosya kısmi hata ve ekran sonucu gerçek çalışma ortamında sınanmadı. A-11 🟡 kalır.
- 🟢 Araç, tedarikçi ve özlük evrakında başarılı kaldırma mesajı geri alınabilir saklamayı açıkça söylüyor. Web Release derlemesi **0 uyarı / 0 hata**. Ekran davranışı tarayıcıda ayrıca kabul edilmelidir.

### 2026-10-07 — A-12 eski araç aktarımının görünür sonucu

- 🟢 Firma veya modal değişimi, servis satır kaydetmeye başladıktan sonra gerçekleşirse eski aktarım sonucu yeni modala yazılmıyor; eklenen/güncellenen/hatalı satır sayısı önceki firma kimliğiyle uyarı olarak gösteriliyor. Kayıt varsa güncel firma listesi yenileniyor. Hata yolunda kısmi kayıt olasılığı açıkça bildiriliyor.
- 🟢 Aktarım sürerken yeni aktarım modalı açılmıyor. Tarayıcıdan açılan dosya akışı `await using` ile kapanıyor. Web Release derlemesi **0 uyarı / 0 hata**. Gerçek dosya ve tarayıcı yarışı çalıştırılmadığından A-12 🟡 kalır.

### 2026-10-07 — 31 görevin renk denetimi

- 🟢 A-08 ve A-22 kendi tanımlı teknik kapsamlarında yeşil kaldı; kaynak/izole kanıt, diğer görevlerin saha kabulü yerine kullanılmadı.
- 🔴 A-16 eski veri, yalnız sınırlı migration ön kontrolleri bulunduğu; kapsamlı envanter, kontrollü onarım ve öncesi/sonrası kanıtı bulunmadığı için 🟡 → 🔴 yeniden sınıflandırıldı.
- 🟡 A-01/A-02/A-04–A-07/A-09/A-11–A-14/A-17–A-21/A-24–A-27 maddelerinin kalan koşulları sürdüğü için renkleri korunuyor. 🔴 A-03/A-10/A-15/A-23/A-28 ve ⚪ A-29–A-31 renkleri de korunuyor. Toplam **2 yeşil / 20 sarı / 6 kırmızı / 3 beyaz**.
- Bu işlem kod veya müşteri verisi değiştirmedi; renkler mevcut yerel kanıt ve açık kapanış koşullarının yeniden denetimidir.

### 2026-10-07 — A-16 salt okunur eski veri ön envanteri

- 🟢 `MKFiloServis.DataSync inventory`, SQLite dosyasını `ReadOnly`, PostgreSQL'i `READ ONLY` transaction ile tarar. On kontrol: firma bağı, şase/plaka tekrarı, araç-cari, banka-hesap, fatura-cari ilişkisi, iki dönem snapshot'ı ve iki varsayılan şablon türü. Çıktıda sayı ve en çok 20 kimlik vardır; eksik şema `Complete=false` yapar.
- 🟢 Sentetik SQLite ve geçici PostgreSQL 17 kümesinde her biri **10/10 bulgu** verdi. Eksik şema raporu tamamlanmamış olarak işaretlendi; raporun SQLite kaynağının üzerine yazılması reddedildi. DataSync Release derlemesi **0 uyarı / 0 hata**. Geçici PostgreSQL sunucusu durduruldu.
- 🔴 Gerçek müşteri verisi taranmadı; bu 10 kontrol tüm ilişkileri kapsamaz ve otomatik onarım yapmaz. A-16 genel rengi 🔴, toplam sayım **2 yeşil / 20 sarı / 6 kırmızı / 3 beyaz** kalır.

### 2026-10-08 — A-16 şemadan tenant FK keşfi

- 🟢 Ön envanter, iki sağlayıcıda firma kapsam kolonu olan tablolar arasındaki `Id` foreign key ilişkilerini şemadan çıkarıp her kaynak tablo için tek sorguda firma uyuşmazlıklarını inceleme adayı olarak sayacak şekilde genişletildi. Bu yöntem yalnız tanımlı FK'leri kapsar; veri değiştirmez ve otomatik onarım önermez.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**. Sentetik SQLite DB'de farklı `FirmaId` değerli tek kolonlu FK bağlı satır eklendi; tarama `A16-TENANT-001` ile **1** inceleme adayı buldu. PostgreSQL denemesinde izole küme Windows'un loopback socket izni nedeniyle başlatılamadı.
- 🔴 FK tanımsız ve composite iş ilişkileri, finansal toplamlar, gerçek müşteri bulguları ve onarım/öncesi-sonrası kanıtı açık. A-16 kırmızı; toplam renk sayımı değişmedi.

### 2026-10-08 — A-16 SQLite foreign key ihlal taraması

- 🟢 Her foreign key tanımlı SQLite kaynak tablosu için `pragma_foreign_key_check` çalıştırılır; bu, composite FK ve örtük hedef anahtarı da kapsar. Sentetik SQLite DB'de tekli ve composite sahipsiz ilişkiler ayrı kontrol kodlarıyla **1'er** bulgu verdi.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**. Tarama salt okunurdur; `WITHOUT ROWID` tablolar için örnek kimliğin `-1` olabileceği raporda açıklanır.
- 🔴 Bu SQLite özelliği PostgreSQL için sahipsiz kayıt denetimi sağlamaz. Tenant kapsamı olmayan iş ilişkileri, gerçek müşteri taraması, finansal uzlaştırma ve kontrollü onarım/öncesi-sonrası kanıtı açık; A-16 🔴, toplam **2 yeşil / 20 sarı / 6 kırmızı / 3 beyaz** kalır.

### 2026-10-08 — A-29 mali kararlar ve A-30/A-31 kapsam kapanışı

- 🟢 A-29 kararları yazıldı; Excel/CSV banka içe aktarımında hem giriş hem çıkış doluysa ya da tutar/yön uyuşmuyorsa satır reddedilir. Kilitli veya muhasebe fişine bağlı maaş snapshot silme engellendi. Web Release derlemesi **0 uyarı / 0 hata**.
- 🟡 Güvenli mali yazımlarda rol değişikliğinin sunucu sınırında yeniden yetkilendirilmesi ve çalışma zamanı kabulü halen açık; A-29 sarı.
- 🟢 A-30 davranış değiştirmeyen P3 refactor'ı satış kapsamından erteleme kararıyla; A-31 çevrimdışı yok/Local/S3/backup sınırlarını belirleme kararıyla kapatıldı. Ayrıntı [A-29–A-31 karar belgesinde](A-29-31-URUN-KARARLARI.md).
- 🟢 Sayım **4 yeşil / 21 sarı / 6 kırmızı / 0 beyaz**. A-03/A-10/A-15/A-16/A-23/A-28 kırmızıları dış kabul veya temel iş kalemi içerdiğinden bu kararlarla kapanmış sayılmadı.

### 2026-10-08 — A-29 yetki sorgularında circuit eskimesi

- 🟢 `KullaniciService` yetki sorguları artık oturumda saklanan rolü Admin kararı için kullanmıyor; kullanıcı etkinliği, güncel rol ve izinler her çağrıda DB'den okunuyor. Pasif/silinmiş kullanıcı, rol ve izin kayıtları yetki alamıyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**.
- 🟡 Bu ortak sorguyu kullanmayan mali servis yazımları merkezi olarak korunmuş sayılmaz; yazım sınırlarının envanteri ve rol değişikliği çalışma zamanı kabulü tamamlanmalı. A-29 sarı kalır.
- 🟢 2026-10-08 regresyon doğrulaması: mevcut Release test paketi **42/42 geçti**; A-29 rol değiştirme senaryosu pakette henüz yok, dolayısıyla bu sonuç tek başına yetki kabulünü kapatmaz.

### 2026-10-08 — Çözüm geneli Release kabul/doğrulama koşusu

- 🟢 `dotnet test MKFiloServis.slnx -c Release --no-restore` çıkış kodu **0**. İçindeki `MKFiloServis.Tests` paketi **42/42 geçti**; çözümdeki Web, DataSync, LisansDesktop, Shared ve MAUI Client projeleri derlendi. Client Android ve Windows Release link/AOT adımları da hata vermeden tamamlandı.
- 🟢 Ayrı `MKFiloServis.PlaywrightSmoke` aracı Release'de **0 uyarı / 0 hata** ile derlendi.
- 🟡 Tarayıcı smoke akışı çalıştırılmadı: `CRMFILO_TEST_USER` ve `CRMFILO_TEST_PASSWORD` test kimlik bilgileri bu oturumda tanımlı değil. Gerçek test kullanıcısı olmadan uygulama oturumuna dair PASS sonucu üretilemez.
- 🟡 Bu koşu müşteri PostgreSQL/SQLite kurulumunu, IIS installer kurulumunu, gerçek S3/SMTP/Luca bağlantılarını veya gerçek DB ve dosya kurtarmasını çalıştırmadı. Bunlar ayrı test ortamı/credential gerektirdiğinden A-01–A-29 kabul durumu yalnızca bu çözüm koşusuyla değişmez.

### 2026-10-08 — A-29 banka hareketi UI yazım kapıları

- 🟢 Banka hareketi formu oluşturma/düzenleme sırasında sırasıyla `BankaHareketleriYaz`/`BankaHareketleriDuzenle`, silme sırasında `BankaHareketleriSil`, içe aktarma yansıtmasında `BankaHareketleriYaz` güncel DB yetkisiyle tekrar sorgulanıyor. Rol kaldırılmış, hesap pasif/silinmiş veya oturum kapanmışsa yazım servisi çağrılmadan işlem reddedilir.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; regresyon paketi **42/42 başarılı**.

### 2026-10-09 — A-16 banka yardımcı firma bağlantısı envanteri

- 🟢 Salt okunur DataSync envanterine beş sabit sorgu eklendi: banka hareketi–personel, araç, araç masrafı, mahsup/geri ödeme ve fatura ödeme eşleştirmesi–banka hareketi firma bağları.
- 🟢 Genişletilmiş geçici SQLite fixture `Complete=true`, toplam **7** bulgu verdi: A16-04 üç hatalı cari bağlantısını iki benzersiz araç olarak; A16-11–A16-15'in her birini de beklenen bir kayıt olarak buldu. Diğer sabit kontroller temiz kaldı; kaynak SHA-256 değişmedi.
- 🟢 Sınır durumunda A16-15 banka hareketi `FirmaId` boş/geçersizse de eşleşmeyi bulacak şekilde düzeltildi; minimal sentetik SQLite taramasında beklenen eşleştirme bulundu. Diğer zorunlu tablolar eksik olduğundan bu hedefli koşunun genel raporu `Complete=false` döndü.
- 🟢 A16-04, eski `INNER JOIN` nedeniyle kaçabilen sahipsiz kiralık/komisyoncu cari bağlantılarını `LEFT JOIN` ile buluyor; iki hatalı bağlantısı olan aynı araç tek bulgu sayılıyor.
- 🔴 Envanter 15 sabit kontrolü kapsasa da FK'siz/FirmaId'siz iş ilişkileri, gerçek müşteri bulgu değerlendirmesi ve yetkili kontrollü onarım/öncesi-sonrası kanıtı açık; A-16 🔴 kalır, görev renkleri/sayımı değişmez.
- 🟡 Bu düzeltme banka UI yollarını kapsar. Maaş, puantaj, fatura ve muhasebe yazımlarının tamamında ortak servis sınırı ve açık oturum rol değişimi UI kabul testi sürüyor; A-29 sarı.

### 2026-10-08 — A-29 maaş ve personel finans UI yazımları

- 🟢 Maaş oluşturma/düzenleme/silme/yeniden hesaplama, ödeme onay/iptali, avans ve borç ekleme/düzenleme/silme/ödeme işlemleri her olayda güncel DB izin sorgusu yapıyor. Veritabanı yetki sorgusu hata verirse işlem fail-closed reddedilir. İlk yüklemede otomatik üretilen kilitli maaş snapshot'ı için `MaasYaz` izni gerekir.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; mevcut Release regresyon paketi **42/42 başarılı**.
- 🟡 Bu UI kontrolleri merkezi servis sınırı değildir. Puantaj/fatura/muhasebe dış çağrıları ve rol değiştirme tarayıcı kabul testi kapsam dışı kaldığından A-29 sarıdır.

### 2026-10-08 — A-29 banka/personel ödeme mali ekranları

- 🟢 Güncel izin kontrolü banka hareketi formu/listesi/aktarımına ek olarak personele geri ödeme/iptal ve gelir-gider kayıt/düzenleme/silme olaylarına eklendi. Gelir-gider işleminde muhasebe fişi seçilmişse `MuhasebeFisleriYaz` ayrıca aranıyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**, otomatik Release test paketi **42/42 başarılı**.
- 🟡 Kontroller kullanıcı UI olaylarında. Puantaj ve kalan fatura/muhasebe servis yolları ile oturumda rol değişimi tarayıcı kabul testi henüz tamamlanmadı; A-29 sarı.


### 2026-10-08 — A-29 fatura servisinde güncel yetki denetimi

- 🟢 `CurrentPermissionGuard` servis sınırında oturum kimliğini HTTP/circuit üzerinden alır; etkin kullanıcı, güncel rol ve izinleri her çağrıda DB'den okur. Oturumsuz, pasif/silinmiş hesap veya silinmiş rol/izin ile yazım reddedilir; DB sorgu hatası yazımı durdurur. Oturumdaki Admin rol iddiası yetki kaynağı değildir.
- 🟢 `FaturaService` oluşturma, düzenleme, silme, Excel/XML/XML+PDF içe aktarma, PDF değiştirme, kalem güncelleme, stok kartlı kalem güncelleme, fatura eşleştirme, mahsup kapatma ve açık muhasebeleştirme girişlerinde güncel izin aranır. Genel fatura izni veya ilgili gelen/kesilen yön izni kabul edilir. Yön değiştirmede hem eski hem yeni yön, eşleştirme/mahsupta iki kayıt da denetlenir; kalemlerde izin DB'deki ana faturadan alınır.
- 🟢 XML+PDF içe aktarmada yeni faturanın PDF'i yazma izniyle eklenir; mevcut faturanın PDF'ini değiştiren dış servis çağrısı düzenleme izni ister. Açık muhasebeleştirme ayrıca `MuhasebeFisleriYaz` ister. `MuhasebeService.CreateFisAsync` doğrudan fiş oluşturmayı aynı güncel servis denetimiyle korur.
- 🟡 Türetilmiş ödeme toplamı (`UpdateOdenenTutarAsync`), otomatik/atomik muhasebe üretimi, hesap planı ve iç servislerin fiş düzenleme/silme çağrıları henüz bu ortak denetime taşınmadı. Bunların banka/masraf/hakediş iş yetkileriyle birlikte ele alınması gerekir; yalnız manuel muhasebe izni eklenerek bu iş akışları kapatılmış sayılmadı. Puantaj ve diğer mali servis/API girişleri de açık; A-29 sarı kalır.

### 2026-10-09 — A-16 banka hesap kimliği sınır kontrolü

- 🟢 A16-05'te eski `BankaHesapId > 0` filtresi boş, sıfır ve negatif hesap kimliklerini yok sayabiliyordu. Sorgu boş/geçersiz kimlik, sahipsiz hesap ve boş/geçersiz firma bağlarını bulacak şekilde genişletildi.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**. Release CLI, 15 sabit kontrolün tamamını içeren sentetik SQLite fixture'ında boş, `0`, `-4` ve bulunmayan `999` hesap kimlikli dört kaydı buldu; geçerli beşinci kayıt temiz kaldı. Rapor `Complete=true`, A16-05 `Count=4`, örnek kimlikler `101,102,103,104`, çıkış kodu `0`.
- 🔴 Bu sentetik test gerçek müşteri verisi taraması/onarımı veya PostgreSQL migration kabulü değildir; A-16 ve A-15 kırmızı, görev renk dağılımı değişmedi.
- 🟡 Bu değişiklik için çalışma zamanı rol kaldırma veya müşteri kabul testi çalıştırılmadı. Önceki 42/42 sonucu bu yeni servis değişikliğinin test kanıtı olarak kullanılmaz.
- 🟢 Son kodla Web Release derlemesi başarılı: **0 uyarı / 0 hata**. İlgili değişikliklerde `git diff --check` temiz. Bu sonuç çalışma zamanı kabulü değildir.


### 2026-10-08 — A-29 muhasebe düzenleme/silme ve masraf servisleri

- 🟢 Muhasebe fişi düzenleme, silme, onaylama ve onayı geri alma servis girişleri artık güncel DB izni arar. Düzenleme/onay işlemleri `MuhasebeFisleriDuzenle`, silme `MuhasebeFisleriSil` gerektirir. Hesap planı düzenleme/silme de kendi izinleriyle korunur.
- 🟢 Araç masrafı oluşturma/düzenleme/silme servis girişleri `AracMasraflariYaz/Duzenle/Sil` izni arar. Muhasebe fişi üretimi seçilmişse ayrıca `MuhasebeFisleriYaz`, bağlı fiş güncellenecekse `MuhasebeFisleriDuzenle`, bağlı fiş kaldırılacaksa `MuhasebeFisleriSil` gerekir. Oluşturma/düzenlemede bu izinler ilk masraf yazımından önce kontrol edilir; fiş düzenleme/silme servisi de izni yeniden denetler. Mevcut rollerde bu işlemleri yapacak kullanıcılara ilgili muhasebe izinleri verilmelidir.
- 🟢 Kolay muhasebe kaydetme servisi cari oluşturma dahil ilk yazımdan önce `MuhasebeFisleriYaz` iznini doğrular. Geri alma akışı korunan fiş silme servisini kullanır.
- 🟢 Manuel fiş oluşturma, fiş düzenleme ve `CreateFisAtomicAsync` ile fiş üretme borç/alacak toplamlarını kalemlerden yeniden hesaplar; fark 0,01'den büyükse kayıt reddedilir. Düzenlemede kontrol eski kalemler kaldırılmadan önce, atomik girişte fiş numarası üretilmeden önce yapılır.
- 🟡 Kalan kapsam: hesap planı oluşturmanın iç servis sözleşmesi, doğrudan atomik/otomatik muhasebe çağrılarının tümü, türetilmiş fatura ödeme toplamı, puantaj ve diğer mali servis/API girişleri. Çok servisli masraf/banka/muhasebe işlemleri tek transaction altında değildir; izin değişimi veya sonraki servis hatası nedeniyle oluşabilecek kısmi kayıt riski bu izin kontrolleriyle bütünüyle kapanmaz. A-29 sarı kalır.
- 🟡 Çalışma zamanı veya müşteri kabul testi bu değişiklik için çalıştırılmadı.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata** ile tamamlandı.


### 2026-10-08 — A-29 toplu muhasebe geri alma ve fiş bütünlüğü

- 🟢 `TopluGeriAlAsync` artık yalnız taslak fişi geri alır; onaylı veya iptal edilmiş fiş işlem görmeden reddedilir. Fiş ve kalemlerin fiziksel silinmesi kaldırıldı: taslak fiş soft-delete olur, kalemler inceleme/audit için korunur. Fatura/masraf bağlantısının kaldırılması ve fişin silinmiş işaretlenmesi aynı `SaveChanges` çağrısındadır.
- 🟢 Toplu fatura/masraf muhasebeleştirme `MuhasebeFisleriYaz`, toplu geri alma `MuhasebeFisleriSil` iznini hem başlangıçta hem her satır öncesinde DB'den denetler. İzin sorgusu hata verirse veya izin kaldırılmışsa döngü durur; daha önce başarıyla kaydedilmiş satırlar geri alınmış sayılmaz.
- 🟢 Üç toplu akışta başarısız satır sonrası EF tracking temizlenir. Böylece başarısız satırın bekleyen değişiklikleri sonraki satırın `SaveChanges` çağrısına taşınmaz. Bu, daha önce başka context'te commit olmuş işlemleri geri almaz; çok servisli transaction kapsamı A-09'da açıktır.
- 🟢 Otomatik fişlerin ortak özel kayıt yardımcısı da kalemlerden borç/alacak dengesini doğrular; fiş onaylama kaydı kalemleriyle yükler ve dengesiz fişi onaylamaz.
- 🟡 A-29'un doğrudan atomik/otomatik muhasebe servis yetkileri, hesap oluşturma/import, puantaj ve diğer mali girişleri ile çalışma zamanı doğrulaması halen açık. Bu değişiklik için test veya gerçek müşteri kabulü çalıştırılmadı; görev renkleri **4 yeşil / 21 sarı / 6 kırmızı / 0 beyaz** olarak korunur.
- 🟢 Web Release derlemesi: **0 uyarı / 0 hata**.


### 2026-10-08 — Test sırası ve hesap/cari/stok servis girişleri

- 🟢 Kullanıcı kararıyla test çalıştırmaları kod düzeltmeleri sonrasındaki son aşamaya taşındı. [Son aşama test planı](SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md) uygulama sırasını ve nihai kapsamı tanımlar.
- 🟢 Ekrandan varsayılan hesap planı oluşturma `HesapPlaniYaz` izni arar. Başlangıç hazırlığı, kullanıcı servis arayüzünde bulunmayan iç metoda taşındı; oturumsuz çağrı için ortak yetki atlama eklenmedi. DI'da somut servis ve arayüz aynı scoped örneğe bağlandı.
- 🟢 Hesap planı Excel yükleme hem ekleme hem güncelleme yapabildiği için `HesapPlaniYaz` ve `HesapPlaniDuzenle` izinlerini girişte ve kaydetmeden hemen önce denetler.
- 🟢 Kolay muhasebenin hızlı cari oluşturma girişleri `CarilerYaz`, hızlı stok oluşturma `StokKartlariYaz` izni ister. Aynı unvanlı mevcut cari aramasında firma filtresini kaldıran `IgnoreQueryFilters` kaldırıldı; global kod çakışması kontrolü ayrı kaldı.
- 🟡 Bu tur derleme/test çalıştırılmadı; yeni kodun nihai doğrulaması son aşama planındadır. A-29 diğer mali girişler, A-15 tüm ilişkiler ve A-07 test/CI kapsamı henüz kapanmadı. Görev renkleri değişmedi.


### 2026-10-08 — A-29 personel finans servis yazımları

- 🟢 `PersonelFinansService` avans/borç oluşturma, düzenleme, silme/iptal, mahsup, maaşa avans mahsup, borç ödeme, ödeme/mahsup kaldırma, toplu mahsup/ödeme ve ayar kaydetme girişlerinde güncel DB izni denetler. Oluşturma `MaasYaz`, düzenleme/ödeme/mahsup/ayar `MaasDuzenle`, avans ve ödeme/mahsup silme `MaasSil`; kalıcı borç silme `PersonelBorcSil` gerektirir. Toplu işlerin her gerçek yazımı da alt servis girişinden denetlenir.
- 🟢 Avans ve borç ödeme kaldırılırken bağlı muhasebe fişi fiziksel silinmez. Güncel `MuhasebeFisleriSil` izni ve taslak fiş durumu gerekir; fiş soft-delete edilir, kalemler korunur. Kalıcı borç silmenin bağlı fişlerinde de aynı kural geçerlidir. Onaylı/iptal edilmiş veya aktif firma kapsamında bulunamayan fiş işlemi durdurur. Borç silmenin ana kayıt sorgusundaki filtre atlaması kaldırıldı.
- 🟢 Tekil mahsup ve borç ödemesinde sıfır/negatif tutar reddedilir; bu girdilerle kalan avans/borç ters yönde artırılamaz.
- 🟡 Mevcut çok context'li otomatik fiş üretme/linkleme tek transaction değildir. Bağlı fişli kayıt düzenleme/iptalinin mali tutarlılığı ve silinmiş alt ödeme kayıtlarının kalıcı silme kapsamı ayrıca denetlenmeli. Doğrudan atomik muhasebe girişleri ve diğer maaş/puantaj/bütçe servisleri henüz bütünüyle korunmuş sayılmaz. A-29/A-09/A-15 genel renkleri değişmedi.
- 🟡 Kullanıcı planına uygun olarak bu tur derleme veya test çalıştırılmadı. Son aşamada personel finans izinleri, taslak/onaylı fiş kaldırma, negatif/sıfır tutar ve firma kapsamı senaryoları doğrulanacak.


### 2026-10-08 — A-29 bağlı fişli personel kayıtlarında tutarlılık

- 🟢 Avans/borç oluşturma ve düzenleme pozitif tutar ister. Düzenlemede avans tutarı mahsup edilenden, borç tutarı ödenenden küçük olamaz. Bağlı fiş veya işlenmiş bakiye varsa tutar/tarih ve ilgili mali alanlar değiştirilemez; açıklama gibi mali olmayan alanların düzenlenmesi korunur. İptal edilmiş kayıt düzenlenemez.
- 🟢 Avans/borç iptalinde aktif mahsup/ödeme geçmişi veya işlenmiş bakiye varsa işlem reddedilir. İşlem görmemiş kayıt varsa bağlı taslak fiş, muhasebe silme izniyle soft-delete edilir; kayıt iptali ve fiş değişikliği tek `SaveChanges` çağrısında kaydedilir. Onaylı/iptal fiş iptali engeller. Zaten iptal edilmiş kayda tekrar iptal notu eklenmez.
- 🟢 Mahsup geri alma, `PersonelAvansMahsup` kaynak kimliğine bağlı fişleri bulur. Taslak fişleri kalemleri korunarak soft-delete eder; avans bakiyesi ve mahsup kaydıyla aynı transaction'da kaydeder. Onaylı fiş veya eksik silme yetkisi bütün işlemi durdurur.
- 🟢 İptal edilmiş avansa yeni mahsup, iptal edilmiş borca yeni ödeme eklenemez.
- 🟡 Eşzamanlı düzenleme/ödeme/iptal için kilit veya concurrency sözleşmesi, otomatik kayıt+fiş+bağlantı üretiminin ortak transaction'ı ve eski tutarsız kayıt onarımı halen açık. Bu kaynak kontrolleri geçmiş müşteri verisinin düzeldiği anlamına gelmez. A-09/A-15/A-16/A-29 genel renkleri korunur.
- 🟡 Derleme ve test çalıştırılmadı; kullanıcı kararıyla son aşama planına bırakıldı.


### 2026-10-08 — A-09/A-29 personel kayıt ve otomatik fişte ortak transaction

- 🟢 Avans oluşturma, borç oluşturma, borç ödeme ve tekil avans mahsubu ortak `ExecuteFinanceWriteAsync` akışına taşındı. Her girişimde yeni context ve `Serializable` transaction açılır; kayıt/bakiye, fiş üretimi ve varsa fiş bağlantısı aynı context üzerinden kaydedilir. İşlem tamamlanmadan commit yapılmaz; fiş veya bağlantı yazımı hata verirse transaction dispose ile geri alınır.
- 🟢 Dört özel muhasebe yardımcısı ayarları da çağıranın context'inden okur; fiş için `CreateFisAtomicAsync(fis, context)` kullanır. Ayrı context'te fiş üretimi ve ayrı context'te bağlantı kaydı kaldırıldı. Mahsup fişi kaynak kimliğiyle aynı transaction'a dahildir; yeni bir fiş FK'sı eklenmedi.
- 🟢 Execution strategy'nin her girişiminde güncel yetki tekrar aranır. Başarısız girişimin ürettiği kayıt/fiş kimliği ve fiş navigation'ı yeniden girişten önce sıfırlanır/eski girdiye döner. Ödeme ve mahsup yeni context'ten yüklenen ana kayda bağlanır. Nihai hata halinde girdi kimlikleri de eski durumuna alınır.
- 🟡 Bu kod transaction sınırını düzeltir; bağlantı kopmasında commit sonucunun belirsizliği/idempotency, execution strategy'nin hedef DB ayarları ve eşzamanlı işlem sonuçları henüz doğrulanmadı. Diğer düzenleme/silme/maaşa toplu mahsup/bütçe/banka servisleri bu yeni ortak transaction'a taşınmış sayılmaz. A-09/A-29 genel durumları sarı kalır.
- 🟡 Kullanıcı planına uygun olarak derleme/test çalıştırılmadı. Son aşamada ilk kayıt, fiş yazımı, link yazımı ve commit kesintisi noktalarına hata enjeksiyonu ve eşzamanlı ödeme/mahsup senaryoları uygulanmalı.


### 2026-10-08 — A-09 personel commit belirsizliğinde tekrar yazma koruması

- 🟢 Ortak personel transaction yardımcısı commit başladığını kaydeder. Commit sonrası hata geçici sayılsa bile execution strategy yeni mali yazım başlatamaz; yeniden giriş kontrolü yazımdan önce durur.
- 🟢 Commit hatasından sonra bağımsız context ile ana kayıt kimliği, oluşturulma zamanı, ana ilişki/tutar ve varsa fiş bağlantısı denetlenir. Kayıt bu ölçütlerle görünüyorsa daha önce oluşturulan sonuç döndürülür. Kayıt doğrulanamıyorsa veya DB erişilemiyorsa işlem sonucu belirsiz olarak bildirilir; sessiz tekrar INSERT yapılmaz.
- 🟢 Belirsiz sonuçta üretilen kimlik korunur ve hata metninde kayıt numarası verilir. Dört oluşturma/ödeme/mahsup girişine kimliği bulunan nesneyi yeniden oluşturma koruması eklendi. Commit öncesi başarısız girişimler önceki reset/new-context retry akışını kullanır.
- 🟡 Bu koruma aynı servis çağrısı ve aynı girdi nesnesi içindir. Yeni nesne/yeni HTTP isteği, yeniden başlatma veya farklı sunucu üzerinden yeniden gönderime kalıcı idempotency anahtarı sağlamaz. DB zaman hassasiyeti ve sorgu filtreleri doğrulamayı engellerse sonuç başarı varsayılmaz. Hedef SQLite/PostgreSQL commit hata enjeksiyonu son aşamada yapılmalı. A-09/A-29 sarı kalır.
- 🟡 Kullanıcı planına uygun olarak derleme ve test çalıştırılmadı.


### 2026-10-08 — A-09/A-29 ödeme ve mahsupta kalıcı işlem kimliği

- 🟢 `PersonelBorcOdeme` ve `PersonelAvansMahsup` için `IslemKimligi` (32 karakter GUID) ve `IslemOzeti` (SHA-256) eklendi. Yeni form nesnesi kimliği bir kez üretir; aynı mantıksal isteğin yeniden gönderiminde kimlik korunmalıdır. Servis boş/geçersiz kimliği reddeder, özeti kendisi hesaplar.
- 🟢 Aynı kimlik ve aynı ebeveyn/istek içeriğinde mevcut kalıcı kayıt döner; bakiye tekrar değiştirilmez ve ikinci fiş üretilmez. Aynı kimlik farklı içerik veya kaldırılmış işlem için kullanılırsa reddedilir. Tekrar sonuç döndürme de güncel izin ve aktif firma kapsamındaki ana kayıt kontrolüne tabidir.
- 🟢 İki tabloya filtrelenmemiş benzersiz işlem kimliği indeksi eklendi; farklı süreç/sunucu aynı DB üzerinde aynı anahtarı ikinci kez yazamaz. Yarış/hata sonrası servis eşleşen kaydı tekrar sorgular; bulunmadığında başarı varsaymaz. İşlem özeti tutar, tarih, ödeme/mahsup şekli, banka, açıklama ve ilgili ana kaydı; borç ödemesinde muhasebe üretim tercihini kapsar.
- 🟢 Ödeme geçmişi bulunan borcun kalıcı silinmesi engellendi; silinmiş ödeme geçmişi de kapsanır. Böylece cascade silme tüketilmiş ödeme anahtarını kaybettiremez. Ödeme/mahsup soft-delete sonrası indeks anahtarı saklanır.
- 🟡 `20261008120000_AddPersonnelPaymentOperationKeys` migration'ı ve model snapshot güncellendi; migration hiçbir gerçek DB'ye uygulanmadı. Eski satırların yeni sütunları NULL kalır; geçmiş işlemler için sahte kimlik/kanıt üretilmez. Hedef PostgreSQL/SQLite migration ve eski şema hazırlık zinciri son aşamada doğrulanmalı. Şema yükseltilmeden bu yeni model mevcut DB üzerinde çalıştırılmamalı.
- 🟡 Kalıcı koruma aynı anahtarla ödeme ve mahsup tekrarına aittir. Farklı anahtar gönderimi, avans/borç oluşturma, maaşa otomatik toplu mahsup ve diğer mali servisler bu kapsamda kapanmaz. Yeni istek/profil/sunucu aynı mantıksal işlem için anahtarı korumalıdır. A-09/A-29 sarı; derleme/test kullanıcı kararıyla son aşamada.


### 2026-10-08 — A-09/A-29 avans ve borç oluşturmada kalıcı tekrar koruması

- 🟢 Avans ve borç modellerine işlem kimliği/istek özeti ile filtrelenmemiş benzersiz indeks eklendi. Oluşturma servisleri aynı kimlik ve içerikte mevcut kaydı döndürür; yeni kayıt/bakiye/fiş üretilmez. Farklı içerik, silinmiş veya iptal edilmiş kayıt kimliği reddedilir. Tekrar sonucu döndürürken `MaasYaz` güncel DB izni denetlenir.
- 🟢 Oluşturma tek seçili firma gerektirir. FirmaId boşsa seçili firma işlemden önce atanır; farklı firma veya tüm firmalar rapor modu oluşturmayı reddeder. Firma bilgisi istek özetindedir ve transaction girişinde tekrar denetlenir. Filtre atlanan tekrar sorgusu açıkça seçili FirmaId ve işlem kimliğiyle sınırlıdır.
- 🟢 İşlem kimliği taşıyan, ödeme geçmişsiz borç kaldırıldığında ana kayıt soft-delete edilir; tekrar koruması ve fiş kalemleri korunur. Eski anahtarsız ve ödeme geçmişsiz borçta mevcut fiziksel kaldırma akışı korunur. Maaş ekranı kaldırma onayı artık ödeme hareketlerinin kalıcı silineceğini söylemez.
- 🟡 `20261008121000_AddPersonnelCreationOperationKeys` migration ve model snapshot hazırlandı; gerçek DB'ye uygulanmadı. Önceki ödeme/mahsup migration'ından sonra çalışmalıdır. Eski satırların anahtarı/özeti NULL kalır. Derleme, migration provası ve test kullanıcı planıyla son aşamada.
- 🟡 Koruma aynı mantıksal istekte aynı işlem kimliği tutulduğu sürece geçerlidir. Yeni kimlik yeni işlem sayılır; maaşa otomatik toplu mahsup, bütçe/banka ve diğer mali zincirler bu kapsama alınmış sayılmaz. A-09/A-29 genel renkleri sarı kalır.


### 2026-10-08 — A-09/A-29 maaşa otomatik mahsup ve geri alma

- 🟢 Otomatik avans mahsubu maaş/avans bakiyeleri ve mahsup satırlarıyla aynı Serializable transaction içinde yürütülür. Maaş kimliğine bağlı kalıcı parti özeti ve avans bazında benzersiz işlem anahtarı kullanılır; tekrar çağrı mevcut parti toplamını döndürür. Yeni açıklama/tarih veya yeni açık avans ikinci otomatik parti açmaz. Avanslar aynı firma/personelle sınırlı, tarih ve kimlik sırasıyla işlenir.
- 🟢 Maaşa bağlı tekil mahsup firma/personel/ödeme durumu ve maaş kapasitesini denetler; maaşın Avans kesintisini aynı transaction içinde artırır. Mahsup kaldırma maaş kesintisini, avans bakiyesini, soft-delete kaydını ve varsa bağlı taslak fiş kaldırmayı aynı transaction içinde yürütür. Ödenmiş maaş, onaylı fiş veya tutarsız bakiye geri almayı reddeder.
- 🟢 Otomatik parti kısmen/tamamen geri alınırsa tekrar uygulanmaz; kayıtlar ve tüketilmiş anahtarlar korunur. Önceden başka/eski mahsup geçmişi olan maaşta yeni otomatik parti reddedilir. Gerekli yeni kesinti tekil mahsup akışından yapılmalıdır. Ekran bildirimi mevcut parti toplamını gösterir; tekrar çağrıyı yeni kesinti gibi sunmaz.
- 🟡 Önceki iki işlem anahtarı migration'ı gereklidir; bu oturumda uygulanmadı. Eski tekil mahsuplar maaş kesintisini güncellememiş olabilir; tutarsız eski kayıtlar otomatik düzeltilmez. Otomatik maaş mahsubunun muhasebe hesabı sözleşmesi ayrıca değerlendirilmelidir; nakit tahsilat fişi bu akışa eklenmedi.
- 🟡 Derleme/test/migration provası son aşamada; eşzamanlı iki sunucu, yanıt kaybı, geri alma, eski veri ve hedef sağlayıcı senaryoları henüz çalıştırılmadı. A-09/A-29 genel durumları sarı kalır; satış onayı verilmiş değildir.


### 2026-10-08 — A-09/A-29 maaş servis sınırının korunması

- 🟢 Maaş oluşturma/toplu oluşturma, düzenleme/ödeme/yeniden hesaplama ve kaldırma servisleri sırasıyla güncel `MaasYaz`, `MaasDuzenle`, `MaasSil` izinlerini denetler. Mali yazımlar taze context ve Serializable transaction içinde yürütülür; commit girişinden sonra otomatik yeniden yazma yapılmaz.
- 🟢 Maaş güncellemede eski `UpdatedAt` reddedilir; firma/personel/dönem ve silinme bilgisi değiştirilemez. Oluşturma/audit alanları istemci nesnesinden ezilmez. Mahsup geçmişi varsa Avans kesintisi doğrudan düzenlenemez; ödenmiş/kısmi ödenmiş maaşın hesap ve kesinti alanları korunur. Negatif ödeme tutarı reddedilir.
- 🟢 Maaş kaldırma soft-delete oldu. Ödeme işareti/tarihi veya aktif/silinmiş mahsup geçmişi bulunan maaş kaldırılamaz. Toplu kaldırma ve yeniden hesaplama seçilen tüm maaşların kapsamını kontrol eder; uygun olmayan kayıt varsa işlem topluca reddedilir. Yeniden hesaplama ödeme/mahsup geçmişini değiştirmez.
- 🟢 Maaş ödeme tarihi, durumu ve açıklaması tek servis çağrısı ve transaction içinde kaydedilir. Aynı tarih/açıklama tekrarında ikinci durum değişikliği yapılmaz; zaten ödenmiş maaşa farklı ödeme bilgisi reddedilir. Düzenleme üzerinden yeni ödeme işareti verilemez; mevcut ödeme iptali hesap alanları değiştirilmeden yapılabilir. Bu akış ödeme durumunu işaretler; banka/kasa hareketi veya ters muhasebe fişi ürettiği iddia edilmez.
- 🟢 Yeni maaşın FirmaId'si doğrulanan personelden atanır. Toplu oluşturmada farklı firma/eksik personel reddedilir; dönem mevcut kayıt kontrolü ve oluşturma aynı transaction içindedir.
- 🟡 Kod incelemesine göre düzenlendi; derleme/test son aşamada. Sağlayıcıda eşzamanlı maaş/mahsup/ödeme, commit belirsizliği, eski timestamp ve toplu rollback senaryoları henüz çalıştırılmadı. Yeni şema migration'ları bu oturumda uygulanmadı. Tüm mali servisler ve gerçek kabul kapsamı kapanmadığından A-09/A-29 sarı kalır.


### 2026-10-08 — A-09/A-29 banka/kasa yazım ve bağlantı koruması

- 🟢 Hareket oluşturma, transfer, cari mahsup ve personel geri ödeme girişlerinde güncel `BankaHareketleriYaz`; genel düzenlemede `BankaHareketleriDuzenle`; kaldırma/mahsup iptali/geri ödeme iptalinde `BankaHareketleriSil` servis düzeyinde denetlenir. Banka hesabı oluşturma/düzenleme/kaldırma ve firma atamasında ilgili hesap izinleri eklenmiştir. Hesap servisindeki kontrol, bu servise yönlendiren wrapper'ları da kapsar.
- 🟢 Genel hareket düzenleme ve kaldırma taze context ve Serializable transaction içinde yapılır. Eski UpdatedAt reddedilir; kaynak, mahsup bağlantısı, muhasebe fişi, geri ödeme durumu ve silinme bilgisi genel düzenlemeden değiştirilemez. Commit başladıktan sonra otomatik tekrar yazma yapılmaz; belirsiz sonuç başarı sayılmaz.
- 🟢 Muhasebe fişine, fatura eşleştirmesine, bütçe ödemesine, transfer/mahsuba, araç masrafına veya personel geri ödemesine bağlı hareket genel düzenleme/kaldırmadan korunur. Bağlantısız hareket kaldırma soft-delete oldu; önceki fiziksel eşleştirme/kayıt silme ve bütçe ödeme alanlarını sıfırlama genel kaldırma yolundan çıkarıldı. Kaynak işlem akışları ayrıca değerlendirilmelidir. Rent a Car için mevcut senkron güncelleme/kaldırma helper'ları transaction içinde korunur; diğer bağlantı kontrolleri bu akışta da geçerlidir.
- 🟢 Yeni harekette işlenmiş/silinmiş kayıt veya bağlı işlem kimliği taşınması reddedilir. Geçersiz yön/kaynak enum'u ve aktif olmayan banka hesabı reddedilir. Banka hesabı düzenleme/kaldırma/firma ataması Serializable transaction içindedir; hareket geçmişli hesabın para birimi, tipi ve açılış bakiyesi değiştirilemez. Firma ataması yalnız seçili firmaya ve firmasız hesaba yapılabilir; mevcut firmalı hesap başka firmaya taşınamaz, hareket firması uyuşmazlığı reddedilir.
- 🟡 Transfer/cari mahsup fişlerinin ayrı context'te üretimi ve hata yutma; mahsup iptalinin fişle atomikliği; personel geri ödemede kısmi seçim ve ortak ödeme iptali sözleşmesi henüz bu düzeltmeyle kapanmaz. Bu özel zincirler genel hareket korumasından ayrı açık iş olarak sürer.
- 🟡 Derleme/test kullanıcı planıyla son aşamada; gerçek DB transaction/yarış/rol değişimi ve kaynak akış uyumluluğu henüz çalıştırılmadı. A-09/A-29 genel renkleri sarı; 4 yeşil / 21 sarı / 6 kırmızı dağılımı değişmedi.


### 2026-10-08 — A-09/A-29 transfer ve cari mahsup atomik kaydı

- 🟢 Hesaplar arası transferde kaynak/hedef hesap sorgusu, kaynak bakiye kontrolü, iki hareket, karşı hareket bağlantısı, muhasebe fişi ve iki hareketin MuhasebeFisId bağlantıları tek context/Serializable transaction içinde yürütülür. Cari mahsupta hareket, cari alt hesap hazırlığı, muhasebe fişi ve fiş bağlantısı aynı transaction içindedir. Ayrı context'te fiş yazma ve fiş hatasını yutup başarı dönme kaldırıldı.
- 🟢 Transfer hesapları aktif, aynı firma ve para biriminde olmalıdır. Cari mahsupta cari/hesap firması eşleşir; iki işlem de tek seçili firma gerektirir. Eksik muhasebe eşleştirmesinde işlem reddedilir ve hareketler kaydedilmez. Bu nedenle muhasebe hesabı hazırlanmamış kurulumda transfer/cari mahsup kullanımı için önce hesap planı/eşleştirme tamamlanmalıdır.
- 🟢 Hareket numarası aynı transaction bağlantısında üretilir; SQLite yazım transaction'ı içinden ayrı bağlantıyla ikinci numara yazımı yapılmaz. Ortak fiş sayacı PostgreSQL/SQLite için sağlayıcı komutu ve mevcut transaction üzerinden çalışacak şekilde düzenlendi; desteklenmeyen sağlayıcı reddedilir. Mevcut muhasebe modelinde fiş/hesap planı FirmaId taşımaz; muhasebe fiş sayacının mevcut ortak kapsamı burada değiştirilmedi. Hareket numaraları firma bazındadır.
- 🟢 Transfer/cari mahsup fişi yardımcıları güncel banka yazım iznini denetler, kalıcı hareket kimliği/yön/tutar/hesap uyuşmasını kontrol eder ve mevcut kaynak fişi varsa yeniden üretmez. Dışarıdan transaction verilmezse yardımcı kendi Serializable transaction'ını açar. Cari hesap hazırlığı sorguları aynı context'e taşındı; üst hesap ve alt hesap değişikliği aynı işlemde kaydedilir.
- 🟡 Yeni bir transfer/cari mahsup isteğinin yeniden gönderiminde kalıcı istek kimliği henüz yoktur; aynı hareketin fiş tekrarını önlemek, yeni istekten ikinci hareketi engellemek anlamına gelmez. Commit sonrası belirsiz sonuç otomatik yeniden yazılmaz; kullanıcı yeni istek göndermeden listeden kontrol etmelidir. Mahsup iptali/ters fiş atomikliği ve ortak personel geri ödeme iptali ayrıca açıktır.
- 🟡 Derleme ve test son aşamada; PostgreSQL/SQLite sayaç, eşzamanlı bakiye, fiş hata/rollback ve karşı hareket bağlantı senaryoları henüz çalıştırılmadı. A-09/A-29 sarı ve genel görev renk dağılımı korunur.


### 2026-10-08 — Ertelenen derleme ve otomatik testlerin çalıştırılması

Kullanıcının bu oturumda verdiği test talebiyle son aşama kontrolleri başlatıldı. 🟢 Çözüm Release derlemesi **0 hata / 0 uyarı**; 🟢 genişletilmiş otomatik paket **54/54 başarılı, 0 atlanan**. Yeni 12 regresyon senaryosu SQLite sayaç/transaction rollback, dört işlem anahtarı migration tablosu, güncel DB yetkisi ve maaş servis yazımlarını kapsar. Test fixture ilişki/HttpContext hataları düzeltilerek son paket yeniden çalıştırıldı. [Komut, kanıt ve kapsam raporu](TEST-DOGRULAMA-2026-10-08.md).

⚪ Tarayıcı testi için yapılandırılmış test hesabı yok; PostgreSQL hazır olma kontrolü kabul testi sayılmadı. Tam migration zinciri, gerçek müşteri restore, S3, çok sunucu, hedef hacim, transfer/iptal/geri ödeme zinciri ve mali hata enjeksiyonu açık. Geçmiş bölümlerdeki “derleme/test ertelendi” ifadeleri o çalışmanın tarihsel durumudur; bu koşunun kapsadığı kontroller artık yukarıdaki sonuçla günceldir. A-09/A-29 genel durumu 🟡 kalır; genel dağılım 4 yeşil / 21 sarı / 6 kırmızıdır.


### 2026-10-08 — İkinci doğrulama: banka transaction ve otomatik mahsup

🟢 Güncel Release çözüm derlemesi **0 hata / 0 uyarı**; otomatik paket **60/60 geçti, 0 atlandı**. Transfer/cari mahsupta başarı ve fiş hata enjeksiyonu sonrası tam rollback; karşı hareket/fiş FK'ları, bakiye, cari alt hesap rollback'i; otomatik maaş mahsubunda tekrar/geri alma doğrulandı.

🟢 Testin yakaladığı SQLite şema yardımcısının DbContext bağlantısını dispose etmesi düzeltildi. Şema yardımcısını gerçekten çalıştıran servis testleri tekrar geçti. 🟢 Ayrı PostgreSQL 17 cluster'ında uygulamanın numara sayacı, transaction rollback, 20 eşzamanlı numara ve dört işlem anahtarı tablosunun yeni migration/benzersiz indeks kontrolleri geçti. Geçici sunucu durduruldu; mevcut müşteri DB'sine yazılmadı. [Güncel kapsam ve kanıt](TEST-DOGRULAMA-2026-10-08.md).

⚪ Tarayıcı hesabı, tam PostgreSQL mali servis ve geçmiş migration zinciri, gerçek restore/S3/çok sunucu/hedef hacim; 🟡 iptal/ters fiş ve personel geri ödeme zinciri hâlâ açık. A-09/A-29 genel renkleri sarı; önceki 54/54 ilk koşu sonucu olup son paket sonucu 60/60'tır.

### 2026-10-08 — Mahsup iptali ve ters fiş bütünlüğü

- 🟢 Transfer/cari mahsup iptalinde ters fiş ve banka hareketlerinin soft-delete işlemi aynı Serializable transaction/context içinde kaydedilir. Hata yutma ve finansal geçmişin fiziksel silinmesi kaldırıldı; ters fiş yazılamazsa banka bakiyesi ve hareketler korunur.
- 🟢 Eski onaylı fiş defterde korunur; onaylı ters fişle net etki sıfırlanır. Eski fişi rapor dışında bırakıp yalnız ters fişi hesaba katma hatası giderildi. Bu kaynak fişleri ve ters kayıtlarının manuel düzenleme, silme, onay ve onay geri alma girişleri reddedilir.
- 🟢 Güncel silme izni ve seçili firma kontrolü; karşı hareket bağlantısı, grup bütünlüğü, tutar/fiş uyuşması ve bağlı ödeme koruması eklendi. Tekrar iptal yeni ters fiş üretmez; kullanıcıya grubun aktif olmadığı bildirilir. Eksik fiş bağlantılı eski kayıtlar otomatik tahminle iptal edilmez, veri onarımı gerektirir.
- 🟢 Altı yeni SQLite regresyon senaryosu: transfer/cari iptal başarısı, her iki akışta ters fiş hata enjeksiyonu/rollback, eksik transfer bağlantısı ve tutar uyuşmazlığı. Başarı senaryoları tekrar iptal reddini, geçmiş satırlarının korunmasını, banka bakiyesini, hesap bazında net sıfırı ve manuel fiş değişikliği retlerini de denetler.
- 🟢 Release çözüm derlemesi **0 hata / 0 uyarı**; otomatik paket **66/66 başarılı, 0 atlanan**. [Kanıt ve kapsam](TEST-DOGRULAMA-2026-10-08.md).
- 🟡 A-09/A-29 genel durumu korunur: yeni transfer isteği için kalıcı istek kimliği, ortak personel geri ödeme iptali ve tam PostgreSQL mali servis kabulü açık. Gerçek müşteri restore/S3/çok sunucu/hedef hacim kabulü bu SQLite sonucu ile kapatılmadı. Önceki 54/54 ve 60/60 sonuçları tarihsel koşulardır; bu koşunun son sonucu 66/66'dır.

### 2026-10-08 — Personel geri ödeme ve ortak iptal bütünlüğü

- 🟢 Personel geri ödeme oluşturma ve iptal yazımları ortak `WriteBankAsync` sınırına taşındı: her denemede yeni context, güncel izin, Serializable transaction ve commit başladıktan sonra otomatik tekrar yazmama koruması. Hareket numarası ödeme transaction'ında üretilir; ayrı hesap/numara context'i kaldırıldı.
- 🟢 Seçilen masrafların tamamı aynı firma/personelin geçerli ve ödenmemiş kayıtları olmalıdır. Eksik/geçersiz seçimde yalnız geçerli alt küme ödenmez. Ödeme hesabı aynı firmada aktif olmalı; masraf/ödeme para birimleri eşleşmelidir. Kaynak kayıtların kapanışı ve banka çıkışı birlikte kaydedilir; aynı ödenmiş kayıtları yeniden ödeme reddedilir.
- 🟢 Ortak banka ödemesindeki bir masrafın iptali, ödemeye bağlı tüm masrafları birlikte yeniden ödenecek duruma alır ve banka çıkışını soft-delete eder. Tek kaydı açıp ortak çıkışı tam tutarla bırakma hatası giderildi. Ödeme tutarı ve geçmiş banka satırı korunur. Hesapsız ödeme işareti yalnız seçili kayıtta geri alınır.
- 🟢 Tutarsız toplam/bağlantı, gizli veya silinmiş bağlı kayıt ve fiş/eşleştirme/bütçe ödemesi olan banka çıkışı iptal edilmez. Fişe bağlanmış ödeme için otomatik ters fiş oluşturulmuş sayılmaz; kaynak akıştan çözülmesi gerekir.
- 🟢 Ekranın iptal izni servisle eşleştirildi; ortak iptalin kapsamı onay mesajında açık. Genel düzenlemede ödeme durumu/tarihi alanı kaldırıldı, personel seçimi salt okunur oldu; ödenmiş veya kaynak masraf/fiş bağlantılı kayıt için düzenleme kapalı. Düzenleme nesnesine `UpdatedAt` ve korunan bağlantılar taşındı.
- 🟢 Dokuz yeni SQLite regresyon senaryosu geçti: ödeme başarısı/hata rollback'i, ortak iptal başarısı/hata rollback'i, eksik seçim reddi, fişli ödeme iptal reddi, hesapsız işaret/iptal, para birimi ve ortak toplam uyuşmazlığı. Başarı kontrolleri tekrar ödeme/iptal reddini, ödeme bağlantılarını, banka bakiyesini ve fiziksel satırların korunmasını da içerir.
- 🟢 Release çözüm derlemesi **0 hata / 0 uyarı**; otomatik paket **75/75 başarılı, 0 atlanan**. [Kanıt ve kapsam](TEST-DOGRULAMA-2026-10-08.md).
- 🟡 A-09/A-29 genel renkleri korunur. Yeni transfer isteği için kalıcı istek kimliği, tam PostgreSQL mali servis/commit belirsizliği/eşzamanlılık doğrulaması ve saha kabulleri açık. Tarayıcı etkileşimi bu koşuda çalıştırılmadı. Önceki günlükte açık olan ortak personel geri ödeme iptalinin yukarıdaki teknik kapsamı artık tamamlandı; önceki 66/66 sonucu tarihsel koşudur.

### 2026-10-08 — Transfer/cari mahsup tekrar istek koruması

- 🟢 Transfer ve cari mahsupta geçerli, boş olmayan GUID işlem kimliği zorunlu; kimliksiz çağrı yeni kayıt üretmez. Kimlik normalize edilir; firma, işlem türü, hesap/cari, yön, tutar, tarih ve metin alanları SHA-256 içerik özetiyle bağlanır.
- 🟢 Aynı kimlik ve aynı içerik mevcut hareket/grup/fişi döndürür; farklı içerik, iptal veya tutarsız fiş/karşı hareket bağlantısı yeni kayıt üretmeden reddedilir. Transferde yalnız ana hareket kimliği tüketir; karşı hareket tekrar anahtar taşımaz. Silinmiş kayıt kimliğini korur.
- 🟢 `BankaKasaHareketleri` için nullable `IslemKimligi`/`IslemOzeti`, benzersiz kimlik indeksi, model snapshot ve `20261008122000_AddBankOperationKeys` migration'ı eklendi. Eski NULL kimlikli satırlar değiştirilmez. Genel hareket oluşturma/düzenlemeden kimlik değiştirme engellenir.
- 🟢 Mahsup ekranı bekleyen kimliği firma/işlem türüne göre `sessionStorage` içinde saklar; form açma ve sekmede yenileme sonrası aynı kimlik korunur, başarılı cevap alındıktan sonra temizlenir. Kaydetme sürerken ikinci çağrı engellenir. Tarayıcı saklama alanı erişilemezse işlem başlatılmaz. Kapanmış sekme/yeni tarayıcı oturumu aynı kimliği kendiliğinden taşımaz; yeniden ödeme öncesi liste kontrolü gerekir.
- 🟢 Bütçe servisinin cari mahsup çağrısı bütçe kaydı ve firmadan türetilen sabit kimliği gönderir. Böylece aynı bütçe satırının cari mahsup tekrarında ikinci banka hareketi üretilmez. Bütçe satırını banka hareketine bağlayan yazım hâlâ ayrı context/işlem sınırındadır; bu değişiklik o bütünlük riskini kapatmaz.
- 🟢 Sekiz yeni SQLite senaryosu geçti: transfer/cari aynı kimlik tekrarı, farklı içerik/tür reddi, iptal edilmiş kimliğin tüketilmiş kalması, fiş hatası/rollback sonrası aynı kimlikle başarı; kimliksiz/geçersiz kimlik reddi ve yeni migration'ın eski NULL satır/soft-delete benzersizlik kontrolü. Release çözüm derlemesi **0 hata / 0 uyarı**, paket **83/83 başarılı, 0 atlanan**. [Kanıt ve kapsam](TEST-DOGRULAMA-2026-10-08.md).
- 🟡 Yeni migration müşteri DB'sine uygulanmadı; dağıtımda bu migration'ın uygulanması gerekir. PostgreSQL yeni banka migration'ı/mali servis/eşzamanlılık, tarayıcı sessionStorage etkileşimi ve bütçe kayıt+hareket atomikliği ayrıca doğrulanmalıdır. A-09/A-29 genel durumu sarı; yukarıdaki tekrar istek teknik kapsamı yeşildir. Önceki 75/75 sonucu tarihsel koşudur.

### A-15 / SQLite dashboard şeması — 2026-10-08

🟢 Finans hareketleri için banka/karşı hareket/geri ödeme kaynak bağlarının firma eşleşmesi EF kaydetme sınırında doğrulanır; altı sync/async çapraz-firma regresyon senaryosu eklendi. Fatura ve banka hareketini bağlayan `OdemeEslestirme` kayıtlarında aynı firma zorunluluğu hem sync hem async kayıtta kontrol edilir; dört pozitif/negatif SQLite senaryosu geçti. SQLite dashboard hatasının kök nedeni olan tüm bekleyen migration'ları uygulandı diye kaydetme davranışı kaldırıldı. Eksik `BankaKasaHareketleri.IslemKimligi` ve `IslemOzeti` sütunları ile benzersiz indeks idempotent ve veri koruyucu biçimde açılışta onarılır. Üçüncü SQLite regresyon testi dashboard son hareket sorgusunun onarım sonrası çalıştığını doğrular. Son Release çözüm derlemesi 0 hata/uyarı; paket 96/96 başarılı. İzole SQLite test kanıtı `TEST-DOGRULAMA-2026-10-08.md` dosyasındadır.

🟡 A-15 genel görev durumu değişmedi: tüm varlık ilişkilerinin envanteri, müşteri eski verilerinin denetimi ve hedef DB/migration kabulü hâlâ tamamlanmalıdır. Gerçek müşteri verisine müdahale yapılmadı.

### 2026-10-08 — A-23 kaynak ve kurulum teslim temizliği

- 🟢 [A-23 teslim kararı](A-23-TESLIM-KARARI-2026-10-08.md) geçerli raporları ve tarihsel belgelerin Git geçmişindeki yerini sabitler. Mevcut satış durumu 31 görevlik envanterle izlenir.
- 🟢 Yerel `dbsettings.json`, `portalsettings.json` ve `backup_settings.json` korunarak Git takibinden çıkarıldı. Web publish ve ana/müşteri/güncelleme kurulum girdileri bu dosyaları dışlar; `setup/build.ps1` eski payload'da bulursa durur. Release Web publish çıktısında bu dosyalar, çerez ve `.db` bulunmadı. PowerShell betiği parser denetiminden geçti.
- 🟡 Gerçek Inno EXE ve hedef kurulum A-21; eski kimlik bilgisi rotasyonu A-06 kapsamında. A-23 🟢 kapandı. Güncel sayım **5 yeşil / 21 sarı / 5 kırmızı / 0 beyaz**; satış kabulü verilmedi.

### 2026-10-08 — GitHub CI kök düzeltmeleri

- 🟢 Gömülü PostgreSQL audit SQL dosyası genel `*.sql` ignore kuralından dar istisnayla çıkarılıp Git'e eklendi. Linux Tests/Docker ve Windows CodeQL'in önceki eksik kaynak derleme hatası giderildi.
- 🟢 NuGet audit Windows/MAUI hedeflerine uygun Windows 2025 çalıştırıcısında restore ve doğrudan/geçişli taramayı başarıyla tamamladı; bilinen açık raporlanmadı. GitHub Linux Tests Release çalışması **102/102 geçti, 0 atlanan**. [Ayrıntılı CI kanıtı](A-07-CI-DOGRULAMA-2026-10-08.md).
- 🟢 Docker imajı oluşturuldu, GHCR'ye gönderildi, aynı digest Trivy ile tarandı ve SARIF GitHub Security'ye yüklendi.
- 🟢 Windows tam çözüm derlemesi ve CodeQL C# analizi GitHub'da başarıyla tamamlandı.
- 🟡 Gerçek lisans, müşteri DB/restore ve saha kabulü açık; A-07 🟡, A-22 🟢 ve genel renk dağılımı değişmedi.

### 2026-10-08 — A-29 banka hesabı oluşturma firma/izin sınırı

- 🟢 Banka hesabı oluşturma, güncel yazma iznini tekrar denetleyen Serializable `WriteAccountAsync` sınırına alındı. Seçili firma olmadan veya çağrıda başka firmanın kimliği verilerek hesap açılması reddedilir; firma boşsa seçili firma atanır.
- 🟢 İki odaklı SQLite regresyonu geçti: yabancı firmaya kayıt/yarım satır oluşmaz, seçili firmaya kayıt yapılır; veritabanında Admin rolü kaldırılan oturum yeni hesap açamaz. Release hedefli test derlemesi başarılıydı.
- 🟡 Puantaj ve diğer otomatik mali yazımların atomiklik/yetki incelemesi ile gerçek rol değişimi ve müşteri veritabanı kabulü açık. A-29 genel rengi değişmedi.

### 2026-10-09 — A-29 puantaj finans snapshot firma ve transaction sınırı

- 🟢 Finansal snapshot üretimi seçili firma zorunluluğu ile sınırlandı; dönem uygunluğu ve yeni satırlar aynı Serializable transaction içinde işleniyor. Üretilen satır açık `FirmaId` taşıyor. Önceden silinmiş satır da benzersiz anahtar çakışmasına yol açmaması için tekrar oluşturulmuyor.
- 🟢 Finansal kayıt listesi, fatura uygunluğu, tekil fatura üretimi ve hakediş işleme çağrılarında seçili firma kapsamı açıkça uygulanıyor; hakediş cari eşleşmesi firma bazında sınırlandı. SQLite regresyonu yabancı dönemi reddetme, yerel firma atama ve tekrar çağrı davranışını doğruladı. Release Web derlemesi 0 hata/0 uyarı; test paketi **105/105 geçti**.
- 🟡 Fatura oluşturma/kalem/link ve hakediş fatura/snapshot zinciri hâlâ birden çok DbContext/commit kullanıyor. Bu atomiklik, diğer servis yetki sınırları ve gerçek müşteri kabulü açık; A-29 rengi sarı kalır.

### 2026-10-09 — A-10 depolama kapasitesi göstergesi eşik uyumu

- 🟢 Sistem Sağlığı ekranı artık servis eşiklerini tutarlı gösterir: %80 üzeri uyarı, %90 üzeri kritik; erişilemeyen depolama sürücüsü de yeşil gösterilmez ve hata mesajı görünür.
- 🟡 Harici alarm teslimi ve kapasite artışı/müdahale prosedürü bu kod değişikliğinin kapsamında değildir; gerçek depolama hacminde kabul edilmelidir. A-10 kırmızı kalır.
