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
    
    // MaxHeight needs to divisible by ChunkSize
    public const int MaxHeight = 512;
    public const int MaxYChunk = MaxHeight / ChunkSize;
    public void SetBlock(Vector3 coords,(int, int, int) chunkCoordinate, byte id, BlockFace blockSide, BoundingBox playerHitBox){
        switch(blockSide){
            case BlockFace.South:
                coords.X -= 1;
                break;
            case BlockFace.North:
                coords.X += 1;
                break;
            case BlockFace.Down:
                coords.Y -= 1;
                break;
            case BlockFace.Up:
                coords.Y += 1;
                break;
            case BlockFace.East:
                coords.Z -= 1;
                break;
            case BlockFace.West:
                coords.Z += 1;
                break;
        }
        if (!Blocks.IsNotSolid(id)){
            if(playerHitBox.Intersects(new BoundingBox(coords, coords+Vector3.One))) {
                return;
            }
        }
        else {
            if (Blocks.IsNotSolid(GetBlock((int)coords.X, (int)coords.Y - 1, (int)coords.Z, chunkCoordinate))) {
                return;
            }
        }
        SetBlock(coords, chunkCoordinate, id, blockSide);
    }
    public void SetBlock(Vector3 coords, (int x,int y,int z) chunkCoordinate,byte id, BlockFace blockSide){
        int x = (int)coords.X;
        int y = (int)coords.Y;
        int z = (int)coords.Z;
        NormalizeChunkCoordinates(ref x,ref y,ref z,ref chunkCoordinate);
        Chunk chunk = TryGetOrCreateChunk(chunkCoordinate);
        if (chunk == null) return;
        HashSet<Chunk> set = [
            chunk
        ];
        int index = x + y * ChunkSize + z * ChunkSizeSquared;
        if (Blocks.CanRotate(id)) {
            switch (blockSide) {
                case BlockFace.South:
                case BlockFace.North:
                    chunk.BlockStates[index] = 2;
                    break;
                case BlockFace.East:
                case BlockFace.West:
                    chunk.BlockStates[index] = 4;
                    break;
            }
        }
        
        if (id == 0) {
            chunk.BlockStates.Remove(index);
            if (Blocks.IsFoliage(GetBlock(x, y + 1, z, chunkCoordinate))) {
                SetBlockWithoutUpdating(x,y+1,z,chunkCoordinate,0);
            }
        }
        chunk.blocks[index]=id;
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
    public void SetBlockWithoutUpdating(int x,int y,int z,(int x,int y,int z) chunkCoordinate,byte id){
        NormalizeChunkCoordinates(ref x,ref y,ref z,ref chunkCoordinate);
        Chunk chunk = TryGetOrCreateChunk(chunkCoordinate);
        if (chunk == null) return;
        chunk.blocks[x+y*ChunkSize+z*ChunkSizeSquared]=id;
    }
    public byte GetBlock(int x,int y,int z, (int x,int y,int z) chunkCoordinate){
        NormalizeChunkCoordinates(ref x,ref y,ref z,ref chunkCoordinate);
        if(WorldMap.TryGetValue(chunkCoordinate, out Chunk chunk)){
            return chunk.blocks[x+(y*ChunkSize)+(z*ChunkSizeSquared)];
        }
        return 0;
    }
    private Chunk TryGetOrCreateChunk((int x, int y, int z) chunkCoordinate) {
        if (chunkCoordinate.y >= MaxYChunk) {
            return null;
        }
        if(!WorldMap.TryGetValue(chunkCoordinate,out Chunk chunk)){
            WorldMap.TryAdd(chunkCoordinate,new(chunkCoordinate,this));
            chunk = WorldMap[chunkCoordinate];
            Array.Fill(chunk.blockLightValues, (ushort)(Light.lightMask << Light.SkyLight));
        }

        return chunk;
    }
    public void GenerateChunkLine(int x,int z){
         Chunk chunk = SaveFile.TryLoadChunkLine(this, x, z) ?? TerrainGen.GenerateTerrain(this,x,z);

         Light.PropagateSkyLight(chunk);
        for (int i=0;i<chunk.coordinates.y;i++){
            WorldMap[(x,i,z)].IsGenerated = true;
        }
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
                if (WorldMap.TryGetValue((x, y, z), out chunk)) {
                    GenerateChunkMesh(chunk);
                }
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