# InfiniteVoxelWork
A voxel workflow integrating Minetest for editing and MagicaVoxel for visualization, with a 256 custom game mode pallette.

**Notes**

* (issue) Currently ExportRegion's output axis is not matching expected MagicaVoxel Viewer orientation - however this concerns byte layout so it's requires a bit thinking to fix it.
* (todo, issue) I think the alpha channel for `.xraw` is not functional?
* (todo, experiment) MagicaVoxel max render range is 2048^2, and very likely MagicaVoxel Viewer won't support advanced node properties - try on Houdini and what kind of rendering setup it can produce, and how much larger it can render.
* (todo, idea) Maybe some sort of Sticher that can combine various pieces together (to overcome export generated block range limit) - in this case we definitely need to keep track of `Pos` of exported blocks and use a post-processing to combine those blocks (and sort them). ¶However, explore some real art with our current capabilities - the goal shouldn't lie in the tool, but the content.
* (specification) Puretest should have 255 (1 reserved for empty) pure color blocks - along with 255 HHlaf and 255 VHalf blocks (and maybe 255 Stairs, and 255 straight stairs) - those later ones (outside the 255 pure color ones) are entirely for in-game aethetics purpose because during export they will become whole blocks. Apparently to write such a mod we will generate the code instead of hand-write it.

# Finite Simulation Theory

**Notes**

* (todo) Several things to change from Model 1 (as Model 2): 1) Evaluation must happen as center at cell; 2) Do not use configuration based pattern, use rule based pattern; 3) A cell should not be just On/Off - it should store values, and it should store not just one value, but really fields (i.e. vectors) of values to represent different quantities; 4) For the moment those are fields of interest: electricity/photon, gravity/graviton; 5) We must pick a simulation scale to start generalizting meaningful physical rules, in this case let's pick something around 100nm (max dimension), to mimic behavior of nanomachines.