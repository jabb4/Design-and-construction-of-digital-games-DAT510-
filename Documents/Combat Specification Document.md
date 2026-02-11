## Design Goal

Create a **small, polished, Sekiro-inspired combat experience** that demonstrates:

- Timing-based parrying
- Aggressive melee duels
- Clear, readable combat feedback

### Core design priorities

1. **Timing over stats:** Player success depends on reaction timing, not upgrades.
2. **Aggression is rewarded:** Attacking and parrying are safer than passive blocking.
3. **Simple systems, clear feedback:** Every combat interaction must have obvious visual/audio feedback to feel satisfying to the player.
4. **Handle small enemy groups:** Too many enemies will overwhelm the player.

---

## Core Feature Set

### Player Mechanics

| Feature       | Description                           |
| ------------- | ------------------------------------- |
| Movement      | WASD + SHIFT to sprint                |
| Camera        | Third-person camera with lock-on      |
| Light Attack  | Sword slash                           |
| Block         | Guard input (tap or hold)             |
| Perfect Parry | Timed deflect during parry window     |
| Health        | Player can die if health reaches zero |

### Enemy Mechanics

| Feature    | Description                          |
| ---------- | ------------------------------------ |
| Attacks    | 3-4 melee attacks                    |
| Telegraphs | Clear wind-up before each attack     |
| Health     | Enemy can die if health reaches zero |

---

## Combat Rules & Systems

### Health System

- Health is a simple numeric value
- Health is reduced when attacks are not blocked or parried
- Health does **not regenerate**

### Blocking & Parry System

#### Block

- Activated by guard input (right click), using tap or hold
- Holding keeps block active continuously
- Tapping opens a short block linger window so block/parry can register without holding
- Dull sound effect
- Reduces damage dealt (50% reduction)

#### Perfect Parry

- Triggered when guard is pressed and the hit lands **within the active parry window**
- Negates all damage
- Plays special sound + VFX

**Base Timing Window:** 0.2 seconds from the latest guard press.

### Attack Resolution Rules

| Situation     | Result                |
| ------------- | --------------------- |
| No block      | Full health damage    |
| Block         | Reduced health damage |
| Perfect parry | No damage             |

---

## Animation & Character Behavior Strategy

### Animation Strategy

> Animation quality is less important than **clear timing**.

Attack windows and eligibility checks are tied to animation events configured in Unity's Animator system. These events:

- Trigger hitboxes during specific active frames.
- Signal transitions between states like idle, wind-up, and recovery.
  Implementation relies on Animator parameters and conditions, which should align with the state machine hierarchy to maintain timing consistency.

#### Free assets

- Mixamo
- Unity Starter Assets (humanoid animations)

### Enemy Specification

#### Enemy States & Animations

- **Idle:** Facing player
  - **Walking:** Slow movement
  - **Sprinting:** Fast movement
- **Active:**
  - **Attacking:** (with animation phases)
    - **Wind-Up:** Telegraphing attack
    - **Slash:** Active hit frames
    - **Slow-down:** Brief pause after attack
  - **Defensive:**
    - **Pursuit:** Walking towards player
    - **Orbiting:** Circling around player
- **Dead:** Falls to the ground, disabled

### Player Specification

#### Player States & Animations

- **Idle:** Reset to idle stance
- **Walking:** Slow movement
- **Sprinting:** Fast movement
- **Attacking:** (attack combos with phases)
  - **Slash 1:** Wind-Up → Slash → Slow-down
  - **Slash 2:** Wind-Up → Slash → Slow-down
  - **Slash 3:** Wind-Up → Slash → Slow-down
- **Dead:** Falls to the ground, game over

#### Player Attack Chain

The player attack system implements a combo chain using a state machine approach, extending the base character controller.

When the attack input is pressed while in Idle or certain movement states, transition to Slash 1. Each slash state follows a phased animation: Wind-Up (telegraph) → Slash (hit frames) → Slow-down (recovery).

- If attack input is detected during the Slow-down phase of Slash 1, transition immediately to Slash 2, skipping the full return to Idle.
- Similarly for Slash 2 → Slash 3.
- After Slash 3's Slow-down, or if no input is received, transition back to Idle.
- Each subsequent slash starts from the pose at the end of the previous Slow-down, ensuring seamless visual flow without redundant animations.

This allows for combos, with timing windows for inputs during recovery phases to maintain responsiveness.

Every attack in the combo chain registers active hit detection during specific animation frames. Hitbox activation is entirely controlled by Unity animation events, which sync with attack state transitions.

#### Anti-spam system

The player's parry mechanic includes an anti-spamming system that reduces the timing window for parries after successive rapid presses.

---

## Technical Architecture

### Character controller / State machine

A scalable design for the player state machine. Instead of complicated player controller, we can break up the player code into different states. Each state has its own code that is only executed when that state is active. All of the states should extend from a class like move, and this means that any functions in the move class can be called by nodes which extend it, leading to not having obsolete code. For example, if we want to call a specific function in the move class, but have it be slightly different in a particular state, we can rewrite that function within that states code. That state will then use the updated function, while all other states will use the default version, unless specified otherwise.

