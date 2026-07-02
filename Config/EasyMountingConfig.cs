namespace EasyMounting.Config;

public sealed class EasyMountingConfig
{
    public bool Enabled { get; set; } = true;
    public string Preset { get; set; } = "AnySurface";
    public bool LogAppliedValues { get; set; } = false;
    public MountingOverrides? Overrides { get; set; }
}
