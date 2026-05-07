# Prontera — design reference

RO-inspired walled city. First proper map deliverable. Used by the `map-author` subagent as canonical design intent. Update this doc to redirect future iterations of Prontera.

## Top-down sketch (200×200u)

```
                    Z+ (north gate)
                         │
        ┌────────────────┼────────────────┐
        │   NW district  │  NE district   │
        │  (residential) │   (merchant)   │
        │                │                │
        │     [house]    │  [weapon shop] │
        │                │                │
   X-───┤────[plaza]─────┼────[kafra]─────├──── X+ (east gate)
   west │   40×40 cobble │                │
   gate │   + fountain   │                │
        │                │                │
        │  [item shop]   │     [inn]      │
        │                │                │
        │   SW district  │   SE district  │
        │   (barracks)   │    (temple)    │
        └────────────────┼────────────────┘
                         │
                    Z- (south gate)
```

- **Bounds:** square 200×200u centered at world origin (corners at (±100, 0, ±100)).
- **Walls:** continuous on all four edges except cardinal midpoints (gates).
- **Gates:** four `bld_rockwall_archway_01` at (+100, 0, 0), (-100, 0, 0), (0, 0, +100), (0, 0, -100).
- **Plaza:** 40×40 paved area centered at origin with a single landmark prop (fountain / brazier substitute).
- **Districts:** quadrants outside plaza, ~80×80 each minus walls. Each district carries one signature landmark for navigation.

## Landmark slots (first iteration)

Each landmark is a single building/tent placed near the plaza. Categories pulled from `synty-catalog.md`:

| Slot | District | Suggested prefab | Notes |
|------|----------|------------------|-------|
| `kafra` | NE merchant | `bld_tent_01` (blue) | Teleport point, near plaza NE corner |
| `weapon-shop` | NE merchant | `bld_castle_door_01` framed by walls | Single tile, faces plaza |
| `item-shop` | SW barracks | `bld_tent_02` (red) | Plaza-adjacent |
| `inn` | SE temple | `bld_tent_03` | Plaza-adjacent, larger |
| `fountain` | plaza centre | `prop_brazier_01` (placeholder) | Replace with fountain prefab when imported |

Tent variants are intentional placeholders — real Prontera uses tile-roofed buildings; we substitute Synty Knights tents until a town pack is imported.

## Style conventions

- **Cardinal axes only** — no diagonal walls in V1. Easier NavMesh, easier validation.
- **Plaza at origin** — Player spawn + camera initial offset reference (0, 0, -12).
- **Parent groups** in JSON: `walls`, `gates`, `plaza`, `district-NE`, `district-NW`, `district-SE`, `district-SW`. Easier to toggle/inspect in Hierarchy.
- **Y=0 baseline** — no vertical content in V1. Hills/elevation in V2.
- **No props inside walls** — keep playable area clean for V1 testing.

## Build order (recommended for `map-author` subagent)

1. **Ground tiling** — single `tilings[]` entry, `env_tile_grass_01` covering 200×200, step matching tile size from catalog. Parent `ground`.
2. **Plaza paving** — overlay 40×40 `env_path_cobble_01` tiles at y=0.05 to sit just above grass. Parent `plaza`.
3. **Walls** — four `tilings[]` entries (N, S, E, W edges). Step matches `bld_rockwall_straight_01.size.z` from catalog. Stop at gate gaps (midpoint ±half-archway-width). Parent `walls`.
4. **Gates** — four explicit `pieces[]` for `bld_rockwall_archway_01` at cardinal midpoints. Rotation aligned with each wall axis. Parent `gates`.
5. **Plaza landmark** — fountain placeholder at origin. Parent `plaza`.
6. **District landmarks** — 4 explicit pieces, one per quadrant slot. Parent `district-<dir>`.
7. **Validate** → fix → **Preview** → screenshot → iterate.

## Acceptance

- `MapLoader.Load("prontera")` at runtime spawns the city without missing pieces.
- `MapValidator` reports 0 errors. Overlap warnings between ground + plaza tiles are expected (whitelisted prefixes in validator).
- Player spawned at origin can walk through any of the four gates without bumping into geometry.
- NavMesh bake shows continuous walkable area within the city.

## Future iterations (out of scope for V1)

- Replace tent placeholders with tile-roof town buildings (requires Synty PolygonTown or equivalent).
- Vertical content: temple raised platform in SE district, residential tier-2 houses in NW.
- Interior scenes (kafra office, inn rooms) — separate map JSONs loaded on door interaction.
- Day/night atmosphere overrides.
- NPC spawn slots data — separate manifest layered over map.
