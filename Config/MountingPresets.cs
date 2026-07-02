using SPTarkov.Server.Core.Models.Eft.Common;

namespace EasyMounting.Config;

public static class MountingPresets
{
    public sealed record Preset(
        double MaxHorizontalMountAngleDotDelta,
        double MaxProneMountAngleDotDelta,
        double MaxVerticalMountAngleDotDelta,
        double GridMinHeight,
        double GridMaxHeight,
        double EdgeDetectionDistance,
        double RaycastDistance,
        double HorizontalGridSize,
        double HorizontalGridStepsAmount,
        double VerticalGridSize,
        double VerticalGridStepsAmount,
        double SecondCheckVerticalGridSizeStepsAmount,
        double SecondCheckVerticalGridSize,
        double SecondCheckVerticalDistance,
        double SecondCheckVerticalGridOffset,
        double PitchHorizontalMin,
        double PitchHorizontalMax,
        double PitchHorizontalBipodMin,
        double PitchHorizontalBipodMax,
        double PitchVerticalMin,
        double PitchVerticalMax);

    public static readonly Dictionary<string, Preset> All = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Vanilla"] = new Preset(
            MaxHorizontalMountAngleDotDelta: 0.707,
            MaxProneMountAngleDotDelta: 0.939,
            MaxVerticalMountAngleDotDelta: 0.707,
            GridMinHeight: 0.81,
            GridMaxHeight: 1.4,
            EdgeDetectionDistance: 1.2,
            RaycastDistance: 1.6,
            HorizontalGridSize: 0.5,
            HorizontalGridStepsAmount: 15,
            VerticalGridSize: 1.0,
            VerticalGridStepsAmount: 20,
            SecondCheckVerticalGridSizeStepsAmount: 5,
            SecondCheckVerticalGridSize: 0.05,
            SecondCheckVerticalDistance: 0.07,
            SecondCheckVerticalGridOffset: 0.05,
            PitchHorizontalMin: -15, PitchHorizontalMax: 15,
            PitchHorizontalBipodMin: -20, PitchHorizontalBipodMax: 20,
            PitchVerticalMin: -15, PitchVerticalMax: 15),

        ["Relaxed"] = new Preset(
            MaxHorizontalMountAngleDotDelta: 0.55,
            MaxProneMountAngleDotDelta: 0.85,
            MaxVerticalMountAngleDotDelta: 0.55,
            GridMinHeight: 0.65,
            GridMaxHeight: 1.55,
            EdgeDetectionDistance: 1.4,
            RaycastDistance: 1.8,
            HorizontalGridSize: 0.6,
            HorizontalGridStepsAmount: 20,
            VerticalGridSize: 1.15,
            VerticalGridStepsAmount: 30,
            SecondCheckVerticalGridSizeStepsAmount: 8,
            SecondCheckVerticalGridSize: 0.09,
            SecondCheckVerticalDistance: 0.11,
            SecondCheckVerticalGridOffset: 0.05,
            PitchHorizontalMin: -22, PitchHorizontalMax: 22,
            PitchHorizontalBipodMin: -28, PitchHorizontalBipodMax: 28,
            PitchVerticalMin: -20, PitchVerticalMax: 20),

        ["Loose"] = new Preset(
            MaxHorizontalMountAngleDotDelta: 0.35,
            MaxProneMountAngleDotDelta: 0.65,
            MaxVerticalMountAngleDotDelta: 0.35,
            GridMinHeight: 0.5,
            GridMaxHeight: 1.7,
            EdgeDetectionDistance: 1.6,
            RaycastDistance: 2.0,
            HorizontalGridSize: 0.75,
            HorizontalGridStepsAmount: 26,
            VerticalGridSize: 1.3,
            VerticalGridStepsAmount: 42,
            SecondCheckVerticalGridSizeStepsAmount: 10,
            SecondCheckVerticalGridSize: 0.14,
            SecondCheckVerticalDistance: 0.16,
            SecondCheckVerticalGridOffset: 0.06,
            PitchHorizontalMin: -30, PitchHorizontalMax: 30,
            PitchHorizontalBipodMin: -38, PitchHorizontalBipodMax: 38,
            PitchVerticalMin: -26, PitchVerticalMax: 26),

        ["AnySurface"] = new Preset(
            MaxHorizontalMountAngleDotDelta: 0.05,
            MaxProneMountAngleDotDelta: 0.30,
            MaxVerticalMountAngleDotDelta: 0.05,
            GridMinHeight: 0.2,
            GridMaxHeight: 2.0,
            EdgeDetectionDistance: 2.6,
            RaycastDistance: 3.5,
            HorizontalGridSize: 0.9,
            HorizontalGridStepsAmount: 40,
            VerticalGridSize: 1.5,
            VerticalGridStepsAmount: 160,
            SecondCheckVerticalGridSizeStepsAmount: 40,
            SecondCheckVerticalGridSize: 0.5,
            SecondCheckVerticalDistance: 0.5,
            SecondCheckVerticalGridOffset: 0.05,
            PitchHorizontalMin: -40, PitchHorizontalMax: 40,
            PitchHorizontalBipodMin: -50, PitchHorizontalBipodMax: 50,
            PitchVerticalMin: -34, PitchVerticalMax: 34),
    };

    public static Preset WithOverrides(Preset basePreset, MountingOverrides? overrides)
    {
        if (overrides == null)
        {
            return basePreset;
        }

        return basePreset with
        {
            MaxHorizontalMountAngleDotDelta = overrides.MaxHorizontalMountAngleDotDelta ?? basePreset.MaxHorizontalMountAngleDotDelta,
            MaxProneMountAngleDotDelta = overrides.MaxProneMountAngleDotDelta ?? basePreset.MaxProneMountAngleDotDelta,
            MaxVerticalMountAngleDotDelta = overrides.MaxVerticalMountAngleDotDelta ?? basePreset.MaxVerticalMountAngleDotDelta,
            GridMinHeight = overrides.GridMinHeight ?? basePreset.GridMinHeight,
            GridMaxHeight = overrides.GridMaxHeight ?? basePreset.GridMaxHeight,
            EdgeDetectionDistance = overrides.EdgeDetectionDistance ?? basePreset.EdgeDetectionDistance,
            RaycastDistance = overrides.RaycastDistance ?? basePreset.RaycastDistance,
            HorizontalGridSize = overrides.HorizontalGridSize ?? basePreset.HorizontalGridSize,
            HorizontalGridStepsAmount = overrides.HorizontalGridStepsAmount ?? basePreset.HorizontalGridStepsAmount,
            VerticalGridSize = overrides.VerticalGridSize ?? basePreset.VerticalGridSize,
            VerticalGridStepsAmount = overrides.VerticalGridStepsAmount ?? basePreset.VerticalGridStepsAmount,
            SecondCheckVerticalGridSizeStepsAmount = overrides.SecondCheckVerticalGridSizeStepsAmount ?? basePreset.SecondCheckVerticalGridSizeStepsAmount,
            SecondCheckVerticalGridSize = overrides.SecondCheckVerticalGridSize ?? basePreset.SecondCheckVerticalGridSize,
            SecondCheckVerticalDistance = overrides.SecondCheckVerticalDistance ?? basePreset.SecondCheckVerticalDistance,
            SecondCheckVerticalGridOffset = overrides.SecondCheckVerticalGridOffset ?? basePreset.SecondCheckVerticalGridOffset,
            PitchHorizontalMin = overrides.PitchHorizontalMin ?? basePreset.PitchHorizontalMin,
            PitchHorizontalMax = overrides.PitchHorizontalMax ?? basePreset.PitchHorizontalMax,
            PitchHorizontalBipodMin = overrides.PitchHorizontalBipodMin ?? basePreset.PitchHorizontalBipodMin,
            PitchHorizontalBipodMax = overrides.PitchHorizontalBipodMax ?? basePreset.PitchHorizontalBipodMax,
            PitchVerticalMin = overrides.PitchVerticalMin ?? basePreset.PitchVerticalMin,
            PitchVerticalMax = overrides.PitchVerticalMax ?? basePreset.PitchVerticalMax,
        };
    }

    public static void Apply(MountingPointDetectionSettings target, Preset preset)
    {
        target.MaxHorizontalMountAngleDotDelta = preset.MaxHorizontalMountAngleDotDelta;
        target.MaxProneMountAngleDotDelta = preset.MaxProneMountAngleDotDelta;
        target.MaxVerticalMountAngleDotDelta = preset.MaxVerticalMountAngleDotDelta;
        target.GridMinHeight = preset.GridMinHeight;
        target.GridMaxHeight = preset.GridMaxHeight;
        target.EdgeDetectionDistance = preset.EdgeDetectionDistance;
        target.RaycastDistance = preset.RaycastDistance;
        target.HorizontalGridSize = preset.HorizontalGridSize;
        target.HorizontalGridStepsAmount = preset.HorizontalGridStepsAmount;
        target.VerticalGridSize = preset.VerticalGridSize;
        target.VerticalGridStepsAmount = preset.VerticalGridStepsAmount;
        target.SecondCheckVerticalGridSizeStepsAmount = preset.SecondCheckVerticalGridSizeStepsAmount;
        target.SecondCheckVerticalGridSize = preset.SecondCheckVerticalGridSize;
        target.SecondCheckVerticalDistance = preset.SecondCheckVerticalDistance;
        target.SecondCheckVerticalGridOffset = preset.SecondCheckVerticalGridOffset;
    }

    public static void ApplyMovement(MountingMovementSettings target, Preset preset)
    {
        target.PitchLimitHorizontal = new XYZ { X = preset.PitchHorizontalMin, Y = preset.PitchHorizontalMax, Z = 0 };
        target.PitchLimitHorizontalBipod = new XYZ { X = preset.PitchHorizontalBipodMin, Y = preset.PitchHorizontalBipodMax, Z = 0 };
        target.PitchLimitVertical = new XYZ { X = preset.PitchVerticalMin, Y = preset.PitchVerticalMax, Z = 0 };
    }
}
