# A-22 — Güncel NuGet bağımlılık taraması

**Tarih:** 2026-10-06  
**Kapsam:** `MKFiloServis.slnx` içindeki altı proje ve çözüm dışında kalan `MKFiloServis.RentACarChecks` projesi; doğrudan ve geçişli NuGet paketleri. Yerel çalışma ağacı esas alındı.

## Bulgu ve düzeltme

İlk `dotnet package list --vulnerable --include-transitive --no-restore` taramasında LisansDesktop için `SQLitePCLRaw.lib.e_sqlite3` **2.1.11** geçişli bağımlılığı yüksek önem dereceli [GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q) bildirimiyle raporlandı. `dotnet nuget why` zinciri `Microsoft.Data.Sqlite 10.0.9` → `SQLitePCLRaw.bundle_e_sqlite3 2.1.11` → `SQLitePCLRaw.lib.e_sqlite3 2.1.11` idi.

LisansDesktop projesine doğrudan `SQLitePCLRaw.lib.e_sqlite3` **2.1.13** eklendi. Sonraki bağımlılık çözümlemesinde native kütüphane 2.1.13 olarak seçildi. Web projesinde aynı doğrudan 2.1.13 düzeltmesi zaten vardı.

## Son tarama

| Kontrol | Sonuç |
|---|---|
| `MKFiloServis.slnx` — doğrudan ve geçişli | 🟢 Altı projenin hiçbirinde geçerli NuGet kaynaklarında bilinen güvenlik açığı raporlanmadı |
| Çözüm dışı Rent-a-Car kontrol projesi | 🟢 Bilinen güvenlik açığı raporlanmadı |
| LisansDesktop Release/win-x64 self-contained/single-file publish | 🟢 `setup/payload/LisansDesktop/MKFiloServisLisans.exe` yenilendi; publish başarılı |
| CI NuGet audit iş akışı | 🟢 Çözüm ve çözüm dışı proje taranıyor; JSON sonuçları sayılıyor; tarama komutu hata verirse iş başarısız oluyor |

Kullanılan komutlar:

```powershell
dotnet package list --project MKFiloServis.slnx --vulnerable --include-transitive --no-restore
dotnet package list --project MKFiloServis.Web/Tests/RentACar/MKFiloServis.RentACarChecks.csproj --vulnerable --include-transitive
dotnet nuget why MKFiloServis.LisansDesktop/MKFiloServis.LisansDesktop.csproj SQLitePCLRaw.lib.e_sqlite3
```

Bu sonuç, tarama anında belirtilen NuGet kaynaklarının bildirdiği **bilinen** açıklarla sınırlıdır. Daha sonra yayımlanacak bildirimleri veya müşteri kurulumunun çalışma zamanı kabulünü kapsamaz. Paketleme/kurulum kabulü A-21'de, ürünün diğer güvenlik/tenant kabulü A-05/A-06'da izlenir.

## Ek tarama — test projesi eklendikten sonra

2026-10-06 tarihinde `MKFiloServis.Tests` projeye ve çözüme eklendi. `dotnet package list --project MKFiloServis.slnx --vulnerable --include-transitive --no-restore --format json` yeniden çalıştırıldı; çözümdeki **yedi** projenin hiçbirinde tarama anındaki NuGet kaynaklarının bildirdiği bilinen açık görünmedi. Yukarıdaki altı proje sayımı ilk taramanın tarihsel kapsamıdır. Çözüm dışı Rent-a-Car projesinin ilk tarama sonucu geçerlidir.

## 2026-10-08 GitHub CI doğrulaması

İlk GitHub Ubuntu audit çalışması Windows hedefli projeleri restore edemedi. İş akışı Windows 2025 çalıştırıcısına taşındı ve Client MAUI workload restore eklendi. [GitHub çalışması #37831503855](https://github.com/karamur/MKFiloServis-MultiDb/actions/runs/37831503855) çözüm ve Rent-a-Car restore, doğrudan/geçişli zafiyet taraması ve iş sonu adımlarını başarıyla tamamladı; açık bulundu koşulu tetiklenmedi. İş akışı dosyası değişince de denetim tetiklenir. Kapsam ve diğer CI sonuçları [A-07 kaydında](A-07-CI-DOGRULAMA-2026-10-08.md) yer alır.
