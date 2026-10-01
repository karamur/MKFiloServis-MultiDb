# İnternetsiz yerel AI kurulumu

Bu kurulum hiçbir dosya indirmez. Resmî Ollama Windows kurulum dosyasını yerel olarak kurduktan sonra kurulan `ollama.exe` dosyasını ve lisansı uygun GGUF model dosyasını kullanın. İki dosyanın SHA256 değerlerini ayrıca doğrulayın. `OllamaSetup.exe` doğrudan model çalıştırıcısı değildir. Model ve çalıştırıcı sağlanmadan AI özelliği kullanılamaz; uygulama bulut servisine geçmez.

Yönetici PowerShell penceresinde örnek kullanım:

```powershell
.\Install-LocalAI.ps1 -OllamaExe 'D:\Kurulum\ollama.exe' -ModelGguf 'D:\Kurulum\model.gguf' -OllamaSha256 '<64 karakter SHA256>' -ModelSha256 '<64 karakter SHA256>'
```

Modelin bilgisayarda bulunup bulunmadığı bilinmiyorsa, `Find-LocalModel.ps1` yaygın yerel klasörleri tarar; herhangi bir internet isteği göndermez.

Betik modeli yalnızca yerel dosyadan içeri alır, Ollama dinleme adresini `127.0.0.1:11434` yapar, Ollama işlemine dış ağ çıkışını engelleyen Windows Güvenlik Duvarı kuralı ekler ve açılışta yerel modeli başlatacak zamanlanmış görev oluşturur. Kuralın ve loopback bağlantısının hedef Windows sürümünde ayrıca doğrulanması gerekir. Uygulamanın başka modüllerindeki internet bağlantıları bu kuralın kapsamında değildir; tamamen kapalı ortam için uygulama sunucusunun dış ağ çıkışı da ağ/işletim sistemi düzeyinde engellenmelidir.

`appsettings.json` içindeki `Ollama:Model` değeri kurulan model adıyla aynı olmalıdır. Modeli silmeden önce kullanılan raporları kapatın. Veri tabanı dosyası bu kurulumda değiştirilmez.

AI özelliği varsayılan olarak kapalıdır. Betik güvenlik duvarı kuralı, yerel model ve açılış görevini oluşturduktan sonra makine ortamında `Ollama__Enabled=true` ayarlar. Uygulamayı yeniden başlatınca yerel AI etkinleşir. Kurulum herhangi bir aşamada başarısız olursa özelliği elle etkinleştirmeyin.

## Veri güvenliği sınırları

- AI HTTP istemcisi yalnızca sayısal loopback IP adresine bağlanır; proxy ve yönlendirme kapalıdır. Eski bulut AI servisleri ve ayarları projeden kaldırılmıştır.
- AI yalnızca etkin veritabanı sağlayıcısı SQLite ve veri kaynağı yerel dosya olduğunda açılır. Uzak veritabanı veya ağ paylaşımındaki SQLite dosyası AI için kabul edilmez.
- Modelin işlendiği bilgisayar güvenilir olmalıdır. Mutlak dışarı çıkış güvencesi için uygulama sunucusunun ve yedekleme/entegrasyon işlemlerinin dış ağ erişimi ayrıca işletim sistemi veya ağ güvenlik duvarında kapatılmalıdır. Uygulamadaki CDN, e-posta, webhook ve piyasa araştırması gibi diğer özellikler bu AI kurulumunun kapsamı dışındadır.
- SQLite dosyası kendiliğinden şifreli değildir. Disk şifrelemesi, dosya izinleri ve şifreli yedekler ayrıca yapılandırılmalıdır. Bu betik mevcut veritabanı dosyasını taşımaz veya dönüştürmez.
- Model yanıtı finansal kayıt veya işlem olarak otomatik kaydedilmemelidir; rapor toplamları uygulamanın veritabanı sorgularından alınmalıdır.
