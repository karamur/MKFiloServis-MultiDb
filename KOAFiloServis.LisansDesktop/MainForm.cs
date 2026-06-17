using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KOAFiloServis.LisansDesktop.Data;
using KOAFiloServis.LisansDesktop.Models;

namespace KOAFiloServis.LisansDesktop;

public sealed class MainForm : Form
{
    // ══════════════════════════════════════════════
    // CONSTANTS — Web LicenseService ile %100 AYNI
    // ══════════════════════════════════════════════
    private const string SecretKey = "KOAFiloServis-LCNS-2026-SECURE-KEY-X9mK2pL5vR8w";

    // ══════════════════════════════════════════════
    // CONTROLS
    // ══════════════════════════════════════════════
    private readonly TextBox _txtFirmaAdi, _txtFirmaKodu, _txtMachineId, _txtVersion;
    private readonly ComboBox _cmbLisansTipi;
    private readonly DateTimePicker _dtpExpire;
    private readonly NumericUpDown _numMaxKullanici;
    private readonly Button _btnUret, _btnKopyalaMakine, _btnExport, _btnYenile;
    private readonly TextBox _txtArama;
    private readonly DataGridView _grid;
    private readonly Label _lblStatus;
    private readonly LicenseDb _db;
    private readonly string _outputRoot;

