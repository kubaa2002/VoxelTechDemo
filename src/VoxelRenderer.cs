using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VoxelTechDemo{
    public static class VoxelRenderer{
        static private GraphicsDevice graphicsDevice;
        static private readonly Dictionary<int,Vector2[]> TextureDictionary = new Blocks().TextureDictionary;
        //z- z+ y- y+ x- x+
        const int offsetX = 0b1010_0101_1010_1010_0000_1111;
        const int offsetY = 0b1100_1100_0000_1111_1100_1100;
        const int offsetZ = 0b0000_1111_0011_1100_0101_1010;
        //ChunkSize needs to be an power of 2. Works up to 64 (YShift = 6)
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
        }
        public static void GenerateChunkMesh(Chunk chunk){
            //TODO: Try to combine multiple chunks into single region to reduce number of world matrixes needed
            int CurrentChunkY = chunk.coordinates.y*ChunkSize;
            List<VertexPositionColorTexture> solidVertices = [];
            List<VertexPositionColorTexture> fluidVertices = [];
            
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
            for(int z=0;z<ChunkSize;z++){
                int currentBlock = z*ChunkSizeSquared;
                for(int y=0;y<chunk.maxY;y++){
                    for(int x=0;x<ChunkSize;x++){
                        if(blocks[currentBlock]!=0){
                            uint faces = 0;
                            byte blockId = blocks[currentBlock];

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
                            : (blocksUp != null && blockId!=blocksUp[currentBlock+(ChunkSize-ChunkSizeSquared)] && Blocks.IsTransparent(blocksUp[currentBlock+(ChunkSize-ChunkSizeSquared)]))){
                                faces |= 4;
                            }
                            // Face y-
                            if (y != 0 ? (blockId!=blocks[currentBlock-ChunkSize] && Blocks.IsTransparent(blocks[currentBlock-ChunkSize]))
                            : (blocksDown != null && blockId!=blocksDown[currentBlock-(ChunkSize-ChunkSizeSquared)] && Blocks.IsTransparent(blocksUp[currentBlock-(ChunkSize-ChunkSizeSquared)]))){
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
                                Vector2[] textureCoordinates = TextureDictionary[blockId];
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
                        currentBlock++;
                    }
                }
            }

            chunk.vertexBufferOpaque?.Dispose();
            if(solidVertices.Count != 0){
                VertexBuffer vertexBufferOpaque = new(graphicsDevice,typeof(VertexPositionColorTexture),solidVertices.Count,BufferUsage.None);
                vertexBufferOpaque.SetData(solidVertices.ToArray());
                chunk.vertexBufferOpaque = vertexBufferOpaque;
            }
            chunk.vertexBufferTransparent?.Dispose();
            if(fluidVertices.Count != 0){
                VertexBuffer vertexBufferTransparent = new(graphicsDevice,typeof(VertexPositionColorTexture),fluidVertices.Count,BufferUsage.None);
                vertexBufferTransparent.SetData(fluidVertices.ToArray());
                chunk.vertexBufferTransparent = vertexBufferTransparent;
            }
        }
        public static void GenerateIndexBuffer(){
            byte[] indicesOffset = [0,1,2,1,3,2];
            int[] indicesArray = new int[ChunkSizeCubed*36]; 
            for (int currentBlock = 0;currentBlock<ChunkSizeCubed*6;currentBlock++){
                for(int i=0;i<6;i++){
                    indicesArray[currentBlock*6+i]=currentBlock*4+indicesOffset[i];
                }       
            }
            indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indicesArray.Length, BufferUsage.None);
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
        static public void SetupCubeFrame(){
            cubeFrameVertex = new VertexBuffer(graphicsDevice,typeof(VertexPositionColorTexture), 24, BufferUsage.None);
            cubePreviewVertex = new VertexBuffer(graphicsDevice,typeof(VertexPositionColorTexture), 12, BufferUsage.None);
            Vector2[] cubeFrameTextureCoordinates = TextureDictionary[0];
            VertexPositionColorTexture[] cubeVertices = new VertexPositionColorTexture[24];
            for(int i=0;i<24;i++){
                cubeVertices[i] = new VertexPositionColorTexture(new Vector3(
                    1.0025f*((offsetX&(1<<i))>>i)-0.00125f,
                    1.0025f*((offsetY&(1<<i))>>i)-0.00125f,
                    1.0025f*((offsetZ&(1<<i))>>i)-0.00125f),
                    Color.White,
                    cubeFrameTextureCoordinates[i]);
            }
            cubeFrameVertex.SetData(cubeVertices);
        }
        static public void DrawCubeFrame(CustomEffect effect, Vector3 LookedAtBlock){
            effect.Apply(Matrix.CreateWorld(LookedAtBlock, Vector3.Forward, Vector3.Up));
            graphicsDevice.SetVertexBuffer(cubeFrameVertex);
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
        }
        static public void ChangeCubePreview(byte Id){
            VertexPositionColorTexture[] cubeVerticesPreview = new VertexPositionColorTexture[12];
            Vector2[] TextureCoordinates = TextureDictionary[Id];
            for(int i=0;i<4;i++){
                cubeVerticesPreview[i] = new VertexPositionColorTexture(new Vector3((offsetX&(1<<i))>>i,(offsetY&(1<<i))>>i,(offsetZ&(1<<i))>>i), Color.White, TextureCoordinates[i]);
            }
            for(int i=8;i<12;i++){
                cubeVerticesPreview[i-4] = new VertexPositionColorTexture(new Vector3((offsetX&(1<<i))>>i,(offsetY&(1<<i))>>i,(offsetZ&(1<<i))>>i), Color.White, TextureCoordinates[i]);
            }
            for(int i=16;i<20;i++){
                cubeVerticesPreview[i-8] = new VertexPositionColorTexture(new Vector3((offsetX&(1<<i))>>i,(offsetY&(1<<i))>>i,(offsetZ&(1<<i))>>i), Color.White, TextureCoordinates[i]);
            }
            cubePreviewVertex.SetData(cubeVerticesPreview);
        }
        static public void DrawCubePreview(CustomEffect effect){
            effect.DrawBlockPreview();
            graphicsDevice.SetVertexBuffer(cubePreviewVertex);
            //Indices have to be set because sprite batch resets it
            graphicsDevice.Indices = indexBuffer;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 6);
        }
        public static Matrix CreateBlockPreviewProj(int x,int y,float scale){
            float aspectRatio = graphicsDevice.Viewport.AspectRatio * scale;
            float translateX = aspectRatio-(float)x / graphicsDevice.Viewport.Width * (aspectRatio + aspectRatio);
            float translateY = (float)y / graphicsDevice.Viewport.Height * (scale + scale) - scale;
            return Matrix.CreateOrthographicOffCenter(translateX-aspectRatio,aspectRatio+translateX,translateY-scale,scale+translateY,1f, 10f);
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
    }
}