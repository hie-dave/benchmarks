using Dave.Benchmarks.Core.Services.Spatial;

namespace Dave.Benchmarks.Tests.Services;

public class GeoDistanceTests
{
    [Fact]
    public void HaversineKm_IdenticalPoints_ReturnsZero()
    {
        double d = GeoDistance.HaversineKm(-33.9, 151.2, -33.9, 151.2);
        Assert.Equal(0.0, d, 8);
    }

    [Fact]
    public void HaversineKm_IsSymmetric()
    {
        double ab = GeoDistance.HaversineKm(-33.86, 151.21, -37.81, 144.96);
        double ba = GeoDistance.HaversineKm(-37.81, 144.96, -33.86, 151.21);
        Assert.Equal(ab, ba, 8);
    }

    [Fact]
    public void HaversineKm_KnownRoute_IsInExpectedRange()
    {
        // Sydney to Melbourne ~= 714 km great-circle
        double d = GeoDistance.HaversineKm(-33.8688, 151.2093, -37.8136, 144.9631);
        Assert.InRange(d, 713, 715);
    }
}
