using System.ComponentModel;
using KOAFiloServis.DataTransfer.Models;
using KOAFiloServis.DataTransfer.Services;
using Npgsql;

namespace KOAFiloServis.DataTransfer.UI;

public class MainForm : Form
{
    // Kaynak bağlantı
    private readonly TextBox _srcHost = new() { Text = "localhost", Width = 130 };
    private readonly NumericUpDown _srcPort = new() { Minimum = 1, Maximum = 65535, Value = 5432, Width = 60 };
    private readonly TextBox _srcDb = new() { Text = "DestekCRMServisBlazorDb", Width = 200 };
    private readonly TextBox _srcUser = new() { Text = "postgres", Width = 150 };
    private readonly TextBox _srcPass = new() { Text = "Fast123", Width = 150, UseSystemPasswordChar = true };

    // Hedef bağlantı
    private readonly TextBox _tgtHost = new() { Text = "localhost", Width = 130 };
    private readonly NumericUpDown _tgtPort = new() { Minimum = 1, Maximum = 65535, Value = 5432, Width = 60 };
    private readonly TextBox _tgtDb = new() { Text = "Koa_USTUN_GRUP_001", Width = 200 };
    private readonly TextBox _tgtUser = new() { Text = "postgres", Width = 150 };
    private readonly TextBox _tgtPass = new() { Text = "Fast123", Width = 150, UseSystemPasswordChar = true };

    // Ayarlar
    private readonly NumericUpDown _firmaId = new() { Minimum = 1, Maximum = 9999, Value = 1, Width = 60 };
    private readonly CheckBox _cbStep0 = new() { Text = "Adım 0: Firma kaydı ekle", Checked = true, AutoSize = true };
    private readonly CheckBox _cbStep1 = new() { Text = "Adım 1: Lookup tabloları (FirmaId'siz)", Checked = true, AutoSize = true };
    private readonly CheckBox _cbStep2 = new() { Text = "Adım 2: Tenant tabloları (FirmaId'li)", Checked = true, AutoSize = true };
    private readonly CheckBox _cbResetSeq = new() { Text = "Sequence reset", Checked = true, AutoSize = true };

    // Progress ve log
    private readonly ProgressBar _progressBar = new() { Style = ProgressBarStyle.Continuous, Height = 22 };
    private readonly Label _progressLabel = new() { Text = "Hazır", AutoSize = true };
    private readonly RichTextBox _logBox = new()
    {
        ReadOnly = true,
        BackColor = Color.FromArgb(30, 30, 30),
        ForeColor = Color.LimeGreen,
        Font = new Font("Consolas", 9),
        Dock = DockStyle.Fill,
        WordWrap = false
    };

