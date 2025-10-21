using BenchmarkDotNet.Attributes;
using VoxelTechDemo;

[MemoryDiagnoser]
public class MeshBenchmark {
    private World worldTerrain = new(123);
    private World worldMesh = new(123);
    private World worldLight = new(123);
    private Game1 game;
    
    [GlobalSetup]
    public void Setup() {
        game = new Game1();
        game.graphics.ApplyChanges();
        VoxelRenderer.InitializeVoxelRenderer(game.GraphicsDevice);
        for (int x = -4; x <= 4; x++) {
            for (int z = -4; z <= 4; z++) {
                worldMesh.GenerateChunkLine(x,z);
                worldLight.GenerateChunkLine(x,z);
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
                    VoxelRenderer.GenerateChunkMesh(worldMesh.WorldMap[(x,y,z)]);
                }
            }
        }
    }

    [Benchmark]
    public void TerrainGen() {
        for (int x = -3; x <= 3; x++) {
            for (int z = -3; z <= 3; z++) {
                worldTerrain.GenerateTerrain(x,z);
            }
        }
    }

    [Benchmark]
    public void LightProp() {
        for (int x = -3; x <= 3; x++) {
            for (int z = -3; z <= 3; z++) {
                Light.PropagateSkyLight(worldLight.WorldMap[(x,World.MaxYChunk-1,z)]);
            }
        }
    }
}