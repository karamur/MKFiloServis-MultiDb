# 🔐 Güvenlik Politikası

MKFiloServis ekibi olarak güvenliği **en yüksek öncelik** olarak ele alıyoruz. Bu doküman, güvenlik açıklarını sorumlu bir şekilde nasıl bildirebileceğinizi açıklar.

---

## 📦 Desteklenen Sürümler

Aşağıdaki sürümler güvenlik güncellemeleri açısından **aktif olarak desteklenir**:

| Sürüm | Destek Durumu |
|---|---|
| `1.0.x` | ✅ Aktif destek |
| `1.1.x` (geliştirme) | ✅ Aktif destek |
| `< 1.0` | ❌ Desteklenmiyor |

---

## 🚨 Güvenlik Açığı Bildirimi

> ⚠️ **Lütfen güvenlik açıklarını GitHub Issues üzerinden açıkça bildirmeyin.**  
> Açık olarak paylaşılan zafiyetler, yama yayınlanmadan önce kötüye kullanılabilir.

### 📬 Nasıl Bildirilir?

**1. Tercih edilen yöntem — GitHub Private Vulnerability Reporting:**

[Yeni güvenlik bildirimi gönder](https://github.com/karamur/MKFiloServis/security/advisories/new)

**2. Alternatif — E-posta:**

Repository sahibiyle özel iletişim üzerinden ulaşın (GitHub profilinden e-posta).

### 📝 Bildiriminizde Yer Alması Gerekenler

- 🎯 Etkilenen sürüm(ler) ve bileşen(ler)
- 🔄 Açığı **adım adım** yeniden üretme
- 💥 Olası etki (veri sızıntısı, RCE, yetki yükseltme vb.)
- 🛡️ (Varsa) önerilen düzeltme veya azaltma yöntemi
- 🆔 (Varsa) PoC kodu / ekran görüntüsü

---

## ⏱️ Yanıt Süreleri

| Aşama | Hedef Süre |
|---|---|
| 📥 İlk yanıt (alındı bildirimi) | **48 saat içinde** |
| 🔍 İlk değerlendirme | **5 iş günü içinde** |
| 🛠️ Yama / hafifletme | Önem derecesine göre **7 — 30 gün** |
| 📢 Kamuya açıklama | Yama yayınından sonra **koordineli** |

---

## 🏆 Tanıma (Hall of Fame)

Sorumlu ifşa (responsible disclosure) yapan araştırmacıların adlarını (izinleri dahilinde) [`SECURITY-CREDITS.md`](SECURITY-CREDITS.md) ya da release notlarında yayınlarız.

---

## 🛡️ Güvenli Kullanım Önerileri

MKFiloServis kullanıcılarına yönelik öneriler:

| Kategori | Öneri |
|---|---|
| 🔑 **JWT Secret** | En az 32 karakter, kurulum başına benzersiz |
| 🌐 **HTTPS** | Üretimde **zorunlu**, HSTS açık |
| 🔒 **DB Parolası** | Güçlü + **environment variable** ile yönetilmeli |
| 🧪 **Bağımlılıklar** | `dotnet list package --vulnerable` ile düzenli tarayın |
| 🔐 **Data Protection Keys** | Yedekleyin ve sızdırmayın |
| 👮 **Roller** | En az ayrıcalık ilkesi (Principle of Least Privilege) |
| 📝 **Audit Log** | Üretimde **mutlaka** etkin tutun |
| 🗄️ **Backup** | Şifreli + offsite kopya bulundurun |

---

## 🤝 Teşekkürler

Sorumlu ifşada bulunan tüm güvenlik araştırmacılarına teşekkür ederiz.  
Birlikte daha güvenli bir ekosistem inşa ediyoruz. ❤️