    private readonly Button _btnMigrate = new() { Text = "▶ MIGRASYONU BAŞLAT", Height = 40, Enabled = true };
    private readonly Button _btnTestSource = new() { Text = "Test", Width = 60 };
    private readonly Button _btnTestTarget = new() { Text = "Test", Width = 60 };
    private readonly Button _btnLoadMasters = new() { Text = "Master'dan Firmaları Yükle", Width = 170, Height = 28 };
    private readonly ComboBox _cmbMasterFirms = new() { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

    private CancellationTokenSource? _cts;

    public MainForm()
    {
        Text = "KOAFiloServis — DataTransfer v1.0";
        Size = new Size(800, 850);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 700);
        Font = new Font("Segoe UI", 9);

        var mainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), RowCount = 4, ColumnCount = 1 };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Kaynak
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Hedef
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Ayarlar
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Log

        mainPanel.Controls.Add(BuildSourcePanel(), 0, 0);
        mainPanel.Controls.Add(BuildTargetPanel(), 0, 1);
        mainPanel.Controls.Add(BuildSettingsPanel(), 0, 2);
        mainPanel.Controls.Add(BuildLogPanel(), 0, 3);

        Controls.Add(mainPanel);

        _btnTestSource.Click += async (_, _) => await TestConnection(true);
        _btnTestTarget.Click += async (_, _) => await TestConnection(false);
        _btnMigrate.Click += async (_, _) => await StartMigration();
        _btnLoadMasters.Click += async (_, _) => await LoadFirmsFromMaster();
        _cmbMasterFirms.SelectedIndexChanged += (_, _) => OnFirmSelected();

        FormClosing += (_, _) => _cts?.Cancel();
    }

    // ─── UI Builders ───

    private Control BuildSourcePanel()
    {
        var panel = CreateGroupBox("KAYNAK VERİTABANI (Eski/Tek DB)");
        var table = new TableLayoutPanel { ColumnCount = 6, RowCount = 2, AutoSize = true, Padding = new Padding(8) };
        table.Controls.Add(new Label { Text = "Host:" }, 0, 0);
        table.Controls.Add(_srcHost, 1, 0);
        table.Controls.Add(new Label { Text = "Port:" }, 2, 0);
        table.Controls.Add(_srcPort, 3, 0);
        table.Controls.Add(new Label { Text = "DB:" }, 0, 1);
        table.Controls.Add(_srcDb, 1, 1);
        table.Controls.Add(new Label { Text = "User:" }, 2, 1);
        table.Controls.Add(_srcUser, 3, 1);
        table.Controls.Add(new Label { Text = "Pass:" }, 4, 1);
        table.Controls.Add(_srcPass, 5, 0);
        _btnTestSource.Margin = new Padding(8, 0, 0, 0);
        table.Controls.Add(_btnTestSource, 5, 1);
        table.SetColumnSpan(_srcPass, 1);
        panel.Controls.Add(table);
        return panel;
    }

    private Control BuildTargetPanel()
    {
        var panel = CreateGroupBox("HEDEF VERİTABANI (Yeni Tenant DB)");
        var table = new TableLayoutPanel { ColumnCount = 6, RowCount = 3, AutoSize = true, Padding = new Padding(8) };
        table.Controls.Add(new Label { Text = "Host:" }, 0, 0);
        table.Controls.Add(_tgtHost, 1, 0);
        table.Controls.Add(new Label { Text = "Port:" }, 2, 0);
        table.Controls.Add(_tgtPort, 3, 0);
        table.Controls.Add(new Label { Text = "DB:" }, 0, 1);
        table.Controls.Add(_tgtDb, 1, 1);
        table.Controls.Add(new Label { Text = "User:" }, 2, 1);
        table.Controls.Add(_tgtUser, 3, 1);
        table.Controls.Add(new Label { Text = "Pass:" }, 4, 1);
        table.Controls.Add(_tgtPass, 5, 0);
        _btnTestTarget.Margin = new Padding(8, 0, 0, 0);
        table.Controls.Add(_btnTestTarget, 5, 1);
        // Master DB firm loader
        table.Controls.Add(_btnLoadMasters, 0, 2);
        table.Controls.Add(_cmbMasterFirms, 1, 2);
        table.SetColumnSpan(_cmbMasterFirms, 2);
        table.SetColumnSpan(_tgtPass, 1);

        panel.Controls.Add(table);
        return panel;
    }

    private Control BuildSettingsPanel()
    {
        var panel = CreateGroupBox("MIGRASYON AYARLARI");
        var flow = new FlowLayoutPanel { AutoSize = true, Padding = new Padding(8), FlowDirection = FlowDirection.TopDown };

        var firmaRow = new FlowLayoutPanel { AutoSize = true };
        firmaRow.Controls.Add(new Label { Text = "Firma ID:", AutoSize = true });
        firmaRow.Controls.Add(_firmaId);
        flow.Controls.Add(firmaRow);

        flow.Controls.Add(_cbStep0);
        flow.Controls.Add(_cbStep1);
        flow.Controls.Add(_cbStep2);
        flow.Controls.Add(_cbResetSeq);

        var btnRow = new FlowLayoutPanel { AutoSize = true };
        btnRow.Controls.Add(_btnMigrate);
        btnRow.Controls.Add(_progressBar);
        _progressBar.Width = 250;
        btnRow.Controls.Add(_progressLabel);
        flow.Controls.Add(btnRow);

        panel.Controls.Add(flow);
        return panel;
    }

    private Panel BuildLogPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var label = new Label { Text = "LOG", Dock = DockStyle.Top, Font = new Font("Segoe UI", 9, FontStyle.Bold), Height = 20 };
        panel.Controls.Add(label);
        panel.Controls.Add(_logBox);
        _logBox.Top = 22;
        _logBox.Height = panel.Height - 22;
        panel.SizeChanged += (_, _) => { _logBox.Height = panel.Height - 22; };
        return panel;
    }

    // ─── Actions ───

    private async Task TestConnection(bool isSource)
    {
        var info = isSource ? GetSourceInfo() : GetTargetInfo();
        var (ok, msg) = await ConnectionValidator.TestAsync(info);
        Log($"{(isSource ? "KAYNAK" : "HEDEF")} Test: {(ok ? "✓" : "✗")} {msg}");
        if (!ok) MessageBox.Show(msg, "Bağlantı Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
        else MessageBox.Show(msg, "Bağlantı Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task StartMigration()
    {
        if (_btnMigrate.Text == "DURDUR")
        {
            _cts?.Cancel();
            return;
        }

        var source = GetSourceInfo();
        var target = GetTargetInfo();

        _btnMigrate.Text = "DURDUR";
        _btnMigrate.BackColor = Color.IndianRed;
        _progressBar.Value = 0;
        _progressBar.Maximum = 100;
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<MigrationProgress>(p =>
            {
                if (p.Mesaj != null) Log(p.Mesaj);
                if (p.SatirSayisi > 0) Log($"  → {p.SatirSayisi} satır eklendi");
                if (p.ToplamTablo > 0)
                {
                    var pct = (int)((double)p.IslenenTablo / p.ToplamTablo * 100);
                    BeginInvoke(() => { _progressBar.Value = Math.Min(pct, 100); _progressLabel.Text = $"%{pct} ({p.IslenenTablo}/{p.ToplamTablo})"; });
                }
                if (p.Tamamlandi)
                {
                    BeginInvoke(() => { _progressBar.Value = 100; _progressLabel.Text = "Tamamlandı!"; });
                }
            });

            var service = new PostgresMigrationService();
            await Task.Run(() => service.MigrateAsync(
                source, target,
                (int)_firmaId.Value,
                progress,
                _cts.Token,
                _cbStep0.Checked, _cbStep1.Checked, _cbStep2.Checked, _cbResetSeq.Checked
            ), _cts.Token);

            Log("═══════════════════════════════");
            Log("MIGRASYON BAŞARIYLA TAMAMLANDI!");
            Log("═══════════════════════════════");
            MessageBox.Show("Migrasyon başarıyla tamamlandı!", "DataTransfer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            Log("Migrasyon kullanıcı tarafından durduruldu.");
        }
        catch (Exception ex)
        {
            Log($"HATA: {ex.Message}");
            if (ex.InnerException != null) Log($"  Inner: {ex.InnerException.Message}");
            MessageBox.Show($"Migrasyon hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnMigrate.Text = "▶ MIGRASYONU BAŞLAT";
            _btnMigrate.BackColor = SystemColors.Control;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task LoadFirmsFromMaster()
    {
        try
        {
            var tgt = GetTargetInfo();
            // Geçici olarak KOAFiloServis_Master'a bağlan
            var masterInfo = new ConnectionInfo
            {
                Host = _tgtHost.Text,
                Port = (int)_tgtPort.Value,
                Database = "KOAFiloServis_Master",
                Username = _tgtUser.Text,
                Password = _tgtPass.Text
            };

            var (ok, msg) = await ConnectionValidator.TestAsync(masterInfo);
            if (!ok)
            {
                // Master'a ulaşılamazsa hedef DB üzerinden dene
                var (ok2, _) = await ConnectionValidator.TestAsync(tgt);
                if (!ok2) { Log($"Master DB bağlantısı başarısız: {msg}"); return; }

                // Hedef DB üzerinden Firmalar'ı sorgula
                await using var conn = new NpgsqlConnection(tgt.BuildConnectionString());
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT \"Id\", \"FirmaKodu\", \"FirmaAdi\", \"DatabaseName\" FROM \"Firmalar\" WHERE \"IsDeleted\" = false ORDER BY \"Id\"", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                _cmbMasterFirms.Items.Clear();
                while (await reader.ReadAsync())
                    _cmbMasterFirms.Items.Add(new FirmItem(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
                Log($"Hedef DB'den {_cmbMasterFirms.Items.Count} firma yüklendi.");
                return;
            }

            await using var masterConn = new NpgsqlConnection(masterInfo.BuildConnectionString());
            await masterConn.OpenAsync();
            await using var cmd2 = new NpgsqlCommand("SELECT \"Id\", \"FirmaKodu\", \"FirmaAdi\", \"DatabaseName\" FROM \"Firmalar\" WHERE \"IsDeleted\" = false ORDER BY \"Id\"", masterConn);
            await using var reader2 = await cmd2.ExecuteReaderAsync();
            _cmbMasterFirms.Items.Clear();
            while (await reader2.ReadAsync())
                _cmbMasterFirms.Items.Add(new FirmItem(reader2.GetInt32(0), reader2.GetString(1), reader2.GetString(2), reader2.IsDBNull(3) ? null : reader2.GetString(3)));
            Log($"Master DB'den {_cmbMasterFirms.Items.Count} firma yüklendi.");
        }
        catch (Exception ex)
        {
            Log($"Firma yükleme hatası: {ex.Message}");
        }
    }

    private void OnFirmSelected()
    {
        if (_cmbMasterFirms.SelectedItem is FirmItem fi)
        {
            _firmaId.Value = fi.Id;
            if (!string.IsNullOrWhiteSpace(fi.DatabaseName))
                _tgtDb.Text = fi.DatabaseName;
        }
    }

    // ─── Helpers ───

    private ConnectionInfo GetSourceInfo() => new()
    {
        Host = _srcHost.Text,
        Port = (int)_srcPort.Value,
        Database = _srcDb.Text,
        Username = _srcUser.Text,
        Password = _srcPass.Text
    };

    private ConnectionInfo GetTargetInfo() => new()
    {
        Host = _tgtHost.Text,
        Port = (int)_tgtPort.Value,
        Database = _tgtDb.Text,
        Username = _tgtUser.Text,
        Password = _tgtPass.Text
    };

    private void Log(string msg)
    {
        if (InvokeRequired)
            BeginInvoke(() => LogInternal(msg));
        else
            LogInternal(msg);
    }

    private void LogInternal(string msg)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        _logBox.AppendText($"[{ts}] {msg}\n");
        _logBox.ScrollToCaret();
    }

    private static Control CreateGroupBox(string title)
    {
        return new GroupBox
        {
            Text = title,
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };
    }

    private record FirmItem(int Id, string Kod, string Ad, string? DatabaseName)
    {
        public override string ToString() => $"[{Id}] {Kod} — {Ad} {(string.IsNullOrEmpty(DatabaseName) ? "(shared)" : $"→ {DatabaseName}")}";
    }
}
