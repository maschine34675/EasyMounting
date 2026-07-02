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
                $"[EasyMounting] Unknown preset '{config.Preset}', falling back to 'Vanilla'. " +
                $"Valid presets: {string.Join(", ", MountingPresets.All.Keys)}");
            preset = MountingPresets.All["Vanilla"];
        }

        preset = MountingPresets.WithOverrides(preset, config.Overrides);

        var mountingSettings = databaseService.GetGlobals().Configuration.MountingSettings;
        var pointDetection = mountingSettings.PointDetectionSettings;
        MountingPresets.Apply(pointDetection, preset);
        MountingPresets.ApplyMovement(mountingSettings.MovementSettings, preset);

        var overrideNote = config.Overrides != null ? " (with overrides)" : "";
        logger.LogWithColor($"[EasyMounting] Applied preset '{config.Preset}'{overrideNote} to weapon mounting surface detection.", LogTextColor.Green);

        if (config.LogAppliedValues)
        {
            logger.Info(
                "[EasyMounting] HorizontalDot=" + pointDetection.MaxHorizontalMountAngleDotDelta +
                " ProneDot=" + pointDetection.MaxProneMountAngleDotDelta +
                " VerticalDot=" + pointDetection.MaxVerticalMountAngleDotDelta +
                " GridHeight=[" + pointDetection.GridMinHeight + ", " + pointDetection.GridMaxHeight + "]" +
                " RaycastDistance=" + pointDetection.RaycastDistance +
                " EdgeDetectionDistance=" + pointDetection.EdgeDetectionDistance +
                " VerticalGridStepsAmount=" + pointDetection.VerticalGridStepsAmount +
                " SecondCheck[Offset=" + pointDetection.SecondCheckVerticalGridOffset +
                " Size=" + pointDetection.SecondCheckVerticalGridSize +
                " Steps=" + pointDetection.SecondCheckVerticalGridSizeStepsAmount +
                " Distance=" + pointDetection.SecondCheckVerticalDistance + "]");
        }

        return Task.CompletedTask;
    }
}
