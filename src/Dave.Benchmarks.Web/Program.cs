using Microsoft.EntityFrameworkCore;
using Dave.Benchmarks.Core.Data;
using Dave.Benchmarks.Core.Logging;
using Dave.Benchmarks.Core.Services.Evaluation;
using Dave.Benchmarks.Web.Configuration;
using Dave.Benchmarks.Web.Services.Evaluation;
using Dave.Benchmarks.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ConnectionStringsSettings connectionStringsSettings = builder.Configuration
    .GetSection("ConnectionStrings")
    .Get<ConnectionStringsSettings>()
    ?? new ConnectionStringsSettings();
connectionStringsSettings.Validate();
string defaultConnection = connectionStringsSettings.DefaultConnection;

if (!builder.Environment.IsDevelopment())
{
    AuthenticationSettings authenticationSettings = builder.Configuration
        .GetSection("Authentication:Schemes:Bearer")
        .Get<AuthenticationSettings>()
        ?? new AuthenticationSettings();
    authenticationSettings.Validate();
}

AuthorisationSettings authorisationSettings = builder.Configuration
    .GetSection("Authorisation")
    .Get<AuthorisationSettings>()
    ?? new AuthorisationSettings();
authorisationSettings.Validate();

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Configure logging
builder.Services.ConfigureLogging();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.GitLabCi, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("project_id", authorisationSettings.AllowedGitlabProjectIds);
    })
    .AddPolicy(AuthorizationPolicies.GitLabProtectedRef, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("project_id", authorisationSettings.AllowedGitlabProjectIds);
        policy.RequireClaim("ref_protected", "true");
    });

// Add database context
builder.Services.AddDbContext<BenchmarksDbContext>(options =>
    options.UseMySql(
        defaultConnection,
        ServerVersion.AutoDetect(defaultConnection),
        mySqlOptions => mySqlOptions
            .EnableRetryOnFailure()
            .MigrationsAssembly("Dave.Benchmarks.Web")
    ));

builder.Services.AddScoped<IEvaluationEngine, EvaluationEngine>();
builder.Services.AddSingleton<IEvaluationJobQueue, EvaluationJobQueue>();
builder.Services.AddHostedService<EvaluationWorker>();

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
    app.UseHttpsRedirection();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
