# Operasyonel Puantaj: Cari→Kurum UI Mockup & Bileşen Mimarisi

**Tarih**: 2025-01-23  
**Odak**: Blazor Bileşen Tasarımı, Layout ve UX Flow  
**Hedef Sayfalar**: 
- `EslestirmeTanimlari.razor` (Main Page - Mevcut)
- `CariPuantajDashboard.razor` (Yeni)
- Alt Bileşenler (Yeni)

---

## 1. Sayfa Mimarisi Genel Bakış

```
MKFiloServis.Web/
├── Components/
│   ├── Pages/
│   │   ├── Filo/
│   │   │   ├── EslestirmeTanimlari.razor ← [REVIZE EDİLECEK]
│   │   │   └── ... (diğer sayfalar)
│   │   └── Dashboards/
│   │       └── CariPuantajDashboard.razor ← [YENİ]
│   └── CommonComponents/
│       ├── CariSelector.razor ← [YENİ]
│       ├── KurumAkkordiyonu.razor ← [YENİ]
│       ├── PuantajGridByKurum.razor ← [YENİ]
│       └── CariToplam.razor ← [YENİ]
└── ...
```

---

## 2. EslestirmeTanimlari.razor (Revize Versiyonu)

### 2.1 Layout Mockup (ASCII Ascii)

```
┌────────────────────────────────────────────────────────────────┐
│  Araç, Şoför ve Güzergah Eşleme Havuzu (CARİ Kırılımlı)        │
│  Özmal veya komisyonlu servislerin puantaj şablon tanımları    │
├────────────────────────────────────────────────────────────────┤
│
│  ┌─ Cari Seçimi ─────────────────────────────────────────────┐
│  │ [Dropdown/Autocomplete: TRT-ANKARA ▼]                     │
│  │ Veya: [Arama: "TRT" ] [Yakındaki: İSKİ, ESHOT]           │
│  └────────────────────────────────────────────────────────────┘
│
│  ┌─ SEÇİLİ CARİ: TRT-ANKARA (3 Kurum) ────────────────────┐
│  │                                    [Puantaj Oluştur ▼]  │
│  │
│  │  ┌─ Kurum 1: TRT Ankara Merkez (5 Eşleştirme) ▼ ──────┐
│  │  │ Durum | Müşteri | Plaka | Araç Sahibi | Şoför | ... │
│  │  ├────────────────────────────────────────────────────┤
│  │  │ ✓ Aktif | TRT  | 34CX8 | Özmal | Ali Demir | ... │
│  │  │ ✓ Aktif | TRT  | 06TX2 | Taşeron | Veli Kaya | ... │
│  │  │ ✓ Aktif | TRT  | 24RH4 | Özmal | Mehmet Yıl | ... │
│  │  │
│  │  │ [Aylık Puantaj Grid (2025-01):
│  │  │  Row: Plaka 34CX8 / Ali Demir
│  │  │   Pzt | Sal | Çar | Per | Cum | Cts | Paz | TOPLAM | [Kaydet][Sil]
│  │  │   [ 2][ 2][ 2][ 0][ 3][ 3][ 0] |  12   | ...
│  │  │
│  │  │  Row: Plaka 06TX2 / Veli Kaya
│  │  │   [ 1][ 1][ 2][ 2][ 2][ 1][ 0] |   9   | ...
│  │  │
│  │  │  Row: Plaka 24RH4 / Mehmet Yıl
│  │  │   [ 3][ 3][ 3][ 3][ 3][ 0][ 0] |  15   | ...
│  │  │
│  │  │  Kurum Toplam: 36 Sefer (2025-01)
│  │  │
│  │  │  [Kaydet] [Rapor Yazdır]
│  │  │]
│  │  └────────────────────────────────────────────────------────┘
│  │
│  │  ┌─ Kurum 2: TRT Ankara Çiftçiler Pazarı (3 Eşleştirme) ▼ ──┐
│  │  │ [benzeri layout]                                         │
│  │  └───────────────────────────────────────────────────────-──┘
│  │
│  │  CARİ TOPLAM (TRT-ANKARA): 60 Sefer → 45,000 TL (Ocak 2025)
│  │
│  └─────────────────────────────────────────────────----────────┘
│
│  ┌─ DİĞER CARİLER (Kompakt Görünüm) ─────────────────────────┐
│  │ İSKİ | 2 Kurum | 25 Sefer | 18,750 TL    [Aç ▶]           │
│  │ ESHOT | 1 Kurum | 10 Sefer | 7,500 TL    [Aç ▶]           │
│  │ ...                                                       │
│  └───────────────────────────────────────────────────────────┘
│
└────────────────────────────────────────────────────────────────┘
```

