using Microsoft.Xna.Framework.Graphics;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo{
    public class Chunk{
        public byte[] blocks = new byte[cubed];
        public (int x,int y,int z) coordinates;
        readonly World world;
        public VertexBuffer vertexBufferOpaque;
        public VertexBuffer vertexBufferTransparent;
        public bool IsGenerated = false;
        public Chunk((int x,int y,int z) coordinates, World world){
            this.coordinates = coordinates;
            this.world = world;
        }
        public ulong[] CheckAllChunkFacesIfNeeded(){
            //face x+ x- y+ y- z+ z- depth 0-63
            ulong[] result = new ulong[square*6];
            Chunk[] adjacentChunks = new Chunk[6];
            world.WorldMap.TryGetValue((coordinates.x+1,coordinates.y,coordinates.z),out adjacentChunks[0]);
            world.WorldMap.TryGetValue((coordinates.x-1,coordinates.y,coordinates.z),out adjacentChunks[1]);
            world.WorldMap.TryGetValue((coordinates.x,coordinates.y+1,coordinates.z),out adjacentChunks[2]);
            world.WorldMap.TryGetValue((coordinates.x,coordinates.y-1,coordinates.z),out adjacentChunks[3]);
            world.WorldMap.TryGetValue((coordinates.x,coordinates.y,coordinates.z+1),out adjacentChunks[4]);
            world.WorldMap.TryGetValue((coordinates.x,coordinates.y,coordinates.z-1),out adjacentChunks[5]);
            int blockPossition = 0, resultPossition = 0;
            for(int z=0;z<ChunkSize;z++){
                for(int y=0;y<ChunkSize;y++){
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
                                    result[square + resultPossition] |= x;
                                }
                            }
                            else{
                                if(currentBlockId != adjacentChunks[1].blocks[ChunkSize-1+blockPossition]
                                && Blocks.IsTransparent(adjacentChunks[1].blocks[ChunkSize-1+blockPossition])){
                                    result[square + resultPossition] |= x;
                                }
                            }
                            // Face y+
                            if (y != ChunkSize - 1){
                                if(currentBlockId != blocks[blockPossition+ChunkSize] && Blocks.IsTransparent(blocks[blockPossition+ChunkSize])){
                                    result[2*square + resultPossition] |= x;
                                }
                            }
                            else{
                                if(adjacentChunks[2] is not null){
                                    if(currentBlockId != adjacentChunks[2].blocks[ChunkSize-square+blockPossition]
                                    && Blocks.IsTransparent(adjacentChunks[2].blocks[ChunkSize-square+blockPossition])){
                                        result[2*square + resultPossition] |= x;
                                    }
                                }
                            }
                            // Face y-
                            if (y != 0){
                                if(currentBlockId != blocks[blockPossition-ChunkSize] && Blocks.IsTransparent(blocks[blockPossition-ChunkSize])){
                                    result[3*square + resultPossition] |= x;
                                }
                            }
                            else{
                                if(adjacentChunks[3] is not null){
                                    if(currentBlockId != adjacentChunks[3].blocks[square-ChunkSize+blockPossition]
                                    && Blocks.IsTransparent(adjacentChunks[3].blocks[square-ChunkSize+blockPossition])){
                                        result[3*square + resultPossition] |= x;
                                    }
                                }
                            }
                            // Face z+
                            if (z != ChunkSize - 1){
                                if(currentBlockId != blocks[blockPossition+square] && Blocks.IsTransparent(blocks[blockPossition+square])){
                                    result[4*square + resultPossition] |= x;
                                }
                            }
                            else{
                                if(currentBlockId != adjacentChunks[4].blocks[square-cubed+blockPossition]
                                && Blocks.IsTransparent(adjacentChunks[4].blocks[square-cubed+blockPossition])){
                                    result[4*square + resultPossition] |= x;
                                }
                            }
                            // Face z-
                            if (z != 0 ){
                                if(currentBlockId != blocks[blockPossition-square] && Blocks.IsTransparent(blocks[blockPossition-square])){
                                    result[5*square + resultPossition] |= x;
                                }  
                            }
                            else{
                                if(currentBlockId != adjacentChunks[5].blocks[cubed-square+blockPossition]
                                && Blocks.IsTransparent(adjacentChunks[5].blocks[cubed-square+blockPossition])){
                                    result[5*square + resultPossition] |= x;
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
    }
}