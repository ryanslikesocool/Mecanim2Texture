# Mecanim2Texture
An attempt at converting a Unity animation to a 2d texture for use with mesh animation
I do not plan to add a shader that deals with the texture any time soon.

## Usage
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
- Click "Create Animation Texture".  Depending on how many vertices your mesh has, it could take up to a few minutes.  With about 700 vertices it takes my MacBook Pro about 2-4 seconds to generate the file.
- Find the file in your base "Assets" directory.
- ???
- Profit
