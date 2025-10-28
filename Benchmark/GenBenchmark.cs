using BenchmarkDotNet.Attributes;
using VoxelTechDemo;

[MemoryDiagnoser]
public class MeshBenchmark {
    private World worldTerrain = new();
    private World worldMesh = new();
    private World worldLight = new();
    private Game1 game;
    
    [GlobalSetup]
    public void Setup() {
        game = new Game1();
        game.graphics.ApplyChanges();
        VoxelRenderer.InitializeVoxelRenderer(game.GraphicsDevice);
        for (int x = -4; x <= 4; x++) {
            for (int z = -4; z <= 4; z++) {
                VoxelTechDemo.TerrainGen.GenerateTerrain(worldLight,x,z);
                VoxelTechDemo.TerrainGen.GenerateTerrain(worldMesh,x,z);
            }
        }
    }

    [GlobalCleanup]
    public void Cleanup() {
        game?.Dispose();
    }

    [IterationSetup]
    public void IterationSetup() {
        worldTerrain.WorldMap.Clear();
        foreach (var chunk in worldLight.WorldMap.Values) {
            Array.Clear(chunk.blockLightValues, 0, chunk.blockLightValues.Length);
        }
    }
    
    [Benchmark]
    public void MeshGen() {
        for (int x = -3; x <= 3; x++) {
            for (int z = -3; z <= 3; z++) {
                for (int y = 0; y < 8; y++) {
                    if (worldMesh.WorldMap.TryGetValue((x, y, z), out Chunk chunk)) {
                        VoxelRenderer.GenerateChunkMesh(chunk);
                    }
                }
            }
        }
    }

    [Benchmark]
    public void TerrainGen() {
        for (int x = -3; x <= 3; x++) {
            for (int z = -3; z <= 3; z++) {
                VoxelTechDemo.TerrainGen.GenerateTerrain(worldTerrain,x,z);
            }
        }
    }

    [Benchmark]
    public void LightProp() {
        for (int x = -3; x <= 3; x++) {
            for (int z = -3; z <= 3; z++) {
                int y = World.MaxYChunk - 1;
                while (!worldLight.WorldMap.ContainsKey((x, y, z))) {
                    y--;
                }
                Light.PropagateSkyLight(worldLight.WorldMap[(x,y,z)]);
            }
        }
    }
}