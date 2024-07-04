using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VoxelTechDemo
{
    public static class VoxelRenderer
    {
        static private GraphicsDevice graphicsDevice;
        static private readonly BlockIds blockIds = new();
        static readonly int offsetX = 0b101001011010101000001111;
        static readonly int offsetY = 0b110011000000111111001100;
        static readonly int offsetZ = 0b000011110011110001011010;
        public const int ChunkSize = 64;
        readonly static int ChunkSizeOne = ChunkSize-1;
        public static int square = (int)Math.Pow(ChunkSize,2);
        public static int cubed = (int)Math.Pow(ChunkSize,3);
        static IndexBuffer indexBuffer;
        public static void InitializeVoxelRenderer(GraphicsDevice _graphicsDevice){
            graphicsDevice=_graphicsDevice;
            SetupCubeFrame();
            GenerateIndexBuffer();
        }
        static public Task GenerateVertexVerticesAsync(Chunk chunk){
            return Task.Run(()=>{
                GenerateVertexVertices(chunk);
            });
        }
        static public void GenerateVertexVertices(Chunk chunk){
            VertexBuffer[] vertexBuffers = GenerateVertices(chunk);
            VertexBuffer oldVertexBuffer = chunk.vertexBufferOpaque;
            chunk.vertexBufferOpaque = vertexBuffers[0];
            oldVertexBuffer?.Dispose();
            oldVertexBuffer = chunk.vertexBufferTransparent;
            chunk.vertexBufferTransparent = vertexBuffers[1];
            oldVertexBuffer?.Dispose();
        }
        //TOFIX cant see glass behind another glass
        public static VertexBuffer[] GenerateVertices(Chunk chunk){
            int CurrentChunkX = chunk.coordinateX*ChunkSize;
            int CurrentChunkY = chunk.coordinateY*ChunkSize;
            int CurrentChunkZ = chunk.coordinateZ*ChunkSize;
            int possition = 0, transparentIndex = cubed*24 - 1;
            VertexPositionTexture[] vertices = new VertexPositionTexture[cubed*24];
            Vector3 voxelPosition;
            ulong[] result = chunk.CheckAllChunkFacesIfNeeded();
            int facePossition = 0, currentBlock;
            byte j;
            Vector2[] textureCoordinates;
            for(int face=0;face<6;face++){
                currentBlock = 0;
                for(int depth=0;depth<ChunkSize;depth++){
                    for(int i=0;i<ChunkSize;i++){
                        j=0;
                        while(result[facePossition] != 0 && j<ChunkSize){
                            if((result[facePossition]&(1uL<<j))!=0){
                                textureCoordinates = blockIds.GiveTextureVectorArrayById(chunk.blocks[currentBlock]);
                                if(Block.IsTransparent(chunk.blocks[currentBlock])){
                                    for(int k=face*4;k<face*4+4;k++){
                                        voxelPosition.X = CurrentChunkX+(currentBlock&ChunkSizeOne)+((offsetX&(1<<k))>>k);
                                        voxelPosition.Y = CurrentChunkY+((currentBlock&(ChunkSizeOne<<6))>>6)+((offsetY&(1<<k))>>k);
                                        voxelPosition.Z = CurrentChunkZ+((currentBlock&(ChunkSizeOne<<12))>>12)+((offsetZ&(1<<k))>>k);
                                        vertices[transparentIndex] = new VertexPositionTexture(voxelPosition, textureCoordinates[k]);
                                        transparentIndex--;
                                    }
                                }
                                else{
                                    for(int k=face*4;k<face*4+4;k++){
                                        voxelPosition.X = CurrentChunkX+(currentBlock&ChunkSizeOne)+((offsetX&(1<<k))>>k);
                                        voxelPosition.Y = CurrentChunkY+((currentBlock&(ChunkSizeOne<<6))>>6)+((offsetY&(1<<k))>>k);
                                        voxelPosition.Z = CurrentChunkZ+((currentBlock&(ChunkSizeOne<<12))>>12)+((offsetZ&(1<<k))>>k);
                                        vertices[possition] = new VertexPositionTexture(voxelPosition, textureCoordinates[k]);
                                        possition++;
                                    }
                                }
                            }
                            j++;
                            currentBlock++;
                        }
                        facePossition++;
                        currentBlock = currentBlock - j + 64;
                    }
                }
            }
            VertexPositionTexture[] tempVerticesOpaque = new VertexPositionTexture[possition];
            VertexPositionTexture[] tempVerticesTransparent = new VertexPositionTexture[cubed*24 - 1 - transparentIndex];
            VertexBuffer[] buffers = new VertexBuffer[2];
            if(tempVerticesOpaque.Length != 0){
                Array.Copy(vertices,tempVerticesOpaque,possition);
                VertexBuffer vertexBufferOpaque = new(graphicsDevice,typeof(VertexPositionTexture),tempVerticesOpaque.Length,BufferUsage.None);
                vertexBufferOpaque.SetData(tempVerticesOpaque);
                buffers[0]=vertexBufferOpaque;
            }
            if(tempVerticesTransparent.Length != 0){
                possition=0;
                for(int i=cubed*24-1;i>transparentIndex;i--){
                    tempVerticesTransparent[possition]=vertices[i];
                    possition++;
                }
                VertexBuffer vertexBufferTransparent = new(graphicsDevice,typeof(VertexPositionTexture),tempVerticesTransparent.Length,BufferUsage.None);
                vertexBufferTransparent.SetData(tempVerticesTransparent);
                buffers[1]=vertexBufferTransparent;
            }
            return buffers;
        }
        public static void GenerateIndexBuffer()
        {
            byte[] indicesOffset = new byte[]{0,1,2,1,3,2};
            int[] indicesArray = new int[cubed*36]; 
            for (int currentBlock = 0;currentBlock<cubed*6;currentBlock++)
            {
                for(int i=0;i<6;i++)
                {
                    indicesArray[currentBlock*6+i]=currentBlock*4+indicesOffset[i%6];
                }       
            }
            indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indicesArray.Length, BufferUsage.None);
            indexBuffer.SetData(indicesArray);
        }
        public static void DrawChunkOpaque(Chunk chunk){
            if(chunk.vertexBufferOpaque is not null){
                graphicsDevice.SetVertexBuffer(chunk.vertexBufferOpaque);
                graphicsDevice.Indices = indexBuffer;
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, chunk.vertexBufferOpaque.VertexCount/2);
            }
        }
        public static void DrawChunkTransparent(Chunk chunk){
            if(chunk.vertexBufferTransparent is not null){
                graphicsDevice.SetVertexBuffer(chunk.vertexBufferTransparent);
                graphicsDevice.Indices = indexBuffer;
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, chunk.vertexBufferTransparent.VertexCount/2);
            }
        }
        static DynamicVertexBuffer cubeVertex;
        static IndexBuffer cubeIndices;
        static Vector2[] cubeFrameTextureCoordinates;
        static public void SetupCubeFrame(){
            short[] indices={
                0,1,2,1,3,2,
                4,5,6,5,7,6,
                8,9,10,9,11,10,
                12,13,14,13,15,14,
                16,17,18,17,19,18,
                20,21,22,21,23,22,
            };
            cubeIndices = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, 36, BufferUsage.None);
            cubeIndices.SetData(indices);
            cubeVertex = new DynamicVertexBuffer(graphicsDevice,typeof(VertexPositionTexture), 24, BufferUsage.None);
            cubeFrameTextureCoordinates = blockIds.GiveTextureVectorArrayById(15);
        }
        static public void DrawCubeFrame((int x,int y,int z) coordinates)
        {
            VertexPositionTexture[] cubeVertices = new VertexPositionTexture[24];
            for(int i=0;i<24;i++)
            {
                cubeVertices[i] = new VertexPositionTexture(new Vector3(coordinates.x+((offsetX&(1<<i))>>i),coordinates.y+((offsetY&(1<<i))>>i),coordinates.z+((offsetZ&(1<<i))>>i)),cubeFrameTextureCoordinates[i]);
            }
            cubeVertex.SetData(cubeVertices);
            graphicsDevice.SetVertexBuffer(cubeVertex);
            graphicsDevice.Indices = cubeIndices;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
        }
        static public void DrawCubePreview(byte id){
            VertexPositionTexture[] cubeVertices = new VertexPositionTexture[12];
            Vector2[] TextureCoordinates = blockIds.GiveTextureVectorArrayById(id);
            for(int i=0;i<4;i++){
                cubeVertices[i] = new VertexPositionTexture(new Vector3((offsetX&(1<<i))>>i,(offsetY&(1<<i))>>i,(offsetZ&(1<<i))>>i),TextureCoordinates[i]);
            }
            for(int i=8;i<12;i++){
                cubeVertices[i-4] = new VertexPositionTexture(new Vector3((offsetX&(1<<i))>>i,(offsetY&(1<<i))>>i,(offsetZ&(1<<i))>>i),TextureCoordinates[i]);
            }
            for(int i=16;i<20;i++){
                cubeVertices[i-8] = new VertexPositionTexture(new Vector3((offsetX&(1<<i))>>i,(offsetY&(1<<i))>>i,(offsetZ&(1<<i))>>i),TextureCoordinates[i]);
            }
            cubeVertex.SetData(cubeVertices);
            graphicsDevice.SetVertexBuffer(cubeVertex);
            graphicsDevice.Indices = cubeIndices;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 6);
        }
        public static Matrix BlockIcon(int x,int y,float scale){
            float aspectRatio = graphicsDevice.Viewport.AspectRatio * scale;
            float translateX = aspectRatio-(float)x / graphicsDevice.Viewport.Width * (aspectRatio + aspectRatio);
            float translateY = (float)y / graphicsDevice.Viewport.Height * (scale + scale) - scale;
            return Matrix.CreateOrthographicOffCenter(translateX-aspectRatio,aspectRatio+translateX,translateY-scale,scale+translateY,1f, 10f);
        }
        public enum BlockFace{Front,Back,Right,Left,Top,Bottom,None};
        public static BlockFace GetFace(Ray ray,BoundingBox box){
            float? distance = ray.Intersects(box);
            if(distance.HasValue)
            {
                Vector3 intersectionPoint = ray.Position + ray.Direction*distance.Value;
                Vector3 localIntersectionPoint = intersectionPoint - box.Min;

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