using Microsoft.Xna.Framework.Graphics;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo{
    public class Chunk{
        public byte[] blocks = new byte[ChunkSizeCubed];
        public (int x,int y,int z) coordinates;
        readonly World world;
        public VertexBuffer vertexBufferOpaque;
        public VertexBuffer vertexBufferTransparent;
        public bool IsGenerated = false;
        public byte maxY = 0;
        public Chunk((int x,int y,int z) coordinates, World world){
            this.coordinates = coordinates;
            this.world = world;
        }
        public ulong[] CheckAllChunkFacesIfNeeded(){
            //face x+ x- y+ y- z+ z- depth 0-63
            ulong[] result = new ulong[ChunkSizeSquared*6];
            Chunk[] adjacentChunks = new Chunk[6];
            world.WorldMap.TryGetValue((coordinates.x+1,coordinates.y,coordinates.z),out adjacentChunks[0]);
            world.WorldMap.TryGetValue((coordinates.x-1,coordinates.y,coordinates.z),out adjacentChunks[1]);
            world.WorldMap.TryGetValue((coordinates.x,coordinates.y+1,coordinates.z),out adjacentChunks[2]);
            world.WorldMap.TryGetValue((coordinates.x,coordinates.y-1,coordinates.z),out adjacentChunks[3]);
            world.WorldMap.TryGetValue((coordinates.x,coordinates.y,coordinates.z+1),out adjacentChunks[4]);
            world.WorldMap.TryGetValue((coordinates.x,coordinates.y,coordinates.z-1),out adjacentChunks[5]);
            for(int z=0;z<ChunkSize;z++){
                int blockPossition = z*ChunkSizeSquared;
                int resultPossition = z*ChunkSize;
                for(int y=0;y<maxY;y++){
                    for(ulong x=1;x!=(ChunkSize == 64 ? 0 : 1uL << ChunkSize);x<<=1){
                        if(blocks[blockPossition]!=0){
                            byte currentBlockId = blocks[blockPossition];
                            //face x+
                            if (x != 1uL<<(ChunkSize-1)){
                                if(currentBlockId != blocks[blockPossition+1] && Blocks.IsTransparent(blocks[blockPossition+1])){
                                    result[resultPossition] |= x;
                                }
                            }
                            else{
                                if(currentBlockId != adjacentChunks[0].blocks[-ChunkSize+1+blockPossition]
                                && Blocks.IsTransparent(adjacentChunks[0].blocks[-ChunkSize+1+blockPossition])){
                                    result[resultPossition] |= x;
                                }
                            }
                            // Face x-
                            if (x != 1){
                                if(currentBlockId != blocks[blockPossition-1] && Blocks.IsTransparent(blocks[blockPossition-1])){
                                    result[ChunkSizeSquared + resultPossition] |= x;
                                }
                            }
                            else{
                                if(currentBlockId != adjacentChunks[1].blocks[ChunkSize-1+blockPossition]
                                && Blocks.IsTransparent(adjacentChunks[1].blocks[ChunkSize-1+blockPossition])){
                                    result[ChunkSizeSquared + resultPossition] |= x;
                                }
                            }
                            // Face y+
                            if (y != ChunkSize - 1){
                                if(currentBlockId != blocks[blockPossition+ChunkSize] && Blocks.IsTransparent(blocks[blockPossition+ChunkSize])){
                                    result[2*ChunkSizeSquared + resultPossition] |= x;
                                }
                            }
                            else{
                                if(adjacentChunks[2] is not null){
                                    if(currentBlockId != adjacentChunks[2].blocks[ChunkSize-ChunkSizeSquared+blockPossition]
                                    && Blocks.IsTransparent(adjacentChunks[2].blocks[ChunkSize-ChunkSizeSquared+blockPossition])){
                                        result[2*ChunkSizeSquared + resultPossition] |= x;
                                    }
                                }
                            }
                            // Face y-
                            if (y != 0){
                                if(currentBlockId != blocks[blockPossition-ChunkSize] && Blocks.IsTransparent(blocks[blockPossition-ChunkSize])){
                                    result[3*ChunkSizeSquared + resultPossition] |= x;
                                }
                            }
                            else{
                                if(adjacentChunks[3] is not null){
                                    if(currentBlockId != adjacentChunks[3].blocks[ChunkSizeSquared-ChunkSize+blockPossition]
                                    && Blocks.IsTransparent(adjacentChunks[3].blocks[ChunkSizeSquared-ChunkSize+blockPossition])){
                                        result[3*ChunkSizeSquared + resultPossition] |= x;
                                    }
                                }
                            }
                            // Face z+
                            if (z != ChunkSize - 1){
                                if(currentBlockId != blocks[blockPossition+ChunkSizeSquared] && Blocks.IsTransparent(blocks[blockPossition+ChunkSizeSquared])){
                                    result[4*ChunkSizeSquared + resultPossition] |= x;
                                }
                            }
                            else{
                                if(currentBlockId != adjacentChunks[4].blocks[ChunkSizeSquared-ChunkSizeCubed+blockPossition]
                                && Blocks.IsTransparent(adjacentChunks[4].blocks[ChunkSizeSquared-ChunkSizeCubed+blockPossition])){
                                    result[4*ChunkSizeSquared + resultPossition] |= x;
                                }
                            }
                            // Face z-
                            if (z != 0 ){
                                if(currentBlockId != blocks[blockPossition-ChunkSizeSquared] && Blocks.IsTransparent(blocks[blockPossition-ChunkSizeSquared])){
                                    result[5*ChunkSizeSquared + resultPossition] |= x;
                                }  
                            }
                            else{
                                if(currentBlockId != adjacentChunks[5].blocks[ChunkSizeCubed-ChunkSizeSquared+blockPossition]
                                && Blocks.IsTransparent(adjacentChunks[5].blocks[ChunkSizeCubed-ChunkSizeSquared+blockPossition])){
                                    result[5*ChunkSizeSquared + resultPossition] |= x;
                                }
                            }
                        }
                        blockPossition++;
                    }
                    resultPossition++;
                }
            }
            return result; 
        }
        public void CheckMaxY(int y){
            y++;
            if(y>maxY){
                maxY = (byte)y;
            }
        }
    }
}