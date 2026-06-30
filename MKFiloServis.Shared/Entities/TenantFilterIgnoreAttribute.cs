namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Bu attribute ile işaretlenen <see cref="IFirmaTenant"/> entity'leri,
/// <c>ApplicationDbContext</c> içindeki global <c>FirmaId</c> query filter'ından muaf tutulur.
/// <para>
/// Kullanım amacı: Bütçe ve Muhasebe gibi konsolide raporlama gerektiren modüllerin
/// kayıtları (kullanıcı talebi K7) firma izolasyonundan etkilenmemelidir.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TenantFilterIgnoreAttribute : Attribute
{
}


