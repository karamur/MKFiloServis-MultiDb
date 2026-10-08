# Satışa çıkarım — son aşama test planı

**Tarih:** 2026-10-08  
**Kullanıcı kararı:** Testler kalan kod düzeltmeleri ve rapor düzenlemeleri bittikten sonra toplu yapılacak.

## Uygulama sırası

1. **Kod düzeltmeleri:** Görev envanterindeki açık uygulama işlerini tamamla; mali servis yetkileri, tenant ilişkileri, dosya yaşam döngüsü ve kurtarma yollarını ele al. Bu aşamada test çalıştırma.
2. **Kapsam ve rapor:** Tamamlanan kodu, kalan veri/dağıtım işlerini ve gerekli test ortamlarını görev satırlarına işle. Çalıştırılmamış kabul için yeşil sonuç yazma.
3. **Son aşama doğrulama:** Nihai kod üzerinde Release derlemesi, kalıcı regresyon testleri ve tarayıcı kabulünü toplu çalıştır. Hata bulunursa ilgili kodu düzelt ve etkilenen testleri yeniden çalıştır.
4. **Dağıtım kabulü:** Kullanılabilir hedef ortamlarda temiz kurulum/güncelleme, DB+dosya restore/rollback, lisans geçişi ve dış entegrasyonları doğrula. Ortam/credential/veri bulunmayan senaryoları çalıştırılmadı olarak kaydet.

## Nihai test kapsamı

| Alan | Senaryolar |
|---|---|
| Mali izinler | Normal/Admin, pasif/silinmiş hesap, silinmiş rol/izin; açık oturumda rol kaldırma; HTTP ve circuit; uzun toplu işlem sırasında izin kaybı |
| Muhasebe | Taslak/onaylı/iptal fiş; toplu geri alma; kalemlerin korunması; borç/alacak dengesi; başarısız satırın sonraki satıra taşınmaması |
| Personel transaction | Avans/borç oluşturma, borç ödeme ve mahsup: kayıt/fiş/link yazım hataları; tek transaction rollback; Serializable eşzamanlılık; execution strategy retry; commit sonucu belirsizliği ve tekrar çağrıda mükerrer kayıt |
| Personel finans | Avans/borç/ödeme/mahsup yazım izinleri; kalıcı borç silme yetkisi; bağlı fişte silme izni ve taslak durumu; kalem koruma; sıfır/negatif tutar; firma kapsamı; otomatik fiş üretme hatası; bağlı fişli mali alan düzenleme; ödeme/mahsup sonrası iptal; tekrarlı iptal; mahsup fişinin aynı transaction içinde kaldırılması; iptal kayda yeni ödeme/mahsup engeli; eşzamanlı işlemler |
| Hesap/cari/stok | Başlangıç hesap hazırlığı ile kullanıcı seed ayrımı; Excel için yazma+düzenleme izinleri; hızlı oluşturma izinleri; aynı unvanlı farklı firma carileri |
| Lisans/oturum | İmzalı modül/sürüm/makine hakları; normal/Admin erişimi; çıkış/iptal ve tenant izolasyonu |
| Veri/dosyalar | SQLite/PostgreSQL ilişki denetimi; eşzamanlı yazım/cleanup; soft-delete ve geri alma; migration; hedef veri hacmi |
| Kurtarma/kurulum | Gerçek izole DB ve belge restore; kesinti/rollback; farklı profil/key ring; desteklenen sağlayıcılarda temiz hedef kurulum |
| Rapor/entegrasyon | Çok sayfa/uzun metin/negatif tutar ve toplamlar; Excel/PDF; S3/SMTP/Luca için uygun test ortamı |

## Sonuç kaydı

Her çalıştırmada kod sürümü, komut/senaryo, ortam, tarih, PASS/FAIL/çalıştırılmadı ve kanıt konumu kaydedilecek. Önceki 42/42 sonucu yeni kodun testi olarak sunulmayacak. Görev renkleri bu nihai sonuç ve görev kapanış ölçütüne göre güncellenecek.


### Personel commit belirsizliği doğrulaması

