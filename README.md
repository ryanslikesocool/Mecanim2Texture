# Mecanim2Texture
Bake skinned mesh animations to 2D textures!

## Usage
### Texture Creator
- Drag and drop the script into your project's Editor folder
- In the top bar, select Window/ifelse/Mecanim2Texture
- Create a container object for each rig you want to export
- Add rigged gameobjects as childs of the container
- Drag and drop the container into the `Animation Rig` field
- Select the animator you want to apply to the rigs in the container
- The following components are required for the script to work properly.  If they are not preset, errors will appear at the bottom of the window and you will not be able to continue
    - Skinned Mesh Renderer
    - Animator (with an Animator Controller with at least one clip)
- Set the `Color Mode`
    - `LDR` exports as a `.png` and clamps colors in a 0 - 1 range, as well as rounds colors to the nearest 1/255th
    - `HDR` (highly recommended) exports as a `.exr` and does not clamp or round colors* (*it's possible that it does, but the range is extended far beyond LDR)
- Set `Bake All` if you want to bake all animations into a Texture2DArray.  Jump to Bake All if you enable this.
- Set the `FPS Capture` based on how you exported the animation from your tool of choice.  30 or 60 is recommended for animation export
- Select the `Clip to Bake`
- Set the `Bake Scale` to how large you want your baked animation to be, compared to the original size
- Set `Min Capture Frame` and `Max Capture Frame` to determine the range of frames to be baked into the texture
- Review details
    - `Animations`: How many animations will be baked.  This is always 1 unless you bake all animations
    - Frames to bake: How many frames will be baked, based on `FPS Capture`, `Min Capture Frame`, `Max Capture Frame`, and the duration of `Clip to Bake`
    - Pixels to fill: How many pixels in the final texture will be filled, based on `Frames to bake` and the mesh's vertex count
    - Result texture size: The minimum power of 2 size that can fit all of the `Pixels to fill`.
    - Estimated bake time: How long the bake is estimated to take
- Click "Bake Animation".  Depending on how long the animation clip is, it could take up to a few minutes.  With about 700 vertices and a clip under 1 second it takes my computer about 1-2 seconds to generate the file
- Do not close the editor window until the texture creation is complete.  Unity has to run the animation through a coroutine to sample each frame of the animation to get all of the vertices.  If you close the window, you stop the coroutine.
#### Bake All
- If you've enabled `Bake All`, the tool will bake all textures into a Texture2DArray asset.
- You will still need to set `FPS Capture` and `Bake Scale`, both detailed above.
- Review details
    - `Result texture size` now has a 3d dimensions, relating to how many elements the Texture2DArray will have.  No matter how small an individual texture can be normally, the result will be the maximum size required.

### UV Mapper
- Select the `Mesh` to copy with the new UVs
- Select which `UV Layer` you want to set.  If you've already got UVs on that layer, a warning will appear
- Set a `Mesh Scale` to scale the mesh.  It's recommended to set this, even if you're scaling the mesh via shader.  Setting it properly will reduce the chance of render bounds being incorrect.
- Click on the button to clone the mesh and apply the UVs on the selected layer

### Shader Graph Custom Nodes (Mecanim2TexShaderGraphNodes.hlsl)
- `AnimationTexture`
    - `float TimeOffset`: How many seconds the animation will be offset by
    - `float4 VertexIDUV`: The UVs for whichever channel you selected when you baked the mesh in UV Mapper
    - `float ColorMode`: LDR textures should use 0, HDR textures should use 1
    - `float FramesPerSecond`: The FPS you baked the texture at
    - `float AnimationFrames`: How many frames are in the baked animation
    - `Texture2D TexIn`: The baked animation texture
    - `float2 TexSize`: The texture's pixel dimensions
    - `float Scaler`: The inverse of what you want to scale the mesh by.  0.5 doubles the mesh size, 2 halves it
    - `float VertexCount`: The vertex count of the mesh
    - `SamplerState TexSampler`: The sampler state used to sample the texture.  Filtering should be Point
    - `out float3 PosOut`: The resulting vertex positions
- `AnimationTexturev2`
    - `Texture2DArray textures`: The Texture2DArray created when `Bake All` is enabled in Texture Creator
    - `float3 vertexPositions`: The original vertex positions
    - `float time`: The current time.  It's not calculated in the method, resulting in more customizability
    - `float4 vertexIdUv`: The UVs for whichever channel you selected when you baked the mesh in UV Mapper
    - `int vertexCount`: The vertex count of the mesh
    - `int framesPerSecond`: The FPS you baked the texture at
    - `float scaler`: The inverse of what you want to scale the mesh by.  0.5 doubles the mesh size, 2 halves it
    - `int textureSize`: The pixel dimensions of the Texture2DArray.  Only one is necessary, since the x and y values of the texture are always the same.
    - `float4 lerper`: With the provided configuration, this lerps between texture layers.  x, y, and z lerp texture layers, while w lerps from texture layers to the original mesh
    - `int4 index`: The indices of the texture layers you want to sample
    - `int4 frames`: The frame count of each texture layer animation you want to sample
    - `SamplerState samplerState`: The sampler state the sample the textures with.  The Filter should be set to Point
    - `out float3 positionOut`: The resulting vertex positions

## Notes
- The pixels on resulting animation textures are ordered Y bottom to top, and X left to right
- Custom Shader Graph nodes are supplied in this repository for your convenience.  They are meant for use with the mesh generated by the UV Mapper
- AnimationTexturev2 can be modified to support however many animation layers you'd like to sample at once
- Credits are appreciated, but not necessary

## Dependencies
Available in the Unity Package Manager
- Editor Coroutines (Required)
