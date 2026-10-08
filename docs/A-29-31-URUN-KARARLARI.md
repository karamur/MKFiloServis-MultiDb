# A-29 / A-30 / A-31 — Ürün kararları

**Karar tarihi:** 2026-10-08  
**Kapsam:** Satışa sunulan web uygulamasının mali davranışı, refactor önceliği ve çalışma/depolama modeli.

## A-29 — Mali işlem kuralları

1. **Banka aktarımında cari eşleştirmesi isteğe bağlıdır.** Cari belirlenemeyen banka satırı, kullanıcı açıkça seçip yansıtabilir; uygulama bunu kendiliğinden muhasebeleştirmez. Muhasebe fişi için ayrı, yetkili muhasebe adımı gerekir.
2. **Tutar yönü açık olmalıdır.** Tek bir imzalı Tutar sütununda artı giriş, eksi çıkıştır. Giriş/Çıkış sütunları varsa bunlardan yalnız biri pozitif olabilir; ikisi birden pozitifse veya imzalı toplamla tutar/yön çelişirse satır reddedilir. Eksi tutar `Abs` uygulanarak sessizce başka yöne çevrilmez.
3. **Maaş snapshot'ı değişmez kayıttır.** Kilitli, normal fişe bağlanmış veya ters fiş kaydı bulunan dönem silinemez. Düzeltme ayrı ters fiş/mahsup ve yeni kayıt akışıyla yapılır. Yalnız kilitsiz, fişsiz taslak soft-delete edilebilir.
4. **Rol değişikliği yazma anında geçerli olmalıdır.** Güvenlik hassas mali yazımlarda yetki, istemci/circuit başlangıcında alınmış role güvenmek yerine sunucu yazım sınırında doğrulanmalıdır. Oturumun güncel rolü doğrulanamıyorsa işlem reddedilir.

5. **Toplu muhasebe geri alma yalnız taslak fiş içindir.** Onaylı veya iptal edilmiş fiş toplu silinemez. Taslak fiş soft-delete edilir; kalemler korunur. Toplu üretim/geri alma her satırda güncel muhasebe izni arar; izin kaybında kalan satırlar işlenmez. Başarılı önceki satırlar korunur. Fiş onayında kalemlerden hesaplanan borç/alacak dengesi doğrulanır.

**Uygulama:** Çift yönlü banka satırları ve tutar/yön çelişkileri içe aktarma hatası sayılır; kilitli ya da muhasebe fişine bağlı maaş snapshot silme reddedilir. 2026-10-08'de yetki listesi sorguları oturumdaki kullanıcı/rol nesnesini yetki kaynağı olarak kullanmayacak şekilde düzeltildi: etkin hesap ve güncel rol/yetkiler sorgu anında DB'den okunur; pasif/silinmiş kullanıcı ve silinmiş rol/yetki reddedilir. Banka hareketi oluşturma/düzenleme/silme, aktarım yansıtma, personele geri ödeme/iptal ve gelir-gider UI yazımları; maaş, ödeme, avans ve personel borcu yazımları işlem anında güncel yetkiyi sorgular. Maaş ekranı otomatik snapshot oluştururken `MaasYaz`, muhasebe fişi seçilmiş gelir-gider işleminde `MuhasebeFisleriYaz` izni gerekir. Fatura oluşturma/düzenleme/silme, içe aktarma, PDF değiştirme, kalem güncelleme, eşleştirme/mahsup ve açık muhasebeleştirme girişleri artık `CurrentPermissionGuard` ile servis sınırında güncel DB izni arar. Doğrudan `MuhasebeService.CreateFisAsync` de korunur. Fiş düzenleme/silme/onay, hesap planı düzenleme/silme, araç masrafı ve kolay muhasebe kaydetme servisleri de güncel izin ister. Masrafta bağlı fiş işleminin muhasebe izni ilk masraf yazımından önce denetlenir. Manuel oluşturma, düzenleme ve atomik fiş üretme girişlerinde kalemlerden hesaplanan borç/alacak dengesi denetlenir. Kalan otomatik/atomik çağrı sözleşmeleri, hesap oluşturma, türetilmiş ödeme toplamı ve puantaj/kalan mali servis/API sınırları ile rol değişimi çalışma zamanı kabulü henüz kapanmadı; A-29 🟡 kalır.

