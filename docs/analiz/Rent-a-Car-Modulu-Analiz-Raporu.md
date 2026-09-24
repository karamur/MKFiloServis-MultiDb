# Rent a Car Günlük Kiralama ve VIP Modülü — Analiz Raporu

**Belge türü:** Ürün / iş gereksinimleri analizi (PRD seviyesi)  
**Kaynaklar:** `Rent_a_Car_Gunluk_ve_VIP_Ihtiyac_Analizi.pptx` ve `Rent_a_Car_Gunluk_ve_VIP_Ihtiyac_Analizi.docx`  
**Kaynak belgelerdeki analiz tarihi:** 24.09.2026  
**Hedef:** Mevcut MKFiloServis uygulamasına günlük/uzun dönem araç kiralama ve VIP hizmet süreçlerini yeni bir modül olarak kazandırmak  
**Revizyon:** 3 — yeni modül katılımı, modüller arası entegrasyon ve yayın bağımlılıkları genişletilmiştir.  
**Kapsam sınırı:** Bu rapor masaüstü/web ERP ilk sürümünü ele alır. Kaynak DOCX bölüm 4 ve 10 ile PPTX slayt 11'de mobil teslim/iade; DOCX bölüm 4'te çevrimdışı taslak/senkronizasyon istenmektedir. Bunlar kaynak gereksinimidir; bu raporda ilk sürüm dışında önerilmeleri, kaynakta bulunmadıkları veya kullanıcı tarafından iptal edildikleri anlamına gelmez. Kaynak kapsamından bu ayrım MVP onayında ayrıca karara bağlanmalıdır.

> Bu rapor, sağlanan ihtiyaç belgelerini ve çalışma alanındaki ilgili mevcut entity/service kayıtlarını analiz eder. Belgelerde geçen mevzuat bilgileri kaynak dokümanın beyanıdır; hukuki görüş veya güncel mevzuat teyidi değildir. Uygulama kapsamı, saklama politikaları ve resmi entegrasyonlar hukuk/KVKK sorumlusu ve ilgili sağlayıcılarla doğrulanmalıdır.

## 1. Yönetici özeti

İhtiyaç belgeleri, araç rezervasyonundan fiyat teklifine, müşteri/sürücü doğrulamasından sözleşme ve teslimata, kiralama sürecinden iadeye ve bakiye kapanışına kadar tek referansla izlenen bir günlük araç kiralama iş akışı tarif ediyor. VIP, bağımsız bir kiralama türü olarak değil; aynı müşteri, araç, rezervasyon ve sözleşme altyapısına bağlanan özel hizmet seviyesi (özel tarife, öncelik, adres teslimatı/concierge görevi ve SLA) olarak ele alınmalıdır.

Çalışma alanında `MusteriKiralama` entity’si ve `MusteriKiralamaService` ile gerçek müşteri kiralamasına yönelik bir başlangıç altyapısı mevcut. Ayrıca araç, müşteri/cari, şube, araç belgeleri, bakım/masraf, fatura ve kasa hareketleri gibi yeniden kullanılabilecek ortak kabiliyetler bulunuyor. Ancak mevcut müşteri kiralama modeli rezervasyon/takvim, fiyat kalemi sürümleme, sözleşme belge paketi, teslim/iade kontrol ve kanıt zinciri, depozito/provizyon mutabakatı, VIP hizmet görevi ve kapsamlı yetki/denetim gereksinimlerini karşılayan tamamlanmış bir rent-a-car ürünü olarak değerlendirilmemelidir.

**Öneri:** Yeni modül, mevcut `MusteriKiralama` temeli ve ortak ERP kabiliyetleri üzerine ayrı bir rent-a-car iş akışı olarak geliştirilmelidir. İlk sürümde tek şube veya sınırlı araç grubu ile operasyon çekirdeği devreye alınmalı; VIP hizmet katmanı aynı çekirdek üzerine eklenmeli; üçüncü taraf entegrasyonları teknik erişim ve hukuki koşullar doğrulanana kadar manuel kontrollü iş adımları olarak kalmalıdır.

## 2. İş hedefleri ve başarı ölçütleri

### 2.1 Hedeflenen iş sonuçları

- Araç müsaitliği ve rezervasyon çakışmalarını azaltmak; araç, bakım ve hazırlık bloklarını tek operasyon takviminde göstermek.
- Tekliften kiralama kapanışına kadar kayıt kopukluğunu ve gelir kaybını önlemek.
- Geç iade, fazla kilometre, yakıt farkı, ek hizmet, hasar ve ceza kalemlerini gerekçe ve kanıtla yönetmek.
- Tahsilat, depozito/provizyon ve iade durumunu fatura/cari/kasa süreçleriyle izlenebilir biçimde bağlamak.
- VIP hizmetini kişiye bağlı olmayan görev, sorumlu, zaman penceresi ve SLA akışına dönüştürmek.
- Yönetim raporlarında gelir, doluluk, araç kullanım dışı zamanı ve mutabakat sapmalarını kaynak kiralama/araç kaydına kadar izlemek.

### 2.2 Önerilen KPI’lar

- **Filo doluluk oranı:** Kiralanan gün / kiralanabilir gün. Bakım, hasar ve transfer nedeniyle kullanılamayan günler ayrıca gösterilir.
- **Araç başına gelir:** Dönem kiralama geliri / araç; araç sınıfı ve şube kırılımında.
- **Rezervasyon dönüşümü:** Kesin rezervasyon / teklif veya talep; kaynak kanal/segment kırılımında.
- **Kullanım dışı gün:** Bakım, hasar, temizlik ve transfer bloklarının toplamı.
- **İade farkı oranı:** Ek ücret/hasar kaydı bulunan iadeler / toplam iade; gerekçe ve kanıt tamlığıyla.
- **Tahsilat kapanış süresi:** İade anından bakiye/depozito mutabakatına kadar geçen süre.
- **VIP SLA uyumu:** Zamanında tamamlanan VIP görevleri / tamamlanan görevler.
- **Araç hazırlık çevrimi:** İade ile aracın yeniden kiralanabilir duruma alınması arasındaki süre.

Her KPI’dan ilgili rezervasyon, sözleşme, teslim/iade, araç veya finans kaydına drill-down yapılabilmelidir.

## 3. Ürün sınırı ve kapsam

### 3.1 Kapsama alınması önerilenler

- Saatlik/günlük, haftalık ve uzun dönemli kiralama fiyatlandırma desteği.
- Çağrı/ofis, web ve iş ortağı kaynaklı taleplerin ortak rezervasyon akışında izlenmesi; kanal entegrasyonu daha sonraki fazda olabilir.
- Araç sınıfına veya belirli plakaya göre rezervasyon; alış/iade şubesi ve farklı lokasyonda iade.
- Bireysel/kurumsal müşteri, bir veya birden fazla sürücü ve fatura profili.
- Tarife, fiyat teklifi, ek hizmet ve indirim yönetimi.
- Sözleşme, ek protokol, teslim/iade kontrol formu ve belge versiyonları.
- Ödeme, provizyon/tahsilat ayrımı, depozito, kısmi ödeme/iade ve mutabakat.
- Kilometre, yakıt/şarj, aksesuar, görünür hasar, ek ücret ve ceza olaylarının kiralama ile ilişkilendirilmesi.
- Filo kullanılabilirliği, bakım/hasar blokajı, araç hazırlığı.
- Şube/rol bazlı erişim, onay limitleri ve denetim izi.
- VIP müşteri seviyesi, özel tarife, teslimat/concierge görevi ve SLA.
- Temel operasyon ve yönetim raporları.

### 3.2 İlk sürüm dışında tutulması önerilenler

- Bağımsız işletmeler arasında ortak müşteri puanı/kara liste veya serbest not havuzu.
- Araç uzaktan kontrolü, otomatik kilit açma ve gelişmiş telematik.
- Muhasebe/ERP’nin tamamını yeniden yazmak; kiralama alt defterinden mevcut finans süreçlerine kontrollü aktarım tercih edilmelidir.
- Resmi teknik erişimi ve yetkisi teyit edilmemiş kamu sistemi API entegrasyonu.
- Sağlayıcısı ve sözleşme/PCI kapsamı belirlenmemiş kart saklama veya ödeme altyapısı.

VIP, kendi başına ayrı bir kiralama türü yerine müşteri/rezervasyon üzerinde hizmet seviyesi ve ek hizmet seti olmalıdır. Böylece araç müsaitliği, sözleşme ve finans akışı iki ayrı ürüne bölünmez.

## 4. Hedef süreç akışları

### 4.1 Talep, teklif ve rezervasyon

1. Kanal, müşteri ve talep kaydı oluşturulur.
2. Tarih/saat, alış ve iade lokasyonu, araç sınıfı/plaka, sürücü ve ek hizmetler girilir.
3. Sistem aynı araçtaki rezervasyon, aktif kiralama, bakım/hasar ve gerekli hazırlık/transfer tamponlarını kontrol eder.
4. Tarife, dönem, süre, lokasyon, km paketi, tek yön, ek sürücü, teslimat, indirim ve vergi kalemleri ayrı ayrı hesaplanır.
5. Teklif geçerliliği, fiyat sürümü ve ön ödeme koşulları saklanır; rezervasyon onayında fiyat sessizce değiştirilemeyecek şekilde sabitlenir.
6. Uygunluk yoksa alternatif araç/sınıf veya yeni zaman önerilir. Yönetici onayı aynı araç için çakışan iki fiili tahsise izin vermez; yalnızca müşteri kabulüyle alternatif çözümü onaylar. Güvenlik ve doğrulanmış yasal uygunluk engelleri ticari istisna ile aşılamaz.

### 4.2 Kiralama öncesi kontrol ve sözleşme

- Müşteri/sürücü, belge ve uygunluk kontrolleri firma politikası ve hukukça onaylı kurallara göre tamamlanır.
- Ödeme/depozito koşulları, gerekli onaylar ve sözleşme tamamlanmadan araç teslim adımına geçilmez.
- Sözleşme ve ek protokol şablonları sürümlenir; imza/OTP/e-imza sağlayıcısı seçilene kadar imza durumu ve kanıt dosyası manuel kaydedilebilir.
- VIP için ek hizmet, özel tarife, onay limiti, teslimat adresi, görevli ve zaman penceresi ilişkilendirilir.

### 4.3 Araç teslimi

- Teslim saati, kilometre, yakıt/şarj, temizlik, ekipman/aksesuar ve görünür hasar kontrol edilir.
- Kontrol sonucu fotoğraf ve form kanıtları ile kiralama kaydına bağlanır.
- Sözleşme ve teslim formu sürüm bilgisiyle saklanır; personel ve müşteri kabulü kaydedilir.
- Resmi bildirim gereken işlemler için sorumlu, zaman, durum, referans ve istisna nedeni kaydedilir.

### 4.4 Kiralama sırasında değişiklik/olay

- Uzatma isteği yeni bitiş saatiyle araç/bakım/sonraki rezervasyon çakışmasına ve fiyat farkına göre değerlendirilir.
- Ek sürücü, lokasyon değişikliği, ikame araç, arıza/çekici, kaza, ceza ve müşteri talebi tarih/sorumlu ve kanıtla olay kaydı olur.
- Fiyat/sözleşme değişikliği mevcut onaylı fiyatı değiştirmez; fark ek protokol veya yeni ücret kalemi olarak izlenir.

