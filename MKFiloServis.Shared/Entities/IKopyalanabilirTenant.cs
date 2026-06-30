namespace MKFiloServis.Shared.Entities;

/// <summary>
/// Şirketler arası kopyalama (K8) sırasında üretilen kayıtların audit alanlarını taşır.
/// <para>
/// Bu interface'i implement eden entity'lerde, başka bir firmadan kopyalanmış kayıtlar
/// <see cref="KaynakFirmaId"/> ve <see cref="KaynakKayitId"/> üzerinden izlenebilir.
/// </para>
/// <list type="bullet">
///   <item><b>Yeni oluşturulan kayıt:</b> her iki alan <c>null</c>.</item>
///   <item><b>Kopyalanmış kayıt:</b> kaynak firmanın id'si ve kaynak kayıt id'si dolu.</item>
/// </list>
/// </summary>
public interface IKopyalanabilirTenant : IFirmaTenant
{
    /// <summary>Bu kayıt başka bir firmadan kopyalandıysa, kaynak firmanın Id'si.</summary>
    int? KaynakFirmaId { get; set; }

    /// <summary>Bu kayıt başka bir firmadan kopyalandıysa, kaynak kayıt Id'si.</summary>
    int? KaynakKayitId { get; set; }
}


