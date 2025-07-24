using System.Collections.Generic;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo {
    internal static class Light {
        public const int bitsPerLight = 4;
        public const int lightMask = (1 << bitsPerLight) - 1;

        public static void PropagateLight(int x, int y, int z, Chunk chunk, int lightValue, int bytesOffset, HashSet<Chunk> Set) {
            Queue<(int, int, int, Chunk, int)> lightQueue = [];
            int index = x + y * ChunkSize + z * ChunkSizeSquared;
            chunk.blockLightValues[index] &= (ushort)~(lightMask << bytesOffset);
            chunk.blockLightValues[index] |= (ushort)(lightValue << bytesOffset);
            lightQueue.Enqueue((x, y, z, chunk, lightValue - 1));

            PropagateLight(bytesOffset, lightQueue, Set);
        }
        private static void PropagateLight(int bytesOffset, Queue<(int, int, int, Chunk, int)> lightQueue, HashSet<Chunk> Set) {
            while (lightQueue.Count > 0) {
                var (x, y, z, chunk, light) = lightQueue.Dequeue();

                CheckNeighborLight(x + 1, y, z, chunk, light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(x - 1, y, z, chunk, light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(x, y + 1, z, chunk, light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(x, y - 1, z, chunk, light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(x, y, z + 1, chunk, light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(x, y, z - 1, chunk, light, bytesOffset, lightQueue, Set);
            }
        }
        private static void CheckNeighborLight(int x, int y, int z, Chunk chunk, int lightValue, int bytesOffset, Queue<(int, int, int, Chunk, int)> queue, HashSet<Chunk> Set) {
            CheckChunkBoundry(ref x, ref y, ref z, ref chunk, Set);
            if(chunk == null) {
                return;
            }
            int index = x + y * ChunkSize + z * ChunkSizeSquared;
            if (Blocks.IsTransparent(chunk.blocks[index])) {
                int current = (chunk.blockLightValues[index] >> bytesOffset) & lightMask;
                if (lightValue > current) {
                    chunk.blockLightValues[index] &= (ushort)~(lightMask << bytesOffset);
                    chunk.blockLightValues[index] |= (ushort)(lightValue << bytesOffset);
                    if (lightValue > 1)
                        queue.Enqueue((x, y, z, chunk, lightValue - 1));
                }
            }
        }
        public static void PropagateShadow(int x, int y, int z, Chunk chunk, HashSet<Chunk> Set) {
            PropagateShadow(x, y, z, chunk, 0, Set);
            PropagateShadow(x, y, z, chunk, bitsPerLight, Set);
            PropagateShadow(x, y, z, chunk, 2*bitsPerLight, Set);
            PropagateSkyShadow(x, y, z, chunk, Set);
        }
        private static void PropagateShadow(int startX, int startY, int startZ, Chunk startChunk, int bytesOffset, HashSet<Chunk> Set) {
            Queue<(int, int, int, Chunk, int)> shadowQueue = [];
            Queue<(int, int, int, Chunk, int)> lightQueue = [];
            int currentValue = ((startChunk.blockLightValues[startX + startY * ChunkSize + startZ * ChunkSizeSquared] >> bytesOffset) & lightMask) - 1;
            shadowQueue.Enqueue((startX, startY, startZ, startChunk, currentValue));
            startChunk.blockLightValues[startX + startY * ChunkSize + startZ * ChunkSizeSquared] &= (ushort)~(lightMask << bytesOffset);
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
        private static void CheckNeighborShadow(int x, int y, int z, Chunk chunk, int oldValue, int bytesOffset, Queue<(int, int, int, Chunk, int)> shadowQueue, Queue<(int, int, int, Chunk, int)> lightQueue, HashSet<Chunk> Set) {
            CheckChunkBoundry(ref x, ref y, ref z, ref chunk, Set);
            int index = x + y * ChunkSize + z * ChunkSizeSquared;
            if (Blocks.IsTransparent(chunk.blocks[index]) || Blocks.IsLightEminiting(chunk.blocks[index])) {
                int current = ((chunk.blockLightValues[index] >> bytesOffset) & lightMask);
                if (current == oldValue) {
                    if (current <= 0) return;
                    shadowQueue.Enqueue((x, y, z, chunk, current - 1));
                    chunk.blockLightValues[index] &= (ushort)~(lightMask << bytesOffset);
                }
                else if (current > oldValue) {
                    chunk.blockLightValues[index] &= (ushort)~(lightMask << bytesOffset);
                    chunk.blockLightValues[index] |= (ushort)(current << bytesOffset);
                    if (current > 1)
                        lightQueue.Enqueue((x, y, z, chunk, current - 1));
                }
            }
        }
        private static void CheckChunkBoundry(ref int x, ref int y, ref int z, ref Chunk chunk, HashSet<Chunk> Set) {
            if (x < 0 || x >= ChunkSize || y < 0 || y >= ChunkSize || z < 0 || z >= ChunkSize) {
                int chunkX = chunk.coordinates.x + (x >= 0 ? x / ChunkSize : ((x + 1) / ChunkSize) - 1);
                int chunkY = chunk.coordinates.y + (y >= 0 ? y / ChunkSize : ((y + 1) / ChunkSize) - 1);
                int chunkZ = chunk.coordinates.z + (z >= 0 ? z / ChunkSize : ((z + 1) / ChunkSize) - 1);

                if (!chunk.world.WorldMap.TryGetValue((chunkX, chunkY, chunkZ), out chunk)) return;
                Set.Add(chunk);

                // Will cause a crash later if x, y or z are below -ChunkSize but they never should be so it should be fine
                x = (x + ChunkSize) % ChunkSize;
                y = (y + ChunkSize) % ChunkSize;
                z = (z + ChunkSize) % ChunkSize;
            }
        }
        public static void PropagateSkyLight(Chunk startChunk) {
            Queue<(int, int, int, Chunk, int)> lightQueue = [];
            for (int x = 0; x < ChunkSize; x++) {
                for (int z = 0; z < ChunkSize; z++) {
                    int index = x + (ChunkSize - 1) * ChunkSize + z * ChunkSizeSquared;
                    startChunk.blockLightValues[index] |= lightMask << (3*bitsPerLight);
                    lightQueue.Enqueue((x, ChunkSize - 1, z, startChunk, lightMask - 1));
                }
            }
            HashSet<Chunk> Set = [];
            PropagateSkyLight(lightQueue, Set);
        }
        private static void PropagateSkyLight(Queue<(int,int,int,Chunk,int)> lightQueue, HashSet<Chunk> Set) {
            while (lightQueue.Count > 0) {
                var (x, y, z, chunk, light) = lightQueue.Dequeue();

                CheckNeighborLight(x + 1, y, z, chunk, light, 3 * bitsPerLight, lightQueue, Set);
                CheckNeighborLight(x - 1, y, z, chunk, light, 3 * bitsPerLight, lightQueue, Set);
                CheckNeighborLight(x, y + 1, z, chunk, light, 3 * bitsPerLight, lightQueue, Set);
                CheckNeighborLight(x, y - 1, z, chunk, light == lightMask - 1 ? light+1 : light, 3 * bitsPerLight, lightQueue, Set);
                CheckNeighborLight(x, y, z + 1, chunk, light, 3 * bitsPerLight, lightQueue, Set);
                CheckNeighborLight(x, y, z - 1, chunk, light, 3 * bitsPerLight, lightQueue, Set);
            }
        }
        public static void PropagateSkyShadow(int startX, int startY, int startZ, Chunk startChunk, HashSet<Chunk> Set) {
            Queue<(int, int, int, Chunk, int)> shadowQueue = [];
            Queue<(int, int, int, Chunk, int)> lightQueue = [];
            int currentValue = ((startChunk.blockLightValues[startX + startY * ChunkSize + startZ * ChunkSizeSquared] >> (3*bitsPerLight)) & lightMask) - 1;
            shadowQueue.Enqueue((startX, startY, startZ, startChunk, currentValue));
            unchecked {
                startChunk.blockLightValues[startX + startY * ChunkSize + startZ * ChunkSizeSquared] &= (ushort)~(lightMask << (3 * bitsPerLight));
            }
            while (shadowQueue.Count > 0) {
                (int x, int y, int z, Chunk chunk, int oldValue) = shadowQueue.Dequeue();

                CheckNeighborShadow(x + 1, y, z, chunk, oldValue, 3 * bitsPerLight, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(x - 1, y, z, chunk, oldValue, 3 * bitsPerLight, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(x, y + 1, z, chunk, oldValue, 3 * bitsPerLight, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(x, y - 1, z, chunk, oldValue == lightMask - 1 ? oldValue+1 : oldValue, 3 * bitsPerLight, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(x, y, z + 1, chunk, oldValue, 3 * bitsPerLight, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(x, y, z - 1, chunk, oldValue, 3 * bitsPerLight, shadowQueue, lightQueue, Set);
            }
            PropagateSkyLight(lightQueue, Set);
        }
    }
}
