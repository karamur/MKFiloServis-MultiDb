namespace KOAFiloServis.Web.Models;

public sealed class SyncOzetRaporu
{
    public Guid BatchId { get; set; }
    public string IslemTipi { get; set; } = "";
    public DateTime Baslangic { get; set; }
    public DateTime Bitis { get; set; }
    public TimeSpan Sure => Bitis - Baslangic;
    public int Olusturulan { get; set; }
    public int Guncellenen { get; set; }
    public int AtlananKilitli { get; set; }
    public int AtlananManuelKaynak { get; set; }
    public int RestoreEdilen { get; set; }
    public int SoftDeleteYapilan { get; set; }
    public int Hata { get; set; }
    public List<string> HataMesajlari { get; set; } = [];

    public string Ozet =>
        $"{Olusturulan} olusturuldu, {Guncellenen} guncellendi, " +
        $"{AtlananKilitli} atlandi (kilitli), {RestoreEdilen} restore, " +
        $"{SoftDeleteYapilan} silindi" +
        (Hata > 0 ? $", {Hata} hata" : "");
}
