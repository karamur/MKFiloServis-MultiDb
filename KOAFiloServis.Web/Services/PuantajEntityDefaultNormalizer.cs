using KOAFiloServis.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace KOAFiloServis.Web.Services;

internal static class PuantajEntityDefaultNormalizer
{
    internal static bool EnsureDefaults(PuantajKayit kayit)
    {
        var changed = false;

        if (kayit.OnayDurum == default)
        {
            kayit.OnayDurum = PuantajOnayDurum.Taslak;
            changed = true;
        }

        if (kayit.GelirOdemeDurumu == default)
        {
            kayit.GelirOdemeDurumu = PuantajOdemeDurum.Odenmedi;
            changed = true;
        }

        if (kayit.GiderOdemeDurumu == default)
        {
            kayit.GiderOdemeDurumu = PuantajOdemeDurum.Odenmedi;
            changed = true;
        }

        if (kayit.SoforOdemeTipi == default)
        {
            kayit.SoforOdemeTipi = SoforOdemeTipi.Ozmal;
            changed = true;
        }

        if (kayit.Kaynak == default)
        {
            kayit.Kaynak = PuantajKaynak.Manuel;
            changed = true;
        }

        if (kayit.Yon == default)
        {
            kayit.Yon = PuantajYon.SabahAksam;
            changed = true;
        }

        return changed;
    }

    internal static void EnsureTrackedDefaults(ChangeTracker changeTracker)
    {
        foreach (var entry in changeTracker.Entries<PuantajKayit>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            EnsureDefaults(entry.Entity);

            if (entry.State == EntityState.Modified)
            {
                MarkRequiredStatusColumnsForWriteBack(entry);
            }
        }
    }

    private static void MarkRequiredStatusColumnsForWriteBack(EntityEntry<PuantajKayit> entry)
    {
        if (entry.Entity.GelirOdemeDurumu == PuantajOdemeDurum.Odenmedi)
            entry.Property(x => x.GelirOdemeDurumu).IsModified = true;

        if (entry.Entity.GiderOdemeDurumu == PuantajOdemeDurum.Odenmedi)
            entry.Property(x => x.GiderOdemeDurumu).IsModified = true;

        if (entry.Entity.OnayDurum == PuantajOnayDurum.Taslak)
            entry.Property(x => x.OnayDurum).IsModified = true;

        if (entry.Entity.Kaynak == PuantajKaynak.Manuel)
            entry.Property(x => x.Kaynak).IsModified = true;

        if (entry.Entity.SoforOdemeTipi == SoforOdemeTipi.Ozmal)
            entry.Property(x => x.SoforOdemeTipi).IsModified = true;

        if (entry.Entity.Yon == PuantajYon.SabahAksam)
            entry.Property(x => x.Yon).IsModified = true;
    }
}
