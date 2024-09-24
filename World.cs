using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static VoxelTechDemo.VoxelRenderer;

namespace VoxelTechDemo{
    public class World{
        public ConcurrentDictionary<(int,int,int),Chunk> WorldMap = new();
        readonly long seed;
        //MaxHeight need to divisable by ChunkSize
        public const int MaxHeight = 512;
        public World(long seed){
            this.seed = seed;
        }
        public void SetBlock(int x,int y,int z,(int, int, int) chunkCoordinate, byte Id, BlockFace blockSide, BoundingBox PlayerHitBox){
            switch(blockSide){
                case BlockFace.Front:
                    if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x,y,z-1),new Vector3(x+1,y+1,z)))){
                        SetBlock(x,y,z-1,chunkCoordinate,Id);
                    }
                    break;
                case BlockFace.Back:
                    if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x,y,z+1),new Vector3(x+1,y+1,z+2)))){
                        SetBlock(x,y,z+1,chunkCoordinate,Id);
                    }
                    break;
                case BlockFace.Right:
                    if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x-1,y,z),new Vector3(x,y+1,z+1)))){
                        SetBlock(x-1,y,z,chunkCoordinate,Id);
                    }
                    break;
                case BlockFace.Left:
                    if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x+1,y,z),new Vector3(x+2,y+1,z+1)))){
                        SetBlock(x+1,y,z,chunkCoordinate,Id);
                    }
                    break;
                case BlockFace.Top:
                    if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x,y+1,z),new Vector3(x+1,y+2,z+1)))){
                        SetBlock(x,y+1,z,chunkCoordinate,Id);
                    }
                    break;
                case BlockFace.Bottom:
                    if(!PlayerHitBox.Intersects(new BoundingBox(new Vector3(x,y-1,z),new Vector3(x+1,y,z+1)))){
                        SetBlock(x,y-1,z,chunkCoordinate,Id);
                    }
                    break;
            }
        }
        public void SetBlock(int x, int y, int z, (int x,int y,int z) chunkCoordinate,byte Id){
            NormalizeChunkCoordinates(ref x,ref y,ref z,ref chunkCoordinate);
            if(!WorldMap.ContainsKey(chunkCoordinate)){
                return;
            }
            Chunk chunk = WorldMap[chunkCoordinate];
            chunk.blocks[x+(y*ChunkSize)+(z*square)]=Id;
            Dictionary<Chunk,VertexBuffer[]> buffers = new();
            buffers[chunk]=GenerateVertices(chunk);
            if(x==0){
                if(WorldMap.TryGetValue((chunkCoordinate.x-1,chunkCoordinate.y,chunkCoordinate.z),out chunk)){
                    buffers[chunk] = GenerateVertices(chunk);
                }
            }
            if(x==ChunkSize-1){
                if(WorldMap.TryGetValue((chunkCoordinate.x+1,chunkCoordinate.y,chunkCoordinate.z),out chunk)){
                    buffers[chunk] = GenerateVertices(chunk);
                }
            }
            if(y==0){
                if(WorldMap.TryGetValue((chunkCoordinate.x,chunkCoordinate.y-1,chunkCoordinate.z),out chunk)){
                    buffers[chunk] = GenerateVertices(chunk);
                }
            }
            if(y==ChunkSize-1){
                if(WorldMap.TryGetValue((chunkCoordinate.x,chunkCoordinate.y+1,chunkCoordinate.z),out chunk)){
                    buffers[chunk] = GenerateVertices(chunk);
                }
            }
            if(z==0){
                if(WorldMap.TryGetValue((chunkCoordinate.x,chunkCoordinate.y,chunkCoordinate.z-1),out chunk)){
                    buffers[chunk] = GenerateVertices(chunk);
                }
            }
            if(z==ChunkSize-1){
                if(WorldMap.TryGetValue((chunkCoordinate.x,chunkCoordinate.y,chunkCoordinate.z+1),out chunk)){
                    buffers[chunk] = GenerateVertices(chunk);
                }
            }
            foreach(KeyValuePair<Chunk,VertexBuffer[]> pair in buffers){
                pair.Key.vertexBufferOpaque?.Dispose();
                pair.Key.vertexBufferOpaque = pair.Value[0];
                pair.Key.vertexBufferTransparent?.Dispose();
                pair.Key.vertexBufferTransparent = pair.Value[1];
            }
        }
        public void SetBlockWithoutUpdating(int x,int y,int z,(int x,int y,int z) chunkCoordinate,byte Id){
            NormalizeChunkCoordinates(ref x,ref y,ref z,ref chunkCoordinate);
            WorldMap.TryAdd(chunkCoordinate,new(chunkCoordinate,this));
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
        public void GenerateChunkLine(int x,int z){
            for(int y=0;y<MaxHeight/ChunkSize;y++){
                WorldMap.TryAdd((x,y,z),new((x,y,z),this));
            }
            GenerateTerrain(x,z);
        }
        public void GenerateTerrain(int chunkX,int chunkZ){
            Chunk[] chunks = new Chunk[MaxHeight/ChunkSize];
            for(int y=0;y<MaxHeight/ChunkSize;y++){
                chunks[y]=WorldMap[(chunkX,y,chunkZ)];
            }
            byte[] chunkBlocks = chunks[0].blocks;
            for(int x=0;x<ChunkSize;x++){
                for(int z=0;z<ChunkSize;z++){
                    // If yLevel below 0 needs to be generated, MountainNoise needs to floored before casting to int
                    int yLevel = 50+(int)MountainNoise((double)chunkX*ChunkSize+x,(double)chunkZ*ChunkSize+z);
                    int blockPossition = x+z*square;
                    for(int y=ChunkSize*(MaxHeight/ChunkSize)-1;y>=0;y--){
                        if(y%ChunkSize==ChunkSize-1){
                            chunkBlocks = chunks[y/ChunkSize].blocks;
                            blockPossition+=square;
                        }
                        blockPossition-=ChunkSize;
                        if(y<=yLevel){
                            if(y>=yLevel-2){
                                if(y == yLevel){
                                    chunkBlocks[blockPossition]=1;
                                }
                                else{
                                    chunkBlocks[blockPossition]=2;
                                }
                            }
                            else{
                                chunkBlocks[blockPossition]=3;
                            } 
                            if(y==yLevel){
                                if(yLevel<65){
                                    chunkBlocks[blockPossition]=12;
                                }
                                if(yLevel<62){
                                    chunkBlocks[blockPossition]=11;
                                }
                            }
                        }
                        else{
                            if(y<64){
                                chunkBlocks[blockPossition]=14;
                            }
                        }
                    }
                    if(OpenSimplex2.Noise2(seed,(double)chunkX*ChunkSize+x,(double)chunkZ*ChunkSize+z) > 0.95f && yLevel>=65){
                        CreateTree(x,yLevel%ChunkSize,z, chunks[yLevel/ChunkSize]);
                    }
                }
            }
            for(int i=0;i<MaxHeight/ChunkSize;i++){
                chunks[i].IsGenerated = true;
            }
        }
        private void CreateTree(int x,int y, int z, Chunk chunk){
            for(int tempy=4;tempy<=5;tempy++){
                for(int tempx=-2;tempx<=2;tempx++){
                    for(int tempz=-2;tempz<=2;tempz++){
                        if(!((tempx == -2 || tempx == 2)&&(tempz == -2 || tempz == 2))){
                            if(GetBlock(x+tempx,y+tempy,z+tempz,chunk.coordinates)==0){
                                SetBlockWithoutUpdating(x+tempx,y+tempy,z+tempz,chunk.coordinates,7);
                            }
                        }
                    }
                }
            }
            for(int tempy=6;tempy<=7;tempy++){
                for(int tempx=-1;tempx<=1;tempx++){
                    for(int tempz=-1;tempz<=1;tempz++){
                        if(!((tempx == -1 || tempx == 1)&&(tempz == -1 || tempz == 1))){
                            if(GetBlock(x+tempx,y+tempy,z+tempz,chunk.coordinates)==0){
                                SetBlockWithoutUpdating(x+tempx,y+tempy,z+tempz,chunk.coordinates,7);
                            }
                        }
                    }
                }
            }
            for(int i=1;i<=6;i++){
                SetBlockWithoutUpdating(x,y+i,z,chunk.coordinates,5);
            }
            SetBlockWithoutUpdating(x,y,z,chunk.coordinates,2);
        }
        // TOFIX: On big x and z coordinates (int.MaxValue/64) trees don't spawn
        public double MountainNoise(double x,double z){
            return Math.Pow(OpenSimplex2.Noise2(seed,x/2000,z/2000)*15
                        + OpenSimplex2.Noise2(seed,x/400,z/400)*4
                        + OpenSimplex2.Noise2(seed,x/100,z/100),2);
        }
        static void NormalizeChunkCoordinates(ref int x,ref int y,ref int z,ref (int x,int y,int z) chunkCoordinate){
            while(x>ChunkSize-1){
                chunkCoordinate.x+=1;
                x-=ChunkSize;
            }
            while(x<0){
                chunkCoordinate.x-=1;
                x+=ChunkSize;
            }
            while(y>ChunkSize-1){
                chunkCoordinate.y+=1;
                y-=ChunkSize;
            }
            while(y<0){
                chunkCoordinate.y-=1;
                y+=ChunkSize;
            }
            while(z>ChunkSize-1){
                chunkCoordinate.z+=1;
                z-=ChunkSize;
            }
            while(z<0){
                chunkCoordinate.z-=1;
                z+=ChunkSize;
            }
        }
    }
}