## A-30 — Refactor kapsamı

Satış sürümünde davranışı değiştirmeyen geniş çaplı refactor yapılmayacak. Listeye alınmış introspection, seed, DTO/render tekrarları ve eski context temizliği P3 teknik borç olarak ertelendi. Güvenlik, veri kaybı, tenant izolasyonu veya düzeltme gerektiren belirli bir hata görülürse ilgili dar kod alanı ayrı hata düzeltmesi olarak ele alınır. Eski bağlam dosyaları ancak sahiplik ve migration uyumluluğu kanıtlandıktan sonra kaldırılır.

**Sonuç:** Bu sürümde refactor yapılmaması kararı verildi; A-30 kapsam kararı 🟢 kapandı. P3 backlog maddeleri kapanmış hata veya tamamlanmış refactor olarak sunulmaz.

## A-31 — Çevrimdışı ve dosya depolama kapsamı

1. **Çevrimdışı kullanım desteklenmez.** Web istemcisi uygulama sunucusu ve ağ erişimi ister. Tarayıcı yerel veritabanı veya daha sonra eşitleme vaadi satış metnine konmaz.
2. **Yerel nesne deposu** tek uygulama düğümlü kurulum için desteklenir. Depolama kökü kalıcı disk üzerinde ve uygulama hesabınca erişilebilir olmalıdır.
3. **S3 uyumlu nesne deposu** harici depolama olarak yapılandırılabilir; çok düğümlü kullanım ancak tüm düğümlerin aynı bucket/anahtar alanına ve gerekli erişim kimliklerine sahip olduğu kurulumda mümkündür. Sağlayıcıya özgü izin, imza ve kullanılabilirlik kabulü A-11'de kalır.
4. **Yedek ve saklama:** Veritabanı yedeği tek başına dosya kurtarma değildir. Kurtarma arşivi şifreli nesneleri, manifest/hash verisini ve çözme için gerekli key ring malzemesini birlikte kapsamalıdır. Tanılama logları muhasebe/audit kayıtlarının yerine geçmez; saklama süresi dağıtım sözleşmesi ve kurum politikasıyla belirlenir.

**Sonuç:** Çevrimdışı, yerel ve S3 işletim sınırları belgelenmiştir; A-31 kapsam kararı 🟢 kapandı. Gerçek S3, çok sunucu ve restore kabul testleri A-10/A-11/A-04'te açık kalır.


### 2026-10-08 — Hesap planı ve hızlı oluşturma izinleri

Kullanıcı hesap planı hazırlığı yazma izni, Excel yükleme yazma ve düzenleme izinleri ister. Başlangıç seed iç metottur. Hızlı cari/stok oluşturma kendi yazma izinlerini arar; cari unvan eşleştirmesi aktif firma filtresini korur. Testler kullanıcı kararıyla [son aşamaya](SATISA-CIKARIM-SON-ASAMA-TEST-PLANI.md) bırakıldı; doğrudan otomatik/atomik muhasebe ve diğer mali servis girişleri henüz kapanmadı.


### 2026-10-08 — Personel finans yazımları

Personel finans servisinin yazım girişleri güncel maaş izinlerini; kalıcı borç silme özel `PersonelBorcSil` iznini arar. Bağlı fiş kaldırma ayrıca muhasebe silme izni ve taslak durumu ister; kalemler korunur. Ödeme/mahsup tutarı pozitif olmalıdır. Çok context ile otomatik fiş/link üretimi ve bağlı fişli kayıt düzenleme/iptal tutarlılığı halen açıktır. Son aşama testleri henüz çalıştırılmadı.


### 2026-10-08 — Bağlı fişli kayıt düzenleme ve iptal

