# Çoklu Kullanıcı Oturum Testi Senaryoları

## Test Ortamı Hazırlığı

### Gereksinimler
- En az 2 farklı PC veya tarayıcı (`Chrome`, `Firefox`, `Edge`)
- 3 farklı kullanıcı hesabı (`Admin`, `Muhasebeci`, `Şoför`)

### Test Kullanıcıları
| Kullanıcı Adı | Şifre    | Rol         | Test PC |
|---------------|----------|-------------|---------|
| admin         | admin123 | Admin       | PC-1    |
| muhasebe      | test123  | Muhasebeci  | PC-2    |
| sofor1        | test123  | Şoför       | PC-3    |

### Yapılan Güvenlik Değişiklikleri

#### 1. Circuit-Scoped Oturum Yönetimi
- **Önceki:** `static` değişkenler kullanılıyordu, tüm kullanıcılar aynı oturumu paylaşıyordu.
- **Şimdi:** Her Blazor circuit (tarayıcı bağlantısı) kendi oturumunu yönetiyor.

#### 2. Session Storage Kullanımı
- Her tarayıcı ve PC kendi `Session Storage` alanını kullanıyor.
- Tarayıcı kapatıldığında oturum otomatik sonlanıyor.
- 24 saatlik session süresi uygulanıyor.

#### 3. Benzersiz Session ID
- Her giriş için benzersiz `GUID` oluşturuluyor.
- Oturum izleme ve güvenlik logları tutuluyor.

#### 4. API Token Güvenliği (Mobil)
- Her cihaz için benzersiz `Device ID` üretiliyor.
- `HMAC` imzalı tokenlar kullanılıyor.
- 7 günlük token süresi uygulanıyor.

---

## Test Senaryoları

### Senaryo 1: Bağımsız Oturum Kontrolü
**Amaç:** Farklı PC'lerdeki kullanıcı oturumlarının birbirini etkilemediğini doğrulamak.

**Adımlar:**
1. **PC-1'de** `admin` kullanıcısı ile giriş yap.
2. **PC-2'de** `muhasebe` kullanıcısı ile giriş yap.
3. **PC-1'de** sayfayı yenile (`F5`).
4. **PC-1'de** kullanıcının hâlâ `admin` olduğunu doğrula.
5. **PC-2'de** sayfayı yenile (`F5`).
6. **PC-2'de** kullanıcının hâlâ `muhasebe` olduğunu doğrula.

**Beklenen Sonuç:** Her PC kendi kullanıcı oturumunu korumalı.

---

### Senaryo 2: Yetki İzolasyonu
**Amaç:** Bir kullanıcının yetkilerinin başka bir kullanıcıyı etkilemediğini doğrulamak.

**Adımlar:**
1. **PC-1'de** `admin` ile giriş yap; tüm menüler görünür olmalı.
2. **PC-2'de** `sofor1` ile giriş yap; sadece şoför menüleri görünür olmalı.
3. **PC-1'de** admin paneline git; erişim olmalı.
4. **PC-2'de** admin paneline gitmeyi dene; erişim engellenmeli.
5. **PC-1'de** yeni bir kullanıcı oluştur.
6. **PC-2'de** kullanıcı listesine erişim olmamalı.

**Beklenen Sonuç:** Her kullanıcı kendi rolüne uygun yetkilere sahip olmalı.

---

### Senaryo 3: Tarayıcı Kapatma ve Oturum Sonlandırma
**Amaç:** Tarayıcı kapatıldığında oturumun sonlanmasını doğrulamak.

**Adımlar:**
1. **PC-1'de** `admin` ile giriş yap.
2. **PC-1'de** tarayıcıyı tamamen kapat.
3. **PC-1'de** tarayıcıyı tekrar aç ve uygulamaya git.
4. Login sayfası gösterilmeli.

**Beklenen Sonuç:** `Session Storage` temizlenmeli ve yeni giriş istenmeli.

---

### Senaryo 4: Aynı PC'de Farklı Tarayıcılar
**Amaç:** Aynı PC'de farklı tarayıcılarda bağımsız oturum kontrolü yapmak.