This is great for creating custom behaviours that only occur in given states, for example customizing the behaviour executed upon entry or exit of a particular state. For example the implementation of **weighted movement:** (idle -> start-running -> running -> stop-running -> idle) and also **attack combos:** (slash 1 -> slash 2 -> slash 3).

Each state should contain its own logic that dictates whether it should switch to a new state or stay on the current one. This switch logic may need to be checked each tick, switching the active state where applicable before finally executing any state specific code.

Interaction triggers like hit, block, and parry are controlled by state transitions. Unity's Animation system ensures that reaction states (e.g., Parry or Block State) activate during specified animation frames. For example, a hitbox/hurtbox interaction triggers `react_on_hit()` only if the state conditions align with the animation event.

Example Integration with Combat Logic:

- During the player's attack, the state machine handles transitions like:
  (Idle → Slash Wind-Up → Slash Active → Recovery → Idle).
- Reaction triggers, such as block, parry, or hit states, run within this modular architecture, enabling shared logic for both player and enemy state machines.

### Camera Locking System

When toggling the camera lock, it should check the camera's view for any lock-on components that are on screen. It should first check the top section of the view (over the player's standing point), and if no targets are found, we check the bottom section. Upon identifying targets, those closest to the center of the screen are considered, with a weighting system to favor targets closer to the player. With an optimal target selected, the camera rig is then rotated to face the target.

To switch between targets, we take the input vector of the mouse and draw a line from the target in that direction in the view space. The view space is the coordinate system where the x and y axis are parallel to the screen, with the z axis corresponding to depth into the scene. We can use the x and y coordinates of objects in the view space to identify where they are on screen. We will calculate the difference in the angle between the input vector and each other target on screen. With consideration to distance, the target with the smallest angle is selected as the new camera target.

> Initially planned to use Cinemachine Target Group for lock-on, but the package signature was invalid so it could not be used.

### Weighted Locomotion

Movement is camera-relative, with animation blending driven by the current input direction and lock-on state.

When locked on, the full 2D input vector drives directional blending (strafe + forward/back). When not locked on, blending prioritizes forward/back motion, with strafe reduced in the animation weighting. Locomotion flows through start/loop/stop phases, with the chosen phase and direction adapting to lock-on vs free movement.

### Player Movement Weighting & Weapon Handling

#### Movement Weighting

- Locomotion uses a start -> loop -> stop flow, with directional starts and stops while locked on
- Acceleration/deceleration for input press/releases for a more natural movement feel
- Airborne movement has reduced control compared to grounded movement
- Landing blends movement back in before full rotation and locomotion control resumes

#### Equip/Unequip & Transition Rules

- Equip weapon triggers when locked on, blocking, or attacking, and only begins while grounded
- Unequip weapon triggers after a delay when none of those conditions are true, and is suppressed while sprinting or jumping
- Equip/unequip transitions suppress movement and state transitions until the animation completes
- Blocking is only valid when equipped and grounded
- During attack recovery, blocking can start immediately, but movement and jump transitions are delayed until recovery passes

### Combat Mechanics

#### Hit Detection

When a hitbox intersects with a hurtbox, we trigger the owner of the hurtbox to react on hit: `hurtbox.owner.react_on_hit()`. The eligibility checks we need to do when the hitbox and hurtboxes collide are:

- The intersecting hitbox and hurtbox do not share the same owner
- Is the hurtbox owner currently vulnerable? `is_vulnerable == true`
- Is the hitbox owner currently attacking? `is_attacking == true`
- The hurtbox is not on the hitbox ignore list: `not hitbox_ignore_list.has(hurtbox)` (to stop multiple hits registering from the same attack. Upon registering the initial hit, we append the hurtbox to the hitbox ignore list, so that it is not detected again until the next attack, where the ignore list is refreshed)
  To check these conditions we use a back-end animation database that stores information about the characters for the duration of each animation.

Attack windows for hitbox and hurtbox checks rely heavily on Unity's Animator system's animation events. Animation events are used to enable and disable hit detection only during active attack frames. Within these attack frames:

- `is_attacking` and `is_vulnerable` are animation-driven flags updated dynamically by the state machine.
- These variables should be synchronized with Unity Animator triggers, ensuring behavior aligns with visual timing.

After verifying the eligibility of a hit, we then check the receiver's current state to decide between three outcomes. Parry state that triggers the parry logic, Block state that triggers the block logic, and anything else (other states) triggers the hit logic. For each of these states, we call a react function that adjusts the health (and posture if implemented) based on the attack's hit data. We then force the state machine to transition the respective reactionary state. Each outcome has its own particles, lighting and sound effects that are triggered upon activation.

#### Spam Prevention

Every time the player presses the parry button in quick succession without actively parrying an attack, it should diminish the timeframe in which the parry window is active. This is to prevent the player spamming the parry button.