    public MainForm()
    {
        Text = "KOA Lisans Olusturucu — v1.0.25";
        Size = new Size(1100, 650);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        _db = new LicenseDb(Path.Combine(AppContext.BaseDirectory, "licenses.db"));
        _outputRoot = Path.Combine(AppContext.BaseDirectory, "licenses");

        // ── SOL PANEL ──
        var leftPanel = new Panel { Location = new Point(12, 12), Size = new Size(380, 520) };

        int y = 10;
        AddLabel(leftPanel, "Firma Adı", y);
        _txtFirmaAdi = AddTextBox(leftPanel, y += 22);

        AddLabel(leftPanel, "Firma Kodu", y += 52);
        _txtFirmaKodu = AddTextBox(leftPanel, y += 22);
        _txtFirmaKodu.TextChanged += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_txtFirmaKodu.Text) && !string.IsNullOrWhiteSpace(_txtFirmaAdi.Text))
                _txtFirmaKodu.Text = _txtFirmaAdi.Text.Replace(" ", "").ToUpperInvariant();
        };

        AddLabel(leftPanel, "Makine ID", y += 52);
        _txtMachineId = AddTextBox(leftPanel, y += 22);
        _btnKopyalaMakine = new Button
        {
            Text = "Bu PC Kodu",
            Location = new Point(270, y),
            Size = new Size(100, 27),
            FlatStyle = FlatStyle.System
        };
        _btnKopyalaMakine.Click += (_, _) => _txtMachineId.Text = GetMachineId();
        leftPanel.Controls.Add(_btnKopyalaMakine);

        AddLabel(leftPanel, "Lisans Tipi", y += 52);
        _cmbLisansTipi = new ComboBox
        {
            Location = new Point(10, y + 22),
            Size = new Size(360, 27),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbLisansTipi.Items.AddRange(new object[] { "Demo (30 gün)", "Standard (1 yıl)", "Professional", "Enterprise" });
        _cmbLisansTipi.SelectedIndex = 1;
        leftPanel.Controls.Add(_cmbLisansTipi);

        AddLabel(leftPanel, "Bitiş Tarihi", y += 52);
        _dtpExpire = new DateTimePicker
        {
            Location = new Point(10, y + 22),
            Size = new Size(360, 27),
            Value = DateTime.Today.AddYears(1)
        };
        leftPanel.Controls.Add(_dtpExpire);

        AddLabel(leftPanel, "Versiyon", y += 52);
        _txtVersion = AddTextBox(leftPanel, y += 22);
        _txtVersion.Text = "1.0.99";

        AddLabel(leftPanel, "Max Kullanıcı", y += 52);
        _numMaxKullanici = new NumericUpDown
        {
            Location = new Point(10, y + 22),
            Size = new Size(100, 27),
            Minimum = 1,
            Maximum = 5000,
            Value = 10
        };
        leftPanel.Controls.Add(_numMaxKullanici);

        _btnUret = new Button
        {
            Text = "🚀 Lisans Oluştur",
            Location = new Point(10, y + 70),
            Size = new Size(360, 44),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            BackColor = Color.FromArgb(25, 135, 84),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnUret.FlatAppearance.BorderSize = 0;
        _btnUret.Click += BtnUret_Click;
        leftPanel.Controls.Add(_btnUret);

        Controls.Add(leftPanel);

        // ── SAĞ PANEL ──
        var rightPanel = new Panel { Location = new Point(405, 12), Size = new Size(680, 560) };

        var lblGrid = new Label
        {
            Text = "Lisans Takibi",
            Location = new Point(0, 5),
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Size = new Size(200, 24)
        };
        rightPanel.Controls.Add(lblGrid);

        _txtArama = new TextBox
        {
            Location = new Point(200, 5),
            Size = new Size(200, 27),
            PlaceholderText = "Firma / Makine ara..."
        };
        _txtArama.TextChanged += (_, _) => GridiDoldur();
        rightPanel.Controls.Add(_txtArama);

        _btnYenile = new Button { Text = "Yenile", Location = new Point(410, 4), Size = new Size(80, 28) };
        _btnYenile.Click += (_, _) => GridiDoldur();
        rightPanel.Controls.Add(_btnYenile);

        _btnExport = new Button { Text = "Dışa Aktar", Location = new Point(500, 4), Size = new Size(100, 28) };
        _btnExport.Click += BtnExport_Click;
        rightPanel.Controls.Add(_btnExport);

        _grid = new DataGridView
        {
            Location = new Point(0, 40),
            Size = new Size(680, 480),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
        _grid.CellFormatting += GridCellFormatting;
        _grid.CellDoubleClick += GridCellDoubleClick;
        rightPanel.Controls.Add(_grid);

        Controls.Add(rightPanel);

        // ── STATUS BAR ──
        _lblStatus = new Label
        {
            Location = new Point(12, 580),
            Size = new Size(1070, 24),
            ForeColor = Color.Gray
        };
        Controls.Add(_lblStatus);

        GridiDoldur();
    }

    // ══════════════════════════════════════════════
    // SIGNATURE — Web LicenseService ile %100 AYNI
    // ══════════════════════════════════════════════

    private static string GenerateSignature(string firmaKodu, string machineId, DateTime expireDate,
        bool isDemo, string allowedVersion, DateTime createdAt)
    {
        var raw = $"{firmaKodu}|{machineId}|{expireDate:yyyy-MM-dd}|{isDemo}|{allowedVersion}|{createdAt:yyyy-MM-dd}|{SecretKey}";
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    // ══════════════════════════════════════════════
    // MACHINE ID — Web ile %100 AYNI
    // ══════════════════════════════════════════════

    private static string GetMachineId()
    {
        try
        {
            var machine = Environment.MachineName;
            var user = Environment.UserName;
            var driveSerial = "";
            try
            {
                var drives = DriveInfo.GetDrives();
                var sys = drives.FirstOrDefault(d => d.IsReady && d.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                       ?? drives.FirstOrDefault(d => d.IsReady);
                if (sys != null) driveSerial = sys.VolumeLabel?.GetHashCode().ToString("X8") ?? "";
            }
            catch { }
            return $"{machine}_{user}_{driveSerial}";
        }
        catch
        {
            return $"{Environment.MachineName}_{Environment.UserName}";
        }
    }

    // ══════════════════════════════════════════════
    // LİSANS OLUŞTUR
    // ══════════════════════════════════════════════

    private void BtnUret_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtFirmaKodu.Text))
        {
            MessageBox.Show("Firma kodu zorunludur.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_txtMachineId.Text))
        {
            MessageBox.Show("Makine ID zorunludur.", "Eksik Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var firmaKodu = _txtFirmaKodu.Text.Trim().ToUpperInvariant();
            var firmaAdi = _txtFirmaAdi.Text.Trim();
            var machineId = _txtMachineId.Text.Trim();
            var isDemo = _cmbLisansTipi.SelectedIndex == 0;
            var lisansTipi = _cmbLisansTipi.SelectedItem?.ToString() ?? "Standard";
            var expireDate = _dtpExpire.Value.Date.AddDays(1).AddSeconds(-1);
            var allowedVersion = _txtVersion.Text.Trim();
            var createdAt = DateTime.UtcNow;
            var maxKullanici = (int)_numMaxKullanici.Value;

            if (string.IsNullOrWhiteSpace(allowedVersion))
                allowedVersion = "1.0.99";

            // Signature
            var signature = GenerateSignature(firmaKodu, machineId, expireDate, isDemo, allowedVersion, createdAt);

            // DB kayıt
            var lic = new LicenseRecord
            {
                FirmaKodu = firmaKodu,
                FirmaAdi = firmaAdi,
                MachineId = machineId,
                ExpireDate = expireDate,
                AllowedVersion = allowedVersion,
                IsDemo = isDemo,
                CreatedAt = createdAt,
                Signature = signature,
                LisansTipi = lisansTipi,
                MaxKullanici = maxKullanici,
                KayitTarihi = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm")
            };
            _db.Insert(lic);

            // 🔥 AKTİVASYON KEY = Base64(license.json)
            var licensePayload = new
            {
                lic.FirmaKodu,
                lic.MachineId,
                lic.ExpireDate,
                lic.AllowedVersion,
                lic.IsDemo,
                lic.CreatedAt,
                lic.Signature
            };
            var json = JsonSerializer.Serialize(licensePayload);
            var activationKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            // Clipboard'a kopyala
            Clipboard.SetText(activationKey);

            // JSON yedek (internal tracking)
            var firmaDir = Path.Combine(_outputRoot, firmaKodu);
            Directory.CreateDirectory(firmaDir);
            var jsonBackupPath = Path.Combine(firmaDir, $"license_{createdAt:yyyy-MM-dd_HHmm}.json");
            File.WriteAllText(jsonBackupPath, JsonSerializer.Serialize(licensePayload, new JsonSerializerOptions { WriteIndented = true }));

            _lblStatus.Text = $"✅ Aktivasyon kodu panoya kopyalandı!  |  Firma: {firmaKodu}  |  Bitiş: {expireDate:yyyy-MM-dd}  |  Yedek: {jsonBackupPath}";
            _lblStatus.ForeColor = Color.DarkGreen;

            GridiDoldur();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ══════════════════════════════════════════════
    // GRID
    // ══════════════════════════════════════════════

    private void GridiDoldur()
    {
        var search = string.IsNullOrWhiteSpace(_txtArama.Text) ? null : _txtArama.Text.Trim();
        var licenses = _db.GetAll(search);

        _grid.DataSource = null;
        _grid.DataSource = licenses.OrderByDescending(l => l.Id).Select(l => new
        {
            l.Id,
            l.FirmaKodu,
            l.FirmaAdi,
            MakineKodu = l.MachineId,
            l.LisansTipi,
            l.ExpireDate,
            KalanGun = l.KalanGun,
            l.AllowedVersion,
            l.KayitTarihi
        }).ToList();

        // Kolonları gizle/düzenle
        if (_grid.Columns["Id"] is DataGridViewColumn cId) cId.Visible = false;
        if (_grid.Columns["MakineKodu"] is DataGridViewColumn cMk) cMk.Width = 80;
        if (_grid.Columns["ExpireDate"] is DataGridViewColumn cEd) { cEd.Width = 100; cEd.HeaderText = "Bitiş"; }
        if (_grid.Columns["KalanGun"] is DataGridViewColumn cKg) { cKg.Width = 60; cKg.HeaderText = "Gün"; }
        if (_grid.Columns["AllowedVersion"] is DataGridViewColumn cAv) { cAv.Width = 60; cAv.HeaderText = "Ver"; }
        if (_grid.Columns["KayitTarihi"] is DataGridViewColumn cKt) { cKt.Width = 120; cKt.HeaderText = "Kayıt"; }

        _lblStatus.Text = $"{licenses.Count} lisans kaydı";
        _lblStatus.ForeColor = Color.Gray;
    }

    private void GridCellFormatting(object? s, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || _grid.Rows[e.RowIndex].DataBoundItem == null) return;

        dynamic? row = _grid.Rows[e.RowIndex].DataBoundItem;
        if (row == null) return;

        int kalanGun = (int)(row.KalanGun ?? 0);
        DateTime expireDate = (DateTime)(row.ExpireDate ?? DateTime.MinValue);

        if (expireDate < DateTime.UtcNow)
        {
            e.CellStyle.BackColor = Color.FromArgb(255, 220, 220);
            e.CellStyle.ForeColor = Color.DarkRed;
        }
        else if (kalanGun <= 7)
        {
            e.CellStyle.BackColor = Color.FromArgb(255, 243, 205);
            e.CellStyle.ForeColor = Color.DarkGoldenrod;
        }
    }

    private void GridCellDoubleClick(object? s, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        dynamic? row = _grid.Rows[e.RowIndex].DataBoundItem;
        if (row == null) return;

        int id = (int)(row.Id ?? 0);
        var lic = _db.GetById(id);
        if (lic == null) return;

        // Forma yükle
        _txtFirmaAdi.Text = lic.FirmaAdi;
        _txtFirmaKodu.Text = lic.FirmaKodu;
        _txtMachineId.Text = lic.MachineId;
        _txtVersion.Text = lic.AllowedVersion;
        _dtpExpire.Value = lic.ExpireDate.Date;
        _numMaxKullanici.Value = Math.Min(5000, Math.Max(1, lic.MaxKullanici));
        _cmbLisansTipi.SelectedIndex = lic.IsDemo ? 0 : 1;

        _lblStatus.Text = $"📋 {lic.FirmaKodu} lisansı forma yüklendi — süreyi güncelleyip yeni lisans üretebilirsiniz";
        _lblStatus.ForeColor = Color.DarkBlue;
    }

    // ══════════════════════════════════════════════
    // EXPORT
    // ══════════════════════════════════════════════

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        var all = _db.ExportAll();
        var exportPath = Path.Combine(AppContext.BaseDirectory, "licenses_export.json");
        File.WriteAllText(exportPath, JsonSerializer.Serialize(all.Select(l => new
        {
            l.FirmaKodu,
            l.FirmaAdi,
            l.MachineId,
            l.ExpireDate,
            l.AllowedVersion,
            l.IsDemo,
            l.CreatedAt,
            l.Signature,
            l.LisansTipi,
            l.MaxKullanici,
            l.KayitTarihi
        }), new JsonSerializerOptions { WriteIndented = true }));

        _lblStatus.Text = $"📤 {all.Count} lisans dışa aktarıldı: {exportPath}";
        _lblStatus.ForeColor = Color.DarkGreen;
    }

    // ══════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════

    private static void AddLabel(Panel panel, string text, int y)
    {
        panel.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(10, y),
            Size = new Size(360, 20),
            Font = new Font("Segoe UI", 8.5f)
        });
    }

    private static TextBox AddTextBox(Panel panel, int y)
    {
        var tb = new TextBox { Location = new Point(10, y), Size = new Size(360, 27) };
        panel.Controls.Add(tb);
        return tb;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _db.Dispose();
        base.OnFormClosed(e);
    }
}
