using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

/// <summary>
/// KOAFiloServis Lisans Aktivasyon Key Üretici + Takip Sistemi.
/// Web'deki LicenseService.ActivateFromKeyAsync() ile %100 uyumlu.
/// Üretilen key = Base64(JSON) → Web'e yapıştır → Aktive olur.
/// Tüm lisanslar local SQLite DB'de kayıt altında.
/// </summary>
public class MainForm : Form
{
    // ══════════════════════════════════════════════
    // AYNI SECRET — LicenseService.cs ile birebir
    // ══════════════════════════════════════════════
    private const string SECRET = "KOAFiloServis-LCNS-2026-SECURE-KEY-X9mK2pL5vR8w";
    private readonly string _dbPath;

    private TextBox txtFirma = new() { PlaceholderText = "Firma Kodu (örn: DEMO)" };
    private TextBox txtMachine = new() { PlaceholderText = "MachineId (web'den kopyala)" };
    private Button btnUret = new() { Text = " Lisans Oluştur" };
    private Label lblInfo = new() { Text = "Web uygulamasından MachineId'yi kopyalayıp yapıştırın.", AutoSize = true };
    private DataGridView grid = new()
    {
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        ReadOnly = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false
    };

    public MainForm()
    {
        _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "licenses.db");

        Text = "KOAFiloServis Lisans Aktivasyon & Takip";
        Width = 820;
        Height = 570;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;

        // ── Sol panel: Giriş + Buton ──
        // Firma Kodu
        txtFirma.Top = 15;
        txtFirma.Left = 15;
        txtFirma.Width = 350;

        // MachineId
        txtMachine.Top = 50;
        txtMachine.Left = 15;
        txtMachine.Width = 350;

        // Buton
        btnUret.Top = 85;
        btnUret.Left = 15;
        btnUret.Width = 350;
        btnUret.Height = 35;

        // Bilgi label
        lblInfo.Top = 130;
        lblInfo.Left = 15;
        lblInfo.AutoSize = true;

        // ── Sağ panel: Grid ──
        grid.Top = 15;
        grid.Left = 400;
        grid.Width = 390;
        grid.Height = 490;
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        Controls.Add(txtFirma);
        Controls.Add(txtMachine);
        Controls.Add(btnUret);
        Controls.Add(lblInfo);
        Controls.Add(grid);

        btnUret.Click += Uret;
        grid.CellFormatting += Grid_CellFormatting;

        // DB başlat + veriyi yükle
        InitDatabase();
        LoadData();
    }

    // ══════════════════════════════════════════════
    // SQLITE VERITABANI
    // ══════════════════════════════════════════════

    private void InitDatabase()
    {
        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();

        using var cmd = con.CreateCommand();
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
    }

    private void SaveLicense(string firma, string machine, DateTime expire, DateTime created, string allowedVersion)
    {
        using var con = new SqliteConnection($"Data Source={_dbPath}");
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Licenses (FirmaKodu, MachineId, ExpireDate, CreatedAt, AllowedVersion)
            VALUES ($f, $m, $e, $c, $v)";
        cmd.Parameters.AddWithValue("$f", firma);
        cmd.Parameters.AddWithValue("$m", machine);
        cmd.Parameters.AddWithValue("$e", expire.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$c", created.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$v", allowedVersion);
        cmd.ExecuteNonQuery();
    }

    private void LoadData()
    {
        try
        {
            using var con = new SqliteConnection($"Data Source={_dbPath}");
            con.Open();

            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Id, FirmaKodu, MachineId, ExpireDate, CreatedAt, AllowedVersion FROM Licenses ORDER BY Id DESC";

            using var reader = cmd.ExecuteReader();
            var dt = new DataTable();
            dt.Load(reader);

            grid.DataSource = dt;

            // Kolon başlıklarını Türkçeleştir
            if (dt.Columns.Count > 0)
            {
                grid.Columns["Id"]!.HeaderText = "No";
                grid.Columns["Id"]!.FillWeight = 10;
                grid.Columns["FirmaKodu"]!.HeaderText = "Firma";
                grid.Columns["FirmaKodu"]!.FillWeight = 20;
                grid.Columns["MachineId"]!.HeaderText = "Makine";
                grid.Columns["MachineId"]!.FillWeight = 40;
                grid.Columns["ExpireDate"]!.HeaderText = "Bitiş";
                grid.Columns["ExpireDate"]!.FillWeight = 15;
                grid.Columns["CreatedAt"]!.HeaderText = "Oluşturma";
                grid.Columns["CreatedAt"]!.FillWeight = 15;
                grid.Columns["AllowedVersion"]!.HeaderText = "Sürüm";
                grid.Columns["AllowedVersion"]!.FillWeight = 10;
            }
        }
        catch (Exception ex)
        {
            // DB yoksa veya bozulmuşsa sessiz kal
            System.Diagnostics.Trace.WriteLine($"[MainForm] LoadData error: {ex.Message}");
        }
    }

    // ══════════════════════════════════════════════
    // RENK KODLAMASI
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
    // LİSANS ÜRETİM
    // ══════════════════════════════════════════════

    /// <summary>
    /// Lisans aktivasyon key'i üretir, DB'ye kaydeder, listeyi günceller.
    /// Format: Base64(JSON{FirmaKodu, MachineId, ExpireDate, IsDemo, AllowedVersion, CreatedAt, Signature})
    /// Signature = SHA256(FirmaKodu|MachineId|ExpireDate|IsDemo|AllowedVersion|CreatedAt|SECRET)
    /// </summary>
    void Uret(object? sender, EventArgs e)
    {
        var firma = txtFirma.Text.Trim();
        var machine = txtMachine.Text.Trim();

        if (string.IsNullOrWhiteSpace(firma))
        {
            MessageBox.Show("Firma kodunu giriniz.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(machine))
        {
            MessageBox.Show("MachineId'yi web uygulamasından kopyalayıp yapıştırın.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var expire = DateTime.UtcNow.AddYears(1);
            var created = DateTime.UtcNow;
            const string allowedVersion = "1.0.99";
            const bool isDemo = false;

            // SIGNATURE — LicenseService.GenerateSignature() ile birebir
            var raw = $"{firma}|{machine}|{expire:yyyy-MM-dd}|{isDemo}|{allowedVersion}|{created:yyyy-MM-dd}|{SECRET}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            var signature = Convert.ToBase64String(hash);

            // JSON — LicenseInfo entity'si ile uyumlu
            var json = JsonSerializer.Serialize(new
            {
                FirmaKodu = firma,
                MachineId = machine,
                ExpireDate = expire,
                AllowedVersion = allowedVersion,
                IsDemo = isDemo,
                CreatedAt = created,
                Signature = signature
            });

            // Base64(JSON) → Aktivasyon anahtarı
            var key = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            // DB'ye kaydet
            SaveLicense(firma, machine, expire, created, allowedVersion);

            // Panoya kopyala
            Clipboard.SetText(key);

            // Listeyi güncelle
            LoadData();

            MessageBox.Show(
                $"✔ Aktivasyon kodu panoya kopyalandı ve DB'ye kaydedildi.\n\n" +
                $"Firma: {firma}\n" +
                $"Makine: {machine}\n" +
                $"Bitiş:  {expire:yyyy-MM-dd}\n" +
                $"Sürüm:  {allowedVersion}\n\n" +
                $"Web uygulamasına yapıştırın.",
                "Başarılı",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