### 4.5 İade ve finansal kapanış

- Gerçek iade zamanı, kilometre, yakıt/şarj ve araç durumu teslim kontrolüyle aynı kontrol listesinden kaydedilir.
- Fotoğraf/teslim kanıtı karşılaştırması; gecikme, kilometre, yakıt, temizlik, hasar ve ceza adayları belirlenir.
- Ücret düzeltmesi yalnızca açıklama, kanıt ve yetkili onayıyla ödeme/tahsilat kaydına dönüşür.
- Depozito/provizyon çözme/iade, tahsilat, cari bakiye, fatura/makbuz ve kasa/terminal mutabakatı tamamlanır.
- Araç, gerekli temizlik/hazırlık/bakım bitmeden takvimde kiralanabilir yapılmaz.

### 4.6 VIP görev akışı

VIP teslimat/concierge işi, rezervasyona bağlı görev olarak açılır; adres, zaman penceresi, sorumlu, araç hazırlığı ve özel tercihleri içerir. Teslim/geri alma kanıtları, SLA hedefi, gecikme nedeni ve yönetici eskalasyonu kaydedilir.

## 5. Fonksiyonel gereksinimler ve öncelik

| Alan | Önerilen gereksinim | Öncelik |
|---|---|---|
| Rezervasyon ve müsaitlik | Takvim, araç sınıfı/plaka seçimi, çakışma engeli, bakım/transfer tamponu, bekleme listesi, iptal/no-show, uzatma, lokasyon ve fiyat teklifi geçmişi | P0 |
| Araç/filo yönetimi | Plaka/VIN, marka-model-yıl, sınıf, şube, sahiplik, km/yakıt, teknik evrak tarihleri, ekipman, durum ve kiralanabilirlik | P0 |
| Müşteri ve sürücü | Bireysel/kurumsal kart, fatura profili, kiralama bazlı birden fazla sürücü, belgelerin kontrol geçerliliği, izin ve saklama durumu | P0 |
| Tarife ve paket | Tarih/sezon/süre/lokasyon/sınıf, km limiti/fazla km, ek sürücü, teslimat, tek yön, indirim, vergi ve yuvarlama | P0 |
| Sözleşme ve belge | Sözleşme şablonu/sürümü, ek protokol, kabul/imza durumu, teslim/iade formları, kontrollü belge erişimi | P0 |
| Tahsilat ve depozito | Nakit/kart/havale, provizyon ve tahsilat ayrımı, depozito çözme/iade, kısmi ödeme, bakiye ve kasa/terminal mutabakatı | P0 |
| Teslim/iade kontrolü | Km, yakıt/şarj, görünür hasar, aksesuar, fotoğraf, imza/kabul, zaman ve görevli kaydı | P0 |
| Şube, rol ve denetim | Firma/şube izolasyonu, yetkiler, onay limitleri, vardiya/kasa sorumluluğu, iptal/değişiklik gerekçesi, dışa aktarım logu | P0 |
| Bakım/hasar/servis | Temel bakım/hasar/hazırlık blokajı ve güvenli kullanıma dönüş kontrolü; ayrıntılı servis maliyeti ve ikame yönetimi | Temel blokaj P0; ayrıntılı yönetim P1 |
| Ceza/olay | Kiralama tarihinden sürücü eşleme, tebligat, ödeme/itiraz, müşteri bilgilendirme ve masraf yansıtma | P1 |
| VIP hizmet | VIP seviyesi, özel tarife/onay, tercih, teslimat/concierge görevi, SLA ve özel iletişim planı | P1 |
| Raporlama | Günlük teslim/iade, açık bakiye/depozito, blokaj ve bildirim istisnaları; gelişmiş KPI ve VIP SLA | Operasyon kontrol raporları P0; gelişmiş analiz P1 |
| Uyum görevleri | KABİS ve uygulanabilir diğer bildirimler için sorumlu, süre, referans/kanıt ve istisna takibi | P0; doğrulanmış yükümlülükler kapsamınca |
| Entegrasyonlar | Ödeme, e-belge/ERP ve kamu sistemlerinde kontrollü manuel işleme/kanıt; dış API, SMS/e-posta, harita, e-imza ve telematik adaptörleri | Manuel kontrol P0; API P1/P2, erişim teyidine bağlı |

## 6. Kritik iş kuralları ve kabul ölçütleri

1. **Çakışma kontrolü:** Aynı araç için kesişen rezervasyon, aktif kiralama veya bakım/hasar bloğu kabul edilmez. Kontrol eşzamanlı iki talebi de kapsayacak şekilde veritabanı/işlem sınırında güvenceye alınmalıdır.
2. **Durum makinesi:** Araç durumları kontrollü geçişlerle yönetilir (ör. Hazır, Rezerve, Kirada, İade kontrolünde, Bakımda, Hasarlı, Pasif). Her geçiş zaman, kullanıcı ve neden taşımalıdır.
3. **Fiyat sabitleme:** Onaylanmış rezervasyon/sözleşme, kullanılan tarife ve kalemlerin sürümünü saklar; sonraki tarife güncellemesi mevcut anlaşmayı değiştirmez.
4. **Uzatma:** Yeni bitiş zamanı ve transfer/hazırlık tamponu yeniden kontrol edilir; çakışmada otomatik sessiz kabul olmaz.
5. **Hasar/ek ücret:** Teslim ve iade kanıtı, gerekçe ve yetkili onayı olmadan otomatik tahsilat üretilmez.
6. **İndirim/depozito iadesi:** Limit üstü indirim, depozito iadesi ve ücret düzeltmesi ikinci onay ve neden koduna tabidir.
7. **Tek kiralama referansı:** Teklif, rezervasyon, sözleşme, teslim/iade, olay, ödeme, fatura ve belge ilişkileri aynı kiralama referansında bulunur.
8. **Finans mutabakatı:** Toplam tahsilat, iade, depozito/provizyon, fatura ve kasa/banka hareketi arasında günlük fark raporu üretilebilir.
9. **Denetim:** Hassas görüntüleme/indirme, dışa aktarma, onay, iptal, fiyat değişikliği ve risk incelemesi kullanıcı/zaman/gerekçeyle loglanır.
10. **Uçtan uca kabul:** Bir test kiralaması talep/tekliften rezervasyon, sözleşme, teslim, uzatma/olay, iade, ücret ve bakiye mutabakatına kadar aynı kayıtla tamamlanabilir.
11. **Rapor kanıtı:** Rapor KPI’ları kaynak rezervasyon/araç/finans kaydına kadar izlenebilir.
12. **Kişisel veri sınırı:** Bağımsız firmaların kullandığı ortak kara liste veya doğrulanmamış paylaşım havuzu ürüne eklenmez.

## 7. Mevcut uygulamayla uyum ve gap analizi

### 7.1 Yeniden kullanılabilir mevcut temeller

| Alan | Mevcut çalışma alanı öğeleri | Analiz |
|---|---|---|
| Müşteri kiralama | `MKFiloServis.Shared/Entities/MusteriKiralama.cs`; `MKFiloServis.Web/Services/MusteriKiralamaService.cs`; DI kaydı `Program.cs` | Firma, müşteri, araç, başlangıç/bitiş, günlük fiyat, toplam, depozito, durum, ödeme durumu, sözleşme numarası ve temel teslim/iadeye ilişkin alan/işlemler var. Bu, yeni iş akışının başlangıç noktası olabilir; gereksinimlerin tamamlandığını göstermez. |
| Araç ana verisi | `MKFiloServis.Shared/Entities/Arac.cs`; Araç kartı UI’ları | Plaka/VIN, marka/model, sınıf/sahiplik, günlük kira bedeli, mevcut araç durumu, km ve sigorta/muayene tarihleri gibi alanlar var. Rent-a-car sınıfı, şube uygunluğu, kiralanabilirlik ve araç durum makinesiyle tutarlılık doğrulanmalı. |
| Müşteri/cari | `MKFiloServis.Shared/Entities/Cari.cs` ve cari hizmetleri | Müşteri/kurumsal cari ve finans bağlantısı için ortak kayıt olarak değerlendirilebilir. Kiralama sürücüsü, kimlik/ehliyet ve izin verileri için ayrı, amaç sınırlı yapı/erişim gerekir. |
| Şube | `MKFiloServis.Shared/Entities/Sube.cs` | Firma altında şube ana kaydı var. Kiralama, araç uygunluğu, teslim/iade lokasyonu, yetki ve raporlar bu şubeye tutarlı biçimde bağlanmalı. |
| Araç belgeleri | `AracEvrak` ve ilgili UI/servisleri | Ruhsat/sigorta/muayene belge ve hatırlatma işlevleri yeniden kullanılabilir; kiralamaya özgü sözleşme, teslim/iade kanıtı ve sürücü evrakı farklı süreç ihtiyacıdır. |
| Bakım/masraf | `BakimPeriyot`, `AracMasraf` ve bakım UI’ları | Bakım geçmişi, uyarı ve masraf temeli var. Kiralama takvimindeki blokaj, planlı hazır olma, hasar inceleme ve ikame araç süreçleriyle bağlantı gereklidir. |
| Finans | `Fatura`, `FaturaKalem`, `BankaKasaHareket`, cari ve ödeme eşleştirme kabiliyetleri | Kiralama gelirinin fatura/cari/kasa süreçlerine bağlanması için kullanılabilir; depozito/provizyonun gelir/tahsilattan ayrı izlenmesi ve iade mutabakatı için boşluk analizi gerekir. |
| Tenant altyapısı | `IFirmaTenant`/tenant filtreleme ve firma izolasyonu | Firma sınırı için temel mevcut; kiralama tabloları, dosya erişimi, arka plan işleri, raporlar ve entegrasyon kayıtlarında uçtan uca doğrulama gereklidir. |
| Diğer kiralama adları | `KiralamaVeServis.cs`, `ServisKiralamaService`, kiralık plaka takip ve taşıma kontratı sayfaları | Bunlar filo/taşıma hizmeti veya başka firmadan araç kiralama operasyonlarıdır; müşteri günlük rent-a-car rezervasyon sistemiyle aynı ürün süreci sayılmamalı, karışan terimler arayüzde ayrıştırılmalıdır. |

### 7.2 Belirgin işlev boşlukları / doğrulanması gerekenler

