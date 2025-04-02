using Dave.Benchmarks.Core.Models.Entities;

namespace Dave.Benchmarks.Web.Extensions;

public static class QueryExtensions
{
    public static IQueryable<T> FilterLayers<T>(this IQueryable<T> query, int? layerId) where T : Datum
    {
        if (layerId.HasValue)
            query = query.Where(d => d.LayerId == layerId.Value);
        return query;
    }
}
