using System;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo{
    public class World{
        public ConcurrentDictionary<(int,int,int),Chunk> WorldMap = new();
        readonly long seed;
        //MaxHeight need to divisable by ChunkSize
        public const int MaxHeight = 512;
        public World(long seed){
            this.seed = seed;
        }
        public void SetBlock(Vector3 coords,(int, int, int) chunkCoordinate, byte Id, BlockFace blockSide, BoundingBox PlayerHitBox){
            switch(blockSide){
                case BlockFace.Right:
                    coords.X -= 1;
                    break;
                case BlockFace.Left:
                    coords.X += 1;
                    break;
                case BlockFace.Bottom:
                    coords.Y -= 1;
                    break;
                case BlockFace.Top:
                    coords.Y += 1;
                    break;
                case BlockFace.Front:
                    coords.Z -= 1;
                    break;
                case BlockFace.Back:
                    coords.Z += 1;
                    break;
            }
            if (!PlayerHitBox.Intersects(new BoundingBox(coords, coords+Vector3.One))) {
                SetBlock(coords, chunkCoordinate, Id);
            }
        }
        public void SetBlock(Vector3 coords, (int x,int y,int z) chunkCoordinate,byte Id){
            int x = (int)coords.X;
            int y = (int)coords.Y;
            int z = (int)coords.Z;
            NormalizeChunkCoordinates(ref x,ref y,ref z,ref chunkCoordinate);
            if(!WorldMap.TryGetValue(chunkCoordinate, out Chunk chunk)){
                return;
            }
            chunk.blocks[x+(y*ChunkSize)+(z*ChunkSizeSquared)]=Id;
            chunk.CheckMaxY(y);
            GenerateChunkMesh(chunk);
            if(x==0){
                if(WorldMap.TryGetValue((chunkCoordinate.x-1,chunkCoordinate.y,chunkCoordinate.z),out chunk)){
                    GenerateChunkMesh(chunk);
                }
            }
            if(x==ChunkSize-1){
                if(WorldMap.TryGetValue((chunkCoordinate.x+1,chunkCoordinate.y,chunkCoordinate.z),out chunk)){
                    GenerateChunkMesh(chunk);
                }
            }
            if(y==0){
                if(WorldMap.TryGetValue((chunkCoordinate.x,chunkCoordinate.y-1,chunkCoordinate.z),out chunk)){
                    GenerateChunkMesh(chunk);
                }
            }
            if(y==ChunkSize-1){
                if(WorldMap.TryGetValue((chunkCoordinate.x,chunkCoordinate.y+1,chunkCoordinate.z),out chunk)){
                    GenerateChunkMesh(chunk);
                }
            }
            if(z==0){
                if(WorldMap.TryGetValue((chunkCoordinate.x,chunkCoordinate.y,chunkCoordinate.z-1),out chunk)){
                    GenerateChunkMesh(chunk);
                }
            }
            if(z==ChunkSize-1){
                if(WorldMap.TryGetValue((chunkCoordinate.x,chunkCoordinate.y,chunkCoordinate.z+1),out chunk)){
                    GenerateChunkMesh(chunk);
                }
            }
        }
        public void SetBlockWithoutUpdating(int x,int y,int z,(int x,int y,int z) chunkCoordinate,byte Id){
            NormalizeChunkCoordinates(ref x,ref y,ref z,ref chunkCoordinate);
            if(WorldMap.TryGetValue(chunkCoordinate,out Chunk chunk)){
                chunk.blocks[x+y*ChunkSize+z*ChunkSizeSquared]=Id;
            }
            else{
                WorldMap.TryAdd(chunkCoordinate,new(chunkCoordinate,this));
                chunk = WorldMap[chunkCoordinate];
                chunk.blocks[x+y*ChunkSize+z*ChunkSizeSquared]=Id;
            }
            chunk.CheckMaxY(y);
        }
        public byte GetBlock(int x,int y,int z, (int x,int y,int z) chunkCoordinate){
            NormalizeChunkCoordinates(ref x,ref y,ref z,ref chunkCoordinate);
            if(WorldMap.TryGetValue(chunkCoordinate, out Chunk chunk)){
                return chunk.blocks[x+(y*ChunkSize)+(z*ChunkSizeSquared)];
            }
            else{
                return 0;
            }
        }
        public void GenerateChunkLine(int x,int z){
            for(int y=0;y<MaxHeight/ChunkSize;y++){
                WorldMap.TryAdd((x,y,z),new((x,y,z),this));
            }
            GenerateTerrain(x,z);
        }
        public void GenerateTerrain(int chunkX,int chunkZ){
            Chunk[] chunks = new Chunk[MaxHeight/ChunkSize];
            for(int y=0;y<MaxHeight/ChunkSize;y++){
                chunks[y]=WorldMap[(chunkX,y,chunkZ)];
            }
            for(int x=0;x<ChunkSize;x++){
                for(int z=0;z<ChunkSize;z++){
                    // If yLevel below 0 needs to be generated, MountainNoise needs to floored before casting to int
                    int yLevel = 50+(int)MountainNoise((double)chunkX*ChunkSize+x,(double)chunkZ*ChunkSize+z);
                    int blockPossition = x+yLevel%ChunkSize*ChunkSize+z*ChunkSizeSquared;
                    byte[] chunkBlocks = chunks[yLevel/ChunkSize].blocks;
                    chunks[yLevel/ChunkSize].CheckMaxY(yLevel%ChunkSize);
                    // Water level
                    if(yLevel < 63){
                        for(int y=63;y>yLevel;y--){
                            if(y%ChunkSize==ChunkSize-1){
                                chunkBlocks = chunks[y/ChunkSize].blocks;
                                chunks[y/ChunkSize].maxY=ChunkSize;
                                blockPossition = x+ChunkSizeSquared-ChunkSize+z*ChunkSizeSquared;
                            }
                            chunkBlocks[blockPossition]=255;
                            blockPossition-=ChunkSize;
                        }
                    }
                    // Dirt level
                    int stoneNoise = (int)(OpenSimplex2.Noise2(seed,(double)chunkX*ChunkSize+x,(double)chunkZ*ChunkSize+z)*5f);
                    stoneNoise += (int)(OpenSimplex2.Noise2(seed,((double)chunkX*ChunkSize+x)/100,((double)chunkZ*ChunkSize+z)/100)*20f);
                    for(int y=yLevel;y>=yLevel-2;y--){
                        if(y%ChunkSize==ChunkSize-1){
                            chunkBlocks = chunks[y/ChunkSize].blocks;
                            chunks[y/ChunkSize].maxY=ChunkSize;
                            blockPossition = x+ChunkSizeSquared-ChunkSize+z*ChunkSizeSquared;
                        }
                        if(y == yLevel){
                            if(y>=65){
                                if(y>=238+stoneNoise){
                                    if(y>=258+stoneNoise){
                                        // Snow
                                        chunkBlocks[blockPossition]=13;
                                    }
                                    else{
                                        // Stone
                                        chunkBlocks[blockPossition]=3;
                                    }
                                }
                                else{
                                    // Grass
                                    chunkBlocks[blockPossition]=1;
                                }
                            }
                            else{
                                if(yLevel<62){
                                    // Gravel
                                    chunkBlocks[blockPossition]=11;
                                }
                                else{
                                    // Sand
                                    chunkBlocks[blockPossition]=12;
                                }
                            }
                        }
                        else{
                            if(y>=238+stoneNoise){
                                // Stone
                                chunkBlocks[blockPossition]=3;
                            }
                            else{
                                // Dirt
                                chunkBlocks[blockPossition]=2;
                            }
                        }
                        blockPossition-=ChunkSize;
                    }
                    // Stone level
                    for(int y=yLevel-3;y>=0;y--){
                        if(y%ChunkSize==ChunkSize-1){
                            chunkBlocks = chunks[y/ChunkSize].blocks;
                            chunks[y/ChunkSize].maxY=ChunkSize;
                            blockPossition += ChunkSizeSquared;
                        }
                        chunkBlocks[blockPossition]=3;
                        blockPossition-=ChunkSize;
                    }
                    if(OpenSimplex2.Noise2(seed,(double)chunkX*ChunkSize+x,(double)chunkZ*ChunkSize+z) > 0.9f + 0.0004f*yLevel && yLevel>=65){
                        CreateTree(x,yLevel%ChunkSize,z, chunks[yLevel/ChunkSize]);
                    }
                }
            }
            for(int i=0;i<MaxHeight/ChunkSize;i++){
                chunks[i].IsGenerated = true;
            }
        }
        // TOFIX: On big x and z coordinates (int.MaxValue/64) trees don't spawn
        // TOFIX: When tree spawns on chunks corner some leaves will not be in the mesh
        private void CreateTree(int x,int y, int z, Chunk chunk){
            for(int tempy=4;tempy<=5;tempy++){
                for(int tempx=-2;tempx<=2;tempx++){
                    for(int tempz=-2;tempz<=2;tempz++){
                        if(!((tempx == -2 || tempx == 2)&&(tempz == -2 || tempz == 2))){
                            if(GetBlock(x+tempx,y+tempy,z+tempz,chunk.coordinates)==0){
                                SetBlockWithoutUpdating(x+tempx,y+tempy,z+tempz,chunk.coordinates,7);
                            }
                        }
                    }
                }
            }
            for(int tempy=6;tempy<=7;tempy++){
                for(int tempx=-1;tempx<=1;tempx++){
                    for(int tempz=-1;tempz<=1;tempz++){
                        if(!((tempx == -1 || tempx == 1)&&(tempz == -1 || tempz == 1))){
                            if(GetBlock(x+tempx,y+tempy,z+tempz,chunk.coordinates)==0){
                                SetBlockWithoutUpdating(x+tempx,y+tempy,z+tempz,chunk.coordinates,7);
                            }
                        }
                    }
                }
            }
            for(int i=1;i<=6;i++){
                SetBlockWithoutUpdating(x,y+i,z,chunk.coordinates,5);
            }
            SetBlockWithoutUpdating(x,y,z,chunk.coordinates,2);
        }
        public double MountainNoise(double x,double z){
            return Math.Pow(OpenSimplex2.Noise2(seed,x/2000,z/2000)*15
                        + OpenSimplex2.Noise2(seed,x/400,z/400)*4
                        + OpenSimplex2.Noise2(seed,x/100,z/100),2);
        }
        static void NormalizeChunkCoordinates(ref int x,ref int y,ref int z,ref (int x,int y,int z) chunkCoordinate){
            while(x>ChunkSize-1){
                chunkCoordinate.x+=1;
                x-=ChunkSize;
            }
            while(x<0){
                chunkCoordinate.x-=1;
                x+=ChunkSize;
            }
            while(y>ChunkSize-1){
                chunkCoordinate.y+=1;
                y-=ChunkSize;
            }
            while(y<0){
                chunkCoordinate.y-=1;
                y+=ChunkSize;
            }
            while(z>ChunkSize-1){
                chunkCoordinate.z+=1;
                z-=ChunkSize;
            }
            while(z<0){
                chunkCoordinate.z-=1;
                z+=ChunkSize;
            }
        }
    }
}