#set page(
  width: 21cm,
  height: 29.7cm,
  margin: (x: 2cm, y: 2cm),
)

#set text(size: 10pt)

= Paper Prototype Document --- Katana

== Raid (Core gameplay)
We play tested our main gameplay loop: combat, and found some good findings. First we tested to play on a map that had many narrow street (See figures 1-3).
We thought it was a bit to tight streets on the first map we did. When we play tested we found out that if many enemies attacked the player would like to have more of an open space.

#grid(

  columns: 3,
  column-gutter: 3pt,


figure(
  image("Images/Paper-prototype/narrow_streets_fight_1.jpg", width: 100%),
  caption: [
    Narrow streets fight 1
  ],
),

figure(
  image("Images/Paper-prototype/narrow_streets_fight_2.jpg", width: 100%),
  caption: [
    Narrow streets fight 2
  ],
),

figure(
  image("Images/Paper-prototype/narrow_streets_map.jpg", width: 100%),
  caption: [
    Narrow streets map
  ],
)
)

#v(20pt)

Here we changed the map to include an open spot in the center (see figure 4). This resulted in the player felling that there was much more space to fight one or more enemies. (figure 5)

#v(20pt)

#grid(
  columns: 2,
  column-gutter: 3pt,
  

  figure(
    image("Images/Paper-prototype/open_spot_map.jpg", width: 100%),
    caption: [
      Open spot city map
    ],
  ),

  figure(
    image("Images/Paper-prototype/open_spot_fight.jpg", width: 100%),
    caption: [
      Open spot city fight
    ],
  )

)

#pagebreak()

== UI iterations
We have tested a bunch of different locations for the UI elements in game. We found out that players likes UI that feels natural and that are standardized in other games too.

#v(30pt)

#grid(

  columns: 2,
  column-gutter: 20pt,
  row-gutter: 30pt,

text("Here the user got a feel for the user interface during the general gameplay (raids). The user suggested moving the player health bar to the top left of the screen instead of the bottom left corner, since the user felt it was more standard, which we agreed on. Though if we would implement the posture system later, this would have to be changed since it introduces new vital UI elements. Another suggestion by the user was the visibility of the health bars of the enemies, in which they suggested that they should be hidden until they took damage. Which was something we did not think about and was a good idea to reduce visual clutter."),

figure(
  image("Images/Paper-prototype/paper-prototype-3.jpg", width: 100%),
  caption: [
    User testing city raid
  ],
),

figure(
  image("Images/Paper-prototype/paper-prototype-2.jpg", width: 100%),
  caption: [
    User testing boss scene UI
  ],
),

text("Since the user wanted to move the player health UI to the top left corner, the boss health bar was moved to the bottom left of the screen. In hindsight we think that this UI placement could be improved upon, since the player item and player health elements are too far apart from each other and not more \"grouped\"."),

text("For the raid map view, the user thought that it was a good idea for the different destinations to use kilometers as the \"currency\" to travel to that place rather than dollars or an arbitrary number. With the kilometers as the unit, the user felt that it gave the map more context and perspective."),

figure(
  image("Images/Paper-prototype/paper-prototype-4.jpg", width: 100%),
  caption: [
    User testing raid map view
  ],
)
)

#pagebreak()

== Game Scenes
After a lot of thinking and testing we have decided on all scenes in the game. Keep in mind that this is not for the MVP. This is for the final state of the game and our vision for what we want it to be.

=== Start screen
Here you will be able to load into existing saves, as well as start new ones. Now we press “New game”  to start a new one.
#figure(
  image("Images/Paper-prototype/start_screen.jpg", width: 100%),
  caption: [
    Start screen
  ],
)

#pagebreak()

=== Back story "movie"
Here we want a backstory cinematic. This is going to go through the story of the main character and let the player get in to the mood. This should only be showed when a new game is started, not when you load in to a save.
#figure(
  image("Images/Paper-prototype/back_story_movie.jpg", width: 100%),
  caption: [
    Back story movie
  ],
)

#pagebreak()

=== Raid combat
 This is were the player is going to be spending most of its time. The city raid scene where the player is going to face multiple waves of enemies. The player is firstly put here with a tutorial which goes through the basic game mechanics.

#figure(
  image("Images/Paper-prototype/combat_city.jpg", width: 100%),
  caption: [
    City raid, combat
  ],
)

#pagebreak()

=== Van scene
After the tutorial scene we get put to the "van view", where we can check our inventory, upgrades, etc by hovering different parts of the car and character. Now we press the RAID button on the right, this sends us to the raid map screen.

#figure(
  image("Images/Paper-prototype/van_scene.jpg", width: 100%),
  caption: [
    Van scene
  ],
)

#pagebreak()

=== Raid map
This is the raid map. Here we can choose where to raid. The main gameplay loop will be raiding the City multiple times in order to gather upgrades and become more powerful, and then when you are ready, raid the Temple for the final boss.
#figure(
  image("Images/Paper-prototype/raid_map.jpg", width: 100%),
  caption: [
    Raid map
  ],
)

#pagebreak()

=== Temple raid (boss fight)
This is where we fight the final boss; if we lose, all our inventory items are lost. If we win, we win the game. So the stakes are high, the player need to have geared up before facing it.
#figure(
  image("Images/Paper-prototype/boss_fight.jpg", width: 100%),
  caption: [
    Boss fight
  ],
)