using Blocks.AspNetCore.Middlewares;
using Fokus.API;
using Fokus.API.Auth;
using Fokus.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xray.GraphQL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFokusServices(builder.Configuration);
builder.Services.AddFokusPersistence(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FokusDbContext>();
    var connStr = db.Database.GetConnectionString();
    if (connStr is not null)
    {
        var dbPath = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connStr).DataSource;
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }
    db.Database.Migrate();

    if (!await db.AppSettings.AnyAsync())
    {
        db.AppSettings.Add(AppSettings.CreateDefault());
        await db.SaveChangesAsync();
    }

    var xrayOptions = scope.ServiceProvider.GetRequiredService<IOptions<XrayOptions>>().Value;
    if (!string.IsNullOrEmpty(xrayOptions.ClientId) && !string.IsNullOrEmpty(xrayOptions.ClientSecret))
    {
        var settings = await db.AppSettings.FirstAsync();
        settings.XrayEnabled = true;
        settings.XrayClientId = xrayOptions.ClientId;
        settings.XrayClientSecret = xrayOptions.ClientSecret;
        await db.SaveChangesAsync();
    }
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseStaticFiles();
app.UseFokusAuth();
app.UseFokusMiddleware();
app.MapFallbackToFile("index.html");

app.Run();