- Commit öncesi hata: transaction geri dönüşü, taze context ile yeniden deneme ve girdi kimlik/navigation temizliği.
- Commit başarılı, yanıt kayıp: bağımsız doğrulama ile tek kayıt ve aynı sonuç.
- Commit sonucu bilinmiyor, DB erişilemiyor: yeniden yazma yapılmaması, kayıt numaralı belirsiz sonuç ve aynı nesnenin yeniden oluşturulmasının reddi.
- Kayıt görünmüyor veya zaman hassasiyeti/filtre nedeniyle doğrulanamıyor: başarı varsayılmaması, otomatik tekrar INSERT olmaması.
- Yeni nesne/yeni istek veya farklı sunucuda tekrar gönderim: kalıcı idempotency kapsamının ayrıca değerlendirilmesi; mevcut koruma bu senaryoyu kapatmaz.


### Kalıcı ödeme/mahsup işlem kimliği

1. PostgreSQL/SQLite temiz ve mevcut şemaya yeni migration; eski NULL anahtarlar ve nullable benzersiz indeks.
2. Aynı anahtar/içerik ile aynı nesne, yeni nesne, yeni oturum ve bağımsız sunucu tekrarında tek kayıt/tek fiş/tek bakiye değişikliği.
3. Aynı anahtarla farklı tutar/tarih/ana kayıt/ödeme şekli/muhasebe tercihi: ret.
4. Eşzamanlı iki istek ve benzersiz indeks yarışında mevcut sonucu alma; commit sonrası yanıt kaybında tekrar sorgu.
5. Soft-delete sonrası anahtarın tekrar kullanımının reddi; silinmiş ödeme geçmişi bulunan borcun kalıcı silinememesi.
6. İzin kaldırma, farklı firma, eksik/geçersiz anahtar ve DB erişim hatası: başarı varsayılmaması.
7. Farklı anahtar taşıyan istek yeni işlem sayılır; istemcinin yeniden gönderimde aynı kimliği koruduğu kontrol edilmeli.

Migration bu çalışma sırasında uygulanmadı; bu senaryolar henüz çalıştırılmadı. Avans/borç oluşturma ve diğer mali zincirlerin kalıcı idempotency kapsamı ayrıca açıktır.


### Kalıcı avans/borç oluşturma anahtarı

Yeni oluşturma migration'ını ödeme/mahsup migration'ı sonrasında temiz ve mevcut PostgreSQL/SQLite şemalarında doğrula. Aynı anahtarın yeni nesne/oturum/sunucu üzerinden aynı içerikle tekrarında tek ana kayıt ve fiş; farklı içerik, iptal ve soft-delete sonrası ret beklenir. Seçili firma değişimi, tüm firmalar modu, NULL eski anahtarlar, anahtarlı borç kaldırmada geçmişin korunması ve yetki kaldırma senaryolarını dahil et. Migration'lar bu oturumda uygulanmadı; bu testler çalıştırılmadı.


### Maaşa otomatik avans mahsubu ve geri alma

Aynı maaşa art arda/yeni oturum/iki sunucudan çağrı; sonradan yeni avans ekleme ve tarih/açıklama değiştirme: tek otomatik parti beklenir. İlk/ara satır hatasında tüm bakiye ve satırlar geri dönmeli; commit sonrası yanıt kaybında mevcut toplam bulunmalıdır. Kısmi/tam geri alma sonrası otomatik yeniden çağrı reddedilmelidir. Tekil mahsupta maaş kapasitesi, firma/personel uyuşmazlığı ve ödenmiş maaş retleri; kaldırmada maaş Avans kesintisinin geri dönmesi, taslak/onaylı fiş ve tutarsız eski kayıt retleri kontrol edilmelidir. Önceki anahtar migration'ları olmadan yeni kod çalıştırılmamalı. Bu senaryolar henüz çalıştırılmadı.


### Maaş servis yazımları

- İzin kaldırma/rol değişimi sonrası doğrudan servis üzerinden oluşturma, toplu oluşturma, düzenleme, ödeme, kaldırma ve yeniden hesaplama reddi.
- Eski UpdatedAt ile ekran kaydetme; eşzamanlı mahsup/maaş düzenleme/ödeme; kesintinin ezilmemesi ve kapasitenin aşılmaması.
- Firma/personel/dönem/audit alanı değiştirme ve ödeme durumunu düzenleme yoluyla atlama reddi; ödeme iptalinde mali alanların korunması.
- Aynı ödeme tarihi/açıklaması tekrarında tek durum değişimi; farklı içerik reddi ve açıklama kaydı hata verirse tüm ödemenin geri dönmesi.
- Ödeme veya silinmiş/aktif mahsup geçmişli maaşın kaldırma/yeniden hesaplama reddi; karma seçimde kısmi değişiklik olmaması; kaldırılabilir maaşın soft-delete olması.
- PostgreSQL/SQLite eşzamanlı dönem oluşturma, precommit retry ve commit sonrası bağlantı hatasında yeniden yazılmama; sonuç belirsizse liste üzerinden kontrol.

