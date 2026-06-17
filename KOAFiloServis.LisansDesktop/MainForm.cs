using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

/// <summary>
/// KOAFiloServis Lisans Aktivasyon Key Üretici + Takip Sistemi.
/// Web'deki LicenseService.ActivateFromKeyAsync() ile %100 uyumlu.
/// Üretilen key ekranda GÖRÜNÜR, panoya kopyalanır, DB'ye kaydedilir.
/// </summary>
public class MainForm : Form
{
    private const string SECRET = "KOAFiloServis-LCNS-2026-SECURE-KEY-X9mK2pL5vR8w";
    private readonly string _dbPath;

    // ── Giriş alanları ──
    private Label lblFirma = new() { Text = "Firma Kodu:", AutoSize = true };
    private TextBox txtFirma = new() { PlaceholderText = "örn: USTUN" };
    private Label lblMachine = new() { Text = "Machine ID:", AutoSize = true };
    private TextBox txtMachine = new() { PlaceholderText = "Web'den kopyalayıp yapıştırın", Multiline = true, Height = 40 };
    private Button btnUret = new() { Text = "Lisans Oluştur", Height = 40 };

    // ── Çıktı alanı — LİSANS ANAHTARI BURADA GÖRÜNÜR ──
    private Label lblKey = new() { Text = "Üretilen Lisans Anahtarı:", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
    private TextBox txtKey = new()
    {
        ReadOnly = true,
        Multiline = true,
        Height = 80,
        BackColor = Color.LightYellow,
        Font = new Font("Consolas", 9),
        Text = "Henüz lisans üretilmedi."
    };
    private Button btnCopy = new() { Text = "Panoya Kopyala", Enabled = false, Height = 35 };

    // ── Geçmiş tablosu ──
    private DataGridView grid = new()
    {
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        ReadOnly = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false
    };
    private Label lblHistory = new() { Text = "Geçmiş:", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };

    public MainForm()
    {
        // DB'yi yazılabilir yere koy
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbDir = Path.Combine(appData, "KOAFiloServis");
        Directory.CreateDirectory(dbDir);
        _dbPath = Path.Combine(dbDir, "licenses.db");

        Text = "KOAFiloServis Lisans Üretim Aracı";
        Width = 850;
        Height = 680;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;

        int y = 10;
        int leftMargin = 15;
        int inputWidth = 380;

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

        // Buton
        btnUret.Location = new Point(leftMargin, y);
        btnUret.Width = inputWidth;
        btnUret.BackColor = Color.SteelBlue;
        btnUret.ForeColor = Color.White;
        btnUret.FlatStyle = FlatStyle.Flat;
        y += 50;

        // ── ANAHTAR ÇIKTI ALANI (BURADA GÖRÜNÜR) ──
        lblKey.Location = new Point(leftMargin, y);
        y += 22;
        txtKey.Location = new Point(leftMargin, y);
        txtKey.Width = inputWidth;
        y += 90;

        btnCopy.Location = new Point(leftMargin, y);
        btnCopy.Width = inputWidth;
        y += 50;

        // ── Sağ panel: Geçmiş ──
        lblHistory.Location = new Point(420, 10);
        grid.Location = new Point(420, 32);
        grid.Width = 400;
        grid.Height = 570;
        grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        Controls.AddRange(new Control[] {
            lblFirma, txtFirma,
            lblMachine, txtMachine,
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
            if (dt.Columns.Count > 0)
            {
                grid.Columns["Id"]!.HeaderText = "No"; grid.Columns["Id"]!.FillWeight = 8;
                grid.Columns["FirmaKodu"]!.HeaderText = "Firma"; grid.Columns["FirmaKodu"]!.FillWeight = 18;
                grid.Columns["MachineId"]!.HeaderText = "Makine"; grid.Columns["MachineId"]!.FillWeight = 35;
                grid.Columns["ExpireDate"]!.HeaderText = "Bitiş"; grid.Columns["ExpireDate"]!.FillWeight = 14;
                grid.Columns["CreatedAt"]!.HeaderText = "Oluşturma"; grid.Columns["CreatedAt"]!.FillWeight = 14;
                grid.Columns["AllowedVersion"]!.HeaderText = "Sürüm"; grid.Columns["AllowedVersion"]!.FillWeight = 11;
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
    // LİSANS ÜRET
    // ══════════════════════════════════════════════

    void Uret()
    {
        var firma = txtFirma.Text.Trim();
        var machine = txtMachine.Text.Trim();

        if (string.IsNullOrWhiteSpace(firma))
        {
            MessageBox.Show("Firma kodunu giriniz.", "Eksik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(machine))
        {
            MessageBox.Show("Machine ID'yi web uygulamasından kopyalayıp yapıştırın.", "Eksik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var expire = DateTime.UtcNow.AddYears(1);
            var created = DateTime.UtcNow;
            const string allowedVersion = "1.0.99";
            const bool isDemo = false;

            // SIGNATURE — LicenseService.GenerateSignature() ile BİREBİR AYNI
            var raw = $"{firma}|{machine}|{expire:yyyy-MM-dd}|{isDemo}|{allowedVersion}|{created:yyyy-MM-dd}|{SECRET}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            var signature = Convert.ToBase64String(hash);

            // JSON → Base64
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
            var key = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            // EKRANDA GÖSTER
            txtKey.Text = key;
            txtKey.SelectAll();
            btnCopy.Enabled = true;

            // DB + Clipboard + Grid
            SaveLicense(firma, machine, expire, created, allowedVersion);
            Clipboard.SetText(key);
            LoadData();

            MessageBox.Show(
                $"Lisans anahtarı üretildi!\n\nFirma: {firma}\nBitiş: {expire:yyyy-MM-dd}\n\n" +
                $"Anahtar EKRANDA gösteriliyor ve PANOYA kopyalandı.\nMüşteriye gönderebilirsiniz.",
                "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void CopyKey()
    {
        if (!string.IsNullOrWhiteSpace(txtKey.Text) && txtKey.Text != "Henüz lisans üretilmedi.")
        {
            Clipboard.SetText(txtKey.Text);
            MessageBox.Show("Panoya kopyalandı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
