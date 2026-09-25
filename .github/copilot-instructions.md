# Copilot Instructions

## Project Guidelines
- Kullanıcı sohbetin Türkçe sürdürülmesini tercih ediyor.
- Kullanıcıyla Türkçe konuşulmalı.
- Kullanıcı, değişiklik yapılırken yalnızca puantaj ile ilgili kodlara dokunulmasını istiyor; ancak açıkça talep edilen Rent a Car değişikliklerine izin verilir. 
- Rent a Car değişikliklerine izin verilmeli; operasyonel puantaj kodları kapsam dışında tutulmalı ve değiştirilmemelidir.
- Operasyonel puantaj kodlarına dokunulmamalı veya değiştirilmemelidir.
- Kullanıcı, Rent a Car çalışmasında operasyonel puantaj koduna dokunulmamasını istiyor.
- Rent a Car değişiklikleri operasyonel puantaj kapsamı dışındadır ve operasyonel puantaj dosyalarına, servislerine veya veritabanı yapılarına dokunulmadan yapılmalıdır.
- Kullanıcı, puantaj alanında analiz/doküman taleplerinde kod, kod önerisi ve veritabanı scripti istemiyor; çıktının PRD seviyesinde, Türkçe, masaüstü/web ERP odaklı, mobil operasyon konularını kapsam dışı bırakan şekilde hazırlanmasını istiyor.
- Kullanıcı, Personel Düzenle ekranında girilen kesintilerin toplamının Maaş/Ödeme Yönetimi ekranındaki Kesinti hanesine yansıtılmasını istiyor.
- Operasyon planı ekranında kullanıcı, güzergâh ve plakanın ay/gün puantaj matrisi gibi yan yana görünmesini ve teyit işleminin açıkça görünen bir butonla yapılmasını tercih ediyor.
- Kullanıcı Rent a Car'ın ana sayfaya Hızlı İşlemler bağlantısı olarak değil, ana sayfanın modül/katalog alanına eklenmesini istiyor.
- Rent a Car ana sayfada açılır/kapanır bir bölüm olarak sunulmalı. Müsait araç listesi yalnızca Araç Durumu boşta olan ve seçilen tarih aralığında rezervasyon/kiralaması bulunmayan araçları içermeli; şartlar sağlanmıyorsa araç listelenmemeli. Rezervasyon oluşturma butonu aktif ve çalışır olmalı.
- Kullanıcı, Rent a Car rezervasyonunun araç teslimiyle kiralamaya dönüşmesini ve ödeme/depozito takibinin ödeme yöntemiyle birlikte ayrı bir Rent a Car sayfasında yönetilmesini istiyor.
- Rent a Car araç uygunluğunda Araç Durumu Boşta veya Kiralık olan ve seçilen tarih aralığında çakışan sözleşmesi bulunmayan araçlar listelenmeli; rezervasyonlu araçlar renkli uyarıyla gösterilmeli. Kiralama başlayınca araç durumu Operasyon yerine Kiralandı olmalı. Ödeme işlemi her sözleşmenin satırındaki Ödeme ve Detay butonlarından başlatılmalı.

## Document Storage Preferences
- Kullanıcı, manuel olarak yönetilen master.key dosyaları olmadan belge depolama tercih ediyor.
- Dosyaların yalnızca uygulama aracılığıyla erişilebilir olmasını istiyor.
- Kullanıcı, bir depo/dizin seçip hem eski hem de yeni dosyaları açabilen bir arşiv görüntüleyici istiyor.

## Data Management
- Kullanıcı, kara liste verilerinin Cari kartına alan eklenmeden ayrı bir kara liste tablosunda tutulmasını istiyor.

