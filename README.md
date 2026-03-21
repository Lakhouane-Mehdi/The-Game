# The Game
**Author:** Mehdi Lakhouane

A 2D top-down action-adventure game written in C# utilizing the MonoGame/FNA framework. "The Game" runs at a smooth locked 60 FPS and leverages a robust state-machine architecture to handle world transitions and complex entity behaviors.

## Features
- **Exploration:** Seamless transitions between multiple game states, including a wide Overworld map and intricate Dungeon layouts.
- **State-Driven Player Controller:** 8-directional movement mapped with a finite state machine handling idle, walking, attack cooldowns, and item usage. 
- **Combat & Items:** Integrated combat system with melee attacks, health/heart drops, and ranged projectiles such as boomerangs via a fully functional inventory system.
- **Custom Asset Pipeline:** Bypasses standard MGCB compilation in favor of a raw localized asset loader for quick iterations on texture sprites.

## Tech Stack
- **Language:** C#
- **Framework:** MonoGame / FNA
