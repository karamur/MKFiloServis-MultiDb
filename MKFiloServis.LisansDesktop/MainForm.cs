using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

/// <summary>
/// MKFiloServis Lisans Aktivasyon Key Uretici + Takip Sistemi.
/// Web'deki LicenseService.ActivateFromKeyAsync() ile %100 uyumlu.
/// Uretilen key ekranda GORUNUR, panoya kopyalanir, DB'ye kaydedilir.
///
/// 🔥 LISANS MODEL (LicenseInfo ile BIREBIR AYNI — MKFiloServis.Shared.Entities.LicenseInfo):
///   FirmaKodu, MachineId, ExpireDate, DurationDays, AllowedVersion, IsDemo, CreatedAt, ContactPhone, Signature
///
/// 🔥 SIGNATURE (LicenseService.GenerateSignature ile BIREBIR AYNI):
///   raw = $"{firma}|{machine}|{expire:yyyy-MM-dd}|{days}|{isDemo}|{version}|{created:yyyy-MM-dd}|{phone}|{SECRET}"
///   SHA256(raw) → Base64
/// </summary>
public class MainForm : Form
{
    private const string SECRET = "MKFiloServis-LCNS-2026-SECURE-KEY-X9mK2pL5vR8w";
    private readonly string _dbPath;

    // ── Giris alanlari ──
    private Label lblFirma = new() { Text = "Firma Kodu:", AutoSize = true };
    private TextBox txtFirma = new() { PlaceholderText = "orn: USTUN" };
    private Label lblMachine = new() { Text = "Machine ID:", AutoSize = true };
    private TextBox txtMachine = new() { PlaceholderText = "Web'den kopyalayip yapistirin", Multiline = true, Height = 40 };

    // ── Sure / Tarih (senkron) ──
    private Label lblDays = new() { Text = "Lisans Suresi (Gun):", AutoSize = true };
    private NumericUpDown txtDays = new() { Minimum = 1, Maximum = 3650, Value = 365, Width = 100 };
    private Label lblExpire = new() { Text = "Bitis Tarihi:", AutoSize = true };
    private DateTimePicker dtExpire = new() { Format = DateTimePickerFormat.Short, Width = 150 };

    // ── Telefon ──
    private Label lblPhone = new() { Text = "Iletisim Telefonu:", AutoSize = true };
    private TextBox txtPhone = new() { PlaceholderText = "orn: 0555xxxxxxx" };

    private Button btnUret = new() { Text = "Lisans Olustur", Height = 40 };

