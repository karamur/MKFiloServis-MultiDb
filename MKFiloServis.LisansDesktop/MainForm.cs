using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

/// <summary>
/// MKFiloServis Lisans Aktivasyon Key Uretici + Satis/Yenileme Takip Sistemi.
/// Web'deki LicenseService.ActivateFromKeyAsync() ile uyumlu lisans key üretir.
/// </summary>
public class MainForm : Form
{
    private const string DefaultAllowedVersion = "1.0.99";
    private readonly Label lblMaximumVersion = new() { Text = "En Fazla Sürüm", AutoSize = true };
    private readonly TextBox txtMaximumVersion = new() { Text = DefaultAllowedVersion, MaxLength = MKFiloServis.Shared.Licensing.LicenseVersionPolicy.MaximumLength, PlaceholderText = "Örn. 1.0.99" };
    private readonly CheckBox chkUnlimitedVersion = new() { Text = "Sınırsız sürüm hakkı", AutoSize = true };
    private readonly CheckedListBox moduleSelection = new() { CheckOnClick = true, DisplayMember = "Value", IntegralHeight = false };
    private string SelectedModules() => MKFiloServis.Shared.Licensing.LicenseModules.Canonical(
        moduleSelection.CheckedItems.Cast<KeyValuePair<string, string>>().Select(x => x.Key));
    private readonly string _dbPath;
    private readonly Button btnSigningKeyStatus = new() { Text = "Anahtar Durumu", Width = 160, Height = 30 };
    private readonly Button btnSigningKeyExport = new() { Text = "Şifreli Yedek Oluştur", Width = 185, Height = 30 };
    private readonly Button btnSigningKeyVerify = new() { Text = "Yedeği Doğrula", Width = 160, Height = 30 };
    private readonly Button btnSigningKeyImport = new() { Text = "Anahtar / Yedek İçe Aktar", Width = 220, Height = 30 };

    private Panel pnlHeader = new() { Height = 88, BackColor = Color.FromArgb(19, 33, 68) };
    private Label lblTitle = new() { Text = "MKFiloServis Lisans Yonetim Merkezi", AutoSize = true, Font = new Font("Segoe UI Semibold", 19, FontStyle.Bold), ForeColor = Color.White };

    private GroupBox grpLicenseEditor = new() { Text = "Lisans Olusturma" };
    private GroupBox grpOutput = new() { Text = "Anahtar Ciktisi" };
    private GroupBox grpPackaging = new() { Text = "Kurulum / Guncelleme Paketleri" };
    private GroupBox grpHistory = new() { Text = "Lisans Gecmisi" };
    private Panel pnlQuickInfo = new() { BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(245, 248, 255) };
    private Label lblQuickInfoTitle = new() { Text = "Hizli Bilgi", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(19, 33, 68) };
    private Label lblQuickInfo = new() { Text = "Firma kodu, machine ID ve iletisim telefonu dogruysa uretilen anahtar web uygulamasinda dogrudan aktive edilir.", AutoSize = false, ForeColor = Color.FromArgb(58, 68, 88) };
    private Label lblFirma = new() { Text = "Firma Kodu", AutoSize = true };
    private TextBox txtFirma = new() { PlaceholderText = "orn: USTUN" };
    private Label lblMachine = new() { Text = "Machine ID", AutoSize = true };
    private TextBox txtMachine = new() { PlaceholderText = "Web'den kopyalayip yapistirin", Multiline = true, Height = 52, ScrollBars = ScrollBars.Vertical };

    private Label lblDays = new() { Text = "Lisans Suresi (Gun)", AutoSize = true };
    private NumericUpDown txtDays = new() { Minimum = 1, Maximum = 3650, Value = 365, Width = 100 };
    private Label lblExpire = new() { Text = "Bitis Tarihi", AutoSize = true };
    private DateTimePicker dtExpire = new() { Format = DateTimePickerFormat.Short, Width = 150 };

    private Label lblSaleDate = new() { Text = "Satis Tarihi", AutoSize = true };
    private DateTimePicker dtSaleDate = new() { Format = DateTimePickerFormat.Short, Width = 150 };
    private Label lblAmount = new() { Text = "Satis Tutari", AutoSize = true };
    private NumericUpDown numAmount = new() { DecimalPlaces = 2, Minimum = 0.01m, Maximum = 1_000_000m, Value = 1m, Width = 150, ThousandsSeparator = true };

    private Label lblPhone = new() { Text = "Iletisim Telefonu", AutoSize = true };
    private TextBox txtPhone = new() { PlaceholderText = "orn: 0555xxxxxxx" };

    private Button btnUret = new() { Text = "Yeni Satis Lisansi Olustur", Height = 42 };
    private Button btnUpdateSale = new() { Text = "Secili Kaydi Guncelle", Height = 36 };
    private Button btnDeleteSale = new() { Text = "Secili Kaydi Sil", Height = 36 };
    private Button btnRenewRemaining = new() { Text = "Secili Kayittan Yenileme Uret", Height = 36 };

