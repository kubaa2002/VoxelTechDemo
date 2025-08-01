using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace VoxelTechDemo {
    static class SaveFile {
        public static void SaveChunkLine(World world, int x, int z) {
            using BrotliStream writer = new(File.OpenWrite($"Save/{x},{z}"), CompressionLevel.Optimal);
            for (int y = 0; y < 8; y++) {
                writer.Write(world.WorldMap[(x, y, z)].blocks);
            }
        }
        public static bool TryLoadChunkLine(World world, int x, int z) {
            if (File.Exists($"Save/{x},{z}")) {
                using BrotliStream stream = new(File.OpenRead($"Save/{x},{z}"), CompressionMode.Decompress);
                Queue<(int, Chunk, int)> RedLightQueue = [];
                Queue<(int, Chunk, int)> GreenLightQueue = [];
                Queue<(int, Chunk, int)> BlueLightQueue = [];
                for (int y = 0; y < World.MaxYChunk; y++) {
                    Chunk chunk = world.WorldMap[(x, y, z)];
                    var blocks = chunk.blocks;
                    stream.Read(blocks);

                    for (int index = 0; index < blocks.Length; index++) {
                        if (Blocks.IsLightEminiting(blocks[index])) {
                            (int red, int green, int blue) = Blocks.ReturnBlockLightValues(blocks[index]);
                            if (red != 0) RedLightQueue.Enqueue((index, chunk, red));
                            if (green != 0) GreenLightQueue.Enqueue((index, chunk, green));
                            if (blue != 0) BlueLightQueue.Enqueue((index, chunk, blue));
                        }
                    }
                    chunk.IsGenerated = true;
                }
                Light.PropagateSkyLight(world.WorldMap[(x, World.MaxYChunk - 1, z)]);
                Light.PropagateLight(Light.RedLight, RedLightQueue, null);
                Light.PropagateLight(Light.GreenLight, GreenLightQueue, null);
                Light.PropagateLight(Light.BlueLight, BlueLightQueue, null);

                return true;
            }
            return false;
        }
    }
}