- Mevcut müşteri kiralama entity’sinde rezervasyon kaynağı, araç sınıfı bazlı tahsis, alış/iade şubeleri, bekleme listesi, teklif ve tarife sürüm geçmişi görünmüyor.
- Mevcut hizmette temel uygunluk kontrolü ve teslim al/teslim et akışı var; çakışmanın veritabanı seviyesinde/eşzamanlı işlemlere dayanıklı olduğu ayrıca doğrulanmalıdır.
- Teklif/fiyat kalemleri (km paketi, fazla km, tek yön, ek sürücü, teslimat, indirim, vergi, yuvarlama) ve rezervasyon onayında fiyat snapshot’ı ayrı gereksinimdir.
- Bir kiralamada birden fazla sürücü, ehliyet kontrolü, müşteri onayı/izin bilgileri ve hassas belge erişim kaydı için model/iş akışı ihtiyacı var.
- Sözleşme şablon/sürüm, imza/OTP kanıtı, teslim/iade formları, fotoğraf dosyalarının kontrollü depolanması ve erişim audit’i ayrıca tasarlanmalıdır.
- Depozito alanı mevcut olsa da provizyon, tahsilat, çözme, kısmi iade, bloke süresi, terminal/kasa mutabakatı ve muhasebe sınıflandırması uçtan uca doğrulanmalıdır.
- İade farkının onaylı ücret kalemine çevrilmesi, hasar/ceza olayının kanıt ve sürücü eşlemesi, müşteri bilgilendirmesi ve itiraz durum takibi genişletilmelidir.
- VIP müşteri tercihi değil, görevlendirme, teslimat, zaman penceresi, SLA, gecikme eskalasyonu ve ayrı KPI’ları kapsayan bir iş akışıdır.
- Şube bazlı araç envanteri, görev/rezervasyon yetkisi ve rapor erişimi ürün bazında değerlendirilmelidir.
- KABİS, ödeme, e-belge, SMS/e-posta, e-imza, harita ve telematik bağlantıları teknik erişim/sağlayıcı kararı gerektirir; var olan sistemlerde entegrasyon bulunduğu varsayılmamalıdır.

## 8. Mimari ve operasyonel kalite beklentileri

- Modül, mevcut Blazor web ERP’sinde masaüstü merkez/şube kullanımına uygun olmalı; belge ve kanıt erişimi ekran tabanlı yetkilerle sınırlandırılmalıdır.
- Rezervasyon takvimi, araç durumu ve bakım/hasar blokları tek doğruluk kaynağından beslenmelidir.
- Eşzamanlı rezervasyon çakışması sadece UI kontrolüne bırakılmamalı; kayıt/işlem seviyesinde önlenmelidir.
- Firma ve şube verisi sunucu tarafında sınırlandırılmalıdır. Tenant filtresinin atlanabildiği yönetim, dışa aktarım, API ve rapor yolları ayrıca incelenmelidir.
- Fotoğraf ve belge içerikleri ana iş veritabanında büyümeyi önleyecek kontrollü dosya/depo yaklaşımıyla tutulmalı; kullanıcı, kiralama, amaç, saklama ve erişim izleri ilişkilendirilmelidir.
- Entegrasyonlar sağlayıcı adaptörü olarak ele alınmalı; güvenli anahtar kasası, en az yetki, TLS, webhook imzası, idempotency, tekrar deneme, hata kuyruğu ve manuel yeniden işleme standardı aranmalıdır.
- Günlük yedek, geri yükleme provası, sağlık/performans izlemesi, log saklama ve sürüm geri alma prosedürleri kabul kriterlerine alınmalıdır.

## 9. Kişisel veri, mevzuat ve entegrasyon riskleri

- Kimlik/ehliyet verilerinde amaç, hukuki dayanak, saklama süresi ve erişen roller ayrı tanımlanmalı; maskeleme ve indirme kısıtları uygulanmalıdır.
- Operasyonel işlem için gereken işleme ile pazarlama izni birbirinden ayrılmalıdır.
- Kart numarası, CVV ve PIN saklanmamalı; ödeme kuruluşu token/provizyon yaklaşımı kullanmalıdır.
- Kurum içi inceleme/risk kaydı ancak nesnel kriter, kanıt, sınırlı saklama, yetkili insan incelemesi ve itiraz süreciyle değerlendirilebilir. Firmalar arası genel havuz kapsam dışıdır.
- KABİS veya Bakanlık sistemine API varmış gibi taahhüt verilmemelidir. İlk aşamada kullanıcı görevi, zaman, durum, referans ve istisna kanıtı izlenebilir; teknik arayüz ve izinler ayrıca teyit edilir.
- Kaynak belgeler 24.09.2026 tarihi itibarıyla 01.01.2027, 01.07.2027 ve 01.01.2028 gibi yürürlük/geçiş tarihleri ve filo/işletme koşulları bildiriyor. Bu tarih ve koşullar yalnızca kaynak belgenin beyanı olarak rapora alınmıştır; uygulama öncesinde Resmî Gazete ve yetkili kurum kaynakları ile uzman hukuk danışmanı üzerinden doğrulanmalı ve işletme profiline göre görev/takvimleştirilmelidir.
- SMS, e-posta ve diğer pazarlama kanalları iletişim izni ve tercih yönetimine göre çalışmalıdır.
- Sağlayıcı ve kurum entegrasyonları için sandbox, uçtan uca test, erişim/amaç incelemesi ve günlük mutabakat planlanmalıdır.

## 10. Aşamalandırılmış yol haritası

### Aşama 0 — Keşif ve kararlar

Şube/filo profili, hedef araç sınıfları, saatlik/günlük/uzun dönem sınırları, tarife örnekleri, sözleşmeler, depozito yöntemi, iade kontrol listeleri, VIP tanımı, roller/onay limitleri, belge saklama politikası ve sağlayıcılar netleştirilir. Süreç haritası, terimler sözlüğü, yetki matrisi ve MVP kapsamı iş sahibi tarafından onaylanır.

### Aşama 1 — Kiralama operasyon çekirdeği (P0)

Araç/müşteri seçimi, müsaitlik takvimi, teklif/rezervasyon, fiyat snapshot’ı, sözleşme/teslim/iade kayıtları, ödeme/depozito hareketi, temel durum makinesi, denetim izi ve temel raporlar geliştirilir. Tek şube veya sınırlı araç grubu üzerinden uçtan uca pilot yapılır.

### Aşama 2 — Finansal/filo kontrolleri

P0'da temel bakım/hasar/hazırlık blokajı, depozito çözme/iade, gerekli ikinci onay, günlük finans mutabakatı ve uyum görevleri tamamlanmış olmalıdır. Bu aşamada ayrıntılı hasar/ceza dosyaları, servis maliyeti, ikame, araç hazırlık analizi ve mevcut e-belge/muhasebe süreçleriyle otomasyon genişletilir; temel güvenlik ve para iadesi kontrolleri bu aşamaya ertelenmez.

### Aşama 3 — VIP hizmet katmanı

Özel tarife/onay, müşteri tercihi, teslimat/concierge görevleri, sorumlu/zaman penceresi, teslim kanıtı, gecikme eskalasyonu ve VIP SLA raporu devreye alınır.

### Aşama 4 — Doğrulanmış entegrasyonlar ve ölçek

KABİS erişim/izinleri ve teknik yöntemi teyit edilirse resmi entegrasyon değerlendirilir. Ödeme, e-belge/ERP, SMS/e-posta, e-imza, harita ve telematik adaptörleri seçili sağlayıcılarla sandbox ve üretim kabul testlerinden sonra firma/şube bazında açılır.

## 11. Proje riskleri ve azaltım yaklaşımı

| Risk | Etki | Azaltım |
|---|---|---|
| “Kiralama” kayıtlarının taşıma/filo kiralama modülleriyle karışması | Yanlış ekran, veri ve rapor kullanımı | Rent-a-car menü/terimlerini ayrı tanımla; ortak araç/finans kabiliyetlerini servis sözleşmeleriyle kullan. |
| Eşzamanlı rezervasyon yarışı | Aynı aracın iki müşteriye verilmesi | UI dışında işlem/DB seviyesinde çakışma koruması ve paralel kabul testi uygula. |
| Belgesiz hasar/ek ücret | İtiraz, gelir kaybı ve hukuki ihtilaf | Teslim/iade karşılaştırması, fotoğraf, açıklama, ikinci onay ve değiştirilemez işlem izi zorunlu olsun. |
| Depozitonun kira geliriyle karışması | Cari/kasa ve finansal raporlama hatası | Provizyon, tahsilat, depozito, iade ve kira gelirini ayrı hareket türleriyle mutabık kıl. |
| KVKK veya mevzuat uyumsuzluğu | Veri ihlali, yaptırım ve süreç durması | Hukuk/KVKK onayı, amaç/saklama envanteri, rol/maskeleme, erişim logu ve itiraz/imha akışı. |
| Resmi/sağlayıcı API’sinin erişilebilir olmaması | Proje gecikmesi ve manuel iş yükü | İlk sürümde manuel görev/fallback; adaptörleri sonradan açılabilir tasarla; entegrasyonu milestone’a bağla. |
| Mevcut araç/fatura verilerinin iş semantiği farkı | Yanlış kullanılabilirlik/tutar | Rent-a-car veri ve hesaplama kurallarını mevcut taşıma süreçlerinden ayır; veri sahipliği ve mutabakat testi yap. |
| Büyük belge/fotoğraf hacmi | Depolama maliyeti ve performans | Kontrollü dış dosya depolama, boyut/format politikası, erişim ve saklama yönetimi uygula. |

## 12. İş sahibi kararı bekleyen sorular

1. Günlük kiralamaya ek olarak saatlik ve 30 gün üzeri kiralama ilk ürün kapsamına dahil mi?
2. Rezervasyon kesinleşirken belirli plaka mı, araç sınıfı mı taahhüt edilecek; sınıf rezervasyonundan plakaya tahsis ne zaman yapılacak?
3. İptal, no-show, ön ödeme ve ücretsiz iptal süreleri nasıl hesaplanacak?
4. Kiralama günü/saat hesabı, tolerans ve geç iade tarifesi nedir?
5. Km paketi, fazla km, yakıt/şarj, temizlik, tek yön/şube değişimi ve teslimat ücretleri hangi kurallarla fiyatlanacak?
6. Depozito provizyonla mı, tahsilatla mı alınacak; iade SLA’sı ve istisna onay limiti nedir?
7. VIP yalnızca üst segment araç/hizmet seviyesi mi; şoförlü transfer, concierge ve teslim alma da dahil mi?
8. Müşteri ve sürücü belge kontrolü, yaş/ehliyet kabul koşulları ve veri saklama süreleri kim tarafından onaylanacak?
9. Sözleşme/e-imza, ödeme, e-belge/ERP, SMS/e-posta, harita ve telematik sağlayıcıları kimlerdir?
10. KABİS/Bakanlık sistemi için işletme hesabı, yetki ve resmi teknik entegrasyon koşulları nedir?
11. Mevcut `MusteriKiralama` verisi aktif kullanılıyor mu; bu kayıtların yeni akışa geçişi, arşivi veya uyumluluk planı nedir?
12. Şube ve birden fazla firma hesabı ilk sürümde gerekli mi; veri sahipliği ve kullanıcı yetkileri nasıl ayrılacak?
13. Finans tarafında depozito, kira geliri, hasar/ceza ve iade hareketleri hangi hesap/kasa/fatura türlerine bağlanacak?
14. Pilot şube/araç grubu, iş sahibi, kabul kullanıcıları ve yayına geçiş koşulları kimlerdir?

## 13. Sonuç ve önerilen karar

Yeni modül, mevcut araç ve cari/finans altyapısını tekrar kullanmalı; ancak müşteri kiralama entity/service’ini tamamlanmış rent-a-car iş akışı olarak kabul etmemelidir. Önce keşif kararları ve hukuki/veri politikası onayları tamamlanmalı; ardından P0 operasyon çekirdeği sınırlı pilotla devreye alınmalıdır. VIP, aynı kiralama çekirdeği üzerinde P1 hizmet katmanı olarak eklenmelidir. Temel blokaj ve depozito mutabakatı P0'da, ayrıntılı bakım/hasar/ceza yönetimi P1'de tamamlanmalı; dış entegrasyonlar yalnızca erişim, yetki ve sağlayıcı koşulları doğrulanınca açılmalıdır.

