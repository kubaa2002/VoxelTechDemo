using static VoxelTechDemo.VoxelRenderer;
using System.Linq;
using Microsoft.Xna.Framework;

namespace VoxelTechDemo;

public static class TerrainGen {
    public static long seed = 12345;
    public static Chunk GenerateTerrain(World world, int chunkX,int chunkZ){
        int[] yLevels = new int[ChunkSizeSquared];
        for (int x = 0; x < ChunkSize; x++) {
            for (int z = 0; z < ChunkSize; z++) {
                // If yLevel below 0 needs to be generated, noise needs to floored before casting to int
                yLevels[x+z*ChunkSize] = (int)TerrainNoise((double)chunkX*ChunkSize+x,(double)chunkZ*ChunkSize+z);
            }
        }

        int highestChunk = yLevels.Max()/ChunkSize;
        Chunk[] chunks = new Chunk[highestChunk+1];
        for(int y=0;y<=highestChunk;y++) {
            world.WorldMap.TryAdd((chunkX,y,chunkZ),new((chunkX, y, chunkZ), world));
            chunks[y] = world.WorldMap[(chunkX,y,chunkZ)];
        }
        for(int x=0;x<ChunkSize;x++){
            for(int z=0;z<ChunkSize;z++){
                int yLevel = yLevels[x+z*ChunkSize];
                double temperature = OpenSimplex2.Noise2(seed+1, (double)(chunkX * ChunkSize + x)/1000, (double)(chunkZ * ChunkSize + z)/1000);
                temperature += OpenSimplex2.Noise2(seed+1, (double)(chunkX * ChunkSize + x)/10, (double)(chunkZ * ChunkSize + z)/10)/100;
                if (temperature < -0.4f) TundraBiome(world, yLevel, x, z, chunks, chunkX, chunkZ);
                else if (temperature > 0.4f) DesertBiome(world, yLevel, x, z, chunks, chunkX, chunkZ);
                else PlainsBiome(world, yLevel, x, z, chunks, chunkX, chunkZ);
            }
        }

        return chunks[^1];
    }

    private static void TundraBiome(World world, int yLevel, int x, int z, Chunk[] chunks, int chunkX, int chunkZ) {
        int blockPosition = x+yLevel%ChunkSize*ChunkSize+z*ChunkSizeSquared;
        byte[] chunkBlocks = chunks[yLevel/ChunkSize].blocks;
       
        // Water level
        if(yLevel < 63){
            chunkBlocks = chunks[0].blocks;
            blockPosition = x+ChunkSizeSquared-ChunkSize+z*ChunkSizeSquared;
            chunkBlocks[blockPosition] = 22;
            blockPosition -= ChunkSize;
            for(int y=62;y>yLevel;y--){
                chunkBlocks[blockPosition]=15;
                blockPosition-=ChunkSize;
            }
        }
        // Dirt level
        for(int y=yLevel;y>=yLevel-1;y--){
            if(y%ChunkSize==ChunkSize-1){
                chunkBlocks = chunks[y/ChunkSize].blocks;
                blockPosition = x+ChunkSizeSquared-ChunkSize+z*ChunkSizeSquared;
            }
            if(yLevel<62){
                // Gravel
                chunkBlocks[blockPosition]=11;
            }
            else{
                // Snow
                chunkBlocks[blockPosition]=13;
            }
            blockPosition-=ChunkSize;
        }
        // Stone level
        for(int y=yLevel-2;y>=0;y--){
            if(y%ChunkSize==ChunkSize-1){
                chunkBlocks = chunks[y/ChunkSize].blocks;
                blockPosition += ChunkSizeSquared;
            }
            chunkBlocks[blockPosition]=3;
            blockPosition-=ChunkSize;
        }
        
        double foliageNoise = OpenSimplex2.Noise2(seed, (double)chunkX * ChunkSize + x,
            (double)chunkZ * ChunkSize + z);
        
        if(foliageNoise > 0.9f + 0.0004f*yLevel && yLevel>=65){
            CreateSpruceTree(world, x,yLevel%ChunkSize,z, chunks[yLevel/ChunkSize]);
        }
        
        if (foliageNoise < -0.5f && yLevel >= 65) {
            if (world.GetBlock(x,yLevel,z,(chunkX,0,chunkZ)) == 13) {
                world.SetBlockWithoutUpdating(x,yLevel+1,z,(chunkX,0,chunkZ),23);
            }
        }
    }
    private static void PlainsBiome(World world, int yLevel, int x, int z, Chunk[] chunks, int chunkX, int chunkZ) {
        int blockPosition = x+yLevel%ChunkSize*ChunkSize+z*ChunkSizeSquared;
        byte[] chunkBlocks = chunks[yLevel/ChunkSize].blocks;
       
        // Water level
        if(yLevel < 63){
            chunkBlocks = chunks[0].blocks;
            blockPosition = x+ChunkSizeSquared-ChunkSize+z*ChunkSizeSquared;
            for(int y=63;y>yLevel;y--){
                chunkBlocks[blockPosition]=15;
                blockPosition-=ChunkSize;
            }
        }
        // Dirt level
        int stoneNoise = (int)(OpenSimplex2.Noise2(seed,(double)chunkX*ChunkSize+x,(double)chunkZ*ChunkSize+z)*5f);
        stoneNoise += (int)(OpenSimplex2.Noise2(seed,((double)chunkX*ChunkSize+x)/400,((double)chunkZ*ChunkSize+z)/400)*20f);
        for(int y=yLevel;y>=yLevel-2;y--){
            if(y%ChunkSize==ChunkSize-1){
                chunkBlocks = chunks[y/ChunkSize].blocks;
                blockPosition = x+ChunkSizeSquared-ChunkSize+z*ChunkSizeSquared;
            }
            if(y == yLevel){
                if(y>=65){
                    if(y>=158+stoneNoise){
                        if(y>=178+stoneNoise){
                            // Snow
                            chunkBlocks[blockPosition]=13;
                        }
                        else{
                            // Stone
                            chunkBlocks[blockPosition]=3;
                        }
                    }
                    else{
                        // Grass
                        chunkBlocks[blockPosition]=1;
                    }
                }
                else{
                    if(yLevel<62){
                        // Gravel
                        chunkBlocks[blockPosition]=11;
                    }
                    else{
                        // Sand
                        chunkBlocks[blockPosition]=12;
                    }
                }
            }
            else{
                if(y>=158+stoneNoise){
                    // Stone
                    chunkBlocks[blockPosition]=3;
                }
                else{
                    // Dirt
                    chunkBlocks[blockPosition]=2;
                }
            }
            blockPosition-=ChunkSize;
        }
        // Stone level
        for(int y=yLevel-3;y>=0;y--){
            if(y%ChunkSize==ChunkSize-1){
                chunkBlocks = chunks[y/ChunkSize].blocks;
                blockPosition += ChunkSizeSquared;
            }
            chunkBlocks[blockPosition]=3;
            blockPosition-=ChunkSize;
        }
        double foliageNoise = OpenSimplex2.Noise2(seed, (double)chunkX * ChunkSize + x,
            (double)chunkZ * ChunkSize + z);
        
        if(foliageNoise > 0.9f + 0.0004f*yLevel && world.GetBlock(x,yLevel,z,(chunkX,0,chunkZ)) == 1) {
            CreateTree(world, x,yLevel%ChunkSize,z, chunks[yLevel/ChunkSize]);
        }
        
        if (foliageNoise < -0.5f && yLevel >= 65) {
            if (world.GetBlock(x,yLevel,z,(chunkX,0,chunkZ)) == 1) {
                if (foliageNoise > -0.525f) {
                    world.SetBlockWithoutUpdating(x, yLevel + 1, z, (chunkX, 0, chunkZ),
                        foliageNoise > -0.5125f ? (byte)20 : (byte)21);
                }
                else {
                    world.SetBlockWithoutUpdating(x,yLevel+1,z,(chunkX,0,chunkZ),19);
                }
            }
        }
    }

