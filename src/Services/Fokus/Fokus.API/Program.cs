using Blocks.AspNetCore.Middlewares;
using Fokus.API;
using Fokus.API.Features.Xray;
using Fokus.API.Hubs;
using Fokus.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFokusServices(builder.Configuration, builder.Environment);
builder.Services.AddFokusPersistence(builder.Configuration);

var app = builder.Build();

app.MigrateFokusDatabase();
await app.SeedDefaultAppSettingsAsync();
await app.ApplyConfiguredXrayCredentialsAsync();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseStaticFiles();
app.UseFokusAuth();
app.UseFokusMiddleware();
app.MapHub<SprintHub>("/hubs/sprint");
app.MapFallbackToFile("index.html");

app.Run();
