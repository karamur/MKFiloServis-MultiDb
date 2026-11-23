using MKFiloServis.DataSync.Exporters;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MKFiloServis.DataSync.UI;

public sealed class MainForm : Form
{
    private readonly RadioButton _rbPgToSqlite;
    private readonly RadioButton _rbSqliteToPg;
    private readonly Label _lblPgBaslik;
    private readonly Label _lblSqliteBaslik;
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

    private bool PgKaynak => _rbPgToSqlite.Checked;

    public MainForm()
    {
        Text = "MKFiloServis — Veri Aktarim";
        Width = 780;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        var lblYon = new Label { Text = "AKTARIM YONU", Left = 20, Top = 15, Width = 400, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        Controls.Add(lblYon);

        _rbPgToSqlite = new RadioButton { Text = "PostgreSQL ➜ SQLite", Left = 20, Top = 42, Width = 220, Checked = true };
        _rbPgToSqlite.CheckedChanged += (_, _) => YonGuncelle();
        Controls.Add(_rbPgToSqlite);

        _rbSqliteToPg = new RadioButton { Text = "SQLite ➜ PostgreSQL", Left = 260, Top = 42, Width = 220 };
        Controls.Add(_rbSqliteToPg);

        _lblPgBaslik = new Label { Text = "KAYNAK (PostgreSQL)", Left = 20, Top = 80, Width = 400, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        Controls.Add(_lblPgBaslik);

        Controls.Add(new Label { Text = "Host:", Left = 20, Top = 110, Width = 80 });
        _txtHost = new TextBox { Left = 110, Top = 107, Width = 250, Text = "localhost" };
        Controls.Add(_txtHost);

        Controls.Add(new Label { Text = "Port:", Left = 380, Top = 110, Width = 40 });
        _txtPort = new TextBox { Left = 430, Top = 107, Width = 80, Text = "5432" };
        Controls.Add(_txtPort);

        Controls.Add(new Label { Text = "Veritabani:", Left = 20, Top = 140, Width = 80 });
        _txtDb = new TextBox { Left = 110, Top = 137, Width = 400, Text = "MKFiloServis" };
        Controls.Add(_txtDb);

        Controls.Add(new Label { Text = "Kullanici:", Left = 20, Top = 170, Width = 80 });
        _txtUser = new TextBox { Left = 110, Top = 167, Width = 180, Text = "postgres" };
        Controls.Add(_txtUser);

        Controls.Add(new Label { Text = "Parola:", Left = 310, Top = 170, Width = 60 });
        _txtPass = new TextBox { Left = 380, Top = 167, Width = 180, UseSystemPasswordChar = true };
        Controls.Add(_txtPass);

        _lblSqliteBaslik = new Label { Text = "HEDEF (SQLite dosyasi)", Left = 20, Top = 215, Width = 400, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        Controls.Add(_lblSqliteBaslik);

        Controls.Add(new Label { Text = "Dosya:", Left = 20, Top = 245, Width = 80 });
        _txtSqlitePath = new TextBox { Left = 110, Top = 242, Width = 500, Text = @"C:\MKFiloServis\MKFiloServis.db" };
        Controls.Add(_txtSqlitePath);

        _btnBrowse = new Button { Text = "Gozat", Left = 620, Top = 240, Width = 100 };
        _btnBrowse.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog { Filter = "SQLite (*.db)|*.db|Tum dosyalar|*.*" };
            if (ofd.ShowDialog() == DialogResult.OK) _txtSqlitePath.Text = ofd.FileName;
        };
        Controls.Add(_btnBrowse);

        _btnTest = new Button { Text = "Baglantiyi Test Et", Left = 20, Top = 285, Width = 180, Height = 32 };
        _btnTest.Click += async (_, _) => await TestConnectionAsync();
        Controls.Add(_btnTest);

        _btnStart = new Button { Text = "AKTARIMI BASLAT", Left = 560, Top = 285, Width = 180, Height = 32, BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _btnStart.Click += async (_, _) => await StartAsync();
        Controls.Add(_btnStart);

        _progress = new ProgressBar { Left = 20, Top = 330, Width = 720, Height = 18, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 0 };
        Controls.Add(_progress);

        _log = new TextBox
        {
            Left = 20,
            Top = 360,
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

    private void YonGuncelle()
    {
        if (PgKaynak)
        {
            _lblPgBaslik.Text = "KAYNAK (PostgreSQL)";
            _lblSqliteBaslik.Text = "HEDEF (SQLite dosyasi)";
        }
        else
        {
            _lblPgBaslik.Text = "HEDEF (PostgreSQL)";
            _lblSqliteBaslik.Text = "KAYNAK (SQLite dosyasi)";
        }
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
                PgKaynak
                    ? "Hedef SQLite dosyasi bulunamadi.\n\nOnce MKFiloServis uygulamasini bir kere calistirin ki sema olussun."
                    : "Kaynak SQLite dosyasi bulunamadi.",
                "Dosya yok", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var hedefAdi = PgKaynak
            ? _txtSqlitePath.Text
            : $"PostgreSQL: {_txtHost.Text}:{_txtPort.Text}/{_txtDb.Text}";

        if (MessageBox.Show(this,
                $"Asagidaki hedef veritabanindaki VERILER silinip yenileri yuklenecek:\n\n{hedefAdi}\n\nDevam edilsin mi?",
                "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            SetBusy(true);
            if (PgKaynak)
            {
                var exporter = new PostgresToSqliteExporter(BuildConnStr(), _txtSqlitePath.Text, AppendLog);
                await Task.Run(exporter.RunAsync);
            }
            else
            {
                var webDir = ResolveWebAppDirectory();
                var pgConn = BuildConnStr();
                Func<Task>? ensureSchema = webDir is null
                    ? null
                    : () => new WebSchemaProvisioner(pgConn, webDir, AppendLog).RunAsync();
                var importer = new SqliteToPostgresImporter(_txtSqlitePath.Text, pgConn, AppendLog, ensureSchema);
                await Task.Run(importer.RunAsync);
            }
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

    /// <summary>
    /// Hedefte eksik tablo olursa semayi uretecek Web uygulamasinin klasoru. Otomatik bulunamazsa
    /// kullanicidan istenir; vazgecilirse null doner ve eksik tablo durumunda aktarim durur.
    /// </summary>
    private string? ResolveWebAppDirectory()
    {
        var located = WebSchemaProvisioner.LocateWebAppDirectory();
        if (located is not null)
        {
            AppendLog($"▸ Sema kaynagi (Web uygulamasi): {located}");
            return located;
        }

        if (MessageBox.Show(this,
                "MKFiloServis Web uygulamasi otomatik bulunamadi.\n\nHedef PostgreSQL'de eksik tablolar varsa sema bu uygulamanin modelinden uretilir. " +
                "MKFiloServis.Web.dll dosyasinin bulundugu klasoru simdi secmek ister misiniz?",
                "Web uygulamasi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return null;

        using var dialog = new FolderBrowserDialog { Description = "MKFiloServis.Web.dll iceren klasoru secin", ShowNewFolderButton = false };
        if (dialog.ShowDialog(this) != DialogResult.OK) return null;
        if (!WebSchemaProvisioner.IsWebAppDirectory(dialog.SelectedPath))
        {
            MessageBox.Show(this, "Secilen klasorde MKFiloServis.Web.dll bulunamadi.", "Web uygulamasi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
        AppendLog($"▸ Sema kaynagi (Web uygulamasi): {dialog.SelectedPath}");
        return dialog.SelectedPath;
    }

    private void SetBusy(bool busy)
    {
        _btnStart.Enabled = !busy;
        _btnTest.Enabled = !busy;
        _btnBrowse.Enabled = !busy;
        _progress.MarqueeAnimationSpeed = busy ? 30 : 0;
    }
}


