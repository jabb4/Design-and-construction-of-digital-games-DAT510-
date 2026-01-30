## Must haves (aka MVP)
### UI

- Start screen
	- Start new game button
	- Load saved game button
	- Quit game button
- Gameplay (in raid)
	- Health bar (lower left corner)
	- Money (top right corner)
- Common Enemy
	- Health bar (abover their head)
- Home base view (Van view)
	- Raid button

### Gameplay

- Raid
	- Waves of enemies
	    - Wave one: One enemy
	    - Wave two: Two enemies
	    - .....
	    - Wave five: One mini-boss
	- "Jump into" van when you want to leave the raid
	    - Takes 30 sec
	    - Takes you back to van view

### Player

- Camera: 3rd person view, lock on enemy during combat
- Movement: Walk, Sprint, Jump
- HP: Base HP, can't be upgraded.
- Combat
  - Attacks: One light (left mouse click)
  - Defense: One block (right mouse click)
- Weapon
  - One katana: can't be changed
  - Sharpness can be upgraded with money -> increases dmg.

### Enemy

- One "common", one mini-boss
- Type: Short range, melee
- Weapon: Katana
- Style: Human like robot
- Combat
  - Attack and defense same as player
  - Runs towards the player if in line of sight

### Combat

- Attack:
  - Damages the entity
- Block:
  - Block an attack reduces the damage taken.
- Parry:
  - A "perfect block" -> entity takes no damage.

### Environment

- Camper van (which is the players "home base")
- City (raid)
  - Some buildings
  - An open space where combat can happen
  - Style:
    - Abandoned city look
  - Van is parked on some road.
  - Enemies spawn in the building and starts to run out in to the city a bit

### Game systems

- Game saves: Every time player gets back to van view, the game state is saved.

## Should haves

### UI

- Map
	- Van location
	- Boss location
	    - How far away it is
	- City location
	    - How far away it is
- Boss
	- Big Health bar (top center)
- Gameplay (in raid)
	- Healing item (lower right corner)
- Home base view (Van view)
	- Money (top right corner)
	- Fuel hover
	    - See how much fuel you have
	    - Upgrade max fuel button
	    - Buy new fuel button

### Player

- Inventory
	- Holds 10 items
	- Loot picked up during raid is put into inventory
- Armor: Adds defense
- Bandage: Heals 15%, takes 10 sec to apply
- Purse: Holds money that the player can collect during the raid.

### Combat

- More attacks
- Combo attacks
  - Chain different attacks together

### Environment

- Camper van
  - Upgrades:
    - Max Fuel -> Increase van range
    - Radar system
  - Fuel: You can spend money to fill up your fuel
  - Storage
- Shop
  - Buy different items
    - Healing items
    - Armour

### Gameplay

- Boss fight
- Storyline intro "movie"
- Gameplay tutorial
  - How the game works etc.
- Collect money from killed enemies
- Upgraded van, buy fuel

### Game system

- Sound effect for most things

## Could haves
### UI

- Start screen
  - Settings menu button
    - Options for brightness etc.???

### Gameplay

- Animated storyline intro

### Combat

- Posture system:
  - If we time multiple attacks the enemy can get unbalanced
    - One taps the enemy
- Block spam prevention system

### Enemy

- Type: Range

### Environment

- New raid location

## Won't haves
### Player

- Appearance customization
- Different weapons

### Game systems

- Multiplayer