### 2.2 Kod Yapısı (Yeni EslestirmeTanimlari.razor)

```razor
@page "/filo/komisyon-eslestirme"
@using MKFiloServis.Web.Services.Interfaces
@using MKFiloServis.Web.Components.Pages.Partials
@using MKFiloServis.Web.Models

@inject IFiloKomisyonService FiloKomisyonService
@inject IToastService ToastService
@inject ILogger<EslestirmeTanimlari> Logger

@rendermode InteractiveServer
@attribute [Authorize]

<PageTitle>Araç, Şoför ve Güzergah Eşleme Havuzu</PageTitle>

<div class="container-fluid">
    <!-- BAŞLIK -->
    <div class="d-flex justify-content-between align-items-center mb-3">
        <div>
            <h3 class="mb-0">
                <i class="bi bi-link-45deg me-2 text-primary"></i>
                Araç, Şoför ve Güzergah Eşleme Havuzu (CARİ Bazlı)
            </h3>
            <small class="text-muted">
                Özmal veya komisyonlu servislerin otomatik puantaj üretmeden önceki şablon tanımları
            </small>
        </div>
        <div class="d-flex gap-2">
            <button class="btn btn-outline-primary" @onclick="PuantajModalAcAsync" disabled>
                <i class="bi bi-calendar-plus me-1"></i> Puantaj Oluştur
            </button>
            <button class="btn btn-primary" @onclick="YeniEkleModalAc" disabled>
                <i class="bi bi-plus-lg me-1"></i> Yeni Eşleşme Ekle
            </button>
        </div>
    </div>

    <!-- CARİ SEÇİCİ (Component) -->
    @if (carilerYuklendi)
    {
        <CariSelector Cariler="@tumCariler"
                      SelectedCariId="@secilmisCariId"
                      OnCariSelected="@SecilmisCariDegisAsync" />
    }

    <!-- LOADING SPINNER -->
    @if (isLoading)
    {
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status"></div>
            <p class="mt-2 text-muted">Veriler yükleniyor...</p>
        </div>
    }
    else if (secilmisCari == null)
    {
        <div class="alert alert-info">
            <i class="bi bi-info-circle me-2"></i>
            Lütfen yukarıdan bir Cari seçiniz.
        </div>
    }
    else
    {
        <!-- SEÇİLİ CARİ PANEL -->
        <div class="card shadow-sm mb-3">
            <div class="card-header bg-light">
                <div class="d-flex justify-content-between align-items-center">
                    <div>
                        <h5 class="mb-0">
                            <strong>@secilmisCari.CariUnvan</strong>
                            <span class="badge bg-info ms-2">@secilmisCari.Kurumlar.Count Kurum</span>
                        </h5>
                        <small class="text-muted">Kod: @secilmisCari.CariKodu</small>
                    </div>
                    <button class="btn btn-sm btn-outline-secondary" @onclick="SecilmisCariTemizleAsync">
                        <i class="bi bi-x-circle me-1"></i> Başka Cari Seç
                    </button>
                </div>
            </div>

            <div class="card-body">
                <!-- KURUM AKKORDIYONU (Component) -->
                @foreach (var kurum inSecilmisCari.Kurumlar)
                {
                    <KurumAkkordiyonu 
                        Kurum="@kurum"
                        CariId="@secilmisCari.CariId"
                        IsExpanded="@(expandedKurumId == kurum.KurumId)"
                        OnToggle="@(() => ToggleKurumAsync(kurum.KurumId))"
                        OnSave="@RefreshSecilmisCariAsync"
                        OnDelete="@RefreshSecilmisCariAsync" />
                }

                <!-- CARİ TOPLAM PANELI -->
                <div class="alert alert-success mt-3">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <strong>
                                @secilmisCari.CariUnvan - Aylık Toplam (Tüm Kurumlar)
                            </strong>
                            <br />
                            <small>@DateTime.Now.Year-@DateTime.Now.Month:00</small>
                        </div>
                        <div class="text-end">
                            <div style="font-size: 1.5rem; font-weight: bold; color: green;">
                                @secilmisCari.Kurumlar
                                    .Sum(k => k.Eslestirmeler
                                        .Sum(e => e.OylukHucreler
                                            .Sum(h => h.Deger ?? 0)))
                                Sefer
                            </div>
                            <small class="text-muted">
                                ≈ 
                                @(secilmisCari.Kurumlar
                                    .Sum(k => k.Eslestirmeler
                                        .Sum(e => e.OylukHucreler
                                            .Sum(h => h.Deger ?? 0)))
                                        * 750).ToString("N0")
                                TL
                            </small>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    }

    <!-- DİĞER CARİLER (Kompakt Özet) -->
    @if (digerCariler != null && digerCariler.Any())
    {
        <div class="card shadow-sm">
            <div class="card-header bg-light">
                <h5 class="mb-0">Diğer Cariler (Hızlı Bakış)</h5>
            </div>
            <div class="card-body">
                <div class="table-responsive">
                    <table class="table table-sm table-hover">
                        <thead>
                            <tr>
                                <th>Cari Adı</th>
                                <th>Kurum Sayısı</th>
                                <th>Aylık Sefer</th>
                                <th>Tahmini Gelir</th>
                                <th></th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var c in digerCariler.Take(10))
                            {
                                var toplamSefer = c.Kurumlar
                                    .Sum(k => k.Eslestirmeler
                                        .Sum(e => e.OylukHucreler
                                            .Sum(h => h.Deger ?? 0)));

                                <tr>
                                    <td><strong>@c.CariUnvan</strong></td>
                                    <td><span class="badge bg-info">@c.Kurumlar.Count</span></td>
                                    <td>@toplamSefer</td>
                                    <td class="text-success fw-bold">@(toplamSefer * 750).ToString("N0") TL</td>
                                    <td>
                                        <button class="btn btn-sm btn-outline-primary"
                                                @onclick="() => SecilmisCariDegisAsync(c.CariId)">
                                            Aç <i class="bi bi-chevron-right"></i>
                                        </button>
                                    </td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    }
</div>

@code {
    private bool carilerYuklendi = false;
    private bool isLoading = false;
    private int? secilmisCariId = null;
    private int? expandedKurumId = null;
    private CariKurumHiyerarsiDto? secilmisCari;
    private List<CariKurumHiyerarsiDto> tumCariler = new();
    private List<CariKurumHiyerarsiDto> digerCariler = new();

    protected override async Task OnInitializedAsync()
    {
        await YukleCarilerAsync();
    }

    private async Task YukleCarilerAsync()
    {
        try
        {
            isLoading = true;

            // TODO: Servis'ten tüm Cariler + Kurum'lar yükle
            // tumCariler = await FiloKomisyonService.GetCarilerKurumlarAsync();

            carilerYuklendi = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Cariler yüklenirken hata");
            ToastService.ShowError("Cariler yüklenemedi");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SecilmisCariDegisAsync(int cariId)
    {
        try
        {
            isLoading = true;
            secilmisCariId = cariId;

            // Seçili Cari'nin hiyerarşisini yükle
            secilmisCari = await FiloKomisyonService
                .GetCariKurumHiyerarsiAsync(cariId, includeEslestirmeler: true);

            // Diğer cariler listesini güncelleş
            digerCariler = tumCariler
                .Where(c => c.CariId != cariId)
                .ToList();

            expandedKurumId = null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Cari yüklenirken hata");
            ToastService.ShowError("Cari yüklenemedi");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ToggleKurumAsync(int kurumId)
    {
        expandedKurumId = expandedKurumId == kurumId ? null : kurumId;
    }

    private async Task RefreshSecilmisCariAsync()
    {
        if (secilmisCariId.HasValue)
        {
            await SecilmisCariDegisAsync(secilmisCariId.Value);
        }
    }

    private async Task SecilmisCariTemizleAsync()
    {
        secilmisCari = null;
        secilmisCariId = null;
        digerCariler = tumCariler;
    }

    private async Task PuantajModalAcAsync()
    {
        // TODO: Puantaj Modal'ı aç
    }

    private void YeniEkleModalAc()
    {
        // TODO: Yeni Eşleştirme Modal'ı aç
    }
}
```

