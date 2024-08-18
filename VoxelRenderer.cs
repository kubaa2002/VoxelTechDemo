using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VoxelTechDemo{
    public static class VoxelRenderer{
        static private GraphicsDevice graphicsDevice;
        static private readonly BlockIds blockIds = new();
        //z- z+ y- y+ x- x+
        const int offsetX = 0b1010_0101_1010_1010_0000_1111;
        const int offsetY = 0b1100_1100_0000_1111_1100_1100;
        const int offsetZ = 0b0000_1111_0011_1100_0101_1010;
        const int exponent = 6;
        //ChunkSize needs to be an power of 2. Works up to 64 (exponent = 6)
        public const int ChunkSize = 1<<exponent;
        public const int square = ChunkSize*ChunkSize;
        public const int cubed = ChunkSize*ChunkSize*ChunkSize;
        public static IndexBuffer indexBuffer;
        public static void InitializeVoxelRenderer(GraphicsDevice _graphicsDevice){
            graphicsDevice=_graphicsDevice;
            SetupCubeFrame();
            GenerateIndexBuffer();
        }
        static public void GenerateVertexVertices(Chunk chunk){
            VertexBuffer[] vertexBuffers = GenerateVertices(chunk);
            chunk.vertexBufferOpaque?.Dispose();
            chunk.vertexBufferOpaque = vertexBuffers[0];
            chunk.vertexBufferTransparent?.Dispose();
            chunk.vertexBufferTransparent = vertexBuffers[1];
        }
        public static VertexBuffer[] GenerateVertices(Chunk chunk){
            //TODO: Try to combine multiple chunks into single region to reduce number of world matrixes needed
            int CurrentChunkY = chunk.coordinates.y*ChunkSize;
            List<VertexPositionTexture> opaqueVertices = new();
            List<VertexPositionTexture> transparentVertices = new();
            Vector3 voxelPosition;
            ulong[] result = chunk.CheckAllChunkFacesIfNeeded();
            Vector2[] textureCoordinates;
            for(int face=0;face<6;face++){
                int currentBlock = 0;
                for(int i=face*square;i<(face+1)*square;i++){
                    if(result[i] != 0){
                        for(ulong j=1;j!=0;j<<=1){
                            if((result[i]&j)!=0){
                                textureCoordinates = blockIds.TextureDictionary[chunk.blocks[currentBlock]];
                                if(Block.IsTransparent(chunk.blocks[currentBlock])){
                                    for(int k=face*4;k<face*4+4;k++){
                                        voxelPosition.X = (currentBlock&(ChunkSize-1))+((offsetX&(1<<k))>>k);
                                        voxelPosition.Y = CurrentChunkY+((currentBlock&((ChunkSize-1)<<exponent))>>exponent)+((offsetY&(1<<k))>>k);
                                        voxelPosition.Z = ((currentBlock&((ChunkSize-1)<<(2*exponent)))>>(2*exponent))+((offsetZ&(1<<k))>>k);
                                        transparentVertices.Add(new VertexPositionTexture(voxelPosition, textureCoordinates[k]));
                                    }
                                }
                                else{
                                    for(int k=face*4;k<face*4+4;k++){
                                        voxelPosition.X = (currentBlock&(ChunkSize-1))+((offsetX&(1<<k))>>k);
                                        voxelPosition.Y = CurrentChunkY+((currentBlock&((ChunkSize-1)<<exponent))>>exponent)+((offsetY&(1<<k))>>k);
                                        voxelPosition.Z = ((currentBlock&((ChunkSize-1)<<(2*exponent)))>>(2*exponent))+((offsetZ&(1<<k))>>k);
                                        opaqueVertices.Add(new VertexPositionTexture(voxelPosition, textureCoordinates[k]));
                                    }
                                }
                                result[i]&=(~j);
                                if(result[i] == 0){
                                    currentBlock+=ChunkSize-(currentBlock%ChunkSize);
                                    break;
                                }
                            }
                            currentBlock++;
                        }
                    }
                    else{
                        currentBlock+=ChunkSize;
                    }
                }
            }
            VertexBuffer[] buffers = new VertexBuffer[2];
            if(opaqueVertices.Count != 0){
                VertexBuffer vertexBufferOpaque = new(graphicsDevice,typeof(VertexPositionTexture),opaqueVertices.Count,BufferUsage.None);
                vertexBufferOpaque.SetData(opaqueVertices.ToArray());
                buffers[0]=vertexBufferOpaque;
            }
            if(transparentVertices.Count != 0){
                VertexBuffer vertexBufferTransparent = new(graphicsDevice,typeof(VertexPositionTexture),transparentVertices.Count,BufferUsage.None);
                vertexBufferTransparent.SetData(transparentVertices.ToArray());
                buffers[1]=vertexBufferTransparent;
            }
            return buffers;
        }
        public static void GenerateIndexBuffer(){
            byte[] indicesOffset = new byte[]{0,1,2,1,3,2};
            int[] indicesArray = new int[cubed*36]; 
            for (int currentBlock = 0;currentBlock<cubed*6;currentBlock++){
                for(int i=0;i<6;i++){
                    indicesArray[currentBlock*6+i]=currentBlock*4+indicesOffset[i%6];
                }       
            }
            indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indicesArray.Length, BufferUsage.None);
            indexBuffer.SetData(indicesArray);
        }
        public static void DrawChunkOpaque(Chunk chunk){
            if(chunk.vertexBufferOpaque is not null){
                graphicsDevice.SetVertexBuffer(chunk.vertexBufferOpaque);
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, chunk.vertexBufferOpaque.VertexCount/2);
            }
        }
        public static void DrawChunkTransparent(Chunk chunk){
            if(chunk.vertexBufferTransparent is not null){
                graphicsDevice.SetVertexBuffer(chunk.vertexBufferTransparent);
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, chunk.vertexBufferTransparent.VertexCount/2);
            }
        }
        static VertexBuffer cubeFrameVertex;
        static VertexBuffer cubePreviewVertex;
        static public void SetupCubeFrame(){
            cubeFrameVertex = new VertexBuffer(graphicsDevice,typeof(VertexPositionTexture), 24, BufferUsage.None);
            cubePreviewVertex = new VertexBuffer(graphicsDevice,typeof(VertexPositionTexture), 12, BufferUsage.None);
            Vector2[] cubeFrameTextureCoordinates = blockIds.TextureDictionary[15];
            VertexPositionTexture[] cubeVertices = new VertexPositionTexture[24];
            for(int i=0;i<24;i++){
                cubeVertices[i] = new VertexPositionTexture(new Vector3(1.0025f*((offsetX&(1<<i))>>i)-0.00125f,1.0025f*((offsetY&(1<<i))>>i)-0.00125f,1.0025f*((offsetZ&(1<<i))>>i)-0.00125f),cubeFrameTextureCoordinates[i]);
            }
            cubeFrameVertex.SetData(cubeVertices);
        }
        static public void DrawCubeFrame(){
            graphicsDevice.SetVertexBuffer(cubeFrameVertex);
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
        }
        static public void ChangeCubePreview(byte Id){
            VertexPositionTexture[] cubeVerticesPreview = new VertexPositionTexture[12];
            Vector2[] TextureCoordinates = blockIds.TextureDictionary[Id];
            for(int i=0;i<4;i++){
                cubeVerticesPreview[i] = new VertexPositionTexture(new Vector3((offsetX&(1<<i))>>i,(offsetY&(1<<i))>>i,(offsetZ&(1<<i))>>i),TextureCoordinates[i]);
            }
            for(int i=8;i<12;i++){
                cubeVerticesPreview[i-4] = new VertexPositionTexture(new Vector3((offsetX&(1<<i))>>i,(offsetY&(1<<i))>>i,(offsetZ&(1<<i))>>i),TextureCoordinates[i]);
            }
            for(int i=16;i<20;i++){
                cubeVerticesPreview[i-8] = new VertexPositionTexture(new Vector3((offsetX&(1<<i))>>i,(offsetY&(1<<i))>>i,(offsetZ&(1<<i))>>i),TextureCoordinates[i]);
            }
            cubePreviewVertex.SetData(cubeVerticesPreview);
        }
        static public void DrawCubePreview(){
            graphicsDevice.SetVertexBuffer(cubePreviewVertex);
            //Indices have to be set because sprite batch resets it
            graphicsDevice.Indices = indexBuffer;
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