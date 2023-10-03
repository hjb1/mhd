using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using mhd.DataAccess;
using Microsoft.Extensions.Options;
using mhd;
using Azure.Core;
using Azure.Identity;
using mhd.Domain;
using Blazorise;
using Blazorise.Bootstrap;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR().AddAzureSignalR();

builder.Services.AddDbContextFactory<DatabaseContext>(
    (IServiceProvider sp, DbContextOptionsBuilder opts) =>
    {
        var cosmosSettings = sp;

        opts.UseCosmos(
            accountEndpoint: Environment.GetEnvironmentVariable("COSMOS_ENDPOINT", EnvironmentVariableTarget.Process),
            tokenCredential: new DefaultAzureCredential(),
            databaseName: Environment.GetEnvironmentVariable("COSMOS_DATABASE", EnvironmentVariableTarget.Process)
        );
    }
);
builder.Services.AddScoped<IMHDService, MHDService>();
builder.Services.AddBlazorise(
    options => 
    {
        options.Immediate = true;
    }
).AddBootstrapProviders();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
