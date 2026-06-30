namespace MKFiloServis.Web.Models;

/// <summary>
/// Sayfalanmış veri sonucu için generic wrapper
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedResult() { }

    public PagedResult(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Boş sonuç döndürür
    /// </summary>
    public static PagedResult<T> Empty(int pageSize = 25) => new()
    {
        Items = new List<T>(),
        TotalCount = 0,
        PageNumber = 1,
        PageSize = pageSize
    };
}

/// <summary>
/// Sayfalama parametreleri
/// </summary>
public class PagingParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 25;

    public int PageNumber { get; set; } = 1;
    
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    /// <summary>
    /// Skip değeri (EF Core sorguları için)
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;
}



