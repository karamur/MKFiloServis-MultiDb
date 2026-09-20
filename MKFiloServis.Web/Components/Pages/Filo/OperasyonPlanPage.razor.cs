using System.Linq;
using MKFiloServis.Shared.Entities;

namespace MKFiloServis.Web.Components.Pages.Filo;

public partial class OperasyonPlanPage
{
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
