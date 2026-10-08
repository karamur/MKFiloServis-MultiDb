# A-07 — GitHub CI doğrulaması (2026-10-08)

## Kök neden ve düzeltme

- GitHub Linux `Tests` ve Docker derlemesi ile Windows CodeQL derlemesi `MKFiloServis.WriteAudit.sql` gömülü kaynağını bulamıyordu. `MKFiloServis.Shared/Auditing/postgres-write-audit.sql` yerelde vardı, ancak genel `*.sql` Git ignore kuralı nedeniyle depoya hiç girmemişti. Yalnız bu gerekli SQL kaynağı için istisna açıldı ve dosya Git'e eklendi (`d53bebe3`).
- SQL kaynağı eklendikten sonra GitHub Web ve test projesi derlemesi geçti. Linux testlerinde 100/102 sonucu veren iki dosya referans testi, Windows'taki harf duyarsız yol eşleşmesini Linux'ta da varsayıyordu. Test verisi gerçek işletim sistemi yol kuralına uyarlandı (`c1c3aac7`). Üretim silme davranışı gevşetilmedi.
- NuGet audit çözümdeki Windows/MAUI projelerini Ubuntu'da restore edemiyordu. İş Windows 2025 çalıştırıcısına taşındı, Client workload restore eklendi ve iş akışı dosyası değişince tetiklenmesi sağlandı (`a9dc5288`).
- Docker `Build & Push` işi başarılıydı; ardından Trivy işi `aquasecurity/trivy-action@0.28.0` sürüm etiketini bulamadı. Eylem GitHub'daki gerçek `v0.36.0` commit'ine sabitlendi; yeni tarama sonucu bekleniyor.
- Yeniden denemede imaj oluşturma/GHCR gönderimi yine geçti. Trivy `ghcr.io/karamur/MKFiloServis-MultiDb:latest` adını büyük harf nedeniyle ayrıştıramadı. Repo adı her iki işte küçük harfe normalize edildi; tarayıcıya aynı derlemenin digest'i verildi. Başarısız taramada olmayan SARIF'i yükleme denemesi de kaldırıldı.

## GitHub çalışma kanıtı

| İş | Sonuç | Kanıt |
|---|---|---|
| NuGet Vulnerability Audit | 🟢 Restore ve doğrudan/geçişli paket taraması başarılı; açık bulunmadı | [GitHub çalışması #37831503855](https://github.com/karamur/MKFiloServis-MultiDb/actions/runs/37831503855) |
| Tests | 🟢 102/102 geçti, 0 başarısız, 0 atlanan; Web ve test projesi Release derlemesi geçti | [GitHub çalışması #37832806383](https://github.com/karamur/MKFiloServis-MultiDb/actions/runs/37832806383) |
| Docker Image | 🟡 İmaj derleme ve GHCR gönderimi geçti; Trivy eylem etiketi ve büyük harfli imaj başvurusu düzeltildi, yeniden tarama bekleniyor | [GitHub çalışması #37833510606](https://github.com/karamur/MKFiloServis-MultiDb/actions/runs/37833510606) |
| CodeQL | 🟡 Sonuç bekleniyor | [GitHub çalışması #37832806362](https://github.com/karamur/MKFiloServis-MultiDb/actions/runs/37832806362) |

Bu kanıt sentetik CI kapsamıdır. Gerçek müşteri lisansı, PostgreSQL/tenant/audit, restore ve mali iş akışlarının saha kabulü A-07 ve ilgili görevlerde açıktır; A-07 genel rengi 🟡 kalır.
