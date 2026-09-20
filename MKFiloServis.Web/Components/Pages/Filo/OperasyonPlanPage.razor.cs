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

    private void MesaiOlarakIsaretlePlanlar()
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

        foreach (var plan in seciliPlanlar)
        {
            plan.ServisTuru = ServisTuru.YardaMesai;
        }

        BilgiMesaji = $"{seciliPlanlar.Count} plan mesai olarak işaretlendi. Kaydetmek için 'Plan Değişikliklerini Kaydet' kullanın.";
        HataMesaji = null;
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
