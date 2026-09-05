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
