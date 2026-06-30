# 🤝 Katkı Rehberi

MKFiloServis'e katkıda bulunmak istediğiniz için teşekkürler! 🎉  
Bu doküman, projeye **kaliteli ve tutarlı** katkı sağlayabilmeniz için izlemeniz gereken adımları açıklar.

---

## 📋 İçindekiler

- [Davranış Kuralları](#-davranış-kuralları)
- [Nasıl Katkıda Bulunabilirim?](#-nasıl-katkıda-bulunabilirim)
- [Geliştirme Ortamı](#-geliştirme-ortamı)
- [Branch & Commit Standartları](#-branch--commit-standartları)
- [Pull Request Süreci](#-pull-request-süreci)
- [Kod Stili](#-kod-stili)
- [Test Yazma](#-test-yazma)
- [Dokümantasyon](#-dokümantasyon)

---

## 🌟 Davranış Kuralları

Bu projede **saygılı, kapsayıcı ve yapıcı** bir iletişim bekliyoruz. Her türlü taciz, ayrımcılık veya saygısız davranış kabul edilmez.

---

## 🛠️ Nasıl Katkıda Bulunabilirim?

### 🐛 Hata Bildirimi

1. [Açık issue'ları](https://github.com/karamur/MKFiloServis/issues) kontrol edin — belki aynı sorun bildirilmiştir.
2. Yoksa **Bug Report** template ile yeni issue açın.
3. Tekrar üretebilmemiz için **adım adım** açıklayın.

### ✨ Yeni Özellik Önerisi

1. Önce bir **Feature Request** issue'su açın.
2. Tasarım onaylandıktan sonra PR gönderin (büyük değişiklikler için bu kritiktir).

### 📝 Dokümantasyon

Yazım hatası, eksik bilgi veya örnek isteği için doğrudan PR açabilirsiniz.

### 🔐 Güvenlik Açığı

Lütfen issue açmayın. Detaylar için [`SECURITY.md`](SECURITY.md) dosyasına bakın.

---

## 💻 Geliştirme Ortamı

### Gereksinimler

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022/2026 **Community** ya da VS Code + C# Dev Kit
- (Önerilen) PostgreSQL 14+ veya SQLite

### İlk Kurulum

```bash
git clone https://github.com/karamur/MKFiloServis.git
cd MKFiloServis
dotnet restore
dotnet build
```

---

## 🌿 Branch & Commit Standartları

### Branch İsimlendirme

| Tür | Örnek |
|---|---|
| Yeni özellik | `feature/araclar-yakit-grafigi` |
| Hata düzeltme | `fix/login-sonsuz-dongu` |
| Refactor | `refactor/navmenu-yetki-cache` |
| Doküman | `docs/readme-kurulum-bolumu` |
| Performans | `perf/fatura-listesi-sorgu` |
| Test | `test/cari-servis-coverage` |
| Yapılandırma | `chore/dependabot-konfigurasyon` |

### Commit Mesajları — [Conventional Commits](https://www.conventionalcommits.org/)

```
<tip>(<kapsam>): <kısa açıklama>

[opsiyonel detay]
[opsiyonel BREAKING CHANGE: ...]
```

**Tipler:**
- `feat` — yeni özellik
- `fix` — hata düzeltme
- `docs` — dokümantasyon
- `style` — kod stili (boşluk, format)
- `refactor` — davranışı değiştirmeyen iyileştirme
- `perf` — performans
- `test` — test ekleme/düzeltme
- `chore` — build/araç/konfigürasyon
- `ci` — CI/CD pipeline

**Örnekler:**
```
feat(araclar): yakıt tüketim grafiği eklendi
fix(login): yanlış parolada sonsuz döngü düzeltildi
refactor(navmenu): yetki kontrolü SemaphoreSlim ile serileştirildi
docs(readme): mimari diyagramı güncellendi
perf(faturalar): liste sorgusu indekslendi (~%70 hızlanma)
```

---

## 🔄 Pull Request Süreci

```bash
# 1) Fork → klonla
git clone https://github.com/<kullanici-adin>/MKFiloServis.git

# 2) Yeni dal aç
git checkout -b feature/harika-ozellik

# 3) Geliştir, commit'le
git commit -m "feat(modul): harika özellik eklendi"

# 4) Fork'una push'la
git push origin feature/harika-ozellik

# 5) GitHub üzerinden PR aç
```

### ✅ PR Açmadan Önce Kontrol Listesi

- [ ] `dotnet build` başarılı
- [ ] `dotnet test` tüm testler geçiyor
- [ ] Yeni kod için **birim testi** eklendi
- [ ] `.editorconfig` kurallarına uyuldu
- [ ] Commit mesajları **Conventional Commits** formatında
- [ ] CHANGELOG.md güncellendi (önemli değişiklik ise)
- [ ] Hassas bilgi (parola, token, API key) eklenmedi
- [ ] Gereksiz dosya/build çıktısı commit'lenmedi

### 📝 PR Açıklaması

PR template'i otomatik yüklenir; tüm alanları **eksiksiz** doldurun.

---

## 🎨 Kod Stili

### Genel Kurallar

- ✏️ **`.editorconfig`** dosyasına uyun (otomatik formatlanır).
- 🔤 İsimlendirme: Microsoft [.NET Naming Guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/naming-guidelines).
- 🇹🇷 UI metinleri **Türkçe**, kod ve commit mesajları **İngilizce öncelikli** (Türkçe de kabul).
- 📦 `using` direktifleri sıralı, gereksiz olanlar kaldırılmış olmalı.
- 🚫 `Console.WriteLine` kullanmayın → `ILogger<T>` kullanın.
- 🚫 Boş `catch { }` blokları **yasak** → en azından `_logger.LogWarning(ex, "...")`.

### Blazor Bileşenleri

- Bileşen başına en fazla **~400 satır** (üzeri için ayırın).
- `OnInitializedAsync` içinde uzun süreli çağrıları **try/catch + log** ile sarın.
- `IDisposable` / `IAsyncDisposable` gerektiğinde **mutlaka** uygulayın.
- JS interop teardown'da `JSDisconnectedException` yakalanmalı.

### Async/Await

- Senkron API üzerinde `.Result` / `.Wait()` **yasak**.
- `ConfigureAwait(false)` kütüphane kodunda zorunlu, UI kodunda gereksiz.
- `CancellationToken` parametresini **destekleyin** ve **iletilin**.

---

## 🧪 Test Yazma

```bash
# Tek bir test çalıştır
dotnet test --filter "FullyQualifiedName~CariServiceTests"

# Coverage ile
dotnet test --collect:"XPlat Code Coverage"
```

**Yeni test kuralları:**
- xUnit kullanın (`[Fact]` veya `[Theory]`).
- AAA düzeni: **Arrange → Act → Assert**.
- Test adı: `Method_Senaryo_BeklenenDavranis` — örn: `Login_YanlisParola_BasarisizDoner`.
- Bağımlılıkları **mock**'layın (`Moq` / `NSubstitute`).
- E2E testler `Tests/PlaywrightSmoke` altına eklenir.

---

## 📚 Dokümantasyon

- Public API'ler için **XML doc comments** ekleyin.
- Yeni modül eklediyseniz README'nin **Modüller** bölümünü güncelleyin.
- Kullanıcıya yönelik değişiklikleri **CHANGELOG.md** içine işleyin.

---

## 🙏 Teşekkürler

Her katkı (kod, doküman, hata raporu, fikir) bizim için değerlidir. ❤️  
Sorularınız için: [Issues](https://github.com/karamur/MKFiloServis/issues) ya da [Discussions](https://github.com/karamur/MKFiloServis/discussions).