Bu senaryolar henüz çalıştırılmadı; derleme ve migration provası son aşamadadır.


### Banka/kasa genel hareket ve hesap koruması

1. Servis üzerinden rol/izin kaldırma sonrası hareket, transfer, cari mahsup, personel geri ödeme ve iptal; hesap oluşturma/düzenleme/kaldırma/firma ataması retleri.
2. Eski UpdatedAt, silinme/kaynak/mahsubun bağlantısı/geri ödeme alanı değiştirme ve yeni kayda işlenmiş alan taşıma retleri.
3. Fiş, fatura eşleştirmesi, bütçe ödemesi, transfer, araç masrafı veya personel geri ödeme bağlantılı hareketin genel güncelleme/kaldırma reddi; bağlantısız kaldırmanın soft-delete olması ve mevcut geçmişi silmemesi.
4. Rent a Car senkron güncelleme/kaldırma; kaynak iş akışlarının yeni korumayla uyumluluğu.
5. Hareket geçmişli hesapta tip/para birimi/açılış bakiyesi değiştirme; firmasız hesap seçili firmaya atama, başka firmalı hesabı taşıma ve hareket firması uyuşmazlığı retleri.
6. PostgreSQL/SQLite eşzamanlı bağlantı oluşturma ve genel hareket/hesap düzenleme, precommit retry, commit sonucu belirsizken otomatik yeniden yazılmama.

Henüz çalıştırılmadı. Özel transfer/cari mahsup ve ortak personel geri ödeme iptal zincirleri ayrıca açık uygulama işi olarak kalır.


### Transfer/cari mahsup atomik kayıt

PostgreSQL/SQLite üzerinde giriş/çıkış satırı, karşı hareket FK, fiş kalemi/numarası ve son fiş FK kaydı aşamalarında hata enjekte edilerek tam rollback kontrol edilmeli. Eksik muhasebe hesabı, farklı firma/para birimi, tüm firmalar modu, yetki kaldırma ve eşzamanlı kaynak bakiye tüketiminde ret doğrulanmalı. Sayaç RETURNING ve mevcut transaction bağlantısı, SQLite kilitlenme davranışı ve transaction rollback sonrası sayaç kontrol edilmeli. Aynı kalıcı hareketin fiş helper'ı tekrarında tek fiş; yeni transfer isteğinde ayrı hareket oluştuğu ve kalıcı istek anahtarının henüz olmadığını gösteren senaryo eklenmeli. Commit sonrası yanıt kaybında otomatik yeniden yazılmama ve liste kontrolü; cari alt hesap oluşumu/üst hesap değişikliğinin fiş hatasında geri dönmesi kontrol edilmeli. Bu senaryolar henüz çalıştırılmadı.


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

### Yeni son-aşama kanıtı — 2026-10-08 SQLite dashboard finans hatası

🟢 Tam Release build 0 hata/uyarı ve otomatik paket 102/102 geçti. Üç izole SQLite bootstrap testi, eski migration geçmişinde yeni DDL'nin atlanmamasını, eski DB'de eksik banka işlem anahtarı kolonlarının veri koruyarak/idempotent onarılmasını ve onarım sonrası dashboard son hareket sorgusunu doğrular. A-15 için dört uygulama katmanı ve altı SQLite migration testi de geçti: migration preflight ve DB seviyesinde çapraz-firma eşleştirme/bağlı uç firma değişikliği korumaları doğrulandı.

🟡 Gerçek müşteri veritabanında migration/onarım, dashboard kullanıcı kabulü, restore ve gerçek veri hacmi ölçümü yapılmadı. A-15 migration'ı izole PostgreSQL 17 cluster'ında çalıştı; bu müşteri/çok sunucu kabulü değildir. Müşteri DB'si yerel testlerde kullanılmadı; saha onayı olmadan bu görevler yeşil kapatılmaz. Ayrıntı: `TEST-DOGRULAMA-2026-10-08.md`.