**Bir sonraki iş ürünü:** İş sahibi cevaplarıyla onaylanmış süreç haritası, MVP kapsamı, rol/onay matrisi, örnek sözleşme/teslim-iade formları ve test edilebilir kabul senaryoları.

## 14. Revizyon bulguları ve gereksinim izlenebilirliği

Bu revizyonda **kaynak gereksinimi**, **mevcut sistem bulgusu** ve **yeni ürün önerisi** ayrı değerlendirilmiştir. Aşağıdaki ekler onaylanmış geliştirme taahhüdü değildir; MVP kararlarına girdi oluşturur. Mevzuatın doğruluğu bağımsız olarak teyit edilmemiştir. İnceleme Office belgelerinden çıkarılan metinlere dayanır; görseller, diyagramlar ve konuşmacı notları için görsel doğrulama yapılmamıştır.

| Kimlik | Kaynak / dayanak | Eksik veya çelişen alan | Revizyon kararı |
|---|---|---|---|
| RA-01 | DOCX 5, 10, 13; PPTX 6, 14 | Çakışma engeli ile yönetici istisnası çelişiyor | Fiziksel çift tahsis yasak; yalnız alternatif araç/zaman onaylanabilir |
| RA-02 | DOCX 4, 5; PPTX 5, 9 | Bakım P1 iken güvenli uygunluk P0 | Temel blokaj ve kaldırma kontrolü P0, ayrıntılı servis P1 |
| RA-03 | DOCX 4, 5, 12; PPTX 5, 9 | Depozito/iade P0 iken kapanış sonraki fazda | Temel iade/çözme ve mutabakat P0 |
| RA-04 | DOCX 4, 10; PPTX 11 | Mobil/çevrimdışı gereksinimin kapsamdan çıkarılması belirsiz | Kaynak gereksinimi korunarak ayrı MVP kararı olarak işaretlendi |
| RA-05 | DOCX 16–18; PPTX 17–23, 28 | Uyum bölümü yalnız genel uyarı düzeyinde | Uyum görevleri, kanıt, sorumlu ve tarihli kural envanteri eklendi |
| RA-06 | DOCX 17–18 | Unutulan eşya, fiziksel olmayan iade, şikâyet eksik | Ayrı istisna ve görev akışları tanımlandı |
| RA-07 | DOCX 16; PPTX 26–27 | Elektronik kabul yöntemleri eşdeğer algılanabilir | OTP/kabul ile güvenli elektronik imza ayrımı açıklandı |
| RA-08 | Mevcut kiralama servis incelemesi | Kalıcılık ve tarih/fiyat değişimi riskleri | Pilot öncesi doğrulama kapısı eklendi |
| RA-09 | Mevcut sisteme güncelleme isteği; ürün önerisi | Veri geçişi, modül kapatma ve geri dönüş eksik | Kayıt eşleme, pilot, mutabakat ve geri dönüş planı eklendi |
| RA-10 | DOCX 7, 10, 13; ürün önerisi | Roller, kalite eşikleri ve kabul kanıtı soyut | Yetki matrisi ve senaryo bazlı kabul listesi eklendi |

### 14.1 Mevcut sistemde somut riskler

`MKFiloServis.Web/Services/MusteriKiralamaService.cs` statik incelemesinde:

- **Kalıcılık riski:** Kayıt okuma işlemi ayrı veri bağlamı açıyor. İptal, kiralama başlatma ve bitirme işlemleri bu okumadan gelen nesneyi değiştirip başka bağlamda kaydetmeye çalışıyor; incelenen metotlarda nesnenin bu bağlama bağlanması görünmüyor. Başarılı mesajına rağmen değişikliğin kalıcı olmaması riski vardır. Uygulama yeniden açılarak durumun doğrulandığı test pilot öncesi zorunludur.
- **Planlanan/gerçek tarih ayrımı:** Başlatma işlemi başlangıç tarihinin üzerine güncel zamanı yazıyor. Planlanan teslim zamanı, gerçek teslim zamanı ve ücretlendirmeye esas zaman ayrı tutulmadan SLA ve sözleşme geçmişi güvenilir olamaz.
- **Uygunluk kapsamı:** İncelenen sorgu müşteri kiralamalarını kontrol ediyor; bakım, hazırlık, şube transferi veya personel taşıma görevi bloklarını sorgulamıyor. Sorgu ile kayıt ayrı işlemlerde yürütülüyor; bu akış tek başına eşzamanlı çifte rezervasyonu engelleme güvencesi değildir.
- **Fiyat geçmişi:** Güncelleme ve iade işlemleri toplamı yeniden hesaplıyor. Onaylı fiyat ile sonradan oluşan farkların ayrılması ve tarihçenin korunması gerekir.

Bu bulgular çalışma zamanı veya üretim verisi üzerinde test edilmiş hata sonuçları değildir. Başka katmandaki ek kontrollerin varlığı ve kullanılan veritabanı davranışı ayrıca doğrulanmalıdır. Bu revizyonda kod değişikliği yapılmamıştır.

## 15. Yeni modül ve mevcut sisteme güncelleme sınırı

**Önerilen ürün kararı:** Ayrı Rent a Car menüsü ve iş akışı; ortak araç/cari/finans altyapısıyla kontrollü entegrasyon. Yeni modül eklenmesi mevcut taşıma, kiralık plaka ve puantaj kayıtlarının yeniden yorumlanması anlamına gelmez.

| Sorumluluk | Yeni modülde yönetilecek | Mevcut altyapıyla bağ / sınır |
|---|---|---|
| Kiralama dosyası | Talep, teklif, rezervasyon, tahsis, sözleşme ve protokoller | Eski müşteri kiralama kaydı kaynak referansıyla korunur |
| Araç | Kiralama segmenti, lokasyon, müsaitlik ve rezervasyon blokları | Şase/plaka ve teknik evrak tek ana kaynaktan; taşıma sınıfı satış segmenti sayılmaz |
| Müşteri/sürücü | Kiracı, yetkili temsilci ve kiralamaya özel ek sürücüler | Cari faturalama kimliği kullanılır; müşteri sürücüsü şirket personeli/şoförü olarak otomatik açılmaz |
| Finans | Fiyat sürümü, ücret gerekçesi, depozito/provizyon ve iade takibi | Fatura, cari ve kasa kaydı mevcut mali kaynağa tek kez aktarılır; ikinci finans defteri oluşturulmaz |
| Belge | Sözleşme, teslim/iade, kabul kanıtı ve saklama sınıfı | Uygulama üzerinden yetkili erişim; halka açık dosya adresi yok |
| VIP | Görev, tercih, SLA ve hizmet onayı | Şoförlü transfer istenirse taşıma işi ve kiralama işi ayrı fiyat/izin değerlendirmesine tabi |
| Uygunluk | Tarih-saat ve konum bazlı kullanılabilirlik | Aynı araç hem taşıma hem kiralama için kullanılacaksa ortak meşguliyet kontrolü P0 bağımlılığıdır |

Ortak meşguliyet kontrolü ilk sürümde sağlanamıyorsa **ayrı ve onaylı kiralama araç havuzu** pilot önkoşulu olmalıdır. Filo genelindeki tüm araçları kiralanabilir kabul etmek güvenli değildir. Taşıma sefer sayısı ile kiralama gün/saat geliri ayrı tutulmalı; aynı gelir hem hakedişe hem kiralama faturasına yazılmamalıdır.

### 15.1 Önerilen ekran haritası

| Ekran | Temel işlem ve gösterge |
|---|---|
| Operasyon panosu | Bugünkü teslim/iade, geciken araç, açık depozito, hazırlık ve bildirim istisnaları |
| Müsaitlik takvimi | Araç/sınıf, şube, saat aralığı, blokaj nedeni, tahsis ve alternatif önerileri |
| Kiralama çalışma alanı | Tek referansta müşteri/sürücü, fiyat, sözleşme, teslim/iade, olay, ödeme ve belge sekmeleri |
| Teslim/iade kontrolü | Planlanan/gerçek zaman, km/yakıt, aksesuar, hasar karşılaştırması, onay ve müşteri kopyası |
| Finans mutabakatı | Tahsilat, provizyon, iade, fatura bağlantısı, fark ve başarısız işlem kuyruğu |
| Uyum/görev panosu | Bildirim, belge, süre, sorumlu, kanıt ve eskalasyon |
| VIP görev panosu | Hazırlık, teslim/geri alma, görevli, zaman penceresi ve gecikme |
| Yönetim raporları | Şube/araç/sınıf/kanal ve dönem kırılımı; kaynak kayda geçiş |

## 16. Durumlar, süre ve istisna kuralları

Aşağıdaki durum ayrımları **ürün önerisidir**; mevcut durumların birebir dönüşüm tablosu değildir.

- **Rezervasyon:** Taslak → teklif → süreli ön blokaj → kesin rezervasyon → kiralamaya dönüşüm. Süre doldu, iptal ve no-show ayrı sonlandırma nedenleridir. Teklifin tek başına stok bloke edip etmeyeceği onaylanmalıdır.
- **Kiralama:** Teslim bekliyor → kullanımda → iade kontrolünde → operasyonel kapalı. Mali bakiye açıkken araç fiziksel olarak hazır hale gelebilir; finansal kapanış ayrı izlenir.
- **Tahsis:** Sınıf taahhüdü ile plaka tahsisi ayrıdır. Sınıf rezervasyonu da kapasite tüketir; plaka seçilmedi diye sınırsız rezervasyon alınamaz. Tahsis son zamanı ve sınıf yükseltme/fiyat farkı müşteri kabulüyle belirlenir.
- **Depozito/provizyon:** Talep, alındı/bloke, kısmen kullanıldı, çözme/iade bekliyor, tamamlandı, başarısız/itirazlı ayrı durumlar olmalıdır. Banka talebinin gönderilmesi iadenin tamamlandığı anlamına gelmez.
- **Görev:** Atandı, kabul edildi, yürütülüyor, tamamlandı, başarısız/iptal. Tamamlama kanıtı ve sorumlu değişimi tarihçesi korunur.

### 16.1 Zaman ve fiyat politikası

