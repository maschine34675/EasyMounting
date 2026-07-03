# EasyMounting

A simple SPT server mod that makes bipod deployment and weapon mounting (ledges, windowsills,
railings, etc.) less picky about the surface. Switch behavior via a preset in `config.json` -
no rebuild required.

## What it does

Vanilla EFT is fairly strict about what counts as a valid mounting point: the surface angle
tolerance, accepted height range, and detection resolution in `globals.json` under
`MountingSettings` reject a lot of real-world edge cases (tilted ledges, thin railings, uneven
cover). EasyMounting overwrites those values in memory at server startup according to the preset
you pick, plus widens how far up/down you can look while mounted (`PitchLimitHorizontal` /
`PitchLimitHorizontalBipod` / `PitchLimitVertical`).

## Why it's a server mod

These are pure server-authoritative config values - `globals.json` is sent to the client once per
raid, so relaxing them is just a matter of serving different numbers. No game code needs to
change, which keeps this mod simple and independent of the game client's version (as long as the
JSON schema stays the same).

## Presets

| Preset | Behavior |
|---|---|
| `Vanilla` | Matches stock game values - picking this effectively disables the mod. |
| `Relaxed` | Noticeably more forgiving - tilted ledges/rubble/uneven cover become usable. |
| `Loose` | Mounts on most terrain/clutter, only steep slopes still get rejected. |
| `AnySurface` | Maximum leniency - mount on almost anything, including thin/rounded railings. |

## Configuration

Edit `config/config.json` and restart the server:

```json
{
  "enabled": true,
  "preset": "AnySurface",
  "logAppliedValues": true,
  "overrides": {
    "maxHorizontalMountAngleDotDelta": null,
    "secondCheckVerticalDistance": null
  }
}
```

- `enabled` - turn the whole mod on/off.
- `preset` - one of `Vanilla`, `Relaxed`, `Loose`, `AnySurface`.
- `logAppliedValues` - log the final effective values to the server console on startup, useful
  for checking what's actually active.
- `overrides` - optional per-field fine-tuning layered on top of the chosen preset. Any field left
  `null` keeps the preset's value; see `Config/MountingOverrides.cs` for the full list of
  overridable fields (surface angle tolerances, grid/detection resolution, pitch limits, etc.).

## Limitations

- Only covers what's actually stored in `globals.json`. The left/right (yaw) look range for a
  bipod-less ledge mount, for example, is derived from the weapon's rig geometry inside the
  client, not from server config at all - no amount of tuning here can widen that.
- A handful of edge cases (e.g. certain thin railings in tight spaces) are blocked by hardcoded
  client-side checks that don't consult `PointDetectionSettings` at all - namely the raycast layer
  mask used to find a surface, and a separate "does my body fit here" clearance check run both at
  mount-start and continuously while mounted. This mod can't touch either.
