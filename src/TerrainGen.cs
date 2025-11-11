using static VoxelTechDemo.VoxelRenderer;
using System.Linq;
using Microsoft.Xna.Framework;

namespace VoxelTechDemo;
public static class TerrainGen {
    public const long Seed = 12345;
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
                double temperature = OpenSimplex2.Noise2(Seed+1, (double)(chunkX * ChunkSize + x)/1000, (double)(chunkZ * ChunkSize + z)/1000);
                temperature += OpenSimplex2.Noise2(Seed+1, (double)(chunkX * ChunkSize + x)/10, (double)(chunkZ * ChunkSize + z)/10)/100;
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
        if(yLevel < 63) {
            FillWater(yLevel, blockPosition, chunks);
            chunkBlocks[blockPosition | YMask] = 22;
        }
        // Dirt level
        for(int y=yLevel;y>=yLevel-1;y--){
            PlaceBlock(y, ref blockPosition, ref chunkBlocks, chunks, yLevel<63 
                ? (byte)11 // Gravel
                : (byte)13); // Snow
        }
        // Stone level
        FillStone(yLevel-2, blockPosition, chunks);
        
        float foliageNoise = OpenSimplex2.Noise2(Seed, (double)chunkX * ChunkSize + x,
            (double)chunkZ * ChunkSize + z);
        
        if(foliageNoise > 0.9f + 0.0004f*yLevel && yLevel>=65){
            CreateSpruceTree(world, x,yLevel%ChunkSize,z, chunks[yLevel/ChunkSize]);
        }
        
        if (foliageNoise < -0.5f && yLevel >= 63) {
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
            FillWater(yLevel, blockPosition, chunks);
        }
        // Dirt level
        int stoneNoise = (int)(OpenSimplex2.Noise2(Seed,(double)chunkX*ChunkSize+x,(double)chunkZ*ChunkSize+z)*5f);
        stoneNoise += (int)(OpenSimplex2.Noise2(Seed,((double)chunkX*ChunkSize+x)/400,((double)chunkZ*ChunkSize+z)/400)*20f);
        
        PlaceBlock(yLevel, ref blockPosition, ref chunkBlocks, chunks, yLevel >= 65 
            ? yLevel >= 158 + stoneNoise
                ? yLevel >= 178 + stoneNoise 
                    ? (byte)13 // Snow
                    : (byte)3 // Stone
                : (byte)1 // Grass
            : yLevel < 62 
                ? (byte)11 // Gravel
                : (byte)12); // Sand

        for(int y=yLevel-1;y>=yLevel-2;y--){
            PlaceBlock(y, ref blockPosition, ref chunkBlocks, chunks, y>=158+stoneNoise ? 
                (byte)3 // Stone
                : (byte)2); // Dirt
        }
        // Stone level
        FillStone(yLevel-3, blockPosition, chunks);
        
        float foliageNoise = OpenSimplex2.Noise2(Seed, (double)chunkX * ChunkSize + x,
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
            FillWater(yLevel, blockPosition, chunks);
        }

        // Dirt level
        int stoneNoise =
            (int)(OpenSimplex2.Noise2(Seed, (double)chunkX * ChunkSize + x, (double)chunkZ * ChunkSize + z) * 5f);
        stoneNoise += (int)(OpenSimplex2.Noise2(Seed, ((double)chunkX * ChunkSize + x) / 400,
            ((double)chunkZ * ChunkSize + z) / 400) * 20f);
        for (int y = yLevel; y >= yLevel - 2; y--) {
            PlaceBlock(y, ref blockPosition, ref chunkBlocks, chunks, y >= 158 + stoneNoise?
                y>= 188 + stoneNoise 
                    ? (byte)13 // Snow
                    : (byte)3 // Stone
                : yLevel < 62 
                    ? (byte)11 // Gravel
                    : (byte)12); // Sand
        }

        // Stone level
        FillStone(yLevel-3, blockPosition, chunks);

        float foliageNoise = OpenSimplex2.Noise2(Seed, (double)chunkX * ChunkSize + x,
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
    private static void FillWater(int yLevel, int blockPosition, Chunk[] chunks) {
        byte[] chunkBlocks = chunks[0].blocks;
        blockPosition |= YMask;
        for (int y = 63; y > yLevel; y--) {
            chunkBlocks[blockPosition] = 15;
            blockPosition -= ChunkSize;
        }
    }
    private static void PlaceBlock(int y, ref int blockPosition, ref byte[] blocks, Chunk[] chunks, byte id) {
        blocks[blockPosition] = id;
        if ((blockPosition&YMask) == 0) {
            blocks = chunks[y / ChunkSize - 1].blocks;
            blockPosition |= YMask;
        }
        else blockPosition -= ChunkSize;
    }
    private static void FillStone(int yLevel, int blockPosition, Chunk[] chunks) {
        for (int yChunk = yLevel / ChunkSize; yChunk >= 0; yChunk--) {
            byte[] chunkBlocks = chunks[yChunk].blocks;
            for (int y = blockPosition; y >= (blockPosition&~YMask); y-=ChunkSize) {
                chunkBlocks[y] = 3;
            }
            blockPosition |= YMask;
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
    public static float TerrainNoise(double x,double z) {
        float result = OpenSimplex2.Noise2(Seed, x / 1000, z / 1000);
        if (result > 0.8f) result = MathHelper.Lerp(90,200, (result-0.8f) * 5);
        else if (result > -0.5f) result = MathHelper.Hermite(70,1,90,1,(result+0.5f)*(1f/1.3f));
        else result = MathHelper.Lerp(50,70,(result+1f)*2);
        result += OpenSimplex2.Noise2(Seed, x / 400, z / 400) * 15;
        result += OpenSimplex2.Noise2(Seed, x / 100, z / 100) * 4;
        return result;
    }
}