- Başlangıç bitişten önce olmalı; planlanan ve gerçek tarih-saatler ayrı tutulmalı, şubenin saat dilimi belirtilmelidir.
- Tampon sıfırken bir kiralamanın bitişiyle diğerinin başlangıcının eşit olduğu sınırın kabulü açıkça kararlaştırılmalıdır. Hazırlık/transfer tamponu tanımlıysa bu süre kullanılabilirliğe dahil edilir.
- Gecikmiş ve fiilen dönmemiş araç, planlanan bitiş geçti diye otomatik müsait sayılmaz. Etkilenen sonraki rezervasyon için görev ve alternatif oluşturulur.
- Saatlik/günlük dönüşüm, tolerans, erken iade, uzatma, no-show ve iptal ücretleri tarihli politika ile açıklanmalıdır. Kaynakta verilen yasal sınırlar doğrulanmadan ticari politika olarak kesinleştirilmez.
- Para birimi, KDV dahil/hariç sunum, yuvarlama sırası ve kur kaynağı/tarihi karara bağlanmalıdır. Çoklu para birimi ilk sürüm kararı alınmazsa tek para birimi pilot sınırı açıkça yazılır.
- Kampanya/kurumsal/VIP tarifelerinin önceliği ve birlikte uygulanabilmesi belirlenir; aynı indirim iki kez uygulanmaz. Depozito gelir, provizyon tahsilat sayılmaz.
- Operasyonel kapanıştan sonra gelen HGS/ceza veya itiraz eski sözleşmenin üzerine yazılmaz; bağlantılı ek dosya/işlem ve müşteri bildirimiyle izlenir.

### 16.2 Eksik istisna akışları

| Olay | İş akışı / kontrol |
|---|---|
| Fiziksel olmayan/mesai dışı iade | Anahtar bırakma, teslim alınma ve kontrol zamanı ayrılır; kanıt ve müşteriye iade formu gönderimi takip edilir |
| Araç değişimi/ikame | Eski ve yeni araç kullanım aralıkları, kilometreleri, belgeleri ve ek protokol aynı dosyada korunur |
| Unutulan eşya | Bulunduğu araç/kiralama, bulma zamanı, müşteri bildirimi, muhafaza sorumlusu ve teslim alındısı; onaylı saklama süresi |
| Şikâyet/ücret itirazı | Başvuru, kanıt, sorumlu, yanıt süresi, karar ve itiraz sonucu; ihtilaflı tutar ayrıca gösterilir |
| Ödeme sonucu belirsiz | Yeniden tahsilattan önce sağlayıcı referansıyla durum sorgusu/mutabakat; aynı işlem ikinci kez mali kayıt üretmez |
| Araç satışı/filodan çıkış | Gelecek rezervasyonlar çözülmeden aktif tahsis kaldırılmaz; çıkış sonrası yeni rezervasyon engeli ve resmi kayıt görevi |
| Belge eksikliği | Güvenlik/teslim için zorunlu belge eksikse teslim engeli; tarihsel eksik belge için açık eksiklik kaydı, sonradan kanıt uydurmama |

## 17. Rol, onay ve belge erişimi matrisi

Bu matris öneridir; parasal eşikler, vekâlet ve şube kapsamı iş sahibi tarafından onaylanacaktır.

| Rol | İzinli işler | Sınır / ikinci kontrol |
|---|---|---|
| Rezervasyon/satış | Teklif ve rezervasyon, politika içi fiyat | Limit üstü indirim ve istisna için yönetici onayı; depozito iadesi gerçekleştiremez |
| Şube operasyonu | Belge kontrolü, teslim/iade ve olay kaydı | Ücret ihtilafını tek başına karara bağlayamaz; firma/şube sınırı geçerli |
| Filo sorumlusu | Bakım/hasar blokajı, hazır olma ve araç değişimi önerisi | Güvenlik engelini ticari gerekçeyle kaldıramaz; kaldırma kanıtı gerekir |
| Muhasebe/kasa | Tahsilat/iade, fatura ve günlük mutabakat | Kendi hazırladığı onaya tabi iadeyi tek başına onaylayamaz |
| Yönetici | İndirim, istisna, ücret düzeltmesi ve iade onayı | Onay rolü hukuki/güvenlik engelini aşmaz; işlem sahibiyle ayrım korunur |
| Uyum/veri sorumlusu | Bildirim, saklama, ilgili kişi talebi ve iç risk incelemesi | Kanıt, erişim amacı, süre ve itiraz kaydı; başka firma verisi yok |
| VIP görevli | Atandığı görevin gerekli iletişim/teslim bilgileri | Tüm müşteri portföyü, kimlik belgeleri ve finans raporları açılmaz |
| Sistem yöneticisi/destek | Teknik yönetim | Sınırsız iş verisi yetkisi varsayılmaz; süreli, gerekçeli ve denetimli destek erişimi |

Belgeler yalnız uygulama aracılığıyla yetkili kullanıcıya sunulmalı. Depo seçimi/değişiminde eski ve yeni belgeleri açabilen arşiv erişimi, bütünlük ve yedek geri dönüşü doğrulanmalı; elle taşınan `master.key` dosyası işletim zorunluluğu olarak tasarlanmamalıdır. Dosya tipi/boyut kontrolü, zararlı içerik kontrolü, belge sürümü ve görüntüleme/indirme kaydı gerekir. İlgili kişi talebi veya imha süreci hukuki bekletme ve mali saklama gereksinimleriyle çakışıyorsa karar ve gerekçe kaydedilir.

## 18. Kaynak mevzuat eklerinin ürün karşılığı

**Doğrulama durumu: Bekliyor.** Aşağıdaki sayılar kaynak DOCX 16–18 ve PPTX 17–23'ün beyanlarıdır; doğrulanmış yürürlükteki hukuk olarak kabul edilmemeli veya otomatik engelleme kuralı olarak etkinleştirilmemelidir. Her kural için resmi dayanak/madde, işletme kapsamı, yürürlük ve geçiş tarihi, hukuk onayı ve kural sürümü bulunmalıdır. Uygulanacak yükümlülük doğrulanmadan pilotun uygunluğu kabul edilmez.

| Kaynak beyanı / konu | Ürün gereksinimi | Doğrulama ve sorumlu |
|---|---|---|
| En fazla 29 günlük tüketici kiralamaları; 30+ gün ve tüketici olmayan kısa kiralamaların farklı kapsamı | Tüketici/ticari sıfatı, süre ve kanal bazlı kural uygunluğu; tüm kiralamalara tek hukuk profili uygulamama | Hukuk / ürün sahibi |
| 01.01.2027 yürürlük; 01.07.2027 ve bazı koşullar için 01.01.2028 geçişi | İşletme/araç bazında tarihli uyum görevi, geçiş kanıtı ve son tarih | Uyum sorumlusu |
| Yetki belgesi, ruhsat, MYK; değişikliklerde 30 günlük takip | Belge envanteri, temsilci, başvuru ve değişiklik referansı, süre uyarısı | Uyum / şube |
| Bölgeye göre 10/5 veya 5/2 araç-mülkiyet sayıları; belirli bölgelerde 2 hibrit/elektrik ve üretim şartı | İl/ilçe, işletme/şube ve araç sahiplik/enerji bilgisine göre açıklanabilir uygunluk raporu | Hukuk / filo; kapsam ve eşik teyidi |
| Yaş/km, ağır hasar, muayene/sigorta, Bakanlık araç kaydı | Filoya giriş/çıkış ve kiralama öncesi uygunluk kanıtı; geçiş istisnasının bitiş takibi | Filo / uyum |
| 1–6 gün için en çok 3 günlük, 7–29 gün için en çok 7 günlük kira tutarı depozito; normal iade için 7 gün | Doğrulanmış kapsamda depozito tavanı, iade başlangıcı, iş günü/takvim günü ayrımı ve istisna takibi | Hukuk / muhasebe |
| Bir saate kadar geç iade ücreti alınmaması; iptalde 24 saat eşiği, no-show ve km koşulları | Fiyat/iptal kuralları, hangi tarihe göre hesaplandığı ve müşteriye sunulan sürüm | Hukuk / satış; ayrıntılı hüküm teyidi |
| Unutulan eşyanın aynı gün bildirimi ve en az 1 ay saklanması; ilişik belgelerde 5 yıl | Eşya görevi ve belge saklama takvimi; diğer yasal saklama/uyuşmazlıkla birlikte değerlendirme | Uyum / şube |
| Fiziksel olmayan iadede gün içinde belge; arızada ikame/iade ve ekspertiz | Görev, kanıt, müşteri kopyası ve süre kontrolü | Operasyon / hukuk |

KABİS ve Bakanlık Kiralama Bilgi Sistemi aynı entegrasyon kabul edilmemelidir; yükümlülük, kullanıcı hesabı ve bildirim nesnesi ayrı doğrulanır. Bildirim gecikmesi yalnız kiralama kapanışında görünmemeli; ilgili teslim/değişiklik olayında görev ve zaman uyarısı oluşmalıdır. Manuel tamamlanma da referans/kanıt ister.

OTP, kutucukla onay ve güvenli elektronik imza aynı hukuki ispat gücünde varsayılmaz. Kullanılan yöntem, doğrulanan kişi, belge sürümü, zaman ve kopya iletim kanıtı ayrı tutulmalıdır. Pazarlama retleri operasyonel zorunlu bildirimlerle karıştırılmamalı; İYS, mali belge, sigorta ve şoförlü hizmet yükümlülükleri kendi kapsamlarında değerlendirilmelidir.

## 19. Güncelleme, veri geçişi ve geri dönüş planı

1. **Envanter:** Aktif eski müşteri kiralamaları, rezervasyonlar, müşteri/araç eşleşmeleri, açık bakiyeler/depozitolar ve belgeler sayılır. Kullanılan veritabanı sağlayıcıları ve müşteri kurulum sürümleri belirlenir. Hiç kullanılmamış altyapı ile gerçek işlem verisi ayrılır.
2. **Eşleme/onay:** Eski kayıt kimliği ve sözleşme numarası korunur. Şube, segment, gerçek teslim zamanı veya depozito hareketi bilinmiyorsa varsayımla doldurulmaz; eksik veri olarak iş sahibine sunulur. Araç kimliği tarihsel plaka değişimlerinden bağımsız izlenir.
3. **Pilot kopya:** Yedekten geri yüklenmiş, erişimi kısıtlı ortamda geçiş provası yapılır. Başarılı/başarısız kayıtlar, mükerrerler, bağlı belgeler ve tutar farkları raporlanır. Kişisel veri test ortamında da korunur.
4. **Mutabakat:** Firma/şube bazında kiralama adedi, aktif araç, açık bakiye, depozito ve belge sayısı karşılaştırılır. Tekrar çalıştırılan aktarım yeni fatura/tahsilat üretmemelidir. Eski fiyatlar yeni tarifeyle yeniden hesaplanmaz.
5. **Aktifleştirme:** Modül firma/şube bazında kapalı başlayıp yalnız onaylı pilot için açılır. Aynı rezervasyonun eski ve yeni ekranda paralel düzenlenmesi engellenir; açık kiralamaları eski akışta bitirme veya kontrollü aktarma kararı önceden alınır.
6. **Kesim/yayın:** Veri giriş kesimi, son fark aktarımı, yedek ve sorumlular açıklanır. Yeni kurulum ile mevcut sürümden güncelleme ayrı kabul senaryolarıdır. Her desteklenen veritabanında geçiş, yetki ve çakışma testi yapılır.
7. **Geri dönüş:** Modülü kapatmak aktif kiralamaları ve iadeleri sahipsiz bırakmaz; kontrollü kapanış erişimi korunur. Veri yapısı ve uygulama sürümü uyumluluğu doğrulanmadan yalnız eski EXE'ye dönülmez. Yayın sonrası gerçek ödeme/bildirim oluştuysa yedek geri yüklemek bunları geri almaz; mali mutabakat ve sağlayıcıdaki işlemler ayrıca ele alınır.