---

## 3. Bileşen Detayları

### 3.1 CariSelector.razor

**Amaç**: Cari seçimi için autocomplete dropdown

```razor
@using MKFiloServis.Web.Models

<div class="mb-3">
    <label class="form-label"><strong>Cari Seçiniz</strong></label>

    <div class="input-group">
        <span class="input-group-text">
            <i class="bi bi-search"></i>
        </span>
        <input type="text"
               class="form-control"
               placeholder="Cari adı veya kod ara..."
               @oninput="AramayaTextAsync"
               @onfocus="OnFocusAsync"
               value="@aramaMetni" />
    </div>

    @if (gorunenCariler.Any())
    {
        <div class="list-group mt-2" style="max-height: 300px; overflow-y: auto;">
            @foreach (var c in gorunenCariler.Take(10))
            {
                <button type="button"
                        class="list-group-item list-group-item-action"
                        @onclick="() => SelectCariAsync(c.CariId)">
                    <div class="d-flex justify-content-between">
                        <div>
                            <strong>@c.CariUnvan</strong>
                            <br />
                            <small class="text-muted">@c.CariKodu</small>
                        </div>
                        <div class="text-end">
                            <span class="badge bg-info">@c.Kurumlar.Count Kurum</span>
                        </div>
                    </div>
                </button>
            }
        </div>
    }
    else if (!string.IsNullOrWhiteSpace(aramaMetni))
    {
        <div class="alert alert-warning mt-2 mb-0">
            Sonuç bulunamadı
        </div>
    }
</div>

@code {
    [Parameter]
    public List<CariKurumHiyerarsiDto> Cariler { get; set; } = new();

    [Parameter]
    public int? SelectedCariId { get; set; }

    [Parameter]
    public EventCallback<int> OnCariSelected { get; set; }

    private string aramaMetni = "";
    private List<CariKurumHiyerarsiDto> gorunenCariler = new();

    private async Task AramayaTextAsync(ChangeEventArgs e)
    {
        aramaMetni = (string?)e.Value ?? "";
        FilterCariler();
    }

    private async Task OnFocusAsync()
    {
        if (string.IsNullOrWhiteSpace(aramaMetni))
        {
            gorunenCariler = Cariler;
        }
    }

    private void FilterCariler()
    {
        if (string.IsNullOrWhiteSpace(aramaMetni))
        {
            gorunenCariler = Cariler;
        }
        else
        {
            var filter = aramaMetni.ToLower();
            gorunenCariler = Cariler
                .Where(c => c.CariUnvan.ToLower().Contains(filter) ||
                            c.CariKodu.ToLower().Contains(filter))
                .ToList();
        }
    }

    private async Task SelectCariAsync(int cariId)
    {
        await OnCariSelected.InvokeAsync(cariId);
        aramaMetni = "";
        gorunenCariler.Clear();
    }
}
```

