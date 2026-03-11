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
builder.Services.AddTransient<ModelOutputParser>();
builder.Services.AddTransient<GitService>();
builder.Services.AddTransient<InstructionFileParser>();
builder.Services.AddTransient<CommandRunner>();
builder.Services.AddTransient<ImportHandler>();
builder.Services.AddTransient<Dave.Benchmarks.CLI.Services.GridlistParser>();
builder.Services.AddSingleton<IOutputFileTypeResolver, OutputFileTypeResolver>();

// Configure HTTP client and API client
builder.Services.AddHttpClient<ProductionApiClient>((sp, client) =>
{
    ApiSettings settings = sp.GetRequiredService<ApiSettings>();
    client.BaseAddress = new Uri(settings.WebApiUrl);
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

// Parse command line
return await Parser.Default.ParseArguments<GriddedOptions, SiteOptions>(args).MapResult(
        (GriddedOptions opts) => Run(opts, (ImportHandler handler, GriddedOptions opts) => handler.HandleGriddedImport(opts)),
        (SiteOptions opts) => Run(opts, (ImportHandler handler, SiteOptions opts) => handler.HandleSiteImport(opts)),
        _ => Task.FromResult(1));
