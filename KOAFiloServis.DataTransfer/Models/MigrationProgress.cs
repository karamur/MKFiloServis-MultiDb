namespace KOAFiloServis.DataTransfer.Models;

public class MigrationProgress
{
    public string Adim { get; set; } = "";
    public string Tablo { get; set; } = "";
    public int SatirSayisi { get; set; }
    public int ToplamTablo { get; set; }
    public int IslenenTablo { get; set; }
    public string? Hata { get; set; }
    public bool Tamamlandi { get; set; }
    public string? Mesaj { get; set; }
}

public class ConnectionInfo
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "";
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = "";

    public string BuildConnectionString()
    {
        return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};Pooling=True;MinPoolSize=1;MaxPoolSize=5;Command Timeout=60";
    }

    public override string ToString() => $"postgresql://{Username}@{Host}:{Port}/{Database}";
}
