namespace EasyMounting.Config;

public sealed class MountingOverrides
{
    public double? MaxHorizontalMountAngleDotDelta { get; set; }
    public double? MaxProneMountAngleDotDelta { get; set; }
    public double? MaxVerticalMountAngleDotDelta { get; set; }
    public double? GridMinHeight { get; set; }
    public double? GridMaxHeight { get; set; }
    public double? EdgeDetectionDistance { get; set; }
    public double? RaycastDistance { get; set; }
    public double? HorizontalGridSize { get; set; }
    public double? HorizontalGridStepsAmount { get; set; }
    public double? VerticalGridSize { get; set; }
    public double? VerticalGridStepsAmount { get; set; }
    public double? SecondCheckVerticalGridSizeStepsAmount { get; set; }
    public double? SecondCheckVerticalGridSize { get; set; }
    public double? SecondCheckVerticalDistance { get; set; }
    public double? SecondCheckVerticalGridOffset { get; set; }
    public double? PitchHorizontalMin { get; set; }
    public double? PitchHorizontalMax { get; set; }
    public double? PitchHorizontalBipodMin { get; set; }
    public double? PitchHorizontalBipodMax { get; set; }
    public double? PitchVerticalMin { get; set; }
    public double? PitchVerticalMax { get; set; }
}
