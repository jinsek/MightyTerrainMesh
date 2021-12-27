# MightyTerrainMesh
A Unity Plugin for Converting Terrain 2 Mesh with LOD & QaudTree infomation.

There are some introduction & result here :

https://zhuanlan.zhihu.com/p/64809281

And thanks for *parahunter*, I used his [triangle-net-for-unity](https://github.com/parahunter/triangle-net-for-unity) as the tessellation solution.

# Update At 27th Dec. 2021
 What I have done this update:
 
 * read the height data from terrain data directly, remove the physically sampling methods
 * changed the texture baking method, make the mesh baking runs faster and more stable. 
 * supports runtime virtual texture by converting terrain to data sets which can baking the "multi-layer blending material" into a simple material with only sampling 2 tertures at runtime.
 * converts the terrain details(grass) into data sets which can be rendered at runtime by unity jobs system.
 * The converted grass system supports wind, water floats and interaction with player.

Parts of the works have been done at Camel Games(https://www.camelgames.com/), where I worked there as a graphics progammer. Special thanks to Kai, who drived & supported this project to achieved this result.
