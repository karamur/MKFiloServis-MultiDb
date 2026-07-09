# ⭐ Satır Bazlı Günlük Grid - Özet Rehberi (23 Ocak 2025)

## TL;DR - 2 Dakikalık Özet

**Kullanıcı İsteği:**  
Puantaj giriş yaparken, eğer aylık veya tarih aralığı seçerse, sonuçlar **satır bazlı grid** olarak gösterilsin ve **eksik günlere araç+şoför doğrudan eklenebilsin**.

**Çözüm (Yeni Sayfa 3):**
```
1. Dönem Seç (Aylık OR Tarih Aralığı)
   ↓
2. Satır Grid (22 iş günü = 22 satır)
   - Tarih | Güzergah | Araç+Şoför | Sefer | Durum | Notlar | İşlem
   - Dolu gün: Edit mümkün ("✎ Düzelt")
   - Boş gün: Add mümkün ("➕ Ekle")
   ↓
3. Eksik Günleri Doldur
   - "➕ Ekle" click → Rota seç → Araç seç → Şoför seç → "✓ Kaydet"
   ↓
4. Inline Düzeltme
   - "✎ Düzelt" → Sefer/Durum/Notlar değiştir → "✓ Kaydet"
   ↓
5. Uyuşmazlık Kontrol
   - "🔍 Kontrol Et" → Toplu vs Günlük total karşılaştır
   ↓
6. Tamamla
   - "✓ Tamamla & Faturaya Git" (consistency OK ise)
```

**Avantajlar:**
- ✅ Hızlı overview (22 satır göz gezdir)
- ✅ Eksik günü kendi ekle (araç+şoför)
- ✅ Makzul transparent (Red row)
- ✅ Fatura-ready (auto tahakkuk)
- ✅ SGK-ready (FiloGunlukPuantaj auto populate)

---

## 📋 Teknik Arkafon

**Yeni Dosya:**  
`GUNLUK-GRID-TASARIMI-SATIR-BAZLI.md` - Tam tasarım (wireframe + Razor component kodu)

**Key Components:**
1. **UI:** `PuantajGunlukDetay.razor` (Yeni Blazor page)
2. **DTO:** `PuantajGunlukDetayDTO` (22 gün kaydı)
3. **Backend:** `IPuantajTopluGirisiService.BulkUpdateGunlukDetaylarAsync()`
4. **DB:** `FiloGunlukPuantaj` (22 record auto-populate)

**Akış:**
```
Operatör seçer (Dönem + Araç)
   ↓
[Web Service] LoadGunlukDetaylar()
   ↓
[DB] SELECT FiloGunlukPuantaj WHERE Tarih BETWEEN ? AND ? AND AracId = ?
   ↓
[UI] 22 satır grid göster (dolu gün + boş gün)
   ↓
Operatör "➕ Ekle" → [Web] UpdateGunlukDetayAsync() → [DB] INSERT
Operatör "✎ Düzelt" → [Web] UpdateGunlukDetayAsync() → [DB] UPDATE
   ↓
[Web] CheckConsistency() → Toplu total vs Günlük total karşılaştır
   ↓
Operatör "✓ Tamamla" → Fatura sayfasına geç
```

---

## 🎯 UI Wireframe

