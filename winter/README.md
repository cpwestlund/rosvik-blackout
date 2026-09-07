# Rosvik Winter

Godot 4.6.3. Open the root project; the main scene is `winter/main.tscn`.
This is the winter slice approved by Patrik, recovered from the delivered
Windows executable on 2026-09-05. Script formatting/comments may differ from
the original source; gameplay, geometry, shaders and map data were recovered.
See `../docs/MALBILD.md` for product direction and `../docs/STATUS.md` for handoff.

Controls: WASD/arrows move, Shift run, hold E interact, F torch, wheel zoom,
Esc pause, F2 hide HUD. The mission restores school emergency power.

Validation:
```
godot --headless --path . --quit-after 120 -- --smoke-test
godot --headless --path . --fixed-fps 60 --quit-after 24000 -- --walk-test
```
The first must print WINTER_SMOKE_OK; the second WINTER_WALK_OK.
Check logs for SCRIPT ERROR and ERROR as well as process status.

The current game remains a short slice. No completed campaign, interiors,
survival economy, NPCs or final-quality character/vehicle assets yet.

## Loot checkpoint

Press I near the service van or refuge to transfer individual items. Elsewhere
I shows your pack. Capacity is 18 kg / 28 l. Condition is retained per stack.
The 56-entry catalog includes 18 types placed in two containers; other entries
are reserved for future locations. There are no use/consume/repair actions yet.
The mission battery remains the original separate carry interaction.

Autosave persists position, mission stage and all container/pack contents.
It saves on transfer, mission progress, every 30 seconds and window/menu exit.
A backup is retained and loaded when the primary save is invalid. Restart
requires confirmation. Tests bypass real saves and use dedicated temporary paths.

Additional checks:
```
godot --headless --path . --script winter/tests/loot_test.gd
godot --headless --path . --quit-after 120 -- --inventory-test
```
