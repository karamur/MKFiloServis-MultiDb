using MKFiloServis.DataSync.Exporters;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MKFiloServis.DataSync.UI;

public sealed class MainForm : Form
{
    private readonly TextBox _txtHost;
    private readonly TextBox _txtPort;
    private readonly TextBox _txtDb;
    private readonly TextBox _txtUser;
    private readonly TextBox _txtPass;
    private readonly TextBox _txtSqlitePath;
    private readonly Button _btnBrowse;
    private readonly Button _btnTest;
    private readonly Button _btnStart;
    private readonly ProgressBar _progress;
    private readonly TextBox _log;

    public MainForm()
    {
        Text = "MKFiloServis — Veri Aktarim (PostgreSQL ➜ SQLite)";
        Width = 780;
        Height = 640;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        var lblKaynak = new Label { Text = "KAYNAK (PostgreSQL)", Left = 20, Top = 15, Width = 400, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        Controls.Add(lblKaynak);

        Controls.Add(new Label { Text = "Host:", Left = 20, Top = 45, Width = 80 });
        _txtHost = new TextBox { Left = 110, Top = 42, Width = 250, Text = "localhost" };
        Controls.Add(_txtHost);

        Controls.Add(new Label { Text = "Port:", Left = 380, Top = 45, Width = 40 });
        _txtPort = new TextBox { Left = 430, Top = 42, Width = 80, Text = "5432" };
        Controls.Add(_txtPort);

        Controls.Add(new Label { Text = "Veritabani:", Left = 20, Top = 75, Width = 80 });
        _txtDb = new TextBox { Left = 110, Top = 72, Width = 400, Text = "DestekCRMServisBlazorDb" };
        Controls.Add(_txtDb);

        Controls.Add(new Label { Text = "Kullanici:", Left = 20, Top = 105, Width = 80 });
        _txtUser = new TextBox { Left = 110, Top = 102, Width = 180, Text = "postgres" };
        Controls.Add(_txtUser);

        Controls.Add(new Label { Text = "Parola:", Left = 310, Top = 105, Width = 60 });
        _txtPass = new TextBox { Left = 380, Top = 102, Width = 180, UseSystemPasswordChar = true };
        Controls.Add(_txtPass);

        var lblHedef = new Label { Text = "HEDEF (SQLite dosyasi)", Left = 20, Top = 150, Width = 400, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        Controls.Add(lblHedef);

        Controls.Add(new Label { Text = "Dosya:", Left = 20, Top = 180, Width = 80 });
        _txtSqlitePath = new TextBox { Left = 110, Top = 177, Width = 500, Text = @"C:\MKFiloServis\koa.db" };
        Controls.Add(_txtSqlitePath);

        _btnBrowse = new Button { Text = "Gozat", Left = 620, Top = 175, Width = 100 };
        _btnBrowse.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog { Filter = "SQLite (*.db)|*.db|Tum dosyalar|*.*" };
            if (ofd.ShowDialog() == DialogResult.OK) _txtSqlitePath.Text = ofd.FileName;
        };
        Controls.Add(_btnBrowse);

        _btnTest = new Button { Text = "Baglantiyi Test Et", Left = 20, Top = 220, Width = 180, Height = 32 };
        _btnTest.Click += async (_, _) => await TestConnectionAsync();
        Controls.Add(_btnTest);

        _btnStart = new Button { Text = "AKTARIMI BASLAT", Left = 560, Top = 220, Width = 180, Height = 32, BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnStart.Click += async (_, _) => await StartAsync();
        Controls.Add(_btnStart);

        _progress = new ProgressBar { Left = 20, Top = 265, Width = 720, Height = 18, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 0 };
        Controls.Add(_progress);

        _log = new TextBox
        {
            Left = 20,
            Top = 295,
            Width = 720,
            Height = 280,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Consolas", 9F),
            BackColor = Color.Black,
            ForeColor = Color.LightGreen
        };
        Controls.Add(_log);
    }

    private string BuildConnStr() =>
        $"Host={_txtHost.Text};Port={_txtPort.Text};Database={_txtDb.Text};Username={_txtUser.Text};Password={_txtPass.Text};Pooling=false;Timeout=15;";

    private void AppendLog(string msg)
    {
        if (InvokeRequired) { Invoke(() => AppendLog(msg)); return; }
        _log.AppendText(msg + Environment.NewLine);
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            _btnTest.Enabled = false;
            AppendLog("▸ PostgreSQL baglantisi test ediliyor...");
            await using var conn = new Npgsql.NpgsqlConnection(BuildConnStr());
            await conn.OpenAsync();
            AppendLog($"✔ Baglandi. Server: {conn.ServerVersion}");
        }
        catch (Exception ex)
        {
            AppendLog($"✖ HATA: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Baglanti hatasi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnTest.Enabled = true;
        }
    }

    private async Task StartAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtSqlitePath.Text) || !System.IO.File.Exists(_txtSqlitePath.Text))
        {
            MessageBox.Show(this,
                "Hedef SQLite dosyasi bulunamadi.\n\n" +
                "Once MKFiloServis uygulamasini bir kere calistirin ki sema olussun.",
                "Dosya yok", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show(this,
                $"Asagidaki hedef veritabanindaki VERILER silinip yenileri yuklenecek:\n\n{_txtSqlitePath.Text}\n\nDevam edilsin mi?",
                "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            SetBusy(true);
            var exporter = new PostgresToSqliteExporter(BuildConnStr(), _txtSqlitePath.Text, AppendLog);
            await Task.Run(exporter.RunAsync);
            MessageBox.Show(this, "Aktarim tamamlandi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            AppendLog($"✖ HATA: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Aktarim hatasi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _btnStart.Enabled = !busy;
        _btnTest.Enabled = !busy;
        _btnBrowse.Enabled = !busy;
        _progress.MarqueeAnimationSpeed = busy ? 30 : 0;
    }
}


