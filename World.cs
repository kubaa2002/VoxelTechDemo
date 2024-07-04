using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo
{
    public class World
    {
        public ConcurrentDictionary<(int,int,int),Chunk> WorldMap;
        readonly long seed;
        public World(long seed)
        {
            WorldMap = new ConcurrentDictionary<(int,int,int),Chunk>();
            this.seed = seed;
        }
        public void SetBlock(int x, int y, int z, byte Id)
        {
            (int x,int y,int z) chunkCoordinates = ((int)Math.Floor((double)x/ChunkSize),(int)Math.Floor((double)y/ChunkSize),(int)Math.Floor((double)z/ChunkSize));
            if(!WorldMap.ContainsKey(chunkCoordinates)){
                WorldMap.TryAdd((chunkCoordinates.x,chunkCoordinates.y,chunkCoordinates.z),new(chunkCoordinates.x,chunkCoordinates.y,chunkCoordinates.z,this));
            }
            Chunk chunk = WorldMap[chunkCoordinates];
            chunk.blocks[Mod(x,ChunkSize)+(Mod(y,ChunkSize)*ChunkSize)+(Mod(z,ChunkSize)*square)]=Id;
            Dictionary<Chunk,VertexBuffer[]> buffers = new();
            if(Mod(x,ChunkSize)==0){
                if(WorldMap.ContainsKey((chunkCoordinates.x-1,chunkCoordinates.y,chunkCoordinates.z))){
                    Chunk chunk2 = WorldMap[(chunkCoordinates.x-1,chunkCoordinates.y,chunkCoordinates.z)];
                    buffers[chunk2] = GenerateVertices(chunk2);
                }
            }
            if(Mod(x,ChunkSize)==ChunkSize-1){
                if(WorldMap.ContainsKey((chunkCoordinates.x+1,chunkCoordinates.y,chunkCoordinates.z))){
                    Chunk chunk2 = WorldMap[(chunkCoordinates.x+1,chunkCoordinates.y,chunkCoordinates.z)];
                    buffers[chunk2] = GenerateVertices(chunk2);
                }
            }
            if(Mod(y,ChunkSize)==0){
                if(WorldMap.ContainsKey((chunkCoordinates.x,chunkCoordinates.y-1,chunkCoordinates.z))){
                    Chunk chunk2 = WorldMap[(chunkCoordinates.x,chunkCoordinates.y-1,chunkCoordinates.z)];
                    buffers[chunk2] = GenerateVertices(chunk2);
                }
            }
            if(Mod(y,ChunkSize)==ChunkSize-1){
                if(WorldMap.ContainsKey((chunkCoordinates.x,chunkCoordinates.y+1,chunkCoordinates.z))){
                    Chunk chunk2 = WorldMap[(chunkCoordinates.x,chunkCoordinates.y+1,chunkCoordinates.z)];
                    buffers[chunk2] = GenerateVertices(chunk2);
                }
            }
            if(Mod(z,ChunkSize)==0){
                if(WorldMap.ContainsKey((chunkCoordinates.x,chunkCoordinates.y,chunkCoordinates.z-1))){
                    Chunk chunk2 = WorldMap[(chunkCoordinates.x,chunkCoordinates.y,chunkCoordinates.z-1)];
                    buffers[chunk2] = GenerateVertices(chunk2);
                }
            }
            if(Mod(z,ChunkSize)==ChunkSize-1){
                if(WorldMap.ContainsKey((chunkCoordinates.x,chunkCoordinates.y,chunkCoordinates.z+1))){
                    Chunk chunk2 = WorldMap[(chunkCoordinates.x,chunkCoordinates.y,chunkCoordinates.z+1)];
                    buffers[chunk2] = GenerateVertices(chunk2);
                }
            }
            buffers[chunk]=GenerateVertices(chunk);
            foreach(KeyValuePair<Chunk,VertexBuffer[]> pair in buffers){
                VertexBuffer oldBuffer = pair.Key.vertexBufferOpaque;
                pair.Key.vertexBufferOpaque = pair.Value[0];
                oldBuffer?.Dispose();
                oldBuffer = pair.Key.vertexBufferTransparent;
                pair.Key.vertexBufferTransparent = pair.Value[1];
                oldBuffer?.Dispose();
            }
        }
        public void SetBlockWithoutUpdating(int x,int y,int z,byte Id)
        {
            (int x,int y,int z) chunkCoordinates = ((int)Math.Floor((double)x/ChunkSize),(int)Math.Floor((double)y/ChunkSize),(int)Math.Floor((double)z/ChunkSize));
            if(!WorldMap.ContainsKey(chunkCoordinates)){
                WorldMap.TryAdd((chunkCoordinates.x,chunkCoordinates.y,chunkCoordinates.z),new(chunkCoordinates.x,chunkCoordinates.y,chunkCoordinates.z,this));
            }
            WorldMap[chunkCoordinates].blocks[Mod(x,ChunkSize)+(Mod(y,ChunkSize)*ChunkSize)+(Mod(z,ChunkSize)*square)]=Id;
        }
        public byte GetBlock(int x, int y, int z)
        {
            if(WorldMap.ContainsKey(((int)Math.Floor((double)x/ChunkSize),(int)Math.Floor((double)y/ChunkSize),(int)Math.Floor((double)z/ChunkSize))))
            {
                Chunk chunk = WorldMap[((int)Math.Floor((double)x/ChunkSize),(int)Math.Floor((double)y/ChunkSize),(int)Math.Floor((double)z/ChunkSize))];
                return chunk.blocks[Mod(x,ChunkSize)+(Mod(y,ChunkSize)*ChunkSize)+(Mod(z,ChunkSize)*square)];
            }
            else
            {
                return 0;
            }
        }
        public void SetBlock(int x,int y,int z, byte Id, BlockFace blockSide, BoundingBox PlayerHitBox)
        {
            if(blockSide == BlockFace.Front)
            {
                if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x,y,z-1),new Vector3(x+1,y+1,z)))){
                    SetBlock(x,y,z-1,Id);
                }
            }
            if(blockSide == BlockFace.Back)
            {
                if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x,y,z+1),new Vector3(x+1,y+1,z+2)))){
                    SetBlock(x,y,z+1,Id);
                }
            }
            if(blockSide == BlockFace.Right)
            {
                if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x-1,y,z),new Vector3(x,y+1,z+1)))){
                    SetBlock(x-1,y,z,Id);
                }
            }
            if(blockSide == BlockFace.Left)
            {
                if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x+1,y,z),new Vector3(x+2,y+1,z+1)))){
                    SetBlock(x+1,y,z,Id);
                }
            }
            if(blockSide == BlockFace.Top)
            {
                if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x,y+1,z),new Vector3(x+1,y+2,z+1)))){
                    SetBlock(x,y+1,z,Id);
                }
            }
            if(blockSide == BlockFace.Bottom)
            {
                if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x,y-1,z),new Vector3(x+1,y,z+1)))){
                    SetBlock(x,y-1,z,Id);
                }
            }
        }
        public static int Mod(int a, int b){
            return (a%=b)<0 ? a+b : a;
        }
        public Task GenerateChunkLineAsync(int chunkX,int chunkZ){
            InitializeChunkLine(chunkX,chunkZ);
            return Task.Run(()=>{
                GenerateTerrain(chunkX,chunkZ);
            });
        }
        public void GenerateChunkLine(int chunkX,int chunkZ){
            InitializeChunkLine(chunkX,chunkZ);
            GenerateTerrain(chunkX,chunkZ);
        }
        public void InitializeChunkLine(int chunkX, int chunkZ){
            for(int i=0;i<8;i++){
                WorldMap.TryAdd((chunkX,i,chunkZ),new(chunkX,i,chunkZ,this));
            }
        }
        public void GenerateTerrain(int chunkX,int chunkZ){
            Chunk[] chunks = new Chunk[8];
            for(int i=0;i<8;i++){
                chunks[i]=WorldMap[(chunkX,i,chunkZ)];
            }
            int yLevel;
            int blockPossition;
            Chunk chunk = chunks[0];
            for(int x=0;x<ChunkSize;x++){
                for(int z=0;z<ChunkSize;z++){
                    yLevel = 50+(int)Math.Floor(MountainNoise(chunkX*ChunkSize+x,chunkZ*ChunkSize+z));
                    blockPossition = x+z*square;
                    for(int y=ChunkSize*8-1;y>=0;y--){
                        if(y%64==63){
                            chunk = chunks[y/ChunkSize];
                            blockPossition+=square;
                        }
                        blockPossition-=ChunkSize;
                        if(y<=yLevel){
                            if(y>=yLevel-2){
                                if(y == yLevel){
                                    chunk.blocks[blockPossition]=1;
                                }
                                else{
                                    chunk.blocks[blockPossition]=2;
                                }
                            }
                            else{
                                chunk.blocks[blockPossition]=3;
                            } 
                            if(y==yLevel){
                                if(yLevel<65){
                                    chunk.blocks[blockPossition]=12;
                                }
                                if(yLevel<62){
                                    chunk.blocks[blockPossition]=11;
                                }
                            }
                        }
                        else{
                            if(y<64){
                                chunk.blocks[blockPossition]=14;
                            } 
                        }
                    }
                    if(OpenSimplex2.Noise2(seed,chunkX*ChunkSize+x,chunkZ*ChunkSize+z) > 0.95 && yLevel>=65){
                        CreateTree(chunkX*ChunkSize+x,yLevel,chunkZ*ChunkSize+z);
                    }
                }
            }
        }
        private void CreateTree(int x,int y, int z){
            for(int tempy=4;tempy<=5;tempy++){
                for(int tempx=-2;tempx<=2;tempx++){
                    for(int tempz=-2;tempz<=2;tempz++){
                        if(!((tempx == -2 || tempx == 2)&&(tempz == -2 || tempz == 2))){
                            if(GetBlock(x+tempx,y+tempy,z+tempz)==0){
                                SetBlockWithoutUpdating(x+tempx,y+tempy,z+tempz,7);
                            }
                        }
                    }
                }
            }
            for(int tempy=6;tempy<=7;tempy++){
                for(int tempx=-1;tempx<=1;tempx++){
                    for(int tempz=-1;tempz<=1;tempz++){
                        if(!((tempx == -1 || tempx == 1)&&(tempz == -1 || tempz == 1))){
                            if(GetBlock(x+tempx,y+tempy,z+tempz)==0){
                                SetBlockWithoutUpdating(x+tempx,y+tempy,z+tempz,7);
                            }
                        }
                    }
                }
            }
            for(int i=1;i<=6;i++){
                SetBlockWithoutUpdating(x,y+i,z,5);
            }
            SetBlockWithoutUpdating(x,y,z,2);
        }
        public double TerrainNoise(int x,int z){
            return OpenSimplex2.Noise2(seed,((double)x)/2000,((double)z)/2000)*160
                        + OpenSimplex2.Noise2(seed,((double)x)/400,((double)z)/400)*32
                        + OpenSimplex2.Noise2(seed,((double)x)/100,((double)z)/100)*8;
        }
        public double MountainNoise(int x,int z){
            return Math.Pow(OpenSimplex2.Noise2(seed,((double)x)/2000,((double)z)/2000)*15
                        + OpenSimplex2.Noise2(seed,((double)x)/400,((double)z)/400)*4
                        + OpenSimplex2.Noise2(seed,((double)x)/100,((double)z)/100),2);
        }
    }
}