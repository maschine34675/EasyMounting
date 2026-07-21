# EasyMounting

An SPT mod bundle that makes bipod deployment and weapon mounting (ledges, windowsills,
railings, etc.) far less picky about the surface. Two parts, shipped together:

- **Server mod** (`SPT/user/mods/EasyMounting`) - relaxes the surface-detection tolerances stored in
  `globals.json` via simple presets. Handles the vast majority of cases on its own.
- **Client plugin** (`BepInEx/plugins/maschine-EasyMounting.Client.dll`) - patches the handful of
  mounting checks hardcoded in the game client that no server config can reach, up to and
  including some deliberately "cursed" bypasses.

Neither strictly requires the other, but they're designed to be used together - some spots only 
become mountable with both installed.

## What it does

Vanilla EFT is fairly strict about what counts as a valid mounting point: the surface angle
tolerance, accepted height range, and detection resolution in `globals.json` under
`MountingSettings` reject a lot of real-world edge cases (tilted ledges, thin railings, uneven
cover). The server mod overwrites those values in memory at server startup according to the
preset you pick, plus widens how far up/down you can look while mounted. The client plugin then
covers what's left: railings whose collision lives on a physics layer the detection rays never
query, spots too tight for the body-clearance check, and even paper-thin colliders that have no
top surface for the scan to find at all.

---

# Part 1: Server mod

## Presets

Switch behavior via `config/config.json` - no rebuild required.

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

- `enabled` - turn the whole server mod on/off.
- `preset` - one of `Vanilla`, `Relaxed`, `Loose`, `AnySurface`.
- `logAppliedValues` - log the final effective values to the server console on startup, useful
  for checking what's actually active.
- `overrides` - optional per-field fine-tuning layered on top of the chosen preset. Any field left
  `null` keeps the preset's value.

## What each parameter actually does

All of this comes from tracing the actual scan code (`GClass2666`/`GClass2667` in the client),
not just the field names.

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

Left/right (yaw) look range isn't in this list on purpose: it isn't stored in `globals.json` at
all - the client derives it from the weapon's rig geometry at runtime, so no server config can
widen it.

---

# Part 2: Client plugin

The server mod can only change values that actually live in `globals.json`. A few parts of the
weapon-mounting pipeline are hardcoded in the client instead: which physics layers the detection
raycasts even query, a separate "does my body fit here" clearance check, how close you need to
end up to the computed stand position, and how aim rotation is clamped once mounted. The client
plugin patches exactly those.

## Configuration

All options live in `BepInEx/config/com.maschine.EasyMounting.cfg` after the first launch, or can
be edited live via the in-game F12 configuration manager.

### General

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | `true` | Master switch for the layer-mask widening below. |
| `IncludeLowPolyCollider` | `true` | Adds the `LowPolyCollider` layer to the mount-point raycast mask. Many thin/decorative railings only have collision on this layer (not `HighPolyCollider`), so vanilla's detection rays never hit them at all, no matter how permissive the server-side settings are. |
| `IncludeDoorCollider` | `false` | Also adds the door low-poly collider layer, for railings/frames near doorways. Off by default: mounts found on a door slab don't reliably register the vanilla dismount-when-door-moves hook, so a door opening under you can leave the weapon anchored in mid-air. |
| `NormalizeSwappedMountAnchors` | `true` | Some weapons ship a broken mounting anchor with the along-the-barrel offset in the sideways component and a garbage height offset (e.g. TRG M10), making the mounted weapon float ~1m beside the body (and ~20cm above the surface) with stretched arms, plus a ballooned aim window. When the sideways component dominates, the components are swapped into place and the height offset clamped to the healthy range at mount time. Affects vanilla mounts of such weapons too. |

### Cursed

Each of these disables a specific vanilla safety/validity check. They're independent and can be
toggled individually, but they were built to solve problems in sequence - see "How these fit
together" below.

