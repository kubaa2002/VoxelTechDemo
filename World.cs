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
        public void SetBlock(int x,int y,int z,(int, int, int) chunkCoordinate, byte Id, BlockFace blockSide, BoundingBox PlayerHitBox)
        {
            if(blockSide == BlockFace.Front)
            {
                if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x,y,z-1),new Vector3(x+1,y+1,z)))){
                    SetBlock(x,y,z-1,chunkCoordinate,Id);
                }
            }
            if(blockSide == BlockFace.Back)
            {
                if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x,y,z+1),new Vector3(x+1,y+1,z+2)))){
                    SetBlock(x,y,z+1,chunkCoordinate,Id);
                }
            }
            if(blockSide == BlockFace.Right)
            {
                if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x-1,y,z),new Vector3(x,y+1,z+1)))){
                    SetBlock(x-1,y,z,chunkCoordinate,Id);
                }
            }
            if(blockSide == BlockFace.Left)
            {
                if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x+1,y,z),new Vector3(x+2,y+1,z+1)))){
                    SetBlock(x+1,y,z,chunkCoordinate,Id);
                }
            }
            if(blockSide == BlockFace.Top)
            {
                if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x,y+1,z),new Vector3(x+1,y+2,z+1)))){
                    SetBlock(x,y+1,z,chunkCoordinate,Id);
                }
            }
            if(blockSide == BlockFace.Bottom)
            {
                if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x,y-1,z),new Vector3(x+1,y,z+1)))){
                    SetBlock(x,y-1,z,chunkCoordinate,Id);
                }
            }
        }
        public void SetBlock(int x, int y, int z, (int x,int y,int z) chunkCoordinate,byte Id){
            NormalizeChunkCoordinates(ref x,ref y,ref z,ref chunkCoordinate);
            if(!WorldMap.ContainsKey(chunkCoordinate)){
                WorldMap.TryAdd((chunkCoordinate.x,chunkCoordinate.y,chunkCoordinate.z),new(chunkCoordinate.x,chunkCoordinate.y,chunkCoordinate.z,this));
            }
            Chunk chunk = WorldMap[chunkCoordinate];
            chunk.blocks[x+(y*ChunkSize)+(z*square)]=Id;
            Dictionary<Chunk,VertexBuffer[]> buffers = new();
            if(x==0){
                if(WorldMap.ContainsKey((chunkCoordinate.x-1,chunkCoordinate.y,chunkCoordinate.z))){
                    Chunk chunk2 = WorldMap[(chunkCoordinate.x-1,chunkCoordinate.y,chunkCoordinate.z)];
                    buffers[chunk2] = GenerateVertices(chunk2);
                }
            }
            if(x==ChunkSize-1){
                if(WorldMap.ContainsKey((chunkCoordinate.x+1,chunkCoordinate.y,chunkCoordinate.z))){
                    Chunk chunk2 = WorldMap[(chunkCoordinate.x+1,chunkCoordinate.y,chunkCoordinate.z)];
                    buffers[chunk2] = GenerateVertices(chunk2);
                }
            }
            if(y==0){
                if(WorldMap.ContainsKey((chunkCoordinate.x,chunkCoordinate.y-1,chunkCoordinate.z))){
                    Chunk chunk2 = WorldMap[(chunkCoordinate.x,chunkCoordinate.y-1,chunkCoordinate.z)];
                    buffers[chunk2] = GenerateVertices(chunk2);
                }
            }
            if(y==ChunkSize-1){
                if(WorldMap.ContainsKey((chunkCoordinate.x,chunkCoordinate.y+1,chunkCoordinate.z))){
                    Chunk chunk2 = WorldMap[(chunkCoordinate.x,chunkCoordinate.y+1,chunkCoordinate.z)];
                    buffers[chunk2] = GenerateVertices(chunk2);
                }
            }
            if(z==0){
                if(WorldMap.ContainsKey((chunkCoordinate.x,chunkCoordinate.y,chunkCoordinate.z-1))){
                    Chunk chunk2 = WorldMap[(chunkCoordinate.x,chunkCoordinate.y,chunkCoordinate.z-1)];
                    buffers[chunk2] = GenerateVertices(chunk2);
                }
            }
            if(z==ChunkSize-1){
                if(WorldMap.ContainsKey((chunkCoordinate.x,chunkCoordinate.y,chunkCoordinate.z+1))){
                    Chunk chunk2 = WorldMap[(chunkCoordinate.x,chunkCoordinate.y,chunkCoordinate.z+1)];
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
        public void SetBlockWithoutUpdating(int x,int y,int z,(int x,int y,int z) chunkCoordinate,byte Id){
            NormalizeChunkCoordinates(ref x,ref y,ref z,ref chunkCoordinate);
            if(!WorldMap.ContainsKey(chunkCoordinate)){
                WorldMap.TryAdd((chunkCoordinate.x,chunkCoordinate.y,chunkCoordinate.z),new(chunkCoordinate.x,chunkCoordinate.y,chunkCoordinate.z,this));
            }
            WorldMap[chunkCoordinate].blocks[x+y*ChunkSize+z*square]=Id;
        }
        public byte GetBlock(int x,int y,int z, (int x,int y,int z) chunkCoordinate){
            NormalizeChunkCoordinates(ref x,ref y,ref z,ref chunkCoordinate);
            if(WorldMap.ContainsKey(chunkCoordinate)){
                return WorldMap[chunkCoordinate].blocks[x+(y*ChunkSize)+(z*square)];
            }
            else{
                return 0;
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
                        CreateTree(x,Mod(yLevel,ChunkSize),z, chunks[yLevel/ChunkSize]);
                    }
                }
            }
        }
        private void CreateTree(int x,int y, int z, Chunk chunk){
            for(int tempy=4;tempy<=5;tempy++){
                for(int tempx=-2;tempx<=2;tempx++){
                    for(int tempz=-2;tempz<=2;tempz++){
                        if(!((tempx == -2 || tempx == 2)&&(tempz == -2 || tempz == 2))){
                            if(GetBlock(x+tempx,y+tempy,z+tempz,(chunk.coordinateX,chunk.coordinateY,chunk.coordinateZ))==0){
                                SetBlockWithoutUpdating(x+tempx,y+tempy,z+tempz,(chunk.coordinateX,chunk.coordinateY,chunk.coordinateZ),7);
                            }
                        }
                    }
                }
            }
            for(int tempy=6;tempy<=7;tempy++){
                for(int tempx=-1;tempx<=1;tempx++){
                    for(int tempz=-1;tempz<=1;tempz++){
                        if(!((tempx == -1 || tempx == 1)&&(tempz == -1 || tempz == 1))){
                            if(GetBlock(x+tempx,y+tempy,z+tempz,(chunk.coordinateX,chunk.coordinateY,chunk.coordinateZ))==0){
                                SetBlockWithoutUpdating(x+tempx,y+tempy,z+tempz,(chunk.coordinateX,chunk.coordinateY,chunk.coordinateZ),7);
                            }
                        }
                    }
                }
            }
            for(int i=1;i<=6;i++){
                SetBlockWithoutUpdating(x,y+i,z,(chunk.coordinateX,chunk.coordinateY,chunk.coordinateZ),5);
            }
            SetBlockWithoutUpdating(x,y,z,(chunk.coordinateX,chunk.coordinateY,chunk.coordinateZ),2);
        }
        public double TerrainNoise(int x,int z){
            return OpenSimplex2.Noise2(seed,((double)x)/2000,((double)z)/2000)*160
                        + OpenSimplex2.Noise2(seed,((double)x)/400,((double)z)/400)*32
                        + OpenSimplex2.Noise2(seed,((double)x)/100,((double)z)/100)*8;
        }
        //TOFIX: On big numbers terrain generation breaks and trees don't spawn
        public double MountainNoise(int x,int z){
            return Math.Pow(OpenSimplex2.Noise2(seed,((double)x)/2000,((double)z)/2000)*15
                        + OpenSimplex2.Noise2(seed,((double)x)/400,((double)z)/400)*4
                        + OpenSimplex2.Noise2(seed,((double)x)/100,((double)z)/100),2);
        }
        static void NormalizeChunkCoordinates(ref int x,ref int y,ref int z,ref (int x,int y,int z) chunkCoordinate){
            if(x>63){
                chunkCoordinate.x+=1;
                x-=64;
            }
            if(x<0){
                chunkCoordinate.x-=1;
                x+=64;
            }
            if(y>63){
                chunkCoordinate.y+=1;
                y-=64;
            }
            if(y<0){
                chunkCoordinate.y-=1;
                y+=64;
            }
            if(z>63){
                chunkCoordinate.z+=1;
                z-=64;
            }
            if(z<0){
                chunkCoordinate.z-=1;
                z+=64;
            }
        }
    }
}