**Regresyon kapısı:** Araç kartı/plaka geçmişi, bakım belgeleri, cari/fatura/kasa ve mevcut operasyon planı/puantaj/hakediş akışları aynı sonuçları vermelidir. Modül kapalı firmalarda yeni zorunlu kiralama alanları mevcut işlemleri engellememelidir. Bu bölüm uygulama değişikliği talimatı değil, sonraki geliştirme için ürün kabul sınırıdır.

## 20. Kabul senaryoları ve yayın kapıları

| Test | Senaryo | Beklenen sonuç / kanıt |
|---|---|---|
| KT-01 | İki kullanıcı aynı araç ve aralığı eşzamanlı kesinleştirir | Yalnız bir aktif tahsis; diğeri açık çakışma sonucu alır |
| KT-02 | Bakım/taşıma/hazırlık bloğu, sınırda bitiş-başlangıç ve geciken iade | Onaylı tampon/sınır kuralı uygulanır; dönmemiş araç otomatik serbestleşmez |
| KT-03 | Sınıf rezervasyonları sınıf kapasitesini doldurur | Plaka atanmamış rezervasyonlar kapasiteden düşer; taşma reddedilir |
| KT-04 | Tarife onaydan sonra değişir, kiralama uzatılır | Eski fiyat korunur; farkın nedeni, onayı ve protokolü görünür |
| KT-05 | İptal/başlatma/iade ardından oturum veya uygulama yeniden açılır | İşlemin durumu ve zamanı kalıcıdır; planlanan zaman korunur |
| KT-06 | Eksik teslim kanıtıyla hasar ücreti istenir | Otomatik tahsilat olmaz; eksiklik/itiraz ve onay görevi görünür |
| KT-07 | Aynı ödeme bildirimi iki kez gelir veya iade zaman aşımına uğrar | Tek mali hareket; belirsiz işlem mutabakata alınır, başarı gibi gösterilmez |
| KT-08 | Başka firma/şube kullanıcısı kayıt, belge adresi veya raporu açar | Yetkisiz veri ve dosya içeriği verilmez; olay denetlenebilir |
| KT-09 | Eski kayıt aktarımı tekrarlanır | Mükerrer sözleşme/ödeme yok; sayılar ve bakiyeler mutabık |
| KT-10 | Resmi entegrasyon kesilir veya hiç yoktur | Yasal yükümlülükten muafiyet varsayılmaz; kanıtlı manuel görev ve gecikme alarmı çalışır |
| KT-11 | VIP görevi gecikir, sorumlu değişir | SLA başlangıç/bitişi, gecikme nedeni ve sorumlu geçmişi raporlanır |
| KT-12 | Yedek geri dönüşü ve sürüm geri alma provası | Belgeler açılır, açık kiralamalar ve mali referanslar mutabık; dış işlemler kaybolmaz |
| KT-13 | Modül kapalıyken mevcut taşıma/finans süreçleri çalıştırılır | Önceki işleyiş korunur; kiralama alanı zorunluluğu veya mükerrer gelir oluşmaz |
| KT-14 | Hukuki bekletmeli belge için imha talebi gelir | Silme otomatik tamamlanmaz; yetkili inceleme, gerekçe ve erişim kısıtı korunur |

### 20.1 Ölçülebilir kalite hedefleri — onay bekliyor

Filo büyüklüğü, eşzamanlı kullanıcı, günlük rezervasyon ve fotoğraf hacmi henüz bilinmediğinden yanıt süresi, kapasite ve takvim tarihi taahhüdü verilmez. Pilot öncesi aşağıdaki hedefler yazılı sayısal değerlerle tamamlanmalıdır:

- Müsaitlik araması ve kayıt kesinleştirmede yüzde 95 yanıt süresi; test veri hacmi ve eşzamanlı kullanıcı sayısı — ürün/teknik sorumlu.
- Rapor üretim süresi, azami satır sayısı ve belge yükleme boyut/adet sınırı — operasyon/teknik sorumlu.
- Kabul edilebilir veri kaybı (RPO), hizmete dönüş süresi (RTO) ve geri yükleme testi sıklığı — işletme/BT.
- İade, bildirim hatası ve VIP gecikmesinde alarm/yanıt süresi — muhasebe/uyum/operasyon.

Kritik kabul senaryolarında açık hata, mutabakatı açıklanamayan tutar veya yetkisiz veri erişimi varken yayına çıkılmaz. Hacim/hedef tablosu boşken performans kabulü verilmez. Test kanıtları ve iş sahibi imzası sürümle ilişkilendirilir.

## 21. Ek karar kaydı ve yönetim raporu standardı

| Karar | Seçenek / belirsizlik | Karar sahibi | Son tarih kapısı |
|---|---|---|---|
| K-01 | Yeni modülün mevcut kayıtlardan devamı, kontrollü dönüşümü veya arşiv + yeni akış | Ürün sahibi / operasyon | Veri geçiş tasarımı öncesi |
| K-02 | Sınıf/plaka taahhüdü, ön blokaj süresi ve atama son zamanı | Satış / filo | P0 kapsam onayı |
| K-03 | Ortak araç havuzu + ortak meşguliyet veya ayrı kiralama havuzu | Filo / taşıma operasyonu | Pilot seçimi |
| K-04 | Para birimi, ücret toleransı, iptal/no-show, vergi ve depozito/iade politikası | Muhasebe / hukuk | Tarife ve sözleşme onayı |
| K-05 | VIP şoförlü taşıma dahil mi; kaynak mobil/çevrimdışı kapsamı ne zaman ele alınacak | Ürün sahibi / hukuk | MVP onayı |
| K-06 | Onay eşikleri, vekâlet ve görevler ayrılığı; teknik destek erişimi | İşletme / veri sorumlusu | Yetki kabulü |
| K-07 | Mevzuat dayanakları ve işletmeye uygulanacak kurallar | Hukuk / uyum | Pilot öncesi |
| K-08 | Hacim, süre hedefleri, pilot süresi, destek ve geri dönüş sorumluları | İşletme / BT | Yayın kararı |

Her karar için seçilen seçenek, gerekçe, onaylayan, tarih ve etkilenen gereksinim kimliği kaydedilmelidir. Bu sorular yanıtlanmadan kesin efor/süre veya tüm entegrasyonların hazır olacağı taahhüt edilmez.

Raporlarda firma/şube, tarih aralığı, kullanılan tarih türü (rezervasyon, fiili kiralama veya mali işlem), para birimi, vergi dahil/hariç bilgisi, filtreler ve oluşturulma zamanı görünmelidir. Ekran, PDF ve Excel aynı veri kapsamını ve toplamları kullanmalıdır. Gruplama şube, müşteri, araç/segment ve kanal bazında seçilebilir; ara toplamlar genel toplama ikinci kez eklenmez. Farklı para birimleri kur politikası olmadan tek toplama çevrilmez. Gelir ile tahsilat/depozito ayrı gösterilir; doluluk hesabında blokaj ve payda tanımı raporda açıklanır. Kişisel veri içeren dışa aktarımlar ayrı yetki ve denetim kaydına tabi olur.

## 22. Projeye yeni modül olarak katılım modeli

### 22.1 Ürün yerleşimi ve kapsam

**Öneri:** Rent a Car, mevcut Blazor ERP içinde ayrı menüsü, işlem yetkileri, firma/şube aktivasyonu ve raporları olan bir iş modülü olmalıdır. Bu öneri yeni bağımsız uygulama veya mikroservis zorunluluğu getirmez. Ortak araç, cari, fatura, kasa ve belge altyapısı yeniden kullanılmalı; kiralamaya özgü rezervasyon, tarife, sözleşme, teslim/iade ve VIP yaşam döngüsü bu modülün sorumluluğunda kalmalıdır.

Önerilen ana menü: **Rent a Car → Operasyon Panosu / Müsaitlik ve Rezervasyon / Kiralama Dosyaları / Teslim–İade / Tarifeler ve Paketler / Depozito ve Mutabakat / VIP Görevleri / Uyum ve İstisnalar / Raporlar / Modül Ayarları.** Ortak araç/cari kartlarına bağlamı koruyan bağlantılar verilir; ikinci araç veya cari kartı ekranı ile ayrı ana kayıt oluşturulmaz.

Kiralama dosyasından fatura, kasa hareketi, araç ve belgeye geçişte kullanıcının hedef modül yetkisi tekrar kontrol edilir. Kiralama ekranına erişim, tüm finans veya personel belgelerine erişim hakkı sağlamaz. Hedef modül yetkisi olmayan kullanıcıya yalnız işini tamamlaması için ayrıca yetkilendirilmiş sınırlı durum özeti sunulabilir.

### 22.2 Doğrulanan katılım noktaları ve sınırları

| Mevcut nokta | İnceleme bulgusu | Yeni modül için gereksinim |
|---|---|---|
| `MKFiloServis.Web/Components/Layout/NavMenu.razor` | Menü/yetki kontrollü Satış / Kiralama başlığı; altında alım-satım ve kiralık C plaka bağlantıları bulunuyor | Mevcut başlığı tamamlanmış Rent a Car olarak kabul etme; ayrı ve anlaşılır menü, arama ve bağlantı düzeni |
| `MKFiloServis.Web/Services/KullaniciService.cs`, `Components/Shared/YetkiKontrol.razor` | Rol-yetki kodları ve ekran kontrolü var; incelenen bileşende admin geçişi ve yetki belirtilmediğinde izin davranışı bulunuyor | Yeni işlemler açık yetki tanımıyla kapalı varsayılan; sunucu tarafı yetki ve firma/şube kontrolü. Bu UI bulgusu bütün sunucu güvenliğinin incelendiği anlamına gelmez |
| `MKFiloServis.Web/Services/LicenseService.cs` | Firma/makine, süre ve izin verilen sürüm bilgileri lisans doğrulama akışında yer alıyor | Ayrı Rent a Car modül lisansı mevcut kabul edilmez; ticari paket ve lisans uyumluluğu ayrıca kararlaştırılır |
| `MKFiloServis.Web/Program.cs`, `Services/MusteriKiralamaService.cs` | Müşteri kiralama servisi kaydı ve temel işlemler var | Yeni menü eklemek tek başına ürünleşme değildir; 14.1 bulguları ve yeni iş kuralları karşılanmalı |
| `MKFiloServis.Web/Services/EbysService.cs` | İncelenen belge seçim/oluşturma akışı araç ve personel kaynaklarını kullanıyor | Kiralama dosyası ve müşteri sürücüsü belgesi bu kaynaklara zorla dönüştürülmez; ilişki ve yetki kapsamı doğrulanır |
| `MKFiloServis.Web/Services/FaturaService.cs` | Karşı firma için fatura oluşturma/eşleştirme akışı bulunuyor | Kiralama entegrasyonunun hangi koşulda bu davranışı tetikleyeceği sınanmalı; varsayılan şirketler arası kopyalama yapılmamalı |
| `MKFiloServis.DataSync/CliRunner.cs` ve `Exporters/` | PostgreSQL–SQLite dışa/içe aktarım girişleri bulunuyor | Gerçek zamanlı, çok yazarlı rezervasyon veya çevrimdışı senkronizasyon garantisi değildir; yeni veriler/ekler için kapsam ve dönüş testi gerekir |

