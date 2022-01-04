# MightyTerrainMesh
A Unity Plugin for Converting Terrain 2 Mesh with LOD & QuadTree infomation.

There are some introduction & result here :

https://zhuanlan.zhihu.com/p/64809281

And thanks for *parahunter*, I used his [triangle-net-for-unity](https://github.com/parahunter/triangle-net-for-unity) as the tessellation solution.

# Update At 27th Dec. 2021
 What I have done in this update:
 
 * read the height data from terrain data directly, remove the physically sampling methods.
 * changed the texture baking method, make the mesh baking runs faster and more stable. 
 * supports runtime virtual texture by converting terrain to data sets which can baking the "multi-layer blending material" into a simple material with only sampling 2 tertures at runtime.
 * converts the terrain details(grass) into data sets which can be rendered at runtime by unity jobs system.
 * The converted grass system supports wind, water floats and interaction with player.

Parts of the works have been done at Camel Games(https://www.camelgames.com/), where I worked there as a graphics progammer. Special thanks to Kai, who drived & supported this project to achieved this result.

# How to Use the mesh creator
* click MightTerrainMesh->MeshCreator open the mesh editor interface
* Drag the terrain you want to convert to the Convert Target
* Fill in the parameters as the screen shot, you will get a prefab which cut this terrain into a 4 by 4 total 16 meshes, each has 2 2048 textures including albeto + ao + normal + metallic + roughness informations.
![mesh_editor](https://user-images.githubusercontent.com/4982625/147907698-939fdc24-a113-42ca-b084-5e823e67768f.png)
* after the converting, the mesh, the texture are all yours, you can editing as you want, but pls remember that, if you need a comparable visual look by staic meshes and textures, the amount of the resources gonna be huge.

# How to Use the data creator
* click MightTerrainMesh->DataCreator open the mesh editor interface
* Drag the terrain you want to convert to the Convert Target
* Fill the parameters as the screen shot, then you will get the runtime virtual texture data set.
* watch out the data pack parameter, which pack 64 mesh data into one *.bytes data, cause we are spliting the terrain into 64*64 patch, which may crash your file system if we dont do any packing policy.
* ![data_editor](https://user-images.githubusercontent.com/4982625/147909008-482a3a21-ad5a-427d-aba0-a4f8affb4686.png)
* after baking all the terrains, I put then into the ArtRes/BakedData, and put the meshdata into Resources folder, since they will be loaded at runtime.
* pls check the VTDemo Scene, all the setting are pretty readable.
* watch out the VTCreator Game Object, which is the virtual texture generator, the Tex Quality setting is the one you may concern the most, 'half' can give us even better visual result then unity terrain, since its mipmap sucks, 'quater' means render the virtual texture as 1/4 resolusion, which looks also promising and can bring you 60 fps on medium end mobile device.
have fun!
