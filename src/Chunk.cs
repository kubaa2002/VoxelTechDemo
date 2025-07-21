using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo{
    public class Chunk{
        public byte[] blocks = new byte[ChunkSizeCubed];
        public short[] blockLightValues = new short[ChunkSizeCubed];
        public (int x,int y,int z) coordinates;
        public readonly World world;
        public VertexBuffer vertexBufferOpaque;
        public VertexBuffer vertexBufferTransparent;
        public bool IsGenerated = false;
        public byte maxY = 0;
        public Chunk((int x,int y,int z) coordinates, World world){
            this.coordinates = coordinates;
            this.world = world;
        }
        public void CheckMaxY(int y){
            y++;
            if(y>maxY){
                maxY = (byte)y;
            }
        }
        public void UpdateLight(int x, int y, int z, byte Id, HashSet<Chunk> Set) {
            if (Blocks.IsLightEminiting(Id)) {
                (int red, int green, int blue) = Blocks.ReturnBlockLightValues(Id);
                short currentLightValue = blockLightValues[x + y * ChunkSize + z * ChunkSizeSquared];
                PropagateShadow(x, y, z, Set);
                if ((currentLightValue & 31) < red) {
                    PropagateLight(x, y, z, red, 0, Set);
                }
                if (((currentLightValue >> 5) & 31) < green) {
                    PropagateLight(x, y, z, green, 5, Set);
                }
                if (((currentLightValue >> 10) & 31) < blue) {
                    PropagateLight(x, y, z, blue, 10, Set);
                }
            }
            else if (Id == 0 || !Blocks.IsTransparent(Id)) {
                PropagateShadow(x, y, z, Set);
            }
        }
        public void PropagateLight(int x, int y, int z, int lightValue, int bytesOffset, HashSet<Chunk> Set) {
            Queue<(int, int, int, Chunk, int)> lightQueue = [];
            int index = x + y * ChunkSize + z * ChunkSizeSquared;
            blockLightValues[index] &= (short)~(31 << bytesOffset);
            blockLightValues[index] |= (short)(lightValue << bytesOffset);
            lightQueue.Enqueue((x, y, z, this, lightValue-1));

            PropagateLight(bytesOffset, lightQueue, Set);
        }
        public void PropagateLight(int bytesOffset, Queue<(int, int, int, Chunk, int)> lightQueue, HashSet<Chunk> Set) {
            while (lightQueue.Count > 0) {
                var (x, y, z, chunk, light) = lightQueue.Dequeue();

                // Check all 6 directions
                CheckNeighborLight(x + 1, y, z, chunk, light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(x - 1, y, z, chunk, light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(x, y + 1, z, chunk, light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(x, y - 1, z, chunk, light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(x, y, z + 1, chunk, light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(x, y, z - 1, chunk, light, bytesOffset, lightQueue, Set);
            }
        }
        private void CheckNeighborLight(int x, int y, int z, Chunk chunk, int lightValue, int bytesOffset, Queue<(int, int, int, Chunk, int)> queue, HashSet<Chunk> Set) {
            CheckChunkBoundry(ref x, ref y, ref z, ref chunk, Set);
            int index = x + y * ChunkSize + z * ChunkSizeSquared;
            if (Blocks.IsTransparent(chunk.blocks[index])) {
                int current = (chunk.blockLightValues[index] >> bytesOffset) & 31;
                if (lightValue > current) {
                    chunk.blockLightValues[index] &= (short)~(31 << bytesOffset);
                    chunk.blockLightValues[index] |= (short)(lightValue << bytesOffset);
                    if(lightValue > 1)
                        queue.Enqueue((x, y, z, chunk, lightValue-1));
                }
            }
        }
        public void PropagateShadow(int x, int y, int z, HashSet<Chunk> Set) {
            PropagateShadow(x, y, z, 0, Set);
            PropagateShadow(x, y, z, 5, Set);
            PropagateShadow(x, y, z, 10, Set);
        }
        public void PropagateShadow(int startX, int startY, int startZ, int bytesOffset, HashSet<Chunk> Set) {
            Queue<(int, int, int, Chunk, int)> shadowQueue = [];
            Queue<(int, int, int, Chunk, int)> lightQueue = [];
            int currentValue = ((blockLightValues[startX + startY * ChunkSize + startZ * ChunkSizeSquared] >> bytesOffset) & 31) - 1;
            shadowQueue.Enqueue((startX,startY,startZ,this,currentValue));
            blockLightValues[startX + startY * ChunkSize + startZ * ChunkSizeSquared] &= (short)~(31 << bytesOffset);
            while (shadowQueue.Count > 0) {
                (int x, int y, int z, Chunk chunk, int oldValue) = shadowQueue.Dequeue();

                CheckNeighborShadow(x + 1, y, z, chunk, oldValue, bytesOffset, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(x - 1, y, z, chunk, oldValue, bytesOffset, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(x, y + 1, z, chunk, oldValue, bytesOffset, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(x, y - 1, z, chunk, oldValue, bytesOffset, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(x, y, z + 1, chunk, oldValue, bytesOffset, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(x, y, z - 1, chunk, oldValue, bytesOffset, shadowQueue, lightQueue, Set);
            }
            PropagateLight(bytesOffset, lightQueue, Set);
        }
        private void CheckNeighborShadow(int x,int y,int z, Chunk chunk, int oldValue, int bytesOffset, Queue<(int, int, int, Chunk, int)> shadowQueue, Queue<(int, int, int, Chunk, int)> lightQueue, HashSet<Chunk> Set) {
            CheckChunkBoundry(ref x, ref y, ref z, ref chunk, Set);
            int index = x + y * ChunkSize + z * ChunkSizeSquared;
            if (Blocks.IsTransparent(chunk.blocks[index]) || Blocks.IsLightEminiting(chunk.blocks[index])) {
                int value = ((chunk.blockLightValues[index] >> bytesOffset) & 31);
                if (value == oldValue) {
                    if (value <= 0) return;
                    shadowQueue.Enqueue((x, y, z, chunk, value - 1));
                    chunk.blockLightValues[index] &= (short)~(31 << bytesOffset);
                }
                else if (value > oldValue) {
                    chunk.blockLightValues[index] &= (short)~(31 << bytesOffset);
                    chunk.blockLightValues[index] |= (short)(value << bytesOffset);
                    if (value > 1)
                        lightQueue.Enqueue((x,y,z,chunk,value-1));
                }
            }
        }
        private void CheckChunkBoundry(ref int x, ref int y, ref int z, ref Chunk chunk, HashSet<Chunk> Set) {
            if (x < 0 || x >= ChunkSize || y < 0 || y >= ChunkSize || z < 0 || z >= ChunkSize) {
                int chunkX = chunk.coordinates.x + (x >= 0 ? x / ChunkSize : ((x + 1) / ChunkSize) - 1);
                int chunkY = chunk.coordinates.y + (y >= 0 ? y / ChunkSize : ((y + 1) / ChunkSize) - 1);
                int chunkZ = chunk.coordinates.z + (z >= 0 ? z / ChunkSize : ((z + 1) / ChunkSize) - 1);

                if (!world.WorldMap.TryGetValue((chunkX, chunkY, chunkZ), out chunk)) return;
                Set.Add(chunk);

                // Will cause a crash later if x, y or z are below -ChunkSize but they never should be so it should be fine
                x = (x + ChunkSize) % ChunkSize;
                y = (y + ChunkSize) % ChunkSize;
                z = (z + ChunkSize) % ChunkSize;
            }
        }
        public Vector3 GetLightValues(int currentBlock, int face) {
            switch (face) {
                // x+
                case 0:
                    if (currentBlock % ChunkSize != ChunkSize - 1) {
                        return ConvertLightValues(blockLightValues[currentBlock + 1]);
                    }
                    else {
                        return ConvertLightValues(world.WorldMap[(coordinates.x + 1, coordinates.y, coordinates.z)].blockLightValues[-ChunkSize + 1 + currentBlock]);
                    }
                // x-
                case 1:
                    if (currentBlock % ChunkSize != 0) {
                        return ConvertLightValues(blockLightValues[currentBlock - 1]);
                    }
                    else {
                        return ConvertLightValues(world.WorldMap[(coordinates.x - 1, coordinates.y, coordinates.z)].blockLightValues[ChunkSize - 1 + currentBlock]);
                    }
                // y+
                case 2:
                    if (currentBlock / ChunkSize % ChunkSize != ChunkSize - 1) {
                        return ConvertLightValues(blockLightValues[currentBlock + ChunkSize]);
                    }
                    else {
                        if (world.WorldMap.TryGetValue((coordinates.x, coordinates.y + 1, coordinates.z), out Chunk adjacentChunk)) {
                            return ConvertLightValues(adjacentChunk.blockLightValues[-ChunkSize * (ChunkSize - 1) + currentBlock]);
                        }
                        else {
                            return Vector3.One;
                        }
                    }
                // y-
                case 3:
                    if (currentBlock / ChunkSize % ChunkSize != 0) {
                        return ConvertLightValues(blockLightValues[currentBlock - ChunkSize]);
                    }
                    else {
                        if (world.WorldMap.TryGetValue((coordinates.x, coordinates.y - 1, coordinates.z), out Chunk adjacentChunk)) {
                            return ConvertLightValues(adjacentChunk.blockLightValues[ChunkSize * (ChunkSize - 1) + currentBlock]);
                        }
                        else {
                            return Vector3.One;
                        }
                    }
                // z+
                case 4:
                    if (currentBlock / ChunkSizeSquared != ChunkSize - 1) {
                        return ConvertLightValues(blockLightValues[currentBlock + ChunkSizeSquared]);
                    }
                    else {
                        return ConvertLightValues(world.WorldMap[(coordinates.x, coordinates.y, coordinates.z + 1)].blockLightValues[-ChunkSizeSquared * (ChunkSize - 1) + currentBlock]);
                    }
                // z-
                case 5:
                    if (currentBlock / ChunkSizeSquared != 0) {
                        return ConvertLightValues(blockLightValues[currentBlock - ChunkSizeSquared]);
                    }
                    else {
                        return ConvertLightValues(world.WorldMap[(coordinates.x, coordinates.y, coordinates.z - 1)].blockLightValues[ChunkSizeSquared * (ChunkSize - 1) + currentBlock]);
                    }
                default:
                    return Vector3.One;
            }
        }
        private static Vector3 ConvertLightValues(short value) {
            return new Vector3(value & 31, (value >> 5) & 31, (value >> 10) & 31) + Vector3.One;
        }
    }
}