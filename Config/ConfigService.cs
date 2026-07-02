using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;

namespace EasyMounting.Config;

[Injectable(InjectionType.Singleton)]
public sealed class ConfigService
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
        var path = Path.Combine(ModFolder, "config", "config.json");
        if (!File.Exists(path))
        {
            return new EasyMountingConfig();
        }

        var config = JsonSerializer.Deserialize<EasyMountingConfig>(File.ReadAllText(path), Options);
        return config ?? new EasyMountingConfig();
    }
}
