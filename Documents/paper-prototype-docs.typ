#set page(
  width: 21cm,
  height: 29.7cm,
  margin: (x: 2cm, y: 2cm),
)

#set text(size: 10pt)

= One-Page game pitch --- Katana

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
      Open spot map
    ],
  ),

  figure(
    image("Images/Paper-prototype/open_spot_fight.jpg", width: 100%),
    caption: [
      Open spot fight
    ],
  )

)

#pagebreak()

== UI iterations
We have tested a bunch of different locations for the UI elements in game. We found out that players likes UI that feels natural and that are standardized in other games too.