Avans/borç tutarı pozitif ve işlenmiş bakiyeden küçük olmayan değerdir. Bağlı fiş veya işlenmiş bakiye bulunan kaydın mali alanları değiştirilemez; iptal edilmiş kayıt düzenlenemez. Aktif ödeme/mahsup varsa doğrudan iptal reddedilir. İşlem görmemiş kaydın iptali bağlı taslak fişin soft-delete işlemiyle tek SaveChanges içinde yapılır; onaylı fişte reddedilir. Mahsup geri alma bağlı taslak fişi de aynı transaction'da kaldırır. İptal edilmiş kayda yeni ödeme/mahsup eklenemez. Eşzamanlılık, otomatik fiş üretiminin transaction kapsamı ve geçmiş veri onarımı açık; nihai testler bekliyor.


### 2026-10-08 — Personel otomatik fiş transaction sınırı

Avans/borç oluşturma, borç ödeme ve tekil mahsupta ana kayıt/bakiye ile otomatik fiş ve fiş bağlantısı aynı context ve Serializable transaction içinde yazılır. Yardımcılar kendi context'lerini açmaz. Her execution strategy girişimi yeni context, güncel izin ve sıfırlanmış kayıt/fiş kimlikleriyle başlar. Commit sonucu belirsizliği/idempotency ve eşzamanlılık kabulü son aşamada doğrulanacak; diğer mali işlem zincirleri bu düzeltmeyle kapanmadı.


### 2026-10-08 — Belirsiz commit sonucu

Personel transaction'ında commit sonrası hata yeni mali yazımla tekrar edilmez. Ayrı context'te ana kayıt ve bağlantı doğrulanırsa başarı döner; doğrulanamıyorsa kayıt numarası korunarak sonuç belirsiz bildirilir. Kimliği bulunan nesne yeniden oluşturulamaz. Yeni istek/nesne ve farklı sunucu için kalıcı idempotency henüz sağlanmadı; nihai testler bekliyor.


### 2026-10-08 — Kalıcı ödeme/mahsup tekrar gönderimi

Ödeme ve mahsupta aynı mantıksal istek aynı `IslemKimligi` ile tekrar gönderilir. Aynı içerik mevcut sonucu döndürür; farklı içerik veya kaldırılmış işlem reddedilir. İndeks silinmiş işlemlerin kimliğini de tutar; ödeme geçmişi bulunan borç kalıcı silinemez. İstemci yeni anahtar üretirse ayrı işlem sayılır. Kalıcı koruma için migration gerekir; henüz uygulanmadı ve test edilmedi. Avans/borç oluşturma ve diğer mali zincirler ayrı açık kapsamdır.


### 2026-10-08 — Avans/borç oluşturma tekrarı ve kaldırma

Avans/borç oluşturma da aynı işlem kimliği ve istek özetiyle kalıcı tekrar korumasındadır. Tek seçili firma gerekir; firma işlem özetine bağlanır. İptal/silinmiş kimlik yeniden kullanılamaz. Yeni anahtarlı borç kaldırıldığında fiziksel silinmez; tüketilmiş kimlik korunur. Eski anahtarsız, ödeme geçmişsiz kaydın fiziksel kaldırılması mevcut sözleşmede kalır. Yeni migration henüz uygulanmadı; derleme ve kabul son aşamada.


### 2026-10-08 — A-09/A-29 maaşa otomatik mahsup ve geri alma

