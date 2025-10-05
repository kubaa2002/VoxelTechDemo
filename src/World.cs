using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo;
public class World{
    public readonly ConcurrentDictionary<(int,int,int),Chunk> WorldMap = new();
    public readonly HashSet<(int x, int z)> CurrentlyLoadedChunkLines = [];
    public readonly long seed;
    // MaxHeight needs to divisible by ChunkSize
    public const int MaxHeight = 512;
    public const int MaxYChunk = MaxHeight / ChunkSize;
    public World(long seed) {
        this.seed = seed;
    }
    public void SetBlock(Vector3 coords,(int, int, int) chunkCoordinate, byte id, BlockFace blockSide, BoundingBox playerHitBox){
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
        if (!Blocks.IsNotSolid(id)){
            if(playerHitBox.Intersects(new BoundingBox(coords, coords+Vector3.One))) {
                return;
            }
        }
        else {
            if (Blocks.IsFoliage(GetBlock((int)coords.X,(int)coords.Y - 1,(int)coords.Z,chunkCoordinate))) {
                return;
            }
        }
        SetBlock(coords, chunkCoordinate, id);
    }
    public void SetBlock(Vector3 coords, (int x,int y,int z) chunkCoordinate,byte id){
        int x = (int)coords.X;
        int y = (int)coords.Y;
        int z = (int)coords.Z;
        NormalizeChunkCoordinates(ref x,ref y,ref z,ref chunkCoordinate);
        if(!WorldMap.TryGetValue(chunkCoordinate, out Chunk chunk)){
            return;
        }
        HashSet<Chunk> set = [
            chunk
        ];
        chunk.blocks[x+(y*ChunkSize)+(z*ChunkSizeSquared)]=id;
        chunk.UpdateLight(x, y, z, id, set);
        GenerateChunkMesh(chunk);
        if(x==0){
            if(WorldMap.TryGetValue((chunkCoordinate.x-1,chunkCoordinate.y,chunkCoordinate.z),out chunk)){
                set.Add(chunk);
            }
        }
        if(x==ChunkSize-1){
            if(WorldMap.TryGetValue((chunkCoordinate.x+1,chunkCoordinate.y,chunkCoordinate.z),out chunk)){
                set.Add(chunk);
            }
        }
        if(y==0){
            if(WorldMap.TryGetValue((chunkCoordinate.x,chunkCoordinate.y-1,chunkCoordinate.z),out chunk)){
                set.Add(chunk);
            }
        }
        if(y==ChunkSize-1){
            if(WorldMap.TryGetValue((chunkCoordinate.x,chunkCoordinate.y+1,chunkCoordinate.z),out chunk)){
                set.Add(chunk);
            }
        }
        if(z==0){
            if(WorldMap.TryGetValue((chunkCoordinate.x,chunkCoordinate.y,chunkCoordinate.z-1),out chunk)){
                set.Add(chunk);
            }
        }
        if(z==ChunkSize-1){
            if(WorldMap.TryGetValue((chunkCoordinate.x,chunkCoordinate.y,chunkCoordinate.z+1),out chunk)){
                set.Add(chunk);
            }
        }
        foreach (Chunk value in set) {
            GenerateChunkMesh(value);
        }
    }
    private void SetBlockWithoutUpdating(int x,int y,int z,(int x,int y,int z) chunkCoordinate,byte id){
        NormalizeChunkCoordinates(ref x,ref y,ref z,ref chunkCoordinate);
        if(!WorldMap.TryGetValue(chunkCoordinate,out Chunk chunk)){
            WorldMap.TryAdd(chunkCoordinate,new(chunkCoordinate,this));
            chunk = WorldMap[chunkCoordinate];
        }
        chunk.blocks[x+y*ChunkSize+z*ChunkSizeSquared]=id;
    }
    public byte GetBlock(int x,int y,int z, (int x,int y,int z) chunkCoordinate){
        NormalizeChunkCoordinates(ref x,ref y,ref z,ref chunkCoordinate);
        if(WorldMap.TryGetValue(chunkCoordinate, out Chunk chunk)){
            return chunk.blocks[x+(y*ChunkSize)+(z*ChunkSizeSquared)];
        }
        return 0;
    }
    public void GenerateChunkLine(int x,int z){
        for(int y=0;y<MaxYChunk;y++){
            WorldMap.TryAdd((x,y,z),new((x,y,z),this));
        }

        if (!SaveFile.TryLoadChunkLine(this, x, z)) {
            GenerateTerrain(x,z);
        }
    }
    private void GenerateTerrain(int chunkX,int chunkZ){
        Chunk[] chunks = new Chunk[MaxYChunk];
        for(int y=0;y<MaxYChunk;y++){
            chunks[y]=WorldMap[(chunkX,y,chunkZ)];
        }
        for(int x=0;x<ChunkSize;x++){
            for(int z=0;z<ChunkSize;z++){
                // If yLevel below 0 needs to be generated, MountainNoise needs to floored before casting to int
                int yLevel = 50+(int)MountainNoise((double)chunkX*ChunkSize+x,(double)chunkZ*ChunkSize+z);
                int blockPosition = x+yLevel%ChunkSize*ChunkSize+z*ChunkSizeSquared;
                byte[] chunkBlocks = chunks[yLevel/ChunkSize].blocks;
                // Water level
                if(yLevel < 63){
                    for(int y=63;y>yLevel;y--){
                        if(y%ChunkSize==ChunkSize-1){
                            chunkBlocks = chunks[y/ChunkSize].blocks;
                            blockPosition = x+ChunkSizeSquared-ChunkSize+z*ChunkSizeSquared;
                        }
                        chunkBlocks[blockPosition]=15;
                        blockPosition-=ChunkSize;
                    }
                }
                // Dirt level
                int stoneNoise = (int)(OpenSimplex2.Noise2(seed,(double)chunkX*ChunkSize+x,(double)chunkZ*ChunkSize+z)*5f);
                stoneNoise += (int)(OpenSimplex2.Noise2(seed,((double)chunkX*ChunkSize+x)/100,((double)chunkZ*ChunkSize+z)/100)*20f);
                for(int y=yLevel;y>=yLevel-2;y--){
                    if(y%ChunkSize==ChunkSize-1){
                        chunkBlocks = chunks[y/ChunkSize].blocks;
                        blockPosition = x+ChunkSizeSquared-ChunkSize+z*ChunkSizeSquared;
                    }
                    if(y == yLevel){
                        if(y>=65){
                            if(y>=238+stoneNoise){
                                if(y>=258+stoneNoise){
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
                        if(y>=238+stoneNoise){
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
                
                if(foliageNoise > 0.9f + 0.0004f*yLevel && yLevel>=65){
                    CreateTree(x,yLevel%ChunkSize,z, chunks[yLevel/ChunkSize]);
                }
                
                if (foliageNoise < -0.5f && yLevel >= 65) {
                    if (GetBlock(x,yLevel,z,(chunkX,0,chunkZ)) == 1) {
                        if (foliageNoise > -0.525f) {
                            SetBlockWithoutUpdating(x, yLevel + 1, z, (chunkX, 0, chunkZ),
                                foliageNoise > -0.5125f ? (byte)20 : (byte)21);
                        }
                        else {
                            SetBlockWithoutUpdating(x,yLevel+1,z,(chunkX,0,chunkZ),19);
                        }
                    }
                }
            }
        }
        Light.PropagateSkyLight(chunks[^1]);
        for (int i=0;i<MaxYChunk;i++){
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
    private static void NormalizeChunkCoordinates(ref int x,ref int y,ref int z,ref (int x,int y,int z) chunkCoordinate){
        if (x > ChunkSize - 1 || x < 0) {
            int amount = x / ChunkSize;
            if (x < 0)
                amount -= 1;
            chunkCoordinate.x += amount;
            x -= ChunkSize * amount;
        }
        if (y > ChunkSize - 1 || y < 0) {
            int amount = y / ChunkSize;
            if (y < 0)
                amount -= 1;
            chunkCoordinate.y += amount;
            y -= ChunkSize * amount;
        }
        if (z > ChunkSize - 1 || z < 0) {
            int amount = z / ChunkSize;
            if (z < 0)
                amount -= 1;
            chunkCoordinate.z += amount;
            z -= ChunkSize * amount;
        }
    }
    public void UpdateLoadedChunks(int chunkX,int chunkZ) {
        for (int x = -UserSettings.RenderDistance; x <= UserSettings.RenderDistance; x++) {
            for (int z = -UserSettings.RenderDistance; z <= UserSettings.RenderDistance; z++) {
                if (x * x + z * z <= (UserSettings.RenderDistance + 0.5f) * (UserSettings.RenderDistance + 0.5f)) {
                    if (!CurrentlyLoadedChunkLines.Contains((chunkX + x, chunkZ + z))) {
                        LoadChunkLine(chunkX + x, chunkZ + z);
                    }
                }
            }
        }
        foreach ((int x, int z) in CurrentlyLoadedChunkLines) {
            if ((x - chunkX) * (x - chunkX) + (z - chunkZ) * (z - chunkZ) > (UserSettings.RenderDistance + 1.5f) * (UserSettings.RenderDistance + 1.5f)) {
                UnloadChunkLine(x, z);
            }
        }
    }
    private Task LoadChunkLine(int x, int z) {
        CurrentlyLoadedChunkLines.Add((x, z));
        return Task.Run(() => {
            // TOFIX: Sometimes chunk is generated 2 times
            if (!WorldMap.TryGetValue((x, 0, z), out Chunk chunk) || !chunk.IsGenerated) {
                GenerateChunkLine(x, z);
            }
            if (!WorldMap.TryGetValue((x + 1, 0, z), out chunk) || !chunk.IsGenerated) {
                GenerateChunkLine(x + 1, z);
            }
            if (!WorldMap.TryGetValue((x, 0, z + 1), out chunk) || !chunk.IsGenerated) {
                GenerateChunkLine(x, z + 1);
            }
            if (!WorldMap.TryGetValue((x - 1, 0, z), out chunk) || !chunk.IsGenerated) {
                GenerateChunkLine(x - 1, z);
            }
            if (!WorldMap.TryGetValue((x, 0, z - 1), out chunk) || !chunk.IsGenerated) {
                GenerateChunkLine(x, z - 1);
            }
            for (int y = 0; y < MaxYChunk; y++) {
                GenerateChunkMesh(WorldMap[(x, y, z)]);
            }
        });
    }
    private void UnloadChunkLine(int x, int z) {
        CurrentlyLoadedChunkLines.Remove((x, z));
        SaveFile.SaveChunkLine(this, x, z);
        for (int y = 0; y < MaxYChunk; y++) {
            if (!WorldMap.Remove((x, y, z), out Chunk chunk)) {
                continue;
            }
            chunk.vertexBufferOpaque?.Dispose();
            chunk.vertexBufferTransparent?.Dispose();
        }

    }
}