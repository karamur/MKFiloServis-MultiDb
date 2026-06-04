# KOAFiloServis Multi-DB Ara Analiz Raporu

Bu dosya, mevcut inceleme sırasında toplanan **kanıta dayalı ara bulguları** içerir.

## 1. Runtime DbContext Oluşturma Zinciri

### Kanıt
`KOAFiloServis.Web/Program.cs`
- `110-141`: `ApplicationDbContext` için önce pooled factory tanımlanıyor.
- `147-160`: bu pooled factory kaldırılıyor.
- `154`: `ITenantConnectionStringProvider -> TenantConnectionStringProvider`
- `160`: `IDbContextFactory<ApplicationDbContext> -> TenantDbContextFactory`
- `163-167`: scoped `ApplicationDbContext`, bu factory üzerinden oluşturuluyor.

### Sonuç
Normal runtime'ta servislerin kullandığı `IDbContextFactory<ApplicationDbContext>` doğrudan pooled default factory değil, **tenant-aware `TenantDbContextFactory`**.

---

## 2. Aktif Tenant Bilgisi Connection String'e Nasıl Dönüşüyor

### Kanıt 1
`KOAFiloServis.Web/Services/IAktifFirmaProvider.cs`
- `24`: `AktifFirmaId`
- `35`: `Mevcut`
- `98-103`: `Set(...)` ile seçili firma state'e yazılıyor

### Kanıt 2
`KOAFiloServis.Web/Services/FirmaService.cs`
- `179-195`: `SetAktifFirma(int firmaId)`
- Seçilen `Firma` kaydından şu alanlar provider'a yazılıyor:
  - `FirmaId`
  - `FirmaKodu`
  - `FirmaAdi`
  - `DatabaseName`

### Kanıt 3
`KOAFiloServis.Web/Services/TenantConnectionStringProvider.cs`
- `34-42`: `GetTenantConnectionString()`
- `36-41`:
  - aktif firma alınır
  - `Mevcut.DatabaseName` okunur
  - `GetConnectionStringForFirma(...)` çağrılır
- `44-60`: `Database = databaseName` olacak şekilde yeni Npgsql connection string üretilir
- `37-38` ve `46-47`:
  - aktif firma yoksa veya `DatabaseName` boşsa fallback olarak `DefaultConnection` dönülür

### Kanıt 4
`KOAFiloServis.Web/Data/TenantDbContextFactory.cs`
- `28-36` ve `39-47`:
  - `connStr = _connectionStringProvider.GetTenantConnectionString() ?? _connectionStringProvider.GetMasterConnectionString();`
  - sonra `ApplicationDbContext(options)` oluşturuluyor
- `50-66`:
  - `UseNpgsql(connectionString, ...)`

### Zincir
Login / firma seçimi  
↓  
`FirmaService.SetAktifFirma(...)`  
↓  
`IAktifFirmaProvider.Mevcut.DatabaseName`  
↓  
`TenantConnectionStringProvider.GetTenantConnectionString()`  
↓  
`TenantDbContextFactory.CreateDbContext()`  
↓  
`ApplicationDbContext`

### Sonuç
Normal kullanıcı akışında `ApplicationDbContext`, aktif firmanın `DatabaseName` alanına göre **tenant DB**'ye bağlanacak şekilde tasarlanmış.

---

## 3. Personel Modülü Analizi

Bu projede “Personel” ana CRUD akışı `Sofor` entity’si üzerinden yürütülüyor.

### UI ekranları
`KOAFiloServis.Web/Components/Pages/Soforler/SoforList.razor`
- `1-2`: route `/soforler` ve `/personel`
- `8`: `@inject ISoforService SoforService`
- `13`: ayrıca `IDbContextFactory<ApplicationDbContext> DbContextFactory`

`KOAFiloServis.Web/Components/Pages/Soforler/SoforForm.razor`
- `1-4`: yeni/düzenle personel route’ları
- `9`: `@inject ISoforService SoforService`
- `19`: ayrıca `IDbContextFactory<ApplicationDbContext> DbContextFactory`

---

## 4. Personel Listeleme Hangi Database'i Kullanıyor?

### Kanıt
`KOAFiloServis.Web/Services/SoforService.cs`
- `22-32`: `GetAllAsync()`
- `25`: `await _contextFactory.CreateDbContextAsync();`
- aynı pattern:
  - `34-45` `GetActiveAsync`
  - `55-64` `GetByIdAsync`
  - `188-199` `GetByGorevAsync`
  - `201-212` `GetActiveSoforlerAsync`

### DbContext zinciri
`SoforService -> IDbContextFactory<ApplicationDbContext> -> TenantDbContextFactory -> TenantConnectionStringProvider -> aktif firmanın DatabaseName`

