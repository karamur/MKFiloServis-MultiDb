using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Services.Interfaces;

public interface IGlobalSearchService
{
    Task<GlobalSearchResult> SearchAsync(string searchTerm, int maxResults = 10);
}

public class GlobalSearchResult
{
    public List<SearchResultItem> Cariler { get; set; } = new();
    public List<SearchResultItem> Araclar { get; set; } = new();
    public List<SearchResultItem> Personeller { get; set; } = new();
    public List<SearchResultItem> Faturalar { get; set; } = new();
    public List<SearchResultItem> Guzergahlar { get; set; } = new();

    public int ToplamSonuc => Cariler.Count + Araclar.Count + Personeller.Count + Faturalar.Count + Guzergahlar.Count;

    public List<SearchResultItem> TumSonuclar => 
        Cariler.Concat(Araclar).Concat(Personeller).Concat(Faturalar).Concat(Guzergahlar)
        .OrderByDescending(x => x.Skor)
        .ToList();
}

public class SearchResultItem
{
    public int Id { get; set; }
    public string Baslik { get; set; } = string.Empty;
    public string AltBaslik { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string BadgeClass { get; set; } = string.Empty;
    public int Skor { get; set; } // E�le�me skoru (daha y�ksek = daha iyi e�le�me)
}