### 3.2 KurumAkkordiyonu.razor

**Amaç**: Kurum altında collapsible eşleştirmeler ve puantaj grid'i

```razor
@using MKFiloServis.Web.Models

<div class="card mb-3">
    <div class="card-header" style="cursor: pointer; user-select: none;">
        <div class="d-flex justify-content-between align-items-center"
             @onclick="@OnHeaderClickAsync">
            <div>
                <strong>@Kurum.KurumAdi</strong>
                <span class="badge bg-info ms-2">@Kurum.Eslestirmeler.Count Eşleştirme</span>
            </div>
            <div>
                @if (IsExpanded)
                {
                    <i class="bi bi-chevron-up text-primary" style="font-size: 1.2rem;"></i>
                }
                else
                {
                    <i class="bi bi-chevron-down text-primary" style="font-size: 1.2rem;"></i>
                }
            </div>
        </div>
    </div>

    @if (IsExpanded)
    {
        <div class="card-body">
            <!-- Eşleştirmeler Tablosu -->
            <div class="table-responsive mb-3">
                <table class="table table-sm table-hover">
                    <thead class="table-light">
                        <tr>
                            <th>Plaka</th>
                            <th>Şoför</th>
                            <th>Güzergah</th>
                            <th>Kuruma Ücret</th>
                            <th>Taşerona Ücret</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var e in Kurum.Eslestirmeler)
                        {
                            <tr>
                                <td><strong>@e.Plaka</strong></td>
                                <td>@e.SoforAdi</td>
                                <td>@e.GuzerigahAdi</td>
                                <td class="text-success fw-bold">@e.KurumaUcret.ToString("N2") TL</td>
                                <td class="text-danger fw-bold">@e.HedefeUcret.ToString("N2") TL</td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>

            <!-- PuantajGridByKurum Bileşeni -->
            <PuantajGridByKurum 
                KurumId="@Kurum.KurumId"
                Yil="@DateTime.Now.Year"
                Ay="@DateTime.Now.Month"
                OnSave="@OnSaveAsync"
                OnDelete="@OnDeleteAsync" />

            <!-- Kurum Toplam -->
            <div class="alert alert-info mt-3">
                <strong>Kurum Toplam (Bu Ay):</strong>
                @{
                    var toplamSefer = Kurum.Eslestirmeler
                        .Sum(e => e.OylukHucreler
                            .Sum(h => h.Deger ?? 0));
                    var toplamTL = toplamSefer * 750;  // Örnek: sefer başına 750 TL
                }
                @toplamSefer Sefer → @toplamTL.ToString("N0") TL
            </div>
        </div>
    }
</div>

@code {
    [Parameter]
    public KurumDetayDto Kurum { get; set; }

    [Parameter]
    public int CariId { get; set; }

    [Parameter]
    public bool IsExpanded { get; set; }

    [Parameter]
    public EventCallback OnToggle { get; set; }

    [Parameter]
    public EventCallback OnSave { get; set; }

    [Parameter]
    public EventCallback OnDelete { get; set; }

    private async Task OnHeaderClickAsync()
    {
        await OnToggle.InvokeAsync();
    }

    private async Task OnSaveAsync()
    {
        await OnSave.InvokeAsync();
    }

    private async Task OnDeleteAsync()
    {
        await OnDelete.InvokeAsync();
    }
}
```

