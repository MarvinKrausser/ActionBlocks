using ActionBlocks.Game;
using ActionBlocks.Game.Items;
using Microsoft.Extensions.Primitives;

namespace ActionBlocks.Server.Config;

/// <summary>Settings and items for new matches. A match keeps the instance it started with.</summary>
public sealed record GameConfig(GameSettings Settings, ItemRegistry Items);

/// <summary>
/// Binds the "Game" configuration section and follows changes to config/game.json.
/// An invalid config at startup stops the server; an invalid edit later is logged and the last good config stays.
/// </summary>
public sealed class GameConfigProvider : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly GameConfigFile _file;
    private readonly ILogger<GameConfigProvider> _logger;
    private readonly IDisposable _subscription;
    private volatile GameConfig _current;

    public GameConfigProvider(IConfiguration configuration, GameConfigFile file, ILogger<GameConfigProvider> logger)
    {
        _configuration = configuration;
        _file = file;
        _logger = logger;

        var (config, errors) = Load();
        if (config is null)
        {
            throw new InvalidOperationException($"Invalid game config ({file.Path ?? "no file"}):\n  {string.Join("\n  ", errors)}");
        }
        _current = config;
        if (file.Path is null)
        {
            _logger.LogInformation("No {File} found, using built-in game settings", "config/game.json");
        }
        LogValues("Game config loaded", config);
        _subscription = ChangeToken.OnChange(configuration.GetReloadToken, Reload);
    }

    public GameConfig Current => _current;

    private void Reload()
    {
        if (_file.Path is not null && !_file.Exists)
        {
            _logger.LogWarning("Game config {Path} is missing, keeping the previous values", _file.Path);
            return;
        }
        var (config, errors) = Load();
        if (config is null)
        {
            _logger.LogError(
                "Game config change ignored, keeping the previous values:\n  {Errors}", string.Join("\n  ", errors));
            return;
        }
        _current = config;
        LogValues("Game config reloaded, used from the next match", config);
    }

    private (GameConfig? Config, IReadOnlyList<string> Errors) Load()
    {
        if (_file.TakeLoadError() is { } loadError)
        {
            return (null, [loadError]);
        }
        GameSettings settings;
        try
        {
            settings = _configuration.GetSection("Game").Get<GameSettings>() ?? new GameSettings();
        }
        catch (InvalidOperationException e)
        {
            // A value of the wrong type, e.g. "Weight": "high".
            return (null, [e.InnerException?.Message ?? e.Message]);
        }
        var errors = settings.Validate();
        return errors.Count > 0 ? (null, errors) : (new GameConfig(settings, ItemRegistry.FromSettings(settings)), errors);
    }

    private void LogValues(string message, GameConfig config)
    {
        var items = config.Items.All.Select(i =>
            $"{i.Id} {config.Items.Share(i.Id):P0}" + (i.DurationSeconds is { } d ? $" {d}s" : ""));
        _logger.LogInformation(
            "{Message}: item chance {Chance} per block; {Items}",
            message,
            config.Settings.ItemSpawnChance,
            string.Join(", ", items));
    }

    public void Dispose() => _subscription.Dispose();
}