To achieve this we use a parry counter that tracks rapid guard presses.

| Rapid press count | Parry window |
| ----------------- | ------------ |
| 1                 | 0.2s         |
| 2                 | 0.2s         |
| 3                 | 0.1s         |
| 4+                | 0.0s         |

When the window reaches `0.0s`, guard still blocks but no perfect parry is possible.

**Reset conditions:**

- No rapid presses for ~0.5 seconds
- Successful perfect parry

**After a successful perfect parry:**

- Movement and attack are locked for 0.5 seconds
- Blocking can continue during this lock
- Parry animation should complete unless interrupted by a new hit/input state change

### Enemy Behaviour

Like our player controller, all state machine nodes extend from a common class, the Hierarchical Finite State Machine (HFSM). On ready, we check each node for children, and if they have children, the node is marked as a container. Every game tick, we check the top of the hierarchy, and if the node is a container we check conditions to determine which internal move is chosen. This is repeated indefinitely until a non-container node is selected. While we can run code in container nodes, the bulk of the behaviour is coded into the non-container. Here is a simplified hierarchy:

- Active
  - Attacking
    - Combo series
      - Attack 1
      - Attack 2
  - Defensive
    - Pursuit
    - Orbiting

When choosing between the attacking or defensive container we can for example determine this by the distance to the player.

While defensive, the enemy will either walk towards the player or orbit them depending on the distance between them. Upon entering the defensive hierarchy, the AI picks a random length of time from a range to stay defensive for:

```
defence_timer = rand_range(min_def_time, max_def_time)
```

Once the timer finishes, we transition to the attacking hierarchy. When attacking, we could assign attack combos and special attacks. We can make these being chosen either randomly or if certain conditions are met. For example initiating a lunge attack that is a special attack at medium or long range (special attack container, sibling of combo series, if we have time).

In the attack combo node when active it should choose a random number of attacks to do within a range:

```
attacks_to_do = rand_range(min_attacks, max_attacks)
```

We could choose which attack it starts on as the attack cycle can loop. On each node of the combo, we should determine which attack can follow the previous one, choosing randomly if more than one follow-up attack. It would be nice if any attack that ends with the sword on a certain side, can be followed up by an attack that starts with the sword on that same side the previous ended on.

To make the combat a "back and forth" exchange, the enemy should be able to parry when the player is attacking. We should check the player's `is_attacking` variable that we can retrieve from the animations and check if it is true. If it is true, then we can tell the enemy to parry. In Sekiro, enemies will only parry a certain number of attacks before countering with their own. When the enemy shifts from defence to offense it should be apparent by a more pronounced parry that is more dramatic with a different sound effect and look. We should set a random range of attacks to parry:

```
will_parry_amount = rand_range(min_parries, max_parries)
```

Every time the enemy parries an attack, we decrement this variable, and if it is `1` we transition to the end parry state, that is that dramatic parry mentioned before. After exiting this state, the enemy then immediately transitions into attacking.

Defensive nodes (Pursuit or Orbiting) and Attack nodes (Combos) should evaluate player states during each game tick. For instance:

- If `player.is_attacking == true`, transition to Parry.
- After executing an end parry, transition directly to the Combo series for counterattacks.

This setup should mean that when we attack the enemy, they will parry for a certain amount of times before firing back with their own attacks that we need to parry ourselves.

---

## Feedback & Feel

With this timing-based combat every action must have immediate, clear responses to make it readable and rewarding. Simple, consistent effects that reinforce timing windows and outcomes.

| Event                   | Visual                              | Audio                        |
| ----------------------- | ----------------------------------- | ---------------------------- |
| **Player Light Attack** | Sword slash                         | Whoosh                       |
| **Enemy Attack**        | Wind-up animation                   | Whoosh                       |
| **Block**               | Shield shimmer, no damage flash     | Dull clang, short block      |
| **Perfect Parry**       | Bright sparks                       | Loud satisfying clang + echo |
| **End Parry**           | More bright sparks                  | Different clang sound        |
| **Player Hit**          | Blood splatter                      | Meaty thud                   |
| **Enemy Hit**           | Metal particles                     | Robot sounds or metal thud   |
| **Player Death**        | Fade to black, death pose           | Scream??                     |
| **Enemy Death**         | Collapse animation, dissolve effect | Robot and metal sounds       |
| **Lock-On**             | Target highlight, camera rotation   | Nothing                      |

---

## Development order

1. Movement & camera
2. Enemies & lock-on
3. Attacks, hit detection
4. Block & parry
5. Enemy AI
6. Feedback & sound
7. Polish & bug fixing

---

## Success Criteria

The combat system is successful if:

- Perfect parries feel rewarding
- Combat is readable and fair
- A full duel can be completed without bugs
- The player understands why they won or lost

---

## Optional features if we have time

- Heavy attacks that knocks the player back
- Parry system with posture damage and breaking mechanics
- Posture system for enemies and players
