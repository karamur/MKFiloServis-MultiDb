using MKFiloServis.Shared.Time;

namespace MKFiloServis.Tests;

public sealed class BusinessTimeTests
{
    [Fact]
    public void Date_boundary_uses_Istanbul_instead_of_host_timezone()
    {
        var utc = new DateTime(2026, 10, 8, 21, 30, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2026, 10, 9), BusinessTime.DateAt(utc));
    }
}
