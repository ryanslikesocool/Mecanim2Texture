# Mecanim2Texture
An attempt at converting a Unity animation to a 2d texture for use with mesh animation

## Usage
### Animation Texture Tab
- Drag and drop the script into one of your project's Editor folders
- In the menu/Finder bar, select ifelse>Animation Texture Creator
- Dock the window so it's on top
- Drag and drop a gameobject/prefab into the field in the window
  - The following components are required for the script to work properly
    - Skinned Mesh Renderer
    - Animator (with Animator Controller with at least one clip)
- Set the capture framerate to whatever.  30 or 60 is recommended
- Scale the original animation vertices as needed, since the texture can only handle values from 0-1 for each component in a pixel.
- Review details
  - Frames to convert: the capture rate multiplied by the first animation clip's length in seconds
  - Pixels to fill: how many pixels in the final texture will be filled.  This is dependent on the Skinned Mesh Renderer's vertex count and the frames to convert
  - Resulting texture size: the square root of the pixels to fill rounded up to the next power of 2.  You could always make it a custom size but I figured that powers of two are easiest to work with
- Click "Create Animation Texture".  Depending on how many vertices your mesh has, it could take up to a few minutes.  With about 700 vertices it takes my computer about 1-2 seconds to generate the file.
- DO NOT CLOSE THE EDITOR WINDOW until the texture creation is complete.  Unity has to run the animation through a coroutine to sample each frame of the animation to get all of the vertices.  If you close the window, you stop the coroutine.
- Find the file in your base "Assets" directory.

### UV Map Tab
- THIS TAB IS NOT NECESSARY.  I had it in here for testing purposes, but I figure that it may be useful to someone so I'm leaving it in.
- Set the texture created in the previous tab in the texture slot
- Set a gameobject with a mesh to add/modify the UVs on
- Select which UV layer you want to set.  If you've already got UVs on that layer, you will see a warning
  - If the proper renderer is not present, the editor will not show the button to apply UVs.
- Select a renderer type for the gameobject.
  - Skinned will look for a Skinned Mesh Renderer
  - Normal will look for a Mesh Filter
- Click on the button to modify the UVs on the selected layer to line up with the pixels on the texture

## Notes
### Animation Texture
- The pixels on resulting animation textures are ordered Y-up to X-right
- To properly use an animation texture, you'll need to use a shader that supports vertex ID.  The shader provided in this repo works, but someone will likely come up with something better
### UV Map
- I recommend exporting the mesh as a new file (such as with Unity's FBX Exporter) once you've updated the map, as the UV will disappear when you close the editor

## Dependencies
Available in the Unity Package Manager
- Editor Coroutines
