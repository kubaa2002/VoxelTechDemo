using Microsoft.Xna.Framework.Graphics;

namespace VoxelTechDemo{
    public class Chunk{
        public byte[] blocks = new byte[VoxelRenderer.cubed];
        public (int x,int y,int z) coordinates;
        readonly World world;
        public VertexBuffer vertexBufferOpaque;
        public VertexBuffer vertexBufferTransparent;
        public bool IsGenerated = false;
        public Chunk((int x,int y,int z) coordinates, World world){
            this.coordinates = coordinates;
            this.world = world;
        }
        //this only works for 64 blocks wide chunks
        public ulong[] CheckAllChunkFacesIfNeeded(){
            //face x+ x- y+ y- z+ z- depth 0-63
            ulong[] result = new ulong[24576];
            Chunk[] adjacentChunks = new Chunk[6];
            world.WorldMap.TryGetValue(coordinates with{x=coordinates.x+1},out adjacentChunks[0]);
            world.WorldMap.TryGetValue(coordinates with{x=coordinates.x-1},out adjacentChunks[1]);
            world.WorldMap.TryGetValue(coordinates with{y=coordinates.y+1},out adjacentChunks[2]);
            world.WorldMap.TryGetValue(coordinates with{y=coordinates.y-1},out adjacentChunks[3]);
            world.WorldMap.TryGetValue(coordinates with{z=coordinates.z+1},out adjacentChunks[4]);
            world.WorldMap.TryGetValue(coordinates with{z=coordinates.z-1},out adjacentChunks[5]);
            int blockPossition = 0, resultPossition = 0;
            byte currentBlockId;
            for(int z=0;z<64;z++){
                for(int y=0;y<64;y++){
                    for(int x=0;x<64;x++){
                        if(blocks[blockPossition]!=0){
                            currentBlockId = blocks[blockPossition];
                            //face x+
                            if (x != 63){
                                if(currentBlockId != blocks[blockPossition+1] && Block.IsTransparent(blocks[blockPossition+1])){
                                    result[resultPossition] |= 1uL << x;
                                }
                            }
                            else{
                                if(currentBlockId != adjacentChunks[0].blocks[blockPossition-63]
                                && Block.IsTransparent(adjacentChunks[0].blocks[blockPossition-63])){
                                    result[resultPossition] |= 1uL << x;
                                }
                            }
                            // Face x-
                            if (x != 0){
                                if(currentBlockId != blocks[blockPossition-1] && Block.IsTransparent(blocks[blockPossition-1])){
                                    result[4096 + resultPossition] |= 1uL << x;
                                }
                            }
                            else{
                                if(currentBlockId != adjacentChunks[1].blocks[blockPossition+63]
                                && Block.IsTransparent(adjacentChunks[1].blocks[blockPossition+63])){
                                    result[4096 + resultPossition] |= 1uL << x;
                                }
                            }
                            // Face y+
                            if (y != 63){
                                if(currentBlockId != blocks[blockPossition+64] && Block.IsTransparent(blocks[blockPossition+64])){
                                    result[8192 + resultPossition] |= 1uL << x;
                                }
                            }
                            else{
                                if(adjacentChunks[2] is not null){
                                    if(currentBlockId != adjacentChunks[2].blocks[blockPossition-4032]
                                    && Block.IsTransparent(adjacentChunks[2].blocks[blockPossition-4032])){
                                        result[8192 + resultPossition] |= 1uL << x;
                                    }
                                }
                            }
                            // Face y-
                            if (y != 0){
                                if(currentBlockId != blocks[blockPossition-64] && Block.IsTransparent(blocks[blockPossition-64])){
                                    result[12288 + resultPossition] |= 1uL << x;
                                }
                            }
                            else{
                                if(adjacentChunks[3] is not null){
                                    if(currentBlockId != adjacentChunks[3].blocks[blockPossition+4032]
                                    && Block.IsTransparent(adjacentChunks[3].blocks[blockPossition+4032])){
                                        result[12288 + resultPossition] |= 1uL << x;
                                    }
                                }
                            }
                            // Face z+
                            if (z != 63){
                                if(currentBlockId != blocks[blockPossition+4096] && Block.IsTransparent(blocks[blockPossition+4096])){
                                    result[16384 + resultPossition] |= 1uL << x;
                                }
                            }
                            else{
                                if(currentBlockId != adjacentChunks[4].blocks[blockPossition-258048]
                                && Block.IsTransparent(adjacentChunks[4].blocks[blockPossition-258048])){
                                    result[16384 + resultPossition] |= 1uL << x;
                                }
                            }
                            // Face z-
                            if (z != 0 ){
                                if(currentBlockId != blocks[blockPossition-4096] && Block.IsTransparent(blocks[blockPossition-4096])){
                                    result[20480 + resultPossition] |= 1uL << x;
                                }  
                            }
                            else{
                                if(currentBlockId != adjacentChunks[5].blocks[blockPossition+258048]
                                && Block.IsTransparent(adjacentChunks[5].blocks[blockPossition+258048])){
                                    result[20480 + resultPossition] |= 1uL << x;
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