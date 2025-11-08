using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VoxelTechDemo;
public static class VoxelRenderer{
    private static GraphicsDevice graphicsDevice;
    public static readonly Dictionary<int,Vector2[]> TextureDictionary = new Blocks().TextureDictionary;
    
    //ChunkSize needs to be a power of 2. Works up to 64 (YShift = 6)
    public const int YShift = 6;
    public const int ZShift = YShift * 2;
    public const int ChunkSize = 1 << YShift;
    public const int ChunkSizeSquared = ChunkSize*ChunkSize;
    public const int ChunkSizeCubed = ChunkSize*ChunkSize*ChunkSize;
    public static IndexBuffer indexBuffer;
    private static VertexBuffer faceBuffer;
    private static VertexBuffer spriteBuffer;
    private static VertexBuffer frameBuffer;
    private static VertexBuffer previewBuffer;
    public static void InitializeVoxelRenderer(GraphicsDevice _graphicsDevice){
        graphicsDevice=_graphicsDevice;
        
        indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, 12, BufferUsage.WriteOnly);
        indexBuffer.SetData((short[])[0,1,2,1,3,2,4,5,6,5,7,6]);

        VertexPositionTexture[] faceVertices = [
            new(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(0, 0)),
            new(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(0, 1f / 16f)),
            new(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(1f / 16f, 0)),
            new(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1f / 16f, 1f / 16f)),
        ];
        faceBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionTexture), faceVertices.Length, BufferUsage.WriteOnly);
        faceBuffer.SetData(faceVertices);
        VertexPositionTexture[] spriteVertices = [
            new(new Vector3(0,0,0), new Vector2(1f/16f,1f/16f)),
            new(new Vector3(1,0,1), new Vector2(0,1f/16f)),
            new(new Vector3(0,1,0), new Vector2(1f/16f,0)),
            new(new Vector3(1,1,1), new Vector2(0,0)),
            
            new(new Vector3(1,0,0), new Vector2(1f/16f,1f/16f)),
            new(new Vector3(0,0,1), new Vector2(0,1f/16f)),
            new(new Vector3(1,1,0), new Vector2(1f/16f,0)),
            new(new Vector3(0,1,1), new Vector2(0,0)),
        ];
        spriteBuffer = new(graphicsDevice, typeof(VertexPositionTexture),  spriteVertices.Length, BufferUsage.WriteOnly);
        spriteBuffer.SetData(spriteVertices);
        
        // cube frame setup
        BlockFaceInstance[] cubeVertices = new BlockFaceInstance[6];
        for(int i=0;i<6;i++){
            cubeVertices[i] = new BlockFaceInstance(new Vector3(0.5f - 0.00125f,0.5f - 0.00125f,0.5f - 0.00125f),
                TextureDictionary[0][i], new Vector2(i,0), Color.White);
        }
        frameBuffer = new VertexBuffer(graphicsDevice,typeof(BlockFaceInstance), 6, BufferUsage.WriteOnly);
        frameBuffer.SetData(cubeVertices);
        previewBuffer = new VertexBuffer(graphicsDevice,typeof(BlockFaceInstance), 3, BufferUsage.WriteOnly);
        
        updateCloudBuffer();
    }
    public static void GenerateChunkMesh(Chunk chunk){
        // Check and assign chunk block arrays
        (int cx, int cy, int cz) = chunk.coordinates;
        byte[] blocks = chunk.blocks;
        byte[] blocksNorth = chunk.world.WorldMap.TryGetValue((cx+1,cy,cz), out Chunk chunkNorth) ? chunkNorth.blocks : null;
        byte[] blocksSouth = chunk.world.WorldMap.TryGetValue((cx-1,cy,cz), out Chunk chunkSouth) ? chunkSouth.blocks : null;
        byte[] blocksWest = chunk.world.WorldMap.TryGetValue((cx,cy,cz+1), out Chunk chunkWest) ? chunkWest.blocks : null;
        byte[] blocksEast = chunk.world.WorldMap.TryGetValue((cx,cy,cz-1), out Chunk chunkEast) ? chunkEast.blocks : null;
        byte[] blocksUp = chunk.world.WorldMap.TryGetValue((cx,cy+1,cz), out Chunk chunkUp) ? chunkUp.blocks : null;
        byte[] blocksDown = chunk.world.WorldMap.TryGetValue((cx,cy-1,cz), out Chunk chunkDown) ? chunkDown.blocks : null;
        int currentChunkY = cy*ChunkSize;

        // Check every block within a chunk if it needs to be added to the mesh
        List<BlockFaceInstance> solidInstances = [];
        List<BlockFaceInstance> fluidInstances = [];
        List<BlockFaceInstance> spriteInstances = [];
        int currentBlock = -1;
        for(int z=0;z<ChunkSize;z++){
            for(int y=0;y<ChunkSize;y++){
                for(int x=0;x<ChunkSize;x++){
                    currentBlock++;
                    byte blockId = blocks[currentBlock];
                    if (blockId == 0) continue;
                    Vector2[] textureCoordinates = TextureDictionary[blockId];
                    
                    if (Blocks.IsFoliage(blockId)) {
                        spriteInstances.Add(new BlockFaceInstance(
                            new Vector3(x, y + currentChunkY, z),
                            textureCoordinates[0], new Vector2(2,0), 
                            Light.ConvertLightValues(chunk.blockLightValues[currentBlock])));
                        continue;
                    }

                    uint faces = 0;
                    //face x+
                    if (IsVisible(blockId, x != ChunkSize - 1 ? blocks[currentBlock + 1] : blocksNorth != null ? blocksNorth[currentBlock - (ChunkSize - 1)] : (byte)0)) {
                        faces |= 1;
                    }
                    // Face x-
                    if (IsVisible(blockId, x != 0 ? blocks[currentBlock - 1] : blocksSouth != null ? blocksSouth[currentBlock + (ChunkSize - 1)] : (byte)0)) {
                        faces |= 2;
                    }
                    // Face y+
                    if (IsVisible(blockId, y != ChunkSize - 1 ? blocks[currentBlock + ChunkSize] : blocksUp != null ? blocksUp[currentBlock - (ChunkSizeSquared - ChunkSize)] : (byte)0)) {
                        faces |= 4;
                    }
                    // Face y-
                    if (IsVisible(blockId, y != 0 ? blocks[currentBlock - ChunkSize] : blocksDown != null ? blocksDown[currentBlock + (ChunkSizeSquared - ChunkSize)] : (byte)0)) {
                        faces |= 8;
                    }
                    // Face z+
                    if (IsVisible(blockId, z != ChunkSize - 1 ? blocks[currentBlock + ChunkSizeSquared] : blocksWest != null ? blocksWest[currentBlock - (ChunkSizeCubed - ChunkSizeSquared)] : (byte)0)) {
                        faces |= 16;
                    }
                    // Face z-
                    if (IsVisible(blockId, z != 0 ? blocks[currentBlock - ChunkSizeSquared] : blocksEast != null ? blocksEast[currentBlock + (ChunkSizeCubed - ChunkSizeSquared)] : (byte)0)) {
                        faces |= 32;
                    }
                    if (faces == 0) continue;

                    byte blockRotation = 0;
                    int[] faceRotation = Blocks.NoRotation;
                    if (Blocks.CanRotate(blockId)) {
                        chunk.BlockStates.TryGetValue(currentBlock, out blockRotation);
                        switch (blockRotation) {
                            case 2:
                                faceRotation = Blocks.AxisXRotation;
                                break;
                            case 4:
                                faceRotation = Blocks.AxisZRotation;
                                break;
                        }
                    }
                    
                    List<BlockFaceInstance> listRef = Blocks.IsNotSolid(blockId) ? fluidInstances : solidInstances;
                    for(int face=0;faces!=0;face++){
                        if((faces&1u)!=0) {
                            listRef.Add(new BlockFaceInstance(
                                new Vector3(x,y + currentChunkY,z) + new Vector3(0.5f, 0.5f, 0.5f),
                                textureCoordinates[(face+blockRotation)%6],
                                new Vector2(face, faceRotation[face]),
                                chunk.GetLightValues(currentBlock, face)));
                        }
                        faces>>=1;
                    }
                }
            }
        }

        UpdateBuffer(ref chunk.vertexBufferOpaque, solidInstances);
        UpdateBuffer(ref chunk.vertexBufferTransparent, fluidInstances);
        UpdateBuffer(ref chunk.vertexBufferFoliage, spriteInstances);
    }
    private static bool IsVisible(byte id, byte anotherId) {
        return id != anotherId && Blocks.IsTransparent(anotherId);
    }
    private static void UpdateBuffer<T>(ref VertexBuffer vertexBuffer, List<T> vertices) where T : struct {
        vertexBuffer?.Dispose();
        if (vertices.Count != 0) {
            vertexBuffer = new VertexBuffer(graphicsDevice,typeof(T),vertices.Count,BufferUsage.WriteOnly);
            vertexBuffer.SetData(0,vertices.GetInternalArray(),0,vertices.Count,0);
        }
    }
    public static void DrawChunk(VertexBuffer buffer){
        if(buffer is not null && !buffer.IsDisposed){
            Draw(buffer);
        }
    }
    public static void DrawCubeFrame(CustomEffect effect, Vector3 lookedAtBlock){
        effect.Apply(Matrix.CreateScale(1.0025f) * Matrix.CreateWorld(lookedAtBlock, Vector3.Forward, Vector3.Up));
        Draw(frameBuffer);
    }
    public static void DrawCubePreview(CustomEffect effect){
        effect.DrawBlockPreview();
        Draw(previewBuffer);
    }
    private static void Draw(VertexBuffer buffer) {
        graphicsDevice.SetVertexBuffers(
            new VertexBufferBinding(faceBuffer, 0, 0),
            new VertexBufferBinding(buffer, 0, 1));
        graphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 2, buffer.VertexCount);
    }
    public static void DrawSprites(VertexBuffer buffer) {
        if(buffer is not null && !buffer.IsDisposed){
            graphicsDevice.SetVertexBuffers(
                new VertexBufferBinding(spriteBuffer, 0, 0),
                new VertexBufferBinding(buffer, 0, 1));
            graphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, buffer.VertexCount);
        }
    }
    public static void ChangeCubePreview(byte id){
        if (Blocks.IsFoliage(id)) return;
        Vector2[] textureCoordinates = TextureDictionary[id];
        BlockFaceInstance[] cubeVerticesPreview = [
            new (new Vector3(0.5f,0.5f,0.5f), textureCoordinates[0], new Vector2(0,0), Color.White),
            new (new Vector3(0.5f,0.5f,0.5f), textureCoordinates[2], new Vector2(2,0), Color.White),
            new (new Vector3(0.5f,0.5f,0.5f), textureCoordinates[4], new Vector2(4,0), Color.White),
        ];
        previewBuffer.SetData(cubeVerticesPreview);
    }
    public enum BlockFace{East,West,South,North,Up,Down,None};
    public static BlockFace GetFace(Ray ray,BoundingBox box){
        float? distance = ray.Intersects(box);
        if(distance.HasValue){
            Vector3 localIntersectionPoint = ray.Position + ray.Direction*distance.Value - box.Min;

            if (localIntersectionPoint.X < 0.0001f) return BlockFace.South;
            if (localIntersectionPoint.X > 0.9999f) return BlockFace.North;
            if (localIntersectionPoint.Y < 0.0001f) return BlockFace.Down;
            if (localIntersectionPoint.Y > 0.9999f) return BlockFace.Up;
            if (localIntersectionPoint.Z < 0.0001f) return BlockFace.East;
            if (localIntersectionPoint.Z > 0.9999f) return BlockFace.West;
        }
        return BlockFace.None;
    }

    private static bool cloudLock;
    private static DynamicVertexBuffer cloudBuffer;
    private static CloudInstance[] cloudInstancesArray;
    private static int cloudIndex;
    public static (int x, int z) CloudOffset;
    public const int CloudRes = 4;
    public static bool CloudBufferUpdate;
    public static void updateCloudBuffer() {
        cloudInstancesArray = new CloudInstance[(int)Math.Pow((ChunkSize/CloudRes)*(UserSettings.RenderDistance*2+1),2)];
        cloudBuffer = new DynamicVertexBuffer(graphicsDevice, typeof(CloudInstance), cloudInstancesArray.Length, BufferUsage.WriteOnly);
    }
    public static void UpdateAndDrawClouds(int offsetChunkX, int offsetChunkZ,double time, float timeOfDay) {
        if (!cloudLock) {
            cloudLock = true;
            if (CloudBufferUpdate) {
                updateCloudBuffer();
                CloudBufferUpdate = false;
            }
            Task.Run(() => {
                int renderDistance = UserSettings.RenderDistance;
                int index = 0;
                for (int x = -(ChunkSize/CloudRes) * renderDistance; x < (ChunkSize/CloudRes) * (renderDistance+1); x++) {
                    for (int z = -(ChunkSize/CloudRes) * renderDistance; z < (ChunkSize/CloudRes) * (renderDistance+1); z++) {
                        double noiseValue = OpenSimplex2.Noise3_ImproveXY(TerrainGen.seed, (double)(x+offsetChunkX*(ChunkSize/CloudRes))/100, (double)(z+offsetChunkZ*(ChunkSize/CloudRes))/100, time);
                        if (noiseValue < 0) {
                            continue;
                        }

                        cloudInstancesArray[index].Offset = new Vector3(x, 250f, z);
                        cloudInstancesArray[index].Color = (float)noiseValue * timeOfDay;
                        index++;
                    }
                }

                if (index > 0) {
                    cloudBuffer.SetData(cloudInstancesArray,0,index, SetDataOptions.NoOverwrite);
                }
                cloudIndex = index;
                CloudOffset = (offsetChunkX,offsetChunkZ);
                cloudLock = false;
            });
        }
        
        graphicsDevice.SetVertexBuffers(
            new VertexBufferBinding(faceBuffer, 0, 0),
            new VertexBufferBinding(cloudBuffer, 0, 1));
        graphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 2, cloudIndex);
    }
    struct CloudInstance(Vector3 offset, float color) : IVertexType {
        public Vector3 Offset = offset;
        public float Color = color;

        static readonly VertexDeclaration VertexDeclaration = new(
            new VertexElement(0,  VertexElementFormat.Vector3,
                VertexElementUsage.Position, 1),
            new VertexElement(12, VertexElementFormat.Single,
                VertexElementUsage.Color,    1));
        
        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
    struct BlockFaceInstance(Vector3 offset, Vector2 texCoords, Vector2 rotation, Color color) : IVertexType {
        public Vector3 Offset = offset;
        public Vector2 TexCoords = texCoords;
        public Vector2 Rotation = rotation;
        public Color Color = color;

        static readonly VertexDeclaration VertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector3,
                VertexElementUsage.Position, 1),
            new VertexElement(12, VertexElementFormat.Vector2, 
                VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(20, VertexElementFormat.Vector2, 
                VertexElementUsage.Normal, 1),
            new VertexElement(28, VertexElementFormat.Color,
                VertexElementUsage.Color, 1));
        
        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
}