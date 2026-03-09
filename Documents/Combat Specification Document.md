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

#### Combat duel flow

Enemy behaviour is a pressure exchange, not constant offense.

- Defensive and offensive turns alternate in short cycles
- Enemy parries some player attacks before taking initiative
- An end-parry feedback acts as a clear momentum shift into counteroffense
- Group pressure stays limited so 1v1 timing remains the core skill test

This gives combat a "back and forth" exchange instead of one-sided pressure.

---

## Design inspiration videos

- [State machine](https://www.youtube.com/watch?v=RmdtA7G7cAE)
- [Combat](https://www.youtube.com/watch?v=OLPJdg6Y9Yo)
- [Enemy AI](https://www.youtube.com/watch?v=2UjeZFzzfB0&t=876s)

---

## Core Feature Set

### Player Mechanics

| Feature       | Description                                                           |
| ------------- | --------------------------------------------------------------------- |
| Movement      | WASD + SHIFT to sprint, SPACE to jump                                 |
| Camera        | Third-person camera with lock-on, target switching, and auto-retarget |
| Light Attack  | Shared 5-step sword combo chain                                       |
| Block         | Guard input (tap or hold)                                             |
| Perfect Parry | Timed deflect during parry window                                     |
| Healing       | Limited bandages with cooldown                                        |
| Health        | Player can die if health reaches zero                                 |

### Enemy Mechanics

| Feature      | Description                                                               |
| ------------ | ------------------------------------------------------------------------- |
| Attacks      | Shared 5-step melee combo (same combo data source as the player)          |
| Telegraphs   | Wind-up -> slash -> recovery attack phases driven by animation events     |
| Parry        | Parry-focused defense with end-parry momentum shift, no gameplay blocking |
| Coordination | Attack-token system limits simultaneous attackers                         |
| Health       | Enemy can die if health reaches zero                                      |
| Tiers        | Normal, MiniBoss (Kensei), Boss (Ronin) with distinct combat profiles     |
| Dash Slash   | Boss gap-closer attack in Phase 2+ (forward dash)                         |

---

## Combat Rules & Systems

### Health System

- Health is a simple numeric value
- Health is reduced when attacks are not blocked or parried
- Health does **not passively regenerate**
- The player can restore health with limited bandages
- Bandages use a cooldown between heals and can be replenished through pickups

### Blocking & Parry System

#### Block

- Activated by guard input (right click), using tap or hold
- Holding keeps block active continuously
- Tapping opens a short block linger window so block/parry can register without holding
- Dull sound effect
- Reduces damage dealt (50% reduction)

**Block linger:**

- Why it matters: It keeps combat outcomes consistent with player feel, so quick guard taps are not punished by frame-perfect release timing.
- How it works: On guard release, blocking remains active for a short linger duration before turning off. Animation still follows live input (tap/hold), while linger only affects combat damage resolution.

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

### Combat Movement Feedback

To make outcomes readable in moment-to-moment play, combat applies short horizontal motion feedback in addition to damage/audio/VFX outcomes.

#### Defender pushback on defended hits

- Triggered on `Blocked` and `Parried` outcomes
- Applies to the defender (player or enemy)
- Push direction is based on attacker-to-defender direction on the XZ plane
- If attacker direction is unavailable, fallback uses defender backward direction
- `Blocked` pushback is stronger than `Parried` pushback
- End-parry uses the same pushback as parry, with distinct optional VFX/SFX

This creates a clear physical response on successful defense and helps communicate outcome strength.

#### Attacker lunge on basic attack slash

- Triggered on attack `Slash` phase
- Applies a short forward horizontal impulse to the attacker
- Uses current attack facing direction on the XZ plane
- Intended as a slight commitment step to improve impact and spacing feel
- Applied to both player and enemy attack phases through shared attack feedback hooks

This keeps attack animations feeling connected to movement.

#### Impulse tuning

- Impulses use smooth horizontal velocity decay over their duration, then clear horizontal velocity
- Enemy NavMesh movement is paused during active impulses and resumed afterward to preserve smooth lunge/pushback motion

---

## Animation & Character Behavior Strategy

### Animation Strategy

> Animation quality is less important than **clear timing**.

Attack windows and eligibility checks are tied to animation events configured in Unity's Animator system. These events:

- Trigger hitboxes during specific active frames.
- Signal transitions between states like idle, wind-up, and recovery.
  Implementation relies on Animator parameters and conditions, which should align with the state machine hierarchy to maintain timing consistency.

### Enemy Specification

#### Enemy States & Animations

- **Idle:** Faces target, no attack/parry window
- **Defensive Turn:**
  - **Pursuit:** Move toward target until engagement distance
  - **Orbit:** Circle target at orbit radius while maintaining facing
  - **Threat-Driven Parry Window:** Opens while the target is attacking within parry trigger range, with short threat memory
  - **Parry Reactions:** Directional parry reaction clips
- **Attack Turn:**
  - Plays a sampled length of the shared combo attack chain
  - Uses wind-up -> slash -> recovery attack phases from animation events
- **Dead:** Combat flags close, AI/nav stop, ragdoll/VFX/SFX play, and the root enemy object is destroyed

### Player Specification

#### Player States & Animations

- **Idle:** Reset to idle stance
- **Walking:** Slow movement
- **Sprinting:** Fast movement
- **Jumping / Airborne:** Jump start -> air loop -> landing
- **Attacking:** (attack combos with phases)
  - **Slash 1:** Wind-Up → Slash → Slow-down
  - **Slash 2:** Wind-Up → Slash → Slow-down
  - **Slash 3:** Wind-Up → Slash → Slow-down
  - **Slash 4:** Wind-Up → Slash → Slow-down
  - **Slash 5:** Wind-Up → Slash → Slow-down
- **Dead:** Falls to the ground, game over

#### Player Attack Chain

The player attack system implements a combo chain using a state machine approach, extending the base character controller.

When the attack input is pressed while in Idle or certain movement states, transition to Slash 1. Each slash state follows a phased animation: Wind-Up (telegraph) → Slash (hit frames) → Slow-down (recovery).

- If attack input is detected during the Slow-down phase of Slash 1, transition immediately to Slash 2.
- Similarly for Slash 2 → Slash 3, Slash 3 → Slash 4, and Slash 4 → Slash 5.
- After Slash 5's Slow-down, or if no input is received, transition back to Idle.
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

Interaction triggers like hit, block, and parry are coordinated through animation events, combat flags, and state changes. For example, a hitbox/hurtbox interaction only becomes valid while the relevant attack phase has opened the hitbox and the combat flags allow damage resolution.

Example Integration with Combat Logic:

- During the player's attack, the state machine handles transitions like:
  (Idle → Slash Wind-Up → Slash Active → Recovery → Idle).
- Reaction triggers, such as block, parry, or hit states, run within this modular architecture, enabling shared logic for both player and enemy state machines.

### Camera Locking System

When toggling the camera lock, the system searches for enemies inside `lockOnRange`, filters to visible on-screen targets with line of sight, then scores them using:

- A top-of-screen priority above the player
- Distance to the player
- Distance from the screen center

The best-scoring valid target becomes the lock target and the camera rig rotates toward that target point.

To switch between targets, the system reads the current look input vector and compares it against other visible targets in view space. The target with the best directional match is selected. Active locks can break on death, excessive distance, or sustained occlusion, and can auto-retarget to another valid enemy.

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

When a hitbox intersects with a hurtbox, the hitbox builds an `AttackHitInfo` payload and calls `ReceiveHit()` on the hurtbox owner. A hit is only valid if:

- The hitbox is currently active
- The hitbox owner is currently attacking
- The intersecting hitbox and hurtbox do not share the same owner
- The hurtbox owner is currently vulnerable
- The hurtbox has not already been hit during the current attack

Attack start/stop is still driven by animation events. Wind-up begins the attack, slash frames enable hit detection, and recovery ends the attack and clears the hit ignore set.

Damage outcomes are resolved through `CombatFlagsComponent` and `DamageResolver`:

- Parry window active -> `Parried`
- Blocking active -> `Blocked`
- Otherwise -> `FullHit`

Outcome feedback hooks then drive VFX, SFX, pushback, and defense reaction animations for both player and enemy combatants.

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

**Post-defense / block-release behaviour:**

- Gameplay blocking can linger briefly after guard release even while the block exit animation starts
- The block-to-idle exit animation can be interrupted by attacking
- After a successful defense reaction, movement remains constrained until the reaction ends, while attacking unlocks after a short delay

### Enemy Tiers

Enemies are categorised into tiers that determine their combat profile and visual identity. All tiers share the same model and animation set.

| Tier     | Name   | HP  | Role                         |
| -------- | ------ | --- | ---------------------------- |
| Normal   | —      | 20  | Standard wave enemy          |
| MiniBoss | Kensei | 60  | Tougher enemy in later waves |
| Boss     | Ronin  | 400 | Solo 1v1 duel                |

#### Kensei (Mini-Boss)

- Appears in wave gameplay alongside normal enemies, replacing normal spawns from a configurable start wave onward
- Kensei count scales
- Visually differentiated: large scale, dark chrome material with red emission, glowing red eyes with trails, red weapon slash trail
- More aggressive combat profile: shorter defense windows, faster counters, higher minimum attack chain
- Respects the attack token system and does not bypass group pressure rules

#### Ronin (Boss)

- Solo 1v1 (not spawned by the wave system)
- Visually differentiated: larger scale, blackened steel material with dark red emission, glowing red eyes with trails, always-on red weapon slash trail
- **Always-on VFX:** Ground scorch glow with rising embers beneath the boss (active in all phases)
- Three combat phases driven by HP thresholds, managed by `BossPhaseController`:

| Phase | HP Range | Behaviour                                                          | VFX                                          |
| ----- | -------- | ------------------------------------------------------------------ | -------------------------------------------- |
| 1     | 100%–60% | Short combos (2–3), generous parry windows, long defense pauses    | Ground scorch + embers only                  |
| 2     | 60%–30%  | Full combos (3–5), fast counters, introduces dash slash gap-closer | + Red sparks emitting from body              |
| 3     | 30%–0%   | Near-max combos (4–5), instant counters, minimal defense pauses    | + Massive electric sparks emitting from body |

- **Phase Transitions:** Each phase shift triggers a burst VFX (sparks, electric burst, flash) spawned at the boss spine, managed by `BossPhaseVfxController`
- **Dash Slash:** A gap-closing attack available in Phase 2+. When the player is beyond normal attack range, the boss dashes forward with an attack. Controlled by profile fields (`dashAttackEnabled`, min/max range, duration, cooldown`) and implemented as a dedicated state machine state (`EnemyDashAttackState`). Travel distance is derived from current target spacing when the dash begins
- Phase transitions swap the combat profile

#### Combat Profiles

Enemy behaviour is data-driven through `EnemyCombatProfile` ScriptableObject assets. Each profile controls:

- Attack chain length (min/max)
- Parries before counter (min/max)
- Defense duration (min/max)
- Counter prep delay
- Spacing and support movement (engage range, orbit radius, attack range, support orbit tuning)
- Token cooldowns, priority handoff support, and same-attacker reentry delays
- Dash attack parameters (boss only)

### Enemy Behaviour

Enemy AI uses explicit turn states (`Idle -> Defense Turn -> Attack Turn -> Defense Turn ...`) with dead-state override.

#### Defensive turn

- On entering defense, enemy samples:
  - A defense duration from profile min/max
  - A required number of successful parries before counter from profile min/max
- During defense, enemy movement mode is distance-based:
  - Pursue while outside engage range
  - Orbit while inside engage range
- Defensive turn keeps the enemy conditionally parry-ready while threat conditions are met:
  - The target is actively attacking
  - The target is within `parryTriggerRange`
  - The recent threat memory window is still active
- Enemy defense is parry-only, no blocking.
- Each successful parry increments a counter. On the final required parry:
  - End-parry feedback is queued (distinct SFX/VFX variant)
  - A configurable `counterPrepDelay` timer starts
  - Enemy transitions into attack turn when that timer is ready

#### Attack turn

- On entering attack turn, enemy closes the parry window and does not parry for the whole attack turn.
- Enemy acquires a global attack token before starting attacks so group pressure stays controlled (one active attacker at a time).
- Enemy samples a combo chain length from profile min/max.
- Range is required only to start the combo chain.
- Once the first attack starts, the combo is committed and enemy finishes the sampled chain even if target distance changes.
- After chain completion, enemy transitions back to defensive turn.

#### Dash attack (Boss only)

- Available when `dashAttackEnabled` is true in the active combat profile (Ronin Phase 2+)
- Evaluated during defensive turn: if the player is between `dashAttackMinRange` and `dashAttackMaxRange`, the boss acquires the attack token and transitions to `EnemyDashAttackState`
- The boss dashes toward the player using a horizontal impulse, fires an attack animation, then returns to defensive turn
- Subject to a cooldown between uses (`dashAttackCooldown`)

---

## Feedback & Feel

With this timing-based combat every action must have immediate, clear responses to make it readable and rewarding. Simple, consistent effects that reinforce timing windows and outcomes.

| Event             | Visual                                           | Audio                    |
| ----------------- | ------------------------------------------------ | ------------------------ |
| **Light Attack**  | Sword slash trail, short forward lunge           | Whoosh                   |
| **Enemy Attack**  | Wind-up animation, short forward lunge           | Whoosh                   |
| **Block**         | Sparks, pushback                                 | Dull clang / block       |
| **Perfect Parry** | Bright sparks, pushback                          | Loud satisfying clang    |
| **End Parry**     | Distinct parry sparks, pushback                  | Distinct end-parry clang |
| **Player Hit**    | Hit VFX, damage vignette                         | Hit impact               |
| **Enemy Hit**     | Hit VFX, health bar response                     | Hit impact               |
| **Player Death**  | Ragdoll, death VFX, death overlay, fade to black | Death SFX                |
| **Enemy Death**   | Ragdoll, death VFX, optional dissolve            | Death SFX                |
| **Boss Death**    | Kill overlay, screen fade                        | Defeat SFX               |
| **Lock-On**       | Camera rotation, lock-on indicator               | Nothing                  |

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
- Posture system for enemies and players
  - Parry system with posture damage and stance breaking mechanics
