using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace VoxelTechDemo;
static class SaveFile {
    private struct SavedChunk(Chunk chunk) {
        public readonly byte[] Blocks = chunk.blocks;
        public readonly Dictionary<int, byte> BlockStates = chunk.BlockStates;
        public readonly int Y = chunk.coordinates.y;
    }
    // TODO: Don't save chunks that did not change
    public static void SaveChunkLine(World world, int x, int z) {
        using BrotliStream writer = new(File.Create($"Save/{x},{z}"), CompressionLevel.Optimal);
        List<SavedChunk> chunks = [];
        for (int y = 0; y < World.MaxYChunk; y++) {
            if (world.WorldMap.TryGetValue((x, y, z), out Chunk chunk)) {
                chunks.Add(new SavedChunk(chunk));
            }
        }

        
        writer.Write([(byte)chunks.Count]);
        Dictionary<int,Dictionary<int, byte>> dict = [];
        foreach (SavedChunk chunk in chunks) {
            writer.Write([(byte)chunk.Y]);
            writer.Write(chunk.Blocks);
            dict[chunk.Y] = chunk.BlockStates;
        }
        writer.Write(JsonSerializer.SerializeToUtf8Bytes(dict));
    }
    public static Chunk TryLoadChunkLine(World world, int x, int z) {
        if (!File.Exists($"Save/{x},{z}")) {
            return null;
        }
        using BrotliStream stream = new(File.OpenRead($"Save/{x},{z}"), CompressionMode.Decompress);
        Queue<(int, Chunk, int)> redLightQueue = [];
        Queue<(int, Chunk, int)> greenLightQueue = [];
        Queue<(int, Chunk, int)> blueLightQueue = [];
        
        int maxY = 0;
        Chunk maxYChunk = null;
        int count = stream.ReadByte();
        for (int i = 0; i < count; i++) {
            int y = stream.ReadByte();
            world.WorldMap.TryAdd((x, y, z), new Chunk((x, y, z), world));
            Chunk chunk = world.WorldMap[(x, y, z)];
            byte[] blocks = chunk.blocks;
            if (y > maxY) {
                maxY = y;
                maxYChunk = chunk;
            }
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
            catch(Exception e) {
                Console.WriteLine($"Failed to read chunk at {x},{y},{z} due to {e.Message}");
            }
        }
        
        Dictionary<int,Dictionary<int, byte>> dict = JsonSerializer.Deserialize<Dictionary<int,Dictionary<int, byte>>>(stream);
        foreach (KeyValuePair<int, Dictionary<int, byte>> pair in dict) {
            if (world.WorldMap.TryGetValue((x, pair.Key, z), out Chunk chunk)) {
                chunk.BlockStates = pair.Value;
            }
        }
        Light.PropagateLight(Light.RedLight, redLightQueue, null);
        Light.PropagateLight(Light.GreenLight, greenLightQueue, null);
        Light.PropagateLight(Light.BlueLight, blueLightQueue, null);

        // Ensure that there is no hole in the line of chunks
        for (int y = 0; y < maxY; y++) {
            if (!world.WorldMap.ContainsKey((x, y, z))) {
                world.WorldMap.TryAdd((x, y, z), new Chunk((x, y, z), world));
            }
        }
        
        return maxYChunk;
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