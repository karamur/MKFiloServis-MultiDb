# MK Filo Servis - Puantaj ve Faturalama Kılavuzu

Bu kılavuz, servis operasyon kontratından aylık puantaj ve fatura kontrolüne kadar olan akışı resimli anlatım düzeninde özetler.

## 1. Servis Operasyon Güzergah Kaydı

Menü yolu:
`Servis Operasyon -> Güzergahlar`
veya 
`Servis Operasyon -> Güzergahlar -> Yeni Güzergah`

![Görsel 1 - Güzergah ekranı genel görünüm](images/puantaj-fatura/gorsel-01-yeni-kontrat-genel.png)

Bu ekranda güzergah ve ilgili personel/araç bilgilerini hazırlayabilirsiniz.

### Adım 1: Kurum seçin veya hızlı kayıt açın

![Görsel 2 - Kurum yazma alanı ve hızlı kayıt](images/puantaj-fatura/gorsel-02-kurum-hizli-kayit.png)

1. `Firma (Kurum)` listesinden kurum seçin.
2. Yazarken alttaki öneri listesinden hızlıca seçim yapabilirsiniz.
3. Kayıt yoksa Müşteri Kartları sayfasından veya hızlı kayıt arayüzünden kart açabilirsiniz.

Not:
Kurum seçildiğinde ve güzergah adı belirlendiğinde, güzergah adına bağlı olarak **Güzergah Kodu** oluşturulabilir, dilediğiniz gibi değiştirebilirsiniz.

### Adım 2: Güzergah bilgilerini belirleyin

![Görsel 3 - Güzergah arama, kod ve ad alanları](images/puantaj-fatura/gorsel-03-guzergah-kod-ad.png)

1. `Güzergah Adı` alanına güzergah ismini yazın.
2. `Güzergah Kodu` otomatik oluşturulur, dilerseniz değiştirebilirsiniz.
3. `Sefer Tipi` (Yön Bilgisi) alanından `Sabah`, `Akşam`, `SabahAksam` veya `Saatlik` seçin.
4. Başlangıç, Bitiş, Personel Sayısı ve Tahmini Mesafe kayıtlarını isteğe bağlı detaylandırabilirsiniz.

### Adım 3: Araç ve Personel eşleştirmesi yapın

![Görsel 4 - Plaka, personel ve telefon alanları](images/puantaj-fatura/gorsel-04-plaka-personel-telefon.png)

1. `Varsayılan Araç` listesinden plaka yazarak aracı seçin.
2. Araç için aktif bir şoför eşleştirmesi varsa ileride personeli otomatik getirir, ancak siz formu hazırlarken `Varsayılan Şoför` listesinden seçim yapabilirsiniz.
3. İlgili şoförün bilgileri listelerde `Ad Soyad` olarak gösterilir, sistem üzerinden telefon gibi detaylarına ulaşılır.

### Adım 4: Gelir ve gider alanlarını belirleyin

![Görsel 5 - Tahsilat, ödeme ve firma bağlantısı](images/puantaj-fatura/gorsel-05-tahsilat-odeme-firma.png)

1. `Birim Fiyat` alanına (Kuruma uygulanacak Gelir/Tahsilat fiyatı) tutarı girin.
2. Gider fiyatı hesaplamaları (Puantaj esnasında Tedarikçi gideri) için diğer formlar kullanılır.
3. Formdaki `Aktif` durumu açık ise güzergah kontratları, puantaj hazırlamaya hazırdır.
4. Kaydı tamamlamak için `Kaydet` butonuna bastığınızda, güzergah listeye eklenir.

---

## 2. Aylık Puantaj Oluşturma

Menü yolu:
`Filo Yönetimi -> Servis Operasyon -> Kontratlar -> Kontrat Detayı -> Puantaj`

![Görsel 6 - Kontrat detayında puantaj sekmesi](images/puantaj-fatura/gorsel-06-puantaj-sekmesi.png)

1. İlgili kontratı açın.
2. `Puantaj` sekmesine geçin.
3. `Yeni Puantaj` düğmesine basın.
4. Yıl, ay, çalışma sayısı, tahsilat birim fiyatı ve gerekiyorsa ödeme fiyatını girin.
5. `Kaydet` ile aylık puantaj kaydını oluşturun.

Not:
Tedarikçi kontratlarında ödeme tarafı da aynı ekranda hesaplanır.

---

## 3. Puantaj Kontrol ve Mali Özet

Menü yolu:
`Filo Yönetimi -> Servis Operasyon -> Kontratlar -> Kontrat Detayı -> Özet / Mali Durum`

![Görsel 7 - Tahsilat, ödeme ve kar özeti](images/puantaj-fatura/gorsel-07-mali-ozet.png)

Bu ekranda şunları takip edebilirsiniz:

1. Toplam tahsilat tutarı
2. Gerçekleşen tahsilat
3. Bekleyen tahsilat
4. Toplam ödeme
5. Gerçekleşen ödeme
6. Bekleyen ödeme
7. Brüt kar / zarar

Bu bölüm, puantaj verisinin faturalamaya hazır olup olmadığını hızlı kontrol etmek için kullanılır.

---

## 4. Fatura Süreci

Menü yolu:
`Cari Yönetimi -> Faturalar`

![Görsel 8 - Giden ve gelen fatura giriş ekranı](images/puantaj-fatura/gorsel-08-fatura-giris.png)

### Giden Fatura

1. Kuruma kesilecek faturada kontrattaki veya puantajdaki tahsilat toplamını esas alın.
2. Cari olarak ilgili kurumu seçin.
3. Fatura satır açıklamasında güzergah adı, dönem ve yön bilgisini kullanın.

### Gelen Fatura veya Tedarikçi Gideri

1. Tedarikçi ile çalışan kontratlarda ödeme toplamını baz alın.
2. Tedarikçi veya ilgili cari kartını seçin.
3. Gider faturasının toplamını puantajdaki ödeme tarafı ile karşılaştırın.

---

## 5. Önerilen İş Akışı

1. Önce kurum ve güzergahı Servis Operasyon kontrat ekranında hazırlayın.
2. Aynı ekranda plaka seçerek personel ve telefon bilgisini otomatik doldurun.
3. Gelir ve gider fiyatlarını tanımlayıp kontratı kaydedin.
4. Her ay puantaj sekmesinden dönem kaydını oluşturun.
5. Mali özetten tahsilat ve ödeme toplamlarını kontrol edin.
6. Son aşamada cari fatura ekranından giden veya gelen faturayı kesin.
