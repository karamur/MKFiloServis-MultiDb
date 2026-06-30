using System.Collections.Concurrent;

namespace MKFiloServis.Web.Services;

/// <summary>
/// Güzergah düzenlemelerini takip eder.
/// Puantaj ve Planlama modüllerinde "güzergah değişti, güncelle" uyarısı için kullanılır.
/// </summary>
public class GuzergahDegisiklikUyariService
{
    private readonly ConcurrentDictionary<int, DateTime> _degisenGuzergahlar = new();

    /// <summary>Güzergah değişikliğini kaydet</summary>
    public void DegisiklikKaydet(int guzergahId)
    {
        _degisenGuzergahlar[guzergahId] = DateTime.UtcNow;
    }

    /// <summary>Değişen güzergah ID'lerini döner</summary>
    public ICollection<int> DegisenGuzergahIds => _degisenGuzergahlar.Keys;

    /// <summary>Değişiklik var mı?</summary>
    public bool DegisiklikVar => !_degisenGuzergahlar.IsEmpty;

    /// <summary>Tüm uyarıları temizle (kullanıcı "güncelle" yaptı veya "hatırlatma" kapattı)</summary>
    public void TumunuTemizle()
    {
        _degisenGuzergahlar.Clear();
    }

    /// <summary>Belirli bir güzergahın uyarısını temizle</summary>
    public void Temizle(int guzergahId)
    {
        _degisenGuzergahlar.TryRemove(guzergahId, out _);
    }

    /// <summary>Değişikliklerin süresi (dakika) — eski uyarıları temizlemek için</summary>
    public int DegisenSayisi => _degisenGuzergahlar.Count;
}


