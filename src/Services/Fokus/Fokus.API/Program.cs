using Fokus.API;
using Fokus.Domain.Entities;
using Fokus.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFokusServices();
builder.Services.AddFokusPersistence(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FokusDbContext>();
    db.Database.Migrate();

    if (!await db.AppSettings.AnyAsync())
    {
        db.AppSettings.Add(AppSettings.CreateDefault());
        await db.SaveChangesAsync();
    }
}

app.UseStaticFiles();
app.UseFokusMiddleware();
app.MapFallbackToFile("index.html");

app.Run();