```
┌──────────────────────────────────────────────────────────────┐
│ 📋 Günlük Puantaj Detayları                                 │
├──────────────────────────────────────────────────────────────┤
│
│ 📅 Dönem Seçimi:  [Aylık ●  Tarih Aralığı ○]  [Ocak 2025]↓
│ 🚗 Araç:          [Araç A (34-ABC-1234)]      [Değiştir]
│
├──────────────────────────────────────────────────────────────┤
│ 📊 ÖZET KARTLARI:                                            │
│  [Toplam: 22]  [Dolu: 20]  [Boş: 2]  [Makzul: 1]  [✓ OK]   │
├──────────────────────────────────────────────────────────────┤
│
│ 📋 GÜNLÜK GRID (Editable):
│
│ ┌──┬─────────────┬───────────┬──────────────┬────┬────┬──────┬──────┐
│ │ # │   Tarih    │  Güzergah │ Araç+Şoför   │Sfr │Dur │Notlar│İşlem │
│ ├──┼─────────────┼───────────┼──────────────┼────┼────┼──────┼──────┤
│ │ 1 │ Pzt 26 Ara │ R. TRT    │ A / Şf1      │ 1  │ ✓  │      │✎ 🗑  │
│ │ 2 │ Sal 27 Ara │ R. TRT    │ A / Şf1      │ 1  │ ✓  │      │✎ 🗑  │
│ │ 3 │ Çar 28 Ara │ [BOŞA]    │ [ ] / [ ]    │ 0  │ W/O│      │➕ 🗑  │
│ │ 4 │ Per 29 Ara │ R. Banka  │ A / Şf1      │ 3  │Makz│      │✎ 🗑  │ RED ROW
│ │ 5 │ Cum 30 Ara │ R. TRT    │ A / Şf1      │ 1  │ ✓  │      │✎ 🗑  │
│ │ 6 │ Pzt 2 Oca  │ [BOŞA]    │ [ ] / [ ]    │ 0  │ W/O│      │➕ 🗑  │
│ │ 7 │ Sal 3 Oca  │ R. TRT    │ A / Şf1      │ 1  │ ✓  │      │✎ 🗑  │
│ │...(22 günü listele)...                                       │
│ └──┴─────────────┴───────────┴──────────────┴────┴────┴──────┴──────┘
│
├──────────────────────────────────────────────────────────────┤
│ 🔘 IŞLEMLER:                                                 │
│  [← Haftalık Özete Dön]  [🔍 Kontrol Et]  [✓ Tamamla ✓]     │
└──────────────────────────────────────────────────────────────┘
```

---

## 💻 Blazor Component (Kısaltılmış)

