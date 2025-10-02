using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VoxelTechDemo{
    public static class VoxelRenderer{
        private static GraphicsDevice graphicsDevice;
        private static readonly Dictionary<int,Vector2[]> TextureDictionary = new Blocks().TextureDictionary;
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
        }
        public static void GenerateChunkMesh(Chunk chunk){
            //TODO: Try to combine multiple chunks into single region to reduce number of world matrices needed
            int CurrentChunkY = chunk.coordinates.y*ChunkSize;
            List<VertexPositionColorTexture> solidVertices = [];
            List<VertexPositionColorTexture> fluidVertices = [];
            List<VertexPositionColorTexture> foliageVertices = [];
            
            byte[] blocks = chunk.blocks;

            // Check and assign adjacent chunks blocks
            byte[] blocksNorth = chunk.world.WorldMap[(chunk.coordinates.x+1,chunk.coordinates.y,chunk.coordinates.z)].blocks;
            byte[] blocksSouth = chunk.world.WorldMap[(chunk.coordinates.x-1,chunk.coordinates.y,chunk.coordinates.z)].blocks;
            byte[] blocksWest = chunk.world.WorldMap[(chunk.coordinates.x,chunk.coordinates.y,chunk.coordinates.z+1)].blocks;
            byte[] blocksEast = chunk.world.WorldMap[(chunk.coordinates.x,chunk.coordinates.y,chunk.coordinates.z-1)].blocks;
            byte[] blocksUp;
            if(chunk.world.WorldMap.TryGetValue((chunk.coordinates.x,chunk.coordinates.y+1,chunk.coordinates.z), out Chunk chunkUp)){
                blocksUp = chunkUp.blocks;
            }
            else{
                blocksUp = null;
            }
            byte[] blocksDown;
            if(chunk.world.WorldMap.TryGetValue((chunk.coordinates.x,chunk.coordinates.y-1,chunk.coordinates.z), out Chunk chunkDown)){
                blocksDown = chunkDown.blocks;
            }
            else{
                blocksDown = null;
            }

            // Check every block within a chunk if it needs to be meshed
            int currentBlock = 0;
            for(int z=0;z<ChunkSize;z++){
                for(int y=0;y<ChunkSize;y++){
                    for(int x=0;x<ChunkSize;x++){
                        if(blocks[currentBlock]!=0){
                            byte blockId = blocks[currentBlock];
                            Vector2[] textureCoordinates = TextureDictionary[blockId];
                            if (!Blocks.IsFoliage(blocks[currentBlock])) {
                                uint faces = 0;

                                //face x+
                                if(x != (ChunkSize-1) ? (blockId != blocks[currentBlock+1] && Blocks.IsTransparent(blocks[currentBlock+1])) 
                                : (blockId != blocksNorth[currentBlock-(ChunkSize-1)] && Blocks.IsTransparent(blocksNorth[currentBlock-(ChunkSize-1)]))){
                                    faces |= 1;
                                }
                                // Face x-
                                if(x != 0 ? (blockId != blocks[currentBlock-1] && Blocks.IsTransparent(blocks[currentBlock-1])) 
                                : (blockId != blocksSouth[currentBlock+(ChunkSize-1)] && Blocks.IsTransparent(blocksSouth[currentBlock+(ChunkSize-1)]))){
                                    faces |= 2;
                                }
                                // Face y+
                                if (y != ChunkSize-1 ? (blockId!=blocks[currentBlock+ChunkSize] && Blocks.IsTransparent(blocks[currentBlock+ChunkSize]))
                                : (blocksUp != null && blockId!=blocksUp[currentBlock-(ChunkSizeSquared-ChunkSize)] && Blocks.IsTransparent(blocksUp[currentBlock-(ChunkSizeSquared-ChunkSize)]))){
                                    faces |= 4;
                                }
                                // Face y-
                                if (y != 0 ? (blockId!=blocks[currentBlock-ChunkSize] && Blocks.IsTransparent(blocks[currentBlock-ChunkSize]))
                                : (blocksDown != null && blockId!=blocksDown[currentBlock+(ChunkSizeSquared-ChunkSize)] && Blocks.IsTransparent(blocksDown[currentBlock+(ChunkSizeSquared-ChunkSize)]))){
                                    faces |= 8;
                                }
                                // Face z+
                                if(z != (ChunkSize-1) ? (blockId != blocks[currentBlock+ChunkSizeSquared] && Blocks.IsTransparent(blocks[currentBlock+ChunkSizeSquared])) 
                                : (blockId != blocksWest[currentBlock-(ChunkSizeCubed-ChunkSizeSquared)] && Blocks.IsTransparent(blocksWest[currentBlock-(ChunkSizeCubed-ChunkSizeSquared)]))){
                                    faces |= 16;
                                }
                                // Face z-
                                if(z != 0 ? (blockId != blocks[currentBlock-ChunkSizeSquared] && Blocks.IsTransparent(blocks[currentBlock-ChunkSizeSquared])) 
                                : (blockId != blocksEast[currentBlock+(ChunkSizeCubed-ChunkSizeSquared)] && Blocks.IsTransparent(blocksEast[currentBlock+(ChunkSizeCubed-ChunkSizeSquared)]))){
                                    faces |= 32;
                                }
                                
                                if(faces != 0){
                                    List<VertexPositionColorTexture> listRef = Blocks.IsNotSolid(blockId) ? fluidVertices : solidVertices;
                                    for(int face=0;faces!=0;face++){
                                        if((faces&1u)!=0){
                                            for(int i=face*4;i<face*4+4;i++){
                                                listRef.Add(new VertexPositionColorTexture(new Vector3(
                                                    x+((offsetX>>i)&1),
                                                    y+((offsetY>>i)&1)+CurrentChunkY,
                                                    z+((offsetZ>>i)&1)),
                                                    chunk.GetLightValues(currentBlock, face),
                                                    textureCoordinates[i]));
                                            }
                                        }
                                        faces>>=1;
                                    }
                                }
                            }
                            else {
                                for (int i = 0; i < 8; i++) {
                                    foliageVertices.Add(new VertexPositionColorTexture(new  Vector3(
                                        x+((offsetSpriteX>>i)&1),
                                        y+((offsetSpriteY>>i)&1)+CurrentChunkY,
                                        z+((offsetSpriteZ>>i)&1)),
                                        Light.ConvertLightValues(chunk.blockLightValues[currentBlock]),
                                        textureCoordinates[i%4]));
                                }
                            }
                        }
                        currentBlock++;
                    }
                }
            }

            chunk.vertexBufferOpaque?.Dispose();
            if(solidVertices.Count != 0){
                VertexBuffer vertexBufferOpaque = new(graphicsDevice,typeof(VertexPositionColorTexture),solidVertices.Count,BufferUsage.WriteOnly);
                vertexBufferOpaque.SetData(solidVertices.ToArray());
                chunk.vertexBufferOpaque = vertexBufferOpaque;
            }
            chunk.vertexBufferTransparent?.Dispose();
            if(fluidVertices.Count != 0){
                VertexBuffer vertexBufferTransparent = new(graphicsDevice,typeof(VertexPositionColorTexture),fluidVertices.Count,BufferUsage.WriteOnly);
                vertexBufferTransparent.SetData(fluidVertices.ToArray());
                chunk.vertexBufferTransparent = vertexBufferTransparent;
            }
            chunk.vertexBufferFoliage?.Dispose();
            if (foliageVertices.Count != 0) {
                VertexBuffer vertexBufferFoliage = new(graphicsDevice,typeof(VertexPositionColorTexture),foliageVertices.Count,BufferUsage.WriteOnly);
                vertexBufferFoliage.SetData(foliageVertices.ToArray());
                chunk.vertexBufferFoliage = vertexBufferFoliage;
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
            effect.Apply(Matrix.CreateWorld(LookedAtBlock, Vector3.Forward, Vector3.Up));
            graphicsDevice.SetVertexBuffer(cubeFrameVertex);
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
        }
        public static void ChangeCubePreview(byte id){
            cubePreviewVertex?.Dispose();
            Vector2[] textureCoordinates = TextureDictionary[id];
            VertexPositionColorTexture[] cubeVerticesPreview;
            if (Blocks.IsFoliage(id)) {
                cubePreviewVertex = new VertexBuffer(graphicsDevice,typeof(VertexPositionColorTexture), 8, BufferUsage.WriteOnly);
                cubeVerticesPreview = new VertexPositionColorTexture[8];
                for (int i = 0; i < 8; i++) {
                    cubeVerticesPreview[i] = new VertexPositionColorTexture(
                        new Vector3((offsetX>>i)&1,(offsetY>>i)&1,(offsetZ>>i)&1),
                        Color.White,
                        textureCoordinates[i%4]);
                }
            }
            else {
                cubePreviewVertex = new VertexBuffer(graphicsDevice,typeof(VertexPositionColorTexture), 12, BufferUsage.WriteOnly);
                cubeVerticesPreview = new VertexPositionColorTexture[12];
                for(int i=0;i<4;i++){
                    cubeVerticesPreview[i] = new VertexPositionColorTexture(new Vector3((offsetX>>i)&1,(offsetY>>i)&1,(offsetZ>>i)&1), Color.White, textureCoordinates[i]);
                }
                for(int i=8;i<12;i++){
                    cubeVerticesPreview[i-4] = new VertexPositionColorTexture(new Vector3((offsetX>>i)&1,(offsetY>>i)&1,(offsetZ>>i)&1), Color.White, textureCoordinates[i]);
                }
                for(int i=16;i<20;i++){
                    cubeVerticesPreview[i-8] = new VertexPositionColorTexture(new Vector3((offsetX>>i)&1,(offsetY>>i)&1,(offsetZ>>i)&1), Color.White, textureCoordinates[i]);
                }
            }
            cubePreviewVertex.SetData(cubeVerticesPreview);
        }
        public static void DrawCubePreview(CustomEffect effect){
            effect.DrawBlockPreview();
            graphicsDevice.SetVertexBuffer(cubePreviewVertex);
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, cubePreviewVertex.VertexCount/2);
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
        private static VertexBuffer cloudBuffer;
        public static (int x, int z) cloudOffset;
        public const int cloudRes = 4;
        public static void UpdateAndDrawClouds(World world, int offsetChunkX, int offsetChunkZ,double time) {
            if (!cloudLock) {
                cloudLock = true;
                Task.Run(() => {
                    List<CloudInstance> cloudVertices = [];
                    for (int x = -(ChunkSize/cloudRes) * UserSettings.RenderDistance; x < (ChunkSize/cloudRes) * (UserSettings.RenderDistance+1); x++) {
                        for (int z = -(ChunkSize/cloudRes) * UserSettings.RenderDistance; z < (ChunkSize/cloudRes) * (UserSettings.RenderDistance+1); z++) {
                            double noiseValue = OpenSimplex2.Noise3_ImproveXY(world.seed, (double)(x+offsetChunkX*(ChunkSize/cloudRes))/100, (double)(z+offsetChunkZ*(ChunkSize/cloudRes))/100, time);
                            if (noiseValue < 0) {
                                continue;
                            }
                            
                            cloudVertices.Add(new  CloudInstance(new Vector3(
                                x,
                                250.5f,
                                z),
                                (float)Math.Abs(noiseValue)));
                        }
                    }

                    if (cloudVertices.Count > 0) {
                        VertexBuffer newCloudBuffer = new(graphicsDevice, typeof(CloudInstance), cloudVertices.Count, BufferUsage.WriteOnly);
                        newCloudBuffer.SetData(cloudVertices.ToArray());
                        cloudBuffer?.Dispose();
                        cloudBuffer = newCloudBuffer;
                    }
                    cloudOffset = (offsetChunkX,offsetChunkZ);
                    cloudLock = false;
                });
            }

            if (cloudBuffer != null) {
                graphicsDevice.SetVertexBuffers(
                    new VertexBufferBinding(faceBuffer, 0, 0),
                    new VertexBufferBinding(cloudBuffer, 0, 1));
                graphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 2, cloudBuffer.VertexCount);
            }
        }
        
        private static VertexBuffer faceBuffer;
        private static readonly VertexPosition[] faceVertices = [
            new (new Vector3(0,0,0)),
            new (new Vector3(1,0,0)),
            new (new Vector3(0,0,1)),
            new (new Vector3(1,0,1)),
        ];
        struct CloudInstance : IVertexType
        {
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
}