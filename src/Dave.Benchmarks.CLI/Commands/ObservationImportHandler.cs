using System.Globalization;
using System.IO.Compression;
using Dave.Benchmarks.CLI.Configuration;
using Dave.Benchmarks.CLI.Models;
using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Models.Importer;
using Dave.Benchmarks.Core.Services;
using LpjGuess.Core.Models;
using LpjGuess.Core.Models.Entities;
using LpjGuess.Core.Services;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dave.Benchmarks.CLI.Commands;

public class ObservationImportHandler
{
    private const int BatchSize = 1000;
    private readonly IApiClient api;
    private readonly GitLabCuratorAuthenticator authenticator;
    private readonly ApiSettings settings;
    private readonly ILogger<ObservationImportHandler> logger;

    public ObservationImportHandler(
        IApiClient api,
        GitLabCuratorAuthenticator authenticator,
        ApiSettings settings,
        ILogger<ObservationImportHandler> logger)
    {
        this.api = api;
        this.authenticator = authenticator;
        this.settings = settings;
        this.logger = logger;
    }

    public async Task RunAsync(ObservationImportOptions options, CancellationToken cancellationToken = default)
    {
        string manifestPath = Path.GetFullPath(options.Manifest);
        ObservationManifest manifest = ReadManifest(manifestPath);
        string baseDirectory = Path.GetDirectoryName(manifestPath)!;
        ValidateManifest(manifest, baseDirectory);

        if (!options.DryRun && api is ProductionApiClient production &&
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ApiSettings.TokenEnvironmentVariable)) &&
            string.IsNullOrWhiteSpace(settings.AccessToken))
        {
            production.SetBearerToken(await authenticator.AuthenticateAsync(cancellationToken));
        }

        int? groupId = null;
        try
        {
            DatasetGroupKind kind = ParseKind(manifest.Kind);
            MatchingStrategy strategy = ParseStrategy(manifest.MatchingStrategy);
            groupId = await api.CreateObservationGroupAsync(
                manifest.Collection, manifest.Source, manifest.Version, manifest.Description,
                kind, manifest.Metadata, cancellationToken);

            string temporalResolution = manifest.Files.Select(f => f.TemporalResolution).Distinct(StringComparer.Ordinal).Single();
            Dictionary<string, int> datasets = [];
            IReadOnlyDictionary<string, HashSet<(string Name, string Layer)>>? siteVariables = null;
            if (kind == DatasetGroupKind.ObservationSite)
            {
                siteVariables = DiscoverSiteVariables(manifest, baseDirectory);
                foreach (string site in siteVariables.Keys.Order(StringComparer.Ordinal))
                {
                    datasets[site] = await api.CreateObservationDatasetAsync(
                        groupId.Value, site, $"{manifest.Collection} observations for {site}", temporalResolution,
                        site, MatchingStrategy.ByName, null, "{}", cancellationToken);
                }
            }
            else
            {
                datasets[manifest.Collection] = await api.CreateObservationDatasetAsync(
                    groupId.Value, manifest.Collection, manifest.Description, temporalResolution,
                    manifest.Collection, strategy, manifest.MaxDistanceKm, "{}", cancellationToken);
            }

            Dictionary<(string Dataset, string Variable, string Layer), int> layerIds = [];
            foreach ((string datasetName, int datasetId) in datasets)
            {
                foreach (ObservationVariableManifest variable in manifest.Files.SelectMany(f => f.Variables)
                             .GroupBy(v => (v.Name, v.Layer)).Select(g => g.First())
                             .Where(v => siteVariables == null || siteVariables[datasetName].Contains((v.Name, v.Layer))))
                {
                    AggregationLevel level = Enum.Parse<AggregationLevel>(variable.Level, true);
                int variableId = await api.CreateObservationVariableAsync(datasetId, new CreateVariableRequest
                {
                    Name = variable.Name,
                    Description = variable.Description,
                    Units = variable.Units,
                    Level = level,
                    ComparisonOutput = variable.Target?.Output
                }, cancellationToken);
                    int layerId = await api.CreateObservationLayerAsync(variableId, new CreateLayerRequest
                    {
                    Name = variable.Layer,
                    Description = variable.Description,
                    ComparisonLayer = variable.Target?.Layer
                }, cancellationToken);
                    layerIds[(datasetName, variable.Name, variable.Layer)] = layerId;
                }
            }

            await UploadFiles(manifest, baseDirectory, kind, layerIds, cancellationToken);
            await api.CompleteObservationGroupAsync(groupId.Value, cancellationToken);
            if (options.Activate) await api.ActivateObservationGroupAsync(groupId.Value, cancellationToken);
            logger.LogInformation("Imported observation release {Collection}/{Version} as group {GroupId}",
                manifest.Collection, manifest.Version, groupId);
        }
        catch (Exception importError) when (groupId.HasValue && options.CleanupOnFailure)
        {
            try
            {
                logger.LogWarning(importError,
                    "Observation import failed; deleting partial group {GroupId}", groupId.Value);
                await api.DeleteObservationGroupAsync(groupId.Value, CancellationToken.None);
            }
            catch (Exception cleanupError)
            {
                logger.LogError(cleanupError,
                    "Failed to delete partial observation group {GroupId}", groupId.Value);
            }
            throw;
        }
    }

    private async Task UploadFiles(
        ObservationManifest manifest,
        string baseDirectory,
        DatasetGroupKind kind,
        IReadOnlyDictionary<(string Dataset, string Variable, string Layer), int> layerIds,
        CancellationToken cancellationToken)
    {
        Dictionary<int, List<ObservationDataPoint>> batches = [];
        foreach (ObservationFileManifest file in manifest.Files)
        {
            foreach (IReadOnlyDictionary<string, string> row in ReadCsv(Path.Combine(baseDirectory, file.Path)))
            {
                string datasetName = kind == DatasetGroupKind.ObservationSite
                    ? Required(row, file.SiteColumn!, file.Path).Trim()
                    : manifest.Collection;
                DateTime timestamp = ParseDate(Required(row, file.DateColumn, file.Path));
                double? longitude = kind == DatasetGroupKind.ObservationGridded
                    ? ParseDouble(Required(row, file.LongitudeColumn!, file.Path), file.LongitudeColumn!) : null;
                double? latitude = kind == DatasetGroupKind.ObservationGridded
                    ? ParseDouble(Required(row, file.LatitudeColumn!, file.Path), file.LatitudeColumn!) : null;

                foreach (ObservationVariableManifest variable in file.Variables)
                {
                    string raw = Required(row, variable.Column, file.Path);
                    if (string.IsNullOrWhiteSpace(raw) || raw.Equals("NA", StringComparison.OrdinalIgnoreCase)) continue;
                    double value = ParseDouble(raw, variable.Column);
                    int layerId = layerIds[(datasetName, variable.Name, variable.Layer)];
                    if (!batches.TryGetValue(layerId, out List<ObservationDataPoint>? batch))
                        batches[layerId] = batch = [];
                    batch.Add(new ObservationDataPoint(timestamp, value, longitude, latitude));
                    if (batch.Count >= BatchSize) await Flush(layerId, batch, cancellationToken);
                }
            }
        }
        foreach ((int layerId, List<ObservationDataPoint> batch) in batches)
            await Flush(layerId, batch, cancellationToken);
    }

    private async Task Flush(int layerId, List<ObservationDataPoint> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return;
        await api.AppendObservationDataAsync(layerId,
            new AppendObservationDataRequest { DataPoints = batch.ToArray() }, cancellationToken);
        batch.Clear();
    }

    private static ObservationManifest ReadManifest(string path)
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        return deserializer.Deserialize<ObservationManifest>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Observation manifest is empty");
    }

    private static void ValidateManifest(ObservationManifest manifest, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(manifest.Collection) || string.IsNullOrWhiteSpace(manifest.Source) ||
            string.IsNullOrWhiteSpace(manifest.Version))
            throw new InvalidDataException("collection, source and version are required");
        if (manifest.Files.Count == 0) throw new InvalidDataException("At least one file is required");
        if (manifest.Files.Select(f => f.TemporalResolution).Distinct(StringComparer.Ordinal).Count() != 1)
            throw new InvalidDataException("All files in a release must currently use one temporal_resolution");
        DatasetGroupKind kind = ParseKind(manifest.Kind);
        foreach (ObservationFileManifest file in manifest.Files)
        {
            if (string.IsNullOrWhiteSpace(file.Path) || string.IsNullOrWhiteSpace(file.DateColumn) ||
                string.IsNullOrWhiteSpace(file.TemporalResolution))
                throw new InvalidDataException("Every file requires path, date_column and temporal_resolution");
            if (!File.Exists(Path.Combine(baseDirectory, file.Path))) throw new FileNotFoundException(file.Path);
            if (file.Variables.Count == 0) throw new InvalidDataException($"{file.Path} declares no variables");
            if (file.Variables.Any(v => string.IsNullOrWhiteSpace(v.Column)))
                throw new InvalidDataException($"Every variable in {file.Path} requires column");
            foreach (ObservationVariableManifest variable in file.Variables)
                ResolveTarget(variable, file);
            if (file.Variables.Any(v => string.IsNullOrWhiteSpace(v.Name) || string.IsNullOrWhiteSpace(v.Units) ||
                                        string.IsNullOrWhiteSpace(v.Layer)))
                throw new InvalidDataException(
                    $"Every untargeted variable in {file.Path} requires name, units and layer");
            if (file.Variables.Any(v => !v.Level.Equals("gridcell", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("The initial observation CSV importer supports gridcell-level variables only");
            if (kind == DatasetGroupKind.ObservationSite && string.IsNullOrWhiteSpace(file.SiteColumn))
                throw new InvalidDataException($"{file.Path} requires site_column");
            if (kind == DatasetGroupKind.ObservationGridded &&
                (string.IsNullOrWhiteSpace(file.LongitudeColumn) || string.IsNullOrWhiteSpace(file.LatitudeColumn)))
                throw new InvalidDataException($"{file.Path} requires longitude_column and latitude_column");
        }


        foreach (IGrouping<(string Name, string Layer), ObservationVariableManifest> variables in
                 manifest.Files.SelectMany(f => f.Variables).GroupBy(v => (v.Name, v.Layer)))
        {
            if (variables.Select(v => (v.Units, v.Level, v.Description)).Distinct().Count() != 1)
                throw new InvalidDataException(
                    $"Variable {variables.Key.Name}/{variables.Key.Layer} has conflicting definitions");
        }
    }

    private static void ResolveTarget(ObservationVariableManifest variable, ObservationFileManifest file)
    {
        if (variable.Target == null) return;
        if (string.IsNullOrWhiteSpace(variable.Target.Output) || string.IsNullOrWhiteSpace(variable.Target.Layer))
            throw new InvalidDataException($"Target for {variable.Column} requires output and layer");

        OutputFileMetadata metadata;
        try { metadata = OutputFileDefinitions.GetMetadata(variable.Target.Output); }
        catch (InvalidOperationException ex)
        {
            throw new InvalidDataException($"Unknown target output {variable.Target.Output}", ex);
        }
        if (metadata.Level != AggregationLevel.Gridcell)
            throw new InvalidDataException($"Observation target {variable.Target.Output} is not gridcell-level");
        if (!metadata.Layers.IsDataLayer(variable.Target.Layer))
            throw new InvalidDataException(
                $"Layer {variable.Target.Layer} is not valid for target {variable.Target.Output}");

        string canonicalUnits = metadata.Layers.GetUnits(variable.Target.Layer).Name;
        if (!string.IsNullOrWhiteSpace(variable.Units) && variable.Units != canonicalUnits)
            throw new InvalidDataException(
                $"Units for {variable.Column} must be {canonicalUnits} for target " +
                $"{variable.Target.Output}/{variable.Target.Layer}; got {variable.Units}");
        if (!Enum.TryParse(file.TemporalResolution, true, out TemporalResolution resolution) ||
            resolution != metadata.TemporalResolution)
            throw new InvalidDataException(
                $"Temporal resolution for {variable.Column} must be {metadata.TemporalResolution} " +
                $"for target {variable.Target.Output}");

        variable.Name = metadata.Name;
        if (string.IsNullOrWhiteSpace(variable.Description)) variable.Description = metadata.Description;
        variable.Units = canonicalUnits;
        variable.Level = metadata.Level.ToString();
        variable.Layer = variable.Target.Layer;
    }

    private static IReadOnlyDictionary<string, HashSet<(string Name, string Layer)>> DiscoverSiteVariables(
        ObservationManifest manifest, string baseDirectory)
    {
        Dictionary<string, HashSet<(string Name, string Layer)>> sites = new(StringComparer.Ordinal);
        foreach (ObservationFileManifest file in manifest.Files)
            foreach (IReadOnlyDictionary<string, string> row in ReadCsv(Path.Combine(baseDirectory, file.Path)))
            {
                string site = Required(row, file.SiteColumn!, file.Path).Trim();
                if (site.Length == 0) throw new InvalidDataException($"Empty site name in {file.Path}");
                if (!sites.TryGetValue(site, out HashSet<(string Name, string Layer)>? variables))
                    sites[site] = variables = [];
                foreach (ObservationVariableManifest variable in file.Variables)
                {
                    string value = Required(row, variable.Column, file.Path);
                    if (!string.IsNullOrWhiteSpace(value) && !value.Equals("NA", StringComparison.OrdinalIgnoreCase))
                        variables.Add((variable.Name, variable.Layer));
                }
            }
        if (sites.Count == 0) throw new InvalidDataException("The observation release contains no sites");
        if (sites.Any(site => site.Value.Count == 0))
            throw new InvalidDataException("Every site must contain at least one observation value");
        return sites;
    }

    private static IEnumerable<IReadOnlyDictionary<string, string>> ReadCsv(string path)
    {
        using FileStream file = File.OpenRead(path);
        using Stream input = path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
            ? new GZipStream(file, CompressionMode.Decompress) : file;
        using StreamReader reader = new(input);
        string[] headers = ParseCsvLine(reader.ReadLine() ?? throw new InvalidDataException($"{path} is empty"));
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length == 0) continue;
            string[] fields = ParseCsvLine(line);
            if (fields.Length != headers.Length) throw new InvalidDataException($"Malformed CSV row in {path}");
            yield return headers.Zip(fields).ToDictionary(pair => pair.First, pair => pair.Second, StringComparer.Ordinal);
        }
    }

    private static string[] ParseCsvLine(string line)
    {
        List<string> fields = [];
        System.Text.StringBuilder field = new();
        bool quoted = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"') { field.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (c == ',' && !quoted) { fields.Add(field.ToString()); field.Clear(); }
            else field.Append(c);
        }
        if (quoted) throw new InvalidDataException("Unterminated quoted CSV field");
        fields.Add(field.ToString());
        return fields.ToArray();
    }

    private static string Required(IReadOnlyDictionary<string, string> row, string column, string file) =>
        row.TryGetValue(column, out string? value) ? value : throw new InvalidDataException($"Missing column {column} in {file}");
    private static double ParseDouble(string value, string column) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) && double.IsFinite(result)
            ? result : throw new InvalidDataException($"Invalid numeric value '{value}' in {column}");
    private static DateTime ParseDate(string value)
    {
        if (value.Length == 4 && int.TryParse(value, out int year)) return new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime date)
            ? date : throw new InvalidDataException($"Invalid date '{value}'");
    }
    private static DatasetGroupKind ParseKind(string value) => value.ToLowerInvariant() switch
    {
        "site" => DatasetGroupKind.ObservationSite,
        "gridded" => DatasetGroupKind.ObservationGridded,
        _ => throw new InvalidDataException("kind must be site or gridded")
    };
    private static MatchingStrategy ParseStrategy(string value) => value.ToLowerInvariant() switch
    {
        "by_name" => MatchingStrategy.ByName,
        "exact" => MatchingStrategy.ExactMatch,
        "nearest" => MatchingStrategy.Nearest,
        _ => throw new InvalidDataException("matching_strategy must be by_name, exact or nearest")
    };
}