    // ── Cikti alani — LISANS ANAHTARI BURADA GORUNUR ──
    private Label lblKey = new() { Text = "Uretilen Lisans Anahtari:", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
    private TextBox txtKey = new()
    {
        ReadOnly = true,
        Multiline = true,
        Height = 80,
        BackColor = Color.LightYellow,
        Font = new Font("Consolas", 9),
        Text = "Henuz lisans uretilmedi."
    };
    private Button btnCopy = new() { Text = "Panoya Kopyala", Enabled = false, Height = 35 };

    // ── Gecmis tablosu ──
    private DataGridView grid = new()
    {
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        ReadOnly = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false
    };
    private Label lblHistory = new() { Text = "Gecmis:", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };

    private bool _syncing; // Re-entrancy guard for days ↔ date sync

    public MainForm()
    {
        // DB'yi yazilabilir yere koy
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbDir = Path.Combine(appData, "MKFiloServis");
        Directory.CreateDirectory(dbDir);
        _dbPath = Path.Combine(dbDir, "licenses.db");

        Text = "MKFiloServis Lisans Uretim Araci";
        Width = 900;
        Height = 750;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;

        int y = 10;
        int leftMargin = 15;
        int inputWidth = 400;

        // Firma
        lblFirma.Location = new Point(leftMargin, y);
        txtFirma.Location = new Point(leftMargin, y + 20);
        txtFirma.Width = inputWidth;
        y += 55;

        // Machine ID
        lblMachine.Location = new Point(leftMargin, y);
        txtMachine.Location = new Point(leftMargin, y + 20);
        txtMachine.Width = inputWidth;
        y += 65;

        // ── Gun / Tarih (yan yana) ──
        lblDays.Location = new Point(leftMargin, y);
        txtDays.Location = new Point(leftMargin, y + 20);
        y += 55;

        lblExpire.Location = new Point(leftMargin + 140, y - 55);
        dtExpire.Location = new Point(leftMargin + 140, y - 35);
        dtExpire.Value = DateTime.Now.AddDays((int)txtDays.Value);

        // Senkronizasyon: gun degisince tarih guncelle, tarih degisince gun guncelle
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

        // ── Telefon ──
        lblPhone.Location = new Point(leftMargin, y);
        txtPhone.Location = new Point(leftMargin, y + 20);
        txtPhone.Width = 250;
        y += 55;

        // Buton
        btnUret.Location = new Point(leftMargin, y);
        btnUret.Width = inputWidth;
        btnUret.BackColor = Color.SteelBlue;
        btnUret.ForeColor = Color.White;
        btnUret.FlatStyle = FlatStyle.Flat;
        y += 55;

        // ── ANAHTAR CIKTI ALANI ──
        lblKey.Location = new Point(leftMargin, y);
        y += 22;
        txtKey.Location = new Point(leftMargin, y);
        txtKey.Width = inputWidth;
        y += 90;

        btnCopy.Location = new Point(leftMargin, y);
        btnCopy.Width = inputWidth;
        y += 50;

        // ── Sag panel: Gecmis ──
        lblHistory.Location = new Point(440, 10);
        grid.Location = new Point(440, 32);
        grid.Width = 430;
        grid.Height = 650;
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        Controls.AddRange(new Control[] {
            lblFirma, txtFirma,
            lblMachine, txtMachine,
            lblDays, txtDays,
            lblExpire, dtExpire,
            lblPhone, txtPhone,
            btnUret,
            lblKey, txtKey, btnCopy,
            lblHistory, grid
        });

        btnUret.Click += (s, e) => Uret();
        btnCopy.Click += (s, e) => CopyKey();
        grid.CellFormatting += Grid_CellFormatting;

        InitDatabase();
        LoadData();
    }

    // ══════════════════════════════════════════════
    // SQLITE
    // ══════════════════════════════════════════════

    private void InitDatabase()
    {
        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();
        using var cmd = con.CreateCommand();

        // V1 tablo (geriye uyumlu)
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Licenses (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                FirmaKodu       TEXT NOT NULL,
                MachineId       TEXT NOT NULL,
                ExpireDate      TEXT NOT NULL,
                CreatedAt       TEXT NOT NULL,
                AllowedVersion  TEXT NOT NULL DEFAULT '1.0.99'
            )";
        cmd.ExecuteNonQuery();

        // V2 migration: yeni kolonlari ekle (yoksa)
        try
        {
            cmd.CommandText = "ALTER TABLE Licenses ADD COLUMN DurationDays INTEGER NOT NULL DEFAULT 365";
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException) { /* kolon zaten var */ }

        try
        {
            cmd.CommandText = "ALTER TABLE Licenses ADD COLUMN ContactPhone TEXT NOT NULL DEFAULT ''";
            cmd.ExecuteNonQuery();
        }
        catch (SqliteException) { /* kolon zaten var */ }
    }

    private void SaveLicense(string firma, string machine, DateTime expire, DateTime created,
        string allowedVersion, int durationDays, string contactPhone)
    {
        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Licenses (FirmaKodu, MachineId, ExpireDate, CreatedAt, AllowedVersion, DurationDays, ContactPhone)
            VALUES ($f, $m, $e, $c, $v, $d, $p)";
        cmd.Parameters.AddWithValue("$f", firma);
        cmd.Parameters.AddWithValue("$m", machine);
        cmd.Parameters.AddWithValue("$e", expire.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$c", created.ToString("yyyy-MM-dd HH:mm"));
        cmd.Parameters.AddWithValue("$v", allowedVersion);
        cmd.Parameters.AddWithValue("$d", durationDays);
        cmd.Parameters.AddWithValue("$p", contactPhone ?? "");
        cmd.ExecuteNonQuery();
    }

    private void LoadData()
    {
        try
        {
            using var con = new SqliteConnection($"Data Source={_dbPath}");
            con.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Id, FirmaKodu, MachineId, ExpireDate, CreatedAt, AllowedVersion, DurationDays, ContactPhone FROM Licenses ORDER BY Id DESC";
            using var reader = cmd.ExecuteReader();
            var dt = new DataTable();
            dt.Load(reader);
            grid.DataSource = dt;
            if (dt.Columns.Count > 0)
            {
                grid.Columns["Id"]!.HeaderText = "No"; grid.Columns["Id"]!.FillWeight = 6;
                grid.Columns["FirmaKodu"]!.HeaderText = "Firma"; grid.Columns["FirmaKodu"]!.FillWeight = 15;
                grid.Columns["MachineId"]!.HeaderText = "Makine"; grid.Columns["MachineId"]!.FillWeight = 28;
                grid.Columns["ExpireDate"]!.HeaderText = "Bitis"; grid.Columns["ExpireDate"]!.FillWeight = 12;
                grid.Columns["DurationDays"]!.HeaderText = "Gun"; grid.Columns["DurationDays"]!.FillWeight = 7;
                grid.Columns["CreatedAt"]!.HeaderText = "Olusturma"; grid.Columns["CreatedAt"]!.FillWeight = 14;
                grid.Columns["ContactPhone"]!.HeaderText = "Telefon"; grid.Columns["ContactPhone"]!.FillWeight = 14;
                grid.Columns["AllowedVersion"]!.HeaderText = "Surum"; grid.Columns["AllowedVersion"]!.FillWeight = 8;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[MainForm] LoadData: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════
    // RENK
    // ══════════════════════════════════════════════

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (grid.Columns[e.ColumnIndex].Name != "ExpireDate") return;
        if (DateTime.TryParse(e.Value?.ToString(), out var expire))
        {
            var row = grid.Rows[e.RowIndex];
            if (expire < DateTime.Now)
            {
                row.DefaultCellStyle.BackColor = Color.LightCoral;
                row.DefaultCellStyle.ForeColor = Color.DarkRed;
            }
            else if ((expire - DateTime.Now).TotalDays < 7)
            {
                row.DefaultCellStyle.BackColor = Color.Moccasin;
                row.DefaultCellStyle.ForeColor = Color.DarkOrange;
            }
        }
    }

    // ══════════════════════════════════════════════
    // LISANS URET
    // ══════════════════════════════════════════════

    void Uret()
    {
        var firma = txtFirma.Text.Trim();
        var machine = txtMachine.Text.Trim();
        var phone = txtPhone.Text.Trim();

        if (string.IsNullOrWhiteSpace(firma))
        {
            MessageBox.Show("Firma kodunu giriniz.", "Eksik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(machine))
        {
            MessageBox.Show("Machine ID'yi web uygulamasindan kopyalayip yapistirin.", "Eksik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            // 🔥 CRITICAL: UTC kullan
            var created = DateTime.UtcNow;
            var expire = dtExpire.Value.Date; // DateTimePicker'tan gelen tarih (local) — gun bazli karsilastirma icin .Date yeterli
            var durationDays = (int)txtDays.Value;
            const string allowedVersion = "1.0.99";
            const bool isDemo = false;

            // 🔥 SIGNATURE — LicenseService.GenerateSignature() ile BIREBIR AYNI
            // FORMAT: firma|machine|expire(yyyy-MM-dd)|days|isDemo|version|created(yyyy-MM-dd)|phone|SECRET
            var raw = $"{firma}|{machine}|{expire:yyyy-MM-dd}|{durationDays}|{isDemo}|{allowedVersion}|{created:yyyy-MM-dd}|{phone}|{SECRET}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            var signature = Convert.ToBase64String(hash);

            // 🔥 JSON → Base64 (LicenseInfo entity deserialize edilebilir)
            // MUST MATCH: MKFiloServis.Shared.Entities.LicenseInfo
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
            var key = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            // EKRANDA GOSTER
            txtKey.Text = key;
            txtKey.SelectAll();
            btnCopy.Enabled = true;

            // DB + Clipboard + Grid
            SaveLicense(firma, machine, expire, created, allowedVersion, durationDays, phone);
            Clipboard.SetText(key);
            LoadData();

            var kalanGun = Math.Max(0, (expire.Date - DateTime.Now.Date).Days);
            MessageBox.Show(
                $"Lisans anahtari uretildi!\n\n" +
                $"Firma: {firma}\n" +
                $"Bitis: {expire:yyyy-MM-dd}\n" +
                $"Sure: {durationDays} gun ({kalanGun} gun kaldi)\n" +
                $"Telefon: {(string.IsNullOrWhiteSpace(phone) ? "-" : phone)}\n\n" +
                $"Anahtar EKRANDA gosteriliyor ve PANOYA kopyalandi.\nMusteriye gonderebilirsiniz.",
                "Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void CopyKey()
    {
        if (!string.IsNullOrWhiteSpace(txtKey.Text) && txtKey.Text != "Henuz lisans uretilmedi.")
        {
            Clipboard.SetText(txtKey.Text);
            MessageBox.Show("Panoya kopyalandi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}


