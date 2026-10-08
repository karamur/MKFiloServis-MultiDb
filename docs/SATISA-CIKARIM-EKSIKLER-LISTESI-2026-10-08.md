# MKFiloServis — Satışa Çıkarım Eksikler Listesi

**Tarih:** 2026-10-08
**Esas:** `claude/kind-volta-f9swn3` dalı, son commit `0aecc4f` (2026-10-05)
**Yöntem:** `docs/` altındaki satış analiz raporlarının incelenmesi ve öne çıkan iddiaların güncel kaynakta grep/dosya kontrolüyle doğrulanması.
**Bu turda yapılmayanlar:** Derleme, test, bağımlılık taraması, kurulum, restore veya lisans üretimi yapılmadı (ortamda `dotnet` yok). Bu liste statik incelemeye dayanır.

---

## 1. Karar

**🔴 Ürün satışa hazır değil.** 10-01 analizindeki kritik kod sorunlarının büyük kısmı kaynakta kapatılmış (asimetrik lisans imzası, parola politikası, Swagger kısıtlaması, sır temizliği vb.). Ancak:

- **6 adet P0** madde (lisans geçişi, modül erişim kabulü, tam kurtarma, bağımsız restore kanıtı, güvenlik/tenant kabulü, sır rotasyonu) kapanmadı.
- **Otomatik test projesi hâlâ yok**; CI yalnız derleme doğruluyor.
- Kullanıcının karşılaşacağı **yarım özellikler** (çalışmayan Excel butonları, sabit "Admin" denetim kaydı) duruyor.
- Satış için gereken **ticari/hukuki paket** (kullanıcı kılavuzu, KVKK metinleri, destek/SLA, sürüm notları) eksik.

| Kategori | Adet |
|---|---|
| P0 — satış öncesi bloklayıcı | 6 |
| P1 — ilk müşteri kurulumundan önce | 17 |
| P2 — sürüm planına göre | 8 |
| P3 — takip | 2 |
| Bu incelemede eklenen yeni bulgular (E-xx) | 9 |
| Ticari/hukuki hazırlık eksikleri (T-xx) | 8 |

> Görev kodları (A-xx) [10-05 son durum raporu](SATISA-CIKARIM-SON-DURUM-2026-10-05.md) ile aynıdır; bu belge onu ikame etmez, üzerine yeni bulguları ve ticari eksikleri ekler.

---

## 2. İncelenen kaynaklar

| Belge | Rol | Not |
|---|---|---|
| [SATISA-CIKARIM-ANALIZ-RAPORU.md](SATISA-CIKARIM-ANALIZ-RAPORU.md) | İlk analiz (10-01), 39 bulgu | Tarihsel kesit |
| [SATISA-CIKARIM-GUNCEL-DURUM-RAPORU.md](SATISA-CIKARIM-GUNCEL-DURUM-RAPORU.md) | Düzeltme ekleri | Tarihsel ekler |
| [DUZELTME-DENETIM-RAPORU-2.md](DUZELTME-DENETIM-RAPORU-2.md) | İkinci denetim | Bölüm 23 güncel karşılaştırma |
| [SATISA-CIKARIM-SON-DURUM-2026-10-05.md](SATISA-CIKARIM-SON-DURUM-2026-10-05.md) | **Birleşik açık görev listesi (A-01..A-31)** | Esas alınan liste |
| [LISANS-IMZA-GECIS.md](LISANS-IMZA-GECIS.md), [SIFRELI-BELGE-YEDEK-KURTARMA.md](SIFRELI-BELGE-YEDEK-KURTARMA.md) | Lisans ve kurtarma kapsamı | — |
| `SATISA-CIKARIM-YENIDEN-ANALIZ-2026-10-02.md` | 39 maddenin tek tek değerlendirmesi | **Depoda yok** — 4 belgeden kırık bağlantı (bkz. A-23) |

---

