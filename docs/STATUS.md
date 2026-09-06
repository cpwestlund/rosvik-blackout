# Handoff — 2026-09-05

Branch: rebuild/winter-slice. Based on main a5e92a3.
The approved WINTER binary survived; the prior temporary source checkout did
not. Recovered the winter project using GDRE Tools 2.6.4 (31 scripts, 34
resources, no conversion failures). Only winter source and its project settings
are restored here; existing main/Unity work is not overwritten remotely.

Next deliverable: data-driven contextual loot with mass, volume and condition,
then persistent inventory and mission save. Preserve the winter art baseline.
Do not call this a finished game. Read MALBILD.md for acceptance criteria.

## Loot checkpoint implemented

56 catalog definitions; 18 types placed in van/refuge. I opens inventory and
nearby storage, transfers one item, retains condition, enforces 18 kg / 28 l.
Mission/position/container saves, backup recovery and confirmed restart added.
School lighting and engine restore correctly for later mission stages.
Gameplay tests cover transfers, capacity, rejected state, corrupt save backup,
world roundtrip, UI pause/range and the entire original walking mission.
No graphics geometry/material/camera changes in this checkpoint.

Next: a garage with visible contextual objects, then actual item uses and
survival needs. Catalog entries are not proof of finished object functionality.

## Environment pass — 2026-09-06

User prioritizes recognizable school/sports hall and clean roads before garage.
Corrected roof triangle winding; distinguished the higher sports hall with
high windows and an entrance. User says small stand on player-facing long side;
recorded for future interior work. No claim that invented details match a survey.
Road banks stop at other roads, are lower/narrower, and wheel tracks are subtler.
CI exports an overview screenshot for inspection alongside the Windows build.

## School grounds pass
User approved grounds before loot artwork. Added courtyard/aprons/parking, planted islands, tree groups, Rosvalla field and contextual schoolyard props. Kept mapped Skolgränd; curved the authored pedestrian approaches. Reused existing procedural assets to preserve style and avoid unrelated asset setup. Loot artwork is next: start with 18 placed types, then all 56 catalog entries. Each item needs its own recognizable image.

## Inventory artwork + small environment corrections

18 placed item types now use individual regions of a generated realistic atlas,
with icons in lists and a larger selected-item preview. Moved three benches off
the paths and relocated bike racks. Corrected snow-bank winding (dark backfaces),
lowered road banks and softened ground patch borders. Existing mission, inventory
and saving are retained. The CI captures both overview and inventory.

User requirement: houses must eventually be enterable and searchable, with cozy,
detailed furnished interiors. Implement one complete interior before extending
that system across houses. Discuss the next pass before making further changes.

## First house, pass 1

User approved rooms/entry/camera with plausible Rosvik placement. Used existing
OSM footprint 1185295272 across Skolgränd northwest of the school entrance.
Hall, kitchen and living room have initial furnishings. Door opens with E;
player walks through physically. Roof and camera-facing upper walls hide inside,
collisions stay active; wind is quieter, exterior snow/footprints hidden indoors.
Position and door state save/load. House test walks from school, opens the door,
visits rooms, roundtrips save, and walks outside. No searchable house containers
yet: those and finer cozy details belong to pass 2 after discussion with user.

## Searchable first house
Three contextual containers: kitchen, hallway dresser and living-room tool box.
Reuses existing pictured item types. Added curtains, cushions, throw, crockery,
books and boots, all authored atmosphere rather than a reconstruction of a private home.
Loot schema 2 migrates legacy two-container saves once and preserves empty storage.
Automated house-loot route exercises all three UI transfers and save/reload.
Keep subsequent work bounded and discuss the next pass with the user first.
