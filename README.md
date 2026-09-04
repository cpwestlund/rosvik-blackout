# ROSVIK: BLACKOUT

Native Godot project for the Rosvik survival / blackout game.

## Direction

This repository replaces the earlier single-file HTML prototypes. The target is a real native game where Rosvik is recognizable as Rosvik, with the school and Norrbotten Stål Arena treated as proper landmarks rather than map blocks. Private interiors are not a priority; exterior world quality, animation, sound, atmosphere, interaction and gameplay feel are.

The visual target is a close isometric / three-quarter winter view with believable scale, snow, roads, parking areas, local props, lighting and an understated post-collapse atmosphere rather than zombies or combat.

## Current foundation

- Godot 4.6 project
- native third-person / isometric movement
- articulated animated player controller
- landmark slice with Rosviks skola and Norrbotten Stål Arena
- correctly scaled roads, cars, lighting, schoolyard and arena forecourt props
- dusk lighting and fog
- automatic Windows export through GitHub Actions

## Controls

- WASD / arrow keys: move
- Shift: run

## Builds

Every push to `main` is configured to produce a Windows artifact named `ROSVIK_BLACKOUT_Windows` through GitHub Actions.

## Production rule

Do not expand the map just to make it larger. Finish a small Rosvik slice to a believable quality bar first, then expand street by street.