### 3.3 PuantajGridByKurum.razor

**Amaç**: Kurum altında eşleştirmeler bazında gün-grid'i (editlenebilir)

*(Mevcut `EslestirmeTanimlari.razor` içindeki alt grid ile benzer, componentsized hali)*

```razor
@using MKFiloServis.Web.Services.Interfaces
@using MKFiloServis.Web.Models

@inject IFiloKomisyonService FiloKomisyonService
@inject IToastService ToastService
@inject ILogger<PuantajGridByKurum> Logger

<div class="table-responsive">
    <table class="table table-sm table-bordered align-middle">
        <thead class="table-light">
            <tr>
                <th>Plaka</th>
                <th>Şoför</th>
                @{
                    var gunSayisi = DateTime.DaysInMonth(Yil, Ay);
                    for (int gün = 1; gün <= gunSayisi; gün++)
                    {
                        <th class="text-center" style="width: 50px;">@gün</th>
                    }
                }
                <th class="text-center fw-bold">TOPLAM</th>
                <th class="text-center">İşlem</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var row in gridData)
            {
                <tr>
                    <td class="fw-bold">@row.Plaka</td>
                    <td>@row.SoforAdi</td>

                    @{
                        for (int gün = 1; gün <= gunSayisi; gün++)
                        {
                            var hucre = row.OylukHucreler.FirstOrDefault(h => h.GunNo == gün);

                            <td class="text-center" style="padding: 2px;">
                                @if (editingCell != null && 
                                     editingCell.AracId == row.SiradakiAracId && 
                                     editingCell.GunNo == gün)
                                {
                                    <input type="number" 
                                           min="0" 
                                           max="10"
                                           @bind="editingCell.Deger"
                                           class="form-control form-control-sm"
                                           @onkeydown="async (e) => await HandleKeyDownAsync(e, row.SiradakiAracId, gün)" />
                                }
                                else
                                {
                                    <input type="number"
                                           disabled
                                           value="@hucre?.Deger"
                                           class="form-control form-control-sm"
                                           @onclick="() => StartEditingAsync(row.SiradakiAracId, gün)" />
                                }
                            </td>
                        }
                    }

                    <td class="text-center fw-bold">
                        @row.OylukHucreler.Sum(h => h.Deger ?? 0)
                    </td>

                    <td class="text-center">
                        <button class="btn btn-sm btn-outline-success" @onclick="() => SaveRowAsync(row)">
                            <i class="bi bi-save"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" @onclick="() => DeleteRowAsync(row)">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>

@code {
    [Parameter]
    public int KurumId { get; set; }
    [Parameter]
    public int Yil { get; set; }
    [Parameter]
    public int Ay { get; set; }
    [Parameter]
    public EventCallback OnSave { get; set; }
    [Parameter]
    public EventCallback OnDelete { get; set; }

    private List<AylikPuantajGridDto> gridData = new();
    private EditingCellModel? editingCell = null;

    protected override async Task OnInitializedAsync()
    {
        await YukleGridAsync();
    }

    private async Task YukleGridAsync()
    {
        try
        {
            gridData = await FiloKomisyonService
                .GetKurumAylikPuantajGridAsync(KurumId, Yil, Ay);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Grid yüklenirken hata");
            ToastService.ShowError("Grid yüklenemedi");
        }
    }

    private async Task StartEditingAsync(int aracId, int gunNo)
    {
        var row = gridData.FirstOrDefault(r => r.SiradakiAracId == aracId);
        var hucre = row?.OylukHucreler.FirstOrDefault(h => h.GunNo == gunNo);

        if (hucre != null)
        {
            editingCell = new EditingCellModel
            {
                AracId = aracId,
                GunNo = gunNo,
                Deger = hucre.Deger ?? 0,
                PuantajId = hucre.PuantajId
            };
        }
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e, int aracId, int gunNo)
    {
        if (e.Key == "Enter")
        {
            await SaveCellAsync();
        }
        else if (e.Key == "Escape")
        {
            editingCell = null;
        }
    }

    private async Task SaveCellAsync()
    {
        if (editingCell != null)
        {
            // TODO: DB'ye kaydet
            editingCell = null;
            await YukleGridAsync();
        }
    }

    private async Task SaveRowAsync(AylikPuantajGridDto row)
    {
        try
        {
            var puantajIds = row.OylukHucreler
                .Where(h => h.PuantajId > 0)
                .Select(h => h.PuantajId)
                .ToList();

            // TODO: FiloKomisyonService.UpdateGunlukPuantajlarAsync

            ToastService.ShowSuccess("Puantajlar kaydedildi");
            await OnSave.InvokeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Kaydet başarısız");
            ToastService.ShowError($"Hata: {ex.Message}");
        }
    }

    private async Task DeleteRowAsync(AylikPuantajGridDto row)
    {
        try
        {
            var puantajIds = row.OylukHucreler
                .Where(h => h.PuantajId > 0)
                .Select(h => h.PuantajId)
                .ToList();

            // TODO: FiloKomisyonService.DeleteGunlukPuantajlarAsync

            ToastService.ShowSuccess("Puantajlar silindi");
            await OnDelete.InvokeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Sil başarısız");
            ToastService.ShowError($"Hata: {ex.Message}");
        }
    }

    private class EditingCellModel
    {
        public int AracId { get; set; }
        public int GunNo { get; set; }
        public int Deger { get; set; }
        public int PuantajId { get; set; }
    }
}
```

