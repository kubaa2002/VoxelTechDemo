using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using static VoxelTechDemo.VoxelRenderer;
using static VoxelTechDemo.Light;

namespace VoxelTechDemo{
    public class Chunk{
        public byte[] blocks = new byte[ChunkSizeCubed];
        public ushort[] blockLightValues = new ushort[ChunkSizeCubed];
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
                ushort currentLightValue = blockLightValues[x + y * ChunkSize + z * ChunkSizeSquared];
                PropagateShadow(x, y, z, this, Set);
                if ((currentLightValue & lightMask) < red) {
                    PropagateLight(x, y, z, this, red, 0, Set);
                }
                if (((currentLightValue >> bitsPerLight) & lightMask) < green) {
                    PropagateLight(x, y, z, this, green, bitsPerLight, Set);
                }
                if (((currentLightValue >> (2*bitsPerLight)) & lightMask) < blue) {
                    PropagateLight(x, y, z, this, blue, (2*bitsPerLight), Set);
                }
            }
            else if (Id == 0 || !Blocks.IsTransparent(Id)) {
                PropagateShadow(x, y, z, this, Set);
            }
        }
        public Color GetLightValues(int currentBlock, int face) {
            switch (face) {
                // x+
                case 0:
                    if (currentBlock % ChunkSize != ChunkSize - 1) {
                        return ConvertLightValues(blockLightValues[currentBlock + 1]);
                    }
                    else if (world.WorldMap.TryGetValue((coordinates.x + 1, coordinates.y, coordinates.z), out Chunk adjecentChunk)){
                        return ConvertLightValues(adjecentChunk.blockLightValues[-ChunkSize + 1 + currentBlock]);
                    }
                    break;
                // x-
                case 1:
                    if (currentBlock % ChunkSize != 0) {
                        return ConvertLightValues(blockLightValues[currentBlock - 1]);
                    }
                    else if (world.WorldMap.TryGetValue((coordinates.x - 1, coordinates.y, coordinates.z), out Chunk adjecentChunk)) {
                        return ConvertLightValues(adjecentChunk.blockLightValues[ChunkSize - 1 + currentBlock]);
                    }
                    break;
                // y+
                case 2:
                    if (currentBlock / ChunkSize % ChunkSize != ChunkSize - 1) {
                        return ConvertLightValues(blockLightValues[currentBlock + ChunkSize]);
                    }
                    else if (world.WorldMap.TryGetValue((coordinates.x, coordinates.y + 1, coordinates.z), out Chunk adjacentChunk)) {
                        return ConvertLightValues(adjacentChunk.blockLightValues[-ChunkSize * (ChunkSize - 1) + currentBlock]);
                    }
                    break;
                // y-
                case 3:
                    if (currentBlock / ChunkSize % ChunkSize != 0) {
                        return ConvertLightValues(blockLightValues[currentBlock - ChunkSize]);
                    }
                    else if (world.WorldMap.TryGetValue((coordinates.x, coordinates.y - 1, coordinates.z), out Chunk adjacentChunk)) {
                        return ConvertLightValues(adjacentChunk.blockLightValues[ChunkSize * (ChunkSize - 1) + currentBlock]);
                    }
                    break;
                // z+
                case 4:
                    if (currentBlock / ChunkSizeSquared != ChunkSize - 1) {
                        return ConvertLightValues(blockLightValues[currentBlock + ChunkSizeSquared]);
                    }
                    else if (world.WorldMap.TryGetValue((coordinates.x, coordinates.y, coordinates.z + 1), out Chunk adjacentChunk)) {
                        return ConvertLightValues(adjacentChunk.blockLightValues[-ChunkSizeSquared * (ChunkSize - 1) + currentBlock]);
                    }
                    break;
                // z-
                case 5:
                    if (currentBlock / ChunkSizeSquared != 0) {
                        return ConvertLightValues(blockLightValues[currentBlock - ChunkSizeSquared]);
                    }
                    else if (world.WorldMap.TryGetValue((coordinates.x, coordinates.y, coordinates.z - 1), out Chunk adjacentChunk)) {
                        return ConvertLightValues(adjacentChunk.blockLightValues[ChunkSizeSquared * (ChunkSize - 1) + currentBlock]);
                    }
                    break;
            }
            return Color.White;
        }
        private static Color ConvertLightValues(ushort value) {
            return new Color((value & lightMask)*17, ((value >> bitsPerLight) & lightMask)*17, ((value >> (2*bitsPerLight)) & lightMask)*17, (value >> (3*bitsPerLight))*17);
        }
    }
}