### Sonuç
**Personel listelenirken tenant DB kullanılıyor**  
kanıt zinciri: service factory bağımlılığı + runtime DI bağlamı.

---

## 5. Personel Kayıt Edilirken Hangi Database'i Kullanıyor?

### Kanıt
`KOAFiloServis.Web/Components/Pages/Soforler/SoforForm.razor`
- `1123-1188`: `Kaydet()`
- `1170`: düzenlemede `await SoforService.UpdateAsync(sofor);`
- `1185`: yeni kayıtta `await SoforService.CreateAsync(sofor);`

`KOAFiloServis.Web/Services/SoforService.cs`
- `67-80`: `CreateAsync`
  - `69`: `await _contextFactory.CreateDbContextAsync();`
  - `78`: `context.Soforler.Add(sofor);`
  - `79`: `await context.SaveChangesAsync();`
- `83-130`: `UpdateAsync`
  - `85`: `await _contextFactory.CreateDbContextAsync();`
  - `128`: `await context.SaveChangesAsync();`
- `133-145`: `DeleteAsync`
  - `135`: `await _contextFactory.CreateDbContextAsync();`
  - `143`: `await context.SaveChangesAsync();`

### Sonuç
**Personel kaydedilirken / güncellenirken / silinirken tenant DB kullanılıyor**  
çünkü aynı tenant-aware factory zinciri üzerinden `ApplicationDbContext` oluşturuluyor.

---

## 6. Personel Modülü İçin Kullanılan Service / DbContext / Connection String Özeti

### Personel listeleme
- Kullanılan service: **`ISoforService` / `SoforService`**
- Kullanılan DbContext: **`ApplicationDbContext`**
- Oluşturma şekli: **`IDbContextFactory<ApplicationDbContext>.CreateDbContextAsync()`**
- Bağlantı kaynağı: **`TenantDbContextFactory`**
- Connection string kaynağı: **aktif firmanın `DatabaseName`’inden üretilen tenant connection string**

### Personel ekleme
- Kullanılan service: **`SoforService.CreateAsync`**
- Kullanılan DbContext: **`ApplicationDbContext`**
- Bağlantı: **tenant DB**

### Personel güncelleme
- Kullanılan service: **`SoforService.UpdateAsync`**
- Kullanılan DbContext: **`ApplicationDbContext`**
- Bağlantı: **tenant DB**

### Personel silme
- Kullanılan service: **`SoforService.DeleteAsync`**
- Kullanılan DbContext: **`ApplicationDbContext`**
- Bağlantı: **tenant DB**

---

## 7. Kritik Gözlem

`KOAFiloServis.Web/Services/TenantConnectionStringProvider.cs`
- `37-38`: aktif firma yoksa `DefaultConnection`
- `46-47`: `DatabaseName` boşsa yine `DefaultConnection`

Bu şu anlama gelir:
- firma seçimi yapılmamışsa
- ya da seçilen firmanın `DatabaseName` alanı boşsa

`ApplicationDbContext` fallback olarak **DestekCRMServisBlazorDb**'ye dönebilir.

Bu, şu ana kadar Personel CRUD'ün hatalı çalıştığını **kanıtlamaz**.  
Ama mimari olarak bir **fallback riski** olduğunu kanıtlar.

---

## 8. Bu Aşamadaki Ara Karar

Şu anki kanıta göre:

- **Personel CRUD akışı tasarım olarak tenant DB'ye yönleniyor**
- Doğrudan “Personel insert/select yanlışlıkla DestekCRMServisBlazorDb kullanıyor” kanıtı henüz yok
- Ama **aktif firma / DatabaseName eksikse DefaultConnection fallback** mekanizması mevcut

---

## 9. Henüz Tamamlanmamış Analiz Başlıkları

Tam nihai karar için henüz tamamlanmadı:

1. **Bütçe modülü**
2. runtime sırasında `DefaultConnection` fallback’ine düşen başka kod yolları
3. background/hosted/migration servisleri
4. anti-pattern kontrolü:
   - `INSERT -> DestekCRMServisBlazorDb`
   - `SELECT -> tenant DB`

---

## 10. Bu Aşamadaki Net Cevaplar

### Personel kayıt edilirken hangi database kullanılıyor?
**Kanıta göre zincir tenant DB’yi hedefliyor.**

### Personel listelenirken hangi database kullanılıyor?
**Kanıta göre zincir tenant DB’yi hedefliyor.**

### Doğrudan `Database.GetConnectionString()` çıktısı var mı?
Bu akış için henüz doğrudan runtime print alınmadı.  
Ama DI ve factory zinciri satır bazında çıkarılmış durumda.
