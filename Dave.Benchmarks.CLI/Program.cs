using CommandLine;
using Dave.Benchmarks.CLI.Commands;
using Dave.Benchmarks.CLI.Options;
using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Add services
builder.Services.AddDbContext<BenchmarksDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection") 
            ?? "server=localhost;database=dave_benchmarks;user=root;password=",
        ServerVersion.AutoDetect("server=localhost;database=dave_benchmarks;user=root;password=")
    ));

// Add core services
builder.Services.AddTransient<ModelOutputParser>();
builder.Services.AddTransient<GitService>();
builder.Services.AddTransient<InstructionFileParser>();
builder.Services.AddTransient<CommandRunner>();

var host = builder.Build();

var runner = host.Services.GetRequiredService<CommandRunner>();

// Parse command line
return await Parser.Default.ParseArguments<GriddedOptions, SiteOptions>(args)
    .MapResult(
        (GriddedOptions opts) => runner.RunAsync<ImportHandler>(handler => 
            handler.HandleGriddedImport(opts)),
        (SiteOptions opts) => runner.RunAsync<ImportHandler>(handler => 
            handler.HandleSiteImport(opts)),
        _ => Task.FromResult(1));
