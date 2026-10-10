# A-06 — tarihsel sırların kapanış kararı ve koşullu JWT rotasyonu

**Tarih:** 2026-10-10
**Kapsam:** `Jwt__Secret` JWT imza sırrı. DB, e-posta, SMS, S3 ve lisans imza anahtarlarının rotasyonu kendi sağlayıcı/işletim prosedürlerine tabidir.

## Kaynak düzeltmesi

- JWT secret doğrulaması tek `JwtSecretPolicy` içinde toplandı; host başlangıcı ve token üretimi aynı kurala bağlıdır.
- İmza anahtarı, issuer/audience ve parola damgası tek `JwtSigningConfiguration` örneğinde başlangıçta sabitlenir. Çalışma sırasında yapılandırma değişse bile yeni token ile doğrulama anahtarı aynı süreçte ayrışmaz; rotasyon için koordineli yeniden başlatma gerekir.
- Production başlangıcı etkin `Jwt:Secret` değerini yalnız ortam değişkeni sağlayıcısından alır. Ortam değişkeni eski JSON/komut satırı değerini gölgelese bile dolu alternatif kaynaklar başlangıcı durdurur; hedefte eski sır dosyada bırakılamaz. Değer korumalı secret deposundan ortama aktarılmalıdır. Sağlayıcı denetimi tek başına vault yetkisini veya gerçek sır rotasyonunu kanıtlamaz.
- Boş, `REPLACE_` yer tutuculu ve 32 UTF-8 baytın altındaki secret reddedilir. Yerel Git geçmişindeki dört ayrı JWT sırrının SHA-256 parmak izi engel listesindedir; eski değerler koda veya teste tekrar konmaz.
- Development ortamında geçici rastgele secret uygulama süresince kullanılır; Production'da secret yoksa uygulama başlamaz. Bu nedenle Development JWT'si yeniden başlatma sonrası geçersizdir.
- Web publish ortama özel `appsettings.*.json` dosyalarını ve çalışma zamanı `dbsettings.json` dosyasını dışlar. Ana/güncelleme kurucuları bunları ayrıca dışlar; `setup/build.ps1` böyle dosya içeren payload'ı reddeder. Yeni müşteri kendi sırrını hedef secret deposundan sağlar.
- 2026-10-10 kaynak ağacı taramasında 24 veya daha uzun karakterli parola/secret/API-key/token ataması için olası sabit literal bulunmadı.
- Yerel Git nesne geçmişindeki 170 JSON blobu ve JWT secret ataması için tarihsel betik/yapılandırma dosyaları değerleri göstermeden tarandı. Eski MKFiloServis, KOA ve CRM `appsettings.json` ile eski `appsettings.PreProduction.json`/build kopyalarında **dört farklı JWT sırrı** doğrulandı; dördü de artık engellenir. Publish betiği, LisansDesktop ve diğer geçici kopyalardaki JWT dışı secret alanları aday olarak sınıflandırıldı; kullanıcı 2026-10-10'da eski DB/entegrasyon credential'larının hiçbirinin bugün geçerli olmadığını bildirdi.
- Aynı yerel geçmişte CRM/KOA/MKFiloServis `appsettings`, `dbsettings`, publish ve yedek kopyalarında DB bağlantı parolası alanları bulundu. Alanların dolu olması geçerli müşteri credential'ı olduklarını kanıtlamaz; sahibinin sağlayıcıda geçerlilik/iptal durumunu doğrulaması gerekir. Değerler rapora alınmadı.
- Geçmişte ifşa edilmiş dört JWT anahtarının fingerprint'i yeni uygulama başlangıcında engellenir. Yeni sürüme geçişte hedefte bunlardan biri varsa uygulama bilerek başlamaz; önce yeni secret vault'a yüklenmelidir.

## Production rotasyon prosedürü

Önce dağıtım sahibi, tarihsel JWT sırlarından birini kullanan **aktif bir kurulum olup olmadığını** kaydeder:

- **Aktif kurulum varsa:** Aşağıdaki JWT rotasyonunun tamamı ve eski token `401` kanıtı zorunludur.
- **Aktif kurulum yoksa:** İş sahibi bunu dağıtım envanteriyle doğrular; eski token `401` adımı uygulanamaz olarak kaydedilir. Yeni müşteri kurulumuna bu dört sırdan hiçbiri taşınmaz; her kurulum için yeni rastgele sır kullanılır. Tarihsel DB/API/lisans adaylarının hâlâ geçerli olup olmadığı ayrıca sahiplerince doğrulanır ve geçerli olanlar iptal edilir.
- **Durum bilinmiyorsa:** Sırların kullanılmadığı varsayılmaz; A-06 kapanmaz. Dağıtım envanteri ve ilgili sır sahipleri belirlenir.

Aşağıdaki 1–7 adımları aktif kurulum varsa uygulanır.

1. Her uygulama düğümünün kullandığı secret deposunu ve değer kaynağını sahibiyle doğrula; `appsettings`/argümanlarda kalmış `Jwt:Secret` değerlerini kaldır. Mevcut değeri rapora veya terminal çıktısına alma.
2. Onaylı secret vault içinde kriptografik rastgele en az 48 baytlık yeni secret üret; `Jwt__Secret` secret sürümünü değiştir. Secret'ı repo, `appsettings`, kurulum ZIP'i, komut satırı, PowerShell history veya destek kaydına yazma.
3. Kısa bakım penceresinde tüm IIS uygulama havuzlarını durdur; yeni secret sürümünü her düğümün secret kaynağına bağla ve havuzları yeniden başlat. Uygulama imza ve doğrulama anahtarını süreç başlangıcında birlikte sabitler; çalışan süreçte yalnız vault değerini değiştirmek rotasyon değildir. Eski JWT'ler yeni secret ile doğrulanmaz; oturumların yeniden açılması beklenir.
4. Korumalı kabul istemcisinde yeni giriş yap; yeni token ile `/api/auth/verify` başarı vermeli. Önceden alınmış token aynı endpoint'te `401` almalı. Anonim protected endpoint `401`, yanlış imza ve yanlış `Jwt__Secret` yapılandırması da başarısız olmalı.
5. Yeni secret her düğümde doğrulandıktan sonra eski vault sürümünü pasifleştir/kaldır. Uygulama ayarlarının ve dağıtım girdilerinin eski secret'a dönüş yolu olmadığını doğrula.
6. Uygulama/ayar hatasında ifşa edilmiş eski secret'a geri dönme. Yeni ve bağımsız rastgele secret sürümü üretip dağıtım adımlarını tekrarla; eski tokenlar için yeniden giriş gerekir.
7. Tutanağa yalnız tarih, onaylayan rol, vault adı/sürüm kimliği (secret değeri değil), düğüm sayısı, eski token 401 sonucu, yeni token doğrulama sonucu ve kesinti süresini yaz.

## Durum

**A-06 🟢 — 2026-10-10.** Yerel kodda ortak doğrulama, tek süreç anahtarı ve Production kaynak kuralı teslim edildi. Dört tarihsel JWT sırrı engelleniyor. Kullanıcı, bu sırları kullanan aktif müşteri/Production kurulumu bulunmadığını ve tarihsel DB/entegrasyon credential adaylarının hiçbirinin bugün geçerli olmadığını bildirdi. Bu beyanlar sağlayıcı tarafında bağımsız doğrulama yerine geçmez; A-06 kapanışının dayanağı olarak açıkça kaydedilir. Eski token `401` ve canlı vault rotasyonu bu kapsamda uygulanamaz, yapılmış gibi gösterilmez.

**Tarihsel kopya kararı:** Geçersiz eski değerleri içeren Git/backup geçmişi zorla yeniden yazılmayacak; denetim kaydı olarak saklanacak ve satış paketine alınmayacak. Eski değerler yeni kurulumda yeniden kullanılmayacak. Güncel Web publish çıktısında yalnız boş JWT alanlı temel `appsettings.json` var; ortama özel ayarlar ile `dbsettings.json` yok. Bu karar uzak kopyaların erişim yetkilerinin denetlendiği iddiası değildir. Sonradan eski bir credential'ın geçerli olduğu veya eski JWT'yi kullanan aktif kurulum bulunduğu anlaşılırsa A-06 yeniden açılır ve yukarıdaki sağlayıcı rotasyonu uygulanır.