**Adımlar:**
1. **Chrome'da** `admin` ile giriş yap.
2. **Firefox'ta** `muhasebe` ile giriş yap.
3. Her iki tarayıcıda da kullanıcı bilgilerini kontrol et.
4. Chrome'da çıkış yap.
5. Firefox'ta oturum devam etmeli.

**Beklenen Sonuç:** Her tarayıcı bağımsız oturum yönetmeli.

---

### Senaryo 5: Aynı Kullanıcı Farklı PC'lerde
**Amaç:** Aynı kullanıcının farklı PC'lerde bağımsız oturum açabilmesini doğrulamak.

**Adımlar:**
1. **PC-1'de** `admin` ile giriş yap.
2. **PC-2'de** `admin` ile giriş yap (aynı kullanıcı).
3. Her iki PC'de de oturum aktif olmalı.
4. **PC-1'de** çıkış yap.
5. **PC-2'de** oturum devam etmeli.

**Beklenen Sonuç:** Her PC bağımsız `Session ID` değerine sahip olmalı.

---

### Senaryo 6: Sayfa Yenileme Sonrası Yetki Kontrolü
**Amaç:** Sayfa yenilendiğinde yetkilerin korunduğunu doğrulamak.

**Adımlar:**
1. **PC-1'de** `admin` ile giriş yap.
2. Kullanıcı yönetimi sayfasına git.
3. Sayfayı 5 kez yenile (`F5`).
4. Her yenilemede admin yetkilerinin korunduğunu doğrula.
5. **PC-2'de** `sofor1` ile giriş yap.
6. Ana sayfada 5 kez yenile.
7. Her yenilemede şoför rolünün korunduğunu doğrula.

**Beklenen Sonuç:** Yetkiler tutarlı kalmalı.

---

### Senaryo 7: Eşzamanlı İşlem Testi
**Amaç:** Farklı kullanıcıların eşzamanlı işlemlerinde veri bütünlüğünü doğrulamak.

**Adımlar:**
1. **PC-1'de** `admin` ile giriş yap.
2. **PC-2'de** `muhasebe` ile giriş yap.
3. Her iki PC'de aynı anda farklı işlemler yap:
   - PC-1: Yeni araç ekle.
   - PC-2: Mevcut fatura oluştur.
4. İşlemlerin başarılı olduğunu doğrula.
5. Her kullanıcının kendi işlemini gördüğünü doğrula.

**Beklenen Sonuç:** İşlemler birbirini etkilememeli.

---

### Senaryo 8: Session Süresi Testi (24 saat)
**Amaç:** 24 saatlik session süresinin çalıştığını doğrulamak.

**Adımlar:**
1. Giriş yap ve session bilgilerini kaydet.
2. 24 saatten fazla bekle veya test için sistem saatini değiştir.
3. Sayfayı yenile.
4. Yeniden giriş istenmeli.

**Beklenen Sonuç:** Session süresi dolunca oturum sonlanmalı.

---

### Senaryo 9: Beni Hatırla Özelliği
**Amaç:** Kullanıcı adının hatırlanmasını doğrulamak.

**Adımlar:**
1. Login sayfasında `Beni Hatırla` seçeneğini işaretle.
2. `admin` ile giriş yap.
3. Çıkış yap.
4. Login sayfasına dön.
5. Kullanıcı adı alanı `admin` ile dolu olmalı.

**Beklenen Sonuç:** `Local Storage` içinde kullanıcı adı saklanmalı.

---

### Senaryo 10: Mobil API Token Güvenliği
**Amaç:** API tokenlarının cihaz bazlı çalıştığını doğrulamak.

**Adımlar:**
1. Mobil cihaz 1'den `/api/auth/login` çağrısı yap.
2. Mobil cihaz 2'den `/api/auth/login` çağrısı yap (aynı kullanıcı).
3. Her cihazın farklı token aldığını doğrula.
4. Cihaz 1'in tokenını cihaz 2'de kullanmayı dene.
5. Her cihaz sadece kendi tokenı ile çalışmalı.

