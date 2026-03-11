using Microsoft.EntityFrameworkCore;
using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Logging;
using Dave.Benchmarks.Web.Configuration;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ConnectionStringsSettings connectionStringsSettings = builder.Configuration
    .GetSection("ConnectionStrings")
    .Get<ConnectionStringsSettings>()
    ?? new ConnectionStringsSettings();
connectionStringsSettings.Validate();
string defaultConnection = connectionStringsSettings.DefaultConnection;

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Configure logging
builder.Services.ConfigureLogging();

// Add database context
builder.Services.AddDbContext<BenchmarksDbContext>(options =>
    options.UseMySql(
        defaultConnection,
        ServerVersion.AutoDetect(defaultConnection),
        mySqlOptions => mySqlOptions
            .EnableRetryOnFailure()
            .MigrationsAssembly("Dave.Benchmarks.Web")
    ));

WebApplication app = builder.Build();

// Apply pending migrations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BenchmarksDbContext>();
    context.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
