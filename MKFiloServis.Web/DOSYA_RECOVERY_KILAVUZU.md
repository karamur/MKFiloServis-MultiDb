# Dosya ve veritabanı kurtarma

Bu kılavuz, güncel `RecoveryArchive` yedeğinin hazırlanması ve kontrollü geri yüklenmesi içindir. Eski `master.key` / KOA1 toplu yeniden şifreleme talimatları bu akışın parçası değildir. Yedek içindeki DataProtection key ring'i tek başına başka Windows makinesinde/profilinde çözülebilir kabul edilmez.

## Kurtarma yedeği oluşturma ve ön kontrol

1. Yönetim arayüzünde **Ayarlar → Yedekleme** bölümünden tam kurtarma yedeği oluşturun. PostgreSQL için tek ZIP, `database.backup` ile dosya arşivini birlikte içerir. DB dump ve dosyalar art arda kopyalanır; aralarında yazım olursa birbirine bağlı DB/dosya durumu aynı ana ait olmayabilir. Uygulama bu işlemle otomatik bakım kipine geçmez. Tutarlı kurtarma noktası gerekiyorsa yedek öncesinde diğer uygulama ve arka plan yazımlarını bakım penceresinde durdurun.
2. Yedek dosyasını uygulama ve kaynak depolamadan ayrı, erişimi sınırlandırılmış bir ortama kopyalayın. İçinde Production ayarları ve hassas dosyalar bulunabileceğinden ZIP'i gizli bilgi gibi koruyun; e-posta veya herkese açık dosya paylaşımıyla taşımayın.
3. Hedef kurulumda **Kurtarmayı hazırla** işlemini çalıştırın. Bu işlem ZIP manifesti, izin verilen yollar, boyutlar ve SHA-256 değerlerini doğrular; içeriği canlı hedefe dokunmadan izole `RecoveryStaging` klasörüne açar ve arşiv key ring'iyle DataProtection probunu dener.
4. `Anahtar probu: Doğrulanamadı` ise uygulama verisini geri yüklemeyin. Kaynakta kullanılan DataProtection koruyucu sertifika/DPAPI hesabını ve key ring erişimini yetkili sistem sahibiyle kurtarın; ardından hazırlığı yeniden çalıştırıp probun doğrulandığını görün. Key XML dosyalarının arşivde bulunması başarılı çözme kanıtı değildir.
5. DB-only yedek dosya ağacını geri yüklemez. Tam geri yükleme için aynı bakım penceresinde alınmış DB dump'ı, belge/dosya arşivi ve çözebilen key ring'i birlikte kullanın. Luca/entegrasyon credential'ları, S3 erişimi ve hedef Production DB bağlantı ayarları ortama özel yapılandırmadan güvenli kanalla sağlanır; `application/*.json` yedekten hedefe kopyalanmaz.

## Kontrollü uygulama ve geri alma

`Deploy/Migrate/05-recovery-archive-apply.ps1` yalnızca doğrulanmış hazırlık klasörüne uygulanır. Script manifestteki dosya boyutu/hash değerlerini yeniden denetler, hedef depolamanın mevcut dosyalarını ve DB durumunu operation journal'a alır, DB/dosya hedeflerini kaydeder ve adlandırılmış IIS havuzunu durdurur. Arka plan yazımlarının durduğunu ve anahtar kurtarma malzemesinin hazır olduğunu açıkça onaylamadan devam etmez.

Kısmi hata veya kabul başarısızlığında IIS'i kapalı tutun ve `Deploy/Migrate/06-recovery-archive-rollback.ps1`'i aynı operation journal ile çalıştırın. Geri alma öncesi dosya snapshot'larının SHA-256 değerlerini ve DB dump makbuzunu denetler. Geri alma da başarısızsa journal'ı silmeyin; elle müdahale ve tutarlılık kontrolü tamamlanana kadar uygulamayı başlatmayın.

DB-only restore servisi kendi içinde hedef DB'nin önceki durumunu alır ve restore doğrulanamazsa geri yüklemeyi dener. Bu mekanizma tam dosya+DB operasyon journal'ının yerine geçmez.

## Taşınabilirlik ve işletim sınırı

- Gerçek restore öncesi yedeği ve operation journal'ı ayrı bir hedefte koruyun. Üretim verisinde ilk prova yapmayın; izole ve yetkili kopya kullanın.
- Başka makine/profilde `KeyProbeVerified=true` olmadan şifreli evrakların çözülebileceğini kabul etmeyin. Sertifika/DPAPI koruyucusu taşınabilir değilse key ring'i kaynak makineden kopyalamak yeterli olmaz.
- Veritabanı, dosyalar, belge şifre çözme, harici credential/S3 erişimi ve uygulamanın yeniden açılması aynı prova tutanağında yer almalıdır. Önce/sonra kayıt sayıları ile örnek belgeler iş sahibi tarafından kabul edilmelidir.
- Canlı müşteri verisi, credential veya sır çalışma ağacına/doc dosyasına yazılmaz. Bu kılavuzun düzeltilmesi gerçek ortam restore kabulinin yapıldığı anlamına gelmez.

## Eski master-key notu

Eski `master.key`/KOA1 kurtarma araçları yalnızca gerçekten bu legacy formatla şifrelenmiş dosyaların ayrı ve yetkili bir kurtarma işi için değerlendirilebilir. Anahtarı terminale, rapora veya kaynak koda koymayın; mevcut anahtarı silmeyin/değiştirmeyin ve dosyaları topluca yeniden şifrelemeyin. Gerekirse önce kopya üzerinde, eski ve yeni anahtarların sahibi doğrulandıktan sonra ilerleyin.