| Setting | Default | What it does |
|---|---|---|
| `SkipClipCheck` | `true` | Skips the "does my body fit here" clearance check for standing/crouched ledge mounts (a `BoxCast` along the path to the required stand position). Lets you mount at spots that are geometrically valid but too tight for vanilla (e.g. close to a wall behind you) - at the cost of possibly visible clipping into geometry. Only applies to standing/crouched mounts; bipod-on-ground never runs this check. |
| `ReachToleranceMeters` | `0.30` (range 0.09-1.0) | How far you may end up from the computed stand position before the mount aborts. Vanilla is ~0.09m. Raise this when a mount visibly starts pulling you in and then pops back out - a sign a collider is blocking the last few centimeters. The pose may anchor slightly off the surface in exchange. The final alignment snap is collision-checked, so raising this cannot teleport you through geometry. Applies live. |
| `SynthesizeThinRailPoints` | `true` | When the vanilla scan finds nothing at all, re-runs it and synthesizes a mount point for paper-thin colliders. Some railings ("metalthin") have collision as a zero-thickness vertical sheet: forward rays hit the front face fine, but the downward probes that normally locate a top *surface* have nothing to land on - there is no surface, just an edge. Vanilla can never mount there, period. This anchors the weapon on that front-face top edge instead. Weapon placement on such rails may look slightly off. |
| `SkipRotationOverlapCheck` | `true` | While mounted, horizontal aiming normally predicts whether the resulting body shift would overlap geometry and blocks the rotation if so. At spots you only reached via `SkipClipCheck`, that prediction reports overlap permanently, freezing horizontal aim completely (vertical aim is unaffected - it doesn't shift the body sideways). This skips the prediction while mounted, restoring horizontal aim; the body may visibly rotate through the clipped geometry. |

### Support

| Setting | Default | What it does |
|---|---|---|
| `GenerateSupportPackage` | *unbound* | Hotkey that collects everything needed for a bug report into one zip - see "Reporting issues" below. Unbound by default (most players never need it, and a default key would likely collide with another mod); set one yourself in the F12 config manager. If `DebugMountLogging` is off, the first press arms it and waits `CaptureWindowSeconds` so you can reproduce the issue with mount traces included; press again during that window to capture immediately. |
| `CaptureWindowSeconds` | `15` (range 3-120) | How long to wait after arming debug logging before packaging. Only applies when `DebugMountLogging` was off at the time you pressed the hotkey. |
| `CleanupOldPackages` | `true` | Delete previous `EasyMounting-Support-*.zip` files after creating a new one, so the folder never ends up with several packages and no clue which one to attach. |

### Debug

| Setting | Default | What it does |
|---|---|---|
| `DebugMountLogging` | `false` | Traces every mount attempt to the BepInEx log: surface-scan result, validation result, computed stand position, and on every exit the remaining distance plus which code path triggered it. When the scan finds nothing, also dumps a full per-ray report of the coarse sweep and refine pass (which collider/layer each group of rays hit, where the edge was picked, why the refine pass rejected it). |

## How these fit together

They were added in the order a real investigation needed them, and layer on the same underlying
mount attempt:

1. **Point not found at all** -> `IncludeLowPolyCollider` (wrong raycast layer) or
   `SynthesizeThinRailPoints` (a real surface exists geometrically, but the collider is too thin
   for the refine pass to ever locate a top face).
2. **Point found and validated, but mounting refuses to even start** -> `SkipClipCheck` (the
   clearance BoxCast found something in the way).
3. **Mount starts, visibly pulls you in, then pops back out** -> `ReachToleranceMeters` (you got
   close but not within the vanilla ~9cm tolerance, likely the same nearby collider).
4. **Mounted, but can't turn left/right** -> `SkipRotationOverlapCheck` (you're in the state from
   #2/#3 clipping into something, and the rotation predictor notices every frame).
5. **Weapon floats beside or above the body while mounted, arms stretched** ->
   `NormalizeSwappedMountAnchors` (the weapon ships broken rig anchor data - a vanilla data bug,
   e.g. TRG M10 - which this fixes at mount time).

`DebugMountLogging` tells you which of these you're actually looking at instead of guessing -
turn it on first when a spot still doesn't work.

---

## Reporting issues

The support hotkey is unbound by default - set one under `Support` in the F12 config manager
(`GenerateSupportPackage`), then press it while the game is running.

- If `DebugMountLogging` is already on, the package is generated immediately.
- Otherwise the first press **arms** debug logging and gives you `CaptureWindowSeconds` (default
  15s) to reproduce the mount issue - press the hotkey again to capture right away instead of
  waiting out the timer. Either way, `DebugMountLogging` is switched back off automatically once
  the package is written, so you don't need to remember to turn it off again.

The mod collects everything a bug report needs into a single zip under
`<GameRoot>/EasyMounting-Support/` and opens Explorer with the file selected. The package contains:
`BepInEx/LogOutput.log`, both EasyMounting configs (client `.cfg` and, when a local server is
installed, the server `config.json`), the newest SPT server logs, and a generated `summary.txt`
with your loaded plugin list plus the mounting settings the server actually delivered to the
client. Missing sources (e.g. no local server on Fika clients) are skipped and noted in the
summary. The hotkey works with movement keys held, so you can press it right after reproducing a
problem.

The zip's own archive comment is set to the readme text, so WinRAR and 7-Zip display it
automatically when the file is opened - no need to extract anything to see what's inside or how to
share it.

**To share the package:** upload the zip to a temporary file-sharing service such as
[wormhole.app](https://wormhole.app/) and post the resulting link in your Forge comment or Discord
report. By default, generating a new package deletes older ones in the same folder
(`CleanupOldPackages`), so there's never more than one file to wonder about.

**Privacy note:** game and server logs can contain your in-game profile name and installed mod
list. Review the files before posting the zip publicly.

## Installation

- Server mod: copy the `EasyMounting` folder into `SPT/user/mods/`.
- Client plugin: copy `maschine-EasyMounting.Client.dll` into `BepInEx/plugins/`.

## Limitations / fragility

- The server mod only covers what's stored in `globals.json`; it is schema-stable and survives
  game client updates.
- The client plugin patches BSG's compiled game code directly, targeting their auto-generated
  internal class names (`GClass2666`, `GClass2667`, `IdleWeaponMountingStateClass`,
  `MovementContext`). Those numbered `GClassNNNN` names are not stable identifiers - they can
  shift after any EFT client update, which would break those patches. A missing target logs a
  registration failure on startup - but note that after renumbering, a `GClassNNNN` name can
  also resolve to a *different* existing class, in which case a patch may silently apply to the
  wrong method with no error at all. After a game update, assume the client plugin is broken
  until re-verified against the new client; the mod targets SPT 4.0.13 specifically.
- The "Cursed" settings are named that on purpose: they intentionally disable real anti-clipping
  and reachability checks. Expect visible clipping into geometry as the traded-off cost, not a
  bug to report.

## Requirements

SPT ~4.0.0

## License

MIT