```razor
@page "/operasyon/puantaj-gunluk-detay"
@inject IPuantajTopluGirisiService PuantajService

<div class="container-fluid mt-4">
    <!-- Dönem & Araç Seçimi -->
    <div class="card mb-3 bg-light">
        <div class="card-body">
            <div class="row align-items-center">
                <div class="col-md-4">
                    <label>📅 Dönem:</label>
                    <div class="btn-group">
                        <input type="radio" @onchange="@((ChangeEventArgs e) => SetPeriodType("month"))" checked />
                        <label>Aylık</label>
                        <input type="radio" @onchange="@((ChangeEventArgs e) => SetPeriodType("range"))" />
                        <label>Tarih Aralığı</label>
                    </div>
                </div>
                <div class="col-md-4">
                    @if (_periodType == "month")
                    {
                        <input type="month" class="form-control" @bind="_selectedMonth" 
                               @onchange="LoadGunlukDetaylar" />
                    }
                    else
                    {
                        <input type="date" class="form-control" @bind="_startDate" 
                               @onchange="LoadGunlukDetaylar" />
                    }
                </div>
                <div class="col-md-4">
                    <select class="form-select" @bind="_selectedAracId" @onchange="LoadGunlukDetaylar">
                        <option value="">-- Araç Seçini --</option>
                        @foreach (var arac in _aracList)
                        {
                            <option value="@arac.Id">@arac.Plaka</option>
                        }
                    </select>
                </div>
            </div>
        </div>
    </div>

    <!-- Özet Kartlar -->
    @if (_gunlukDetaylar != null)
    {
        <div class="row mb-3">
            <div class="col-md-2">
                <div class="card text-center">
                    <div class="card-body">
                        <h6>Toplam Gün</h6>
                        <h3>@_gunlukDetaylar.Count</h3>
                    </div>
                </div>
            </div>
            <div class="col-md-2">
                <div class="card text-center">
                    <div class="card-body">
                        <h6>Dolu Gün</h6>
                        <h3>@_gunlukDetaylar.Count(x => !string.IsNullOrEmpty(x.GuezergahAdi))</h3>
                    </div>
                </div>
            </div>
            <!-- ... Boş, Makzul, Total Sefer kartları ... -->
        </div>
    }

    <!-- Günlük Grid -->
    <table class="table table-bordered table-sm">
        <thead class="table-dark">
            <tr>
                <th>No</th>
                <th>Tarih</th>
                <th>Güzergah</th>
                <th>Araç+Şoför</th>
                <th>Sefer</th>
                <th>Durum</th>
                <th>Notlar</th>
                <th>İşlem</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var (detay, index) in _gunlukDetaylar.Select((d, i) => (d, i)))
            {
                <tr class="@GetRowClass(detay)">
                    <td>@(index + 1)</td>
                    <td>@detay.Tarih.ToString("ddd dd MMM", trCulture)</td>
                    <td>
                        @if (detay.IsEditing)
                        {
                            <select class="form-select form-select-sm" @bind="detay.GuezergahId"
                                    @onchange="@((ChangeEventArgs e) => UpdateGuezergahInfo(index, e))">
                                <option>-- Seç --</option>
                                @foreach (var rota in _rotaList)
                                {
                                    <option value="@rota.Id">@rota.GuzergahAdi</option>
                                }
                            </select>
                        }
                        else
                        {
                            <span>@detay.GuezergahAdi</span>
                        }
                    </td>
                    <td>
                        @if (detay.IsEditing)
                        {
                            <select class="form-select form-select-sm" @bind="detay.AracId">
                                <option>-- Araç --</option>
                                @foreach (var arac in _aracList)
                                {
                                    <option value="@arac.Id">@arac.Plaka</option>
                                }
                            </select>
                            <select class="form-select form-select-sm" @bind="detay.SoforId">
                                <option>-- Şoför --</option>
                                @foreach (var sofor in _soforList)
                                {
                                    <option value="@sofor.Id">@sofor.Ad</option>
                                }
                            </select>
                        }
                        else
                        {
                            <strong>@GetAracAd(detay.AracId)</strong> / @GetSoforAd(detay.SoforId)
                        }
                    </td>
                    <td>
                        @if (detay.IsEditing)
                        {
                            <input type="number" class="form-control form-control-sm" @bind="detay.SeferSayisi" />
                        }
                        else
                        {
                            @detay.SeferSayisi
                        }
                    </td>
                    <td>
                        @if (detay.IsEditing)
                        {
                            <select class="form-select form-select-sm" @bind="detay.Durum"
                                    @onchange="@((ChangeEventArgs e) => UpdateDurum(index, e))">
                                <option value="1">Gitti</option>
                                <option value="2">Makzul</option>
                                <option value="3">Taksi</option>
                                <option value="4">Tatil</option>
                            </select>
                        }
                        else
                        {
                            <span class="badge @GetDurumClass(detay.Durum)">@GetDurumAdi(detay.Durum)</span>
                        }
                    </td>
                    <td>
                        @if (detay.IsEditing)
                        {
                            <input type="text" class="form-control form-control-sm" @bind="detay.Notlar" />
                        }
                        else
                        {
                            <small>@detay.Notlar</small>
                        }
                    </td>
                    <td>
                        @if (detay.IsEditing)
                        {
                            <button class="btn btn-sm btn-success" @onclick="@(() => SaveDetay(index))">✓</button>
                            <button class="btn btn-sm btn-secondary" @onclick="@(() => CancelEdit(index))">✕</button>
                        }
                        else
                        {
                            @if (string.IsNullOrEmpty(detay.GuezergahAdi))
                            {
                                <button class="btn btn-sm btn-outline-success" @onclick="@(() => EditDetay(index))">➕</button>
                            }
                            else
                            {
                                <button class="btn btn-sm btn-outline-primary" @onclick="@(() => EditDetay(index))">✎</button>
                            }
                            <button class="btn btn-sm btn-outline-danger" @onclick="@(() => DeleteDetay(index))">🗑</button>
                        }
                    </td>
                </tr>
            }
        </tbody>
    </table>

    <!-- Bottom Buttons -->
    <div class="d-flex justify-content-between mt-4">
        <button class="btn btn-secondary" @onclick="GoBack">← Haftalık Özet</button>
        <div>
            <button class="btn btn-warning me-2" @onclick="CheckConsistency">🔍 Kontrol Et</button>
            <button class="btn btn-success btn-lg" @onclick="FinalizeAndGoToFatura"
                    disabled="@(!_consistencyOk)">✓ Tamamla</button>
        </div>
    </div>
</div>

@code {
    private string _periodType = "month";
    private string _selectedMonth = DateTime.Now.ToString("yyyy-MM");
    private DateTime _startDate = DateTime.Now;
    private int _selectedAracId;

    private List<PuantajGunlukDetayDTO> _gunlukDetaylar = new();
    private List<Arac> _aracList = new();
    private List<Guzergah> _rotaList = new();
    private List<Sofor> _soforList = new();
    private bool _consistencyOk = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        // TODO: Load araç, rota, şoför listesi
    }

    private async Task LoadGunlukDetaylar()
    {
        // TODO: 22 iş günü grid oluştur + DB'den mevcut kaydları load et
    }

    private void EditDetay(int index) => _gunlukDetaylar[index].IsEditing = true;

    private async Task SaveDetay(int index)
    {
        // TODO: Service call to UpdateGunlukDetayAsync
        await CheckConsistency();
    }

    private async Task CheckConsistency()
    {
        // TODO: Toplu vs Günlük total karşılaştır
    }

    private async Task FinalizeAndGoToFatura()
    {
        // TODO: Fatura sayfasına geç
    }

    // ... Helper methods ...
}
```

