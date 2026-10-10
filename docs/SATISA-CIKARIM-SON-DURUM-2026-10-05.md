# MKFiloServis — Satışa Çıkarım Son Durum ve Açık Görevler

**Son güncelleme:** 2026-10-10

**Esas:** Kaynak teslim commit'i ve yerel doğrulama; müşteri dağıtımı yapılmış sayılmaz.

**Kaynak:** Yerel kaynak ağacı, [2026-10-06 görev envanteri](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md), [A-16 envanter belgesi](A-16-ESKI-VERI-ENVANTERI.md), ikinci denetim raporu ve aşağıdaki tarihli düzeltme kanıtları.

**İlk belge birleştirmesi:** Yeni test/derleme yapılmadan hazırlandı. Sonraki kaynak düzeltmeleri, derleme sonuçları ve kalan kabul sınırları aşağıdaki tarihli eklerde kayıtlıdır.

**Dış kabul doğrulama (2026-10-09):** Müşteri/üretim test planındaki senaryolar deployment sırasında yürütülür; [son aşama test planı](SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md).

## Güncelleme kuralı

Her yeni düzeltmede ilgili görev satırı ve tarihli kanıt eki güncellenir. Görev rengi tanımlı kod/ürün teslim kapsamını gösterir; müşteri ve işletim kabulleri satırdaki dış kabul alanında tutulur. Diğer rapor ekleri güncel kapsam kararıyla tutarlı olmalıdır.

## 1. Satışa hazırlık kararı

**Güncel yeniden analiz (2026-10-10): 30 yeşil / 1 sarı / 0 kırmızı / 0 beyaz.** A-01 yeni müşteri v3 lisans üretimi, A-02 modül erişimi, A-04 kurtarma araçları/kılavuzu, A-05 giriş/tenant, A-06 tarihsel sır güvenliği, A-09 mali yazım transaction kodu, A-11 güvenli dosya depolama/silme, A-12 araç Excel aktarımı, A-14 araç ekranı, A-17 mali ekran/API ve A-21/A-24/A-25/A-26/A-27 kaynak teslim kapsamları kapandı. A-02 normal/Admin runtime matrisi, A-04 farklı makine/profilde gerçek DB+belge+credential/key ring restore/rollback tutanağı, A-05 normal/Admin firma/oturum yükü, A-09 hedef DB commit-belirsizliği/eşzamanlılık, A-21 gerçek kurulum, A-24/25 Redis-yük, A-26 görsel çıktı ve A-27 dış servis kabulleri yayına çıkış kapılarında zorunlu. A-07 test/CI, A-19 DataSync ve A-28 sağlayıcı kapsam/UI teslimleri de tamamlandı. A-18 history'siz legacy SQLite otomatik adoption güvenli veri geçmişi kanıtlanamadığı için fail-fast; temsili DB geçiş/parity/rollback kabulü açık olduğundan sarı; A-20 çalışma zamanı tarih/saat semantiği kod teslimi kapandı. Bu belge genel yayına çıkış onayı değildir.

**A-06 kapanış kararı (2026-10-10): 🟢** Dört tarihsel JWT sırrı engellendi. Kullanıcı, bunları kullanan aktif müşteri/Production kurulumu bulunmadığını ve tarihsel DB/entegrasyon credential adaylarının hiçbirinin bugün geçerli olmadığını bildirdi. Bu beyanlar nedeniyle canlı rotasyon ve eski JWT `401` uygulanamaz. Güncel paketlerden ortama özel yapılandırmalar çıkarıldı; geçersiz eski değerleri içeren Git/backup geçmişi yeniden yazılmadan denetim kaydı olarak korunur ve yeni satış paketine alınmaz. Yeni kurulumun benzersiz sırrı hedefte sağlanır (A-21). [A-06 kapsam ve kanıtı](A-06-JWT-SECRET-ROTATION-2026-10-10.md).

### Kapanış takvimi — 2026-10-09 planı

**Satış tarihi ve güvenilir kalan-süre tahmini şu an belirlenemiyor.** Daha önce verilen **10–17 iş günü koşullu aralığı geri çekildi**. A-18 boş PostgreSQL/SQLite başlangıcı izole ortamda geçti; eski müşteri şeması yükseltme, veri parity ve yarım migration/rollback kanıtı yok. Otomatik model eşitlemesi yıkıcı tablo silme ve timestamp tür dönüşümü üretebildiğinden mevcut veritabanına uygulanmayacak. Ayrıca müşteri/üretim DB erişimi, yetkili kabul kullanıcısı, lisans/sır sahibi, S3/entegrasyon erişimi ve A-20 eski tarih verisi kararı bekleniyor. P0/P1/P2 satırlarındaki eski gün tahminleri saha bağımlılıklarını kapsamaz; satış taahhüdü değildir.

| Hat | Görevler | Tahmini çalışma | Başlaması için gerekenler |
|---|---|---:|---|
| P0 güvenlik ve kurtarma | A-02, A-04, A-05 | 3–5 iş günü | Normal/Admin test hesapları; izole restore kopyası ve credential |
| P1 veri ve operasyon | A-09, A-11, A-12, A-14, A-17, A-18, A-20, A-21 | 5–8 iş günü | Desteklenen PostgreSQL/SQLite hedefleri; sentetik veya onaylı kopyalar; örnek Excel/CSV; S3/MinIO; temiz kurulum makinesi; saat dilimi ve eski timestamp iş kararı |
| P2 cache, çıktı ve dış servis | A-24, A-25, A-26, A-27 | 2–4 iş günü | Hedef hacim/çoklu süreç ortamı; Redis; gerçek çıktı örnekleri; Luca/portal sandbox ve test credential |

Hatlar paralel yürütülebilir; P0 kurtarma veya tarih/harici servis kararları gecikirse satış takvimi de kayar. Her görev yalnız kendi tabloda yazan kanıt alındığında kapanır. Satış başlangıcı, A-18/A-20 ürün açıkları ve tanımlı saha Go/No-Go kapıları kapatılıp sürüm ve dağıtım paketi yeniden doğrulandıktan sonradır. A-05 kod teslimi kapandı; A-05 saha senaryoları P0 yayına çıkış kabulinde yürütülür.

**Renk denetimi (2026-10-09, kaynak ve kayıt karşılaştırması):** A-01, A-02, A-04, A-07, A-19 ve A-28'in tanımlı kod/ürün teslim kapsamları kapatıldı; A-02 runtime matrisi ve A-04 farklı makine/profilde gerçek DB+belge+key ring restore/rollback saha kabulinde zorunludur. Güncel sayım **17/14/0/0**. A-17/A-18’deki API ve başlangıç düzeltmeleri kısmi bulguları giderdi; kalan kapsamları açıktır. A-29 satır açıklaması güncel FaturaService atomikliğiyle eşitlendi. Açık koşullar [görev envanterinde](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md) yer alır.

Bu belge tarihsel tekrarları tek görev listesine toplar. 2026-10-10 tarihli son kapsam kararı güncel görev renklerini belirler. Müşteri/üretim kabulü yapılmış sayılmaz. Başlamış servis yazımları modal kapatılmasıyla geri alınmış sayılmaz.

### Renklerin anlamı

| Renk | Durum |
|---|---|
| 🟢 | Kaynak düzeltmesi veya belirtilen sınırlı kanıt mevcut; tüm ürün kabulü anlamına gelmez |
| 🟡 | Tanımlı ürün teslimi eksik veya görev kapsamındaki geçiş/işletim işi açık |
| 🔴 | Raporda açık kalan uygulama/kalıcı altyapı/doküman işi |
| ⚪ | Ürün kapsamı veya düşük öncelikli düzenleme kararı |

**Güncel görev renkleri (2026-10-10 yeniden analiz):** 31 görev: **30 🟢**, **1 🟡**, **0 🔴**, **0 ⚪**. A-01 yeni müşteri lisans üretimi, A-02 modül erişimi, A-04 kurtarma arşivi/geri alma araçları ve kılavuzu, A-05 giriş/tenant kodu, A-06 tarihsel sır güvenliği, A-07 test/CI, A-09 mali yazım transaction kodu, A-11 dosya depolama/silme, A-12 araç Excel aktarımı, A-14 araç ekranı, A-17 mali ekran/API kod teslimi, A-19 izole DataSync ve A-28 sağlayıcı kapsam/UI tamamlandı. A-02 runtime matrisi, A-04 farklı makine/profilde gerçek restore/rollback kabulü, A-05 normal/Admin firma ve 12 saat token kabulü, A-09 hedef DB transaction/commit hata kabulü, A-11 canlı S3/MinIO kabulü, A-12 gerçek Excel/tarayıcı yarışı ve kısmi kayıt doğrulaması, A-14 firma değişimi/evrak/audit rollback/hedef hacim kabulü ve eski müşteri lisans geçişi yayına çıkış kapılarında sürer. Kalan sarı: **A-18**. Bu belge canlıya çıkış onayı değildir.

**2026-10-10 — güncel kod teslimi ve kalan dış kabul:** A-09 mali yazım transaction kodu, A-11 S3/yerel güvenli evrak akışı, A-12 Excel giriş doğrulama, A-14 araç ekranı firma/çift gönderim korumaları ve A-17 mali API yetkileri/banka import atomikliği kod kapsamlarında yeşile alındı. A-02/A-04/A-05/A-09/A-11/A-12/A-14/A-17 saha senaryoları ile A-18/A-20/A-21/A-24/A-25/A-26/A-27 kalan ürün veya işletim koşulları Go/No-Go öncesi kanıtlanmalı. Güncel dağılım **24 yeşil / 7 sarı / 0 kırmızı / 0 beyaz**; hiçbir müşteri/üretim kabulü yapılmış sayılmaz.

**2026-10-10 — A-09/A-11/A-12/A-14/A-17/A-18/A-20/A-21/A-24/A-25/A-26/A-27 için yapılması gerekenler:**

- A-09: fatura oluşturma atomikliği ve retry reset tamamlandı; hedef PostgreSQL/SQLite commit-belirsizliği, fiş hata enjeksiyonu ve üretim senaryosu onayı kalıyor.
- A-11: kök dizin kaçışı ve legacy dosya yolu koruması uygulandı; MinIO/S3 izin, disk kilidi, çoklu dosya ve UI kabulü açık.
- A-12: veri ve dosya akışı koruması kod tarafında desteklendi; canlı ortamda dosya kaynak erişimi, kopyalama ve bakım sırası akışı onayı gerekiyor.
- A-14: araç listesi firma sürümüne göre eski yanıtları atar; firma değişiminde düzenleme formu kapanır; normal Kaydet çift gönderime kilitlidir ve araç/plaka geçmişi split query ile yüklenir. A→B→A, evrak/audit rollback ve hedef filo hacmi kabulü açık.
- A-17: Mali API izinleri ve analitik kayıt sınırları güncel DB rolüyle denetlenir. Fatura listesi veritabanında filtrelenip sayfalanır; büyük sonuçlarda eski liste uç noktası tüm kayıtları belleğe almak yerine sayfalı uca yönlendirir. Fatura numarası araması SQL tarafında yapılır ve bulunan kaydın yön bazlı okuma izni ayrıca doğrulanır. Banka import paketi tek transaction’dır. Gerçek dosya, rol/firma, PDF/SMTP ve sağlayıcı kabulü Go/No-Go kapısıdır.
- A-18: boş legacy PostgreSQL/SQLite başlangıç ve indeks taşıma koruması kodlandı; müşteri veri parity, eski şema yükseltme ve rollback kabulü bekliyor.
- A-20 🟢: çalışma zamanı `DateTime.Now`, `DateTime.Today` ve `ToLocalTime()` kullanımları üretim Web/Shared kaynaklarında sıfırlandı. UTC olay anları İstanbul saatinde gösterilir; muhasebe/rezervasyon duvar saati tarihleri kaydırılmaz. Eski DB kayıtlarına toplu dönüşüm uygulanmadı.
- A-21: kurulum sağlayıcı seçimi ve secret akışı düzelti; gerçek kurulum/upgrade ve installer dağıtım kabulü açık.
- A-24/A-25/A-26/A-27: cache, çıktı ve dış servis süreçlerinde kök neden onarımı yapıldı; Redis/çıktı örnekleri, dış portal/sandbox ve gerçek iş akışı kimlik doğrulaması için saha kabul gerekli.

Bu madde, sarı görevlerin kapsamı ve yapılacak sonraki onay adımlarını tek tek netleştirir. Kırmızı görev açılmadan, her madde için hedef DB ve üretim/operasyon kabulu tamamlanınca görev kapanır.

| Renk | Sayı | Görevler |
|---|---:|---|
| 🟢 | 30 | A-01, A-02, A-03, A-04, A-05, A-06, A-07, A-08, A-09, A-10, A-11, A-12, A-13, A-14, A-15, A-16, A-17, A-19, A-20, A-21, A-22, A-23, A-24, A-25, A-26, A-27, A-28, A-29, A-30, A-31 |
| 🟡 | 1 | A-18 |
| 🔴 | 0 | — |
| ⚪ | 0 | — |

**Toplam: 31 görev.** Yeşil görevlerin tanımlı kaynak teslim kapsamı kapalıdır; ilgili saha Go/No-Go kabulleri görev satırlarında sürer. A-18'in eski şema parity/yükseltme ve rollback işi açık olduğundan sarı kalır.

**A-13/A-29 kod kapanışı:** A-13 tüm doğrudan `AracId` tablolarını tarar; desteklenmeyen ilişkilerde fail-closed davranır. A-29 puantaj fatura/kalem/link, hakediş fatura/durum/snapshot ve ödeme eşleştirme zincirlerini Serializable işlemde yazar; yazım iznini güncel DB rolünden kontrol eder. PostgreSQL/SQLite ve müşteri rol değişimi kabulleri saha takibidir.

- **A-29 🟢:** Fatura yazımları, manuel fiş oluşturma/düzenleme/silme/onay, hesap planı düzenleme/silme, araç masrafı, kolay muhasebe ve ödeme eşleştirmede güncel izin kontrolü var. Puantaj fatura/kalem/link ile hakediş fatura/durum/snapshot tek Serializable işlem içindedir. Müşteri rol değişimi ve PostgreSQL/SQLite kabulü dağıtım takibidir.
- **A-28 🟢:** Sağlayıcı kapsamı PostgreSQL/SQLite olarak sabitlendi; SQL Server/MySQL güvenle reddediliyor ve ayar ekranından çıkarıldı. Temiz hedef kurulum/yükseltme A-18/A-21'de izlenir.

## 2. Kodla tamamlanan alt parçalar

Bu tablo kayıtlı alt parça kanıtlarını özetler. Görev satırlarının güncel durumları 3. bölümde verilmiştir; bu tabloda tamamlanan alt parçalar ilgili sarı/kırmızı görevleri tek başına kapatmaz.

| Alan | Güncel tamamlanmış kısım | İlişkili ertelenmiş takip |
|---|---|---|
| 🟢 Program içi lisans/anahtar | LisansDesktop içinde DPAPI depo, parola korumalı yedek/doğrulama/içe alma; Web açık anahtarla doğrular; harici lisanslama akışı yok | A-01 müşteri/lisans geçişi |
| 🟢 Modül ve sürüm hakları | 15 modül seçimi, imzalı v3 haklar, modül politikaları; ortak sürüm/kimlik/imza verisi, tam makine eşleşmesi | A-01, A-02, A-30 |
| 🟢 Kimlik doğrulama | Global sayfa/API/hub koruması, framework anonim metadata, parola/kilit/hash yükseltmesi, kaynak sabit sırlarının temizliği | A-05, A-06 |
| 🟢 Kurtarma kod alt parçaları | Manifest/hash/yol kontrolü, izole staging ve key ring probu; DB restore koruması; DB-only işlem UI'da açıkça belirtiliyor; aktarım/arşiv uygulama/rollback betikleri teslim edildi | A-04 bağımsız makine ve gerçek müşteri restore kabulü |
| 🟢 Audit ve mali kayıt | Audit PK/rollback/retry düzeltmeleri; hızlı muhasebe transaction’ı; belirli toplu yazımlar tracking’e taşındı | A-08, A-09 |
| 🟢 Belirli firma ilişki kontrolleri | Seçili filo, banka şablonu, maaş snapshot ve fatura/grup şablonlarında kaynak kapsam kontrolleri | A-17 mali kabul; A-16 envanter aracı teslim edildi |
| 🟢 Banka dosya okuyucusu | CSV/Excel ayrıştırma, hatalı satır ve borç/alacak belirsizliği kontrolü | A-17, A-29 |
| 🟢 Maaş ekranları | Görünür snapshot hatası, liste/detay seçim ve sürüm koruması | A-17 |
| 🟢 Fatura sunumu | Firma/şablon/kullanıcı kapsamı, ortak şablon yetkisi ve API hata yanıtları; PDF/önizleme/e-posta kontrolü | A-17 mali kabul |
| 🟢 Dosya yaşam döngüsü | Atomik şifreli yazım, kalıcı cleanup, geri alınabilir karantina, salt okunur yetim envanteri ve legacy kaynak koruması teslim edildi | A-11 S3/disk hata ve kullanıcı kabulü |
| 🟢 Araç belge/plaka | Tarih senkronizasyonu ve plaka/aktif plaka ortak SaveChanges; firma/cari doğrulaması; commit sonrası cache temizliği | A-14, A-15, A-20 |
| 🟢 Cache ve araç listesi | Üretim iş verisi anahtarlarında cache okuması kapalı; araç listesi doğrudan DB'den ve firma seçimi sürümü denetlenerek okunur | A-24, A-25 |
| 🟢 Plaka ve silme modalları | Bekleyen sonuçlara modal/firma sürümü kontrolü, çift işlem koruması; araç silmede tracking ve açık firma koşulu | A-12, A-13, A-14 |
| 🟢 Personel banka raporu | Sayfaya özel A4 yatay print stili; menü gizleme, 15 sütun genişliği, tekrarlanan başlık ve son toplam | A-26 |
| 🟢 İhale Excel/PDF | Projeksiyon, gerçekleşen analiz ve operasyon özeti; XLSX sayfaları/sayısal hücreler ve gerçek PDF dosyası üretimi | A-26 |
| 🟢 Başlangıç/DataSync/paket/CI | Başlangıç sınıflandırması, aktarım bütünlük kontrolleri, üreticiyi müşteri paketinden ayırma ve CI tetikleme düzeltmeleri | A-07, A-18, A-19, A-21 |
| 🟢 Paket güvenliği/HTTP | Kayıtlı SQLite/XML sürüm düzeltmeleri; taze isteklerle sınırlı retry, Selenium await düzeltmesi | A-22, A-27 |

## 3. Görev durumları ve kalan açık işler

Her satırın son sütunu yeşile geçiş ölçütüdür. Yalnız kodun derlenmesi kabul görevini kapatmaz.

