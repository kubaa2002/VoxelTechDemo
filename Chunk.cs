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
        //this only works for 64 blocks wide chunks
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
                    for(int x=0;x<ChunkSize;x++){
                        if(blocks[blockPossition]!=0){
                            byte currentBlockId = blocks[blockPossition];
                            //face x+
                            if (x != ChunkSize - 1){
                                if(currentBlockId != blocks[blockPossition+1] && Block.IsTransparent(blocks[blockPossition+1])){
                                    result[resultPossition] |= 1uL << x;
                                }
                            }
                            else{
                                if(currentBlockId != adjacentChunks[0].blocks[-ChunkSize+1+blockPossition]
                                && Block.IsTransparent(adjacentChunks[0].blocks[-ChunkSize+1+blockPossition])){
                                    result[resultPossition] |= 1uL << x;
                                }
                            }
                            // Face x-
                            if (x != 0){
                                if(currentBlockId != blocks[blockPossition-1] && Block.IsTransparent(blocks[blockPossition-1])){
                                    result[square + resultPossition] |= 1uL << x;
                                }
                            }
                            else{
                                if(currentBlockId != adjacentChunks[1].blocks[ChunkSize-1+blockPossition]
                                && Block.IsTransparent(adjacentChunks[1].blocks[ChunkSize-1+blockPossition])){
                                    result[square + resultPossition] |= 1uL << x;
                                }
                            }
                            // Face y+
                            if (y != ChunkSize - 1){
                                if(currentBlockId != blocks[blockPossition+ChunkSize] && Block.IsTransparent(blocks[blockPossition+ChunkSize])){
                                    result[2*square + resultPossition] |= 1uL << x;
                                }
                            }
                            else{
                                if(adjacentChunks[2] is not null){
                                    if(currentBlockId != adjacentChunks[2].blocks[ChunkSize-square+blockPossition]
                                    && Block.IsTransparent(adjacentChunks[2].blocks[ChunkSize-square+blockPossition])){
                                        result[2*square + resultPossition] |= 1uL << x;
                                    }
                                }
                            }
                            // Face y-
                            if (y != 0){
                                if(currentBlockId != blocks[blockPossition-ChunkSize] && Block.IsTransparent(blocks[blockPossition-ChunkSize])){
                                    result[3*square + resultPossition] |= 1uL << x;
                                }
                            }
                            else{
                                if(adjacentChunks[3] is not null){
                                    if(currentBlockId != adjacentChunks[3].blocks[square-ChunkSize+blockPossition]
                                    && Block.IsTransparent(adjacentChunks[3].blocks[square-ChunkSize+blockPossition])){
                                        result[3*square + resultPossition] |= 1uL << x;
                                    }
                                }
                            }
                            // Face z+
                            if (z != ChunkSize - 1){
                                if(currentBlockId != blocks[blockPossition+square] && Block.IsTransparent(blocks[blockPossition+square])){
                                    result[4*square + resultPossition] |= 1uL << x;
                                }
                            }
                            else{
                                if(currentBlockId != adjacentChunks[4].blocks[square-cubed+blockPossition]
                                && Block.IsTransparent(adjacentChunks[4].blocks[square-cubed+blockPossition])){
                                    result[4*square + resultPossition] |= 1uL << x;
                                }
                            }
                            // Face z-
                            if (z != 0 ){
                                if(currentBlockId != blocks[blockPossition-square] && Block.IsTransparent(blocks[blockPossition-square])){
                                    result[5*square + resultPossition] |= 1uL << x;
                                }  
                            }
                            else{
                                if(currentBlockId != adjacentChunks[5].blocks[cubed-square+blockPossition]
                                && Block.IsTransparent(adjacentChunks[5].blocks[cubed-square+blockPossition])){
                                    result[5*square + resultPossition] |= 1uL << x;
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