## 3. Kaynakta doğrulanan kapanışlar (özet)

| İlk bulgu | Güncel kaynak durumu |
|---|---|
| K-1 lisans anahtarı gömülü | Asimetrik imza; özel anahtar yalnız LisansDesktop’ta (DPAPI + parola korumalı yedek). 14 gün yenileme toleransı ve saat geri alma kontrolü var (`LicenseService.cs:372-381`). |
| K-3 Cookie şeması | `DefaultScheme = JwtBearer` (`Program.cs:628`). |
| K-4 / O-11 sırlar | `appsettings*.json` ve `docker-compose.yml` içinde düz sır bulunmadı. **Rotasyon kanıtı yok → A-06 açık.** |
| K-6 parola politikası | Uzunluk 12, rakam zorunlu (`Program.cs:225-229`). |
| Y-5 Swagger | Yalnız Admin + Bearer ile açık (`Program.cs:1313-1330`). |
| Y-6 PendingModelChanges | Susturulmuyor, log seviyesinde izleniyor (`Program.cs:196-199`). Kabul A-18’de. |
| Y-10 legacy timestamp | Yapılandırılabilir; varsayılan hâlâ **açık** (`Program.cs:57`). Geçiş A-20’de. |
| Y-11 NuGet audit bastırma | `NuGetAuditSuppress` kalmadı. |
| O-13 / D-7 / D-8 bin/obj/log | Git’te izlenen bin/obj/log dosyası yok. |

---

## 4. Birleşik eksik listesi (önceki raporlardan devreden)

### P0 — Satış öncesi bloklayıcı

| Kod | Durum | Eksik | Kapanış ölçütü |
|---|---|---|---|
| A-01 | 🟡 | Müşteri lisans envanteri, v3 lisans yeniden basımı, .mkkey anahtar yedeğinin bağımsız profilde geri yüklenmesi | Teslim kaydı + bağımsız geri yükleme kanıtı |
| A-02 | 🟡 | Lisanssız modüle sayfa/API/hub/dosya/menü erişimi ve açık oturumda lisans değişimi kabul testi | Lisans dışı modül engelleniyor; Admin lisansı aşamıyor |
| A-03 | 🔴 | **Tam kurtarma uygulaması yok**: DB + dosya + ayar + key ring canlı uygulama sırası ve geri dönüş | Uygulanabilir tam kurtarma akışı |
| A-04 | 🟡 | Farklı makine/profilde gerçek dump + şifreli belge + credential kurtarma provası (DPAPI, S3 dahil) | Belgeler çözülebiliyor, DB-dosya ilişkileri tutarlı |
| A-05 | 🟡 | Giriş/oturum/tenant kabul senaryoları (firma A/B, 401/403, kilit, çıkış, bootstrap kapanışı) | Senaryo kanıtları |
| A-06 | 🟡 | Daha önce repoda bulunmuş JWT/admin/DB sırlarının rotasyonu ve git geçmişi kararı | Belgeli rotasyon |

### P1 — İlk müşteri kurulumundan önce

