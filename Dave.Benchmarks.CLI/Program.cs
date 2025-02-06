using CommandLine;
using Dave.Benchmarks.CLI.Commands;
using Dave.Benchmarks.CLI.Configuration;
using Dave.Benchmarks.CLI.Logging;
using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

builder.Services.AddHttpClient<ImportHandler>((sp, client) =>
{
    var settings = sp.GetRequiredService<ApiSettings>();
    client.BaseAddress = new Uri(settings.WebApiUrl);
});

// Configure logging
builder.Logging.ClearProviders();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole(options =>
    {
        options.FormatterName = CustomConsoleFormatterOptions.FormatterName;
    });
    logging.AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>(options =>
    {
        options.TimestampFormat = "HH:mm:ss ";
        options.IncludeScopes = true;
    });
});

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
