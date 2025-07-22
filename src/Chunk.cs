using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using static VoxelTechDemo.VoxelRenderer;
using static VoxelTechDemo.Light;

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
                PropagateShadow(x, y, z, this, Set);
                if ((currentLightValue & 31) < red) {
                    PropagateLight(x, y, z, this, red, 0, Set);
                }
                if (((currentLightValue >> 5) & 31) < green) {
                    PropagateLight(x, y, z, this, green, 5, Set);
                }
                if (((currentLightValue >> 10) & 31) < blue) {
                    PropagateLight(x, y, z, this, blue, 10, Set);
                }
            }
            else if (Id == 0 || !Blocks.IsTransparent(Id)) {
                PropagateShadow(x, y, z, this, Set);
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