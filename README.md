# The Fading Resonance

A 2D top-down action RPG in C# with MonoGame.

> The world was once alive with sound. Every stone hummed, every tree sang. The
> Sound-Crystals kept the harmony — until the Stillness came.
>
> Now silence creeps across the land, hollowing creatures into mindless husks.
> The Bamboos and Raccoons you fight are what's left of the people who couldn't
> escape.
>
> You're a Tuner. Your sword vibrates to shatter the Stillness, and your Echo
> boomerang wakes dormant Sound-Crystals. Find the Great Tuning Fork in the
> North Dungeon before the world goes quiet for good.

By Mehdi Lakhouane.

## Combat and movement

Eight-way movement with animated walk, idle, and attack sprites. Sword attacks
have directional hitboxes, knockback, and invincibility frames.

The Echo is a boomerang with outbound and return physics — it bounces off walls
and tracks multiple hits on the way back. When it strikes a Sound-Crystal tile
it emits a pulse that reveals hidden paths for three seconds.

Pots, crates, and grass shatter and drop hearts or ammo.

## World

Rooms connect through a manager that handles transitions and a minimap. Pressure
plates hold or toggle to open doors. NPCs have idle animation, name tags, and
dialogue that changes with quest state. Signs are readable and rendered in
pixel art.

Dialogue is a typewriter text box with speaker names, multiple pages, and word
wrap.

## Items

The item catalog loads from JSON. Cycle equipped items with Q and E, use with X.
Ammo shows in the HUD slot as a bar.

## UI

Animated title screen with a scrolling background. Pause menu on Escape with
resume, controls, title, and quit. Heart-based health that pulses red when
critical, and a screen-wide red flash on hit.

The font is a custom 5x7 bitmap renderer covering A–Z, 0–9, and punctuation —
no SpriteFont or MGCB needed.

## Controls

| Action | Key |
|---|---|
| Move | W A S D |
| Attack | Space |
| Use item | X |
| Cycle items | Q / E |
| Interact | E |
| Pause | Escape or P |
| Debug hitboxes | F3 |

## Structure

Locked to 60 FPS. State machines drive game states, player behaviour, and enemy
AI.

```
Core/          Game1.cs — main loop, state management
States/        Menu, Overworld, Dungeon, PauseMenu
Entities/      Player, Enemy (Bamboo, Raccoon)
  Environment/   Breakable, PressurePlate, Interactable, NPC, SignPost
  PlayerStates/  Idle, Walk, Attack, Cooldown, Item
  EnemyStates/   Idle, Chase, Attack, Hurt, Dead, Wander, Circle
  Items/         Inventory, ItemData, Projectile
Systems/       StateMachine, Camera, CollisionSystem, RoomManager, TileMap, PulseEffect
UI/            DialogueBox
Utils/         AssetLoader, SpriteAnimation, PixelFont
Content/       Data/ (items, dialogues, world JSON), Sprites/, Tiles/, UI/, Audio/
```

Assets load through a custom raw loader that reads PNGs and WAVs directly from
streams, bypassing MGCB.

## Build

C# / .NET 8, MonoGame DesktopGL.

```bash
dotnet build
dotnet run
```

Sprites from the [Ninja Adventure asset pack](https://pixel-boy.itch.io/ninja-adventure-asset-pack).
