using System.IO.Compression;
using Dave.Benchmarks.CLI.Commands;
using Dave.Benchmarks.CLI.Configuration;
using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Core.Models.Entities;
using Dave.Benchmarks.Core.Models.Importer;
using Microsoft.Extensions.Logging;
using Moq;

namespace Dave.Benchmarks.Tests.Services;

public class ObservationImportHandlerTests
{
    [Fact]
    public async Task RunAsync_WhenImportFailsAndCleanupRequested_DeletesGroup()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"observation-cleanup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "flux.csv"),
                "date,site,gpp\n2026-01-01,AU-Tum,1.5\n");
            string manifestPath = Path.Combine(directory, "observations.yaml");
            await File.WriteAllTextAsync(manifestPath, """
                collection: ozflux
                source: ozflux
                version: test-cleanup
                kind: site
                files:
                  - path: flux.csv
                    temporal_resolution: daily
                    variables:
                      - column: gpp
                        name: gpp
                        units: kgC/m2/day
                """);

            Mock<IApiClient> api = new();
            api.Setup(a => a.CreateObservationGroupAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<DatasetGroupKind>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(73);
            api.Setup(a => a.CreateObservationDatasetAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<MatchingStrategy>(), It.IsAny<int?>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("upload failed"));
            ObservationImportHandler handler = CreateHandler(api.Object);

            await Assert.ThrowsAsync<HttpRequestException>(() => handler.RunAsync(new ObservationImportOptions
            {
                Manifest = manifestPath,
                CleanupOnFailure = true
            }));

            api.Verify(a => a.DeleteObservationGroupAsync(73, It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_SiteGzipCsv_CreatesDatasetPerSiteAndActivatesRelease()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"observation-import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string csvPath = Path.Combine(directory, "flux.csv.gz");
            await using (FileStream file = File.Create(csvPath))
            await using (GZipStream gzip = new(file, CompressionMode.Compress))
            await using (StreamWriter writer = new(gzip))
            {
                await writer.WriteAsync("date,site,gpp\n2026-01-01,AU-Tum,1.5\n2026-01-02,AU-How,2.5\n");
            }
            string manifestPath = Path.Combine(directory, "observations.yaml");
            await File.WriteAllTextAsync(manifestPath, """
                collection: ozflux
                source: ozflux
                version: 2026-08-17
                description: Flux tower observations
                kind: site
                files:
                  - path: flux.csv.gz
                    date_column: date
                    site_column: site
                    temporal_resolution: daily
                    variables:
                      - column: gpp
                        name: gpp
                        units: kgC/m2/day
                        layer: total
                """);

            Mock<IApiClient> api = new();
            api.Setup(a => a.CreateObservationGroupAsync(
                    "ozflux", "ozflux", "2026-08-17", It.IsAny<string>(),
                    DatasetGroupKind.ObservationSite, "{}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(10);
            api.SetupSequence(a => a.CreateObservationDatasetAsync(
                    10, It.IsAny<string>(), It.IsAny<string>(), "daily", It.IsAny<string>(),
                    MatchingStrategy.ByName, null, "{}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(20).ReturnsAsync(21);
            api.SetupSequence(a => a.CreateObservationVariableAsync(
                    It.IsAny<int>(), It.IsAny<CreateVariableRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(30).ReturnsAsync(31);
            api.SetupSequence(a => a.CreateObservationLayerAsync(
                    It.IsAny<int>(), It.IsAny<CreateLayerRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(40).ReturnsAsync(41);

            ObservationImportHandler handler = CreateHandler(api.Object);

            await handler.RunAsync(new ObservationImportOptions { Manifest = manifestPath, Activate = true });

            api.Verify(a => a.CreateObservationDatasetAsync(
                10, "AU-How", It.IsAny<string>(), "daily", "AU-How", MatchingStrategy.ByName,
                null, "{}", It.IsAny<CancellationToken>()), Times.Once);
            api.Verify(a => a.CreateObservationDatasetAsync(
                10, "AU-Tum", It.IsAny<string>(), "daily", "AU-Tum", MatchingStrategy.ByName,
                null, "{}", It.IsAny<CancellationToken>()), Times.Once);
            api.Verify(a => a.AppendObservationDataAsync(
                It.IsAny<int>(),
                It.Is<AppendObservationDataRequest>(r => r.DataPoints.Count == 1 &&
                    r.DataPoints[0].Longitude == null && r.DataPoints[0].Latitude == null),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
            api.Verify(a => a.CompleteObservationGroupAsync(10, It.IsAny<CancellationToken>()), Times.Once);
            api.Verify(a => a.ActivateObservationGroupAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static ObservationImportHandler CreateHandler(IApiClient api)
    {
        ApiSettings settings = new() { WebApiUrl = "https://benchmarks.example.test" };
        GitLabCuratorAuthenticator authenticator = new(
            Mock.Of<IHttpClientFactory>(), settings, Mock.Of<ILogger<GitLabCuratorAuthenticator>>());
        return new ObservationImportHandler(
            api, authenticator, settings, Mock.Of<ILogger<ObservationImportHandler>>());
    }
}
