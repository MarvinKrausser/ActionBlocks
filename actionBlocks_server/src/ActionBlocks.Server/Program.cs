using System.Text.Json.Serialization;
using ActionBlocks.Game;
using ActionBlocks.Server.Hubs;
using ActionBlocks.Server.Lobbies;

var builder = WebApplication.CreateBuilder(args);

// Comma separated list of origins allowed to connect, e.g. "https://blocks.example.com".
// Empty = allow any origin (useful for local development).
var allowedOrigins = (builder.Configuration["ALLOWED_ORIGINS"] ?? "")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var hubPath = builder.Configuration["HUB_PATH"] ?? "/hub";
// Optional prefix when the reverse proxy forwards a sub-path without stripping it, e.g. "/server".
// Requests without the prefix keep working.
var pathBase = builder.Configuration["PATH_BASE"] ?? "";

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins);
    }
    else
    {
        policy.SetIsOriginAllowed(_ => true);
    }
    policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

builder.Services
    .AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Rules can be tuned via configuration, e.g. env Game__ItemSpawnChance=0.2
builder.Services.AddSingleton(builder.Configuration.GetSection("Game").Get<GameSettings>() ?? new GameSettings());
builder.Services.AddSingleton<LobbyManager>();
builder.Services.AddHostedService<MatchRunner>();

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(pathBase))
{
    app.UsePathBase(pathBase);
}

app.UseCors();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapHub<GameHub>(hubPath);

app.Run();
