# MKFiloServis — Satışa Çıkarım Eksikler Listesi

**Tarih:** 2026-10-08 (2. kontrol: `a99bcfa` sonrası)
**Esas:** `main` @ `a99bcfa` — "Satışa çıkarım düzeltmelerini ve regresyonları tamamla" (187 dosya)
**Yöntem:** Satış raporlarının incelenmesi; raporlardaki iddiaların ve bu listenin önceki sürümündeki E-xx/T-xx bulgularının güncel kaynakta grep/dosya kontrolüyle yeniden doğrulanması; GitHub Actions sonuçlarının okunması.
**Bu turda yapılmayanlar:** Yerel derleme/test yapılmadı (ortamda `dotnet` yok). Derleme/test durumu GitHub Actions kayıtlarından alındı.

---

## 0. 2. kontrolün özeti

| Konu | Sonuç |
|---|---|
| Test projesi (A-07) | ✅ `MKFiloServis.Tests` eklendi (xUnit, 42 `[Fact]` + 17 `[Theory]`), `tests.yml` artık testleri koşuyor |
| Yerel test raporu | Raporda 102/102 başarılı (Windows). **Kanıt dosyaları (log/TRX) depoda yok**, bağlantılar kırık |
| **GitHub CI @ a99bcfa** | 🔴 **Tests: FAILURE**, 🔴 **Docker Image: FAILURE**, 🔴 **NuGet Audit: FAILURE** |
| Önceki listedeki 9 yeni bulgu (E-01..E-09) | 1 tanesi büyük ölçüde kapandı (E-05), 8’i **hâlâ açık** |
| Ticari/hukuki eksikler (T-01..T-08) | T-01 kısmen (A-31 kapsam kararı); diğerleri **açık** |
| Genel karar | 🔴 **Satışa hazır değil** — üstelik `main` şu an temiz bir makinede **derlenmiyor** |

---

## 1. 🔴 YENİ BLOKLAYICI — `main` dalı temiz checkout’ta derlenmiyor (B-01..B-03)