- 🟢 Otomatik avans mahsubu maaş/avans bakiyeleri ve mahsup satırlarıyla aynı Serializable transaction içinde yürütülür. Maaş kimliğine bağlı kalıcı parti özeti ve avans bazında benzersiz işlem anahtarı kullanılır; tekrar çağrı mevcut parti toplamını döndürür. Yeni açıklama/tarih veya yeni açık avans ikinci otomatik parti açmaz. Avanslar aynı firma/personelle sınırlı, tarih ve kimlik sırasıyla işlenir.
- 🟢 Maaşa bağlı tekil mahsup firma/personel/ödeme durumu ve maaş kapasitesini denetler; maaşın Avans kesintisini aynı transaction içinde artırır. Mahsup kaldırma maaş kesintisini, avans bakiyesini, soft-delete kaydını ve varsa bağlı taslak fiş kaldırmayı aynı transaction içinde yürütür. Ödenmiş maaş, onaylı fiş veya tutarsız bakiye geri almayı reddeder.
- 🟢 Otomatik parti kısmen/tamamen geri alınırsa tekrar uygulanmaz; kayıtlar ve tüketilmiş anahtarlar korunur. Önceden başka/eski mahsup geçmişi olan maaşta yeni otomatik parti reddedilir. Gerekli yeni kesinti tekil mahsup akışından yapılmalıdır. Ekran bildirimi mevcut parti toplamını gösterir; tekrar çağrıyı yeni kesinti gibi sunmaz.
- 🟡 Önceki iki işlem anahtarı migration'ı gereklidir; bu oturumda uygulanmadı. Eski tekil mahsuplar maaş kesintisini güncellememiş olabilir; tutarsız eski kayıtlar otomatik düzeltilmez. Otomatik maaş mahsubunun muhasebe hesabı sözleşmesi ayrıca değerlendirilmelidir; nakit tahsilat fişi bu akışa eklenmedi.
- 🟡 Derleme/test/migration provası son aşamada; eşzamanlı iki sunucu, yanıt kaybı, geri alma, eski veri ve hedef sağlayıcı senaryoları henüz çalıştırılmadı. A-09/A-29 genel durumları sarı kalır; satış onayı verilmiş değildir.


### Maaş düzenleme ve ödeme sınırı — 2026-10-08

Maaş servisinde güncel izin ve Serializable yazım sınırı uygulanır. Mahsup kesintisi mahsup akışından yönetilir; ödeme/mahsup geçmişli maaş kaldırılmaz veya toplu yeniden hesaplanmaz. Ödenmiş maaşın hesap alanları kilitlidir. Ödeme ekranı durum, tarih ve açıklamayı atomik işaretler; banka/kasa hareketi üretilmiş sayılmaz. Ödeme iptali aynı hesap değerlerini koruyarak durum işaretini kaldırır. Nihai eşzamanlılık ve sağlayıcı testleri beklediğinden A-09/A-29 kapanmaz.


### Banka/kasa genel düzenleme sınırı — 2026-10-08

Bağlı muhasebe, bütçe, fatura eşleştirmesi, transfer/mahsup, araç masrafı ve personel geri ödeme kayıtları genel hareket düzenleme/kaldırmadan değiştirilemez. Bağlantısız kaldırma soft-delete ile geçmişi korur. Hareket geçmişli hesapta para birimi, hesap tipi ve açılış bakiyesi kilitlidir. Banka/kasa ve hesap yazım izinleri servis sınırında taze sorgulanır. Genel hareket düzenleme/kaldırma ve hesap düzenleme/kaldırma/firma ataması Serializable transaction içindedir. Transfer/cari mahsup ve özel iptal/geri ödeme mali zincirleri halen ayrıca tamamlanmalıdır; A-09/A-29 sarı kalır.


### Transfer/cari mahsup kayıt sınırı — 2026-10-08

Hareket ve zorunlu muhasebe fişi aynı context/transaction içinde kaydedilir; fiş üretim hatası yutulmaz. Eksik muhasebe hesap eşleştirmesi işlemi reddeder. Transfer yalnız aynı firma/para birimindeki aktif hesaplarda, cari mahsup aynı firma cari/hesabında yapılır; tüm firmalar modu yazım için reddedilir. Hareket numarası aynı transaction bağlantısından üretilir. Mevcut ortak muhasebe hesap/fiş kapsamı değişmedi. Yeni isteğin kalıcı tekrar kimliği ve iptal/ters kayıt zinciri henüz ayrı açık iştir; A-09/A-29 sarıdır.

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