---

## 📦 Backend Service Update

```csharp
public async Task BulkUpdateGunlukDetaylarAsync(
    int puantajTopluGirisiId,
    List<PuantajGunlukDetayDTO> detaylar,
    CancellationToken cancellationToken = default)
{
    var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    try
    {
        foreach (var detay in detaylar)
        {
            await UpdateGunlukDetayAsync(
                puantajTopluGirisiId,
                detay.Tarih,
                detay.GuezergahId,
                detay.SeferSayisi,
                detay.Durum,
                detay.Notlar,
                cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        _logger.LogInformation("Bulk update: {Count} kayıt", detaylar.Count);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
```

---

## ✅ Checklist

- [ ] `PuantajGunlukDetay.razor` sayfası oluştur
- [ ] Dönem seçimi (Aylık vs Tarih Aralığı) implement et
- [ ] 22 iş günü grid otomatik generate et
- [ ] Inline editing (Sefer, Durum, Notlar) implement et
- [ ] "➕ Ekle" button (eksik gün ekle) implement et
- [ ] "✎ Düzelt" button (edit mode toggle) implement et
- [ ] Makzul yönetimi (Red row, auto sefer=0) implement et
- [ ] Özet kartları (Dolu/Boş/Makzul/Total) implement et
- [ ] Uyuşmazlık kontrol ("🔍 Kontrol Et") implement et
- [ ] `BulkUpdateGunlukDetaylarAsync()` service method yazın
- [ ] Test: Aylık seçim → grid doldur
- [ ] Test: Tarih aralığı seçim → grid doldur
- [ ] Test: "➕ Ekle" → Araç+Şoför ekleme çalışıyor mu?
- [ ] Test: "✎ Düzelt" → Sefer değişikliği çalışıyor mu?
- [ ] Test: Makzul durum → sefer otomatik 0 oluyor mu?
- [ ] Test: Uyuşmazlık kontrol → Consistency badge doğru mu?

---

## 📚 Vollständige Dokumentation

Tam tasarım, wireframe, Razor component kodu, DTO tanımları ve workflow adımları için bkz:  
👉 **[GUNLUK-GRID-TASARIMI-SATIR-BAZLI.md](./GUNLUK-GRID-TASARIMI-SATIR-BAZLI.md)**

---

**Tarih**: 23 Ocak 2025  
**Versiyon**: 1.0 - Satır Bazlı Grid Design Update
