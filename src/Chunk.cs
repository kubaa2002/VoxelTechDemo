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
        public void UpdateLight(int x, int y, int z, byte Id, Dictionary<(int, int, int), Chunk> Dict) {
            if (Blocks.IsLightEminiting(Id)) {
                (int red, int green, int blue) = Blocks.ReturnBlockLightValues(Id);
                if ((blockLightValues[x + y * ChunkSize + z * ChunkSizeSquared] & 31) < red) {
                    PropagateLight(x, y, z, red, 0, Dict);
                }
                if (((blockLightValues[x + y * ChunkSize + z * ChunkSizeSquared] >> 5) & 31) < green) {
                    PropagateLight(x, y, z, green, 5, Dict);
                }
                if (((blockLightValues[x + y * ChunkSize + z * ChunkSizeSquared] >> 10) & 31) < blue) {
                    PropagateLight(x, y, z, blue, 10, Dict);
                }
            }
            else {
                if (Id == 0) {
                    if (Blocks.IsLightEminiting(blocks[x + y * ChunkSize + z * ChunkSizeSquared])) {
                        PropagateShadow(x, y, z, 0, Dict);
                        PropagateShadow(x, y, z, 5, Dict);
                        PropagateShadow(x, y, z, 10, Dict);
                    }
                    else {
                        UpdateSingleBlockLightValue(x, y, z, Dict);
                    }
                }
            }
        }
        // TOFIX: Some blocks in the corner of chunks are not light up properly, could be casued by checking wrong block possitions somewhere
        private void PropagateLight(int x, int y, int z, int color, int bytesOffset, Dictionary<(int, int, int), Chunk> Dict) {
            blockLightValues[x + y * ChunkSize + z * ChunkSizeSquared] &= (short)~(31 << bytesOffset);
            blockLightValues[x + y * ChunkSize + z * ChunkSizeSquared] |= (short)(color << bytesOffset);
            if (x != 0) {
                if (Blocks.IsTransparent(blocks[x - 1 + y * ChunkSize + z * ChunkSizeSquared]) && ((blockLightValues[x - 1 + y * ChunkSize + z * ChunkSizeSquared] >> bytesOffset) & 31) < color - 1) {
                    PropagateLight(x - 1, y, z, color - 1, bytesOffset, Dict);
                }
            }
            else {
                if (world.WorldMap.TryGetValue((coordinates.x - 1, coordinates.y, coordinates.z), out Chunk adjacentChunk)) {
                    Dict[(coordinates.x - 1, coordinates.y, coordinates.z)] = adjacentChunk;
                    if (Blocks.IsTransparent(adjacentChunk.blocks[ChunkSize - 1 + y * ChunkSize + z * ChunkSizeSquared]) && ((adjacentChunk.blockLightValues[ChunkSize - 1 + y * ChunkSize + z * ChunkSizeSquared] >> bytesOffset) & 31) < color - 1) {
                        adjacentChunk.PropagateLight(ChunkSize - 1, y, z, color - 1, bytesOffset, Dict);
                    }
                }
            }
            if (x != ChunkSize - 1) {
                if (Blocks.IsTransparent(blocks[x + 1 + y * ChunkSize + z * ChunkSizeSquared]) && ((blockLightValues[x + 1 + y * ChunkSize + z * ChunkSizeSquared] >> bytesOffset) & 31) < color - 1) {
                    PropagateLight(x + 1, y, z, color - 1, bytesOffset, Dict);
                }
            }
            else {
                if (world.WorldMap.TryGetValue((coordinates.x + 1, coordinates.y, coordinates.z), out Chunk adjacentChunk)) {
                    Dict[(coordinates.x + 1, coordinates.y, coordinates.z)] = adjacentChunk;
                    if (Blocks.IsTransparent(adjacentChunk.blocks[y * ChunkSize + z * ChunkSizeSquared]) && ((adjacentChunk.blockLightValues[y * ChunkSize + z * ChunkSizeSquared] >> bytesOffset) & 31) < color - 1) {
                        adjacentChunk.PropagateLight(0, y, z, color - 1, bytesOffset, Dict);
                    }
                }
            }
            if (y != 0) {
                if (Blocks.IsTransparent(blocks[x + (y - 1) * ChunkSize + z * ChunkSizeSquared]) && ((blockLightValues[x + (y - 1) * ChunkSize + z * ChunkSizeSquared] >> bytesOffset) & 31) < color - 1) {
                    PropagateLight(x, y - 1, z, color - 1, bytesOffset, Dict);
                }
            }
            else {
                if (world.WorldMap.TryGetValue((coordinates.x, coordinates.y - 1, coordinates.z), out Chunk adjacentChunk)) {
                    Dict[(coordinates.x, coordinates.y - 1, coordinates.z)] = adjacentChunk;
                    if (Blocks.IsTransparent(adjacentChunk.blocks[x + ChunkSize * (ChunkSize - 1) + z * ChunkSizeSquared]) && ((adjacentChunk.blockLightValues[x + (ChunkSize - 1) * ChunkSize + z * ChunkSizeSquared] >> bytesOffset) & 31) < color - 1) {
                        adjacentChunk.PropagateLight(x, ChunkSize - 1, z, color - 1, bytesOffset, Dict);
                    }
                }
            }
            if (y != ChunkSize - 1) {
                if (Blocks.IsTransparent(blocks[x + (y + 1) * ChunkSize + z * ChunkSizeSquared]) && ((blockLightValues[x + (y + 1) * ChunkSize + z * ChunkSizeSquared] >> bytesOffset) & 31) < color - 1) {
                    PropagateLight(x, y + 1, z, color - 1, bytesOffset, Dict);
                }
            }
            else {
                if (world.WorldMap.TryGetValue((coordinates.x, coordinates.y + 1, coordinates.z), out Chunk adjacentChunk)) {
                    Dict[(coordinates.x, coordinates.y + 1, coordinates.z)] = adjacentChunk;
                    if (Blocks.IsTransparent(adjacentChunk.blocks[x + z * ChunkSizeSquared]) && ((adjacentChunk.blockLightValues[x + z * ChunkSizeSquared] >> bytesOffset) & 31) < color - 1) {
                        adjacentChunk.PropagateLight(x, 0, z, color - 1, bytesOffset, Dict);
                    }
                }
            }
            if (z != 0) {
                if (Blocks.IsTransparent(blocks[x + y * ChunkSize + (z - 1) * ChunkSizeSquared]) && ((blockLightValues[x + y * ChunkSize + (z - 1) * ChunkSizeSquared] >> bytesOffset) & 31) < color - 1) {
                    PropagateLight(x, y, z - 1, color - 1, bytesOffset, Dict);
                }
            }
            else {
                if (world.WorldMap.TryGetValue((coordinates.x, coordinates.y, coordinates.z - 1), out Chunk adjacentChunk)) {
                    Dict[(coordinates.x, coordinates.y, coordinates.z - 1)] = adjacentChunk;
                    if (Blocks.IsTransparent(adjacentChunk.blocks[x + y * ChunkSize + (ChunkSize - 1) * ChunkSizeSquared]) && ((adjacentChunk.blockLightValues[x + y * ChunkSize] >> bytesOffset) & 31) < color - 1) {
                        adjacentChunk.PropagateLight(x, y, ChunkSize - 1, color - 1, bytesOffset, Dict);
                    }
                }
            }
            if (z != ChunkSize - 1) {
                if (Blocks.IsTransparent(blocks[x + y * ChunkSize + (z + 1) * ChunkSizeSquared]) && ((blockLightValues[x + y * ChunkSize + (z + 1) * ChunkSizeSquared] >> bytesOffset) & 31) < color - 1) {
                    PropagateLight(x, y, z + 1, color - 1, bytesOffset, Dict);
                }
            }
            else {
                if (world.WorldMap.TryGetValue((coordinates.x, coordinates.y, coordinates.z + 1), out Chunk adjacentChunk)) {
                    Dict[(coordinates.x, coordinates.y, coordinates.z + 1)] = adjacentChunk;
                    if (Blocks.IsTransparent(adjacentChunk.blocks[x + y * ChunkSize]) && ((adjacentChunk.blockLightValues[x + y * ChunkSize] >> bytesOffset) & 31) < color - 1) {
                        adjacentChunk.PropagateLight(x, y, 0, color - 1, bytesOffset, Dict);
                    }
                }
            }
        }
        public void UpdateSingleBlockLightValue(int x, int y, int z, Dictionary<(int, int, int), Chunk> Dict) {
            short value = 0;
            if (x != 0) {
                if (blockLightValues[x - 1 + y * ChunkSize + z * ChunkSizeSquared] > 0) {
                    value = blockLightValues[x - 1 + y * ChunkSize + z * ChunkSizeSquared];
                }
            }
            else {
                if (world.WorldMap.TryGetValue((coordinates.x - 1, coordinates.y, coordinates.z), out Chunk chunk)) {
                    value = chunk.blockLightValues[ChunkSize - 1 + y * ChunkSize + z * ChunkSizeSquared];
                }
            }
            if (x != ChunkSize - 1) {
                value = CalculateLightValue(x + 1, y, z, value);
            }
            else {
                if (world.WorldMap.TryGetValue((coordinates.x + 1, coordinates.y, coordinates.z), out Chunk chunk)) {
                    value = chunk.CalculateLightValue(0, y, z, value);
                }
            }
            if (y != 0) {
                value = CalculateLightValue(x, y - 1, z, value);
            }
            else {
                if (world.WorldMap.TryGetValue((coordinates.x, coordinates.y - 1, coordinates.z), out Chunk chunk)) {
                    value = chunk.CalculateLightValue(x, ChunkSize - 1, z, value);
                }
            }
            if (y != ChunkSize - 1) {
                value = CalculateLightValue(x, y + 1, z, value);
            }
            else {
                if (world.WorldMap.TryGetValue((coordinates.x, coordinates.y + 1, coordinates.z), out Chunk chunk)) {
                    value = chunk.CalculateLightValue(x, 0, z, value);
                }
            }
            if (z != 0) {
                value = CalculateLightValue(x, y, z - 1, value);
            }
            else {
                if (world.WorldMap.TryGetValue((coordinates.x, coordinates.y, coordinates.z - 1), out Chunk chunk)) {
                    value = chunk.CalculateLightValue(x, y, ChunkSize - 1, value);
                }
            }
            if (z != ChunkSize - 1) {
                value = CalculateLightValue(x, y, z + 1, value);
            }
            else {
                if (world.WorldMap.TryGetValue((coordinates.x, coordinates.y, coordinates.z + 1), out Chunk chunk)) {
                    value = chunk.CalculateLightValue(x, y, 0, value);
                }
            }
            if ((value & 31) != 0) {
                value--;
            }
            if (((value >> 5) & 31) != 0) {
                value -= 32;
            }
            if (((value >> 10) & 31) != 0) {
                value -= 1024;
            }
            PropagateLight(x, y, z, value & 31, 0, Dict);
            PropagateLight(x, y, z, (value >> 5) & 31, 5, Dict);
            PropagateLight(x, y, z, (value >> 10) & 31, 10, Dict);
        }
        private short CalculateLightValue(int x, int y, int z, short lightValue) {
            short adjacentLightValue = blockLightValues[x + y * ChunkSize + z * ChunkSizeSquared];
            if ((adjacentLightValue & 31) > (lightValue & 31)) {
                lightValue &= ~31;
                lightValue |= (short)(adjacentLightValue & 31);
            }
            if (((adjacentLightValue >> 5) & 31) > ((lightValue >> 5) & 31)) {
                lightValue &= ~(31 << 5);
                lightValue |= (short)((adjacentLightValue) & (31 << 5));
            }
            if (((adjacentLightValue >> 10) & 31) > ((lightValue >> 10) & 31)) {
                lightValue &= ~(31 << 10);
                lightValue |= (short)((adjacentLightValue) & (31 << 10));
            }
            return lightValue;
        }
        public void PropagateShadow(int x, int y, int z, int bytesOffset, Dictionary<(int, int, int), Chunk> Dict) {
            Queue<(int, int, int)> shadowQueue = [];
            Queue<(int, int, int)> lightQueue = [];
            shadowQueue.Enqueue((x, y, x));
            while (shadowQueue.Count > 0) {
                (int x, int y, int z) coords;
                coords = shadowQueue.Dequeue();
                int index = coords.x + coords.y * ChunkSize + coords.z * ChunkSizeSquared;
                byte oldValue = (byte)((blockLightValues[index] >> bytesOffset) & 31);
                blockLightValues[index] &= (short)~(31 << bytesOffset);
                if (x != 0) {
                    if (Blocks.IsTransparent(blocks[index - 1])) {
                        if (((blockLightValues[index - 1] >> bytesOffset) & 31) == oldValue - 1) {
                            shadowQueue.Enqueue((coords.x - 1, coords.y, coords.z));
                        }
                        else {
                            if (((blockLightValues[index - 1] >> bytesOffset) & 31) > oldValue - 1) {
                                lightQueue.Enqueue((coords.x - 1, coords.y, coords.z));
                            }
                        }
                    }
                }
                if (x != ChunkSize - 1) {
                    if (Blocks.IsTransparent(blocks[index + 1])) {
                        if (((blockLightValues[index + 1] >> bytesOffset) & 31) == oldValue - 1) {
                            shadowQueue.Enqueue((coords.x + 1, coords.y, coords.z));
                        }
                        else {
                            if (((blockLightValues[index + 1] >> bytesOffset) & 31) > oldValue - 1) {
                                lightQueue.Enqueue((coords.x + 1, coords.y, coords.z));
                            }
                        }
                    }
                }
                if (y != 0) {
                    if (Blocks.IsTransparent(blocks[index - ChunkSize])) {
                        if (((blockLightValues[index - ChunkSize] >> bytesOffset) & 31) == oldValue - 1) {
                            shadowQueue.Enqueue((coords.x, coords.y - 1, coords.z));
                        }
                        else {
                            if (((blockLightValues[index - ChunkSize] >> bytesOffset) & 31) > oldValue - 1) {
                                lightQueue.Enqueue((coords.x, coords.y - 1, coords.z));
                            }
                        }
                    }
                }
                if (y != ChunkSize - 1) {
                    if (Blocks.IsTransparent(blocks[index + ChunkSize])) {
                        if (((blockLightValues[index + ChunkSize] >> bytesOffset) & 31) == oldValue - 1) {
                            shadowQueue.Enqueue((coords.x, coords.y + 1, coords.z));
                        }
                        else {
                            if (((blockLightValues[index + ChunkSize] >> bytesOffset) & 31) > oldValue - 1) {
                                lightQueue.Enqueue((coords.x, coords.y + 1, coords.z));
                            }
                        }
                    }
                }
                if (z != 0) {
                    if (Blocks.IsTransparent(blocks[index - ChunkSizeSquared])) {
                        if (((blockLightValues[index - ChunkSizeSquared] >> bytesOffset) & 31) == oldValue - 1) {
                            shadowQueue.Enqueue((coords.x, coords.y, coords.z - 1));
                        }
                        else {
                            if (((blockLightValues[index - ChunkSizeSquared] >> bytesOffset) & 31) > oldValue - 1) {
                                lightQueue.Enqueue((coords.x, coords.y, coords.z - 1));
                            }
                        }
                    }
                }
                if (z != ChunkSize - 1) {
                    if (Blocks.IsTransparent(blocks[index + ChunkSizeSquared])) {
                        if (((blockLightValues[index + ChunkSizeSquared] >> bytesOffset) & 31) == oldValue - 1) {
                            shadowQueue.Enqueue((coords.x, coords.y, coords.z + 1));
                        }
                        else {
                            if (((blockLightValues[index + ChunkSizeSquared] >> bytesOffset) & 31) > oldValue - 1) {
                                lightQueue.Enqueue((coords.x, coords.y, coords.z + 1));
                            }
                        }
                    }
                }
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