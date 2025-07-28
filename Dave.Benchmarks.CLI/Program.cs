using CommandLine;
using Dave.Benchmarks.CLI.Commands;
using Dave.Benchmarks.CLI.Configuration;
using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Core.Logging;
using Dave.Benchmarks.Core.Services;
using LpjGuess.Core.Parsers;
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

builder.Services.AddScoped<IApiClient>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<OptionsBase>>().Value;
    if (opts.DryRun)
        return sp.GetRequiredService<DryRunApiClient>();
    return sp.GetRequiredService<ProductionApiClient>();
});

// Configure logging
builder.Services.ConfigureLogging();

IHost host = builder.Build();

CommandRunner runner = host.Services.GetRequiredService<CommandRunner>();

// Parse command line
return await Parser.Default.ParseArguments<GriddedOptions, SiteOptions>(args)
    .MapResult(
        (GriddedOptions opts) => runner.RunAsync<ImportHandler>(handler => 
            handler.HandleGriddedImport(opts)),
        (SiteOptions opts) => runner.RunAsync<ImportHandler>(handler => 
            handler.HandleSiteImport(opts)),
        _ => Task.FromResult(1));
