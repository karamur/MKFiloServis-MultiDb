# Akış ve Entegrasyon Denetim Raporu

**Konu:** Cari, Fatura, Personel/Bordro, Puantaj/Hakediş ve Muhasebe modüllerindeki akışlar; projedeki tüm eşleştirmeler ve entegrasyonlar
**Tarih:** 2026-10-26
**Dal / HEAD:** `feat/maas-puantaj-hakedis-cari-guncellemeleri` @ `fbab7481` (denetim başında çalışma ağacı temizdi)
**Tür:** Salt okunur denetim. Hiçbir kaynak dosya değiştirilmedi, commit yapılmadı.
**Okuyucu:** ürün sahibi ve geliştirici ekip

> Dosya yolları kısaltılmıştır: `S/` = `MKFiloServis.Web/Services/`, `E/` = `MKFiloServis.Shared/Entities/`, `J/` = `MKFiloServis.Web/Jobs/`. Razor sayfaları dosya adıyla verilmiştir (`Components/Pages/` altındadır).

---

## 1. Yönetici özeti

**Genel hüküm.** Çekirdek kayıt işlemleri ve çok firmalı (kiracı) izolasyon sağlam çalışıyor; sorunların büyük bölümü modüllerin **birleştiği dikiş yerlerinde**. Fatura, ödeme, fiş, puantaj, hakediş ve bordro tek tek yazılmış; birbirine bağlanan halkaların bir kısmı kopuk, bir kısmı aynı işi farklı kurallarla yapıyor. Bu yüzden ekranlar tek başına doğru görünürken, bir akışın sonunda (örneğin ödeme sonrası fatura durumu, mizan, KDV beyanı) rakamlar birbirini tutmuyor.

**Toplam 61 bulgu:** 4 kritik, 28 yüksek, 26 orta, 3 düşük. FAT-11, FAT-12, MUH-03 ve JOB-01 (kiracı filtresi davranışı) çalıştırma deneyleriyle doğrulandı; FAT-02 ve FAT-04'ün toplama formülü deneyle sınandı (Ek A). Kalan bulgular kodla izlendi; her bulguda güven düzeyi belirtilmiştir.

**Beş ana tema**

1. **Kopuk halkalar.** Ödeme kaydı faturayı kapatmıyor (FAT-03); ödeme/tahsilat muhasebe fişi üretmiyor (MUH-07); Puantaj Excel Grid hakediş/fatura/maaşı beslemiyor (PUA-01); fatura iptal/iade akışı yok (FAT-05).
2. **Aynı kavram, birden çok kural.** Cari→hesap eşleştirmesi 6 şema, personel→hesap 5 şema, masraf→770.xx 3 çelişkili tablo, fiyat önceliği iki ters sıra, Borç/Alacak sütunu iki ekranda ters, e-posta için 3 SMTP yolu.
3. **Tutar anlamı karışık.** KDV dahil/hariç toplamlar karışıyor ve KDV çift sayılıyor (FAT-04); tevkifatlı faturada fiş eksik tutarlı (MUH-03); elle girilen faturada toplam 0 (FAT-02); düzenlemede kalemler kaydedilmiyor (FAT-01).
4. **Sessiz hata ve sahte başarı.** WhatsApp hiçbir şey göndermeden "Gönderildi" yazıyor (INT-01); fiş ve stok hataları `Debug.WriteLine` ile yutuluyor (FAT-10); toplu e-posta tek başarıyla "başarılı" sayılıyor (INT-02).
5. **Yetki ve arka plan bağlamı.** Kullanıcı/rol yönetiminde yazma, düzenleme ve silme izinleri tanımlı ama uygulanmıyor, sayfa yalnız okuma iznine bakıyor (AUTH-01); servislerin çoğunda izin denetimi yok (AUTH-02); zamanlanmış işler firma bağlamı olmadan çalıştığı için kiracı filtresi tüm satırları gizliyor (JOB-01).

### 1.1 İlk on bulgu

| # | ID | Başlık | Önem |
|---|----|--------|------|
| 1 | FAT-03 | Ödeme/tahsilat faturayı kapatmıyor; iki "gerçek" çelişiyor | Kritik |
| 2 | FAT-01 | Fatura düzenlemede kalem değişiklikleri kaydedilmiyor | Kritik |
| 3 | FAT-02 | Elle girilen faturada toplamlar 0 yazılıyor | Kritik |
| 4 | INT-01 | WhatsApp gönderimi simülasyon; "Gönderildi" yalan | Kritik |
| 5 | FAT-04 | KDV çift sayımı (Proforma→Fatura, Puantaj fatura taslağı) | Yüksek |
| 6 | MUH-03 | Tevkifatlı faturada fiş tutarları eksik (deneyle doğrulandı) | Yüksek |
| 7 | JOB-01 | Zamanlanmış işler kiracı bağlamı olmadan boş veri görüyor | Yüksek |
| 8 | MUH-07 | Tahsilat/tediye için muhasebe fişi hiç üretilmiyor | Yüksek |
| 9 | MUH-09 | Muhasebe dönem kilidi uygulanmıyor | Yüksek |
| 10 | AUTH-01 | Kullanıcı/rol yönetiminde yazma izinleri uygulanmıyor (yetki yükseltme) | Yüksek |

### 1.2 Satış açısından

Bu bulgular ürünün çalışmadığı anlamına gelmiyor; **hangi iddiaların güvenle yapılabileceğini** belirliyor. Bölüm 11 satış metinlerinde kullanılabilecek ve kullanılmaması gereken ifadeleri listeler. Repo kökündeki `SATIS_HAZIRLIK.md` sürüm kararını ayrıca tutar; bu rapor o karara girdi sağlar, onun yerine geçmez.

---

## 2. Kapsam, yöntem ve güven düzeyleri

**İncelenen alanlar:** Cari, Fatura (manuel, XML/UBL-TR, Excel, Luca, API), Muhasebe (fiş, mizan, KDV, hesap planı, Kolay Muhasebe), Personel/Bordro/Maaş, Puantaj (Excel Grid, Operasyon Merkezi, hakediş), Banka/Kasa ve ekstre içe aktarma, e-posta/SMS/WhatsApp/Webhook/Luca entegrasyonları, 11 Quartz işi, yetkilendirme ve kiracı filtresi.

**Yöntem**

1. Kod okuma: servisler, sayfalar, entity'ler, `ApplicationDbContext`, `Program.cs`.
2. Karşılaştırmalı tarama: aynı kavramın (cari hesabı, masraf hesabı, fiyat, bakiye) uygulandığı tüm yerler yan yana konarak tutarlılık denetlendi.
3. Üç küçük çalıştırma deneyi (Ek A): UBL-TR ayrıştırma ve fiş tutarları, `FindAsync` ve kiracı filtresi, bağlamsız kapsamda kiracı filtresi.

**Güven kodları**

| Kod | Anlam |
|-----|-------|
| D | Çalıştırılarak doğrulandı |
| K | Kodla izlendi (çağrı zinciri baştan sona okundu) |
| O | Olası; kod işaret ediyor ama koşul doğrulanmadı |

**Önem ölçütleri**

| Önem | Ölçüt |
|------|-------|
| Kritik | Normal kullanımda doğrudan tetiklenir; veri kaybı, yanlış mali kayıt veya güvenlik açığı |
| Yüksek | Yanlış muhasebe/mali sonuç ya da güvenlik zayıflığı; yaygın ama koşula bağlı |
| Orta | Sınırlı koşulda ortaya çıkan hata, tutarsızlık veya yan etki |
| Düşük | Kozmetik veya bakım borcu |

**Sınırlar (denetlenmeyenler)**

- Canlı veritabanı okunmadı; uygulama üzerinde uçtan uca kullanıcı senaryosu yürütülmedi. Uygulama günlük dosyası bulunamadığı için işlerin canlıdaki çıktısı doğrulanamadı.
- `EFaturaXmlService` (UBL-TR üretimi, 829 satır) ve `LucaPortalService`'in HTTP/oturum ayrıntıları yüzeysel incelendi.
- REST denetleyicileri yalnız yetkilendirme niteliğiyle bakıldı.
- Destek, EBYS, Rent a Car, CRM ve Holding iç işleyişi bu raporun konusu değil (Destek için 2026-10-22 tarihli ayrı denetim vardır).
- Test paketi bu denetimde çalıştırılmadı (bkz. Bölüm 12).

---

## 3. Akış haritaları

İşaretler: **Çalışır** · **Kısmi** (çalışır ama sorunlu) · **Kopuk** (bağlantı yok) · **Yok**

### 3.1 Servis geliri: fiyat → puantaj → hakediş → fatura → muhasebe → tahsilat

| Adım | Durum | Not | Bulgu |
|------|-------|-----|-------|
| Fiyat kaynağı (güzergâh / eşleştirme / sözleşme) | Kısmi | Puantaj ve hakediş farklı öncelik sırasıyla fiyat seçiyor | PUA-03 |
| Puantaj üretimi (`FiloGunlukPuantaj`, Operasyon Merkezi) | Çalışır | Gerçek akış; onay/fatura bayrakları hiç atanmıyor | OPR-01 |
| Puantaj Excel Grid (`PuantajKayit`) | Kopuk | Kaydet yalnız kendi tablosunu yazar | PUA-01, PUA-04 |
| Puantaj → Hakediş | Çalışır | KDV oranı %20'ye sabit | HKD-03 |
| Hakediş → Fatura | Kısmi | `FaturaService.CreateAsync` atlanıyor | HKD-01 |
| Hakediş → Muhasebe fişi | Kısmi | Cari satırı prefix tahminiyle; çift kayıt riski | HKD-02 |
| Fatura → Muhasebe fişi | Kısmi | Yalnız ayar açıksa otomatik (`FaturaOtomatikMuhasebeFisi` varsayılan kapalı) ya da Muhasebeleştirme sayfasıyla | MUH-03, MUH-04 |
| Tahsilat → Fatura kapanışı | Kopuk | Hareket yazılır, eşleştirme yazılmaz | FAT-03 |
| Tahsilat → Muhasebe fişi | Yok | `CreateTahsilatFisiAsync` hiç çağrılmıyor | MUH-07 |
| Banka ekstresi → Hareket → Cari | Kısmi | Mükerrer yükleme koruması yok; unvan tahmini | BNK-01, BNK-02 |
| Cari bakiye | Kısmi | İki ekranda sütun anlamı ters; iade/iptal/döviz bilinmiyor | CAR-03, CAR-04 |

### 3.2 Gelen fatura / gider

| Adım | Durum | Not | Bulgu |
|------|-------|-----|-------|
| Fatura girişi (XML, Excel, Luca, manuel) | Kısmi | Her yol farklı eksik: kalemsiz Excel, KDV oranı 0 (XML), toplam 0 (manuel) | FAT-02, FAT-11, FAT-17 |
| Cari eşleştirme | Kısmi | Unvan `ToLower` eşitliği; mükerrer cari | FAT-15 |
| Cari muhasebe hesabı açma | Kısmi | Altı farklı şema | MUH-01 |
| Kalem sınıflandırma | Kısmi | Anahtar sözcük; Türkçe `İ` kaçırılır; 253/254 | FAT-12 |
| Fiş üretimi | Kısmi | Hata yutulur; tek gider hesabı | FAT-10, MUH-04 |
| Stok hareketi | Kısmi | Hatalar yutulur | FAT-10 |
| Ödeme → Fatura kapanışı / fiş | Kopuk / Yok | Bkz. 3.1 | FAT-03, MUH-07 |
| İptal / iade / ters fiş | Yok | `IptalEdildi` atanmıyor, `IptalFisId` kullanılmıyor | FAT-05 |
| KDV beyanı | Kısmi | Fatura başlıkları + defter hesapları karışık | MUH-10 |

### 3.3 Personel / maaş

