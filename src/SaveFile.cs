using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace VoxelTechDemo;
static class SaveFile {
    // TODO: Don't save chunks that did not change or generated
    public static void SaveChunkLine(World world, int x, int z) {
        using BrotliStream writer = new(File.Create($"Save/{x},{z}"), CompressionLevel.Optimal);
        for (int y = 0; y < World.MaxYChunk; y++) {
            if (world.WorldMap.TryGetValue((x, y, z), out Chunk chunk)) {
                writer.Write(chunk.blocks);
            }
            else {
                Console.WriteLine($"Failed to save chunk line {x}, {z}");
                return;
            }
        }
    }
    public static bool TryLoadChunkLine(World world, int x, int z) {
        if (!File.Exists($"Save/{x},{z}")) {
            return false;
        }
        using BrotliStream stream = new(File.OpenRead($"Save/{x},{z}"), CompressionMode.Decompress);
        Queue<(int, Chunk, int)> redLightQueue = [];
        Queue<(int, Chunk, int)> greenLightQueue = [];
        Queue<(int, Chunk, int)> blueLightQueue = [];
        for (int y = 0; y < World.MaxYChunk; y++) {
            Chunk chunk = world.WorldMap[(x, y, z)];
            byte[] blocks = chunk.blocks;
            try {
                stream.ReadExactly(blocks);

                for (int index = 0; index < blocks.Length; index++) {
                    if (Blocks.IsLightEminiting(blocks[index])) {
                        (int red, int green, int blue) = Blocks.ReturnBlockLightValues(blocks[index]);
                        if (red != 0) redLightQueue.Enqueue((index, chunk, red));
                        if (green != 0) greenLightQueue.Enqueue((index, chunk, green));
                        if (blue != 0) blueLightQueue.Enqueue((index, chunk, blue));
                    }
                }

                chunk.IsGenerated = true;
            }
            catch {
                Console.WriteLine($"Failed to read chunk at {x},{y},{z}");
            }
        }
        Light.PropagateSkyLight(world.WorldMap[(x, World.MaxYChunk - 1, z)]);
        Light.PropagateLight(Light.RedLight, redLightQueue, null);
        Light.PropagateLight(Light.GreenLight, greenLightQueue, null);
        Light.PropagateLight(Light.BlueLight, blueLightQueue, null);

        return true;
    }
    private struct PlayerSaveFile {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        
        public int CX { get; set; }
        public int CY { get; set; }
        public int CZ { get; set; }

        public PlayerSaveFile(Player player) {
            (X, Y, Z) = player.camPosition;
            (CX, CY, CZ) = player.CurrentChunk;
        }
    }
    public static void SavePlayer(Player player) {
        File.WriteAllText("Save/Player.json", JsonSerializer.Serialize(new PlayerSaveFile(player)));
    }

    public static bool GetPlayerPosition(Player player) {
        if (!File.Exists("Save/Player.json")) {
            return false;
        }
        try {
            PlayerSaveFile file = JsonSerializer.Deserialize<PlayerSaveFile>(File.ReadAllText("Save/Player.json"));
            player.camPosition = new Vector3(file.X, file.Y, file.Z);
            player.CurrentChunk = (file.CX, file.CY, file.CZ);
            return true;
        }
        catch {
            Console.WriteLine("Failed to read player position");
            return false;
        }
    }
}