---

## 4. CariPuantajDashboard.razor (Yeni Sayfa)

**Amaç**: Tüm Cariler için kümülâtif puantaj ve raporlama dashboard'u

```razor
@page "/filo/cari-puantaj-dashboard"
@using MKFiloServis.Web.Services.Interfaces
@using MKFiloServis.Web.Models

@inject IFiloKomisyonService FiloKomisyonService
@inject ILogger<CariPuantajDashboard> Logger

@rendermode InteractiveServer
@attribute [Authorize]

<PageTitle>Cari Puantaj Dashboard</PageTitle>

<div class="container-fluid">
    <h3 class="mb-3">
        <i class="bi bi-graph-up me-2"></i>
        Cari Puantaj Dashboard
    </h3>

    <!-- Tarih Filtresi -->
    <div class="card mb-3">
        <div class="card-body">
            <div class="row">
                <div class="col-md-4">
                    <label>Başlama Tarihi</label>
                    <input type="date" 
                           @bind="baslamaTarihi" 
                           class="form-control"
                           @onchange="RefreshAsync" />
                </div>
                <div class="col-md-4">
                    <label>Bitiş Tarihi</label>
                    <input type="date" 
                           @bind="bitisTarihi" 
                           class="form-control"
                           @onchange="RefreshAsync" />
                </div>
                <div class="col-md-4">
                    <label>&nbsp;</label>
                    <button class="btn btn-primary w-100" @onclick="RefreshAsync">
                        <i class="bi bi-arrow-clockwise me-1"></i> Yenile
                    </button>
                </div>
            </div>
        </div>
    </div>

    <!-- Overall Özet Kartları -->
    @if (ozetler != null && ozetler.Any())
    {
        var toplamSefer = ozetler.Sum(o => o.ToplamSefer);
        var toplamGelir = ozetler.Sum(o => o.ToplamGelir);
        var toplamMaliyet = ozetler.Sum(o => o.ToplamMaliyet);
        var toplamKar = toplamGelir - toplamMaliyet;

        <div class="row mb-3">
            <div class="col-md-3">
                <div class="card text-center">
                    <div class="card-body">
                        <h6 class="card-title text-muted">Toplam Sefer</h6>
                        <h2 class="text-primary">@toplamSefer</h2>
                        <small>@ozetler.Count Cari</small>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-center">
                    <div class="card-body">
                        <h6 class="card-title text-muted">Toplam Gelir</h6>
                        <h2 class="text-success">@toplamGelir.ToString("N0") TL</h2>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-center">
                    <div class="card-body">
                        <h6 class="card-title text-muted">Toplam Maliyet</h6>
                        <h2 class="text-danger">@toplamMaliyet.ToString("N0") TL</h2>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-center">
                    <div class="card-body">
                        <h6 class="card-title text-muted">Toplam Kar</h6>
                        <h2 class="text-info">@toplamKar.ToString("N0") TL</h2>
                        <small>@((toplamKar / toplamGelir * 100).ToString("N1"))% Kar Marjı</small>
                    </div>
                </div>
            </div>
        </div>
    }

    <!-- Detaylı Cariler Tablosu -->
    <div class="card">
        <div class="card-header">
            <h6 class="mb-0">Cari Bazında Detaylı Rapor</h6>
        </div>
        <div class="card-body">
            @if (ozetler == null)
            {
                <div class="spinner-border text-primary" role="status"></div>
            }
            else if (!ozetler.Any())
            {
                <div class="alert alert-info">Veriler bulunamadı</div>
            }
            else
            {
                <div class="table-responsive">
                    <table class="table table-hover table-sm">
                        <thead>
                            <tr>
                                <th>Cari Adı</th>
                                <th class="text-center">Kurum</th>
                                <th class="text-center">Sefer</th>
                                <th class="text-end">Gelir</th>
                                <th class="text-end">Maliyet</th>
                                <th class="text-end">Kar</th>
                                <th class="text-center">Kar %</th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var o in ozetler.OrderByDescending(x => x.ToplamGelir))
                            {
                                var kar = o.ToplamGelir - o.ToplamMaliyet;
                                var karMarji = (kar / o.ToplamGelir * 100);

                                <tr>
                                    <td><strong>@o.CariUnvan</strong><br /><small class="text-muted">@o.CariKodu</small></td>
                                    <td class="text-center"><span class="badge bg-info">@o.KurumSayisi</span></td>
                                    <td class="text-center"><span class="badge bg-warning">@o.ToplamSefer</span></td>
                                    <td class="text-end text-success fw-bold">@o.ToplamGelir.ToString("N0") TL</td>
                                    <td class="text-end text-danger fw-bold">@o.ToplamMaliyet.ToString("N0") TL</td>
                                    <td class="text-end fw-bold" style="color: @(kar > 0 ? "green" : "red");">
                                        @kar.ToString("N0") TL
                                    </td>
                                    <td class="text-center">@karMarji.ToString("N1")%</td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            }
        </div>
    </div>
</div>

@code {
    private DateTime baslamaTarihi = DateTime.Now.AddMonths(-1).AddDays(1 - DateTime.Now.Day);
    private DateTime bitisTarihi = DateTime.Now;
    private List<CariPuantajOzetDto>? ozetler;

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            ozetler = await FiloKomisyonService
                .GetCarilarPuantajOzetiAsync(baslamaTarihi, bitisTarihi);
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "Dashboard verisi yüklenirken hata");
        }
    }
}
```

