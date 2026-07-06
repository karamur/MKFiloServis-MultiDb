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
    private const string SECRET = "MKFiloServis-LCNS-2026-SECURE-KEY-X9mK2pL5vR8w";
    private const string AllowedVersion = "1.0.99";
    private readonly string _dbPath;

    private Panel pnlHeader = new() { Height = 88, BackColor = Color.FromArgb(19, 33, 68) };
    private Label lblTitle = new() { Text = "MKFiloServis Lisans Yonetim Merkezi", AutoSize = true, Font = new Font("Segoe UI Semibold", 19, FontStyle.Bold), ForeColor = Color.White };
    private Label lblSubtitle = new() { Text = "Lisans, yenileme ve paketleme islemlerini tek ekrandan, tutarli ve hizli sekilde yonetin.", AutoSize = true, Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(208, 220, 255) };
    private Label lblStatusBadge = new() { Text = "WEB UYUMLU", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(34, 145, 92), Padding = new Padding(10, 6, 10, 6) };

    private GroupBox grpLicenseEditor = new() { Text = "Lisans Olusturma" };
    private GroupBox grpOutput = new() { Text = "Anahtar Ciktisi" };
    private GroupBox grpPackaging = new() { Text = "Kurulum / Guncelleme Paketleri" };
    private GroupBox grpHistory = new() { Text = "Lisans Gecmisi" };
    private Panel pnlQuickInfo = new() { BorderStyle = BorderStyle.FixedSingle, BackColor = Color.FromArgb(245, 248, 255) };
    private Label lblQuickInfoTitle = new() { Text = "Hizli Bilgi", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(19, 33, 68) };
    private Label lblQuickInfo = new() { Text = "Firma kodu, machine ID ve iletisim telefonu dogruysa uretilen anahtar web uygulamasinda dogrudan aktive edilir.", AutoSize = false, ForeColor = Color.FromArgb(58, 68, 88) };
    private Label lblHistoryHint = new() { Text = "Arama, filtreleme ve yenileme gecmisini buradan yonetin.", AutoSize = true, ForeColor = Color.FromArgb(85, 96, 122) };
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

    private Label lblHistory = new() { Text = "Lisans Listesi", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
    private Label lblSearch = new() { Text = "Arama", AutoSize = true };
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
    private bool _isLoadingSelection;
    private bool _suppressHistoryRefresh;

    public MainForm()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbDir = Path.Combine(appData, "MKFiloServis");
        Directory.CreateDirectory(dbDir);
        _dbPath = Path.Combine(dbDir, "licenses.db");

        Text = "MKFiloServis Lisans Yonetim Merkezi";
        Width = 1420;
        Height = 920;
        MinimumSize = new Size(1340, 860);
        BackColor = Color.FromArgb(236, 240, 248);
        Font = new Font("Segoe UI", 9f);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;

        InitializeLayout();
        ApplyProfessionalTheme();

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
            if (_isLoadingSelection)
                return;

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

        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Padding = new Padding(22, 16, 22, 14);
        Controls.Add(pnlHeader);

        lblTitle.Location = new Point(18, 10);
        lblSubtitle.Location = new Point(20, 46);
        lblStatusBadge.Location = new Point(1120, 24);
        lblStatusBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, lblStatusBadge });

        pnlQuickInfo.Padding = new Padding(2);
        grpLicenseEditor.Padding = new Padding(12, 18, 12, 12);
        grpOutput.Padding = new Padding(12, 18, 12, 12);
        grpPackaging.Padding = new Padding(12, 18, 12, 12);
        grpHistory.Padding = new Padding(12, 18, 12, 12);
        grpLicenseEditor.FlatStyle = FlatStyle.Standard;
        grpOutput.FlatStyle = FlatStyle.Standard;
        grpPackaging.FlatStyle = FlatStyle.Standard;
        grpHistory.FlatStyle = FlatStyle.Standard;

        grpLicenseEditor.Location = new Point(16, 104);
        grpLicenseEditor.Size = new Size(480, 408);
        grpLicenseEditor.Anchor = AnchorStyles.Top | AnchorStyles.Left;

        grpOutput.Location = new Point(16, 514);
        grpOutput.Size = new Size(480, 146);
        grpOutput.Anchor = AnchorStyles.Top | AnchorStyles.Left;

        grpPackaging.Location = new Point(16, 670);
        grpPackaging.Size = new Size(480, 210);
        grpPackaging.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

        grpHistory.Location = new Point(510, 104);
        grpHistory.Size = new Size(890, 774);
        grpHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        pnlQuickInfo.Location = new Point(20, 28);
        pnlQuickInfo.Size = new Size(438, 62);
        lblQuickInfoTitle.Location = new Point(14, 10);
        lblQuickInfo.Location = new Point(14, 28);
        lblQuickInfo.Size = new Size(408, 24);
        pnlQuickInfo.Controls.AddRange(new Control[] { lblQuickInfoTitle, lblQuickInfo });

        lblFirma.Location = new Point(20, 106);
        txtFirma.Location = new Point(20, 126);
        txtFirma.Width = 205;

        lblPhone.Location = new Point(246, 106);
        txtPhone.Location = new Point(246, 126);
        txtPhone.Width = 212;

        lblMachine.Location = new Point(20, 164);
        txtMachine.Location = new Point(20, 184);
        txtMachine.Width = 438;

        lblDays.Location = new Point(20, 248);
        txtDays.Location = new Point(20, 268);

        lblExpire.Location = new Point(156, 248);
        dtExpire.Location = new Point(156, 268);

        lblSaleDate.Location = new Point(20, 308);
        dtSaleDate.Location = new Point(20, 328);

        lblAmount.Location = new Point(156, 308);
        numAmount.Location = new Point(156, 328);

        btnUret.Location = new Point(20, 360);
        btnUret.Width = 438;

        btnUpdateSale.Location = new Point(20, 408);
        btnUpdateSale.Width = 208;

        btnDeleteSale.Location = new Point(250, 408);
        btnDeleteSale.Width = 208;

        btnRenewRemaining.Location = new Point(20, 452);
        btnRenewRemaining.Width = 438;

        grpLicenseEditor.Controls.AddRange(new Control[]
        {
            pnlQuickInfo,
            lblFirma, txtFirma,
            lblPhone, txtPhone,
            lblMachine, txtMachine,
            lblDays, txtDays,
            lblExpire, dtExpire,
            lblSaleDate, dtSaleDate,
            lblAmount, numAmount,
            btnUret, btnUpdateSale, btnDeleteSale, btnRenewRemaining
        });

        lblKey.Location = new Point(20, 34);
        txtKey.Location = new Point(20, 56);
        txtKey.Width = 438;
        btnCopy.Location = new Point(20, 120);
        btnCopy.Width = 438;
        grpOutput.Controls.AddRange(new Control[] { lblKey, txtKey, btnCopy });

        lblUpdateZip.Location = new Point(20, 26);
        txtUpdateSourceFolder.Location = new Point(20, 52);
        txtUpdateSourceFolder.Width = 316;
        btnSelectUpdateSource.Location = new Point(344, 50);
        btnSelectUpdateSource.Width = 114;

        txtUpdateVersion.Location = new Point(20, 88);
        txtUpdateVersion.Width = 438;

        txtUpdateOutputFolder.Location = new Point(20, 124);
        txtUpdateOutputFolder.Width = 316;
        btnSelectUpdateOutput.Location = new Point(344, 122);
        btnSelectUpdateOutput.Width = 114;

        btnCreateUpdateZip.Location = new Point(20, 164);
        btnCreateUpdateZip.Width = 214;

        btnPrepareCustomerSetup.Location = new Point(244, 164);
        btnPrepareCustomerSetup.Width = 214;

        grpPackaging.Controls.AddRange(new Control[]
        {
            lblUpdateZip,
            txtUpdateSourceFolder, btnSelectUpdateSource,
            txtUpdateVersion,
            txtUpdateOutputFolder, btnSelectUpdateOutput,
            btnCreateUpdateZip, btnPrepareCustomerSetup
        });

        lblHistory.Location = new Point(18, 28);
        lblSearch.Location = new Point(18, 60);
        txtSearch.Location = new Point(18, 80);
        txtSearch.Width = 330;

        cmbOperationFilter.Location = new Point(360, 80);
        cmbOperationFilter.Width = 190;
        cmbOperationFilter.Items.AddRange(new object[] { "Sadece Satislar", "Tum Islemler" });
        cmbOperationFilter.SelectedIndex = 0;

        btnExportReport.Location = new Point(714, 78);
        btnExportReport.Width = 150;
        btnExportReport.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        grid.Location = new Point(18, 122);
        grid.Size = new Size(854, 500);
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        lblRenewalHistory.Location = new Point(18, 636);
        lstRenewals.Location = new Point(18, 660);
        lstRenewals.Size = new Size(854, 92);
        lstRenewals.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

        lblHistoryHint.Location = new Point(18, 48);

        grpHistory.Controls.AddRange(new Control[]
        {
            lblHistory, lblHistoryHint, lblSearch, txtSearch, cmbOperationFilter, btnExportReport, grid, lblRenewalHistory, lstRenewals
        });

        Controls.AddRange(new Control[]
        {
            grpLicenseEditor, grpOutput, grpPackaging, grpHistory
        });

        dtExpire.Value = DateTime.Now.AddDays((int)txtDays.Value);
        dtSaleDate.Value = DateTime.Today;
        txtUpdateOutputFolder.Text = GetDefaultSetupOutputRoot();
        txtUpdateVersion.Text = "1.0.0";

        txtDays.ValueChanged += (s, e) =>
        {
            if (_syncing) return;
            _syncing = true;
            dtExpire.Value = DateTime.Now.AddDays((int)txtDays.Value);
            _syncing = false;
        };

        dtExpire.ValueChanged += (s, e) =>
        {
            if (_syncing) return;
            _syncing = true;
            var days = Math.Max(1, (dtExpire.Value.Date - DateTime.Now.Date).Days);
            txtDays.Value = Math.Min(days, 3650);
            _syncing = false;
        };

        ResumeLayout(false);
    }

    private void ApplyProfessionalTheme()
    {
        foreach (var box in new[] { grpLicenseEditor, grpOutput, grpPackaging, grpHistory })
        {
            box.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            box.ForeColor = Color.FromArgb(27, 40, 67);
            box.BackColor = Color.White;
        }

        foreach (var input in new Control[] { txtFirma, txtMachine, txtPhone, txtKey, txtSearch, txtUpdateSourceFolder, txtUpdateVersion, txtUpdateOutputFolder, txtDays, dtExpire, dtSaleDate, numAmount, cmbOperationFilter })
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
        int remainingDaysAtIssue)
    {
        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Licenses
            (
                FirmaKodu, MachineId, ExpireDate, CreatedAt, AllowedVersion,
                DurationDays, ContactPhone, SaleDate, SaleAmount, OperationType,
                ParentLicenseId, RemainingDaysAtIssue
            )
            VALUES
            (
                $f, $m, $e, $c, $v,
                $d, $p, $sd, $sa, $ot,
                $parent, $remaining
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
                    AllowedVersion
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
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (grid.Columns[e.ColumnIndex].Name != "KalanGun")
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

            var key = BuildLicenseKey(firma, machine, expire, durationDays, phone, created);
            SaveLicense(
                firma,
                machine,
                expire,
                created,
                AllowedVersion,
                durationDays,
                phone,
                dtSaleDate.Value.Date,
                numAmount.Value,
                operationType: "Sale",
                parentLicenseId: null,
                remainingDaysAtIssue: durationDays);

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

            var key = BuildLicenseKey(firma, machine, expire, remainingDays, phone, created);
            var newId = SaveLicense(
                firma,
                machine,
                expire,
                created,
                AllowedVersion,
                remainingDays,
                phone,
                dtSaleDate.Value.Date,
                numAmount.Value,
                operationType: "Renewal",
                parentLicenseId: parentId,
                remainingDaysAtIssue: remainingDays);

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

    private bool ValidateMandatoryFields()
    {
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
        => (value ?? string.Empty).Trim().ToUpperInvariant();

    /// <summary>
    /// Machine ID icindeki TUM whitespace karakterlerini (satir sonu, tab, bosluk) temizler.
    /// Multiline kutuya yapistirilan makine kodlarindaki gizli satir sonlari lisans imzasini bozuyordu.
    /// </summary>
    private static string NormalizeMachineId(string? value)
        => string.Concat((value ?? string.Empty).Where(c => !char.IsWhiteSpace(c)));

    /// <summary>
    /// Telefon alanindaki whitespace karakterlerini temizler.
    /// Format karakterlerini korur, sadece yapistirma kaynakli gizli bosluklari atar.
    /// </summary>
    private static string NormalizePhone(string? value)
        => string.Concat((value ?? string.Empty).Where(c => !char.IsWhiteSpace(c)));

    private static string BuildLicenseKey(string firma, string machine, DateTime expire, int durationDays, string phone, DateTime created)
    {
        const bool isDemo = false;
        firma = NormalizeFirmaKodu(firma);
        machine = NormalizeMachineId(machine);
        phone = NormalizePhone(phone);

        var raw = $"{firma}|{machine}|{expire:yyyy-MM-dd}|{durationDays}|{isDemo}|{AllowedVersion}|{created:yyyy-MM-dd}|{phone}|{SECRET}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var signature = Convert.ToBase64String(hash);

        var json = JsonSerializer.Serialize(new
        {
            FirmaKodu = firma,
            MachineId = machine,
            ExpireDate = expire,
            DurationDays = durationDays,
            AllowedVersion,
            IsDemo = isDemo,
            CreatedAt = created,
            ContactPhone = phone,
            Signature = signature
        });

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private void ShowKeyAndRefresh(string key)
    {
        txtKey.Text = key;
        txtKey.SelectAll();
        btnCopy.Enabled = true;
        Clipboard.SetText(key);
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
            ($"MKFiloServisKurulum-{version}.exe", FindLatest(p => Path.GetFileName(p).StartsWith("MKFiloServisKurulum", StringComparison.OrdinalIgnoreCase) && !Path.GetFileName(p).StartsWith("MKFiloServisKurulumMusteri", StringComparison.OrdinalIgnoreCase))),
            ($"MKLisansArac-{version}.exe", FindLatest(p => Path.GetFileName(p).StartsWith("MKLisansArac", StringComparison.OrdinalIgnoreCase)))
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
            var versionFolder = Path.Combine(outputRoot, $"v{cleanVersion}");

            Directory.CreateDirectory(outputRoot);
            Directory.CreateDirectory(versionFolder);

            var firma = NormalizeFirmaKodu(txtFirma.Text);
            var machine = NormalizeMachineId(txtMachine.Text);
            var phone = NormalizePhone(txtPhone.Text);
            var created = DateTime.UtcNow;
            var expire = dtExpire.Value.Date;
            var durationDays = (int)txtDays.Value;

            var key = BuildLicenseKey(firma, machine, expire, durationDays, phone, created);

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
                AllowedVersion,
                durationDays,
                phone,
                dtSaleDate.Value.Date,
                numAmount.Value,
                operationType: "Sale",
                parentLicenseId: null,
                remainingDaysAtIssue: durationDays);

            txtUpdateSourceFolder.Text = sourceFolder;
            txtUpdateOutputFolder.Text = outputRoot;

            CreateUpdateZipPackage();
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
        if (selectedRow?.DataBoundItem is not DataRowView)
        {
            MessageBox.Show("Guncellemek icin gecerli bir kayit secin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!ValidateMandatoryFields())
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