| Kod | Durum | Eksik |
|---|---|---|
| A-07 | 🔴 | **Kalıcı test projesi yok.** `MKFiloServis.Tests` diskte yok; `tests.yml` yalnız Web’i derliyor ve test adımını atlıyor. Mevcut "testler" iki konsol uygulaması (RentACar, PlaywrightSmoke). |
| A-08 | 🔴 | ExecuteUpdate/Raw SQL yazımlarının audit ve transaction kapsamı envanteri |
| A-09 | 🟡 | PostgreSQL/SQL Server üzerinde audit + mali rollback kabulü |
| A-10 | 🔴 | Başarısız fiziksel dosya silme için kalıcı kuyruk/yeniden deneme; yetim dosya envanteri (kaynakta kuyruk yapısı bulunamadı) |
| A-11 | 🟡 | Dosya silme hata senaryoları kabulü (S3 4xx/5xx, kilit, kısmi hata) |
| A-12 | 🔴 | Araç Excel import sonucu için modal/firma sürüm koruması |
| A-13 | 🟡 | `IgnoreQueryFilters` ile firma atama/firma değiştirme yollarının yetki denetimi |
| A-14 | 🟡 | Araç ekranı/servis kabulü (cache, modal, plaka, soft delete) |
| A-15 | 🔴 | Banka referansı, aktif plaka, dönem snapshot, varsayılan şablon için **DB tekillik kısıtları** |
| A-16 | 🟡 | Eski kayıtlarda bozuk tenant ilişkileri taraması ve onarım planı |
| A-17 | 🟡 | Gerçek banka CSV/XLSX, maaş, fatura API ve SMTP kabulü |
| A-18 | 🟡 | Temiz ve eski kurulumda migration/başlangıç kabulü |
| A-19 | 🟡 | DataSync iki yönlü aktarım provası (FK, sequence, rollback) |
| A-20 | 🟡 | Legacy timestamp → `timestamptz` geçiş planı (varsayılan hâlâ açık) |
| A-21 | 🟡 | Temiz makinede müşteri paketi kurulum/güncelleme kabulü; LisansDesktop pakette olmamalı |
| A-22 | 🟡 | Güncel publish için tam (transitif dahil) zafiyet taraması kaydı |
| A-23 | 🔴 | Kırık doküman bağlantısı (`SATISA-CIKARIM-YENIDEN-ANALIZ-2026-10-02.md`), silinen raporların gerekçesi, teslim commit’i |

### P2 / P3

| Kod | Öncelik | Durum | Eksik |
|---|---|---|---|
| A-24 | P2 | 🔴 | Çok süreçli Redis önbellek tazeliği |
| A-25 | P2 | 🟡 | Önbellek yarış/yük kabulü |
| A-26 | P2 | 🟡 | Excel/PDF çıktılarının görsel kabulü (uzun metin, çok sayfa, Türkçe karakter) |
| A-27 | P2 | 🟡 | Luca/UBL/portal entegrasyon kabulü, çift POST koruması |
| A-28 | P2 | ⚪ | SQL Server/MySQL destek kararı (migration’lar yalnız Npgsql) |
| A-29 | P2 | ⚪ | Mali ürün politikaları (referanssız banka hareketi, muhasebeleşmiş snapshot silme) |
| A-30 | P3 | ⚪ | Kod/doküman düzeni (tekrarlar, eski context’ler, DTO doğrulama) |
| A-31 | P3 | ⚪ | Çevrimdışı ve depolama (local/S3) kapsamının müşteriye yazılı tanımı |

---

## 5. Bu incelemede eklenen yeni bulgular

Önceki raporlarda açık görev olarak yer almayan veya kapanmış sayılan, ancak güncel kaynakta duran eksikler:

