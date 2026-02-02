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

text("Here the user got a feel for the user interface during the general gameplay (raids). The user suggested moving the player health bar to the top left of the screen instead of the bottom left corner, since the user felt it was more standard, which we agreed on. Though if we would implement the posture system later, this would have to be changed since it introduces new vital UI elements. Another suggestion by the user was the visibility of the health bars, in which they suggested that they should be hidden until they engage in combat, which was something we did not think about and was a good idea to reduce visual clutter."),

figure(
  image("Images/Paper-prototype/paper-prototype-2.jpg", width: 100%),
  caption: [
    Narrow streets fight 1
  ],
),

figure(
  image("Images/Paper-prototype/paper-prototype-3.jpg", width: 100%),
  caption: [
    Narrow streets fight 2
  ],
),

text("Since the user wanted to move the player health UI to the top left corner, the boss health bar was moved to the bottom left of the screen. In hindsight we think that this UI placement could be improved upon, since the player item and player health elements are too far apart from each other and not more \"grouped\"."),

text("For the map view, the user thought that it was a good idea for the different destinations to use kilometers as the \"currency\" to travel to that place rather than dollars or an arbitrary number. With the kilometers as the unit, the user felt that it gave the map more context and perspective."),

figure(
  image("Images/Paper-prototype/paper-prototype-4.jpg", width: 100%),
  caption: [
    Narrow streets map
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
Here we want a backstory cinematic. This is going to go through the story of the main character and let the player get in to the mood.
#figure(
  image("Images/Paper-prototype/back_story_movie.jpg", width: 100%),
  caption: [
    Back story
  ],
)

#pagebreak()

=== Raid combat
 Here we get put into a tutorial scene after the backstory. After we finish the tutorial, we get to the main inventory/out of raid scene (image 4). The following times we go to this scene, it will be treated like a Main raid scene.

#figure(
  image("Images/Paper-prototype/combat_city.jpg", width: 100%),
  caption: [
    City raid, combat
  ],
)

#pagebreak()

=== Van scene
After the tutorial scene we get put to the main out of raid scene, where we can check our inventory, upgrades, etc by hovering different parts of the car and character. Now we press the RAID button on the right, this sends us to the map screen (image 5).

#figure(
  image("Images/Paper-prototype/van_scene.jpg", width: 100%),
  caption: [
    Van scene
  ],
)

#pagebreak()

=== Raid map
Now we get shown the “map” scene. Here we can choose where to raid. The main gameplay loop will be raiding the City multiple times in order to gather upgrades and become more powerful (images 5 -> 3 -> 4 -> 5), and then when you are ready, raid the Temple for the final boss (image 6). We now raid the temple
#figure(
  image("Images/Paper-prototype/raid_map.jpg", width: 100%),
  caption: [
    Raid map
  ],
)

#pagebreak()

=== Boss fight
This is where we fight the final boss; if we lose, all our equipped items are lost. If we win, we win the game.
#figure(
  image("Images/Paper-prototype/boss_fight.jpg", width: 100%),
  caption: [
    Boss fight
  ],
)