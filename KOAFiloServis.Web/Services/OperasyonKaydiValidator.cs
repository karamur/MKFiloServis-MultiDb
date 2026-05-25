using KOAFiloServis.Shared.Entities;

namespace KOAFiloServis.Web.Services;

/// <summary>
/// OperasyonKaydi için giriş validasyon kuralları.
/// </summary>
public static class OperasyonKaydiValidator
{
    public static List<string> Validate(OperasyonKaydi kayit)
    {
        var errors = new List<string>();

        if (kayit.Tarih == default)
            errors.Add("Tarih zorunludur.");
        if (kayit.Tarih.Year < 2000 || kayit.Tarih.Year > 2100)
            errors.Add("Geçersiz tarih aralığı.");
        if (kayit.GuzergahId <= 0)
            errors.Add("Güzergah seçimi zorunludur.");
        if (kayit.AracId <= 0)
            errors.Add("Araç seçimi zorunludur.");
        if (kayit.SeferSayisi < 0)
            errors.Add("Sefer sayısı negatif olamaz.");
        if (kayit.PuantajCarpani <= 0)
            errors.Add("Puantaj çarpanı sıfırdan büyük olmalıdır.");
        if (!Enum.IsDefined(typeof(SeferSlot), kayit.Slot))
            errors.Add("Geçersiz slot değeri.");
        if (!Enum.IsDefined(typeof(OperasyonDurumu), kayit.OperasyonDurumu))
            errors.Add("Geçersiz operasyon durumu.");

        return errors;
    }

    public static List<string> ValidateToplu(IEnumerable<OperasyonKaydi> kayitlar)
    {
        var errors = new List<string>();
        foreach (var k in kayitlar)
            errors.AddRange(Validate(k));
        return errors;
    }
}