| Kod | Öncelik | Bulgu | Kanıt | Çözüm |
|---|---|---|---|---|
| **B-01** | **P0** | `MKFiloServis.Shared/Auditing/postgres-write-audit.sql` **depoya hiç commit edilmemiş**. `.gitignore:61` içindeki `*.sql` kuralı dosyayı dışlıyor. Shared, Web ve LisansDesktop bu dosyayı `EmbeddedResource`/`Content` olarak kullanıyor. Sonuç: CI ve Docker derlemesi `CS1566 … Could not find file …postgres-write-audit.sql` ile kırılıyor. A-08 (🟢 işaretli) “ortak veritabanı denetimi”nin çekirdek SQL’i yalnız geliştiricinin makinesinde. | [Tests run 37826445846](https://github.com/karamur/MKFiloServis-MultiDb/actions/runs/37826445846), [Docker run 37826445804](https://github.com/karamur/MKFiloServis-MultiDb/actions/runs/37826445804) | Bu dalda `.gitignore`’a `!MKFiloServis.Shared/Auditing/postgres-write-audit.sql` istisnası eklendi. **Dosyanın kendisi geliştirici makinesinden eklenmeli:** `git add MKFiloServis.Shared/Auditing/postgres-write-audit.sql`. Bu yapılana kadar A-08 🟢 sayılamaz; yerel 102/102 sonucu depodaki kodla tekrarlanamaz. |
| **B-02** | P1 | NuGet Audit iş akışı Linux’ta `dotnet restore MKFiloServis.slnx` adımında `NETSDK1100` ile düşüyor (DataSync, LisansDesktop Windows hedefli). Bu iş akışı 10-05’ten beri kırmızı; yani A-22 “CI’de izlenir” iddiası şu an geçerli değil. | [NuGet Audit run 37826445996](https://github.com/karamur/MKFiloServis-MultiDb/actions/runs/37826445996) | `-p:EnableWindowsTargeting=true` ile restore edin veya işi `windows-latest` üzerinde çalıştırın. Client (`net10.0-android`) için workload gereksinimini de kontrol edin. |
| **B-03** | P1 | Test kanıt dosyaları (`build-final-validation.log`, `test-final-validation.log`, `TestResults/**`, `pg-validation.log`, `build-a15-final.log`, `test-a15-final.log`) depoda yok; `TEST-DOGRULAMA-2026-10-08.md` içindeki 7 bağlantı kırık. | `docs/TEST-DOGRULAMA-2026-10-08.md` | Satış kabul kanıtı olarak CI artefaktı (TRX) kullanın veya özetleri depoya ekleyin. |

> **Sonuç:** “102/102 test geçti” sonucu yalnız geliştirici makinesinde geçerli. B-01 düzeltilip CI yeşil olmadan A-07 ve A-08 kapatılmamalı.

---

## 2. Güncel görev durumu (A-01..A-31)

Kaynak: [SATISA-CIKARIM-SON-DURUM-2026-10-05.md](SATISA-CIKARIM-SON-DURUM-2026-10-05.md) (son güncelleme 2026-10-08).

| Renk | Sayı | Görevler |
|---|---:|---|
| 🟢 | 4 | A-08\*, A-22\*, A-30, A-31 |
| 🟡 | 21 | A-01, A-02, A-04, A-05, A-06, A-07, A-09, A-11, A-12, A-13, A-14, A-17, A-18, A-19, A-20, A-21, A-24, A-25, A-26, A-27, A-29 |
| 🔴 | 6 | A-03, A-10, A-15, A-16, A-23, A-28 |

\* Bu kontrolde **A-08** B-01 nedeniyle, **A-22** ise B-02 nedeniyle (CI audit kırık) 🟡’ye çekilmelidir.

### Bu committe ilerleyen görevler

| Görev | Önceki | Şimdi | Ne yapıldı | Kalan |
|---|---|---|---|---|
| A-03 | 🔴 | 🔴 | `05-recovery-archive-apply.ps1` / `06-recovery-archive-rollback.ps1` ile DB+dosya uygulama ve journal’dan geri dönüş | Gerçek restore ve hata enjeksiyonu kabulü |
| A-07 | 🔴 | 🟡 | xUnit projesi, CI’de test adımı | **CI kırmızı (B-01)** |
| A-08 | 🔴 | 🟢→🟡 | Ortak yazım denetimi, sözleşme ve izole kanıt belgeleri | SQL kaynağı depoda yok (B-01) |
| A-10 | 🔴 | 🔴 | Atomik şifreli yazım, karantina, kalıcı temizlik günlüğü, yetim tarayıcı | Karantina kapasitesi, legacy yarışı, çok sunucu |
| A-12 | 🔴 | 🟡 | Import sürüm/kilit koruması | Gerçek Excel ile ekran kabulü |
| A-15 | 🔴 | 🔴 | Filtreli tekil indeksler, banka/fatura eşleştirme firma tetikleyicileri | Diğer ilişkiler, eşzamanlılık kabulü |
| A-16 | 🟡 | 🔴 | DataSync’e salt okunur eski veri envanteri | Müşteri verisinde tarama ve onarım |
| A-22 | 🟡 | 🟢→🟡 | Tarama belgesi, SQLite 2.1.13 | CI audit kırık (B-02) |
| A-23 | 🔴 | 🔴 | Kırık yeniden analiz bağlantıları güncel envantere yönlendirildi | Yeni kırık kanıt bağlantıları (B-03), teslim commit’i |
| A-28 | ⚪ | 🔴 | SQL Server/MySQL güvenli reddediliyor | Ürün kapsam kararı belgesi |
| A-29..A-31 | ⚪ | 🟡/🟢/🟢 | [Ürün kararları](A-29-31-URUN-KARARLARI.md) | A-29 rol değişimi kabulü |

### P0 — hâlâ açık (değişmedi)

A-01 lisans müşteri geçişi · A-02 modül erişim kabulü · A-03 tam kurtarma kabulü · A-04 bağımsız makinede restore · A-05 giriş/tenant kabulü · A-06 sır rotasyonu. Bu committe bunlar için çalışma zamanı/müşteri kanıtı eklenmedi.

---

## 3. Önceki listedeki yeni bulguların yeniden kontrolü (E-01..E-09)

| Kod | Durum | Güncel kanıt |
|---|---|---|
| E-01 | 🔴 Açık | Bordro işlemlerinde kullanıcı hâlâ sabit `"Admin"`: `Personel/NormalBordro.razor:997`, `Personel/ArgeBordro.razor:970` |
| E-02 | 🔴 Açık | EBYS aramasında kullanıcı ID hâlâ `1`: `EBYS/BelgeArama.razor:512, 567, 590` |
| E-03 | 🔴 Açık, **kapsam genişledi** | Çalışmayan Excel butonları duruyor (`AuditLogYonetimi.razor:663`, `KrediTaksitler.razor:1498`, `HedefGerceklesen.razor:676`, `PersonelOdenecekler.razor:769`, `CariRiskAnalizi.razor:563`). Ek olarak **“yakında eklenecek”** uyarısı veren butonlar: `Butce/AylikOdemeler.razor:368-399` (yeni plan, düzenleme, silme, detay, ödeme — 5 işlem), `Personel/PersonelAvanslar.razor:594` (detay) |
| E-04 | 🟡 Açık | `CRMService.cs:461` WhatsApp TODO; `PersonelFinansAyarlar.razor:365` hesap listesi yorum satırında |
| E-05 | 🟢 Büyük ölçüde kapandı | Mali yollardaki boş catch’ler kaldırıldı (`BankaKasaHareketService` 0). Kalanlar ya tipli ve bilinçli (`SecureFileService`, `FileCleanupJournal`, `LisansHelper` donanım kodu fallback’i) ya da zararsız (`FaturaSablonService:1082` renk ayrıştırma, `WebhookService:327` yanıt gövdesi). Scraper servislerinde 19 adet duruyor (P3). |
| E-06 | 🟡 Açık | Paralel `ContinueWith(t => t.Result)`: `ServisOperasyon/KontratList.razor:266-268`, `PuantajDetay.razor:501, 524` |
| E-07 | 🟢 Düşük risk | `CalismaPuantaji.razor:1145` varsayılan `1`, ancak `OnInitializedAsync` içinde ilk erişilebilir firmaya ayarlanıyor (`:1231`). Kozmetik. |
| E-08 | 🔴 Açık | `tests.yml` hâlâ yalnız `main` push/PR’ında çalışıyor; diğer dallarda kontrol yok |
| E-09 | 🟡 Açık | Yalnız `AddHealthChecks()` (`Program.cs:601`); merkezi log/hata izleme ve kritik roller için zorunlu 2FA bulunamadı |

---

## 4. Ticari / hukuki satış hazırlığı (T-01..T-08)

| Kod | Öncelik | Eksik | Durum |
|---|---|---|---|
| T-01 | P0 | Ürün kapsamı / paket tanımı (modüller, DB, çevrimdışı) | 🟡 Kısmen: [A-29-31 kararları](A-29-31-URUN-KARARLARI.md) çevrimdışı ve depolama kapsamını tanımlıyor; modül paketleri ve DB sağlayıcı kararı (A-28) yok |
| T-02 | P0 | EULA hukuki onayı | 🔴 `LICENSE` var, onay kaydı yok |
| T-03 | P0 | KVKK aydınlatma/açık rıza/DPA/imha politikası | 🔴 Yok |
| T-04 | P1 | Son kullanıcı kılavuzu | 🔴 Yalnız puantaj/fatura kılavuzu var |
| T-05 | P1 | Müşteri BT için kurulum/yedek/kurtarma runbook’u | 🟡 Recovery betikleri ve `setup/README.md` güncellendi; müşteri için tek parça runbook yok |
| T-06 | P1 | Destek/SLA/güncelleme politikası | 🔴 Yok |
| T-07 | P1 | Rent a Car belge şablonları hukuk onayı | 🔴 README hâlâ “hukuken onaylı değildir” diyor |
| T-08 | P2 | Sürüm notları / CHANGELOG, demo senaryosu | 🔴 Yok |

---

## 5. Öncelikli yapılacaklar (güncel sıra)

1. **Hemen (bugün):** B-01 — `postgres-write-audit.sql` dosyasını commit et; CI’nin Tests ve Docker işlerinin yeşile döndüğünü doğrula. B-02 NuGet Audit restore düzeltmesi.
2. **P0 kabul:** A-01, A-02, A-05, A-06 kanıtları; A-03/A-04 gerçek restore provası.
3. **Kullanıcıya görünen eksikler:** E-01, E-02 (denetim izi), E-03 (12 çalışmayan buton — uygula ya da gizle).
4. **P1 kod/veri:** A-10, A-15, A-16; E-06; E-08.
5. **Ticari paket:** T-01..T-07 (kod işleriyle paralel).
6. **Teslim:** B-03 kanıt bağlantıları, A-23 teslim commit’i ve sürüm etiketi.

---

## 6. Satış kabul kontrol listesi

- [ ] `main` temiz checkout’ta derleniyor; Tests, Docker ve NuGet Audit CI yeşil (B-01, B-02)
- [ ] Tüm P0 maddeleri (A-01..A-06, T-01..T-03) kanıtla kapandı
- [ ] Kullanıcıya görünen yarım özellik kalmadı (E-03, E-04)
- [ ] Denetim izi gerçek kullanıcıyı yazıyor (E-01, E-02)
- [ ] Temiz makinede müşteri paketi kuruldu, güncellendi, tam kurtarma provası yapıldı (A-03, A-04, A-21)
- [ ] Kullanıcı kılavuzu, KVKK metinleri, SLA ve EULA müşteri paketinde (T-02..T-06)
- [ ] Doküman/kanıt bağlantıları geçerli, teslim commit’i etiketlendi (A-23, B-03)

*Bu liste statik incelemeye ve GitHub Actions kayıtlarına dayanır. “🟡 kabul” maddeleri yalnızca çalışan ortamda, kayıtlı kanıtla kapatılabilir.*
