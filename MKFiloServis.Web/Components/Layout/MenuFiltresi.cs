namespace MKFiloServis.Web.Components.Layout;

/// <summary>Bir alt grubun görünür öğeleri.</summary>
public sealed record GorunenAlt(MenuAltGrubu Alt, string Anahtar, IReadOnlyList<MenuOgesi> Ogeler);

/// <summary>Görünür öğesi olan ana grup.</summary>
public sealed record GorunenGrup(MenuGrubu Grup, IReadOnlyList<GorunenAlt> Altlar);

/// <summary>Menünün kökündeki görünür düğüm: ya tek bağlantı ya da grup.</summary>
public sealed record GorunenKok(MenuOgesi? Oge, GorunenGrup? Grup)
{
    public IEnumerable<MenuOgesi> TumOgeler => Oge is not null ? [Oge] : Grup!.Altlar.SelectMany(a => a.Ogeler);
}

/// <summary>
/// Menü tanımını kullanıcının görebildiği parçaya indirger. Bileşen ve testler aynı kodu kullanır.
/// Bir öğe görünürse: üst grup kuralı VE alt grup kuralı VE kendi kuralı geçmiştir; görünür öğesi kalmayan
/// alt grup ve grup hiç çizilmez.
/// </summary>
public static class MenuFiltresi
{
    public static string AltAnahtar(MenuGrubu grup, MenuAltGrubu alt) => $"{grup.Anahtar}/{alt.Baslik}";

    public static IReadOnlyList<GorunenKok> Filtrele(IEnumerable<IMenuDugumu> kokler, MenuGorunurluk gorunurluk)
    {
        var sonuc = new List<GorunenKok>();
        foreach (var dugum in kokler)
        {
            switch (dugum)
            {
                case MenuOgesi oge when gorunurluk.Gorunur(oge.Kural):
                    sonuc.Add(new GorunenKok(oge, null));
                    break;

                case MenuGrubu grup when gorunurluk.Gorunur(grup.Kural):
                    var altlar = new List<GorunenAlt>();
                    foreach (var alt in grup.AltGruplar)
                    {
                        if (!gorunurluk.Gorunur(alt.Kural))
                            continue;

                        var ogeler = alt.Ogeler.Where(o => gorunurluk.Gorunur(o.Kural)).ToList();
                        if (ogeler.Count > 0)
                            altlar.Add(new GorunenAlt(alt, AltAnahtar(grup, alt), ogeler));
                    }

                    if (altlar.Count > 0)
                        sonuc.Add(new GorunenKok(null, new GorunenGrup(grup, altlar)));
                    break;
            }
        }

        return sonuc;
    }

    /// <summary>Görünürlüğe bakmadan menüdeki tüm bağlantılar (yapı testleri için).</summary>
    public static IEnumerable<MenuOgesi> TumOgeler(IEnumerable<IMenuDugumu> kokler)
        => kokler.SelectMany(d => d switch
        {
            MenuOgesi oge => [oge],
            MenuGrubu grup => grup.AltGruplar.SelectMany(a => a.Ogeler),
            _ => Enumerable.Empty<MenuOgesi>()
        });
}