Bu tablo salt okunur inceleme sonucudur. Listelenen dosyalar sonraki teknik keşif için referanstır; bu raporla uygulama değişikliği yapılmış veya entegrasyonlar test edilmiş sayılmaz.

### 22.3 Aktivasyon, yetki ve lisans yaşam döngüsü

Üç karar ayrı tutulmalıdır: **ticari kullanım hakkı**, **firmanın modülü etkinleştirmesi**, **kullanıcının işlem yetkisi**. Birinin açık olması diğerlerini otomatik açmaz. Şube kapsamı, kayıt üzerindeki firma/şube kimliğiyle doğrulanır; ekrandaki aktif firma seçimi tek başına yeterli değildir.

| Durum | Ürün davranışı önerisi |
|---|---|
| Kurulu, etkin değil | Yeni kiralama alınmaz; mevcut ERP akışları değişmez; otomatik rol yetkisi verilmez |
| Pilot | Yalnız onaylı firma/şube ve araç havuzu; bağımlılık kontrol listesi tamamlanmış |
| Aktif | Onaylı ürün kapsamı ve işlem yetkileriyle çalışır |
| Yeni işlem durduruldu | Yeni rezervasyon/uzatma için politika uygulanır; açık kiralamaların teslim-iade ve mali kapanışı kontrollü devam eder |
| Arşiv | Geçmiş kayıtlara saklama ve yetki kurallarıyla erişilir; ticari hareket oluşturulmaz |

Lisans bitişinde açık işlerin iadesi, depozito çözümü ve yasal belge erişiminin nasıl devam edeceği ticari/hukuki onay gerektirir. Bu davranış mevcut lisans kontrolünün aşılması olarak tasarlanmaz; yayın öncesinde lisans sağlayıcısı ve iş sahibiyle onaylı kapanış erişimi belirlenir. Modül kapatılırken açık iş sayısı, sorumlusu ve kapanış planı gösterilir; hiçbir kayıt veya belge otomatik silinmez.

## 23. Modüller arası entegrasyon ve veri sahipliği matrisi

Tablodaki akışlar hedef ürün gereksinimidir; mevcut durumda hazır ve çalışan bağlantı taahhüdü değildir. Aynı kayıt için bir ana sahip olmalı; diğer modül ilgili referansı ve gerekirse tarihsel işlem kopyasını tutmalıdır.

| Entegrasyon alanı | Ana kayıt sahibi | Veri / yön | Kontrol ve hata davranışı | Öncelik |
|---|---|---|---|---|
| Firma/şube ve kullanıcı | Ortak organizasyon/yetki | Aktif firma, şube, kullanıcı kapsamı → Rent a Car | Firma değiştirilince eski bağlamdaki taslak/görev yanlış firmada kaydedilemez; pasif şubede açık işlere kapanış yolu | P0 |
| Araç ve plaka geçmişi | Ortak araç yönetimi | Araç kimliği, teknik belge, sahiplik → kiralama; kullanım/konum değişikliği → yetkili araç süreci | Şase kimliğiyle bağ; geçmiş sözleşmedeki plaka değişmez; onaysız km geri düşürme yok | P0 |
| Cari/CRM | Cari ana verisi; kiralama profili Rent a Car | Faturalama kimliği → kiralama; kiralama özeti → yetkili cari görünümü | Aynı müşteri için ikinci cari otomatik açılmaz; sürücü, kiracı, ödeyen ve fatura alıcısı ayrılır; pazarlama rızası varsayılmaz | P0; CRM kampanyası P2 |
| Bakım/onarım ve evrak | Bakım/evrak ana kaydı | Geçersizlik ve servis blokajı → müsaitlik; iade hasar bulgusu → bakım inceleme talebi | Hasar bulgusu otomatik müşteri borcu veya servis faturası değildir; blokaj kaldırma yetkili kanıtla | Temel P0, ayrıntı P1 |
| Operasyon planı/puantaj | Taşıma işinin sahibi operasyon; kiralama tahsisinin sahibi Rent a Car | Araç/görevli meşguliyet aralıkları karşılıklı kontrol edilir | Ortak havuzda çakışma kontrolü her iki yönde zorunlu; kiralama günü sefer veya personel puantajı oluşturmaz | Ortak havuzda P0 |
| Hakediş | Taşıma hakediş süreci | Yalnız ayrıca onaylanan taşıma/VIP hizmetinin gerçekleşme referansı | Kira bedeli hakedişe aktarılmaz; hizmet ücretinin hangi akışta faturalanacağı tekil belirlenir | Şoförlü hizmet kararı sonrası |
| Fatura/e-belge | Mevcut fatura modülü | Onaylı kiralama ücret kalemleri → fatura talebi; belge no/durum/iptal → kiralama | Tek kaynak referansı, vergi/kur politikası, dönem kontrolü; taslak fatura e-belge kabulü sayılmaz | Kontrollü aktarım P0, sağlayıcı otomasyonu koşullu |
| Banka/kasa ve ödeme eşleme | Mevcut mali hareket süreci | Ödeme/iade talebi ve işlem referansı; gerçekleşen mali sonuç → kiralama | Provizyon, depozito ve gelir ayrımı; tekrar gelen sonuç tek hareket; belirsiz işlem mutabakata | P0 |
| EBYS/arşiv | Onaylı belge saklama altyapısı; iş bağlamı Rent a Car | Sözleşme/form sürümü ve dosya referansı → kiralama | Belge yetkisi kiralama ilişkisine göre; müşteri belgesi personel özlük dosyası değildir; saklama ve imha ortak yönetilir | P0 belge erişimi; EBYS bağlantısı yetki teyidiyle |
| Bildirim/görev | İş kararının sahibi Rent a Car; gönderim ortak bildirim | Teslim, gecikme, iade, SLA ve uyum görevleri → bildirim | Gönderildi/teslim edildi/iş tamamlandı ayrılır; bildirim hatası ana işlemi tekrar yaratmaz | Uygulama içi P0, dış kanallar koşullu |
| Personel/görevli | Personel ana kaydı | Görevli uygunluğu → VIP görev; görev gerçekleşmesi → onaylı operasyon kaydı | Müşteri sürücüsü çalışan değildir; ücret/mesai/maaş otomatik oluşmaz | VIP kapsamıyla P1 |
| Bütçe/maliyet ve yönetim raporu | Mali gerçekleşme mevcut finans; kiralama KPI Rent a Car | Kira geliri, kullanım süresi ve onaylı maliyetler → analitik | Depozito gelir değildir; fatura geliri ikinci kez toplanmaz; maliyet dağıtım yöntemi onaylanır | P1 |
| DataSync/yedek ve kurulum | Mevcut aktarım/işletim süreci | Onaylı kiralama kayıtları, ilişkiler ve belge envanteri → taşıma/yedek | Aktarımın ödeme/bildirim tetiklemesi engellenir; dosyaların ayrıca kapsamda olduğu kanıtlanır | Kullanılan dağıtımda P0 |

### 23.1 Ana veri ile tarihsel işlem bilgisinin ayrımı

- Cari unvanı/adresi değiştiğinde taslak işlemler güncel veriyle hazırlanabilir; imzalı sözleşme ve düzenlenmiş mali belge sessizce yenilenmez. Düzeltme ilgili belge sahibinin sürecinden geçer.
- Araç segmenti kiralama pazarlama sınıfıdır; mevcut taşıma sınıfını değiştirmek yerine kiralamaya özgü bilgi olarak yönetilir.
- Müşteri, kiracı tüzel kişi, sürücü, ödeme yapan kişi ve fatura alıcısı aynı olmak zorunda değildir. İlişki, onay ve doğrulama kaydı tutulur; otomatik eşitleme yapılmaz.
- Firma kopyalama veya veritabanı aktarımı; kimlik belgesi, iletişim izni, risk incelemesi ve açık ödeme referanslarını bağımsız firmaya paylaşma izni değildir. Aktarım kapsamı ve hukuki yetkisi ayrıca onaylanır.
- Ortak modülden gelen pasifleştirme/silme isteği bağlı açık kiralama veya mali kayıt varsa etki listesi üretir; tarihsel ilişkiler koparılmaz.

## 24. Uçtan uca entegrasyon olayları ve tutarlılık

### 24.1 Rezervasyon ve ortak araç havuzu

1. Kiralama talebi firma/şube ve araç havuzuyla açılır; araç, belge ve mevcut meşguliyet kontrol edilir.
2. Taşıma görevi, servis, hazırlık, transfer ve başka kiralama aralıkları tek uygunluk kararında değerlendirilir. Ayrı ekranların birbirinden habersiz kesin kayıt oluşturması kabul edilmez.
3. Kiralama kesinleşirse ilgili süre bloke edilir. İptal/değişimde yalnız bu işleme ait blokaj çözülür; bakım veya başka işin blokajı kaldırılmaz.
4. Ortak uygunluk doğrulanamıyorsa kesin rezervasyon alınmaz; taslak/bekleyen talep olarak saklanabilir. Ayrı havuz pilotu onaylandıysa sadece bu havuz kullanılabilir.

### 24.2 Teslim, iade, bakım ve belgeler

Teslimin iş sahibi Rent a Car'dır. Araç ana verisi, kontrol kanıtı ve sözleşme sürümü bağlanır; gerçek teslim, güncel kilometre ve görevlendirme izlenir. İadede araç önce kontrol/hazırlık durumuna alınır. Hasar varsa bakım inceleme talebi ve müsaitlik blokajı oluşturulur; müşteri borcu ayrı kanıt/onaydan geçer. Belge yükleme başarısızsa zorunlu kanıt tamamlanmış gösterilmez. Daha önce tamamlanan teslim kaydı sadece bildirim gönderilemedi diye yeniden oluşturulmaz.

### 24.3 Kiralama → fatura → banka/kasa → kapanış

1. Kiralama onaylı ücret kalemleri, müşteri/fatura alıcısı, para birimi, vergi ve kaynak iş referansıyla faturalama talebi üretir.
2. Fatura modülü taslak/kesin belge ve e-belge durumunun sahibidir. Rent a Car bu sonucu bağlantılı olarak gösterir; fatura numarası veya dış sistem kabul durumu uydurmaz.
3. Banka/kasa hareketi ve ödeme eşleştirmesi mali süreçte kesinleşir. Kiralama ekranındaki “ödendi” bilgisi bu mutabık sonuca dayanır; elle başarı işaretlemek mali kaydın yerine geçmez.
4. Kısmi tahsilat/iade ve fatura dağılımı görünür olmalıdır. Kurumsal toplu faturada birden çok kiralama, tek kiralamada birden çok belge olasılığı eşleme kuralıyla kararlaştırılır; zorunlu birebir ilişki varsayılmaz.
5. Fatura iptali otomatik para iadesi, kiralama iptali otomatik mali belge iptali değildir. Her biri yetkili süreç ve sonuçla tamamlanır. Kapalı mali dönemde düzeltme mevcut finans politikasına göre yapılır.
6. Depozitonun borca mahsup edilmesi ayrı onay, dayanak ve mali hareket ister. İade talebi ve bankanın gerçekleşen sonucu ayrı gösterilir.

### 24.4 VIP ve taşıma hizmeti

