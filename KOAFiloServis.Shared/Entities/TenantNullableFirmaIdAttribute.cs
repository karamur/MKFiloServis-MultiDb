namespace KOAFiloServis.Shared.Entities;

/// <summary>
/// Bu attribute ile işaretlenen <see cref="IFirmaTenant"/> entity'leri için,
/// <c>ApplicationDbContext</c> otomatik olarak <c>FirmaId</c> kolonuna
/// <c>IsRequired()</c> uygulamaz.
/// <para>
/// Kullanım amacı (K9 geçiş deseni): Aşama C aşamalı tenant migrasyonu sırasında
/// yeni eklenen entity'ler önce <b>nullable FirmaId</b> ile ayağa kalkmalı, ardından
/// startup backfill ile var olan satırlar doldurulmalı, sonraki bir migration'da bu
/// attribute kaldırılıp <c>NOT NULL</c>'a alınmalıdır. Bu attribute o "ara aşama"
/// boyunca migration'ın erken NOT NULL üretmesini engeller.
/// </para>
/// </summary>
[Obsolete("Nihai mimari: Tum FirmaId'ler NOT NULL. Bu attribute kullanilmiyor.")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TenantNullableFirmaIdAttribute : Attribute
{
}
