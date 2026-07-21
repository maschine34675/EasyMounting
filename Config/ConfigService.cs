using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;

namespace EasyMounting.Config;

[Injectable(InjectionType.Singleton)]
public sealed class ConfigService(ISptLogger<ConfigService> logger)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private string ModFolder { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? AppContext.BaseDirectory;

    public EasyMountingConfig Load()
    {
        var path = Path.Combine(ModFolder, "Config", "config.json");
        if (!File.Exists(path))
        {
            logger.Warning($"[EasyMounting] {path} not found - using defaults (preset '{new EasyMountingConfig().Preset}').");
            return new EasyMountingConfig();
        }

        try
        {
            var config = JsonSerializer.Deserialize<EasyMountingConfig>(File.ReadAllText(path), Options);
            return config ?? new EasyMountingConfig();
        }
        catch (JsonException ex)
        {
            logger.Error($"[EasyMounting] config.json is invalid JSON: {ex.Message} - using defaults (preset '{new EasyMountingConfig().Preset}').");
            return new EasyMountingConfig();
        }
        catch (Exception ex)
        {
            logger.Error($"[EasyMounting] Failed to read config.json: {ex.Message} - using defaults (preset '{new EasyMountingConfig().Preset}').");
            return new EasyMountingConfig();
        }
    }
}
