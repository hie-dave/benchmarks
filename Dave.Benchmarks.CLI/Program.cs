using CommandLine;
using Dave.Benchmarks.CLI.Commands;
using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.CLI.Services;
using Dave.Benchmarks.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Add services
builder.Services.AddHttpClient();

// Add core services
builder.Services.AddTransient<ModelOutputParser>();
builder.Services.AddTransient<GitService>();
builder.Services.AddTransient<InstructionFileParser>();
builder.Services.AddTransient<CommandRunner>();

// Configure HttpClient for ImportHandler
builder.Services.AddHttpClient<ImportHandler>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["WebApiUrl"] ?? "http://localhost:5000");
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
