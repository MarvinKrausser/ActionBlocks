using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;

namespace ActionBlocks.Server.Config;

/// <summary>
/// Location of the game config file (config/game.json) and the last error from reading it.
/// The file is added as a configuration source, so its "Game" section binds to <see cref="Game.GameSettings"/>.
/// </summary>
public sealed class GameConfigFile
{
    private const string RelativePath = "config/game.json";

    /// <summary>Full path of the file, or null when none was configured or found.</summary>
    public string? Path { get; private init; }

    public bool Exists => Path is not null && File.Exists(Path);

    /// <summary>Why the last read failed (e.g. a JSON syntax error), or null. Cleared by <see cref="TakeLoadError"/>.</summary>
    private string? _loadError;

    public string? TakeLoadError() => Interlocked.Exchange(ref _loadError, null);

    /// <summary>
    /// Adds the file as a reloading configuration source. <paramref name="configuredPath"/> comes from GAME_CONFIG;
    /// when empty, config/game.json is searched upwards from <paramref name="contentRoot"/> (finds the repo root in dev).
    /// </summary>
    public static GameConfigFile AddTo(IConfigurationBuilder configuration, string? configuredPath, string contentRoot)
    {
        var file = new GameConfigFile
        {
            Path = string.IsNullOrWhiteSpace(configuredPath)
                ? FindUpwards(contentRoot)
                : System.IO.Path.GetFullPath(configuredPath, contentRoot),
        };
        if (file.Path is null)
        {
            return file;
        }
        var source = new JsonConfigurationSource
        {
            FileProvider = new PhysicalFileProvider(System.IO.Path.GetDirectoryName(file.Path)!),
            Path = System.IO.Path.GetFileName(file.Path),
            Optional = true,
            ReloadOnChange = true,
            // Don't crash on a broken edit; GameConfigProvider reports it and keeps the last good values.
            OnLoadException = context =>
            {
                file._loadError = context.Exception.InnerException?.Message ?? context.Exception.Message;
                context.Ignore = true;
            },
        };
        // Later sources win: insert before the environment variables so Game__* env vars still override the file.
        var envIndex = configuration.Sources.ToList().FindIndex(s => s is EnvironmentVariablesConfigurationSource);
        if (envIndex < 0)
        {
            configuration.Sources.Add(source);
        }
        else
        {
            configuration.Sources.Insert(envIndex, source);
        }
        return file;
    }

    private static string? FindUpwards(string start)
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, RelativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        return null;
    }
}