    private Label lblKey = new() { Text = "Uretilen Lisans Anahtari", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
    private TextBox txtKey = new()
    {
        ReadOnly = true,
        Multiline = true,
        Height = 94,
        BackColor = Color.FromArgb(255, 252, 227),
        Font = new Font("Consolas", 9),
        Text = "Henuz lisans uretilmedi."
    };
    private Button btnCopy = new() { Text = "Anahtari Panoya Kopyala", Enabled = false, Height = 35 };

    private TextBox txtSearch = new() { PlaceholderText = "Firma, Makine veya Telefon ara..." };
    private ComboBox cmbOperationFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private Button btnExportReport = new() { Text = "CSV Disa Aktar", Width = 150, Height = 32 };

    private DataGridView grid = new()
    {
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        ReadOnly = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        RowHeadersVisible = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.None
    };

    private readonly ContextMenuStrip gridContextMenu = new();

    private Label lblRenewalHistory = new() { Text = "Secili Kaydin Yenileme Gecmisi", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
    private ListBox lstRenewals = new() { Height = 96 };

    private Label lblUpdateZip = new() { Text = "Kurulum / Guncelleme Paketleme", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
    private TextBox txtUpdateSourceFolder = new() { PlaceholderText = "Publish klasorunu secin", ReadOnly = true };
    private Button btnSelectUpdateSource = new() { Text = "Kaynak Klasor", Height = 32 };
    private TextBox txtUpdateVersion = new() { PlaceholderText = "Versiyon (orn: 1.0.25)" };
    private TextBox txtUpdateOutputFolder = new() { PlaceholderText = "Cikis klasoru", ReadOnly = true };
    private Button btnSelectUpdateOutput = new() { Text = "Cikis Klasor", Height = 32 };
    private Button btnCreateUpdateZip = new() { Text = "Guncelleme Paketi ZIP Olustur", Height = 36 };
    private Button btnPrepareCustomerSetup = new() { Text = "Musteri Kurulum Paketini Hazirla", Height = 36 };

    private bool _syncing;
    private int? _selectedSaleId;

    public MainForm()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbDir = Path.Combine(appData, "MKFiloServis");
        Directory.CreateDirectory(dbDir);
        _dbPath = Path.Combine(dbDir, "licenses.db");

        Text = "MKFiloServis Lisans Yonetim Merkezi";
        Width = 1420;
        Height = 920;
        MinimumSize = new Size(1180, 720);
        BackColor = Color.FromArgb(236, 240, 248);
        Font = new Font("Segoe UI", 9f);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;

        InitializeLayout();
        ApplyProfessionalTheme();

        btnSigningKeyStatus.Click += (_, _) => ShowSigningKeyStatus();
        btnSigningKeyExport.Click += (_, _) => ExportSigningKeyBackup();
        btnSigningKeyVerify.Click += (_, _) => VerifySigningKeyBackup();
        btnSigningKeyImport.Click += (_, _) => ImportSigningKeyBackup();
        chkUnlimitedVersion.CheckedChanged += (_, _) => txtMaximumVersion.Enabled = !chkUnlimitedVersion.Checked;

        btnUret.Click += (s, e) => UretSatisLisansi();
        btnUpdateSale.Click += (s, e) => SeciliSatisiGuncelle();
        btnDeleteSale.Click += (s, e) => SeciliSatisiSil();
        btnRenewRemaining.Click += (s, e) => YenileKalanGunle();
        btnCopy.Click += (s, e) => CopyKey();
        btnExportReport.Click += (s, e) => SatisDokumunuDisaAktar();
        btnSelectUpdateSource.Click += (s, e) => SelectUpdateSourceFolder();
        btnSelectUpdateOutput.Click += (s, e) => SelectUpdateOutputFolder();
        btnCreateUpdateZip.Click += (s, e) => CreateUpdateZipPackage();
        btnPrepareCustomerSetup.Click += (s, e) => PrepareCustomerSetupPackage();
        txtSearch.TextChanged += (s, e) => LoadData();
        cmbOperationFilter.SelectedIndexChanged += (s, e) => LoadData();
        grid.CellFormatting += Grid_CellFormatting;
        grid.KeyDown += Grid_KeyDown;
        grid.MouseDown += Grid_MouseDown;
        KeyPreview = true;
        KeyDown += MainForm_KeyDown;
        ConfigureGridContextMenu();
        grid.SelectionChanged += (s, e) =>
        {
            LoadSelectedSaleToForm();
            LoadRenewalHistoryForSelected();
            UpdateActionButtons();
        };

        InitDatabase();
        UpdateHistorySummary();
        UpdateActionButtons();
        LoadData();
    }

    private void InitializeLayout()
    {
        SuspendLayout();
        pnlHeader.Height = 76;
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Padding = new Padding(18, 10, 18, 10);
        lblTitle.Location = new Point(18, 8);
        lblTitle.Text = "MKFiloServis Lisans Yönetimi";
        var subtitle = new Label
        {
            Text = "Lisans oluşturun, geçmişi yönetin ve imza anahtarını güvenle yedekleyin.",
            AutoSize = true, ForeColor = Color.FromArgb(208, 220, 255),
            Location = new Point(20, 44)
        };
        pnlHeader.Controls.AddRange([lblTitle, subtitle]);

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(20, 9) };
        var licensesPage = new TabPage("Lisanslar") { Padding = new Padding(12), BackColor = BackColor };
        var keysPage = new TabPage("Anahtar ve Yedek") { Padding = new Padding(20), BackColor = Color.White, AutoScroll = true };
        var packagingPage = new TabPage("Paketleme") { Padding = new Padding(20), BackColor = Color.White, AutoScroll = true };
        tabs.TabPages.AddRange([licensesPage, keysPage, packagingPage]);
        Controls.Add(tabs);
        Controls.Add(pnlHeader);

        var licenseLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            Margin = Padding.Empty
        };
        licenseLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 496));
        licenseLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        licenseLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        licensesPage.Controls.Add(licenseLayout);
        var editorScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = new Padding(0, 0, 12, 0) };
        licenseLayout.Controls.Add(editorScroll, 0, 0);
        grpLicenseEditor.Text = "Lisans Bilgileri";
        grpLicenseEditor.SetBounds(0, 0, 480, 778);
        grpLicenseEditor.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        grpOutput.Text = "İmzalı Lisans";
        grpOutput.SetBounds(0, 788, 480, 205);
        grpOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        editorScroll.Controls.AddRange([grpLicenseEditor, grpOutput]);
        editorScroll.AutoScrollMinSize = new Size(480, 1005);

        pnlQuickInfo.SetBounds(20, 24, 438, 62);
        lblQuickInfoTitle.Text = "Satış Özeti";
        lblQuickInfoTitle.Location = new Point(14, 10);
        lblQuickInfo.SetBounds(14, 28, 408, 24);
        pnlQuickInfo.Controls.AddRange([lblQuickInfoTitle, lblQuickInfo]);
        lblFirma.Text = "Firma Kodu";
        lblFirma.Location = new Point(20, 100);
        txtFirma.SetBounds(20, 122, 205, 26);
        lblPhone.Text = "İletişim Telefonu";
        lblPhone.Location = new Point(246, 100);
        txtPhone.SetBounds(246, 122, 212, 26);
        lblMachine.Text = "Makine Kodu";
        lblMachine.Location = new Point(20, 158);
        txtMachine.SetBounds(20, 180, 438, 52);
        lblDays.Text = "Süre (Gün)";
        lblDays.Location = new Point(20, 246);
        txtDays.Location = new Point(20, 268);
        lblExpire.Text = "Bitiş Tarihi";
        lblExpire.Location = new Point(246, 246);
        dtExpire.Location = new Point(246, 268);
        lblSaleDate.Text = "Satış Tarihi";
        lblSaleDate.Location = new Point(20, 308);
        dtSaleDate.Location = new Point(20, 330);
        lblAmount.Text = "Satış Tutarı (TL)";
        lblAmount.Location = new Point(246, 308);
        numAmount.Location = new Point(246, 330);
        lblMaximumVersion.Location = new Point(20, 368);
        txtMaximumVersion.SetBounds(20, 390, 205, 26);
        chkUnlimitedVersion.Location = new Point(246, 392);
        var modulesLabel = new Label { Text = "Lisanslı Modüller (yalnızca seçilenler açılır)", AutoSize = true, Location = new Point(20, 424) };
        moduleSelection.SetBounds(20, 448, 438, 170);
        foreach (var item in MKFiloServis.Shared.Licensing.LicenseModules.All)
            moduleSelection.Items.Add(item, false);
        grpLicenseEditor.Controls.AddRange([modulesLabel, moduleSelection]);
        btnUret.Text = "İmzalı Lisans Oluştur";
        btnUret.SetBounds(20, 638, 438, 42);
        btnRenewRemaining.Text = "Seçili Lisansı Yenile";
        btnRenewRemaining.SetBounds(20, 688, 438, 36);
        btnUpdateSale.Text = "Kaydı Güncelle";
        btnUpdateSale.SetBounds(20, 732, 208, 32);
        btnDeleteSale.Text = "Kaydı Sil";
        btnDeleteSale.SetBounds(250, 732, 208, 32);
        grpLicenseEditor.Controls.AddRange([
            pnlQuickInfo, lblFirma, txtFirma, lblPhone, txtPhone, lblMachine, txtMachine,
            lblDays, txtDays, lblExpire, dtExpire, lblSaleDate, dtSaleDate,
            lblAmount, numAmount, btnUret, btnRenewRemaining, btnUpdateSale, btnDeleteSale,
            lblMaximumVersion, txtMaximumVersion, chkUnlimitedVersion
        ]);
        lblKey.Text = "Müşteriye iletilecek lisans";
        lblKey.Location = new Point(20, 26);
        txtKey.SetBounds(20, 50, 438, 94);
        txtKey.ScrollBars = ScrollBars.Vertical;
        txtKey.Text = "Henüz lisans oluşturulmadı.";
        btnCopy.Text = "Lisansı Kopyala";
        btnCopy.SetBounds(20, 154, 438, 35);
        grpOutput.Controls.AddRange([lblKey, txtKey, btnCopy]);

        grpHistory.Text = "Lisans Geçmişi";
        grpHistory.Dock = DockStyle.Fill;
        grpHistory.Margin = Padding.Empty;
        grpHistory.Padding = new Padding(14, 22, 14, 12);
        licenseLayout.Controls.Add(grpHistory, 1, 0);
        var historyLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        historyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        historyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        historyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        historyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        historyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = true };
        txtSearch.Width = 250;
        txtSearch.PlaceholderText = "Firma, makine veya telefon ara";
        cmbOperationFilter.Width = 150;
        cmbOperationFilter.Items.AddRange(["Sadece Satışlar", "Tüm İşlemler"]);
        cmbOperationFilter.SelectedIndex = 0;
        btnExportReport.Text = "CSV Dışa Aktar";
        btnExportReport.Size = new Size(130, 28);
        filters.Controls.AddRange([txtSearch, cmbOperationFilter, btnExportReport]);
        grid.Dock = DockStyle.Fill;
        grid.Margin = Padding.Empty;
        lblRenewalHistory.Text = "Seçili Lisansın İşlem Geçmişi";
        lblRenewalHistory.Dock = DockStyle.Fill;
        lblRenewalHistory.TextAlign = ContentAlignment.MiddleLeft;
        lstRenewals.Dock = DockStyle.Fill;
        lstRenewals.HorizontalScrollbar = true;
        historyLayout.Controls.Add(filters, 0, 0);
        historyLayout.Controls.Add(grid, 0, 1);
        historyLayout.Controls.Add(lblRenewalHistory, 0, 2);
        historyLayout.Controls.Add(lstRenewals, 0, 3);
        grpHistory.Controls.Add(historyLayout);

        var keyLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 8, 0, 20)
        };
        keyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        keyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var keyHeading = new Label
        {
            Text = "İmza Anahtarı ve Şifreli Yedek",
            AutoSize = true, Font = new Font("Segoe UI Semibold", 15, FontStyle.Bold), Margin = new Padding(0, 0, 0, 16)
        };
        keyLayout.Controls.Add(keyHeading, 0, 0);
        keyLayout.SetColumnSpan(keyHeading, 2);
        var keyHelp = new Label
        {
            Text = "Lisanslar programın şifreli deposundaki anahtarla imzalanır. Anahtar ve yedek işlemlerini bu ekrandan yönetin.",
            AutoSize = true, MaximumSize = new Size(780, 0), Margin = new Padding(0, 0, 0, 24)
        };
        keyLayout.Controls.Add(keyHelp, 0, 1);
        keyLayout.SetColumnSpan(keyHelp, 2);
        var keyButtons = new[] { btnSigningKeyStatus, btnSigningKeyExport, btnSigningKeyVerify, btnSigningKeyImport };
        var descriptions = new[]
        {
            "Programın imzalama anahtarını kontrol edin.",
            "Parola korumalı bir yedek oluşturun. Parolayı yedekten ayrı saklayın.",
            "Yedeğin açılabildiğini kontrol edin. Mevcut anahtar değiştirilmez.",
            "Yeni bilgisayarda yedeği geri yükleyin veya mevcut anahtarı içe alın."
        };
        for (var i = 0; i < keyButtons.Length; i++)
        {
            keyButtons[i].Size = new Size(230, 40);
            keyButtons[i].Margin = new Padding(0, 0, 20, 16);
            keyLayout.Controls.Add(keyButtons[i], 0, i + 2);
            keyLayout.Controls.Add(new Label
            {
                Text = descriptions[i], AutoSize = true, MaximumSize = new Size(650, 0),
                Margin = new Padding(0, 10, 0, 16)
            }, 1, i + 2);
        }
        keysPage.Controls.Add(keyLayout);

        grpPackaging.Text = "Kurulum ve Güncelleme";
        grpPackaging.SetBounds(20, 20, 480, 290);
        grpPackaging.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        lblUpdateZip.Text = "Müşteri paketi hazırlama";
        lblUpdateZip.Location = new Point(20, 26);
        txtUpdateSourceFolder.SetBounds(20, 52, 316, 26);
        btnSelectUpdateSource.Text = "Kaynak Klasör";
        btnSelectUpdateSource.SetBounds(344, 50, 114, 32);
        var versionLabel = new Label { Text = "Paket Sürümü", AutoSize = true, Location = new Point(20, 92) };
        txtUpdateVersion.SetBounds(20, 114, 438, 26);
        txtUpdateOutputFolder.SetBounds(20, 154, 316, 26);
        btnSelectUpdateOutput.Text = "Çıkış Klasörü";
        btnSelectUpdateOutput.SetBounds(344, 152, 114, 32);
        btnCreateUpdateZip.Text = "Güncelleme ZIP Oluştur";
        btnCreateUpdateZip.SetBounds(20, 200, 214, 36);
        btnPrepareCustomerSetup.Text = "Müşteri Kurulumu Hazırla";
        btnPrepareCustomerSetup.SetBounds(244, 200, 214, 36);
        grpPackaging.Controls.AddRange([
            lblUpdateZip, txtUpdateSourceFolder, btnSelectUpdateSource, versionLabel,
            txtUpdateVersion, txtUpdateOutputFolder, btnSelectUpdateOutput,
            btnCreateUpdateZip, btnPrepareCustomerSetup
        ]);
        packagingPage.Controls.Add(grpPackaging);
        packagingPage.Controls.Add(new Label
        {
            Text = "İmza anahtarı ve lisans üretici müşteri paketine eklenmez.",
            AutoSize = true, Location = new Point(20, 328)
        });

        dtExpire.Value = DateTime.Now.AddDays((int)txtDays.Value);
        dtSaleDate.Value = DateTime.Today;
        txtUpdateOutputFolder.Text = GetDefaultSetupOutputRoot();
        txtUpdateVersion.Text = "1.0.0";
        txtDays.ValueChanged += (_, _) =>
        {
            if (_syncing) return;
            _syncing = true;
            dtExpire.Value = DateTime.Now.AddDays((int)txtDays.Value);
            _syncing = false;
        };
        dtExpire.ValueChanged += (_, _) =>
        {
            if (_syncing) return;
            _syncing = true;
            var days = Math.Max(1, (dtExpire.Value.Date - DateTime.Now.Date).Days);
            txtDays.Value = Math.Min(days, 3650);
            _syncing = false;
        };
        ResumeLayout(true);
    }
    private void ApplyProfessionalTheme()
    {
        foreach (var box in new[] { grpLicenseEditor, grpOutput, grpPackaging, grpHistory })
        {
            box.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            box.ForeColor = Color.FromArgb(27, 40, 67);
            box.BackColor = Color.White;
        }

        foreach (var input in new Control[] { txtFirma, txtMachine, txtPhone, txtMaximumVersion, txtKey, txtSearch, txtUpdateSourceFolder, txtUpdateVersion, txtUpdateOutputFolder, txtDays, dtExpire, dtSaleDate, numAmount, cmbOperationFilter })
        {
            input.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        }

        foreach (var button in new[] { btnUret, btnUpdateSale, btnDeleteSale, btnRenewRemaining, btnCopy, btnExportReport, btnSelectUpdateSource, btnSelectUpdateOutput, btnCreateUpdateZip, btnPrepareCustomerSetup })
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
        }

        btnUret.BackColor = Color.FromArgb(33, 115, 70);
        btnUret.ForeColor = Color.White;
        btnUpdateSale.BackColor = Color.FromArgb(218, 140, 20);
        btnUpdateSale.ForeColor = Color.White;
        btnDeleteSale.BackColor = Color.FromArgb(181, 53, 53);
        btnDeleteSale.ForeColor = Color.White;
        btnRenewRemaining.BackColor = Color.FromArgb(28, 125, 152);
        btnRenewRemaining.ForeColor = Color.White;
        btnCopy.BackColor = Color.FromArgb(36, 80, 180);
        btnCopy.ForeColor = Color.White;
        btnExportReport.BackColor = Color.FromArgb(72, 84, 112);
        btnExportReport.ForeColor = Color.White;
        btnSelectUpdateSource.BackColor = Color.FromArgb(223, 229, 242);
        btnSelectUpdateSource.ForeColor = Color.FromArgb(22, 36, 67);
        btnSelectUpdateOutput.BackColor = Color.FromArgb(223, 229, 242);
        btnSelectUpdateOutput.ForeColor = Color.FromArgb(22, 36, 67);
        btnCreateUpdateZip.BackColor = Color.FromArgb(93, 71, 195);
        btnCreateUpdateZip.ForeColor = Color.White;
        btnPrepareCustomerSetup.BackColor = Color.FromArgb(53, 64, 126);
        btnPrepareCustomerSetup.ForeColor = Color.White;

        foreach (var button in new[] { btnSigningKeyStatus, btnSigningKeyExport, btnSigningKeyVerify, btnSigningKeyImport })
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            button.BackColor = Color.FromArgb(223, 229, 242);
            button.ForeColor = Color.FromArgb(22, 36, 67);
        }

        lstRenewals.BorderStyle = BorderStyle.FixedSingle;
        lstRenewals.Font = new Font("Segoe UI", 9f);

        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(31, 54, 98);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
        grid.ColumnHeadersHeight = 34;
        grid.RowTemplate.Height = 30;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 230, 255);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(22, 36, 67);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 253);
    }

    private void InitDatabase()
    {
        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();
        using var cmd = con.CreateCommand();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Licenses (
                Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                FirmaKodu           TEXT NOT NULL,
                MachineId           TEXT NOT NULL,
                ExpireDate          TEXT NOT NULL,
                CreatedAt           TEXT NOT NULL,
                AllowedVersion      TEXT NOT NULL DEFAULT '1.0.99'
            )";
        cmd.ExecuteNonQuery();

        AddColumnIfMissing(con, "Modules", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(con, "DurationDays", "INTEGER NOT NULL DEFAULT 365");
        AddColumnIfMissing(con, "ContactPhone", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(con, "SaleDate", "TEXT NOT NULL DEFAULT ''");
        AddColumnIfMissing(con, "SaleAmount", "REAL NOT NULL DEFAULT 0");
        AddColumnIfMissing(con, "OperationType", "TEXT NOT NULL DEFAULT 'Sale'");
        AddColumnIfMissing(con, "ParentLicenseId", "INTEGER NULL");
        AddColumnIfMissing(con, "RemainingDaysAtIssue", "INTEGER NOT NULL DEFAULT 0");

        using var idxCmd = con.CreateCommand();
        idxCmd.CommandText = @"
            CREATE INDEX IF NOT EXISTS IX_Licenses_OperationType ON Licenses(OperationType);
            CREATE INDEX IF NOT EXISTS IX_Licenses_ParentLicenseId ON Licenses(ParentLicenseId);
            CREATE INDEX IF NOT EXISTS IX_Licenses_FirmaMachine ON Licenses(FirmaKodu, MachineId);";
        idxCmd.ExecuteNonQuery();
        // Yerel satış/yenileme geçmişindeki doğrudan SQL yazımları da aynı audit motorundan geçer.
        MKFiloServis.Shared.Auditing.DatabaseWriteAudit.EnsureAsync(con).GetAwaiter().GetResult();
    }

    private static void AddColumnIfMissing(SqliteConnection con, string columnName, string definition)
    {
        using var checkCmd = con.CreateCommand();
        checkCmd.CommandText = "PRAGMA table_info(Licenses)";
        using var reader = checkCmd.ExecuteReader();

        while (reader.Read())
        {
            if (string.Equals(reader[1]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                return;
        }

        using var alter = con.CreateCommand();
        alter.CommandText = $"ALTER TABLE Licenses ADD COLUMN {columnName} {definition}";
        alter.ExecuteNonQuery();
    }

    private int SaveLicense(
        string firma,
        string machine,
        DateTime expire,
        DateTime created,
        string allowedVersion,
        int durationDays,
        string contactPhone,
        DateTime saleDate,
        decimal saleAmount,
        string operationType,
        int? parentLicenseId,
        int remainingDaysAtIssue,
        string modules)
    {
        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Licenses
            (
                FirmaKodu, MachineId, ExpireDate, CreatedAt, AllowedVersion,
                DurationDays, ContactPhone, SaleDate, SaleAmount, OperationType,
                ParentLicenseId, RemainingDaysAtIssue, Modules
            )
            VALUES
            (
                $f, $m, $e, $c, $v,
                $d, $p, $sd, $sa, $ot,
                $parent, $remaining, $modules
            );
            SELECT last_insert_rowid();";

        cmd.Parameters.AddWithValue("$f", firma);
        cmd.Parameters.AddWithValue("$m", machine);
        cmd.Parameters.AddWithValue("$e", expire.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$c", created.ToString("yyyy-MM-dd HH:mm"));
        cmd.Parameters.AddWithValue("$v", allowedVersion);
        cmd.Parameters.AddWithValue("$d", durationDays);
        cmd.Parameters.AddWithValue("$p", NormalizePhone(contactPhone));
        cmd.Parameters.AddWithValue("$sd", saleDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$sa", saleAmount);
        cmd.Parameters.AddWithValue("$ot", operationType);
        cmd.Parameters.AddWithValue("$parent", (object?)parentLicenseId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$remaining", remainingDaysAtIssue);
        cmd.Parameters.AddWithValue("$modules", modules);

        var inserted = cmd.ExecuteScalar();
        return Convert.ToInt32(inserted, CultureInfo.InvariantCulture);
    }

    private void LoadData()
    {
        try
        {
            using var con = new SqliteConnection($"Data Source={_dbPath}");
            con.Open();
            using var cmd = con.CreateCommand();

            var search = txtSearch.Text.Trim();
            var onlySales = string.Equals(cmbOperationFilter.SelectedItem?.ToString(), "Sadece Satislar", StringComparison.OrdinalIgnoreCase);

            cmd.CommandText = @"
                SELECT
                    Id,
                    OperationType,
                    FirmaKodu,
                    MachineId,
                    SaleDate,
                    SaleAmount,
                    ExpireDate,
                    DurationDays,
                    RemainingDaysAtIssue,
                    ParentLicenseId,
                    ContactPhone,
                    CreatedAt,
                    AllowedVersion, Modules
                FROM Licenses
                WHERE
                    ($search = '' OR FirmaKodu LIKE $like OR MachineId LIKE $like OR ContactPhone LIKE $like)
                    AND ($onlySales = 0 OR OperationType = 'Sale')
                ORDER BY Id DESC";

            cmd.Parameters.AddWithValue("$search", search);
            cmd.Parameters.AddWithValue("$like", $"%{search}%");
            cmd.Parameters.AddWithValue("$onlySales", onlySales ? 1 : 0);

            using var reader = cmd.ExecuteReader();
            var dt = new DataTable();
            dt.Load(reader);

            if (!dt.Columns.Contains("KalanGun"))
                dt.Columns.Add("KalanGun", typeof(int));

            foreach (DataRow row in dt.Rows)
            {
                if (DateTime.TryParse(row["ExpireDate"]?.ToString(), out var expire))
                    row["KalanGun"] = Math.Max(0, (expire.Date - DateTime.Now.Date).Days);
                else
                    row["KalanGun"] = 0;
            }

            grid.DataSource = dt;
            ConfigureGridColumns();

            if (grid.Rows.Count > 0 && grid.CurrentRow is null)
            {
                grid.Rows[0].Selected = true;
                grid.CurrentCell = grid.Rows[0].Cells["Id"];
            }

            LoadSelectedSaleToForm();
            LoadRenewalHistoryForSelected();
            UpdateActionButtons();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[MainForm] LoadData: {ex.Message}");
        }
    }

    private void ConfigureGridColumns()
    {
        if (grid.DataSource is not DataTable dt || dt.Columns.Count == 0)
            return;

        grid.Columns["Id"]!.HeaderText = "No";
        grid.Columns["Id"]!.FillWeight = 6;

        grid.Columns["OperationType"]!.HeaderText = "Islem";
        grid.Columns["OperationType"]!.FillWeight = 8;

        grid.Columns["FirmaKodu"]!.HeaderText = "Firma";
        grid.Columns["FirmaKodu"]!.FillWeight = 11;

        grid.Columns["MachineId"]!.HeaderText = "Makine";
        grid.Columns["MachineId"]!.FillWeight = 18;

        grid.Columns["SaleDate"]!.HeaderText = "Satis Tarihi";
        grid.Columns["SaleDate"]!.FillWeight = 10;

        grid.Columns["SaleAmount"]!.HeaderText = "Tutar";
        grid.Columns["SaleAmount"]!.FillWeight = 8;
        grid.Columns["SaleAmount"]!.DefaultCellStyle.Format = "N2";

        grid.Columns["ExpireDate"]!.HeaderText = "Bitis";
        grid.Columns["ExpireDate"]!.FillWeight = 10;

        grid.Columns["DurationDays"]!.HeaderText = "Sure(Gun)";
        grid.Columns["DurationDays"]!.FillWeight = 7;

        grid.Columns["KalanGun"]!.HeaderText = "Kalan";
        grid.Columns["KalanGun"]!.FillWeight = 6;

        grid.Columns["RemainingDaysAtIssue"]!.HeaderText = "YenilemeGun";
        grid.Columns["RemainingDaysAtIssue"]!.FillWeight = 8;

        grid.Columns["ParentLicenseId"]!.HeaderText = "BagliNo";
        grid.Columns["ParentLicenseId"]!.FillWeight = 7;

        grid.Columns["ContactPhone"]!.HeaderText = "Telefon";
        grid.Columns["ContactPhone"]!.FillWeight = 10;

        grid.Columns["CreatedAt"]!.HeaderText = "Olusturma";
        grid.Columns["CreatedAt"]!.FillWeight = 10;

        grid.Columns["AllowedVersion"]!.HeaderText = "Surum";
        grid.Columns["AllowedVersion"]!.FillWeight = 6;

        grid.Columns["Modules"]!.HeaderText = "Modüller";
        grid.Columns["Modules"]!.FillWeight = 18;
        grid.Columns["Modules"]!.MinimumWidth = 100;

        // Ayrıntılar seçilen kayıtta ve CSV raporunda korunur; liste temel alanları gösterir.
        foreach (var name in new[] { "SaleDate", "DurationDays", "RemainingDaysAtIssue", "ParentLicenseId", "ContactPhone", "CreatedAt", "AllowedVersion" })
            grid.Columns[name]!.Visible = false;
        grid.Columns["OperationType"]!.HeaderText = "İşlem";
        grid.Columns["ExpireDate"]!.HeaderText = "Bitiş Tarihi";
        grid.Columns["Id"]!.MinimumWidth = 40;
        grid.Columns["FirmaKodu"]!.MinimumWidth = 110;
        grid.Columns["MachineId"]!.MinimumWidth = 150;
        grid.Columns["ExpireDate"]!.MinimumWidth = 100;
        grid.Columns["SaleAmount"]!.MinimumWidth = 85;
        grid.Columns["KalanGun"]!.MinimumWidth = 55;
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
        var column = grid.Columns[e.ColumnIndex].Name;
        if (column == "ExpireDate" && DateTime.TryParse(Convert.ToString(e.Value, CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry))
        {
            e.Value = expiry.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            e.FormattingApplied = true;
            return;
        }
        if (column == "Modules")
        {
            var ids = Convert.ToString(e.Value)?.Split(',') ?? [];
            e.Value = string.Join(", ", ids.Where(MKFiloServis.Shared.Licensing.LicenseModules.All.ContainsKey)
                .Select(x => MKFiloServis.Shared.Licensing.LicenseModules.All[x]));
            if (string.IsNullOrEmpty(Convert.ToString(e.Value))) e.Value = "Modül seçilmemiş";
            e.FormattingApplied = true;
            return;
        }
        if (column == "OperationType")
        {
            e.Value = Convert.ToString(e.Value) switch
            {
                "Sale" => "Satış",
                "Renewal" => "Yenileme",
                "V2Reissue" => "v2 Yeniden Basım",
                "V3Reissue" => "Modüllü Yeniden Basım",
                _ => e.Value
            };
            e.FormattingApplied = true;
            return;
        }
        if (column != "KalanGun")
            return;

        var row = grid.Rows[e.RowIndex];
        var value = Convert.ToInt32(e.Value ?? 0, CultureInfo.InvariantCulture);

        if (value <= 0)
        {
            row.DefaultCellStyle.BackColor = Color.LightCoral;
            row.DefaultCellStyle.ForeColor = Color.DarkRed;
        }
        else if (value <= 7)
        {
            row.DefaultCellStyle.BackColor = Color.Moccasin;
            row.DefaultCellStyle.ForeColor = Color.DarkOrange;
        }
        else
        {
            row.DefaultCellStyle.BackColor = Color.Empty;
            row.DefaultCellStyle.ForeColor = Color.Empty;
        }
    }

    private void UretSatisLisansi()
    {
        if (!ValidateMandatoryFields())
            return;

        try
        {
            var firma = NormalizeFirmaKodu(txtFirma.Text);
            var machine = NormalizeMachineId(txtMachine.Text);
            var phone = NormalizePhone(txtPhone.Text);
            var created = DateTime.UtcNow;
            var expire = dtExpire.Value.Date;
            var durationDays = (int)txtDays.Value;

            var key = BuildLicenseKey(firma, machine, expire, durationDays, phone, created, GetMaximumVersion(), SelectedModules());
            SaveLicense(
                firma,
                machine,
                expire,
                created,
                GetMaximumVersion(),
                durationDays,
                phone,
                dtSaleDate.Value.Date,
                numAmount.Value,
                operationType: "Sale",
                parentLicenseId: null,
                remainingDaysAtIssue: durationDays, modules: SelectedModules());

            ShowKeyAndRefresh(key);

            MessageBox.Show(
                $"Satis lisansi olusturuldu.\n\nFirma: {firma}\nBitis: {expire:yyyy-MM-dd}\nSure: {durationDays} gun\nTutar: {numAmount.Value:N2}",
                "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void YenileKalanGunle()
    {
        if (GetSelectedGridRow() is not DataGridViewRow selectedGridRow || selectedGridRow.DataBoundItem is not DataRowView rowView)
        {
            MessageBox.Show("Lutfen yenilemek icin bir lisans secin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!ValidateMandatoryFields())
            return;

        var selected = rowView.Row;
        var operationType = selected["OperationType"]?.ToString() ?? "Sale";
        var parentId = Convert.ToInt32(selected["Id"], CultureInfo.InvariantCulture);
        if (string.Equals(operationType, "Renewal", StringComparison.OrdinalIgnoreCase) && selected["ParentLicenseId"] != DBNull.Value)
            parentId = Convert.ToInt32(selected["ParentLicenseId"], CultureInfo.InvariantCulture);

        if (!DateTime.TryParse(selected["ExpireDate"]?.ToString(), out var originalExpire))
        {
            MessageBox.Show("Secilen kaydin bitis tarihi okunamadi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var remainingDays = Math.Max(0, (originalExpire.Date - DateTime.Now.Date).Days);
        if (remainingDays <= 0)
        {
            MessageBox.Show("Secilen lisansin kalan gunu yok (suresi dolmus).", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var created = DateTime.UtcNow;
            var expire = DateTime.Now.Date.AddDays(remainingDays);
            var firma = NormalizeFirmaKodu(txtFirma.Text);
            var machine = NormalizeMachineId(txtMachine.Text);
            var phone = NormalizePhone(txtPhone.Text);

            var key = BuildLicenseKey(firma, machine, expire, remainingDays, phone, created, GetMaximumVersion(), SelectedModules());
            var newId = SaveLicense(
                firma,
                machine,
                expire,
                created,
                GetMaximumVersion(),
                remainingDays,
                phone,
                dtSaleDate.Value.Date,
                numAmount.Value,
                operationType: "Renewal",
                parentLicenseId: parentId,
                remainingDaysAtIssue: remainingDays, modules: SelectedModules());

            ShowKeyAndRefresh(key);
            SelectGridRowById(newId);

            MessageBox.Show(
                $"Kalan gunle yenileme lisansi olusturuldu.\n\nKaynak No: {parentId}\nYenileme No: {newId}\nKalan Gun: {remainingDays}",
                "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Yenileme hatasi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ReissueSelectedLicenseAsV2()
    {
        try { ReissueSelectedLicenseAsV2Core(); }
        catch (Exception ex)
        {
            MessageBox.Show($"Lisans yeniden basımı tamamlanamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ReissueSelectedLicenseAsV2Core()
    {
        if (GetSelectedGridRow()?.DataBoundItem is not DataRowView rowView)
        {
            MessageBox.Show("modüllü olarak yeniden basmak için listeden bir lisans kaydı seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var row = rowView.Row;
        var sourceId = Convert.ToInt32(row["Id"], CultureInfo.InvariantCulture);
        var firma = NormalizeFirmaKodu(row["FirmaKodu"]?.ToString());
        var machine = NormalizeMachineId(row["MachineId"]?.ToString());
        var phone = NormalizePhone(row["ContactPhone"]?.ToString());
        var version = row["AllowedVersion"]?.ToString()?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(firma) || string.IsNullOrWhiteSpace(machine) ||
            !MKFiloServis.Shared.Licensing.LicenseVersionPolicy.IsValid(version) ||
            !DateTime.TryParse(row["ExpireDate"]?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var expire))
        {
            MessageBox.Show("Seçili lisans kaydında firma, makine, sürüm veya bitiş tarihi eksik/geçersiz.", "Lisans kaydı geçersiz", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (expire.Date <= DateTime.Today)
        {
            MessageBox.Show("Süresi dolmuş lisans yeniden basılamaz. Yeni satış veya yenileme akışını kullanın.", "Lisans süresi dolmuş", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var remainingDays = (expire.Date - DateTime.Today).Days;
        var originalDuration = row["DurationDays"] is DBNull ? 0 : Convert.ToInt32(row["DurationDays"], CultureInfo.InvariantCulture);
        if (!DateTime.TryParse(row["CreatedAt"]?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var originalCreated)
            || Math.Abs((expire.Date - originalCreated.Date).Days - originalDuration) > 2)
        {
            MessageBox.Show("Kaydın oluşturma tarihi, süresi ve bitiş tarihi tutarlı değil. Yetkili kaynaktan doğrulayın.", "Lisans kaydı geçersiz", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (originalDuration < 1 || originalDuration > 3650)
        {
            MessageBox.Show("Kaydın lisans süresi geçersiz; süreyi yetkili kaynaktan doğrulamadan yeniden basmayın.", "Lisans kaydı geçersiz", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var operationType = row["OperationType"]?.ToString() ?? "Sale";
        if (string.Equals(operationType, "V3Reissue", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Bu kayıt zaten modüllü yeniden basım kaydı. Aynı lisansı tekrar üretmeyin.", "Yeniden basım zaten yapıldı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using (var con = new SqliteConnection($"Data Source={_dbPath}"))
        {
            con.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
                SELECT EXISTS(
                    SELECT 1 FROM Licenses
                    WHERE FirmaKodu = $firma AND MachineId = $machine AND Id <> $id
                      AND (date(ExpireDate) > date($expire)
                           OR (OperationType = 'V3Reissue' AND date(ExpireDate) >= date($expire)))
                )";
            cmd.Parameters.AddWithValue("$firma", firma);
            cmd.Parameters.AddWithValue("$machine", machine);
            cmd.Parameters.AddWithValue("$id", sourceId);
            cmd.Parameters.AddWithValue("$expire", expire.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            if (Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) != 0)
            {
                MessageBox.Show("Bu firma/makine için daha ileri tarihli veya aynı bitiş tarihine sahip modüllü yeniden basım kaydı var. En güncel lisans kaydını seçin.", "Eski lisans kaydı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        var rootLicenseId = sourceId;
        if (row["ParentLicenseId"] is not DBNull && row["ParentLicenseId"] != null)
            rootLicenseId = Convert.ToInt32(row["ParentLicenseId"], CultureInfo.InvariantCulture);

        var answer = MessageBox.Show(
            $"Bu kayıt müşteri lisans hakkının yetkili kaynağından doğrulandı mı?\n\n" +
            $"Firma: {firma}\nMakine: {machine}\nBitiş: {expire:yyyy-MM-dd}\nKalan: {remainingDays} gün\nModüller: {string.Join(", ", moduleSelection.CheckedItems.Cast<KeyValuePair<string, string>>().Select(x => x.Value))}\nKaynak kayıt: #{sourceId} ({operationType})\n\n" +
            "Onaylanırsa aynı firma, makine ve bitiş tarihi için Seçili modüller için v3 imzalı lisans anahtarı üretilecek. Bu işlem yeni satış/uzatma yapmaz.",
            "Modüllü lisansı yeniden bas", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (answer != DialogResult.Yes)
            return;

        try
        {
            var created = originalCreated;
            if (moduleSelection.CheckedItems.Count == 0)
                throw new InvalidOperationException("Yeniden basım için müşterinin lisanslı modüllerini seçin.");
            var key = BuildLicenseKey(firma, machine, expire.Date, originalDuration, phone, created, version, SelectedModules());
            var newId = SaveLicense(
                firma,
                machine,
                expire.Date,
                created,
                version,
                originalDuration,
                phone,
                DateTime.Today,
                0m,
                operationType: "V3Reissue",
                parentLicenseId: rootLicenseId,
                remainingDaysAtIssue: remainingDays, modules: SelectedModules());

            ShowKeyAndRefresh(key);
            cmbOperationFilter.SelectedItem = "Tum Islemler";
            LoadData();
            SelectGridRowById(newId);
            MessageBox.Show(
                $"Modüllü v3 lisansı oluşturuldu ve geçiş kaydına eklendi.\n\nYeni kayıt: #{newId}\nKaynak: #{sourceId}\nBitiş: {expire:yyyy-MM-dd}\nKalan: {remainingDays} gün\n\nAnahtar panoya kopyalandı. Müşteriye teslimi ve kabulü ayrıca kaydedin.",
                "v3 lisansı hazır", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Modüllü lisans oluşturulamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool ValidateMandatoryFields(bool requireModules = true)
    {
        if (requireModules && moduleSelection.CheckedItems.Count == 0)
        {
            MessageBox.Show("En az bir lisans modülü seçin. Eski kayıtlarda modül hakkını müşteri anlaşmasına göre belirleyin.", "Modül Seçimi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        if (!MKFiloServis.Shared.Licensing.LicenseVersionPolicy.IsValid(GetMaximumVersion())
            || (!chkUnlimitedVersion.Checked && GetMaximumVersion() == MKFiloServis.Shared.Licensing.LicenseVersionPolicy.Unlimited))
        {
            MessageBox.Show(this, "En fazla sürüm alanına 1.0.99 gibi geçerli bir sürüm girin. Sınırsız hak için ilgili kutuyu işaretleyin.",
                "Lisans Sürümü", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        if (string.IsNullOrWhiteSpace(txtFirma.Text))
        {
            MessageBox.Show("Firma kodu zorunlu. Web uygulamasindaki firma kodu ile ayni degeri girin (orn: F001).", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtMachine.Text))
        {
            MessageBox.Show("Machine ID zorunlu.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        var normalizedMachine = NormalizeMachineId(txtMachine.Text);
        if (!normalizedMachine.Contains('_'))
        {
            var devam = MessageBox.Show(
                "Machine ID beklenen formatta gorunmuyor (MAKINE_KULLANICI_KOD).\nWeb uygulamasindaki makine kodunu birebir kopyaladiginizdan emin olun.\n\nYine de devam edilsin mi?",
                "Machine ID Kontrolu", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (devam != DialogResult.Yes)
                return false;
        }

        if (string.IsNullOrWhiteSpace(txtPhone.Text))
        {
            MessageBox.Show("Iletisim telefonu zorunlu.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (dtSaleDate.Value.Date == DateTime.MinValue.Date)
        {
            MessageBox.Show("Satis tarihi zorunlu.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (numAmount.Value <= 0)
        {
            MessageBox.Show("Satis tutari 0'dan buyuk olmali.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }

    /// <summary>Firma kodunu web'deki LicenseService ile ayni sekilde normalize eder.</summary>
    private static string NormalizeFirmaKodu(string? value)
        => MKFiloServis.Shared.Licensing.LicenseIdentity.CompanyCode(value);

    /// <summary>
    /// Machine ID icindeki TUM whitespace karakterlerini (satir sonu, tab, bosluk) temizler.
    /// Multiline kutuya yapistirilan makine kodlarindaki gizli satir sonlari lisans imzasini bozuyordu.
    /// </summary>
    private static string NormalizeMachineId(string? value)
        => MKFiloServis.Shared.Licensing.LicenseIdentity.MachineId(value);

    /// <summary>
    /// Telefon alanindaki whitespace karakterlerini temizler.
    /// Format karakterlerini korur, sadece yapistirma kaynakli gizli bosluklari atar.
    /// </summary>
    private static string NormalizePhone(string? value)
        => MKFiloServis.Shared.Licensing.LicenseIdentity.ContactPhone(value);

    private string GetMaximumVersion()
        => chkUnlimitedVersion.Checked ? MKFiloServis.Shared.Licensing.LicenseVersionPolicy.Unlimited
            : txtMaximumVersion.Text.Trim();

    private static string BuildLicenseKey(string firma, string machine, DateTime expire, int durationDays, string phone, DateTime created, string allowedVersion, string modules)
    {
        const bool isDemo = false;
        allowedVersion = allowedVersion.Trim();
        if (!MKFiloServis.Shared.Licensing.LicenseVersionPolicy.IsValid(allowedVersion))
            throw new ArgumentException("Lisans sürüm hakkı geçersiz. 1.0.99 gibi sayısal bir sürüm gerekir.", nameof(allowedVersion));
        firma = NormalizeFirmaKodu(firma);
        machine = NormalizeMachineId(machine);
        phone = NormalizePhone(phone);

        var v2Payload = MKFiloServis.Shared.Licensing.LicenseSignaturePayload.Create(
            firma, machine, expire, durationDays, isDemo, allowedVersion, created, phone);
        var raw = MKFiloServis.Shared.Licensing.LicenseModules.Payload(v2Payload, modules);
        using var rsa = LicenseSigningKeyStore.OpenSigningKey();
        var signature = MKFiloServis.Shared.Licensing.LicenseModules.Envelope(modules, Convert.ToBase64String(rsa.SignData(
            Encoding.UTF8.GetBytes(raw), HashAlgorithmName.SHA256, RSASignaturePadding.Pss)));

        var json = JsonSerializer.Serialize(new
        {
            FirmaKodu = firma,
            MachineId = machine,
            ExpireDate = expire,
            DurationDays = durationDays,
            AllowedVersion = allowedVersion,
            IsDemo = isDemo,
            CreatedAt = created,
            ContactPhone = phone,
            Signature = signature
        });

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private void ShowSigningKeyStatus()
    {
        try
        {
            var fingerprint = LicenseSigningKeyStore.GetFingerprint();
            MessageBox.Show(this, "İmzalama anahtarı programın şifreli deposunda hazır.\n\nAçık anahtar parmak izi:\n" + fingerprint,
                "İmza Anahtarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "İmza Anahtarı", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ExportSigningKeyBackup()
    {
        using var password = new KeyBackupPasswordDialog(creatingBackup: true);
        if (password.ShowDialog(this) != DialogResult.OK) return;
        using var save = new SaveFileDialog
        {
            Title = "Şifreli imzalama anahtarı yedeğini kaydet",
            Filter = "MKFiloServis şifreli anahtar yedeği (*.mkkey)|*.mkkey",
            FileName = $"MKFiloServis-ImzaAnahtari-{DateTime.Now:yyyyMMdd-HHmmss}.mkkey",
            OverwritePrompt = true
        };
        if (save.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            LicenseSigningKeyStore.ExportBackup(save.FileName, password.Password, overwrite: true);
            MessageBox.Show(this, "Şifreli yedek kaydedildi ve geri açılarak doğrulandı.\n\n" + save.FileName,
                "Anahtar Yedeği", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Anahtar Yedeği", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void VerifySigningKeyBackup()
    {
        using var open = new OpenFileDialog
        {
            Title = "Şifreli anahtar yedeğini doğrula",
            Filter = "MKFiloServis şifreli anahtar yedeği (*.mkkey)|*.mkkey",
            CheckFileExists = true
        };
        if (open.ShowDialog(this) != DialogResult.OK) return;
        using var password = new KeyBackupPasswordDialog(creatingBackup: false);
        if (password.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            LicenseSigningKeyStore.VerifyBackup(open.FileName, password.Password);
            MessageBox.Show(this, "Yedek parola ile açıldı ve bu sürümün açık anahtarıyla imza eşleşmesi doğrulandı.\nMevcut imzalama anahtarı değiştirilmedi.",
                "Yedek Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Yedek Doğrulama", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ImportSigningKeyBackup()
    {
        using var open = new OpenFileDialog
        {
            Title = "İmzalama anahtarını veya yedeğini seç",
            Filter = "Desteklenen anahtarlar (*.mkkey;*.pem;*.json)|*.mkkey;*.pem;*.json",
            CheckFileExists = true
        };
        if (open.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var extension = Path.GetExtension(open.FileName);
            if (extension.Equals(".pem", StringComparison.OrdinalIgnoreCase) || extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                LicenseSigningKeyStore.Import(open.FileName, []);
            else
            {
                using var password = new KeyBackupPasswordDialog(creatingBackup: false);
                if (password.ShowDialog(this) != DialogResult.OK) return;
                LicenseSigningKeyStore.Import(open.FileName, password.Password);
            }
            MessageBox.Show(this, "İmzalama anahtarı doğrulandı ve programın şifreli deposuna alındı.",
                "İmza Anahtarı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "İmza Anahtarı", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ShowKeyAndRefresh(string key)
    {
        txtKey.Text = key;
        txtKey.SelectAll();
        btnCopy.Enabled = true;
        try { Clipboard.SetText(key); }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            MessageBox.Show("Lisans kaydedildi; pano kullanılamıyor. Anahtarı ekrandan kopyalayın.", "Pano", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        UpdateHistorySummary();
        LoadData();
    }

    private void CopyKey()
    {
        if (string.IsNullOrWhiteSpace(txtKey.Text) || txtKey.Text == "Henuz lisans uretilmedi.")
            return;

        Clipboard.SetText(txtKey.Text);
        MessageBox.Show("Panoya kopyalandi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SelectUpdateSourceFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Guncelleme paketi icin kaynak (publish) klasorunu secin"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
            txtUpdateSourceFolder.Text = dialog.SelectedPath;
    }

    private void SelectUpdateOutputFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Olusan guncelleme ZIP dosyasinin kaydedilecegi klasoru secin"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
            txtUpdateOutputFolder.Text = dialog.SelectedPath;
    }

    private void CreateUpdateZipPackage()
    {
        var sourceFolder = txtUpdateSourceFolder.Text.Trim();
        var outputRoot = txtUpdateOutputFolder.Text.Trim();
        var versionInput = txtUpdateVersion.Text.Trim();

        if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
        {
            MessageBox.Show("Gecerli bir kaynak klasor secin.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(versionInput))
        {
            MessageBox.Show("Versiyon bilgisini girin (orn: 1.0.26).", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!Directory.EnumerateFileSystemEntries(sourceFolder).Any())
        {
            MessageBox.Show("Kaynak klasor bos. Paket olusturulamadi.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(outputRoot))
            outputRoot = GetDefaultSetupOutputRoot();

        var cleanVersion = versionInput.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? versionInput[1..]
            : versionInput;
        if (!MKFiloServis.Shared.Licensing.LicenseVersionPolicy.IsValid(cleanVersion))
        {
            MessageBox.Show(this, "Paket sürümü geçersiz. 1.0.26 gibi sayısal bir sürüm girin.",
                "Paket Sürümü", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var versionFolderName = $"v{cleanVersion}";
        var versionFolder = Path.Combine(outputRoot, versionFolderName);

        Directory.CreateDirectory(outputRoot);
        Directory.CreateDirectory(versionFolder);

        var packageName = $"MKFiloServis_Update_v{cleanVersion}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
        var zipPath = Path.Combine(versionFolder, packageName);

        if (File.Exists(zipPath))
            File.Delete(zipPath);

        ZipFile.CreateFromDirectory(sourceFolder, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

        var sigPath = Path.ChangeExtension(zipPath, ".sig");
        using (var sha = SHA256.Create())
        using (var fs = File.OpenRead(zipPath))
        {
            var hash = sha.ComputeHash(fs);
            var sig = Convert.ToBase64String(hash);
            File.WriteAllText(sigPath, sig);
        }

        CopySetupExecutablesToVersionFolder(sourceFolder, versionFolder, cleanVersion);
        File.WriteAllText(Path.Combine(versionFolder, "version.txt"), $"v{cleanVersion}");
        File.WriteAllText(
            Path.Combine(versionFolder, "README.md"),
            $"# MKFiloServis Kurulum Ciktilari\n\nSurum: v{cleanVersion}\nOlusturma: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n- Update ZIP: {Path.GetFileName(zipPath)}\n- Signature: {Path.GetFileName(sigPath)}\n");

        Clipboard.SetText(versionFolder);
        MessageBox.Show(
            $"Kurulum/guncelleme ciktilari olusturuldu.\n\nKlasor: {versionFolder}\nZIP: {zipPath}\nSIG: {sigPath}\n\nCikis klasoru panoya kopyalandi.",
            "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void CopySetupExecutablesToVersionFolder(string sourceFolder, string versionFolder, string version)
    {
        var sourceExes = Directory
            .EnumerateFiles(sourceFolder, "*.exe", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToList();

        string? FindLatest(Func<string, bool> predicate)
            => sourceExes.FirstOrDefault(predicate);

        var filesToCopy = new (string Target, string? Source)[]
        {
            ($"MKFiloServisGuncelle-{version}.exe", FindLatest(p => Path.GetFileName(p).StartsWith("MKFiloServisGuncelle", StringComparison.OrdinalIgnoreCase))),
            ($"MKFiloServisKurulumMusteri-{version}.exe", FindLatest(p => Path.GetFileName(p).StartsWith("MKFiloServisKurulumMusteri", StringComparison.OrdinalIgnoreCase))),
            ($"MKFiloServisKurulum-{version}.exe", FindLatest(p => Path.GetFileName(p).StartsWith("MKFiloServisKurulum", StringComparison.OrdinalIgnoreCase) && !Path.GetFileName(p).StartsWith("MKFiloServisKurulumMusteri", StringComparison.OrdinalIgnoreCase)))
        };

        foreach (var file in filesToCopy)
        {
            if (string.IsNullOrWhiteSpace(file.Source))
                continue;

            var targetPath = Path.Combine(versionFolder, file.Target);
            File.Copy(file.Source, targetPath, true);
        }
    }

    private static string GetDefaultSetupOutputRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "setup", "output");
            if (Directory.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "setup", "output");
    }

    private static string? TryFindDefaultPublishSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "publish", "MKFiloServis.Web");
            if (Directory.Exists(candidate) && Directory.EnumerateFileSystemEntries(candidate).Any())
                return candidate;

            dir = dir.Parent;
        }

        return null;
    }

    private void PrepareCustomerSetupPackage()
    {
        try
        {
            if (!ValidateMandatoryFields())
                return;

            var versionInput = txtUpdateVersion.Text.Trim();
            if (string.IsNullOrWhiteSpace(versionInput))
            {
                MessageBox.Show("Versiyon bilgisini girin (orn: 1.0.26).", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var sourceFolder = txtUpdateSourceFolder.Text.Trim();
            if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
            {
                sourceFolder = TryFindDefaultPublishSource() ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
            {
                MessageBox.Show("Publish kaynak klasoru otomatik bulunamadi. Bir kez kaynak klasoru secin, sonraki islemde otomatik kullanilir.", "Kaynak Bulunamadi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.EnumerateFileSystemEntries(sourceFolder).Any())
            {
                MessageBox.Show("Kaynak klasor bos. Paket olusturulamadi.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var outputRoot = txtUpdateOutputFolder.Text.Trim();
            if (string.IsNullOrWhiteSpace(outputRoot))
                outputRoot = GetDefaultSetupOutputRoot();

            var cleanVersion = versionInput.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? versionInput[1..]
                : versionInput;
            if (!MKFiloServis.Shared.Licensing.LicenseVersionPolicy.IsValid(cleanVersion)
                || !Version.TryParse(cleanVersion, out var packageVersion))
                throw new ArgumentException("Paket sürümü geçersiz. 1.0.26 gibi sayısal bir sürüm girin.");
            if (!MKFiloServis.Shared.Licensing.LicenseVersionPolicy.Allows(GetMaximumVersion(), packageVersion))
                throw new InvalidOperationException("Paket sürümü seçilen lisans sürüm hakkını aşıyor. Lisanslar sekmesindeki hakkı kontrol edin.");
            var versionFolder = Path.Combine(outputRoot, $"v{cleanVersion}");

            Directory.CreateDirectory(outputRoot);
            Directory.CreateDirectory(versionFolder);

            var firma = NormalizeFirmaKodu(txtFirma.Text);
            var machine = NormalizeMachineId(txtMachine.Text);
            var phone = NormalizePhone(txtPhone.Text);
            var created = DateTime.UtcNow;
            var expire = dtExpire.Value.Date;
            var durationDays = (int)txtDays.Value;

            var key = BuildLicenseKey(firma, machine, expire, durationDays, phone, created, GetMaximumVersion(), SelectedModules());

            var licenseFileName = $"musteri-lisans-{firma}-v{cleanVersion}.txt";
            var licenseFilePath = Path.Combine(versionFolder, licenseFileName);
            File.WriteAllText(
                licenseFilePath,
                $"FirmaKodu={firma}{Environment.NewLine}MachineId={machine}{Environment.NewLine}ExpireDate={expire:yyyy-MM-dd}{Environment.NewLine}ContactPhone={phone}{Environment.NewLine}{Environment.NewLine}LicenseKey={key}",
                Encoding.UTF8);

            // Uygulama ilk açılışta otomatik lisans aktivasyonu için publish içine anahtar dosyası bırak.
            var autoLicenseFileName = "license.auto.key";
            var autoLicenseSourcePath = Path.Combine(sourceFolder, autoLicenseFileName);
            File.WriteAllText(autoLicenseSourcePath, key, Encoding.UTF8);

            // Takip/denetim için version çıktısına da kopya bırak.
            var autoLicenseAuditPath = Path.Combine(versionFolder, autoLicenseFileName);
            File.WriteAllText(autoLicenseAuditPath, key, Encoding.UTF8);

            SaveLicense(
                firma,
                machine,
                expire,
                created,
                GetMaximumVersion(),
                durationDays,
                phone,
                dtSaleDate.Value.Date,
                numAmount.Value,
                operationType: "Sale",
                parentLicenseId: null,
                remainingDaysAtIssue: durationDays, modules: SelectedModules());

            txtUpdateSourceFolder.Text = sourceFolder;
            txtUpdateOutputFolder.Text = outputRoot;

            // Kurulum paketlerini (Inno Setup) build.ps1 ile uret ve MK adlariyla version klasorune yerlestir.
            if (!BuildInstallerPackages(cleanVersion, versionFolder, key))
                return;

            ShowKeyAndRefresh(key);

            MessageBox.Show(
                $"Musteri kurulum dosyalari hazirlandi.\n\nKlasor: {versionFolder}\nLisans Dosyasi: {licenseFileName}\n\nKullanicidan manuel dosya istemeden kurulum paketi hazir.",
                "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Musteri kurulum paketleme hatasi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// setup\build.ps1 dosyasini bulur; girilen versiyonla kurulum paketlerini uretir,
    /// license.auto.key'i payload'a gomup paketleri yeniden derler ve ciktilari
    /// v1.0.28 formatindaki gibi (MKFiloServis... adlari, version.txt, README.md) hazirlar.
    /// </summary>
    private bool BuildInstallerPackages(string version, string versionFolder, string licenseKey)
    {
        var setupDir = TryFindSetupDirectory();
        if (setupDir == null)
        {
            MessageBox.Show("setup\\build.ps1 bulunamadi. Kurulum paketleri uretilemedi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        var buildScript = Path.Combine(setupDir, "build.ps1");

        // 1) Tam build: publish + Inno Setup paketleri
        if (!RunPowerShellScript(buildScript, $"-Version {version}", setupDir, "Kurulum paketleri uretiliyor (publish + paketleme)..."))
            return false;

        // 2) Otomatik lisans anahtarini payload'a gom ve paketleri yeniden derle (publish atlanir)
        var payloadWeb = Path.Combine(setupDir, "payload", "Web");
        if (!Directory.Exists(payloadWeb))
            throw new DirectoryNotFoundException("Müşteri paketi için Web publish çıktısı bulunamadı.");
        var temporaryLicense = Path.Combine(payloadWeb, "license.auto.key");
        try
        {
            File.WriteAllText(temporaryLicense, licenseKey, Encoding.UTF8);
            if (!RunPowerShellScript(buildScript, $"-Version {version} -SkipPublish", setupDir, "Lisans anahtari gomulup paketler yeniden derleniyor..."))
                return false;
        }
        finally
        {
            // Müşteriye özel anahtarı sonraki genel paketlere taşımamak için publish kaynağından kaldır.
            if (File.Exists(temporaryLicense)) File.Delete(temporaryLicense);
        }

        // 3) Ciktilari MK adlariyla version klasorune tasi
        var buildOutput = Path.Combine(setupDir, "output", $"v{version}");
        var renames = new (string Source, string Target)[]
        {
            ($"MKFiloServisKurulum-{version}.exe", $"MKFiloServisKurulum-{version}.exe"),
            ($"MKFiloServisGuncelle-{version}.exe", $"MKFiloServisGuncelle-{version}.exe"),
            ($"MKFiloServisKurulumMusteri-{version}.exe", $"MKFiloServisKurulumMusteri-{version}.exe")
        };

        Directory.CreateDirectory(versionFolder);
        foreach (var (source, target) in renames)
        {
            var sourcePath = Path.Combine(buildOutput, source);
            if (!File.Exists(sourcePath))
                continue;

            var targetPath = Path.Combine(versionFolder, target);

            // Kaynak ve hedef ayni dosyaysa dokunma; aksi halde once silinen dosya tasinamiyor
            // ve MKFiloServisKurulum-*.exe versiyon klasorunde eksik kaliyordu.
            if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
                continue;

            if (File.Exists(targetPath))
                File.Delete(targetPath);

            if (string.Equals(Path.GetFullPath(buildOutput), Path.GetFullPath(versionFolder), StringComparison.OrdinalIgnoreCase))
                File.Move(sourcePath, targetPath);
            else
                File.Copy(sourcePath, targetPath, true);
        }

        File.WriteAllText(Path.Combine(versionFolder, "version.txt"), version);
        File.WriteAllText(
            Path.Combine(versionFolder, "README.md"),
            $"# MK FiloServis v{version} Kurulum Paketleri{Environment.NewLine}{Environment.NewLine}Olusturma: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}");

        return true;
    }

    private static string? TryFindSetupDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "setup");
            if (File.Exists(Path.Combine(candidate, "build.ps1")))
                return candidate;

            dir = dir.Parent;
        }

        return null;
    }

    private static bool RunPowerShellScript(string scriptPath, string arguments, string workingDirectory, string statusMessage)
    {
        var shell = FindPowerShellExecutable();

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = shell,
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" {arguments}",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process == null)
        {
            MessageBox.Show("PowerShell baslatilamadi.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        using var progress = new Form
        {
            Text = "Paketleme",
            Size = new Size(520, 120),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ControlBox = false
        };
        progress.Controls.Add(new Label { Text = statusMessage, Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleCenter });
        var bar = new ProgressBar { Style = ProgressBarStyle.Marquee, Dock = DockStyle.Top, Height = 24 };
        progress.Controls.Add(bar);
        bar.BringToFront();

        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdOut.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stdErr.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) =>
        {
            try { progress.Invoke(() => progress.Close()); } catch { /* form kapali olabilir */ }
        };

        if (process.HasExited)
            progress.Load += (_, _) => progress.Close();

        progress.ShowDialog();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var tail = stdOut.ToString();
            if (tail.Length > 2000) tail = tail[^2000..];
            MessageBox.Show(
                $"Paketleme betigi hata verdi (exit code {process.ExitCode}).\n\n{stdErr}\n{tail}",
                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        return true;
    }

    private static string FindPowerShellExecutable()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe"),
            "pwsh.exe"
        };

        foreach (var candidate in candidates)
        {
            try
            {
                if (Path.IsPathRooted(candidate) && File.Exists(candidate))
                    return candidate;
            }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine($"[MainForm] Aday yol atlandi: {candidate} ({ex.Message})"); }
        }

        // PATH uzerinden pwsh dene, yoksa Windows PowerShell kullan
        try
        {
            using var probe = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "pwsh.exe",
                Arguments = "-NoProfile -Command exit",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            probe?.WaitForExit(5000);
            if (probe != null) return "pwsh.exe";
        }
        catch { /* pwsh yok */ }

        return "powershell.exe";
    }

    private void LoadSelectedSaleToForm()
    {
        _selectedSaleId = null;

        if (GetSelectedGridRow()?.DataBoundItem is not DataRowView rowView)
            return;

        var row = rowView.Row;
        _selectedSaleId = Convert.ToInt32(row["Id"], CultureInfo.InvariantCulture);

        txtFirma.Text = row["FirmaKodu"]?.ToString() ?? string.Empty;
        txtMachine.Text = row["MachineId"]?.ToString() ?? string.Empty;
        txtPhone.Text = row["ContactPhone"]?.ToString() ?? string.Empty;
        var recordedModules = (row["Modules"]?.ToString() ?? "").Split(',');
        for (var i = 0; i < moduleSelection.Items.Count; i++)
            moduleSelection.SetItemChecked(i, recordedModules.Contains(((KeyValuePair<string, string>)moduleSelection.Items[i]).Key));
        var recordedVersion = row["AllowedVersion"]?.ToString()?.Trim() ?? string.Empty;
        chkUnlimitedVersion.Checked = recordedVersion == MKFiloServis.Shared.Licensing.LicenseVersionPolicy.Unlimited;
        txtMaximumVersion.Enabled = !chkUnlimitedVersion.Checked;
        txtMaximumVersion.Text = chkUnlimitedVersion.Checked ? DefaultAllowedVersion : recordedVersion;

        if (DateTime.TryParse(row["ExpireDate"]?.ToString(), out var expireDate))
            dtExpire.Value = expireDate.Date;

        if (DateTime.TryParse(row["SaleDate"]?.ToString(), out var saleDate))
            dtSaleDate.Value = saleDate.Date;

        var amount = Convert.ToDecimal(row["SaleAmount"] ?? 0, CultureInfo.InvariantCulture);
        numAmount.Value = Math.Clamp(amount, numAmount.Minimum, numAmount.Maximum);

        var days = Convert.ToInt32(row["DurationDays"] ?? 1, CultureInfo.InvariantCulture);
        txtDays.Value = Math.Clamp(days, (int)txtDays.Minimum, (int)txtDays.Maximum);
    }

    private void SeciliSatisiGuncelle()
    {
        if (_selectedSaleId is null)
        {
            MessageBox.Show("Guncellemek icin listeden bir kayit secin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedRow = GetSelectedGridRow();
        if (selectedRow?.DataBoundItem is not DataRowView rowView)
        {
            MessageBox.Show("Guncellemek icin gecerli bir kayit secin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var source = rowView.Row;
        var sourceModules = source["Modules"]?.ToString()?.Trim() ?? string.Empty;
        var selectedModules = moduleSelection.CheckedItems.Count == 0 ? string.Empty : SelectedModules();
        var sourceVersion = source["AllowedVersion"]?.ToString()?.Trim() ?? string.Empty;
        if (!string.Equals(sourceModules, selectedModules, StringComparison.Ordinal)
            || !string.Equals(sourceVersion, GetMaximumVersion(), StringComparison.Ordinal))
        {
            MessageBox.Show(this,
                "Satış kaydını düzenleme modül veya sürüm hakkını değiştirmez. Hakları imzalı lisansa geçirmek için 'Seçili Lisansı Modüllü Yeniden Bas' akışını kullanın.",
                "İmzalı lisans yeniden basımı gerekli", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!ValidateMandatoryFields(requireModules: false))
            return;

        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
            UPDATE Licenses
            SET FirmaKodu = $f,
                MachineId = $m,
                ExpireDate = $e,
                DurationDays = $d,
                ContactPhone = $p,
                SaleDate = $sd,
                SaleAmount = $sa
            WHERE Id = $id";

        cmd.Parameters.AddWithValue("$f", NormalizeFirmaKodu(txtFirma.Text));
        cmd.Parameters.AddWithValue("$m", NormalizeMachineId(txtMachine.Text));
        cmd.Parameters.AddWithValue("$e", dtExpire.Value.Date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$d", (int)txtDays.Value);
        cmd.Parameters.AddWithValue("$p", NormalizePhone(txtPhone.Text));
        cmd.Parameters.AddWithValue("$sd", dtSaleDate.Value.Date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$sa", numAmount.Value);
        cmd.Parameters.AddWithValue("$id", _selectedSaleId.Value);

        var affected = cmd.ExecuteNonQuery();
        if (affected <= 0)
        {
            MessageBox.Show("Kayit guncellenemedi.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        UpdateHistorySummary();
        LoadData();
        SelectGridRowById(_selectedSaleId.Value);
        UpdateActionButtons();
        MessageBox.Show("Secili lisans kaydi guncellendi.", "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SeciliSatisiSil()
    {
        if (_selectedSaleId is null)
        {
            MessageBox.Show("Silmek icin listeden bir kayit secin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedRow = GetSelectedGridRow();
        if (selectedRow?.DataBoundItem is not DataRowView)
        {
            MessageBox.Show("Silmek icin gecerli bir kayit secin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var operationType = selectedRow.Cells["OperationType"].Value?.ToString() ?? string.Empty;
        var isSale = string.Equals(operationType, "Sale", StringComparison.OrdinalIgnoreCase);

        var confirmMessage = isSale
            ? "Secili ana lisans ve bagli yenileme kayitlari silinecek. Devam etmek istiyor musunuz?"
            : "Secili yenileme kaydi silinecek. Devam etmek istiyor musunuz?";

        var confirm = MessageBox.Show(confirmMessage, "Kayit Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
            return;

        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();
        using var tx = con.BeginTransaction();

        if (isSale)
        {
            using var renewDelete = con.CreateCommand();
            renewDelete.Transaction = tx;
            renewDelete.CommandText = "DELETE FROM Licenses WHERE ParentLicenseId = $id";
            renewDelete.Parameters.AddWithValue("$id", _selectedSaleId.Value);
            renewDelete.ExecuteNonQuery();
        }

        using (var deleteCmd = con.CreateCommand())
        {
            deleteCmd.Transaction = tx;
            deleteCmd.CommandText = "DELETE FROM Licenses WHERE Id = $id";
            deleteCmd.Parameters.AddWithValue("$id", _selectedSaleId.Value);
            deleteCmd.ExecuteNonQuery();
        }

        tx.Commit();
        _selectedSaleId = null;
        UpdateHistorySummary();
        LoadData();
        lstRenewals.Items.Clear();
        UpdateActionButtons();

        MessageBox.Show(isSale ? "Ana lisans kaydi ve bagli yenilemeler silindi." : "Lisans kaydi silindi.", "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SatisDokumunuDisaAktar()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "CSV Dosyasi (*.csv)|*.csv",
            FileName = $"satis-dokumu-{DateTime.Now:yyyyMMdd-HHmm}.csv"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
            SELECT
                s.Id,
                s.FirmaKodu,
                s.MachineId,
                s.SaleDate,
                s.SaleAmount,
                s.ExpireDate,
                s.DurationDays,
                s.ContactPhone,
                COUNT(r.Id) AS RenewalCount
            FROM Licenses s
            LEFT JOIN Licenses r ON r.ParentLicenseId = s.Id AND r.OperationType = 'Renewal'
            WHERE s.OperationType = 'Sale'
            GROUP BY s.Id, s.FirmaKodu, s.MachineId, s.SaleDate, s.SaleAmount, s.ExpireDate, s.DurationDays, s.ContactPhone
            ORDER BY s.Id DESC";

        using var reader = cmd.ExecuteReader();
        var sb = new StringBuilder();
        sb.AppendLine("No;Firma;Makine;SatisTarihi;Tutar;Bitis;SureGun;Telefon;YenilemeAdedi");

        while (reader.Read())
        {
            sb.AppendLine(string.Join(";", new[]
            {
                reader[0]?.ToString() ?? string.Empty,
                reader[1]?.ToString() ?? string.Empty,
                reader[2]?.ToString() ?? string.Empty,
                reader[3]?.ToString() ?? string.Empty,
                reader[4]?.ToString() ?? string.Empty,
                reader[5]?.ToString() ?? string.Empty,
                reader[6]?.ToString() ?? string.Empty,
                reader[7]?.ToString() ?? string.Empty,
                reader[8]?.ToString() ?? "0"
            }));
        }

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show("Satis dokumu disa aktarildi.", "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LoadRenewalHistoryForSelected()
    {
        lstRenewals.Items.Clear();

        if (GetSelectedGridRow()?.DataBoundItem is not DataRowView rowView)
            return;

        var selected = rowView.Row;
        var selectedId = Convert.ToInt32(selected["Id"], CultureInfo.InvariantCulture);
        var operation = selected["OperationType"]?.ToString() ?? "Sale";

        var rootId = selectedId;
        if (operation == "Renewal" && selected["ParentLicenseId"] != DBNull.Value)
            rootId = Convert.ToInt32(selected["ParentLicenseId"], CultureInfo.InvariantCulture);

        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, CreatedAt, RemainingDaysAtIssue, SaleAmount
            FROM Licenses
            WHERE OperationType = 'Renewal' AND ParentLicenseId = $rootId
            ORDER BY Id DESC";
        cmd.Parameters.AddWithValue("$rootId", rootId);

        using var reader = cmd.ExecuteReader();
        var hasRow = false;
        while (reader.Read())
        {
            hasRow = true;
            var id = reader.GetInt32(0);
            var created = reader.IsDBNull(1) ? "-" : reader.GetString(1);
            var days = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            var amount = reader.IsDBNull(3) ? 0 : reader.GetDouble(3);
            lstRenewals.Items.Add($"Yenileme #{id} | Tarih: {created} | Gun: {days} | Tutar: {amount:N2}");
        }

        if (!hasRow)
            lstRenewals.Items.Add("Bu kayit icin yenileme gecmisi bulunamadi.");
    }

    private void UpdateActionButtons()
    {
        var hasSelection = _selectedSaleId.HasValue;
        btnUpdateSale.Enabled = hasSelection;
        btnDeleteSale.Enabled = hasSelection;
        btnRenewRemaining.Enabled = hasSelection;
    }

    private void ConfigureGridContextMenu()
    {
        gridContextMenu.Items.Clear();
        gridContextMenu.Items.Add(new ToolStripMenuItem("Seçili Kaydı Düzenle", null, (s, e) => SeciliSatisiGuncelle()));
        gridContextMenu.Items.Add(new ToolStripMenuItem("Seçili Kayıttan Yenileme Üret", null, (s, e) => YenileKalanGunle()));
        gridContextMenu.Items.Add(new ToolStripMenuItem("Seçili Lisansı Modüllü Yeniden Bas", null, (s, e) => ReissueSelectedLicenseAsV2()));
        gridContextMenu.Items.Add(new ToolStripSeparator());
        gridContextMenu.Items.Add(new ToolStripMenuItem("Seçili Kaydı Sil", null, (s, e) => SeciliSatisiSil()));
        grid.ContextMenuStrip = gridContextMenu;
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete && btnDeleteSale.Enabled)
        {
            SeciliSatisiSil();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.F2 && btnUpdateSale.Enabled)
        {
            SeciliSatisiGuncelle();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void Grid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete && btnDeleteSale.Enabled)
        {
            SeciliSatisiSil();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (e.KeyCode == Keys.F2 && btnUpdateSale.Enabled)
        {
            SeciliSatisiGuncelle();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void Grid_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
            return;

        var hit = grid.HitTest(e.X, e.Y);
        if (hit.RowIndex < 0 || hit.RowIndex >= grid.Rows.Count)
            return;

        grid.ClearSelection();
        var row = grid.Rows[hit.RowIndex];
        row.Selected = true;
        if (row.Cells["Id"].Visible)
            grid.CurrentCell = row.Cells["Id"];

        LoadSelectedSaleToForm();
        LoadRenewalHistoryForSelected();
        UpdateActionButtons();
    }

    private void UpdateHistorySummary()
    {
        try
        {
            using var con = new SqliteConnection($"Data Source={_dbPath}");
            con.Open();

            using var cmd = con.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    COALESCE(SUM(CASE WHEN OperationType = 'Sale' THEN SaleAmount ELSE 0 END), 0),
                    SUM(CASE WHEN OperationType = 'Sale' THEN 1 ELSE 0 END),
                    SUM(CASE WHEN OperationType = 'Renewal' THEN 1 ELSE 0 END),
                    COALESCE(SUM(CASE WHEN OperationType = 'Sale' AND SaleDate >= $monthStart THEN SaleAmount ELSE 0 END), 0)
                FROM Licenses";

            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).ToString("yyyy-MM-dd");
            cmd.Parameters.AddWithValue("$monthStart", monthStart);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var totalSales = reader.IsDBNull(0) ? 0m : Convert.ToDecimal(reader.GetDouble(0));
                var saleCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                var renewCount = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                var monthSales = reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetDouble(3));

                lblQuickInfo.Text = $"Toplam satis: {totalSales:N2} TL | Ilk satis: {saleCount} | Yenileme: {renewCount} | Bu ay: {monthSales:N2} TL";
            }
        }
        catch (Exception ex)
        {
            lblQuickInfo.Text = $"Ozet yuklenemedi: {ex.Message}";
        }
    }

    private DataGridViewRow? GetSelectedGridRow()
    {
        return grid.CurrentRow is { IsNewRow: false } currentRow
            ? currentRow
            : grid.SelectedRows.Count > 0
                ? grid.SelectedRows[0]
                : null;
    }

    private void SelectGridRowById(int id)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.Cells["Id"].Value is null)
                continue;

            if (Convert.ToInt32(row.Cells["Id"].Value, CultureInfo.InvariantCulture) != id)
                continue;

            row.Selected = true;
            grid.CurrentCell = row.Cells["Id"];
            return;
        }
    }
}
