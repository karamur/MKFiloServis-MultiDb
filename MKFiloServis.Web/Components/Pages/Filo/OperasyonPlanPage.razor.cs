using System.Linq;
using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Components.Pages.Filo;

public partial class OperasyonPlanPage
{
    private bool EkSeferFormuAcik;
    private DateTime EkSeferTarihi = DateTime.Today;
    private int EkSeferGuzergahId;
    private int EkSeferAracId;
    private int EkSeferSoforId;
    private decimal EkSeferAdedi = 1m;
    private decimal? EkSeferFiyati;
    private ServisTuru EkSeferServisTuru = ServisTuru.SabahAksam;

    private IEnumerable<KeyValuePair<int, string>> EkSeferAracSecenekleri =>
        Planlar
            .Where(p => p.GuzergahId == EkSeferGuzergahId)
            .GroupBy(p => p.AracId)
            .Select(g => new KeyValuePair<int, string>(g.Key, GetAracAd(g.Key)))
            .OrderBy(x => x.Value);

    private void EkSeferFormunuAc()
    {
        EkSeferFormuAcik = true;
        EkSeferTarihi = AyBaslangic >= DateTime.Today.AddMonths(-1) && AyBaslangic <= DateTime.Today.AddMonths(1)
            ? DateTime.Today
            : AyBaslangic;
        EkSeferGuzergahId = Planlar.Select(p => p.GuzergahId).Distinct().FirstOrDefault();
        EkSeferAracId = 0;
        EkSeferSoforId = 0;
        EkSeferAdedi = 1m;
        EkSeferFiyati = null;
        EkSeferServisTuru = ServisTuru.SabahAksam;
        EkSeferGuzergahDegisti();
    }

    private void EkSeferFormunuKapat() => EkSeferFormuAcik = false;

    private void EkSeferGuzergahDegisti()
    {
        var arac = EkSeferAracSecenekleri.FirstOrDefault();
        EkSeferAracId = arac.Key;
        EkSeferAracDegisti();
    }

    private void EkSeferAracDegisti()
    {
        var plan = Planlar.FirstOrDefault(p =>
            p.GuzergahId == EkSeferGuzergahId && p.AracId == EkSeferAracId);
        if (plan is null)
            return;

        EkSeferSoforId = plan.SoforId;
        EkSeferFiyati = plan.KurumSeferUcretiSnapshot;
    }

    private async Task EkSeferPlaniniEkleAsync()
    {
        if (EkSeferTarihi == default || EkSeferGuzergahId <= 0 || EkSeferAracId <= 0 || EkSeferSoforId <= 0 || EkSeferAdedi <= 0m)
        {
            HataMesaji = "Ek sefer için tarih, güzergâh, plaka, şoför ve sıfırdan büyük sefer sayısı girilmelidir.";
            BilgiMesaji = null;
            return;
        }

        Yukleniyor = true;
        HataMesaji = null;
        BilgiMesaji = null;
        try
        {
            await PlanService.EkSeferPlanSatiriEkleAsync(
                EkSeferTarihi,
                EkSeferGuzergahId,
                EkSeferAracId,
                EkSeferSoforId,
                EkSeferAdedi,
                EkSeferFiyati,
                EkSeferServisTuru);

            EkSeferFormuAcik = false;
            BilgiMesaji = "Ek sefer planı kaydedildi.";
            await YukleAsync();
        }
        catch (Exception ex)
        {
            HataMesaji = $"Ek sefer planı kaydedilemedi: {ex.Message}";
        }
        finally
        {
            Yukleniyor = false;
        }
    }

    private void HaftaSonuOlanlariIsaretle()
        => SecimleriTarihTipineGoreBelirle(t => t.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
            "Hafta sonu kayıtları seçildi.");

    private void HaftaIciOlanlariIsaretle()
        => SecimleriTarihTipineGoreBelirle(t => t.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday),
            "Hafta içi kayıtları seçildi.");

    private async Task MesaiOlarakIsaretlePlanlarAsync()
    {
        var seciliPlanlar = Planlar
            .Where(p => SecilenIdler.Contains(p.Id))
            .Where(p => p.Durum == OperasyonPlanDurumu.Planlandi && !p.FiloGunlukPuantajId.HasValue)
            .ToList();

        if (seciliPlanlar.Count == 0)
        {
            HataMesaji = "Mesai olarak işaretlenecek uygun plan bulunamadı.";
            BilgiMesaji = null;
            return;
        }

        Yukleniyor = true;
        HataMesaji = null;
        BilgiMesaji = null;
        try
        {
            foreach (var plan in seciliPlanlar)
            {
                plan.ServisTuru = ServisTuru.YardaMesai;
            }

            var kaydedilen = await PlanService.PlanlariGuncelleAsync(seciliPlanlar);
            BaslangicDegerleri = Planlar.ToDictionary(p => p.Id, p => (p.PlanlananSefer, p.ServisTuru));
            BilgiMesaji = $"{kaydedilen} plan mesai olarak işaretlendi ve kaydedildi.";
        }
        catch (Exception ex)
        {
            HataMesaji = $"Mesai işaretleme kaydedilemedi: {ex.Message}";
        }
        finally
        {
            Yukleniyor = false;
        }
    }

    private void SecimleriTarihTipineGoreBelirle(Func<DateTime, bool> tarihKriteri, string basariMesaji)
    {
        var uygunIdler = Planlar
            .Where(p => p.Durum == OperasyonPlanDurumu.Planlandi && !p.FiloGunlukPuantajId.HasValue)
            .Where(p => tarihKriteri(p.Tarih))
            .Select(p => p.Id)
            .ToList();

        SecilenIdler.Clear();
        foreach (var id in uygunIdler)
        {
            SecilenIdler.Add(id);
        }

        BilgiMesaji = uygunIdler.Count == 0
            ? "İşaretlenecek uygun kayıt bulunamadı."
            : $"{basariMesaji} ({uygunIdler.Count})";
        HataMesaji = null;
    }
}