---

## 5. UX/UI Best Practices

### 5.1 Erişilebilirlik (A11y)
- ✅ Buttons: `aria-label` ekle
- ✅ Icons: `role="img"` ve `aria-hidden` hareket
- ✅ Form: `<label>` assosiasyonları
- ✅ Keyboard Nav: Tab order, Enter/Escape handling

### 5.2 Performans
- ✅ Component lazy loading (opsiyonel)
- ✅ Virtualization (çok satır varsa)
- ✅ Caching (servis tarafında)
- ✅ Debounce arama (250ms)

### 5.3 Mobile Responsiveness
- ✅ Grid tablosu: Horizontal scroll (mobilde)
- ✅ Dropdown: Full-width (mobilde)
- ✅ Buttons: Larger touch targets (48px min)

---

## 6. Bileşen Dosya Listesi (Checklist)

- [ ] `Components/CommonComponents/CariSelector.razor`
- [ ] `Components/CommonComponents/KurumAkkordiyonu.razor`
- [ ] `Components/CommonComponents/PuantajGridByKurum.razor`
- [ ] `Components/CommonComponents/CariToplam.razor` (opsiyonel)
- [ ] `Components/Pages/Filo/EslestirmeTanimlari.razor` (revize)
- [ ] `Components/Pages/Dashboards/CariPuantajDashboard.razor` (yeni)
- [ ] `Models/CariKurumPuantajDtos.cs` (DTO'lar)
- [ ] `CSS`: `Components/Pages/Filo/EslestirmeTanimlari.razor.css` (opsiyonel)

---

**Versiyon**: 1.0 (UI Mockup & Component Architecture)  
**Son Güncelleme**: 2025-01-23
