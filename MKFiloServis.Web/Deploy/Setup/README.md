# Eski Deploy Setup belgesi

Bu klasördeki `setup.ps1`/`setup.bat` yayın akışı tarihsel dağıtım akışıdır; yeni satış paketi veya kurulum kaynağı olarak kullanılmamalıdır. Eski sürüm, sağlayıcı kapsamı, sır yönetimi ve kurulum dosyaları hakkındaki örnekleri güncel olmayabilir.

Güncel Windows kurulum ve müşteri paketleri için [setup/README.md](../../../setup/README.md) içindeki `setup/build.ps1` akışını kullanın. Ana/müşteri kurulumunda PostgreSQL veya SQLite seçilir; `dbsettings.json` hedefte kurucu tarafından oluşturulur ve ACL ile korunur. `Jwt__Secret` üretim ortamının korumalı secret deposundan ilk başlangıç öncesi sağlanmalıdır; paket, ZIP, `appsettings.Production.json` veya komut satırına eklenmemelidir.

Üretimde boş PostgreSQL şeması başlangıç akışı halen kabul edilmediğinden kurulum/yükseltme için A-18/A-21 görevlerindeki restore ve hedef makine kabul adımlarını tamamlayın.
