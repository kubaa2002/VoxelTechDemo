using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VoxelTechDemo;
public static class VoxelRenderer{
    private static GraphicsDevice graphicsDevice;
    public static readonly Dictionary<int,Vector2[]> TextureDictionary = new Blocks().TextureDictionary;
    //z- z+ y- y+ x- x+
    const int offsetX = 0b1010_0101_1010_1010_0000_1111;
    const int offsetY = 0b1100_1100_0000_1111_1100_1100;
    const int offsetZ = 0b0000_1111_0011_1100_0101_1010;

    private const int offsetSpriteX = 0b1010_1010;
    private const int offsetSpriteY = 0b1100_1100;
    private const int offsetSpriteZ = 0b0101_1010;
    
    //ChunkSize needs to be a power of 2. Works up to 64 (YShift = 6)
    public const int YShift = 6;
    public const int ZShift = YShift * 2;
    public const int ChunkSize = 1 << YShift;
    public const int ChunkSizeSquared = ChunkSize*ChunkSize;
    public const int ChunkSizeCubed = ChunkSize*ChunkSize*ChunkSize;
    public static IndexBuffer indexBuffer;
    public static void InitializeVoxelRenderer(GraphicsDevice _graphicsDevice){
        graphicsDevice=_graphicsDevice;
        SetupCubeFrame();
        GenerateIndexBuffer();
        faceBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPosition), faceVertices.Length, BufferUsage.WriteOnly);
        faceBuffer.SetData(faceVertices);
        updateCloudBuffer();
        cubePreviewVertex = new VertexBuffer(graphicsDevice,typeof(VertexPositionColorTexture), 12, BufferUsage.WriteOnly);
    }
    public static void GenerateChunkMesh(Chunk chunk){
        // Check and assign chunk block arrays
        (int cx, int cy, int cz) = chunk.coordinates;
        byte[] blocks = chunk.blocks;
        byte[] blocksNorth = chunk.world.WorldMap[(cx+1,cy,cz)].blocks;
        byte[] blocksSouth = chunk.world.WorldMap[(cx-1,cy,cz)].blocks;
        byte[] blocksWest = chunk.world.WorldMap[(cx,cy,cz+1)].blocks;
        byte[] blocksEast = chunk.world.WorldMap[(cx,cy,cz-1)].blocks;
        byte[] blocksUp = chunk.world.WorldMap.TryGetValue((cx,cy+1,cz), out Chunk chunkUp) ? chunkUp.blocks : null;
        byte[] blocksDown = chunk.world.WorldMap.TryGetValue((cx,cy-1,cz), out Chunk chunkDown) ? chunkDown.blocks : null;
        int currentChunkY = cy*ChunkSize;

        // Check every block within a chunk if it needs to be added to the mesh
        List<VertexPositionColorTexture> solidVertices = [];
        List<VertexPositionColorTexture> fluidVertices = [];
        List<VertexPositionColorTexture> foliageVertices = [];
        int currentBlock = -1;
        for(int z=0;z<ChunkSize;z++){
            for(int y=0;y<ChunkSize;y++){
                for(int x=0;x<ChunkSize;x++){
                    currentBlock++;
                    byte blockId = blocks[currentBlock];
                    if (blockId == 0) continue;
                    Vector2[] textureCoordinates = TextureDictionary[blockId];
                    
                    if (Blocks.IsFoliage(blockId)) {
                        Color light = Light.ConvertLightValues(chunk.blockLightValues[currentBlock]);
                        for (int i = 0; i < 8; i++) {
                            foliageVertices.Add(new VertexPositionColorTexture(new Vector3(
                                x + ((offsetSpriteX >> i) & 1),
                                y + ((offsetSpriteY >> i) & 1) + currentChunkY,
                                z + ((offsetSpriteZ >> i) & 1)),
                                light,
                                textureCoordinates[i % 4]));
                        }
                        continue;
                    }

                    uint faces = 0;
                    //face x+
                    if (IsVisible(blockId, x != ChunkSize - 1 ? blocks[currentBlock + 1] : blocksNorth[currentBlock - (ChunkSize - 1)])) {
                        faces |= 1;
                    }
                    // Face x-
                    if (IsVisible(blockId, x != 0 ? blocks[currentBlock - 1] : blocksSouth[currentBlock + (ChunkSize - 1)])) {
                        faces |= 2;
                    }
                    // Face y+
                    if (IsVisible(blockId, y != ChunkSize - 1 ? blocks[currentBlock + ChunkSize] : (blocksUp != null ? blocksUp[currentBlock - (ChunkSizeSquared - ChunkSize)] : (byte)0))) {
                        faces |= 4;
                    }
                    // Face y-
                    if (IsVisible(blockId, y != 0 ? blocks[currentBlock - ChunkSize] : (blocksDown != null ? blocksDown[currentBlock + (ChunkSizeSquared - ChunkSize)] : (byte)0))) {
                        faces |= 8;
                    }
                    // Face z+
                    if (IsVisible(blockId, z != ChunkSize - 1 ? blocks[currentBlock + ChunkSizeSquared] : blocksWest[currentBlock - (ChunkSizeCubed - ChunkSizeSquared)])) {
                        faces |= 16;
                    }
                    // Face z-
                    if (IsVisible(blockId, z != 0 ? blocks[currentBlock - ChunkSizeSquared] : blocksEast[currentBlock + (ChunkSizeCubed - ChunkSizeSquared)])) {
                        faces |= 32;
                    }
                    if (faces == 0) continue;

                    byte rotation = 0;
                    if (Blocks.CanRotate(blockId) && chunk.BlockStates.TryGetValue(currentBlock, out rotation)) {
                        textureCoordinates = Blocks.RotateUV(textureCoordinates, rotation);
                    }
                    
                    List<VertexPositionColorTexture> listRef = Blocks.IsNotSolid(blockId) ? fluidVertices : solidVertices;
                    for(int face=0;faces!=0;face++){
                        if((faces&1u)!=0) {
                            for(int i=face*4;i<face*4+4;i++) {
                                Color light = chunk.GetLightValues(currentBlock, face);
                                listRef.Add(new VertexPositionColorTexture(new Vector3(
                                    x+((offsetX>>i)&1),
                                    y+((offsetY>>i)&1)+currentChunkY,
                                    z+((offsetZ>>i)&1)),
                                    light,
                                    textureCoordinates[(i+8*rotation)%24]));
                            }
                        }
                        faces>>=1;
                    }
                }
            }
        }

        UpdateBuffer(ref chunk.vertexBufferOpaque, solidVertices);
        UpdateBuffer(ref chunk.vertexBufferTransparent, fluidVertices);
        UpdateBuffer(ref chunk.vertexBufferFoliage, foliageVertices);
    }
    private static bool IsVisible(byte id, byte anotherId) {
        return id != anotherId && Blocks.IsTransparent(anotherId);
    }
    private static void UpdateBuffer(ref VertexBuffer vertexBuffer, List<VertexPositionColorTexture> vertices) {
        vertexBuffer?.Dispose();
        if (vertices.Count != 0) {
            VertexBuffer newVertexBuffer = new(graphicsDevice,typeof(VertexPositionColorTexture),vertices.Count,BufferUsage.WriteOnly);
            newVertexBuffer.SetData(vertices.ToArray());
            vertexBuffer = newVertexBuffer;
        }
    }
    static void GenerateIndexBuffer(){
        byte[] indicesOffset = [0,1,2,1,3,2];
        int[] indicesArray = new int[ChunkSizeCubed*36]; 
        for (int currentBlock = 0;currentBlock<ChunkSizeCubed*6;currentBlock++){
            for(int i=0;i<6;i++){
                indicesArray[currentBlock*6+i]=currentBlock*4+indicesOffset[i];
            }       
        }
        indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indicesArray.Length, BufferUsage.WriteOnly);
        indexBuffer.SetData(indicesArray);
    }
    public static void DrawChunk(VertexBuffer buffer){
        if(buffer is not null && !buffer.IsDisposed){
            graphicsDevice.SetVertexBuffer(buffer);
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.VertexCount/2);
        }
    }
    static VertexBuffer cubeFrameVertex;
    static VertexBuffer cubePreviewVertex;
    static void SetupCubeFrame(){
        cubeFrameVertex = new VertexBuffer(graphicsDevice,typeof(VertexPositionColorTexture), 24, BufferUsage.WriteOnly);
        Vector2[] cubeFrameTextureCoordinates = TextureDictionary[0];
        VertexPositionColorTexture[] cubeVertices = new VertexPositionColorTexture[24];
        for(int i=0;i<24;i++){
            cubeVertices[i] = new VertexPositionColorTexture(new Vector3(
                1.0025f*((offsetX>>i)&1)-0.00125f,
                1.0025f*((offsetY>>i)&1)-0.00125f,
                1.0025f*((offsetZ>>i)&1)-0.00125f),
                Color.White,
                cubeFrameTextureCoordinates[i]);
        }
        cubeFrameVertex.SetData(cubeVertices);
    }
    public static void DrawCubeFrame(CustomEffect effect, Vector3 LookedAtBlock){
        effect.Apply(LookedAtBlock);
        graphicsDevice.SetVertexBuffer(cubeFrameVertex);
        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
    }
    public static void ChangeCubePreview(byte id){
        if (Blocks.IsFoliage(id)) return;
        Vector2[] textureCoordinates = TextureDictionary[id];
        VertexPositionColorTexture[] cubeVerticesPreview = new VertexPositionColorTexture[12];
        for(int i=0;i<4;i++){
            cubeVerticesPreview[i] = new VertexPositionColorTexture(new Vector3((offsetX>>i)&1,(offsetY>>i)&1,(offsetZ>>i)&1), Color.White, textureCoordinates[i]);
        }
        for(int i=8;i<12;i++){
            cubeVerticesPreview[i-4] = new VertexPositionColorTexture(new Vector3((offsetX>>i)&1,(offsetY>>i)&1,(offsetZ>>i)&1), Color.White, textureCoordinates[i]);
        }
        for(int i=16;i<20;i++){
            cubeVerticesPreview[i-8] = new VertexPositionColorTexture(new Vector3((offsetX>>i)&1,(offsetY>>i)&1,(offsetZ>>i)&1), Color.White, textureCoordinates[i]);
        }
        cubePreviewVertex.SetData(cubeVerticesPreview);
    }
    public static void DrawCubePreview(CustomEffect effect){
        effect.DrawBlockPreview();
        graphicsDevice.SetVertexBuffer(cubePreviewVertex);
        graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 6);
    }
    public enum BlockFace{Front,Back,Right,Left,Top,Bottom,None};
    public static BlockFace GetFace(Ray ray,BoundingBox box){
        float? distance = ray.Intersects(box);
        if(distance.HasValue){
            Vector3 localIntersectionPoint = ray.Position + ray.Direction*distance.Value - box.Min;

            if (localIntersectionPoint.X < 0.0001f) return BlockFace.Right;
            if (localIntersectionPoint.X > 0.9999f) return BlockFace.Left;
            if (localIntersectionPoint.Y < 0.0001f) return BlockFace.Bottom;
            if (localIntersectionPoint.Y > 0.9999f) return BlockFace.Top;
            if (localIntersectionPoint.Z < 0.0001f) return BlockFace.Front;
            if (localIntersectionPoint.Z > 0.9999f) return BlockFace.Back;
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
    public static void UpdateAndDrawClouds(World world, int offsetChunkX, int offsetChunkZ,double time) {
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
                        double noiseValue = OpenSimplex2.Noise3_ImproveXY(world.seed, (double)(x+offsetChunkX*(ChunkSize/CloudRes))/100, (double)(z+offsetChunkZ*(ChunkSize/CloudRes))/100, time);
                        if (noiseValue < 0) {
                            continue;
                        }

                        cloudInstancesArray[index].Offset = new Vector3(x, 250.5f, z);
                        cloudInstancesArray[index].Color = (float)noiseValue;
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
    
    private static VertexBuffer faceBuffer;
    private static readonly VertexPosition[] faceVertices = [
        new (new Vector3(0,0,0)),
        new (new Vector3(1,0,0)),
        new (new Vector3(0,0,1)),
        new (new Vector3(1,0,1)),
    ];
    struct CloudInstance : IVertexType {
        public Vector3 Offset;
        public float   Color;

        public static readonly VertexDeclaration VertexDeclaration = new(
            new VertexElement(0,  VertexElementFormat.Vector3,
                VertexElementUsage.Position, 1),
            
            new VertexElement(12, VertexElementFormat.Single,
                VertexElementUsage.Color,    1));
        
        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public CloudInstance(Vector3 offset, float color) {
            Offset = offset;
            Color = color;
        }
    }
}