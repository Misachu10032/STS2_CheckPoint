# CheckPoint

A Slay the Spire 2 mod that automatically saves your run at the start of every floor, letting you reload any previous floor at any time.

## Demo
https://www.youtube.com/watch?v=YHtND8cpsZA

## Features

- Auto-saves a checkpoint at the beginning of every floor
- Checkpoints are saved to disk and survive game restarts
- Load any previous floor from a history panel
- Quick load to jump back to the current floor instantly

## Controls

| Input | Action |
|---|---|
| Hold `L` | Open/close checkpoint history panel |
| Hold `R` | Quick load current floor |
| `Esc` | Close panel |


## How it works
The mod uses [Harmony](https://github.com/pardeike/Harmony) to patch into the game at key points:
At the start of every floor, the mod captures the full run state and writes it to disk as a JSON file:

```
mod_checkpoints/active/floor_XX/checkpoint.json  — run state
mod_checkpoints/active/floor_XX/meta.json        — timestamp
```

Because they're on disk, checkpoints persist across game restarts. When you continue a run, the mod reads them back from disk automatically.

When you start a new run, the entire `active/` folder is wiped and recreated fresh — so checkpoints never carry over between runs.

## Installation
subscribe on steam workshop
https://steamcommunity.com/sharedfiles/filedetails/?id=3751584509

