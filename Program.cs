using Azure.Identity;
using Blazorise;
using Blazorise.Bootstrap;
using Microsoft.EntityFrameworkCore;
using mhd.DataAccess;
using mhd.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddMemoryCache();

var cosmosEndpoint = Environment.GetEnvironmentVariable("COSMOS_ENDPOINT", EnvironmentVariableTarget.Process);
var cosmosDatabase = Environment.GetEnvironmentVariable("COSMOS_DATABASE", EnvironmentVariableTarget.Process);

var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ExcludeInteractiveBrowserCredential = true,
    ExcludeVisualStudioCredential = true,
    ExcludeVisualStudioCodeCredential = true,
    ExcludeAzurePowerShellCredential = true,
    ExcludeManagedIdentityCredential = builder.Environment.IsDevelopment()
});

builder.Services.AddDbContextFactory<DatabaseContext>((_, opts) =>
{
    opts.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    opts.UseCosmos(
        accountEndpoint: string.IsNullOrWhiteSpace(cosmosEndpoint) ? "https://mhd.invalid" : cosmosEndpoint,
        tokenCredential: credential,
        databaseName: string.IsNullOrWhiteSpace(cosmosDatabase) ? "mhd" : cosmosDatabase
    );
});

builder.Services.AddScoped<IMHDService, MHDService>();
builder.Services.AddBlazorise(options =>
{
    options.Immediate = false;
}).AddBootstrapProviders();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

var staticFiles = new StaticFileOptions();
if (!app.Environment.IsDevelopment())
{
    staticFiles.OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public,max-age=604800";
    };
}

app.UseStaticFiles(staticFiles);
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();
