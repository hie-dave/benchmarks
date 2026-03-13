using Dave.Benchmarks.CLI.Commands;
using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Core.Services;
using Dave.Benchmarks.Tests.Helpers;
using LpjGuess.Core.Helpers;
using LpjGuess.Core.Models;
using LpjGuess.Core.Models.Entities;
using LpjGuess.Core.Models.Importer;
using LpjGuess.Core.Parsers;
using LpjGuess.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Dave.Benchmarks.Tests.CLI;

public class ImportHandlerTests
{
    [Fact]
    public async Task HandleSiteImport_NoRuns_ThrowsInvalidOperationException()
    {
        var (handler, _, git, _, _, _, _, _, _) = CreateHandler();
        git.Setup(x => x.FindSiteLevelRuns("repo")).Returns([]);

        SiteOptions options = new()
        {
            RepoPath = "repo",
            Name = "site-import",
            Description = "desc",
            ClimateDataset = "clim",
            TemporalResolution = "daily"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleSiteImport(options));
    }

    [Fact]
    public async Task HandleGriddedImport_NoOutputFiles_ReturnsWithoutApiCalls()
    {
        var (handler, api, git, _, resolver, fileSystem, _, instructionFactory, instructionParser) = CreateHandler();
        git.Setup(x => x.GetRepositoryInfo(It.IsAny<string>())).Returns(BuildRepositoryInfo());
        instructionFactory.Setup(x => x.Create("run.ins")).Returns(instructionParser.Object);
        fileSystem
            .Setup(x => x.EnumerateFiles("/tmp/out", "*.out", SearchOption.TopDirectoryOnly))
            .Returns([]);

        GriddedOptions options = new()
        {
            RepoPath = "repo",
            OutputDir = "/tmp/out",
            InstructionFile = "run.ins",
            SpatialResolution = "0.5",
            SimulationId = "sim-1",
            Name = "grid-import",
            Description = "desc",
            ClimateDataset = "clim",
            TemporalResolution = "daily"
        };

        await handler.HandleGriddedImport(options);

        resolver.Verify(x => x.BuildLookupTable(It.IsAny<IInstructionFileParser>()), Times.Never);
        api.Verify(x => x.CreateGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        api.Verify(x => x.CreateDatasetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<RepositoryInfo>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleGriddedImport_ImportsOnlyFreshFiles()
    {
        string freshFile = "/tmp/out/fresh.out";
        string staleFile = "/tmp/out/stale.out";
        DateTime now = DateTime.Now;

        var (handler, api, git, parser, resolver, fileSystem, _, instructionFactory, instructionParser) = CreateHandler();
        git.Setup(x => x.GetRepositoryInfo(It.IsAny<string>())).Returns(BuildRepositoryInfo());
        instructionFactory.Setup(x => x.Create("run.ins")).Returns(instructionParser.Object);
        resolver.Setup(x => x.BuildLookupTable(instructionParser.Object));

        fileSystem
            .Setup(x => x.EnumerateFiles("/tmp/out", "*.out", SearchOption.TopDirectoryOnly))
            .Returns([freshFile, staleFile]);
        fileSystem.Setup(x => x.GetLastWriteTime(freshFile)).Returns(now);
        fileSystem.Setup(x => x.GetLastWriteTime(staleFile)).Returns(now.AddSeconds(-10));

        Quantity quantity = BuildGridcellQuantity();
        parser.Setup(x => x.ParseOutputFileAsync(freshFile, default)).ReturnsAsync(quantity);

        api.Setup(x => x.CreateGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(10);
        api.Setup(x => x.CreateDatasetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<RepositoryInfo>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>()))
            .ReturnsAsync(20);

        GriddedOptions options = new()
        {
            RepoPath = "repo",
            OutputDir = "/tmp/out",
            InstructionFile = "run.ins",
            SpatialResolution = "0.5",
            SimulationId = "sim-1",
            Name = "grid-import",
            Description = "desc",
            ClimateDataset = "clim",
            TemporalResolution = "daily"
        };

        await handler.HandleGriddedImport(options);

        parser.Verify(x => x.ParseOutputFileAsync(freshFile, default), Times.Once);
        parser.Verify(x => x.ParseOutputFileAsync(staleFile, default), Times.Never);
        api.Verify(x => x.AddQuantityAsync(20, quantity), Times.Once);
    }

    [Fact]
    public async Task HandleGriddedImport_WhenImportFails_DeletesCreatedGroup()
    {
        string freshFile = "/tmp/out/fresh.out";
        DateTime now = DateTime.Now;

        var (handler, api, git, parser, resolver, fileSystem, _, instructionFactory, instructionParser) = CreateHandler();
        git.Setup(x => x.GetRepositoryInfo(It.IsAny<string>())).Returns(BuildRepositoryInfo());
        instructionFactory.Setup(x => x.Create("run.ins")).Returns(instructionParser.Object);
        resolver.Setup(x => x.BuildLookupTable(instructionParser.Object));

        fileSystem
            .Setup(x => x.EnumerateFiles("/tmp/out", "*.out", SearchOption.TopDirectoryOnly))
            .Returns([freshFile]);
        fileSystem.Setup(x => x.GetLastWriteTime(freshFile)).Returns(now);

        Quantity quantity = BuildGridcellQuantity();
        parser.Setup(x => x.ParseOutputFileAsync(freshFile, default)).ReturnsAsync(quantity);

        api.Setup(x => x.CreateGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(42);
        api.Setup(x => x.CreateDatasetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<RepositoryInfo>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>()))
            .ReturnsAsync(100);
        api.Setup(x => x.AddQuantityAsync(100, It.IsAny<Quantity>())).ThrowsAsync(new InvalidOperationException("import failed"));

        GriddedOptions options = new()
        {
            RepoPath = "repo",
            OutputDir = "/tmp/out",
            InstructionFile = "run.ins",
            SpatialResolution = "0.5",
            SimulationId = "sim-1",
            Name = "grid-import",
            Description = "desc",
            ClimateDataset = "clim",
            TemporalResolution = "daily"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleGriddedImport(options));

        api.Verify(x => x.DeleteGroupAsync(42), Times.Once);
    }

    [Fact]
    public async Task HandleSiteImport_ImportsFreshFiles_ForFreshSite_AndSkipsStaleSite()
    {
        using TempDirectory temp = TempDirectory.Create(GetType().Name);
        string freshSiteDir = Path.Combine(temp.AbsolutePath, "fresh");
        string staleSiteDir = Path.Combine(temp.AbsolutePath, "stale");
        Directory.CreateDirectory(freshSiteDir);
        Directory.CreateDirectory(staleSiteDir);

        string freshIns = Path.Combine(temp.AbsolutePath, "fresh.ins");
        string staleIns = Path.Combine(temp.AbsolutePath, "stale.ins");

        string freshOut = Path.Combine(freshSiteDir, "lai.out");
        string staleOut = Path.Combine(staleSiteDir, "lai.out");

        DateTime now = DateTime.Now;

        var (handler, api, git, parser, resolver, fileSystem, gridlistParser, instructionFactory, _) = CreateHandler();
        git.Setup(x => x.FindSiteLevelRuns("repo")).Returns([
            ("fresh-site", freshIns, freshSiteDir),
            ("stale-site", staleIns, staleSiteDir)
        ]);
        git.Setup(x => x.GetRepositoryInfo(It.IsAny<string>())).Returns(BuildRepositoryInfo());

        var freshInsParser = CreateInstructionParserMock(freshIns, "fresh.gridlist");
        var staleInsParser = CreateInstructionParserMock(staleIns, "stale.gridlist");
        instructionFactory.Setup(x => x.Create(freshIns)).Returns(freshInsParser.Object);
        instructionFactory.Setup(x => x.Create(staleIns)).Returns(staleInsParser.Object);

        fileSystem.Setup(x => x.EnumerateFiles(freshSiteDir, "*.out", SearchOption.TopDirectoryOnly)).Returns([freshOut]);
        fileSystem.Setup(x => x.EnumerateFiles(staleSiteDir, "*.out", SearchOption.TopDirectoryOnly)).Returns([staleOut]);
        fileSystem.Setup(x => x.GetLastWriteTime(freshOut)).Returns(now);
        fileSystem.Setup(x => x.GetLastWriteTime(staleOut)).Returns(now.AddSeconds(-400));

        gridlistParser.Setup(x => x.ParseAsync(It.IsAny<string>())).ReturnsAsync([new Gridcell(-33.0, 151.0)]);
        resolver.Setup(x => x.BuildLookupTable(It.IsAny<IInstructionFileParser>()));

        Quantity quantity = BuildGridcellQuantity();
        parser.Setup(x => x.ParseOutputFileAsync(freshOut, default)).ReturnsAsync(quantity);

        api.Setup(x => x.CreateGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(11);
        api.Setup(x => x.CreateDatasetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<RepositoryInfo>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>()))
            .ReturnsAsync(21);

        SiteOptions options = new()
        {
            RepoPath = "repo",
            Name = "site-import",
            Description = "desc",
            ClimateDataset = "clim",
            TemporalResolution = "daily"
        };

        await handler.HandleSiteImport(options);

        api.Verify(x => x.CreateDatasetAsync(
                "fresh-site",
                "site-import - fresh-site",
                It.IsAny<RepositoryInfo>(),
                "clim",
                "daily",
                "fresh-site",
                "lpjguess_dave",
                "{}",
                11),
            Times.Once);
        api.Verify(x => x.AddQuantityAsync(21, quantity), Times.Once);
        parser.Verify(x => x.ParseOutputFileAsync(staleOut, default), Times.Never);
    }

    [Fact]
    public async Task HandleSiteImport_SiteWithMixedFreshAndStaleOutputs_ImportsOnlyFreshOutputs()
    {
        using TempDirectory temp = TempDirectory.Create(GetType().Name);
        string siteOutDir = Path.Combine(temp.AbsolutePath, "site");
        Directory.CreateDirectory(siteOutDir);

        string siteIns = Path.Combine(temp.AbsolutePath, "site.ins");
        string freshOut = Path.Combine(siteOutDir, "fresh.out");
        string staleOut = Path.Combine(siteOutDir, "stale.out");

        DateTime now = DateTime.Now;

        var (handler, api, git, parser, resolver, fileSystem, gridlistParser, instructionFactory, _) = CreateHandler();
        git.Setup(x => x.FindSiteLevelRuns("repo")).Returns([("site", siteIns, siteOutDir)]);
        git.Setup(x => x.GetRepositoryInfo(It.IsAny<string>())).Returns(BuildRepositoryInfo());

        var insParser = CreateInstructionParserMock(siteIns, "site.gridlist");
        instructionFactory.Setup(x => x.Create(siteIns)).Returns(insParser.Object);

        fileSystem.Setup(x => x.EnumerateFiles(siteOutDir, "*.out", SearchOption.TopDirectoryOnly)).Returns([freshOut, staleOut]);
        fileSystem.Setup(x => x.GetLastWriteTime(freshOut)).Returns(now);
        fileSystem.Setup(x => x.GetLastWriteTime(staleOut)).Returns(now.AddSeconds(-10));

        gridlistParser.Setup(x => x.ParseAsync(It.IsAny<string>())).ReturnsAsync([new Gridcell(-33.0, 151.0)]);
        resolver.Setup(x => x.BuildLookupTable(It.IsAny<IInstructionFileParser>()));

        Quantity quantity = BuildGridcellQuantity();
        parser.Setup(x => x.ParseOutputFileAsync(freshOut, default)).ReturnsAsync(quantity);

        api.Setup(x => x.CreateGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(50);
        api.Setup(x => x.CreateDatasetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<RepositoryInfo>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>()))
            .ReturnsAsync(60);

        SiteOptions options = new()
        {
            RepoPath = "repo",
            Name = "site-import",
            Description = "desc",
            ClimateDataset = "clim",
            TemporalResolution = "daily"
        };

        await handler.HandleSiteImport(options);

        api.Verify(x => x.CreateDatasetAsync(
                "site",
                "site-import - site",
                It.IsAny<RepositoryInfo>(),
                "clim",
                "daily",
                "site",
                "lpjguess_dave",
                "{}",
                50),
            Times.Once);
        parser.Verify(x => x.ParseOutputFileAsync(freshOut, default), Times.Once);
        parser.Verify(x => x.ParseOutputFileAsync(staleOut, default), Times.Never);
        api.Verify(x => x.AddQuantityAsync(60, quantity), Times.Once);
    }

    [Fact]
    public async Task HandleSiteImport_SiteWithNoOutputFiles_IsSkipped()
    {
        using TempDirectory temp = TempDirectory.Create(GetType().Name);
        string siteAOutDir = Path.Combine(temp.AbsolutePath, "site-a");
        string siteBOutDir = Path.Combine(temp.AbsolutePath, "site-b");
        Directory.CreateDirectory(siteAOutDir);
        Directory.CreateDirectory(siteBOutDir);

        string siteAIns = Path.Combine(temp.AbsolutePath, "site-a.ins");
        string siteBIns = Path.Combine(temp.AbsolutePath, "site-b.ins");

        string siteAOut = Path.Combine(siteAOutDir, "lai.out");
        DateTime now = DateTime.Now;

        var (handler, api, git, parser, resolver, fileSystem, gridlistParser, instructionFactory, _) = CreateHandler();
        git.Setup(x => x.FindSiteLevelRuns("repo")).Returns([
            ("site-a", siteAIns, siteAOutDir),
            ("site-b", siteBIns, siteBOutDir)
        ]);
        git.Setup(x => x.GetRepositoryInfo(It.IsAny<string>())).Returns(BuildRepositoryInfo());

        var siteAInsParser = CreateInstructionParserMock(siteAIns, "a.gridlist");
        var siteBInsParser = CreateInstructionParserMock(siteBIns, "b.gridlist");
        instructionFactory.Setup(x => x.Create(siteAIns)).Returns(siteAInsParser.Object);
        instructionFactory.Setup(x => x.Create(siteBIns)).Returns(siteBInsParser.Object);

        fileSystem.Setup(x => x.EnumerateFiles(siteAOutDir, "*.out", SearchOption.TopDirectoryOnly)).Returns([siteAOut]);
        fileSystem.Setup(x => x.EnumerateFiles(siteBOutDir, "*.out", SearchOption.TopDirectoryOnly)).Returns([]);
        fileSystem.Setup(x => x.GetLastWriteTime(siteAOut)).Returns(now);

        gridlistParser.Setup(x => x.ParseAsync(It.IsAny<string>())).ReturnsAsync([new Gridcell(-33.0, 151.0)]);
        resolver.Setup(x => x.BuildLookupTable(It.IsAny<IInstructionFileParser>()));

        Quantity quantity = BuildGridcellQuantity();
        parser.Setup(x => x.ParseOutputFileAsync(siteAOut, default)).ReturnsAsync(quantity);

        api.Setup(x => x.CreateGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(12);
        api.Setup(x => x.CreateDatasetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<RepositoryInfo>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>()))
            .ReturnsAsync(22);

        SiteOptions options = new()
        {
            RepoPath = "repo",
            Name = "site-import",
            Description = "desc",
            ClimateDataset = "clim",
            TemporalResolution = "daily"
        };

        await handler.HandleSiteImport(options);

        api.Verify(x => x.CreateDatasetAsync(
                "site-a",
                "site-import - site-a",
                It.IsAny<RepositoryInfo>(),
                "clim",
                "daily",
                "site-a",
                "lpjguess_dave",
                "{}",
                12),
            Times.Once);
        api.Verify(x => x.CreateDatasetAsync(
                "site-b",
                It.IsAny<string>(),
                It.IsAny<RepositoryInfo>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleSiteImport_MultipleCoordinates_ThrowsInvalidDataException()
    {
        using TempDirectory temp = TempDirectory.Create(GetType().Name);
        string siteOutDir = Path.Combine(temp.AbsolutePath, "site");
        Directory.CreateDirectory(siteOutDir);

        string siteIns = Path.Combine(temp.AbsolutePath, "site.ins");
        string outputFile = Path.Combine(siteOutDir, "lai.out");

        DateTime now = DateTime.Now;
        var (handler, _, git, _, _, fileSystem, gridlistParser, instructionFactory, _) = CreateHandler();
        git.Setup(x => x.FindSiteLevelRuns("repo")).Returns([("site", siteIns, siteOutDir)]);
        git.Setup(x => x.GetRepositoryInfo(It.IsAny<string>())).Returns(BuildRepositoryInfo());

        var insParser = CreateInstructionParserMock(siteIns, "site.gridlist");
        instructionFactory.Setup(x => x.Create(siteIns)).Returns(insParser.Object);

        fileSystem.Setup(x => x.EnumerateFiles(siteOutDir, "*.out", SearchOption.TopDirectoryOnly)).Returns([outputFile]);
        fileSystem.Setup(x => x.GetLastWriteTime(outputFile)).Returns(now);

        gridlistParser
            .Setup(x => x.ParseAsync(It.IsAny<string>()))
            .ReturnsAsync([new Gridcell(-33.0, 151.0), new Gridcell(-34.0, 152.0)]);

        SiteOptions options = new()
        {
            RepoPath = "repo",
            Name = "site-import",
            Description = "desc",
            ClimateDataset = "clim",
            TemporalResolution = "daily"
        };

        await Assert.ThrowsAsync<InvalidDataException>(() => handler.HandleSiteImport(options));
    }

    [Fact]
    public async Task HandleSiteImport_WhenImportFails_DeletesCreatedGroup()
    {
        using TempDirectory temp = TempDirectory.Create(GetType().Name);
        string siteOutDir = Path.Combine(temp.AbsolutePath, "site");
        Directory.CreateDirectory(siteOutDir);

        string siteIns = Path.Combine(temp.AbsolutePath, "site.ins");
        string outputFile = Path.Combine(siteOutDir, "lai.out");

        DateTime now = DateTime.Now;
        var (handler, api, git, parser, resolver, fileSystem, gridlistParser, instructionFactory, _) = CreateHandler();
        git.Setup(x => x.FindSiteLevelRuns("repo")).Returns([("site", siteIns, siteOutDir)]);
        git.Setup(x => x.GetRepositoryInfo(It.IsAny<string>())).Returns(BuildRepositoryInfo());

        var insParser = CreateInstructionParserMock(siteIns, "site.gridlist");
        instructionFactory.Setup(x => x.Create(siteIns)).Returns(insParser.Object);

        fileSystem.Setup(x => x.EnumerateFiles(siteOutDir, "*.out", SearchOption.TopDirectoryOnly)).Returns([outputFile]);
        fileSystem.Setup(x => x.GetLastWriteTime(outputFile)).Returns(now);

        gridlistParser.Setup(x => x.ParseAsync(It.IsAny<string>())).ReturnsAsync([new Gridcell(-33.0, 151.0)]);
        resolver.Setup(x => x.BuildLookupTable(It.IsAny<IInstructionFileParser>()));
        parser.Setup(x => x.ParseOutputFileAsync(outputFile, default)).ReturnsAsync(BuildGridcellQuantity());

        api.Setup(x => x.CreateGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(99);
        api.Setup(x => x.CreateDatasetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<RepositoryInfo>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>()))
            .ReturnsAsync(77);
        api.Setup(x => x.AddQuantityAsync(77, It.IsAny<Quantity>())).ThrowsAsync(new InvalidOperationException("fail"));

        SiteOptions options = new()
        {
            RepoPath = "repo",
            Name = "site-import",
            Description = "desc",
            ClimateDataset = "clim",
            TemporalResolution = "daily"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleSiteImport(options));
        api.Verify(x => x.DeleteGroupAsync(99), Times.Once);
    }

    private static Mock<IInstructionFileParser> CreateInstructionParserMock(string instructionFilePath, string gridlistRelativePath)
    {
        Mock<IInstructionFileParser> parser = new();
        parser.SetupGet(x => x.FilePath).Returns(instructionFilePath);
        parser.Setup(x => x.GetTopLevelParameter("file_gridlist"))
            .Returns(new InstructionParameter(gridlistRelativePath));
        return parser;
    }

    private static Quantity BuildGridcellQuantity()
    {
        return new Quantity(
            "lai",
            "lai",
            [new Layer("total", new Unit("m2m2"), [new DataPoint(new DateTime(2025, 1, 1), 151, -33, 1.0)])],
            AggregationLevel.Gridcell,
            TemporalResolution.Daily);
    }

    private static RepositoryInfo BuildRepositoryInfo()
    {
        return new RepositoryInfo
        {
            CommitHash = "abc123",
            RepositoryPath = "/repo"
        };
    }

    private static (ImportHandler Handler,
        Mock<IApiClient> Api,
        Mock<IGitService> Git,
        Mock<IModelOutputParser> Parser,
        Mock<IOutputFileTypeResolver> Resolver,
        Mock<IFileSystem> FileSystem,
        Mock<IGridlistParser> GridlistParser,
        Mock<IInstructionFileParserFactory> InstructionFileParserFactory,
        Mock<IInstructionFileParser> InstructionFileParser) CreateHandler()
    {
        Mock<IApiClient> api = new();
        Mock<IGitService> git = new();
        Mock<IModelOutputParser> parser = new();
        Mock<IOutputFileTypeResolver> resolver = new();
        Mock<IFileSystem> fileSystem = new();
        Mock<IGridlistParser> gridlistParser = new();
        Mock<IInstructionFileParserFactory> instructionFileParserFactory = new();
        Mock<IInstructionFileParser> instructionFileParser = new();

        ImportHandler handler = new(
            NullLogger<ImportHandler>.Instance,
            parser.Object,
            git.Object,
            api.Object,
            resolver.Object,
            gridlistParser.Object,
            NullLogger<InstructionFileHelper>.Instance,
            fileSystem.Object,
            instructionFileParserFactory.Object);

        return (handler, api, git, parser, resolver, fileSystem, gridlistParser, instructionFileParserFactory, instructionFileParser);
    }
}
