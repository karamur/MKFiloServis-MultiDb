using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace KOAFiloServis.LisansDesktop;

public partial class Form1 : Form
{
    // 🔴 Web LicenseService ile %100 AYNI secret key
    private const string WebSecretKey = "KOAFiloServis-LCNS-2026-SECURE-KEY-X9mK2pL5vR8w";
    private const string LisansAesKey = "KOAFiloServis2026SecretKey!@";

    private readonly string _dbPath;
    private readonly string _licensesRoot;
    private string? _seciliFirmaKodu;
    private TextBox txtArama = null!;
    private Button btnExport = null!;

    public Form1()
    {
        InitializeComponent();
        cmbLisansTipi.SelectedIndex = 1; // Standard
        cmbIslemTipi.SelectedIndex = 0;
        dtpBitisTarihi.Value = DateTime.Today.AddYears(1);

        // SQLite DB: exe ile aynı klasörde
        _dbPath = Path.Combine(AppContext.BaseDirectory, "licenses.db");

        // Lisans çıktıları: exe'nin yanında /licenses/{FirmaKodu}/
        _licensesRoot = Path.Combine(AppContext.BaseDirectory, "licenses");
        Directory.CreateDirectory(_licensesRoot);

        // 🔴 Arama kutusu — grid'in üstüne dinamik ekle
        txtArama = new TextBox
        {
            Location = new Point(675, 55),
            Size = new Size(200, 27),
            PlaceholderText = "Firma / Makine ara..."
        };
        txtArama.TextChanged += txtArama_TextChanged;
        Controls.Add(txtArama);

        // 🔴 Export butonu
        btnExport = new Button
        {
            Location = new Point(880, 53),
            Size = new Size(100, 30),
            Text = "Dışa Aktar"
        };
        btnExport.Click += btnExport_Click;
        Controls.Add(btnExport);

        InitDatabase();
        GridiDoldur();
    }

    // ══════════════════════════════════════════════
    // DATABASE
    // ══════════════════════════════════════════════

    private void InitDatabase()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Licenses (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TakipNo TEXT NOT NULL,
                FirmaKodu TEXT NOT NULL,
                FirmaAdi TEXT,
                YetkiliKisi TEXT,
                Email TEXT,
                Telefon TEXT,
                MachineId TEXT NOT NULL,
                ExpireDate TEXT NOT NULL,
                AllowedVersion TEXT,
                IsDemo INTEGER NOT NULL DEFAULT 0,
                LisansTipi TEXT,
                MaxKullanici INTEGER DEFAULT 10,
                CreatedAt TEXT NOT NULL,
                Signature TEXT NOT NULL,
                AktivasyonKodu TEXT,
                IslemTipi TEXT,
                Notlar TEXT,
                KayitTarihi TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();

        // Eski JSON'dan migration (tek seferlik)
        MigrateFromJsonIfNeeded(conn);
    }

