# A-23 — Belge ve kaynak teslim kararı

**Tarih:** 2026-10-08
**Kapsam:** Kaynak depo, satışa çıkarım raporları ve kurulum girdisi. Müşteri kurulum kabulü A-21'de izlenir.

## Geçerli belgeler

- [Görev envanteri](SATISA-CIKARIM-GOREV-ENVANTERI-2026-10-06.md) A-01…A-31 için tek güncel görev ve renk kaynağıdır.
- [Son durum](SATISA-CIKARIM-SON-DURUM-2026-10-05.md) satış kararını ve kapanış sınırlarını özetler.
- [İlk analiz](SATISA-CIKARIM-ANALIZ-RAPORU.md) 39 tarihsel K/Y/O/D bulgusunu saklar; bu bulgular güncel 31 görevle bire bir aynı sayım değildir.
- [İkinci denetim](DUZELTME-DENETIM-RAPORU-2.md) tarihli düzeltme kaydıdır. Eski bir bölümün durum ifadesi, güncel envanter satırının yerine geçmez.

2026-10-02 tarihli ayrı `SATISA-CIKARIM-YENIDEN-ANALIZ-2026-10-02.md` dosyası mevcut ağaçta veya Git geçmişinde bulunamadı. İçeriği tahmin edilerek yeniden yazılmadı; güncel 31 görev envanteri bu belgenin teslim işlevini üstlenir. İlk denetim belgesi 0aecc4f1 commit'inde kaldırılmıştır ve Git geçmişinde `git show 04342dc3:docs/DUZELTME-DENETIM-RAPORU.md` ile okunabilir. Rent-a-Car tarihsel analizinin önceki sürümü `git show 89e46c3c:docs/analiz/Rent-a-Car-Modulu-Analiz-Raporu.md` ile okunabilir. Bu tarihsel belgeler güncel satış kararı olarak yeniden yayımlanmaz.

## Teslim girdisi ve yerel veri

- `MKFiloServis.Web/dbsettings.json`, `portalsettings.json`, `backup_settings.json` sürüm takibinden çıkarıldı; mevcut yerel dosyalar silinmedi. `.kilo/` de yerel ayar olarak Git dışında tutulur.
- Web publish, çalışma zamanı ayarlarını, üretim appsettings dosyasını ve oturum çerezini içerik listesinden çıkarır. `setup/build.ps1` bu dosyaları payload içinde bulursa, `-SkipPublish` kullanılsa da paketlemeyi durdurur.
- Ana, müşteri ve güncelleme Inno kurulum girdileri çalışma zamanı ayarlarını ve yerel veri/yedek dosyalarını dışlar. `setup/README.md` mevcut ortak veritabanı ve kurulumda üretilen ayar sözleşmesini açıklar.
- İzlenen kaynaklarda `bin`, `obj`, TestResults ve üretilmiş kurulum çıktısı bulunmaz. Yerel build/test logları teslim dosyası sayılmaz.

## Doğrulama ve sınır

`dotnet publish MKFiloServis.Web/MKFiloServis.Web.csproj -c Release --no-restore -o TestResults/a23-publish --nologo -v minimal` başarılı oldu. Çıktıda `dbsettings.json`, `portalsettings.json`, `backup_settings.json`, `appsettings.Production.json`, `cookies.txt` veya `.db` dosyası bulunmadı; temel `appsettings.json` bulundu. `setup/build.ps1` PowerShell parser denetiminden geçti. Gerçek Inno EXE üretilmedi ve hedef makine kurulumu yapılmadı; bunlar A-21 kabulünde açıktır. Git geçmişindeki olası eski sırların rotasyonu A-06'da izlenir.

**A-23 teknik belge ve kaynak teslim kapsamı: 🟢 tamamlandı.** Bu renk ürünün satışa hazır olduğu anlamına gelmez.