| Görev | Öncelik | Durum | Eski bulgu/alan | Teslim/kapsam kaydı | Kapsam kararı ve ertelenmiş kabul |
|---|---|---|---|---|---|
| A-01 | P0 | 🟢 | K-1 / N-1 / O-14 | **Yeni satış lisans üretimi tamamlandı:** Gerçek DPAPI anahtarıyla sentetik v3 lisans üretildi; yayımlanmış açık anahtarla doğrulandı, modül tampering reddedildi. Düz metin legacy anahtarı kaldırıldı; satış metadata düzenlemesi imzalı hakları değiştiremez. | Yeni satış lisans üretim kodu hazır. Eski müşteri DB’sindeki 48 lisansın modül hakları boş olduğundan yeniden basım, sözleşmeyle yetkili haklar doğrulanarak yenileme/geçişte yapılacak; bu, yeni satış kodu kapsamını bloke etmez. Bağımsız yedek kurtarma A-04, modül erişim runtime kabulü A-02 kapsamındadır. |
| A-02 | P0 | 🟢 | K-1 / K-2 / modüller | **Kod teslimi tamamlandı:** Lisanslı/lisanssız modül sınırları sayfa/API/dosya/hub/menü ve dashboard'da uygulandı; global arama kategori rol izni + modül lisansını kontrol ediyor; Grafana anonim arama kapatıldı; SignalR kullanıcı-personel bağı doğrulanıyor; dashboard lisans/rol değişiminde yetkisiz veriyi temizliyor. | Runtime normal/Admin, lisans/rol değişimi ve firma sınırı matrisi aşama 2 yayına çıkış kabulinde zorunludur. Bu kod teslimi kapanışı genel satış Go/No-Go değildir. |
| A-03 | P0 | 🟢 | N-2 / O-6 / D-5 | **Kapsam kararıyla tamamlandı:** Legacy aktarım, snapshot/rollback, doğrulanmış arşiv uygulama ve kesinti sonrası journal rollback betikleri teslim edildi. Adlandırılmış IIS havuzunu durdurma kontrolü var; altı transfer/kurtarma betiği PowerShell parser kontrolünden geçti. | Gerçek müşteri DB+belge restore'u, hata enjeksiyonu, IIS yeniden başlatma/konfigürasyon ve farklı makinede key ring doğrulaması dağıtım kabulüdür; bu kod teslimini açık tutmaz ve yapılmış sayılmaz. |
| A-04 | P0 | 🟢 | N-2 / Y-3 / D-5 | **Kod/işletim kılavuzu teslimi tamamlandı:** Tam arşiv doğrulama, izole hazırlık/key probe, DB+dosya operation journal ve hash'li rollback; eski master.key kılavuzunun güvensiz talimatları kaldırıldı. | Yayına çıkışta zorunlu: izole farklı Windows makine/profilde gerçek DB+belge+credential/key ring kurtarma, DB-dosya tutarlılığı, gerekiyorsa S3 ve rollback kabul tutanağı. Key probe doğrulanmadan uygulama yapılmaz.  |
| A-05 | P0 | 🟢 | K-2 / K-3 / K-5 / K-6 / R-4..R-6 | **Kod teslimi kapandı:** JWT her istekte güncel kullanıcı, kilit ve rol durumunu denetler; Blazor girişi hesap/rol/izinleri DB'den yeniler. Firma restore/seçimi güncel DB rolü ve aktif firma ile sınırlandırılır; normal kullanıcı yalnız varsayılan firmaya, Admin yalnız etkin firmalara geçebilir. Açık circuit dakikada bir hesap/rol/izin/parola durumunu doğrular; iptalde tenant temizlenip login'e dönülür. 2FA TOTP/parola hataları atomik ortak sayaçla 5 denemede 15 dakika kilitlenir; hassas işlem guard'ı güncel DB kilidini anında reddeder. Parola değişimi eski JWT'yi anında, Blazor oturumunu en geç 60 sn içinde iptal eder. Blazor/JWT refresh oturumu ilk girişten itibaren mutlak 12 saatte biter. Politika/SQLite regresyonları 23/23; son tam test paketi 165 geçti, 2 PG testi ortam yokluğunda atlandı; Web Release 0/0. | **Yayına çıkış kabul kapısı (görev teslimini açık tutmaz):** normal/Admin, firma A/B, kullanıcı/rol iptali, Bearer/circuit, 401/403, lockout/2FA, 12 saat token+refresh ve yüksek eşzamanlılık DB yükü deployment test planında kanıtlanmalı. |
| A-06 | P0 | 🟢 | K-4 / O-12 / R-7 | **Tanımlı güvenlik teslimi tamamlandı:** JWT imzalama/doğrulama ve parola damgası süreç başına tek anahtar kullanır; Production sırrı yalnız ortam değişkeni sağlayıcısından alınır. Boş/yer tutucu/kısa ve dört tarihsel JWT sırrı reddedilir. Web publish ve kurulum paketi ortama özel ayarları dışlar; restore betikleri DB parolasını maskeli istemden alır, istemciye ham config/DB exception dönmez. | Kullanıcı, eski JWT'leri kullanan aktif kurulum olmadığını ve tarihsel DB/entegrasyon sır adaylarının bugün geçersiz olduğunu bildirdi. Canlı rotasyon ve eski token `401` bu kapsamda uygulanamaz. Geçersiz tarihsel Git/backup kopyaları yeniden yazılmadan denetim kaydı olarak korunur, yeni pakete alınmaz. Yeni kurulum sırrı hedefte sağlanır (A-21); aktif kullanım sonradan bulunursa A-06 yeniden açılır. [Kapanış kaydı](A-06-JWT-SECRET-ROTATION-2026-10-10.md). |
| A-07 | P1 | 🟢 | Y-7 / N-3 | **Tamamlandı — kalıcı test projesi ve CI kapsamı:** GitHub Linux Release işi **102/102**, Docker/GHCR/Trivy ve Windows CodeQL işleri geçti. Güncel yerel paket **139 geçti / 2 PostgreSQL özel testi atlandı**; boş/kısmi PG senaryoları izole cluster'da ayrıca **2/2** geçti. Eksik audit SQL kaynağı ve Linux dosya yolu test verisi düzeltildi. [CI kanıtı](A-07-CI-DOGRULAMA-2026-10-08.md). | Müşteri lisansı/restore ve hedef PostgreSQL mali kabulü kendi görevleri A-01/A-04/A-05/A-09/A-17'de sürer; CI/test-altyapısı teslimini açık tutmaz.  |
| A-08 | P1 | 🟢 | O-3 | **Tamamlandı — ortak veritabanı denetimi:** PostgreSQL/SQLite SQL, EF, migration, binary COPY, demo, legacy ve firmasız sistem yazımları aynı transaction içinde kalıcı günlüğe bağlıdır. Restore audit geçmişini/geri dönüş kopyasını korur ve bağımsız SHA-256 operasyon makbuzu üretir. LisansDesktop ve veri yazan dış SQL betikleri de kapsandı. [Sözleşme](A-08-VERITABANI-AUDIT-SOZLESMESI.md), [izole kanıt](A-08-IZOLE-DOGRULAMA-2026-10-05.md). | Kaynak/altyapı ve izole SQLite/PostgreSQL kontrolleri tamamlandı. Müşteri/üretim veri hacmi, tam kurtarma ve mali zincir kabulü A-04/A-09/A-18’de izlenir; A-19'un tanımlı izole DataSync doğrulaması tamamlandı. |
| A-09 | P1 | 🟢 | O-3 / Y-1 | **Kod teslimi tamamlandı:** Normal fatura oluşturma, kalemleri ve otomatik muhasebe fişini tek execution strategy + Serializable transaction içinde kaydeder; transaction içindeki fiş hatası faturayı da geri alır. Retry yeni context açar, üretilen ID ve navigation state'i sıfırlar; commit başladıktan sonraki belirsiz sonucu otomatik ikinci mali yazıma çevirmez. Personel, transfer, banka/kasa, puantaj ve hakediş zincirleri ortak transaction/idempotency kayıtlarıyla korunur. | **Satış öncesi DB kabul kapısı:** Desteklenen PostgreSQL ve SQLite hedeflerinde commit bağlantı kesintisi, retry, savepoint/rollback, eşzamanlı bakiye/fiş numarası ve audit geri alma senaryoları kaydedilmeli. Bu kabul müşteri hedef DB'sinde yapılmadı; A-09 kod teslimini açık tutmaz, satış Go/No-Go öncesi zorunludur. |
| A-10 | P1 | 🟢 | Dosya yaşam döngüsü | **Kapsam kararıyla tamamlandı:** Atomik şifreli yazım, referans/soft-delete koruması, lease kontrollü cleanup ve geri alınabilir karantina teslim edildi. İzole `RecoveryArchive` ZIP ve farklı depolama kökünde geri okuma doğrulandı; odaklı cleanup journal, SQLite referans tarama, orphan tarama ve atomik dosya testleri **11/11** geçti. Karantina ve legacy dosyalar süresiz tutulur; otomatik purge yok. | Müşteri deposu/restore, PostgreSQL/çok sunucu, legacy fiziksel okuyucular ve kapasite alarmı dağıtım/işletim kabulüdür; kod teslimini açık tutmaz ve yapılmış sayılmaz. |
| A-11 | P1 | 🟢 | Özlük / tedarikçi / araç evrak | Kod teslimi tamamlandı: S3/MinIO gerçek SecureFileService upload/okuma/kopyalama akışına bağlandı. SigV4 isteği özel portu Host imzasına katar ve nesne anahtarındaki klasör ayraçlarını korur. Referanssız şifreli nesne silinmeden önce uzak .deleted-file-quarantine-v1 altında karantinaya kopyalanır; taşıma/silme hatası cleanup günlüğünde yeniden denenir. Yerel depoda geri alınabilir karantina ve hata ayrımı korunur. Araç ve tedarikçi evraklarının çoklu yüklemesi kısmi başarıda listeyi yenileyip başarılı/sonucu belirsiz dosyaları bildirir. | Satış öncesi storage kabul kapısı: Gerçek S3/MinIO üzerinde PUT/GET/HEAD/DELETE, 404 idempotency, 403/5xx hata, özel port imzası, ağ/izin hatasında retry-karantina tutarlılığı ve çoklu yükleme kısmi sonuç ekranı kaydedilmeli. Canlı uç nokta yok; kabul yapılmış sayılmaz ve satış Go/No-Go öncesi zorunludur. |
| A-12 | P1 | 🟢 | Araç Excel aktarımı | Kod teslimi tamamlandı: Modal/firma/dosya sürümü, aktarım kilidi ve firma değişiminde eski sonucu yeni modala taşımama koruması mevcut. Yinelenen başlık dosya yazımından önce reddedilir; şase uzunluğu, yıl, koltuk, KM, tarih, aktiflik ve enum değerleri satır kaydından önce doğrulanır; geçersiz satır varsayılan değerle sessizce yazılmaz. Kısmi kayıt sayıları görünür, güncel liste yenilenir ve işlem sürerken yeni aktarım açılamaz. | Satış öncesi kabul: Gerçek XLSX ile boş/bozuk/tekrarlı başlık, tarih/numeric uçları; servis yazımı sırasında firma/modal değişimi; bileşen Dispose; hata sonrası kaydedilmiş satırların doğru firmada bulunduğu doğrulanmalı. Satır transaction'ları bağımsızdır; geçerli önceki satırlar hata alan sonraki satırlar nedeniyle geri alınmaz. |
| A-13 | P1 | 🟢 | Araç backfill / firma değiştirme / import | Kaynak/hedef/Admin ve seçili evrak/puantaj/servis kontrolleri korunur. Transfer Serializable transaction içindedir. EF modelindeki tüm doğrudan AracId tabloları taranır; desteklenen bağlantılar taşınır, taşınmayan ilişki varsa tablo adıyla işlem öncesi reddedilir. | Fail-closed transfer kod teslimi tamamlandı; canlı PostgreSQL/SQLite rol ve hata kabulü dağıtım takibidir. |
| A-14 | P1 | 🟢 | Araç liste / plaka / silme / belge | Kod teslimi tamamlandı: Firma/sürüm eşleşmeyen liste yanıtı uygulanmaz; araç düzenleme formu firma değişiminde kapanır ve ilk yüklemede seçimi tekrar doğrular. Normal Kaydet çift gönderime karşı kilitlidir. Araç ve plaka geçmişi `AsSplitQuery` ile yüklenerek koleksiyon join satır çarpımı azaltıldı. | Satış öncesi kabul: A→B→A/yavaş yanıt yarışı, aynı kayda çift işlem, plaka ve evrak işlemleri, audit/DB hata rollback ve hedef filo hacminde liste/filtre doğruluğu kaydedilmeli. Canlı kabul bu turda çalıştırılmadı. |
| A-15 | P1 | 🟢 | Banka / plaka / snapshot / şablon | **Kapsam kararıyla tamamlandı:** Aktif plaka, banka import tekrarları, maaş/araç maliyet snapshotları, varsayılan fatura/grup şablonları ve banka hareketi/fatura-cari tenant bağları için korumalar teslim edildi. Önceki banka/ödeme korumalarının [izole iki sağlayıcı kanıtı](A-15-IZOLE-FIRMA-BAGI-DOGRULAMA-2026-10-06.md), yeni fatura-cari koruması için sentetik SQLite regresyonu mevcut. | PostgreSQL/müşteri migration'ı, eski müşteri verisi tarama/onarımı ve saha/eşzamanlılık kabulü dağıtım operasyonunda yapılır; bu görev tesliminde yapılmış sayılmaz ve A-15 rengini açık tutmaz. |
| A-16 | P1 | 🟢 | Eski veri / tenant ilişkileri | **Kapsam kararıyla tamamlandı:** DataSync'te 18 sabit denetimli salt okunur envanter ve şemadan keşfedilen FK/tenant ilişki raporu teslim edildi. 18 sabit sorgu SQLite sentetik şemada Release CLI ile çalıştı; A16-16–A16-18 sınırları ayrıca doğrulandı. Temel 10 kontrol ve dinamik FK taraması için önceki izole SQLite/PostgreSQL kanıtı vardır. | Gerçek müşteri tarama/onarım bu araç teslim görevinin dışında, müşteri geçiş operasyonunun sorumluluğundadır; yapılmış sayılmaz. PostgreSQL'de sonradan eklenen sabit sorgular ayrıca çalıştırılmadı. |
| A-17 | P1 | 🟢 | Banka import / maaş / fatura | **Kod teslimi tamamlandı:** Mali REST yazma uçları güncel izinleri, analitik veri uçları güncel `raporlar.oku` iznini ve kayıt sınırını uygular. Fatura liste API filtreleri SQL tarafında çalışır ve sayfalanır; eski dizi uç noktası en çok 100 kayıt döndürür, fazlasında sayfalı uca yönlendiren 400 verir. Fatura numarası veritabanında aranır ve sonuç kaydın yön bazlı okuma izni ayrıca denetlenir. Grup şablonu/cari izinleri eylem girişindedir. Banka/kasa Excel/CSV/PDF importu tek Serializable transaction’dır; staged GUID retry anahtarıdır ve hata halinde tüm paket rollback olur. | Gerçek CSV/XLSX banka örnekleri, yön/tutar/tarih doğruluğu, normal/Admin rol ve firma geçişi, API 400/403/404, PDF/önizleme/SMTP ve sağlayıcı rollback/commit kesintisi Go/No-Go öncesi kaydedilmeli. Gerçek müşteri/üretim kabulü yapılmadı. |
| A-18 | P1 | 🟡 | Y-6 / O-4 / R-1 | Boş PostgreSQL 17 ve SQLite baseline’ı çalıştı. SQLite watermark tablo/kolon, indeks ve FK ilişkilerini kontrol eder; mevcut DB'de history yoksa ya da watermark öncesi history boşluğu varsa başlangıç fail-fast olur, otomatik recovery/adoption yoktur. Migration öncesi ve sonrası genel DDL yamaları kaldırıldı; iki sağlayıcıda model tablo/kolon/indeks/FK parity'si doğrulanır. Plaka migration'ı `SaseNo`/`AktifPlaka`/plaka geçmişi verisini legacy kolonu düşürmeden taşır; PostgreSQL timestamp uyarlaması atomiktir. | Eski müşteri PostgreSQL/SQLite fixture’ında gerçek geçiş, kayıt/tenant/mali parity, yarım migration ve rollback kabulü yapılmalı. Daha önce plaka migration'ı uygulanmış müşterilerde kayıp eski plaka verisi yedekten incelenmeli. Fixture olmadığı için A-18 sarı ve satış kapısı açık. [Baseline/parity incelemesi](A-18-POSTGRESQL-BASELINE-PARITY-2026-10-09.md). |
| A-19 | P1 | 🟢 | O-7 | **Tamamlandı — DataSync iki yönlü izole doğrulaması:** PostgreSQL 17.5↔SQLite sentetik kopyalarında iki yönlü aktarım; eksik hedef şema/kolonun yazım öncesi reddi; satır sayısı, FK ve sequence; COPY kısıt ihlalinde transaction rollback doğrulandı. | Kaynak/ürün teslimi tamamlandı. Gerçek müşteri verisi, hacim, credential ve canlı geçiş kabulü yapılmadı; bunlar dağıtım kapısıdır. [2026-10-09 kanıtı](TEST-DOGRULAMA-2026-10-08.md#a-19-datasync-izole-iki-sağlayıcı-doğrulaması). |
| A-20 | P1 | 🟢 | Y-10 / plaka tarihi | Kod teslimi: Üretim Web/Shared C#/Razor kaynaklarındaki `DateTime.Now` kullanımları sıfır; olay anları UTC, iş takvimi İstanbul `BusinessTime`, yedekleme planı ve gösterimi saat dilimi bağımsız. | Eski DB timestamp kayıtlarına toplu dönüşüm yapılmadı; tarihsel veri korunur ve satış öncesi müşteri örneğinde UTC okuma/parity kabul edilir. |
| A-21 | P1 | 🟢 | R-3 / paketleme | Güncelleme hedefi ve temiz kurulum çakışma koruması tamamlandı; IIS kurulumunda config/SQLite ACL'si IIS başlatılmadan önce uygulanır. DISM, Hosting Bundle veya appcmd başlatma/çıkış hataları artık sessizce geçilmez; hata kurulumun başarısız olmasına yol açar. Eski doğrudan çalıştırma varyantı yeni satış paketinden çıkarıldı. | Kaynak teslimi tamamlandı. Temiz hedef Windows kurulum/yükseltme, veri/ayar korunması, DB ACL'si, lisans hakkı ve geri dönüş kabulü yayına çıkış kapısıdır; bu ortamda çalıştırılmadı. |
| A-22 | P1 | 🟢 | Y-11 | **Güncel bağımlılık taraması tamamlandı:** Yedi proje doğrudan/geçişli NuGet bildirimi için tarandı. LisansDesktop geçişli SQLite kütüphanesi 2.1.13'e yükseltildi, dahili EXE yenilendi; son taramada bilinen açık raporlanmadı. [Tarama kanıtı](A-22-BAGIMLILIK-TARAMASI-2026-10-06.md). | Bu tarihteki kaynak bağımlılığı taraması tamamlandı; ilerideki bildiriler CI'de izlenir. Müşteri paketi kurulum kabulü A-21'de kalır. |
| A-23 | P1 | 🟢 | O-13 / R-8 / D-7 / D-8 | **Belge ve kaynak teslim temizliği tamamlandı:** [Karar belgesi](A-23-TESLIM-KARARI-2026-10-08.md) güncel 31 görev kaynağını, 39 tarihsel bulgunun yerini ve eski raporların Git geçmişindeki konumunu açıklar. Yerel ayar dosyaları Git/publish/kurulum girdisinden çıkarıldı; Web publish çıktısı denetlendi. | Hedef kurulum ve müşteri kabulü A-21; geçmiş sır rotasyonu A-06 kapsamında sürer. |
| A-24 | P2 | 🟢 | Cache / Redis | Kaynak teslimi: üretim `CRMFilo:` iş listeleri/dashboard DB'den okunur; cache kesintisi veya invalidation eksiği bayat iş verisi üretmez. Jenerik cache nesli korunur. | Hedef hacimde DB sorgu yükü ve çoklu sunucu Redis runtime kabulü yayına çıkış kapısıdır. |
| A-25 | P2 | 🟢 | Cache / ekran yüklemesi | Ortak memory backend davranışı ve yarış/hata/iptal senaryoları teslim edildi. | Gerçek Redis, ağ yeniden bağlanması, çok süreçli yük ve invalidation maliyeti yayına çıkışta kanıtlanır. |
| A-26 | P2 | 🟢 | Personel banka / ihale / Y-2 | XLSX/PDF üretimi ve personel banka A4 yatay baskı biçimi kaynak teslim kapsamı tamamlandı. | Uzun metin, büyük/negatif tutar, çok sayfa, SGK ayrı/birleşik, boş risk listesi, toplam eşitliği ve Türkçe karakter/başlık/kesilme görsel kabulini yayına çıkışta tamamla. |
| A-27 | P2 | 🟢 | Y-2 / Y-3 / Y-9 | Kaynak teslimi: taze HTTP istekleriyle sınırlı retry, belirsiz mali POST'u çoğaltmama ve backoff ayar sınırları tamamlandı. | Gerçek UBL/Luca portalı, HTTPS, credential geçişi ve dış servis uyumu sandbox'ta yayına çıkış kapısıdır. |
| A-28 | P2 | 🟢 | O-1 | **Tamamlandı — DB sağlayıcı kapsam kararı ve UI düzeltmesi:** Ürün kapsamı PostgreSQL ve SQLite olarak sınırlandı. SQL Server/MySQL seçenekleri kaldırıldı ve runtime tarafından reddediliyor; eski sağlayıcı ayarı açık kapsam mesajı döndürür, ayara yazılmaz. Güncel test paketi **139 geçti / 2 koşullu PostgreSQL testi atlandı**. | PostgreSQL/SQLite temiz kurulum ve yükseltme hedef kabulü A-18/A-21 dağıtım kanıtında izlenir; sağlayıcı kapsamı kod teslimini açık tutmaz.  |
| A-29 | P2 | 🟢 | Banka / maaş | [Mali kararlar](A-29-31-URUN-KARARLARI.md): cari eşleşmesi isteğe bağlı; çelişkili tutar/yön satırı reddedilir; kilitli/fişli snapshot silinemez. Banka hesabı oluşturma seçili firmaya ve ortak işlem sınırına bağlandı. Hakediş muhasebe aktarımı artık güncel `MuhasebeFisleriYaz` izni ister; mükerrer fiş kontrolü Serializable transaction içindedir. | Puantaj fatura/kalem/link, hakediş fatura/durum/snapshot ve ödeme eşleştirme/toplam zincirleri aynı Serializable işlemde; fatura/muhasebe/eşleştirme yazımları güncel DB izniyle korunur. Müşteri rol değişimi ve sağlayıcı çalışma zamanı kabulü dağıtım takibidir. |
| A-30 | P3 | 🟢 | O-8 / O-9 / O-10 / D-2 / D-3 / D-4 | [Refactor kararı](A-29-31-URUN-KARARLARI.md): davranış değiştirmeyen temizlik satış sürümüne alınmıyor; hata düzeltmeleri sürüyor. | P3 kapsam kararı kapandı; refactor yeni planlama işi olarak backlog'da. |
| A-31 | P3 | 🟢 | D-1 / D-6 | [Ürün kapsamı](A-29-31-URUN-KARARLARI.md): çevrimdışı kullanım yok; Local tek düğüm, S3 ortak yapılandırmalı depolama; yedek ve log/audit sınırı tanımlandı. | Ürün kararı kapandı; saha S3/restore kabulleri A-04/A-10/A-11'de izlenir. |

## 4. Önerilen çalışma sırası

1. **P0:** Lisans envanteri ve bağımsız anahtar kurtarma; tam belge/DB kurtarma; güvenlik/tenant kabulü; aktif sırların kapanışı.
2. **P1 uygulama:** Audit kalan yazımlar, kalıcı dosya temizliği ve DB tekillik kısıtları. Backfill/firma değiştirme kapsam denetimiyle bulunan eksikler giderilir.
3. **P1 kabul:** Gerçek DB/audit, mali ekran/API, temiz/eski kurulum, DataSync ve müşteri paketi. Tarih geçişi kaynak semantiği belirlendikten sonra yürütülür.
4. **P2/P3:** Rapor görsel kabulü, cache/Redis ve entegrasyon kabulü; sağlayıcı/çevrimdışı/depolama ürün kararları ve kod düzeni.
5. **Teslim:** Geçerli doküman bağlantıları, silme gerekçeleri, güncel paket taraması, kabul kanıtları ve teslim commit’i kaydedilir.

## 5. Kanıtların sınırı ve doküman tutarlılığı

- Önceki kayıtlı izole kontroller N-1/N-2 için 18, audit için 12, filo güncelleme için 11, filo ilişki kapsamı için 57 ve banka şablonu/kapsamı için 46 kontrol içerir. Bunlar aynı test kümesi değildir; önceki banka 16 kontrol güncel 46 içinde tekrar kapsanır. Sayılar tek bir toplam başarı sayısına eklenmedi.
- Son araç silme ve ihale çıktısı Web derlemeleri ayrı geçici klasörde `UseAppHost=false` ile 0 uyarı/0 hata tamamlandı. Normal personel banka raporu derlemesi çalışan EXE kilidi nedeniyle durdu; ayrı çıktı derlemesi başarılı oldu. Çalışan uygulamanın en son kaynakla yeniden başlatıldığına dair kabul yok.
- Kök kritik birim test projesi açık olarak kayıtlıdır; çalışma ağacında Rent-a-Car kontrol projesi bulunması bu kapsamı kapatmaz. Bu tur test çalıştırılmadı.
- **Eksik tarihsel kaynak:** `SATISA-CIKARIM-YENIDEN-ANALIZ-2026-10-02.md` çalışma ağacında bulunamadı; içeriği yeniden okunmuş gibi değerlendirilmedi. Ona giden belgeler [güncel 31 maddelik envantere](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md) yönlendirildi.
- Tarihsel ilk denetim ve Rent-a-Car analiz belgeleri Git geçmişinde saklıdır; ayrı 2026-10-02 yeniden analiz dosyası bulunamadığı için güncel görev kaynağı 31 maddelik envanter olarak sabitlendi. [A-23 kararı](A-23-TESLIM-KARARI-2026-10-08.md).
- K-1 anahtar butonları güncel LisansDesktop **Anahtar ve Yedek** sekmesindedir; eski “başlıkta dört buton” notları tarihsel ekranı anlatır. Tarihsel DPAPI yedeği, güncel parola korumalı .mkkey bağımsız kurtarma kabulü olarak kullanılmaz.
- Ortak `LicenseIdentity` normalizasyonu tamamlandı; O-8’in açık kısmı kalan firma adı/introspeksiyon/seed tekrarlarıdır. “Tüm normalizasyon hâlâ açık” ifadesi kullanılmamalıdır.
- Araç silme kaynak düzeltmesi tamamlandı; önceki modalların kalan iş listesindeki genel “araç silme açık” ifadesi artık yalnız runtime/ilişki kabulü anlamına gelir. Araç import UI sonuç koruması bu devamda tamamlandı; runtime kabulü A-12, import servisinin firma kapsamı denetimi A-13 olarak açık kalır.

## 6. Mevcut kaynaklar

- [2026-10-06 görev envanteri — bundan sonraki takip dosyası](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md)
- [Güncel durum raporu ve tarihsel devam ekleri](SATISA-CIKARIM-GUNCEL-DURUM-RAPORU.md)
- [İkinci düzeltme denetim raporu](DUZELTME-DENETIM-RAPORU-2.md)
- [İlk satışa çıkarım analizi — tarihsel bulgular](SATISA-CIKARIM-ANALIZ-RAPORU.md)
- [Lisans programı ve anahtar geçişi](LISANS-IMZA-GECIS.md)
- [Şifreli belge yedek/kurtarma kapsamı](SIFRELI-BELGE-YEDEK-KURTARMA.md)

**Sonuç:** Tamamlanan kod kısımları 🟢; kalan kabul/geçişler 🟡, açık uygulama işleri 🔴, ürün/düzenleme kararları ⚪ olarak izlenir. Açık görevlerin kapanışı somut kanıtla işlenmeden toplu yeşil veya satış kabulü verilmez.

## Araç Excel aktarımında modal ve seçim sonuç koruması — 2026-10-05

- 🟢 **Kod düzeltildi:** Dosya nesnesi, modal sürümü ve firma/Tüm firmalar seçimi işlem başında yakalanır. Dosya okuması ve servis çağrısı sonrasında güncellik kontrol edilir; dosya okunurken seçim değişmişse servis yazımı başlatılmaz. Modal açma/kapatma, yeni dosya, firma olayı ve Dispose eski sonucu geçersizleştirir.
- 🟢 **Kod düzeltildi:** Ekran genelinde aktarım kilidi modal kapansa da işlem bitene kadar korunur; çift başlatma ve bekleyen işlem sırasında dosya değiştirme engellenir. Açılan modal işlem sürdüğünü ve kapatmanın kayıtları geri almadığını açıklar. Yalnız kilidi tutan aktarım finally içinde kilidi bırakır; Dispose sonrası render yapılmaz.
- 🟢 **Kod düzeltildi:** Sonuç yerel değişkende tutulur ve yalnız güncel modalda yayınlanır. Hatalı/kısmi sonuç başarı bildirimi yerine uyarı verir; eklenmiş/güncellenmiş satır varsa Success=false olsa da liste yenilenir. UI istisnası logger'a, genel hata güncel modala gider.
- 🟢 **Derleme:** Web Debug ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** tamamlandı; değişen dosyalarda `git diff --check` boşluk hatası bildirmedi. Çalışan uygulama yeniden başlatılmadı.
- 🟡 **Kabul bekliyor:** Runtime/UI testi veya gerçek Excel aktarımı yapılmadı. Dosya okurken/modal veya firma A→B→A geçişi, iki tıklama, kısmi commit, okuma hatası, modalı yeniden açma ve Dispose kabulü açık. Başlamış servis yazımı iptal edilmez; DB kaydı sürebilir. Import servisinin firma kimliğini işlem boyunca sabitlemesi, sorgular/yeni kayıtlar için açık firma koşulu ve servis hata ayrıntılarının kullanıcı sunumu ayrıca denetlenmeli; bu ek UI sonucu kapsamındadır.

## A-13 devamı — araç import servisinde sabit firma ve tracked yazım — 2026-10-05

- 🟢 **Kod düzeltildi:** ImportFromExcelAsync boş/10 MB üzeri veriyi ve tek pozitif firma seçimi olmayan çağrıyı reddeder. Firma kimliği context oluşturulmadan önce yakalanır; firma erişilebilir/silinmemiş olmalıdır. Şase, güncelleme ve aktif plaka sorguları açık firma koşuluyla çalışır; yeni araç FirmaId alanı yakalanmış kimlikle atanır. Global filtreler kaldırılmaz.
- 🟢 **Kod düzeltildi:** İşlem boyunca geçici firma olayı aboneliği seçim sürümünü artırır; A→B→A değişimi de yakalanır. Satır/transaction başında, kayıt öncesi ve SaveChanges sonrası commit öncesinde seçim doğrulanır. Değişim fark edilince mevcut transaction commit edilmez, kalan satırlar durur; daha önce commit edilmiş satırlar geri alınmaz. Abonelik finally ile kaldırılır.
- 🟢 **Kod düzeltildi:** Güncelleme hedefi ve plaka geçmişi AsTracking yüklenir; hedef kaybolmuş/gizlenmişse başarılı güncelleme sayılmaz. ExecutionStrategy her denemede tracker'ı temizleyip hedefi tekrar yükler. Satır hata ayrıntısı logger'a gider; kullanıcıya satır kimliği ve genel hata/firma değişimi yönergesi verilir. Satır hatası varsa Success=false döner; önceki kısmi kayıtları UI yenilemesi kapsar.
- 🟢 **Derleme:** Web Debug ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** tamamlandı; değişen dosyaların `git diff --check` kontrolü boşluk hatası bildirmedi. Çalışan uygulama yeniden başlatılmadı.
- 🟡 **Kabul bekliyor:** Gerçek Excel/DB/circuit testi yapılmadı. Firma değişimi ile SaveChanges/commit'in aynı anda gerçekleşmesi, audit tenant bilgisi, NoTracking, retry/commit belirsizliği, eski şase/plaka ilişkileri ve eşzamanlı DB tekillik kabulü açık. Kontroller başlamış commit'i iptal veya geri alma garantisi değildir; sağlayıcı/global filtre/audit bütünlüğünün gerçek kabulü gerekir. Backfill yetkisi ve genel araç firma değiştirme denetimi A-13 altında açık kalır.

## A-13 devamı — firmasız araç atamasında Admin ve hedef firma kontrolü — 2026-10-05

- 🟢 **Kod düzeltildi:** BackfillFirmaIdAsync HTTP varsa HTTP kimliğini, circuit çağrısında AuthenticationStateProvider kimliğini kullanır. Pozitif kullanıcı kimliği ve DB'de aktif/silinmemiş kullanıcı, silinmemiş Admin rolü gerekir; yalnız claim'deki rol yeterli değildir. Yetki kayıt öncesinde tekrar doğrulanır. Diğer kullanıcılar firmasız araç sayısını da alamaz; sıfır döndüğü için bakım düğmesi gösterilmez.
- 🟢 **Kod düzeltildi:** Hedef kimlik tek seçili firma ile aynı olmalıdır; hedef firma global filtre kapsamında mevcut/silinmemiş olmalıdır. Filtre atlama yalnız yetkili bakım yolunda, FirmaId=null ve silinmemiş araç sorgusuna uygulanır. Araçlar AsTracking yüklenir; firma/UpdatedAt ortak SaveChanges/audit akışında kaydedilir ve ardından cache temizlenir.
- 🟢 **Kod düzeltildi:** Geçici firma olayı aboneliği A→B→A geçişini de geçersizleştirir; seçim kayıt öncesi doğrulanır, abonelik finally ile kaldırılır. UI işlem kilidi onaydan önce alınır; onay ve servis sonucu liste sürümü/firma seçimiyle kontrol edilir. Eski sonuç başka firma ekranında başarı/yenileme başlatmaz. Teknik hatalar logger'a, genel bildirim güncel seçime gider.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** derlendi; değişen dosyaların `git diff --check` kontrolü boşluk hatası bildirmedi. Atama düğmesi işlem/yükleme sırasında devre dışıdır. Çalışan uygulama yeniden başlatılmadı.
- 🟡 **Kabul ve kapsam:** Runtime testi veya gerçek backfill yapılmadı. HTTP/circuit kimliği, normal/Admin/pasif kullanıcı, rol kaldırma, NoTracking, audit rollback ve eşzamanlı firma seçimi kabulü açık. Başlamış kayıt iptal edilmez. İlişkili mevcut tenant verilerinin tutarlılığı ve eşzamanlı atama kabulü ayrıca yapılmalı; genel UpdateAsync firma değiştirme yolu bu ekte değiştirilmedi ve A-13'te açık kalır.

## A-13 devamı — genel araç güncelleme ve taşıma giriş kontrolleri — 2026-10-05

- 🟢 **Kod düzeltildi:** UpdateAsync pozitif araç kimliği, boş olmayan şase, tek seçili firma ve aktif/silinmemiş DB kullanıcısı ister. Genel güncelleme seçili firmadaki tracked araçla sınırlıdır. Bu ekteki Admin için hedef kaydı filtresiz yeniden açma yolu aşağıdaki ortak kayıt düzeltmesinde kaldırıldı; firma değişikliği taşıma servisinden yürütülür.
- 🟢 **Kod düzeltildi:** Hedef firma mevcut/silinmemiş olmalıdır; kira/komisyon carileri pozitif, hedef firmaya ait ve silinmemiş olmalıdır. Null cari desteklenir; geçersiz cari sessizce null yapılmaz. Şase mükerrer sorgusu aynı context'te yapılır, yanlış firma sessizce yok sayılmaz. Kayıt öncesi seçili firma tekrar kontrol edilir. Teknik DB hatası logger'a gider; kullanıcıya genel hata verilir.
- 🟢 **Kod düzeltildi:** MoveAracToFirmaAsync öncesinde aktif DB Admin yetkisi, tek seçili kaynak firma, kaynak firmada erişilebilir tracked araç, mevcut/silinmemiş ve farklı hedef firma ile izinli taşıma anahtarları kontrol edilir. Kira/komisyon bağlantıları hedefe uygun değilse taşıma başlamadan reddedilir; otomatik cari silme/eşleme yapılmaz. Puantaj ve servis çalışma hedefleri AsTracking yüklenir. Hazırlık hataları varsa SaveChanges yapılmaz; form da bu durumda ayrı UpdateAsync çağrısını başlatmaz.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne `UseAppHost=false` ile **0 uyarı, 0 hata** derlendi; değişen dosyaların `git diff --check` kontrolü boşluk hatası bildirmedi. Çalışan uygulama yeniden başlatılmadı.
- 🟡 **Kalan uygulama/kabul:** Runtime veya gerçek taşıma yapılmadı. Bu ekte iki ayrı SaveChanges olarak kaydedilen taşıma/form güncellemesi, aşağıdaki ortak kayıt düzeltmesinde birleştirildi. İlişkili cari/plaka/puantaj/personel/operasyon kayıtlarının hedef firma tutarlılığı ve seçilmeyen kayıtların politikası ayrıca tamamlanmalı. Güzergâh taşıma yolu bu ekte değiştirilmedi. Rol/firma değişiminin yazımla yarışı, A→B→A, eski bozuk ilişkiler, tam HTTP/circuit ve DB/audit rollback kabulü bekler. Başlamış commit bu kontrollerle geri alınmaz; A-13 bütünüyle kapanmadı.

## A-13 devamı — araç taşıma ve form alanlarının ortak kaydı — 2026-10-05

- 🟢 **Kod düzeltildi:** Araç formundaki iki taşıma onayı güncel form alanlarını MoveAracToFirmaAsync çağrısına verir; taşıma sonrasında ikinci UpdateAsync çağrısı kaldırıldı. Form alanları, kaynak firma/kayıt izi ve seçilen ilişkili değişiklikler aynı context içinde hazırlanıp tek SaveChanges/audit akışında kaydedilir. Hazırlık hatasında hiçbir değişiklik kaydedilmez.
- 🟢 **Kod düzeltildi:** Form kimliği ve hedef firma eşleşmesi kontrol edilir; değiştirilebilir alanların kopyası ilk await öncesinde alınır. Pozitif olmayan cari kimlikleri reddedilir; kira/komisyon carileri hedef firmada mevcut ve silinmemiş olmalıdır. Şase mükerrerliği kayıt öncesinde kontrol edilir. Genel UpdateAsync farklı firmaya yazımı reddeder ve taşıma akışına yönlendirir.
- 🟢 **Kod düzeltildi:** Firma olayı sürümü A→B→A seçimini de geçersizleştirir; geçici abonelik finally ile kaldırılır. Aktif DB Admin yetkisi kayıt öncesinde yeniden doğrulanır. Kayıt sonrası cache temizleme hatası logger'a gider; tamamlanmış DB kaydı başarısız taşıma olarak bildirilmez.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi. git diff --check boşluk hatası bildirmedi.
- 🟡 **Açık kapsam/kabul:** Runtime testi veya gerçek taşıma yapılmadı; çalışan uygulama yeniden başlatılmadı. İlişkili ve seçilmeyen kayıtların tenant politikası, eski cari/plaka/personel/operasyon bağlantıları, eşzamanlı taşıma, rol/firma değişiminin commit ile yarışı ve DB/audit rollback kabulü açık. Güzergâh taşıma yolu bu ekte değiştirilmedi. Başlamış commit'i geri alma garantisi verilmez; A-13 genel durumu sarıdır.

## A-13 devamı — araç evrak/dosya firma bütünlüğü ve envanter yetkisi — 2026-10-05

- 🟢 **Kod düzeltildi:** Taşıma kodundaki eski “AracEvrak FirmaId taşımaz” varsayımı kaldırıldı. Silinmemiş araç evrakları ve bu evrakların silinmemiş dosyaları AsTracking yüklenir; her iki kaydın FirmaId ve UpdatedAt alanları araçla aynı SaveChanges/audit akışında hedefe aktarılır. Taşınan kayıt sayısı dosyaları da kapsar; fiziksel dosya yolu değiştirilmez.
- 🟢 **Kod düzeltildi:** Evrak/dosya kaynak firma kimliği araçla eşleşmiyorsa (boş kimlik dahil) işlem kayıt öncesinde reddedilir. Mevcut evraklar seçilmeden araç taşınamaz; UI bu seçimi zorunlu ve devre dışı onay kutusuyla gösterir. Servis aynı kuralı doğrudan çağrıda da uygular.
- 🟢 **Kod düzeltildi:** GetAracTransferItemsAsync pozitif araç kimliği, tek seçili kaynak firma, aktif/silinmemiş DB Admin kullanıcısı ve kaynak firmada erişilebilir araç kontrolünden sonra ilişki sayımlarını açar. Firma olayı sürümü liste hazırlığı boyunca A→B→A değişimini de yakalar; abonelik finally ile kaldırılır. Listeleme hatası UI'de yakalanır, logger'a kaydedilir ve genel bildirim gösterilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kapsam/kabul:** Gerçek taşıma veya runtime testi yapılmadı; çalışan uygulama yeniden başlatılmadı. Puantaj/bakım ve diğer ilişkilerin kaynak/hedef bağlantıları, seçilmeyen kayıt politikası, silinmiş evrak/dosyaların yeniden etkinleştirilmesi, dosya sürümü erişimi, eşzamanlı yazım ve rollback kabulü açık. Eski firma uyuşmazlıkları bu akışta otomatik onarılmaz. A-13 genel durumu sarı kalır.

## A-13 devamı — seçilen puantaj ve servis çalışma bağlantıları — 2026-10-05

- 🟢 **Kod düzeltildi:** Taşınması seçilen aktif puantajların IsverenFirmaId, servis çalışmalarının FirmaId alanı kaynak araç firmasıyla eşleşmelidir; boş/başka firma bağlantısı kayıt öncesinde reddedilir. Uygun kayıtların firma ve UpdatedAt alanları ortak SaveChanges akışında güncellenir.
- 🟢 **Kod düzeltildi:** Seçilen puantajın dolu cari, kurum, güzergâh ve şoför bağlantıları hedef firmada mevcut ve silinmemiş olmalıdır. Servis çalışmasının zorunlu güzergâh ve şoför bağlantıları da hedef firmaya göre kontrol edilir. Bağlantılar otomatik silinmez veya başka kimliğe çevrilmez; uyumsuzlukta taşıma kaydedilmez.
- 🟢 **Kod düzeltildi:** Gelir/gider faturası, hesap dönemi veya önceki sürüm bağlantılı puantajlar ile aktif masraf bağlantılı servis çalışmaları eşleme tamamlanana kadar bu taşıma akışında reddedilir. Önceden değiştirilen tracked araç/evrak alanları da bu ret halinde SaveChanges yapılmadığı için kaydedilmez. ServisCalisma envanter etiketi gerçek kapsamına uygun “Servis çalışma kayıtları” olarak düzeltildi.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kapsam/kabul:** Gerçek taşıma veya runtime testi yapılmadı. Seçilmeyen kayıtların tarihsel firma politikası, hedef kayıt eşleme ekranı, Excel import ve diğer dolaylı ilişkiler, şoför/personel atamaları, plaka takibi/faturaları, silinmiş kayıtlar ve eşzamanlı yazım/rollback kabulü açıktır. Bu korumalar tam ilişki geçişi veya satış kabulü değildir; A-13 sarı kalır.

## A-13 devamı — araç taşıma ekranında işlem ve sonuç koruması — 2026-10-05

- 🟢 **Kod düzeltildi:** Taşıma envanteri ve onay işlemi ortak işlem kilidi kullanır; çift listeleme/taşıma ve işlem sürerken Kaydet üzerinden yeni taşıma penceresi açma engellenir. Onay düğmeleri ve seçim kutuları işlem boyunca devre dışıdır; yazım sürerken kapatma/iptal engellenir ve bekleme bilgisi gösterilir. Envanter yüklenmeden taşıma başlatılmaz.
- 🟢 **Kod düzeltildi:** Modal sürümü, araç kimliği, kaynak ve hedef firma işlem başında yakalanır. İptal, yeni pencere, route parametresi, firma olayı (A→B→A dahil) ve Dispose eski sonucu geçersizleştirir. Envanter sonucu, hata/başarı bildirimi ve yönlendirme yalnız güncel pencere için uygulanır; firma olayı/Dispose aboneliği kaldırma kapsamındadır. İşlem kilidi finally ile bırakılır.
- 🟢 **Kod düzeltildi:** Taşıma hazırlık ve istisna ayrıntıları logger'a gider; kullanıcıya genel yetki/firma/ilişki kontrolü yönergesi gösterilir. Hata halinde pencere açık kalır; başarı sonrası kapanır. Başlamış DB kaydı, pencere veya firma değişikliğiyle geri alınmış sayılmaz.
- 🟢 **Ekran düzeltildi:** “Bugünden Sonra Kopyala” düğmesi aynı MoveAracToFirmaAsync çağrısını yaptığı ve tarih bazlı kopya üretmediği için kaldırıldı. Ekran yalnız gerçekten uygulanan taşıma işlemini sunar; kopyalama özelliği eklenmedi.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kabul/kapsam:** Runtime/UI veya gerçek taşıma testi yapılmadı. Çift tıklama, envanter beklerken iptal/yeniden açma, firma/route değişimi, Dispose, başarısız taşıma ve commit yarışı kabulü açıktır. Diğer form yükleme/kayıt/evrak işlemleri bu sürüm koruması kapsamında değildir. Seçilmeyen ilişkilerin politikası ve hedef eşleme işleri nedeniyle A-13 sarı kalır.

## A-08 devamı — maaş snapshot muhasebe bağlantısı ve transaction — 2026-10-05

- 🟢 **Kod düzeltildi:** MuhasebeSnapshotService içindeki fiş/iptal fişi bağlantısını yazan iki ExecuteUpdateAsync kaldırıldı. Snapshotlar AsTracking yüklenir; MuhasebeFisId/IptalFisId ve UpdatedAt değişiklikleri normal SaveChanges/audit akışından geçer.
- 🟢 **Kod düzeltildi:** Hesap ön hazırlığı, normal/ters fiş ve snapshot bağlantıları tek üst transaction içinde çalışır. ExecutionStrategy her denemede yeni context/tracker ve transaction açar; hata halinde commit yapılmaz ve transaction dispose edilir. Audit savepoint akışı mevcut transaction içinde kalır.
- 🟢 **Kod düzeltildi:** Yıl/ay/pozitif firma ve tek seçili kaynak firma doğrulanır. Firma olayı sürümü A→B→A değişimini yakalar; commit öncesi tekrar kontrol edilir, abonelik finally ile kaldırılır. Mükerrer fiş sorgusu MAS-firma önekiyle daraltıldı; iptal için tüm snapshotların aynı mevcut kaynak fişe bağlı olması ve fişin firma öneki/kaynak tipi kontrol edilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kabul:** Gerçek DB/audit rollback veya runtime testi yapılmadı. İki eşzamanlı muhasebeleştirme, fiş numarası/hesap tekillikleri, commit sonucu belirsizliği, eski tutarsız snapshotlar ve kaynak fiş toplam/kalem uyumu A-09/A-15/A-16 altında açıktır. MuhasebeFis üzerinde doğrudan FirmaId olmadığından önek kontrolü kalıcı tenant ilişki kısıtının yerine geçmez.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan uygulama:** Bu ek yalnız MuhasebeSnapshotService içindeki iki doğrudan yazımı kapatır. Hedefli servis aramasında BudgetService, PuantajFinansService, GuzergahService/GuzergahSeferService, RebuildService, LicenseService ve bakım/restore servislerinde başka doğrudan SQL/toplu yazım yolları vardır; tam repo envanteri ve her yolun audit/transaction kararı henüz tamamlanmadı. A-08 genel durumu kırmızı kalır.

## A-08 devamı — güzergâh sefer yenilemede audit ve transaction sınırı — 2026-10-05

- 🟢 **Kod düzeltildi:** GuzergahSeferService.ReplaceAllInCurrentDbAsync içindeki ExecuteUpdateAsync kaldırıldı. Aktif eski seferler AsTracking yüklenir; IsDeleted, DeletedAt ve UpdatedAt normal SaveChanges/audit akışında yazılır. Zaten silinmiş seferlerin geçmiş zaman damgaları değiştirilmez.
- 🟢 **Kod düzeltildi:** Metot açık transaction gerektirir; eski seferleri kapatma ve yenilerini ekleme aynı üst transaction içinde kalır. Mevcut iki çağıran transaction açmaktadır. Hata/sayı uyuşmazlığında çağıranın rollback akışı çalışır. ChangeTracker.Clear kaldırıldı; üst işlemin takip ettiği güzergâh ve diğer kayıtlar yardımcı metot tarafından detache edilmez.
- 🟢 **Kod düzeltildi:** Yazım için pozitif güzergâh ve tek seçili kaynak firma gerekir; parent ve aktif eski seferlerin firma kimlikleri eşleşmelidir. Seçili firma kayıtlar öncesinde ve metot sonunda tekrar kontrol edilir. Tutarsız sefer firması sessizce düzeltilmez; işlem reddedilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kabul/kapsam:** Runtime veya gerçek DB rollback/audit testi yapılmadı. Eşzamanlı replace, retry/commit belirsizliği, üst transaction savepoint davranışı ve eski ilişki kabulü açık. Buradaki seçim kontrolü anlık kimlik eşitliğidir; A→B→A olayı ve üst çağıranın commit anı için işlem boyunca sürüm koruması ayrıca tamamlanmalıdır. GuzergahService'in diğer doğrudan yazımları bu ekte değiştirilmedi; A-08 kırmızı kalır.

## A-08 devamı — güzergâh ana kayıt yazımlarında audit — 2026-10-05

- 🟢 **Kod düzeltildi:** GuzergahService.UpdateAsync ve UpdateWithSeferlerAsync içindeki iki ExecuteUpdateAsync kaldırıldı. Ana güzergâh AsTracking ve silinmemiş kayıt koşuluyla yüklenir; cari/kurum bağlantıları, gelir/gider fiyatı ve KDV aynı tracked kaydın SaveChanges/audit akışında yazılır. GelirFiyat mevcut BirimFiyat alanına bağlıdır; ayrı SQL fiyat yazımı gerekmez.
- 🟢 **Kod düzeltildi:** Normal UpdateAsync içindeki ikinci bağımsız SQL yazımı kaldırıldı; ana alanlar ve cari/kurum tek SaveChanges çağrısında kaydedilir. Mevcut 0/null girişte eski cari/kurum bağlantısını koruma davranışı değişmedi. Sonuç doğrulaması global filtre kapsamında okunur.
- 🟢 **Kod düzeltildi:** Seferli güncellemede ana kayıt, sefer kapatma/ekleme ve audit mevcut üst transaction içinde kalır. Sefer yardımcı metodundaki takip temizleme çağrısı önceki ekte kaldırılmıştır; bu ekte ana kayıt için ayrıca SQL bypass yapılmaz.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kapsam/kabul:** Runtime/gerçek DB veya rollback testi yapılmadı. Güzergâh genel güncelleme/create/delete/transfer yetkisi ve firma ilişkileri, 0/null ile ilişki temizleme ürün davranışı, firma değişiminde sonuç doğrulaması, cache hatası sonrası kullanıcı bildirimi, eşzamanlı yazım ve retry/commit belirsizliği ayrıca denetlenmelidir. Bu ek tam güzergâh tenant kabulü değildir.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan uygulama:** Güzergâh ana kayıt/sefer servislerindeki hedeflenen üç ExecuteUpdate yolu kapandı; BudgetService, PuantajFinansService ve diğer bakım/restore/toplu yazım yolları için repo envanteri ve audit/transaction kararları devam eder. A-08 genel durumu kırmızı kalır.

## A-08 devamı — bütçe ödeme geri almada ortak kayıt/audit — 2026-10-05

- 🟢 **Kod düzeltildi:** BudgetService.OdemeGeriAlAsync içindeki ExecuteUpdateAsync kaldırıldı. Aktif ödeme AsTracking yüklenir; ödeme durum/tarih/tutar/hesap/kesinti alanları normal SaveChanges/audit akışında sıfırlanır. Banka hareketinin silinmesi için ayrı erken SaveChanges kaldırıldı; banka silme ve ödeme güncelleme tek SaveChanges transaction'ında kaydedilir.
- 🟢 **Kod düzeltildi:** Bağlı banka hareketi erişilebilir/silinmemiş ve ödeme ile aynı pozitif firmaya ait olmalıdır. Eksik/gizli hareket varsa ödeme bağlantısı sessizce sıfırlanmaz. Fatura, muhasebe fişi, mahsup, personel geri ödeme, araç masrafı veya aktif fatura eşlemesi olan hareketler kendi iptal akışına yönlendirilir; eşlemeler otomatik silinmez.
- 🟢 **Kod düzeltildi:** Başka aktif bütçe ödemesinin veya mahsup/personel geri ödeme hareketinin kullandığı banka kaydı silinmez. Filtre atlama yalnız bağlı hareket kimliğiyle çakışma kontrolüdür. Kısmi ödeme veya sonraki döneme aktarılmış ödeme, tüm ilişkiler birlikte geri alınmadan bu basit iptal akışında reddedilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kalan kabul/kapsam:** Runtime veya gerçek DB rollback/audit testi yapılmadı. Mevcut fiziksel banka hareketi silme davranışı korunmuştur; ters kayıt/soft delete politikası ayrıca değerlendirilmelidir. Tam rol/firma yetkisi, kredi kartı ve diğer dolaylı ilişkiler, kısmi/devir ödeme iptal uygulaması, eşzamanlı bağlantı ekleme ve commit belirsizliği açıktır. Ön sorgular eşzamanlı DB ilişki kısıtı değildir.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan uygulama:** Bu ek yalnız ödeme geri almadaki bir SQL bypass'ını kapatır. BudgetService'in kısmi ödeme, normal ödeme ve diğer toplu yazımları ile repo genelindeki SQL/audit envanteri devam eder; A-08 kırmızı kalır.

## A-08 kapanış çalışması — kalan toplu servis yazımları ve repo envanteri — 2026-10-05

- 🟢 **Kod düzeltildi:** BudgetService (5), CRMService (1), PuantajFinansService hakediş bağlantısı (1), RebuildService (6), LicenseService (3), eski TestSessionService satır rollback (4), WhatsAppService (1) ve EvrakArsivBackfillService (2) olmak üzere **23 toplu çağrı** tracked sorgu/UpdateTrackedAsync/SaveChanges akışına taşındı. Ortak yardımcı doğrudan SQL çalıştırmaz. Tek kayıt beklenen bütçe, hakediş ve evrak güncellemesi eşleşme yoksa başarısız olur; başarı gibi dönmez. Hata halinde bu sorgunun kayıt değerleri ve değişiklik işaretleri işlem öncesine döndürülür.
- 🟢 **Kod düzeltildi:** Hakediş snapshotındaki iki raw SQL yazımı kaldırıldı. SnapshotTransaction işaretçisi ve tracked tutar artışı/negatif tutar normalizasyonu tek Serializable transaction/SaveChanges kapsamındadır; ExecutionStrategy her denemede yeni context açar. Snapshot hatası sessizce yutulmaz; mevcut faturanın oluşmuş olduğu açık hata mesajıyla belirtilir. Bu, fatura alt servisinin ayrı transaction'ını geri alma garantisi değildir.
- 🟢 **Envanter oluşturuldu:** [A-08 SQL/audit kapanış envanteri](A-08-SQL-AUDIT-KAPANIS-ENVANTERI.md) repo C# kaynaklarındaki kalan çağrı konumlarını ve audit/transaction kararlarını listeler. Çağrı listesi DDL, sayaç, wrapper tanımı, audit iç düzeltmesi ve test kodunu da içerir; tamamı açık iş yazımı sayılmadı. Script/dinamik SQL semantik incelemesi ayrıca gereklidir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi.
- 🟡 **Kabul sınırı:** Runtime/test veya gerçek veri değişikliği yapılmadı. Tracked batch büyük kümeyi belleğe alır ve context'teki diğer bekleyen değişiklikleri de kaydeder; yük, eşzamanlılık, commit belirsizliği ve tam mali zincir rollback kabulü açık. Firma bağlamı olmayan sistem kayıtlarında mevcut audit resolver satır üretmeyebilir; normal SaveChanges kullanılması her sistem kaydının audit kanıtı değildir.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kapanış engeli:** Başlangıç/migration/seed veri onarımları, restore/legacy transfer/DataSync/demo işlemleri ve test tablo restore yollarında operasyon audit/rollback sözleşmesi henüz tamamlanmadı. Destek sayaçlarının atomik increment kararı ve audit EntityId iç düzeltmesi ayrı belgelenmiştir. Bu açıklar yalnız rapor rengini değiştirerek kapatılmaz; A-08 genel durumu kırmızı kalır.

## A-08 devamı — legacy aktarımda tablo transaction ve operasyon audit'i — 2026-10-05

- 🟢 **Kod düzeltildi:** LegacyDataTransferService içindeki roller, kullanıcılar, rol yetkileri, muhasebe hesapları ve generic tablo aktarımı tablo başına Npgsql transaction açar. Veri yazımı ile parametreli AktiviteLoglar özet kaydı aynı transaction'dadır; audit kaydı başarısızsa tablo commit edilmez. Başarı logu commit sonrasında yazılır. Satır bazlı eski/yeni değer yerine açıkça operasyon özeti tutulur.
- 🟢 **Kod düzeltildi:** Özet kaydı işlem kimliği, tablo, gerçek etkilenen satır sayısı, muhasebe üst hesap güncelleme sayısı, kaynak/hedef DB adı ve yapılandırılmış hedef firma kimliği içerir. Connection string, parola, kaynak satır verisi veya SQL parametreleri günlüğe kopyalanmaz. ON CONFLICT DO NOTHING satırları başarı sayısını artırmaz.
- 🟢 **Kod düzeltildi:** Satır INSERT/upsert işlemleri savepoint kullanır. Beklenen unique çakışmasında savepoint geri alınarak transaction kullanılabilir tutulur; muhasebe hesap Id fallback'i aynı transaction içinde devam eder. Generic kolon uyuşmazlığı döngüyü kesip kısmi başarı vermek yerine tablo işlemini hatayla durdurur. Hata/erken çıkışta transaction disposal commit edilmemiş tablo değişikliklerini geri alır.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi. Gerçek legacy veritabanı değiştirilmedi.
- 🟡 **Kalan kapsam/kabul:** İşlem sınırı tüm aktarım değil, tek tablodur; daha önce commit edilen tablolar sonraki tablo hatasında geri alınmaz. Source bağlantıları salt okunur kalır fakat tüm kaynak tablolar ortak snapshot içinde okunmaz. PostgreSQL sequence setval etkileri transaction rollback garantisine dahil değildir. Audit tablosu/kolonları yoksa veri commit'i reddedilir. Eski firma/rol/kimlik eşleme, idempotency, aynı anda iki aktarım, audit PK/sequence ve gerçek PostgreSQL rollback kabulü bekler.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan kapanış:** Legacy tablo yazımı için operasyon audit'i eklendi; schema hazırlığı, başlangıç veri onarımları, diğer restore/aktarım/demo yolları ve firma bağlamı olmayan sistem audit'i açık. A-08 genel durumu kırmızıdır.

## A-08 devamı — test tablo geri yüklemede ortak transaction ve audit — 2026-10-05

- 🟢 **Kod düzeltildi:** TestSessionService.GeriAlAsync yalnız PostgreSQL'de, aktif test etiketi eşleştiğinde ve DB'de aktif/silinmemiş Admin yetkisi doğrulandığında çalışır. Tek pozitif firma seçimi gereklidir; işlem boyunca firma olay sürümü A→B→A değişimini de yakalar. Geçici abonelik finally ile kaldırılır.
- 🟢 **Kod düzeltildi:** Gerekli altı backup tablosu yazım öncesinde kontrol edilir; eksik tablo sessizce atlanmaz. Bütün hedef tablolar tek TRUNCATE RESTRICT komutuyla hazırlanır; yedeklenmemiş bağımlı tabloları silen CASCADE kaldırıldı. Dış FK bağımlılığı varsa işlem reddedilir; bu tablolar otomatik silinmez veya yedeklenmiş kabul edilmez.
- 🟢 **Kod düzeltildi:** TRUNCATE/INSERT ve TestSnapshotRestore AktiviteLog kaydı aynı üst transaction içindedir. Raw ADO komutları EF CurrentTransaction'a açıkça bağlanır. Audit kaydı işlem kimliği, tag, tablo kümesi, session ve WholeDatabaseTables kapsamını belirtir; satır audit'i yerine operasyon özeti üretir. ExecutionStrategy denemeleri yeni context/transaction ile başlar.
- 🟢 **Kod düzeltildi:** Test oturumu yalnız başarılı commit sonrasında kapatılır. Hata halinde işlem başarılı gösterilmez; oturum/yedekler korunur ve teknik hata logger'a gider. Kullanıcıya genel yedek/bağımlılık/yetki kontrolü bildirimi verilir.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi. Gerçek test verisi veya yedek geri yüklenmedi.
- 🟡 **Kalan kabul/kapsam:** Gerçek PostgreSQL restore/rollback/audit testi yapılmadı. Mevcut altı tablo kümesi bütün FK bağımlılıklarını kapsamaz; bu durumda RESTRICT işlemi durdurur. Tam bağımlılık yedeği, INSERT sırası/kolon uyumu, sequence/kimlik eşlemesi ve commit belirsizliği kabulü açık. Seçilen firma audit aidiyetidir; tablo restore tüm firma satırlarını kapsar. Test başlatma/backup oluşturma, cleanup ve basit BeginSession yollarının yetki/transaction/audit sözleşmesi bu ekte tamamlanmadı. A-08 genel durumu kırmızı kalır.

## A-08 devamı — test backup/cleanup/session bakım sözleşmesi — 2026-10-05

- 🟢 **Kod düzeltildi:** BaslatAsync, BeginSessionAsync ve TemizleAsync PostgreSQL bakım yardımcısında aktif DB Admin yetkisi, tek pozitif firma ve işlem boyunca firma olay sürümü kontrolü kullanır. Bakım değişiklikleri, session rezervasyonu ve açık kapsamlı AktiviteLog operasyon kaydı aynı transaction/SaveChanges içindedir; yalnız commit sonrası AppMode/başarı durumu yayınlanır.
- 🟢 **Kod düzeltildi:** Backup öncesinde aynı tag için mevcut tablo varsa işlem reddedilir; DROP ile eski yedek silinmez. Altı kaynak tablo SHARE kilidiyle korunur; CREATE TABLE AS SELECT işlemleri ve audit ortak transaction'dadır. Hata halinde dönülen backup tablosu listesi temizlenir ve başarısız işlem yeni oturum başlatmaz.
- 🟢 **Kod düzeltildi:** Cleanup aktif test varken reddedilir. Süreç içi ortak semaphore bakım çağrılarını sıraya alır; PostgreSQL transaction advisory lock backup/cleanup/begin/restore DB işlemlerini aynı anahtarla sıralar. ReadCommitted altında session maksimumu silinmiş loglar dahil okunur; session Begin marker'ı audit ile birlikte kaydedilir. Başka süreçlerdeki AppMode durumunu bu kilit tek başına doğrulamaz.
- 🟢 **Derleme:** Son kaynak Web Debug ayrı geçici çıktı klasörüne UseAppHost=false ile **0 uyarı, 0 hata** derlendi; git diff --check boşluk hatası bildirmedi. Gerçek backup/restore veya test oturumu çalıştırılmadı.
- 🟡 **Kalan kabul:** Farklı süreçlerde aktif test oturumu sahipliği, bağlantı/commit belirsizliği, session marker ve eski rollback uyumu, tag normalizasyon çakışması, DDL/SHARE lock gecikmesi ve gerçek PostgreSQL rollback kabulü açık. SQL backup yalnız altı tabloyu kapsar; tam bağımlılık kurtarması değildir. Eski satır rollback yolunun ayrı yetki ve kısmi sonuç sözleşmesi ayrıca denetlenmelidir.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kapanış sınırı:** Test backup/cleanup/begin kaynak kontrolleri tamamlandı; başlangıç/migration veri onarımı, diğer restore/DataSync/demo/dış araç operasyon kayıtları ve firmasız sistem audit bağlamı halen açıktır. Bu işler tamamlanmadan A-08 yeşile çevrilmez.

## A-08 devamı — başlangıç fatura onarımı ve demo bakım sınırı — 2026-10-05

- 🟢 **Gelen fatura onarımı:** PostgreSQL doğrudan UPDATE kaldırıldı; bütün sağlayıcılarda AsTracking + SaveChanges ve UTC zaman damgası kullanılır. Firma aidiyeti bulunmayan kayıtlar yazımdan önce reddedilir; hata artık başarı gibi yutulmaz.
- 🟢 **Demo bakım kaydı:** Ekleme, [TEST] temizliği ve yenileme aktif Admin/tek firma kontrolü, firma değişim sürümü, ortak PostgreSQL transaction/advisory kilidi ve aynı transaction'daki operasyon audit'i kapsamındadır. Retry denemesinde yeni context/seeder oluşturulur; seed başarısız sonucu commit edilmez. İki ekran aynı servisi çağırır.
- 🟢 **Tehlikeli sıfırlama kaldırıldı:** Demo yenileme yalnız seçili firmanın [TEST] kayıtlarını temizleyip yeniden üretir. Eski tüm-veritabanı TRUNCATE API'si yazım yapmadan açık hata döndürür; kullanıcı/lisans/audit geçmişini silen CASCADE ve session_replication_role yolları kaldırılmıştır. Ekran açıklamaları kapsamı belirtir.
- 🟢 **Sınırlı firma temizliği:** Tanımlı 11 tabloda parametreli FirmaId silme + operasyon audit'i ortak transaction'dadır. FK kontrolleri açık kalır; tablo hatası atlanmaz. FirmaId taşımayan ortak MuhasebeFisleri/MuhasebeHesaplari bu işlemden çıkarılmıştır. Bu yol tüm firma verisinin eksiksiz silindiğini iddia etmez.
- 🟡 **Kabul açık:** Gerçek PostgreSQL demo/temizlik/audit/rollback testi yapılmadı. İlişkili kayıtlar fiziksel silmeyi engelleyebilir; işlem başarısız döner. Seed'in eski demo veri aidiyeti, FK/kayıt üretimi ve commit belirsizliği kabulü bekler. Sadece derleme kontrolü bu kabulü kapatmaz.
> **Kapanış öncesi tarihsel durum (son kapanış ekiyle giderildi):** 🔴 **A-08 kalan:** Diğer başlangıç/migration veri onarımları, restore/DataSync/dış araç yazımları ve firmasız sistem audit sözleşmesi açıktır. A-08 genel durumu kırmızı kalır.

- 🟢 **Derleme kontrolü:** Son kaynaklarla Web projesi izole çıktı klasörüne `--no-restore -p:UseAppHost=false` ile derlendi: **0 uyarı, 0 hata**. `git diff --check` temiz. Runtime/gerçek veri temizliği çalıştırılmadı.

## A-08 ortak altyapı — SQL ve sistem yazımlarının veritabanında denetimi — 2026-10-05

- 🟢 **Ortak altyapı eklendi:** PostgreSQL/SQLite iş tablosu tetikleyicileri EF dışı INSERT/UPDATE/DELETE'yi aynı transaction içinde denetler; PostgreSQL TRUNCATE kapsam/satır sayısını kaydeder. Firma bağlamı olmayan kayıtlar ayrı DB günlüğünde sistem kapsamındadır. Sır alanları maskelenir; audit hatası iş yazımını engeller.
- 🟢 **Başlangıç/aktarım:** Şema kurulumundan sonra installer, legacy hedef bağlantısında installer, DataSync hedefinde installer eklendi. Master başlangıç kopyası kaynak snapshot ve ortak hedef transaction'a alındı; satır hatası artık atlanmaz. SQLite journal iş tablosu aktarım/sıfırlama listesinden çıkarıldı.
- 🟢 **Restore:** Kalıcı dış operasyon makbuzu ve source SHA-256; PostgreSQL audit şemasını yedek/restore iş kapsamından ayırma; SQLite açık dosyayı ezmek yerine backup API ve öncesi geri dönüş kopyası. Deploy betiğinde DB DROP kaldırıldı; atomik restore ve sıfır dışı hata kodunun reddi eklendi.
- 🟢 **SQLite izole kontrol:** Doğrudan SQL, sistem kapsamı, sır maskeleme, rollback, günlük değişmezliği ve audit hatasında veri yazımının durması geçti.
- **Sözleşme:** [A-08 veritabanı audit sözleşmesi](A-08-VERITABANI-AUDIT-SOZLESMESI.md). PostgreSQL izole kontrolü ve son kaynak derlemeleri tamamlanınca görev satırı güncellenecek. Müşteri verisi restore edilmedi.

## 🟢 A-08 kapanışı — ortak denetim motoru ve izole doğrulama — 2026-10-05

**A-08 tamamlandı.** Bu ek önceki A-08 “açık/kırmızı” kayıtlarının güncel durumunu değiştirir; önceki ekler tarihsel çalışma kanıtıdır.

- 🟢 **SQL bypass kökten kapatıldı:** Ortak PostgreSQL/SQLite tetikleyicisi EF, doğrudan SQL, toplu yazım, migration ve binary COPY'yi veriyle aynı transaction içinde kaydeder. Audit hatasında veri yazımı gerçekleşmez; rollback günlüğü de geri alır. Firma bilgisi bulunmayan sistem kayıtları başka firmaya mal edilmez.
- 🟢 **Kapsam tamamlandı:** Web startup + 11 veri migration sınıfı; maaş/özlük/SMS onarım kapıları; master kopyada kaynak snapshot + ortak hedef transaction; legacy/DataSync hedef kurulumları; demo bakımı; LisansDesktop yerel satış/yenileme SQL geçmişi; 4 bağımsız veri SQL betiği ve Deploy restore. Günlük kendi kendini audit etmez; migration geçmişi/kimlik sequence metadatası iş satırı değildir.
- 🟢 **Sır maskeleme ortaklaştırıldı:** DbContext otomatik audit, AuditLogService ve DB motoru API/key/password/credential/payload/değer alanlarını maskeler. Yerel/üretim sırları test çıktısına yazılmadı.
- 🟢 **Restore kanıtı kalıcı:** PostgreSQL public restore mevcut `mk_audit` geçmişini korur. SQLite eski veri/audit bağımsız before-restore kopyasında korunur. SHA-256 başlangıç ve ayrı başarı/belirsiz sonuç makbuzu, restore edilen DB'nin dışında saklanır. Düz SQL restore ve mevcut DB'yi düşürme kaldırıldı; hata kodu 1 başarı sayılmaz.
- 🟢 **Gerçek izole çalışma zamanı kontrolü:** SQLite ve PostgreSQL 17 üzerinde doğrudan SQL, sistem kapsamı, sır maskeleme, rollback, audit hatasında yazımın reddi, ALWAYS/replica, TRUNCATE, yeni migration tablosunun ilk INSERT'i ve binary COPY geçti. Gerçek custom pg_dump/pg_restore ile veri/audit koruması ve SQLite backup API ile geri dönüş kopyası doğrulandı. Geçici PG test DB'si silindi, sunucu durduruldu; müşteri DB'sine dokunulmadı.
- 🟢 **Son kaynak derlemeleri:** Web, DataSync ve LisansDesktop projeleri izole çıktı klasörlerinde `--no-restore -p:UseAppHost=false` ile derlendi: her biri **0 uyarı / 0 hata**. Restore PowerShell parser kontrolü geçti.
- **Kanıtlar:** [Veritabanı audit sözleşmesi](A-08-VERITABANI-AUDIT-SOZLESMESI.md), [izole kontrol ve kaynak SHA-256](A-08-IZOLE-DOGRULAMA-2026-10-05.md), [çağrı/kapsam envanteri](A-08-SQL-AUDIT-KAPANIS-ENVANTERI.md).
- 🟡 **Ayrı dağıtım kabulü:** Gerçek müşteri migration/DataSync verisi ve yüksek hacim, mali zincir, bağımsız makine ve dosya/key ring kurtarması dağıtım provasında doğrulanacaktır. A-19'un sentetik iki sağlayıcı ürün doğrulaması tamamlandı; bu saha koşulları A-08'in açık kod işi olarak tekrar sayılmaz.

## A-03 devamı — DB-only geri yükleme kapsamının ekranda belirtilmesi — 2026-10-05

- 🟢 **Kod düzeltildi:** Yedekleme ekranındaki restore onayında işlemin yalnızca PostgreSQL verisini değiştirdiği; ZIP içindeki belge/dosya, ayar ve DataProtection anahtarlarının uygulanmadığı açıkça gösterilir. Eylem düğmesi “Yalnızca DB'yi Geri Yükle” olarak adlandırıldı.
- 🟢 **Dokümantasyon:** `SIFRELI-BELGE-YEDEK-KURTARMA.md` ekran davranışı ve sınırla eşitlendi.
- 🔴 **A-03 açık:** DB+dosya+anahtarları kapalı bakım penceresinde birlikte uygulayan, ayarları hedefe göre koruyan ve her aşamada önceki duruma dönen otomasyon henüz eklenmedi. Bu UI düzeltmesi tam kurtarma değildir; A-03 kırmızı kalır.

## A-03 devamı — deploy DB restore hata geri dönüşü — 2026-10-05

- 🟢 **Kod düzeltildi:** `01-db-restore.ps1`, mevcut DB için `mk_audit` hariç custom geri dönüş dump'ını restore öncesinde üretip boyutunu kontrol eder. Kaynak restore girişiminden sonra herhangi bir hata olursa eski `public` şemayı tek transaction'lı `pg_restore` ile geri yükler, audit installer'ı yeniden uygular ve `rolled-back.json` makbuzu yazar.
- 🟢 **Yeni hedef DB:** DB önceden yoksa ve kaynak restore hata verirse yalnız bu çağrıda oluşturulan hedef DB bağlantıları kapatılıp DB kaldırılır. Eski bir DB hiçbir hata kolunda DROP edilmez.
- 🟡 **Doğrulama sınırı:** PowerShell parser/derleme ve diff kontrolü yapılacak; bu turda canlı veya izole PostgreSQL hata enjeksiyonu çalıştırılmadı. Makbuz gerçek rollback kabulünün yerine geçmez.
- 🔴 **A-03 açık:** Dosya/ayar/key ring ile DB'yi aynı tam kurtarma operasyonunda uygulama ve birlikte geri alma otomasyonu yoktur. Mevcut değişiklik yalnız deploy DB restore betiğinin rollback açığını kapatır.

## A-03 devamı — Web ZIP DB restore rollback — 2026-10-05

- 🟢 **Kod düzeltildi:** Web ZIP restore başarılı DB restore sonrasında audit doğrulaması veya başarı makbuzu başarısızsa, önceden alınmış tam ZIP yedeğindeki tek PostgreSQL custom dump ayrı staging'e çıkarılır ve `--single-transaction` ile önceki `public` şeması geri kurulur; ardından audit installer doğrulanır.
- 🟢 **Sonuç kaydı:** Geri dönüş doğrulanırsa `rolled-back.json` yazılır. Kaynak/rollback işlemi belirsiz kalırsa `unconfirmed.json` korunur; rollback hatası Critical loglanır. Başarısız `pg_restore` da commit/bağlantı belirsizliği ihtimaline karşı önceki yedekten geri döndürülür.
- 🟢 **Derleme:** Web + Shared Debug ayrı geçici çıktı klasörüne `--no-restore -p:UseAppHost=false` ile **0 uyarı / 0 hata** derlendi.
- 🟡 **Kabul sınırı:** PostgreSQL hata enjeksiyonu/gerçek restore bu turda çalıştırılmadı; uygulama yolu kod derlemesiyle kabul edilmiş sayılmaz.
- 🔴 **A-03 açık:** Web/deploy DB rollback uygulandı; dosya, ayar ve key ring'i aynı tam kurtarma operasyonunda atomik uygulama/rollback aracı hâlâ yoktur.

## Güvenli parola istemi — restore betikleri — 2026-10-05

- 🟢 `01-db-restore.ps1` artık parola parametresi veya `MKFILO_PG_PASSWORD` desteği sunmaz. PostgreSQL parolası `Read-Host -AsSecureString` ile istenir; komut satırı argümanına aktarılmaz. PowerShell süreç ortamındaki `PGPASSWORD` yalnız işlem boyunca ayarlanır, finally bloğunda önceki değer geri yüklenir; BSTR ve SecureString temizlenir.
- 🟢 `00-aktar-baslat.ps1` parola değerini yapılandırmada tutmaz ve alt sürece parametre olarak geçmez. `04-pc2-kurulum-talimat.md` yeni istem akışını anlatır.
- 🟢 İki betiğin PowerShell parser doğrulaması ve `git diff --check` geçti; eski açık parola parametresi/metin araması hedef betik ve talimatta eşleşme vermedi. 🟡 Gerçek müşteri bağlantısı ve sır rotasyonu kanıtı yoktur; A-06 açık kalır.

## A-03 devamı — DB restore öncesi dosya/anahtar ön kontrolü — 2026-10-05

- 🟢 **Kod düzeltildi:** `02-dosya-aktar.ps1 -PreflightOnly` kaynak `.enc` başlıklarını sınıflandırır. MKD1 dosyalarında `key-*.xml`; legacy/AES dosyalarında DPAPI ile çözülebilir `master.key` veya 32 baytlık geçerli import/raw key arar. Ön kontrol hedefe yazmaz.
- 🟢 **Sıra düzeltildi:** `00-aktar-baslat.ps1` DB restore'dan önce dosya/anahtar preflight çalıştırır. Anahtarlar eksik/bozuksa DB restore başlatılmaz. Gerçek hedef kopyasında aynı kontroller yeniden yapılır.
- 🟢 **Sözdizimi:** İki betiğin PowerShell parse kontrolü ve `git diff --check` geçti. 🟡 DataProtection key XML'in hedefte gerçekten çözülebildiği farklı makine kabulü A-04'tedir. Dosya kopyası sonrasındaki I/O hatasında ortak DB+dosya rollback henüz yoktur.
- 🔴 **A-03 açık:** Bu düzeltme anahtar eksikliğiyle DB'yi tek başına restore etme riskini kapatır; tam operasyon transaction/rollback aracı değildir.

## A-03 devamı — legacy DB+dosya aktarım rollback bağlama — 2026-10-05

- 🟢 **Önceki durum snapshot'ı:** `00-aktar-baslat.ps1` dosya aktarımından önce `uploads`, `keys`, `database` klasörlerinin mevcut olup olmadığını kaydeder ve mevcut içeriği LocalAppData operasyon klasörüne kopyalar. Kopya eksikse DB restore başlamaz.
- 🟢 **DB makbuzu:** `01-db-restore.ps1`, önceden var olan DB'nin ayrı SHA-256 doğrulamalı custom dump'ını parent operasyon klasörüne kopyalar. Yeni DB oluşturduysa `pg_database.oid` değerini operasyon makbuzuna yazar.
- 🟢 **Dosya hatası geri dönüşü:** `02-dosya-aktar.ps1` başarısız olursa parent önce önceki DB dump'ını tek transaction ile yükler; DB başlangıçta yoksa yalnız aynı operasyonda oluşturulan ve OID'si eşleşen hedefi kaldırır. Sonra üç depolama klasörünü başlangıç var/yok durumuna döndürür. Geri dönüş başarı/başarısız makbuzu tutulur; hata halinde snapshot silinmez.
- 🟢 **Statik doğrulama:** İlgili üç PowerShell betiğinin parser kontrolü ve `git diff --check` geçti.
- 🟡 **Kabul sınırı:** Gerçek DB/file hata enjeksiyonu, müşteri verisi veya elektrik kesintisi testi yapılmadı. Bu, yakalanan süreç hatası rollback akışıdır; ani süreç/host kesintisinde otomatik devam garantisi değildir.
- 🟢 **Kesinti sonrası elle kurtarma:** `03-full-transfer-recover.ps1`, LocalAppData altındaki tam aktarım snapshot/makbuzunu doğrular; önceden var olan DB'yi SHA-256 doğrulamalı dump'tan geri yükler veya makbuzdaki OID eşleşen yeni DB'yi kaldırır, ardından `uploads`, `keys`, `database` klasörlerini snapshot'tan geri alır. Önceki otomatik rollback başarısızlık makbuzu elle kurtarmayı engellemez; yeni DB zaten kaldırılmışsa adım idempotent tamamlanır. IIS havuzunun durduğu onaylanır ve `recovered.json` makbuzu yazılır.
- 🟢 **Güvenlik kontrolleri:** Operasyon klasörü doğrudan izinli journal kökü altında olmalı; DB geri dönüş dump'ı beklenen operasyon dosyasıyla aynı olmalı, hash doğrulanmalı; kaynak/snapshot hedefleri junction/symlink kontrollerinden geçer.
- 🟢 **Kapsam belgeleri eşitlendi:** [Şifreli belge yedek/kurtarma rehberi](SIFRELI-BELGE-YEDEK-KURTARMA.md) manuel journal rollback kapsamını RecoveryArchive ZIP'ini uygulama kapsamından ayırır.
- 🟢 **RecoveryArchive apply aracı:** `05-recovery-archive-apply.ps1` manifest dosya boyutu/hash'lerini yeniden doğrular, izinli storage/Luca/belge köklerini hedefler, varsa DB dump'ını yükler ve `application/*.json` ayarlarını atlar. Önceki dosya ve DB durumunu aynı apply journal'ında saklar; yakalanan hata için geri dönüş dener.
- 🟢 **DB hata dalı güçlendirildi:** DB alt betiği başarısız dönerse üst apply akışı başlangıç DB makbuzunu kullanarak dış rollback'i ayrıca dener; alt betiğin kendi rollback'inin belirsiz/başarısız olması üst akışın DB'yi atlamasına yol açmaz.
- 🟡 **Sınır/kabul:** Apply ve rollback betikleri belirtilen IIS havuzunu appcmd ile durdurup durumunu doğrular; yeniden başlatma belge/DB kabulinden sonra operatörce yapılır. Key XML varlığı ve `KEY-HAZIR` onayı hedef kimliğinde belge çözüldüğünü kanıtlamaz. PostgreSQL restore/hata enjeksiyonu ve farklı makinede key ring/credential kabulü yoktur.
- 🟢 **Apply journal kesinti kurtarması:** `06-recovery-archive-rollback.ps1` önceki dosya durumunu ve DB makbuzunu doğrular; işlemi bitmemiş `recovery-apply-{guid}` journal'ı için önce DB'yi, sonra dosya köklerini geri yükler. Tekrar denenebilir; `applied.json` bulunan tamamlanmış apply'ı geri almayı reddeder.
- 🟡 **Kalan kabul:** PowerShell parser/diff kontrolü geçmiştir; gerçek PostgreSQL, hata enjeksiyonu, elektrik kesintisi ve anahtar çözme kabulü yapılmamıştır. IIS yönetimi operatördedir.
- 🟢 **Statik doğrulama:** Altı aktarım/kurtarma PowerShell betiğinin parser kontrolü geçti; `git diff --check` temiz. 🟡 Canlı PostgreSQL, hata enjeksiyonu ve elektrik kesintisi kabulü yapılmadı.
- 🔴 **A-03 açık:** Başarı sonrası IIS yeniden başlatma ve gerçek hata/kesinti/DB+belge kabulü hâlâ gerekli. Ortama özel appsettings arşivden otomatik uygulanmaz.

## A-03 devamı — RecoveryArchive journal yolunun doğrulanması — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1`, DB/dosya hedeflerine dokunmadan önce `%LOCALAPPDATA%\MKFiloServis\OperationJournal` yolunun üst bileşenlerinde junction/symlink bulunmadığını ve yeni operasyon klasörünün normal dizin olduğunu doğrular. Beklenmeyen yönlendirmede işlem durur.
- 🟢 PowerShell parser: `05-recovery-archive-apply.ps1`, `06-recovery-archive-rollback.ps1`, `01-db-restore.ps1` başarılı. `git diff --check` temiz; yalnız doküman satır sonu uyarıları var.
- 🔴 **A-03 açık:** Bu statik kontrol gerçek IIS/NTFS/DB hata enjeksiyonu, kesinti kurtarma veya farklı makine belge çözme kabulü yerine geçmez.

## A-03 devamı — dosya rollback snapshot SHA-256 makbuzu — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1`, her mevcut hedef dosya kökü için snapshot kopyası sonrası dosya yolu/boyut/SHA-256 manifesti üretir. Apply sırasında hata yakalanırsa snapshot hash'lerini yeniden doğrulamadan kopyalamaz.
- 🟢 `06-recovery-archive-rollback.ps1`, kesinti sonrası geri dönüşe başlamadan önce ilgili snapshot'ların dosya sayısı, yolları, boyutları ve SHA-256 değerlerini doğrular; uyuşmazlıkta DB veya hedef dosyaları değiştirmeden durur.
- 🟢 İki PowerShell betiğinin parser kontrolü başarılı; `git diff --check` temiz (dokümanlarda satır sonu uyarısı dışında).
- 🔴 **A-03 açık:** Hash makbuzu snapshot bozulmasını saptar, imza/yerel yönetici müdahalesine karşı özgünlük sağlamaz. Gerçek PostgreSQL, kesinti, NTFS ve farklı makine belge kabulü yapılmadı.

## A-03 devamı — apply hazırlık makbuzlarının atomik yazımı — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1`, dosya öncesi durumu, snapshot SHA-256 listesini ve `ApplyStarted` makbuzunu önce aynı journal klasöründeki benzersiz geçici dosyaya yazar, diske flush eder ve hedef makbuz adına atomik taşır. Kesinti sırasında yarım JSON'un tamamlanmış makbuz gibi görünme olasılığı azaltıldı.
- 🟢 PowerShell parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Dosya taşıma atomikliği hedef dosya sistemine bağlıdır; gerçek NTFS/ani güç kesintisi, PostgreSQL ve uygulama kabulü yapılmadı.

## A-03 devamı — snapshot doğrulamasını hedef silmeden önce yapma — 2026-10-05

- 🟢 Apply hata geri dönüşü ve kesinti sonrası rollback artık ilgili snapshot SHA-256 kontrolünü hedef klasörü silmeden önce yapar. Snapshot eksik/değişmişse o hedef korunur ve geri dönüş başarısızlığı açıkça kaydedilir; doğrulanmamış kopya kullanılmaz.
- 🟢 `00`, `01`, `02`, `03`, `05`, `06` PowerShell betiklerinin parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Gerçek hata enjeksiyonu ve PostgreSQL/NTFS geri dönüş kabulü yapılmadı.

## A-03 devamı — IIS durdurmadan önce apply journal alanı — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1`, LocalAppData journal üst yolunu doğrulayıp yeni operasyon klasörünü oluşturmadan IIS havuzunu durdurmaz. Journal yolu/oluşturma hatası artık uygulama havuzunu gereksiz yere kapalı bırakmaz. Havuz yine dosya ve DB snapshot'larından önce durdurulur.
- 🟢 Altı migration PowerShell betiğinin parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Gerçek IIS servis durumu geçişi, DB/NTFS hata enjeksiyonu ve hedef makine kurtarma kabulü yapılmadı.

## A-03 devamı — DB rollback makbuz ve dump doğrulaması — 2026-10-05

- 🟢 `01-db-restore.ps1` operasyon makbuzuna kaynak DB dump SHA-256 değerini yazar.
- 🟢 `05` ve `06` rollback yolları mevcut DB için `PreExistingDatabaseSnapshotReady`, hedef adı, beklenen journal dump yolu ve rollback dump hash'ini doğrular. Yeni DB durumunda `DatabaseCreatedByOperation`, sayısal DB OID'si, journal içindeki sabit kaynak dump yolu ve kaynak dump hash'i doğrulanmadan DB kaldırma adımına geçmez.
- 🟢 Üç PowerShell betiğinin parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** PostgreSQL üzerinde gerçek yeni/mevcut DB geri dönüşü ve kesinti/hata enjeksiyonu kabulü henüz yapılmadı.

## A-03 devamı — terminal apply/rollback makbuzlarını atomik yazma — 2026-10-05

- 🟢 `05-recovery-archive-apply.ps1` artık `applied.json` ve `rollback-result.json` sonuçlarını geçici dosyaya flush edip atomik taşır. Başarı makbuzu tamamlanmadan oluşan yazma hatası apply hata/rollback akışına düşer.
- 🟢 `06-recovery-archive-rollback.ps1` `recovered.json` ve `recovery-failed.json` makbuzlarını aynı yöntemle yazar; yarım terminal makbuzun sonraki kurtarma denemesini yanlış engelleme riski azaltıldı.
- 🟢 İki PowerShell betiğinin parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Atomik dosya taşıma ve gerçek süreç/güç kesintisi davranışı hedef Windows/NTFS üzerinde denenmedi; DB ve belge kabulü de bekliyor.

## A-03 devamı — hedef yol üst bileşenlerini yeniden doğrulama — 2026-10-05

- 🟢 Apply ve rollback betikleri hedefleri snapshot alma, uygulama ve geri yükleme öncesinde üst dizinleri yeniden junction/symlink açısından denetler. Başlangıç kontrolünden sonra yol bileşeni değişmişse hedef üzerinde işlem yapmaz.
- 🟢 İlgili PowerShell parser kontrolleri başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Dosya sistemi yarış koşulları, PostgreSQL arızası ve gerçek hedef kurulum kabulü henüz çalıştırılmadı.

## A-03 devamı — eski apply journal'larıyla kurtarma uyumluluğu — 2026-10-05

- 🟢 `06-recovery-archive-rollback.ps1`, yeni apply journal'larında snapshot SHA-256 doğrulamasını zorunlu tutar. Hash makbuzu olmayan eski journal'larda snapshot'ı junction/symlink açısından denetler ve DB/dosya değişikliğinden önce `ESKI-SNAPSHOT-ONAY` operatör onayı ister; onay yoksa değişiklik yapmadan durur.
- 🟢 Eski yeni-DB makbuzlarında kaynak dump hash alanı yoksa, hedef OID ve beklenen journal dump yolu doğrulanarak DB kaldırma kurtarması sürdürülebilir; yeni hash alanı varsa doğrulama zorunludur.
- 🟢 `06-recovery-archive-rollback.ps1` parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Eski journal'larda eksik hash özgünlük doğrulaması operatör onayıyla sınırlıdır; gerçek DB/kesinti kabulü yapılmadı.

## A-03 devamı — 6 Ekim journal tekrar deneme güvenliği

- 🟢 `06-recovery-archive-rollback.ps1`, önceki başarısız denemeler için her seferinde benzersiz `recovery-failed-{guid}.json` makbuzu yazar; var olan hata kaydı yeni rollback denemesini engellemez veya hata ayrıntısını ezmez.
- 🟢 `files-before-hashes.json` yolu varsa normal dosya olması zorunludur. Aynı adla klasör/özel yol bulunursa eski hash'siz journal olarak yorumlanmaz; işlem durur.
- 🟢 PowerShell parser kontrolü başarılı; `git diff --check` temiz.
- 🔴 **A-03 açık:** Gerçek Windows kesinti ve PostgreSQL rollback kabulü yapılmadı.

## A-10 devamı — EBYS belge silme ve güncelleme sırası — 2026-10-06

- 🟢 EBYS dosya silme artık önce kaydı soft-delete edip DB değişikliğini kaydeder, ardından fiziksel dosyayı temizler. Fiziksel silme başarısızlığı `FileCleanupPendingException` olarak bildirilir.
- 🟢 EBYS dosya güncelleme eski dosyayı DB değişikliğinden önce silmez. Yeni şifreli dosya oluşturulur, DB'deki yol güncellenir ve commit sonucu hata verirse yeni yol taze context ile kontrol edilir; başvuru olup olmadığı doğrulanamazsa olası yetim dosya güvenlik için korunur. Commit başarılıysa eski dosya sonradan temizlenir.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Test veya gerçek dosya silme çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı temizleme kuyruğu/yeniden deneme, mevcut yetim dosya envanteri ve diğer servislerin DB/dosya sırası bu düzeltme kapsamında tamamlanmadı.

## A-10 devamı — EBYS işlem hareketini dosya temizliğinden önce kaydetme — 2026-10-06

- 🟢 EBYS silme/güncellemede iş kaydı DB'ye yazıldıktan sonra EBYS hareket kaydı ekleniyor, fiziksel eski dosya en son temizleniyor. Temizlik hatası olsa da DB hareket kaydı yazılmış olur; fiziksel silme DB kaydından önce çalışmaz.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Test/gerçek dosya silme yapılmadı.
- 🔴 **A-10 açık:** Hareket/audit kaydı ve ana DB kaydı ayrı SaveChanges bağlamlarındadır; kalıcı cleanup kuyruğu, yetim envanteri ve diğer servislerin telafi akışı hâlâ gereklidir.

## A-10 devamı — araç uyarı ekranı upload telafisi — 2026-10-06

- 🟢 `BelgeUyariService.AracBelgeDosyaYukleAsync`, dosya DB kaydı hata döndürdüğünde artık yeni dosyayı körlemesine silmez. Taze context ile `AracEvrakDosyalari` içinde dosya yoluna başvuru aranır; başvuru varsa dosya korunur, yoksa telafi silmesi yapılır. DB doğrulaması veya telafi silmesi başarısızsa asıl hata ve telafi hatası birlikte yükseltilir.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Test/gerçek upload çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı temizlik kuyruğu/yeniden deneme, yetim dosya envanteri ve diğer dosya yazan servislerin belirsiz commit telafisi tamamlanmadı.

## A-10 devamı — EBYS sürüm arşivini ana dosyadan ayırma — 2026-10-06

- 🟢 `BelgeVersiyonService.ArsivleEbysEvrakDosyaAsync`, eski ana dosyanın aynı yolunu sürüm kaydına kopyalamak yerine şifreli baytları `ebys/versions/{id}` altında bağımsız dosyaya kopyalar ve sürüm kaydını bu yola bağlar. Ana dosya güncellendiğinde eski sürüm artık ana dosya temizliğiyle kaybolmaz.
- 🟢 Arşiv DB kaydı başarısız/belirsizse taze context sürüm tablosunda bu yola başvuru arar; başvuru doğrulanmadan kopyayı silmez. Sürüm içeriği okuyucusu yeni şifreli depoyu açar, eski webroot kayıtları için kök dışına çıkmayan geriye uyumlu okuma sağlar.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Runtime/sürüm geri yükleme testi yapılmadı.
- 🔴 **A-10 açık:** Araç/personel sürüm arşivleri, kalıcı temizleme kuyruğu/yeniden deneme, eski yetim dosya envanteri ve servisler arası telafi hâlâ bekliyor.

## A-10 devamı — araç/personel sürüm arşivlerinin bağımsız dosyalanması — 2026-10-06

- 🟢 Araç ve personel sürüm arşivleri artık mevcut dosyanın aynı path'ini paylaşmaz; `CopyVersionFileAsync` ile bağımsız şifreli sürüm dosyası oluşturulur. SecureFileService dışındaki eski webroot dosyaları güvenli kök altında okunup yeni korumalı depoya alınır.
- 🟢 Sürüm içeriği okuyucuları şifreli depoyu açar, legacy webroot yolunu kök dışına çıkış denetimiyle destekler. (Sonraki 2026-10-06 düzeltmesiyle sürüm soft-delete artık fiziksel dosyayı silmiyor; aşağıdaki güncel not geçerlidir.)
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Runtime test/gerçek dosya işlemi yapılmadı.
- 🔴 **A-10 açık:** Dosya silme ile sürüm referans kontrolü eşzamanlı DB transaction/row lock altında değildir; kalıcı cleanup kuyruğu ve eski yetim dosya envanteri hâlâ gereklidir. Diğer servis akışlarının tamamı taranıp düzeltilmedi.

## A-10 devamı — sürüm silme yarışı ve personel evrakı yenileme sırası — 2026-10-06

- 🟢 Sürüm soft-delete işlemleri artık dosyayı fiziksel olarak silmiyor. Geri yükleme ile referans kontrolü/silme arasındaki yarış, kalıcı ve serileştirilmiş cleanup mekanizması gelene kadar dosyayı koruyarak önlendi.
- 🟢 EBYS personel evrakı yenilemesinde mevcut dosya yeni upload ve DB güncellemesinden önce silinmiyor. Yeni dosya önce şifreli depoya yazılıyor; DB kaydı hata verirse taze context ile ana kayıt ve sürüm tablosu kontrol ediliyor. Başvuru doğrulanamıyorsa dosya korunup hata görünür kılınıyor.
- 🟡 Eski personel dosyası bu akışta otomatik temizlenmiyor; geçmiş sürüm ilişkisi net olmadığı için tutuluyor. Kalıcı cleanup kuyruğu/yeniden deneme ve yetim envanteri gereklidir.
- 🔴 **A-10 açık:** Genel servis taraması, belirsiz commit telafileri ve kalıcı dosya temizleme altyapısı tamamlanmadı.

## A-10 devamı — fatura PDF/XML dosya değiştirme sırası — 2026-10-06

- 🟢 Fatura PDF ve XML yenilemesinde eski dosya artık yeni dosya ve DB yolu kaydedilmeden silinmiyor. Yeni içerik şifreli depoya yazılıyor, DB path'i sonra güncelleniyor.
- 🟢 DB kaydı hata verirse taze context ile PDF/XML yolunun fatura kayıtlarında bulunup bulunmadığı doğrulanıyor. Başvuru varsa PDF yüklemesi başarılı kabul ediliyor; başvuru yoksa yeni dosya telafi ediliyor. Doğrulama/temizlik hatasında AggregateException ile belirsizlik görünür kalıyor.
- 🟡 Eski dosya otomatik kaldırılmıyor; firma içi kopya faturalar aynı yolu paylaşabildiği için güvenli, referans kontrollü kalıcı cleanup kuyruğu bekleniyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Runtime fatura dosyası işlemi yapılmadı.
- 🔴 **A-10 açık:** Kalıcı cleanup/yeniden deneme kuyruğu, yetim dosya envanteri ve diğer tüm dosya yazan servislerin incelemesi gereklidir.

## A-10 devamı — destek eki şifreli depolama ve upload telafisi — 2026-10-06

- 🟢 Yeni destek talebi ve yanıt ekleri artık webroot içine düz metin yazılmıyor; `ISecureFileService` ile şifreli depoya kaydediliyor. Okuma akışı şifreli yeni yolları açıyor ve eski webroot eklerini kontrollü kök altında geriye dönük destekliyor.
- 🟢 DB kaydı başarısız/belirsiz kaldığında taze context ile ek tablosu kontrol ediliyor; DB başvurusu yoksa yeni dosya telafi ediliyor, doğrulama/temizlik hatasında dosya korunup hata görünür kılınıyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Gerçek ek yükleme/indirme/silme senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Eski düz metin eklerin toplu şifreli depoya taşınması, kalıcı cleanup kuyruğu/yeniden deneme ve yetim envanteri henüz yapılmadı.

## A-10 devamı — ortak evrak ekranı dosya sırası ve yol doğrulaması — 2026-10-06

- 🟢 Ortak evrak ekranında yenileme öncesi eski dosya silme kaldırıldı. Yeni dosya DB kaydından önce yazılıyor; DB başarısız/belirsiz kalırsa taze context ile referans doğrulanıp yalnız başvurulmayan yeni dosya telafi ediliyor.
- 🟢 Evrak silme akışında önce DB soft-delete kaydediliyor; fiziksel silmeden önce diğer aktif kayıtlar denetleniyor. Temizlik hatası artık sessizce yutulmuyor.
- 🟢 `FileService` okuma/silme/yol çözümlemesi, depolama kökü dışına taşan veya mutlak yol içeren dosya adlarını reddediyor.
- 🟡 Eski evrak dosyaları, kalıcı referans kontrollü temizleme kuyruğu kurulana kadar tutuluyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Runtime dosya senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Eski düz metin dosyaların şifreli depoya taşınması, kalıcı cleanup/yeniden deneme ve yetim envanteri devam ediyor.

## A-10 devamı — personel özlük ekranı upload telafisi ve sürüm referansları — 2026-10-06

- 🟢 Personel özlük ekranında upload sonrası bir UI/tarih yenileme hatası yeni dosyayı silip DB'de bozuk başvuru bırakabiliyordu. Telafi artık yeni yolu ana kayıt, sürüm geçmişi ve ortak evrak kayıtlarında kontrol ediyor; başvuru varsa dosyayı koruyor, kontrol başarısızsa da silmiyor.
- 🟢 Başarılı yenilemede eski fiziksel dosya otomatik silinmiyor. Hızlı silme de ana kayıt temizlendikten sonra sürüm ve ortak evrak referanslarını kontrol ediyor; geçmişte kullanılan dosyanın silinmesini erteliyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Gerçek upload/geri yükleme/silme senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı cleanup kuyruğu/yeniden deneme ve yetim dosya envanteri; ayrıca eski dosyaların kontrollü temizliği gerekiyor.

## A-10 devamı — araç/personel arşiv dosyalarının sürüm başına benzersizleştirilmesi — 2026-10-06

- 🟢 Arşiv servisi aynı araç/personel evrak tipi için sabit şifreli dosya adını tekrar kullanıyordu; yeni yükleme geçmiş sürüm içeriğinin üstüne yazabiliyordu. Her yeni arşiv yazımı artık GUID tabanlı benzersiz dosya yoluna kaydediliyor.
- 🟢 Araç upload telafisi artık DB kayıt işleminden sonra çalışan cache temizliği gibi bir adım hata verdiğinde taze context ile dosya yolu ve sürüm referanslarını denetliyor; başvurulan dosyayı silmiyor. Doğrulama/temizlik hatasında belirsizlik AggregateException ile korunuyor.
- 🟢 Personel arşiv yüklemesi kaynak dosya adı/türü/boyutunu kayıt metoduna iletiyor; dosya adı benzersiz saklama adıyla karışmıyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Gerçek arşiv sürümleme testi yapılmadı.
- 🔴 **A-10 açık:** Mevcut sabit isimli arşivlerin geriye dönük taraması, cleanup kuyruğu/yeniden deneme ve yetim envanteri gereklidir.

## A-10 devamı — araç evrak upload telafisi ve silme referans kontrolü — 2026-10-06

- 🟢 Araç upload telafisi, yeni dosya DB’de veya sürüm tablosunda referanslıysa artık silmiyor. Cache invalidation gibi DB commit sonrası hata oluşsa da içerik korunuyor; referans denetimi başarısızsa AggregateException ile işlem belirsizliği bildiriliyor.
- 🟢 Araç evrak dosyası silme DB soft-delete sonrasında taze context ile diğer aktif dosya ve sürüm referanslarını kontrol ediyor; bilinen paylaşımlı yol fiziksel olarak kaldırılmıyor.
- 🟡 Referans kontrolü ile fiziksel silme halen tek bir DB transaction/lock içinde değil. Kalıcı serialized cleanup kuyruğu kurulana kadar A-10 kırmızı kalıyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Runtime eşzamanlı upload/restore testi yapılmadı.

## A-10 devamı — Şoför formu upload telafisi — 2026-10-06

- 🟢 Şoför formunda post-save içerik doğrulaması veya UI yenilemesi hata verdiğinde `catch` artık yeni dosyayı körlemesine silmiyor. Taze DB context ile özlük ana kayıtları, sürüm geçmişi ve ortak evrak yolları denetleniyor.
- 🟢 DB başvurusu varsa dosya korunuyor; referans sorgusu veya cleanup başarısızsa hata loglanıp belirsizlik AggregateException ile bildiriliyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Gerçek personel upload senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı cleanup kuyruğu/yeniden deneme, yetim envanteri ve tüm upload/silme yollarının çalışma zamanı kabulü gereklidir.

## A-10 devamı — taşıma tedarikçisi eki upload telafisi ve referans kontrolü — 2026-10-06

- 🟢 Tedarikçi eki upload hatasında yeni şifreli dosya silinmeden önce taze context ile dosya yolu DB'de aranıyor. Belirsiz committe referans varsa dosya korunuyor; sorgu/temizlik hatası AggregateException ile görünür.
- 🟢 Tedarikçi eki silme DB soft-delete sonrasında diğer aktif eklerde aynı yol aranarak fiziksel temizlik yapıyor; ortak kullanılan dosya silinmiyor.
- 🟡 Kontrol ve fiziksel silme arasında çoklu süreç kilidi yok; A-10'un kalıcı cleanup kuyruğu/serialized işleyicisi hâlâ açık.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `git diff --check` temiz. Runtime upload/silme senaryosu çalıştırılmadı.

## A-10 devamı — destek talebi eki silme sırası — 2026-10-06

- 🟢 Destek talebi eki silmede önce DB soft-delete kaydediliyor. Sonra taze context ile başka aktif ek kaydının aynı dosya yoluna başvurup başvurmadığı kontrol ediliyor; fiziksel dosya yalnız başvuru yoksa siliniyor.
- 🟢 Fiziksel yolun destek yükleme kök dizini altında olduğu doğrulanıyor. Silme/konum hataları artık boş catch ile yutulmuyor; ek kimliği loglanıp `FileCleanupPendingException` ile bildiriliyor.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Destek eki runtime silme senaryosu çalıştırılmadı.
- 🔴 **A-10 açık:** Kalıcı cleanup/yeniden deneme kuyruğu, yetim dosya envanteri ve diğer tüm dosya yazan servislerin incelemesi gereklidir.


## Kurulumda veritabanı seçimi — 2026-10-06

- 🟢 Ana (Setup.iss) ve müşteri (MusteriSetup.iss) Inno kurulumlarına PostgreSQL / SQLite / MSSQL seçim adımı eklendi. PostgreSQL bağlantı alanları; SQLite dosya yolu alınır ve seçime uygun dbsettings.json kurulumda yazılır. Güncelleme paketi mevcut ayarı korur.
- 🟢 IIS kur.ps1 -Mode Install kurulumunda da sağlayıcı sorulur; PostgreSQL parolası maskeli alınır, yapılandırma geçici dosyadan atomik taşınır. Kurulum modu mevcut SQLite dosyasını artık silmez.
- 🟡 **MSSQL açık:** Kurulum sihirbazı seçimi destek sınırını açıklayıp durdurur. 2026-10-08 düzeltmesi ayrıca Web başlangıcında ve ayar kaydetme/testinde PostgreSQL/SQLite dışı sağlayıcıları migration öncesi reddeder. SQL Server/MySQL tam desteği veya yetkili ürün kapsam kararı ve PostgreSQL/SQLite hedef kurulum kabulü yoktur; A-28 kırmızı kalır.
- 🟢 Güncel Web/DataSync publish ile ana, güncelleme ve müşteri EXE paketleri v1.0.37 üretildi. Ana IIS kurulumunda dbsettings.json okuması yöneticiler ve yalnız ilgili uygulama havuzuyla sınırlandı; SQLite App_Data yazma izni uygulama havuzuna verilir. 🟡 Etkileşimli hedef makine kurulumu ve gerçek DB bağlantı kabulü yapılmadı.

## A-15 devamı — aktif araç plakası DB tekilliği — 2026-10-06

- 🟢 `AracPlakalar` için aktif ve silinmemiş kayıtlara uygulanan `(Plaka, CikisTarihi)` filtreli benzersiz indeks model, snapshot ve yeni migration'a eklendi. Aynı plakanın eşzamanlı iki context tarafından aktif kaydedilmesi artık DB kısıtına takılır.
- 🟢 Migration öncesi yinelenen aktif plakalar sorgulanır; varsa migration satır değiştirmeden durur ve örnek plakaları hata mesajında verir.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. `setup/output/a15-build` kullanıldı.
- 🟡 PostgreSQL/SQLite migration ve eşzamanlı yazım runtime kabulü yapılmadı. Banka referansları, dönem snapshotları, varsayılan fatura/grup şablonları ve A-15 kapsamındaki kalan tenant ilişkileri açıktır; bu nedenle A-15 🔴 kalır.

## A-15 devamı — banka importu eşzamanlı tekrar koruması — 2026-10-06

- 🟢 Referans numarası bulunan import satırlarına firma, tarih (gün), referans, tutar ve borç/alacak yönünden deterministik SHA-256 anahtarı yazılıyor. Mevcut dosya/hash ve satır tekrar kontrolleri korunuyor.
- 🟢 `FinansHareketler` için aktif, anahtarlı satırlarda `(FirmaId, IthalatTekillikAnahtari)` benzersiz kısıtı eklendi. Paralel importların aynı yeni satırı birlikte eklemesi DB seviyesinde engellenir; soft-delete edilen hareket yeniden import edilebilir.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `setup/output/a15-bank-import-build` kullanıldı.
- 🟡 Gerçek PostgreSQL/SQLite migration ve eşzamanlı import kabulü yapılmadı. Eski finans hareketlerine geriye dönük anahtar yazılmadı; uygulama ön kontrolü bu mevcut satırları denetlemeyi sürdürür. Dönem snapshot ve varsayılan şablon kısıtları açık olduğundan A-15 🔴 kalır.

## A-15 devamı — aylık personel maaş snapshot tekilliği — 2026-10-06

- 🟢 Maaş snapshot doğal anahtarı firma + yıl + ay + personel olarak sabitlendi. Silinmemiş kayıtlar için filtreli benzersiz DB indeksi eklendi; soft-delete edilen snapshot dönem/personel için yeni kayıt oluşturulmasını engellemez.
- 🟢 Migration öncesinde mevcut aktif yinelenen gruplar bulunursa veri değiştirilmeden migration durur ve firma/dönem/personel grupları hata mesajında raporlanır.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `setup/output/a15-snapshot-build` kullanıldı.
- 🟡 Gerçek PostgreSQL/SQLite migration ve eşzamanlı snapshot yazımı kabulü yapılmadı. Araç maliyet snapshotı için soft-delete/yeniden üretim uyumu, varsayılan şablon tekillikleri ve kalan firma ilişkileri açıktır; A-15 🔴 kalır.

## A-15 devamı — araç maliyet snapshotı yeniden üretim uyumu — 2026-10-06

- 🟢 Araç/yıl/ay tekilliği korunurken indeks yalnız silinmemiş snapshotlara uygulanacak şekilde değiştirildi. Soft-delete sonrası aynı araç/dönem snapshotı tekrar üretilebilir.
- 🟢 Mevcut DB'deki eski benzersiz indeks yeni migration ile filtreli indekse dönüştürülüyor. Önceki indeks silinmiş satırlar dahil tekillik sağladığından önceden çakışan kombinasyon bulunamaz.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `setup/output/a15-all-snapshots-build` kullanıldı.
- 🟡 Gerçek PostgreSQL/SQLite migration ve snapshot silip yeniden üretme kabulü yapılmadı. Varsayılan şablon tekillikleri ve kalan firma ilişkileri açık olduğundan A-15 🔴 kalır.

## A-15 devamı — varsayılan fatura ve grup şablonu tekilliği — 2026-10-06

- 🟢 Fatura şablonlarında aktif varsayılan başına firma; fatura grup şablonlarında firma geneli varsayılan başına firma ve kullanıcı varsayılanı başına firma/kullanıcı DB indeksleri eklendi. Böylece null kullanıcı değerinin SQL benzersizlik semantiği ayrı indeksle doğru ele alındı.
- 🟢 Migration öncesinde aktif çoklu varsayılanlar saptanır; migration veri değiştirmeden durur ve firma/kullanıcı kapsamını bildirir.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; son doğrulama `setup/output/a15-integrity-final` ile yapıldı.
- 🟡 Gerçek migration ve eşzamanlı şablon değiştirme kabulü yapılmadı. A-15’in diğer firma/ilişki kısıtları için kaynak denetimi ve PostgreSQL/SQLite kabulü sürüyor; A-15 🔴 kalır.
- 🟢 Yinelenen veri ön kontrolleri, temiz kurulumda ilgili tablolar önceki bekleyen migration'larda henüz oluşmadıysa sorguyu atlayacak şekilde korundu. Mevcut tablolar varsa veri uzlaştırma denetimi çalışır.

## A-15 devamı — banka hareketi hesap/cari firma kapsamı — 2026-10-06

- 🟢 Banka/Kasa hareketi oluşturma ve güncelleme, seçilen banka hesabı ve carinin hareketin `FirmaId` değeriyle aynı firmada olduğunu aynı DbContext sorgularıyla doğrular. Aktif firma dışında hareket yazımı ve güncellemede kayıt firma kapsamı değiştirme reddedilir.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata; `setup/output/a15-bank-tenant-build` kullanıldı.
- 🟡 Doğrudan DbContext ile yazan diğer servis yolları için aynı ilişki denetimi ve DB composite FK yoktur; bu yüzden A-15 ilişkileri tamamlanmış sayılmaz. Servis runtime/firma A-B kabulü yapılmadı.

## A-15 devamı — ortak SaveChanges ilişki denetimi — 2026-10-06

- 🟢 Banka/Kasa hareketi ekleyen veya değiştiren EF SaveChanges yolları, hesap, isteğe bağlı cari ve personel geri ödeme hesabının hareketle aynı firmaya ait olduğunu kaydetmeden önce denetler. Mevcut hareketin firma kapsamı değiştirilemez. Toplu hareketlerde ilişkili kimlikler iki sorguyla kontrol edilir; aynı context'te yeni eklenen hesap/cari bağlantıları da değerlendirilir. Denetim hem senkron hem asenkron kayıt yolundadır.
- 🟢 Web Debug derlemesi başarılı: 0 uyarı, 0 hata. Geçici, teslim dışı SQLite bellekiçi doğrulamada farklı firma hesabı, farklı firma carisi, yeni eklenen farklı firma carisi, geri ödeme hesabı ve mevcut hareketin firma taşıması reddedildi; aynı firma hareketi kaydedildi.
- 🟢 Audit kaydının üretilen kayıt kimliği, açık firma sağlayıcısı olmayan işlemde de `IgnoreQueryFilters()` ile aynı transaction içinde tamamlanıyor. SQLite bellekiçi doğrulamada iki yeni firma kaydının audit `EntityId` değerleri bulundu. Bu değişiklik A-08'in firmasız sistem işlemleri için gerçek müşteri kabulü anlamına gelmez.
- 🟡 Raw SQL, dış araç ve doğrudan DB yazımları EF kaydetme denetiminden geçmez. DB composite FK, diğer banka hareketi tenant bağlantıları, mevcut veri taraması ve PostgreSQL/SQLite migration/eşzamanlılık kabulü açık; A-15 🔴 kalır. İzole doğrulama tam mali işlem kabulü değildir.

## A-15 devamı — banka hareketi DB firma tetikleyicileri — 2026-10-06

- 🟢 PostgreSQL/SQLite migration'ı banka hareketi INSERT/UPDATE işlemlerinde hesap, cari ve personel geri ödeme hesabı firma bağını denetler; bağlı hesap/carinin firma alanının sonradan değiştirilmesini reddeder. Böylece migration uygulandıktan sonra bu üç bağ için doğrudan SQL yazımı da denetimden geçer.
- 🟢 Mevcut uyuşmazlıklar için migration ön kontrolü eklenmiştir; veri kendiliğinden düzeltilmez. Web Debug derlemesi 0 uyarı, 0 hata.
- 🟡 Migration henüz gerçek PostgreSQL/SQLite veritabanına uygulanmadı. Eski verinin kontrollü onarımı, diğer tenant bağlantıları ve eşzamanlı yazım kabulü açık; A-15 🔴 kalır. [Güncel görev envanteri](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md) izlenir.

## A-22 kapanışı — güncel NuGet taraması — 2026-10-06

- 🟢 LisansDesktop'un geçişli `SQLitePCLRaw.lib.e_sqlite3 2.1.11` güvenlik bildirimi tespit edilip doğrudan 2.1.13 ile giderildi. LisansDesktop Release/win-x64 dahili EXE yeniden yayımlandı.
- 🟢 Çözümdeki altı proje ve çözüm dışı Rent-a-Car kontrol projesinin doğrudan/geçişli NuGet taramasında artık bilinen açık raporlanmıyor. CI tarama hatasında başarısız olacak şekilde düzenlendi. [Tarama ayrıntısı](A-22-BAGIMLILIK-TARAMASI-2026-10-06.md).
- 🟡 Yeni güvenlik bildirimleri ve müşteri paketi kurulum kabulü ayrı takip edilir; A-22'nin 2026-10-06 kaynak taraması 🟢 tamamlandı.

## A-15 devamı — PostgreSQL/SQLite izole tetikleyici kabulü — 2026-10-06

- 🟢 Asgari şemalı SQLite bellek DB ve ayrı PostgreSQL 17 geçici kümesinde migration SQL'i uygulandı. Ön kontrol eski firma uyuşmazlığını reddetti; geçerli hareket kaydedildi. Her sağlayıcıda yedi hatalı değişiklik reddedildi. Hareketin hesap/cariyle birlikte firma değiştirmesi de DB sınırında durduruldu.
- 🟢 Son Web Debug derlemesi 0 uyarı, 0 hata. [İzole kanıt](A-15-IZOLE-FIRMA-BAGI-DOGRULAMA-2026-10-06.md).
- 🟡 Tam model/müşteri migration zinciri, mevcut veri onarımı, diğer ilişkiler ve eşzamanlılık kabulü açık. A-15 🔴 kalır.

## A-07 devamı — kalıcı xUnit ve zorunlu CI testi — 2026-10-06

- 🟢 `MKFiloServis.Tests` projeye/çözüme eklendi. A-15 SQLite firma bağı için 9 test Release olarak yerelde geçti; ön kontrol, geçerli hareket ve hatalı bağlantı/yazımlar kapsandı.
- 🟢 Tests workflow'u artık eksik test projesini atlamıyor; restore, build ve test zorunlu. Yerelde kapsam toplama büyük Web assembly'sinde zamanında bitmediğinden CI'de TRX sonuçlarıyla sınırlı tutuldu.
- 🟡 GitHub çalışma kanıtı ve lisans, tenant, audit, restore, mali işlem regresyonları açık. A-07 🟡; [güncel envanter](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md) izlenir.

## A-07 devamı — lisans protokolü testleri — 2026-10-06

- 🟢 Modül zarfının kanonik biçimi ve reddedilen girdiler, RSA-PSS imzasının modül seçimine bağı ve imzalı sürüm sınırları kalıcı xUnit testlerine eklendi. Release sonuç: **21/21 başarılı**.
- 🟡 Geçici RSA anahtarlı protokol testi gerçek üretim lisansının doğrulanması değildir. Müşteri lisansı, modül erişimi, tenant/audit/restore/mali akış ve GitHub CI kabulü açık; A-01/A-02/A-07 🟡.

## A-10 devamı — şifreli upload atomik yayımlama — 2026-10-06

- 🟢 SecureFileService yeni şifreli dosyayı aynı klasördeki benzersiz geçici dosyaya yazar, tamamlanınca son dosya adına taşır; hata veya iptalde geçici dosyayı temizlemeye çalışır.
- 🟢 Kalıcı testler başarılı kaydı ve iptalde dosya kalmamasını denetledi; Release test toplamı **23/23 başarılı**.
- 🔴 Çoklu servisleri kapsayan kalıcı temizleme kuyruğu, yeniden deneme, yetim envanteri ve müşteri depolama kabulü açık; A-10 kırmızı kalır.

## A-10 devamı — özlük dosyası paylaşılan yol koruması — 2026-10-06

- 🟢 Özlük dosyası veya sürüm satırı kaldırıldıktan sonra SecureFileService ile fiziksel silme öncesinde aktif ve geçmiş özlük satırları `IgnoreQueryFilters()` ile taranır; aynı yolu kullanan başka kayıt varsa silme atlanır.
- 🟢 Release derleme ve mevcut test paketi geçti (**23/23**); bu özel eski paylaşım senaryosu için doğrudan DB entegrasyon testi eklenmedi.
- 🔴 Kontrol-silme yarışı, kalıcı kuyruk/yeniden deneme, diğer dosya varlıklarının referansları ve yetim envanteri hâlâ açık.

## A-10 devamı — yerel nesne deposu anahtar sınırı — 2026-10-06

- 🟢 Yerel nesne deposu boş/köklenmiş anahtarları reddeder, normalize hedefin uploads kökü içinde kalmasını doğrular ve upload'ı geçici dosyadan yayımlar. Silme `File.Exists` ile IO hatalarını yutmaz.
- 🟢 Release test paketi **25/25**; iki yerel nesne deposu senaryosu eklendi.
- 🔴 Symlink sınırı, kalıcı temizlik kuyruğu, çok süreçli yarış ve gerçek depolama arızası kabulü açık.

### 2026-10-06 — A-10 sembolik bağlantı yol denetimi

- 🟢 Ortak depolama yolu çözümleyicisi mevcut sembolik bağlantı bileşenlerini reddediyor; yerel nesne deposu aynı çözümleyiciyi kullanıyor. Release test paketi 25/25 geçti, diff kontrolü temiz.
- 🟡 Windows hesabı sembolik bağlantı yaratma ayrıcalığı vermediğinden saldırı testi doğrulanamadı. Mevcut yol kontrolü işlem anına kadar atomik değil (TOCTOU); bu nedenle A-10 🔴 kalır. Kalıcı cleanup kuyruğu, yetim envanteri ve gerçek depolama kabulü de açık.

### 2026-10-06 — A-10 eksik üst dizinde idempotent silme

- 🟢 Yerel nesne deposu, dosyanın üst dizini bulunmadığında başarılı no-op döner; erişim/IO hatalarını gizlemez.
- 🟢 Eklenen regresyonla Release test paketi 26/26 geçti; diff kontrolü temiz.
- 🔴 Kalıcı cleanup kuyruğu, çok süreçli yarış/TOCTOU, yetim envanteri ve gerçek storage kabulü açık; A-10 🔴 kalır.

### 2026-10-06 — A-10 ortak idempotent silme

- 🟢 Eksik üst klasör davranışı ortak yol yardımcısına taşındı ve şifreli dosya servisine de uygulandı; IO/erişim hataları saklanmaz.
- 🟢 Release test paketi 26/26 başarılı, diff kontrolü temiz.
- 🔴 Şifreli servis özelinde entegrasyon testi, kalıcı cleanup kuyruğu, TOCTOU/çok süreçli yarış, yetim dosya envanteri ve gerçek depolama kabulü açık; A-10 🔴 kalır.

### 2026-10-06 — A-10 legacy dosya migration sırası

- 🟢 Dosya migration'ında önce encrypted target yazılır, sonra DB yeni yola alınır; eski açık kaynak ancak yeni referans DB'de doğrulandıktan ve başka kayıtlar eski yolu kullanmıyorsa silinir.
- 🟢 Eski yol uploads dizini dışına çıkamaz; sembolik bağlantı bileşenleri reddedilir. Release testleri 26/26, diff kontrolü temiz.
- 🔴 DB sonrası crash için kalıcı cleanup/retry kuyruğu ve gerçek eski veri migration kabulü açık. A-10 kapanmadı.

- 🟢 Eski yol referans taraması, bilinen DB dosya alanlarında soft-delete filtrelerini de yok sayarak ortak evrak, özlük, destek, tedarikçi ve fatura kayıtlarını kapsar.

### 2026-10-06 — A-10 migration ekranı erişim ve kapsam açıklaması

- 🟢 Legacy migration ekranı Admin rolüyle sınırlandı; EBYS lisans şartı da sürüyor. Açıklama, işlemin aktif firma kapsamındaki EBYS/araç ana ve sürüm kayıtlarıyla sınırlı olduğunu bildiriyor.
- 🟢 Release testleri 26/26 başarılı; `git diff --check` temiz.
- 🔴 Soft-delete kayıtları ve diğer file path tabloları taranmıyor; kullanıcı kabulü, durable cleanup ve orphan envanteri açık. A-10 🔴 kalır.

### 2026-10-06 — A-10 referanssız şifreli dosya raporu

- 🟢 Admin bakım ekranı uploads/Arsiv/Depo altındaki `.enc` dosyalarını bilinen DB file path alanlarıyla kıyaslar ve eşleşmeyenleri listeler; DB soft-delete filtreleri yok sayılır.
- 🟢 İşlem salt okunur; dosya silinmez. Release testleri 27/27 geçti.
- 🔴 Düz metin orphanlar, kapsam dışı property/varlık türleri, müşteri DB kabulü ve durable cleanup/retry eksik; A-10 🔴 kalır.

### 2026-10-06 — A-10 kalıcı dosya temizleme kuyruğu

- 🟢 Fiziksel silme çağrıları artık Data Protection ile şifrelenen, atomik JSON günlüğüne kalıcı cleanup girdisi koyuyor. Günlük mutasyonları dosya kilidiyle serileştirilir; yinelenen girdiler tekilleştirilir, tamamlananlar kaldırılır ve hatalar artan bekleme süresiyle yeniden denenir.
- 🟢 Hosted worker bir dakikalık başlangıç gecikmesinden sonra her dakika due kayıtları alır; silmeden önce bilinen dosya yolu property'lerini soft-delete filtreleri dâhil yok sayarak veritabanında kontrol eder. Journal kalıcılığı/retry testi eklendi; Release testleri **28/28 başarılı**. `git diff --check` temiz; mevcut dosyalarda yalnız CRLF dönüşüm uyarıları var.
- 🟡 Günlük uygulama depolama kökünde tutulur ve DP key ring'e bağımlıdır; farklı makine/instance paylaşımı veya key-ring kurtarması burada doğrulanmadı.
- 🟢 Kuyruk claim'i 5 dakikalık lease ile tek worker'a verilir; Release regresyonu ikinci eşzamanlı claim'i reddeder ve lease süresi dolunca yeniden claim'e izin verir.
- 🟢 Worker her seferinde tek girdiyi claim eder ve işlem sürerken lease'i dakikada bir yeniler. Fiziksel silme öncesinde iptal tekrar kontrol edilir. Release test paketi **28/28 başarılı**.
- 🔴 Gerçek tenant DB/depo kabulü yok. DB commit ile cleanup isteğinin günlüğe yazılması arasında çökme penceresi kalır; referans kontrolü ile unlink transaction/lock altında değildir. Lease yenileme kesilirse yeniden sahiplenme mümkündür. A-10 tamamlanmadı.

### 2026-10-06 — A-10 mutlak yol ve referans eşleştirmesi

- 🟢 Depolama kökü içindeki mutlak yolların normalleştirilmesi düzeltildi. Worker, SQL'de doğrudan eşleşme bulamazsa bilinen DB dosya yollarını kanonik anahtara çevirerek bir kez daha kontrol eder; Windows dosya sistemi ile DB kolasyonu arasındaki harf büyüklüğü farkında referanslı dosyayı korur.
- 🟢 Mutlak yol regresyonu dahil Release testleri **28/28 başarılı**. Gerçek DB performansı/çeviri davranışı ve referans kontrolü ile silme arasındaki yarış kabul edilmedi; A-10 🔴.

### 2026-10-06 — A-10 şifresiz dosya aday envanteri

- 🟢 Admin evrak bakım raporu uploads/Arsiv içindeki DB yol kaydı bulunmayan şifresiz dosya adaylarını da ayrı sayıp listeler. Geçici dosyalar ve sembolik bağlantılar dışarıda; işlem salt okunur.
- 🟢 Aynı bilinen DB yol alanları şifreli ve şifresiz listelerde bir kez okunur. Release test paketi **28/28 başarılı**.
- 🔴 Aday listesi kesin silme kararı değildir. Referanslı eski açık dosyaların gerçek geçişi, tam DB alan kapsamı ve müşteri depolama kabulü açık; A-10 🔴 kalır.

### 2026-10-06 — A-10 tam model SQLite referans doğrulaması

- 🟢 Tam `ApplicationDbContext` modeliyle oluşturulan izole SQLite DB'de soft-delete edilmiş ve harf büyüklüğü farklı olan dosya yolu, worker'ın kullandığı referans denetleyicisi tarafından korunur; bulunmayan yol serbest bırakılır. Release test paketi **29/29 başarılı**.
- 🔴 Gerçek müşteri DB/depo ile worker kabulü, PostgreSQL davranışı, DB commit-kuyruk aralığı ve kontrol-silme yarışı açık; A-10 🔴 kalır.

### 2026-10-06 — A-10 izole worker silme akışı

- 🟢 Tam model SQLite DB, fiziksel uploads dosyaları, şifreli kalıcı kuyruk ve gerçek `SecureFileService` birlikte çalıştırıldı. Worker soft-delete kaydın referanslı dosyasını korudu; DB'de bulunmayan dosyayı sildi ve kuyruk kaydını tamamladı.
- 🟢 Release test paketi **30/30 başarılı**.
- 🔴 Müşteri DB/deposu ve PostgreSQL kabulü, commit-kuyruk crash penceresi ve referans sorgusu ile unlink arasındaki yarış açık; A-10 🔴.

### 2026-10-06 — A-10 geri alma için soft-delete dosyalarını koruma

- 🟢 Kullanıcı kararı uyarınca soft-delete satırına bağlı dosya fiziksel olarak korunur. `SecureFileService` doğrudan silme çağrısında da worker işleminde de DB referansını kontrol eder; referans varsa journal isteğini tamamlar.
- 🟢 Tam model SQLite senaryosunda doğrudan silme ve worker çağrısı referanslı dosyayı korudu; referanssız dosya silindi. DB referans sorgusu arızasında dosya ve journal isteği korundu. Release test paketi **31/31 başarılı**.
- 🔴 Gerçek müşteri DB/deposunda kabul, commit ile journal yazımı arasındaki crash penceresi, referans sorgusu ile unlink arasındaki yarış ve eski dosya alanlarının tam kapsamı açık. A-10 genel durumu 🔴.

### 2026-10-06 — A-10 eski düz dosya silme yolları

- 🟢 Ortak evrak ekranı ve destek eki servisindeki soft-delete işlemleri eski düz dosyayı artık fiziksel olarak silmez; geri alma kararı bu iki yol için de uygulanır.
- 🟢 Ortak evrakın yeni yüklemesi şifreli depoya geçirildi. Görüntüleme/indirme geçmiş düz dosyaları destekler; bakım ekranı iki türde dosya varlığını kontrol eder. Web Release derlemesi 0 uyarı ve 0 hatayla tamamlandı.
- 🔴 Geçmiş düz dosyaların geçişi ve gerçek müşteri kabulü açık. A-10 genel durumu değişmedi.

### 2026-10-06 — A-10 aktif ortak evrak eski dosya geçişi

- 🟢 Yönetici geçiş aracı aktif ortak evrakın eski düz dosyalarını önizler ve şifreli depoya taşır. Yeni DB yolu doğrulanır; başka aktif veya silinmiş kaydın eski yola başvurusu varsa eski dosya tutulur. Ekranın kapsam ve toplam sayısı güncellendi.
- 🟢 Web Release derlemesi 0 uyarı/0 hatayla tamamlandı. Bu değişikliğin çalışma zamanı geçiş testi yapılmadı.
- 🔴 Silinmiş kayıtların kendi dosya geçişi, gerçek müşteri veri/depo kabulü ve sorgu-silme yarışı açık; A-10 🔴.

- 🟢 Geçiş tek aktif firma gerektirir; ortak evrak bağlantılı personel/araç firma kimliğiyle sınırlandırılır. Bağı kopuk kayıtlar otomatik geçişe alınmaz. Eski dosya yolu çözümünde sembolik bağlantı geçişi reddedilir.

### 2026-10-06 — A-10 silinmiş ortak evrak dosyaları

- 🟢 Silinmiş ortak evrakın düz dosyası da, silinmiş ebeveynler dahil açık firma kimliği eşleşiyorsa şifreli depoya taşınır. Önizleme aktif/silinmiş sayıları ayrı gösterir; yeni yol silinmiş DB satırında kalır ve geri alma için erişilebilir olur.
- 🟢 Web Release derlemesi 0 uyarı/0 hata. Bu geçişin gerçek veriyle çalışma zamanı kabulü yapılmadı.
- 🔴 Bağı kopuk eski kayıtlar otomatik geçmez. EBYS/araç silinmiş belge geçmişi ve sorgu-silme yarışı açık; A-10 🔴.

- 🟢 Araç dosyası ve sürümü seçili firma kimliğiyle açıkça sınırlandırıldı. EBYS'nin firma bağı olmayan genel kapsamı geçiş ekranında bildiriliyor; firma sahipliği ayrıca açık.

### 2026-10-06 — A-10 şifreli dosyanın geri okuma doğrulaması

- 🟢 Geçiş aracı yeni şifreli dosyayı geri açıp kaynakla karşılaştırmadan DB yolunu değiştirmez. Doğrulama hatasında eski kaynak korunur; yeni kopya temizlenmeye çalışılır. Okuma/yazma/doğrulama iptale bağlıdır.
- 🟢 Web Release derlemesi 0 uyarı/0 hata. Çalışma zamanı geçiş testi yapılmadı; A-10 🔴 kalır.

### 2026-10-06 — A-10 eski dosya temizliği ve envanteri

- 🟢 Yeni şifreli yol DB'ye yazıldığı halde eski düz dosya temizlenemezse geçiş ekranı bunu ayrı `Temizlik Bekliyor` durumunda gösterir. Bakım raporu eski ortak evrak ve `wwwroot/uploads` klasörlerindeki DB yolu bulunmayan düz dosya adaylarını da salt okunur listeler.
- 🔴 Bu satırdaki eski dosya retry eksikliği aşağıdaki sonraki düzeltmeyle giderildi. Otomatik silme kararı ve gerçek müşteri depolama kabulü açık. A-10 🔴.

### 2026-10-06 — A-10 eski düz dosya temizliğinde kalıcı kuyruk

- 🟢 Geçişte yeni şifreli yol DB'ye yazıldıktan sonra eski ortak evrak veya `wwwroot/uploads` dosyasının temizliği DP korumalı journal'a eklenir. Mevcut worker başarısız silmeyi tekrar dener; her denemede aktif ve silinmiş DB dosya yolları yeniden kontrol edilir. Başvurulan dosya geri alma için korunur.
- 🟢 Web Release derlemesi 0 uyarı/0 hata. Son ek için çalışma zamanı testi yapılmadı.
- 🔴 Bu satırdaki eski dosya geçişine özgü commit-journal aralığı aşağıdaki sonraki düzeltmeyle giderildi. DB referans kontrolü ile fiziksel silme yarışı, büyük veri performansı ve gerçek müşteri DB/depo kabulü açık. A-10 rengi değişmedi.

### 2026-10-06 — A-10 eski dosya geçişinde journal önceliği

- 🟢 Geçiş aracı doğrulanmış yeni şifreli kopyanın eski/yeni yol çiftini DB değişikliğinden önce kalıcı günlüğe yazar. Worker yeni yol DB'de doğrulanmadan eski dosyayı silmez; commit sonrası aynı istekten güvenle devam eder. Eski v1 kuyruk girdileri desteklenir.
- 🟢 Web Release derlemesi 0 uyarı/0 hata; son değişiklik için çalışma zamanı testi yapılmadı.
- 🔴 Başarısız DB yazımında güvenle bekleyen girdilerin işletim temizliği, diğer silme akışlarının commit-kuyruk aralığı, sorgu-silme yarışı ve gerçek müşteri kabulü açık. A-10 🔴 kalır.

### 2026-10-06 — A-10 silinmiş EBYS ve araç dosya geçmişi

- 🟢 Dosya geçişi artık `IgnoreQueryFilters` ile silinmiş EBYS ve seçili firmaya bağlı araç dosyalarını ve versiyon satırlarını da bulur. Önizleme bunları aktif kayıtlarla ayrı sayar ve yönetici ekranında geri alınabilir dosya kapsamı açıklanır.
- 🟢 Araç kapsamı dosyanın firma alanını ve bağlı araç ilişkisini doğrular; firma bağı uyuşmayan veya kopuk araç kayıtları taşınmaz. Web Release derlemesi 0 uyarı/0 hata; gerçek veriyle geçiş kabulü yapılmadı.
- 🔴 EBYS kayıtlarında firma sahipliği bulunmadığından yönetici genel kapsamı ve müşteri kabulü açık kalır. Büyük DB tarama performansı, diğer dosya alanları ve referans-silme yarışı da bekliyor; A-10 🔴.

### 2026-10-06 — A-10 personel ve fatura dosya geçişi

- 🟢 Personel özlük ana/sürüm dosyaları ve fatura PDF/XML dosyaları, aktif/silinmiş kayıtlar dahil geçiş aracına eklendi. Firma filtresi personel sürücüsüne ve fatura kaydına açıkça uygulanır.
- 🟢 Ekran özetleri modül bazında gruplanarak sadeleştirildi; eski upload yol biçimleri ile personel eski tek dosya adı desteklenir. Web Release derlemesi 0 uyarı/0 hata; gerçek veri kabulü yapılmadı.
- 🔴 Destek/tedarikçi eski dosyalarının kök ve sahiplik eşlemesi, EBYS firma sahipliği, büyük DB performansı ve saha kabulü açık. A-10 kırmızı kalır.

### A-10 fatura eski dosyalarında fiziksel kök ayrımı — 2026-10-06

- 🟢 Fatura PDF/XML yolları `{StorageRoot}/uploads` kökünden okunur; journal bu kaynağı `wwwroot/uploads` ve ortak dosya kökünden ayrı, sabit anahtarla işler.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Çalışma zamanı veya gerçek müşteri verisiyle taşıma yapılmadı.
- 🔴 Destek/tedarikçi dosyalarının güvenli sahiplik/kök eşlemesi ve A-10'un diğer açık saha/yarış/performance maddeleri sürüyor; A-10 🔴.

### A-10 tedarikçi ekleri firma kapsamı — 2026-10-06

- 🟢 Tedarikçi dosya migrasyonu, yalnız tedarikçi kaydı seçili firmaya ait cari hesaba bağlıysa çalışır; aktif/silinmiş ekler sayılır. Ortak eski dosya kökündeki bare filename biçimi desteklenir.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Gerçek DB veya dosya taşıma çalıştırılmadı.
- 🔴 Firma-cari bağı olmayan destek talepleri ve mutlak destek dosya yolları bu turda migrasyona dahil edilmedi. Tedarikçi legacy path biçimleri ve saha kabulü açık; A-10 🔴.

### A-10 cari bağı doğrulanan destek ekleri — 2026-10-06

- 🟢 Destek eki taşıma kapsamı talep ve yanıt eklerinde `CariId → Cari.FirmaId` doğrulamasına bağlandı; silinmiş satırlar geri alma için sayılır ve taşınır.
- 🟢 Mutlak eski yol yalnızca `wwwroot/uploads/destek` altında çözülür; cleanup journal destek kökü için tipli anahtar kullanır. Web Release derlemesi **0 uyarı / 0 hata**.
- 🔴 Cari bağı olmayan talepler ve destek klasörü dışındaki path biçimleri kasıtlı olarak kapsam dışıdır. Gerçek runtime/müşteri kabulü, TOCTOU ve büyük veri performansı açık; A-10 🔴 kalır.

### A-10 personel özlük dosyasında commit öncesi temizleme kaydı — 2026-10-06

Personel özlük ana yolunun boşaltılması veya sürüm satırının kaldırılması öncesinde temizleme isteği journal'a yazılıyor. Yeni bekleme türü, worker isteği DB commit'inden önce alsa bile mevcut başvuru nedeniyle silmeyi tamamlamayıp yeniden dener. SaveChanges belirsiz sonuç verse de istek kaybolmaz; soft-delete/paylaşılan başvuru varsa dosya korunur. Release derlemesi 0 uyarı/0 hata; gerçek DB/runtime çalışması yapılmadı. Diğer silme iş akışları ile worker-saha kabulü ve TOCTOU halen açık.

### A-10 EBYS dosya güncellemesinde kalıcı eski-yol isteği — 2026-10-06

EBYS eski dosya yolu, yeni yol DB'ye kaydedilmeden önce referans kalkmasını bekleyen cleanup journal'a yazılır. Worker commit öncesi çalışsa dahi eski path başvurusu nedeniyle isteği tamamlamaz. Belirsiz DB sonucu mevcut yoldan doğrulanır; commit olmadıysa eski isteği kaldırıp yeni kopya telafi edilir. Release derlemesi 0 uyarı/0 hata; müşteri/runtime testi yapılmadı. Kalan dosya akışlarına uygulama ve TOCTOU kabulü açık.

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

- 🟢 Yeniden yüklemede geçmiş satırı artık yeni dosya yerine önceki yol/ad/tip/boyut bilgilerini tutuyor. Hızlı kaldırma DB’de aktif yolu gerçekten temizliyor ve eski dosyayı aynı DB işleminde geri alınabilir sürüm geçmişine bağlıyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; canlı personel verisiyle geri alma kabulü yapılmadı.
- 🔴 A-10 açık: müşteri DB/storage geçiş/restore kabulü, genel TOCTOU, büyük DB tarama maliyeti ve modüller arası kalan silme sözleşmeleri.

### 2026-10-07 — A-10 soft-delete dosyalarının geri alınabilir tutulması

- 🟢 Araç, taşıma tedarikçisi ve EBYS eklerini soft-delete ederken fiziksel şifreli dosya korunuyor. Geri alınabilir işaretli DB kaydıyla dosya yaşam döngüsü artık uyumlu; kalıcı purge ayrı bir işlem olmalı.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Gerçek müşteri DB’sinde restore kabulü yapılmadı.
- 🔴 A-10 açık: müşteri geçiş/restore kabulü, eşzamanlı referans-silme yarışı, büyük veri maliyeti ve kalan kalıcı silme akışları.

### 2026-10-07 — A-10 dosya yolu referans kontrolü ve indeksleme

- 🟢 12 dosya yolu kolonu için model indeksleri ve migration eklendi. Referans kontrolü tam dosya yolu listesini her silmede uygulama belleğine taşımıyor; mutlak storage yol aliasları ve Windows harf/ayraç normalizasyonu destekleniyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; test çalıştırılmadı.
- 🔴 A-10 açık: hedef hacimde migration ve indeks sorgu planı kabulü, Windows/çoklu sunucu eşzamanlı silme kabulü, müşteri verisiyle geçiş/restore ve firma bağı olmayan legacy destek ekleri.

### 2026-10-07 — A-10 personel dosya yolu değişikliğini amaca özel hale getirme

- 🟢 Genel personel evrak güncellemesi artık dosya yolunu değiştirmiyor. Kayıp dosya referansının temizliği ayrı servis metoduna taşındı; hızlı kaldırma yolu geri alınabilir sürümü koruyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**.
- 🔴 A-10 müşteri restore/geçiş ve hedef DB hacim performans kabulünü bekliyor.

### 2026-10-07 — A-10 restore ve çoklu worker izole kabulü

- 🟢 İki bağımsız cleanup journal örneğinin aynı işi eşzamanlı sahiplenememesi Release xUnit testinde doğrulandı.
- 🟢 Tam model SQLite üzerinde soft-delete dosyası cleanup turundan sonra korunuyor; satır geri açılınca referansı sürüyor. Release test paketi **32/32 başarılı**.
- 🟡 Gerçek müşteri DB/storage geçişi, hedef hacimde migration/ölçüm ve iki fiziksel sunucunun ortak ağ deposu kabulü yapılmadı; müşteri bağlantısı veya ikinci sunucu bu çalışma ortamında yok. Bu nedenle müşteri/sunucu kabulü yapılmış gibi işaretlenmedi ve üretim verisi değiştirilmedi.
### 2026-10-07 — A-10 kuyruk sahipliği ve geçiş güvenliği

- 🟢 Worker tamamlaması lease/revizyon kontrolüne bağlandı; eski işlem yeniden kuyruğa alınan veya bekleme türü değişen isteği kaldıramaz. Doğrudan silme, DB başvurusunun kalkmasını bekleyen isteği korur.
- 🟢 Legacy kaynak silinmeden önce yeni şifreli dosya tekrar çözülüp SHA-256 ile karşılaştırılır. Eksik/uyuşmayan hedefte eski dosya ve kuyruk korunur. Referans sorgusunda boşluk, ters ayraç ve Türkçe harf koruması tamamlandı; tam yol listesi belleğe alınmaz.
- 🟢 Tam model SQLite ile gerçek geçiş servisi, soft-delete geri alma ve tekrar çalıştırma kabulü geçti. 100.000 sentetik satırda yerel sorgu ölçümleri 2,7 / 28,0 / 37,5 ms; Release paketi **34/34 başarılı**.
- 🔴 Bu kanıt genel referans ekleme–silme yarışını, tüm firma sahipliği açıklarını veya müşteri/çoklu sunucu kabulünü kapatmaz. Güncel kapsam ve ölçüm sınırları [görev envanterinin son ekinde](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md) kayıtlıdır.
### 2026-10-07 — A-24/A-25 süreçler arası cache nesli

- 🟢 CacheService ortak depodaki nesil belirteciyle çalışıyor. Geçersizleştirme, başka servisin başlattığı eski factory sonucunun yeniden görünmesini engeller; süreç içi anahtar listesine bağımlılık kaldırıldı. Prefix temizliği tüm uygulama cache'ini geçersizleştirdiği için DB yükü artabilir.
- 🟢 İptal yutulmaz; okuma arızasında veri kaynağı kullanılır, invalidation arızası çağırana bildirilir. Dört yeni cache regresyonuyla Release paketi **38/38 başarılı**.
- 🔴 Gerçek Redis/çok süreçli yük, diğer araç yazımları ve backend kesintisinde kalıcı invalidation retry kapsamı açık. Docker daemon erişilemedi. Detaylar [görev envanterinde](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md); A-24 🔴 ve A-25 🟡.

### 2026-10-07 — A-24 güncel düzeltme: iş verisi cache okuması kapatıldı

- 🟢 `CRMFilo:` iş verisi anahtarları artık cache'den sunulmaz; araç listesi de doğrudan DB okur. Böylece başka yazım yolunun invalidation atlaması veya cache backend arızası sonrası bayat üretim listesi dönme yolu kapandı. Önceki A-24 kırmızı cache satırları bu tarihten önceki durumu anlatır.
- 🟡 Hedef hacim sorgu performansı ve çoklu sunucu çalışma zamanı kabulü açık olduğundan güncel A-24 durumu 🟡. Ayrıntı ve güncel sayım [görev envanterindedir](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md).

### 2026-10-07 — A-10 şifreli dosya karantinası

- 🟢 Referanssız şifreli içerik fiziksel silme yerine atomik ve geri alınabilir karantinaya taşınır; eski DB yolu yeniden kullanılırsa servis okuma/varlık/kopyalama işlemleri karantinadan sonuç verir. Tam dosya yedeği karantina ağacını içerir.
- 🔴 Bu kod düzeltmesi A-10 kapanışı değildir: legacy düz dosya yarışı, doğrudan fiziksel yol kullanan akışlar, karantina kapasite/purge politikası ve gerçek müşteri/çok sunucu kabulü açık. Güncel ayrıntı [görev envanterindedir](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md).

### 2026-10-07 — A-10 farklı kurulum yolunda karantina ve düz dosya sınırı

- 🟢 Karantina özgün depolama içi klasör yapısını korur; arşivin başka köke geri yüklenmesi eşlemeyi bozmaz ve DB satırı olmasa da operatör özgün yolu bulabilir. Şifresiz dosya bu karantinaya alınmaz; eski dosya geçişi gerektiren durum görünür hatayla kalır. Varsayılan master key kurtarma taraması karantinayı da kapsar. Disk sağlığı yapılandırılmış depolama sürücüsünü ölçer.
- 🟡 Farklı makinede gerçek restore denemesi yapılmadı; A-10 🔴 durumu ve saha kabulü değişmez.

### 2026-10-07 — A-10 karantina kurtarma arşivi izole kanıtı

- 🟢 Sentetik şifreli dosya karantinaya taşındı, gerçek kurtarma ZIP'i oluşturulup manifest/hash/key ring probu doğrulandı ve hazırlanmış arşivden farklı depolama köküne aktarılan dosya özgün DB yoluyla tekrar çözüldü. Tam Release test paketi **39/39 başarılı**.

## A-11 devamı — S3 hata sınıfları ve imza başlığı — 2026-10-07

- 🟢 SigV4 `Authorization` başlığı artık .NET HTTP istemcisinin sözdizimi denetimine takılmıyor. S3 indirme/varlık sorgusu yalnızca 404'ü eksik nesne sayıyor; 403/5xx hatayı ileterek dosya silme ve geri alma akışlarında yanlış “dosya yok” sonucunu engelliyor. Silme 404 için idempotent, diğer hatalarda başarısız. Sahte imzalı URL üretimi kaldırıldı.
- 🟢 Hedefli test **2/2**, tam Release test paketi **41/41** başarılı. Yerel sahte HTTP yanıtı gerçek S3/MinIO kabulü değildir. Çoklu dosya, disk izin/kilit, iptal ve UI bildirim kabulü açık; A-11 🟡 kalır.

## A-11 devamı — yerel depo okuma ve varlık sonucu — 2026-10-07

- 🟢 Yerel nesne deposu dosyayı `File.Exists` ön denetimi olmadan açıyor. Okuma ve varlık sorgusunda yalnız bulunmayan yol `null`/`false` sonucuna çevriliyor; diğer dosya sistemi hataları çağırana iletiliyor. İptal önceden denetleniyor. Desteklenmeyen imzalı URL isteği boş bağlantı üretmek yerine açık hata veriyor.
- 🟢 Yerel depo hedefli testleri **4/4**, tam Release paketi **42/42** başarılı. Gerçek izin/kilit arızası, çoklu dosya ve ekran kabulü yapılmadı; A-11 🟡 kalır.
- 🟢 Araç, tedarikçi ve özlük evrakı ekranlarında başarılı kaldırma, dosyanın geri alma için saklandığını açıkça bildiriyor. Web Release derlemesi **0 uyarı / 0 hata**; tarayıcıda kullanım kabulü açık.

## A-12 devamı — firma değişen Excel aktarımının sonucu — 2026-10-07

- 🟢 Servis yazımı sırasında firma/modal değişirse önceki aktarımın ekleme, güncelleme ve hata sayıları yeni modala taşınmadan uyarıyla bildiriliyor; kayıt varsa mevcut firma listesi yenileniyor. Hata durumunda kısmi kayıt olasılığı söyleniyor. İşlem sürerken ikinci aktarım modalı açılmıyor; tarayıcı dosya akışı kapanıyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Gerçek Excel/tarayıcı firma değişimi ve Dispose yarışı kabulü yapılmadı; A-12 🟡 kalır.

## A-16 devamı — iki sağlayıcıda salt okunur eski veri ön envanteri — 2026-10-07

- 🟢 DataSync `inventory` komutu on kritik eski veri sınıfını salt okunur ve tutarlı snapshot içinde raporlar. Sentetik SQLite ve ayrı PostgreSQL 17 kümesinde her biri **10/10 beklenen bulgu** verdi; eksik şema `Complete=false` işaretlendi. Kaynak SQLite DB'nin üzerine rapor yazımı reddedildi. DataSync Release derlemesi **0 uyarı / 0 hata**.
- 🟢 Bu salt okunur ön envanter, tasarım ve şema kontrolü için yararlıdır; ancak yalnızca eski veri riskini tarar, veri temizleme veya otomatik düzeltme yapmaz. A-16 genel görevi bu nedenle **kontrol ve durumu bildirme** düzeyinde kalır.
- 🔴 Gerçek müşteri verisi taranmadı, onarım/geri dönüş akışı yok, iş sahibi teyidi ve öncesi/sonrası rapor kabulü açık. A-16 🔴 ve toplam görev renkleri **2 yeşil / 20 sarı / 6 kırmızı / 3 beyaz**. Kapsam/kullanım [A-16 ön envanterinde](A-16-ESKI-VERI-ENVANTERI.md).
- 🔴 Gerçek müşteri verisi, ayrı fiziksel sunucu/SMB, legacy düz dosya yarışı, tenant/mali ilişki onarımı ve kontrol sonrası teyit çalışması kabul edilmedi; A-10 genel durumu değişmez.

## A-16 devamı — tenant foreign key şema keşfi — 2026-10-08

- 🟢 Salt okunur envanter, SQLite/PostgreSQL şemasından `FirmaId` bulunan kaynak ve hedef tablolar arasındaki `Id` foreign key'leri keşfedip kaynak kayıtları firma uyuşmazlığı için incelenecek adaylar olarak raporlayacak şekilde genişletildi. Sabit 10 kontrole eklenir; her kaynak tablo için gruplanmış sorgu çalıştırır.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**. Sentetik SQLite DB'de farklı `FirmaId` değerli tek kolonlu FK bağlı satır eklendi; `A16-TENANT-001` kontrolü **1** inceleme adayı buldu. PostgreSQL kümesi Windows'un loopback socket iznini vermemesi nedeniyle başlatılamadı.
- 🔴 Foreign key tanımsız ilişkiler, composite iş ilişkileri, finansal toplamlar, gerçek müşteri taraması ve kontrollü onarım/öncesi-sonrası tutarlılık kanıtı açık. A-16 🔴; toplam görev renkleri **2 yeşil / 20 sarı / 6 kırmızı / 3 beyaz**.

## A-16 devamı — SQLite sahipsiz foreign key taraması — 2026-10-08

- 🟢 SQLite ön envanterine her FK sahibi tablo için `pragma_foreign_key_check` eklendi. Sentetik DB'de tekli ve composite foreign key sahipsiz kayıtları ayrı ayrı **1'er** sayıldı; tenant firma uyuşmazlığı kontrolü de çalıştı.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**; test edilen SQLite raporu `Complete=true` döndü.
- 🔴 PostgreSQL sahipsiz FK taraması ve yeni tenant metadata sorgusunun saha/izole doğrulaması, FK'siz uygulama ilişkileri, finansal uzlaştırma, gerçek müşteri onarımı ve öncesi/sonrası kabulü açık. Yerel geçici PostgreSQL sunucusu Windows loopback soket izni yüzünden başlatılamadı; A-16 🔴 ve görev renk sayısı değişmedi.

## A-29 / A-30 / A-31 — mali kurallar ve ürün kapsamı — 2026-10-08

- 🟢 Kararlar [ürün kararları belgesine](A-29-31-URUN-KARARLARI.md) eklendi. Excel/CSV'de hem giriş hem çıkışın pozitif olması veya imzalı Tutar ile yön/tutar uyuşmazlığı satırı hatalı yapar; önceki davranışın sessizce Giriş seçme riski kapatıldı. Kilitli veya muhasebe fişi/ters fiş bağlantısı bulunan maaş snapshot'ı soft-delete edilemez.
- 🟢 A-30'da davranış değiştirmeyen P3 refactor bu satış sürümüne alınmadı. A-31 için çevrimdışı desteklenmediği; tek düğümde Local, ortak yapılandırmada S3 ve yedek/log sorumluluğu belirlendi.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; `git diff --check` belge ve kaynak değişikliklerinde temiz.
- 🟡 A-29 sarı kaldı: hassas yazımlarda circuit/rol değişiminin sunucu sınırında tekrar denetlenmesi ve kabul edilmesi bu değişiklik kapsamında kapanmadı. A-30 ve A-31 karar kapsamı yeşil. Güncel görev sayımı **4 yeşil / 21 sarı / 6 kırmızı / 0 beyaz**.

## A-28 devamı — desteklenmeyen DB sağlayıcısında güvenli duruş — 2026-10-08

- 🟢 `Program.cs`, SQL Server/MySQL ayarıyla başlangıç yardımcıları veya migration'lar çalışmadan açık hata verir. `DatabaseSettingsService` aynı sağlayıcılarda bağlantı testi, ayar kaydı ve geçişi reddeder.
- 🟢 Veritabanı ayar ekranı PostgreSQL/SQLite dışı seçenekleri yeni seçim için kapatır; mevcut eski SQL Server/MySQL ayarını bulursa kırmızı uyarı gösterir ve geçişten önce durumu açıklar.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; değişen dosyalar için `git diff --check` temiz.
- 🔴 A-28 kapanmadı: SQL Server/MySQL şema/migration desteği veya yetkili ürün kapsam kararı ile PostgreSQL/SQLite hedef temiz kurulum/güncelleme kabulü hâlâ yok. Renk sayımı **4 yeşil / 21 sarı / 6 kırmızı / 0 beyaz**.

## A-16 devamı — PostgreSQL foreign key ihlal taraması — 2026-10-08

- 🟢 DataSync `inventory`, PostgreSQL foreign key metadata'sını `information_schema.referential_constraints` ve referans unique key `key_column_usage` üzerinden okuyup tekli ve composite bağları tarıyor. Aynı metadata join hatası tenant FK keşfinde de düzeltildi.
- 🟢 Release derlemesi **0 uyarı / 0 hata**. Geçici PostgreSQL 17 kümesinde bir tekli ve bir composite sahipsiz kayıt `A16-PG-FK-*` kontrollerinde **1'er** bulgu olarak raporlandı. Kopya yalnız sentetikti ve salt okunur tarandı; sabit 10 iş kontrolünün tabloları bulunmadığından tüm rapor eksik şemalı (`Complete=false`, çıkış 3) kaldı.
- 🔴 Gerçek müşteri taraması/onarımı, FK'siz ilişkiler, composite FK firma uyuşmazlığı ve öncesi/sonrası kabulü açık. A-16 kırmızı; toplam renk **4 yeşil / 21 sarı / 6 kırmızı / 0 beyaz**.

## A-16 devamı — composite tenant FK denetimi — 2026-10-08

- 🟢 PostgreSQL ve SQLite şema keşfi, composite FK'lerin tüm sütunlarını constraint sırasıyla eşleştirip hedef satırın `FirmaId` değeriyle karşılaştırıyor. Firma uyuşmazlığı otomatik onarılmadan `A16-TENANT-*` inceleme adayı olarak raporlanıyor.
- 🟢 Geçici PostgreSQL 17 kümesinde synthetic composite FK tenant uyuşmazlığı **1** bulundu; önceki tekli/composite orphan FK kontrolleri de çalıştı. DataSync Release derlemesi **0 uyarı / 0 hata**.
- 🔴 Gerçek müşteri DB taraması ve yetkili kontrollü onarım/geri dönüş kabulü yapılmadı; A-16 kırmızı, toplam **4 yeşil / 21 sarı / 6 kırmızı / 0 beyaz**.

### 2026-10-08 — A-29 güncel kullanıcı yetkisi

- 🟢 Yetki listesi sorguları her çağrıda aktif kullanıcı ve rol/izinleri veritabanından yeniden okuyor; circuit'te eski Admin rolü veya eski izin listesi yetki kararı üretmiyor. Pasif ya da silinmiş kullanıcı/rol/izin reddediliyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**.
- 🟡 Merkezi yetki sorgusunu kullanmayan bütün hassas mali yazımların servis sınırında denetimi ve rol değişimi çalışma zamanı kabulü açık. A-29 sarı, sayım **4 yeşil / 21 sarı / 6 kırmızı / 0 beyaz**.
- 🟢 Mevcut Release regresyon paketi **42/42 başarılı** oldu. A-29 için oturum açıkken rol/hesap durumu değişimi senaryosu test paketinde bulunmadığından A-29 ve görev sayımı değişmedi.

### 2026-10-08 — Çözüm geneli Release kabul koşusu

- 🟢 `dotnet test MKFiloServis.slnx -c Release --no-restore` **başarılı (exit 0)**; regresyon paketi **42/42**. Web, DataSync, LisansDesktop, Shared ve MAUI Client derlendi; Android/Windows Client Release link/AOT adımları tamamlandı. PlaywrightSmoke Release derlemesi de **0 uyarı / 0 hata**.
- 🟡 Playwright UI kabul senaryoları test kullanıcı adı/parolası ortamda olmadığı için çalıştırılmadı. Müşteri veritabanı, IIS kurulumu, S3/SMTP/Luca gerçek entegrasyonları ve DB+dosya gerçek restore bu koşuda çalıştırılmadı.
- 🟡 Sonuçlar yalnızca yerel Release derlemesi ve mevcut 42 otomatik testi kanıtlar; müşteri/saha kabul görevlerinin renkleri değişmedi.

### 2026-10-08 — A-29 banka hareketi yazımlarında taze yetki

- 🟢 Yeni/düzenle/sil ve banka içe aktarmayı yansıtma olaylarında işlem öncesi güncel DB rolü ve izni doğrulanıyor; yetki kalktıysa kayıt servisine geçilmiyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**, otomatik regresyon paketi **42/42**.
- 🟡 Maaş, puantaj, fatura ve muhasebe yazımlarının tümünü kapsayan ortak servis sınırı ile rol değişimi tarayıcı kabulü açık; A-29 sarı.

### 2026-10-08 — A-29 maaş/avans/borç yazımlarında taze yetki

- 🟢 Maaş, ödeme, avans ve personel borcu yazımları kullanıcı olayı anında güncel veritabanı izniyle kapılanıyor; doğrulama hatası yazımı reddediyor. Maaş listesini açmak artık izinsiz yeni kilitli dönem snapshot'ı üretmiyor.
- 🟢 Web Release **0 uyarı / 0 hata**; Release test paketi **42/42**.
- 🟡 Puantaj/fatura/muhasebe servislerinin ortak sunucu sınırı ve açık oturum rol değiştirme kabul testi tamamlanmadı. A-29 sarı kalır.

### 2026-10-08 — A-29 diğer banka/personel ödeme ekranları

- 🟢 Banka hareketi, personele geri ödeme ve gelir-gider ekranlarındaki kullanıcı yazımları güncel DB yetkisini işlem anında tekrar kontrol ediyor. Muhasebe fişi istenmişse ek muhasebe yazma izni de aranıyor.
- 🟢 Web Release **0 uyarı / 0 hata**; regresyon testi **42/42**.
- 🟡 Puantaj/fatura ve kalan muhasebe servis çağrılarının tamamı ile gerçek tarayıcıda rol değişikliği denemesi açık. A-29 genel rengi sarıdır.


### 2026-10-08 — A-29 fatura servisinde güncel yetki denetimi

- 🟢 `CurrentPermissionGuard` servis sınırında oturum kimliğini HTTP/circuit üzerinden alır; etkin kullanıcı, güncel rol ve izinleri her çağrıda DB'den okur. Oturumsuz, pasif/silinmiş hesap veya silinmiş rol/izin ile yazım reddedilir; DB sorgu hatası yazımı durdurur. Oturumdaki Admin rol iddiası yetki kaynağı değildir.
- 🟢 `FaturaService` oluşturma, düzenleme, silme, Excel/XML/XML+PDF içe aktarma, PDF değiştirme, kalem güncelleme, stok kartlı kalem güncelleme, fatura eşleştirme, mahsup kapatma ve açık muhasebeleştirme girişlerinde güncel izin aranır. Genel fatura izni veya ilgili gelen/kesilen yön izni kabul edilir. Yön değiştirmede hem eski hem yeni yön, eşleştirme/mahsupta iki kayıt da denetlenir; kalemlerde izin DB'deki ana faturadan alınır.
- 🟢 XML+PDF içe aktarmada yeni faturanın PDF'i yazma izniyle eklenir; mevcut faturanın PDF'ini değiştiren dış servis çağrısı düzenleme izni ister. Açık muhasebeleştirme ayrıca `MuhasebeFisleriYaz` ister. `MuhasebeService.CreateFisAsync` doğrudan fiş oluşturmayı aynı güncel servis denetimiyle korur.
- 🟡 Türetilmiş ödeme toplamı (`UpdateOdenenTutarAsync`), otomatik/atomik muhasebe üretimi, hesap planı ve iç servislerin fiş düzenleme/silme çağrıları henüz bu ortak denetime taşınmadı. Bunların banka/masraf/hakediş iş yetkileriyle birlikte ele alınması gerekir; yalnız manuel muhasebe izni eklenerek bu iş akışları kapatılmış sayılmadı. Puantaj ve diğer mali servis/API girişleri de açık; A-29 sarı kalır.
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

### 2026-10-08 — Dashboard SQLite finans kolonu düzeltmesi

- Önceki belirti: dashboard finans yüklemesi `SQLite Error 1: no such column: b.IslemKimligi` veriyordu.
- 🟢 Kök neden düzeltildi: migration geçmişi bulunmayan SQLite kurulumlarında bootstrap'ın bekleyen her migration'ı DDL çalıştırmadan uygulanmış göstermesi engellendi. Yalnız eski şema watermark'ına kadar baseline yapılır; yeni migration'lar gerçekten çalışır.
- 🟢 Daha önce yanlış geçmişe yazılmış kurulumlara yönelik idempotent, veri koruyan açılış onarımı eklendi: `BankaKasaHareketleri.IslemKimligi`, `IslemOzeti` ve benzersiz indeks tamamlanır.
- 🟢 İzole SQLite regresyon testleri: 3 yeni test; mevcut satır korunması, iki kez onarım, yeni migration'ın baseline dışında kalması ve eski şemada dashboard son hareket sorgusunun onarım sonrası çalışması doğrulandı. A-15 için uygulama katmanındaki dört senaryoya ek olarak SQLite DB trigger migration'ı, eski çapraz-firma verisini ve doğrudan SQL üzerinden geçersiz eşleştirme/uç firma değişikliklerini reddeden altı testle doğrulandı. Release çözüm derlemesi **0 hata / 0 uyarı**, son paket **102/102 geçti, 0 atlandı**.
- 🟡 Gerçek müşteri SQLite dosyası değiştirilmedi; düzeltme dağıtılmalı ve müşteri dashboard açılışı kabul edilmelidir. Satış görev renkleri müşteri/sağlayıcı kabulü kanıtı olmadan değiştirilmedi. [Teknik kanıt](TEST-DOGRULAMA-2026-10-08.md).
- 🟡 A-15 PostgreSQL migration'ı izole PostgreSQL 17 sunucusunda çalıştırıldı; eski müşteri verisi, paralel bağlantı kabulü ve diğer tenant ilişkileri doğrulanmadı. A-15 🔴 kalır.

### 2026-10-08 — A-23 belge ve kurulum girdisi kapanışı

- 🟢 [Teslim kararı](A-23-TESLIM-KARARI-2026-10-08.md) güncel görev belgesini sabitledi; ilk denetim ve Rent-a-Car tarihsel içeriklerinin Git geçmişindeki konumu kaydedildi. Eksik ayrı yeniden analiz belgesinin içeriği uydurulmadı.
- 🟢 Üç yerel ayar dosyası mevcut makinede korunarak Git takibinden çıkarıldı. Web publish, kurulum betiği ve Inno girdileri bu dosyaları paket dışı tutuyor. Release publish çıktısında ayar/oturum dosyaları ve `.db` bulunmadı.
- 🟡 Inno EXE/temiz hedef kurulum A-21; eski sır rotasyonu A-06 kapsamındadır. A-23 🟢; güncel renk dağılımı **5 yeşil / 21 sarı / 5 kırmızı**. Satış kararı 🔴 kalır.

### 2026-10-08 — CI ve audit SQL teslim düzeltmesi

- 🟢 Git'te eksik olan gömülü PostgreSQL audit SQL dosyası eklendi. Bu dosyanın yokluğu GitHub Linux Tests/Docker ve Windows CodeQL derlemelerini engelliyordu. GitHub Tests Web ve test projesi derlemeleri sonrasında **102/102 test geçti**; Linux/Windows dosya adı harf duyarlılığı test verisinde ayrıldı.
- 🟢 NuGet audit Windows 2025 ve MAUI workload restore ile çözüm ve Rent-a-Car paket taramasını başarıyla tamamladı; bilinen açık raporlanmadı. [Çalışma bağlantıları ve sınırlar](A-07-CI-DOGRULAMA-2026-10-08.md).
- 🟢 Docker imajı oluşturulup GHCR'ye gönderildi; aynı digest için Trivy ve SARIF yüklemesi başarılı.
- 🟢 Windows tam çözüm derlemesi ve CodeQL C# analizi GitHub'da geçti.
- 🟡 Gerçek müşteri kabulü ayrıca izlenir. A-07 🟡, A-22 🟢; görev renkleri ve satış kararı değişmedi.

### 2026-10-08 — A-29 banka hesabı oluşturma güvenliği

- 🟢 Banka hesabı oluşturma, güncel izin kontrolü, seçili firma zorunluluğu ve Serializable ortak yazım/commit sınırına taşındı. Başka firma kimliğiyle yeni hesap oluşturma reddedilir; boş firma seçili firmaya bağlanır.
- 🟢 İki SQLite regresyonu geçti: yabancı firma girişinde kayıt yok, seçili firmaya oluşturma başarılı; veritabanından Admin rolü kaldırılınca önceki oturum hesabı oluşturamaz.
- 🟢 `5d1c4ed7` için [GitHub Tests koşusu](https://github.com/karamur/MKFiloServis-MultiDb/actions/runs/37835118724) başarılı: **104/104 test, 0 başarısız, 0 atlanan**. Yerel Release koşusu da 104/104 geçti.
- 🟡 Diğer otomatik mali yazımlar, puantaj ve gerçek müşteri rol değişimi kabulü açık. A-29 🟡 ve genel satış kararı 🔴 kalır.

### 2026-10-09 — Personel özel kesintisinin Maaş/Ödeme Yönetimi'ne bağlanması

- 🟢 `Sofor.OzelKesintiToplami`, personel kartındaki icra, BES, sendika, hayat sigortası, bireysel emeklilik ve diğer özel kesinti alanlarının ortak toplamını verir. Maaş/Ödeme Yönetimi listesindeki Kesinti değeri bu toplamı kullanır; alanlar için tekrar eden toplama kaldırıldı.
- 🟢 Bordro servisindeki kalem bazlı özel kesinti aktarımı ve SGK/vergi hesaplaması değiştirilmedi. Operasyonel puantaj koduna dokunulmadı.
- 🟢 Web projesi derlemesi başarılı: `dotnet build "C:\Users\muratk\Desktop\dyedek\calisma\Claude-Code\MKFiloServis-MultiDb\MKFiloServis.Web\MKFiloServis.Web.csproj" -nologo` — **0 hata / 0 uyarı**.
- 🟢 Hedefli otomatik regresyon testi `dotnet test "C:\Users\muratk\Desktop\dyedek\calisma\Claude-Code\MKFiloServis-MultiDb\MKFiloServis.Tests\MKFiloServis.Tests.csproj" --no-restore --filter "FullyQualifiedName~SoforOzelKesintiTests" -nologo` — **2/2 başarılı**; altı personel kartı kesinti kaleminin toplamı ve toplamın maaş hesaplamasında ödenecek tutardan düşülmesi doğrulandı.
- 🟡 Maaş ekranında gerçek personel verisiyle görsel/iş akışı kabulü yapılmadı. Bu nedenle otomatik kod testi geçse de müşteri kabulü ve satışa hazırlık kararı değişmedi.

### 2026-10-09 — A-29 puantaj finans snapshot firma ve işlem sınırı

- 🟢 Puantaj finans snapshot'ı artık seçili firmada oluşturuluyor; dönemin aktif/kilitli durumu kayıtla aynı Serializable transaction içinde doğrulanıyor. Aynı dönem tekrar çağrıldığında ikinci satır üretilmiyor; silinmiş geçmiş satırın benzersiz anahtarı da korunuyor.
- 🟢 Finansal kayıt okuma ve fatura üretim girişleri ile hakedişin cari eşleşmesi seçili firmaya sınırlandı. SQLite regresyonu yabancı dönemi reddetme, doğru firma atama ve tekrar çağrıyı doğruladı. Release Web derlemesi **0 hata / 0 uyarı**; otomatik paket **105/105 başarılı, 0 atlanan**.
- 🟡 Puantaj fatura/kalem/link ve hakediş fatura/snapshot işlemleri henüz tek commit sınırında değil; diğer mali servis izinleri ve gerçek müşteri kabulü de açık. A-29 🟡, genel satış kararı 🔴 ve görev renk dağılımı değişmedi.

### 2026-10-09 — A-28 sağlayıcı kapsamının PostgreSQL/SQLite olarak sabitlenmesi

- 🟢 Ürün kararı: bu sürümün desteklenen veritabanları PostgreSQL ve SQLite'tır; SQL Server/MySQL için migration, audit ve başlangıç zinciri geliştirilerek destek verilmeyecektir.
- 🟢 Veritabanı ayarları ekranından SQL Server/MySQL sağlayıcı ve hızlı seçimleri kaldırıldı. Desteklenen runtime sağlayıcı kontrolü PostgreSQL/SQLite ile sınırlandı. Ayar servisinin bağlantı testi ve uygulama/kaydetme yolları eski desteklenmeyen sağlayıcıları reddediyor; yeni ayar veya geçiş manifesti yazmıyor. Legacy enum/ayar okuma, eski yapılandırmayı tanıyarak uygulama başlangıcındaki açık ret mesajını korumak için tutuldu; eski ayarlar sessizce başka sağlayıcıya çevrilmez.
- 🟢 `dotnet build "C:\Users\muratk\Desktop\dyedek\calisma\Claude-Code\MKFiloServis-MultiDb\MKFiloServis.Web\MKFiloServis.Web.csproj" --no-restore -nologo` — **0 hata / 0 uyarı**.
- 🟢 `dotnet test "C:\Users\muratk\Desktop\dyedek\calisma\Claude-Code\MKFiloServis-MultiDb\MKFiloServis.Tests\MKFiloServis.Tests.csproj" --no-restore --filter "FullyQualifiedName~DatabaseProviderScopeTests" -nologo` — **6/6 başarılı**; desteklenen sağlayıcı listesi, eski SQL Server/MySQL ayarlarının test/uygulama reddi ve ayar dosyasına yazılmaması doğrulandı.
- 🟢 A-03/A-10/A-15/A-28 hedefli regresyon paketi: `dotnet test "C:\Users\muratk\Desktop\dyedek\calisma\Claude-Code\MKFiloServis-MultiDb\MKFiloServis.Tests\MKFiloServis.Tests.csproj" --no-restore --filter "FullyQualifiedName~BankMovementTenantMigrationTests|FullyQualifiedName~InvoicePaymentMatchFirmMigrationTests|FullyQualifiedName~FileCleanupJournalTests|FullyQualifiedName~SecureFileAtomicSaveTests|FullyQualifiedName~SecureFileReferenceCheckerSqliteTests|FullyQualifiedName~SecureFileOrphanScannerTests|FullyQualifiedName~DatabaseProviderScopeTests" -nologo` — **32/32 başarılı**.
- 🟢 A-03 kurtarma/aktarim betikleri `00-aktar-baslat.ps1`, `01-db-restore.ps1`, `02-dosya-aktar.ps1`, `03-full-transfer-recover.ps1`, `05-recovery-archive-apply.ps1` ve `06-recovery-archive-rollback.ps1` PowerShell parser kontrolünden geçti.
- 🟡 Bu yerel regresyon ve sözdizimi sonuçları gerçek PostgreSQL/SQLite temiz kurulum-yükseltme, IIS kurtarma, çoklu sunucu dosya yaşam döngüsü veya müşteri verisi kabulünün yerine geçmez. Kabul veritabanına erişim ve değişiklik yapılmadı; A-03, A-10, A-15 ve A-16 için görev renkleri değiştirilmedi.

### 2026-10-09 — A-15 banka hareketi yardımcı firma bağlantıları

- 🟢 A-15 incelemesinde `ApplicationDbContext` içindeki `SaveChanges` firma kontrollerinin `PersonelCebindenId`, `AracId`, `AracMasrafId`, `MahsupHareketId` ve `PersonelGeriOdemeHareketId` bağlantılarını doğruladığı; ancak veritabanı trigger katmanının bu bağlantıları kapsamadığı görüldü.
- 🟢 `20261009150000_GuardBankMovementAuxiliaryTenantLinks` migration'ı SQLite/PostgreSQL için eski çapraz-firma bağlarında preflight, yeni/güncellenen hareketlerde firma eşleşmesi ve referans verilen araç/masraf/şoförün firma değişikliğine karşı koruma ekliyor. Ayrıca ana `20261006200000_GuardBankMovementTenantLinks` migration'ındaki preflight yarışı kapatıldı: her iki PostgreSQL migration'ı da hareket/hesap/cari (yardımcı migration'da araç/masraf/şoför) tablolarını `SHARE ROW EXCLUSIVE` kilidiyle tarama başlamadan kilitliyor.
- 🟢 `BankMovementAuxiliaryTenantMigrationTests`, `BankMovementTenantMigrationTests`, `InvoicePaymentMatchFirmMigrationTests` ve `BankTransactionSqliteTests` Release paketi **68/68** geçti; ana migration için yeni test kilit → preflight → trigger sırasını ve kilitlenen tabloları doğruluyor. DataSync Release build'i **0 uyarı / 0 hata**; A16-05 Release CLI sentetik kabulinde 4/4 bozuk hesap bağı bulundu.
- 🟡 Gerçek PostgreSQL migration uygulaması bu oturumda yapılmadı; migration eşzamanlılık ve müşteri verisi kabulü bekliyor. A-15 kırmızı, görev renkleri/sayımı değişmedi.
- 🟡 PostgreSQL credential'ı görev sürecine aktarılmadığı için migration gerçek PostgreSQL'de denenmedi ve hiçbir DB'ye bağlanılıp değişiklik yapılmadı. Diğer tenant ilişkileri, tüm eski verinin taranması ve müşteri migration/eşzamanlılık kabulü açık kaldığından A-15 🔴; görev renkleri/sayımı değişmedi.

### 2026-10-09 — A-16 banka yardımcı firma bağlantısı envanteri

- 🟢 Salt okunur DataSync ön envanterine A16-11–A16-15 kontrolleri eklendi. Banka hareketi-personel, araç, araç masrafı, mahsup/geri ödeme ve fatura ödeme eşleştirmesi-banka hareketi firma bağları artık sabit sorgularla taranıyor; envanter toplamı 15 sabit kontrol oldu.
- 🟢 Genişletilmiş geçici SQLite taraması `Complete=true` döndü. A16-04, üç hatalı cari bağlantısı olan iki aracı tekilleştirerek saydı; A16-11–A16-15'in her biri beklenen tek kaydı buldu. Toplam **7** sentetik bulgu oluştu, diğer sabit kontroller temiz kaldı ve kaynak SHA-256 değişmedi.
- 🟢 A16-15 sınır incelemesinde banka hareketi `FirmaId` boşsa `<>` karşılaştırmasının uyumsuzluğu atlayabileceği saptandı. Sorgu NULL ve geçersiz firma değerlerini açıkça bulacak biçimde düzeltildi; minimal sentetik SQLite şemasında beklenen eşleştirme bulundu. Eksik diğer tablolar nedeniyle bu hedefli raporun `Complete=false` dönmesi bekleniyordu.
- 🟢 A16-04'te eski `INNER JOIN` ile yok olan cari kayıtları görünmez kalabiliyor ve iki sorunlu cari tek aracı çoğaltabiliyordu. Ayrı `LEFT JOIN` ve tekil araç kimliğiyle eksik/geçersiz kiralık-komisyoncu bağlantıları kapsandı.
- 🟢 DataSync Debug derlemesi **0 uyarı / 0 hata**; A-15 banka/ödeme migrasyonları ve A-28 sağlayıcı sınırını kapsayan odaklı regresyon paketi **73/73** geçti.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**. Release CLI ile genişletilmiş sentetik SQLite envanteri yeniden çalıştırıldı: `Complete=true`, 15 sabit kontrol, beklenen **7** bulgu ve değişmeyen kaynak SHA-256.
- 🔴 Gerçek müşteri verisi taranmadı veya değiştirilmedi. FK'siz/FirmaId'siz ilişkiler, yetkili bulgu incelemesi ve onarım/öncesi-sonrası kanıtı tamamlanmadığından A-16 kırmızı; görev renkleri/sayımı değişmedi.

### 2026-10-09 — A-03/A-10 yerel regresyon doğrulaması ve kapanış engelleri

- 🟢 A-03 kapsamında `00-aktar-baslat.ps1`, `01-db-restore.ps1`, `02-dosya-aktar.ps1`, `03-full-transfer-recover.ps1`, `05-recovery-archive-apply.ps1` ve `06-recovery-archive-rollback.ps1` PowerShell parser kontrolünden **6/6** geçti.
- 🟢 A-10 cleanup journal, tam model SQLite referans kontrolü, orphan tarayıcı ve atomik/karantina dosya testleri **11/11** geçti.
- 🟡 Bu yerel kontroller gerçek DB+belge restore'u, hata enjeksiyonu, IIS çalıştırma/yeniden başlatma, farklı makine key ring'i, PostgreSQL migration/eşzamanlılığı veya müşteri veri onarımını kanıtlamaz. Bu oturumda PostgreSQL için `PGPASSWORD`, `PGPASSFILE` veya `MKF_A16_PG_SOURCE` ortam değişkeni bulunmadı; hiçbir gerçek DB'ye bağlanılmadı.
- 🔴 A-03/A-10/A-15/A-16 yeşile kapatılmadı: izole kurtarma ve işletim kabulü, dosya referansı–silme ve retention politikası, tüm tenant ilişkilerinin/veri bulgularının kabulü ile gerçek eski veri tarama/onarım kanıtı hâlâ gerekir. Toplam **5 yeşil / 22 sarı / 4 kırmızı / 0 beyaz** değişmedi.
- 🟡 Kullanıcının test verisi olarak belirttiği `MKFiloServis.Web\App_Data\test.db` Release envanterinde 15 sabit kontrolden 14'ünü çalıştırdı; A16-11 için `Soforler` tablosu eksik olduğundan `Complete=false`, `FindingCount=0` verdi. Salt okunur kaynak dosyanın SHA-256 özeti değişmedi; bu sonuç temiz tam tarama sayılmaz.
- 🟡 Depoda uygulanabilir kurtarma ZIP arşivi bulunmadı. IIS'te 9 site/app pool var, fakat adında test/accept/stage olan ayrı hedef tespit edilmedi; hiçbir site durdurulmadı veya yeniden başlatılmadı. Eski izole PostgreSQL 17 kümesi `127.0.0.1:55437` üzerinde başlatılamadı (Windows loopback bind `Permission denied`); diğer PostgreSQL servislerine dokunulmadı.

### 2026-10-09 — A-16 banka hesap kimliği envanter sınırı

- 🟢 A16-05'in `BankaHesapId > 0` koşulu geçersiz referansları dışarıda bırakabiliyordu. Sorgu boş, sıfır/negatif, bulunmayan hesap kimliklerini ve boş/geçersiz firma bağlarını artık raporluyor.
- 🟢 DataSync Release build **0 hata / 0 uyarı**. Release CLI sentetik SQLite envanteri `Complete=true` ve çıkış kodu `0` verdi; boş, `0`, `-4`, `999` hesap kimlikli kayıtların dördünü buldu (`A16-05 Count=4`, `SampleIds=101,102,103,104`), geçerli hesaplı kaydı bulgu saymadı.
- 🔴 Gerçek müşteri verisi incelenmedi/onarılmadı. A-16/A-15/A-10/A-03 kapanış kanıtları tamamlanmadığından toplam **5 yeşil / 22 sarı / 4 kırmızı / 0 beyaz** değişmedi.

### 2026-10-09 — IIS kabul erişim durumu

- 🟡 Kullanıcı `localhost` bilgisini verdi; bu host adı tek başına IIS site veya application pool'u tanımlamıyor. Güncel VS Code oturumu yönetici/yükseltilmiş değil (`IsAdministrator=false`, `Elevated=false`); `appcmd list site` IIS `redirection.config` dosyasını yetersiz izin nedeniyle okuyamadı.
- 🟡 Bu oturumda hiçbir IIS site/pool durdurulmadı, başlatılmadı veya değiştirilmedi. Test IIS hedefi ve kurtarma kabulü hâlâ doğrulanmadığından A-03 🔴 kalır; görev renkleri/sayımı **5 yeşil / 22 sarı / 4 kırmızı / 0 beyaz** olarak değişmedi.

### 2026-10-09 — A-10 dosya saklama politikası kararı

- 🟢 Kullanıcı kararı: şifreli silme karantinası ve legacy düz dosyalar **süresiz tutulacak; otomatik purge yapılmayacak**. Bu karar, geri dönen/geç yazılan DB başvurularında içeriğin kaybolmasını önleyen mevcut koruyucu davranışla uyumludur.
- 🟢 Kod incelemesinde `SecureFileService` karantinaya taşıma dışında kalıcı silme yapmıyor; `LegacyFileCleanupService.ProcessAsync` referanssız eski dosyayı yerinde bırakıp cleanup isteğini tamamlıyor. Karantina `storage/uploads` ağacındadır ve RecoveryArchive kapsamına dahildir.
- 🟢 Depolama hacmi için Sistem Sağlığı ekranında %80 uyarı/%90 kritik eşikleri ve Admin ayrıntı API'si mevcut; 2026-10-09'da ekran renk/durum tutarsızlığı ile erişilemeyen disk hata sunumu düzeltildi.
- 🟡 Harici/operasyonel alarm ve müdahale prosedürü, uygulama içi doğrudan fiziksel dosya okuyucularının uçtan uca kabulü, PostgreSQL/çoklu sunucu ile gerçek müşteri geçiş/restore kanıtı açık. Bu nedenle A-10 🔴 kalır; görev toplamı **5 yeşil / 22 sarı / 4 kırmızı / 0 beyaz** değişmedi.

### 2026-10-09 — A-10 doğrudan legacy dosya okuyucusu kaynak taraması

- 🟡 Kaynak taramasında şifreli depoya geçiş/geri uyumluluk amacıyla kalan doğrudan legacy okuyucular belirlendi: EBYS sürüm okuma/arşivleme (`BelgeVersiyonService`), destek eki indirme (`DestekTalebiService`), fatura PDF/XML indirme (`FaturaService`) ve ortak eski yükleme kökü okuyucusu (`FileService`). Fatura/EBYS yollarında webroot sınırı uygulanıyor; destek eki mutlak yolları `wwwroot/uploads/destek` altında kanonikleştiriliyor; ortak okuyucu izinli uploads köküne sınırlandırılmış.
- 🟡 Bu okuyucular veritabanı yolunu fiziksel diskte çözerek eski düz dosyayı döndürebilir; legacy dosyaları süresiz tutma kararı nedeniyle bunları topluca kaldırmak veya dosya taşımak doğru kapanış ölçütü değildir. Yetki kapsamı, firma/sahiplik doğrulaması, erişilemeyen dosya hatalarının UI'da sunumu ve karantina/restore sonrası aynı okuyucu akışı uçtan uca kabul edilmelidir.
- 🔴 Kod kapsamı taraması bu hizmetlerin canlı depolama, müşteri verisi veya yetkilendirme davranışını kanıtlamaz. Otomatik purge yok; A-10 ve toplam **5 yeşil / 22 sarı / 4 kırmızı / 0 beyaz** değişmedi.

### 2026-10-09 — A-16 banka hareketi cari ve ödeme hesabı kontrolleri eklendi

- 🟢 A-15 banka hareketi tenant guard'ı ile A-16 sabit sorguları karşılaştırıldı. A-16'nın atladığı `CariId` ve `PersonelOdemeHesapId` bağları için A16-16/A16-17 eklendi; boş/negatif kimlik, bulunmayan hedef, geçersiz firma ve firma uyuşmazlığı taranıyor. Sabit kontrol sayısı 15'ten 17'ye çıktı.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata** ile geçti.
- 🟢 Sentetik SQLite CLI kabulinde `Complete=true`, `ReadOnly=true`, toplam **6 bulgu** döndü; A16-16/A16-17'nin her biri beklenen **3** çapraz-firma/boş-geçersiz/sahipsiz hedef örneğini buldu. Kaynak DB SHA-256 değişmedi.
- 🟡 PostgreSQL 17 izole kümesi `127.0.0.1` soket izni (`Permission denied`) nedeniyle başlayamadı; PostgreSQL kabulü çalıştırılmadı. Önceki 15 kontrolün fixture sonuçları yeni sorgular için kanıt sayılmaz. A-16 görev durumu 🔴 ve toplam görev renkleri değişmedi.
- 🔴 Gerçek müşteri taraması, bulunan kayıtların yetkili değerlendirmesi ve kontrollü onarım/öncesi-sonrası raporu yapılmadı. Detay [A-16 envanter belgesinde](A-16-ESKI-VERI-ENVANTERI.md).

### 2026-10-09 — yerel test DB'de A-16 ön taraması

- 🟡 `MKFiloServis.Web\App_Data\test.db` Release CLI ile salt okunur tarandı; rapor kaynak DB dışında tutuldu. Toplam 248 kontrolün 247'si temiz, biri (`A16-11`) `Soforler` tablosu eksikliği nedeniyle `schema_missing`; `Complete=false`, `FindingCount=0`, çıkış kodu 3. Bu temiz tam tarama değildir. A16-16/A16-17 mevcut şema için çalışıp bulgu vermedi.
- 🟢 Kaynak SHA-256 değişmedi. Test DB'si müşteri verisi değildir; müşteri taraması/onarımı yapılmadı. A-16 🔴 ve genel renk dağılımı değişmedi.

### 2026-10-09 — A16-16/A16-17 SQLite sınır durumları

- 🟢 Ayrı sentetik tam şema SQLite fixture'ında çapraz-firma, sıfır/negatif kimlik, bulunmayan hedef ve geçersiz hareket firması senaryoları her yeni sorguda **5/5** beklenen kaydı buldu; geçerli NULL opsiyonel bağlar bulgu vermedi. CLI `Complete=true`, `ReadOnly=true`, toplam 12 bulgu, çıkış kodu 0; kaynak SHA-256 değişmedi.
- 🟡 Bu, SQLite sentetik kanıtıdır. PostgreSQL soket izni kabulini ve gerçek müşteri verisini kapsamaz; A-16 kırmızı kalır.

### 2026-10-09 — A16 sabit kontrollerinde sıfır FirmaId düzeltmesi

- 🟢 A16-06 ve A16-11–A16-14 sorguları iki uçtaki `FirmaId=0` değerinin yanlışlıkla eşit kabul edilmesini önleyecek şekilde düzeltildi; boş/sıfır/negatif firma kimlikleri artık bulgu.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**; sentetik SQLite'ta `Complete=true`, `ReadOnly=true` ve beş kontrolün her biri için beklenen **1** bulgu; kaynak SHA-256 aynı kaldı.
- 🟡 PostgreSQL/müşteri verisi doğrulanmadı. A-16 🔴 ve genel görev renk dağılımı değişmedi.

### 2026-10-09 — A16-18 maaş snapshot personel/firma bağı

- 🟢 A-16'nın şemadan keşfedilen FK taramasında FK'siz kalabilen `MaasOdemeSnapshotlar.PersonelId` ilişkisi için A16-18 eklendi. Aktif snapshot'ın personel kaydını ve `FirmaId` uyumunu denetliyor.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**. Sentetik SQLite CLI `Complete=true`, `ReadOnly=true`; A16-18 beklenen **3** hatalı örneği buldu, geçerli ve soft-delete örneklerini dışarıda tuttu; kaynak SHA-256 aynı.
- 🟡 Sabit kontrol sayısı 18 oldu. PostgreSQL ve müşteri verisi kabulü yapılmadı; A-16 🔴 ve görev renk dağılımı değişmedi.

### 2026-10-09 — A-16 kapsam kararıyla kapatıldı

- 🟢 Kullanıcı kararıyla A-16'nın kapanış kapsamı salt okunur eski veri/tenant raporlama aracının teslimi olarak sabitlendi. DataSync 18 sabit kontrol ve şemadan keşfedilen FK/tenant bulgularını raporlar; otomatik onarım yapmaz. 18 sabit sorgu sentetik SQLite'ta derlenip çalıştırıldı; A16-16–A16-18 sınırları doğrulandı. Temel 10 kontrol ve dinamik FK keşfi için önceki izole PostgreSQL/SQLite kanıtı kayıtlıdır.
- 🟡 Sonradan eklenen A16-11–A16-18 sabit kontrolleri PostgreSQL'de ayrıca çalıştırılmadı; yerel PostgreSQL sunucusu Windows loopback soket izni nedeniyle başlatılamadı. Bu sınırlama belgede açık tutulur.
- 🟡 Gerçek müşteri verisi taranmadı veya onarılmadı; müşteri geçişinde raporun kontrollü kopyada çalıştırılması ve bulguların yetkili incelemesi operasyonel adımdır. A-16 kod teslimini açık tutmaz ve müşteri verisinin temiz olduğunu ifade etmez.
- 🟢 Güncel görev dağılımı **6 yeşil / 22 sarı / 3 kırmızı / 0 beyaz**; satışa çıkarım kararı A-03/A-10/A-15 ve kabul bekleyen işler nedeniyle 🔴 kalır.

### 2026-10-09 — A-15 fatura-cari firma bağı

- 🟢 Kaynak incelemesinde A-16 ön envanterinin fatura-cari uyuşmazlığını saptadığı, ancak yeni yazımları ve cari firma değişikliklerini engelleyen DB korumasının olmadığı görüldü. PostgreSQL ve SQLite için preflight ile fatura insert/update ve cari firma update tetikleyicileri eklendi; eski aktif uyumsuz fatura varsa migration açık hata vererek durur.
- 🟢 Yeni SQLite regresyonları **5/5** geçti: geçerli aynı-firma insert/update; eski çapraz-firma verinin preflight'ta reddi; fatura ve cari uçlarında uyumsuz firma değişikliğinin reddi.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**.
- 🟡 PostgreSQL migration çalışma zamanı kabulü ayrıca yapılmalıdır.
- 🟢 Kullanıcının kapsam kararıyla A-15, tanımlı tenant korumaları ve yerel regresyon teslimi kapsamında kapatıldı. PostgreSQL/müşteri migration'ı ile müşteri veri tarama/onarımı dağıtım operasyonunda ele alınacak; yapılmış sayılmaz ve A-15 görev rengini açık tutmaz. Güncel dağılım **7 yeşil / 22 sarı / 2 kırmızı / 0 beyaz**.

### 2026-10-09 — A-03 ve A-10 kapsam kararıyla kapatıldı

- 🟢 Kullanıcı kararıyla A-03 kapsamı kurtarma/geri dönüş kod teslimi; A-10 kapsamı şifreli dosya yaşam döngüsü ve geri alınabilir cleanup kod teslimi olarak sabitlendi. Mevcut kanıtlar: A-03 altı PowerShell betiği parser kontrolü; A-10 odaklı Release regresyonları **11/11**, RecoveryArchive ZIP ve farklı kökte geri okuma.
- 🟡 Gerçek müşteri restore'u, IIS işletimi, canlı PostgreSQL/çoklu depo, legacy okuyucular ve kapasite alarmı saha/operasyon kabulüdür; yapılmış sayılmaz ve görev renklerini açık tutmaz.
- 🟢 Güncel dağılım **9 yeşil / 22 sarı / 0 kırmızı / 0 beyaz**. Genel satış kabulü sarı görevlerin müşteri/çalışma zamanı koşulları nedeniyle henüz verilmemiştir.

### 2026-10-09 — Önceki toplu kapanışın yeniden analizi

- 🟡 Önceki toplu kapanış kararı geçersiz kılındı; görevler kod teslimi, uygulama açığı ve kabul kanıtına göre yeniden sınıflandırıldı.
- 🟡 Müşteri/üretim lisansı ve sır geçişi, bağımsız restore, rol/tenant ve API kabulü, gerçek DB/migration, dosya deposu/çoklu sunucu, kurulum, entegrasyon ve görsel çıktı kabulleri yapılmış sayılmaz. Bunlar açık A-görevi değil, dağıtım ve işletim operasyonudur; burada gerçekleşmiş kabul gibi sunulmaz.
- 🔴 Güncel görev dağılımı **10 yeşil / 20 sarı / 1 kırmızı / 0 beyaz**; A-13 güvenli kapsamla kapatıldı; A-29 kabul bekliyor. Satış/üretim yayına çıkış onayı verilmemiştir.

### 2026-10-09 — A-13/A-29 kod düzeltmeleri tamamlandı

- 🟢 A-13 araç transferi tek Serializable transaction içindedir. EF modelindeki doğrudan AracId tablolarını tarar; desteklenen bağlantıları birlikte taşır, eşlenmeyen ilişki varsa isimlendirerek işlem öncesi reddeder.
- 🟢 A-29 puantaj fatura/kalem/finans linki, hakediş fatura/durum/snapshot ve ödeme eşleştirme/türetilmiş toplam zincirleri aynı Serializable işlem içinde yazılır. Fatura ve muhasebe/ödeme eşleştirme yazımları güncel izin ister.
- 🟢 Web Release build: 0 uyarı / 0 hata. Test çalıştırılmadı.
- 🟡 Müşteri rol değişimi ve PostgreSQL/SQLite çalışma zamanı kabulü yapılmış sayılmaz. Güncel dağılım: 11 yeşil / 20 sarı / 0 kırmızı / 0 beyaz.



### 2026-10-09 — A-09/A-29 otomatik fatura muhasebe zinciri düzeltmesi

- 🟢 Puantaj fatura kalemi ile otomatik muhasebe fişi, faturayla aynı context/Serializable transaction içinde kaydedilir. Fiş oluşturma hatası transaction sırasında yutulmaz; fatura, kalem, fiş ve finans bağlantısı birlikte geri alınır.
- 🟢 Web Release build **0 uyarı / 0 hata**. Bu turda test çalıştırılmadı. A-09 genel fatura zinciri ve çalışma zamanı/sağlayıcı kabulü nedeniyle sarı kalır; A-29'un kod kapsamı yeşildir. Görev dağılımı **11 yeşil / 20 sarı / 0 kırmızı / 0 beyaz**.


### 2026-10-09 — A-19 DataSync ve A-27 HTTP retry düzeltmeleri

- 🟢 A-19 PostgreSQL→SQLite aktarımında bağlantı başındaki SQLite foreign key ve synchronous ayarları başarı/hata/rollback sonrasında finally ile geri yüklenir.
- 🟢 A-27 retry handler her isteğin klonunu dispose eder, başarılı response'u özgün isteğe bağlar ve HTTP VersionPolicy'yi kopyalar; idempotent olmayan belirsiz istekler tekrar gönderilmez.
- 🟢 Web ve DataSync Release derlemeleri **0 uyarı / 0 hata**. Bu turda test çalıştırılmadı; A-19/A-27 gerçek sağlayıcı/dış servis kabulleri sarı kalır.

### 2026-10-09 — A-01 lisans doğrulama hata sızıntısı ve demo kilidi

- 🟢 Kod incelemesinde lisans doğrulama exception mesajının giriş/erişim sonucuna ham biçimde verildiği ve Registry erişilemezken demo kilidinin izin verdiği bulundu. Kullanıcıya genel doğrulama hatası döndürülür; makine kimlikleri lisans uyumsuzluğu mesajından çıkarıldı. Ayrıntılar yalnız sunucu loguna gider.
- 🟢 Demo kilidi Windows dışı platformda veya Registry denetimi hata verdiğinde fail-closed davranır; başarısız denetim yeni demo hakkı vermez.
- 🟢 Web Release build **0 uyarı / 0 hata**. Test çalıştırılmadı.
- 🟡 Yetkili müşteri/lisans geçişi, Windows demo akışı ve bağımsız profil kabulü yapılmadı. A-01 sarı kalır.

### 2026-10-09 — A-06/A-28 veritabanı ayarı hata ayrıntısı

- 🟢 Veritabanı bağlantı denemesi ve ayar uygulama exception ayrıntıları sunucu loguna yazılır; istemciye connection string/host/path içerebilecek ham exception yerine genel, uygulanabilir mesaj döner.
- 🟢 Web Release build **0 uyarı / 0 hata**. Test çalıştırılmadı.
- 🟡 Aktif Production sırlarının rotasyonu ve gerçek sağlayıcı ayarı kabulü açık; A-06 ve A-28 sarı kalır.

### 2026-10-09 — A-05 JWT rol ve hesap durumu iptali

- 🟢 Kaynak incelemesinde token rol/aktiflik bilgilerinin kullanıcı veya rol değişiminden sonra JWT ömrü boyunca kabul edildiği görüldü. Her JWT doğrulamasında kullanıcı DB'den yüklenip silinmemiş/aktif olması, geçerli kilit taşımaması ve güncel rolü kontrol edilir; koşullar bozulmuş veya durum sorgulanamıyorsa API isteği reddedilir.
- 🟢 API girişinde başarısız parola, bulunamayan/devre dışı hesap ve kullanılamayan hesap aynı genel yanıtı verir; kullanıcı varlığı/aktifliği yanıt metninden ayırt edilemez.
- 🟢 JWT yenileme uç noktası da silinmiş/devre dışı veya halen kilitli hesaplara yeni token vermez; istemciye genel oturum geçersiz mesajı döner.
- 🟢 Web Release build **0 uyarı / 0 hata**. Test çalıştırılmadı.
- 🟡 Her API token kontrolü ek DB okuması yapar; PostgreSQL yük/perf, normal/Admin/rol iptali ve tenant oturum kabulü çalıştırılmadı. A-05 sarı kalır.

### 2026-10-09 — A-17/A-09 banka dosyası tekrar yansıtma koruması

- 🟢 Kaynak incelemesinde banka dosyası yansıtımının hareketi oluşturduktan sonra yanıt/işlem belirsiz kalırsa aynı stage satırının yeni hareket üretebildiği görüldü. Staged satır GUID'si tenant kapsamlı işlem anahtarı ve içerik özeti olarak aynı Serializable banka yazımında kaydedilir.
- 🟢 Aynı kimlik ve aynı içerik tekrarı mevcut hareketi döndürür; aynı anahtar farklı içerikle veya soft-delete edilmiş önceki işlemle kullanılırsa yeni kayıt reddedilir. Web Release build **0 uyarı / 0 hata**.
- 🟡 Test çalıştırılmadı; gerçek PostgreSQL/SQLite commit belirsizliği, CSV/XLSX import ve eşzamanlı tekrar kabulü açık. A-17/A-09 sarı kalır.

### 2026-10-09 — A-20 araç plaka tarihlerinde gün semantiği

- 🟢 Kaynak incelemesinde araç plaka uygunluk sorgularının UTC gününü ve yerel günü karışık kullandığı; plaka giriş/çıkış kayıtlarının bazı yollarda saat içeren `UtcNow` timestamp'i yazdığı bulundu. Plaka giriş/çıkış yeni yazımları gün başına normalize edildi; plaka aktiflik sorguları ve modeldeki mevcut `DateTime.Today` kuralıyla eşleştirildi. Gelen plaka tarihleri `.Date` yapılır.
- 🟢 Web Release build **0 uyarı / 0 hata**. Test çalıştırılmadı.
- 🟡 Geçmiş timestamp'ler dönüştürülmedi. Üretim host timezone'u, UTC/yerel alan envanteri ve migration/geri dönüş kararı hâlâ açık; A-20 sarı kalır.

### 2026-10-09 — A-04 PostgreSQL restore argüman güvenliği

- 🟢 PostgreSQL `pg_restore` çağrısı tek `Arguments` metni yerine `ProcessStartInfo.ArgumentList` kullanır; host, port, kullanıcı, veritabanı ve backup dosya yolu ayrı argüman olarak işletim sistemine aktarılır.
- 🟡 Gerçek bağımsız makine restore'u ve rollback kabulü yapılmadı; A-04 sarı kalır. Test çalıştırılmadı.

### 2026-10-09 — A-09 sıradan fatura oluşturma atomikliği

- 🟢 A-09 kök neden çözümü tamamlandı: `FaturaService.CreateAsync(Fatura)` artık execution strategy altında `Serializable` transaction açar; fatura, kalemler, firmalar arası karşı kayıt ve otomatik muhasebe fişi aynı işlemde tamamlanır. Fiş oluşturma hatası transaction içinde yutulmaz.
- 🟢 Bilinen rollback sonrası retry için üretilen fatura/kalem kimlikleri ve detached navigasyonlar temizlenir; `ResetNewInvoiceGraphForRetry` ile yeniden denemede eski generated ids/kayıt grafiği sıfırlanır. Commit başladıktan sonraki belirsiz sonuçta ikinci kez otomatik kayıt yapılmaz; fatura numarasıyla kontrol edilmesi istenir.
- 🟢 Regression güvence eklendi: `Retry_reset_clears_generated_ids_and_detached_navigation_graph` testi, generated id'lerin ve detached graph reset davranışının çalıştığını doğruluyor.
- 🟢 Web Release build doğrulandı: **0 uyarı / 0 hata**.
- 🟢 Hedefli doğrulama çalıştırıldı: `dotnet test "C:/Users/muratk/Desktop/dyedek/calisma/Claude-Code/MKFiloServis-MultiDb/MKFiloServis.Tests/MKFiloServis.Tests.csproj" --filter "Invoice" --nologo` → **18/18 test geçti**.
- 🟡 Kalan açık kabul alanı, müşteri hedef DB üzerinde gerçek PostgreSQL/SQLite retry, fiş hata enjeksiyonu, commit belirsizliği ve iş akışı regresyonu doğrulamasıdır. Kod seviyesi kök çözüm ve kanıtlanmış invoice-focused test başarıları tamamlanmıştır; canlı üretim/onay kabulü için hedef DB teyidi gereklidir.

### 2026-10-09 — A-11 eski dosya yollarında kök dizin kaçışı

- 🟢 Kod taramasında `StartsWith(root)` kullanan legacy fatura, EBYS sürüm, destek eki ve arşiv yollarının kardeş klasörü kökün altı sanabildiği bulundu. Ortak yol yardımcısı normalize göreli yolu bileşen bazında sınar ve sembolik bağlantı geçişini reddeder; okuyucular bu kontrole geçirildi.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Bu turda test çalıştırılmadı.
- 🟡 Gerçek S3/MinIO imza/izin, disk kilidi/izin, çoklu dosya ve UI kabulü açık. A-11 sarı kalır; dağılım **11 yeşil / 20 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-18 fatura indeksi başarısızlığında startup davranışı

- 🟢 Başlangıç fatura benzersiz indeksi hazırlığı hatayı yakalayıp devam ediyordu ve eski indeksi yeni koruma kurulmadan kaldırabiliyordu. PostgreSQL/SQLite artık yeni unique indeksi transaction içinde önce kurup eski indeksi sonra kaldırıyor; hata zorunlu startup akışına taşınıyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Test veya gerçek sağlayıcı başlangıç provası çalıştırılmadı.
- 🟡 Eski müşteri verisinde yinelenen fatura numarası çözümü ve temiz/eski kurulum kabulü açık; A-18 sarı kalır. Dağılım **11 yeşil / 20 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-17 analitik API üst kayıt sınırları

- 🟢 Fatura/cari/araç analitik OData uç noktalarında `top` parametresine azami sınır uygulandı; istemci çok büyük bir değerle sınırsız kayıt çekemez. Varsayılan ve üst sınırlar sırasıyla 10.000, 5.000 ve 2.000.
- 🟡 Dış entegrasyon ve gerçek firma/rol kabulü çalıştırılmadı; A-17 sarı kalır. Test çalıştırılmadı.

### 2026-10-09 — A-02/A-17 cari API eylem izinleri

- 🟢 Cari REST create/update/delete uçları yalnız modül lisansı ve Bearer doğrulamasına bağlıydı. Şimdi güncel DB rolünden `cariler.yaz`, `cariler.duzenle` veya `cariler.sil` aranıyor; yoksa 403 ile reddediliyor.
- 🟡 Gerçek normal/Admin kullanıcı ve rol değişikliği kabul testi yapılmadı; A-02/A-17 sarı kalır.

### 2026-10-09 — A-01–A-31 renklerinin yeniden denetimi

- 🟢 Envanterdeki 31 görev, güncel kaynak notları ve kayıtlı doğrulama kanıtlarıyla yeniden karşılaştırıldı. Mevcut kapsam kararlarına göre renk sayımı **11 yeşil / 20 sarı / 0 kırmızı / 0 beyaz** olarak korundu; yeni bir sarı görevin tamamı kapanış eşiğini karşılamıyor.
- 🟡 A-02/A-17/A-18 son düzeltmeleri ilgili alt bulguları kapatır; diğer API rol matrisi, dış servis ve sağlayıcı/kurulum kabulü açık kalır. A-09'un sıradan fatura oluşturma kodu atomiktir; hedef DB ve commit-belirsizliği kabulü tamamlanmadı.

### 2026-10-09 — A-02/A-17 mali REST eylem izinleri
- 🟢 `GuzergahlarController` ve `PuantajIstisnaController` okuma/yazma/düzenleme/silme uçlarına güncel veritabanı rol izni denetimi eklendi. Güzergâh Excel import'u mevcut Admin rol şartına ek olarak yazma izni de ister. Önceki cari, fatura, araç ve şoför REST izin düzeltmeleriyle birlikte bu uçlar modül lisans kontrolüne ek eylem kontrolü uygular.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; test çalıştırılmadı.
- 🟡 Normal/Admin kullanıcı ve rol değişiminin uçtan uca kabulü, kalan API/uçların tam rol matrisi ve firma kapsamı kontrolleri açık. A-02/A-17 sarı; görev sayımı **11 yeşil / 20 sarı / 0 kırmızı / 0 beyaz**.
- 🟡 Ek taramada `DosyaController` indirme/önizleme yollarının istekten verilen depolama yolu üzerinden EBYS lisanslı her oturuma açık olduğu görüldü. Repo içinde bu REST uçlarını çağıran istemci bulunmadığından, indir/önizle uçları tahmin edilebilir dosya yolu ile içerik ifşasını önlemek üzere Admin rolüne sınırlandı; dosya görüntüleme işlemi uygulamadaki yetkili modül akışlarıyla devam eder.

### 2026-10-09 — Tam yerel test paketi ve A-28 hata mesajı
- 🟢 Test paketinin ilk derlemesinde iki eski fixture servis kurucusu değişikliğine göre güncel değildi; fixture'lara logger ve eksik bağımlılık eklendi.
- 🟢 Test, SQL Server/MySQL ayar testi sırasında kullanıcıya özel migration kapsamı yerine genel bağlantı hatası döndüğünü yakaladı. `DatabaseSettingsService` şimdi bağlantı denemesi ve ayarı uygulama sonucunda desteklenmeyen sağlayıcı mesajını açık döndürür; ayar dosyasına yazılmaz.
- 🟢 `dotnet test MKFiloServis.Tests/MKFiloServis.Tests.csproj --no-restore -nologo`: **134/134 başarılı, 0 başarısız, 0 atlanan**.
- 🟡 PostgreSQL/SQLite hedef makine temiz kurulum/yükseltme kabulü yapılmadı; A-28 ve toplam renkler **11 yeşil / 20 sarı / 0 kırmızı / 0 beyaz** kaldı.

### 2026-10-09 — A-07/A-18/A-28 test ve teslim güncellemesi
- 🟢 A-18 fatura başlangıç indeksi için iki SQLite regresyonu eklendi: başarılı geçiş ve duplicate eski veri halinde rollback/eski indeksi koruma. Hedef PostgreSQL ve tam başlangıç kabulü açık kalır.
- 🟢 Tam yerel test paketi **136/136** geçti; Web ve test projesi Release derlemesi **0 uyarı / 0 hata**. Test fixture'ları güncel servis kurucularına uyarlandı ve A-28'in desteklenmeyen sağlayıcı hata mesajı düzeltildi.
- 🟢 A-07'nin kalıcı test/CI teslimi (Linux CI 102/102, Docker/GHCR/Trivy, Windows CodeQL ve yerel 136/136) tamamlandı. Gerçek müşteri/DB kabul senaryoları ilgili görevlerde takip edilir.
- 🟢 A-28'in sağlayıcı kapsam/UI teslimi tamamlandı; temiz hedef kurulum/yükseltme kanıtı A-18/A-21'e taşındı.
- 🟡 Güncel görev renkleri **13 yeşil / 18 sarı / 0 kırmızı / 0 beyaz**. Satış kararı hâlâ müşteri/üretim kabul koşullarına bağlıdır.
- 🟢 A-29 envanter satırındaki eski “ayrı best-effort muhasebe çağrısı” ifadesi düzeltildi. Güncel kod ve görev satırları [görev envanterinde](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md) eşitlendi.

### 2026-10-09 — A-19 DataSync iki sağlayıcı doğrulaması ve kapanışı

- 🟢 İzole PostgreSQL 17.5 kümesi ve sentetik SQLite DB ile DataSync CLI iki yönde çalıştırıldı: PostgreSQL→SQLite (2 tablo, 4 satır) ve SQLite→PostgreSQL (ebeveyn/çocuk tablolarında 3'er satır). Hedefteki eski satırlar değiştirildi; FK kontrolü geçti ve sequence bir sonraki değere doğru ilerledi.
- 🟢 Eksik hedef şema/kolon iki yönde yazım başlamadan reddedildi; mevcut hedef verisinin korunduğu doğrulandı. PostgreSQL COPY sırasında check constraint ihlali tüm transaction'ı geri aldı ve önceki hedef satırlar kaldı.
- 🟢 DataSync Release derlemesi **0 uyarı / 0 hata**. Yalnız sentetik veri kullanıldı; geçici PG kümesi durduruldu. A-19 kaynak/izole teslim kapsamı kapandı; müşteri verisi, büyük hacim ve canlı geçiş kabulü satış öncesi dış kapılardır.
- 🟢 Güncel renk dağılımı **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**. A-19 sarı listesinden çıkarıldı; kalan müşteri ve işletim kabulleri görev bazında açık tutuldu.

### 2026-10-09 — A-18 PostgreSQL indeks doğrulaması ve temiz başlangıç engeli

- 🟢 PostgreSQL 17.5 izole kümesinde üretim `DbInitializer.EnsureFaturaFirmaYonUniqueIndexAsync` rutini doğrudan çalıştırıldı. Normal geçiş eski `IX_Faturalar_FaturaNo` indeksini yeni firma/yön/numara unique indeksiyle değiştirdi; duplicate eski satır senaryosu hata verip transaction'ı geri aldı ve eski indeksi korudu.
- 🟡 Temiz/boş PostgreSQL `DbInitializer.InitializeAsync` tamamlanmadı. Eski migration skip yolu boş DB'de henüz oluşmamış `__EFMigrationsHistory` tablosuna yazıyor. Bu korumayı geçici kaldırma denemesinde sıradaki eski migration `AylikOdemeGerceklesenler` tablosunun varlığını varsayarak durdu. Bu nedenle migration zinciri/kapsamı netleştirilmeden A-18 kapanamaz.
- 🟡 Test edilen indeks yolu başarılı olsa da tam PostgreSQL temiz kurulum/yükseltme ve müşteri duplicate-veri kararı açık kaldı. Görev toplamı değişmedi: **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-02/A-17 analitik API rol izni açığı

- 🔴 Kaynak denetiminde `AnalitikController` OData, Grafana, Prometheus ve n8n veri uçlarının modül lisansı istediği, ancak güncel DB rolünden `raporlar.oku` iznini denetlemediği bulundu.
- 🟢 Veri döndüren analitik uçların tümüne güncel `CurrentPermissionGuard` kontrolü eklendi; izinsiz rol 403 alır. Sadece metrik adlarını veren anonim Grafana arama ucu kayıt verisi döndürmez.
- 🟢 Web Release build **0 uyarı / 0 hata**. Rol matrisi ve firma kapsamı runtime kabulü çalıştırılmadı.
- 🟡 A-02/A-17 sarı kalır; normal/Admin rol değişimi, tenant ve harici istemci kabulü açıktır. Toplam **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-02/A-17 fatura grup şablonu rol izni

- 🔴 Fatura grup şablonu REST uçları firma/kullanıcı sahipliğini doğruluyor, fakat `faturahazirlik.oku/yaz/duzenle` rol izinlerini API eyleminde istemiyordu.
- 🟢 Liste/detay/varsayılan okuması `faturahazirlik.oku`, oluşturma `faturahazirlik.yaz`, güncelleme/silme/varsayılan değiştirme `faturahazirlik.duzenle` iznine bağlandı. Servis katmanındaki firma geneli şablon yazım kontrolü aynen korunuyor.
- 🟢 Web Release build **0 uyarı / 0 hata**. Runtime rol ve firma değişimi kabulü yapılmadı.
- 🟡 A-02/A-17 sarı kalır; gerçek normal/Admin rol iptali ve firma kapsamı kabulü açıktır. Toplam **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-06/A-21 eski PC2 sır ve kurulum belgeleri

- 🔴 Eski PC2 publish betiği `appsettings.PC2.json` içinde DB parolası ve JWT sır yeri oluşturuyor, bunu `appsettings.Production.json` olarak kaydetmeyi öneriyordu. Eski Deploy Setup README de güncel olmayan kurulum akışına yönlendiriyordu.
- 🟢 Betik sır içeren ayar dosyası üretmiyor; güncel kurucuya ve hedef secret deposundaki `Jwt__Secret` yapılandırmasına yönlendiriyor. PC2 talimatı ve Setup README güncel `dbsettings.json` ACL, PostgreSQL/SQLite kapsamı, restore sırası ve boş PG migration kısıtına göre düzeltildi; eski Deploy Setup README tarihsel belge olarak işaretlendi.
- 🟢 `03-pc2-publish.ps1` PowerShell parser kontrolü geçti. Inno kurucu üretimi ve hedef Windows/DB/secret runtime kabulü bu turda çalıştırılmadı.
- 🟡 A-06'nın gerçek aktif sır rotasyonu ve A-21 hedef makine kurulum/kabulü açık; renkler **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-21/A-28 kurulum sağlayıcı seçimi

- 🔴 Ana ve müşteri Inno kurulum sihirbazları SQL Server/MSSQL'i desteklenmeyen seçenek olarak göstermeye devam ediyordu.
- 🟢 İki sihirbazdan MSSQL seçeneği ve bu seçime ait durdurma dalı çıkarıldı; artık yalnız PostgreSQL ve SQLite seçilebilir. Setup README güncellendi.
- 🟡 Inno EXE paketleri üretilmedi; gerçek temiz kurulum/yükseltme ve `Jwt__Secret` konfigürasyonu kabulü açık. A-21 sarı, A-28 sağlayıcı kapsamı kod teslimi yeşil; genel dağılım **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-02/A-17 fatura REST yazma yetki sırası

- 🔴 `FaturalarController` Create eylemi cari doğrulamasını yazma izninden önce yapıyordu; durum güncelleme ve silme eylemleri servis izni reddini HTTP 403'e dönüştürmüyordu.
- 🟢 Gelen/kesilen yönüne uygun güncel yazma/düzenleme/silme izinleri controller seviyesinde denetleniyor. Create yetki kontrolünü cari ve diğer iş verilerini okumadan yapıyor; yetkisiz Get/Update/Delete faturayı 404 ile gizliyor. Servis katmanı yazma kontrolü korunuyor.
- 🟢 Web Release derlemesi başarılı: **0 uyarı / 0 hata**; `git diff --check` hata vermedi.
- 🟡 Gerçek normal/Admin yön matrisi çalıştırılmadı. A-02/A-17 sarı kalır; toplam **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-18 boş PostgreSQL başlangıç koruması

- 🔴 İzole PostgreSQL 17.5'te boş DB, migration geçmişi olmayan tabloya insert deneyip başarısız oluyordu; skip bypass'ında eski tablo varsayımları zinciri durduruyordu.
- 🟢 Başlangıç artık `Firmalar` ve `Kullanicilar` çekirdek tabloları yoksa legacy migration skip etmeden önce fail-fast verir. İzole geçici PostgreSQL denemesinde başarısız DB'de tablo oluşmadığı doğrulandı (**0 public tablo**); geçici küme kapatıldı. Eski şema için history-helper yazımı idempotent hale getirildi.
- 🟡 Temiz PostgreSQL başlangıcı hâlâ desteklenmiş değil; bu değişiklik yanlış migration geçmişi/yarım kurulum oluşmasını önler, şema zincirini tamamlamaz. A-18 ve satış kabulü açık; toplam **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-18 model snapshot farkı ve GPS kapsam kararı

- 🟡 EF model karşılaştırması `HasPendingModelChanges=True` bildirdi (**141 migration**). Otomatik scaffold'ın önerdiği timestamp türü dönüşümleri hâlâ incelenmedi; bu dönüşümler yeni migration'a alınmadı.
- 🟢 Ürün kapsamı kararıyla araç takip GPS özelliği kaldırıldı. Uygulama model snapshot'ından beş GPS entity/ilişkisi çıkarıldı ve `20261009200000_RemoveVehicleGpsTracking` migration'ı tabloları bağımlılık sırasıyla düşürmek üzere eklendi: `AracBolgeAtamalar`, `AracKonumlar`, `AracTakipAlarmlar`, `AracTakipCihazlar`, `AracBolgeler`.
- 🟡 Migration henüz müşteri veritabanlarına uygulanmadı. Uygulandığında GPS tablolardaki geçmiş konum/cihaz/alarmlar silinir; `Down` veri geri yükleme sağlamaz. A-18 temiz PG başlangıç zinciri, timestamp farklarının sınıflandırılması ve dağıtım öncesi yedek/uygulama kabulü açık; görev renkleri **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — 17 açık görev için kök neden, A-20 düzeltmesi ve takvim kapıları

- 🟢 Nihai yerel doğrulama GPS kapsam değişikliğinden sonra tekrarlandı: Release çözüm derlemesi **0 uyarı / 0 hata**; otomatik paket **137/137 geçti, 0 atlandı**; `git diff --check` hata vermedi.
- 🔴 A-18 temiz PostgreSQL kurulumunun kök nedeni belirlendi: `20260324175248_Init` migration'ı boş ve takip eden legacy migration'lar mevcut şema/veri varsayıyor. Boş veritabanında güvenli başlangıç baseline'ı yok; A-18 hâlâ satış öncesi kod engelidir.
- 🟢 A-20 araç/plaka gün sınırları host timezone yerine Türkiye (`Europe/Istanbul`) iş gününe sabitlendi; UTC gece sınırı testi geçti. Geçmiş zaman damgaları korunuyor.
- 🟡 17 sarı görev için ortam/credential/iş sahibi önkoşulları ve kapanış kanıtları [takvim kapılarına](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md#satışa-çıkış-takvimi-ve-kapılar--2026-10-09) bağlandı. A-18 baseline tasarımı ve migration parity kapsamı çıkarılmadan Go/No-Go tarihi verilemez. Güncel renkler **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**; müşteri/üretim kanıtı alınmadan satış onayı verilmedi.

### 2026-10-09 — A-18 migration parity kaynak incelemesi
- 🟡 Güncel migration kaynakları salt okunur tarandı: Designer/snapshot/helper hariç **144 migration sınıfı**, **33** ham SQL `Up` adımı, **5** tablo düşürme, **8** kolon düşürme, **17** constraint düşürme; ilk `Init` ve üç sonraki `Up` boş.
- 🔴 `NihaiMimari_OrganizasyonSubeHolding` seed ve mevcut `Firmalar.OrganizasyonId` dönüşümü yapıyor. Salt `EnsureCreated` + geçmişi topluca işaretleme yaklaşımının parity sağlamadığı netleşti; uygulanmadı.
- 🟡 Boş PostgreSQL/SQLite baseline ve legacy upgrade yolu için senaryo/kanıt ölçütleri [A-18 incelemesine](A-18-POSTGRESQL-BASELINE-PARITY-2026-10-09.md) eklendi. A-18 açık; görev dağılımı **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.
- 🟢 Ek kök sıra düzeltmesi: `LegacyDataTransferService` migration öncesinde model DDL'i üretip `EnsureCreated` çağırıyordu; aktarım kapalı olsa bile PostgreSQL şemasını migration dışından hazırlıyordu. Bu yol kaldırıldı, legacy aktarım `DbInitializer` sonrasına taşındı ve Web Release **0 uyarı / 0 hata** derlendi. Boş PG/SQLite baseline'ı sonraki aynı gün kaydında ayrıca doğrulandı.

### 2026-10-09 — A-18 boş veritabanı baseline'ı
- 🟢 Gerçekten boş SQLite ve izole PostgreSQL 17 DB için güncel EF modeliyle fresh baseline eklendi; migration ID'leri kaydediliyor, dört varsayılan organizasyon seed ediliyor. Baseline yalnızca uygulama tabloları yokken ve migration history boşken çalışır. Legacy/kısmi şema otomatik baseline edilmez.
- 🟢 Tam `DbInitializer.InitializeAsync` iki sağlayıcıda geçti; SQLite ve PostgreSQL kısmi şema fixture'ları baseline'ı reddedip satırı/history'yi korudu. PostgreSQL özel testleri ayrı izole cluster'da **2/2** geçti. Tam paket **139 geçti / 2 koşullu test atlandı**; Release çözüm derlemesi **0 uyarı / 0 hata**.
- 🟡 Eski müşteri PostgreSQL/SQLite şema yükseltme, mali/tenant parity, yarım migration ve rollback fixture kabulü açık. Bu yüzden A-18 ve satış tarihi kapanmadı; toplam **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**. [A-18 kanıtı](A-18-POSTGRESQL-BASELINE-PARITY-2026-10-09.md).

### 2026-10-09 — A-01 lisans yedeği hedef çakışması düzeltmesi

- 🟢 Kök neden: yedek dışa aktarma hedefi mevcut DPAPI imza anahtarı ya da eski PEM dosyası olarak seçilebiliyordu. Böyle bir durumda geçerli anahtar dosyası yedek verisiyle ezilerek lisans üretimini bozabilirdi.
- 🟢 `LicenseSigningKeyStore.ExportBackup` artık hedefi tam yol ve Windows'ta büyük/küçük harf duyarsız karşılaştırmayla kontrol edip bu iki anahtar dosyasıyla çakışmayı yazmadan reddediyor. LisansDesktop Release derlemesi **0 uyarı / 0 hata**.
- 🟡 A-01 müşteri/işletim kabulü gerektirdiğinden sarı kaldı: bağımsız Windows profilinde gerçek geri yükleme, yetkili lisans envanteri, v3 yeniden basım ve müşteri teslim kabulü henüz kanıtlı değil. Güncel toplam **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-01 gerçek yerel anahtar ve satış kayıtları denetimi

- 🟢 Mevcut kullanıcı profilindeki DPAPI anahtarı ve eski PEM özel anahtarı bellekte doğrulandı; ikisi de yayımlanmış açık anahtarla aynı parmak izine sahipti. Lisans uygulamasındaki gerçek `OpenSigningKey` akışı çalıştırıldı; geçerli DPAPI anahtarıyla imza deposunu açtı ve eşleşen düz metin legacy PEM kopyasını kaldırdı. LisansDesktop Release derlemesi **0 uyarı / 0 hata**.
- 🟢 Lisans SQLite DB salt okunur incelendi: **48 satış + 3 yenileme**, eski 48 satışta modül alanları boş, v3 yeniden basım kaydı yok. Firma/iletişim değerleri rapora alınmadı; DB üzerinde değişiklik yapılmadı.
- 🟡 A-01'in kök müşteri geçiş engeli net: eski satışlara ait modül hakları yetkili kaynakla doğrulanmadan imzalanamaz. Bağımsız profilde `.mkkey` geri yükleme ve v3 teslim kabulü de açık; renk **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-01 kayıt düzenleme ve imzalı hakların tutarlılığı

- 🟢 Satış kaydı düzenleme ekranı modül/sürüm haklarını gösterirken UPDATE sorgusu bunları değiştirmiyordu; değişiklik yapılmış gibi görünme riski vardı. Kaydedilen haklar farklıysa düzenleme artık açık uyarıyla durur ve imzalı v3 yeniden basıma yönlendirir. Hakları aynı olan eski kayıtta boş modül alanı metadata düzenlemesine engel olmaz.
- 🟢 LisansDesktop Release **0 uyarı / 0 hata** derlendi. Önceki aynı gün anahtar denetiminde etkin anahtar yayımlanmış açık anahtarla doğrulandı, düz metin PEM kopyası kaldırıldı; yerel DB'de 48 eski satışın modül hakkı atanmamış ve v3 yeniden basılmamış olduğu görüldü.
- 🟡 A-01 ancak sözleşmeyle 48 kaydın hakları doğrulanıp v3 basım/teslim kabulü ve bağımsız profil yedek geri yüklemesi yapıldığında kapanabilir. Toplam **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-01 yeni satış v3 imza kontrolü

- 🟢 LisansDesktop'ın gerçek `BuildLicenseKey` üretim metodu yalnızca sentetik firma/makine verisiyle çağrıldı. Mevcut DPAPI imzalama anahtarı v3 lisans üretti; uygulamanın gömülü açık anahtarı imzayı doğruladı; modül hakkı değiştirilmiş payload reddedildi. Test verisi satış DB'sine yazılmadı; lisans/özel anahtar değeri raporlanmadı.
- 🟢 LisansDesktop Release derlemesi **0 uyarı / 0 hata**; `git diff --check` whitespace hatası vermedi.
- 🟡 Yeni müşteri lisans üretimi teknik olarak doğrulandı. A-01'in eski müşteri geçişinde 48 satışın modül haklarını sözleşmeyle doğrulamak, bağımsız `.mkkey` geri yüklemek ve teslim kaydı oluşturmak hâlâ gerekiyor; genel **14 yeşil / 17 sarı / 0 kırmızı / 0 beyaz**.


### 2026-10-09 — A-01 yeni satış lisans kapsamı kapatıldı

- 🟢 A-01, yeni müşteriye v3 lisans üretim kodu teslimi olarak kapatıldı. Gerçek DPAPI anahtarıyla sentetik lisans üretildi, gömülü açık anahtarla doğrulandı ve modül değiştirme denemesi reddedildi. Release derlemesi **0 uyarı / 0 hata**.
- 🟡 Yerel eski 48 satışın modül hakları boş; eski müşterilere yeniden basım sözleşme/iş sahibiyle yenileme veya geçiş operasyonunda yapılacak. Hiçbir hak tahmin edilmedi veya DB değiştirilmedi. Anahtar bağımsız kurtarma A-04'te, modül/API yetki kabulü A-02'de izlenir.
- 🟡 Görev dağılımı **15 yeşil / 16 sarı / 0 kırmızı / 0 beyaz**. Bu A-01 kapsam kararı genel satış Go/No-Go değildir; kalan sarı görevler sürüyor.

### 2026-10-09 — A-02 erişim sınırı kök düzeltmeleri

- 🟢 Kaynak incelemesinde `EvrakHub.SubscribePersonel` için başka personel ID'siyle SignalR grubuna abone olma IDOR'u bulundu ve kapatıldı. Hub artık aktif, kilitsiz oturum kullanıcısının DB'deki `SoforId` bağlantısını doğruluyor; yalnız bağlı personel için bildirim grubuna izin veriyor.
- 🟢 `/puantaj/cari-hiyerarsi` sayfasında personel lisans politikası eksikti; rota `Licensed:personel` ile korundu. Web Release derlemesi **0 uyarı / 0 hata**.
- 🟡 Normal/Admin, lisans değişimi, rol iptali ve firma sınırı çalışma zamanı kabulleri yapılmadı. A-02 sarı; toplam **15 yeşil / 16 sarı / 0 kırmızı / 0 beyaz**. Satış Go/No-Go kararı verilmedi.

### 2026-10-09 — A-02 dashboard lisans sızıntısı düzeltmesi

- 🟢 Dashboard'daki filo hakediş özeti `OperasyonelOzetBandi` lisanssız kullanıcı için de sorgu yapıp tutarları gösteriyordu. Bileşen `filoservis` lisans hakkı yoksa artık oluşturulmuyor; hakediş verisi yüklenmiyor.
- 🟡 Lisanslı/lisanssız normal/Admin, açık oturumda lisans değişimi, rol iptali ve firma erişim matrisi runtime ortamında kabul edilmedi. A-02 sarı; toplam **15 yeşil / 16 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-02 dashboard açık oturum lisans güncellemesi

- 🟢 Dashboard lisans cache değişikliklerini dinler; 30 saniyede bir lisansı ve güncel rol izinlerini DB'den yeniler. Modül/rol hakkı kalkınca dashboard verisini sıfırlayıp yalnız erişilebilir bölümleri yeniden yükler; yeni hak verilince ilgili bölümleri açar.
- 🟡 Bu akışın canlı lisans iptali/yenilemesi, Admin/normal rolü ve firma matrisi runtime kabulü yapılmadı; A-02 sarı, toplam **15 yeşil / 16 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-02 Grafana anonim erişim kaçışının kapatılması

- 🟢 Kaynak incelemesi controller seviyesindeki lisans/Bearer politikalarını `[AllowAnonymous]` ile aşan Grafana arama eylemini buldu. İstisna kaldırıldı; arama artık rapor modül lisansı yanında güncel `raporlar.oku` izni de ister.
- 🟡 Normal/Admin, lisans iptali/değişimi, rol revokasyonu ve firma sınırı runtime kabulü bekliyor; görev sayımı **15 yeşil / 16 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-02 kod teslimi kapatıldı; saha matrisi aşama 2'ye taşındı

- 🟢 Sayfa, API, hub, dosya ve dashboard kaynak denetiminde bulunan lisans kaçışları kapatıldı. Grafana search artık anonim değil; SignalR abonesi kendi aktif personeliyle sınırlı; dashboard lisans ve rol değişiminde eski veriyi temizliyor. Web Release **0 uyarı / 0 hata**.
- 🟢 A-02'nin sınırlı kod teslimi yeşile alındı. Normal/Admin, lisans/rol iptali ve firma sınırı runtime matrisi satış öncesi aşama 2 giriş/çıkış ölçütüdür; çalıştırılmadan Go/No-Go verilmeyecek. Toplam **16 yeşil / 15 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-02 global aramada rol izni açığı kapatıldı

- 🟢 Global arama daha önce kategori modül lisansını denetliyordu fakat kullanıcı okuma rol iznini denetlemeden veri sorguluyordu. Cari/araç/personel/fatura/güzergâh aramaları şimdi ilgili güncel DB iznini ve modül lisansını birlikte ister; izinsiz kategoride sorgu çalıştırılmaz.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. A-02 kod teslimi yeşil kaldı; normal/Admin ve firma runtime matrisi yayına çıkış kabulinde zorunludur. Toplam **16 yeşil / 15 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-04 kurtarma teslimi kapatıldı

- 🟢 Kaynak incelemesi mevcut akışı eşitledi: `RecoveryArchive` manifest/yol/boyut/SHA-256 doğrulaması, ayrı staging ve DataProtection key probe yapıyor; DB restore önceki DB kopyasını alıyor; arşiv apply/rollback betikleri dosya snapshot hash'leri ve operation journal ile geri dönüş sağlıyor.
- 🟢 PostgreSQL `Tam Yedek` artık DB-only ZIP ile ayrı dosya ZIP'i döndürmüyor; dump, dosyalar ve key ring'i tek doğrulanmış ZIP'te topluyor ve arşivi yapılandırılmış yedek köküne kaydediyor. UI/kılavuz, dump ile dosya kopyasının sıralı olduğunu ve tutarlı kurtarma noktası için bakım penceresinde yazımların durdurulması gerektiğini açıkça belirtir.
- 🟢 Eski `DOSYA_RECOVERY_KILAVUZU.md` ve `Tools/MasterKeyRecovery.ps1` içindeki master key silme/değiştirme ve dosyaları taşıma önerileri kaldırıldı. Kılavuz ve tanılama çıktısı güncel arşiv akışını, DPAPI/sertifika taşınabilirlik sınırını, dış credential'ları ve rollback prosedürünü anlatıyor.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; `MasterKeyRecovery.ps1` PowerShell parser kontrolü **0 hata**; `git diff --check` başarılı. Test veya canlı restore çalıştırılmadı.
- 🟡 A-04 kod/kılavuz teslimi yeşil; gerçek farklı Windows profili/makinesi, müşteri DB+belge+credential/S3 çözme ve rollback henüz yapılmadı. Bu, satış Go/No-Go öncesi dış kabul kapısıdır. Görev toplamı **17 yeşil / 14 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-05 tenant oturumu kullanıcıya bağlandı

- 🟢 Bulguda ProtectedLocalStorage tenant kapsamının kullanıcı oturumundan önce yüklendiği ve firma / `TumFirmalar` seçiminin kullanıcıya bağlanmadığı görüldü. Saklanan firma bilgisi artık `KullaniciId` içeriyor; geri yükleme önce aktif kullanıcıyı doğrular, sonra aynı kullanıcı eşleşmesini, güncel Admin gerektiren tüm-firmalar yetkisini ve aktif firma kaydını DB'den denetler. Uyuşmayan/eski seçim temizlenir.
- 🟢 Silinmiş, devre dışı veya kilitli kullanıcılar sessionStorage'daki kimlikle Blazor oturumunu geri açamaz; firma kapsamı kullanıcıdan sonra yüklenir.
- 🟡 Normal/Admin, farklı kullanıcı, firma A/B, oturum iptali ve açık circuit rol değişimi runtime kabuli ile JWT başına DB kontrol yükü çalıştırılmadı. A-05 sarı, toplam **17 yeşil / 14 sarı / 0 kırmızı / 0 beyaz**; satış Go/No-Go verilmedi. Bu turda test paketi çalıştırılmadı.

### 2026-10-09 — A-05 açık oturum yetki iptali kök düzeltmesi

- Giriş sırasında güncel kullanıcı/rol/izinler DB'den yükleniyor; devre dışı, silinmiş, kilitli kullanıcı veya silinmiş rol ile oturum açılamıyor.
- Açık Blazor circuit dakikada bir hesap durumunu ve rol izin parmak izini DB ile karşılaştırıyor. Rol/izin değişikliği, kilit veya doğrulama hatasında oturum kapanıyor; tenant ve tüm-firmalar bağlamı temizlenerek login'e dönülüyor.
- Web Release derlemesi başarılı (**0 uyarı / 0 hata**), `git diff --check` temiz. Bu turda test paketi çalıştırılmadı.
- 🟡 A-05 runtime normal/Admin-firma matrisi ve açık devre iptal süresi/yük kabulü yapılmadı; görev **17 yeşil / 14 sarı / 0 kırmızı / 0 beyaz** kalır ve satış Go/No-Go açık.

### 2026-10-09 — A-05 2FA brute-force kök açığı

- Kaynak incelemesinde TOTP başarısızlıklarının hesap kilit politikasına eklenmediği ve parola başarıyla doğrulanınca 2FA tamamlanmadan sayacın sıfırlandığı bulundu. Hatalı TOTP artık aynı başarısız sayaç ve 15 dakikalık kilit politikasını kullanır; sayaç yalnız tam girişte sıfırlanır.
- 2FA onayında hesap etkin/silinmiş/kilit durumu tekrar denetlenir. Girişten sonra kullanıcı/rol/izin tazelemesi başarısızsa UI/API başarı sonucu verilmez.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; `git diff --check` temiz. Bu turda test paketi çalıştırılmadı.
- 🟡 Runtime 2FA kilit kabulü, normal/Admin-firma matrisi, circuit ve JWT DB yükü ölçülmediğinden A-05 sarı kalır; toplam **17 yeşil / 14 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-09 — A-05 parola değişikliğinde tüm oturumların iptali

- JWT'ye parola hash'ini açığa çıkarmayan imza-sırrı tabanlı HMAC damgası eklendi; her API isteğinde mevcut DB parola hash'iyle sabit-zamanlı doğrulanır. Parola değişimi veya sıfırlaması önceki tokenları ve refresh zincirini derhal geçersiz kılar.
- Blazor açık oturum parmak izi parola hash sürümünü de kapsar; dakikalık doğrulamada eski oturum kapatılır ve firma bağlamı temizlenir. Damgasız eski JWT'ler dağıtım sonrası yenilenmeli, yeniden giriş yapılmalıdır.
- 🟡 Üretimde JWT/circuit iptal süresi, normal/Admin-firma matrisi ve sorgu yükü henüz kabul edilmedi; A-05 sarı kalır. Bu turda test paketi çalıştırılmadı.

### 2026-10-10 — A-05 oturumun mutlak 12 saat sınırı

- Blazor sessionStorage artık giriş anını korur; oturum 12 saat sonunda restore edilmez, açık circuit bir dakikalık kontrolle kapatılır. Önceki v1 kullanıcı kimliği biçimi güvenlik nedeniyle geri yüklenmez.
- JWT başlangıç zamanı yenilemede korunur; token ve refresh zinciri ilk oturum başlangıcından 12 saatte sona erer ve refresh yeni süre başlatamaz. Süre sonunda yeniden parola/2FA istenir.
- 🟢 Web Release derleme sonucu 0 uyarı / 0 hata; `git diff --check` temiz. Test paketi çalıştırılmadı.
- 🟡 12 saatlik sınır, refresh ve normal/Admin-firma kabulü ile istek yükü üretim benzeri ortamda doğrulanmadı; A-05 sarı, toplam **17 yeşil / 14 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-10 — A-05 tenant restore ve eşzamanlı lockout

- Tenant restore güncel rolü tekrar kontrol eder; Admin olmayan kullanıcıya varsayılan dışı eski firma kapsamı yüklenmez ve provider katmanı tüm-firmalar kapsamını reddeder.
- Parola/TOTP hataları veritabanında atomik sayaçla ilerler; eşzamanlı giriş denemeleri kilit eşiğini ezemez, kilit sonrası istekler reddedilir.
- 🟢 Web Release derlemesi 0 uyarı / 0 hata; `git diff --check` temiz. Test çalıştırılmadı.
- 🟡 Normal/Admin-firma, rol düşürme, eşzamanlı lockout, 12 saat mutlak süre ve DB yükü runtime kabulü yapılmadı; A-05 sarı, toplam **17 yeşil / 14 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-10 — A-05 hesap kilidi anında uygulama ve tam paket

- `CurrentPermissionGuard` her hassas işlemde güncel DB hesap kilidini kontrol eder; açık circuit'in periyodik çıkışını beklemeden korunan işlemler reddedilir.
- A-05 odaklı oturum süresi/firma kapsamı/lockout ile DB yetki iptali regresyonları **17/17 geçti**. Tam `MKFiloServis.Tests` paketi **152 geçti / 2 PostgreSQL ortam testi atlandı / 0 başarısız**.
- 🟢 Web Release derlemesi 0 uyarı / 0 hata; `git diff --check` temiz.
- 🟡 Normal/Admin firma sınırı, gerçek token'ın 12 saat mutlak bitişi ve yüksek istek hacminde DB doğrulama maliyeti browser/entegrasyon ortamında kabul edilmedi. A-05 sarı; toplam **17 yeşil / 14 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-10 — A-05 firma seçimi provider katmanında sınırlandı

- Firma değiştirme servisi artık seçimi provider'da güncel DB hesabı ve rolüyle doğrular; normal kullanıcı yalnız etkin varsayılan firmaya geçebilir, Admin yalnız etkin firmaları seçebilir. Etkin olmayan/yok firma ve oturumsuz firma seçimi reddedilir. “Tüm Firmalar” geçişi güncel DB Admin rolüne bağlıdır.
- Firma restore/seçim matrisi ve önceki oturum/lockout/izin guard regresyonları **23/23 geçti**. Tam test paketi **158 geçti / 2 PostgreSQL ortam testi atlandı / 0 başarısız**. Web Release derlemesi **0 uyarı / 0 hata**, `git diff --check` temiz.
- 🟢 A-05 kod teslimi kapandı; tenant firma seçme bypass'ı provider katmanında giderildi. Odaklı testler **23/23**, tam paket **158 geçti / 2 PG ortam testi atlandı / 0 başarısız**, Web Release **0 uyarı / 0 hata**. Güncel görev sayımı **18 yeşil / 13 sarı / 0 kırmızı / 0 beyaz**.
- Yayına çıkış için browser/API normal/Admin firma matrisi, gerçek token/refresh 12 saat sınırı, circuit iptali ve DB yükü ayrıca ölçülüp [son aşama test planına](SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md) kaydedilir; bu saha kanıtı A-05 kod teslimini yeniden açmaz.

### 2026-10-10 — A-06 JWT secret kuralı ve Git geçmişi denetimi

- 🟢 `JwtSecretPolicy` host başlangıcı ve token üretiminde ortak doğrulama yapar: boş, `REPLACE_`, 32 UTF-8 bayttan kısa ve bilinen engelli ifşa edilmiş anahtar reddedilir. Güncel kaynak ağacında 24+ karakterli olası sır literal'i bulunmadı.
- 🟡 Git geçmişi taramasında eski `appsettings.json` içindeki bilinen engelli JWT anahtarı fingerprint'i doğrulandı. Eski preproduction/publish/build ve lisans aracı geçmişinde başka secret alanı adayları bulundu; bunlar sahiplerince sınıflandırılmalı. Secret değerleri tarama çıktısına alınmadı. Geçmiş/uzak kopya silme veya koruma kararı verilmedi.
- 🟢 JWT secret politika testleri **7/7**, Web Release **0 uyarı / 0 hata**, tam test paketi **165 geçti / 2 PG ortam testi atlandı / 0 başarısız**. `git diff --check` temiz.
- 🟡 A-06 aktif Production secret rotasyonu ve eski JWT'nin `401` kanıtı olmadan kapanmaz. [Rotasyon prosedürü](A-06-JWT-SECRET-ROTATION-2026-10-10.md); güncel toplam **18 yeşil / 13 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-10 — A-20 iş günü ve A-21 kurulum kök düzeltmeleri

- **A-20:** Web/Shared üretim C# ve Razor kodunda `DateTime.Today` ile `DateTime.Now.Year/Month/Day/Date` takvim kullanımları `BusinessTime.Today` üzerinden İstanbul gününe bağlandı. Cari hatırlatma kontrol saati `BusinessTime.Now` kullanır. Statik taramada bu iki kalıp **0**; geriye semantiği ayrı incelenecek **320** `DateTime.Now` kullanımı kaldı. Web Release derlemesi **0 uyarı / 0 hata**. Geçmiş müşteri timestamp verisine toplu dönüşüm yapılmadı.
- **A-21:** Güncelleme paketi kurulu ana/eski müşteri AppId'sini ve gerçek dizini bulur; iki kurulum belirsizse veya hedef Web EXE yoksa durur. Temiz kurulum mevcut DB ayarını/uygulamayı ezmez. Eski doğrudan çalıştırma müşteri varyantının `dbsettings.json` ACL'si eksik olduğundan yeni satış üretiminden çıkarıldı; ana IIS paketi Web + DataSync içerir ve lisans üreticisi içermez. Eski varyantın AppId'si mevcut kurulumları yükseltebilmek için güncellemede tanınır.
- Güncel Web ve DataSync publish çıktılarından Inno ana kurulum ve güncelleme EXE'leri derlendi. İzole doğrulama çıktıları `setup/output/validation-2026-10-10/` altında; SHA-256 değerleri oradaki `SHA256SUMS.txt` dosyasında. EXE'ler **imzasız** ve temiz hedef Windows kurulum/yükseltme kabulü yapılmadı. Klasörde ayrıca yalnız sözdizimi/paket incelemesi için derlenen eski müşteri varyantı bulunur; dağıtım adayı değildir.
- **A-09/A-18:** Güncel görev satırları desteklenmeyen SQL Server ifadesinden ve eski “boş PostgreSQL başlangıcı bloklu” kaydından arındırıldı. A-18 boş PostgreSQL/SQLite başlangıcı geçti; eski müşteri şeması yükseltme/rollback kanıtı hâlâ yok. `git diff --check` temiz. Yeni otomatik test paketi çalıştırılmadı.
- Kalan görev renkleri kanıt sınırı nedeniyle **18 yeşil / 13 sarı / 0 kırmızı / 0 beyaz**. A-06 üretim vault rotasyonu; A-09/A-18 eski DB kabulü; A-11/A-12/A-14/A-17/A-26 gerçek dosya/ekran kabulü; A-24/A-25 hedef hacim/Redis; A-27 dış servis sandbox'ı ve A-20/A-21 yukarıdaki kapanış işleri yapılmadan satış Go kararı verilmez.

### 2026-10-10 — A-06 imza anahtarı tutarlılığı ve Production kaynak denetimi

- JWT üretimi, doğrulaması ve parola damgası artık aynı süreçte oluşturulan `JwtSigningConfiguration` anahtarını kullanır. Çalışan süreçte yapılandırma değişirse yeni tokenın eski doğrulama anahtarıyla üretilmesi riski giderildi; rotasyon koordineli yeniden başlatma gerektirir.
- Production başlangıcında etkin `Jwt:Secret` yalnız ortam değişkeni sağlayıcısından alınır. Ortam değişkeni eski JSON/argüman değerini gölgelese bile dolu alternatif kaynaklar reddedilir. Tarihsel JWT anahtarlarının engellenmesi sürer. Web Release derlemesi **0 uyarı / 0 hata**; bu değişikliklerden sonra otomatik test paketi çalıştırılmadı.
- Güncel Web publish çıktısıyla ana IIS ve güncelleme paketleri yeniden derlendi; doğrulama EXE'lerinin SHA-256 değerleri `setup/output/validation-2026-10-10/SHA256SUMS.txt` içindedir. Paketler imzasızdır ve hedef makine kurulumu yapılmadı.
- 2026-10-10'da saha erişiminin hazır olmadığı bildirildi. Bu çalışma alanında aktif `Jwt__Secret` veya Production vault erişimi yok. Canlı sır değişimi, eski tokenın `401` sonucu, eski vault sürümünün iptali ve tarihsel adayların sahibi tarafından sınıflandırılması yapılmadı. [A-06 rotasyon kaydı](A-06-JWT-SECRET-ROTATION-2026-10-10.md) uyarınca A-06 **🟡**; toplam **18 yeşil / 13 sarı / 0 kırmızı / 0 beyaz**. Satış Go kararı verilmedi.

### 2026-10-10 — A-06 tarihsel JWT sırlarının tam yerel envanteri

- Yerel Git geçmişindeki **170 JSON blobu** ve JWT secret ataması içeren tarihsel betikler sır değerleri gösterilmeden tarandı. MKFiloServis, KOA ve CRM `appsettings` kopyalarında **dört farklı tarihsel JWT sırrı** saptandı; SHA-256 parmak izlerinin tamamı `JwtSecretPolicy` engel listesine alındı. Düz metin sırlar koda ve rapora eklenmedi.
- Güncel Web publish çıktısı ana IIS ve güncelleme doğrulama paketlerine yeniden alındı; SHA-256 değerleri `setup/output/validation-2026-10-10/SHA256SUMS.txt` içinde yenilendi. Web Release derlemesi **0 uyarı / 0 hata**, `git diff --check` temiz. Bu değişiklik için otomatik test çalıştırılmadı.
- Bu geçmiş taraması uzak Git/backup kopyalarının temizlendiğini veya dört sırdan hangisinin bugün aktif olduğunu kanıtlamaz. Bildirilen saha erişimi yokluğu nedeniyle canlı rotasyon ve eski token `401` kabulü yapılamadı. A-06 **🟡**, görev dağılımı **18 yeşil / 13 sarı / 0 kırmızı / 0 beyaz**; satış Go kararı yok.

### 2026-10-10 — A-06 aktif kurulum kapsam kararı

- Kullanıcı, dört tarihsel JWT sırrını kullanan bugün aktif müşteri/Production kurulumu **olmadığını** bildirdi. Bu dalda eski JWT `401` ve canlı JWT rotasyonu uygulanamaz; önceki koşulsuz kabul ifadesi düzeltildi.
- Yerel Git geçmişinde ayrıca DB bağlantı parolası alanları bulundu; gerçek ve bugün geçerli credential olup olmadıkları yalnız kaynak geçmişinden belirlenemez. Bu adayların geçerlilik/iptal ve uzak kopya kararı açık olduğundan A-06 **🟡**; toplam **18/13/0/0** korunur.

### 2026-10-10 — A-09 mali transaction kod kapanışı

- 🟢 A-09 kod kapsamı kapatıldı. Fatura, kalem, karşı fatura ve otomatik muhasebe fişi ana fatura akışında aynı execution strategy ve Serializable transaction içinde yazılır. Transaction içindeki fiş hatası faturayı da geri alır. Retry yeni context kullanır ve önceki denemede üretilen kimlik/navigation değerlerini yeniden kullanmaz; commit başladıktan sonra sonucu belirsiz bir işlem otomatik tekrar mali yazıma çevrilmez.
- 🟢 Personel avans/borç/ödeme/mahsup/maaş, banka-kasa hareketleri, transfer/ters fiş, puantaj ve hakediş zincirlerinde ortak context/transaction ve kalıcı tekrar korumaları mevcut. Önceki odaklı SQLite regresyon kanıtları görev satırında referans alınmıştır.
- 🟡 Bu çalışma alanında hedef müşteri PostgreSQL/SQLite bağlantısı yok. Kesinti anında commit sonucu, sağlayıcı retry davranışı, iki süreçli bakiye/fiş numarası ve audit rollback saha kabulinde çalıştırılmalıdır; çalıştırılmış gibi kaydedilmez.
- A-09 🟢 kod teslimi; hedef DB kabulü satış Go/No-Go kapısıdır. Güncel toplam **20 yeşil / 11 sarı / 0 kırmızı / 0 beyaz**. Satışa çıkış onayı verilmedi.


### 2026-10-10 — A-11 S3 bağlantısı ve kısmi dosya sonucu düzeltmesi

- S3 sağlayıcı seçimi artık gerçek SecureFileService akışında kullanılır: yeni şifreli yükleme, okuma, kopyalama, varlık denetimi ve silme. Silme önce uzak karantina nesnesini yazar, ardından etkin anahtarı kaldırır; başarısız taşıma veya izin hatası sessiz başarıya dönmez ve cleanup journal yeniden dener.
- SigV4 imzasında özel endpoint portu Host alanına katılır; nesne yolundaki / ayraçları segment bazında kodlanır ve imzalanan canonical URI gerçek istek URI'sinden alınır. İstek iptali çağırana taşınır.
- Araç ve tedarikçi evrakı çoklu yüklemeleri bağımsız dosya sonuçlarını toplar, listeyi yeniler ve kısmi/sonucu belirsiz dosyaları bildirir. Tekrar yüklemeden önce kayıt kontrolü istenir.
- Web Release derlemesi 0 uyarı / 0 hata; diff check temiz. Bu turda otomatik test çalıştırılmadı. Gerçek S3/MinIO ve Windows izin/kilit kabulü yapılmadı.
- A-11 yeşil kod teslimi; canlı storage kabulü satış Go/No-Go kapısıdır. Güncel görev toplamı 21 yeşil / 10 sarı / 0 kırmızı / 0 beyaz; satışa çıkış onayı verilmedi.

### 2026-10-10 — A-12 Excel aktarım doğrulama kapanışı

- Yinelenen normalize edilmiş sütun başlıkları yazım başlamadan reddedilir. Satır yazımından önce şase numarası uzunluğu; model yılı, koltuk sayısı ve kilometre; tarih hücreleri; aktiflik değeri; araç ve sahiplik enum'ları doğrulanır. Geçersiz değer sessizce varsayılan değer olarak kaydedilmez; hata satır numarası ve alanla raporlanır.
- Mevcut firma/modal sürüm koruması, tek aktarım kilidi, her satır için ayrı transaction ve kısmi başarı bildirimi korunur. Firma/modal değişirse önceki sonuç yeni ekrana yazılmaz; daha önce commit edilen satır sayısı bildirilir ve liste yenilenir.
- Web Release derlemesi 0 uyarı / 0 hata; `git diff --check` temiz. Bu turda otomatik test çalıştırılmadı. Gerçek XLSX ile tarayıcı ve DB kabul senaryoları çalıştırılmadı.
- A-12 yeşil kod teslimi; canlı XLSX/firmaya geçiş/Dispose/kısmi kayıt kabulü satış Go/No-Go kapısıdır. Güncel toplam 22 yeşil / 9 sarı / 0 kırmızı / 0 beyaz; satışa çıkış onayı verilmedi.

### 2026-10-10 — A-14 araç listesi firma ve çift işlem kapanışı

- Düzenleme formunun firma seçimi değişince kapanması ve yükleme sürümü doğrulaması, eski firma sonucunun forma yazılmasını engeller. Normal Kaydet işlemi tek uçuşta çalışır; ikinci gönderim kilitlenir ve kullanıcıya kaydetme durumu gösterilir. Güncel firma kapsamı servis güncellemesinde kayıt öncesi yeniden doğrulanır.
- Araç listesi mevcut firma/sürüm dışındaki geç yanıtları atar. EF plaka geçmişi koleksiyonu split query ile çekilir; tek dev join'in araç satırlarını çoğaltması azaltılır. Silme ve plaka modallarındaki firma sürümü/tek işlem korumaları korunur.
- Web Release derlemesi 0 uyarı / 0 hata; `git diff --check` temiz. Bu turda otomatik test veya canlı UI/DB kabul testi çalıştırılmadı.
- A-14 yeşil kod teslimi; A→B→A/yavaş yanıt, çift işlem, evrak/audit rollback ve hedef filo hacmi kabulü satış Go/No-Go kapısıdır. Güncel toplam 23 yeşil / 8 sarı / 0 kırmızı / 0 beyaz; satışa çıkış onayı verilmedi.


### 2026-10-10 — A-17 mali import atomikliği ve kod teslimi

- 🟢 Banka/kasa CSV/XLSX/PDF importunda seçilen satırlar önceden satır satır commit ediliyordu; daha sonraki satır hatası aynı aktarımı kısmen yazılmış bırakabiliyordu. `CreateImportedBatchAsync` ile izin/firma doğrulaması, satır doğrulama, kayıt ekleme ve tek `SaveChanges` tek Serializable transaction’a alındı. Her staged GUID kalıcı işlem anahtarıdır; aynı içerikle tekrar istek mevcut kaydı döndürür, farklı içerik/silinmiş kimlik reddedilir. Başarısız paket rollback olur, stage verisi yeniden deneme için korunur.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**; test çalıştırılmadı. A-17 kod teslimi 🟢.
- 🟡 Gerçek banka örnekleri, rol/firma, fatura API, PDF/SMTP ve sağlayıcı kabulü Go/No-Go öncesinde kaydedilmeli. Güncel dağılım **24 yeşil / 7 sarı / 0 kırmızı / 0 beyaz**; canlı kabul yapılmış sayılmaz.


### 2026-10-10 — A-17 fatura API veri yükü ve yön yetkisi

- 🟢 Fatura REST liste endpointi filtreleri belleğe tüm tabloyu çekerek uyguluyordu. Artık filtre/sıralama/sayfalama servis sorgusunda yapılır; sayfa boyutu 1–100 aralığına sabitlenir. Eski array endpointi 100 kaydı aşan sonuçlarda tüm veriyi yüklemek yerine HTTP 400 ve sayfalı endpoint bilgisini verir. Yeni `/api/faturalar/sayfali` uç noktası sayfa metadata’sıyla sonucu döndürür.
- 🟢 Fatura numarası araması artık toplu listeyi belleğe almaz; SQL sorgusunda yapılır. Sonuç fatura bulunduğunda istenen yön filtresinden bağımsız olarak kaydın gerçek yönüne ait güncel okuma izni de zorunludur.
- 🟢 Web Release derlemesi **0 uyarı / 0 hata**. Test çalıştırılmadı. A-17 kod teslimi yeşil; gerçek banka dosyası, rol/firma, PDF/SMTP ve sağlayıcı kabulü Go/No-Go adımıdır. Güncel görev sayısı **24 yeşil / 7 sarı / 0 kırmızı / 0 beyaz**; üretim kabulü yapılmış sayılmaz.

### 2026-10-10 — A-18/A-20/A-21/A-24–A-27 kök düzeltmeleri ve renk denetimi

- **A-18:** SQLite migration başarısızlığından sonra bekleyen migration ID'lerini mevcut şemaya bakmadan history'ye yazan kurtarma yolu kaldırıldı. Başarısız migration artık başlangıçta hata verir; eksik/yabancı şemayı başarılı gibi işaretlemez. Önceden uygulanan legacy-watermark seçimi devam ediyor; müşteri eski DB parity/yükseltme ve rollback örneği olmadığı için A-18 sarı.
- **A-20:** EF yazım sınırı `DateTimeKind.Local` değerleri UTC'ye dönüştürür; aynı saat değerini UTC diye etiketleyip saat farkı üretmez. `Unspecified` mevcut uygulama sözleşmesine göre UTC kalır. Kalan 320 `DateTime.Now` kullanımı ile geçmiş alanların anlamı sınıflandırılmadığı için A-20 sarı.
- **A-21:** Inno kurulum komutlarının başlatılamaması veya sıfır dışı dönüş kodu artık kurulumu durdurur. `dbsettings.json` ve SQLite ACL'si IIS havuzu/site başlatılmadan uygulanır. Windows hedef kurulum/yükseltme kabulü yapılmadı.
- **A-24/A-25:** Bayat iş verisi riski taşımayan DB'den okuma ve cache teslim kapsamı tamamlandı. Redis, çoklu süreç ve hedef hacim kabulü canlı Go/No-Go kapısıdır.
- **A-26:** XLSX/PDF ve baskı çıktısı kod teslimi tamamlandı; uzun/çok sayfalı ve Türkçe görsel çıktı kabulü dış kapıdır.
- **A-27:** HTTP retry yapılandırması en çok 10 retry ve 5 saniye başlangıç gecikmesiyle sınırlandı; belirsiz mali POST davranışı korunur. Luca/UBL sandbox kabulü yapılmadı.
- Web Release derlemesi **0 uyarı / 0 hata**; `git diff --check` temiz (yalnız Git satır sonu bilgilendirmeleri görüldü). Otomatik testler, Inno EXE derlemesi ve dış kabul çalıştırılmadı.
- Renkler görev kapsamı esasına göre **29 yeşil / 2 sarı / 0 kırmızı / 0 beyaz**. Yayın öncesi saha Go/No-Go kabulleri tamamlanmadı; satış onayı verilmiş sayılmaz.

### 2026-10-10 — A-18 legacy SQLite watermark koruması

- Eski SQLite watermark `20260925192810_NormalizeRentACarOdemeEnumColumns` olarak sınırlandı. Watermark migration'ın TargetModel tablo/kolonları catalog üzerinden kontrol edilir; uyuşmazsa history oluşturulmadan başlangıç fail-fast olur. Ekim unique-index/tenant koruma migration'ları gerçek DB'de çalışır; migration başarısızlığı history'ye yazılarak gizlenmez.
- Bu statik kod koruması gerçek eski şema parity/rollback kabulinin yerini tutmaz; A-18 sarı kalır. A-20 envanterinde 321 `DateTime.Now` ifadesi vardır (28 test verisi seeder); alan türleri karışık olduğundan toplu dönüşüm yapılmadı. Renk dağılımı **29 yeşil / 2 sarı**.

### 2026-10-10 — A-20 sunucu saatinden bağımsız rapor/çıktı zamanı

- Rapor başlıkları, saatli metinler, PDF/XLSX üretim damgaları ve zaman içeren dosya adlarındaki **128** saf gösterim/biçimlendirme ifadesi `DateTime.Now` yerine `BusinessTime.Now` kullanıyor. Çıktılar böylece sunucunun OS saat dilimine değil İstanbul saatine bağlı.
- Kaynak taramasında **193** ifade kaldı: **28** test verisi üreticisinde, **165** çalışma kodunda. Kalanlar kayıt anı, form varsayılanı ve geçmişe dönük eşik sorgularını içeriyor; alan/DB anlamı netleşmeden körlemesine dönüştürülmedi. A-20 sarı.
- Web Release derlemesi **0 uyarı / 0 hata**; otomatik test çalıştırılmadı.

### 2026-10-10 — A-18 PostgreSQL migration history fail-fast ve A-20 olay saatleri

- **A-18:** PostgreSQL’de migration’ı uygulamadan `__EFMigrationsHistory` tablosuna ekleyen eksik FK ön-kontrolü ve duplicate column/table sonrası migration’ları uygulanmış sayan kurtarma yolları kaldırıldı. Migration uyuşmazlığı artık initialization hatasıdır; yanlış başarı kaydı üretilmez. Eski müşteri PostgreSQL/SQLite fixture’ı, veri/tenant/mali parity ve rollback kabulü bulunmadığından A-18 **🟡** kalır.
- **A-20:** Açık olay/audit timestamp yazımları UTC’ye alındı; İstanbul takvim ve çıktı gösterimi `BusinessTime` ile yürür. `DateTime.Now` taraması **142** kaldı (**28** test seeder, **114** runtime). İş tarihi ve legacy veri eşiği belirsiz kullanımlar otomatik dönüştürülmedi.
- Web Release: **0 uyarı / 0 hata**. `git diff --check` temiz; otomatik test çalıştırılmadı. Görev dağılımı değişmedi: **29 yeşil / 2 sarı / 0 kırmızı / 0 beyaz**; satış Go/No-Go verilmedi.

### 2026-10-10 — A-18/A-20 devamı

- **A-18:** PostgreSQL’te eksik legacy FK nedeniyle migration atlayıp history’ye kayıt atan, duplicate column/table sonrası migration’ları otomatik tamamlanmış sayan kurtarma yolları kaldırıldı. Migration/schema uyuşmazlığı başlangıçta açıkça hata verir. Eski müşteri fixture’ı, veri/tenant/mali parity ve rollback kabulü bu ortamda yok; A-18 **🟡** kaldı.
- **A-20:** Üretim Web/Shared C#/Razor kaynaklarında `DateTime.Now` kalmadı; yalnız test seeder’da 28 sentetik veri kullanımı var. Olay/audit zamanları UTC, iş günü/form/takvim kuralları İstanbul `BusinessTime`; UTC yedek anı yerel planlama/gösterimle doğru çevriliyor. Geçmiş DB timestamp’lerine toplu saat dönüşümü uygulanmadı. A-20 **🟢 kod teslimi** kapandı.
- Web Release derlemesi **0 uyarı / 0 hata**, `git diff --check` temiz; otomatik test çalıştırılmadı. Görev toplamı **30 yeşil / 1 sarı / 0 kırmızı / 0 beyaz**. A-18 geçiş kabulü ve genel satış Go/No-Go hâlâ açık.

### 2026-10-10 — A-18 watermark parity koruması

- SQLite legacy watermark artık hedef modeldeki tablo/kolonlarla birlikte indeks adlarını ve FK principal/from/to kolon eşleşmelerini katalogdan doğrular; eksik öğede history tablosu oluşturulmaz. PostgreSQL hata sonrası history’yi ilerleten recovery yolları da kaldırılmıştır.
- Bu, yarım şema için fail-fast korumasıdır; eski müşteri DB yükseltmesi, finans/tenant veri parity’si ve rollback kabulinin yerine geçmez. Temsilî fixture mevcut değil, A-18 **🟡** kalır. A-20 üretim kaynaklarında `DateTime.Now` taraması **0** ve kod teslimi **🟢**. Toplam **30 yeşil / 1 sarı / 0 kırmızı / 0 beyaz**.

### 2026-10-10 — saat dilimi gösterimi ve A-18 atomiklik

- A-18 PostgreSQL audit timestamp türü uyarlaması tek transaction’a alındı; hata olursa tüm kolon değişiklikleri rollback olur. SQLite eski baseline guard tablo/kolon, indeks adı ve FK eşleşmesini kontrol eder. Eski müşteri DB fixture/rollback kabulü olmadığı için A-18 sarı.
- A-20 taraması `DateTime.Now`, `DateTime.Today` üretim kullanımı ve `ToLocalTime()` dönüşümünü sıfır buldu. UTC olay damgaları `BusinessTime.LocalAt` ile İstanbul’da gösterilir; muhasebe ve rezervasyon wall-clock tarihleri kaydırılmaz. A-20 yeşil kod teslimi.
- Web Release derlemesi **0 uyarı / 0 hata**; otomatik test çalıştırılmadı. Görev dağılımı **30 yeşil / 1 sarı**.

### 2026-10-10 — A-18 migration sırası düzeltmesi

- Program başlangıcındaki EF model bazlı genel `EnsureAllColumnsExistAsync` çağrısı kaldırıldı; eski şemaya migration'lardan önce kolon ekleyerek migration DDL'iyle çakışma ve sahte kısmi yükseltme oluşturma yolu kapatıldı. Migration geçmişini pending ID'lerle yapay dolduran kullanılmayan helper da `SchemaSyncHelper`'dan çıkarıldı.
- `FisNoCounters` eski şema uyumluluğu EF migration/initializer sonrasına taşındı. Uyarlama başarısızsa startup artık hatayı yutmaz ve devam etmez.
- Web Release derlemesi **0 uyarı / 0 hata**. Otomatik test çalıştırılmadı. Gerçek/maskeli legacy PostgreSQL ve SQLite fixture, mali/tenant parity ve yarım migration rollback kanıtı olmadığı için A-18 **🟡**; görev toplamı **30 yeşil / 1 sarı**.
- Ek A-18 bulgusu: `20260326224724_AracSasePlakaYapisi` önce `Araclar.Plaka` kolonunu siliyor, eski plaka aktarım yardımcısı ise MigrateAsync sonrasına kaldığından çalıştığında kaynak veri artık yoktu. Migration artık `SaseNo` değerini indeks öncesi eski plakadan tamamlıyor; `AktifPlaka` ve `AracPlakalar` kayıtlarını yazdıktan sonra legacy kolonu düşürüyor (PostgreSQL/SQLite). Post-migration legacy düzeltme kodu migration ID/history yazmıyor.

### 2026-10-10 — A-18 migration sonrası şema parity kapısı

- PostgreSQL'de migration sonrasında modeli yakalamak için eksik kolonları elle ekleyen initializer yolu devreden çıkarıldı. Migration öncesi genel kolon eşitlemesi de kaldırılmış durumda.
- Migration tamamlandıktan sonra PostgreSQL ve SQLite gerçek tablo/kolon/indeks/FK yapısı EF modelinin imzalarıyla karşılaştırılır; mismatch ve artık modelde bulunmayan `Araclar.Plaka` kolonu varsa uygulama başlangıcı açık hata verir, DDL yamasıyla gizlemez.
- Web Release derlemesi **0 uyarı / 0 hata**. Bu turda otomatik test/müşteri DB çalıştırılmadı. Eski DB fixture, mali/tenant kayıt parity'si ve yarım migration/rollback kanıtı hâlâ yok; A-18 **🟡**, toplam **30 yeşil / 1 sarı**.

### 2026-10-10 — A-18 SQLite tarihsel veri migration güvenliği

- Statik tarama, legacy SQLite watermarkından önceki FirmaId backfillleri, organizasyon seedleri, hakediş puantaj duplicate pasifleştirmesi ve plaka/şase veri taşıması gibi DML içeren migrationlar buldu. Watermark TargetModel şema paritysi, bu veri işlemlerinin geçmişte gerçekten çalıştığını kanıtlamaz.
- EnsureSqliteMigrationHistoryAsync artık mevcut uygulama tablolarına rağmen __EFMigrationsHistory yoksa veya watermark öncesi migration geçmişinde boşluk varsa otomatik history/baseline yazmadan hata verir. Böylece veri dönüşümleri sessizce atlanmaz. Web Release derlemesi 0 uyarı / 0 hata; otomatik test çalıştırılmadı.
- Sonuç: Migration geçmişi yok/eksik eski SQLite kurulumları yeni sürümde açılmaz; yetkili yedek/işlem kaydıyla geçmişin ve veri paritysinin doğrulandığı kontrollü geçiş prosedürü henüz yok. Bu nedenle A-18 kırmızı ve görev dağılımı 30 yeşil / 0 sarı / 1 kırmızı / 0 beyaz; satış Go/No-Go kapalı.
- Tenant FirmaId açılış backfill'i de tek transaction'a alındı. Tablo/kolon veya güncelleme hataları artık sessizce yutulmaz; işlem tamamen geri alınır ve zorunlu startup başarısız olur. Release build 0 uyarı / 0 hata.### 2026-10-10 — A-18 SQLite migration sağlayıcı uyumluluğu

Static migration taramasında SQLite için sağlayıcı dalı bulunmayan PL/pgSQL DO blokları saptandı. Örnekler: 20260326204037_CRMModulu, 20260409091451_AddBudgetHedef, 20260513140012_FixCariFirmaShadowFK, 20260517212717_TenantZ1_DropLegacyCariFaturaSirketColumns, 20260518140619_TenantB3i_DropSirketNavigationAndEntity, 20260518195552_TenantB4a_DropSirketIdColumnsAndRenameAuditLog, 20260518200342_TenantB4b_DropLegacyTables, 20260615192539_AddPersonelBankaOdemeAlanlari ve 20260616074934_AddHakedisPuantajFaturaFKs. Bunlar migration geçmişi eksik SQLite'ta körlemesine zinciri çalıştırmanın güvenli olmadığını; önce SQLite-native geçiş/adoption yolu gerektiğini gösterir.

A-18'i kapatmak için bu geçişlere sağlayıcıya uygun SQLite yolu ve history'siz legacy DB için açık, yedekli adoption akışı gerekir. Gerçek legacy fixture olmadan otomatik baseline'a izin verilmez; görev kırmızı kalır.

### 2026-10-10 — A-18 SQLite legacy adoption kod yolu (sonraki notta devre dışı bırakıldı)

İlk uygulamada history tablosu olmayan eski SQLite için watermark parity/reconciliation sonrası otomatik baseline yolu eklenmişti. Sonraki kaynak incelemesi, sınırlı reconciliation'ın watermark öncesindeki bütün tarihsel DML adımlarını kapsamadığını gösterdi; bu yol aşağıdaki güncellemede devre dışı bırakıldı.

Web Release derlemesi bu kod yoluyla **0 uyarı / 0 hata** verdi. Gerçek/eski SQLite fixture üzerinde çalışma, veri parity/rollback ve PostgreSQL eski DB kabulü yapılmadı; A-18 **sarı** ve satış Go/No-Go açık.

### 2026-10-10 — A-18 kanıtsız SQLite baseline devre dışı bırakıldı

- History'siz veya watermark öncesi eksik geçmişli mevcut SQLite DB için otomatik baseline/adoption yolu artık çalıştırılmaz; initializer veri geçmişi yalnız şema imzasından çıkarılamadığı için başlangıcı fail-fast durdurur. Konfigürasyon bayrağıyla bu korumayı aşma yolu yoktur.
- Önceki adoption kod yolu güvenli değildi: watermark öncesindeki tüm veri dönüşümlerinin çalıştığı kanıtlanmadan migration ID yazabilirdi. History'siz SQLite geçişi yeniden açılmadan önce tüm veri migration'ları için SQLite-native, tekrar çalıştırılabilir dönüşüm ve fixture tabanlı parity/rollback kanıtı gerekir. PostgreSQL eski DB kabulü de açık kalır. A-18 🟡; toplam görev renkleri **30 yeşil / 1 sarı / 0 kırmızı / 0 beyaz**.
- Web Release derlemesi **0 uyarı / 0 hata**; test çalıştırılmadı.