VIP teslimat görevi sırf şoför atandı diye sefer/hakediş üretmez. Eğer ayrıca ücretli taşıma hizmeti satılıyorsa rezervasyon dosyası, taşıma iş emri ve fiyat kalemi bağlanır; hizmetin kira paketine dahil olup olmadığı açıkça belirlenir. Görev iptalinde araç/personel blokajı, müşteri bedeli ve tedarikçi maliyeti ayrı değerlendirilir. Şoförlü hizmetin izin/sigorta ve çalışma süresi kapsamı doğrulanmadan mevcut servis operasyonu aynen kullanılamaz.

### 24.5 Ortak işlem izleme ve başarısızlık standardı

Her modüller arası işlem; firma/şube, kaynak dosya, işlem türü, iş referansı, sürüm, oluşturan/onaylayan, zaman, hedef kayıt ve sonuç bilgisiyle izlenir. Kimlik belgesi veya ödeme sırrı işlem loguna kopyalanmaz. Önerilen görünür durumlar: **Bekliyor, İşleniyor, Tamamlandı, Yeniden denenecek, Manuel inceleme, İptal/Düzeltme bekliyor.**

- Aynı talep tekrar gönderilirse aynı mali/operasyonel sonuç referansına bağlanır; mükerrer kayıt oluşmaz.
- Geç gelen eski durum, yeni iptal veya tamamlanma bilgisini geri alamaz; sıra/sürüm uyuşmazlığı incelemeye düşer.
- Rezervasyon meşguliyeti ve zorunlu yetki kontrolü kesinleşmeden başarı verilemez. Rapor, bildirim ve dış sistem işlemleri gecikmeli tamamlanabilir ancak bekleyen durum görünür olmalıdır.
- Hata ekranı yalnız teknik mesaj değil, iş dosyası, etkilenen tutar, sorumlu ve yapılabilecek işlemi gösterir. Yeniden deneme yetkilidir; önceden gerçekleşmiş ödeme tekrar yapılmaz.
- Günlük mutabakat; bağlantısız fatura/ödeme, çözülemeyen blokaj, belgesiz tamamlanma ve başarısız bildirimleri listeler. Modüller toplamları uyuşmadığında “tam entegre” kabulü verilmez.

## 25. Entegrasyon bağımlılıkları ve iş paketi sırası

| İş paketi | Önkoşul | Teslim / çıkış kapısı |
|---|---|---|
| EP-01 Modül katılımı | Lisans/aktivasyon ve yetki kararları | Ayrı menü ve rol matrisi; kapalı firmalar için regresyon ölçütü |
| EP-02 Ana veri eşleme | Firma/şube, cari/araç sahipliği ve eski kayıt envanteri | Mükerrer oluşturmayan eşleme; tarihsel sözleşme verisinin korunması |
| EP-03 Ortak uygunluk | Ortak/ayrı araç havuzu kararı; bakım ve operasyon sahiplerinin onayı | Her iki yönde çift tahsis engeli; alternatif olarak sınırları belgelenmiş ayrı pilot havuzu |
| EP-04 Teslim/iade ve belge | EP-02/03, kontrollü dosya erişimi | Kanıtlı teslim/iade; bakım blokajı ve arşiv erişim testi |
| EP-05 Finans bağlantısı | Ücret, vergi, depozito, belge/ödeme eşleme politikası | Tekil fatura/kasa sonucu; kısmi ödeme/iade ve günlük mutabakat |
| EP-06 Bildirim/uyum | İş tetikleyicisi ve görev sahipleri | Manuel kanıtlı görevler, hata ve süre panosu; dış API zorunlu varsayılmaz |
| EP-07 VIP/taşıma ve analitik | Çekirdek kabulü, hizmetin ticari/hukuki tanımı | Çift faturalama olmadan bağlı görev, SLA ve maliyet raporu |
| EP-08 Kurulum/aktarım ve yayın | Kullanılan sağlayıcı/sürüm envanteri; EP-01–06 kabulü | Yeni kurulum/güncelleme/geri dönüş provası; lisans ve açık iş kapanış kabulü |

EP numaraları geliştirme süresi tahmini değildir. İlk yayında EP-01–06 temel kontrolleri ve EP-08 yayın güvenliği gereklidir; EP-07'nin kapsamı MVP kararına bağlıdır. Mevcut finans altyapısına erişim yoksa fatura/ödeme varmış gibi işaretlemek yerine onaylı manuel aktarım ve referans-mutabakat prosedürü tanımlanır. Yasal belge düzeni sağlanamıyorsa canlı mali işlem açılmaz.

## 26. Modüller arası entegrasyon kabul senaryoları

Bu senaryolar bölüm 20'deki kiralama işlev testlerine eklenir; her test için firma/şube, kullanıcı rolü, kaynak-hedef kayıtlar, beklenen/gerçek tutar ve kanıt raporu tutulur.

| Kimlik | Senaryo | Kabul sonucu |
|---|---|---|
| EN-01 | Modül kapalı veya işlem yetkisi yok; kullanıcı doğrudan sayfa/işlem bağlantısını açar | Menü gizlemenin ötesinde sunucu erişimi reddeder; mevcut modüller çalışmaya devam eder |
| EN-02 | Firma değiştirildikten sonra eski firmada açılmış taslak gönderilir | Kayıt yanlış firmaya yazılmaz; kullanıcı yeni bağlamda işlemi doğrular |
| EN-03 | Taşıma ve kiralama ekranları aynı aracı eşzamanlı tahsis eder | Ortak havuzda yalnız bir kesin tahsis; diğer işlem çakışmayı görür; ayrı havuzda kapsam dışı araç seçilemez |
| EN-04 | İadede bakım bloğu açılır, ardından kiralama iptal/düzeltmesi yapılır | Bakım bloğu yanlışlıkla çözülmez; yalnız yetkili bakım sonucu aracı hazır yapar |
| EN-05 | Araç plakası veya cari bilgisi değiştirilir | Ana kayıt güncellenir; imzalı sözleşme ve kesin mali belge geçmişi değişmez |
| EN-06 | Fatura talebi iki kez gönderilir; karşı firma ilişkisi bulunan cari kullanılır | Tek kaynak talebe tek mali sonuç; onaysız karşı firma kaydı oluşmaz; görünürlük firma sınırını aşmaz |
| EN-07 | Kısmi ödeme/iade, mahsup ve gecikmiş banka sonucu gelir | Bakiye doğru; depozito gelir değildir; tekrar bildirim ikinci kasa hareketi üretmez |
| EN-08 | Kiralama iptal edilir fakat fatura/ödeme henüz iptal edilmez | Ayrı işlem durumları görünür; eksik mali düzeltme kapanmış gibi gösterilmez |
| EN-09 | Kiralama yetkili kullanıcısı başka dosyanın EBYS bağlantısını açar | Dosya bazlı yetki uygulanır; ilgisiz kimlik/personel/araç belgesi sızmaz |
| EN-10 | VIP görevi kira paketine dahilken operasyon gerçekleşmesi aktarılır | Aynı hizmet hem kiralama faturası hem hakediş olarak ikinci kez borçlandırılmaz |
| EN-11 | Bildirim gönderilemez veya eski olay yeni durumdan sonra gelir | Teslim/tahsilat tekrar oluşturulmaz; yeni durum korunur; hata sorumlusu ve yeniden işleme kaydı görünür |
| EN-12 | Modül lisansı biter veya firma modülü kapatır | Yeni işlem politikası uygulanır; onaylı kapanış planıyla açık kiralama, iade ve belge erişimi yönetilir; lisans kontrolü aşılmaz |
| EN-13 | Kullanılan DataSync aktarımı ve geri dönüşü tekrar çalıştırılır | Kayıt/ilişki/belge envanteri mutabık; yeni ödeme/fatura/bildirim tetiklenmez; canlı rezervasyon senkronizasyonu varsayılmaz |
| EN-14 | Mevcut kurulum yeni sürüme geçirilir, ardından geri dönüş provası yapılır | Modül ayarı/yetki/lisans uyumu ve açık işler korunur; eski puantaj/hakediş ve mali raporlar aynı sonuçları verir |

### 26.1 Yayın onayları ve sorumlular

- **Ürün/operasyon sahibi:** Menü, araç havuzu, işlem sırası, VIP kapsamı ve açık iş kapanış prosedürü.
- **Finans sahibi:** Vergi/kur, fatura/ödeme eşlemeleri, depozito/iade, karşı firma davranışı ve günlük mutabakat.
- **Filo/taşıma sahibi:** Ortak araç/görevli meşguliyeti, bakım blokajı ve mevcut plan/puantaj regresyonu.
- **Veri/uyum sorumlusu:** Firma/şube izolasyonu, belge erişimi, saklama ve bildirim kanıtları.
- **BT/lisans sorumlusu:** Modül aktivasyonu, kurulum/sürüm uyumu, veri aktarımı, yedek/geri dönüş ve destek erişimi.

Kritik entegrasyon hatası veya açıklanamayan finans farkı bulunan pilot yayına alınmaz. Onaylar ilgili test kanıtı ve sürümle bağlanır. Finans, belge veya uygunluk altyapısının hazır olmadığı durumlar “sonra entegre edilir” şeklinde kapatılmaz; sınırlandırılmış pilot veya onaylı kontrollü manuel süreç tanımlanır.

## 27. Entegrasyon için ek kararlar ve sonuç

| Karar | Onaylanması gereken konu | İlgili bölüm |
|---|---|---|
| K-09 | Rent a Car mevcut lisansa dahil mi, ayrı ticari modül mü; lisans bitişinde kontrollü kapanış erişimi nasıl sağlanacak? | 22.3 |
| K-10 | Hangi ortak modüller pilotta kullanılabilir; mevcut rol lisansı bu modüllerde hangi sınırlı işlemlere izin veriyor? | 22–23 |
| K-11 | Ortak uygunluk kararının iş sahibi kim; kiralama ve taşıma tahsisleri hangi onayla tek kontrol altında birleşecek? | 24.1 |
| K-12 | Kurumsal toplu fatura, farklı ödeyen/fatura alıcısı, kısmi ödeme ve karşı firma faturasının izin verilen senaryoları neler? | 24.3 |
| K-13 | EBYS kiralama dosyasını nasıl sınıflandıracak; hassas sürücü belgelerine kim erişecek ve eski belge depoları nasıl korunacak? | 23 |
| K-14 | DataSync müşteri kurulumlarında gerçekten kullanılıyor mu; yeni modül kayıtları ve dosyaları için desteklenen aktarım kapsamı nedir? | 25–26 |
| K-15 | Entegrasyon hatasının iş sahibi, yeniden işleme yetkilisi ve günlük mutabakat onaylayıcısı kim? | 24.5 |

**Sonuç:** Yeni modül eklemek yalnızca menü ve kiralama ekranları oluşturmak değildir. Başarı ölçütü; mevcut araç/cari verisini çoğaltmadan kullanmak, ortak araç meşguliyetini korumak, mali sonuçları tek kaynağa bağlamak, belge yetkilerini tüm geçişlerde uygulamak ve mevcut modüllerin davranışını bozmadan güncellenebilmektir. Bu revizyon entegrasyonun ürün analizini genişletir; uygulama kodu, veritabanı değişikliği veya çalışan entegrasyon teslimi içermez.