| Adım | Durum | Not | Bulgu |
|------|-------|-----|-------|
| Personel puantajı → Bordro | Çalışır | Bu denetimde ayrıntılı izlenmedi | — |
| Bordro onayı → Muhasebe fişi | Kısmi | Giderler 770.01 (varsayılan planda Kira) gibi varsayılanlara gidiyor | PER-02 |
| Bordro onay iptali | Kısmi | Fiş ters çevrilmiyor; yeniden onayda eski fiş korunuyor | PER-03 |
| Maaş snapshot → Muhasebe fişi | Kısmi | Bordro fişinden bağımsız ikinci kaynak; çift kayıt koruması yok | PER-02 |
| Personel muhasebe hesabı | Kısmi | Beş farklı şema | PER-01 |
| Banka ödeme listesi | Kısmi | Maaş/Ödeme Yönetimi'nden farklı (eski) mantık | PER-04 |

### 3.4 Araç masrafı

Araç masrafı → muhasebe fişi (`AracMasrafService`) varsayılan hesap planıyla uyumlu 770.06–770.09 kodlarını kullanıyor; ancak Kolay Muhasebe sihirbazı aynı masraf türleri için farklı kod tablosu kullanıyor (MUH-05). Cari hesabı bu yolda üçüncü bir şemayla (`320`+`cari.Id`) açılıyor (MUH-01).

---

## 4. Eşleştirme envanteri

### 4.1 Cari ↔ muhasebe hesabı (7 uygulama)

| # | Yer | Şema | `Cari.MuhasebeHesapId` ile ilişki |
|---|-----|------|-----------------------------------|
| 1 | `S/CariService.cs:530-784` `CreateMuhasebeHesapAsync` | `{MusteriPrefix}.{NNN}` (ör. 120.01.001) | Yazar; aynı ünvanlı hesabı yeniden kullanır; tüm hataları yutar (780-783) |
| 2 | `S/FaturaService.cs:1326-1346` XML içe aktarma | `GetSonrakiHesapKoduAsync(prefix)` | Yazar |
| 3 | `S/MuhasebeService.cs:988-1042` `GetOrCreateCariHesapAsync` | `120`/`320` + CariKodu'ndan alt kod | **Okumaz**; fatura, tahsilat, tediye, mahsup, masraf fişleri bunu kullanır |
| 4 | `S/AracMasrafService.cs:423-455` | `320` + `cari.Id` | Okumaz |
| 5 | `S/HakedisMuhasebeService.cs:80-85, 268-276` `FindHesap` | Prefix `StartsWith`, ilk eşleşen (sırasız) | Okumaz |
| 6 | `S/KolayMuhasebeService.cs:339-346` | `Cari.MuhasebeHesap.HesapKodu`, yoksa `120.01` | Okur |
| 7 | `S/CariService.cs:798-828` `FillMuhasebeBilgisiAsync` | Ünvan `Contains` ile tahmin | Tahmini yazar; `UpdateAsync:413` ile kalıcılaşır |

Sonuç: aynı cariye ait hareketler 3-4 farklı hesap kartına dağılır; mizan/muavin ile cari ekstresi uyuşmaz (MUH-01, MUH-02).

### 4.2 Personel ↔ muhasebe hesabı (5 uygulama)

| # | Yer | Şema |
|---|-----|------|
| 1 | `S/MuhasebeSnapshotService.cs:65` | `335.01.{PersonelId:D4}` |
| 2 | `S/SoforService.cs:938-939, 1048-1068, 1148` | `335.01.{id:D4}` / `195.01.{id:D4}` (1 ile uyumlu; "Personel Hesap Bağlantılarını Onar" aracı 924-976 sorunun bilindiğini gösterir) |
| 3 | `S/CariService.cs:546-552, 667-673` | `{PersonelPrefix}.{NNN}` (personel carisi) |
| 4 | `S/AracMasrafService.cs:457-470` | `335` + `sofor.Id` |
| 5 | `S/BordroService.cs:506-529` | `Sofor.MuhasebeHesapId` ya da `PersonelPrefix.*` + `HesapAdi == Ad Soyad`; bulunamazsa hata |

Müşteri/tedarikçi için "onar" aracının eşdeğeri yok.

### 4.3 Masraf kategorisi → 770.xx

| Masraf | Varsayılan hesap planı (`MuhasebeService.cs:195-203`) | `AracMasrafService.cs:356-363`, `BudgetService.cs:1258-1263` | `KolayMuhasebeService.cs:1684-1703` |
|--------|------------------|----------------------|-----------------|
| Yakıt | 770.06 Yakıt | 770.06 | **770.01 (planda Kira)** |
| Bakım | 770.07 Bakım-Onarım | 770.07 | **770.02 (planda Elektrik)** |
| Tamir | 770.07 | 770.07 | **770.03 (planda Su)** |
| Sigorta | 770.08 Sigorta | 770.08 | **770.04 (planda Doğalgaz)** |
| Personel | 770.09 Personel | 770.09 | **770.06 (planda Yakıt)** |
| Lastik / Yedek parça | (tanımlı değil) | 770.07 | 770.07 / 770.08; 770.10-.12 planda yok |

Ek: `MuhasebeAyar.PersonelGiderHesabi` varsayılanı `770.01` (`E/Muhasebe.cs:257`), `AlisGiderHesabi` varsayılanı `153.01` (`E/Muhasebe.cs:220`; varsayılan planda 153.01 yok). Maaş snapshot fişi 770.01'i sabit kullanır (`S/MuhasebeSnapshotService.cs:91-111`).

### 4.4 Banka/kasa hesap kodu çözümü

| Yer | Kural |
|-----|-------|
| `MuhasebeService` tahsilat/tediye (884-986, ölü kod) | `102` / `100` üst hesap |
| `KolayMuhasebeService` (1705-1715) | `BankaHesap.VarsayilanMuhasebeKodu ?? 102.01 / 100.01 / 103.01 / 300.01` |
| `AracMasrafService` | `VarsayilanMuhasebeKodu ?? "102"` |
| `BudgetService` | `100.01` / `102.01` |

### 4.5 Fiyat önceliği

| Yer | Sıra |
|-----|------|
| Puantaj üretimi (`S/FiloKomisyonService.cs:690-697`) | güzergâh → eşleştirme |
| Hakediş (`Kapsam.cs:521-562`) | eşleştirme → puantaj → güzergâh |
| `PuantajHakedisSyncService` (ölü kod) | `BirimGelir` yoksa `BirimGider` |

### 4.6 Kalem sınıflandırma ve numaralama

| Konu | Uygulamalar |
|------|-------------|
| Kalem tipi/hesap | `DetermineKalemTipi` anahtar sözcükle (`S/FaturaService.cs:2896-2928`); `Arac` → 253, `Demirbas` → 255 (`:3012-3038`); fiş bu kalem hesabını kullanmaz (MUH-04) |
| Fatura numarası | `SF…` global sayaç (`ProformaFaturaService`), `SF_GIDEN…` firma sayacı (`FaturaForm`), `HKD-{Tip}-{YYYYMM}-{Id}` (hakediş) |
| Fiş numarası | `firmaId=0` sayaç (`MuhasebeService.cs:561-578`) ↔ firma bazlı sayaç (`HakedisMuhasebeService`); ay bilgisi "bugün"den (`:512`) |

### 4.7 Zincir bağlantı özeti

| Halka | Durum |
|-------|-------|
| `FiloGunlukPuantaj` → hakediş | Bağlı |
| `PuantajKayit` (Excel Grid) → hakediş / fatura / maaş | **Kopuk** |
| Hakediş → fatura | Bağlı (`CreateAsync` atlanır) |
| Fatura ↔ ödeme eşleştirmesi (`OdemeEslestirme`) | **Kopuk** (servis kayıtlı, kullanılmıyor) |
| Ödeme → muhasebe fişi | **Yok** |
| Bordro → fiş | Bağlı; iptalde ters fiş yok |
| Maaş snapshot → fiş | Bordrodan bağımsız ikinci kaynak |
| Banka ekstresi → hareket | Bağlı (mükerrer koruması yok) |

---

## 5. Entegrasyon envanteri

### 5.1 Dış kanallar

| Kanal | Gerçekten çalışıyor mu | Gizli bilgi saklama | Dayanıklılık | Bulgu |
|-------|------------------------|---------------------|--------------|-------|
| E-posta (SMTP) | Yapılandırılırsa evet; üç ayrı yol, varsayılan kapalı | `appsettings` `Email:*`; CRM yolu DB tablosunda düz metin | Toplu gönderimde kısmi başarı = başarı | INT-02, INT-03 |
| SMS (Netgsm, İleti Merkezi, Mutlucell, Twilio) | Evet (gerçek HTTP çağrıları, durum sağlayıcı yanıtına göre) | `SmsAyar.KullaniciAdi/ApiKey` düz metin | HTTP istemcileri zaman aşımsız; Netgsm bakiye URL'sinde parola | INT-03 |
| WhatsApp | **Hayır — simülasyon** | Ayar düz metin | — | INT-01 |
| Webhook (giden) | Evet; HMAC imza ve yeniden deneme var | Gizli anahtar DB'de düz metin | Yetki ve URL doğrulama yok | INT-04 |
| Luca Portal (e-Fatura/e-Arşiv) | Elle çalıştırma evet; gece işi bağlam sorunu | Parola `IDataProtector` ile korunuyor | — | JOB-01 |
| Banka ekstresi | Dosya içe aktarma (Excel/CSV/PDF) | — | Mükerrer koruması yok | BNK-01..03 |
| REST API (denetleyiciler) | Evet | Bearer şeması | `Licensed:*` politikası yalnız oturum + lisans | AUTH-02 |
| Yapay zekâ (Ollama) | İsteğe bağlı; kapalıysa sessizce geçer | — | — | — |

Denetimde Luca dışında bir e-fatura gönderim entegratörüne ve çevrimiçi banka (açık bankacılık) entegrasyonuna rastlanmadı (kapsamlı bir arama yapılmadı).

### 5.2 Zamanlanmış işler (Quartz)

