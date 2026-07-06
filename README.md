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
  `null` keeps the preset's value.

## What each parameter actually does

All of this comes from tracing the actual scan code (`GClass2666`/`GClass2667` in the client),
not just the field names - see below for how each one fits into the mounting pipeline.

Weapon mounting without a deployed bipod (ledges, windowsills, railings) works in two passes: a
coarse sweep that finds the *edge* of an obstacle in front of you, then a fine refine pass that
confirms the exact *top surface* there. Bipod-on-the-ground (prone) uses a single, much simpler
raycast and doesn't consult most of these at all.

**Surface angle tolerance** - each is a minimum for `Dot(surface normal, world-up)`; `1.0` means
the surface must be perfectly flat, lower values accept steeper/rougher tilts, `0.0` accepts
surfaces up to 90° off vertical.

| Parameter | Used for |
|---|---|
| `maxHorizontalMountAngleDotDelta` | The refine pass's "is this top surface flat enough" check for a standing/crouched ledge mount (no bipod contact with the ground). |
| `maxProneMountAngleDotDelta` | The single check for deploying a bipod directly on the ground/terrain while prone. |
| `maxVerticalMountAngleDotDelta` | The equivalent flatness check for the vertical wall-lean fallback (used when no horizontal ledge is found). |

**Coarse sweep (finds the obstacle's edge)** - a vertical column of forward-facing rays scans
top to bottom looking for where a surface begins.

| Parameter | Used for |
|---|---|
| `gridMinHeight` / `gridMaxHeight` | Absolute floor/ceiling (relative to the player) of the height band that gets scanned at all. Widen to let lower or higher obstacles qualify. |
| `verticalGridSize` | Total height span of the coarse sweep (clamped by the two above). |
| `verticalGridStepsAmount` | Number of samples across that span - i.e. the scan's vertical resolution. Too coarse and a thin rail/bar can fall between two samples and never get seen at all. |
| `raycastDistance` | Maximum forward reach of each sample ray. |
| `edgeDetectionDistance` | Used twice: hits farther than this are ignored outright, and it's also the threshold that decides whether two consecutive hits are "the same surface continuing" or "a fresh edge" - i.e. it's the actual edge-detection logic, not just a range cutoff. |

**Refine pass (confirms the exact top surface)** - once an edge is found, a few more rays probe
slightly above and ahead of it to pin down the real, flat top surface.

| Parameter | Used for |
|---|---|
| `secondCheckVerticalGridOffset` | How far above the coarse edge point the refine probes start (safety margin so they don't start inside the object). |
| `secondCheckVerticalGridSize` / `secondCheckVerticalGridSizeStepsAmount` | How far forward, and with how many samples, the refine pass searches for the top surface. Also doubles as the length of an initial forward "is anything blocking the view down onto this surface" clearance ray. |
| `secondCheckVerticalDistance` | How far down each refine probe reaches. Short values are the main reason thin or curved surfaces (round railing tubes) get missed even when the coarse sweep found them fine. |

**Wall-lean fallback** (tried only when no horizontal ledge is found):

| Parameter | Used for |
|---|---|
| `horizontalGridSize` / `horizontalGridStepsAmount` | Width and resolution of the sideways scan used to find a nearby vertical surface to lean against. Not used by the main ledge search. |

**Look freedom while mounted** (`MovementSettings`, not detection - controls the camera, not
whether a spot counts as mountable):

| Parameter | Used for |
|---|---|
| `pitchHorizontalMin` / `pitchHorizontalMax` | Up/down look range for a standing/crouched ledge mount with no bipod ground contact. |
| `pitchHorizontalBipodMin` / `pitchHorizontalBipodMax` | Same, but with the bipod deployed on the ledge. |
| `pitchVerticalMin` / `pitchVerticalMax` | Up/down look range for the vertical wall-lean mount. |

Left/right (yaw) look range isn't in this list on purpose - see Limitations below.

## Limitations

- Only covers what's actually stored in `globals.json`. The left/right (yaw) look range for a
  bipod-less ledge mount, for example, is derived from the weapon's rig geometry inside the
  client, not from server config at all - no amount of tuning here can widen that.
- A handful of edge cases (e.g. certain thin railings in tight spaces) are blocked by hardcoded
  client-side checks that don't consult `PointDetectionSettings` at all - namely the raycast layer
  mask used to find a surface, and a separate "does my body fit here" clearance check run both at
  mount-start and continuously while mounted. This mod can't touch either.

## Optional companion: EasyMountingClient

A separate, independent BepInEx client mod (own folder, no dependency between the two) covers the
cases this server mod fundamentally can't - see its own README for details. Short version: it
widens the raycast layer mask so thin/LowPoly-only railings are even detectable, and offers a set
of increasingly aggressive ("Cursed") bypasses for the anti-clip and reachability checks that sit
entirely outside `globals.json`, up to synthesizing a mount point on paper-thin colliders that
have no real top surface at all.

Its patches target BSG's auto-generated internal class names (`GClass2666`/`GClass2667`/etc.),
which can shift on game updates and may need re-identifying afterwards - the server mod above has
no such fragility.

## Requirements

SPT ~4.0.0

## License

MIT
