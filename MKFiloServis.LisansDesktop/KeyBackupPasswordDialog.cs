internal sealed class KeyBackupPasswordDialog : Form
{
    private readonly TextBox _password = new() { UseSystemPasswordChar = true, Width = 350 };
    private readonly TextBox _confirmation = new() { UseSystemPasswordChar = true, Width = 350 };
    private readonly bool _creatingBackup;
    public char[] Password { get; private set; } = [];

    public KeyBackupPasswordDialog(bool creatingBackup)
    {
        _creatingBackup = creatingBackup;
        Text = creatingBackup ? "Şifreli anahtar yedeği" : "Şifreli anahtar yedeğini aç";
        ClientSize = new Size(390, creatingBackup ? 260 : 175);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        var label = new Label { Text = creatingBackup ? "Yedek parolası (en az 16 karakter)" : "Yedek parolası", AutoSize = true, Location = new Point(20, 18) };
        _password.Location = new Point(20, 43);
        Controls.AddRange([label, _password]);
        if (creatingBackup)
        {
            Controls.Add(new Label { Text = "Parolayı tekrar girin", AutoSize = true, Location = new Point(20, 80) });
            _confirmation.Location = new Point(20, 105);
            Controls.Add(_confirmation);
            Controls.Add(new Label
            {
                Text = "Parolayı yedek dosyasından ayrı saklayın. Parola olmadan yedek açılamaz.",
                Location = new Point(20, 143), Size = new Size(350, 48)
            });
        }
        var accept = new Button { Text = "Devam", Size = new Size(100, 30), Location = new Point(160, ClientSize.Height - 48) };
        var cancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Size = new Size(100, 30), Location = new Point(270, ClientSize.Height - 48) };
        accept.Click += (_, _) =>
        {
            if (_creatingBackup && (_password.Text.Length < 16 || !_password.Text.AsSpan().SequenceEqual(_confirmation.Text.AsSpan())))
            {
                MessageBox.Show(this, "En az 16 karakterlik aynı parolayı iki alana girin.", "Yedek parolası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Password = _password.Text.ToCharArray();
            _password.Clear();
            _confirmation.Clear();
            DialogResult = DialogResult.OK;
        };
        Controls.AddRange([accept, cancel]);
        AcceptButton = accept;
        CancelButton = cancel;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Array.Clear(Password);
            _password.Clear();
            _confirmation.Clear();
        }
        base.Dispose(disposing);
    }
}
