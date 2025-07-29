using System.Collections.Generic;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo {
    internal static class Light {
        public const int bitsPerLight = 4;
        public const int lightMask = (1 << bitsPerLight) - 1;

        private const int RedLight = 0;
        private const int GreenLight = bitsPerLight;
        private const int BlueLight = 2*bitsPerLight;
        private const int SkyLight = 3*bitsPerLight;
        private enum Direction { PosX = 1, NegX = -1, PosY = ChunkSize, NegY = -ChunkSize, PosZ = ChunkSizeSquared, NegZ = -ChunkSizeSquared }

        public static void PropagateLight(int x, int y, int z, Chunk chunk, int lightValue, int bytesOffset, HashSet<Chunk> Set) {
            Queue<(int, Chunk, int)> lightQueue = [];
            int index = x + y * ChunkSize + z * ChunkSizeSquared;
            chunk.blockLightValues[index] &= (ushort)~(lightMask << bytesOffset);
            chunk.blockLightValues[index] |= (ushort)(lightValue << bytesOffset);
            lightQueue.Enqueue((index, chunk, lightValue - 1));

            PropagateLight(bytesOffset, lightQueue, Set);
        }
        public static void PropagateSkyLight(Chunk startChunk) {
            Queue<(int, Chunk, int)> lightQueue = [];
            for (int x = 0; x < ChunkSize; x++) {
                for (int z = 0; z < ChunkSize; z++) {
                    int index = x + (ChunkSize - 1) * ChunkSize + z * ChunkSizeSquared;
                    Chunk currentChunk = startChunk;
                    while (Blocks.IsTransparent(currentChunk.blocks[index])) {
                        currentChunk.blockLightValues[index] |= lightMask << SkyLight;
                        if ((index >> 6) % ChunkSize == 0) {
                            if (!currentChunk.world.WorldMap.TryGetValue((currentChunk.coordinates.x, currentChunk.coordinates.y - 1, currentChunk.coordinates.z), out currentChunk)) break;
                            index += ChunkSizeSquared - ChunkSize;
                        }
                        else {
                            index -= ChunkSize;
                        }
                    }
                    if(currentChunk is not null)
                        lightQueue.Enqueue((index, currentChunk, lightMask - 1));
                }
            }
            PropagateLight(SkyLight, lightQueue, null);
        }
        private static void PropagateLight(int bytesOffset, Queue<(int, Chunk, int)> lightQueue, HashSet<Chunk> Set) {
            while (lightQueue.Count > 0) {
                var (index, chunk, light) = lightQueue.Dequeue();

                CheckNeighborLight(index, Direction.PosX, chunk, light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(index, Direction.NegX, chunk, light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(index, Direction.PosY, chunk, light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(index, Direction.NegY, chunk, (light == lightMask - 1) && (bytesOffset == SkyLight) ? light + 1 : light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(index, Direction.PosZ, chunk, light, bytesOffset, lightQueue, Set);
                CheckNeighborLight(index, Direction.NegZ, chunk, light, bytesOffset, lightQueue, Set);
            }
        }
        private static void CheckNeighborLight(int index, Direction dir, Chunk chunk, int lightValue, int bytesOffset, Queue<(int, Chunk, int)> queue, HashSet<Chunk> Set) {
            if(!CheckChunkBoundry(ref index, dir, ref chunk, Set)) return;
            if (Blocks.IsTransparent(chunk.blocks[index])) {
                int current = (chunk.blockLightValues[index] >> bytesOffset) & lightMask;
                if (lightValue > current) {
                    chunk.blockLightValues[index] &= (ushort)~(lightMask << bytesOffset);
                    chunk.blockLightValues[index] |= (ushort)(lightValue << bytesOffset);
                    if (lightValue > 1)
                        queue.Enqueue((index, chunk, lightValue - 1));
                }
            }
        }
        public static void PropagateShadow(int x, int y, int z, Chunk chunk, HashSet<Chunk> Set) {
            int index = x + y * ChunkSize + z * ChunkSizeSquared;
            PropagateShadow(index, chunk, RedLight, Set);
            PropagateShadow(index, chunk, GreenLight, Set);
            PropagateShadow(index, chunk, BlueLight, Set);
            PropagateShadow(index, chunk, SkyLight, Set);
        }
        private static void PropagateShadow(int startIndex, Chunk startChunk, int bytesOffset, HashSet<Chunk> Set) {
            Queue<(int, Chunk, int)> shadowQueue = [];
            Queue<(int, Chunk, int)> lightQueue = [];
            int currentValue = ((startChunk.blockLightValues[startIndex] >> bytesOffset) & lightMask) - 1;
            shadowQueue.Enqueue((startIndex, startChunk, currentValue));
            startChunk.blockLightValues[startIndex] &= (ushort)~(lightMask << bytesOffset);
            while (shadowQueue.Count > 0) {
                (int index, Chunk chunk, int oldValue) = shadowQueue.Dequeue();

                CheckNeighborShadow(index, Direction.PosX, chunk, oldValue, bytesOffset, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(index, Direction.NegX, chunk, oldValue, bytesOffset, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(index, Direction.PosY, chunk, oldValue, bytesOffset, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(index, Direction.NegY, chunk, (oldValue == lightMask - 1) && (bytesOffset == SkyLight) ? oldValue + 1 : oldValue, bytesOffset, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(index, Direction.PosZ, chunk, oldValue, bytesOffset, shadowQueue, lightQueue, Set);
                CheckNeighborShadow(index, Direction.NegZ, chunk, oldValue, bytesOffset, shadowQueue, lightQueue, Set);
            }
            PropagateLight(bytesOffset, lightQueue, Set);
        }
        private static void CheckNeighborShadow(int index, Direction dir, Chunk chunk, int oldValue, int bytesOffset, Queue<(int, Chunk, int)> shadowQueue, Queue<(int, Chunk, int)> lightQueue, HashSet<Chunk> Set) {
            if (!CheckChunkBoundry(ref index, dir, ref chunk, Set)) return;
            if (Blocks.IsTransparent(chunk.blocks[index]) || Blocks.IsLightEminiting(chunk.blocks[index])) {
                int current = ((chunk.blockLightValues[index] >> bytesOffset) & lightMask);
                if (current == oldValue) {
                    if (current <= 0) return;
                    shadowQueue.Enqueue((index, chunk, current - 1));
                    chunk.blockLightValues[index] &= (ushort)~(lightMask << bytesOffset);
                }
                else if (current > oldValue) {
                    chunk.blockLightValues[index] &= (ushort)~(lightMask << bytesOffset);
                    chunk.blockLightValues[index] |= (ushort)(current << bytesOffset);
                    if (current > 1)
                        lightQueue.Enqueue((index, chunk, current - 1));
                }
            }
        }
        private static bool CheckChunkBoundry(ref int index, Direction dir, ref Chunk chunk, HashSet<Chunk> Set) {
            switch (dir) {
                case Direction.PosX:
                    if ((index & (ChunkSize - 1)) == (ChunkSize - 1)) {
                        if (!chunk.world.WorldMap.TryGetValue((chunk.coordinates.x + 1, chunk.coordinates.y, chunk.coordinates.z), out chunk)) return false;
                        index &= ~(ChunkSize - 1);
                        Set?.Add(chunk);
                        return true;
                    }
                    break;
                case Direction.NegX:
                    if ((index & (ChunkSize - 1)) == 0) {
                        if (!chunk.world.WorldMap.TryGetValue((chunk.coordinates.x - 1, chunk.coordinates.y, chunk.coordinates.z), out chunk)) return false;
                        index |= ChunkSize - 1;
                        Set?.Add(chunk);
                        return true;
                    }
                    break;
                case Direction.PosY:
                    if (((index >> 6) & (ChunkSize - 1)) == (ChunkSize - 1)) {
                        if (!chunk.world.WorldMap.TryGetValue((chunk.coordinates.x, chunk.coordinates.y + 1, chunk.coordinates.z), out chunk)) return false;
                        index &= ~((ChunkSize - 1) << 6);
                        Set?.Add(chunk);
                        return true;
                    }
                    break;
                case Direction.NegY:
                    if (((index >> 6) & (ChunkSize - 1)) == 0) {
                        if (!chunk.world.WorldMap.TryGetValue((chunk.coordinates.x, chunk.coordinates.y - 1, chunk.coordinates.z), out chunk)) return false;
                        index |= (ChunkSize - 1) << 6;
                        Set?.Add(chunk);
                        return true;
                    }
                    break;
                case Direction.PosZ:
                    if ((index >> 12) == (ChunkSize - 1)) {
                        if (!chunk.world.WorldMap.TryGetValue((chunk.coordinates.x, chunk.coordinates.y, chunk.coordinates.z + 1), out chunk)) return false;
                        index &= ~((ChunkSize - 1) << 12);
                        Set?.Add(chunk);
                        return true;
                    }
                    break;
                case Direction.NegZ:
                    if ((index >> 12) == 0) {
                        if (!chunk.world.WorldMap.TryGetValue((chunk.coordinates.x, chunk.coordinates.y, chunk.coordinates.z - 1), out chunk)) return false;
                        index |= (ChunkSize - 1) << 12;
                        Set?.Add(chunk);
                        return true;
                    }
                    break;
            }
            index += (int)dir;
            return true;
        }
    }
}
