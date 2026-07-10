using EasyMounting.Config;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace EasyMounting;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class Mod(ConfigService configService, DatabaseService databaseService, ISptLogger<Mod> logger) : IOnLoad
{
    public Task OnLoad()
    {
        var config = configService.Load();

        if (!config.Enabled)
        {
            logger.Info("[EasyMounting] Disabled via config, leaving mounting settings untouched.");
            return Task.CompletedTask;
        }

        if (!MountingPresets.All.TryGetValue(config.Preset, out var preset))
        {
            logger.Warning(
                $"[EasyMounting] Unknown preset '{config.Preset}', leaving mounting settings untouched. " +
                $"Valid presets: {string.Join(", ", MountingPresets.All.Keys)}");
            return Task.CompletedTask;
        }

        var hasOverrides = config.Overrides != null && typeof(MountingOverrides)
            .GetProperties()
            .Any(p => p.GetValue(config.Overrides) != null);
        preset = MountingPresets.WithOverrides(preset, config.Overrides);

        var mountingSettings = databaseService.GetGlobals().Configuration.MountingSettings;
        var pointDetection = mountingSettings.PointDetectionSettings;
        MountingPresets.Apply(pointDetection, preset);
        MountingPresets.ApplyMovement(mountingSettings.MovementSettings, preset);

        var overrideNote = hasOverrides ? " (with overrides)" : "";
        logger.LogWithColor($"[EasyMounting] Applied preset '{config.Preset}'{overrideNote} to weapon mounting surface detection.", LogTextColor.Green);

        if (config.LogAppliedValues)
        {
            logger.Info("[EasyMounting] Effective values: " + preset);
        }

        return Task.CompletedTask;
    }
}