| İş | Zamanlama | Varsayılan | Firma bağlamı / not |
|----|-----------|------------|---------------------|
| `AutoBackupJob` | 30 dk | Açık | Yedekleme; bu rapor kapsamı dışı |
| `DatabaseBackupJob` | 03:00 | Açık | Yedekleme; kapsam dışı |
| `CariHatirlatmaJob` | 30 dk | Açık | Firma döngüsü var, sorgular kiracı filtresine takılır (JOB-01) |
| `BelgeUyariJob` | `BelgeUyari:CheckIntervalHours` (24 sa) | Yalnız `BelgeUyari:EmailEnabled=true` ise kayıtlı | JOB-01; uygulama içi bildirimler de bu bayrağa bağlı |
| `GunlukOzetJob` | Günlük 08:00 | `GunlukOzet:Enabled=false` | JOB-01 + INT-01 (yalnız WhatsApp'a bağlı) |
| `ZamanliRaporJob` | Günlük 07:00 | `ZamanlıRapor:Enabled=false` | JOB-01 |
| `BakimPeriyotJob` | Günlük 09:30 | Açık | Log alanları koşulsuz `true` (INT-01) |
| `HoldingVeriToplamaJob` | Her ayın 1'i 02:07 | Açık | Kiracı filtresini bilinçli atlıyor (`S/HoldingService.cs:31,46`); doğru tasarlanmış tek iş |
| `LucaPortalSenkronJob` | Günlük 03:00 | Açık | JOB-01 |
| `DestekSlaKontrolJob` | 15 dk | Açık | Destek denetiminin konusu (ayrı rapor) |
| `PuantajAnomaliJob` | Pazartesi 06:00 | Açık | `TumTaramaAsync(yil, ay)` firma verilmeden çağrılıyor (JOB-01) |

Tüm işler `[DisallowConcurrentExecution]` ile işaretli.

---

## 6. Bulgular

### 6.1 Yetkilendirme

#### AUTH-01 — Kullanıcı ve rol yönetiminde yazma/düzenleme/silme izinleri uygulanmıyor
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** `KullaniciYonetimi.razor:15` sayfayı yalnız `kullanici.oku` ("Okuma") iznini arayan `YetkiKontrol` bileşeniyle sarar. `kullanici.yaz`, `kullanici.duzenle` ve `kullanici.sil` izinleri tanımlı (`E/KullaniciVeLisans.cs:638-641`) ve Rol Yönetimi'nde atanabiliyor, ancak kod tabanının hiçbir yerinde denetlenmiyor. Sayfa işleyicileri (`KullaniciYonetimi.razor:626-832`: `UpdateAsync`, `CreateAsync`, `SifreSifirlaAsync`, `ToggleAktifAsync`, `UpdateRolAsync`, `CreateRolAsync`, `DeleteRolAsync`, `SetRolYetkileriAsync`) ve `S/KullaniciService.cs` yöntemleri (`UpdateAsync` 118-137 `RolId`, `SoforId`, `Aktif` alanlarını kopyalar; ayrıca `DeleteAsync`, `CreateRolAsync`, `UpdateRolYetkileriAsync`, `SetRolYetkileriAsync`) hiçbir yazma izni denetlemez.
- **Senaryo:** Yalnızca "Okuma" izni verilmiş bir rol (ör. denetçi veya yardımcı) sayfayı açabildiği için kendi rolünü Admin yapabilir, başka kullanıcının şifresini sıfırlayabilir, rol izinlerini genişletebilir. Yönetici bu rolü "salt okunur" sandığı için risk fark edilmez.
- **Öneri:** Sayfadaki her eylemi ilgili izne bağla (`yaz` oluşturma; `duzenle` güncelleme, şifre, rol atama; `sil` silme) ve aynı denetimi `KullaniciService` yöntemlerine koy; kullanıcının kendi rolünü/izinlerini değiştirmesini ve son Admin'in kaldırılmasını engelle; değişiklikleri denetim kaydına yaz.
- **Doğrulama adımı:** Yalnız `kullanici.oku` izni olan bir rolle giriş yap, Kullanıcı Yönetimi'nde bir kullanıcının rolünü değiştirip kaydet; işlem başarılıysa bulgu geçerlidir.

#### AUTH-02 — Rol/izin denetimi geniş ölçüde yok
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** 287 sayfanın yaklaşık 18'inde sayfa düzeyi izin denetimi (`YetkiKontrol`) var; kullanıcı yönetimi örneğinde bu denetim yalnız okuma iznine bakıyor (AUTH-01), diğer sayfalarda yazma izinlerinin uygulanıp uygulanmadığı tek tek taranmadı. Giriş dışında genel kural olarak yalnız `[Authorize]` (`Components/_Imports.razor:10`) ve modül lisansı (`Licensed:*`, `LicenseModuleAuthorization.cs:9-16`) devrede. Servis katmanında izin denetimi çağrısı olan servisler: Fatura (22 çağrı), Muhasebe (27), KolayMuhasebe (7), PersonelFinans (24), BankaKasaHareket (8). Denetim çağrısı **bulunmayan** servisler: Cari, Şoför/Personel, Bordro, Puantaj, Hakediş, FiloKomisyon, OperasyonPlan, Bütçe, Stok, Proforma, CariHatirlatma, Firma, Kullanıcı, Backup, Webhook, SMS, CRM, Luca. `CariList.razor:203-217` düğmeleri koşulsuz gösterir.
- **Senaryo:** "Yalnız görüntüleme" yetkili bir kullanıcı cari silebilir/düzenleyebilir, bordro onaylayabilir, yedek alabilir veya webhook tanımlayabilir.
- **Öneri:** Önce izin matrisini ürün olarak tanımla (Bölüm 10.3, soru 6); sonra mali, bordro ve güvenlik servislerine servis katmanı denetimi ekle (sayfa gizlemesi tek başına yeterli değildir). Fatura servisindeki desen örnek alınabilir.

#### AUTH-03 — "Kilidi kaldır" düğmesi çalışmıyor
**Önem:** Orta · **Güven:** K

- **Kanıt:** `KullaniciYonetimi.razor:669-682` sabit `"123456"` parolasını atar; parola politikası (`Program.cs:228-234`) en az 12 karakter, rakam, büyük/küçük harf ve sembol ister, bu yüzden atama reddedilir. Bildirim metni sabit şifre atanmış izlenimi verir.
- **Öneri:** Rastgele geçici parola üret ve ilk girişte değiştirmeye zorla; bildirim metnini düzelt.

### 6.2 Zamanlanmış işler

#### JOB-01 — İşler etkin firma bağlamı olmadan çalışıyor; kiracı filtresi tüm satırları gizliyor
**Önem:** Yüksek · **Güven:** D (filtre davranışı) + K (iş → servis zinciri). İşlerin canlıdaki çıktısı doğrulanmadı.

- **Kanıt:** Kiracı filtresi `FirmaTenantDisabled || FirmaId == FirmaTenantId` biçimindedir (`Data/ApplicationDbContext.cs:3318`). İş kapsamında `AktifFirmaId` boştur ve `TumFirmalar` kapalıdır; `AktifFirmaProvider.Set` oturumsuz çağrıyı reddeder (`S/IAktifFirmaProvider.cs:119-129`). Aynı koşulu taklit eden deney (Ek A.3) **0 satır** döndürdü. `Data/ScopedDbContextFactory.cs` yorumu da "bu olmadan tüm veriler filtrelenir" der. Kiracı filtresini bilinçli atlayan tek servis `S/HoldingService.cs:31,46`. Şu servislerde `IgnoreQueryFilters` ya da firma bağlamı kuran bir kod bulunmuyor: `BelgeUyariBackgroundService`, `CariHatirlatmaService`, `GunlukOzetService`, `PuantajAnomaliService`, `ZamanlıRaporService`, `BildirimService`; `LucaPortalService` bağlamı `FirmaService.GetAktifFirma()` üzerinden okuyor (aşağıda). Sorgu zinciri BelgeUyari, GunlukOzet, PuantajAnomali ve Luca için okundu; CariHatirlatma, ZamanlıRapor ve Bildirim için servis içi sorgular ayrıntılı okunmadı, sonuç filtre atlama/bağlam kurma kodunun bulunmamasından çıkarıldı.
- **Etkilenen işler:**
  - `BelgeUyariJob`: araç/personel/cari belge sorguları boş döner → "Uyarılacak belge bulunamadı".
  - `CariHatirlatmaJob`: firma kimlikleri `Firmalar` tablosundan (kiracı filtresiz) alınır, ancak hatırlatma sorguları kiracı filtreli bağlamda çalışır.
  - `GunlukOzetJob`: sefer sayımları sıfır çıkar.
  - `PuantajAnomaliJob`: `TumTaramaAsync(yil, ay)` firma verilmeden çağrılır.
  - `LucaPortalSenkronJob`: `FirmaService.GetAktifFirma()` varsayılan firmayı **döndürür** (`S/FirmaService.cs:158-197`) ama `Set` oturumsuz reddedildiği için bağlam yine boştur; ETTN mükerrer sorgusu (`S/LucaPortalService.cs:649`) kayıtları göremez, yani aynı belgeler her gece yeniden aktarılmaya çalışılabilir (mükerrer denetimi başka bir yerde yakalamıyorsa).
  - Bağlam yokken yeni kiracı kaydı eklemek ayrıca hata verir (`ApplicationDbContext.cs:3661-3678`, "firma kapsamı belirlenemedi").
- **Senaryo:** Belge uyarısı, cari hatırlatması ve puantaj anomali taraması etkinleştirilir; iş hata vermeden çalışır, hiçbir uyarı üretmez. Yönetici sistemin "sorun olmadığını" düşünür.
- **Öneri:** İşler için açık bir "sistem bağlamı" tasarla: firma listesini dön, her firma için kapsamlı (`FirmaId` atanmış) bir sağlayıcı ile yeni scope aç; ya da `HoldingService` gibi `IgnoreQueryFilters(["Tenant"])` kullanıp her sorguya açık `FirmaId` koşulu ekle. Her işe "kaç satır işlendi" günlüğü ekle; 0 sonuç şüpheli sayılsın.
- **Doğrulama adımı:** Süresi dolmak üzere belgesi olan bir araçla `BelgeUyari:EmailEnabled=true` yapıp işi tetikle; günlükte "Uyarılacak belge bulunamadı" görünüyorsa bulgu geçerlidir.

### 6.3 Entegrasyonlar

#### INT-01 — WhatsApp gönderimi simülasyon; "Gönderildi" yalan söylüyor
**Önem:** Kritik · **Güven:** K

- **Kanıt:** `S/CRMService.cs:449-482` `SendWhatsAppAsync`: `// TODO: WhatsApp API implementasyonu`; ayar varsa mesaj `Durum=Gonderildi` kaydedilir ve `true` döner. `S/WhatsAppService.cs:276-300` `SendMesajToKisiAsync` kaydı önce `Gonderildi` yazar, sonra bu saplamayı çağırır, dönüş değerini yok sayar; `302-332` grup gönderimi aynı saplamayı üyeler için çağırır. `S/GunlukOzetService.cs:33-60` günlük özet yalnız WhatsApp'a bağlı. `S/BakimPeriyotService.cs:331-332` e-posta/WhatsApp hiç çağrılmadan `WhatsAppGonderildi=true`, `EmailGonderildi=true` yazılır (yalnız uygulama içi bildirim üretilir). Arayüzde "WhatsApp ile belge uyarısı gönder" seçeneği (Ayarlar/BelgeUyariAyarlari) ve CRM WhatsApp sohbet sayfası vardır.
- **Senaryo:** Yönetici günlük özeti veya belge uyarısını WhatsApp'a bağlar; sistem "gönderildi" der, kimseye ulaşmaz. Müşteriye WhatsApp ile gönderildiği düşünülen hatırlatma ulaşmaz.
- **Öneri:** Ya gerçek sağlayıcı (Meta Cloud API veya Twilio) bağla ya da modülü "yakında" olarak gizle. `Durum` yalnız sağlayıcı yanıtından sonra `Gonderildi` olsun; `BakimPeriyot` log alanları gerçek sonuca bağlansın. Satış metninde WhatsApp bildirimi vaat edilmesin.
- **Doğrulama adımı:** CRM'den bir WhatsApp mesajı gönder; veritabanında `Gonderildi` görünür, alıcıya mesaj ulaşmaz.

#### INT-02 — E-posta için üç ayrı SMTP yapılandırması
**Önem:** Orta · **Güven:** K

- **Kanıt:** (1) `S/EmailService.cs:37-102` `appsettings` `Email:*`, `Enabled` varsayılan kapalı; CariHatirlatma, BelgeUyari, ZamanlıRapor, şifre sıfırlama ve Destek bunu kullanır. (2) `S/CRMService.cs:263-291` MailKit + DB tablosu `EmailAyarlari`; yalnız CRM e-posta sayfası. (3) `S/FaturaSablonService.cs:1094-1143` `System.Net.Mail` ile kendi kopyası. Toplu fatura e-postası "en az bir başarılı" ise `true` döner (`S/FaturaSablonService.cs:635-660`); kimin başarısız olduğu bildirilmez, gönderim kaydı yok.
- **Senaryo:** Yönetici CRM'de e-posta ayarını girer; şifre sıfırlama, vade hatırlatma, belge uyarısı ve destek e-postaları yine kapalı kalır. 50 faturadan 49'u gitmese bile ekran "gönderildi" der.
- **Öneri:** Tek `IEmailService` ve tek ayar kaynağı (DB tabanlı, şifreli); gönderim sonuçlarını alıcı bazında kaydet; toplu sonucu "X/Y başarılı" olarak göster.

#### INT-03 — Gizli bilgiler düz metin; SMS istemcileri zaman aşımsız
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** SMS, CRM e-posta, WhatsApp ve Webhook kimlik bilgileri/anahtarları DB alanlarında düz metindir (`SmsAyar.KullaniciAdi/ApiKey`, `EmailAyarlari`, webhook gizli anahtarı). Buna karşın Luca Portal parolası `IDataProtector` ile korunuyor (`S/LucaPortalService.cs:33,161,227`), yani koruma deseni projede var. `S/SmsService.cs`: `HttpClientFactory.CreateClient()` çağrıları zaman aşımı tanımsız (447, 486, 520, 580, 605, 633); Netgsm bakiye sorgusunda parola URL sorgu dizgesinde (`:488`) olduğundan proxy/günlüklerde açığa çıkabilir; Mutlucell XML'inde `pwd` (583, 606).
- **Senaryo:** Veritabanı yedeği veya salt okunur DB erişimi ele geçirilirse tüm SMS/e-posta/webhook sırları okunur. Sağlayıcı yanıt vermezse gönderim iş parçacığı uzun süre asılı kalır.
- **Öneri:** Luca'daki `IDataProtector` desenini tüm sağlayıcı sırlarına uygula (mevcut kayıtlar için tek seferlik dönüştürme); istemcilere zaman aşımı ve iptal belirteci ekle; Netgsm'de parolayı gövdeye taşı.

#### INT-04 — Webhook: yetki denetimi ve hedef URL doğrulaması yok
**Önem:** Yüksek · **Güven:** K (URL doğrulaması aranıp bulunamadı)

- **Kanıt:** `Webhooks.razor` yalnız `[Authorize]` (giriş) ister; `WebhookService` içinde izin denetimi yok. Sayfa yalnız URL'nin boş olmadığını denetler (`Webhooks.razor:501`; girdideki `type="url"` yalnız tarayıcı doğrulamasıdır); `WebhookService`'te şema, adres aralığı veya izinli alan adı denetimi bulunmuyor. Servis kullanıcının girdiği URL'ye veri POST eder. İmza (HMAC) ve yeniden deneme mevcut.
- **Senaryo:** Yetkisiz bir kullanıcı kendi sunucusuna olay verisi gönderen webhook tanımlar (veri sızıntısı) ya da iç ağ adreslerine istek attırır (SSRF).
- **Öneri:** Webhook yönetimine yönetici izni; yalnız `https` ve genel IP aralıkları (loopback, özel ağ, bağlantı-yerel ve metadata adresleri engelli); olay yükünde kişisel veri ve tutar alanlarını gözden geçir.

### 6.4 Fatura

#### FAT-01 — Düzenlemede kalem değişiklikleri kaydedilmiyor
**Önem:** Kritik · **Güven:** K

- **Kanıt:** `FaturaForm.razor` `Kaydet`: `IsEdit` ise `FaturaService.UpdateAsync(fatura)`. `S/FaturaService.cs:373-436` `UpdateAsync` mevcut kayıttaki `FaturaKalemleri`'ni Include eder ama gelen kalemleri uygulamaz; sonra `CalculateTotals(existing)` (`:2126-2135`) eski kalemlerden toplamı yeniden yazar. Formdan gelen toplamlar ezilir. Muhasebe fişi güncellenmez veya ters çevrilmez.
- **Senaryo:** Kullanıcı faturada miktar veya fiyatı değiştirip kaydeder; hata görmeden listeye döner, eski kalemler ve toplamlar kalır (sessiz veri kaybı).
- **Öneri:** Kalem ekle/güncelle/sil eşlemesi yap, toplamları buna göre hesapla; fişli faturada ters fiş + yeni fiş politikası uygula veya düzenlemeyi kilitle (Bölüm 10.3, soru 2). Düzenleme için entegrasyon testi ekle.
- **Doğrulama adımı:** Bir faturayı aç, bir kalemin miktarını değiştir, kaydet, yeniden aç.

#### FAT-02 — Elle girilen faturada toplamlar 0 yazılıyor
**Önem:** Kritik · **Güven:** K (zincir kodla izlendi; toplama formülü deneyle doğrulandı)

- **Kanıt:** `FaturaForm.razor` kalem satırı yalnız `Aciklama/Miktar/Birim/BirimFiyat/KdvOrani` bağlar; ekrandaki "Toplam" yalnız gösterimdir. `FaturaKalem.ToplamTutar` ve `KdvTutar` saklanan alanlardır (`E/FaturaKalem.cs:23-24`) ve formda hiç atanmaz. `Kaydet` form toplamlarını `fatura.AraToplam/KdvTutar/GenelToplam`'a yazar (`:419-421`) fakat `FaturaService.CreateAsync` (`:347-350`) hemen `CalculateTotals(fatura)` çağırır; bu `Σ ToplamTutar` ve `Σ KdvTutar` (ikisi de 0) ile ezer (`:2126-2135`). `PrepareFaturaForSaveAsync` (`:1792`) kalem tutarlarını doldurmaz. `ApplicationDbContext` içinde de kalem tutarı hesaplayan bir kanca yok.
- **Senaryo:** Manuel fatura 10 × 100 + %20 KDV ile girilir; listede ve cari bakiyesinde 0 ₺ görünür, fiş 0 tutarlı olur.
- **Öneri:** `CreateAsync`/`UpdateAsync` içinde kalem tutarlarını `Miktar × BirimFiyat`, `KdvOrani`, iskonto ve tevkifattan yeniden hesapla (tek kaynak); formdaki toplamlar yalnız gösterim kalsın.
- **Doğrulama adımı:** Yeni fatura oluştur, bir kalem gir, kaydet; listedeki Genel Toplam'a bak.

#### FAT-03 — Ödeme/tahsilat faturayı kapatmıyor
**Önem:** Kritik · **Güven:** K

- **Kanıt:** `GelenFaturalar.razor:1459-1493` (`OdemeKaydetAsync`) ve `GidenFaturalar.razor:1701-1732` (`TahsilatKaydetAsync`) `BankaKasaHareket` yazar, sonra `FaturaService.UpdateOdenenTutarAsync` (`S/FaturaService.cs:486-513`) çağırır. Bu yöntem `OdenenTutar`'ı `OdemeEslestirmeleri` toplamından yeniden hesaplar; eşleştirme kaydı oluşturulmadığı için sonuç 0'dır ve fatura "Beklemede" kalır. `IOdemeEslestirmeService` (transaction'lı, aşım kontrollü) `Program.cs:318`'de kayıtlı ama hiçbir yerde kullanılmaz. `FaturaDetay.razor:317` "Ödeme Eşleştir" düğmesi `/odeme-eslestirme` adresine gider; bu rotada sayfa yok (404).
- **Etki:** Vadesi geçmiş/kalan/yaşlandırma/tahsilat planı/nakit akış/vade riski (hepsi `GenelToplam − OdenenTutar`) ödenmiş faturaları açık gösterir. Cari bakiyesi banka hareketinden hesaplandığı için doğru çıkar; iki "gerçek" birbiriyle çelişir.
- **Öneri:** Ödeme ve tahsilat ekranlarını `IOdemeEslestirmeService` üzerinden tek transaction'da çalıştır; eksik sayfayı yaz veya düğmeyi kaldır; mevcut ödenmiş faturalar için tek seferlik düzeltme betiği.
- **Doğrulama adımı:** Gelen faturaya ödeme kaydet; fatura durumuna ve "kalan tutar"a bak.

#### FAT-04 — KDV çift sayılıyor
**Önem:** Yüksek · **Güven:** K (formül deneyle doğrulandı — Ek A.1)

- **Kanıt:** `CalculateTotals` `AraToplam = Σ ToplamTutar`, `Genel = Ara + Σ KdvTutar − İskonto` alır; yani `ToplamTutar` KDV **hariç** beklenir. KDV **dahil** `ToplamTutar` yazan yollar:
  - `S/ProformaFaturaService.cs:381, 473-545`: `kalem.ToplamTutar = Net + KDV`; 1.000 + %20 → 1.400 (proforma iskontosu ikinci kez düşülür).
  - `PuantajDetay.razor:566-625` (Giden/Gelen fatura taslağı): toplam KDV dahil kabul edilir, `kalem.ToplamTutar = toplam`; 12.000 → 14.000. KDV %20'ye sabit, sözleşme/güzergâh oranı kullanılmaz, tevkifat yok.
  - `S/PuantajFinansService.cs:324-343` `FaturaOlusturHizliAsync` (yalnız `RebuildService.cs:118` yolu): `ToplamTutar = tutar + kdv`; ayrıca `Durum = Odendi` (ödeme yok) → vade ve tahsilat listelerinden kaybolur.
  - `StokTuruEslestir.razor:1337`: `ToplamTutar = Tutar × (1 + KdvOrani/100)`.
  - Doğru yollar: XML içe aktarma `LineExtensionAmount` (`FaturaService.cs:1460`), API `Miktar × BirimFiyat` (`FaturalarController.cs:160`).
- **Senaryo:** Puantajdan fatura taslağı üretilir; fatura 12.000 yerine 14.000 çıkar, cari bakiyesi ve KDV beyanı şişer.
- **Öneri:** `ToplamTutar` anlamını tek tanıma bağla (KDV hariç) ve bunu `CalculateTotals` yerine kalem düzeyinde zorla; ilgili yolları düzelt; her yol için birim testi.

#### FAT-05 — Fatura iptal ve iade akışı yok
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** `FaturaDurum.IptalEdildi` hiçbir yerde **atanmaz**; yalnız filtre ve etiketlerde okunur (`CariService.cs:927`, `FaturaCariSenkronService.cs:47,115`, `HoldingService.cs:136,395`). `Fatura.IptalFisId` faturalar için kullanılmaz. Silme yalnız `IsDeleted=true` (FAT-06). XML'de iade tipi (`InvoiceTypeCode=IADE`) normal fatura olarak aktarılır (FAT-16).
- **Etki:** İptal/ters fiş üretilemez; iptal edilen fatura bakiyeden düşürülemez; iade faturası bakiyeyi azaltmak yerine artırır (CAR-04).
- **Öneri:** İptal durumu + ters fiş + bakiye hesabında iptal/iade işareti; iade türleri için fişte ters yön.

#### FAT-06 — Fatura silme bağlı kayıtları bırakıyor
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** `S/FaturaService.cs:455-465` `DeleteAsync` yalnız `IsDeleted` işaretler. Ödeme eşleştirmeleri, muhasebe fişi, stok hareketleri, masraf, karşı firma faturası, kiralık plaka bağlantısı, puantaj/hakediş "faturalandı" bayrakları kontrol edilmez veya temizlenmez.
- **Senaryo:** Hakedişten üretilmiş fatura silinir; hakediş "faturalandı" kalır, fiş hâlâ mizanda durur, stok hareketi geri alınmaz.
- **Öneri:** Silmeden önce bağımlılık denetimi (varsa engelle ya da iptal akışına yönlendir).

#### FAT-07 — `UpdateOdenenTutarAsync` iptal durumunu eziyor
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** `S/FaturaService.cs:486-513`: iptal edilmiş faturayı da Beklemede/KısmiÖdendi/Ödendi durumuna çevirir; fazla ödeme (ödenen > genel toplam) izlenmez.
- **Öneri:** İptal durumunu koru; fazla ödemeyi ayrı alan veya uyarı olarak göster.

#### FAT-08 — Gelen fatura benzersizlik anahtarı tedarikçiyi içermiyor
**Önem:** Yüksek · **Güven:** K (indeks adı hata iletisinden)

- **Kanıt:** `S/FaturaService.cs:2011-2029` `FindExistingFaturaAsync` ve `:442-450` (`IX_Faturalar_FirmaId_FaturaYonu_FaturaNo`): anahtar `(FirmaId, Yön, FaturaNo)`.
- **Senaryo:** İki farklı tedarikçi aynı numaralı ("0001") kâğıt veya e-arşiv fatura keserse ikincisi "zaten kayıtlı" diye reddedilir veya içe aktarmada atlanır; gider kaybolur.
- **Öneri:** Gelen faturada anahtarı `(FirmaId, Yön, CariId, FaturaNo)` yap (indeks + kontrol).

#### FAT-09 — ETTN kontrolü tüm firmalar ve silinmiş kayıtlar üzerinde
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** `S/FaturaService.cs:1403-1415` `AnyAsync(EttnNo)` + `IgnoreQueryFilters()`.
- **Senaryo:** (a) Silinen fatura düzeltilip yeniden aktarılmak istenirse "UUID zaten kayıtlı — atlandı" ile sonsuza dek engellenir. (b) Aynı XML'in A firmasında Giden, B firmasında Gelen olarak ayrı ayrı aktarımı (firmalar arası işlem) reddedilir.
- **Öneri:** Kontrolü firma + yön kapsamına ve silinmemiş kayıtlara sınırla.

#### FAT-10 — XML içe aktarma işlemsiz; fiş ve stok hataları sessizce yutuluyor
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** `S/FaturaService.cs:893-1560`: cari kaydı, muhasebe hesabı, `cari.MuhasebeHesapId`, fatura, XML dosyası, fiş, stok her biri ayrı `SaveChanges`; transaction yok. `:2092-2123` `TryCreateMuhasebeFisiAsync` transaction yoksa hatayı yalnız `Debug.WriteLine` ile yutar (Release'te görünmez) ve `ImportedCount` yine artar. `:3211-3215` stok hareketi hataları da yutulur (serviste toplam 6 `Debug.WriteLine`). `:1547-1559` `catch` yalnız Added girdileri ayırır; fatura `SaveChanges` sonrası hata olursa fatura kalır, kullanıcıya "parse hatası" gösterilir, yeniden denemede "zaten var" çıkar.
- **Senaryo:** İçe aktarılan 200 faturanın bir kısmı fişsiz veya stoksuz kalır; kullanıcı "200 başarılı" mesajını görür.
- **Öneri:** Fatura başına tek transaction (kaydet + hesap + fiş + stok); hata durumunda fatura geri alınsın veya "fişsiz" olarak işaretlenip raporlansın; `Debug.WriteLine` yerine günlük + kullanıcıya özet.

#### FAT-11 — UBL satır ayrıştırması: KDV oranı 0, ürün kodu satır numarası
**Önem:** Yüksek · **Güven:** D (Ek A.1)

- **Kanıt:** `S/FaturaService.cs:1458` `KdvOrani = GetDecimalValue(TaxCategory, "Percent")`; UBL'de `cbc:Percent`, `TaxSubtotal`'ın çocuğudur (`TaxCategory`'nin değil) → 0. `:1448` `urunKodu = GetValue(line, "ID")` satır sıra numarasını (`1`) verir; gerçek kod `Item/SellersItemIdentification/ID` (`SRV-001`). Deney çıktısı: `urunKodu='1'`, satır `KdvOrani=0`.
- **Etki:** KDV oranına göre hesap eşleştirme ve rapor %0'a düşer; `DetermineKalemTipi`'nin "ürün kodu 3 karakterden uzunsa Mal" kuralı hiç çalışmaz; stok eşleştirmesi satır numarasıyla yapılır.
- **Öneri:** `Percent` değerini `TaxTotal/TaxSubtotal` altından oku; ürün kodunu `SellersItemIdentification`'dan al.

#### FAT-12 — Kalem sınıflandırma: Türkçe `İ` ve 253/254
**Önem:** Yüksek · **Güven:** D (Ek A.1) + K (hesap kodları)

- **Kanıt:** `S/FaturaService.cs:2896-2928` `DetermineKalemTipi`: "araç/plaka/şase/otomobil/minibüs/otobüs" geçen her satır `Arac`; "servis/bakım/onarım/sigorta/kasko/muayene" `Servis`. `ToLowerInvariant` büyük `İ`'yi küçültmez: `"PERSONEL SERVİS HİZMETİ"` → `"personel servİs hİzmetİ"`, `Contains("servis") = False`; `"SİGORTA PRİMİ"` → `Contains("sigorta") = False`; `"ARAÇ KİRALAMA"` → `Contains("araç") = True`. `:1501-1515`: açıklamada plaka/şase geçen ve sistemde kayıtlı araç varsa satır `AracAlis/AracSatis` olur, `fatura.AracFaturasi=true`. `:3012-3038`: `Arac` → 253 (Tekdüzen planda 253 Tesis, Makine ve Cihazlar; taşıtlar 254), `Demirbas` → 255.
- **Senaryo:** Yakıt/bakım/kira/sigorta faturasının açıklamasında filonun plakası geçerse satır varlık hesabına ve "Araç Alış" tipine gider. Büyük harfle yazılmış e-fatura kalemleri anahtar sözcüklerle eşleşmez.
- **Öneri:** `ToLower(new CultureInfo("tr-TR"))` ya da karakter katlama; araç alış/satış kararı açıklama tahminiyle değil kalem tipi seçimiyle yapılsın; araç hesabı 254.

#### FAT-13 — Hesap kodu üretimi: metin sıralaması, 999 sonrası tekrar, yarış
**Önem:** Orta · **Güven:** K

- **Kanıt:** `S/FaturaService.cs:1973-1992` `GetSonrakiHesapKoduAsync` son kodu `OrderByDescending(HesapKodu)` ile **metin** olarak bulur; `120.999` metin olarak `120.1000`'den büyüktür, bu yüzden son kod hep `120.999` kalır ve `120.1000` tekrar üretilir. `GetUniqueCariCodeAsync` (`:2031-2044`) aynı mantık ve yarışa açık.
- **Öneri:** Sayısal sıralama ve veritabanı düzeyinde benzersiz indeks; üretimi tek yerde topla (MUH-01 ile birlikte).

#### FAT-14 — Tarih ayrıştırma hatasında fatura "bugün" olarak kaydediliyor
**Önem:** Orta · **Güven:** K

- **Kanıt:** `S/FaturaService.cs:950-954` (XML) ve `:735` (Excel): ayrıştırılamayan tarih yerine `UtcNow`/bugün atanır.
- **Öneri:** Hata ver veya satırı "kontrol gerekli" olarak işaretle.

#### FAT-15 — Cari eşleştirmede `ToLower` ünvan eşitliği
**Önem:** Orta · **Güven:** K

- **Kanıt:** `S/FaturaService.cs:1205-1208` `c.Unvan.ToLower() == cariUnvan.ToLower()`. Türkçe `İ/ı` ve PostgreSQL `lower()` farkı (bkz. `Genel arama Türkçe duyarsız` notu) aynı firmanın tekrar cari olarak açılmasına yol açar. VKN eşleşmesi önce denenmiyor.
- **Öneri:** Önce VKN/TCKN, sonra Türkçe duyarsız normalleştirme.

#### FAT-16 — Döviz, iskonto ve iade desteği eksik
**Önem:** Orta · **Güven:** D (Ek A.1) + K

- **Kanıt:** XML: `DocumentCurrencyCode` okunmuyor; `IskontoTutar=0`; iade tipi (`InvoiceTypeCode=IADE`) normal fatura olarak aktarılır. Tevkifatlı XML'de `GenelToplam = PayableAmount` (tevkifat düşülmüş), manuel faturada `GenelToplam = ara + kdv − iskonto` (brüt); aynı alanın anlamı kaynağa göre değişir (MUH-03).
- **Öneri:** Para birimi ve kur alanlarını oku; iade türlerini ayrı ele al; `GenelToplam` tanımı tek olsun.

#### FAT-17 — Excel içe aktarma: kalemsiz fatura, KDV oranı kaybı, hesap açılmaz
**Önem:** Orta · **Güven:** K

- **Kanıt:** `S/FaturaService.cs:649-888`: Excel yolu fatura başlığı oluşturur, kalem satırı oluşturmaz, KDV oranını taşımaz, cari için muhasebe hesabı açmaz; hata satırı "bugün" tarihle kalır (FAT-14).
- **Öneri:** Excel yolunu XML ile aynı çekirdeğe bağla veya kapsamını (yalnız özet fatura) arayüzde açıkça belirt.

#### FAT-18 — Para simgesi `₺` yerine `?` görünüyor
**Önem:** Düşük · **Güven:** D (kaynakta literal `?`)

- **Kanıt:** `FaturaForm.razor` kalem satırı toplam hücresi `...ToString("N2")) ?`; `FaturaDetay.razor:274` aynı. Muhtemel kodlama kaybı.
- **Öneri:** `₺` karakterini UTF-8 olarak geri yaz.

#### FAT-19 — Fatura numarası biçimleri tutarsız
**Önem:** Orta · **Güven:** K

- **Kanıt:** `ProformaFaturaService` → `GenerateNextFaturaNoAsync(SatisFaturasi)` yön/firma verilmeden global `SF…` sayaç kullanır; `FaturaForm` `SF_GIDEN…` firma sayacı üretir; hakediş `HKD-{Tip}-{YYYYMM}-{Id}` yer tutucu numarası verir (HKD-01). Not: biçimli numarada yalnız yıl varsa sayaç yıllık olmalı (bkz. `Numara sayacı yıllık kural` notu).
- **Öneri:** Tek numaralandırma servisi (firma + yön + yıl).

### 6.5 Cari

#### CAR-01 — Sözleşme alanları düzenlemede kaydedilmiyor
**Önem:** Orta · **Güven:** K

- **Kanıt:** `S/CariService.cs:401-416` `UpdateAsync` `SozlesmeNo`, `SozlesmeBaslangicTarihi`, `SozlesmeBitisTarihi` alanlarını kaydetmez; `CariForm.razor:172-184` bu alanlara bağlıdır. Oluşturma yolunda kalır, düzenlemede kaybolur.
- **Öneri:** Alanları `UpdateAsync`'e ekle; form ile servisin alan listesini karşılaştıran test yaz.

#### CAR-02 — Cari silme kontrolsüz
**Önem:** Orta · **Güven:** K

- **Kanıt:** `S/CariService.cs:497-515`: açık fatura, bakiye veya muhasebe hesabı bağlantısı denetlenmeden soft-delete.
- **Etki:** Silinen cariye bağlı faturalar listelerde "cari boş" görünür.
- **Öneri:** Bağımlılık varsa engelle veya pasifleştirmeye yönlendir.

#### CAR-03 — Borç/Alacak sütunları iki ekranda ters; net bakiye farklı
**Önem:** Orta · **Güven:** K

- **Kanıt:** `CariBakiyeHesaplayici` (Cari Listesi): Alacak = giden + ödeme, Borç = gelen + tahsilat. `S/CariHareketTakipService.cs:68-79, 215-219, 304-305` (Borç/Alacak Takip ve ekstre): Borç = giden, Alacak = gelen + tahsilat − ödeme + AracMasrafı. Net işaret aynı, sütunlar tersine. Takip ekranı `AracMasraf.CariId` tutarlarını ekler, Cari Listesi eklemez → farklı net bakiye.
- **Öneri:** Tek bakiye hesaplayıcı; sütun anlamını (yön ve etiket) tek tanıma bağla.

#### CAR-04 — Bakiye hesabı iptal, iade, tevkifat ve dövizi bilmiyor
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** `S/CariService.cs:140-151` `CariId` bazında `GenelToplam` toplanır. İade türleri (SatisIade/AlisIade) pozitif eklenir; `IptalEdildi` hiç atanmadığı için sorun görünmez (FAT-05). `S/MuhasebeService.cs:639-879` `CreateFaturaFisiCoreAsync` iade tipi için ters yön üretmez, yalnız `FaturaYonu`'na bakar.
- **Senaryo:** Bir satış iade faturası kesilince müşterinin borcu azalmaz, artar.
- **Öneri:** Bakiye ve fiş hesabında fatura türüne göre işaret; dövizli faturalar için TL karşılığı alanı.

### 6.6 Muhasebe

#### MUH-01 — Cari ↔ muhasebe hesabı için en az altı şema
**Önem:** Yüksek · **Güven:** K

- **Kanıt ve tablo:** Bölüm 4.1. Aynı cari için hareketler 120/320 altında 3-4 farklı hesap kartına dağılır.
- **Senaryo:** Mizanda bir müşterinin bakiyesi üç satıra bölünür; cari ekstresi ile muavin defteri uyuşmaz.
- **Öneri:** Tek `CariHesapResolver`: önce `Cari.MuhasebeHesapId`, yoksa tek şemayla aç ve cariye yaz; tüm servisler bunu kullansın; mevcut dağınık hesaplar için birleştirme aracı (kişi bazlı "onar" aracının eşdeğeri).

#### MUH-02 — Bulanık ünvan eşleşmesi hesabı kalıcı olarak yanlış bağlayabilir
**Önem:** Orta · **Güven:** K

- **Kanıt:** `S/CariService.cs:798-828` `FillMuhasebeBilgisiAsync`: `HesapAdi == unvan`, bulunamazsa `HesapAdi.Contains(unvan) || unvan.Contains(HesapAdi)`. `GetByIdAsync`/`GetByKodAsync` bunu çağırır; form nesneyi `UpdateAsync`'e geri gönderince tahmin kalıcı olur (`:413`).
- **Senaryo:** "Kasa Ticaret" ünvanlı cari "Kasa" hesabına bağlanabilir.
- **Öneri:** Tahmini yalnız öneri olarak göster, kaydetme; eşleştirme yalnız açık kullanıcı seçimiyle.

#### MUH-03 — Tevkifatlı faturada fiş eksik tutarlı
**Önem:** Yüksek · **Güven:** D (Ek A.1)

- **Kanıt:** XML içe aktarmada `GenelToplam = PayableAmount` (tevkifat düşülmüş), `KdvTutar = Payable − TaxExclusive` (= KDV − tevkifat) (`S/FaturaService.cs:1362-1368`). `S/MuhasebeService.cs:677-679, 757-759, 807-809` ise `GenelToplam`'ı brüt varsayıp `TevkifatTutar`'ı tekrar düşer.
- **Deney (1.000 TL, %20 KDV, 5/10 tevkifat):** içe aktarma sonucu Ara 1000, Genel 1100, KdvTutar 100, Tevkifat 100. Oluşan giden fiş: **120 Borç 1000** (doğrusu 1100), 136 Borç 100, 600 Alacak 1000, **391 Alacak 100** (doğrusu 200). Fiş **dengeli** olduğu için hata fark edilmez; 120 alacak hesabı ve hesaplanan KDV tevkifat kadar eksik kalır.
- **Öneri:** `GenelToplam` tanımını tek yap (brüt); tevkifatı ayrı alanla taşı; fiş üretiminde 120 = brüt − tevkifat, 136 = tevkifat, 391 = tam KDV.

#### MUH-04 — Fatura fişi kalem hesaplarını kullanmıyor
**Önem:** Orta · **Güven:** K

- **Kanıt:** Fiş tek gelir (`SatisGelirHesabi`) ve tek gider (`AlisGiderHesabi`) hesabı kullanır; `kalem.MuhasebeHesapId` (`S/FaturaService.cs:1517-1526, 3012-3038`) yalnız metadata'dır. Varsayılan `AlisGiderHesabi = "153.01"` (Ticari Mallar, `E/Muhasebe.cs:220`); varsayılan planda 153.01 yok ve 770.01 Kira Giderleri'dir.
- **Etki:** "Kalem bazlı hesap eşleştirme" ekranları (`StokTuruEslestir` vb.) fişe yansımaz.
- **Öneri:** Fişte kalem hesaplarını kullan veya bu ekranları kaldır; varsayılan hesapları plana uydur.

#### MUH-05 — Masraf kategorisi → 770.xx üç çelişkili tabloda
**Önem:** Yüksek · **Güven:** K

- **Kanıt ve tablo:** Bölüm 4.3. Aynı yakıt masrafı bir yolda 770.06'ya, Kolay Muhasebe'de 770.01 (Kira) hesabına yazılır.
- **Senaryo:** Kira gideri hesabı yakıtla şişer; gider analizi ve bütçe-gerçekleşen karşılaştırması bozulur.
- **Öneri:** Tek masraf-hesap eşleme tablosu (ayarlardan düzenlenebilir), tüm servisler buradan okusun; planda olmayan kodları (770.10-.12) ya ekle ya kullanma.

#### MUH-06 — Banka/kasa hesabı çözümü tutarsız
**Önem:** Orta · **Güven:** K

- **Kanıt:** Bölüm 4.4. Aynı banka hareketi servise göre 102, 102.01 veya 103.01 hesabına gider.
- **Öneri:** `BankaHesap.MuhasebeHesapId` tek kaynak olsun.

#### MUH-07 — Tahsilat/tediye için muhasebe fişi üretilmiyor
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** `S/MuhasebeService.cs:884-986` `CreateTahsilatFisiAsync` / `CreateTediyeFisiAsync` hiçbir yerden çağrılmaz. Muhasebeleştirme sayfası yalnız fatura ve masraf listeler; banka hareketleri yalnız Kolay Muhasebe sihirbazıyla tek tek fişlenir (`S/KolayMuhasebeService.cs:957, 999, 1041, 1126`).
- **Etki:** 120/320 hesapları alacaklanmaz/borçlanmaz → mizanda alıcı ve satıcı bakiyeleri şişer, kasa/banka muhasebede güncellenmez.
- **Öneri:** Ödeme/tahsilat kaydında (FAT-03 düzeltmesiyle birlikte) otomatik veya toplu fiş; Muhasebeleştirme sayfasına banka hareketi sekmesi.

#### MUH-08 — Fiş numaralaması firma ve ay bağlamında tutarsız
**Önem:** Orta · **Güven:** K

- **Kanıt:** `CreateFisAtomic`/`FisKaydetKilitli` → `GenerateNextFisNoInContextAsync(context, tip)` `firmaId=0` ile sayaç üretir (`S/MuhasebeService.cs:561-578`); `HakedisMuhasebeService` `GenerateNextFisNoAsync(FisTipi.Mahsup, FirmaId)` ile firma bazlı üretir. Sayaç ayı fiş tarihinden değil "bugün"den alır (`:512`).
- **Öneri:** Tek numaralandırma; ay/yıl fiş tarihinden.

#### MUH-09 — Muhasebe dönem kilidi uygulanmıyor
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** `S/MuhasebeService.cs:1067-1076` `DonemKapatAsync` yetki denetimi olmadan yalnız `Durum=Kapali` yazar. `CreateFisAsync`, `UpdateFisAsync`, `OnayGeriAlFisAsync`, `DeleteFisAsync` ve fatura/masraf fişi yolları fiş tarihinin kapalı dönemde olup olmadığına bakmaz (`DonemDurum.Kapali` kod tabanında yalnız `DonemKapatAsync` içinde atanır, `:1073`; `GetAktifDonemAsync` `:1061` yalnız açık dönemi seçer).
- **Senaryo:** Beyannamesi verilmiş ayın fişleri sonradan eklenir, düzenlenir veya onayı geri alınır.
- **Öneri:** Tüm fiş yazma yollarında merkezi dönem kontrolü; `DonemKapatAsync` için yetki.

#### MUH-10 — KDV beyan raporu karışık kaynaktan besleniyor
**Önem:** Yüksek · **Güven:** K (alt hesap eşleşmesi O)

- **Kanıt:** `S/MuhasebeService.cs:1466-1545` `GetKdvBeyanRaporuAsync`: hesaplanan ve indirilecek KDV fatura başlıklarından, devreden KDV defter hesaplarından alınır. `KdvOrani` başlık varsayılanı 20 (satır oranı kullanılmaz); tevkifat yalnız düşülür. `GetKdvBakiyeAsync` kümülatif bakiye alır ve "191/391/190" üst hesap kodlarıyla tam eşleşme arar (alt hesaplı planlarda eksik kalabilir — doğrulanmadı).
- **Etki:** Fatura toplamları yanlışsa (FAT-02, FAT-04, MUH-03) beyan rakamı da yanlış çıkar; defter ile fatura tutmaz.
- **Öneri:** Beyanı tek kaynaktan (onaylı fiş satırları) üret; oran bazlı döküm.

#### MUH-11 — Mizan açılış (devreden) bakiyesi taşımıyor
**Önem:** Orta · **Güven:** K

- **Kanıt:** `S/MuhasebeService.cs:1346-1401`: dönem mizanı yalnız seçilen aralıktaki hareketleri toplar, önceki dönemlerden devreden bakiyeyi içermez.
- **Öneri:** Açılış bakiyesi sütunu; yıl sonu kapanış/açılış fişi akışı.

### 6.7 Hakediş, Puantaj ve Operasyon

#### HKD-01 — Hakediş→Fatura `FaturaService.CreateAsync`'i atlıyor
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** `S/OperasyonelHakedisService.cs:252-328` `FaturayaDonustureAsync` faturayı doğrudan `context.Faturalar.Add` ile ekler: yetki denetimi, numara benzersizliği ve otomatik fiş yok; `FaturaNo = "HKD-{Tip}-{YYYYMM}-{Id}"` yer tutucu; kalem satırı yok (e-fatura XML/PDF satırsız); tevkifat taşınmaz; iki ayrı `SaveChanges` (322-325) transaction'sız. Tedarikçi/AracSahibi hakedişi için **Gelen** yönde sahte e-arşiv fatura yaratılır; gerçek tedarikçi faturası XML'i aktarıldığında ikinci kayıt açılır (eşleştirme yok).
- **Öneri:** `FaturaService.CreateAsync(context, …)` aşırı yüklemesini (çağıranın transaction'ında) kullan; kalem satırları ve tevkifat taşınsın; hakediş ↔ gerçek fatura eşleştirme adımı.

#### HKD-02 — Hakediş muhasebesi: prefix tahmini, çift kayıt riski, tevkifat yok
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** `S/HakedisMuhasebeService.cs:80-81, 121, 190` ve `FindHesap` (268-276): fişteki cari satırı cari kartının hesabına değil prefix'in ilk `StartsWith` eşleşmesine yazılır. Hakediş fişi `KaynakTip=Hakedis`; hakediş sonradan faturalandığında ya da gerçek fatura aktarıldığında aynı gelir/KDV ikinci kez kaydedilebilir. Tevkifat işlenmez; `FisDurum` doğrudan `Onaylandi`; borç/alacak dengesi doğrulanmaz (`GenelToplam` ile `Tutar+KdvTutar`).
- **Öneri:** MUH-01'deki `CariHesapResolver`; hakediş fişi ile fatura fişi arasında tek-kayıt kuralı; denge doğrulaması.

#### HKD-03 — Hakedişte KDV oranı hep %20
**Önem:** Orta · **Güven:** K

- **Kanıt:** `E/Hakedis.cs:44`, `S/OperasyonelHakedisService.cs:207`, `Kapsam.cs:317`: `KdvOran` sabit 20; güzergâh/sözleşme `KdvOrani` taşınmıyor. Aynı sabit `PuantajDetay.razor` fatura taslağında da var (FAT-04).
- **Öneri:** Oranı güzergâh/sözleşme/cari kaynağından al.

#### PUA-01 — Puantaj dünyaları birbirinden kopuk
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** Dört ayrı puantaj modeli var: (1) `PuantajKayit` (Puantaj Excel Grid, `/personel/puantaj-grid`) — `Kaydet` yalnız `PuantajKayit` günlerini yazar; `IPuantajHakedisSyncService` enjekte edilmiş ama **hiç çağrılmıyor** (`PuantajExcelGrid.razor.cs:551-598`); `PuantajSyncService` (PuantajKayit→OperasyonKaydi) hiçbir yerden çağrılmıyor. (2) `FiloGunlukPuantaj` (Operasyon Merkezi/Plan/Puantaj/Matris/Hakediş) — gerçek akış. (3) `ServisKontrat` puantajı. (4) `PersonelPuantaj` (personel). Ölü sync kodlarında ek sorunlar: `PuantajSyncService.cs:59-62` gün döngüsü 1..31 ve `new DateTime(Yil, Ay, gun)` koşulsuz → Şubat/30 günlük aylarda `ArgumentOutOfRangeException` (şu an çağrılmadığı için gizli bomba); `PuantajHakedisSyncService` (196 satır) `BirimGelir` yoksa `BirimGider`'e düşer (Kurum=GELİR hakedişi maliyet fiyatıyla), mesai/çarpanı kullanmaz, KDV oranı son kaydın değeri.
- **Senaryo:** Kullanıcı Excel Grid'e puantaj girer, "kaydedildi" görür; hakediş, fatura ve maaş bu veriyi görmez.
- **Öneri:** Ürün kararı (Bölüm 10.3, soru 1): Excel Grid'i resmi akışa bağla (tek yönlü sync, onaylı) ya da ekranı "bilgi amaçlı" olarak etiketle/kaldır.

#### PUA-02 — Dönem kilidi yalnız `OperasyonKaydi` için
**Önem:** Orta · **Güven:** K

- **Kanıt:** `OperasyonKaydiBusinessRules.cs:52-57`. `FiloGunlukPuantaj`, hakediş, `PuantajKayit` ve `PersonelPuantaj` yazımlarında dönem kilidi/onay denetimi yok.
- **Öneri:** Ortak dönem kilidi servisi (MUH-09 ile birlikte).

#### PUA-03 — Fiyat önceliği iki yerde ters
**Önem:** Orta · **Güven:** K

- **Kanıt:** Bölüm 4.5. Puantaj `güzergâh → eşleştirme`, hakediş `eşleştirme → puantaj → güzergâh` sırasını izler.
- **Senaryo:** Aynı sefer için puantaj ekranındaki tutar ile hakediş tutarı farklı çıkar.
- **Öneri:** Tek `FiyatCozucu`.

#### PUA-04 — Excel Grid "Engine ile hesapla" Auto hücreleri sıfırlıyor
**Önem:** Orta · **Güven:** K

- **Kanıt:** `PuantajExcelGrid.razor.cs:600-624` `EngineHesapla`: düğme ipucu "Auto hücreleri engine ile yeniden hesapla" der ama kod Auto hücrelerinin `Deger/Mesai/EkSefer` değerlerini 0'lar; `Kaydet` ile kalıcı olur.
- **Öneri:** Gerçek hesaplamayı yap ya da düğmeyi kaldır.

#### OPR-01 — Onay ve faturalama bayrakları fiilen atanmıyor; kilit kontrolleri etkisiz
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** `FiloGunlukPuantaj.Onaylandi` ve `KurumFaturaId` kod tabanında hiçbir yerde atanmaz; `KurumFaturaKesildiMi` yalnız `KurumFaturalastirAsync` içinde atanır (`S/FiloKomisyonService.cs:542`), ancak bu yöntem ve `TaseronOdeAsync` hiçbir yerden çağrılmaz. Bayraklara dayanan kilit kontrolleri (`S/FiloKomisyonService.cs:476, 659`; `S/OperasyonPlanService.cs:268, 370`) bu yüzden hiçbir zaman tetiklenmez. `S/FiloKomisyonService.cs:143-165` `DeleteEslestirmeAsync` bağlı tüm puantajları onay/fatura durumuna bakmadan siler. `IPuantajWorkflowService` (Finans → Muhasebe → Kilit onay akışı) hiçbir sayfada kullanılmıyor.
- **Senaryo:** Faturalanmış bir dönemin eşleştirmesi silinince puantaj kayıtları da silinir; fatura ile destek verisi kopar.
- **Öneri:** Bayrakları faturalama/onay akışında ata ve silme/düzenleme kontrollerini bunlara bağla.

#### OPR-02 — Toplu puantaj üretimi verimsiz ve konsola yazıyor
**Önem:** Orta · **Güven:** K

- **Kanıt:** `TopluPuantajUretAsync` O(n³) döngü; `S/FiloKomisyonService.cs` içinde 25 `Console.WriteLine` (sunucu çıktısını kirletir, hassas veri yazabilir).
- **Öneri:** Önceden yükleme ve sözlük araması; `ILogger` kullan.

### 6.8 Personel ve Bordro

#### PER-01 — Personel için de çok şemalı 335 hesabı
**Önem:** Yüksek · **Güven:** K

- **Kanıt ve tablo:** Bölüm 4.2.
- **Senaryo:** Aynı personelin maaş borcu 335'in farklı alt hesaplarına yazılır; bordro hesabı bulamayıp hata verir.
- **Öneri:** Tek `PersonelHesapResolver`; "onar" aracını müşteri/tedarikçi/personel için genelleştir.

#### PER-02 — Maaş snapshot fişi 770.01'e yazıyor ve yalnız net tutarı muhasebeleştiriyor
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** `S/MuhasebeSnapshotService.cs:91-137`: gider 770.01 sabit (varsayılan planda Kira Giderleri; ayardaki `PersonelGiderHesabi` dikkate alınmıyor), yoksa "Genel Yönetim Giderleri" adıyla hesap açar; yalnız net `Odenecek` fişlenir. Bordro tahakkuk fişi (`BordroService.CreateBordroMuhasebeFisiAsync`: 770 brüt + işveren SGK, 335 net, 360, 361, 368) ile Maaş snapshot fişi (770/335 Odenecek) ayrı kaynaklardır; aralarında çift kayıt koruması yok (kontroller `f.BordroId == bordro.Id` ve `KaynakTip == "MaasSnapshot"` ayrı ayrı).
- **Senaryo:** Aynı personel ve ay için iki fiş oluşur; gider iki kez kaydedilir.
- **Öneri:** Maaş muhasebeleştirmesi için tek kaynak (bordro); snapshot yalnız ödeme planı olsun veya diğerini engellesin; gider hesabı ayardan.

#### PER-03 — Bordro onay iptali muhasebe fişini ters çevirmiyor
**Önem:** Orta · **Güven:** K

- **Kanıt:** `S/BordroService.cs:674-700` `OnayIptalEtAsync` yalnız `Onaylandi=false` yapar. `CreateBordroMuhasebeFisiAsync` mükerrer kontrolü (`:455-458`) `f.BordroId == bordro.Id` varsa dönüş yapar → bordro düzenlenip yeniden onaylansa da muhasebe **eski rakamlarda** kalır. Fiş toplamları kalemlerden yeniden hesaplanır (`:669-670`), denge doğrulaması yok; ek ödeme (yemek, yol, ikramiye) ve özel kesintiler (icra, BES) muhasebeleşmiyor.
- **Öneri:** İptalde ters fiş, yeniden onayda yeni fiş; denge doğrulaması; ek ödeme/kesinti kalemlerini fişe taşı.

#### PER-04 — Banka ödeme listesi eski mantıkta
**Önem:** Orta · **Güven:** O (önceki oturum kaydından)

- **Kanıt:** `BankaOdemeListesi` tüm-zaman ve `Kesinti=0` mantığıyla çalışıyor; Maaş/Ödeme Yönetimi aylık (bkz. `Maaş/Ödeme aylık liste tasarımı` notu). Bu denetimde yeniden doğrulanmadı.
- **Öneri:** Aynı aylık liste servisini kullan.

### 6.9 Banka içe aktarma

#### BNK-01 — Aynı ekstre iki kez yüklenirse hareketler çift kaydedilir
**Önem:** Yüksek · **Güven:** K

- **Kanıt:** `S/BankaHareketImportService.cs:204-206` ve `CreateImportedBatchAsync`: her satıra `IslemKimligi = Guid.NewGuid()` verilir, içerik karması yok. SHA256 ile mükerrer korumalı `BankaImportService` kayıtlı ama kullanılmıyor (Bölüm 8).
- **Senaryo:** Kullanıcı aynı ekstreyi iki kez yükler; bakiye ve nakit akışı iki katına çıkar.
- **Öneri:** Satır içerik karması (tarih + tutar + açıklama + bakiye) veya dosya karması ile mükerrer koruması; `BankaImportService`'teki yöntemi devral.

#### BNK-02 — Otomatik cari eşleştirme açıklamada ünvan arıyor
**Önem:** Orta · **Güven:** K

- **Kanıt:** `S/BankaHareketImportService.cs:146-164` `OtoCariEslestirAsync`: açıklamada unvan `Contains` + `ToUpperInvariant`, ilk eşleşen; IBAN/VKN kullanılmaz.
- **Senaryo:** Kısa veya ortak sözcüklü ünvan yanlış cariye bağlanır.
- **Öneri:** IBAN → VKN → tam ünvan sırası; belirsizse eşleştirme yapma, kullanıcıya sor.

#### BNK-03 — PDF ekstre ayrıştırması kırılgan
**Önem:** Orta · **Güven:** K

- **Kanıt:** `S/BankaHareketImportService.cs:380-442`: tutar "satırdaki sondan ikinci tutar", yön işaretten çıkarılır.
- **Öneri:** Banka başına şablon veya içe aktarma sonrası önizleme + toplam kontrolü (açılış + hareketler = kapanış).

### 6.10 Diğer

#### DGR-01 — Hata ayıklama çıktıları üretim kodunda
**Önem:** Düşük · **Güven:** D

- **Kanıt:** 26 `Console.WriteLine` (`FiloKomisyonService` 25, `SoforService` 1); `FaturaService`'te 6 `Debug.WriteLine` (bazıları hata yutma noktasında, FAT-10).
- **Öneri:** `ILogger`'a taşı.

#### DGR-02 — Saplama ve sahte metrikler
**Önem:** Orta · **Güven:** K

- **Kanıt:** `S/CariRiskService.cs:298` `OrtalamaTahsilatSuresi = 0` sabit; `S/MaliAnalizService.cs:443, 511` olmayan alanlara başvuru; `Satis/PiyasaArastirma.razor` "Gerçekçi Simülasyon" üretiyor.
- **Etki:** Ekranda gerçek veriymiş gibi görünen simüle rakamlar.
- **Öneri:** Gerçek hesaplama yoksa metriği gizle; simülasyon sayfalarını "örnek veri" olarak etiketle.

#### DGR-03 — PDF çıktısında bozuk karakterler
**Önem:** Düşük · **Güven:** O (önceki oturum kaydı; yeniden doğrulanmadı)

- **Kanıt:** `PdfService` bazı çıktılarında bozuk Türkçe karakterler (hafıza kaydı).
- **Öneri:** Yazı tipini ve kodlamayı denetle.

---

## 7. Akışta eksik kalan işlevler

| # | Eksik | Etki | İlgili |
|---|-------|------|--------|
| 1 | Fatura iptal/iade/ters fiş akışı | Bakiye ve fiş düzeltilemez | FAT-05, CAR-04 |
| 2 | Ödeme ↔ fatura eşleştirme arayüzü (`/odeme-eslestirme` yok) | Faturalar kapanmaz | FAT-03 |
| 3 | Tahsilat/tediye otomatik fişi | Mizan yanlış | MUH-07 |
| 4 | Muhasebe dönem kilidi | Kapalı dönem değişir | MUH-09 |
| 5 | Puantaj onay/kilit/faturalama akışı | Faturalı dönem değişebilir | OPR-01, PUA-02 |
| 6 | Excel Grid → hakediş/maaş bağlantısı | Girilen veri hiçbir yere gitmez | PUA-01 |
| 7 | Banka ekstre mükerrer koruması | Çift hareket | BNK-01 |
| 8 | Döviz desteği | Dövizli faturalar yanlış | FAT-16 |
| 9 | Mizan açılış bakiyesi | Dönem mizanı eksik | MUH-11 |
| 10 | Hakediş ↔ gerçek tedarikçi faturası eşleştirme | İkili kayıt | HKD-01 |
| 11 | Cari silmede bağımlılık denetimi | Boş cari görünür | CAR-02 |
| 12 | Gerçek WhatsApp kanalı | Bildirim ulaşmaz | INT-01 |
| 13 | Arka plan işleri için sistem bağlamı | İşler boş çalışır | JOB-01 |
| 14 | Bordro iptalinde ters fiş | Muhasebe eski kalır | PER-03 |

---

## 8. Ölü kod ve bağlantısız servisler

Kayıtlı veya yazılmış ama çağrılmayan bileşenler (çalıştırılırsa davranışları ayrıca doğrulanmalıdır):

| Bileşen | Not |
|---------|-----|
| `PuantajSyncService` | PuantajKayit → OperasyonKaydi; gün döngüsü hatası (PUA-01) |
| `PuantajHakedisSyncService` | Fiyat ve KDV mantığı hatalı (PUA-01) |
| `IPuantajWorkflowService` | Finans → Muhasebe → Kilit onay akışı hiçbir sayfada yok |
| `IOdemeEslestirmeService` | Transaction'lı, aşım kontrollü; kullanılmıyor (FAT-03) |
| `MuhasebeService.CreateTahsilatFisiAsync` / `CreateTediyeFisiAsync` | MUH-07 |
| `PuantajFinansService.TopluFaturaUretAsync` / `GelirFaturasiUretAsync` / `GiderFaturasiUretAsync` | Yalnız Rebuild yolu `IsleAsync` kullanılır |
| `BankaImportService` | SHA256 mükerrer korumalı; kullanılmıyor (BNK-01) |
| `KurumFaturalastirAsync`, `TaseronOdeAsync` | OPR-01 |

---

## 9. Sağlam bulunanlar

- **Kiracı izolasyonu çalışıyor.** Deneyle: `FindAsync` silinmiş ve başka firma kayıtlarında `null` döndü (Ek A.2). Yeni kiracı kaydı için firma bağlamı yoksa kayıt reddediliyor (`ApplicationDbContext.cs:3661-3678`); bu güvenli bir varsayılandır.
- **Parola politikası** güçlü: en az 12 karakter, rakam, büyük/küçük harf, sembol (`Program.cs:228-234`).
- **Luca parolası şifreli saklanıyor** (`IDataProtector`); desen diğer sırlara da uygulanabilir.
- **Webhook** imza (HMAC) ve yeniden deneme içeriyor.
- **Fatura, Muhasebe, KolayMuhasebe, PersonelFinans ve BankaKasaHareket servisleri** izin denetimi içeriyor.
- **İnce taneli izin altyapısı hazır:** oku/yaz/düzenle/sil izin tanımları, Rol Yönetimi'nde atama ekranı ve sayfa düzeyi `YetkiKontrol` bileşeni var; eksik olan, izinlerin eylem ve servis düzeyinde uygulanması.
- **`IOdemeEslestirmeService`** tasarımı (transaction, aşım kontrolü) doğru; yalnız bağlanmamış.
- **`HoldingService`** arka plan için kiracı filtresini bilinçli ve adlandırılmış biçimde atlıyor (diğer işler için örnek).
- Tüm Quartz işleri eşzamanlı çalışmaya karşı korumalı.

---

## 10. Öncelikli aksiyon planı

Efor tahmini kaba bir rehberdir: **S** < 1 gün, **M** 1-3 gün, **L** > 3 gün.

### 10.1 Dalgalar

| Dalga | Amaç | Bulgular | Efor |
|-------|------|----------|------|
| 0 — Hemen | Güvenlik ve veri kaybı | AUTH-01 (S), FAT-01 (M), FAT-02 (S), FAT-03 (M), INT-01 (S: gizle / L: gerçek sağlayıcı), FAT-04 (M), INT-04 (S), JOB-01 (M) | M-L |
| 1 — Mali doğruluk | Rakamların doğru çıkması | MUH-03 (M), FAT-11 (S), FAT-12 (S), FAT-10 (M), FAT-07 (S), FAT-05+FAT-06 (L), CAR-04 (M), MUH-07 (M), MUH-09 (M), MUH-10 (M), HKD-01+HKD-02 (L), PER-02+PER-03 (L), BNK-01 (M), OPR-01 (M) | L |
| 2 — Birleştirme | Aynı kavram, tek kural | MUH-01 (L), PER-01 (M), MUH-05 (M), MUH-06 (S), PUA-03 (M), CAR-03 (M), INT-02 (M), INT-03 (M), AUTH-02 (L) | L |
| 3 — Olgunlaştırma | Kalan bulgular ve temizlik | PUA-01 kararı, OPR-02, FAT-08/09/13..19, CAR-01/02, HKD-03, BNK-02/03, MUH-02/04/08/11, DGR-* | L |

Küçük, düşük riskli ilk adımlar (hemen yapılabilir): AUTH-01 (eylemleri yazma izinlerine bağlama), FAT-02 (kalem tutarı hesabı), FAT-11 (iki ifade), FAT-12 (kültür duyarlı küçültme), FAT-18 (karakter), AUTH-03 (rastgele geçici parola), INT-04 (izin + URL kuralı).

### 10.2 Önerilen test güvencesi

Her dalganın içinde, düzeltmeden önce başarısız olan testler yazılmalıdır (deney betikleri başlangıç noktasıdır, Ek A):

- Fatura: oluştur/düzenle/sil toplamları; KDV dahil/hariç yolları; tevkifatlı XML → fiş tutarları; ödeme sonrası durum.
- Eşleştirme: aynı cari için tüm servislerin aynı hesabı kullandığı; aynı masraf için tüm servislerin aynı 770.xx koduna gittiği.
- Arka plan: bağlamsız kapsamda işin 0 yerine beklenen sayıda satır gördüğü.
- Yetki: her rol için kritik servis yöntemlerinin reddedildiği.

### 10.3 Ürün sahibinden karar gereken sorular

1. **Puantaj Excel Grid** resmi akışa bağlanacak mı, yoksa "bilgi amaçlı" mı kalacak/kaldırılacak mı? (PUA-01)
2. **Fişi olan faturada düzenleme** serbest mi? Serbestse ters fiş + yeni fiş mi, kilit mi? (FAT-01)
3. **`GenelToplam` tanımı:** brüt mü (tevkifat ayrı alan) net mi? (MUH-03, FAT-16)
4. **Hesap planı:** Tekdüzen sabit mi, firma bazlı özelleştirilebilir mi? Eşleme tabloları ayarlardan mı yönetilecek? (MUH-01, MUH-05)
5. **WhatsApp:** gerçek sağlayıcı mı bağlanacak, modül gizlenecek mi? (INT-01)
6. **İzin matrisi:** hangi rol hangi modülde okur/yazar/onaylar? (AUTH-02)
7. **Hakediş faturası** resmi fatura mı, proforma mı; gerçek tedarikçi faturası geldiğinde eşleşme nasıl olacak? (HKD-01)

---

## 11. Satış beyanları için güvenli çerçeve

**Güvenle söylenebilir (kanıtlanmış veya zayıf noktası yok)**

- Çok firmalı kullanım ve firma bazlı veri izolasyonu (deneyle doğrulandı).
- Cari, araç, şoför/personel, güzergâh, puantaj/hakediş ve fatura kayıtlarının yönetimi (kayıt ve listeleme düzeyinde).
- Banka ekstresinin dosyadan içe aktarılması ve cariyle eşleştirme önerisi (mükerrer yükleme uyarısıyla birlikte kullanılmalı).
- Parola politikası, denetim kayıtları, şifreli belge saklama.

**Koşullu söylenmeli (demo ve kurulumda doğrulanmış senaryoyla)**

- "XML e-fatura içe aktarma": temel akış çalışır; tevkifatlı faturalar, KDV oranı raporlaması ve hata durumundaki fiş/stok davranışı Dalga 1 sonrası güvenle anlatılabilir.
- "Otomatik muhasebe fişi": fatura ve masraf fişi var; tahsilat/tediye fişi ve dönem kilidi yok.
- "KDV beyan raporu": fatura toplamları düzeltilmeden beyan hazırlığı için güvenilmemeli.
- "Rol bazlı yetkilendirme": kısmi; Bölüm 6.1.

**Şimdilik söylenmemeli**

- WhatsApp ile bildirim/hatırlatma (simülasyon).
- Zamanlanmış otomatik belge uyarısı, cari hatırlatması, günlük özet ve puantaj anomali taraması (JOB-01 çözülene kadar).
- "Puantajdan faturaya ve muhasebeye uçtan uca otomatik" (Excel Grid kopuk; hakediş faturası eksik veriyle).
- Ödemelerin faturaları otomatik kapattığı, döviz desteği, iade/iptal akışı.
- Piyasa araştırma verilerinin gerçekliği (simülasyon).

---

## 12. Doğrulama durumu ve açık noktalar

- **Çalıştırma deneyleri:** Ek A'daki üç deney (UBL-TR ayrıştırma ve fiş tutarları; `FindAsync` ve kiracı filtresi; bağlamsız kapsamda kiracı filtresi). Deneyler repodan bağımsız küçük konsol uygulamalarıdır; repoya eklenmedi.
- **Test paketi:** Bu denetimde çalıştırılmadı. Önceki oturum kaydına göre HEAD'de 14 test zaten başarısız (9 `BankMovementAuxiliaryTenantMigrationTests`, 5 `InvoiceCariTenantLinkMigrationTests`; migration test sınıfları); son tam suit sonucu 14 başarısız / 622 başarılı / 3 atlanmış. A-29 açık maddeleri bu raporun konusu değildir.
- **Canlı doğrulama gerekenler:** JOB-01 (iş günlükleri), AUTH-01 (yalnız okuma izinli kullanıcıyla yazma eylemleri), FAT-01/02/03 (arayüz senaryosu), INT-01 (gerçek telefona gönderim).
- **Doğrulanmayan varsayımlar:** MUH-10'daki alt hesap eşleşmesi (O); PER-04 ve DGR-03 (önceki oturum kayıtları); Luca job'ının tekrar aktarma davranışı (mükerrer denetimi başka bir katmanda varsa etki azalır).
- **Bilinçli yanlış alarm elemesi:** "`FindAsync` kiracı filtresini atlar" varsayımı deneyle çürütüldü ve bulgu yapılmadı.

---

## Ek A — Çalıştırma deneyleri

### A.1 UBL-TR ayrıştırma ve fiş tutarları

**Yöntem:** `FaturaService.ImportFromXmlAsync` içindeki LINQ-to-XML ifadelerinin birebir kopyası, GİB UBL-TR 1.2 yapısına uygun örnek bir fatura (1.000 TL, %20 KDV, 5/10 tevkifat, ödenecek 1.100, para birimi EUR) üzerinde çalıştırıldı; fiş tutarları `MuhasebeService.CreateFaturaFisiCoreAsync` formülüyle (`GenelToplam − TevkifatTutar` vb.) hesaplandı.

| Ölçüm | Beklenen | Kodun ürettiği |
|-------|----------|----------------|
| Ürün kodu | `SRV-001` | `'1'` (satır numarası) |
| Satır KDV oranı | 20 | 0 |
| Ara / Genel / KdvTutar / Tevkifat | 1000 / 1200 (brüt) / 200 / 100 | 1000 / 1100 / 100 / 100 |
| Fiş: 120 Borç | 1100 | 1000 |
| Fiş: 391 Alacak | 200 | 100 |
| `"PERSONEL SERVİS HİZMETİ".ToLowerInvariant().Contains("servis")` | True | False |
| `"SİGORTA PRİMİ"...Contains("sigorta")` | True | False |
| `"ARAÇ KİRALAMA"...Contains("araç")` | True | True |
| `CalculateTotals`: Proforma→Fatura | 1200 | 1400 |
| `CalculateTotals`: Puantaj fatura taslağı | 12000 | 14000 |
| `CalculateTotals`: FaturaForm (kalem tutarları atanmaz) | 1200 | 0 |

Son üç satır `CalculateTotals` formülünün (`Σ ToplamTutar + Σ KdvTutar − İskonto`) ilgili yollardaki kalem değerleriyle çalıştırılmasıdır; kalem değerleri kodun o yollarda atadığı değerlerdir (FAT-02, FAT-04).

### A.2 `FindAsync` ve kiracı filtresi

**Yöntem:** SQLite bellek içi veritabanı, `HasQueryFilter(!IsDeleted && FirmaId == aktifFirma)`; her sorgu için yeni context.

| Kayıt | `FindAsync` | `FirstOrDefault` |
|-------|-------------|------------------|
| Kendi firma, silinmemiş | bulundu | bulundu |
| Kendi firma, silinmiş | `null` | `null` |
| Başka firma | `null` | `null` |

Sonuç: EF Core 10 `FindAsync` global sorgu filtrelerini uyguluyor.

### A.3 Bağlamsız kapsamda kiracı filtresi

**Yöntem:** Uygulamadaki filtre biçimi (`FirmaTenantDisabled || (int?)FirmaId == FirmaTenantId`) iki adlandırılmış filtre (`SoftDelete`, `Tenant`) olarak taklit edildi; iki firmaya ait iki fatura eklendi.

| Bağlam | `Count()` | `Where(Id == 1)` | `IgnoreQueryFilters(["Tenant"])` |
|--------|-----------|------------------|----------------------------------|
| Bağlam yok (iş kapsamı) | **0** | **0** | 2 |
| Etkin firma = 1 (kullanıcı) | 1 | 1 | 2 |
| `TumFirmalar` (yönetici) | 2 | 1 | 2 |

Sonuç: firma bağlamı olmayan kapsamda kiracı filtreli entity'ler **hiç satır döndürmüyor** (JOB-01); yalnız adlandırılmış filtreyi açıkça atlayan sorgu veriyi görüyor.
