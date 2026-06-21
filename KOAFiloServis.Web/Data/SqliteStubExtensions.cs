using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace — EF Core namespace'inde olmalı ki tüm mevcut IsSqlite()
// çağrıları using değişikliği olmadan derlensin. Proje PostgreSQL-only mimariye geçti.
namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// Microsoft.EntityFrameworkCore.Sqlite paketi kaldırıldığı için
/// DatabaseFacade.IsSqlite() extension metodunun yerine geçen stub.
/// Proje sadece PostgreSQL kullandığı için her zaman false döner.
/// Migration helper'lardaki SQLite fallback'leri dead code olur.
/// </summary>
internal static class SqliteStubExtensions
{
    public static bool IsSqlite(this DatabaseFacade _) => false;
}