    private void MigrateFromJsonIfNeeded(SqliteConnection conn)
    {
        var jsonPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KOAFiloServis", "LisansOlusturucu", "lisans_kayitlari.json");

        if (!File.Exists(jsonPath)) return;

        // Check if migration already done
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM Licenses";
        var count = (long)checkCmd.ExecuteScalar()!;
        if (count > 0) return; // Already migrated

        try
        {
            var json = File.ReadAllText(jsonPath, Encoding.UTF8);
            var kayitlar = JsonSerializer.Deserialize<List<JsonLegacyRecord>>(json);
            if (kayitlar == null || !kayitlar.Any()) return;

            using var tx = conn.BeginTransaction();
            foreach (var k in kayitlar)
            {
                using var ins = conn.CreateCommand();
                ins.CommandText = """
                    INSERT INTO Licenses (TakipNo, FirmaKodu, FirmaAdi, YetkiliKisi, Email, Telefon,
                        MachineId, ExpireDate, AllowedVersion, IsDemo, LisansTipi, MaxKullanici,
                        CreatedAt, Signature, AktivasyonKodu, IslemTipi, Notlar, KayitTarihi)
                    VALUES (@tn, @fk, @fa, @yk, @em, @tl, @mk, @ed, @av, @id, @lt, @mx,
                            @ca, @sg, @ak, @it, @nt, @kt)
                    """;
                ins.Parameters.AddWithValue("@tn", k.TakipNo ?? "");
                ins.Parameters.AddWithValue("@fk", k.FirmaAdi?.Replace(" ", "").ToUpperInvariant() ?? "");
                ins.Parameters.AddWithValue("@fa", k.FirmaAdi ?? "");
                ins.Parameters.AddWithValue("@yk", k.YetkiliKisi ?? "");
                ins.Parameters.AddWithValue("@em", k.Email ?? "");
                ins.Parameters.AddWithValue("@tl", k.Telefon ?? "");
                ins.Parameters.AddWithValue("@mk", k.MakineKodu ?? "");
                ins.Parameters.AddWithValue("@ed", k.BitisTarihi.ToString("yyyy-MM-dd"));
                ins.Parameters.AddWithValue("@av", "1.0.99");
                ins.Parameters.AddWithValue("@id", k.LisansTipi == "Trial" ? 1 : 0);
                ins.Parameters.AddWithValue("@lt", k.LisansTipi ?? "");
                ins.Parameters.AddWithValue("@mx", k.MaxKullaniciSayisi);
                ins.Parameters.AddWithValue("@ca", k.BaslangicTarihi.ToString("yyyy-MM-dd"));
                ins.Parameters.AddWithValue("@sg", "");
                ins.Parameters.AddWithValue("@ak", k.AktivasyonKodu ?? "");
                ins.Parameters.AddWithValue("@it", k.IslemTipi ?? "");
                ins.Parameters.AddWithValue("@nt", k.Notlar ?? "");
                ins.Parameters.AddWithValue("@kt", k.IslemTarihi.ToString("yyyy-MM-dd HH:mm"));
                ins.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch { /* Migration best-effort */ }
    }

    // ══════════════════════════════════════════════
    // MACHINE ID (LicenseService ile %100 aynı)
    // ══════════════════════════════════════════════

    private void btnBuPcMakineKodu_Click(object? sender, EventArgs e)
    {
        txtMakineKodu.Text = GetHardenedMachineId();
    }

    private static string GetHardenedMachineId()
    {
        try
        {
            var machine = Environment.MachineName;
            var user = Environment.UserName;
            var driveSerial = "";

            try
            {
                var drives = DriveInfo.GetDrives();
                var systemDrive = drives.FirstOrDefault(d =>
                    d.IsReady && d.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                    ?? drives.FirstOrDefault(d => d.IsReady);

                if (systemDrive != null)
                    driveSerial = systemDrive.VolumeLabel?.GetHashCode().ToString("X8") ?? "NOSERIAL";
            }
            catch { driveSerial = "NOSERIAL"; }

            return $"{machine}_{user}_{driveSerial}";
        }
        catch
        {
            return $"{Environment.MachineName}_{Environment.UserName}";
        }
    }

    // ══════════════════════════════════════════════
    // SIGNATURE — LicenseService.GenerateSignature ile %100 AYNI
    // ══════════════════════════════════════════════

    private static string GenerateWebSignature(string firmaKodu, string machineId, DateTime expireDate,
        bool isDemo, string allowedVersion, DateTime createdAt)
    {
        var raw = $"{firmaKodu}|{machineId}|{expireDate:yyyy-MM-dd}|{isDemo}|{allowedVersion}|{createdAt:yyyy-MM-dd}|{WebSecretKey}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToBase64String(hash);
    }

    // ══════════════════════════════════════════════
    // ENCRYPT (aktivasyon kodu için)
    // ══════════════════════════════════════════════

    private static string EncryptString(string plainText)
    {
        using var aes = Aes.Create();
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(LisansAesKey));
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        var iv = aes.IV;
        var encrypted = msEncrypt.ToArray();
        var result = new byte[iv.Length + encrypted.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);

        return Convert.ToBase64String(result);
    }

    // ══════════════════════════════════════════════
    // LİSANS ÜRET + DB KAYDET + DOSYALA
    // ══════════════════════════════════════════════

    private void btnLisansUret_Click(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(txtFirmaAdi.Text))
            {
                MessageBox.Show("Firma adı zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFirmaAdi.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(txtMakineKodu.Text))
            {
                MessageBox.Show("Makine kodu zorunludur.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMakineKodu.Focus();
                return;
            }

            var firmaAdi = txtFirmaAdi.Text.Trim();
            var firmaKodu = firmaAdi.Replace(" ", "").ToUpperInvariant();
            var machineId = txtMakineKodu.Text.Trim();
            var lisansTipi = cmbLisansTipi.SelectedItem?.ToString() ?? "Standard";
            var isDemo = lisansTipi.Equals("Trial", StringComparison.OrdinalIgnoreCase);
            var islemTipi = cmbIslemTipi.SelectedItem?.ToString() ?? "Yeni Kayıt";
            var createdAt = DateTime.UtcNow;
            var expireDate = DateTime.SpecifyKind(dtpBitisTarihi.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            var allowedVersion = "1.0.99";
            var maxKullanici = (int)numMaxKullanici.Value;
            var takipNo = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

            // Takip no: yenileme ise mevcut kaydın takip nosunu kullan
            if (islemTipi != "Yeni Kayıt" && !string.IsNullOrEmpty(_seciliFirmaKodu))
            {
                using var tnConn = new SqliteConnection($"Data Source={_dbPath}");
                tnConn.Open();
                using var tnCmd = tnConn.CreateCommand();
                tnCmd.CommandText = "SELECT TakipNo FROM Licenses WHERE FirmaKodu = @fk ORDER BY Id DESC LIMIT 1";
                tnCmd.Parameters.AddWithValue("@fk", _seciliFirmaKodu);
                var existingTn = tnCmd.ExecuteScalar() as string;
                if (!string.IsNullOrEmpty(existingTn)) takipNo = existingTn;
            }

            // 🔴 Signature — web ile %100 uyumlu
            var signature = GenerateWebSignature(firmaKodu, machineId, expireDate, isDemo, allowedVersion, createdAt);

            // Aktivasyon kodu (şifreli)
            var lisansBilgi = new
            {
                LisansKodu = $"KOA-{Random.Shared.Next(1000, 9999)}-{Random.Shared.Next(1000, 9999)}",
                FirmaAdi = firmaAdi,
                FirmaKodu = firmaKodu,
                YetkiliKisi = txtYetkili.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Telefon = txtTelefon.Text.Trim(),
                LisansTipi = lisansTipi,
                BaslangicTarihi = createdAt,
                BitisTarihi = expireDate,
                MaxKullaniciSayisi = maxKullanici,
                MakineKodu = machineId,
                AllowedVersion = allowedVersion,
                IsDemo = isDemo
            };
            var aktivasyonKodu = EncryptString(JsonSerializer.Serialize(lisansBilgi));
            txtLisansAnahtari.Text = aktivasyonKodu;

            // ── DOSYALAMA: /licenses/{FirmaKodu}/license_{tarih}.json ──
            var firmaDir = Path.Combine(_licensesRoot, firmaKodu);
            Directory.CreateDirectory(firmaDir);
            var jsonFileName = $"license_{createdAt:yyyy-MM-dd_HHmm}.json";
            var jsonPath = Path.Combine(firmaDir, jsonFileName);

            var webLicense = new
            {
                FirmaKodu = firmaKodu,
                MachineId = machineId,
                ExpireDate = expireDate,
                AllowedVersion = allowedVersion,
                IsDemo = isDemo,
                CreatedAt = createdAt,
                Signature = signature
            };
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(webLicense, new JsonSerializerOptions { WriteIndented = true }));

            // Ayrıca kısa isimle de kaydet (müşteriye göndermek için)
            var shortPath = Path.Combine(firmaDir, $"license_{firmaKodu}.json");
            File.WriteAllText(shortPath, JsonSerializer.Serialize(webLicense, new JsonSerializerOptions { WriteIndented = true }));

            // ── SQLite DB kaydı ──
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO Licenses (TakipNo, FirmaKodu, FirmaAdi, YetkiliKisi, Email, Telefon,
                    MachineId, ExpireDate, AllowedVersion, IsDemo, LisansTipi, MaxKullanici,
                    CreatedAt, Signature, AktivasyonKodu, IslemTipi, Notlar, KayitTarihi)
                VALUES (@tn, @fk, @fa, @yk, @em, @tl, @mk, @ed, @av, @id, @lt, @mx,
                        @ca, @sg, @ak, @it, @nt, @kt)
                """;
            cmd.Parameters.AddWithValue("@tn", takipNo);
            cmd.Parameters.AddWithValue("@fk", firmaKodu);
            cmd.Parameters.AddWithValue("@fa", firmaAdi);
            cmd.Parameters.AddWithValue("@yk", txtYetkili.Text.Trim());
            cmd.Parameters.AddWithValue("@em", txtEmail.Text.Trim());
            cmd.Parameters.AddWithValue("@tl", txtTelefon.Text.Trim());
            cmd.Parameters.AddWithValue("@mk", machineId);
            cmd.Parameters.AddWithValue("@ed", expireDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@av", allowedVersion);
            cmd.Parameters.AddWithValue("@id", isDemo ? 1 : 0);
            cmd.Parameters.AddWithValue("@lt", lisansTipi);
            cmd.Parameters.AddWithValue("@mx", maxKullanici);
            cmd.Parameters.AddWithValue("@ca", createdAt.ToString("yyyy-MM-dd HH:mm"));
            cmd.Parameters.AddWithValue("@sg", signature);
            cmd.Parameters.AddWithValue("@ak", aktivasyonKodu);
            cmd.Parameters.AddWithValue("@it", islemTipi);
            cmd.Parameters.AddWithValue("@nt", txtNotlar.Text.Trim());
            cmd.Parameters.AddWithValue("@kt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();

            _seciliFirmaKodu = firmaKodu;
            GridiDoldur();

            MessageBox.Show(
                $"✅ Lisans oluşturuldu!\n\n📁 JSON: {shortPath}\n📦 DB: licenses.db\n🔑 Aktivasyon kodu panoya kopyalandı.",
                "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Clipboard.SetText(aktivasyonKodu);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ══════════════════════════════════════════════
    // GRID — Color-coded + Search
    // ══════════════════════════════════════════════

    private void GridiDoldur(string? searchText = null)
    {
        var rows = new List<LicenseGridRow>();

        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();

        var where = "1=1";
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            where = "(FirmaKodu LIKE @s OR FirmaAdi LIKE @s OR MachineId LIKE @s)";
            cmd.Parameters.AddWithValue("@s", $"%{searchText}%");
        }
        cmd.CommandText = $"SELECT * FROM Licenses WHERE {where} ORDER BY Id DESC LIMIT 50";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var expireStr = reader["ExpireDate"]?.ToString() ?? "";
            DateTime.TryParse(expireStr, out var expireDate);
            var kalanGun = Math.Max(0, (expireDate.Date - DateTime.UtcNow.Date).Days);

            rows.Add(new LicenseGridRow
            {
                Id = Convert.ToInt32(reader["Id"]),
                TakipNo = reader["TakipNo"]?.ToString() ?? "",
                FirmaKodu = reader["FirmaKodu"]?.ToString() ?? "",
                FirmaAdi = reader["FirmaAdi"]?.ToString() ?? "",
                MakineKodu = reader["MachineId"]?.ToString() ?? "",
                LisansTipi = reader["LisansTipi"]?.ToString() ?? "",
                IsDemo = Convert.ToInt32(reader["IsDemo"]) == 1,
                MaxKullanici = Convert.ToInt32(reader["MaxKullanici"]),
                ExpireDate = expireDate,
                AllowedVersion = reader["AllowedVersion"]?.ToString() ?? "",
                KalanGun = kalanGun,
                IslemTipi = reader["IslemTipi"]?.ToString() ?? "",
                KayitTarihi = reader["KayitTarihi"]?.ToString() ?? ""
            });
        }

        dgvKayitlar.DataSource = null;
        dgvKayitlar.DataSource = rows;

        // Kolon görünürlüğü
        HideColumn(nameof(LicenseGridRow.Id));
        HideColumn(nameof(LicenseGridRow.IsDemo));

        // 🔴 RENK KURALI: Süresi dolmuş → KIRMIZI, 7 gün → TURUNCU (tek seferlik event)
        dgvKayitlar.CellFormatting -= GridCellFormatting;
        dgvKayitlar.CellFormatting += GridCellFormatting;

        // Kayıt sayısı
        lblKayitlar.Text = $"Lisans Takibi ({rows.Count} kayıt)";
    }

    private void HideColumn(string name)
    {
        if (dgvKayitlar.Columns[name] is DataGridViewColumn col)
            col.Visible = false;
    }

    private void GridCellFormatting(object? s, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || dgvKayitlar.Rows[e.RowIndex].DataBoundItem is not LicenseGridRow row) return;

        if (row.ExpireDate < DateTime.UtcNow)
        {
            e.CellStyle.BackColor = Color.FromArgb(255, 220, 220);
            e.CellStyle.ForeColor = Color.DarkRed;
        }
        else if (row.KalanGun <= 7)
        {
            e.CellStyle.BackColor = Color.FromArgb(255, 243, 205);
            e.CellStyle.ForeColor = Color.DarkGoldenrod;
        }
    }

    // ══════════════════════════════════════════════
    // GRID SELECTION → Forma yükle
    // ══════════════════════════════════════════════

    private void dgvKayitlar_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvKayitlar.CurrentRow?.DataBoundItem is LicenseGridRow row)
        {
            _seciliFirmaKodu = row.FirmaKodu;
        }
    }

    private void btnSeciliKaydiYukle_Click(object? sender, EventArgs e)
    {
        if (dgvKayitlar.CurrentRow?.DataBoundItem is not LicenseGridRow row) return;

        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Licenses WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", row.Id);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return;

        txtFirmaAdi.Text = reader["FirmaAdi"]?.ToString() ?? "";
        txtYetkili.Text = reader["YetkiliKisi"]?.ToString() ?? "";
        txtEmail.Text = reader["Email"]?.ToString() ?? "";
        txtTelefon.Text = reader["Telefon"]?.ToString() ?? "";
        txtMakineKodu.Text = reader["MachineId"]?.ToString() ?? "";
        txtNotlar.Text = reader["Notlar"]?.ToString() ?? "";
        txtLisansAnahtari.Text = reader["AktivasyonKodu"]?.ToString() ?? "";

        var lisansTipi = reader["LisansTipi"]?.ToString() ?? "Standard";
        cmbLisansTipi.SelectedItem = cmbLisansTipi.Items.Contains(lisansTipi) ? lisansTipi : cmbLisansTipi.Items[0];

        cmbIslemTipi.SelectedItem = "Yenileme";

        if (DateTime.TryParse(reader["ExpireDate"]?.ToString(), out var exp))
            dtpBitisTarihi.Value = exp.ToLocalTime().Date;

        if (int.TryParse(reader["MaxKullanici"]?.ToString(), out var mx))
            numMaxKullanici.Value = Math.Min(numMaxKullanici.Maximum, Math.Max(numMaxKullanici.Minimum, mx));

        _seciliFirmaKodu = row.FirmaKodu;
    }

    // ══════════════════════════════════════════════
    // BUTTONS
    // ══════════════════════════════════════════════

    private void btnKopyala_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtLisansAnahtari.Text)) return;
        Clipboard.SetText(txtLisansAnahtari.Text);
    }

    private void btnTxtKaydet_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtLisansAnahtari.Text)) return;

        saveFileDialog1.FileName = $"lisans_{txtFirmaAdi.Text.Trim().Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.txt";
        if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;

        File.WriteAllText(saveFileDialog1.FileName, txtLisansAnahtari.Text, Encoding.UTF8);
    }

    private void btnYeniForm_Click(object? sender, EventArgs e)
    {
        _seciliFirmaKodu = null;
        txtFirmaAdi.Clear();
        txtYetkili.Clear();
        txtEmail.Clear();
        txtTelefon.Clear();
        txtMakineKodu.Clear();
        txtNotlar.Clear();
        txtLisansAnahtari.Clear();
        cmbLisansTipi.SelectedIndex = 1;
        cmbIslemTipi.SelectedIndex = 0;
        numMaxKullanici.Value = 10;
        dtpBitisTarihi.Value = DateTime.Today.AddYears(1);
    }

    private void btnPaketOlustur_Click(object? sender, EventArgs e)
    {
        using var form = new PaketFormu();
        form.ShowDialog(this);
    }

    // ══════════════════════════════════════════════
    // SEARCH — TextBox enter ile filtre
    // ══════════════════════════════════════════════

    private void txtArama_TextChanged(object? sender, EventArgs e)
    {
        GridiDoldur(txtArama.Text.Trim());
    }

    // ══════════════════════════════════════════════
    // EXPORT
    // ══════════════════════════════════════════════

    private void btnExport_Click(object? sender, EventArgs e)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Licenses ORDER BY Id DESC";

        var list = new List<object>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new
            {
                FirmaKodu = reader["FirmaKodu"]?.ToString(),
                FirmaAdi = reader["FirmaAdi"]?.ToString(),
                MachineId = reader["MachineId"]?.ToString(),
                ExpireDate = reader["ExpireDate"]?.ToString(),
                AllowedVersion = reader["AllowedVersion"]?.ToString(),
                IsDemo = Convert.ToInt32(reader["IsDemo"]) == 1,
                LisansTipi = reader["LisansTipi"]?.ToString(),
                MaxKullanici = Convert.ToInt32(reader["MaxKullanici"]),
                CreatedAt = reader["CreatedAt"]?.ToString(),
                Signature = reader["Signature"]?.ToString(),
                IslemTipi = reader["IslemTipi"]?.ToString(),
                KayitTarihi = reader["KayitTarihi"]?.ToString()
            });
        }

        var exportPath = Path.Combine(AppContext.BaseDirectory, "licenses_export.json");
        File.WriteAllText(exportPath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));

        MessageBox.Show($"✅ {list.Count} lisans dışa aktarıldı:\n{exportPath}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ══════════════════════════════════════════════
    // MODELS
    // ══════════════════════════════════════════════

    private sealed class LicenseGridRow
    {
        public int Id { get; set; }
        public string TakipNo { get; set; } = "";
        public string FirmaKodu { get; set; } = "";
        public string FirmaAdi { get; set; } = "";
        public string MakineKodu { get; set; } = "";
        public string LisansTipi { get; set; } = "";
        public bool IsDemo { get; set; }
        public int MaxKullanici { get; set; }
        public DateTime ExpireDate { get; set; }
        public string AllowedVersion { get; set; } = "";
        public int KalanGun { get; set; }
        public string IslemTipi { get; set; } = "";
        public string KayitTarihi { get; set; } = "";
    }

    private sealed class JsonLegacyRecord
    {
        public string? TakipNo { get; set; }
        public string? FirmaAdi { get; set; }
        public string? YetkiliKisi { get; set; }
        public string? Email { get; set; }
        public string? Telefon { get; set; }
        public string? MakineKodu { get; set; }
        public string? LisansTipi { get; set; }
        public int MaxKullaniciSayisi { get; set; }
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public string? AktivasyonKodu { get; set; }
        public string? IslemTipi { get; set; }
        public string? Notlar { get; set; }
        public DateTime IslemTarihi { get; set; }
    }
}
