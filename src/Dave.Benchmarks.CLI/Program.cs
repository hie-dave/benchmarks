using CommandLine;
using Dave.Benchmarks.CLI.Commands;
using Dave.Benchmarks.CLI.Configuration;
using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Core.Logging;
using Dave.Benchmarks.Core.Services;
using LpjGuess.Core.Parsers;
using LpjGuess.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Configure and validate settings
builder.Services.Configure<ApiSettings>(builder.Configuration);
builder.Services.AddSingleton(sp =>
{
    ApiSettings settings = sp.GetRequiredService<IOptions<ApiSettings>>().Value;
    settings.Validate();
    return settings;
});

// Add core services
builder.Services.AddTransient<IModelOutputParser, ModelOutputParser>();
builder.Services.AddTransient<IGitService, GitService>();
builder.Services.AddTransient<InstructionFileParser>();
builder.Services.AddTransient<CommandRunner>();
builder.Services.AddTransient<ImportHandler>();
builder.Services.AddTransient<EvaluateHandler>();
builder.Services.AddTransient<BenchmarkHandler>();
builder.Services.AddTransient<ObservationImportHandler>();
builder.Services.AddTransient<GitLabCuratorAuthenticator>();
builder.Services.AddTransient<IGridlistParser, GridlistParser>();
builder.Services.AddSingleton<IOutputFileTypeResolver, OutputFileTypeResolver>();
builder.Services.AddSingleton<IFileSystem, PhysicalFileSystem>();
builder.Services.AddSingleton<IInstructionFileParserFactory, InstructionFileParserFactory>();

// Configure HTTP client and API client
builder.Services.AddHttpClient<ProductionApiClient>((sp, client) =>
{
    ApiSettings settings = sp.GetRequiredService<ApiSettings>();
    client.BaseAddress = new Uri(settings.WebApiUrl);

    string token = Environment.GetEnvironmentVariable(ApiSettings.TokenEnvironmentVariable)
        ?? settings.AccessToken;
    if (!string.IsNullOrWhiteSpace(token))
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Trim());
});

// Configure logging
builder.Services.ConfigureLogging();

async Task<int> Run<THandler, TOptions>(TOptions options, Func<THandler, TOptions, Task> handlerFunc)
        where THandler : notnull
        where TOptions : OptionsBase
{
    if (options.DryRun)
        builder.Services.AddTransient<IApiClient, DryRunApiClient>();
    else
        builder.Services.AddTransient<IApiClient, ProductionApiClient>(sp =>
            sp.GetRequiredService<ProductionApiClient>());
    using IHost host = builder.Build();
    CommandRunner runner = host.Services.GetRequiredService<CommandRunner>();
    return await runner.RunAsync<THandler>(handler => handlerFunc(handler, options));
}

async Task<int> RunApiCommand<THandler, TOptions>(TOptions options, Func<THandler, TOptions, Task> action)
    where THandler : notnull
{
    builder.Services.AddTransient<IApiClient, ProductionApiClient>(sp => sp.GetRequiredService<ProductionApiClient>());
    using IHost host = builder.Build();
    return await host.Services.GetRequiredService<CommandRunner>()
        .RunAsync<THandler>(handler => action(handler, options));
}

// Parse command line
return await Parser.Default.ParseArguments<GriddedOptions, SiteOptions, EvaluateOptions, BenchmarkOptions, ObservationImportOptions>(args).MapResult(
        (GriddedOptions opts) => Run(opts, (ImportHandler handler, GriddedOptions opts) => handler.HandleGriddedImport(opts)),
        (SiteOptions opts) => Run(opts, (ImportHandler handler, SiteOptions opts) => handler.HandleSiteImport(opts)),
        (EvaluateOptions opts) => RunApiCommand(opts, (EvaluateHandler handler, EvaluateOptions opts) => handler.RunAsync(opts)),
        (BenchmarkOptions opts) => RunApiCommand(opts, (BenchmarkHandler handler, BenchmarkOptions opts) => handler.RunAsync(opts)),
        (ObservationImportOptions opts) => Run(opts, (ObservationImportHandler handler, ObservationImportOptions opts) => handler.RunAsync(opts)),
        _ => Task.FromResult(1));