| Kod | Öncelik | Bulgu | Konum | Öneri |
|---|---|---|---|---|
| E-01 | P1 | **Bordro işlemlerinde kullanıcı sabit "Admin" yazılıyor** — denetim izi yanlış (Y-4’ün benzeri, kapsam dışında kalmış) | `Personel/NormalBordro.razor:997`, `Personel/ArgeBordro.razor:970` | Kullanıcıyı `AuthenticationStateProvider`’dan al |
| E-02 | P1 | **EBYS belge aramasında kullanıcı ID sabit 1** — arama geçmişi/önerileri tüm kullanıcılar arasında karışır | `EBYS/BelgeArama.razor:512, 567, 590` | Oturumdaki kullanıcı ID’si |
| E-03 | P1 | **Çalışmayan Excel butonları**: tıklanınca "yakında eklenecek" uyarısı veya işlem yapmayan TODO | `Ayarlar/AuditLogYonetimi.razor:663`, `Budget/KrediTaksitler.razor:1498`, `Butce/HedefGerceklesen.razor:676`, `Finans/PersonelOdenecekler.razor:769`, `Cariler/CariRiskAnalizi.razor:563` | Uygula veya butonu gizle |
| E-04 | P2 | CRM WhatsApp gönderimi uygulanmamış (TODO); muhasebe hesap listesi yorum satırında | `CRMService.cs:459`, `Personel/PersonelFinansAyarlar.razor:365` | Uygula veya UI’dan kaldır; satış broşüründe vaat edilmemeli |
| E-05 | P1 | **Boş `catch` blokları hâlâ 39 adet** (ilk raporda 40). Mali/kritik dosyalarda: `BankaKasaHareketService` (2), `BackupService` (2), `FaturaSablonService` (1), `WebhookService` (1), `PersonelOzlukService` (1), `ApplicationDbContext` | `Services/*`, `Data/ApplicationDbContext.cs` | Mali, yedek ve DbContext yollarındakileri en az `LogWarning` ile logla |
| E-06 | P2 | Blazor sayfasında aynı scoped servislere paralel `ContinueWith(t => t.Result)` — aynı DbContext üzerinde eşzamanlı sorgu riski | `ServisOperasyon/KontratList.razor:266-268`, `PuantajDetay.razor:501, 524` | Sıralı `await` veya `IDbContextFactory` |
| E-07 | P1 | `CalismaPuantaji.razor:1145` firma filtresi varsayılanı sabit `1` | `Personel/CalismaPuantaji.razor:1145` | Aktif firmadan başlat |
| E-08 | P1 | **CI yalnız `main` için tetikleniyor**; geliştirme dallarında (`claude/*` dahil) test/derleme kontrolü çalışmıyor | `.github/workflows/tests.yml:12-16` | PR hedefini genişlet veya dallarda `workflow_dispatch` kullan |
| E-09 | P2 | Üretim izleme zayıf: yalnız `AddHealthChecks()`; yapılandırılmış log toplama/hata izleme (Serilog, OpenTelemetry vb.) yok. Yönetici/muhasebe rolleri için **2FA zorunluluğu** kodda bulunamadı | `Program.cs:589` | Merkezi log + uyarı; kritik rollerde 2FA zorunlu |

---

## 6. Ticari / hukuki satış hazırlığı eksikleri

Kod dışı, satış ve teslim için gerekli paket. Depoda arama ile kontrol edildi; bulunmayanlar "yok" olarak işaretlendi.

| Kod | Öncelik | Eksik | Mevcut durum |
|---|---|---|---|
| T-01 | P0 | **Ürün kapsamı ve sürüm/paket tanımı**: hangi 15 modül hangi pakette, desteklenen DB (PostgreSQL/SQLite), çevrimdışı sınırı | Kodda modül hakları var; müşteriye yönelik kapsam belgesi yok (A-28, A-31 ile bağlı) |
| T-02 | P0 | **Lisans sözleşmesi (EULA) son hukuki onayı** | `LICENSE` dosyası mevcut (Allbatros Global Teknoloji); hukuk onayı kaydı yok |
| T-03 | P0 | **KVKK paketi**: aydınlatma metni, açık rıza, veri işleyen sözleşmesi (DPA), saklama/imha politikası, VERBİS rehberi | `KvkkYonetimi.razor` ekranı var; müşteriye verilecek metin/şablon yok |
| T-04 | P1 | **Son kullanıcı kılavuzu** | Yalnız puantaj/fatura kılavuzu var (`wwwroot/docs/puantaj-ve-fatura-kilavuzu.md`); diğer modüller yok. `MKFiloServis.Web/Docs/` iç tasarım notlarıdır |
| T-05 | P1 | **Kurulum, yedekleme ve kurtarma işletim kılavuzu (müşteri BT’si için)** | `setup/` altında geliştirici rehberleri var; tam kurtarma uygulanmadığı için (A-03) müşteri runbook’u yazılamaz |
| T-06 | P1 | **Destek / SLA / güncelleme politikası** ve destek kanalı | Belge yok; `SECURITY.md` yalnız güvenlik bildirimi |
| T-07 | P1 | **Rent a Car belge şablonlarının hukuk onayı** | README:36 açıkça "hukuken onaylı değildir" diyor |
| T-08 | P2 | **Sürüm notları / CHANGELOG ve sürüm numaralandırma** | `release-drafter.yml` var; yayımlanmış sürüm notu yok. Demo verisi ve demo lisans akışının satış sunumu için hazırlanması |

