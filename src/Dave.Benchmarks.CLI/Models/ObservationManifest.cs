namespace Dave.Benchmarks.CLI.Models;

public class ObservationManifest
{
    public string Collection { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Kind { get; set; } = "site";
    public string Metadata { get; set; } = "{}";
    public string MatchingStrategy { get; set; } = "by_name";
    public int? MaxDistanceKm { get; set; }
    public List<ObservationFileManifest> Files { get; set; } = [];
}

public class ObservationFileManifest
{
    public string Path { get; set; } = string.Empty;
    public string DateColumn { get; set; } = "date";
    public string? SiteColumn { get; set; } = "site";
    public string? LongitudeColumn { get; set; }
    public string? LatitudeColumn { get; set; }
    public string TemporalResolution { get; set; } = string.Empty;
    public List<ObservationVariableManifest> Variables { get; set; } = [];
}

public class ObservationVariableManifest
{
    public string Column { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Units { get; set; } = string.Empty;
    public string Level { get; set; } = "gridcell";
    public string Layer { get; set; } = "mean";
}