**Beklenen Sonuç:** Tokenlar cihaz bazlı ve bağımsız olmalı.

---

## Test Kontrol Listesi

| Test Senaryosu | PC-1 | PC-2 | PC-3 | Sonuç |
|----------------|------|------|------|-------|
| Senaryo 1: Bağımsız Oturum | [ ] | [ ] | - | [ ] |
| Senaryo 2: Yetki İzolasyonu | [ ] | [ ] | [ ] | [ ] |
| Senaryo 3: Tarayıcı Kapatma | [ ] | - | - | [ ] |
| Senaryo 4: Farklı Tarayıcılar | [ ] | - | - | [ ] |
| Senaryo 5: Aynı Kullanıcı | [ ] | [ ] | - | [ ] |
| Senaryo 6: Sayfa Yenileme | [ ] | [ ] | - | [ ] |
| Senaryo 7: Eşzamanlı İşlem | [ ] | [ ] | - | [ ] |
| Senaryo 8: Session Süresi | [ ] | - | - | [ ] |
| Senaryo 9: Beni Hatırla | [ ] | - | - | [ ] |
| Senaryo 10: Mobil API | [ ] | [ ] | - | [ ] |

---

## Hata Durumunda

Eğer test başarısız olursa:

1. Tarayıcı geliştirici araçlarını aç (`F12`).
2. `Application > Session Storage` altında `CRMFiloServis_Session` anahtarını kontrol et.
3. `Application > Local Storage` altında `CRMFiloServis_RememberMe` anahtarını kontrol et.
4. `Console` sekmesindeki hata mesajlarını incele.
5. `Network` sekmesindeki API çağrılarını kontrol et.

### Session Storage İçeriği
```json
{
  "SessionId": "abc123...",
  "KullaniciId": 1,
  "GirisTarihi": "2024-01-15T10:30:00Z",
  "Expiry": "2024-01-16T10:30:00Z",
  "ClientInfo": "Circuit_..."
}
```

### Log Kontrolü
Uygulama loglarında oturum bilgileri:
```
[INF] Kullanici giris yapti: admin, Rol: Admin, SessionId: abc123...
[INF] Oturum yuklendi: admin, SessionId: abc123...
[INF] Kullanici cikis yapti: admin
```

---

## Teknik Notlar

### Oturum Yönetimi Mimarisi
```
                    +-----------------------------------+
                    |           Blazor Server          |
                    +-----------------------------------+
                                      |
            +-------------------------+-------------------------+
            |                         |                         |
    +---------------+         +---------------+         +---------------+
    |   Circuit 1   |         |   Circuit 2   |         |   Circuit 3   |
    |    (PC-1)     |         |    (PC-2)     |         |    (PC-3)     |
    +---------------+         +---------------+         +---------------+
            |                         |                         |
    +---------------+         +---------------+         +---------------+
    | Scoped        |         | Scoped        |         | Scoped        |
    | AuthProvider  |         | AuthProvider  |         | AuthProvider  |
    | Session: A1   |         | Session: B2   |         | Session: C3   |
    | User: Admin   |         | User: Muhasebe|         | User: Sofor   |
    +---------------+         +---------------+         +---------------+
```

### Storage Kullanımı
- **Session Storage:** Oturum verileri, tarayıcı kapatılınca silinir.
- **Local Storage:** `Beni Hatırla` tercihi kalıcı olarak tutulur.
- **Protected Storage:** Veriler şifreli saklanır.

### Güvenlik Özellikleri
1. Her circuit bağımsız oturum yönetir, `static` değişken kullanılmaz.
2. `Session ID` ile oturum takibi yapılır.
3. 24 saatlik session süresi uygulanır.
4. `HMAC` imzalı API tokenları kullanılır.
5. Cihaz bazlı token yönetimi uygulanır.
6. Başarısız giriş denemeleri sayaçla izlenir ve 5 deneme sonrası kilitlenir.