---

## 7. Raporlar arası tutarsızlıklar

1. **Kırık bağlantı:** `SATISA-CIKARIM-YENIDEN-ANALIZ-2026-10-02.md` dört yerde bağlantılı, dosya yok (`SATISA-CIKARIM-ANALIZ-RAPORU.md:7`, `DUZELTME-DENETIM-RAPORU-2.md:16, 739`, `SATISA-CIKARIM-GUNCEL-DURUM-RAPORU.md:126`).
2. **Migration sayısı:** İlk rapor "288 EF migration" diyor; güncel `Data/Migrations` altında 125 `Designer.cs` (156 migration dosyası, snapshot hariç) var. Raporlarda sayı güncellenmeli.
3. **Proje profili:** İlk rapordaki `MKFiloServis.Infrastructure`/`Service` artık ne diskte ne README’de var; O-10 kapanmış sayılabilir.
4. **Boş catch sayısı:** Y-1 için kapanış iddiası yok ama raporlarda da açık görev olarak ayrılmamış; E-05 ile listeye alındı.

---

## 8. Önerilen sıra ve kaba efor

| Faz | İçerik | Kaba süre |
|---|---|---|
| 1 | P0 kabulleri: A-01, A-02, A-05, A-06 + T-01, T-02, T-03 başlangıcı | 1 hafta |
| 2 | A-03 tam kurtarma uygulaması + A-04 bağımsız prova + T-05 | 1–2 hafta |
| 3 | A-07 xUnit test projesi (lisans, tenant, audit, mali) + E-08 CI | 1 hafta |
| 4 | Kod eksikleri: A-08, A-10, A-12, A-15, E-01..E-07 | 1–2 hafta |
| 5 | P1 kabulleri: A-09, A-11, A-13, A-14, A-16..A-22 | 1–2 hafta |
| 6 | Teslim paketi: T-04, T-06, T-07, T-08, A-23 | 1 hafta (paralel yürüyebilir) |

**Toplam:** paralel yürütmeyle ~5–7 hafta. P2/P3 (A-24..A-31, E-09) ilk sürüm sonrasına bırakılabilir; ancak A-28 ve A-31’in *kararı* (ürün kapsamı) satış öncesi T-01 için gereklidir.

---

## 9. Satış kabul kontrol listesi

- [ ] Tüm P0 maddeleri (A-01..A-06, T-01..T-03) kanıtla kapandı
- [ ] Test projesi CI’de çalışıyor ve yeşil (A-07, E-08)
- [ ] Temiz makinede müşteri paketi kuruldu, güncellendi, tam kurtarma provası yapıldı (A-03, A-04, A-21)
- [ ] Kullanıcıya görünen yarım özellik kalmadı (E-03, E-04)
- [ ] Denetim izi gerçek kullanıcıyı yazıyor (E-01, E-02)
- [ ] Güncel bağımlılık taraması temiz (A-22)
- [ ] Kullanıcı kılavuzu, KVKK metinleri, SLA ve EULA müşteri paketinde (T-02..T-06)
- [ ] Doküman bağlantıları geçerli, teslim commit’i etiketlendi (A-23)

*Bu liste statik incelemeye dayanır. "🟡 kabul" maddeleri yalnızca çalışan ortamda, kayıtlı kanıtla kapatılabilir.*
