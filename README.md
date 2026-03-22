# The Game — "The Fading Resonance"
**Author:** Mehdi Lakhouane

A 2D top-down Zelda-like action RPG written in C# with the MonoGame framework. The world is built on Sound-Crystals, and a plague called "The Stillness" is turning creatures into hollow husks. You play as Mehdi, a Tuner, on a quest to re-ignite the Great Tuning Fork and save the world from silence.

Runs at a locked 60 FPS with a state-machine architecture powering game states, player behavior, and enemy AI.

## Features

### Core Gameplay
- **8-Way Movement** — Smooth directional movement with animated walk/idle/attack sprites
- **Melee Combat** — Sword attacks with directional hitboxes, knockback, and invincibility frames
- **Inventory System** — Item catalog loaded from JSON, cycle through equipped items with Q/E, use with X
- **Ranged Weapons** — Boomerang ("The Echo") with outbound/return physics, wall bouncing, and multi-hit tracking
- **Breakable Objects** — Pots, crates, and grass that shatter on hit and drop hearts/ammo
- **Echo Pulse** — When the boomerang hits a Sound-Crystal tile, it emits a pulse that reveals hidden paths for 3 seconds

### Narrative & Dialogue
- **Dialogue System** — Typewriter-effect text box with speaker names, multi-page conversations, and word wrapping
- **NPCs** — Interactive characters with idle bob animation, name tags, and quest-conditional dialogue
- **Sign Posts** — Readable signs placed throughout the world with pixel-art rendering
- **Story Scripts** — Dialogue loaded from JSON (`Content/Data/dialogues.json`):
  - **Old Man Elam** — Gives the first quest, explains the world's lore
  - **Silent Guard** — Blocks the dungeon until you find the Echo
  - **A Mysterious Pot** — Shouts at you if you try to break it
- **Quest Flags** — Tracked per session to unlock conditional dialogue and story progression

### World & Camera
- **Zelda-Style Room Scrolling** — World divided into screen-sized rooms with smooth lerp camera transitions
- **TileMap Renderer** — 16x16 tiles from Ninja Adventure spritesheets rendered at 2x scale via source rectangle math
- **3x2 Overworld Grid** — 6 themed rooms (Forest, Ruined Path, Quiet Village) with grass/earth tilesets
- **World Data** — Room definitions stored in JSON (`Content/Data/world.json`) with themes, quest flags, NPC/chest coordinates
- **Dungeon Room** — Separate dungeon state with stone floor tiles and corridor layout
- **Screen Shake** — Camera shake on enemy death, player hit, room transitions, and pulse activation

### Enemy AI
- **Finite State Machine** — Generic `StateMachine<T>` drives all enemy behavior (idle/chase/attack/hurt/dead)
- **Wander Behavior** — Bamboo enemies (hollowed husks of the Stillness) roam randomly
- **Circle + Dash** — Raccoon Guardian orbits the player then dashes in for attack
- **Knockback & Death** — Enemies get pushed back on hit with fade-out death animation

### Systems
- **AABB Collision** — Axis-separated resolution (move X then resolve, move Y then resolve) for wall sliding
- **Room Manager** — Activates/deactivates entities per room for performance
- **Pressure Plates** — Hold/toggle triggers that open doors (remove wall collision rects)
- **Pulse Effect** — Expanding ring visual with area reveal, triggered by Echo hitting Sound-Crystals

### UI & Menus
- **Title Screen** — Animated title with scrolling background, floating text, and blinking prompt
- **Command Menu** — In-game pause menu (Escape) with Resume, Controls, Title Screen, and Quit options
- **Controls Screen** — Full keybinding reference accessible from the pause menu
- **HUD** — Heart-based health display, item slot with ammo bar, room minimap with gold border
- **Pixel Font** — Custom 5x7 bitmap font renderer (A-Z, 0-9, punctuation) — no SpriteFont/MGCB needed
- **Low Health Flash** — Player sprite pulses red when health is critical
- **Damage Flash** — Screen-wide red overlay on player hit

## Controls

| Action | Key |
|---|---|
| Move | W A S D |
| Attack | Space |
| Use Item | X |
| Cycle Items | Q / E |
| Interact (NPC/Sign) | E |
| Pause Menu | Escape |
| Pause (Alt) | P |
| Debug Hitboxes | F3 |

## Tech Stack
- **Language:** C# / .NET 8
- **Framework:** MonoGame (DesktopGL)
- **Assets:** [Ninja Adventure](https://pixel-boy.itch.io/ninja-adventure-asset-pack) (free, open-source)
- **Asset Pipeline:** Custom raw loader (bypasses MGCB) — loads PNGs/WAVs directly via streams

## Project Structure
```
Core/          Game1.cs — main loop, state management
States/        MenuState, OverworldState, DungeonState, PauseMenuState
Entities/      Player, Enemy (Bamboo, Raccoon)
  Environment/   Breakable, PressurePlate, Interactable, NPC, SignPost
  PlayerStates/  Idle, Walk, Attack, Cooldown, Item
  EnemyStates/   Idle, Chase, Attack, Hurt, Dead, Wander, Circle
  Items/         Inventory, ItemData, Projectile
Systems/       StateMachine, Camera, CollisionSystem, RoomManager, TileMap, PulseEffect
UI/            DialogueBox
Utils/         AssetLoader, SpriteAnimation, PixelFont
Content/
  Data/        items.json, dialogues.json, world.json
  Sprites/     Player, monster, and weapon animations
  Tiles/       Tilesets and game_tileset.png
  UI/          Heart sprites
  Audio/       SFX and music
```

## The Lore

> *The world was once alive with sound. Every stone hummed, every tree sang. The Sound-Crystals kept the harmony — until the Stillness came.*
>
> *Now silence creeps across the land, hollowing out creatures into mindless husks — the Bamboos and Raccoons you fight are what remains of the people who couldn't escape.*
>
> *You are a Tuner. Your sword vibrates to shatter the Stillness. Your Echo boomerang can awaken dormant Sound-Crystals. Find the Great Tuning Fork in the North Dungeon before the world goes silent forever.*

## Build & Run
```bash
dotnet build
dotnet run
```

Requires .NET 8 SDK and MonoGame 3.8+.