    private static void DesertBiome(World world, int yLevel, int x, int z, Chunk[] chunks, int chunkX, int chunkZ) {
        int blockPosition = x + yLevel % ChunkSize * ChunkSize + z * ChunkSizeSquared;
        byte[] chunkBlocks = chunks[yLevel / ChunkSize].blocks;

        // Water level
        if (yLevel < 63) {
            chunkBlocks = chunks[0].blocks;
            blockPosition = x + ChunkSizeSquared - ChunkSize + z * ChunkSizeSquared;
            for (int y = 63; y > yLevel; y--) {
                chunkBlocks[blockPosition] = 15;
                blockPosition -= ChunkSize;
            }
        }

        // Dirt level
        int stoneNoise =
            (int)(OpenSimplex2.Noise2(seed, (double)chunkX * ChunkSize + x, (double)chunkZ * ChunkSize + z) * 5f);
        stoneNoise += (int)(OpenSimplex2.Noise2(seed, ((double)chunkX * ChunkSize + x) / 400,
            ((double)chunkZ * ChunkSize + z) / 400) * 20f);
        for (int y = yLevel; y >= yLevel - 2; y--) {
            if (y % ChunkSize == ChunkSize - 1) {
                chunkBlocks = chunks[y / ChunkSize].blocks;
                blockPosition = x + ChunkSizeSquared - ChunkSize + z * ChunkSizeSquared;
            }

            if (y == yLevel) {
                if (y >= 158 + stoneNoise) {
                    if(y>= 188 + stoneNoise){
                        // Snow
                        chunkBlocks[blockPosition]=13;
                    }
                    else{
                        // Stone
                        chunkBlocks[blockPosition]=3;
                    }
                }
                else {
                    if (yLevel < 62) {
                        // Gravel
                        chunkBlocks[blockPosition] = 11;
                    }
                    else {
                        // Sand
                        chunkBlocks[blockPosition] = 12;
                    }
                }
            }
            else {
                if (yLevel >= 158 + stoneNoise) {
                    // Stone
                    chunkBlocks[blockPosition] = 3;
                }
                else {
                    // Sand
                    chunkBlocks[blockPosition] = 12;
                }
            }

            blockPosition -= ChunkSize;
        }

        // Stone level
        for (int y = yLevel - 3; y >= 0; y--) {
            if (y % ChunkSize == ChunkSize - 1) {
                chunkBlocks = chunks[y / ChunkSize].blocks;
                blockPosition += ChunkSizeSquared;
            }

            chunkBlocks[blockPosition] = 3;
            blockPosition -= ChunkSize;
        }

        double foliageNoise = OpenSimplex2.Noise2(seed, (double)chunkX * ChunkSize + x,
            (double)chunkZ * ChunkSize + z);

        if (foliageNoise > 0.98f && yLevel >= 63 && world.GetBlock(x, yLevel, z, (chunkX, 0, chunkZ)) == 12) {
            for (int y = 1; y <= 3; y++) {
                world.SetBlockWithoutUpdating(x,yLevel+y,z,(chunkX,0,chunkZ),28);
            }
        }
        if (foliageNoise < -0.95f && yLevel >= 63 && world.GetBlock(x, yLevel, z, (chunkX, 0, chunkZ)) == 12) {
            world.SetBlockWithoutUpdating(x, yLevel + 1, z, (chunkX, 0, chunkZ), 27);
        }
    }
    // TODO: On big x and z coordinates (int.MaxValue/64) trees don't spawn
    // TODO: When tree spawns on chunks corner some leaves will not be in the mesh
    private static void CreateTree(World world, int x,int y, int z, Chunk chunk){
        for(int tempy=4;tempy<=5;tempy++){
            for(int tempx=-2;tempx<=2;tempx++){
                for(int tempz=-2;tempz<=2;tempz++){
                    if(!((tempx == -2 || tempx == 2)&&(tempz == -2 || tempz == 2))){
                        if(world.GetBlock(x+tempx,y+tempy,z+tempz,chunk.coordinates)==0){
                            world.SetBlockWithoutUpdating(x+tempx,y+tempy,z+tempz,chunk.coordinates,7);
                        }
                    }
                }
            }
        }
        for(int tempy=6;tempy<=7;tempy++){
            for(int tempx=-1;tempx<=1;tempx++){
                for(int tempz=-1;tempz<=1;tempz++){
                    if(!((tempx == -1 || tempx == 1)&&(tempz == -1 || tempz == 1))){
                        if(world.GetBlock(x+tempx,y+tempy,z+tempz,chunk.coordinates)==0){
                            world.SetBlockWithoutUpdating(x+tempx,y+tempy,z+tempz,chunk.coordinates,7);
                        }
                    }
                }
            }
        }
        for(int i=1;i<=6;i++){
            world.SetBlockWithoutUpdating(x,y+i,z,chunk.coordinates,5);
        }
        world.SetBlockWithoutUpdating(x,y,z,chunk.coordinates,2);
    }
    private static void CreateSpruceTree(World world, int x,int y, int z, Chunk chunk){
        for(int tempy=4;tempy<=8;tempy+=2){
            for(int tempx=-2;tempx<=2;tempx++){
                for(int tempz=-2;tempz<=2;tempz++){
                    if(!((tempx == -2 || tempx == 2)&&(tempz == -2 || tempz == 2))){
                        if(world.GetBlock(x+tempx,y+tempy,z+tempz,chunk.coordinates)==0){
                            world.SetBlockWithoutUpdating(x+tempx,y+tempy,z+tempz,chunk.coordinates,26);
                        }
                    }
                }
            }
        }
        for(int tempy=5;tempy<=9;tempy+=2){
            for(int tempx=-1;tempx<=1;tempx++){
                for(int tempz=-1;tempz<=1;tempz++){
                    if(!((tempx == -1 || tempx == 1)&&(tempz == -1 || tempz == 1))){
                        if(world.GetBlock(x+tempx,y+tempy,z+tempz,chunk.coordinates)==0){
                            world.SetBlockWithoutUpdating(x+tempx,y+tempy,z+tempz,chunk.coordinates,26);
                        }
                    }
                }
            }
        }
        world.SetBlockWithoutUpdating(x,y+10,z,chunk.coordinates,26);
        for(int i=1;i<=8;i++){
            world.SetBlockWithoutUpdating(x,y+i,z,chunk.coordinates,24);
        }
    }
    public static double TerrainNoise(double x,double z) {
        double result = 0;
        double bigNoise = OpenSimplex2.Noise2(seed, x / 1000, z / 1000);
        if (bigNoise > 0.8) result += MathHelper.Lerp(90,200, (float)(bigNoise-0.8) * 5);
        else if (bigNoise > -0.5) result += MathHelper.Hermite(70,1,90,1,(float)(bigNoise+0.5)*(1f/1.3f));
        else result += MathHelper.Lerp(50,70,(float)(bigNoise+1)*2);
        result += OpenSimplex2.Noise2(seed, x / 400, z / 400) * 15;
        result += OpenSimplex2.Noise2(seed, x / 100, z / 100) * 4;
        return result;
    }
}